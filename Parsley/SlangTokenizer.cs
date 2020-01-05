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
public const int namespaceKeyword = 364;
public const int usingKeyword = 365;
public const int verbatimIdentifier = 366;
public const int outKeyword = 367;
public const int refKeyword = 368;
public const int typeOf = 369;
public const int nameOf = 370;
public const int defaultOf = 371;
public const int newKeyword = 372;
public const int stringType = 373;
public const int boolType = 374;
public const int charType = 375;
public const int floatType = 376;
public const int doubleType = 377;
public const int decimalType = 378;
public const int sbyteType = 379;
public const int byteType = 380;
public const int shortType = 381;
public const int ushortType = 382;
public const int intType = 383;
public const int uintType = 384;
public const int longType = 385;
public const int ulongType = 386;
public const int objectType = 387;
public const int boolLiteral = 388;
public const int nullLiteral = 389;
public const int thisRef = 390;
public const int baseRef = 391;
public const int verbatimStringLiteral = 392;
public const int identifier = 393;
public const int stringLiteral = 394;
public const int characterLiteral = 395;
public const int lte = 396;
public const int lt = 397;
public const int gte = 398;
public const int gt = 399;
public const int eqEq = 400;
public const int notEq = 401;
public const int eq = 402;
public const int inc = 403;
public const int addAssign = 404;
public const int add = 405;
public const int dec = 406;
public const int subAssign = 407;
public const int sub = 408;
public const int mulAssign = 409;
public const int mul = 410;
public const int divAssign = 411;
public const int div = 412;
public const int modAssign = 413;
public const int mod = 414;
public const int and = 415;
public const int bitwiseAndAssign = 416;
public const int bitwiseAnd = 417;
public const int or = 418;
public const int bitwiseOrAssign = 419;
public const int bitwiseOr = 420;
public const int not = 421;
public const int lbracket = 422;
public const int rbracket = 423;
public const int lparen = 424;
public const int rparen = 425;
public const int lbrace = 426;
public const int rbrace = 427;
public const int comma = 428;
public const int colonColon = 429;
public const int dot = 430;
public const int integerLiteral = 431;
public const int floatLiteral = 432;
public const int whitespace = 433;
public const int ifKeyword = 434;
public const int gotoKeyword = 435;
public const int elseKeyword = 436;
public const int forKeyword = 437;
public const int throwKeyword = 438;
public const int whileKeyword = 439;
public const int returnKeyword = 440;
public const int tryKeyword = 441;
public const int catchKeyword = 442;
public const int finallyKeyword = 443;
public const int semi = 444;
public const int varType = 445;
public const int colon = 446;
public const int directive = 447;
public const int lineComment = 448;
public const int blockComment = 449;
public const int assemblyKeyword = 450;
public const int voidType = 451;
public const int partialKeyword = 452;
public const int classKeyword = 453;
public const int enumKeyword = 454;
public const int structKeyword = 455;
public const int interfaceKeyword = 456;
public const int getKeyword = 457;
public const int setKeyword = 458;
public const int eventKeyword = 459;
public const int publicKeyword = 460;
public const int privateKeyword = 461;
public const int protectedKeyword = 462;
public const int internalKeyword = 463;
public const int staticKeyword = 464;
public const int abstractKeyword = 465;
public const int constKeyword = 466;
public const int overrideKeyword = 467;
public const int whereKeyword = 468;
public const int _EOS = 469;
public const int _ERROR = 470;

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
