using System.Collections.Generic;

namespace Parsley
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
		public ParseNode(int symbolId, string symbol,ParseNode[] children,int line,int column,long position)
		{
			_symbolId = symbolId;
			_symbol = symbol;
			_value = null;
			_children = children;
			_line = line;
			_column = column;
			_position = position;
		}
		public ParseNode(int symbolId, string symbol,string value,int line, int column, long position)
		{
			_symbolId = symbolId;
			_symbol = symbol;
			_value = value;
			_children = null;
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
	}
}
