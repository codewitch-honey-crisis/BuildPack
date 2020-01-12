using System;
using System.Collections.Generic;
using System.Text;
using System.CodeDom;
using System.Globalization;
using System.Reflection;

namespace CD
{
	static partial class SlangParser
	{
		public static CodeTypeMember ToMember(ParseNode node)
		{
			return _BuildMember(node);
		}
		public static CodeStatement ToStatement(ParseNode node)
		{
			return _BuildStatement(node);
		}
		public static CodeCompileUnit ToCompileUnit(ParseNode node)
		{
			return _BuildCompileUnit(node);
		}
		static CodeNamespace _BuildNamespace(ParseNode node)
		{
			var result = new CodeNamespace().Mark(node);
			result.Name = _BuildNamespaceName(node.C(1));
			for (var i = 2;i<node.Children.Length;i++)
			{
				var c = node.C(i);
				switch (c.SymbolId)
				{
					case UsingDirective:
						var nsi = new CodeNamespaceImport(_BuildNamespaceName(c.C(1))).Mark(c.C(1));
						result.Imports.Add(nsi);
						break;
					case TypeDeclParser.Class:
					case TypeDeclParser.Struct:
					case TypeDeclParser.Enum:
					case TypeDeclParser.Interface:
						result.Types.Add(_BuildTypeDecl(c));
						break;
				}
			}
			return result;
		}
		static CodeCompileUnit _BuildCompileUnit(ParseNode node)
		{
			var result = new CodeCompileUnit().Mark(node);
			var rootns = new CodeNamespace().Mark(node);
			for (var i = 0;i<node.Children.Length;i++)
			{
				var c = node.C(i);
				switch(c.SymbolId)
				{
					case UsingDirective:
						var nsi = new CodeNamespaceImport(_BuildNamespaceName(c.C(1))).Mark(c.C(1));
						rootns.Imports.Add(nsi);
						break;
					case TypeDeclParser.CustomAttributeGroups:
						var ca= _BuildCustomAttributeGroups(c);
						_AddCustomAttributes(ca, "assembly", result.AssemblyCustomAttributes);
						ca.Remove("assembly");
						if (0 < ca.Count)
							throw new SyntaxException("Invalid attribute target. Attributes at this level must be targeted to assembly.", c.Line, c.Column, c.Position);
						break;
					case TypeDeclParser.Class:
					case TypeDeclParser.Struct:
					case TypeDeclParser.Enum:
					case TypeDeclParser.Interface:
						rootns.Types.Add(_BuildTypeDecl(c));
						break;
					case Namespace:
						result.Namespaces.Add(_BuildNamespace(c));
						break;
				}
			}
			if(0<rootns.Imports.Count || 0<rootns.Types.Count || 0<rootns.Comments.Count)
				result.Namespaces.Insert(0,rootns);
			return result;
		}
		static string _BuildNamespaceName(ParseNode node)
		{
			var result = "";
			for(var i = 0;i<node.Children.Length;i++)
				result = string.Concat(result, node.C(i).Value);
			return result;
		}
		static MemberAttributes _BuildMemberAttributes(ParseNode node)
		{
			var modifiers = new HashSet<string>();
			for(var i = 0;i<node.Children.Length;i++)
			{
				modifiers.Add(node.Children[i].Value);
			}
			var result = (MemberAttributes)0;
			foreach (var kw in modifiers)
			{
				switch (kw)
				{
					case "protected":
						if (modifiers.Contains("internal"))
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyOrAssembly;
						else
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Family;
						break;
					case "internal":
						if (modifiers.Contains("protected"))
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyOrAssembly;
						else
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyAndAssembly;
						break;
					case "const":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Const;
						break;
					case "new":
						result = (result & ~MemberAttributes.VTableMask) | MemberAttributes.New;
						break;
					case "override":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Override;
						break;
					case "public":
						if (modifiers.Contains("virtual"))
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
						else
						{
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
							result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Final;
						}
						break;
					case "private":
						result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
						break;
					case "abstract":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Abstract;
						break;
					case "static":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Static;
						break;
				}
			}
			return result;
		}
		static TypeAttributes _BuildMemberTypeAttributes(ParseNode node)
		{
			var attrs = new HashSet<string>();
			for (var i = 0; i < node.Children.Length; i++)
			{
				attrs.Add(node.Children[i].Value);
			}
			// TODO: see if this works
			var result = TypeAttributes.NestedFamANDAssem;
			foreach (var attr in attrs)
			{
				switch (attr)
				{
					case "protected":
						if (attrs.Contains("internal"))
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamORAssem | TypeAttributes.NotPublic;
						else
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamily | TypeAttributes.NotPublic;
						break;
					case "internal":
						if (attrs.Contains("protected"))
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamORAssem | TypeAttributes.NotPublic;
						else
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamANDAssem | TypeAttributes.NotPublic;
						break;
					case "public":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
						break;
					case "private":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPrivate | TypeAttributes.NotPublic;
						break;

				}
			}
			return result;
		}
		static TypeAttributes _BuildTypeAttributes(ParseNode node)
		{
			var attrs = new HashSet<string>();
			for (var i = 0; i < node.Children.Length; i++)
			{
				attrs.Add(node.Children[i].Value);
			}
			var result = (TypeAttributes)0;
			foreach (var attr in attrs)
			{
				switch (attr)
				{
					case "public":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.Public;
						break;
					case "internal":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic;
						break;
					case "abstract":
						result |= TypeAttributes.Abstract;
						break;
					case "private":
						throw new SyntaxException("Top level types cannot be private", node.Line,node.Column,node.Position); 
					case "protected":
						throw new SyntaxException("Top level types cannot be protected", node.Line,node.Column,node.Position);
					case "static":
						throw new SyntaxException("Top level types cannot be static", node.Line, node.Column, node.Position);
					case "new":
						throw new SyntaxException("Top level types cannot be declared new", node.Line, node.Column, node.Position);
					case "override":
						throw new SyntaxException("Top level types cannot be declared override", node.Line, node.Column, node.Position);

				}
			}
			return result;
		}
		static CodeTypeMember _BuildMember(ParseNode node)
		{
			var c = node.C(0);
			var ca = _BuildCustomAttributeGroups(c.C(1));
			var ma = _BuildMemberAttributes(c.C(2));
			
			var isPublic = (ma & MemberAttributes.Public) == MemberAttributes.Public;
			var isStatic = (ma & MemberAttributes.Static) == MemberAttributes.Static;
			switch (c.SymbolId)
			{
				case TypeDeclParser.Field:
					#region Field
					var fld = new CodeMemberField().Mark(node);
					fld.Attributes = ma;
					_AddCustomAttributes(ca, "", fld.CustomAttributes);
					ca.Remove("");
					if (0<ca.Count)
						throw new SyntaxException("Unsupported attribute target.", node.Line, node.Column, node.Position);
					fld.Type = _BuildType(c.C(3));
					var n = c.C(4).Value;
					if (verbatimIdentifier == c.C(4).SymbolId)
						n = n.Substring(1);
					if (6 < c.CL())
						fld.InitExpression = _BuildExpression(c.C(6));
					fld.Name = n;
					return fld;
				#endregion
				case TypeDeclParser.Event:
					#region Event
					var eve = new CodeMemberEvent().Mark(node);
					eve.Attributes = ma;
					_AddCustomAttributes(ca, "", eve.CustomAttributes);
					ca.Remove("");
					if (0 < ca.Count)
						throw new SyntaxException("Unsupported attribute target.", node.Line, node.Column, node.Position);
					eve.Type = _BuildType(c.C(4));
					n = c.C(5).Value;
					if (verbatimIdentifier == c.C(5).SymbolId)
						n = n.Substring(1);
					eve.Name = n;
					return eve;
					#endregion
				case TypeDeclParser.Constructor:
					#region Ctor
					CodeMemberMethod mctor = null;
					if ((ma & ~MemberAttributes.ScopeMask) == MemberAttributes.Static)
					{
						var ctor = new CodeTypeConstructor().Mark(node);
						mctor = ctor;
					}
					else
					{
						var ctor = new CodeConstructor().Mark(node);
						if (TypeDeclParser.colon == c.C(7).SymbolId)
						{
							var cpn = c.C(8);
							if (TypeDeclParser.baseRef == cpn.C(0).SymbolId)
								ctor.BaseConstructorArgs.AddRange(_BuildArgList(cpn.C(2)));
							else
								ctor.ChainedConstructorArgs.AddRange(_BuildArgList(cpn.C(2)));
						}
						mctor = ctor;
					}
					_AddCustomAttributes(ca, "", mctor.CustomAttributes);
					ca.Remove("");
					if (0 < ca.Count)
						throw new SyntaxException("Unsupported attribute target.", node.Line, node.Column, node.Position);

					n = c.C(3).Value;
					if (verbatimIdentifier == c.C(3).SymbolId)
						n = n.Substring(1);
					mctor.Name = n;
					_AddCustomAttributes(ca, "", mctor.CustomAttributes);
					mctor.Attributes = ma;
					mctor.Parameters.AddRange(_BuildParamList(c.C(5)));
					var sn = c.C(c.CL() - 1);
					mctor.Statements.AddRange(_BuildStatementBlock(sn));
					return mctor;
					#endregion
				case TypeDeclParser.Method:
					#region Method
					CodeMemberMethod meth = null;
					
					if (isStatic && isPublic)
					{
						if(TypeDeclParser.voidType==c.C(3).SymbolId)
						{
							// no priv impl type and no method parameters, respectively:
							if(0==c.C(4).CL() && 0==c.C(7).CL())
							{
								if(0==string.Compare("Main",c.C(5).Value,StringComparison.InvariantCulture))
								{
									meth = new CodeEntryPointMethod().Mark(node);
								}
							}
						}
					}
					if (null == meth)
						meth = new CodeMemberMethod().Mark(node,isPublic && !isStatic);
					_AddCustomAttributes(ca, "", meth.CustomAttributes);
					ca.Remove("");
					meth.Attributes = ma;
					_AddCustomAttributes(ca, "return", meth.ReturnTypeCustomAttributes);
					ca.Remove("return");
					if (0 < ca.Count)
						throw new SyntaxException("Unsupported attribute target.", node.Line, node.Column, node.Position);
					if (verbatimIdentifier == c.C(5).SymbolId)
						meth.Name = c.C(5).Value.Substring(1);
					else
						meth.Name = c.C(5).Value;
					if(TypeDeclParser.voidType != c.C(3).SymbolId)
					{
						meth.ReturnType = _BuildType(c.C(3));
					}
					if (0 != c.C(4).CL())
						meth.PrivateImplementationType = _BuildType(c.C(4).C(0));

					meth.Parameters.AddRange(_BuildMethodParamList(c.C(7)));
					sn = c.C(c.CL() - 1);
					if(TypeDeclParser.StatementBlock==sn.SymbolId)
						meth.Statements.AddRange(_BuildStatementBlock(sn));
					return meth;
					#endregion
				case TypeDeclParser.Property:
					#region Property
					CodeMemberProperty prop = new CodeMemberProperty().Mark(node,isPublic &&!isStatic);
					prop.Attributes = ma;
					_AddCustomAttributes(ca, "", prop.CustomAttributes);
					ca.Remove("");
					if (0 < ca.Count)
						throw new SyntaxException("Unsupported attribute target.", node.Line, node.Column, node.Position);
					if (verbatimIdentifier == c.C(5).SymbolId)
						prop.Name = c.C(5).Value.Substring(1);
					else if (TypeDeclParser.thisRef == c.C(5).SymbolId)
						prop.Name = "Item";
					else
						prop.Name = c.C(5).Value;
					
					prop.Type= _BuildType(c.C(3));
					if (0 != c.C(4).CL())
						prop.PrivateImplementationType = _BuildType(c.C(4).C(0));
					var pai = 7;
					if (TypeDeclParser.ParamList == c.C(pai).SymbolId)
					{
						prop.Parameters.AddRange(_BuildParamList(c.C(7)));
						pai += 3;
					}
					var ps = c.C(pai);
					for(var i = 0;i<ps.Children.Length;++i)
					{
						var psn = ps.C(i);
						if(TypeDeclParser.PropertyGet==psn.SymbolId)
						{
							if(TypeDeclParser.StatementBlock ==psn.C(1).SymbolId)
							{
								prop.GetStatements.AddRange(_BuildStatementBlock(psn.C(1)));
							}
						} else if (TypeDeclParser.PropertySet == psn.SymbolId)
						{
							if (TypeDeclParser.StatementBlock == psn.C(1).SymbolId)
							{
								prop.SetStatements.AddRange(_BuildStatementBlock(psn.C(1)));
							}
						}
					}
					return prop;
				#endregion
				case TypeDeclParser.Class:
				case TypeDeclParser.Struct:
				case TypeDeclParser.Interface:
				case TypeDeclParser.Enum:
					return _BuildTypeDecl(c);
			}
			throw new NotImplementedException();
		}
		static IDictionary<string,CodeAttributeDeclarationCollection> _BuildCustomAttributeGroups(ParseNode node)
		{
			var result = new Dictionary<string, CodeAttributeDeclarationCollection>();
			for(var i = 0;i<node.Children.Length;i++)
			{
				var res = _BuildCustomAttributeGroup(node.C(i));
				CodeAttributeDeclarationCollection col;
				if (!result.TryGetValue(res.Key,out col))
				{
					col = new CodeAttributeDeclarationCollection();
					result.Add(res.Key, col);
				}
				col.AddRange(res.Value);
			}
			return result;
		}
		static void _AddCustomAttributes(IDictionary<string,CodeAttributeDeclarationCollection> attrs,string target, CodeAttributeDeclarationCollection mem)
		{
			CodeAttributeDeclarationCollection col;
			if(attrs.TryGetValue(target, out col))
				mem.AddRange(col);
		}
		static KeyValuePair<string,CodeAttributeDeclarationCollection> _BuildCustomAttributeGroup(ParseNode node)
		{
			var result = new CodeAttributeDeclarationCollection();
			string target = "";
			var t = node.C(1);
			if(0<t.CL())
				target = t.C(0).Value;

			for (var i = 2; i < node.Children.Length-1; i++)
				result.Add(_BuildCustomAttribute(node.C(i)));
			return new KeyValuePair<string, CodeAttributeDeclarationCollection>(target, result);
		}
		static CodeAttributeDeclaration _BuildCustomAttribute(ParseNode node)
		{
			var result = new CodeAttributeDeclaration(_BuildTypeBase(node.C(0)));
			result.AttributeType.Mark(node, true);
			result.Arguments.AddRange(_BuildCustomAttributeArgList(node.C(1)));
			return result;
		}
		static CodeAttributeArgumentCollection _BuildCustomAttributeArgList(ParseNode node)
		{
			var result = new CodeAttributeArgumentCollection();
			for(var i = 1;i<node.Children.Length;i++)
			{
				result.Add(_BuildCustomAttributeArg(node.C(i)));
				++i;
			}
			return result;
		}
		static CodeAttributeArgument _BuildCustomAttributeArg(ParseNode node)
		{
			if(node.CL()==1)
				return new CodeAttributeArgument(_BuildExpression(node.C(0)));
			var name = node.C(0).Value;
			if (verbatimIdentifier == node.C(0).SymbolId)
				name = name.Substring(1);
			return new CodeAttributeArgument(name, _BuildExpression(node.C(2)));
		}
		static CodeTypeDeclaration _BuildTypeDecl(ParseNode node)
		{
			var result = new CodeTypeDeclaration().Mark(node);
			var ca = _BuildCustomAttributeGroups(node.C(1));
			_AddCustomAttributes(ca, "", result.CustomAttributes);
			ca.Remove("");
			if (0 < ca.Count)
				throw new SyntaxException("Invalid attribute target", node.Line, node.Column, node.Position);
			result.Attributes = 0;
			if (TypeDeclParser.MemberAttributes == node.C(2).SymbolId)
			{
				result.Attributes = _BuildMemberAttributes(node.C(2));
				result.TypeAttributes = _BuildMemberTypeAttributes(node.C(2));
			}
			else
				result.TypeAttributes = _BuildTypeAttributes(node.C(2));
			result.IsPartial = 0<node.C(3).CL();
			var name = node.C(5).Value;
			if (verbatimIdentifier == node.C(5).SymbolId)
				name = name.Substring(1);
			result.Name = name;
			switch (node.SymbolId)
			{
				case TypeDeclParser.Enum:
					result.IsEnum = true;
					break;
				case TypeDeclParser.Class:
					result.IsClass = true;
					break;
				case TypeDeclParser.Struct:
					result.IsStruct = true;
					break;
				case TypeDeclParser.Interface:
					result.IsInterface = true;
					break;
			}
			result.TypeParameters.AddRange(_BuildTypeParams(node.C(6)));
			result.BaseTypes.AddRange(_BuildBaseTypes(node.C(7)));
			var wn = node.C(8);
			for(var i = 0;i<wn.Children.Length-1;i++)
			{
				++i;
				CodeTypeParameter p = null;
				var tpn = wn.C(i).C(0).Value;
				if (verbatimIdentifier == wn.C(i).C(0).SymbolId)
					tpn = tpn.Substring(1);
				for (int jc= result.TypeParameters.Count,j = 0;j<jc;++j)
				{
					var tp = result.TypeParameters[j];
					if(0==string.Compare(tp.Name,tpn,StringComparison.InvariantCulture))
					{
						p = tp;
						break;
					}
				}
				if (null == p)
					throw new SyntaxException("Where clause constraint on unspecified type parameter", wn.Line, wn.Column, wn.Position);
				var wcpn = wn.C(i).C(2);
				for(var j = 0;j<wcpn.Children.Length;j++)
				{
					var wcc = wcpn.C(j);
					if(TypeDeclParser.newKeyword==wcc.C(0).SymbolId)
						p.HasConstructorConstraint = true;
					else
						p.Constraints.Add(_BuildType(wcc.C(0)));
					
					++j;
				}
			}
			var mn = node.C(9);
			for(var i = 0;i<mn.Children.Length;i++)
				result.Members.Add(_BuildMember(mn.C(i)));
			return result;
		}
		static CodeTypeReferenceCollection _BuildBaseTypes(ParseNode node)
		{
			var result = new CodeTypeReferenceCollection();
			for(var i = 0;i<node.Children.Length;i++)
				result.Add(_BuildType(node.C(i)));
			return result;

		}
		static CodeTypeParameterCollection _BuildTypeParams(ParseNode node)
		{
			var result = new CodeTypeParameterCollection();

			for(var i = 1;i<node.Children.Length;i++)
			{
				result.Add(_BuildTypeParam(node.C(i)));
				++i;
			}
			return result;
		}
		static CodeTypeParameter _BuildTypeParam(ParseNode node)
		{
			var result = new CodeTypeParameter().Mark(node);
			var ci = 0;
			if (TypeDeclParser.CustomAttributeGroups == node.C(0).SymbolId)
			{
				var ca = _BuildCustomAttributeGroups(node.C(0));
				_AddCustomAttributes(ca, "", result.CustomAttributes);
				ca.Remove("");
				if (0 < ca.Count)
					throw new SyntaxException("Invalid attribute target", node.Line, node.Column, node.Position);
				ci = 1;
			}
			var name = node.C(ci).Value;
			if (verbatimIdentifier == node.C(ci).SymbolId)
				name = name.Substring(1);
			result.Name = name;
			return result;
		}
		static CodeStatementCollection _BuildStatementBlock(ParseNode node)
		{
			var result = new CodeStatementCollection();
			for(var i=1;i<node.Children.Length-1;i++)
				result.Add(_BuildStatement(node.C(i)));
			return result;
		}
		static CodeParameterDeclarationExpressionCollection _BuildParamList(ParseNode node)
		{
			var result = new CodeParameterDeclarationExpressionCollection();
			for (var i = 0; i < node.Children.Length; ++i)
			{
				result.Add(_BuildParam(node.C(i)));
				++i;
			}
			return result ;
		}
		static CodeParameterDeclarationExpression _BuildParam(ParseNode node)
		{
			var ctr = _BuildType(node.C(0));
			var name = node.C(1).Value;
			if(verbatimIdentifier==node.C(1).SymbolId)
				name = name.Substring(1);
			return new CodeParameterDeclarationExpression(ctr, name).Mark(node);
		}

