using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Parsley
{
	using C = CD.CodeDomUtility;
	using V = CD.CodeDomVisitor;
	class Program
	{
		static readonly string _CodeBase = Assembly.GetEntryAssembly().GetModules()[0].FullyQualifiedName;
		static readonly string _File = Path.GetFileName(_CodeBase);
		static readonly string _Name = _GetName();

		static int Main(string[] args)
		{
			int result = 0;
			TextWriter output = null;

			string inputfile = null;
			string outputfile = null;
			string rolexfile = null;
			string codenamespace = null;
			string codelanguage = null;
			string name = null;
			bool noshared = false;
			bool ifstale = false;

			HashSet<string> exclude = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			exclude.Add(@"Properties\AssemblyInfo.cs");
			try
			{
				if (0 == args.Length)
				{
					_PrintUsage();
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
							codenamespace= args[i];
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
						case "/output":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							outputfile = args[i];
							break;
						case "/ifstale":
							ifstale = true;
							break;
						case "/noshared":
							noshared = true;
							break;
						case "/rolex":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							rolexfile = args[i];
							break;
						default:
							throw new ArgumentException(string.Format("Unknown switch {0}", args[i]));
					}
				}
				var stale = true;
				if (ifstale && null != outputfile)
				{
					stale = false;
					if (_IsStale(inputfile, outputfile))
						stale = true;
					if (!stale && null != rolexfile)
						if (_IsStale(inputfile, rolexfile))
							stale = true;
					// see if our exe has changed
					if (!stale && _IsStale(_CodeBase, outputfile))
						stale = true;
				}

				if (!stale)
				{
					if (null == rolexfile)
						Console.Error.WriteLine("{0} skipped {1} because it was not stale.", _Name, outputfile);
					else
						Console.Error.WriteLine("{0} skipped {1} and {2} because they were not stale.", _Name, outputfile, rolexfile);
				}
				else
				{
					if (null != outputfile)
					{
						if (null == rolexfile)
							Console.Error.WriteLine("{0} is building {1}.", _Name, outputfile);
						else
							Console.Error.WriteLine("{0} is building {1} and {2}.", _Name, outputfile, rolexfile);
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
					if (null == name)
					{
						if(null!=outputfile)
							name = Path.GetFileNameWithoutExtension(outputfile);
					}
					var doc = XbnfDocument.ReadFrom(inputfile);
					var cfg = XbnfConvert.ToCfg(doc);
					cfg.PrepareLL1();
					var ccu = CodeGenerator.GenerateCompileUnit(doc,cfg,name,codenamespace);
					var ccuNS = ccu.Namespaces[ccu.Namespaces.Count - 1];
					var ccuShared = CodeGenerator.GenerateSharedCompileUnit(codenamespace);
					var sNS = ccuShared.Namespaces[ccuShared.Namespaces.Count - 1];
					var parserContext = C.GetByName("ParserContext",sNS.Types);
					var parseNode = C.GetByName("ParseNode", sNS.Types);
					var syntaxException = C.GetByName("SyntaxException", sNS.Types);
					ccuNS.Types.Add(syntaxException);
					ccuNS.Types.Add(parseNode);
					ccuNS.Types.Add(parserContext);
					ccu.ReferencedAssemblies.Add(typeof(TypeConverter).Assembly.GetName().ToString());
					CD.SlangPatcher.Patch(ccu, ccuShared);
					var co = CD.SlangPatcher.GetNextUnresolvedElement(ccu);
					if(null!=co)
					{
						Console.Error.WriteLine("Warning: Not all of the elements could be resolved. The generated code may not be correct in all languages.");
						Console.Error.WriteLine("  Next unresolved: {0}", C.ToString(co).Trim());
					}
					if(!noshared)
					{
						// we just needed these for slang resolution
						ccuNS.Types.Remove(syntaxException);
						ccuNS.Types.Remove(parseNode);
						ccuNS.Types.Remove(parserContext);
					}
					foreach (CodeNamespace ns in ccu.Namespaces)
					{
						var hasColNS = false;
						foreach (CodeNamespaceImport nsi in ns.Imports)
						{
							if (0 == string.Compare(nsi.Namespace, "System.Collections.Generic",StringComparison.InvariantCulture))
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
						output = Console.Out;
					var opts = new CodeGeneratorOptions();
					opts.VerbatimOrder = true;
					opts.BlankLinesBetweenMembers = false;
					prov.GenerateCodeFromCompileUnit(ccu, output, opts);
					output.Flush();
					output.Close();
					output = null;
					if(null!=rolexfile)
					{
						var sw = new StreamWriter(rolexfile);
						sw.BaseStream.SetLength(0);
						output = sw;
						output.WriteLine(XbnfConvert.ToRolexSpec(doc, cfg));
						output.Flush();
						output.Close();
						output = null;
					}

				}
			}
#if !DEBUG
			catch (Exception ex)
			{
				result = _ReportError(ex);
			}
#endif
			finally
			{
				if (outputfile != null && null != output)
				{
					output.Close();
				}

			}
			return result;
			
			
		}
		static string _GetName()
		{
			foreach (var attr in Assembly.GetEntryAssembly().CustomAttributes)
			{
				if (typeof(AssemblyTitleAttribute) == attr.AttributeType)
				{
					return attr.ConstructorArguments[0].Value as string;
				}
			}
			return Path.GetFileNameWithoutExtension(_File);
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
			t.WriteLine("{0} generates a recursive descent parser and optional lexer spec", _Name);
			t.WriteLine();
			t.Write(_File);
			t.WriteLine(" <inputfile> [/output <outputfile>] [/rolex <rolexfile>]");
			t.WriteLine("   [/namespace <codenamespace>] [/name <classname>]");
			t.WriteLine("	[/langage <codelanguage>] [/noshared] [/ifstale]");
			t.WriteLine();
			t.WriteLine("   <inputfile>     The input project file to use.");
			t.WriteLine("   <outputfile>    The output file to use - default stdout.");
			t.WriteLine("   <rolexfile>		Output a Rolex lexer specification to the specified file");
			t.WriteLine("   <codenamespace>	Generate code under the specified namespace - default none");
			t.WriteLine("   <classname>		Generate code with the specified class name - default derived from <outputfile> or the grammar.");
			t.WriteLine("   <codelanguage>	Generate code in the specified language - default derived from <outputfile> or C#.");
			t.WriteLine("	<noshared>		Do not include shared library prerequisites");
			t.WriteLine("	<ifstale>       Do not generate unless <outputfile> or <rolexfile> is older than <inputfile>.");
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
				if (File.GetLastWriteTimeUtc(outputfile) >= File.GetLastWriteTimeUtc(inputfile))
					result = false;
			}
			catch { }
			return result;
		}
	}
}
