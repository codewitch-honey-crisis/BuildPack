using System.Text;
using System.Collections.Generic;
namespace ParsleyAdvancedDemo
{
	internal partial class ParseNode
	{
		private int _symbolId;
		private string _symbol;
		private string _value;
		private int _line;
		private int _column;
		private long _position;
		private ParseNode[] _children;
		public ParseNode(int symbolId, string symbol, ParseNode[] children, int line, int column, long position)
		{
			this._symbolId = symbolId;
			this._symbol = symbol;
			this._value = null;
			this._children = children;
			this._line = line;
			this._column = column;
			this._position = position;
		}
		public ParseNode(int symbolId, string symbol, string value, int line, int column, long position)
		{
			this._symbolId = symbolId;
			this._symbol = symbol;
			this._value = value;
			this._children = null;
			this._line = line;
			this._column = column;
			this._position = position;
		}
		public bool IsNonTerminal {
			get {
				return (null != this._children);
			}
		}
		public ParseNode[] Children {
			get {
				return this._children;
			}
		}
		public int SymbolId {
			get {
				return this._symbolId;
			}
		}
		public string Symbol {
			get {
				return this._symbol;
			}
		}
		public string Value {
			get {
				return this._value;
			}
		}
		public int Line {
			get {
				return this._line;
			}
		}
		public int Column {
			get {
				return this._column;
			}
		}
		public long Position {
			get {
				return this._position;
			}
		}
		public override string ToString()
		{
			return ToString(null);
		}
		public string ToString(string format)
		{
			if ("t" == format)
			{
				var sb = new StringBuilder();
				_AppendTree(this, sb);
				return sb.ToString();
			}
			if (IsNonTerminal)
				return string.Concat(Symbol, ": Count = ", _children.Length.ToString());
			return string.Concat(Symbol, ": ", Value);
		}
		static void _AppendTree(ParseNode node, StringBuilder builder)
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
					var indent = "";
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
					var ss = string.Concat(indent , "+- " , string.Concat(s , " " , ns));
					ss = ss.TrimEnd();
					builder.Append(ss);
					builder.AppendLine();
					if (node.IsNonTerminal && 0 < node.Children.Length)
					{
						var pnl = new List<ParseNode>(node.Children);
						childListStack.Add(pnl);
					}
				}
			}
		}
	}
}
