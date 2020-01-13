//
// Deslang - cooks a series of Slang files into static arrays of codedom structures
// This can make your slang enabled projects perform much faster.
//
// Copyright (c) 2019 by honey the codewitch
// MIT license

using System;
using System.Reflection;
using System.IO;
using System.CodeDom;
using CD;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Text;
using Slang;
namespace Deslang
{
	using C = CodeDomUtility;
	using V = CodeDomVisitor;
	class Program
	{
		static readonly string _CodeBase = Assembly.GetEntryAssembly().GetModules()[0].FullyQualifiedName;
		static readonly string _File = Path.GetFileName(_CodeBase);
		static readonly string _Name = _GetName();
		static int Main(string[] args)
		{
			var result = 0;

			string outputfile = null;
			string codeclass = null;
			string codenamespace = null;
			string codelanguage = null;
			string t4language = null;
			bool ifstale = false;
			bool mutable = false;
			TextReader input = null;
			TextWriter output = null;
			try
			{
				var asms = new List<string>(args.Length);
				var inputs = new List<string>(args.Length);
				if (0 == args.Length)
				{
					_PrintUsage();
					result = -1;
				}
				else if (args[0].StartsWith("/"))
				{
					throw new ArgumentException("Missing input file.");
				}
				else
				{
					int start = 0;
					
					// process the command line args
					for (start = 0; start < args.Length; ++start)
					{
						var a = args[start];
						if (a.StartsWith("/"))
							break;
						inputs.Add(a);
					}
					for (var i = start; i < args.Length; ++i)
					{
						switch (args[i])
						{
							case "/output":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								outputfile = args[i];
								break;
							case "/language":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								codelanguage = args[i];
								break;
							case "/t4language":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								t4language = args[i];
								break;
							case "/class":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								codeclass = args[i];
								break;
							case "/namespace":
								if (args.Length - 1 == i) // check if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i; // advance 
								codenamespace = args[i];
								break;
							case "/asms":
								if(args.Length-1==i) // check to see if we're at the end
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
								++i;
								while(i<args.Length && !args[i].StartsWith("/"))
								{
									asms.Add(args[i]);
									++i;
								}
								if(0==asms.Count)
									throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i-1].Substring(1)));
								break;
							case "/ifstale":
								ifstale = true;
								break;
							case "/mutable":
								mutable = true;
								break;
							default:
								throw new ArgumentException(string.Format("Unknown switch {0}", args[i]));
						}
					}
				}
				// now build it.
				if (string.IsNullOrEmpty(t4language))
					t4language = "cs";
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
				if (ifstale && null != outputfile)
				{
					stale = false;
					foreach(var f in inputs)
					{
						if(_IsStale(f,outputfile) || _IsStale(_CodeBase,outputfile))
						{
							stale = true;
							break;
						}
					}
				}
				if (!stale)
				{
					Console.Error.WriteLine("{0} skipped cook of {1} because it was not stale.", _Name, outputfile);
				} else
				{
					// main build code here
					if (null != outputfile)
						Console.Error.Write("{0} is cooking {1}.", _Name, outputfile);
					else
						Console.Error.Write("{0} is cooking deslanged code.", _Name);
					
					#region Load Builder
					CodeCompileUnit builderCcu = null;
					using (var stm = typeof(Program).Assembly.GetManifestResourceStream("Deslang.Shared.CodeDomBuilder.cs"))
					{
						builderCcu = SlangParser.ReadCompileUnitFrom(stm);
					}
					builderCcu.ReferencedAssemblies.Add(typeof(CodeObject).Assembly.GetName().ToString());
					SlangPatcher.Patch(builderCcu);
					#endregion

					var donor = builderCcu.Namespaces[1].Types[0];

					Console.Error.WriteLine();
					var sb = new StringBuilder();
					var sw = new StringWriter(sb);
					var ccus = new CodeCompileUnit[inputs.Count];
					for(var i = 0;i<ccus.Length;i++)
					{
						var f = inputs[i];
						sb.Clear();
						input = new StreamReader(f);
						// TODO: make a way to propagate template arguments from the command line
						SlangPreprocessor.Preprocess(input, sw, null,t4language);
						input.Close();
						input = null;
						var ccu = SlangParser.ParseCompileUnit(sw.ToString());
						if(0==i)
						{
							ccu.ReferencedAssemblies.AddRange(asms.ToArray());
						}
						ccus[i]=ccu;
					}
					// now our unpatched input is in ccus
					SlangPatcher.Patch(ccus);
					var co = SlangPatcher.GetNextUnresolvedElement(ccus);
					if (null != co)
						Console.Error.WriteLine("Warning - input was not entirely resolved. Output may not be valid. Next unresolved element is: " + CodeDomUtility.ToString(co));
					// now they're patched. Let's serialize.
					// create our namespace and compileunit.
					var ns = new CodeNamespace();
					if (string.IsNullOrEmpty(codeclass))
						codeclass = "Deslanged";
					var cls = new CodeTypeDeclaration(codeclass);
					cls.IsClass = true;
					cls.IsPartial = true;
					cls.TypeAttributes = TypeAttributes.NotPublic;
					for (var i = 0; i < ccus.Length; i++)
					{
						var ccuInit = C.Literal(ccus[i], new CodeDomTypeConverter());
						V.Visit(ccuInit, (ctx) => {
							var tr = ctx.Target as CodeTypeReference;
							if(null!=tr)
							{
								if (tr.BaseType.StartsWith("System.CodeDom."))
									tr.BaseType = tr.BaseType.Substring(15);
								else if(tr.BaseType.StartsWith("System.Reflection."))
									tr.BaseType = tr.BaseType.Substring(18);
							}
							// look for our uses of codedombuilder
							var mi = ctx.Target as CodeMethodInvokeExpression;
							if (null != mi)
							{
								var tref = mi.Method.TargetObject as CodeTypeReferenceExpression;
								if (null != tref)
								{
									if (0 == string.Compare("CD.CodeDomBuilder", tref.Type.BaseType, StringComparison.InvariantCulture))
									{
										mi.Method.TargetObject = C.TypeRef(codeclass);
										// find the method in our donor type;
										var m = C.GetByName(mi.Method.MethodName, donor.Members);
										if (null != m) // if it hasn't already been moved
										{
											// move it 
											m.Name = "_" + m.Name;
											m.Attributes = (m.Attributes & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
											donor.Members.Remove(m);
											cls.Members.Add(m);
										}
										mi.Method.MethodName = "_" + mi.Method.MethodName;
									}
								}
							}
						});
						var name = Path.GetFileNameWithoutExtension(inputs[i]);
						if (mutable)
						{
							var fld = C.Field(typeof(CodeCompileUnit), name, MemberAttributes.Public | MemberAttributes.Static, ccuInit);
							cls.Members.Add(fld);
						} else
						{
							var prop = C.Property(typeof(CodeCompileUnit), name, MemberAttributes.Public | MemberAttributes.Static);
							prop.GetStatements.Add(C.Return(ccuInit));
							cls.Members.Add(prop);
						}
					}
					if (!string.IsNullOrEmpty(codenamespace))
						ns.Name = codenamespace;
					ns.Types.Add(cls);
					ns.Imports.Add(new CodeNamespaceImport("System.CodeDom"));
					ns.Imports.Add(new CodeNamespaceImport("System.Reflection"));
					var ccuFinal = new CodeCompileUnit();
					ccuFinal.Namespaces.Add(ns);
					
					// we're ready with ccuFinal
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
					prov.GenerateCodeFromCompileUnit(ccuFinal, output, opts);
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
				if(null!=input)
				{
					input.Close();
					input = null;
				}
				if(null!=outputfile && null!=output)
				{
					output.Close();
					output = null;
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
			t.WriteLine("{0} cooks one or more input Slang files into static arrays of codedom structures", _Name);
			t.WriteLine();
			t.Write(_File);
			t.WriteLine(" <inputfile> [/output <outputfile>] [/name <name>] [/class <codeclass>]");
			t.WriteLine("	[/namespace <codenamespace>] [/language <codelanguage>] [/ifstale]");
			t.WriteLine("	[/mutable] [/asms <assembly1> <assembly2> ... <assemblyN>]");
			t.WriteLine();
			t.WriteLine("   <inputfile>     The input file to use.");
			t.WriteLine("   <outputfile>    The output file to use - default stdout.");
			t.WriteLine("   <name>          The name to use - default taken from <inputfile>.");
			t.WriteLine("   <codeclass>	    The class to generate under - default Deslanged");
			t.WriteLine("   <codenamepace>  The code namespace");
			t.WriteLine("   <codelanguage>  The code language - default based on output file - default C#");
			t.WriteLine("   <t4language>	The t4 preprocessing language between <# and #> - default C#");
			t.WriteLine("	<ifstale>       Do not generate unless <outputfile> is older than <inputfile>.");
			t.WriteLine("	<mutable>       Generate output as fields instead of properties so their contents can be changed.");
			t.WriteLine("	<assembly>		The assembly name or path to the assembly to reference.");
			t.WriteLine();
			t.WriteLine("Any other switch displays this screen and exits.");
			t.WriteLine();
		}
		static bool _IsStale(string inputfile,string outputfile)
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
