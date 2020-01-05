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
	
		static void _Main()
		{
			var text = "1.ToString(\"x2\")";
			//text = "try { a=0; } catch(SyntaxException ex) {a=1;} catch(IOException) {a=3;} finally {a=2;}";
			text = "[Foo(1,foo=2)] class Foo<[Foo] T,T2> : Bar, IBar where T:IComparable<T>,IEquatable<T> where T2:IComparable<T2>,new() { Foo() {Console.WriteLine(\"Hello World!\");}}";
			var tokenizer = new SlangTokenizer(text);
			var node = TypeDeclParser.ParseMember(tokenizer);
			_WriteTree(node, Console.Out);
			Console.WriteLine(CodeDomUtility.ToString(SlangParser.ToMember(node)));
			
		}
		
		static int Main()
		{
			Stream stm = null;
			// Slang doesn't understand the using directive
			ParseNode node;
			try
			{
				stm = File.OpenRead(@"..\..\Program.cs");
				var tokenizer = new SlangTokenizer(stm);
				node = SlangParser.Parse(tokenizer);
			}
			finally
			{
				if (null != stm)
					stm.Close();
			}
			_WriteTree(node, Console.Out);
			Console.WriteLine(CodeDomUtility.ToString(SlangParser.ToCompileUnit(node)));

			return 0;
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
