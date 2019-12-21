using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;

namespace Parsley
{
	using C = CD.CodeDomUtility;
	using V = CD.CodeDomVisitor;
	static class CodeGenerator
	{
		public static readonly CodeAttributeDeclaration GeneratedCodeAttribute
			= new CodeAttributeDeclaration(C.Type(typeof(GeneratedCodeAttribute)), new CodeAttributeArgument(C.Literal(Program.Name)), new CodeAttributeArgument(C.Literal(Assembly.GetExecutingAssembly().GetName().Version.ToString())));
		public static CodeCompileUnit GenerateSharedCompileUnit(string @namespace)
		{
			var ns = new CodeNamespace();
			if (!string.IsNullOrEmpty(@namespace))
				ns.Name = @namespace;
			var parserContext = Deslanged.ParserContext.Namespaces[Deslanged.ParserContext.Namespaces.Count - 1].Types[0];
			parserContext.CustomAttributes.Add(GeneratedCodeAttribute);
			var parseNode = Deslanged.ParseNode.Namespaces[Deslanged.ParseNode.Namespaces.Count - 1].Types[0];
			parseNode.CustomAttributes.Add(GeneratedCodeAttribute);
			var syntaxException = Deslanged.SyntaxException.Namespaces[Deslanged.SyntaxException.Namespaces.Count - 1].Types[0];
			syntaxException.CustomAttributes.Add(GeneratedCodeAttribute);
			_FixParserContext(parserContext);
			ns.Types.Add(syntaxException);
			ns.Types.Add(parseNode);
			ns.Types.Add(parserContext);
			var result = new CodeCompileUnit();
			result.Namespaces.Add(ns);
			V.Visit(result, (ctx) => {
				var ctr = ctx.Target as CodeTypeReference;
				if (null != ctr)
				{
					if (ctr.BaseType.StartsWith("Parsley."))
						ctr.BaseType = ctr.BaseType.Substring(8);
				}
			});
			return result;
		}
		public static CodeCompileUnit GenerateCompileUnit(XbnfDocument doc, CfgDocument cfg,string name = null,string @namespace=null)
		{
			if (0 == cfg.Rules.Count)
				throw new ArgumentException("The CFG document contains no rules.", nameof(cfg));
			var result = new CodeCompileUnit();
			var ns = new CodeNamespace();
			cfg.RebuildCache();
			if (!string.IsNullOrEmpty(@namespace))
				ns.Name = @namespace;
			ns.Imports.Add(new CodeNamespaceImport("System"));
			ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			result.Namespaces.Add(ns);
			if (null==name)
			{
				name = cfg.StartSymbol + "Parser";
			}
			var parser = Deslanged.Parser.Namespaces[Deslanged.Parser.Namespaces.Count - 1].Types[0];
			parser.CustomAttributes.Add(GeneratedCodeAttribute);
			parser.Comments.Clear();
			var dc = doc.ToString("xc");
			var sb = new StringBuilder();
			for (int ic = cfg.Rules.Count, i = 0; i < ic; ++i)
				sb.AppendLine(cfg.Rules[i].ToString());
			var cc = sb.ToString();
			parser.Comments.AddRange(C.ToComments(string.Format("<summary>Parses the following grammar:\r\n{0}\r\n</summary>\r\n<remarks>The rules for the factored grammar are as follows:\r\n{1}\r\n</remarks>", dc.TrimEnd(),cc.TrimEnd()),true));
			var hasColNS = false;
			foreach(CodeNamespaceImport nsi in ns.Imports)
			{
				if(0==string.Compare(nsi.Namespace,"System.Collections.Generic"))
				{
					hasColNS = true;
					break;
				}
			}
			if(!hasColNS)
				ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			
			var syms = cfg.FillSymbols();
			var consts = new string[syms.Count];
			for (var i = 0; i < consts.Length; i++)
			{
				if ('#' != syms[i][0])
				{
					var s = _MakeSafeName(syms[i]);
					s = _MakeUniqueMember(parser, s);
					var fld = C.Field(typeof(int), s, MemberAttributes.Public | MemberAttributes.Const, C.Literal(cfg.GetIdOfSymbol(syms[i])));
					parser.Members.Add(fld);
					consts[i] = s;
				}
				else if ("#ERROR" == syms[i])
				{
					consts[i] = "ErrorSymbol";
				}
				else if ("#EOS" == syms[i])
				{
					consts[i] = "EosSymbol";
				}
			}
			var predict = cfg.FillPredict();
			var follows = cfg.FillFollows();
			_BuildParserParseFunctions(parser,doc,cfg,consts,predict,follows);
			var hasEval = false;
			for (int ic=doc.Productions.Count,i=0;i<ic;++i)
			{
				if(null!=doc.Productions[i].Code)
				{
					hasEval = true;
					break;
				}
			}
			if(hasEval)
				_BuildParserEvalFunctions(parser, doc, cfg,consts, predict, follows);
			ns.Types.Add(parser);
			V.Visit(result, (ctx) => {
				var ctr = ctx.Target as CodeTypeReference;
				if (null != ctr)
				{
					if (0 == string.Compare("Parser", ctr.BaseType, StringComparison.InvariantCulture))
						ctr.BaseType = name;
					else if (ctr.BaseType.StartsWith("Parsley."))
						ctr.BaseType = ctr.BaseType.Substring(8);
				}
			});
			parser.Name = name;
			return result;
		}
		static string _MakeSafeName(string name)
		{
			var sb = new StringBuilder();
			if (char.IsDigit(name[0]))
				sb.Append('_');
			for (var i = 0; i < name.Length; ++i)
			{
				var ch = name[i];
				if ('_' == ch || char.IsLetterOrDigit(ch))
					sb.Append(ch);
				else
					sb.Append('_');
			}
			return sb.ToString();
		}
		static string _MakeUniqueMember(CodeTypeDeclaration decl, string name)
		{
			var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			for (int ic = decl.Members.Count, i = 0; i < ic; i++)
				seen.Add(decl.Members[i].Name);
			var result = name;
			var suffix = 2;
			while (seen.Contains(result))
			{
				result = string.Concat(name, suffix.ToString());
				++suffix;
			}
			return result;
		}

