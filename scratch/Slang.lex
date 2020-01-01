%namespace CD

%visibility internal

%option stack, classes, minimize, noparser, verbose, persistbuffer, noembedbuffers, out:SlangScanner.cs

/* 
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
		public int Skip(int sym) {
			Token t = _InitToken();
			t.SymbolId=sym;
			t.Line = Current.Line;
			t.Column =Current.Column;
			t.Position=Current.Position;
			t.Value = yytext;
			t.Skipped = null;
			_skipped.Add(t);
			var result = yylex();
			return result;
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
"namespace" 	{ UpdatePosition(yytext); return 364; }
"try" 	{ UpdatePosition(yytext); return 441; }
"return" 	{ UpdatePosition(yytext); return 440; }
"while" 	{ UpdatePosition(yytext); return 439; }
"throw" 	{ UpdatePosition(yytext); return 438; }
"for" 	{ UpdatePosition(yytext); return 437; }
"else" 	{ UpdatePosition(yytext); return 436; }
"goto" 	{ UpdatePosition(yytext); return 435; }
"if" 	{ UpdatePosition(yytext); return 434; }
[ \t\r\n\v\f]+ 	 { UpdatePosition(yytext); return yylex(); }
(([0-9]+)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?[DdMmFf]?)|((\.[0-9]+)([Ee][\+\-]?[0-9]+)?[DdMmFf]?) 	{ UpdatePosition(yytext); return 432; }
"catch" 	{ UpdatePosition(yytext); return 442; }
(0x[0-9A-Fa-f]{1,16}|([0-9]+))([Uu][Ll]?|[Ll][Uu]?)? 	{ UpdatePosition(yytext); return 431; }
"::" 	{ UpdatePosition(yytext); return 429; }
"," 	{ UpdatePosition(yytext); return 428; }
"}" 	{ UpdatePosition(yytext); return 427; }
"{" 	{ UpdatePosition(yytext); return 426; }
")" 	{ UpdatePosition(yytext); return 425; }
"(" 	{ UpdatePosition(yytext); return 424; }
"]" 	{ UpdatePosition(yytext); return 423; }
"[" 	{ UpdatePosition(yytext); return 422; }
"!" 	{ UpdatePosition(yytext); return 421; }
"|" 	{ UpdatePosition(yytext); return 420; }
"." 	{ UpdatePosition(yytext); return 430; }
"finally" 	{ UpdatePosition(yytext); return 443; }
";" 	{ UpdatePosition(yytext); return 444; }
"var" 	{ UpdatePosition(yytext); return 445; }
"where" 	{ UpdatePosition(yytext); return 468; }
"override" 	{ UpdatePosition(yytext); return 467; }
"const" 	{ UpdatePosition(yytext); return 466; }
"abstract" 	{ UpdatePosition(yytext); return 465; }
"static" 	{ UpdatePosition(yytext); return 464; }
"internal" 	{ UpdatePosition(yytext); return 463; }
"protected" 	{ UpdatePosition(yytext); return 462; }
"private" 	{ UpdatePosition(yytext); return 461; }
"public" 	{ UpdatePosition(yytext); return 460; }
"event" 	{ UpdatePosition(yytext); return 459; }
"set" 	{ UpdatePosition(yytext); return 458; }
"get" 	{ UpdatePosition(yytext); return 457; }
"interface" 	{ UpdatePosition(yytext); return 456; }
"struct" 	{ UpdatePosition(yytext); return 455; }
"enum" 	{ UpdatePosition(yytext); return 454; }
"class" 	{ UpdatePosition(yytext); return 453; }
"partial" 	{ UpdatePosition(yytext); return 452; }
"void" 	{ UpdatePosition(yytext); return 451; }
"assembly" 	{ UpdatePosition(yytext); return 450; }
"/*" 	{ if(!_TryReadUntilBlockEnd("*/")) return -1;UpdatePosition(yytext); return Skip(449);UpdatePosition(yytext); return Skip(449); }
\/\/[^\n]* 	{ UpdatePosition(yytext); return Skip(448); }
#[A-Za-z]+[\t ]*[^\n]* 	{ UpdatePosition(yytext); return Skip(447); }
":" 	{ UpdatePosition(yytext); return 446; }
"|=" 	{ UpdatePosition(yytext); return 419; }
"||" 	{ UpdatePosition(yytext); return 418; }
"&" 	{ UpdatePosition(yytext); return 417; }
"object" 	{ UpdatePosition(yytext); return 387; }
"ulong" 	{ UpdatePosition(yytext); return 386; }
"long" 	{ UpdatePosition(yytext); return 385; }
"uint" 	{ UpdatePosition(yytext); return 384; }
"int" 	{ UpdatePosition(yytext); return 383; }
"ushort" 	{ UpdatePosition(yytext); return 382; }
"short" 	{ UpdatePosition(yytext); return 381; }
"byte" 	{ UpdatePosition(yytext); return 380; }
"sbyte" 	{ UpdatePosition(yytext); return 379; }
"decimal" 	{ UpdatePosition(yytext); return 378; }
"double" 	{ UpdatePosition(yytext); return 377; }
"float" 	{ UpdatePosition(yytext); return 376; }
"char" 	{ UpdatePosition(yytext); return 375; }
"bool" 	{ UpdatePosition(yytext); return 374; }
"string" 	{ UpdatePosition(yytext); return 373; }
"new" 	{ UpdatePosition(yytext); return 372; }
"default" 	{ UpdatePosition(yytext); return 371; }
"nameOf" 	{ UpdatePosition(yytext); return 370; }
"typeof" 	{ UpdatePosition(yytext); return 369; }
"ref" 	{ UpdatePosition(yytext); return 368; }
"out" 	{ UpdatePosition(yytext); return 367; }
@(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ UpdatePosition(yytext); return 366; }
"using" 	{ UpdatePosition(yytext); return 365; }
"true|false" 	{ UpdatePosition(yytext); return 388; }
"null" 	{ UpdatePosition(yytext); return 389; }
"this" 	{ UpdatePosition(yytext); return 390; }
"base" 	{ UpdatePosition(yytext); return 391; }
"&&" 	{ UpdatePosition(yytext); return 415; }
"%" 	{ UpdatePosition(yytext); return 414; }
"%=" 	{ UpdatePosition(yytext); return 413; }
"/" 	{ UpdatePosition(yytext); return 412; }
"/=" 	{ UpdatePosition(yytext); return 411; }
"*" 	{ UpdatePosition(yytext); return 410; }
"*=" 	{ UpdatePosition(yytext); return 409; }
"-" 	{ UpdatePosition(yytext); return 408; }
"-=" 	{ UpdatePosition(yytext); return 407; }
"--" 	{ UpdatePosition(yytext); return 406; }
"+" 	{ UpdatePosition(yytext); return 405; }
"&=" 	{ UpdatePosition(yytext); return 416; }
"+=" 	{ UpdatePosition(yytext); return 404; }
"=" 	{ UpdatePosition(yytext); return 402; }
"!=" 	{ UpdatePosition(yytext); return 401; }
"==" 	{ UpdatePosition(yytext); return 400; }
">" 	{ UpdatePosition(yytext); return 399; }
">=" 	{ UpdatePosition(yytext); return 398; }
"<" 	{ UpdatePosition(yytext); return 397; }
"<=" 	{ UpdatePosition(yytext); return 396; }
[\u0027]([^\\\"\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})[\u0027] 	{ UpdatePosition(yytext); return 395; }
\"([^\\\"\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})*\" 	{ UpdatePosition(yytext); return 394; }
@\"([^\"|\"\"])*\" 	{ UpdatePosition(yytext); return 392; }
"++" 	{ UpdatePosition(yytext); return 403; }
(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ UpdatePosition(yytext); return 393; }

[\n]|[^\n]		{ UpdatePosition(yytext); return -1; }


	