		static CodeParameterDeclarationExpressionCollection _BuildMethodParamList(ParseNode node)
		{
			var result = new CodeParameterDeclarationExpressionCollection();
			for (var i = 0; i < node.Children.Length; ++i)
			{
				result.Add(_BuildMethodParam(node.C(i)));
				++i;
			}

			return result;
		}
		static CodeParameterDeclarationExpression _BuildMethodParam(ParseNode node)
		{
			if (node.CL() == 2)
			{
				var ctr = _BuildType(node.C(0));
				var name = node.C(1).Value;
				if (verbatimIdentifier == node.C(1).SymbolId)
					name = name.Substring(1);
				return new CodeParameterDeclarationExpression(ctr, name).Mark(node);
			} else
			{
				var ctr = _BuildType(node.C(1));
				var name = node.C(2).Value;
				if (verbatimIdentifier == node.C(2).SymbolId)
					name = name.Substring(2);
				FieldDirection fd = FieldDirection.In;
				if (TypeDeclParser.refKeyword == node.C(0).SymbolId)
					fd = FieldDirection.Ref;
				else
					fd = FieldDirection.Out;
				var result = new CodeParameterDeclarationExpression(ctr, name).Mark(node);
				result.Direction = fd;
				return result;
			}
		}
		static CodeStatement _BuildExpressionStatement(ParseNode node)
		{
			var expr = _BuildExpression(node.C(0));
			var bo = expr as CodeBinaryOperatorExpression;
			// for compat with more languages, turn assign expressions
			// into assign statements where possible
			if (null != bo && CodeBinaryOperatorType.Assign == bo.Operator)
				return new CodeAssignStatement(bo.Left, bo.Right).Mark(node);
			return new CodeExpressionStatement(_BuildExpression(node.C(0))).Mark(node);
		}
		static CodeExpression _BuildForTest(ParseNode node)
		{
			return _BuildExpression(node.C(0));
		}
		static CodeStatement _BuildStatement(ParseNode node)
		{
			var c = node.C(1);
			switch(c.SymbolId)
			{
				case StatementParser.EmptyStatement:
					return new CodeSnippetStatement().SetLoc(node);
				case StatementParser.ExpressionStatement:
					return _BuildExpressionStatement(c);
				case StatementParser.LabelStatement:
					return new CodeLabeledStatement(c.C(0).Value).SetLoc(node);
				case StatementParser.VariableDeclarationStatement:
					return _BuildVariableDeclarationStatement(c);
				case StatementParser.ForStatement:
					#region Evaluate For
					var iter = new CodeIterationStatement().Mark(node);
					var sid = c.C(2).SymbolId;
					if (StatementParser.ExpressionStatement == sid)
						iter.InitStatement = _BuildExpressionStatement(c.C(2));
					else if (c.C(2).IsNonTerminal)
						iter.InitStatement = _BuildVariableDeclarationStatement(c.C(2));
					else
						iter.InitStatement = new CodeSnippetStatement().SetLoc(c.C(2));
					if (c.C(3).IsNonTerminal)
						iter.TestExpression = _BuildForTest(c.C(3));
					else
						iter.TestExpression = new CodeSnippetExpression();
					var si = 5;
					if (c.C(4).IsNonTerminal)
					{
						++si;
						var expr = _BuildExpression(c.C(4));
						var bo = expr as CodeBinaryOperatorExpression;
						// turn assign exprs into assign stmts
						if (null != bo && CodeBinaryOperatorType.Assign == bo.Operator)
							iter.IncrementStatement = new CodeAssignStatement(bo.Left, bo.Right).Mark(c.C(5));
						else
							iter.IncrementStatement = new CodeExpressionStatement(_BuildExpression(c.C(5))).Mark(node);
					}
					else
						iter.IncrementStatement = new CodeSnippetStatement();
					var s = c.C(si);
					if (StatementParser.StatementBlock == s.SymbolId)
					{
						for (var i = 1; i < s.Children.Length - 1; i++)
						{
							var stmt = _BuildStatement(s.C(i));
							iter.Statements.Add(stmt);
						}
					}
					else
						iter.Statements.Add(_BuildStatement(s));
					return iter;
				#endregion
				case StatementParser.WhileStatement:
					#region Evaluate While
					iter = new CodeIterationStatement().Mark(node);
					iter.TestExpression = _BuildExpression(c.C(2));
					iter.InitStatement = new CodeSnippetStatement();
					iter.IncrementStatement = new CodeSnippetStatement();
					s = c.C(4);
					if (StatementParser.StatementBlock == s.SymbolId)
					{
						for (var i = 1; i < s.Children.Length - 1; i++)
						{
							var stmt = _BuildStatement(s.C(i));
							iter.Statements.Add(stmt);
						}
					}
					else
						iter.Statements.Add(_BuildStatement(s));
					return iter;
					#endregion
				case StatementParser.IfStatement:
					#region Evaluate If
					var cnd = new CodeConditionStatement().Mark(node);
					cnd.Condition = _BuildExpression(c.C(2));
					s = c.C(4);
					if (StatementParser.StatementBlock == s.SymbolId)
					{
						for (var i = 1; i < s.Children.Length - 1; i++)
						{
							var stmt = _BuildStatement(s.C(i));
							cnd.TrueStatements.Add(stmt);
						}
					}
					else
						cnd.TrueStatements.Add(_BuildStatement(s));
					if(5<c.CL())
					{
						s = c.C(6);
						if (StatementParser.StatementBlock == s.SymbolId)
						{
							for (var i = 1; i < s.Children.Length - 1; i++)
							{
								var stmt = _BuildStatement(s.C(i));
								cnd.FalseStatements.Add(stmt);
							}
						}
						else
							cnd.FalseStatements.Add(_BuildStatement(s));
					}
					return cnd;
					#endregion
				case StatementParser.GotoStatement:
					return new CodeGotoStatement(c.C(1).Value).SetLoc(node);
				case StatementParser.ThrowStatement:
					if (2 < c.CL())
						return new CodeThrowExceptionStatement(_BuildExpression(c.C(1))).Mark(node);
					else
						return new CodeThrowExceptionStatement().Mark(node);
				case StatementParser.ReturnStatement:
					if (2 < c.CL())
						return new CodeMethodReturnStatement(_BuildExpression(c.C(1))).Mark(node);
					else
						return new CodeMethodReturnStatement().Mark(node);
				case StatementParser.TryStatement:
					#region Evaluate Try/Catch/Finally
					var tcf = new CodeTryCatchFinallyStatement().Mark(node);
					tcf.Mark(node);
					s = c.C(1);
					for (var i = 1; i < s.Children.Length - 1; i++)
					{
						var stmt = _BuildStatement(s.C(i));
						tcf.TryStatements.Add(stmt);
					}
					
					si = 2;
					while(StatementParser.CatchClause==c.C(si).SymbolId)
					{
						var cc = c.C(si);
						var ctr = _BuildType(cc.C(2));
						++si;
						var ctc = new CodeCatchClause();
						ctc.CatchExceptionType = ctr;
						var ssi = 5;
						if (ExpressionParser.verbatimIdentifier == cc.C(3).SymbolId)
						{
							ctc.LocalName = cc.C(3).Value.Substring(1);
						}
						else if (ExpressionParser.identifier2 == cc.C(3).SymbolId)
							ctc.LocalName = cc.C(3).Value;
						else --ssi;
						s = cc.C(ssi);
						for (var i = 1; i < s.Children.Length - 1; i++)
						{
							var stmt = _BuildStatement(s.C(i));
							ctc.Statements.Add(stmt);
						}
						tcf.CatchClauses.Add(ctc);

					}
					if(c.CL()>si && StatementParser.FinallyClause==c.C(si).SymbolId)
					{
						s = c.C(si).C(1);
						for (var i = 1; i < s.Children.Length - 1; i++)
						{
							var stmt = _BuildStatement(s.C(i));
							tcf.FinallyStatements.Add(stmt);
						}
					}
					return tcf;
					#endregion
			}
			throw new NotImplementedException();
		}
		
