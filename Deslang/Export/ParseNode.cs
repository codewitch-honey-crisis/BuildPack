using System;
using System.Collections.Generic;
using System.Text;
namespace Glory
{
	partial class ParseNode
	{
		private int _symbolId;
		private string _symbol;
		private string _value;
		private int _line;
		private int _column;
		private long _position;
		private ParseNode[] _children;
		private ParseAttribute[] _attributes;
		public ParseNode(int symbolId, string symbol, ParseNode[] children,string value, ParseAttribute[] attributes, int line, int column, long position)
		{
			_symbolId = symbolId;
			_symbol = symbol;
			_value = null;
			_children = children;
			_value = value;
			_attributes = attributes;
			_line = line;
			_column = column;
			_position = position;
		}
		
		public bool IsNonTerminal { get { return null != _children; } }

		public ParseNode[] Children {
			get {
				return _children;
			}
		}
		public int SymbolId {
			get {
				return _symbolId;
			}
		}
		public string Symbol {
			get {
				return _symbol;
			}
		}
		public string Value {
			get {
				return _value;
			}
		}
		public int Line {
			get {
				return _line;
			}
		}
		public int Column {
			get {
				return _column;
			}
		}
		public long Position {
			get {
				return _position;
			}
		}
		public ParseAttribute[] Attributes {
			get {
				return _attributes;
			}
		}
		public bool IsCollapsed {
			get {
				for(var i =0;i<_attributes.Length;++i)
				{
					var a = _attributes[i];
					if("collapsed"==a.Name)
					{
						try
						{
							return (bool)a.Value;
						}
						catch(Exception) { return false; }
					}
				}
				return false;
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
		static void _AppendTree(ParseNode node, System.Text.StringBuilder builder)
		{
			// adapted from https://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure
			List<ParseNode> firstStack = new List<ParseNode>();
			firstStack.Add(node);

			List<List<ParseNode>> childListStack = new List<List<ParseNode>>();
			childListStack.Add(firstStack);

			while (childListStack.Count > 0)
			{
				List<ParseNode> childStack = childListStack[childListStack.Count - 1];

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
					var ss = string.Concat(indent, "+- ", string.Concat(s, " ", ns));
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
