%namespace CD

%visibility internal

%option stack, classes, minimize, noparser, verbose, persistbuffer

/* , out:SlangScanner.cs, noembedbuffers
 * Expected file format is Unicode. In the event that no 
 * byte order mark prefix is found, revert to raw bytes.
 */
%option unicode, codepage:raw


%{
		public const int TabWidth = 4;
		// yyline and yycol are useless
		int _line=1;
		int _column=1;
		long _position=0;
		List<Token> _skipped=new List<Token>();
		// required to shut gplex up
		enum Tokens {
		EOF = -1
		}
		// foreach Token support
    	public Token Current = _InitToken();
		static Token _InitToken()
		{
			var result = default(Token);
			result.SymbolId = -4;
			result.Line = 1;
			result.Column = 1;
			result.Position = 0;
			result.Skipped=null;
			return result;
		}
		public void UpdatePosition(string text) 
		{
			if(string.IsNullOrEmpty(text)) return;
			for(var i = 0;i<text.Length;++i) 
			{
				var ch = text[i];
				switch(ch) 
				{
					case '\n':
						++_line;
						_column=1;
						break;
					case '\r':
						_column=1;
						break;
					case '\t':
						_column+=TabWidth;
						break;
					default:
						++_column;
						break;
				}
				++_position;
			}
		}
		public void Skip(int sym) {
			Token t = _InitToken();
			t.SymbolId=sym;
			t.Line = Current.Line;
			t.Column =Current.Column;
			t.Position=Current.Position;
			t.Value = yytext;
			t.Skipped = null;
			_skipped.Add(t);
		}
		public void Advance()
		{
			Current.SymbolId = yylex();
			Current.Value = yytext;
			Current.Line = _line;
			Current.Column = _column;
			Current.Position = _position;
			Current.Skipped=new Token[_skipped.Count];
			_skipped.CopyTo(Current.Skipped,0);
			_skipped.Clear();
			
		}
		public void Close()
		{
			Current.SymbolId = -3; // _Disposed
		}
		bool _TryReadUntil(int character,StringBuilder sb)
		{
			if (-1 == code) return false;
			var chcmp = character.ToString();
			var s = char.ConvertFromUtf32(code);
			sb.Append(s);
			if (code == character)
				return true;
			while (true)
			{
				GetCode();
				if (-1 == code || code == character)
					break;
				s = char.ConvertFromUtf32(code);
				sb.Append(s);
			}
			if (-1!=code)
			{
				s = char.ConvertFromUtf32(code);
				sb.Append(s);
				if (null == tokTxt)
					tokTxt = sb.ToString();
				else
					tokTxt += sb.ToString();
				UpdatePosition(tokTxt);
				return code == character;
			}
			return false;
		}
		// reads until the string is encountered, capturing it.
		bool _TryReadUntilBlockEnd(string blockEnd)
		{
			string s = yytext;
			var sb = new StringBuilder();
			int ch = -1;
			var isPair = false;
			if (char.IsSurrogatePair(blockEnd, 0))
			{
				ch = char.ConvertToUtf32(blockEnd, 0);
				isPair = true;
			} 
			else
				ch = blockEnd[0];
			while (-1 != code && _TryReadUntil(ch,sb))
			{
				bool found = true;
				int i = 1;
				if (isPair)
					++i;
				for (; found && i < blockEnd.Length; ++i)
				{
					GetCode();
					int scmp=blockEnd[i];
					if (char.IsSurrogatePair(blockEnd, i))
					{
						scmp = char.ConvertToUtf32(blockEnd, i);
						++i;
					}
					if (-1==code || code!=scmp)
						found = false;
					else if (-1!=code)
						sb.Append(char.ConvertFromUtf32(code));
				}
				if (found)
				{
					// TODO: verify this
					GetCode();
					tokTxt = s+ sb.ToString();
					UpdatePosition(tokTxt);
					return true;
				}
			}
			tokTxt = s+sb.ToString();
			UpdatePosition(tokTxt);
			return false;
		}
%}

