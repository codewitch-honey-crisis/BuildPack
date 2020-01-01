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
public const int namespaceKeyword = 357;
public const int usingKeyword = 358;
public const int verbatimIdentifier = 359;
public const int outKeyword = 360;
public const int refKeyword = 361;
public const int typeOf = 362;
public const int nameOf = 363;
public const int defaultOf = 364;
public const int newKeyword = 365;
public const int stringType = 366;
public const int boolType = 367;
public const int charType = 368;
public const int floatType = 369;
public const int doubleType = 370;
public const int decimalType = 371;
public const int sbyteType = 372;
public const int byteType = 373;
public const int shortType = 374;
public const int ushortType = 375;
public const int intType = 376;
public const int uintType = 377;
public const int longType = 378;
public const int ulongType = 379;
public const int objectType = 380;
public const int boolLiteral = 381;
public const int nullLiteral = 382;
public const int thisRef = 383;
public const int baseRef = 384;
public const int verbatimStringLiteral = 385;
public const int identifier = 386;
public const int stringLiteral = 387;
public const int characterLiteral = 388;
public const int lte = 389;
public const int lt = 390;
public const int gte = 391;
public const int gt = 392;
public const int eqEq = 393;
public const int notEq = 394;
public const int eq = 395;
public const int inc = 396;
public const int addAssign = 397;
public const int add = 398;
public const int dec = 399;
public const int subAssign = 400;
public const int sub = 401;
public const int mulAssign = 402;
public const int mul = 403;
public const int divAssign = 404;
public const int div = 405;
public const int modAssign = 406;
public const int mod = 407;
public const int and = 408;
public const int bitwiseAndAssign = 409;
public const int bitwiseAnd = 410;
public const int or = 411;
public const int bitwiseOrAssign = 412;
public const int bitwiseOr = 413;
public const int not = 414;
public const int lbracket = 415;
public const int rbracket = 416;
public const int lparen = 417;
public const int rparen = 418;
public const int lbrace = 419;
public const int rbrace = 420;
public const int comma = 421;
public const int colonColon = 422;
public const int dot = 423;
public const int integerLiteral = 424;
public const int floatLiteral = 425;
public const int whitespace = 426;
public const int ifKeyword = 427;
public const int gotoKeyword = 428;
public const int elseKeyword = 429;
public const int forKeyword = 430;
public const int throwKeyword = 431;
public const int whileKeyword = 432;
public const int returnKeyword = 433;
public const int tryKeyword = 434;
public const int catchKeyword = 435;
public const int finallyKeyword = 436;
public const int semi = 437;
public const int varType = 438;
public const int colon = 439;
public const int directive = 440;
public const int lineComment = 441;
public const int blockComment = 442;
public const int assemblyKeyword = 443;
public const int voidType = 444;
public const int partialKeyword = 445;
public const int classKeyword = 446;
public const int enumKeyword = 447;
public const int structKeyword = 448;
public const int interfaceKeyword = 449;
public const int getKeyword = 450;
public const int setKeyword = 451;
public const int eventKeyword = 452;
public const int publicKeyword = 453;
public const int privateKeyword = 454;
public const int protectedKeyword = 455;
public const int internalKeyword = 456;
public const int staticKeyword = 457;
public const int abstractKeyword = 458;
public const int constKeyword = 459;
public const int overrideKeyword = 460;
public const int whereKeyword = 461;
public const int _EOS = 462;
public const int _ERROR = 463;

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
