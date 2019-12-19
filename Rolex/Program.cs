//
// Rolex .2b - The Gold Plated Lexer Generator (again!)
//
// Much faster than .2a thanks to Deslang, both much 
// more maintainable than 1 thanks to Slang
//
// Copyright (c) 2019, honey the codewitch
// MIT license

using CD;
using RE;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
namespace Rolex
{
	class Program
	{
		static string _GetName() {
			foreach(var attr in Assembly.GetEntryAssembly().CustomAttributes)
			{
				if(typeof(AssemblyTitleAttribute)==attr.AttributeType)
				{
					return attr.ConstructorArguments[0].Value as string;
				}
			}
			return Path.GetFileNameWithoutExtension(_File);
		}
		static readonly string _File = Assembly.GetEntryAssembly().GetModules()[0].Name;
		static readonly string _Name = _GetName();
		static int Main(string[] args)
		{
			// our return code
			var result = 0;
			// app parameters
			string inputfile = null;
			string outputfile = null;
			string name = null;
			string codelanguage = null;
			string codenamespace = null;
			bool compiled = false;
			bool noshared = false;
			bool ifstale = false;
			// our working variables
			TextReader input = null;
			TextWriter output = null;
			try
			{
				if (0 == args.Length)
				{
					_PrintUsage();
					result = -1;
				}
				else if(args[0].StartsWith("/"))
				{
					throw new ArgumentException("Missing input file.");
				} else
				{
					// process the command line args
					inputfile = args[0];
					for(var i = 1;i<args.Length;++i)
					{
						switch(args[i])
						{
							case "/output":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								outputfile = args[i];
								break;
							case "/name":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								name = args[i];
								break;
							case "/language":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								codelanguage = args[i];
								break;
							case "/namespace":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								codenamespace = args[i];
								break;
							case "/noshared":
								noshared = true;
								break;
							case "/compiled":
								compiled = true;
								break;
							case "/ifstale":
								ifstale= true;
								break;
							default:
								throw new ArgumentException(string.Format("Unknown switch {0}", args[i]));
						}
					}
					// now build it
					if (string.IsNullOrEmpty(name))
					{
						// default we want it to be named after the code file
						// otherwise we'll use inputfile
						if (null != outputfile)
							name = Path.GetFileNameWithoutExtension(outputfile);
						else
							name = Path.GetFileNameWithoutExtension(inputfile);
					}
					if (string.IsNullOrEmpty(codelanguage))
					{
						if (!string.IsNullOrEmpty(outputfile))
						{
							codelanguage = Path.GetExtension(outputfile);
							if (codelanguage.StartsWith("."))
								codelanguage = codelanguage.Substring(1);
						}
						if (string.IsNullOrEmpty(codelanguage))
							codelanguage = "cs";
					}
					var stale = true;
					if (ifstale && null!=outputfile)
					{
						// File.Exists doesn't always work right
						try
						{
							if (File.GetLastWriteTimeUtc(outputfile) >= File.GetLastWriteTimeUtc(inputfile))
								stale = false;
						}
						catch { }
					}
					if (!stale)
					{
						Console.Error.WriteLine("{0} skipped building {1} because it was not stale.",_Name, outputfile);
					} else
					{
						if (null != outputfile)
							Console.Error.Write("{0} is building {1}.", _Name, outputfile);
						else
							Console.Error.Write("{0} is building tokenizer.", _Name);
						input = new StreamReader(inputfile);
						var rules = _ParseRules(input);
						input.Close();
						input = null;
						_FillRuleIds(rules);
						
						var ccu = new CodeCompileUnit();
						var cns = new CodeNamespace();
						if (!string.IsNullOrEmpty(codenamespace))
							cns.Name = codenamespace;
						ccu.Namespaces.Add(cns);
						var fa = _BuildLexer(rules);
						var symbolTable = _BuildSymbolTable(rules);
						var blockEnds = _BuildBlockEnds(rules);
						var nodeFlags = _BuildNodeFlags(rules);
						var dfaTable = _ToDfaStateTable(fa,symbolTable);
						if (!noshared)
						{
							// import our Shared/Token.cs into the library
							_ImportCompileUnit(Shared.Token, cns);
							if (!compiled)
							{
								// import our Shared/TableTokenizer.cs into the library
								_ImportCompileUnit(Shared.TableTokenizer, cns);
							} else
							{
								// import our Shared/CompiledTokenizer.cs into the library
								_ImportCompileUnit(Shared.CompiledTokenizer, cns);
							}
						}
						if (!compiled)
						{
							var origName = "Rolex.";
							CodeTypeDeclaration td = null;
							CodeDomVisitor.Visit(Shared.TableTokenizerTemplate, (ctx) => {
								td = ctx.Target as CodeTypeDeclaration;
								if (null != td && td.Name.EndsWith("Template"))
								{
									origName += td.Name;
									td.Name = name;
									var f = CodeDomUtility.GetByName("DfaTable", td.Members) as CodeMemberField;
									f.InitExpression = CodeGenerator.GenerateDfaTableInitializer(dfaTable);
									f = CodeDomUtility.GetByName("NodeFlags", td.Members) as CodeMemberField;
									f.InitExpression = CodeDomUtility.Literal(nodeFlags);
									f = CodeDomUtility.GetByName("BlockEnds", td.Members) as CodeMemberField;
									f.InitExpression = CodeDomUtility.Literal(blockEnds);
									CodeGenerator.GenerateSymbolConstants(td, symbolTable);
									ctx.Cancel = true;
								}
							});
							CodeDomVisitor.Visit(Shared.TableTokenizerTemplate, (ctx) => {
								var tr = ctx.Target as CodeTypeReference;
								if (null != tr && 0 == string.Compare(origName, tr.BaseType, StringComparison.InvariantCulture))
									tr.BaseType = name;
							});
							cns.Types.Add(td);
						}
						else
						{
							var tokOrigName = "";
							var enumOrigName = "";
							var types = Shared.CompiledTokenizerTemplate.Namespaces[Shared.CompiledTokenizerTemplate.Namespaces.Count - 1].Types;
							var td = CodeDomUtility.GetByName("CompiledTokenizerTemplate",types);
							tokOrigName += td.Name;
							td.Name = name;
							CodeGenerator.GenerateSymbolConstants(td, symbolTable); // syms are modified here
							cns.Types.Add(td);
							td = CodeDomUtility.GetByName("CompiledTokenizerEnumeratorTemplate", types);
							enumOrigName += td.Name;
							td.Name = name + "Enumerator";
							td.Members.Add(CodeGenerator.GenerateLexMethod(name, symbolTable, dfaTable));
							td.Members.Add(CodeGenerator.GenerateGetBlockEndMethod(name, symbolTable, blockEnds));
							td.Members.Add(CodeGenerator.GenerateIsHiddenMethod(name, symbolTable, nodeFlags));
							cns.Types.Add(td);
							CodeDomVisitor.Visit(Shared.CompiledTokenizerTemplate, (ctx) => {
								var tr = ctx.Target as CodeTypeReference;
								if (null != tr) {
									if (0 == string.Compare(tokOrigName, tr.BaseType, StringComparison.InvariantCulture) ||
									0 == string.Compare("Rolex."+tokOrigName, tr.BaseType, StringComparison.InvariantCulture)
									)
										tr.BaseType = name;
									else if (0 == string.Compare(enumOrigName, tr.BaseType, StringComparison.InvariantCulture)||
									0 == string.Compare("Rolex."+enumOrigName, tr.BaseType, StringComparison.InvariantCulture))
										tr.BaseType = name + "Enumerator";
								}
							});
							
							//cns.Types.Add(CodeGenerator.GenerateCompiledTokenizer(name, symbolTable));
							//cns.Types.Add(CodeGenerator.GenerateCompiledTokenizerEnumerator(name, symbolTable, dfaTable, blockEnds, nodeFlags));
						}
						var hasColNS = false;
						foreach (CodeNamespaceImport nsi in cns.Imports)
						{
							if (0 == string.Compare(nsi.Namespace, "System.Collections.Generic", StringComparison.InvariantCulture))
							{
								hasColNS = true;
								break;
							}
						}
						if (!hasColNS)
							cns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
						Console.Error.WriteLine();
						var prov = CodeDomProvider.CreateProvider(codelanguage);
						var opts = new CodeGeneratorOptions();
						opts.BlankLinesBetweenMembers = false;
						opts.VerbatimOrder = true;
						if (null == outputfile)
							output = Console.Out;
						else
						{
							// open the file and truncate it if necessary
							var stm = File.Open(outputfile, FileMode.Create);
							stm.SetLength(0);
							output = new StreamWriter(stm);
						}
						prov.GenerateCodeFromCompileUnit(ccu, output, opts);
					}
				}
			}
			// we don't like to catch in debug mode
#if !DEBUG
		    catch(Exception ex)
			{
				result = _ReportError(ex);
			}
#endif
			finally
			{
				// close the input file if necessary
				if (null != input)
					input.Close();
				// close the output file if necessary
				if (null != outputfile && null != output)
					output.Close();
			}
			return result;
		}