%%
<<EOF>>		{ return -2; }
"where" 	{ UpdatePosition(yytext); return 504; }
"override" 	{ UpdatePosition(yytext); return 503; }
"const" 	{ UpdatePosition(yytext); return 502; }
"abstract" 	{ UpdatePosition(yytext); return 501; }
"static" 	{ UpdatePosition(yytext); return 500; }
"internal" 	{ UpdatePosition(yytext); return 499; }
"protected" 	{ UpdatePosition(yytext); return 498; }
"private" 	{ UpdatePosition(yytext); return 497; }
"public" 	{ UpdatePosition(yytext); return 496; }
"event" 	{ UpdatePosition(yytext); return 495; }
"set" 	{ UpdatePosition(yytext); return 494; }
"get" 	{ UpdatePosition(yytext); return 493; }
"interface" 	{ UpdatePosition(yytext); return 492; }
"struct" 	{ UpdatePosition(yytext); return 491; }
"enum" 	{ UpdatePosition(yytext); return 490; }
"class" 	{ UpdatePosition(yytext); return 489; }
"partial" 	{ UpdatePosition(yytext); return 488; }
"void" 	{ UpdatePosition(yytext); return 487; }
"assembly" 	{ UpdatePosition(yytext); return 486; }
"/*" 	{ if(!_TryReadUntilBlockEnd("*/")) { UpdatePosition(yytext); return -1; } UpdatePosition(yytext); Skip(485); }
(\/\/[^\n]*[\n])+ 	{ UpdatePosition(yytext); Skip(484); }
#[A-Za-z]+[\t ]*[^\n]* 	{ UpdatePosition(yytext);  }
":" 	{ UpdatePosition(yytext); return 482; }
"var" 	{ UpdatePosition(yytext); return 481; }
";" 	{ UpdatePosition(yytext); return 480; }
"finally" 	{ UpdatePosition(yytext); return 479; }
"catch" 	{ UpdatePosition(yytext); return 478; }
"try" 	{ UpdatePosition(yytext); return 477; }
"return" 	{ UpdatePosition(yytext); return 476; }
"while" 	{ UpdatePosition(yytext); return 475; }
"throw" 	{ UpdatePosition(yytext); return 474; }
"for" 	{ UpdatePosition(yytext); return 473; }
"else" 	{ UpdatePosition(yytext); return 472; }
"goto" 	{ UpdatePosition(yytext); return 471; }
"if" 	{ UpdatePosition(yytext); return 470; }
[ \t\r\n\v\f]+ 	{ UpdatePosition(yytext);  }
"." 	{ UpdatePosition(yytext); return 466; }
"::" 	{ UpdatePosition(yytext); return 465; }
"," 	{ UpdatePosition(yytext); return 464; }
"}" 	{ UpdatePosition(yytext); return 463; }
"{" 	{ UpdatePosition(yytext); return 462; }
")" 	{ UpdatePosition(yytext); return 461; }
"(" 	{ UpdatePosition(yytext); return 460; }
"]" 	{ UpdatePosition(yytext); return 459; }
"[" 	{ UpdatePosition(yytext); return 458; }
"!" 	{ UpdatePosition(yytext); return 457; }
"|" 	{ UpdatePosition(yytext); return 456; }
"|=" 	{ UpdatePosition(yytext); return 455; }
"||" 	{ UpdatePosition(yytext); return 454; }
"&" 	{ UpdatePosition(yytext); return 453; }
"&=" 	{ UpdatePosition(yytext); return 452; }
"&&" 	{ UpdatePosition(yytext); return 451; }
"%" 	{ UpdatePosition(yytext); return 450; }
"%=" 	{ UpdatePosition(yytext); return 449; }
"/" 	{ UpdatePosition(yytext); return 448; }
"/=" 	{ UpdatePosition(yytext); return 447; }
"*" 	{ UpdatePosition(yytext); return 446; }
"*=" 	{ UpdatePosition(yytext); return 445; }
"-" 	{ UpdatePosition(yytext); return 444; }
"-=" 	{ UpdatePosition(yytext); return 443; }
"--" 	{ UpdatePosition(yytext); return 442; }
"+" 	{ UpdatePosition(yytext); return 441; }
"+=" 	{ UpdatePosition(yytext); return 440; }
"++" 	{ UpdatePosition(yytext); return 439; }
"=" 	{ UpdatePosition(yytext); return 438; }
"!=" 	{ UpdatePosition(yytext); return 437; }
"==" 	{ UpdatePosition(yytext); return 436; }
">" 	{ UpdatePosition(yytext); return 435; }
">=" 	{ UpdatePosition(yytext); return 434; }
"<" 	{ UpdatePosition(yytext); return 433; }
"<=" 	{ UpdatePosition(yytext); return 432; }
[\u0027]([^\\\"\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})[\u0027] 	{ UpdatePosition(yytext); return 431; }
\"([^\\\"\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})*\" 	{ UpdatePosition(yytext); return 430; }
@\"([^\"|\"\"])*\" 	{ UpdatePosition(yytext); return 428; }
"global" 	{ UpdatePosition(yytext); return 427; }
"base" 	{ UpdatePosition(yytext); return 426; }
"this" 	{ UpdatePosition(yytext); return 425; }
"null" 	{ UpdatePosition(yytext); return 424; }
true|false 	{ UpdatePosition(yytext); return 423; }
"object" 	{ UpdatePosition(yytext); return 422; }
"ulong" 	{ UpdatePosition(yytext); return 421; }
"long" 	{ UpdatePosition(yytext); return 420; }
"uint" 	{ UpdatePosition(yytext); return 419; }
"int" 	{ UpdatePosition(yytext); return 418; }
"ushort" 	{ UpdatePosition(yytext); return 417; }
"short" 	{ UpdatePosition(yytext); return 416; }
"byte" 	{ UpdatePosition(yytext); return 415; }
"sbyte" 	{ UpdatePosition(yytext); return 414; }
"decimal" 	{ UpdatePosition(yytext); return 413; }
"double" 	{ UpdatePosition(yytext); return 412; }
"float" 	{ UpdatePosition(yytext); return 411; }
"char" 	{ UpdatePosition(yytext); return 410; }
"bool" 	{ UpdatePosition(yytext); return 409; }
"string" 	{ UpdatePosition(yytext); return 408; }
"new" 	{ UpdatePosition(yytext); return 407; }
"default" 	{ UpdatePosition(yytext); return 406; }
"typeof" 	{ UpdatePosition(yytext); return 405; }
"ref" 	{ UpdatePosition(yytext); return 404; }
"out" 	{ UpdatePosition(yytext); return 403; }
@(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ UpdatePosition(yytext); return 402; }
"using" 	{ UpdatePosition(yytext); return 401; }
"namespace" 	{ UpdatePosition(yytext); return 400; }
(0x[0-9A-Fa-f]{1,16}|([0-9]+))([Uu][Ll]?|[Ll][Uu]?)? 	{ UpdatePosition(yytext); return 467; }
(([0-9]+)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?[DdMmFf]?)|((\.[0-9]+)([Ee][\+\-]?[0-9]+)?[DdMmFf]?) 	{ UpdatePosition(yytext); return 468; }
(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ UpdatePosition(yytext); return 429; }

[\n]|[^\n]		{ UpdatePosition(yytext); return -1; }


	

