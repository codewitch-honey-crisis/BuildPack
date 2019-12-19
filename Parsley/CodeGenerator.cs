using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parsley
{
	using C = CD.CodeDomUtility;
	using V = CD.CodeDomVisitor;
	static class CodeGenerator
	{
		public static CodeCompileUnit GenerateSharedCompileUnit(string @namespace)
		{
			var ns = new CodeNamespace();
			if (!string.IsNullOrEmpty(@namespace))
				ns.Name = @namespace;
			var parserContext = Deslanged.ParserContext.Namespaces[Deslanged.ParserContext.Namespaces.Count - 1].Types[0];
			var parseNode = Deslanged.ParseNode.Namespaces[Deslanged.ParseNode.Namespaces.Count - 1].Types[0];
			var syntaxException = Deslanged.SyntaxException.Namespaces[Deslanged.SyntaxException.Namespaces.Count - 1].Types[0];
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
					m.Statements.Add(cs);

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
						/*
						var prod = doc.Productions[pi];
						if (0 != prod.Line)
						{
							m.LinePragma = new CodeLinePragma(filename, prod.Line);
						}
						*/
					}
				}
				parser.Members.Add(m);
			}

			var ss = cfg.StartSymbol;
			var et = new CodeTypeReference(typeof(IEnumerable<>));
			et.TypeArguments.Add(C.Type("Token"));
			var sm = C.Method(C.Type("ParseNode"), string.Concat("Parse", ss), MemberAttributes.Static | MemberAttributes.Public, C.Param(et, "tokenizer"));
			sm.Statements.Add(C.Var(C.Type("ParserContext"), "context", C.New(C.Type("ParserContext"), C.ArgRef("tokenizer"))));
			sm.Statements.Add(C.Call(C.VarRef("context"), "EnsureStarted"));
			sm.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("_Parse", ss), C.VarRef("context"))));
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
			var syms = cfg.FillSymbols();
			var node = C.ArgRef("node");
			for(int ic=doc.Productions.Count,i=0;i<ic;++i)
			{
				var prod = doc.Productions[i];
				var isStart = ReferenceEquals(doc.StartProduction, prod);
				if (null!=prod.Code)
				{
					MemberAttributes attrs; 
					if (isStart)
						attrs = MemberAttributes.Public | MemberAttributes.Static;
					else
						attrs = MemberAttributes.FamilyAndAssembly | MemberAttributes.Static;
					var m = C.Method(typeof(object), string.Concat("Evaluate", prod.Name), attrs, C.Param("ParseNode", "node"), C.Param(typeof(object), "state"));
					var cnst = consts[syms.IndexOf(prod.Name)];
					var fr = C.FieldRef(C.TypeRef("Parser"), cnst);
					var cnd = C.If(C.Eq(fr, C.PropRef(node, "SymbolId")));
					cnd.TrueStatements.AddRange(CD.SlangParser.ParseStatements(prod.Code, true));
					m.Statements.Add(cnd);
					m.Statements.Add(C.Throw(C.New(C.Type("SyntaxException"), C.Literal(string.Concat("Expecting ", prod.Name)), C.PropRef(node, "Line"), C.PropRef(node, "Column"), C.PropRef(node, "Position"))));
					parser.Members.Add(m);
					m = C.Method(m.ReturnType, m.Name, attrs, C.Param("ParseNode", "node"));
					m.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), m.Name, C.ArgRef("node"), C.Null)));
					parser.Members.Add(m);
					var hasReturn = false;
					V.Visit(cnd, (ctx) => {
						var r = ctx.Target as CodeMethodReturnStatement;
						if (null != r)
						{
							hasReturn = true;
							ctx.Cancel = true;
						}
					});
					if(!hasReturn)
					{
						cnd.TrueStatements.Add(C.Return(C.Null));
					}
				}
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
