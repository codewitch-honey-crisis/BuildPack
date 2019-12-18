using System.Collections.Generic;

namespace Parsley
{
	partial class ParseNode
	{
		int _symbolId;
		string _value;
		ParseNode[] _children;
		public ParseNode(int symbolId,ParseNode[] children)
		{
			_symbolId = symbolId;
			_value = null;
			_children = children;
		}
		public ParseNode(int symbolId, string value)
		{
			_symbolId = symbolId;
			_value = value;
			_children = null;
		}
		public bool IsNonTerminal { get { return null != _children; } }

		public IList<ParseNode> Children {
			get {
				return _children;
			}
		}
		public int SymbolId {
			get {
				return _symbolId;
			}
		}
		public string Value {
			get {
				return _value;
			}
		}
	}
}
