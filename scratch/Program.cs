using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CD;
[assembly: scratch.Foo]


namespace scratch
{ 

	class Bar<[Foo] T> where T:IComparable<T>,new()
	{

	}
	// line comment
	/* block comment */

	[AttributeUsage(AttributeTargets.All)]
	class FooAttribute : System.Attribute
	{

	}
	/// <summary>
	/// MOAR COMMENTS (test Skipped)
	/// </summary>
	partial class Program
	{
		
		static void _Lex()
		{
			#region Fetch constants
			var consts = new Dictionary<int, string>();
			var fa = typeof(SlangTokenizer).GetFields(BindingFlags.Public | BindingFlags.Static);
			for (var i = 0; i < fa.Length; ++i)
			{
				var f = fa[i];
				if (f.FieldType == typeof(int) && f.IsLiteral)
				{
					var j = (int)f.GetValue(null);
					if (-3 < j)
						consts[j] = f.Name;
				}
			}
			#endregion

			Stream stm = null;
			try
			{
				stm = File.Open(@"..\..\..\Program.cs", FileMode.Open);
				var tokenizer = new SlangTokenizer(stm);
				var e = tokenizer.GetEnumerator();
				while (e.MoveNext())
					Console.WriteLine("{0}: {1} at line {2}", consts[e.Current.SymbolId], e.Current.Value, e.Current.Line);
			}
			finally
			{
				if (null != stm)
					stm.Close();
			}
		}
		[return: Foo]
		static int Main()
		{

			
			Stream stm = null;
			try
			{
				stm = File.OpenRead(@"..\..\..\Program.cs");
				var tokenizer = new SlangTokenizer(stm);
				var pt = SlangParser.Parse(tokenizer);
				_WriteTree(pt.Children[0], Console.Out);
			}
			finally
			{
				if (null != stm)
					stm.Close();
			}
			return 0;
		}
		static void _Statements()
		{
			var test = "a=1;int b =2;";
			var tokenizer = new SlangTokenizer(test);
			_WriteTree(StatementParser.ParseStatements(tokenizer), Console.Out);
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
							indent+= "   ";
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
