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
		static bool _AddNamespace(CodeNamespace ns,string import)
		{
			var hasNS = false;

			foreach (CodeNamespaceImport nsi in ns.Imports)
			{
				if (0 == string.Compare(nsi.Namespace, import,StringComparison.InvariantCulture))
				{
					hasNS = true;
					break;
				}
			}
			if (!hasNS)
				ns.Imports.Add(new CodeNamespaceImport(import));
			return !hasNS;
		}
		public static CodeCompileUnit GenerateCompileUnit(XbnfDocument document, XbnfGenerationInfo genInfo,string name = null,string @namespace=null,bool fast=false)
		{
			var docs = new List<XbnfDocument>();
			docs.Add(document);
			var result = new CodeCompileUnit();
			var ns = new CodeNamespace();
			var tstart = 0;
			var stbl=XbnfConvert.GetMasterSymbolTable(genInfo.CfgMap, out tstart);
			var symtbl = new string[stbl.Count];
			var symmap = new ListDictionary<string, int>(); // preserve order
			for (var i = 0;i<symtbl.Length;i++)
			{
				symtbl[i] = stbl[i].Key;
				symmap.Add(stbl[i].Key, i);
			}
				
			if (!string.IsNullOrEmpty(@namespace))
				ns.Name = @namespace;
			result.ReferencedAssemblies.Add(typeof(HashSet<>).Assembly.GetName().FullName);
			result.ReferencedAssemblies.Add(typeof(CodeObject).Assembly.GetName().FullName);
			_AddNamespace(ns, "System");
			_AddNamespace(ns, "System.Text");
			_AddNamespace(ns, "System.Collections.Generic");
			
			result.Namespaces.Add(ns);
			foreach (var mapEntry in genInfo.CfgMap)
			{
				
				var cfg = mapEntry.Value;
				var doc = mapEntry.Key;
				string sname;
				sname = cfg.StartSymbol + "Parser";
				if (ReferenceEquals(mapEntry.Key,document))
				{
					if (null != name)
					{
						sname = name;
					}
				} 
				if (0 == cfg.Rules.Count)
					throw new ArgumentException("The CFG document contains no rules.", nameof(cfg));
				cfg.RebuildCache();
				
				var parser = Deslanged.Parser.Namespaces[Deslanged.Parser.Namespaces.Count - 1].Types[0];
				parser.CustomAttributes.Add(GeneratedCodeAttribute);
				parser.Comments.Clear();
				var dc = "Refer to " + doc.Filename;
				parser.Comments.AddRange(C.ToComments(string.Format("<summary>Parses the indicated grammar. {0}</summary>", dc.TrimEnd()), true));
				
				var syms = cfg.FillSymbols();
				var consts = new string[symtbl.Length];
				symmap.Keys.CopyTo(consts,0);
				for (var i = 0; i < consts.Length; i++)
				{
					if ('#' != consts[i][0])
					{
						var sym = consts[i];
						if (genInfo.GetCfgAttribute(sym, "nocode", false))
							continue;
						var s = _MakeSafeName(sym);
						s = _MakeUniqueMember(parser, s);
						if (syms.Contains(symtbl[i]) 
							// || ReferenceEquals(doc,document)
							)
						{
							var fld = C.Field(typeof(int), s, MemberAttributes.Public | MemberAttributes.Const, C.Literal(i));
							parser.Members.Add(fld);
						}
						consts[i] = s;
					}
					else if ("#ERROR" == consts[i])
					{
						consts[i] = "ErrorSymbol";
					}
					else if ("#EOS" == consts[i])
					{
						consts[i] = "EosSymbol";
					}
				}
				// add the user code blocks
				for (int ic = doc.Code.Count, i = 0; i < ic; ++i)
				{
					var code = doc.Code[i];
					if (null != code && !string.IsNullOrWhiteSpace(code.Value))
					{
						parser.Members.AddRange(CD.SlangParser.ParseMembers(code.Value, sname));
					}
				}
				CfgLL1ParseTable tbl = null;
				// we don't care about conflicts here because we can backtrack:
				cfg.TryToLL1ParseTable(out tbl);
				_BuildParserParseFunctions(document,parser, doc, cfg, consts, stbl, tbl, sname,symmap,genInfo);
				var hasEval = false;
				for (int ic = doc.Productions.Count, i = 0; i < ic; ++i)
				{
					if (null != doc.Productions[i].Action)
					{
						hasEval = true;
						break;
					}
				}
				
				if (hasEval)
					_BuildParserEvalFunctions(parser, doc, cfg, consts,symmap);
				ns.Types.Add(parser);

				if (!fast)
					V.Visit(result, (ctx) =>
					{
						var ctr = ctx.Target as CodeTypeReference;
						if (null != ctr)
						{
							if (0 == string.Compare("Parser", ctr.BaseType, StringComparison.InvariantCulture))
								ctr.BaseType = sname;
							else if (ctr.BaseType.StartsWith("Parsley."))
								ctr.BaseType = ctr.BaseType.Substring(8);
						}
					});
				parser.Name = sname;
			}
			
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
			XbnfDocument primaryDoc,
			CodeTypeDeclaration parser,
			XbnfDocument doc,
			CfgDocument cfg,
			string[] consts,
			IDictionary<string,XbnfDocument> symtbl,
			CfgLL1ParseTable parseTable,
			string codeclass,
			IDictionary<string,int> symmap,
			XbnfGenerationInfo genInfo
			)

		{
			// for error reporting
			var firstsNT = cfg.FillFirstNonTerminals();
			var context = C.ArgRef("context");
			var syms = cfg.FillSymbols();
			syms.Remove("#ERROR");
			syms.Remove("#EOS");
			var start = doc.StartProduction.Name;
			foreach(var row in parseTable)
			{
				var nocode = genInfo.GetCfgAttribute(row.Key, "nocode", false);
				if (nocode)
					continue;
				var isExtern = genInfo.ExternalsMap[doc].Contains(row.Key);
				if (isExtern)
					continue;
				var isStart = 0==string.Compare(row.Key ,start,StringComparison.InvariantCulture);
				var nt = row.Key;
				var rmap = _BuildRuleMap(parseTable, nt);
				var isShared = false;
				var pi = doc.Productions.IndexOf(nt);
				XbnfProduction prod = null;
				if (-1 < pi)
					prod = doc.Productions[pi];
				if (null!=prod)
				{
					var ai = prod.Attributes.IndexOf("shared");
					if (-1 < ai)
					{
						var o = prod.Attributes[ai].Value;
						if (o is bool && (bool)o)
							isShared = true;
					}
				}
				var parseNtImpl = C.Method(C.Type("ParseNode"), string.Concat("Parse", nt),  MemberAttributes.FamilyAndAssembly | MemberAttributes.Static, C.Param(C.Type("ParserContext"), "context"));
				parser.Members.Add(parseNtImpl);
				if (null != prod && prod.IsVirtual)
				{
					var body = CD.SlangParser.ParseStatements(prod.Body.Value, true);
					foreach (CodeCommentStatement comment in C.ToComments(prod.Name))
						parseNtImpl.Statements.Add(comment);

					parseNtImpl.Statements.AddRange(body);
				}
				else
				{
					parseNtImpl.Statements.Add(C.Var(typeof(int), "line__", C.PropRef(C.ArgRef("context"), "Line")));
					parseNtImpl.Statements.Add(C.Var(typeof(int), "column__", C.PropRef(C.ArgRef("context"), "Column")));
					parseNtImpl.Statements.Add(C.Var(typeof(long), "position__", C.PropRef(C.ArgRef("context"), "Position")));
					var l = C.VarRef("line__");
					var c = C.VarRef("column__");
					var p = C.VarRef("position__");
					foreach (var kvp in rmap)
					{

					foreach (CodeCommentStatement comment in C.ToComments(kvp.Key))
							parseNtImpl.Statements.Add(comment);
						
						if (null == prod || (!prod.IsVirtual && !prod.IsAbstract))
						{
							
							var rules = row.Value[kvp.Value.First()].Rules;
							// simple, non backtracking case
							#region Non-Backtracking
							if (1 == rules.Count)
							{
								var rule = rules[0];
								var stmts = new CodeStatementCollection();
								_BuildParseRule(primaryDoc,cfg, doc,prod,consts,symtbl, context, syms, nt, stmts, l, c, p, kvp, rule, null, codeclass,symmap,genInfo);
								parseNtImpl.Statements.AddRange(stmts);
							}
							#endregion Non-Backtracking
							// more complicated backtracking case
							#region Backtracking
							else
							{
								
								var pc = C.Var(C.Type("ParserContext"), "context2__");
								// sort the conflicting rules by grammar priority.
								/*rules.Sort((x,y) => {
									return cfg.Rules.IndexOf(x) - cfg.Rules.IndexOf(y);
								});*/
								var ac = C.Var(typeof(int), "advanceCount__", C.Zero);
								var cnd = C.If(_BuildIfRuleExprsCnd(genInfo.Document,prod, syms,symtbl,consts, context, kvp.Value, codeclass,symmap));
								cnd.TrueStatements.Add(pc);
								cnd.TrueStatements.Add(ac);
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
									cnd.TrueStatements.Add(C.Let(C.VarRef("context2__"), C.Invoke(context, "GetLookAhead")));
									cnd.TrueStatements.Add(C.Invoke(C.VarRef("context2__"), "EnsureStarted"));
									var stmts = new CodeStatementCollection();
									_BuildParseRule(primaryDoc,cfg, doc,prod, consts,symtbl, C.VarRef("context2__"), syms, nt, stmts, l, c, p, kvp, rule, context, codeclass,symmap,genInfo);
									// we use except. handing to process our alternatives so we don't 
									// need to double the code size
									var tcf = new CodeTryCatchFinallyStatement();
									cnd.TrueStatements.Add(new CodeCommentStatement(rule.ToString()));
									cnd.TrueStatements.Add(tcf);
									var cc = new CodeCatchClause("ex", C.Type("SyntaxException"));
									cc.Statements.Add(C.If(C.Gt(C.PropRef(C.VarRef("context2__"), "AdvanceCount"), C.VarRef("advanceCount__")),
										C.Let(C.VarRef(vex.Name), C.VarRef("ex")),
										C.Let(C.VarRef("advanceCount__"), C.PropRef(C.VarRef("context2__"), "AdvanceCount")
										)));
									tcf.CatchClauses.Add(cc);
									tcf.TryStatements.AddRange(stmts);
									var exps = new StringBuilder();
									_BuildErrorList(new List<string>(kvp.Value), exps);
									tcf.TryStatements.Add(C.Call(context, "Error", C.Literal(string.Concat("Expecting ", exps.ToString()))));
									tcf.FinallyStatements.Add(new CodeSnippetStatement()); // have to make sure it renders
								}
								if (hasEmpty)
								{
									var stmts = new CodeStatementCollection();
									_BuildParseRule(primaryDoc,cfg,doc, prod,consts, symtbl, context, syms, nt, stmts, l, c, p, kvp, lastRule, null, codeclass,symmap,genInfo);
									cnd.TrueStatements.AddRange(stmts);
								}
								cnd.TrueStatements.Add(C.Throw(C.VarRef(vex.Name)));
								parseNtImpl.Statements.Add(cnd);
							}
							#endregion Backtracking
						}
					}

				
					StringBuilder exp = new StringBuilder();
					_BuildErrorList(row,firstsNT, exp);
					var sx = C.New(C.Type("SyntaxException"), C.Literal(string.Concat("Expecting ", exp.ToString())), l, c, p);
					parseNtImpl.Statements.Add(C.Throw(sx));
					
				}
				if (null != prod)
				{
					if (null != prod.Where && null != prod.Where.Value)
					{
						var stmts = CD.SlangParser.ParseStatements(prod.Where.Value, true);
						var whereImpl = C.Method(typeof(bool), string.Concat("Where", nt), MemberAttributes.Static | MemberAttributes.FamilyAndAssembly, C.Param(C.Type("ParserContext"), "context"));
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
						var rs = new StringBuilder();
						foreach (var r in cfg.FillNonTerminalRules(prod.Name))
							rs.AppendLine(r.ToString());

						stmts.Add(C.Var(C.Type("ParserContext"), "context", C.New(C.Type("ParserContext"), C.ArgRef("tokenizer"))));
						stmts.Add(C.Call(C.VarRef("context"), "EnsureStarted"));
						stmts.Add(C.Var(C.Type("ParseNode"), "result", C.Invoke(C.TypeRef(codeclass), parseNtImpl.Name, C.VarRef("context"))));
						stmts.Add(C.If(C.Not(C.PropRef(C.VarRef("context"), "IsEnded")),
							C.Call(C.VarRef("context"), "Error", C.Literal("Unexpected remainder in input."))));
						stmts.Add(C.Return(C.VarRef("result")));
						var ctr = C.Type(typeof(IEnumerable<>));
						ctr.TypeArguments.Add(C.Type("Token"));

						if (0 == string.Compare(nt, cfg.StartSymbol, StringComparison.InvariantCulture))
						{
							
							var parse = C.Method(parseNtImpl.ReturnType, "Parse", MemberAttributes.Public | MemberAttributes.Static, C.Param(ctr,"tokenizer"));
							parse.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nParses a production of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"tokenizer\">The tokenizer to parse with</param><returns>A <see cref=\"ParseNode\" /> representing the parsed tokens</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
							parse.Statements.AddRange(stmts);
							parser.Members.Add(parse);
						}
						if(isShared)
						{
							var parse = C.Method(parseNtImpl.ReturnType, string.Concat("Parse",nt), MemberAttributes.Public | MemberAttributes.Static, C.Param(ctr, "tokenizer"));
							parse.Comments.AddRange(C.ToComments(string.Format("<summary>\r\nParses a production of the form:\r\n{0}\r\n</summary>\r\n<remarks>\r\nThe production rules are:\r\n{1}\r\n</remarks>\r\n<param name=\"tokenizer\">The tokenizer to parse with</param><returns>A <see cref=\"ParseNode\" /> representing the parsed tokens</returns>", prod.ToString("p").TrimEnd(), rs.ToString().TrimEnd()), true));
							parse.Statements.AddRange(stmts);
							parser.Members.Add(parse);
						}
					}
					
				}
			}
		}
		private static void _BuildErrorList(IList<string> terms,StringBuilder exp)
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
		private static void _BuildErrorList(KeyValuePair<string, IDictionary<string, CfgLL1ParseTableEntry>> row, IDictionary<string,ICollection<string>> firsts, StringBuilder exp)
		{
			var terms = new List<string>();
			var col = firsts[row.Key];
			foreach (var s in col)
			{
				if(!string.IsNullOrWhiteSpace(s))
					terms.Add(s);
			}

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
		static string _GetClassName(XbnfDocument primary,IDictionary<string,XbnfDocument> symtbl, string codeclass,string sym)
		{
			var cls = codeclass;
			var doc = symtbl[sym];
			if (!ReferenceEquals(doc, primary))
			{
				cls= doc.StartProduction.Name + "Parser";
			}
			return cls;
		}
		private static void _BuildParseRule(
			XbnfDocument primaryDoc,
			CfgDocument cfg,
			XbnfDocument doc,
			XbnfProduction prod,
			string[] consts,
			IDictionary<string,XbnfDocument> symtbl,
			CodeExpression context,
			IList<string> syms,
			string nt,
			CodeStatementCollection stmts,
			CodeVariableReferenceExpression l,
			CodeVariableReferenceExpression c,
			CodeVariableReferenceExpression p,
			KeyValuePair<string, ICollection<string>> rmapEntry,
			CfgRule rule,
			CodeExpression recover,
			string codeclass,
			IDictionary<string,int> symmap,
			XbnfGenerationInfo genInfo
			)
		{
			var collapsed =_BuildCollapsed(genInfo, rule);
			var cnd = C.If(_BuildIfRuleExprsCnd(primaryDoc, prod, syms,symtbl, consts,context, rmapEntry.Value,codeclass,symmap));
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
				var recStmts = new CodeStatementCollection();
				for (int ic = rule.Right.Count, i = 0; i < ic; ++i)
				{
					
					var right = rule.Right[i];
					
					var isNonTerminal = cfg.IsNonTerminal(right);
					if (isNonTerminal)
					{
						var cls = _GetClassName(primaryDoc, symtbl, codeclass, right);
						var pinv = C.Invoke(C.TypeRef(cls), string.Concat("Parse", right), context);
						if (0 == collapsed.Count)
							cnd.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(i)), pinv));
						else
						{
							if (collapsed.Contains(right))
								cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "AddRange", C.PropRef(pinv, "Children")));
							else
								cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "Add", pinv));
						}
						if (null != recover)
						{
							recStmts.Add(C.Call(C.TypeRef(cls), string.Concat("Parse", right), recover));
						}
					}
					else // terminal
					{
						var si = symmap[right];
						var d = symtbl[right];
						var cls = _GetClassName(primaryDoc, symtbl, codeclass, right);
						var fr = C.FieldRef(C.TypeRef(cls), consts[si]);
						var np = C.New(C.Type("ParseNode"), fr, C.Literal(right), C.FieldRef(context, "Value"), C.PropRef(context, "Line"), C.PropRef(context, "Column"), C.PropRef(context, "Position"));
						var ccs = C.If(C.NotEq(fr, C.FieldRef(context, "SymbolId")),
							C.Call(context, "Error", C.Literal(string.Concat("Expecting ", right))));
						cnd.TrueStatements.Add(ccs);

						if (!collapsed.Contains(right))
						{
							if (0 == collapsed.Count)
								cnd.TrueStatements.Add(C.Let(C.ArrIndexer(C.VarRef("children"), C.Literal(i)), np));
							else
								cnd.TrueStatements.Add(C.Call(C.VarRef("children"), "Add", np));
						}
						cnd.TrueStatements.Add(C.Call(context, "Advance"));
						if(null!=recover)
							recStmts.Add(C.Call(recover, "Advance"));
						
					}
				}
				var ccls = _GetClassName(primaryDoc, symtbl, codeclass, nt);
				var ffr = C.FieldRef(C.TypeRef(ccls), consts[symmap[nt]]);
				if (null != recover)
				{
					/*
					cnd.TrueStatements.Add(C.Var(typeof(int), "adv", C.Zero));
					cnd.TrueStatements.Add(C.While(C.Lt(C.VarRef("adv"), C.FieldRef(context, "AdvanceCount")),
						C.Call(recover, "Advance"),
						C.Let(C.VarRef("adv"), C.Add(C.VarRef("adv"), C.One))
						));
					*/
					cnd.TrueStatements.AddRange(recStmts);
				}
				if (0 == collapsed.Count)
					cnd.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.VarRef("children"), l, c, p)));
				else
					cnd.TrueStatements.Add(C.Return(C.New(C.Type("ParseNode"), ffr, C.Literal(nt), C.Invoke(C.VarRef("children"), "ToArray"), l, c, p)));
			}
			stmts.Add(cnd);
		}

		private static HashSet<string> _BuildCollapsed(XbnfGenerationInfo genInfo, CfgRule rule)
		{
			var result = new HashSet<string>();
			foreach (var cfg in genInfo.CfgMap.Values)
			{
				for (int jc = rule.Right.Count, j = 0; j < jc; ++j)
				{
					var o = cfg.GetAttribute(rule.Right[j], "collapsed");
					if (o is bool && (bool)o)
						result.Add(rule.Right[j]);
				}
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
			string[] consts,
			IDictionary<string,int> symmap

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
					var cnst = consts[symmap[prod.Name]];
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
		private static CodeExpression _BuildIfRuleExprsCnd(XbnfDocument doc,XbnfProduction prod,IList<string> syms,IDictionary<string,XbnfDocument> symtbl, string[] consts,CodeExpression context, IEnumerable<string> cmps,string codeclass,IDictionary<string,int> symmap)
		{
			var exprs = new CodeExpressionCollection();
			foreach (var s in cmps)
			{
				if (null !=s)
				{
					var si = symmap[s];
					var cls = _GetClassName(doc, symtbl, codeclass, s);
					var fr = C.FieldRef(C.TypeRef(cls), (-1 < si) ? consts[si]:"EosSymbol");
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
				var cls = _GetClassName(doc, symtbl, codeclass, prod.Name);
				var inv = C.Invoke(C.TypeRef(cls), string.Concat("Where", prod.Name), C.Invoke(context, "GetLookAhead", C.True));
				result = C.And(result, inv);
			}
			return result;
		}
		private static CodeExpression _BuildIfRuleExprsCnd(XbnfDocument doc,XbnfProduction prod,IList<string> syms,IDictionary<string,XbnfDocument> symtbl, string[] consts, CodeExpression context, KeyValuePair<CfgRule, ICollection<string>> r,string codeclass,IDictionary<string,int> symmap)
		{
			var cmps = new List<string>();
			foreach (var s in r.Value)
				if (null != s)
					cmps.Add(s);
			return _BuildIfRuleExprsCnd(doc,prod,syms,  symtbl,consts,context, cmps,codeclass,symmap);
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
							(0 == string.Compare("IList`1", ctr.BaseType, StringComparison.InvariantCulture)) ||
							(0 == string.Compare("List`1", ctr.BaseType, StringComparison.InvariantCulture)) ||
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
