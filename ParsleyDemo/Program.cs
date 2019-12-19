using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace ParsleyDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			var text = "3*5+7*2"; 
			var exprTokenizer = new ExpressionTokenizer(text);
			var pt = ExpressionParser.ParseExpression(exprTokenizer);
			Console.WriteLine("{0} = {1}",text,ExpressionParser.EvaluateExpression(pt));
			Console.WriteLine();
			_WriteTree(pt, Console.Out);
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			using (var sw = File.OpenText(@"..\..\data.json"))
				text = sw.ReadToEnd();
			var jsonTokenizer = new JsonTokenizer(text);
			pt = JsonParser.ParseJson(jsonTokenizer);
			_WriteTree(pt, Console.Out);
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			return;

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
					if (node.IsNonTerminal && 0<node.Children.Length)
					{
						childListStack.Add(new List<ParseNode>(node.Children));
					}
				}
			}
		}
	}
}
