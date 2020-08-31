using System;
using System.Collections.Generic;
using System.Text;

namespace Glory
{
	/// <summary>
	/// Represents a single token produced from a lexer/tokenizer, and consumed by a parser
	/// A token contains the symbol, the value, and the location information for each lexeme returned from a lexer/tokenizer
	/// </summary>
	struct Token
	{
		public string Symbol;
		public int SymbolId;
		public int Line;
		public int Column;
		public long Position;
		public string Value;

		public override string ToString()
		{
			return string.Concat(Symbol, "(", string.Concat(SymbolId.ToString(), ") : ", Value));
		}
	}
}
