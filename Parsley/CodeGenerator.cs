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
			var laTypes = Deslanged.LookAheadEnumerator.Namespaces[Deslanged.LookAheadEnumerator.Namespaces.Count - 1].Types;
			var lookAheadEnumerator = C.GetByName("LookAheadEnumerator",laTypes);
			var lookAheadEnumeratorEnumerable = C.GetByName("LookAheadEnumeratorEnumerable", laTypes);
			var lookAheadEnumeratorEnumerator = C.GetByName("LookAheadEnumeratorEnumerator", laTypes);
			lookAheadEnumerator.CustomAttributes.Add(GeneratedCodeAttribute);
			lookAheadEnumeratorEnumerable.CustomAttributes.Add(GeneratedCodeAttribute);
			lookAheadEnumeratorEnumerator.CustomAttributes.Add(GeneratedCodeAttribute);
			_FixParserContext(parserContext);
			ns.Types.Add(syntaxException);
			ns.Types.Add(parseNode);
			ns.Types.Add(parserContext);
			ns.Types.Add(lookAheadEnumerator);
			ns.Types.Add(lookAheadEnumeratorEnumerable);
			ns.Types.Add(lookAheadEnumeratorEnumerator);
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
			result.ReferencedAssemblies.Add(typeof(HashSet<>).Assembly.GetName().FullName);
			result.ReferencedAssemblies.Add(typeof(CodeObject).Assembly.GetName().FullName);
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
			// add the user code blocks
			for(int ic = doc.Code.Count,i=0;i<ic;++i)
			{
				var code = doc.Code[i];
				if(null!=code&&!string.IsNullOrWhiteSpace(code.Value))
				{
					parser.Members.AddRange(CD.SlangParser.ParseMembers(code.Value, "Parser"));
				}
			}
			CfgLL1ParseTable tbl = null;
			// we don't care about conflicts here because we can backtrack:
			cfg.TryToLL1ParseTable(out tbl);
			_BuildParserParseFunctions(parser, doc, cfg,consts,tbl);
			var hasEval = false;
			for (int ic=doc.Productions.Count,i=0;i<ic;++i)
			{
				if(null!=doc.Productions[i].Action)
				{
					hasEval = true;
					break;
				}
			}
			//
			// FOR TESTING:			
			// hasEval = false;
			//
			//
			
			if (hasEval)
				_BuildParserEvalFunctions(parser, doc, cfg,consts);
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
			CfgLL1ParseTable parseTable
			)

		{
			var context = C.ArgRef("context");
			var syms = cfg.FillSymbols();
			syms.Remove("#ERROR");
			syms.Remove("#EOS");
			foreach(var row in parseTable)
			{
				var nt = row.Key;
				var rmap = _BuildRuleMap(parseTable, nt);
				var parseNtImpl = C.Method(C.Type("ParseNode"), string.Concat("Parse", nt), MemberAttributes.Private | MemberAttributes.Static, C.Param(C.Type("ParserContext"), "context"));
				parser.Members.Add(parseNtImpl);
				parseNtImpl.Statements.Add(C.Var(typeof(int), "line", C.PropRef(C.ArgRef("context"), "Line")));
				parseNtImpl.Statements.Add(C.Var(typeof(int), "column", C.PropRef(C.ArgRef("context"), "Column")));
				parseNtImpl.Statements.Add(C.Var(typeof(long), "position", C.PropRef(C.ArgRef("context"), "Position")));
				var l = C.VarRef("line");
				var c = C.VarRef("column");
				var p = C.VarRef("position");
				var pi = doc.Productions.IndexOf(nt);
				XbnfProduction prod = null;
				if (-1 < pi)
					prod = doc.Productions[pi];
				foreach (var kvp in rmap)
				{
					foreach (CodeCommentStatement comment in C.ToComments(kvp.Key))
						parseNtImpl.Statements.Add(comment);
					var rules = row.Value[kvp.Value.First()].Rules;
					// simple, non backtracking case
					#region Non-Backtracking
					if (1 == rules.Count)
					{
						var rule = rules[0];
						//if ("Leaf" == rules[0].Left)
						//	System.Diagnostics.Debugger.Break();
						var stmts = new CodeStatementCollection();
						_BuildParseRule(cfg, prod,consts, context, syms, nt, stmts, l, c, p, kvp, rule, null);
						parseNtImpl.Statements.AddRange(stmts);
					}
					#endregion Non-Backtracking
					// more complicated backtracking case
					#region Backtracking
					else
					{
						var vd = C.Var(C.Type("ParserContext"), "pc2");
						// sort the conflicting rules by grammar priority.
						/*rules.Sort((x,y) => {
							return cfg.Rules.IndexOf(x) - cfg.Rules.IndexOf(y);
						});*/

						var cnd = C.If(_BuildIfRuleExprsCnd(prod,syms, consts, context, kvp.Value));
						cnd.TrueStatements.Add(vd);
						var collapsed = new HashSet<string>();
						var r = rules[0];
						for (int jc = r.Right.Count, j = 0; j < jc; ++j)
						{
							var o = cfg.GetAttribute(r.Right[j], "collapsed");
							if (o is bool && (bool)o)
								collapsed.Add(r.Right[j]);
						}
						var vex = C.Var(typeof(Exception), "lastExcept", C.Null);
						cnd.TrueStatements.Add(vex);
						// evaluate the rules in order, excepting the 
						// empty rule (epsilon) which we always process 
						// last
						var hasEmpty = false;
						CfgRule lastRule = null;
						foreach (var rule in rules)
						{
							if (0 == rule.Right.Count)
							{
								lastRule = rule;
								hasEmpty = true;
								continue;
							}
							cnd.TrueStatements.Add(C.Let(C.VarRef("pc2"), C.Invoke(context, "GetLookAhead")));
							cnd.TrueStatements.Add(C.Invoke(C.VarRef("pc2"), "EnsureStarted"));
							var stmts = new CodeStatementCollection();
							_BuildParseRule(cfg,prod, consts, C.VarRef("pc2"), syms, nt, stmts, l, c, p, kvp, rule, context);
							// we use except. handing to process our alternatives so we don't 
							// need to double the code size
							var tcf = new CodeTryCatchFinallyStatement();
							cnd.TrueStatements.Add(new CodeCommentStatement(rule.ToString()));
							cnd.TrueStatements.Add(tcf);
							var cc = new CodeCatchClause("ex", C.Type("SyntaxException"));
							cc.Statements.Add(C.If(C.IdentEq(C.VarRef(vex.Name), C.Null),
								C.Let(C.VarRef(vex.Name), C.VarRef("ex")))
								);
							tcf.CatchClauses.Add(cc);
							tcf.TryStatements.AddRange(stmts);
							var exps = new StringBuilder();
							_BuildErrorList(new List<string>(kvp.Value),exps);
							tcf.TryStatements.Add(C.Call(context, "Error", C.Literal(string.Concat("Expecting ", exps.ToString()))));
							tcf.FinallyStatements.Add(new CodeSnippetStatement()); // have to make sure it renders
						}
						if (hasEmpty)
						{
							var stmts = new CodeStatementCollection();
							_BuildParseRule(cfg,prod, consts, context, syms, nt, stmts, l, c, p, kvp, lastRule, null);
							cnd.TrueStatements.AddRange(stmts);
						}
						cnd.TrueStatements.Add(C.Throw(C.VarRef(vex.Name)));
						parseNtImpl.Statements.Add(cnd);
					}
					#endregion Backtracking
				}
				StringBuilder exp = new StringBuilder();
				_BuildErrorList(row,exp);
				parseNtImpl.Statements.Add(C.Call(context, "Error", C.Literal(string.Concat("Expecting ", exp.ToString()," at line {0}, column {1}, position {2}")),l,c,p));
				parseNtImpl.Statements.Add(C.Return(C.Null));
				if (null != prod)
				{
					if (null != prod.Where && null != prod.Where.Value)
					{
						var stmts = CD.SlangParser.ParseStatements(prod.Where.Value, true);
						var whereImpl = C.Method(typeof(bool), string.Concat("_Where", nt), MemberAttributes.Static | MemberAttributes.Private, C.Param(C.Type("ParserContext"), "context"));
						whereImpl.Statements.AddRange(stmts);
						var hasReturn = false;
						V.Visit(whereImpl, (ctx) =>
						{
							var r = ctx.Target as CodeMethodReturnStatement;
							if (null != r)
							{
								hasReturn = true;
								ctx.Cancel = true;
							}
						});
						if (!hasReturn)
						{
							whereImpl.Statements.Add(C.Return(C.True));
						}
						parser.Members.Add(whereImpl);
					}
					if (!prod.IsCollapsed)
					{

						var stmts = new CodeStatementCollection();
						stmts.Add(C.Var(C.Type("ParserContext"), "context", C.New(C.Type("ParserContext"), C.ArgRef("tokenizer"))));
						stmts.Add(C.Call(C.VarRef("context"), "EnsureStarted"));
						stmts.Add(C.Return(C.Invoke(C.TypeRef("Parser"), parseNtImpl.Name, C.VarRef("context"))));
						var ctr = C.Type(typeof(IEnumerable<>));
						ctr.TypeArguments.Add(C.Type("Token"));
						var parseNt = C.Method(parseNtImpl.ReturnType, parseNtImpl.Name, MemberAttributes.Public | MemberAttributes.Static, C.Param(ctr, "tokenizer"));
						var rs = new StringBuilder();
						foreach (var r in cfg.FillNonTerminalRules(prod.Name))
							rs.AppendLine(r.ToString());
						parseNt.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nParses a production of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"tokenizer\">The tokenizer to parse with</param><returns>A <see cref=\"ParseNode\" /> representing the parsed tokens</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
						parseNt.Statements.AddRange(stmts);
						parser.Members.Add(parseNt);
						if (0 == string.Compare(nt, cfg.StartSymbol, StringComparison.InvariantCulture))
						{
							var parse = C.Method(parseNt.ReturnType, "Parse", MemberAttributes.Public | MemberAttributes.Static, (CodeParameterDeclarationExpression[])parseNt.Parameters.ToArray(typeof(CodeParameterDeclarationExpression)));
							parse.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nParses a production of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"tokenizer\">The tokenizer to parse with</param><returns>A <see cref=\"ParseNode\" /> representing the parsed tokens</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
							parse.Statements.AddRange(stmts);
							parser.Members.Add(parse);
						}
					}
					
				}
			}
		}
		private static void _BuildErrorList(IList<string> terms, StringBuilder exp)
		{
			
			exp.Append(terms[0]);
			int ic = terms.Count;
			if (2 < ic)
			{
				for (var i = 1; i < ic - 1; ++i)
					exp.Append(string.Concat(", ", terms[i]));
				exp.Append(",");
			}
			if (1 < ic)
				exp.Append(string.Concat(" or ", terms[ic - 1]));
		}
		private static void _BuildErrorList(KeyValuePair<string, IDictionary<string, CfgLL1ParseTableEntry>> row, StringBuilder exp)
		{
			var terms = new List<string>();
			foreach (var col in row.Value)
				if (null != col.Value.Rules && 0 < col.Value.Rules.Count)
					terms.Add(col.Key);

			exp.Append(terms[0]);
			int ic = terms.Count;
			if (2 < ic)
			{
				for (var i = 1; i < ic - 1; ++i)
					exp.Append(string.Concat(", ", terms[i]));
				exp.Append(",");
			}
			if (1 < ic)
				exp.Append(string.Concat(" or ", terms[ic - 1]));
		}
		private static void _BuildParseRule(
			CfgDocument cfg,
			XbnfProduction prod,
			string[] consts,
			CodeExpression context,
			IList<string> syms,
			string nt,
			CodeStatementCollection stmts,
			CodeVariableReferenceExpression l,
			CodeVariableReferenceExpression c,
			CodeVariableReferenceExpression p,
			KeyValuePair<string, ICollection<string>> rmapEntry,
			CfgRule rule,
			CodeExpression recover)
		{
			var collapsed =_BuildCollapsed(cfg, rule);
			var cnd = C.If(_BuildIfRuleExprsCnd(prod, syms, consts, context, rmapEntry.Value));
			if (null!=prod && prod.IsVirtual)
			{
				cnd.TrueStatements.AddRange(CD.SlangParser.ParseStatements(prod.Body.Value, true));
			}
			else
			{
				#region children declaration
				if (0 == collapsed.Count)
					cnd.TrueStatements.Add(C.Var(C.Type("ParseNode", 1), "children", C.NewArr("ParseNode", rule.Right.Count)));
				else
				{
					var lt = C.Type(typeof(List<>));
					lt.TypeArguments.Add(C.Type("ParseNode"));
					cnd.TrueStatements.Add(C.Var(lt, "children", C.New(lt)));
				}
				#endregion children declaration

				for (int ic = rule.Right.Count, i = 0; i < ic; ++i)
				{
					var right = rule.Right[i];
					var isNonTerminal = cfg.IsNonTerminal(right);
					if (isNonTerminal)
					{
						var pinv = C.Invoke(C.TypeRef("Parser"), string.Concat("Parse", right), context);
						if (0 == collapsed.Count)
							cnd.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(i)), pinv));
						else
						{
							if (collapsed.Contains(right))
								cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "AddRange", C.PropRef(pinv, "Children")));
							else
								cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "Add", pinv));
						}
					}
					else // terminal
					{
						var si = syms.IndexOf(right);
						var fr = C.FieldRef(C.TypeRef("Parser"), consts[si]);
						var np = C.New(C.Type("ParseNode"), fr, C.Literal(right), C.FieldRef(context, "Value"), C.PropRef(context, "Line"), C.PropRef(context, "Column"), C.PropRef(context, "Position"));
						var ccs = C.If(C.NotEq(fr, C.FieldRef(context, "SymbolId")),
							C.Call(context, "Error", C.Literal(string.Concat("Expecting ", right, " at line {0}, column {1}, position {2}")), C.PropRef(context, "Line"), C.PropRef(context, "Column"), C.PropRef(context, "Position")));
						cnd.TrueStatements.Add(ccs);

						if (!collapsed.Contains(right))
						{
							if (0 == collapsed.Count)
								cnd.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(i)), np));
							else
								cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "Add", np));
						}
						cnd.TrueStatements.Add(C.Call(context, "Advance"));
					}
				}
				var ffr = C.FieldRef(C.TypeRef("Parser"), consts[syms.IndexOf(nt)]);
				if (null != recover)
				{
					cnd.TrueStatements.Add(C.Var(typeof(int), "adv", C.Zero));
					cnd.TrueStatements.Add(C.While(C.Lt(C.VarRef("adv"), C.FieldRef(context, "AdvanceCount")),
						C.Call(recover, "Advance"),
						C.Let(C.VarRef("adv"), C.Add(C.VarRef("adv"), C.One))
						));

				}
				if (0 == collapsed.Count)
					cnd.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.VarRef("children"), l, c, p)));
				else
					cnd.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.Invoke(C.VarRef("children"), "ToArray"), l, c, p)));
			}
			stmts.Add(cnd);
		}

		private static void _BuildParseRule2(CfgDocument cfg, XbnfProduction prod, string[] consts, CodeExpression context, IList<string> syms, string nt, CodeStatementCollection stmts, CodeVariableReferenceExpression l, CodeVariableReferenceExpression c, CodeVariableReferenceExpression p, KeyValuePair<string, ICollection<string>> rmapEntry, CfgRule rule,CodeExpression recover)
		{
			var cnd = C.If(_BuildIfRuleExprsCnd(prod,syms, consts, context, rmapEntry.Value));
			var collapsed = _BuildCollapsed(cfg, rule);
			if (0 == collapsed.Count)
				cnd.TrueStatements.Add(C.Var(C.Type("ParseNode", 1), "children", C.NewArr("ParseNode", rule.Right.Count)));
			else
			{
				var lt = C.Type(typeof(List<>));
				lt.TypeArguments.Add(C.Type("ParseNode"));
				cnd.TrueStatements.Add(C.Var(lt, "children", C.New(lt)));
			}
			for (int jc = rule.Right.Count, j = 0; j < jc; ++j)
			{
				var s = rule.Right[j];
				if (cfg.IsNonTerminal(s))
				{
					var pinv = C.Invoke(C.TypeRef("Parser"), string.Concat("Parse", s), context);
					if (0 == collapsed.Count)
						cnd.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(j)), pinv));
					else
					{
						if (collapsed.Contains(s))
						{
							cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "AddRange", C.PropRef(pinv, "Children")));
						}
						else
							cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "Add", pinv));
					}
				}
				else // s is terminal
				{
					var cnd2 = cnd;
					var ts = s;
					var subs = cfg.GetAttribute(s, "substitute") as string;
					if (!string.IsNullOrEmpty(subs))
					{
						ts = subs;
					}
					var si = syms.IndexOf(ts);
					var fr = C.FieldRef(C.TypeRef("Parser"), consts[si]);
					var np = C.New(C.Type("ParseNode"), fr, C.Literal(ts), C.FieldRef(context, "Value"), l, c, p);
					if (0 != j)
					{
						var ccs = C.If(C.Eq(fr, C.FieldRef(context, "SymbolId")));
						cnd2.TrueStatements.Add(ccs);
						cnd2 = ccs;
					}
					if (!collapsed.Contains(ts))
					{
						if (0 == collapsed.Count)
							cnd2.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(j)), np));
						else
							cnd2.TrueStatements.Add(C.Call(C.VarRef("children"), "Add", np));
					}
					
					cnd2.TrueStatements.Add(C.Call(context, "Advance"));

				}
			}
			var subst = cfg.GetAttribute(nt, "substitute") as string;
			if (!string.IsNullOrEmpty(subst))
			{
				nt = subst;
			}
			var ffr = C.FieldRef(C.TypeRef("Parser"), consts[syms.IndexOf(nt)]);
			if(null!=recover)
			{
				cnd.TrueStatements.Add(C.Var(typeof(int), "adv", C.Zero));
				cnd.TrueStatements.Add(C.While(C.Lt(C.VarRef("adv"), C.FieldRef(context, "AdvanceCount")),
					C.Call(recover,"Advance"),
					C.Let(C.VarRef("adv"),C.Add(C.VarRef("adv"),C.One))
					));

			}
			if (0 == collapsed.Count)
				cnd.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.VarRef("children"), l, c, p)));
			else
				cnd.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.Invoke(C.VarRef("children"), "ToArray"), l, c, p)));
			stmts.Add(cnd);
			
		}
		private static HashSet<string> _BuildCollapsed(CfgDocument cfg, CfgRule rule)
		{
			var result = new HashSet<string>();
			for (int jc = rule.Right.Count, j = 0; j < jc; ++j)
			{
				var o = cfg.GetAttribute(rule.Right[j], "collapsed");
				if (o is bool && (bool)o)
					result.Add(rule.Right[j]);
			}
			return result;
		}
		static IDictionary<string,ICollection<string>> _BuildRuleMap(CfgLL1ParseTable parseTable,string nt)
		{
			var result = new Dictionary<string, ICollection<string>>(StringComparer.InvariantCulture);
			var row = parseTable[nt];
			foreach(var col in row)
			{
				ICollection<string> terms;
				var strs = new StringBuilder();
				if (null != col.Value.Rules && 0 < col.Value.Rules.Count)
				{
					foreach (var r in col.Value.Rules)
						strs.AppendLine(r.ToString());
					if (!result.TryGetValue(strs.ToString(), out terms))
					{
						terms = new HashSet<string>();
						result.Add(strs.ToString(), terms);
					}

					terms.Add(col.Key);
				}
			}

			return result;
		}
		
		static void _BuildParserEvalFunctions(
			CodeTypeDeclaration parser,
			XbnfDocument doc,
			CfgDocument cfg,
			string[] consts
			)
		{
			var hasChangeType = false;
			var hasEvalAny = false;
			var syms = cfg.FillSymbols();
			var node = C.ArgRef("node");
			for(int ic=doc.Productions.Count,i=0;i<ic;++i)
			{
				var prod = doc.Productions[i];
				var isStart = ReferenceEquals(doc.StartProduction, prod);
				if (null!=prod.Action && null!=prod.Action.Value)
				{
					var type = new CodeTypeReference(typeof(object));
					
					var ti = prod.Attributes.IndexOf("type");
					if (-1<ti)
					{
						var s = prod.Attributes[ti].Value as string;
						if (!string.IsNullOrEmpty(s))
							type = new CodeTypeReference(CD.CodeDomResolver.TranslateIntrinsicType(s));

					}

					MemberAttributes attrs; 
					attrs = MemberAttributes.Public| MemberAttributes.Static;
					var rs = new StringBuilder();
					foreach (var r in cfg.FillNonTerminalRules(prod.Name))
						rs.AppendLine(r.ToString());

					if (isStart)
					{
						var ms = C.Method(type, "Evaluate", attrs, C.Param(C.Type("ParseNode"), "node"));
						//if (0 != prod.Line && !string.IsNullOrEmpty(doc.Filename))
						//	ms.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
						ms.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
						ms.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("Evaluate", prod.Name), C.ArgRef("node"))));
						parser.Members.Add(ms);
						ms = C.Method(type, "Evaluate", attrs, C.Param(C.Type("ParseNode"), "node"),C.Param(C.Type(typeof(object)),"state"));
						//if (0 != prod.Line && !string.IsNullOrEmpty(doc.Filename))
						//	ms.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
						ms.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<param name=\"state\">A user supplied state object. What it should be depends on the production's associated code block</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
						ms.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("Evaluate", prod.Name), C.ArgRef("node"),C.ArgRef("state"))));
						parser.Members.Add(ms);
					}
					var m = C.Method(type, string.Concat("Evaluate", prod.Name), attrs, C.Param("ParseNode", "node"), C.Param(typeof(object), "state"));
					//if (0 != prod.Line)
					//	m.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
					m.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<param name=\"state\">A user supplied state object. What it should be depends on the production's associated code block</param>\r\n<returns>The result of the evaluation</returns>",prod.ToString("p").TrimEnd(),rs.ToString().TrimEnd()), true));
					var cnst = consts[syms.IndexOf(prod.Name)];
					var fr = C.FieldRef(C.TypeRef("Parser"), cnst);
					var cnd = C.If(C.Eq(fr, C.PropRef(node, "SymbolId")));
					var stmts = CD.SlangParser.ParseStatements(prod.Action.Value, true);
					cnd.TrueStatements.AddRange(stmts);
					m.Statements.Add(cnd);
					m.Statements.Add(C.Throw(C.New(C.Type("SyntaxException"), C.Literal(string.Concat("Expecting ", prod.Name)), C.PropRef(node, "Line"), C.PropRef(node, "Column"), C.PropRef(node, "Position"))));
					parser.Members.Add(m);
					m = C.Method(m.ReturnType, m.Name, attrs, C.Param("ParseNode", "node"));
					//if (0 != prod.Line && !string.IsNullOrEmpty(doc.Filename))
					//	m.LinePragma = new CodeLinePragma(doc.Filename, prod.Line);
					m.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nEvaluates a derivation of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"node\">The <see cref=\"ParseNode\"/> to evaluate</param>\r\n<returns>The result of the evaluation</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
					m.Statements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), m.Name, C.ArgRef("node"), C.Null)));
					parser.Members.Add(m);
					var hasReturn = false;
					V.Visit(cnd, (ctx) => {
						var idx = ctx.Target as CodeIndexerExpression;
						if(null!=idx && 1==idx.Indices.Count)
						{
							var to = idx.TargetObject as CodeVariableReferenceExpression;
							if(null!=to)
							{
								int pi;
								if(0==string.Compare("Child",to.VariableName,StringComparison.InvariantCulture))
								{
									// is a thing like Child[0]
									hasEvalAny = true;
									var mi = C.Invoke(C.TypeRef("Parser"), "_EvaluateAny", C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), C.ArgRef("state"));
									V.ReplaceTarget(ctx, mi);
								} else if(-1<(pi=doc.Productions.IndexOf(to.VariableName)))
								{
									// is a thing like Factor[0]
									var p = doc.Productions[pi];
									if(!p.IsCollapsed && !p.IsHidden)
									{
										if (!p.IsTerminal)
										{
											var mi = C.Invoke(C.TypeRef("Parser"), string.Concat("Evaluate", p.Name), C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), C.ArgRef("state"));
											V.ReplaceTarget(ctx, mi);
										}
										else
										{
											var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), "Value");
											V.ReplaceTarget(ctx, pr);
										}
									}
								} else if(0==string.Compare("SymbolId",to.VariableName))
								{
									var pr = C.PropRef(C.ArrIndexer(C.PropRef(node, "Children"), idx.Indices[0]), "SymbolId");
									V.ReplaceTarget(ctx, pr);
								}
							}
						}
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
								else if (v.VariableName.StartsWith("Child", StringComparison.InvariantCulture))
								{
									if (5 < v.VariableName.Length)
									{
										var s = v.VariableName.Substring(5);
										int num;
										if (int.TryParse(s, out num))
										{
											if (0 < num)
											{
												hasEvalAny = true;
												var mi = C.Invoke(C.TypeRef("Parser"), "_EvaluateAny", C.ArrIndexer(C.PropRef(node, "Children"), C.Literal(num - 1)), C.ArgRef("state"));
												V.ReplaceTarget(ctx, mi);
												
											}
										}
									}
								}
								else if(0==string.Compare("Length",v.VariableName,StringComparison.InvariantCulture))
								{
									var ffr = C.PropRef(C.PropRef(node, "Children"), "Length");
									V.ReplaceTarget(ctx, ffr);
								}
								else
								{
									if (v.VariableName.StartsWith("SymbolId",StringComparison.InvariantCulture))
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
			if(hasEvalAny)
			{
				var sid = C.PropRef(node, "SymbolId");
				var m = C.Method(typeof(object), "_EvaluateAny", MemberAttributes.Private | MemberAttributes.Static, C.Param(C.Type("ParseNode"), "node"), C.Param(typeof(object), "state"));
				for(int ic=doc.Productions.Count,i=0;i<ic;++i)
				{
					var p = doc.Productions[i];
					if(!p.IsCollapsed && !p.IsHidden)
					{
						
						var sidcmp = cfg.GetIdOfSymbol(p.Name);
						var sidcf = C.FieldRef(C.TypeRef("Parser"),consts[sidcmp]);
						var cnd = C.If(C.Eq(sid, sidcf));
						if (!p.IsTerminal)
							cnd.TrueStatements.Add(C.Return(C.Invoke(C.TypeRef("Parser"), string.Concat("Evaluate", p.Name), node, C.ArgRef("state"))));
						else
							cnd.TrueStatements.Add(C.Return(C.PropRef(node,"Value")));
						m.Statements.Add(cnd);
					}
				}
				m.Statements.Add(C.Return(C.Null));
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
		private static CodeExpression _BuildIfRuleExprsCnd(XbnfProduction prod,IList<string> syms, string[] consts, CodeExpression context, IEnumerable<string> cmps)
		{
			var exprs = new CodeExpressionCollection();
			foreach (var s in cmps)
			{
				if (null !=s)
				{
					var si = syms.IndexOf(s);
					
					var fr = C.FieldRef(C.TypeRef("Parser"), (-1 < si) ? consts[si]:"EosSymbol");
					exprs.Add(C.Eq(fr, C.PropRef(context, "SymbolId")));
				}
			}
			CodeExpression result = null;
			switch (exprs.Count)
			{
				case 0:
					return null;
				case 1:
					result = exprs[0];
					break;
				default:
					result =  C.BinOp(exprs, CodeBinaryOperatorType.BooleanOr);
					break;
			}
			if(null!=prod && null!=prod.Where)
			{
				// add the semantic constraint
				var inv = C.Invoke(C.TypeRef("Parser"), string.Concat("_Where", prod.Name), C.Invoke(context, "GetLookAhead", C.True));
				result = C.And(result, inv);
			}
			return result;
		}
		private static CodeExpression _BuildIfRuleExprsCnd(XbnfProduction prod,IList<string> syms, string[] consts, CodeExpression context, KeyValuePair<CfgRule, ICollection<string>> r)
		{
			var cmps = new List<string>();
			foreach (var s in r.Value)
				if (null != s)
					cmps.Add(s);
			return _BuildIfRuleExprsCnd(prod,syms, consts, context, cmps);
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
							(0 == string.Compare("IEnumerator`1", ctr.BaseType, StringComparison.InvariantCulture)) ||
							(0==string.Compare("Parsley.LookAheadEnumerator`1",ctr.BaseType,StringComparison.InvariantCulture)) ||
							(0 == string.Compare("LookAheadEnumerator`1", ctr.BaseType, StringComparison.InvariantCulture)))
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
