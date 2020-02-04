using System.ComponentModel;
using System.CodeDom;
using System;
using System.ComponentModel.Design.Serialization;
using System.Globalization;
using System.Reflection;
using System.Collections.Generic;

namespace CD
{
	/// <summary>
	/// This class supports the framework. Provides serialization of code dom constructs to code dom constructs.
	/// </summary>
	public class CodeDomTypeConverter : TypeConverter
	{
		/// <summary>
		/// This class supports the framework. This method is called by the serialization process to check to see if it's serializable.
		/// </summary>
		/// <param name="context">Not used</param>
		/// <param name="destinationType"><see cref="InstanceDescriptor"/></param>
		/// <returns>True if <paramref name="destinationType"/> is instance descriptor, otherwise false</returns>
		public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
		{
			return destinationType == typeof(InstanceDescriptor) || base.CanConvertTo(context, destinationType);
		}
		/// <summary>
		/// This class supports the framework. This method is called by the serialization process to get a <see cref="InstanceDescriptor"/> back that shows how to serialize the object
		/// </summary>
		/// <param name="context">Not used</param>
		/// <param name="culture">Not used</param>
		/// <param name="value">The code object</param>
		/// <param name="destinationType">Should be <see cref="InstanceDescriptor"/></param>
		/// <returns>A <see cref="InstanceDescriptor"/> that can be used to serialize the object or null</returns>
		public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
		{
			object result=null;

			if (null!=value)
			{
				if (destinationType == typeof(InstanceDescriptor))
				{
					var kvp = _GetInstanceData(value);
					result = new InstanceDescriptor(kvp.Key, kvp.Value);
				}
			}

			return result ?? base.ConvertTo(context, culture, value, destinationType);
		}
		static KeyValuePair<MemberInfo,object[]> _GetInstanceData(object value)
		{
			var cu = value as CodeCompileUnit;
			if (null != cu)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod("CompileUnit"),
					new object[] {_ToArray(cu.ReferencedAssemblies),
					_ToArray(cu.Namespaces),
					_ToArray(cu.AssemblyCustomAttributes),
					_ToArray(cu.StartDirectives),
					_ToArray(cu.EndDirectives) });
			}
			var ns = value as CodeNamespace;
			if(null!=ns)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod("Namespace"),
					new object[] {ns.Name,_ToArray(ns.Imports),_ToArray(ns.Types),
						_ToArray(ns.Comments) });
			}
			var nsi = value as CodeNamespaceImport;
			if(null!=nsi)
			{
				if(null==nsi.LinePragma)
				{
					return new KeyValuePair<MemberInfo, object[]>(
						nsi.GetType().GetConstructor(new Type[] {typeof(string)}),
						new object[] {nsi.Namespace});
				}
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod("NamespaceImport"),
					new object[] {nsi.Namespace,nsi.LinePragma});
			}
			var e = value as CodeExpression;
			if (null != e)
				return _GetInstanceData(e);
			var s = value as CodeStatement;
			if (null != s)
				return _GetInstanceData(s);
			var tr = value as CodeTypeReference;
			if (null != tr)
				return _GetInstanceData(tr);
			var td = value as CodeTypeDeclaration;
			if(null!=td)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod("TypeDeclaration"),
					new object[] {
						td.Name,td.IsClass,td.IsEnum,td.IsInterface,
						td.IsStruct,td.IsPartial,td.Attributes,td.TypeAttributes,
						_ToArray(td.TypeParameters),
						_ToArray(td.BaseTypes),
						_ToArray(td.Members),
						_ToArray(td.Comments),
						_ToArray(td.CustomAttributes),
						_ToArray(td.StartDirectives),
						_ToArray(td.EndDirectives),
						td.LinePragma}
					);		
			}
			var tm = value as CodeTypeMember;
			if(null!=tm)
				return _GetInstanceData(tm);	
			var tp = value as CodeTypeParameter;
			if(null!=tp)
			{
				// see if we can use the simplified instantiation
				if(!tp.HasConstructorConstraint && 0==tp.Constraints.Count && 0==tp.CustomAttributes.Count)
				{
					return new KeyValuePair<MemberInfo, object[]>(
						value.GetType().GetConstructor(new Type[] { typeof(string) }),
						new object[] { tp.Name });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod("TypeParameter"),
					new object[] { tp.Name,tp.HasConstructorConstraint,_ToArray(tp.Constraints),_ToArray(tp.CustomAttributes) });
			}
			var cad = value as CodeAttributeDeclaration;
			if (null != cad)
			{
				if (null != cad.AttributeType)
				{
					
					return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference), typeof(CodeAttributeArgument[]) }),
					new object[] { cad.AttributeType, _ToArray(cad.Arguments)});
				} else
				{
					return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string), typeof(CodeAttributeArgument[]) }),
					new object[] { cad.Name, _ToArray(cad.Arguments) });
				}
			}
			var caa = value as CodeAttributeArgument;
			if(null!=caa)
			{
				if(string.IsNullOrEmpty(caa.Name))
				{
					return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeExpression)}),
					new object[] { caa.Value});
				} else
				{
					return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] {typeof(string), typeof(CodeExpression) }),
					new object[] { caa.Name, caa.Value });
				}
			}
			var cc = value as CodeCatchClause;
			if(null!=cc)
			{

				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string), typeof(CodeTypeReference), typeof(CodeStatement[]) }),
					new object[] { cc.LocalName, cc.CatchExceptionType, _ToArray(cc.Statements) });
			}
			var rd = value as CodeRegionDirective;
			if(null!=rd)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeRegionMode),typeof(string) }),
					new object[] {rd.RegionMode,rd.RegionText });
			}
			var cp = value as CodeChecksumPragma;
			if (null != cp)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string), typeof(Guid),typeof(byte[]) }),
					new object[] { cp.FileName, cp.ChecksumAlgorithmId,cp.ChecksumData});
			}
			var lp = value as CodeLinePragma;
			if(null!=lp)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string),typeof(int)}),
					new object[] { lp.FileName,lp.LineNumber });
			}
			var cm = value as CodeComment;
			if(null!=cm)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string), typeof(bool) }),
					new object[] { cm.Text, cm.DocComment});
			}
			
			Guid g;
			if(value is Guid)
			{
				g = (Guid)value;
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string)}),
					new object[] { g.ToString()});
			}
			
			throw new NotSupportedException("Unsupported type of code object. Could not retrieve the instance data.");
		}
		
		static KeyValuePair<MemberInfo,object[]> _GetInstanceData(CodeTypeMember member)
		{
			var t = member.GetType();
			var tc = member as CodeTypeConstructor;
			if(null!=tc)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
					new object[] {tc.Attributes,_ToArray(tc.Parameters),
								_ToArray(tc.Statements),_ToArray(tc.Comments),
								_ToArray(tc.CustomAttributes),_ToArray(tc.StartDirectives),
								_ToArray(tc.EndDirectives),tc.LinePragma}	);
			}
			var c = member as CodeConstructor;
			if (null != c)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
					new object[] {c.Attributes,_ToArray(c.Parameters),
								_ToArray(c.ChainedConstructorArgs),
								_ToArray(c.BaseConstructorArgs),
								_ToArray(c.Statements),_ToArray(c.Comments),
								_ToArray(c.CustomAttributes),_ToArray(c.StartDirectives),
								_ToArray(c.EndDirectives),c.LinePragma});
			}
			var em = member as CodeEntryPointMethod;
			if (null != em)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
					new object[] {
						em.ReturnType,em.Name,em.Attributes,_ToArray(em.Parameters),
						_ToArray(em.Statements),_ToArray(em.ImplementationTypes),
						em.PrivateImplementationType,_ToArray(em.Comments),
						_ToArray(em.CustomAttributes),_ToArray(em.ReturnTypeCustomAttributes),
						_ToArray(em.StartDirectives),_ToArray(em.EndDirectives),em.LinePragma
					});
			}
			var m = member as CodeMemberMethod;
			if (null != m)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
					new object[] {
						m.ReturnType,m.Name,m.Attributes,_ToArray(m.Parameters),
						_ToArray(m.Statements),_ToArray(m.ImplementationTypes),
						m.PrivateImplementationType,_ToArray(m.Comments),
						_ToArray(m.CustomAttributes),_ToArray(m.ReturnTypeCustomAttributes),
						_ToArray(m.StartDirectives),_ToArray(m.EndDirectives),m.LinePragma
					});
			}
			var p = member as CodeMemberProperty;
			if (null != p)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
					new object[] {
						p.Type,p.Name,p.Attributes,_ToArray(p.Parameters),
						_ToArray(p.GetStatements),_ToArray(p.SetStatements),
						_ToArray(p.ImplementationTypes),
						p.PrivateImplementationType,_ToArray(p.Comments),
						_ToArray(p.CustomAttributes),
						_ToArray(p.StartDirectives),_ToArray(p.EndDirectives),p.LinePragma
					});
			}
			var f = member as CodeMemberField;
			if (null != f)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
					new object[] {
						f.Type,f.Name,f.InitExpression, f.Attributes,
						_ToArray(f.Comments),_ToArray(f.CustomAttributes),
						_ToArray(f.StartDirectives),_ToArray(f.EndDirectives),f.LinePragma
					});
			}
			var e = member as CodeMemberEvent;
			if (null != e)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
					new object[] {
						e.Type,e.Name,e.Attributes,_ToArray(e.ImplementationTypes),
						e.PrivateImplementationType,_ToArray(e.Comments),_ToArray(e.CustomAttributes),
						_ToArray(e.StartDirectives),_ToArray(e.EndDirectives),e.LinePragma
					});
			}
			throw new NotSupportedException("Unsupported member type. Can't get instance data");
		}
		static KeyValuePair<MemberInfo,object[]> _GetInstanceData(CodeTypeReference tr)
		{
			if(0<tr.ArrayRank && null!=tr.ArrayElementType && 0==(int)tr.Options && 0==tr.TypeArguments.Count)
			{
				return new KeyValuePair<MemberInfo, object[]>(
					tr.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference), typeof(int) }),
					new object[] { tr.ArrayElementType,tr.ArrayRank });
			}
			
			if (0!=(int)tr.Options)
			{
				if(0==tr.TypeArguments.Count)
					return new KeyValuePair<MemberInfo, object[]>(
					tr.GetType().GetConstructor(new Type[] { typeof(string), typeof(CodeTypeReferenceOptions) }),
					new object[] { tr.BaseType, tr.Options});
			} else
			{
				
				if (0 == tr.TypeArguments.Count)
				{
					var t = Type.GetType(tr.BaseType, false, false);
					if (null == t)
						return new KeyValuePair<MemberInfo, object[]>(
						tr.GetType().GetConstructor(new Type[] { typeof(string) }),
						new object[] { tr.BaseType });
					return new KeyValuePair<MemberInfo, object[]>(
					tr.GetType().GetConstructor(new Type[] { typeof(Type) }),
					new object[] { t });
				}
				else
				{
					
					return new KeyValuePair<MemberInfo, object[]>(
						tr.GetType().GetConstructor(new Type[] { typeof(string), typeof(CodeTypeReference[]) }),
						new object[] { tr.BaseType, _ToArray(tr.TypeArguments) });
				}
			}
			return new KeyValuePair<MemberInfo, object[]>(typeof(CodeDomBuilder).GetMethod("TypeReference"),
				new object[] { tr.BaseType, tr.Options, _ToArray(tr.TypeArguments),tr.ArrayElementType,tr.ArrayRank });
		}

		static KeyValuePair<MemberInfo,object[]> _GetInstanceData(CodeStatement stmt)
		{
			var a = stmt as CodeAssignStatement;
			if(null!=a)
			{
				if(_HasExtraNonsense(a))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] {a.Left,a.Right,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeExpression), typeof(CodeExpression) }),
					new object[] { a.Left, a.Right });
			}
			var ae = stmt as CodeAttachEventStatement;
			if (null != ae)
			{
				if (_HasExtraNonsense(ae))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { ae.Event, ae.Listener, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeEventReferenceExpression), typeof(CodeExpression) }),
					new object[] { ae.Event, ae.Listener});
			}
			var cm = stmt as CodeCommentStatement;
			if (null != cm)
			{
				if (_HasExtraNonsense(cm))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { cm.Comment, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				if (!cm.Comment.DocComment)
					return new KeyValuePair<MemberInfo, object[]>(
						stmt.GetType().GetConstructor(new Type[] { typeof(string) }),
						new object[] { cm.Comment.Text });
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(string), typeof(bool) }),
					new object[] { cm.Comment.Text,cm.Comment.DocComment});
			}
			var c = stmt as CodeConditionStatement;
			if (null != c)
			{
				if (_HasExtraNonsense(c))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { c.Condition, _ToArray(c.TrueStatements),_ToArray(c.FalseStatements), _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeExpression), typeof(CodeStatement[]), typeof(CodeStatement[]) }),
					new object[] { c.Condition,_ToArray(c.TrueStatements),_ToArray(c.FalseStatements)});
			}
			var e = stmt as CodeExpressionStatement;
			if (null != e)
			{
				if (_HasExtraNonsense(e))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { e.Expression, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeExpression)}),
					new object[] { e.Expression});
			}
			var g = stmt as CodeGotoStatement;
			if (null != g)
			{
				if (_HasExtraNonsense(g))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { g.Label, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(string) }),
					new object[] { g.Label});
			}
			var i = stmt as CodeIterationStatement;
			if (null != i)
			{
				if (_HasExtraNonsense(i))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { i.InitStatement,i.TestExpression,i.IncrementStatement,_ToArray(i.Statements), _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeStatement),typeof(CodeExpression),typeof(CodeStatement),typeof(CodeStatement[]) }),
					new object[] { i.InitStatement, i.TestExpression, i.IncrementStatement, _ToArray(i.Statements) });
			}
			var l = stmt as CodeLabeledStatement;
			if (null != l)
			{
				if (_HasExtraNonsense(l))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { l.Label, l.Statement, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(string), typeof(CodeStatement) }),
					new object[] { l.Label,l.Statement});
			}
			var r = stmt as CodeMethodReturnStatement;
			if (null != r)
			{
				if (_HasExtraNonsense(r))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { r.Expression,_ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeExpression) }),
					new object[] { r.Expression });
			}
			var re = stmt as CodeRemoveEventStatement;
			if (null != re)
			{
				if (_HasExtraNonsense(re))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { re.Event, re.Listener, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeEventReferenceExpression), typeof(CodeExpression) }),
					new object[] { re.Event, re.Listener});
			}
			var s = stmt as CodeSnippetStatement;
			if (null != s)
			{
				if (_HasExtraNonsense(s))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { s.Value, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(string)}),
					new object[] { s.Value });
			}
			var t = stmt as CodeThrowExceptionStatement;
			if (null != t)
			{
				if (_HasExtraNonsense(t))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { t.ToThrow, _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeExpression) }),
					new object[] { t.ToThrow });
			}
			var tc = stmt as CodeTryCatchFinallyStatement;
			if (null != tc)
			{
				if (_HasExtraNonsense(tc))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { _ToArray(tc.TryStatements), _ToArray(tc.CatchClauses),_ToArray(tc.FinallyStatements), _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeStatement[]),typeof(CodeCatchClause[]), typeof(CodeStatement[]) }),
					new object[] { _ToArray(tc.TryStatements), _ToArray(tc.CatchClauses),_ToArray(tc.FinallyStatements)});
			}
			var v = stmt as CodeVariableDeclarationStatement;
			if (null != v)
			{
				if (_HasExtraNonsense(v))
				{
					return new KeyValuePair<MemberInfo, object[]>(
						typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
						new object[] { v.Type,v.Name,v.InitExpression,  _ToArray(a.StartDirectives), _ToArray(a.EndDirectives), a.LinePragma });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					stmt.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference), typeof(string),typeof(CodeExpression) }),
					new object[] { v.Type,v.Name,v.InitExpression });
			}
			throw new NotSupportedException("The statement instance data could not be serialized.");
		}
		static bool _HasExtraNonsense(CodeStatement stmt)
		{
			return (null != stmt.LinePragma || 0 < stmt.StartDirectives.Count || 0 < stmt.EndDirectives.Count);
		}
		static KeyValuePair<MemberInfo,object[]> _GetInstanceData(CodeExpression value)
		{
			var ar = value as CodeArgumentReferenceExpression;
			if(null!=ar)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string) }),
					new object[] {ar.ParameterName});
			var ac = value as CodeArrayCreateExpression;
			if (null != ac)
			{
				if (null != ac.Initializers && 0 < ac.Initializers.Count)
				{
					return new KeyValuePair<MemberInfo, object[]>(
						value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference), typeof(CodeExpression[]) }),
						new object[] { ac.CreateType, _ToArray(ac.Initializers) });
				}
				else if (null != ac.SizeExpression)
				{
					return new KeyValuePair<MemberInfo, object[]>(
						value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference), typeof(CodeExpression) }),
						new object[] { ac.CreateType, ac.SizeExpression });
				}
				else
					return new KeyValuePair<MemberInfo, object[]>(
						value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference), typeof(int) }),
						new object[] { ac.CreateType, ac.Size });
			}
			var ai = value as CodeArrayIndexerExpression;
			if (null != ai)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeExpression),typeof(CodeExpression[]) }),
					new object[] { ai.TargetObject,_ToArray(ai.Indices) });
			var br = value as CodeBaseReferenceExpression;
			if (null != br)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { }),
					new object[] { });
			var bo = value as CodeBinaryOperatorExpression;
			if (null != bo)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeExpression),typeof(CodeBinaryOperatorType),typeof(CodeExpression) }),
					new object[] {bo.Left,bo.Operator,bo.Right });
			var c = value as CodeCastExpression;
			if (null != c)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference),typeof(CodeExpression)}),
					new object[] { c.TargetType,c.Expression});
			var dv = value as CodeDefaultValueExpression;
			if (null != dv)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference) }),
					new object[] { dv.Type});
			var dc = value as CodeDelegateCreateExpression;
			if (null != dc)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference),typeof(CodeExpression),typeof(string) }),
					new object[] {dc.DelegateType,dc.TargetObject,dc.MethodName });
			var di = value as CodeDelegateInvokeExpression;
			if (null != di)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] {typeof(CodeExpression),typeof(CodeExpression[]) }),
					new object[] { di.TargetObject,_ToArray(di.Parameters) });
			var d = value as CodeDirectionExpression;
			if (null != d)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(FieldDirection),typeof(CodeExpression) }),
					new object[] { d.Direction,d.Expression });
			var er = value as CodeEventReferenceExpression;
			if (null != er)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] {typeof(CodeExpression),typeof(string) }),
					new object[] { er.TargetObject,er.EventName });
			var fr = value as CodeFieldReferenceExpression;
			if (null != fr)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] {typeof(CodeExpression),typeof(string) }),
					new object[] { fr.TargetObject,fr.FieldName});
			var ci = value as CodeIndexerExpression;
			if (null != ci)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeExpression),typeof(CodeExpression[])}),
					new object[] { ci.TargetObject,_ToArray(ci.Indices) });
			var mi = value as CodeMethodInvokeExpression;
			if (null != mi)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] {typeof(CodeMethodReferenceExpression),typeof(CodeExpression[])}),
					new object[] { mi.Method,_ToArray(mi.Parameters) });
			var mr = value as CodeMethodReferenceExpression;
			if (null != mr)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeExpression),typeof(string) }),
					new object[] { mr.TargetObject,mr.MethodName });
			var oc = value as CodeObjectCreateExpression;
			if (null != oc)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] {typeof(CodeTypeReference),typeof(CodeExpression[]) }),
					new object[] { oc.CreateType,_ToArray(oc.Parameters) });
			var pd = value as CodeParameterDeclarationExpression;
			if (null != pd)
			{
				if(0==pd.CustomAttributes.Count && FieldDirection.In==pd.Direction)
				{
					return new KeyValuePair<MemberInfo, object[]>(
						value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference), typeof(string) }),
						new object[] {pd.Type,pd.Name });
				}
				return new KeyValuePair<MemberInfo, object[]>(
					typeof(CodeDomBuilder).GetMethod(pd.GetType().Name.Substring(4)),
					new object[] { pd.Type, pd.Name, pd.Direction, _ToArray(pd.CustomAttributes)});
			}
			var p = value as CodePrimitiveExpression;
			if (null != p)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(object) }),
					new object[] { p.Value});
			var pr = value as CodePropertyReferenceExpression;
			if (null != pr)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] {typeof(CodeExpression),typeof(string) }),
					new object[] {pr.TargetObject,pr.PropertyName });
			var ps = value as CodePropertySetValueReferenceExpression;
			if (null != ps)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { }),
					new object[] { });
			var s = value as CodeSnippetExpression;
			if (null != s)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string) }),
					new object[] {s.Value });
			var th = value as CodeThisReferenceExpression;
			if (null != th)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { }),
					new object[] { });
			var to = value as CodeTypeOfExpression;
			if (null != to)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference) }),
					new object[] { to.Type });
			var tr = value as CodeTypeReferenceExpression;
			if (null != tr)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(CodeTypeReference) }),
					new object[] { tr.Type });
			var vr = value as CodeVariableReferenceExpression;
			if (null != vr)
				return new KeyValuePair<MemberInfo, object[]>(
					value.GetType().GetConstructor(new Type[] { typeof(string) }),
					new object[] { vr.VariableName });

			throw new NotSupportedException("Unsupported code type. Cannot convert to instance data.");
		}
		static CodeAttributeArgument[] _ToArray(CodeAttributeArgumentCollection args)
		{
			var result = new CodeAttributeArgument[args.Count];
			args.CopyTo(result, 0);
			return result;
		}
		static CodeExpression[] _ToArray(CodeExpressionCollection exprs)
		{
			var result = new CodeExpression[exprs.Count];
			exprs.CopyTo(result, 0);
			return result;
		}
		static CodeCatchClause[] _ToArray(CodeCatchClauseCollection ccs)
		{
			var result = new CodeCatchClause[ccs.Count];
			ccs.CopyTo(result, 0);
			return result;
		}
		static CodeStatement[] _ToArray(CodeStatementCollection stmts)
		{
			var result = new CodeStatement[stmts.Count];
			stmts.CopyTo(result, 0);
			return result;
		}

		static CodeAttributeDeclaration[] _ToArray(CodeAttributeDeclarationCollection attrs)
		{
			var result = new CodeAttributeDeclaration[attrs.Count];
			attrs.CopyTo(result, 0);
			return result;
		}
		static CodeDirective[] _ToArray(CodeDirectiveCollection dirs)
		{
			var result = new CodeDirective[dirs.Count];
			dirs.CopyTo(result, 0);
			return result;
		}
		static CodeTypeReference[] _ToArray(CodeTypeReferenceCollection refs)
		{
			var result = new CodeTypeReference[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
		static CodeCommentStatement[] _ToArray(CodeCommentStatementCollection refs)
		{
			var result = new CodeCommentStatement[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
		static CodeTypeParameter[] _ToArray(CodeTypeParameterCollection refs)
		{
			var result = new CodeTypeParameter[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
		static CodeTypeMember[] _ToArray(CodeTypeMemberCollection refs)
		{
			var result = new CodeTypeMember[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
		static string[] _ToArray(System.Collections.Specialized.StringCollection refs)
		{
			var result = new string[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
		static CodeNamespace[] _ToArray(CodeNamespaceCollection refs)
		{
			var result = new CodeNamespace[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
		static CodeNamespaceImport[] _ToArray(CodeNamespaceImportCollection refs)
		{
			var result = new CodeNamespaceImport[refs.Count];
			((System.Collections.ICollection)refs).CopyTo(result, 0);
			return result;
		}
		static CodeTypeDeclaration[] _ToArray(CodeTypeDeclarationCollection refs)
		{
			var result = new CodeTypeDeclaration[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
		static CodeParameterDeclarationExpression[] _ToArray(CodeParameterDeclarationExpressionCollection refs)
		{
			var result = new CodeParameterDeclarationExpression[refs.Count];
			refs.CopyTo(result, 0);
			return result;
		}
	}
}
