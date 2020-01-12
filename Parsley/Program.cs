//#define NOPREPARELL1
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Parsley
{
	using C = CD.CodeDomUtility;
	public class Program
	{
		internal static readonly string CodeBase = _GetCodeBase();
		internal static readonly string FileName = Path.GetFileName(CodeBase);
		internal static readonly string Name = _GetName();
		
		static int Main(string[] args)
		{
			return Run(args, Console.In, Console.Out, Console.Error);
		}
		
		public static int Run(string[] args,TextReader stdin,TextWriter stdout,TextWriter stderr)
		{
			int result = 0;
			TextWriter output = null;

			string inputfile = null;
			string outputfile = null;
			string rolexfile = null;
			string gplexfile = null;
			string gplexcodeclass = null;
			string codenamespace = null;
			string codelanguage = null;
			string codeclass = null;
			bool verbose = false;
			bool noshared = false;
			bool ifstale = false;
			bool fast = false;
			bool noparser = false;

			try
			{
				if (0 == args.Length)
				{
					_PrintUsage(stderr);
					return -1;
				}
				if (args[0].StartsWith("/"))
					throw new ArgumentException("Missing input file.");

				// process the command line args
				inputfile = args[0];
				for (var i = 1; i < args.Length; ++i)
				{
					switch (args[i])
					{
						case "/namespace":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							codenamespace = args[i];
							break;
						case "/class":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							codeclass = args[i];
							break;
						case "/language":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							codelanguage = args[i];
							break;
						case "/output":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							outputfile = args[i];
							break;
						case "/ifstale":
							ifstale = true;
							break;
						case "/fast":
							fast = true;
							break;
						case "/noparser":
							noparser= true;
							break;
						case "/noshared":
							noshared = true;
							break;
						case "/verbose":
							verbose = true;
							break;
						case "/rolex":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							rolexfile = args[i];
							break;
						case "/gplex":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							gplexfile = args[i];
							break;
						case "/gplexclass":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							gplexcodeclass = args[i];
							break;

						default:
							throw new ArgumentException(string.Format("Unknown switch {0}", args[i]));
					}
				}
				if (null != outputfile && noparser)
					throw new ArgumentException("<noparser> and <ouputfile> cannot both be specified.", "outputfile");
				
				if (null == codeclass)
				{
					if (null != outputfile)
						codeclass = Path.GetFileNameWithoutExtension(outputfile);
					else
						codeclass = Path.GetFileNameWithoutExtension(inputfile);
				}

				string gplexshared = null;
				string gplexcode = null;
				if (null != gplexfile)
				{
					if (null == gplexcodeclass)
					{
						gplexcodeclass = Path.GetFileNameWithoutExtension(inputfile);
					}
					var dir = Path.GetDirectoryName(gplexfile);
					if (!noshared)
					{
						gplexshared = Path.Combine(dir, "GplexShared.cs");
					}
					gplexcode = Path.Combine(dir, string.Concat(gplexcodeclass, "Tokenizer.cs"));
				}
				// override the options with our document's options
				var doc = XbnfDocument.ReadFrom(inputfile);
				var oi = -1;
				oi = doc.Options.IndexOf("outputfile");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					var s = o as string;
					if (null!=s)
					{
						outputfile = s;
						if ("" == outputfile)
							outputfile = null;
					}
					// if it's specified in the doc we need to make it doc relative
					if (null != outputfile)
					{
						if (!Path.IsPathRooted(outputfile))
						{
							var dir = Path.GetDirectoryName(Path.GetFullPath(inputfile));
							outputfile = Path.GetFullPath(Path.Combine(dir, outputfile));
						}
					}
				}
				oi = doc.Options.IndexOf("rolexfile");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					var s = o as string;
					if (null != s)
					{
						rolexfile= s;
						if ("" == rolexfile)
							rolexfile= null;
					}
					// if it's specified in the doc we need to make it doc relative
					if (null != rolexfile)
					{
						if (!Path.IsPathRooted(rolexfile))
						{
							var dir = Path.GetDirectoryName(Path.GetFullPath(inputfile));
							rolexfile = Path.GetFullPath(Path.Combine(dir, rolexfile));
						}
					}
				}
				oi = doc.Options.IndexOf("gplexfile");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					var s = o as string;
					if (null != s)
					{
						gplexfile= s;
						if ("" == gplexfile)
							gplexfile = null;
					}
					// if it's specified in the doc we need to make it doc relative
					if(null!=gplexfile)
					{
						var dir = Path.GetDirectoryName(Path.GetFullPath(inputfile));
						if (!Path.IsPathRooted(gplexfile))
						{
							gplexfile = Path.GetFullPath(Path.Combine(dir, gplexfile));
						}
						dir = Path.GetDirectoryName(gplexfile);
						if (!noshared)
						{
							gplexshared = Path.Combine(dir, "GplexShared.cs");
						}
					}
				}
				
				oi = doc.Options.IndexOf("gplexcodeclass");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					var s = o as string;
					if (null != s)
					{
						gplexcodeclass = s;
						if ("" == gplexcodeclass)
							gplexcodeclass = null;
					}
				}
				if (null!=gplexfile)
				{
					if(null == gplexcodeclass)
						gplexcodeclass = Path.GetFileNameWithoutExtension(inputfile);
					gplexcode = Path.Combine(Path.GetDirectoryName(gplexfile), string.Concat(gplexcodeclass, "Tokenizer.cs"));
				}
				
				oi = doc.Options.IndexOf("codenamespace");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					var s = o as string;
					if (null != s)
					{
						codenamespace = s;
					}
				}
				oi = doc.Options.IndexOf("codelanguage");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					var s = o as string;
					if (!string.IsNullOrEmpty(s))
					{
						codelanguage = s;
					}
				}
				oi = doc.Options.IndexOf("codeclass");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					var s = o as string;
					if (null != s)
					{
						codeclass = s;
						if ("" == codeclass)
						{
							if (null != outputfile)
								codeclass = Path.GetFileNameWithoutExtension(outputfile);
							else
								codeclass = Path.GetFileNameWithoutExtension(inputfile);
						}
					}
				}
				oi = doc.Options.IndexOf("verbose");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					if (o is bool)
					{
						verbose = (bool)o;
					}
				}
				oi = doc.Options.IndexOf("fast");
				if (-1 < oi)
				{
					var o = doc.Options[oi].Value;
					if (o is bool)
					{
						fast = (bool)o;
					}
				}
				if (fast && null != codelanguage)
					throw new ArgumentException("<codelanguage> and <fast> cannot both be specified. The <fast> option is C# only.");

				var stale = true;
				if (ifstale)
				{
					stale = false;
					if (!stale && null != rolexfile)
						if (_IsStale(inputfile, rolexfile))
							stale = true;
					if (!stale && null != gplexfile)
						if (_IsStale(inputfile, gplexfile))
							stale = true;
					if (!stale)
					{
						var files = XbnfDocument.GetResources(inputfile);
						foreach (var s in files)
						{
							if (_IsStale(s, outputfile))
							{
								stale = true;
								break;
							}
						}

					}
					// see if our exe has changed
					if (!stale && null!=outputfile && _IsStale(CodeBase, outputfile))
						stale = true;

				}

				if (!stale)
				{
					stderr.WriteLine("Skipped building of the following because they were not stale:");
					if (null != outputfile)
						stderr.WriteLine("Output file: " + outputfile);
					if (null != rolexfile)
						stderr.WriteLine("Rolex file: " + rolexfile);
					if (null != gplexfile)
						stderr.WriteLine("Gplex file: " + gplexfile);
					if (null != gplexshared)
						stderr.WriteLine("Gplex shared code file: " + gplexshared);
					if (null != gplexcode)
						stderr.WriteLine("Gplex tokenizer code file: " + gplexcode);
				}
				else
				{
					stderr.WriteLine("{0} is building the following:", Name);
					if (null != outputfile)
						stderr.WriteLine("Output file: " + outputfile);
					if (null != rolexfile)
						stderr.WriteLine("Rolex file: " + rolexfile);
					if (null != gplexfile)
						stderr.WriteLine("Gplex file: " + gplexfile);
					if (null != gplexshared)
						stderr.WriteLine("Gplex shared code file: " + gplexshared);
					if (null != gplexcode)
						stderr.WriteLine("Gplex tokenizer code file: " + gplexcode);
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
					var s = codelanguage.ToUpperInvariant();
					if (null != gplexfile && s != "CS" && s != "C#" && s != "CSHARP")
						stderr.WriteLine("Warning: Gplex only targets C# but the langauge specified was {0}. This will require the C# code to be compiled separately and referenced from the main project.", codelanguage);

					var isLexerOnly = true;
					if (doc.HasNonTerminalProductions)
						isLexerOnly = false;
					else {
						foreach(var import in doc.Imports)
						{
							if(import.Document.HasNonTerminalProductions)
							{
								isLexerOnly = false;
								break;
							}
						}
						
					}
					// we need to prepare it by marking every terminal
					// with an attribute if it isn't already. we use 
					// "terminal" because it doesn't impact terminals
					// in any way, but this way the CfgDocument can
					// "see" them.
					for (int ic = doc.Productions.Count, i = 0; i < ic; ++i)
					{
						var p = doc.Productions[i];
						if (p.IsTerminal && 0 == p.Attributes.Count)
							p.Attributes.Add(new XbnfAttribute("terminal", true));

					}

					XbnfGenerationInfo genInfo;
					var msgs = XbnfConvert.TryCreateXbnfImportData(doc, out genInfo);

					foreach (var msg in msgs)
					{
						if (verbose || ErrorLevel.Message != msg.ErrorLevel)
							stderr.WriteLine(msg);

					}
					CfgException.ThrowIfErrors(msgs);
					CfgDocument primaryCfg = genInfo.CfgMap[doc];
					
					if (!isLexerOnly)
					{

						if (verbose)
						{
							stderr.WriteLine("Final grammars:");
							foreach (var cfg in genInfo.CfgMap)
							{
								stderr.WriteLine(cfg.ToString());
								stderr.WriteLine();
							}
						}
						CfgException.ThrowIfErrors(msgs);
						if (!noparser)
						{
							var ccu = CodeGenerator.GenerateCompileUnit(doc, genInfo, codeclass, codenamespace, fast);
							var ccuNS = ccu.Namespaces[ccu.Namespaces.Count - 1];
							var ccuShared = CodeGenerator.GenerateSharedCompileUnit(codenamespace);
							var sNS = ccuShared.Namespaces[ccuShared.Namespaces.Count - 1];
							var parserContext = C.GetByName("ParserContext", sNS.Types);
							var parseNode = C.GetByName("ParseNode", sNS.Types);
							var syntaxException = C.GetByName("SyntaxException", sNS.Types);
							var lookAheadEnumerator = C.GetByName("LookAheadEnumerator", sNS.Types);
							var lookAheadEnumeratorEnumerable = C.GetByName("LookAheadEnumeratorEnumerable", sNS.Types);
							var lookAheadEnumeratorEnumerator = C.GetByName("LookAheadEnumeratorEnumerator", sNS.Types);
							ccuNS.Types.Add(syntaxException);
							ccuNS.Types.Add(parseNode);
							ccuNS.Types.Add(parserContext);
							ccuNS.Types.Add(lookAheadEnumerator);
							ccuNS.Types.Add(lookAheadEnumeratorEnumerable);
							ccuNS.Types.Add(lookAheadEnumeratorEnumerator);
							ccu.ReferencedAssemblies.Add(typeof(TypeConverter).Assembly.GetName().ToString());

							if (fast)
							{
								CD.CodeDomVisitor.Visit(ccu, (ctx) =>
								{
									var vd = ctx.Target as CodeVariableDeclarationStatement;
									if (null != vd && CD.CodeDomResolver.IsNullOrVoidType(vd.Type))
										vd.Type = C.Type("var");
								}, CD.CodeDomVisitTargets.All & ~(CD.CodeDomVisitTargets.Expressions | CD.CodeDomVisitTargets.Comments | CD.CodeDomVisitTargets.Attributes | CD.CodeDomVisitTargets.Directives | CD.CodeDomVisitTargets.Types | CD.CodeDomVisitTargets.TypeRefs));
							}
							else
							{
								CD.SlangPatcher.Patch(ccu, ccuShared);
								var co = CD.SlangPatcher.GetNextUnresolvedElement(ccu);
								if (null != co)
								{
									stderr.WriteLine("Warning: Not all of the elements could be resolved. The generated code may not be correct in all languages.");
									stderr.WriteLine("  Next unresolved: {0}", C.ToString(co).Trim());
								}
							}
							if (noshared)
							{
								// we just needed these for slang resolution
								ccuNS.Types.Remove(syntaxException);
								ccuNS.Types.Remove(parseNode);
								ccuNS.Types.Remove(parserContext);
								ccuNS.Types.Remove(lookAheadEnumerator);
								ccuNS.Types.Remove(lookAheadEnumeratorEnumerable);
								ccuNS.Types.Remove(lookAheadEnumeratorEnumerator);
							}
							foreach (CodeNamespace ns in ccu.Namespaces)
							{
								var hasColNS = false;
								foreach (CodeNamespaceImport nsi in ns.Imports)
								{
									if (0 == string.Compare(nsi.Namespace, "System.Collections.Generic", StringComparison.InvariantCulture))
									{
										hasColNS = true;
										break;
									}
								}
								if (!hasColNS)
									ns.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
							}
							var prov = CodeDomProvider.CreateProvider(codelanguage);

							if (null != outputfile)
							{
								var sw = new StreamWriter(outputfile);
								sw.BaseStream.SetLength(0);
								output = sw;
							}
							else
								output = stdout;
							var opts = new CodeGeneratorOptions();
							opts.VerbatimOrder = true;
							opts.BlankLinesBetweenMembers = false;
							prov.GenerateCodeFromCompileUnit(ccu, output, opts);
							output.Flush();
							output.Close();
							output = null;
						}
						
					}
					else
						stderr.WriteLine("{0} skipped parser generation because there are no non-terminals and no imports defined.", Name);

					if (null != rolexfile)
					{
						var sw = new StreamWriter(rolexfile);
						sw.BaseStream.SetLength(0);
						output = sw;
						output.WriteLine(XbnfConvert.ToRolexSpec(genInfo));
						output.Flush();
						output.Close();
						output = null;
					}
					if (null != gplexfile)
					{
						if (!noshared)
						{
							using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Parsley.Export.GplexShared.Prototype.cs")))
								s = sr.ReadToEnd();
							s = s.Replace("Parsley_codenamespace", codenamespace);
							using (var sw = new StreamWriter(System.IO.File.Open(gplexshared, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
							{
								sw.BaseStream.SetLength(0);
								sw.Write(s);
								sw.Flush();
							}
						}

						s = XbnfConvert.ToGplexSpec(genInfo, codenamespace, gplexcodeclass);
						using (var sw = new StreamWriter(System.IO.File.Open(gplexfile, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
						{
							sw.BaseStream.SetLength(0);
							sw.Write(s);
							sw.Flush();
						}
						if (null != gplexcode)
						{
							using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Parsley.Export.GplexDepends.Prototype.cs")))
								s = sr.ReadToEnd();
							if (null != codenamespace)
							{
								s = string.Concat("namespace ", codenamespace, Environment.NewLine, "{", Environment.NewLine, s, "}", Environment.NewLine);
							}
							s = s.Replace("Parsley_codeclass", gplexcodeclass);
							s = s.Replace("Parsley_constants", XbnfConvert.ToGplexSymbolConstants(genInfo.CfgMap));
							using (var sw = new StreamWriter(System.IO.File.Open(gplexcode, FileMode.OpenOrCreate, FileAccess.ReadWrite)))
							{
								sw.BaseStream.SetLength(0);
								sw.Write(s);
								sw.Flush();
							}
						}
					}
				}
			}
#if !DEBUG
			catch (Exception ex)
			{
				result = _ReportError(ex,stderr);
			}
#endif
			finally
			{
				stderr.Close();
				stdout.Close();
				if (outputfile != null && null != output)
				{
					output.Close();
				}

			}
			return result;


		}
		static string _GetCodeBase()
		{
			try
			{
				return Assembly.GetExecutingAssembly().GetModules()[0].FullyQualifiedName;
			}
			catch
			{
				return "parsley.exe";
			}
		}
		static string _GetName()
		{
			try
			{
				foreach (var attr in Assembly.GetExecutingAssembly().CustomAttributes)
				{
					if (typeof(AssemblyTitleAttribute) == attr.AttributeType)
					{
						return attr.ConstructorArguments[0].Value as string;
					}
				}
			}
			catch { }
			return Path.GetFileNameWithoutExtension(FileName);
		}
		// do our error handling here (release builds)
		static int _ReportError(Exception ex,TextWriter stderr)
		{
			_PrintUsage(stderr);
			stderr.WriteLine("Error: {0}", ex.Message);
			return -1;
		}
		static void _PrintUsage(TextWriter stderr)
		{
			var t = stderr;
			// write the name of our app. this actually uses the 
			// name of the executable so it will always be correct
			// even if the executable file was renamed.
			t.WriteLine("{0} generates a recursive descent parser and optional lexer spec", Name);
			t.WriteLine();
			t.Write(FileName);
			t.WriteLine(" <inputfile> [/output <outputfile>] [/rolex <rolexfile>]");
			t.WriteLine("	[/gplex <gplexfile>] [/gplexclass <gplexcodeclass>]");
			t.WriteLine("	[/namespace <codenamespace>] [/class <codeclass>]");
			t.WriteLine("	[/langage <codelanguage>] [/fast] [/noshared]");
			t.WriteLine("	[/verbose] [/ifstale] [/noparser]");
			t.WriteLine();
			t.WriteLine("	<inputfile>		The XBNF input file to use.");
			t.WriteLine("	<outputfile>		The output file to use - default stdout.");
			t.WriteLine("	<rolexfile>		Output a Rolex lexer specification to the specified file");
			t.WriteLine("	<gplexfile>		Generate Gplex lexer specification to the specified file file. Will also generate supporting C# files (C# only)");
			t.WriteLine("	<gplexcodeclass>	Generate Gplex lexer specification with the specified class name. - default takes the name from <gplexfile>");
			t.WriteLine("	<codenamespace>		Generate code under the specified namespace - default none");
			t.WriteLine("	<codeclass>		Generate code with the specified class name - default derived from <outputfile> or the grammar.");
			t.WriteLine("	<codelanguage>		Generate code in the specified language - default derived from <outputfile> or C#.");
			t.WriteLine("	<fast>			Generate code quickly, without resolution using C# only - not valid with the <codelanguage> option.");
			t.WriteLine("	<noshared>		Do not include shared library prerequisites");
			t.WriteLine("	<verbose>		Output all messages from the generation process");
			t.WriteLine("	<ifstale>		Do not generate unless output files are older than the input files.");
			t.WriteLine("	<noparser>		Generate any specified lexers with the appropriate symbol table, and factor the grammar(s), but do not generate the parser output.");
			t.WriteLine();
			t.WriteLine("Any other switch displays this screen and exits.");
			t.WriteLine();
		}
		static bool _IsStale(string inputfile, string outputfile)
		{
			var result = true;
			// File.Exists doesn't always work right
			try
			{
				if (System.IO.File.GetLastWriteTimeUtc(outputfile) >= System.IO.File.GetLastWriteTimeUtc(inputfile))
					result = false;
			}
			catch { }
			return result;
		}
	}
}
