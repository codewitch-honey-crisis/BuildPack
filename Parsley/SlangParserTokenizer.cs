namespace CD
{
using System;
using System.Collections.Generic;
using System.IO;
	
	internal partial class SlangParserTokenizer : IEnumerable<Token>
	{
		// symbol constants follow
public const int ErrorSymbol = -1;
public const int EosSymbol = -2;
public const int namespaceKeyword = 279;
public const int usingKeyword = 280;
public const int voidType = 281;
public const int partialKeyword = 282;
public const int classKeyword = 283;
public const int enumKeyword = 284;
public const int structKeyword = 285;
public const int interfaceKeyword = 286;
public const int getKeyword = 287;
public const int setKeyword = 288;
public const int eventKeyword = 289;
public const int publicKeyword = 290;
public const int privateKeyword = 291;
public const int protectedKeyword = 292;
public const int internalKeyword = 293;
public const int staticKeyword = 294;
public const int abstractKeyword = 295;
public const int constKeyword = 296;
public const int overrideKeyword = 297;
public const int whereKeyword = 298;
public const int ifKeyword = 299;
public const int gotoKeyword = 300;
public const int elseKeyword = 301;
public const int forKeyword = 302;
public const int throwKeyword = 303;
public const int whileKeyword = 304;
public const int returnKeyword = 305;
public const int tryKeyword = 306;
public const int catchKeyword = 307;
public const int finallyKeyword = 308;
public const int semi = 309;
public const int varType = 310;
public const int colon = 311;
public const int verbatimIdentifier = 312;
public const int outKeyword = 313;
public const int refKeyword = 314;
public const int typeOf = 315;
public const int nameOf = 316;
public const int defaultOf = 317;
public const int newKeyword = 318;
public const int stringType = 319;
public const int boolType = 320;
public const int charType = 321;
public const int floatType = 322;
public const int doubleType = 323;
public const int decimalType = 324;
public const int sbyteType = 325;
public const int byteType = 326;
public const int shortType = 327;
public const int ushortType = 328;
public const int intType = 329;
public const int uintType = 330;
public const int longType = 331;
public const int ulongType = 332;
public const int objectType = 333;
public const int boolLiteral = 334;
public const int nullLiteral = 335;
public const int thisRef = 336;
public const int baseRef = 337;
public const int verbatimStringLiteral = 338;
public const int identifier = 339;
public const int stringLiteral = 340;
public const int characterLiteral = 341;
public const int lte = 342;
public const int lt = 343;
public const int gte = 344;
public const int gt = 345;
public const int eqEq = 346;
public const int notEq = 347;
public const int eq = 348;
public const int inc = 349;
public const int addAssign = 350;
public const int add = 351;
public const int dec = 352;
public const int subAssign = 353;
public const int sub = 354;
public const int mulAssign = 355;
public const int mul = 356;
public const int divAssign = 357;
public const int div = 358;
public const int modAssign = 359;
public const int mod = 360;
public const int and = 361;
public const int bitwiseAndAssign = 362;
public const int bitwiseAnd = 363;
public const int or = 364;
public const int bitwiseOrAssign = 365;
public const int bitwiseOr = 366;
public const int not = 367;
public const int lbracket = 368;
public const int rbracket = 369;
public const int lparen = 370;
public const int rparen = 371;
public const int lbrace = 372;
public const int rbrace = 373;
public const int comma = 374;
public const int colonColon = 375;
public const int dot = 376;
public const int directive = 377;
public const int integerLiteral = 378;
public const int floatLiteral = 379;
public const int lineComment = 380;
public const int blockComment = 381;
public const int whitespace = 382;
public const int _EOS = 383;
public const int _ERROR = 384;

		string _fallbackCodePage;
		Stream _input;
		string _inputText;
		bool _called; // we can only return an enumerator once if there is no backing file
		public SlangParserTokenizer(Stream input, string fallbackCodePage = null)
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
		public SlangParserTokenizer(string text)
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
				var result = new SlangParserTokenizerEnumerator(_input, _fallbackCodePage);
				_called = true;
				return result;
			}
			return new SlangParserTokenizerEnumerator(_inputText);
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
	internal partial class SlangParserTokenizerEnumerator : IEnumerator<Token>
	{
		const int _Error = -1;// scan error
		const int _Eos = -2; // end of input
		const int _Disposed = -3; // Dispose() has been called
									// -4 or any other neg value other than above = _Initial
									// non-negative (0) = _Enumerating

		Scanner _outer;
		public SlangParserTokenizerEnumerator(Stream input, string fallbackCodePage)
		{
			_outer = new Scanner(input, fallbackCodePage);
		}
		public SlangParserTokenizerEnumerator(string text)
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