		private static void _ImportCompileUnit(CodeCompileUnit fromCcu,CodeNamespace dst)
		{
			CD.CodeDomVisitor.Visit(fromCcu, (ctx) => {
				var ctr = ctx.Target as CodeTypeReference;
				if(null!=ctr)
				{
					if (ctr.BaseType.StartsWith("Rolex."))
						ctr.BaseType = ctr.BaseType.Substring(6);
				}
			});
			// import all the usings and all the types
			foreach (CodeNamespace ns in fromCcu.Namespaces)
			{
				foreach (CodeNamespaceImport nsi in ns.Imports)
				{
					var found = false;
					foreach (CodeNamespaceImport nsicmp in dst.Imports)
					{
						if (0 == string.Compare(nsicmp.Namespace, nsi.Namespace, StringComparison.InvariantCulture))
						{
							found = true;
							break;
						}
					}
					if (!found)
						dst.Imports.Add(nsi);
				}
				foreach (CodeTypeDeclaration type in ns.Types)
				{
					type.CustomAttributes.Add(CodeGenerator.GeneratedCodeAttribute);
					dst.Types.Add(type);
				}
			}
		}

		static void _FillRuleIds(IList<_LexRule> rules)
		{
			var ids = new HashSet<int>();
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				if (int.MinValue!=rule.Id && !ids.Add(rule.Id))
					throw new InvalidOperationException(string.Format("The input file has a rule with a duplicate id at line {0}, column {1}, position {2}", rule.Line, rule.Column, rule.Position));
			}
			var lastId = 0;
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				if(int.MinValue==rule.Id)
				{
					rule.Id = lastId;
					ids.Add(lastId);
					while(ids.Contains(lastId))
						++lastId;
				} else
				{
					lastId = rule.Id;
					while (ids.Contains(lastId))
						++lastId;
				}
			}
		}
		static string[] _BuildBlockEnds(IList<_LexRule> rules)
		{
			int max = int.MinValue;
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				if (rule.Id > max)
					max = rule.Id;
			}
			var result = new string[max + 1];
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				var be = _GetAttr(rule, "blockEnd") as string;
				if(!string.IsNullOrEmpty(be))
				{
					result[rule.Id] = be;
				}
			}
			return result;
		}
		static int[] _BuildNodeFlags(IList<_LexRule> rules)
		{
			int max = int.MinValue;
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				if (rule.Id > max)
					max = rule.Id;
			}
			var result = new int[max + 1];
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				var hidden = _GetAttr(rule, "hidden");
				if ((hidden is bool) && (bool)hidden)
					result[rule.Id] = 1;
			}
			return result;
		}
		static object _GetAttr(_LexRule rule,string name,object @default = null)
		{
			var attrs = rule.Attributes;
			if (null != attrs)
			{
				for (var i = 0; i < attrs.Length; i++)
				{
					var attr = attrs[i];
					if (0 == string.Compare(attr.Key, name))
						return attr.Value;
				}
			}
			return @default;
		}
		static string[] _BuildSymbolTable(IList<_LexRule> rules)
		{
			int max = int.MinValue;
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				if (rule.Id > max)
					max = rule.Id;
			}
			var result = new string[max + 1];
			for (int ic = rules.Count, i = 0; i < ic; ++i)
			{
				var rule = rules[i];
				result[rule.Id] = rule.Symbol;
			}
			return result;
		}
		static IList<_LexRule> _ParseRules(TextReader inp)
		{
			var result = new List<_LexRule>();
			var pc = ParseContext.CreateFrom(inp);
			pc.EnsureStarted();
			while (-1 != pc.Current)
			{
				pc.TrySkipCCommentsAndWhiteSpace();
				if (-1 == pc.Current)
					break;
				pc.ClearCapture();
				var l = pc.Line;
				var c = pc.Column;
				var p = pc.Position;
				var rule = new _LexRule();
				rule.Line = l;
				rule.Column = c;
				rule.Position = p;
				if (!pc.TryReadCIdentifier())
					throw new ExpectingException(string.Format("Identifier expected at line {0}, column {1}, position {2}", l, c, p), l, c, p, "identifier");
				rule.Symbol = pc.GetCapture();
				rule.Id = int.MinValue;
				pc.ClearCapture();
				pc.TrySkipCCommentsAndWhiteSpace();
				pc.Expecting('<', '=');
				if('<'==pc.Current)
				{
					pc.Advance();
					pc.Expecting();
					var attrs = new List<KeyValuePair<string, object>>();
					while(-1!=pc.Current && '>'!=pc.Current)
					{
						pc.TrySkipCCommentsAndWhiteSpace();
						pc.ClearCapture();
						l = pc.Line;
						c = pc.Column;
						p = pc.Position;
						if (!pc.TryReadCIdentifier())
							throw new ExpectingException(string.Format("Identifier expected at line {0}, column {1}, position {2}", l,c,p), l, c, p, "identifier");
						var aname = pc.GetCapture();
						pc.TrySkipCCommentsAndWhiteSpace();
						pc.Expecting('=', '>', ',');
						if ('=' == pc.Current)
						{
							pc.Advance();
							pc.TrySkipCCommentsAndWhiteSpace();
							l = pc.Line;
							c = pc.Column;
							p = pc.Position;
							var value = pc.ParseJsonValue();
							attrs.Add(new KeyValuePair<string, object>(aname, value));
							if (0 == string.Compare("id", aname) && (value is double))
							{
								rule.Id = (int)((double)value);
								if (0 > rule.Id)
									throw new ExpectingException(string.Format("Expecting a non-negative integer at line {0}, column {1}, position {2}", l, c, p), l, c, p, "nonNegativeInteger");
							}
						}
						else
						{ // boolean true
							attrs.Add(new KeyValuePair<string, object>(aname, true));
						}
						pc.TrySkipCCommentsAndWhiteSpace();
						pc.Expecting(',', '>');
						if(','==pc.Current)
							pc.Advance();
					}
					pc.Expecting('>');
					pc.Advance();
					rule.Attributes = attrs.ToArray();
					pc.TrySkipCCommentsAndWhiteSpace();
				}
				pc.Expecting('=');
				pc.Advance();
				pc.TrySkipCCommentsAndWhiteSpace();
				pc.Expecting('\'', '\"');
				if ('\'' == pc.Current)
				{
					pc.Advance();
					pc.ClearCapture();
					pc.TryReadUntil('\'', '\\', false);
					pc.Expecting('\'');
					var pc2 = ParseContext.Create(pc.GetCapture());
					pc2.EnsureStarted();
					pc2.SetLocation(pc.Line, pc.Column, pc.Position);
					var rx = RegexExpression.Parse(pc2);
					pc.Advance();
					rule.Expression = rx;
				}
				else
				{
					var str = pc.ParseJsonString();
					rule.Expression = RegexLiteralExpression.CreateString(str);
				}
				result.Add(rule);
			}
			if (0 == result.Count)
				throw new ExpectingException("Expecting lexer rules, but the document was empty", 0, 0, 0, "rule");
			return result;

		}
		static CharFA<string> _BuildLexer(IList<_LexRule> rules)
		{
			var exprs = new CharFA<string>[rules.Count];
			var result = new CharFA<string>();
			for (var i = 0;i<exprs.Length;++i)
			{
				var rule = rules[i];
				var fa = rule.Expression.ToFA(rule.Symbol);
				var o = _GetAttr(rule, "ignoreCase", false);
				if (o is bool && (bool)o)
					fa=CharFA<string>.CaseInsensitive(fa, fa.FirstAcceptSymbol);
				result.EpsilonTransitions.Add(fa);
			}
			return result;
		}

		// do our error handling here (release builds)
		static int _ReportError(Exception ex)
		{
			_PrintUsage();
			Console.Error.WriteLine("Error: {0}", ex.Message);
			return -1;
		}
		static void _PrintUsage()
		{
			var t = Console.Error;
			// write the name of our app. this actually uses the 
			// name of the executable so it will always be correct
			// even if the executable file was renamed.
			t.WriteLine("{0} generates a lexer/tokenizer", _Name);
			t.WriteLine();
			t.Write(_File);
			t.WriteLine(" <inputfile> [/output <outputfile>] [/name <name>]");
			t.WriteLine("   [/namespace <codenamespace>] [/language <codelanguage>]");
			t.WriteLine("   [/noshared] [/compiled] [/ifstale]");
			t.WriteLine();
			t.WriteLine("   <inputfile>      The input file to use.");
			t.WriteLine("   <outputfile>     The output file to use - default stdout.");
			t.WriteLine("   <name>           The name to use - default taken from <inputfile>.");
			t.WriteLine("   <codelanguage>   The code language - default based on output file - default C#");
			t.WriteLine("   <codenamepace>   The code namespace");
			t.WriteLine("   <noshared>       Do not generate the shared dependency code");
			t.WriteLine("   <compiled>       Generate compiled instead of table driven code (not recommended)");
			t.WriteLine("   <ifstale>        Do not generate unless <outputfile> is older than <inputfile>.");
			t.WriteLine();
			t.WriteLine("Any other switch displays this screen and exits.");
			t.WriteLine();
		}
		static DfaEntry[] _ToDfaStateTable<TAccept>(CharFA<TAccept> fsm,IList<TAccept> symbolTable = null, IProgress<CharFAProgress> progress = null)
		{
			
			
			var dfa = fsm.ToDfa(new _ConsoleProgress());
			//dfa.TrimDuplicates(new _ConsoleProgress());
			var closure = dfa.FillClosure();
			var symbolLookup = new ListDictionary<TAccept, int>();
			// if we don't have a symbol table, build 
			// the symbol lookup from the states.
			if (null == symbolTable)
			{
				// go through each state, looking for accept symbols
				// and then add them to the new symbol table if we
				// haven't already
				var i = 0;
				for (int jc = closure.Count, j = 0; j < jc; ++j)
				{
					var fa = closure[j];
					if (fa.IsAccepting && !symbolLookup.ContainsKey(fa.AcceptSymbol))
					{
						symbolLookup.Add(fa.AcceptSymbol, i);
						++i;
					}
				}
			}
			else // build the symbol lookup from the symbol table
				for (int ic = symbolTable.Count, i = 0; i < ic; ++i)
					if (null != symbolTable[i])
						symbolLookup.Add(symbolTable[i], i);

			// build the root array
			var result = new DfaEntry[closure.Count];
			for (var i = 0; i < result.Length; i++)
			{
				var fa = closure[i];
				// get all the transition ranges for each destination state
				var trgs = fa.FillInputTransitionRangesGroupedByState();
				// make a new transition entry array for our DFA state table
				var trns = new DfaTransitionEntry[trgs.Count];
				var j = 0;
				// for each transition range
				foreach (var trg in trgs)
				{
					// add the transition entry using
					// the packed ranges from CharRange
					trns[j] = new DfaTransitionEntry(
						CharRange.ToPackedChars(trg.Value),
						closure.IndexOf(trg.Key));

					++j;
				}
				// now add the state entry for the state above
				result[i] = new DfaEntry(trns,
					fa.IsAccepting ? symbolLookup[fa.AcceptSymbol] : -1);

			}
			return result;
		}
		
		/// <summary>
		/// Reference implementation for a DfaEntry
		/// </summary>
	}
	struct DfaEntry
	{
		/// <summary>
		/// The state transitions
		/// </summary>
		public DfaTransitionEntry[] Transitions;
		/// <summary>
		/// The accept symbol id or -1 for non-accepting
		/// </summary>
		public int AcceptSymbolId;
		/// <summary>
		/// Constructs a new instance
		/// </summary>
		/// <param name="transitions">The state transitions</param>
		/// <param name="acceptSymbolId">The accept symbol id</param>
		public DfaEntry(DfaTransitionEntry[] transitions, int acceptSymbolId)
		{
			this.Transitions = transitions;
			this.AcceptSymbolId = acceptSymbolId;
		}
	}
	/// <summary>
	/// The state transition entry
	/// </summary>
	struct DfaTransitionEntry
	{
		/// <summary>
		/// The character ranges, packed as adjacent pairs.
		/// </summary>
		public char[] PackedRanges;
		/// <summary>
		/// The destination state
		/// </summary>
		public int Destination;
		/// <summary>
		/// Constructs a new instance
		/// </summary>
		/// <param name="packedRanges">The packed character ranges</param>
		/// <param name="destination">The destination state</param>
		public DfaTransitionEntry(char[] packedRanges, int destination)
		{
			this.PackedRanges = packedRanges;
			this.Destination = destination;
		}
	}
	// used to hold the results of reading the input document
	class _LexRule
	{
		public int Id;
		public string Symbol;
		public KeyValuePair<string, object>[] Attributes;
		public RegexExpression Expression;
		public int Line;
		public int Column;
		public long Position;
	}
	class _ConsoleProgress : IProgress<CharFAProgress>
	{
		public void Report(CharFAProgress progress)
		{
			Console.Error.Write(".");
		}
	}
}