		static void _BuildParserParseFunctions(
			CodeTypeDeclaration parser,
			XbnfDocument doc,
			CfgDocument cfg,
			string[] consts,
			IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> predict,
			IDictionary<string,ICollection<string>> follows
			)
		{
			
			var syms = cfg.FillSymbols();
			
			var context = C.ArgRef("context");
			var nts = cfg.FillNonTerminals();
			for (int ic = nts.Count, i = 0; i < ic; ++i)
			{
				var es = new HashSet<string>();
				var nt = nts[i];
				var m = C.Method(C.Type("ParseNode"), string.Concat("_Parse", nt), MemberAttributes.Private | MemberAttributes.Static, C.Param("ParserContext", "context"));
				m.Statements.Add(C.Var(typeof(int), "line", C.PropRef(C.ArgRef("context"), "Line")));
				m.Statements.Add(C.Var(typeof(int), "column", C.PropRef(C.ArgRef("context"), "Column")));
				m.Statements.Add(C.Var(typeof(long), "position", C.PropRef(C.ArgRef("context"), "Position")));
				var l = C.VarRef("line");
				var c = C.VarRef("column");
				var p = C.VarRef("position");
				var pred = predict[nt];
				Dictionary<CfgRule, ICollection<string>> rmap = _BuildRuleMap(follows, es, nt, pred);
				foreach (var r in rmap)
				{
					var cnd = _BuildIfRuleExprsCnd(syms, consts, context, r);
					if (null == cnd)
						continue;
					// _Parse{nt}()
					var cs = C.If(cnd);
					m.Statements.Add(cs);
					cs.TrueStatements.Add(new CodeCommentStatement(r.Key.ToString()));

					var collapsed = new HashSet<string>();
					for (int jc = r.Key.Right.Count, j = 0; j < jc; ++j)
					{
						var o = cfg.GetAttribute(r.Key.Right[j], "collapsed");
						if (o is bool && (bool)o)
							collapsed.Add(r.Key.Right[j]);
					}
					if (0 == collapsed.Count)
						cs.TrueStatements.Add(C.Var(C.Type("ParseNode", 1), "children", C.NewArr("ParseNode", r.Key.Right.Count)));
					else {
						var lt = C.Type(typeof(List<>));
						lt.TypeArguments.Add(C.Type("ParseNode"));
						cs.TrueStatements.Add(C.Var(lt, "children", C.New(lt)));
					}
					for (int jc = r.Key.Right.Count, j = 0; j < jc; ++j)
					{
						var s = r.Key.Right[j];
						if (cfg.IsNonTerminal(s))
						{
							var pinv = C.Invoke(C.TypeRef("Parser"), string.Concat("_Parse", s), C.ArgRef("context"));
							if (0 == collapsed.Count)
								cs.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(j)), pinv));
							else
							{
								if(collapsed.Contains(s))
								{
									cs.TrueStatements.Add(C.Call(C.VarRef("children"), "AddRange", C.PropRef(pinv, "Children")));
								} else
									cs.TrueStatements.Add(C.Call(C.VarRef("children"), "Add", pinv));
							}
						}
						else
						{
							var ts = s;
							var subs = cfg.GetAttribute(s, "substitute") as string;
							if (!string.IsNullOrEmpty(subs))
							{
								ts = subs;
							}
							var si = syms.IndexOf(ts);
							var fr = C.FieldRef(C.TypeRef("Parser"), consts[si]);
							var np = C.New(C.Type("ParseNode"), fr,C.Literal(ts), C.FieldRef(context, "Value"),l,c,p);
							if(0==collapsed.Count)
								cs.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(j)), np));
							else if(!collapsed.Contains(s))
								cs.TrueStatements.Add(C.Call(C.VarRef("children"),"Add", np));
							if (0 != j)
							{
								var ccs = C.If(C.Eq(fr, C.FieldRef(context, "SymbolId")));
								cs.TrueStatements.Add(ccs);
								cs.TrueStatements.Add(C.Call(context, "Error", C.Literal(string.Concat("Expecting ", s))));
								cs = ccs;
							}
							cs.TrueStatements.Add(C.Call(context, "Advance"));
							
						}
					}
					var subst=cfg.GetAttribute(nt,"substitute") as string;
					if(!string.IsNullOrEmpty(subst))
					{
						nt = subst;
					}
					var ffr = C.FieldRef(C.TypeRef("Parser"), consts[syms.IndexOf(nt)]);
					if (0 == collapsed.Count)
						cs.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.VarRef("children"), l, c, p)));
					else
						cs.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.Invoke(C.VarRef("children"),"ToArray"),l,c,p)));
					
					var ppi = doc.Productions.IndexOf(nt);
					if(-1<ppi)
					{
						var prod = doc.Productions[i];
						if (0 < prod.Line && !string.IsNullOrEmpty(doc.Filename))
							m.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
					}

				}

				var cc = es.Count - 1;
				var exp = new StringBuilder();
				exp.Append("Expecting ");
				// english sucks
				if (1 == cc)
				{
					var delim = "";
					foreach (var s in es)
					{
						exp.Append(delim);
						exp.Append(s);
						delim = " or ";
					}
				}
				else if (0 < cc)
				{
					var delim = "";
					foreach (var s in es)
					{
						exp.Append(delim);
						if (0 == cc)
							exp.Append("or ");
						exp.Append(s);
						delim = ", ";
						--cc;
					}
				}
				else if (0 == cc)
					exp.Append(es.First());

				m.Statements.Add(C.Call(context, "Error", C.Literal(exp.ToString())));
				m.Statements.Add(C.Return(C.Null));
				var pi = doc.Productions.IndexOf(nt);
				if (!string.IsNullOrEmpty(doc.Filename))
				{
					if (-1 < pi)
					{
						var prod = doc.Productions[pi];
						if (0 != prod.Line)
						{
							m.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
						}
					}
				}
				parser.Members.Add(m);
			}
			var rs = new StringBuilder();

			for (int ic = doc.Productions.Count,i=0;i<ic;++i)
			{
				var prod = doc.Productions[i];
				if (!prod.IsTerminal && !prod.IsCollapsed)
				{
					var s = prod.Name;
					var t = new CodeTypeReference(typeof(IEnumerable<>));
					t.TypeArguments.Add(C.Type("Token"));
					var m = C.Method(C.Type("ParseNode"), string.Concat("Parse", s), MemberAttributes.Static | MemberAttributes.Public, C.Param(t, "tokenizer"));
					m.Statements.Add(C.Var(C.Type("ParserContext"), "context", C.New(C.Type("ParserContext"), C.ArgRef("tokenizer"))));
					m.Statements.Add(C.Call(C.VarRef("context"), "EnsureStarted"));
					m.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("_Parse", s), C.VarRef("context"))));
					if (!string.IsNullOrEmpty(doc.Filename) && 0 != prod.Line)
						m.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
					rs.Clear();
					foreach (var r in cfg.FillNonTerminalRules(prod.Name))
						rs.AppendLine(r.ToString());
					m.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nParses a production of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"tokenizer\">The tokenizer to parse with</param><returns>A <see cref=\"ParseNode\" /> representing the parsed tokens</returns>",prod.ToString("p").TrimEnd(),rs.ToString().TrimEnd()),true));
					parser.Members.Add(m);
				}
			}
			var pprod = doc.StartProduction;
			var ss = pprod.Name;
			var et = new CodeTypeReference(typeof(IEnumerable<>));
			et.TypeArguments.Add(C.Type("Token"));
			var sm = C.Method(C.Type("ParseNode"), "Parse", MemberAttributes.Static | MemberAttributes.Public, C.Param(et, "tokenizer"));
			sm.Statements.Add(C.Var(C.Type("ParserContext"), "context", C.New(C.Type("ParserContext"), C.ArgRef("tokenizer"))));
			sm.Statements.Add(C.Call(C.VarRef("context"), "EnsureStarted"));
			sm.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("_Parse", ss), C.VarRef("context"))));
			rs.Clear();
			foreach (var r in cfg.FillNonTerminalRules(pprod.Name))
				rs.AppendLine(r.ToString());
			sm.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nParses a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"tokenizer\">The tokenizer to parse with</param><returns>A <see cref=\"ParseNode\" /> representing the parsed tokens</returns>", pprod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));

			if (!string.IsNullOrEmpty(doc.Filename) && 0 != pprod.Line)
				sm.LinePragma = new CodeLinePragma(doc.Filename, pprod.Line);

			parser.Members.Add(sm);

		}
		static void _BuildParserEvalFunctions(
			CodeTypeDeclaration parser,
			XbnfDocument doc,
			CfgDocument cfg,
			string[] consts,
			IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> predict,
			IDictionary<string, ICollection<string>> follows
			)
		{
			var hasChangeType = false;
			var syms = cfg.FillSymbols();
			var node = C.ArgRef("node");
			for(int ic=doc.Productions.Count,i=0;i<ic;++i)
			{
				var prod = doc.Productions[i];
				var isStart = ReferenceEquals(doc.StartProduction, prod);
				if (null!=prod.Code)
				{
					var type = new CodeTypeReference(typeof(object));
					CodeTypeReference typeConverter = null;

					var ti = prod.Attributes.IndexOf("type");
					if (-1<ti)
					{
						var s = prod.Attributes[ti].Value as string;
						if (!string.IsNullOrEmpty(s))
							type = new CodeTypeReference(CD.CodeDomResolver.TranslateIntrinsicType(s));

						ti = prod.Attributes.IndexOf("typeConverter");
						if (-1 < ti)
						{
							s = prod.Attributes[ti].Value as string;
							if (!string.IsNullOrEmpty(s))
								typeConverter = new CodeTypeReference(s);
						}
					}

					MemberAttributes attrs; 
					attrs = MemberAttributes.Public| MemberAttributes.Static;
					var rs = new StringBuilder();
					foreach (var r in cfg.FillNonTerminalRules(prod.Name))
						rs.AppendLine(r.ToString());

					if (isStart)
					{
						var ms = C.Method(type, "Evaluate", attrs, C.Param(C.Type("ParseNode"), "node"));
						if (0 != prod.Line && !string.IsNullOrEmpty(doc.Filename))
							ms.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
						ms.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
						ms.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("Evaluate", prod.Name), C.ArgRef("node"))));
						parser.Members.Add(ms);
						ms = C.Method(type, "Evaluate", attrs, C.Param(C.Type("ParseNode"), "node"),C.Param(C.Type(typeof(object)),"state"));
						if (0 != prod.Line && !string.IsNullOrEmpty(doc.Filename))
							ms.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
						ms.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<param name=\"state\">A user supplied state object. What it should be depends on the production's associated code block</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
						ms.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("Evaluate", prod.Name), C.ArgRef("node"),C.ArgRef("state"))));
						parser.Members.Add(ms);
					}
					var m = C.Method(type, string.Concat("Evaluate", prod.Name), attrs, C.Param("ParseNode", "node"), C.Param(typeof(object), "state"));
					if (0 != prod.Line)
						m.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
					m.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<param name=\"state\">A user supplied state object. What it should be depends on the production's associated code block</param>\r\n<returns>The result of the evaluation</returns>",prod.ToString("p").TrimEnd(),rs.ToString().TrimEnd()), true));
					var cnst = consts[syms.IndexOf(prod.Name)];
					var fr = C.FieldRef(C.TypeRef("Parser"), cnst);
					var cnd = C.If(C.Eq(fr, C.PropRef(node, "SymbolId")));
					cnd.TrueStatements.AddRange(CD.SlangParser.ParseStatements(prod.Code, true));
					m.Statements.Add(cnd);
					m.Statements.Add(C.Throw(C.New(C.Type("SyntaxException"), C.Literal(string.Concat("Expecting ", prod.Name)), C.PropRef(node, "Line"), C.PropRef(node, "Column"), C.PropRef(node, "Position"))));
					parser.Members.Add(m);
					m = C.Method(m.ReturnType, m.Name, attrs, C.Param("ParseNode", "node"));
					if (0 != prod.Line && !string.IsNullOrEmpty(doc.Filename))
						m.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
					m.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
					m.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), m.Name, C.ArgRef("node"), C.Null)));
					parser.Members.Add(m);
					var hasReturn = false;
					V.Visit(cnd, (ctx) => {
						var v =ctx.Target as CodeVariableReferenceExpression;
						if (null != v)
						{
							foreach (var p in doc.Productions)
							{
								if (p.IsHidden || p.IsCollapsed)
									continue;
								if (v.VariableName.StartsWith(p.Name, StringComparison.InvariantCulture))
								{
									if (p.Name.Length < v.VariableName.Length)
									{
										var s = v.VariableName.Substring(p.Name.Length);
										int num;
										if (int.TryParse(s, out num))
										{
											if (0 < num)
											{
												if (!p.IsTerminal)
												{
													var mi = C.Invoke(C.TypeRef("Parser"), string.Concat("Evaluate", p.Name), C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), C.ArgRef("state"));
													V.ReplaceTarget(ctx, mi);
												}
												else
												{
													var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), "Value");
													V.ReplaceTarget(ctx, pr);
												}
											}
										}
									}
								}
								else
								{
									if (v.VariableName.StartsWith("SymbolId"))
									{
										if (8 < v.VariableName.Length)
										{
											var s = v.VariableName.Substring(8);
											int num;
											if (int.TryParse(s, out num))
											{
												if (0 < num)
												{

													var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), "SymbolId");
													V.ReplaceTarget(ctx, pr);

												}
											}
										}
									}
								}
							}
						}
					});
					V.Visit(cnd, (ctx) =>
					{
						var r = ctx.Target as CodeMethodReturnStatement;
						if (null != r)
						{
							if (!CD.CodeDomResolver.IsNullOrVoidType(type) && (0 != type.ArrayRank || 0 != string.Compare("System.Object", type.BaseType, StringComparison.InvariantCulture)))
							{
								var hasVoid = false;
								if (null != r.Expression)
								{
									var p = r.Expression as CodePrimitiveExpression;
									if (null != p)
									{
										if (null == p.Value)
											hasVoid = true;
									}
								}
								if (null == r.Expression || hasVoid)
								{
									r.Expression = C.Default(type);
								}
								else
								{
									var isType = false;
									var cc = r.Expression as CodeCastExpression;
									if (null != cc)
									{
										if (CD.CodeTypeReferenceEqualityComparer.Equals(cc.TargetType, type))
											isType = true;
									}
									if (!isType)
									{
										hasChangeType = true;
										r.Expression = C.Cast(type, C.Invoke(C.TypeRef("Parser"), "_ChangeType", r.Expression, C.TypeOf(type)));
									}
								}
							}
							hasReturn = true;
						}
					});
					if(!hasReturn)
					{
						if (!CD.CodeDomResolver.IsNullOrVoidType(type))
							cnd.TrueStatements.Add(C.Return(C.Default(type)));
						else
							cnd.TrueStatements.Add(C.Return(C.Null));
					}
				}
			}
			if (hasChangeType)
			{

				var m = C.Method(C.Type(typeof(object)), "_ChangeType", MemberAttributes.Static | MemberAttributes.Private, C.Param(typeof(object), "obj"), C.Param(typeof(Type), "type"));
				m.Statements.Add(C.Var(typeof(TypeConverter), "typeConverter", C.Invoke(C.TypeRef(typeof(TypeDescriptor)), "GetConverter", C.ArgRef("obj"))));
				// if(null!=typeConverter || !typeConverter.CanConvertTo(type))
				m.Statements.Add(C.If(C.Or(C.IdentEq(C.Null, C.VarRef("typeConverter")), C.Not(C.Invoke(C.VarRef("typeConverter"), "CanConvertTo", C.ArgRef("type")))),
					C.Return(C.Invoke(C.TypeRef(typeof(Convert)), "ChangeType", C.ArgRef("obj"), C.ArgRef("type")))
					));
				m.Statements.Add(C.Return(C.Invoke(C.VarRef("typeConverter"), "ConvertTo", C.ArgRef("obj"), C.ArgRef("type"))));
				parser.Members.Add(m);
			}
			
		}

		private static Dictionary<CfgRule, ICollection<string>> _BuildRuleMap(IDictionary<string, ICollection<string>> follows, HashSet<string> es, string nt, ICollection<(CfgRule Rule, string Symbol)> pred)
		{
			var rmap = new Dictionary<CfgRule, ICollection<string>>();
			foreach (var p in pred)
			{
				if (null != p.Symbol)
				{
					es.Add(p.Symbol);
					ICollection<string> col;
					if (!rmap.TryGetValue(p.Rule, out col))
					{
						col = new List<string>();
						rmap.Add(p.Rule, col);
					}
					col.Add(p.Symbol);
				}
				else
				{
					foreach (var s in follows[nt])
						es.Add(s);
					rmap.Add(p.Rule, follows[nt]);
				}
			}

			return rmap;
		}

		private static CodeExpression _BuildIfRuleExprsCnd(IList<string> syms, string[] consts, CodeExpression context, KeyValuePair<CfgRule, ICollection<string>> r)
		{
			var exprs = new CodeExpressionCollection();
			foreach (var s in r.Value)
			{
				if (null != s)
				{
					var si = syms.IndexOf(s);
					var fr = C.FieldRef(C.TypeRef("Parser"), consts[si]);
					exprs.Add(C.Eq(fr, C.PropRef(context, "SymbolId")));
				}
			}
			switch (exprs.Count)
			{
				case 0:
					return null;
				case 1:
					return exprs[0];
				default:
					return C.BinOp(exprs, CodeBinaryOperatorType.BooleanOr);
			}
		}

		static void _FixParserContext(CodeTypeDeclaration parserContext)
		{
			V.Visit(parserContext, (ctx) => {
				var ctr = ctx.Target as CodeTypeReference;
				if (null != ctr)
				{
					if (1 == ctr.TypeArguments.Count)
					{
						if (0 == string.Compare("System.Collections.Generic.IEnumerable`1", ctr.BaseType, StringComparison.InvariantCulture) ||
							(0 == string.Compare("IEnumerable`1", ctr.BaseType, StringComparison.InvariantCulture)) ||
							(0 == string.Compare("System.Collections.IEnumerator`1", ctr.BaseType, StringComparison.InvariantCulture)) ||
							(0 == string.Compare("IEnumerator`1", ctr.BaseType, StringComparison.InvariantCulture)))
						{
							if (0 == string.Compare("System.Object", ctr.TypeArguments[0].BaseType, StringComparison.InvariantCultureIgnoreCase))
							{
								ctr.TypeArguments[0] = new CodeTypeReference("Token");
							}
						}
					}
					return;
				}
				var c = ctx.Target as CodeCastExpression;
				if (null != c && 0 == string.Compare("Token", c.TargetType.BaseType, StringComparison.InvariantCulture))
				{
					V.ReplaceTarget(ctx, c.Expression);
				}
			});
		}
	}
}
