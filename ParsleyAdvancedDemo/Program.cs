using System;
using System.Collections.Generic;
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
			Stream stm = null;
			// Slang doesn't understand the using directive
			try
			{
				// open this file
				stm = File.OpenRead(@"..\..\ParseNode.export.cs");
				// parse it
				var tokenizer = new SlangTokenizer(stm);
				var pn = SlangParser.Parse(tokenizer);
				// write the AST to the console
				var ccu = SlangParser.ToCompileUnit(pn);
				// patch it
				SlangPatcher.Patch(ccu);
				var co = SlangPatcher.GetNextUnresolvedElement(ccu);
				if(null!=co)
				{
					Console.WriteLine("Next unresolved code element:");
					Console.WriteLine(CodeDomUtility.ToString(co));
					var line = 0;
					var column = 0;
					var position = 0L;
					var o = co.UserData["slang:line"];
					if (null != o)
						line = (int)o;
					o = co.UserData["slang:column"];
					if (null != o)
						column = (int)o;
					o = co.UserData["slang:position"];
					if (null != o)
						position = (long)o;
					Console.WriteLine("at line {0}, column {1}, position {2}", line, column, position);
					
				} else
				{
					Console.WriteLine(CodeDomUtility.ToString(ccu));
				}
			} 
			finally
			{
				if (null != stm)
					stm.Close();
			}
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
