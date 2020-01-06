// TODO: Load this into Slang so we can resolve Program.cs
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
			if (this.IsNonTerminal)
			{
				return string.Concat(this.Symbol, ": Count = ", this._children.Length.ToString());
			}
			return string.Concat(this.Symbol, ": ", this.Value);
		}
	}
}
