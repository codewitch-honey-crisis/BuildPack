using System.CodeDom;
using System.Collections.Generic;

namespace CD
{
	using ST = SlangTokenizer;
	/// <summary>
	/// Represents the parser for parsing Slang, a C# subset, into a CodeDOM structure
	/// </summary>
#if GOKITLIB
	public 
#endif
	static partial class SlangParser
	{
		// a minature parse context used to simplify recursive descent parsing
		private class _PC
		{
			readonly LookAheadEnumerator<Token> _e;
			int _state;
			public _PC(IEnumerator<Token> e)
			{
				_e = new LookAheadEnumerator<Token>(e);
				_state = -2;
			}
			
			public _PC GetLookAhead()
			{
				var e = new ConcatEnumerator<Token>(new Token[] { _e.Current }, _e.LookAhead);
				return new _PC(e);
			}
			public int SymbolId => Current.SymbolId;
			public string Value => Current.Value;
			public Token Current {
				get {
					if (0 > _state)
					{
						var t = default(Token);
						t.SymbolId = -1 == _state ? -2 : -1;
						return t;
					}
					return _e.Current;
				}
			}
			public void EnsureStarted()
			{
				if (-2 == _state)
					Advance();
			}
			public bool IsEnded { get { return -1 == _state; } }
			public bool Advance()
			{
				if (!_e.MoveNext())
					_state = -1;
				else
				{
					_state = 0;
					return true;
				}
				return false;
			}
		}
		static void _SkipComments(_PC pc)
		{
			Token t;
			while ((ST.blockComment == (t = pc.Current).SymbolId ||
					ST.lineComment == t.SymbolId) && pc.Advance()) ;
		}
		static void _Error(string message,Token tok)
		{
			throw new SlangSyntaxException(message, tok.Line, tok.Column, tok.Position);
		}
	}
}