		static CodeStatement _BuildVariableDeclarationStatement(ParseNode node)
		{
			CodeTypeReference vt = null;
			if (StatementParser.varType != node.C(0).SymbolId)
				vt = _BuildType(node.C(0));
			var vd = new CodeVariableDeclarationStatement().SetLoc(node);
			if (null != vt)
				vd.Type = vt;
			vd.Name = node.C(1).Value;
			if (3 < node.CL())
			{
				vd.InitExpression = _BuildExpression(node.C(3));
			}
			if (null != vd.InitExpression)
			{
				if (null == vd.Type || 0 == string.Compare(vd.Type.BaseType, "System.Void"))
					vd.Mark(node, true);
				else
					vd.Mark(node);
			}
			return vd;
		}
		public static CodeExpression ToExpression(ParseNode node)
		{
			return _BuildExpression(node);
		}
		public static CodeTypeReference ToType(ParseNode node)
		{
			return _BuildType(node);
		}
		static T Mark<T>(this T obj,ParseNode node,bool unresolved=false) where T:CodeObject
		{
			obj.UserData["codedomgokit:visit"]=true;
			if(unresolved)
				obj.UserData["slang:unresolved"]= true;
			SetLoc(obj,node);
			return obj;
		}
		static T SetLoc<T>(this T obj,ParseNode node) where T:CodeObject
		{
			obj.UserData["slang:line"]= node.Line;
			obj.UserData["slang:column"]= node.Column;
			obj.UserData["slang:position"]= node.Position;
			return obj;
		}
		[System.Diagnostics.DebuggerNonUserCode()]
		static ParseNode C(this ParseNode pn,int index)
		{
			return pn.Children[index];
		}
		static int CL(this ParseNode pn)
		{
			return pn.Children.Length;
		}
		
