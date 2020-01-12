namespace Slang
{
using System;
using System.Collections.Generic;
using System.IO;
	
	internal partial class SlangTokenizer : IEnumerable<Token>
	{
		// symbol constants follow
public const int ErrorSymbol = -1;
public const int EosSymbol = -2;
public const int namespaceKeyword = 396;
public const int usingKeyword = 397;
public const int verbatimIdentifier = 398;
public const int outKeyword = 399;
public const int refKeyword = 400;
public const int typeOf = 401;
public const int defaultOf = 402;
public const int newKeyword = 403;
public const int globalKeyword = 404;
public const int stringType = 405;
public const int boolType = 406;
public const int charType = 407;
public const int floatType = 408;
public const int doubleType = 409;
public const int decimalType = 410;
public const int sbyteType = 411;
public const int byteType = 412;
public const int shortType = 413;
public const int ushortType = 414;
public const int intType = 415;
public const int uintType = 416;
public const int longType = 417;
public const int ulongType = 418;
public const int objectType = 419;
public const int boolLiteral = 420;
public const int nullLiteral = 421;
public const int thisRef = 422;
public const int baseRef = 423;
public const int verbatimStringLiteral = 424;
public const int identifier = 425;
public const int stringLiteral = 426;
public const int characterLiteral = 427;
public const int lte = 428;
public const int lt = 429;
public const int gte = 430;
public const int gt = 431;
public const int eqEq = 432;
public const int notEq = 433;
public const int eq = 434;
public const int inc = 435;
public const int addAssign = 436;
public const int add = 437;
public const int dec = 438;
public const int subAssign = 439;
public const int sub = 440;
public const int mulAssign = 441;
public const int mul = 442;
public const int divAssign = 443;
public const int div = 444;
public const int modAssign = 445;
public const int mod = 446;
public const int and = 447;
public const int bitwiseAndAssign = 448;
public const int bitwiseAnd = 449;
public const int or = 450;
public const int bitwiseOrAssign = 451;
public const int bitwiseOr = 452;
public const int not = 453;
public const int lbracket = 454;
public const int rbracket = 455;
public const int lparen = 456;
public const int rparen = 457;
public const int lbrace = 458;
public const int rbrace = 459;
public const int comma = 460;
public const int colonColon = 461;
public const int dot = 462;
public const int integerLiteral = 463;
public const int floatLiteral = 464;
public const int whitespace = 465;
public const int ifKeyword = 466;
public const int gotoKeyword = 467;
public const int elseKeyword = 468;
public const int forKeyword = 469;
public const int throwKeyword = 470;
public const int whileKeyword = 471;
public const int returnKeyword = 472;
public const int tryKeyword = 473;
public const int catchKeyword = 474;
public const int finallyKeyword = 475;
public const int semi = 476;
public const int varType = 477;
public const int colon = 478;
public const int directive = 479;
public const int lineComment = 480;
public const int blockComment = 481;
public const int assemblyKeyword = 482;
public const int voidType = 483;
public const int partialKeyword = 484;
public const int classKeyword = 485;
public const int enumKeyword = 486;
public const int structKeyword = 487;
public const int interfaceKeyword = 488;
public const int getKeyword = 489;
public const int setKeyword = 490;
public const int eventKeyword = 491;
public const int publicKeyword = 492;
public const int privateKeyword = 493;
public const int protectedKeyword = 494;
public const int internalKeyword = 495;
public const int staticKeyword = 496;
public const int virtualKeyword = 497;
public const int abstractKeyword = 498;
public const int constKeyword = 499;
public const int overrideKeyword = 500;
public const int whereKeyword = 501;
public const int _EOS = 502;
public const int _ERROR = 503;

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
