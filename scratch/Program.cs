using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CD;
namespace scratch
{
	// line comment
	/* block comment */
	class Program
	{
		static void Main()
		{
			var tokenizer = new SlangTokenizer("new object[] = { new object() }");
			var pt = SlangParser.Parse(tokenizer);
			_WriteTree(pt, Console.Out);
		}
		
		static void _Lex()
		{
			
			#region fetch consts
			var consts = new Dictionary<int, string>();
			foreach (var f in typeof(SlangTokenizer).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if(f.FieldType==typeof(int) && f.IsLiteral)
				{
					var i = (int)f.GetValue(null);
					if(-3<i)
						consts[i]= f.Name;
				}
			}
			#endregion Fetch consts
			using (var stm = File.Open(@"..\..\..\Program.cs", FileMode.Open))
			{
				
				var tokenizer = new SlangTokenizer(stm);
				foreach (var tok in tokenizer)
					Console.WriteLine("{0}: {1}", consts[tok.SymbolId], tok.Value);
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
					for (int i = 0; i < childListStack.Count - 1; i++)
					{
						indent += (childListStack[i].Count > 0) ? "|  " : "   ";
					}
					var s = node.Symbol;
					writer.Write(string.Concat(indent, "+- ", s, " ", node.Value ?? "").TrimEnd());
					writer.WriteLine();// string.Concat(" at line ", node.Line, ", column ", node.Column, ", position ", node.Position, ", length of ", node.Length));
					if (node.IsNonTerminal && 0 < node.Children.Length)
					{
						for(var i = 0;i<node.Children.Length;i++)
						{
							if (null == node.Children[i])
								System.Diagnostics.Debugger.Break();
						}
						childListStack.Add(new List<ParseNode>(node.Children));
					}
				}
			}
		}
	}
}
