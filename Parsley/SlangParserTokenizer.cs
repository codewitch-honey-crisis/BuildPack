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
public const int namespaceKeyword = 356;
public const int usingKeyword = 357;
public const int verbatimIdentifier = 358;
public const int outKeyword = 359;
public const int refKeyword = 360;
public const int typeOf = 361;
public const int nameOf = 362;
public const int defaultOf = 363;
public const int newKeyword = 364;
public const int stringType = 365;
public const int boolType = 366;
public const int charType = 367;
public const int floatType = 368;
public const int doubleType = 369;
public const int decimalType = 370;
public const int sbyteType = 371;
public const int byteType = 372;
public const int shortType = 373;
public const int ushortType = 374;
public const int intType = 375;
public const int uintType = 376;
public const int longType = 377;
public const int ulongType = 378;
public const int objectType = 379;
public const int boolLiteral = 380;
public const int nullLiteral = 381;
public const int thisRef = 382;
public const int baseRef = 383;
public const int verbatimStringLiteral = 384;
public const int identifier = 385;
public const int stringLiteral = 386;
public const int characterLiteral = 387;
public const int lte = 388;
public const int lt = 389;
public const int gte = 390;
public const int gt = 391;
public const int eqEq = 392;
public const int notEq = 393;
public const int eq = 394;
public const int inc = 395;
public const int addAssign = 396;
public const int add = 397;
public const int dec = 398;
public const int subAssign = 399;
public const int sub = 400;
public const int mulAssign = 401;
public const int mul = 402;
public const int divAssign = 403;
public const int div = 404;
public const int modAssign = 405;
public const int mod = 406;
public const int and = 407;
public const int bitwiseAndAssign = 408;
public const int bitwiseAnd = 409;
public const int or = 410;
public const int bitwiseOrAssign = 411;
public const int bitwiseOr = 412;
public const int not = 413;
public const int lbracket = 414;
public const int rbracket = 415;
public const int lparen = 416;
public const int rparen = 417;
public const int lbrace = 418;
public const int rbrace = 419;
public const int comma = 420;
public const int colonColon = 421;
public const int dot = 422;
public const int integerLiteral = 423;
public const int floatLiteral = 424;
public const int lineComment = 425;
public const int blockComment = 426;
public const int whitespace = 427;
public const int ifKeyword = 428;
public const int gotoKeyword = 429;
public const int elseKeyword = 430;
public const int forKeyword = 431;
public const int throwKeyword = 432;
public const int whileKeyword = 433;
public const int returnKeyword = 434;
public const int tryKeyword = 435;
public const int catchKeyword = 436;
public const int finallyKeyword = 437;
public const int semi = 438;
public const int varType = 439;
public const int colon = 440;
public const int directive = 441;
public const int assemblyKeyword = 442;
public const int voidType = 443;
public const int partialKeyword = 444;
public const int classKeyword = 445;
public const int enumKeyword = 446;
public const int structKeyword = 447;
public const int interfaceKeyword = 448;
public const int getKeyword = 449;
public const int setKeyword = 450;
public const int eventKeyword = 451;
public const int publicKeyword = 452;
public const int privateKeyword = 453;
public const int protectedKeyword = 454;
public const int internalKeyword = 455;
public const int staticKeyword = 456;
public const int abstractKeyword = 457;
public const int constKeyword = 458;
public const int overrideKeyword = 459;
public const int whereKeyword = 460;
public const int _EOS = 461;
public const int _ERROR = 462;

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
