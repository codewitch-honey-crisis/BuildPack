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
public const int outKeyword = 77;
public const int refKeyword = 78;
public const int verbatimIdentifier = 106;
public const int typeOf = 104;
public const int nameOf = 105;
public const int newObj = 114;
public const int stringType = 81;
public const int boolType = 79;
public const int charType = 80;
public const int floatType = 82;
public const int doubleType = 83;
public const int decimalType = 84;
public const int sbyteType = 85;
public const int byteType = 86;
public const int shortType = 87;
public const int ushortType = 88;
public const int intType = 89;
public const int uintType = 90;
public const int longType = 91;
public const int ulongType = 92;
public const int objectType = 93;
public const int verbatimStringLiteral = 100;
public const int identifier = 107;
public const int stringLiteral = 103;
public const int lineComment = 119;
public const int blockComment = 120;
public const int characterLiteral = -1;
public const int whitespace = 121;
public const int lte = -1;
public const int lt = 116;
public const int gte = -1;
public const int gt = 113;
public const int eqEq = -1;
public const int notEq = -1;
public const int eq = 117;
public const int inc = -1;
public const int addAssign = -1;
public const int add = 98;
public const int dec = -1;
public const int subAssign = -1;
public const int sub = 99;
public const int mulAssign = -1;
public const int mul = 108;
public const int divAssign = -1;
public const int div = 109;
public const int modAssign = -1;
public const int mod = 110;
public const int and = -1;
public const int bitwiseAndAssign = -1;
public const int bitwiseAnd = -1;
public const int or = -1;
public const int bitwiseOrAssign = -1;
public const int bitwiseOr = -1;
public const int not = -1;
public const int lbracket = 96;
public const int rbracket = 112;
public const int lparen = 95;
public const int rparen = 97;
public const int lbrace = 118;
public const int rbrace = 115;
public const int comma = 94;
public const int colonColon = -1;
public const int colon = -1;
public const int semi = -1;
public const int dot = 76;
public const int integerLiteral = 101;
public const int floatLiteral = 102;
public const int directive = 122;

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
