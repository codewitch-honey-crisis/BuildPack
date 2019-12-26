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
public const int verbatimIdentifier = 177;
public const int outKeyword = 150;
public const int refKeyword = 151;
public const int typeOf = 204;
public const int nameOf = 205;
public const int defaultOf = 206;
public const int newObj = 202;
public const int stringType = 154;
public const int boolType = 152;
public const int charType = 153;
public const int floatType = 155;
public const int doubleType = 156;
public const int decimalType = 157;
public const int sbyteType = 158;
public const int byteType = 159;
public const int shortType = 160;
public const int ushortType = 161;
public const int intType = 162;
public const int uintType = 163;
public const int longType = 164;
public const int ulongType = 165;
public const int objectType = 166;
public const int boolLiteral = 212;
public const int nullLiteral = 176;
public const int thisRef = 213;
public const int baseRef = 214;
public const int verbatimStringLiteral = 207;
public const int identifier = 178;
public const int stringLiteral = 211;
public const int characterLiteral = 208;
public const int lte = 180;
public const int lt = 179;
public const int gte = 182;
public const int gt = 181;
public const int eqEq = 183;
public const int notEq = 184;
public const int eq = 189;
public const int inc = 174;
public const int addAssign = 190;
public const int add = 171;
public const int dec = 175;
public const int subAssign = 191;
public const int sub = 172;
public const int mulAssign = 192;
public const int mul = 197;
public const int divAssign = 193;
public const int div = 198;
public const int modAssign = 194;
public const int mod = 199;
public const int and = 187;
public const int bitwiseAndAssign = 195;
public const int bitwiseAnd = 185;
public const int or = 188;
public const int bitwiseOrAssign = 196;
public const int bitwiseOr = 186;
public const int not = 173;
public const int lbracket = 169;
public const int rbracket = 201;
public const int lparen = 168;
public const int rparen = 170;
public const int lbrace = 215;
public const int rbrace = 203;
public const int comma = 167;
public const int colonColon = -1;
public const int colon = -1;
public const int semi = -1;
public const int dot = 149;
public const int integerLiteral = 209;
public const int floatLiteral = 210;
public const int directive = 216;
public const int lineComment = 217;
public const int blockComment = 218;
public const int whitespace = 219;

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
