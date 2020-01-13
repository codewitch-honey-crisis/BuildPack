using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using CD;
[assembly: Foo]
class FooAttribute : Attribute
{

}
class Foo<T, T2> where T : IComparable<T>, IEquatable<T> where T2 : IComparable<T2>
{
	
}
namespace ParsleyAdvancedDemo
{
	
	partial class Program
	{
	
		static void Main()
		{
			Demo2();
		}
		static void Demo3()
		{
			Stream stm = null;
			CodeCompileUnit ccu = null;
			try
			{
				stm = File.OpenRead(@"..\..\Test\PatchTest.cs");
				var tok = new SlangTokenizer(stm);
				ccu = SlangParser.ToCompileUnit(SlangParser.Parse(tok));
			}
			finally
			{
				stm.Close();
			}
			SlangPatcher.Patch(ccu);
			var co = SlangPatcher.GetNextUnresolvedElement(ccu);
			if (null != co)
			{
				Console.WriteLine("Next unresolved element is:");
				Console.WriteLine(CodeDomUtility.ToString(co).TrimEnd());
			}
			else
			{
				Console.WriteLine(CodeDomUtility.ToString(ccu));
			}
		}
		static void Demo2()
		{
			var files = Directory.GetFiles(@"..\..\Test", "*.cs");
			for(var i = 0;i<files.Length;++i)
			{
				_Test(files[i]);
			}
		}
		
		static void _Test(string file)
		{
			Console.WriteLine("Parsing file: " + file);

			// don't read directly from the file for perf testing.
			StreamReader sr = null;
			string text = null;
			try
			{
				sr = new StreamReader(file);
				text = sr.ReadToEnd();
			}
			finally
			{
				if (null != sr)
					sr.Close();
			}
			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < 100; ++i)
			{
				var tok = new SlangTokenizer(text);
				var pc = new ParserContext(tok);
				pc.EnsureStarted();
				CodeObject co = SlangParser.ToCompileUnit(SlangParser.ParseCompileUnit(pc));
			}
			sw.Stop();
			Console.WriteLine("Parsed " + Path.GetFileName(file) + " in " + (sw.ElapsedMilliseconds / 100d) + " msec");

			//Console.WriteLine(CodeDomUtility.ToString(co).TrimEnd());
		}
		static void Demo1()
		{
			var text = "using System;" +
				"class Program {" +
					"static void Main() {" +
						"Console.WriteLine(\"Hello World!\");" +
					"}" +
				"}";

			var tokenizer = new SlangTokenizer(text);
			// parse the above
			var pn = SlangParser.Parse(tokenizer);
			// write the tree
			Console.WriteLine(pn.ToString("t"));
			// build our AST
			var ccu=SlangParser.ToCompileUnit(pn);
			// now write our compile unit AST to the console
			Console.WriteLine(CodeDomUtility.ToString(ccu));
			Console.Write("Press any key...");
			Console.ReadKey();
			Console.Clear();

			Stream stm = null;
			// Slang doesn't understand the using directive
			try
			{
				// open this file
				stm = File.OpenRead(@"..\..\Program.cs");
				// parse it
				tokenizer = new SlangTokenizer(stm);				
				pn = SlangParser.Parse(tokenizer);
				// write the AST to the console
				ccu = SlangParser.ToCompileUnit(pn);
				Console.WriteLine(CodeDomUtility.ToString(ccu));

			}
			finally
			{
				if (null != stm)
					stm.Close();
			}
		}
		
		static void _WriteTree(ParseNode node, TextWriter writer)
		{
			// adapted from https://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure
			var firstStack = new List<ParseNode>();
			firstStack.Add(node);

			var childListStack = new List<List<ParseNode>>();
			childListStack.Add(firstStack);

			while (childListStack.Count > 0)
			{
				var childStack = childListStack[childListStack.Count - 1];

				if (childStack.Count == 0)
				{
					childListStack.RemoveAt(childListStack.Count - 1);
				}
				else
				{
					node = childStack[0];
					childStack.RemoveAt(0);
					string indent = "";
					for (int i = 0; i < childListStack.Count - 1; ++i)
					{
						if (0 < childListStack[i].Count)
							indent += "|  ";
						else
							indent += "   ";
					}
					var s = node.Symbol;
					var ns = "";
					if (null != node.Value)
						ns = node.Value;
					writer.Write(string.Concat(indent, "+- ", s, " ", ns).TrimEnd());
					writer.WriteLine();// string.Concat(" at line ", node.Line, ", column ", node.Column, ", position ", node.Position, ", length of ", node.Length));
					if (node.IsNonTerminal && 0 < node.Children.Length)
					{
						childListStack.Add(new List<ParseNode>(node.Children));
					}
				}
			}
		}
		
	}
}
