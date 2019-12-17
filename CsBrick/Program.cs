using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.XPath;

namespace CSBrick
{
	class Program
	{
		static readonly string _File = Assembly.GetEntryAssembly().GetModules()[0].Name;
		static readonly string _Name = _GetName();
		
		static int Main(string[] args)
		{
			int result = 0;
			TextWriter output = null;

			string inputfile = null;
			string outputfile = null;
			int linewidth = 150;
			bool definefiles = false;
			bool ifstale = false;
			
			HashSet<string> exclude = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			exclude.Add(@"Properties\AssemblyInfo.cs");
			try
			{
				if (0 == args.Length)
					throw new ArgumentException("No arguments specified");
				if (args[0].StartsWith("/"))
					throw new ArgumentException("Missing input file.");
				
				// process the command line args
				inputfile = args[0];
				for (var i = 1; i < args.Length; ++i)
				{
					switch (args[i])
					{
						case "/output":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							outputfile = args[i];
							break;
						case "/ifstale":
							ifstale = true;
							break;
						case "/definefiles":
							definefiles = true;
							break;
						case "/linewidth":
							if (args.Length - 1 == i) // check if we're at the end
								throw new ArgumentException(string.Format("The parameter \"{0}\" is missing an argument", args[i].Substring(1)));
							++i; // advance 
							linewidth=int.Parse(args[i]);
							break;
						case "/exclude":
							++i;
							exclude.Clear();
							while (i < args.Length && !args[i].StartsWith("/"))
							{
								exclude.Add(args[i]);
								++i;
							}
							break;
						default:
							throw new ArgumentException(string.Format("Unknown switch {0}", args[i]));
					}
				}
				var inputs = _GetInputs(inputfile,exclude);
				var stale = true;
				if (ifstale && null != outputfile)
				{
					stale = false;
					if (_IsStale(inputfile, outputfile))
					{
						stale = true;
					}
					else
					{
						foreach (var f in inputs)
						{
							if (_IsStale(f, outputfile))
							{
								stale = true;
								break;
							}
						}
					}
				}

				if (!stale)
				{
					Console.Error.WriteLine("{0} skipped merge of {1} because it was not stale.", _Name, outputfile);
				}
				else
				{
					if (null != outputfile)
					{
						var sw = new StreamWriter(outputfile);
						sw.BaseStream.SetLength(0);
						output = sw;
					}
					else
						output = Console.Out;
						Minifier.MergeMinify(output, linewidth, definefiles,
							inputs.ToArray());
					output.Flush();
					
				}
			}
#if !DEBUG
			catch (Exception ex)
			{
				result = _ReportError(ex);
			}
#endif
			finally {
				if(outputfile!=null && null!=output)
				{
					output.Close();
				}

			}
			return result;
		}
		static List<string> _GetInputs(string projectFile,HashSet<string> excludedFiles)
		{
			string ns = null;
			var isSdkProj = false;
			using (var r = XmlReader.Create(projectFile))
			{
				while (r.Read() && XmlNodeType.Element != r.NodeType) ;
				if (XmlNodeType.Element != r.NodeType)
					throw new IOException("The project file does not contain a valid project.");
				ns = r.NamespaceURI;
				var s = r.GetAttribute("Sdk");
				if (!string.IsNullOrEmpty(s))
					isSdkProj = true;
			}
			var result = new List<string>();
			if (!isSdkProj)
			{
				using (var r = XmlReader.Create(projectFile))
				{
					var d = new XPathDocument(r);
					var nav = d.CreateNavigator();
					var res = new XmlNamespaceManager(nav.NameTable);
					res.AddNamespace("e", ns);
					var iter = nav.Select("/e:Project/e:ItemGroup/e:Compile/@Include", res);
					while (iter.MoveNext())
						result.Add(iter.Current.Value);
				}
				foreach (var e in excludedFiles)
				{
					var s = e;
					if (!Path.IsPathRooted(s))
						s = Path.Combine(Path.GetDirectoryName(projectFile), e);
					try
					{
						// in case it doesn't exist
						s = Path.GetFullPath(s);
					}
					catch { }
					for (int ic = result.Count, i = 0; i < ic; ++i)
					{
						var sc = result[i];
						if(!Path.IsPathRooted(sc))
							sc = Path.Combine(Path.GetDirectoryName(projectFile), sc);
						try
						{
							sc = Path.GetFullPath(sc);
						}
						catch { }
						if (0 == string.Compare(s, sc))
						{
							result.RemoveAt(i);
							--i;
							--ic;
						}
					}
				}
			} else // Core or Standard project
			{
				var dir = Path.GetDirectoryName(projectFile);
				var files = Directory.GetFiles(dir, "*.cs");
				for(var i = 0;i<files.Length;++i)
				{
					var f = Path.Combine(dir,files[i]);
					f = Path.GetFullPath(f);
					result.Add(f);
				}
				foreach (string d in Directory.GetDirectories(dir))
				{
					_DirSearch(dir, d, result);
				}
				using (var r = XmlReader.Create(projectFile))
				{
					var d = new XPathDocument(r);
					var nav = d.CreateNavigator();
					var res = new XmlNamespaceManager(nav.NameTable);
					res.AddNamespace("e", ns);
					var iter = nav.Select("/e:Project/e:ItemGroup/e:Compile/@Remove", res);
					while (iter.MoveNext())
					{
						var s = iter.Current.Value;
						s = Path.Combine(Path.GetDirectoryName(projectFile), s);
						s = Path.GetFullPath(s);
						result.Remove(s);
					}
					iter = nav.Select("/e:Project/e:ItemGroup/e:Compile/@Include", res);
					while (iter.MoveNext())
					{
						var s = iter.Current.Value;
						result.Add(s);
					}
				}
				foreach(var e in excludedFiles)
				{
					var s = e;
					if (!Path.IsPathRooted(s))
						s = Path.Combine(Path.GetDirectoryName(projectFile), e);
					try
					{
						// in case it doesn't exist
						s = Path.GetFullPath(s);
					}
					catch { }
					for(int ic=result.Count,i=0;i<ic;++i)
					{
						var sc = result[i];
						if (!Path.IsPathRooted(sc))
							sc = Path.Combine(Path.GetDirectoryName(projectFile), sc);
						try
						{
							sc = Path.GetFullPath(sc);
						}
						catch { }
						if (0==string.Compare(s,sc))
						{
							result.RemoveAt(i);
							--i;
							--ic;
						}
					}
				}
			}
			if (0 == result.Count)
				throw new IOException("The project file does not contain any valid source files.");
			for (int ic = result.Count, i = 0; i < ic; ++i)
			{
				var s = result[i];
				if (!Path.IsPathRooted(s))
				{
					s = Path.Combine(Path.GetDirectoryName(projectFile), s);
				}
				try
				{
					s = Path.GetFullPath(s);
				}
				catch { }
				result[i] = s;
			}
			return result;
		}
		static void _DirSearch(string root,string currentDir,IList<string> result,bool first=true)
		{
			
			foreach (string d in Directory.GetDirectories(currentDir))
			{
				foreach (string f in Directory.GetFiles(d,"*.cs"))
				{
					result.Add(f);
				}
				var s = currentDir;
				var i = s.LastIndexOfAny(new char[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar });
				if (-1 < i)
					s = s.Substring(i + 1);
				if (!first || (0 != string.Compare(s, "bin") && 0 != string.Compare(s, "obj")))
					_DirSearch(root,d,result,false);
			}
			
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
			t.WriteLine("{0} merges and minifies C# project source files", _Name);
			t.WriteLine();
			t.Write(_File);
			t.WriteLine(" <inputfile> [/output <outputfile>] [/ifstale] [/exclude [<exclude1> <exclude2> ... <excludeN>]]");
			t.WriteLine("   [/linewidth <linewidth>] [/definefiles]");
			t.WriteLine();
			t.WriteLine("   <inputfile>     The input project file to use.");
			t.WriteLine("   <outputfile>    The output file to use - default stdout.");
			t.WriteLine("	<ifstale>       Do not generate unless <outputfile> is older than <inputfile> or its associated files.");
			t.WriteLine("	<exclude>       Exclude the specified file(s) from the output. - defaults to \"Properties\\AssemblyInfo.cs\"");
			t.WriteLine("   <linewidth>     After the width is hit, break the line at the next opportunity - defaults to 150");
			t.WriteLine("   <definefiles>   Insert #define decls for every file included");
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
