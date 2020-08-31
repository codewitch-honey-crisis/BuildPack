#define NOPATHS
using System;
using System.CodeDom;
using System.Reflection;

namespace CD
{
	/// <summary>
	/// Performed each time the visitor visits an element
	/// </summary>
	/// <param name="args">A <see cref="CodeDomVisitContext"/> object describing the current target and context</param>
#if GOKITLIB
	public 
#endif
	delegate void CodeDomVisitAction(CodeDomVisitContext args);

	/// <summary>
	/// Indicates the targets for the visit operation
	/// </summary>
	[Flags]
#if GOKITLIB
	public 
#endif
	enum CodeDomVisitTargets
	{
		/// <summary>
		/// <see cref="CodeAttributeDeclaration"/> objects should be visited
		/// </summary>
		Attributes=		0x01,
		/// <summary>
		/// <see cref="CodeTypeMember"/> objects should be visited
		/// </summary>
		Members =		0x02,
		/// <summary>
		/// <see cref="CodeStatement"/> and <see cref="CodeCatchClause"/> objects should be visited
		/// </summary>
		Statements =	0x04,
		/// <summary>
		/// <see cref="CodeExpression"/> objects should be visited
		/// </summary>
		Expressions =	0x08,
		/// <summary>
		/// <see cref="CodeTypeDeclaration"/> objects should be visited
		/// </summary>
		Types =			0x10,
		/// <summary>
		/// <see cref="CodeTypeReference"/> and <see cref="CodeTypeParameter"/> objects should be visited
		/// </summary>
		TypeRefs =		0x20,
		/// <summary>
		/// <see cref="CodeComment"/> objects should be visited
		/// </summary>
		Comments =		0x40,
		/// <summary>
		/// <see cref="CodeDirective"/> and <see cref="CodeLinePragma"/> objects should be visited
		/// </summary>
		Directives =	0x80,
		/// <summary>
		/// Indicates that only entries that have been Mark()ed are visited
		/// </summary>
		Marked = 0x100,
		/// <summary>
		/// All objects should be visited
		/// </summary>
		All = Attributes | Members | Statements | Expressions | Types | TypeRefs | Comments | Directives
	}
	/// <summary>
	/// Represents the current context of the visit operatoion
	/// </summary>
#if GOKITLIB
	public 
#endif
	class CodeDomVisitContext
	{
		/// <summary>
		/// Indicates root where the visit operation started
		/// </summary>
		public object Root;
		/// <summary>
		/// Indicates the parent of the current target
		/// </summary>
		public object Parent;
		/// <summary>
		/// The name of the parent member retrieved to navigate to the target.
		/// </summary>
		public string Member;
		/// <summary>
		/// Indicates the index of the target in the parent's collection, or -1 if not in a collection
		/// </summary>
		public int Index;
		/// <summary>
		/// Indicates the path to the object from the root, in C# format
		/// </summary>
		public string Path;

		/// <summary>
		/// Indicates the target of the visit operation
		/// </summary>
		public object Target;
		/// <summary>
		/// A <see cref="CodeDomVisitTargets"/> flag set that tells the visitor which objects should be visited
		/// </summary>
		public CodeDomVisitTargets Targets;
		/// <summary>
		/// True if the visitation should immediately be canceled. No more notifications will occur.
		/// </summary>
		public bool Cancel;

		internal CodeDomVisitContext() { }
		internal CodeDomVisitContext Set(object root,object parent,string member,int index,string path, object target,CodeDomVisitTargets targets)
		{
			Root = root;
			Parent = parent;
			Member = member;
			Index = index;
			Path = path;
			Target = target;
			Targets = targets;
			return this;
		}
	}

