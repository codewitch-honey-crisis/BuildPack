﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CD
{
	/// <summary>
	/// Provides the ability to fix up a CodeDOM tree produced by <see cref="SlangParser"/>
	/// </summary>
#if GOKITLIB
	public 
#endif
	static class SlangPatcher
	{
		const BindingFlags _BindFlags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy;
		/// <summary>
		/// Patches the CodeDOM tree received from the <see cref="SlangParser"/> into something more usable, by resolving type information and replacing various elements in the CodeDOM graph
		/// </summary>
		/// <param name="compileUnits">The <see cref="CodeCompileUnit"/> objects to patch</param>
		public static void Patch(params CodeCompileUnit[] compileUnits)
			=> Patch((IEnumerable<CodeCompileUnit>)compileUnits);
		/// <summary>
		/// Patches the CodeDOM tree received from the <see cref="SlangParser"/> into something more usable, by resolving type information and replacing various elements in the CodeDOM graph
		/// </summary>
		/// <param name="compileUnits">The <see cref="CodeCompileUnit"/> objects to patch</param>
		public static void Patch(IEnumerable<CodeCompileUnit> compileUnits)
		{
			var resolver = new CodeDomResolver();
			foreach (var ccu in compileUnits)
				resolver.CompileUnits.Add(ccu);
			resolver.Refresh();
			restart:
			var working = -1;
			var oworking = 0;
			while(0!=working && oworking!=working)
			{
				oworking = working;
				working = 0;
				for(int ic = resolver.CompileUnits.Count,i=0;i<ic;++i)
				{
					CodeDomVisitor.Visit(resolver.CompileUnits[i], (ctx) => {
					var co = ctx.Target as CodeObject;
						if (null != co && co.UserData.Contains("slang:unresolved"))
						{
							++working;
							_Patch(ctx.Target as CodeFieldReferenceExpression, ctx, resolver);
							_Patch(ctx.Target as CodeVariableDeclarationStatement, ctx, resolver);
							_Patch(ctx.Target as CodeVariableReferenceExpression, ctx, resolver);
							_Patch(ctx.Target as CodeDelegateInvokeExpression, ctx, resolver);
							_Patch(ctx.Target as CodeObjectCreateExpression, ctx, resolver);
							_Patch(ctx.Target as CodeIndexerExpression, ctx, resolver);
							_Patch(ctx.Target as CodeMemberMethod, ctx, resolver);
							_Patch(ctx.Target as CodeMemberProperty, ctx, resolver);
							_Patch(ctx.Target as CodeTypeReference, ctx, resolver);
						}
					});
				}
				resolver.Refresh();
			}
			oworking = working;
			working = 0;
			if (0 < oworking)
			{
				// one last time
				for (int ic = resolver.CompileUnits.Count, i = 0; i < ic; ++i)
				{
					CodeDomVisitor.Visit(resolver.CompileUnits[i], (ctx) =>
					{
						var co = ctx.Target as CodeObject;
						if (null != co && co.UserData.Contains("slang:unresolved"))
						{
							++working;
							_Patch(ctx.Target as CodeFieldReferenceExpression, ctx, resolver);
							_Patch(ctx.Target as CodeVariableDeclarationStatement, ctx, resolver);
							_Patch(ctx.Target as CodeVariableReferenceExpression, ctx, resolver);
							_Patch(ctx.Target as CodeDelegateInvokeExpression, ctx, resolver);
							_Patch(ctx.Target as CodeObjectCreateExpression, ctx, resolver);
							_Patch(ctx.Target as CodeIndexerExpression, ctx, resolver);
							_Patch(ctx.Target as CodeMemberMethod, ctx, resolver);
							_Patch(ctx.Target as CodeMemberProperty, ctx, resolver);
							_Patch(ctx.Target as CodeTypeReference, ctx, resolver);
						}
					});
				}
				if (oworking != working)
					goto restart;
			}
		}
		/// <summary>
		/// Gets the next element that has not been resolved
		/// </summary>
		/// <param name="compileUnits">The compile units to search</param>
		/// <returns>A <see cref="CodeObject"/> representing the next code object that needs to be patched</returns>
		public static CodeObject GetNextUnresolvedElement(params CodeCompileUnit[] compileUnits)
			=> GetNextUnresolvedElement((IEnumerable<CodeCompileUnit>)compileUnits);
		/// <summary>
		/// Gets the next element that has not been resolved
		/// </summary>
		/// <param name="compileUnits">The compile units to search</param>
		/// <returns>A <see cref="CodeObject"/> representing the next code object that needs to be patched</returns>
		public static CodeObject GetNextUnresolvedElement(IEnumerable<CodeCompileUnit> compileUnits)
		{
			CodeObject result = null;
			foreach (var cu in compileUnits)
			{
				CodeDomVisitor.Visit(cu, (ctx) =>
				{
					var co = ctx.Target as CodeObject;
					if (null != co)
					{
						if (co.UserData.Contains("slang:unresolved"))
						{
							result = co;
							ctx.Cancel = true;
						}
					}
				});
				if (null != result)
					return result;
			}
			return null;
		}
		static void _Patch(CodeTypeReference tr, CodeDomVisitContext ctx, CodeDomResolver res)
		{
			if(null!=tr)
			{
				
				if(res.IsValidType(tr, res.GetScope(tr)))
				{
					tr.UserData.Remove("slang:unresolved");
					return;
				}
				// this is probably a nested type but with . instead of +
				// so now we need to crack it apart and hunt it down
				throw new NotImplementedException();
			}
		}
		static void _Patch(CodeObjectCreateExpression oc,CodeDomVisitContext ctx,CodeDomResolver res)
		{
			if (null != oc) // we have to check to see if this is a delegate creation expression
			{
				oc.UserData.Remove("slang:unresolved");
				if (1 == oc.Parameters.Count)
				{
					if (_IsDelegate(oc.Parameters[0], res))
					{
						var del = _GetDelegateFromFields(oc, oc.Parameters[0], res);
						CodeDomVisitor.ReplaceTarget(ctx, del);
					}
				}
			}
		}
		static void _Patch(CodeMemberProperty prop, CodeDomVisitContext ctx, CodeDomResolver resolver)
		{
			if (null != prop)
			{
				// TODO: add public implementation types
				prop.UserData.Remove("slang:unresolved");
			}
		}
		static void _Patch(CodeMemberMethod meth,CodeDomVisitContext ctx,CodeDomResolver resolver)
		{
			if (null != meth)
			{
				// TODO: Populate public implementation types
				meth.UserData.Remove("slang:unresolved");
				if ("Main" == meth.Name && (meth.Attributes & MemberAttributes.ScopeMask) == MemberAttributes.Static)
				{
					if (0 == meth.Parameters.Count && null == meth.ReturnType || "System.Void" == meth.ReturnType.BaseType)
					{
						var epm = new CodeEntryPointMethod();
						epm.Attributes = meth.Attributes;
						epm.LinePragma = meth.LinePragma;
						epm.StartDirectives.AddRange(meth.StartDirectives);
						epm.EndDirectives.AddRange(meth.EndDirectives);
						epm.Comments.AddRange(meth.Comments);
						epm.CustomAttributes.AddRange(meth.CustomAttributes);
						epm.ReturnTypeCustomAttributes.AddRange(meth.ReturnTypeCustomAttributes);
						epm.TypeParameters.AddRange(meth.TypeParameters);
						epm.PrivateImplementationType = meth.PrivateImplementationType;
						epm.ImplementationTypes.AddRange(meth.ImplementationTypes);
						epm.Name = meth.Name;
						epm.Statements.AddRange(meth.Statements);
						CodeDomVisitor.ReplaceTarget(ctx, epm);
					}
				}
				//return;
			}

		}
		static void _Patch(CodeVariableReferenceExpression vr,CodeDomVisitContext ctx,  CodeDomResolver resolver)
		{
			if(null!=vr)
			{
				if (vr.VariableName == "done")
					System.Diagnostics.Debug.WriteLine("done var ref hit");
				var scope = resolver.GetScope(vr);
				CodeTypeReference ctr;
				if (scope.VariableTypes.TryGetValue(vr.VariableName, out ctr))
				{
					if (!CodeDomResolver.IsNullOrVoidType(ctr))
					{
						if (vr.VariableName == "done")
							System.Diagnostics.Debug.WriteLine("done var resolved to var");
						vr.UserData.Remove("slang:unresolved");
						return;
					}
				}
				//else if (vr.VariableName == "done")
				//	System.Diagnostics.Debugger.Break();
				// we need to replace it.
				if (scope.ArgumentTypes.ContainsKey(vr.VariableName))
				{
					var a = new CodeArgumentReferenceExpression(vr.VariableName);
					CodeDomVisitor.ReplaceTarget(ctx,a);
					return;
					//args.Cancel = true;
				}
				else if (scope.FieldNames.Contains(vr.VariableName))
				{
					CodeTypeReference tref;
					// find out where it belongs.
					if (scope.ThisTargets.Contains(vr.VariableName))
					{
						var f = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx, f);
						//return;
					}
					else if (scope.TypeTargets.TryGetValue(vr.VariableName, out tref))
					{
						var f = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(tref), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx, f);
						//return;
					}
										
					return;
				}
				else if (scope.MethodNames.Contains(vr.VariableName))
				{
					CodeTypeReference tref;
					// find out where it belongs.
					if (scope.ThisTargets.Contains(vr.VariableName))
					{
						var m = new CodeMethodReferenceExpression(new CodeThisReferenceExpression(), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx, m);
						return;
						//args.Cancel = true;
					}
					if (scope.TypeTargets.TryGetValue(vr.VariableName, out tref))
					{
						var m = new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(tref), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx,m);
						return;
						//args.Cancel = true;
					}
				}
				else if (scope.PropertyNames.Contains(vr.VariableName))
				{
					CodeTypeReference tref;
					// find out where it belongs.
					if (scope.ThisTargets.Contains(vr.VariableName))
					{
						var p = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx,p);
						return;
						//args.Cancel = true;
					}
					else if (scope.TypeTargets.TryGetValue(vr.VariableName, out tref))
					{
						var p = new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(tref), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx,p);
						return;
						//args.Cancel = true;
					}
				}
				else if (scope.EventNames.Contains(vr.VariableName))
				{
					CodeTypeReference tref;
					// find out where it belongs.
					if (scope.ThisTargets.Contains(vr.VariableName))
					{
						var e = new CodeEventReferenceExpression(new CodeThisReferenceExpression(), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx,e);
						return;
						//args.Cancel = true;
					}
					else if (scope.TypeTargets.TryGetValue(vr.VariableName, out tref))
					{
						var e = new CodeEventReferenceExpression(new CodeTypeReferenceExpression(tref), vr.VariableName);
						CodeDomVisitor.ReplaceTarget(ctx,e);
						return;
						//args.Cancel = true;
					}
				}
				return;
			}
			return;
		}
		static void _Patch(CodeIndexerExpression indexer,CodeDomVisitContext ctx,CodeDomResolver resolver)
		{
			if (null != indexer)
			{

				if (indexer.TargetObject.UserData.Contains("slang:unresolved"))
					return;
				
				var ctr = resolver.GetTypeOfExpression(indexer.TargetObject);
				if (null != ctr.ArrayElementType && 0 < ctr.ArrayRank)
				{
					var ai = new CodeArrayIndexerExpression(indexer.TargetObject);
					ai.Indices.AddRange(indexer.Indices);
					CodeDomVisitor.ReplaceTarget(ctx, ai);
					
					//return;
				}
				indexer.UserData.Remove("slang:unresolved");
				
			}
		}
		static void _Patch(CodeDelegateInvokeExpression di,CodeDomVisitContext ctx,CodeDomResolver resolver)
		{
			if (null != di)
			{
				// these can be method invokes depending.
				if (null != di.TargetObject)
				{
					// probably already fixed in an earlier visit
					var mr = di.TargetObject as CodeMethodReferenceExpression;
					if (null != mr)
					{
						var mi = new CodeMethodInvokeExpression(mr);
						mi.Parameters.AddRange(di.Parameters);
						CodeDomVisitor.ReplaceTarget(ctx,mi);
						
						//args.Cancel = true;
					}
					else
					{
						var cco = di.TargetObject as CodeObject;
						if (null == cco)
							System.Diagnostics.Debugger.Break();
					}

				}
				else
				{
					// we really are at a loss here as the only way this would be valid is
					// through a self call on a delegate object itself, like this();
					throw new InvalidProgramException("Untargeted delegate invoke produced by slang parser!");
				}
				//return;
			}
		}
		static bool _HasUnresolved(CodeObject target)
		{
			if (target.UserData.Contains("slang:unresolved")) return true;
			var result = false;
			CodeDomVisitor.Visit(target, (ctx) =>
			{
				var co = ctx.Target as CodeObject;
				if(null!=co && co.UserData.Contains("slang:unresolved"))
				{
					result = true;
					ctx.Cancel = true;
				}
			});
			return result;
		}
		static void _Patch(CodeVariableDeclarationStatement vd,CodeDomVisitContext ctx, CodeDomResolver resolver)
		{
			if (null != vd)
			{
				if (vd.Name == "done")
					System.Diagnostics.Debug.WriteLine("debug var decl hit");
				if (CodeDomResolver.IsNullOrVoidType(vd.Type))
				{
					if (null == vd.InitExpression)
						throw new ArgumentException("The code contains an incomplete variable declaration.", "resolver");
					if (!_HasUnresolved(vd.InitExpression))
					{
						var t = resolver.GetTypeOfExpression(vd.InitExpression, resolver.GetScope(vd.InitExpression));

						vd.Type = t;
						if (!CodeDomResolver.IsNullOrVoidType(t))
						{
							if (vd.Name == "done")
								System.Diagnostics.Debug.WriteLine("debug var decl resolved");

							vd.UserData.Remove("slang:unresolved");

						}
					}
				}
			}
		}
		static void _Patch(CodeFieldReferenceExpression fr, CodeDomVisitContext ctx, CodeDomResolver resolver)
		{
			if (null != fr)
			{
				
				// this probably means part of our field has been resolved, or at the very least
				// it does not come from a rooted var ref.
				if (!fr.TargetObject.UserData.Contains("slang:unresolved"))
				{

					var scope = resolver.GetScope(fr);
					var binder = new CodeDomBinder(scope);
					var t = resolver.GetTypeOfExpression(fr.TargetObject);
					if (null!=t && CodeDomResolver.IsNullOrVoidType(t) && fr.TargetObject is CodeVariableReferenceExpression)
						return; // can't patch this field yet - it's part of a var reference that hasn't been filled in
					var isStatic = false;
					var tre = fr.TargetObject as CodeTypeReferenceExpression;
					if (null != tre)
						isStatic = true;
					var tt = resolver.TryResolveType(isStatic ? tre.Type: t, scope);
					if (null == tt)
						throw new InvalidOperationException(string.Format("The type {0} could not be resolved.", t.BaseType));
					var td = tt as CodeTypeDeclaration;
					// TODO: This code could be a lot faster if we added some functionality to the binder
					// we're just checking to see if the method, property or field exists
					var m = binder.GetField(tt, fr.FieldName, _BindFlags);
					if (null != m)
					{
						fr.UserData.Remove("slang:unresolved");
						return;
					}
					m = binder.GetEvent(tt, fr.FieldName, _BindFlags);
					if (null != m)
					{
						var er = new CodeEventReferenceExpression(fr.TargetObject, fr.FieldName);
						CodeDomVisitor.ReplaceTarget(ctx, er);
						return;
					}
					var ml = binder.GetMethodGroup(tt, fr.FieldName, _BindFlags);
					if (0 < ml.Length)
					{
						var mr = new CodeMethodReferenceExpression(fr.TargetObject, fr.FieldName);
						CodeDomVisitor.ReplaceTarget(ctx, mr);
						return;
					}
					ml = binder.GetPropertyGroup(tt, fr.FieldName, _BindFlags);
					if (0 < ml.Length)
					{
						var pr = new CodePropertyReferenceExpression(fr.TargetObject, fr.FieldName);
						CodeDomVisitor.ReplaceTarget(ctx, pr);
						return;
					}
					throw new InvalidProgramException(string.Format("Cannot deterimine the target reference {0}", fr.FieldName));
				}
				// TODO: This used to be first but I moved it here.
				// This shouldn't be done first because it's resolving types before fields and 
				// that is a no no. I still need to make sure it doesn't break things
				var path = _GetUnresRootPathOfExpression(fr);
				if (null != path)
				{
					// now we have something to work with.
					var scope = resolver.GetScope(fr);
					var sa = path.Split('.');
					if (1 == sa.Length)
					{
						System.Diagnostics.Debugger.Break();
						throw new NotImplementedException();
					}
					else
					{
						object t = null;
						string tn = null;
						CodeExpression tf = fr;
						CodeExpression ptf = null;
						CodeTypeReference ctr = null;
						for (var i = sa.Length - 1; i >= 1; --i)
						{
							tn = string.Join(".", sa, 0, i);
							ptf = tf;
							tf = _GetTargetOfExpression(tf);
							ctr = new CodeTypeReference(tn);
							t = resolver.TryResolveType(ctr, scope);
							if (null != t)
								break;
						}
						if (null != t)
						{
							var tt = t as Type;
							if (null != tt)
								ctr = new CodeTypeReference(tt);
							else
								ctr = resolver.GetQualifiedType(ctr, scope);
							// we found a type reference
							_SetTargetOfExpression(ptf, new CodeTypeReferenceExpression(ctr));
							return;
							//args.Cancel = true;
						}
					}
				}

			}
		}
		static CodeDelegateCreateExpression _GetDelegateFromFields(CodeObjectCreateExpression oc, CodeExpression target, CodeDomResolver res)
		{
			var v = target as CodeVariableReferenceExpression;
			if (null != v)
			{
				var scope = res.GetScope(v);
				if (scope.MemberNames.Contains(v.VariableName))
					return new CodeDelegateCreateExpression(oc.CreateType, new CodeThisReferenceExpression(), v.VariableName);
			}
			throw new NotImplementedException();

		}
		static bool _IsDelegate(CodeExpression target, CodeDomResolver res)
		{
			var v = target as CodeVariableReferenceExpression;
			if (null != v && v.UserData.Contains("slang:unresolved"))
			{
				var scope = res.GetScope(target);
				if (scope.MemberNames.Contains(v.VariableName))
					return true;
			}
			return false;
		}
		static string _GetUnresRootPathOfExpression(CodeExpression t)
		{
			var result = _GetNameOfExpression(t);
			var sawVar = false;
			while (null != (t = _GetTargetOfExpression(t)))
			{
				if (!t.UserData.Contains("slang:unresolved"))
					return null;
				result = string.Concat(_GetNameOfExpression(t), ".", result);
				sawVar = null != (t as CodeVariableReferenceExpression);
			}
			if (!sawVar) return null;
			return result;
		}
		static CodeExpression _GetTargetOfExpression(CodeExpression e)
		{
			var fr = e as CodeFieldReferenceExpression;
			if (null != fr)
				return fr.TargetObject;
			var mr = e as CodeMethodReferenceExpression;
			if (null != mr)
				return mr.TargetObject;
			var pr = e as CodePropertyReferenceExpression;
			if (null != pr)
				return pr.TargetObject;
			var er = e as CodeEventReferenceExpression;
			if (null != er)
				return er.TargetObject;
			return null;
		}
		static void _SetTargetOfExpression(CodeExpression e, CodeExpression t)
		{
			var fr = e as CodeFieldReferenceExpression;
			if (null != fr)
			{
				fr.TargetObject = t;
				return;
			}
			var mr = e as CodeMethodReferenceExpression;
			if (null != mr)
			{
				mr.TargetObject = t;
				return;
			}
			var pr = e as CodePropertyReferenceExpression;
			if (null != pr)
			{
				pr.TargetObject = t;
				return;
			}
			var er = e as CodeEventReferenceExpression;
			if (null != er)
			{
				er.TargetObject = t;
				return;
			}
			throw new ArgumentException("Invalid expression", nameof(e));
		}
		static string _GetNameOfExpression(CodeExpression e)
		{
			var fr = e as CodeFieldReferenceExpression;
			if (null != fr)
				return fr.FieldName;
			var mr = e as CodeMethodReferenceExpression;
			if (null != mr)
				return mr.MethodName;
			var pr = e as CodePropertyReferenceExpression;
			if (null != pr)
				return pr.PropertyName;
			var er = e as CodeEventReferenceExpression;
			if (null != er)
				return er.EventName;
			var vr = e as CodeVariableReferenceExpression;
			if (null != vr)
				return vr.VariableName;
			return null;
		}
	}
}