		static CodeExpression _BuildExpression(ParseNode node)
		{
			return _BuildAssignExpression(node.C(0));
		}
		static CodeExpression _BuildAssignExpression(ParseNode node)
		{
			var lhs = _BuildOrExpression(node.C(0));
			if(3==node.CL())
			{
				var id = node.C(1).SymbolId;
				var rhs = _BuildOrExpression(node.C(2));
				if (ExpressionParser.eq == id)
				{
					return Mark(new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.Assign, rhs),node);
				} else
				{
					CodeBinaryOperatorType op = CodeBinaryOperatorType.Add;
					switch (id)
					{
						case ExpressionParser.addAssign:
							break;
						case ExpressionParser.subAssign:
							op = CodeBinaryOperatorType.Subtract;
							break;
						case ExpressionParser.mulAssign:
							op = CodeBinaryOperatorType.Multiply;
							break;
						case ExpressionParser.divAssign:
							op = CodeBinaryOperatorType.Divide;
							break;
						case ExpressionParser.modAssign:
							op = CodeBinaryOperatorType.Modulus;
							break;
						case ExpressionParser.bitwiseAndAssign:
							op = CodeBinaryOperatorType.BitwiseAnd;
							break;
						case ExpressionParser.bitwiseOrAssign:
							op = CodeBinaryOperatorType.BitwiseOr;
							break;
					}
					return Mark(new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.Assign,
						Mark(new CodeBinaryOperatorExpression(lhs,op,rhs),node)),node);
				}
			}
			return lhs;
		}
		static CodeExpression _BuildOrExpression(ParseNode node)
		{
			var lhs = _BuildAndExpression(node.C(0));
			if (3 == node.CL())
			{
				var rhs = _BuildAndExpression(node.C(2));
				return Mark(new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.BooleanOr, rhs),node);
			}
			return lhs;
		}
		static CodeExpression _BuildAndExpression(ParseNode node)
		{
			var lhs = _BuildBitwiseOrExpression(node.C(0));
			if (3 == node.CL())
			{
				var rhs = _BuildBitwiseOrExpression(node.C(2));
				return Mark(new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.BooleanAnd, rhs),node);
			}
			return lhs;
		}
		static CodeExpression _BuildBitwiseOrExpression(ParseNode node)
		{
			var lhs = _BuildBitwiseAndExpression(node.C(0));
			if (3 == node.CL())
			{
				var rhs = _BuildBitwiseAndExpression(node.C(2));
				return Mark(new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.BitwiseOr, rhs),node);
			}
			return lhs;
		}
		static CodeExpression _BuildBitwiseAndExpression(ParseNode node)
		{
			var lhs = _BuildEqualityExpression(node.C(0));
			if (3 == node.CL())
			{
				var rhs = _BuildEqualityExpression(node.C(2));
				return Mark(new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.BitwiseAnd, rhs),node);
			}
			return lhs;
		}
		static CodeExpression _BuildEqualityExpression(ParseNode node)
		{
			var lhs = _BuildRelationalExpression(node.C(0));
			if (3 == node.CL())
			{
				var rhs = _BuildRelationalExpression(node.C(2));
				CodeBinaryOperatorType op = CodeBinaryOperatorType.IdentityEquality;
				if (ExpressionParser.notEq == node.C(1).SymbolId)
					op = CodeBinaryOperatorType.IdentityInequality;
				return Mark(new CodeBinaryOperatorExpression(lhs, op, rhs),node,true);
			}
			return lhs;
		}
		static CodeExpression _BuildRelationalExpression(ParseNode node)
		{
			var lhs = _BuildTermExpression(node.C(0));
			if (3 == node.CL())
			{
				var id = node.C(1).SymbolId;
				var rhs = _BuildTermExpression(node.C(2));
				CodeBinaryOperatorType op = CodeBinaryOperatorType.LessThan;
				switch(id)
				{
					case ExpressionParser.lt:
						break;
					case ExpressionParser.gt:
						op = CodeBinaryOperatorType.GreaterThan;
						break;
					case ExpressionParser.lte:
						op = CodeBinaryOperatorType.LessThanOrEqual;
						break;
					case ExpressionParser.gte:
						op = CodeBinaryOperatorType.GreaterThanOrEqual;
						break;
				}
				return Mark(new CodeBinaryOperatorExpression(lhs, op, rhs),node);
			}
			return lhs;
		}
		static CodeExpression _BuildTermExpression(ParseNode node)
		{
			var lhs = _BuildFactorExpression(node.C(0));
			if (3 == node.CL())
			{
				var id = node.C(1).SymbolId;
				var rhs = _BuildFactorExpression(node.C(2));
				CodeBinaryOperatorType op = CodeBinaryOperatorType.Add;
				switch (id)
				{
					case ExpressionParser.add:
						break;
					case ExpressionParser.sub:
						op = CodeBinaryOperatorType.Subtract;
						break;
				}
				return Mark(new CodeBinaryOperatorExpression(lhs, op, rhs),node);
			}
			return lhs;
		}
		static CodeExpression _BuildFactorExpression(ParseNode node)
		{
			var lhs = _BuildUnaryExpression(node.C(0));
			if (3 == node.CL())
			{
				var id = node.C(1).SymbolId;
				var rhs = _BuildUnaryExpression(node.C(2));
				CodeBinaryOperatorType op = CodeBinaryOperatorType.Multiply;
				switch (id)
				{
					case ExpressionParser.mul:
						break;
					case ExpressionParser.div:
						op = CodeBinaryOperatorType.Divide;
						break;
					case ExpressionParser.mod:
						op = CodeBinaryOperatorType.Modulus;
						break;
				}
				return Mark(new CodeBinaryOperatorExpression(lhs, op, rhs),node);
			}
			return lhs;
		}

		static CodeExpression _BuildUnaryExpression(ParseNode node)
		{
			if(2==node.CL())
			{
				var id = node.C(0).SymbolId;
				switch(id)
				{
					case ExpressionParser.add:
						return _BuildUnaryExpression(node.C(1));
					case ExpressionParser.sub:
						var rhs = _BuildUnaryExpression(node.C(1));
						var p = rhs as CodePrimitiveExpression;
						if(null!=p)
						{
							// I didn't forget to Mark these. They don't need to be visited
							if(p.Value is int)
								return new CodePrimitiveExpression(-(int)p.Value).SetLoc(node);
							if (p.Value is long)
								return new CodePrimitiveExpression(-(long)p.Value).SetLoc(node);
							if (p.Value is float)
								return new CodePrimitiveExpression(-(float)p.Value).SetLoc(node);
							if (p.Value is double)
								return new CodePrimitiveExpression(-(double)p.Value).SetLoc(node);
						}
						return Mark(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(0), CodeBinaryOperatorType.Subtract,
							_BuildUnaryExpression(node.C(1))),node);
					case ExpressionParser.not:
						return Mark(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false), CodeBinaryOperatorType.ValueEquality, _BuildUnaryExpression(node.C(1))),node);
					case ExpressionParser.inc:
						rhs = _BuildUnaryExpression(node.C(1));
						return Mark(new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Assign, new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1))),node);
					case ExpressionParser.dec:
						rhs = _BuildUnaryExpression(node.C(1));
						return Mark(new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Assign, new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Subtract, new CodePrimitiveExpression(1))),node);
				}
			} else if (ExpressionParser.CastExpression == node.C(0).SymbolId)
			{
				// cast expression
				return _BuildCastExpression(node.C(0));
			}
			return _BuildPrimaryExpression(node.C(0));
		}
		static CodeExpression _BuildPrimaryExpression(ParseNode node)
		{
			CodeExpression result = null;
			var i = 1;
			var id = node.C(0).SymbolId;
			switch(id)
			{
				case ExpressionParser.TypeRef:
					result = new CodeTypeReferenceExpression(_BuildType(node.C(0).C(0))).Mark(node,true);
					break;
				case ExpressionParser.integerLiteral:
					result = _BuildInteger(node.C(0));
					break;
				case ExpressionParser.floatLiteral:
					result= _BuildFloat(node.C(0));
					break;
				case ExpressionParser.stringLiteral:
					result= _BuildString(node.C(0));
					break;
				case ExpressionParser.verbatimStringLiteral:
					result = _BuildVerbatimString(node.C(0));
					break;
				case ExpressionParser.characterLiteral:
					result= _BuildChar(node.C(0));
					break;
				case ExpressionParser.boolLiteral:
					// doesn't need to be Marked
					result = new CodePrimitiveExpression("true" == node.C(0).Value).SetLoc(node);
					break;
				case ExpressionParser.nullLiteral:
					// doesn't need to be Marked
					return new CodePrimitiveExpression(null);
				case ExpressionParser.typeOf:
					// no need to mark since Type isn't marked
					result = new CodeTypeOfExpression(_BuildType(node.C(2))).SetLoc(node);
					i = 4; // advance past typeof part
					break;
				case ExpressionParser.defaultOf:
					// no need to mark since Type isn't marked
					result = new CodeDefaultValueExpression(_BuildType(node.C(2))).SetLoc(node);
					i = 4; // advance past default part
					break;
				case ExpressionParser.lparen:
					return _BuildExpression(node.C(1));
				
				case ExpressionParser.FieldRef:
					// we treat these as variable refs in the codedom
					result = new CodeVariableReferenceExpression(node.C(0).C(0).Value).Mark(node,true);
					break;
				case ExpressionParser.NewExpression:
					var n = node.C(0);
					var ctr= _BuildTypeElement(n.C(1));
					if (ExpressionParser.rparen==n.C(n.CL()-1).SymbolId)
					{
						var no = new CodeObjectCreateExpression(ctr).Mark(node,true);
						// new object
						if(n.CL()==5)
						{
							no.Parameters.AddRange(_BuildArgList(n.C(3)));
						}
						result = no;
					} else
					{
						// new array
						result = _BuildArraySpec(ctr,n.C(2));
					}
					break;
				case ExpressionParser.thisRef:
					result = new CodeThisReferenceExpression().SetLoc(node);
					break;
				case ExpressionParser.baseRef:
					result = new CodeBaseReferenceExpression().SetLoc(node);
					break;
				default:
					throw new NotImplementedException();
			}
			
			while(i<node.CL())
			{
				var pn = node.C(i);
				if (ExpressionParser.MemberFieldRef == pn.SymbolId)
				{
					result = new CodeFieldReferenceExpression(result, pn.C(1).Value).Mark(node,true);
				}
				else if (ExpressionParser.MemberInvokeRef == pn.SymbolId)
				{
					var di = new CodeDelegateInvokeExpression(result).Mark(node,true);
					di.Parameters.AddRange(_BuildMethodArgList(pn.C(1)));
					result = di;
				} else if(ExpressionParser.MemberIndexerRef==pn.SymbolId)
				{
					var ie = new CodeIndexerExpression(result).Mark(node,true);
					ie.Indices.AddRange(_BuildArgList(pn.C(1)));
				}
				else
					throw new NotImplementedException();
				++i;				
			}
			return result;
		}
		static CodeArrayCreateExpression _BuildArraySpec(CodeTypeReference typeElem,ParseNode node)
		{
			CodeArrayCreateExpression result = null;
			var c = node.C(0);
			if(ExpressionParser.TypeArraySpec==c.SymbolId)
			{
				// array with an initializer
				var ctr = typeElem;
				var i = 0;
				for (;i<node.Children.Length;i++)
				{
					c = node.C(i);
					if (ExpressionParser.TypeArraySpec != c.SymbolId)
						break;
				}
				var ic = i;
				for(i=i-1;0<=i;--i)
				{
					c = node.C(i);
					ctr=_BuildTypeArraySpec(ctr, c);
				}
				if (1 < ctr.ArrayRank)
					throw new NotSupportedException(string.Format("The CodeDOM does not support instantiation of multidimensional arrays at line {0}, column {1}, position {2}", c.Line, c.Column, c.Position));
				result = new CodeArrayCreateExpression(ctr).Mark(node);
				var pn = node.C(ic);
				for (i = 1; i < pn.Children.Length; i++)
				{
					var ppn = pn.Children[i];
					result.Initializers.Add(_BuildExpression(ppn));
					++i;

				}
				return result.Mark(node);
			}
			var cn = c.C(0);
			var expc=_BuildArgList(cn);
			if(1<expc.Count)
				throw new NotSupportedException(string.Format("The CodeDOM does not support instantiation of multidimensional arrays at line {0}, column {1}, position {2}", c.Line, c.Column, c.Position));
			CodeTypeReference cctr = typeElem;
			for (var i = 1;i<node.Children.Length;i++)
				cctr = _BuildTypeArraySpec(cctr, node.C(i));
			return new CodeArrayCreateExpression(new CodeTypeReference(cctr,1).Mark(node), expc[0]).Mark(node);
		}
		static CodeExpression _BuildCastExpression(ParseNode node)
		{
			var ctr = _BuildType(node.C(1));
			var expr = _BuildUnaryExpression(node.C(3));
			return Mark(new CodeCastExpression(ctr, expr),node);
		}
		static CodeTypeReference _BuildType(ParseNode node)
		{
			var result = _BuildTypeElement(node.C(0));
			return _BuildTypeArraySpec(result,node);
		}

		private static CodeTypeReference _BuildTypeArraySpec(CodeTypeReference result,ParseNode node )
		{
			if (2 == node.Children.Length)
			{
				return new CodeTypeReference(result, 1).SetLoc(node);
			}
			else if (1 == node.Children.Length)
				return result;
			var rank = node.Children.Length - 1;
			return new CodeTypeReference(result, rank).SetLoc(node);
		}

		static CodeTypeReference _BuildTypeElement(ParseNode node)
		{
			var result = new CodeTypeReference(_BuildTypeBase(node.C(0))).SetLoc(node);
			if(2==node.CL())
			{
				result.TypeArguments.AddRange(_BuildTypeGenericPart(node.C(1)));
			}
			return result;
		}
		static CodeTypeReferenceCollection _BuildTypeGenericPart(ParseNode node)
		{
			var result = new CodeTypeReferenceCollection();
			for(var i = 0;i<node.Children.Length-1;i++)
			{
				++i;
				result.Add(_BuildType(node.C(i)));
			}
			return result;
		}
		static string _BuildTypeBase(ParseNode node)
		{
			var result = "";
			for(var i = 0;i<node.Children.Length;i++)
			{
				var v = node.C(i).Value;
				switch (node.C(i).SymbolId)
				{
					case ExpressionParser.IntrinsicType:
						var id = node.C(i).C(0).SymbolId;
						switch (id)
						{
							case ExpressionParser.boolType:
								return "System.Boolean";
							case ExpressionParser.objectType:
								return "System.Object";
							case ExpressionParser.charType:
								return "System.Char";
							case ExpressionParser.stringType:
								return "System.String";
							case ExpressionParser.byteType:
								return "System.Byte";
							case ExpressionParser.sbyteType:
								return "System.SByte";
							case ExpressionParser.shortType:
								return "System.Int16";
							case ExpressionParser.ushortType:
								return "System.UInt16";
							case ExpressionParser.intType:
								return "System.Int32";
							case ExpressionParser.uintType:
								return "System.UInt32";
							case ExpressionParser.longType:
								return "System.Int64";
							case ExpressionParser.ulongType:
								return "System.UInt64";
							case ExpressionParser.floatType:
								return "System.Single";
							case ExpressionParser.doubleType:
								return "System.Double";
							case ExpressionParser.decimalType:
								return "System.Decimal";
							default:
								throw new NotImplementedException();
						}
					case dot:
						result = string.Concat(result, ".");
						break;
					case verbatimIdentifier:
						// codedom doesn't want leading @
						result = string.Concat(result,v.Substring(1));
						break;
					case identifier2:
						result = string.Concat(result, v);
						break;

				}
			}
			return result;
		}
		static CodeExpressionCollection _BuildMethodArgList(ParseNode node)
		{
			var result = new CodeExpressionCollection();
			for(var i = 0;i<node.Children.Length;i++)
			{
				var pn = node.Children[i];
				if (2 == pn.CL())
				{
					var expr = _BuildExpression(pn.C(1));
					var id = pn.C(0).SymbolId;
					FieldDirection fd = FieldDirection.In;
					switch (id)
					{
						case ExpressionParser.refKeyword:
							fd = FieldDirection.Ref;
							break;
						case ExpressionParser.outKeyword:
							fd = FieldDirection.Out;
							break;
					}
					result.Add(Mark(new CodeDirectionExpression(fd, expr),node));
				}
				else
					result.Add(_BuildExpression(pn.C(0)));
				++i;
			}
			return result;
		}
		static CodeExpressionCollection _BuildArgList(ParseNode node)
		{
			var result = new CodeExpressionCollection();
			for (var i = 0; i < node.Children.Length; i++)
			{
				var pn = node.Children[i];
				result.Add(_BuildExpression(pn));
				++i;
			}
			return result;
		}
		#region Eval Primitives
		static CodePrimitiveExpression _BuildString(ParseNode node)
		{
			var sb = new StringBuilder();
			var e = node.Value.GetEnumerator();
			e.MoveNext();
			if (e.MoveNext())
			{
				while (true)
				{
					if ('\"' == e.Current)
						return new CodePrimitiveExpression(sb.ToString()).SetLoc(node);
					else if ('\\' == e.Current)
						sb.Append(_ParseEscapeChar(e, node));
					else
					{
						sb.Append(e.Current);
						if (!e.MoveNext())
							break;
					}
				}
			}
			throw new SyntaxException("Unterminated string in input", node.Line,node.Column,node.Position);
		}
		static CodePrimitiveExpression _BuildVerbatimString(ParseNode node)
		{
			var sb = new StringBuilder();
			var e = node.Value.GetEnumerator();
			e.MoveNext();
			e.MoveNext();
			if (e.MoveNext())
			{
				while (true)
				{
					if ('\"' == e.Current)
					{
						
						if(!e.MoveNext() || '\"' !=e.Current)
							return new CodePrimitiveExpression(sb.ToString()).SetLoc(node);
						sb.Append('\"');
						if (!e.MoveNext())
							break;
					}
					else
					{
						sb.Append(e.Current);
						if (!e.MoveNext())
							break;
					}
				}
			}
			throw new SyntaxException("Unterminated string in input", node.Line, node.Column, node.Position);
		}
		static CodePrimitiveExpression _BuildChar(ParseNode node)
		{
			var s = node.Value;
			// remove quotes.
			s = s.Substring(1, s.Length - 2);
			var e = s.GetEnumerator();
			e.MoveNext();
			if ('\\' == e.Current)
			{
				s = _ParseEscapeChar(e, node);
				if (1 == s.Length)
					return new CodePrimitiveExpression(s[0]).SetLoc(node);
				else
					return new CodePrimitiveExpression(s).SetLoc(node); // for UTF-32 this has to be a string
			}
			return new CodePrimitiveExpression(s[0]).SetLoc(node);
		}
		static CodePrimitiveExpression _BuildFloat(ParseNode node)
		{
			var s = node.Value;
			var ch = char.ToLowerInvariant(s[s.Length - 1]);
			var isDouble = 'd' == ch;
			var isDecimal = 'm' == ch;
			var isFloat = 'f' == ch;
			if ((isDouble || isDecimal || isFloat))
				s = s.Substring(0, s.Length - 1);
			else
				isDouble = true;
			object n = null;
			if (isFloat)
				n = float.Parse(s);
			else if (isDecimal)
				n = decimal.Parse(s);
			else
				n = double.Parse(s);
			return new CodePrimitiveExpression(n).SetLoc(node);
		}
		static CodePrimitiveExpression _BuildInteger(ParseNode node)
		{
			var s = node.Value;
			var isLong = false;
			var isUnsigned = false;
			var isNeg = '-' == s[0];
			var isHex = s.StartsWith("-0x") || s.StartsWith("0x");
			var ch = char.ToLowerInvariant(s[s.Length - 1]);
			if ('l' == ch)
			{
				isLong = true;
				s = s.Substring(0, s.Length - 1);
			}
			else if ('u' == ch)
			{
				isUnsigned = true;
				s = s.Substring(0, s.Length - 1);
			}
			// do it twice in case we have like, "ul" or "lu" at the end
			// this routine would accept "ll" or "uu" but it doesn't matter
			// because the lexer won't.
			ch = char.ToLowerInvariant(s[s.Length - 1]);
			if ('l' == ch)
			{
				isLong = true;
				s = s.Substring(0, s.Length - 1);
			}
			else if ('u' == ch)
			{
				isUnsigned = true;
				s = s.Substring(0, s.Length - 1);
			}
			// parse this into a double so we can do bounds checking
			if (isHex)
				s = s.Substring(2);
			var d = (double)long.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			object n = null;
			if (isUnsigned && (isLong || (d <= uint.MaxValue && d >= uint.MinValue)))
			{
				if (isNeg)
				{
					if (!isHex)
						n = unchecked((ulong)long.Parse(s));
					else
						n = unchecked((ulong)-long.Parse(s.Substring(1), NumberStyles.AllowHexSpecifier));
				}
				else
					n = ulong.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			}
			else if (isUnsigned)
			{
				if (isNeg)
				{
					if (!isHex)
						n = unchecked((uint)int.Parse(s));
					else
						n = unchecked((uint)-int.Parse(s.Substring(1), NumberStyles.AllowHexSpecifier));
				}
				else
					n = uint.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			}
			else
			{
				if (isNeg)
				{
					if (!isHex)
						n = int.Parse(s);
					else
						n = unchecked(-int.Parse(s.Substring(1), NumberStyles.AllowHexSpecifier));
				}
				else
					n = int.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			}
			return new CodePrimitiveExpression(n).SetLoc(node);
		}
		#endregion

		#region String/Char escapes
		static string _ParseEscapeChar(IEnumerator<char> e, ParseNode node)
		{
			if (e.MoveNext())
			{
				switch (e.Current)
				{
					case 'r':
						e.MoveNext();
						return "\r";
					case 'n':
						e.MoveNext();
						return "\n";
					case 't':
						e.MoveNext();
						return "\t";
					case 'a':
						e.MoveNext();
						return "\a";
					case 'b':
						e.MoveNext();
						return "\b";
					case 'f':
						e.MoveNext();
						return "\f";
					case 'v':
						e.MoveNext();
						return "\v";
					case '0':
						e.MoveNext();
						return "\0";
					case '\\':
						e.MoveNext();
						return "\\";
					case '\'':
						e.MoveNext();
						return "\'";
					case '\"':
						e.MoveNext();
						return "\"";
					case 'u':
						var acc = 0L;
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						e.MoveNext();
						return unchecked((char)acc).ToString();
					case 'x':
						acc = 0;
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (e.MoveNext() && _IsHexChar(e.Current))
						{
							acc <<= 4;
							acc |= _FromHexChar(e.Current);
							if (e.MoveNext() && _IsHexChar(e.Current))
							{
								acc <<= 4;
								acc |= _FromHexChar(e.Current);
								if (e.MoveNext() && _IsHexChar(e.Current))
								{
									acc <<= 4;
									acc |= _FromHexChar(e.Current);
									e.MoveNext();
								}
							}
						}
						return unchecked((char)acc).ToString();
					case 'U':
						acc = 0;
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						e.MoveNext();
						return char.ConvertFromUtf32(unchecked((int)acc));
					default:
						throw new NotSupportedException(string.Format("Unsupported escape sequence \\{0}", e.Current));
				}
			}
			throw new SyntaxException("Unterminated escape sequence", node.Line,node.Column,node.Position);
		}
		static bool _IsHexChar(char hex)
		{
			return (':' > hex && '/' < hex) ||
				('G' > hex && '@' < hex) ||
				('g' > hex && '`' < hex);
		}
		static byte _FromHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new ArgumentException("The value was not hex.", "hex");
		}
		#endregion
	}
}
