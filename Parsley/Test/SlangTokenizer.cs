namespace CD
{
using System;
using System.Collections.Generic;
using System.IO;
	
	internal partial class SlangTokenizer : IEnumerable<Token>
	{
		// symbol constants follow
public const int ErrorSymbol = -1;
public const int EosSymbol = -2;
public const int verbatimIdentifier = 0;
public const int keyword = 1;
public const int identifier = 2;
public const int lineComment = 3;
public const int blockComment = 4;
public const int verbatimStringLiteral = 5;
public const int stringLiteral = 6;
public const int characterLiteral = 7;
public const int whitespace = 8;
public const int lte = 9;
public const int lt = 10;
public const int gte = 11;
public const int gt = 12;
public const int eqEq = 13;
public const int notEq = 14;
public const int eq = 15;
public const int inc = 16;
public const int addAssign = 17;
public const int add = 18;
public const int dec = 19;
public const int subAssign = 20;
public const int sub = 21;
public const int mulAssign = 22;
public const int mul = 23;
public const int divAssign = 24;
public const int div = 25;
public const int modAssign = 26;
public const int mod = 27;
public const int and = 28;
public const int bitwiseAndAssign = 29;
public const int bitwiseAnd = 30;
public const int or = 31;
public const int bitwiseOrAssign = 32;
public const int bitwiseOr = 33;
public const int not = 34;
public const int lbracket = 35;
public const int rbracket = 36;
public const int lparen = 37;
public const int rparen = 38;
public const int lbrace = 39;
public const int rbrace = 40;
public const int comma = 41;
public const int colonColon = 42;
public const int colon = 43;
public const int semi = 44;
public const int dot = 45;
public const int integerLiteral = 46;
public const int floatLiteral = 47;
public const int directive = 48;

		string _fallbackCodePage;
		Stream _input;
		string _inputText;
		bool _called; // we can only return an enumerator once if there is no backing file
		public SlangTokenizer(Stream input, string fallbackCodePage = null)
		{
			if (null == input)
				throw new ArgumentNullException(nameof(input));
			if (null == fallbackCodePage)
				fallbackCodePage = "default";
			_fallbackCodePage = fallbackCodePage;
			_input = input;
			_inputText = null;
			_called = false;
		}
		public SlangTokenizer(string text)
		{
			if (null == text)
				throw new ArgumentNullException("text");
			_input = null;
			_inputText = text;
		}
		public IEnumerator<Token> GetEnumerator()
		{
			if (null != _input)
			{
				if (_called)
					throw new NotSupportedException("A stream cannot support multiple cursors. A new enumerator cannot be returned.");
				var result = new SlangTokenizerEnumerator(_input, _fallbackCodePage);
				_called = true;
				return result;
			}
			return new SlangTokenizerEnumerator(_inputText);
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
	internal partial class SlangTokenizerEnumerator : IEnumerator<Token>
	{
		const int _Error = -1;// scan error
		const int _Eos = -2; // end of input
		const int _Disposed = -3; // Dispose() has been called
									// -4 or any other neg value other than above = _Initial
									// non-negative (0) = _Enumerating

		Scanner _outer;
		public SlangTokenizerEnumerator(Stream input, string fallbackCodePage)
		{
			_outer = new Scanner(input, fallbackCodePage);
		}
		public SlangTokenizerEnumerator(string text)
		{
			_outer = new Scanner(MemoryStream.Null,"default");
			_outer.SetSource(text,0);
		}
		Token IEnumerator<Token>.Current => _GetCurrent();
		object System.Collections.IEnumerator.Current => _GetCurrent();
		Token _GetCurrent()
		{
			var cur = _outer.Current;
			#region Error Handling
			if (0 > cur.SymbolId)
			{
				switch (cur.SymbolId)
				{
					case _Disposed:
						throw new ObjectDisposedException(GetType().Name);
					case _Eos:
						throw new InvalidOperationException("The cursor is after the end of the enumeration");
					case _Error: // we need to report this so just break
						break;
					default: // initial state
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
				}
			}
			#endregion Error Handling
			return cur;
		}
		bool System.Collections.IEnumerator.MoveNext()
		{
			var cur = _outer.Current;
			if (_Disposed == cur.SymbolId)
				throw new ObjectDisposedException(GetType().Name);
			if (_Eos == cur.SymbolId)
				return false;
			_outer.Advance();
			return _Eos != _outer.Current.SymbolId;	
		}
		void System.Collections.IEnumerator.Reset()
		{
			throw new NotSupportedException("Gplex tokenizers cannot be reset.");
		}
		void IDisposable.Dispose()
		{
			_outer.Close();
		}
	}

}