	/// <summary>
	/// Visits a CodeDOM abstract syntax tree, performing the requested action at each visit.
	/// </summary>
#if GOKITLIB
	public 
#endif
	class CodeDomVisitor
	{
		/// <summary>
		/// Begins a visit operation
		/// </summary>
		/// <param name="obj">The code dom object to visit</param>
		/// <param name="action">A <see cref="CodeDomVisitAction"/> that indicates the action to perform</param>
		/// <param name="targets">A <see cref="CodeDomVisitTargets"/> flag set that indicates which objects to visit</param>
		public static void Visit(object obj, CodeDomVisitAction action,CodeDomVisitTargets targets)
		{
			var args = new CodeDomVisitContext();
			args=args.Set(obj, null,null,-1,"", obj,targets);
			var cc = obj as CodeComment;
			if (null != cc)
			{
				_VisitComment(cc,args, action);
				return;
			}
			var ccu = obj as CodeCompileUnit;
			if (null != ccu)
			{
				_VisitCompileUnit(ccu,args, action);
				return;
			}
			var cd = obj as CodeDirective;
			if (null != ccu)
			{
				_VisitDirective(cd, args, action);
				return;
			}
			var ce = obj as CodeExpression;
			if (null != ce)
			{
				_VisitExpression(ce, args, action);
				return;
			}
			var cns = obj as CodeNamespace;
			if (null != cns)
			{
				_VisitNamespace(cns, args, action);
				return;
			}
			var cni = obj as CodeNamespaceImport;
			if (null != cni)
			{
				_VisitNamespaceImport(cni, args, action);
				return;
			}
			var cs = obj as CodeStatement;
			if (null != cs)
			{
				_VisitStatement(cs, args, action);
				return;
			}
			var ctm = obj as CodeTypeMember;
			if (null != ctm)
			{
				_VisitTypeMember(ctm, args, action);
				return;
			}
			var ctp = obj as CodeTypeParameter;
			if (null != ctp)
			{
				_VisitTypeParameter(ctp, args, action);
				return;
			}
			var ctr = obj as CodeTypeReference;
			if (null != ctr)
			{
				_VisitTypeReference(ctr, args, action);
				return;
			}
			var cad = obj as CodeAttributeDeclaration;
			if(null!=cad)
			{
				_VisitAttributeDeclaration(cad, args, action);
				return;
			}
			var ccc = obj as CodeCatchClause;
			if (null != ccc)
			{
				_VisitCatchClause(ccc, args, action);
				return;
			}
			var clp = obj as CodeLinePragma;
			if(null!=clp)
			{
				_VisitLinePragma(clp, args, action);
				return;
			}
		}
		/// <summary>
		/// Begins a visit operation
		/// </summary>
		/// <param name="obj">The code dom object to visit</param>
		/// <param name="action">A <see cref="CodeDomVisitAction"/> that indicates the action to perform</param>
		public static void Visit(object obj, CodeDomVisitAction action)
		{
			Visit(obj, action, CodeDomVisitTargets.All);
		}
		/// <summary>
		/// Marks an object for visitation
		/// </summary>
		/// <remarks>When the Marked visit target flag is specified, the visitor will only visit marked nodes</remarks>
		/// <param name="obj">The object to mark</param>
		/// <returns>Returns <paramref name="obj"/></returns>
		public static T Mark<T>(T obj) where T:CodeObject
		{
			obj.UserData.Add("codedomgokit:visit",true);
			return obj;
		}
		/// <summary>
		/// Unmarks an object for visitation
		/// </summary>
		/// <remarks>When the Marked visit target flag is specified, the visitor will only visit marked nodes</remarks>
		/// <param name="obj">The object to mark</param>
		/// <returns>Returns <paramref name="obj"/></returns>
		public static T Unmark<T>(T obj) where T : CodeObject
		{
			obj.UserData.Remove("codedomgokit:visit");
			return obj;
		}
		/// <summary>
		/// A helper method to replace the current target with a new value during the visit operation
		/// </summary>
		/// <param name="ctx">The visit context</param>
		/// <param name="newTarget">The target value to replace the current target with</param>
		/// <remarks>This method is intended to be called from inside the anonymous visit method. This method uses reflection.</remarks>
		public static void ReplaceTarget(CodeDomVisitContext ctx, object newTarget)
		{
			try
			{

				var ma = ctx.Parent.GetType().GetMember(ctx.Member, BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.GetProperty);

				var pi = ma[0] as PropertyInfo;
				if (-1 != ctx.Index)
				{
					var l = pi.GetValue(ctx.Parent) as System.Collections.IList;
					l[ctx.Index] = newTarget;
					return;
				}
				pi.SetValue(ctx.Parent, newTarget);
			}
			catch(TargetInvocationException tex)
			{
				throw tex.InnerException;
			}
		}
		static bool _CanVisit(CodeObject obj,CodeDomVisitContext args)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Marked))
				return true;
			return obj.UserData.Contains("codedomgokit:visit");
		}
		static bool _HasTarget(CodeDomVisitContext args, CodeDomVisitTargets target)
		{
			return target == (args.Targets & target);
		}
		static void _VisitLinePragma(CodeLinePragma obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Directives)) return;
			if (args.Cancel) return;
			// nothing to do here other than report it.
			action(args);
		}
		static void _VisitComment(CodeComment obj,CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Comments) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// nothing to do here other than report it.
			action(args);
		}
		static void _VisitDirective(CodeDirective obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (args.Cancel || !_CanVisit(obj, args)) return;

			var rd = obj as CodeRegionDirective;
			if (null != rd )
			{
				_VisitRegionDirective(rd, args, action);
				return;
			}
			var cp = obj as CodeChecksumPragma;
			if(null!=cp )
			{
				_VisitChecksumPragma(cp, args, action);
				return;
			}
			throw new NotSupportedException("Unsupported directive type in graph");
		}
		static void _VisitRegionDirective(CodeRegionDirective obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Directives) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			action(args);
		}
		static void _VisitChecksumPragma(CodeChecksumPragma obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Directives) || !_CanVisit(obj, args)) return;
			if (args.Cancel) return;
			action(args);
		}
		static void _VisitNamespaceImport(CodeNamespaceImport obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (args.Cancel || !_CanVisit(obj, args)) return;
			// nothing to do here other than report it.
			action(args);
		}
		static void _VisitAttributeDeclaration(CodeAttributeDeclaration obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Attributes) ) return;
			if (args.Cancel)
				return;
			// report it
			action(args);
			if (null != obj.AttributeType && _CanVisit(obj.AttributeType,args))
				_VisitTypeReference(obj.AttributeType, args.Set(args.Root, obj,"AttributeType",-1,_BuildPath(args.Path,"AttributeType",-1), obj.AttributeType, args.Targets), action);
			if (args.Cancel)
				return;
			for(int ic=obj.Arguments.Count,i=0;i<ic;++i)
			{
				var arg = obj.Arguments[i];
				_VisitAttributeArgument(arg, args.Set(args.Root, obj,"Arguments",i,_BuildPath(args.Path,"Arguments",i), arg, args.Targets),action);
				if (args.Cancel)
					return;
			}
		}
		static void _VisitAttributeArgument(CodeAttributeArgument obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Attributes)) return;
			if (args.Cancel)
				return;
			// report it
			action(args);
			if (args.Cancel)
				return;
			if (null != obj.Value && _CanVisit(obj.Value,args))
				_VisitExpression(obj.Value, args.Set(args.Root, obj,"Value",-1,_BuildPath(args.Path,"Value",-1), obj.Value, args.Targets), action);
		}
		static void _VisitCompileUnit(CodeCompileUnit obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (args.Cancel || !_CanVisit(obj,args)) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir, args.Targets), action);
					if (args.Cancel) return;
				}
				
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.AssemblyCustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.AssemblyCustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "AssemblyCustomAttributes",i,_BuildPath(args.Path,"AssemblyCustomAttributes",i), attrDecl, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			for (int ic=obj.Namespaces.Count,i=0;i<ic;++i)
			{
				var ns = obj.Namespaces[i];
				if(_CanVisit(ns,args))
					_VisitNamespace(ns, args.Set(args.Root, obj,"Namespaces",i,_BuildPath(args.Path,"Namespaces",i), ns, args.Targets), action);
				if (args.Cancel) return;
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
					_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitNamespace(CodeNamespace obj,CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic=obj.Comments.Count,i=0;i<ic;++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj,"Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
					if (args.Cancel) return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Types))
			{
				for(int ic=obj.Types.Count,i=0;i<ic;++i)
				{
					var decl = obj.Types[i];
					if(_CanVisit(decl,args))
						_VisitTypeDeclaration(decl, args.Set(args.Root, obj, "Types",i,_BuildPath(args.Path,"Types",i),decl, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitTypeDeclaration(CodeTypeDeclaration obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Types) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
					if (null != obj.LinePragma )
						_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",i), obj.LinePragma,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic = obj.Comments.Count, i = 0; i < ic; ++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj, "Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
					if (args.Cancel) return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			for (int ic=obj.TypeParameters.Count,i=0;i<ic;++i)
			{
				var ctp = obj.TypeParameters[i];
				if(_CanVisit(ctp,args))
					_VisitTypeParameter(ctp, args.Set(args.Root, obj,"TypeParameters",i,_BuildPath(args.Path,"TypeParameters",i), ctp, args.Targets), action);
				if (args.Cancel)
					return;
			}
			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic=obj.BaseTypes.Count,i=0;i<ic;++i)
				{
					var ctr = obj.BaseTypes[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj, "BaseTypes",i,_BuildPath(args.Path,"BaseTypes",i),ctr, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Members))
			{
				for(int ic=obj.Members.Count,i=0;i<ic;++i)
				{
					var ctm = obj.Members[i];
					if(_CanVisit(ctm,args))
						_VisitTypeMember(ctm, args.Set(args.Root, obj,"Members",i,_BuildPath(args.Path,"Members",i), ctm, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitTypeMember(CodeTypeMember obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (args.Cancel || !_CanVisit(obj,args)) return;
			var ce = obj as CodeMemberEvent;
			if(null!=ce)
			{
				_VisitMemberEvent(ce, args, action);
				return;
			}
			var cf = obj as CodeMemberField;
			if (null != cf)
			{
				_VisitMemberField(cf, args, action);
				return;
			}
			var cm = obj as CodeMemberMethod;
			if (null != cm)
			{
				_VisitMemberMethod(cm, args, action);
				return;
			}
			var cp = obj as CodeMemberProperty;
			if (null != cp)
			{
				_VisitMemberProperty(cp, args, action);
				return;
			}
			var cstm = obj as CodeSnippetTypeMember;
			if (null != cstm)
			{
				_VisitSnippetTypeMember(cstm, args, action);
				return;
			}
			var ctd = obj as CodeTypeDeclaration;
			if (null != ctd)
			{
				_VisitTypeDeclaration(ctd, args, action);
				return;
			}
			throw new NotSupportedException("The graph contains an unsupported type declaration");
		}
		static void _VisitMemberEvent(CodeMemberEvent obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Members) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for(int ic=obj.Comments.Count,i=0;i<ic;++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj,"Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic=obj.CustomAttributes.Count,i=0;i<ic;++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if(null!=obj.Type && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj,"Type",-1,_BuildPath(args.Path,"Type",-1), obj.Type, args.Targets), action);

			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i)
				{
					var ctr = obj.ImplementationTypes[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i), ctr, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null!=obj.PrivateImplementationType && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.PrivateImplementationType,args))
				_VisitTypeReference(obj.PrivateImplementationType, args.Set(args.Root, obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1), obj.PrivateImplementationType,args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitSnippetTypeMember(CodeSnippetTypeMember obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Members) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma", -1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic = obj.Comments.Count,i=0;i<ic;++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj, "Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitMemberField(CodeMemberField obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Members) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic = obj.Comments.Count, i = 0; i < ic; ++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj, "Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.Type && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj, "Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets), action);
			if (null != obj.InitExpression && _HasTarget(args, CodeDomVisitTargets.Expressions) && _CanVisit(obj.InitExpression,args))
				_VisitExpression(obj.InitExpression, args.Set(args.Root, obj,"InitExpression",-1,_BuildPath(args.Path,"InitExpression",-1), obj.InitExpression, args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitMemberMethod(CodeMemberMethod obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Members) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			var ctor = obj as CodeConstructor;
			if(null!=ctor)
			{
				_VisitConstructor(ctor, args, action);
				return;
			}
			var entryPoint = obj as CodeEntryPointMethod;
			if (null != entryPoint)
			{
				_VisitEntryPointMethod(entryPoint, args, action);
				return;
			}
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic = obj.Comments.Count, i = 0; i < ic; ++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj, "Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.ReturnType && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.ReturnType,args))
				_VisitTypeReference(obj.ReturnType, args.Set(args.Root, obj,"ReturnType",-1,_BuildPath(args.Path,"ReturnType",-1), obj.ReturnType,args.Targets), action);

			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic = obj.ImplementationTypes.Count, i = 0; i < ic; ++i)
				{
					var ctr = obj.ImplementationTypes[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj, "ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i), ctr, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.PrivateImplementationType && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.PrivateImplementationType,args))
				_VisitTypeReference(obj.PrivateImplementationType, args.Set(args.Root, obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1), obj.PrivateImplementationType,args.Targets), action);
			if(_HasTarget(args,CodeDomVisitTargets.Expressions))
			{
				for(int ic=obj.Parameters.Count,i=0;i<ic;++i)
				{
					var pd = obj.Parameters[i];
					if(_CanVisit(pd,args))
						_VisitParameterDeclarationExpression(pd, args.Set(args.Root, obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i), pd,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if(_HasTarget(args,CodeDomVisitTargets.Statements))
			{
				for(int ic=obj.Statements.Count,i=0;i<ic;++i)
				{
					var stmt = obj.Statements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj,"Statements",i,_BuildPath(args.Path,"Statements",i), stmt,args.Targets),action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitEntryPointMethod(CodeEntryPointMethod obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Members) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic = obj.Comments.Count, i = 0; i < ic; ++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj, "Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.ReturnType && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.ReturnType,args))
				_VisitTypeReference(obj.ReturnType, args.Set(args.Root, obj,"ReturnType",-1,_BuildPath(args.Path,"ReturnType",-1), obj.ReturnType,args.Targets), action);

			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic = obj.ImplementationTypes.Count, i = 0; i < ic; ++i)
				{
					var ctr = obj.ImplementationTypes[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj, "ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i), ctr, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.PrivateImplementationType && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.PrivateImplementationType,args))
				_VisitTypeReference(obj.PrivateImplementationType, args.Set(args.Root, obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1), obj.PrivateImplementationType,args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic=obj.Parameters.Count,i=0;i<ic;++i)
				{
					var pd = obj.Parameters[i];
					if(_CanVisit(pd,args))
						_VisitParameterDeclarationExpression(pd, args.Set(args.Root, obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i), pd, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic = obj.Statements.Count, i = 0; i < ic; ++i)
				{
					var stmt = obj.Statements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj, "Statements",i,_BuildPath(args.Path,"Statements",i), stmt,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitConstructor(CodeConstructor obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Members) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(!_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic = obj.Comments.Count, i = 0; i < ic; ++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj, "Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			
			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic = obj.ImplementationTypes.Count, i = 0; i < ic; ++i)
				{
					var ctr = obj.ImplementationTypes[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj, "ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i), ctr, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.PrivateImplementationType && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.PrivateImplementationType,args))
				_VisitTypeReference(obj.PrivateImplementationType, args.Set(args.Root, obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1), obj.PrivateImplementationType,args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic = obj.Parameters.Count, i = 0; i < ic; ++i)
				{
					var pd = obj.Parameters[i];
					if(_CanVisit(pd,args))
						_VisitParameterDeclarationExpression(pd, args.Set(args.Root, obj, "Parameters",i,_BuildPath(args.Path,"Parameters",i), pd, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic=obj.ChainedConstructorArgs.Count,i=0;i<ic;++i)
				{
					var ce = obj.ChainedConstructorArgs[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj,"ChainedConstructorArgs",i,_BuildPath(args.Path,"ChainedConstructorArgs",i), ce, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic = obj.BaseConstructorArgs.Count, i = 0; i < ic; ++i)
				{
					var ce = obj.BaseConstructorArgs[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj, "BaseConstructorArgs",i,_BuildPath(args.Path,"BaseConstructorArgs",i), ce, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic = obj.Statements.Count, i = 0; i < ic; ++i)
				{
					var stmt = obj.Statements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj, "Statements",i,_BuildPath(args.Path,"Statements",i), stmt,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitMemberProperty(CodeMemberProperty obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Members) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Comments))
			{
				for (int ic = obj.Comments.Count, i = 0; i < ic; ++i)
				{
					var cc = obj.Comments[i];
					if(_CanVisit(cc,args))
						_VisitCommentStatement(cc, args.Set(args.Root, obj, "Comments",i,_BuildPath(args.Path,"Comments",i), cc,args.Targets), action);
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.Type && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj,"Type",-1,_BuildPath(args.Path,"Type",-1), obj.Type,args.Targets), action);

			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic = obj.ImplementationTypes.Count, i = 0; i < ic; ++i)
				{
					var ctr = obj.ImplementationTypes[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj, "ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i), ctr, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.PrivateImplementationType && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.PrivateImplementationType,args))
				_VisitTypeReference(obj.PrivateImplementationType, args.Set(args.Root, obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1), obj.PrivateImplementationType,args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic = obj.Parameters.Count, i = 0; i < ic; ++i)
				{
					var pd = obj.Parameters[i];
					if(_CanVisit(pd,args))
						_VisitParameterDeclarationExpression(pd, args.Set(args.Root, obj, "Parameters",i,_BuildPath(args.Path,"Parameters",i), pd, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic=obj.GetStatements.Count,i=0;i<ic;++i)
				{
					var stmt = obj.GetStatements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj,"GetStatements",i,_BuildPath(args.Path,"GetStatements",i), stmt,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic = obj.SetStatements.Count, i = 0; i < ic; ++i)
				{
					var stmt = obj.SetStatements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj, "SetStatements",i,_BuildPath(args.Path,"SetStatements",i), stmt,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitExpression(CodeExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			var argr = obj as CodeArgumentReferenceExpression;
			if (null != argr)
			{
				_VisitArgumentReferenceExpression(argr, args, action);
				return;
			}
			var arrc = obj as CodeArrayCreateExpression;
			if (null != arrc)
			{
				_VisitArrayCreateExpression(arrc, args, action);
				return;
			}
			var aic = obj as CodeArrayIndexerExpression;
			if (null != aic)
			{
				_VisitArrayIndexerExpression(aic, args, action);
				return;
			}
			var br = obj as CodeBaseReferenceExpression;
			if (null != br)
			{
				_VisitBaseReferenceExpression(br, args, action);
				return;
			}
			var bo = obj as CodeBinaryOperatorExpression;
			if(null!=bo)
			{
				_VisitBinaryOperatorExpression(bo, args, action);
				return;
			}
			var cc = obj as CodeCastExpression;
			if (null != cc)
			{
				_VisitCastExpression(cc, args, action);
				return;
			}
			var cdv = obj as CodeDefaultValueExpression;
			if (null != cdv)
			{
				_VisitDefaultValueExpression(cdv, args, action);
				return;
			}
			var cdc = obj as CodeDelegateCreateExpression;
			if (null != cdc)
			{
				_VisitDelegateCreateExpression(cdc, args, action);
				return;
			}
			var cdi = obj as CodeDelegateInvokeExpression;
			if (null != cdi)
			{
				_VisitDelegateInvokeExpression(cdi, args, action);
				return;
			}
			var cd = obj as CodeDirectionExpression;
			if(null!=cd)
			{
				_VisitDirectionExpression(cd, args, action);
				return;
			}
			var cer = obj as CodeEventReferenceExpression;
			if (null != cer)
			{
				_VisitEventReferenceExpression(cer, args, action);
				return;
			}
			var cfr = obj as CodeFieldReferenceExpression;
			if (null != cfr)
			{
				_VisitFieldReferenceExpression(cfr, args, action);
				return;
			}
			var ci = obj as CodeIndexerExpression;
			if (null != ci)
			{
				_VisitIndexerExpression(ci, args, action);
				return;
			}
			var cmi = obj as CodeMethodInvokeExpression;
			if (null != cmi)
			{
				_VisitMethodInvokeExpression(cmi, args, action);
				return;
			}
			var cmr = obj as CodeMethodReferenceExpression;
			if (null != cmr)
			{
				_VisitMethodReferenceExpression(cmr, args, action);
				return;
			}
			var coc = obj as CodeObjectCreateExpression;
			if (null != coc)
			{
				_VisitObjectCreateExpression(coc, args, action);
				return;
			}
			var cpd = obj as CodeParameterDeclarationExpression;
			if(null!=cpd)
			{
				_VisitParameterDeclarationExpression(cpd, args, action);
				return;
			}
			var cp = obj as CodePrimitiveExpression;
			if (null != cp)
			{
				_VisitPrimitiveExpression(cp, args, action);
				return;
			}
			var cpr = obj as CodePropertyReferenceExpression;
			if (null != cpr)
			{
				_VisitPropertyReferenceExpression(cpr, args, action);
				return;
			}
			var cpsvr = obj as CodePropertySetValueReferenceExpression;
			if (null != cpsvr)
			{
				_VisitPropertySetValueReferenceExpression(cpsvr, args, action);
				return;
			}
			var cs = obj as CodeSnippetExpression;
			if(null!=cs)
			{
				_VisitSnippetExpression(cs,args,action);
				return;
			}
			var cthr = obj as CodeThisReferenceExpression;
			if (null != cthr)
			{
				_VisitThisReferenceExpression(cthr, args, action);
				return;
			}
			var cto = obj as CodeTypeOfExpression;
			if (null != cto)
			{
				_VisitTypeOfExpression(cto, args, action);
				return;
			}
			var ctr = obj as CodeTypeReferenceExpression;
			if (null != ctr)
			{
				_VisitTypeReferenceExpression(ctr, args, action);
				return;
			}
			var cvr = obj as CodeVariableReferenceExpression;
			if(null!=cvr)
			{
				_VisitVariableReferenceExpression(cvr, args, action);
				return;
			}
			throw new NotSupportedException("An expression that is not supported was part of the code graph");
		}
		static void _VisitVariableReferenceExpression(CodeVariableReferenceExpression obj,CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// just report it
			action(args);
		}
		static void _VisitTypeOfExpression(CodeTypeOfExpression obj,CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.Type && _HasTarget(args, CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj, "Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets), action);
		}
		static void _VisitSnippetExpression(CodeSnippetExpression obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// just report it
			action(args);
		}
		static void _VisitParameterDeclarationExpression(CodeParameterDeclarationExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (null != obj.Type && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj,"Type",-1,_BuildPath(args.Path,"Type",-1), obj.Type,args.Targets), action);
		}
		static void _VisitArgumentReferenceExpression(CodeArgumentReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// just report it
			action(args);
		}
		static void _VisitPrimitiveExpression(CodePrimitiveExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// just report it
			action(args);
		}
		static void _VisitDirectionExpression(CodeDirectionExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				if (null != obj.Expression && _CanVisit(obj.Expression,args))
					_VisitExpression(obj.Expression, args.Set(args.Root, obj, "Expression",-1,_BuildPath(args.Path,"Expression",-1),obj.Expression, args.Targets), action);
			}
		}
		static void _VisitEventReferenceExpression(CodeEventReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1), obj.TargetObject, args.Targets), action);
		}
		static void _VisitFieldReferenceExpression(CodeFieldReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1), obj.TargetObject, args.Targets), action);
		}
		static void _VisitPropertyReferenceExpression(CodePropertyReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1), obj.TargetObject, args.Targets), action);
		}
		static void _VisitPropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// just report it
			action(args);
		}
		static void _VisitMethodReferenceExpression(CodeMethodReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1), obj.TargetObject, args.Targets), action);
		}
		static void _VisitTypeReferenceExpression(CodeTypeReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.Type && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj,"Type",-1,_BuildPath(args.Path,"Type",-1), obj.Type,args.Targets), action);
		}
		static void _VisitDelegateCreateExpression(CodeDelegateCreateExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.DelegateType && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.DelegateType,args))
				_VisitTypeReference(obj.DelegateType, args.Set(args.Root, obj,"DelegateType",-1,_BuildPath(args.Path,"DelegateType",-1), obj.DelegateType,args.Targets),action);
			if (args.Cancel) return;
			if (null!=obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj, "TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject, args.Targets), action);
		}
		static void _VisitDelegateInvokeExpression(CodeDelegateInvokeExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return; 
			if (null != obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1), obj.TargetObject, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic=obj.Parameters.Count,i=0;i<ic;++i)
				{
					var ce = obj.Parameters[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i), ce, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitMethodInvokeExpression(CodeMethodInvokeExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.Method && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Method,args))
			{
				_VisitMethodReferenceExpression(obj.Method, args.Set(args.Root, obj,"Method",-1,_BuildPath(args.Path,"Method",-1), obj.Method, args.Targets), action);
				if (args.Cancel) return;
			}
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (var i=0;i< obj.Parameters.Count;++i)
				{
					var ce = obj.Parameters[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj, "Parameters",i,_BuildPath(args.Path,"Parameters",i),ce, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitBinaryOperatorExpression(CodeBinaryOperatorExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.Left && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Left,args))
				_VisitExpression(obj.Left, args.Set(args.Root, obj,"Left",-1,_BuildPath(args.Path,"Left",-1), obj.Left, args.Targets), action);
			if (args.Cancel) return;
			if (null != obj.Right && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Right,args))
				_VisitExpression(obj.Right, args.Set(args.Root, obj, "Right",-1,_BuildPath(args.Path,"Right",-1),obj.Right, args.Targets), action);
		}
		static void _VisitCastExpression(CodeCastExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.TargetType && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.TargetType,args))
				_VisitTypeReference(obj.TargetType, args.Set(args.Root, obj, "TargetType",-1,_BuildPath(args.Path,"TargetType",-1),obj.TargetType,args.Targets), action);
			if (args.Cancel) return;
			if (null != obj.Expression && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Expression,args))
				_VisitExpression(obj.Expression, args.Set(args.Root, obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1), obj.Expression, args.Targets), action);
		}
		static void _VisitDefaultValueExpression(CodeDefaultValueExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.Type && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj, "Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets), action);
		}
		static void _VisitBaseReferenceExpression(CodeBaseReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// just report it
			action(args);
		}
		static void _VisitThisReferenceExpression(CodeThisReferenceExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// just report it
			action(args);
		}
		static void _VisitArrayCreateExpression(CodeArrayCreateExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.CreateType && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.CreateType,args))
				_VisitTypeReference(obj.CreateType, args.Set(args.Root, obj,"CreateType",-1,_BuildPath(args.Path,"CreateType",-1), obj.CreateType,args.Targets),action);
			if (args.Cancel) return;
			if (null != obj.SizeExpression && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.SizeExpression,args))
			{
				_VisitExpression(obj.SizeExpression, args.Set(args.Root, obj,"SizeExpression",-1,_BuildPath(args.Path,"SizeExpression",-1), obj.SizeExpression, args.Targets), action);
				if (args.Cancel) return;
			}
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for(int ic=obj.Initializers.Count,i=0;i<ic;++i)
				{
					var ce = obj.Initializers[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj,"Initializers",i,_BuildPath(args.Path,"Initializers",i), ce, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitObjectCreateExpression(CodeObjectCreateExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.CreateType && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.CreateType,args))
				_VisitTypeReference(obj.CreateType, args.Set(args.Root, obj,"CreateType",-1,_BuildPath(args.Path,"CreateType",-1), obj.CreateType,args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic=obj.Parameters.Count,i=0;i<ic;++i)
				{
					var ce = obj.Parameters[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj, "Parameters",i,_BuildPath(args.Path,"Parameters",i),ce, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitArrayIndexerExpression(CodeArrayIndexerExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj, "TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1), obj.TargetObject, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic =obj.Indices.Count,i=0;i<ic;++i)
				{
					var ce = obj.Indices[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj,"Indices",i,_BuildPath(args.Path,"Indices",i), ce, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitIndexerExpression(CodeIndexerExpression obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Expressions) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.TargetObject && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TargetObject,args))
				_VisitExpression(obj.TargetObject, args.Set(args.Root, obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1), obj.TargetObject, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Expressions))
			{
				for (int ic = obj.Indices.Count, i = 0; i < ic; ++i)
				{
					var ce = obj.Indices[i];
					if(_CanVisit(ce,args))
						_VisitExpression(ce, args.Set(args.Root, obj, "Indices",i,_BuildPath(args.Path,"Indices",i), ce, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitStatement(CodeStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (args.Cancel) return;
			
			var ca = obj as CodeAssignStatement;
			if(null!=ca)
			{
				_VisitAssignStatement(ca, args, action);
				return;
			}
			var cae = obj as CodeAttachEventStatement;
			if (null != cae)
			{
				_VisitAttachEventStatement(cae, args, action);
				return;
			}
			var cc = obj as CodeCommentStatement;
			if (null != cc)
			{
				_VisitCommentStatement(cc, args, action);
				return;
			}
			var ccnd = obj as CodeConditionStatement;
			if (null != ccnd)
			{
				_VisitConditionStatement(ccnd, args, action);
				return;
			}
			var ce = obj as CodeExpressionStatement;
			if (null != ce)
			{
				_VisitExpressionStatement(ce, args, action);
				return;
			}
			var cg = obj as CodeGotoStatement;
			if (null != cg)
			{
				_VisitGotoStatement(cg, args, action);
				return;
			}
			var ci = obj as CodeIterationStatement;
			if (null != ci)
			{
				_VisitIterationStatement(ci, args, action);
				return;
			}
			var cl = obj as CodeLabeledStatement;
			if (null != cl)
			{
				_VisitLabeledStatement(cl, args, action);
				return;
			}
			var cm = obj as CodeMethodReturnStatement;
			if (null != cm)
			{
				_VisitMethodReturnStatement(cm, args, action);
				return;
			}
			var cre = obj as CodeRemoveEventStatement;
			if(null!=cre)
			{
				_VisitRemoveEventStatement(cre, args, action);
				return;
			}
			var cs = obj as CodeSnippetStatement;
			if (null != cs)
			{
				_VisitSnippetStatement(cs, args, action);
				return;
			}
			var cte = obj as CodeThrowExceptionStatement;
			if(null!=cte)
			{
				_VisitThrowExceptionStatement(cte, args, action);
				return;
			}
			var ctcf = obj as CodeTryCatchFinallyStatement;
			if (null != ctcf)
			{
				_VisitTryCatchFinallyStatement(ctcf, args, action);
				return;
			}
			var cvd = obj as CodeVariableDeclarationStatement;
			if(null!=cvd)
			{
				_VisitVariableDeclarationStatement(cvd, args, action);
				return;
			}
			throw new NotSupportedException("The graph contains an unsupported statement");
		}
		static void _VisitCatchClause(CodeCatchClause obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.CatchExceptionType && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.CatchExceptionType,args))
				_VisitTypeReference(obj.CatchExceptionType,args.Set(args.Root,obj,"CatchExceptionType",-1,_BuildPath(args.Path,"CatchExceptionType",-1),obj.CatchExceptionType,args.Targets),action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic = obj.Statements.Count, i = 0; i < ic; ++i)
				{
					var stmt = obj.Statements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj, "Statements",i,_BuildPath(args.Path,"Statements",i), stmt,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
		}
		static void _VisitTryCatchFinallyStatement(CodeTryCatchFinallyStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic=obj.TryStatements.Count,i=0;i<ic;++i)
				{
					var stmt = obj.TryStatements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj,"TryStatements",i,_BuildPath(args.Path,"TryStatements",i), stmt,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic=obj.CatchClauses.Count,i=0;i<ic;++i)
				{
					var cl = obj.CatchClauses[i];
					_VisitCatchClause(cl, args.Set(args.Root, obj,"CatchClauses",-1,_BuildPath(args.Path,"CatchClauses",-1), cl, args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			for (int ic = obj.FinallyStatements.Count, i = 0; i < ic; ++i)
			{
				var stmt = obj.FinallyStatements[i];
				if(_CanVisit(stmt,args))
					_VisitStatement(stmt, args.Set(args.Root, obj, "FinallyStatements",i,_BuildPath(args.Path,"FinallyStatements",i), stmt,args.Targets), action);
				if (args.Cancel)
					return;
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",-1,_BuildPath(args.Path,"EndDirectives",-1), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitRemoveEventStatement(CodeRemoveEventStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.Event && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Event,args))
				_VisitEventReferenceExpression(obj.Event, args.Set(args.Root, obj,"Event",-1,_BuildPath(args.Path,"Event",-1), obj.Event, args.Targets), action);
			if (args.Cancel) return;
			if(null!=obj.Listener && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Listener,args))
				_VisitExpression(obj.Event, args.Set(args.Root, obj,"Listener",-1,_BuildPath(args.Path,"Listener",-1), obj.Listener, args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitSnippetStatement(CodeSnippetStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitVariableDeclarationStatement(CodeVariableDeclarationStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if( null!=obj.Type && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.Type,args))
			{
				_VisitTypeReference(obj.Type, args.Set(args.Root, obj, "Type",-1,_BuildPath(args.Path,"Type",-1), obj.Type,args.Targets),action);
				if (args.Cancel) return;
			}
			if (null != obj.InitExpression&& _HasTarget(args, CodeDomVisitTargets.Expressions) && _CanVisit(obj.InitExpression,args))
			{
				_VisitExpression(obj.InitExpression, args.Set(args.Root, obj, "InitExpression",-1,_BuildPath(args.Path,"InitExpression",-1), obj.InitExpression, args.Targets), action);
				if (args.Cancel) return;
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitAssignStatement(CodeAssignStatement obj,CodeDomVisitContext args,CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.Left && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Left,args))
				_VisitExpression(obj.Left, args.Set(args.Root, obj,"Left",-1,_BuildPath(args.Path,"Left",-1), obj.Left, args.Targets), action);
			if (args.Cancel) return;
			if (null != obj.Right && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Right,args))
				_VisitExpression(obj.Right, args.Set(args.Root, obj, "Right",-1,_BuildPath(args.Path,"Right",-1),obj.Right, args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitLabeledStatement(CodeLabeledStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.Statement && _HasTarget(args,CodeDomVisitTargets.Statements) && _CanVisit(obj.Statement,args))
				_VisitStatement(obj.Statement, args.Set(args.Root, obj, "Statement",-1,_BuildPath(args.Path,"Statement",-1),obj.Statement, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitGotoStatement(CodeGotoStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) ||!_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",-1,_BuildPath(args.Path,"EndDirectives",-1), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitConditionStatement(CodeConditionStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.Condition && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Condition,args))
				_VisitExpression(obj.Condition, args.Set(args.Root, obj, "Condition",-1,_BuildPath(args.Path,"Condition",-1),obj.Condition, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic=obj.TrueStatements.Count,i=0;i<ic;++i)
				{
					var stmt = obj.TrueStatements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj,"TrueStatements",i,_BuildPath(args.Path,"TrueStatements",i), stmt,args.Targets), action);
					if (args.Cancel) return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for(int ic=obj.FalseStatements.Count,i=0;i<ic;++i)
				{
					var stmt = obj.FalseStatements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj,"FalseStatements",i,_BuildPath(args.Path,"FalseStatements",i), stmt,args.Targets), action);
					if (args.Cancel) return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitIterationStatement(CodeIterationStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.InitStatement && _HasTarget(args,CodeDomVisitTargets.Statements) && _CanVisit(obj.InitStatement,args))
				_VisitStatement(obj.InitStatement, args.Set(args.Root, obj,"InitStatement",-1,_BuildPath(args.Path,"InitStatement",-1), obj.InitStatement, args.Targets), action);
			if (args.Cancel) return;
			if (null!=obj.TestExpression && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.TestExpression,args))
				_VisitExpression(obj.TestExpression, args.Set(args.Root, obj, "TestExpression",-1,_BuildPath(args.Path,"TestExpression",-1),obj.TestExpression, args.Targets), action);
			if (args.Cancel) return;
			if (null != obj.IncrementStatement && _HasTarget(args,CodeDomVisitTargets.Statements) && _CanVisit(obj.IncrementStatement,args))
				_VisitStatement(obj.IncrementStatement, args.Set(args.Root, obj, "IncrementStatement",-1,_BuildPath(args.Path,"IncrementStatement",-1),obj.IncrementStatement, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Statements))
			{
				for (int ic = obj.Statements.Count, i = 0; i < ic; ++i)
				{
					var stmt = obj.Statements[i];
					if(_CanVisit(stmt,args))
						_VisitStatement(stmt, args.Set(args.Root, obj, "Statements",i,_BuildPath(args.Path,"Statements",i), stmt,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.Directives)) {
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitAttachEventStatement(CodeAttachEventStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.Event && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Event,args))
				_VisitEventReferenceExpression(obj.Event, args.Set(args.Root, obj,"Event",-1,_BuildPath(args.Path,"Event",-1), obj.Event, args.Targets), action);
			if (args.Cancel) return;
			if (null != obj.Listener && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Listener,args))
				_VisitExpression(obj.Listener, args.Set(args.Root, obj,"Listener",-1,_BuildPath(args.Path,"Listener",-1), obj.Listener, args.Targets), action);
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitExpressionStatement(CodeExpressionStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.Expression && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Expression,args))
				_VisitExpression(obj.Expression, args.Set(args.Root, obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1), obj.Expression, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitMethodReturnStatement(CodeMethodReturnStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.Expression && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.Expression,args))
				_VisitExpression(obj.Expression, args.Set(args.Root, obj, "Expression",-1,_BuildPath(args.Path,"Expression",-1),obj.Expression, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitThrowExceptionStatement(CodeThrowExceptionStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if (null != obj.ToThrow && _HasTarget(args,CodeDomVisitTargets.Expressions) && _CanVisit(obj.ToThrow,args))
				_VisitExpression(obj.ToThrow, args.Set(args.Root, obj,"ToThrow",-1,_BuildPath(args.Path,"ToThrow",-1), obj.ToThrow, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitCommentStatement(CodeCommentStatement obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.Statements) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.StartDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.StartDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
				if (null != obj.LinePragma)
					_VisitLinePragma(obj.LinePragma, args.Set(args.Root, obj, "LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1), obj.LinePragma,args.Targets), action);
			}
			if(null!=obj.Comment && _HasTarget(args,CodeDomVisitTargets.Comments))
				_VisitComment(obj.Comment, args.Set(args.Root, obj,"Comment",-1,_BuildPath(args.Path,"Comment",-1), obj.Comment, args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Directives))
			{
				for (int ic = obj.EndDirectives.Count, i = 0; i < ic; ++i)
				{
					var dir = obj.EndDirectives[i];
					if(_CanVisit(dir,args))
						_VisitDirective(dir, args.Set(args.Root, obj, "EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i), dir,args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitTypeParameter(CodeTypeParameter obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.Attributes))
			{
				for (int ic = obj.CustomAttributes.Count, i = 0; i < ic; ++i)
				{
					var attrDecl = obj.CustomAttributes[i];
					_VisitAttributeDeclaration(attrDecl, args.Set(args.Root, obj, "CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i), attrDecl,args.Targets), action);
					if (args.Cancel)
						return;
				}
			}
			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic=obj.Constraints.Count,i=0;i<ic;++i)
				{
					var ctr = obj.Constraints[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj,"Constraints",i,_BuildPath(args.Path,"Constraints",i), ctr, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static void _VisitTypeReference(CodeTypeReference obj, CodeDomVisitContext args, CodeDomVisitAction action)
		{
			if (!_HasTarget(args, CodeDomVisitTargets.TypeRefs) || !_CanVisit(obj,args)) return;
			if (args.Cancel) return;
			// report it
			action(args);
			if (args.Cancel) return;
			if (null != obj.ArrayElementType && _HasTarget(args,CodeDomVisitTargets.TypeRefs) && _CanVisit(obj.ArrayElementType,args))
				_VisitTypeReference(obj.ArrayElementType, args.Set(args.Root, obj,"ArrayElementType",-1,_BuildPath(args.Path,"ArrayElementType",-1), obj.ArrayElementType,args.Targets), action);
			if (args.Cancel) return;
			if (_HasTarget(args, CodeDomVisitTargets.TypeRefs))
			{
				for (int ic=obj.TypeArguments.Count,i=0;i<ic;++i)
				{
					var ctr = obj.TypeArguments[i];
					if(_CanVisit(ctr,args))
						_VisitTypeReference(ctr, args.Set(args.Root, obj,"TypeArguments",i,_BuildPath(args.Path,"TypeArguments",i), ctr, args.Targets), action);
					if (args.Cancel) return;
				}
			}
		}
		static string _BuildPath(string path,string member,int index)
		{
#if !NOPATHS
			if (string.IsNullOrEmpty(path))
				path = member;
			else
				path = string.Concat(path, ".", member);
			if (-1 != index)
				path = string.Concat(path, "[", index.ToString(), "]");
#endif
			return path;
		}
		/// <summary>
		/// Returns the path from <paramref name="root"/> to <paramref name="target"/>
		/// </summary>
		/// <param name="root">The containing object to start the search from</param>
		/// <param name="target">The target object to search for</param>
		/// <param name="visitTargets">The targets to examine and traverse</param>
		/// <returns>The path, or null if the object could not be found</returns>
		public static string GetPathToObject(object root, object target, CodeDomVisitTargets visitTargets = CodeDomVisitTargets.All)
		{
			string result = null;
			CodeDomVisitor.Visit(root, (ctx) => {
				if (ReferenceEquals(ctx.Target, target))
				{
					result = ctx.Path;
					ctx.Cancel = true;
				}
			}, visitTargets);
			return result;
		}
	}
}
