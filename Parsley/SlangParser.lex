%namespace CD

%visibility internal

%option stack, classes, minimize, noparser, verbose, persistbuffer, noembedbuffers, out:SlangParserScanner.cs

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
			}
		}
		public void Advance()
		{
			Current.SymbolId = yylex();
			Current.Value = yytext;
			Current.Line = _line;
			Current.Column = _column;
			Current.Position = _position;
			
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
"namespace" 	{ UpdatePosition(yytext); return 279; }
"*" 	{ UpdatePosition(yytext); return 356; }
"*=" 	{ UpdatePosition(yytext); return 355; }
"-" 	{ UpdatePosition(yytext); return 354; }
"-=" 	{ UpdatePosition(yytext); return 353; }
"--" 	{ UpdatePosition(yytext); return 352; }
"+" 	{ UpdatePosition(yytext); return 351; }
"+=" 	{ UpdatePosition(yytext); return 350; }
"++" 	{ UpdatePosition(yytext); return 349; }
"=" 	{ UpdatePosition(yytext); return 348; }
"!=" 	{ UpdatePosition(yytext); return 347; }
"/=" 	{ UpdatePosition(yytext); return 357; }
"==" 	{ UpdatePosition(yytext); return 346; }
">=" 	{ UpdatePosition(yytext); return 344; }
"<" 	{ UpdatePosition(yytext); return 343; }
"<=" 	{ UpdatePosition(yytext); return 342; }
[\u0027]([^\\\"\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})[\u0027] 	{ UpdatePosition(yytext); return 341; }
\"([^\\\"\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})*\" 	{ UpdatePosition(yytext); return 340; }
@\"([^\"|\"\"])*\" 	{ UpdatePosition(yytext); return 338; }
"base" 	{ UpdatePosition(yytext); return 337; }
"this" 	{ UpdatePosition(yytext); return 336; }
"null" 	{ UpdatePosition(yytext); return 335; }
"true|false" 	{ UpdatePosition(yytext); return 334; }
">" 	{ UpdatePosition(yytext); return 345; }
"object" 	{ UpdatePosition(yytext); return 333; }
"/" 	{ UpdatePosition(yytext); return 358; }
"%" 	{ UpdatePosition(yytext); return 360; }
[ \t\r\n\v\f]+ 	 { UpdatePosition(yytext); return yylex(); }
"/*" 	{ if(!_TryReadUntilBlockEnd("*/")) return -1;UpdatePosition(yytext); return yylex(); }
\/\/[^\n]* 	 { UpdatePosition(yytext); return yylex(); }
(([0-9]+)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?[DdMmFf]?)|((\.[0-9]+)([Ee][\+\-]?[0-9]+)?[DdMmFf]?) 	{ UpdatePosition(yytext); return 379; }
(0x[0-9A-Fa-f]{1,16}|([0-9]+))([Uu][Ll]?|[Ll][Uu]?)? 	{ UpdatePosition(yytext); return 378; }
#[A-Za-z]+ 	{ if(!_TryReadUntilBlockEnd("\n")) return -1;UpdatePosition(yytext); return 377; }
"." 	{ UpdatePosition(yytext); return 376; }
"::" 	{ UpdatePosition(yytext); return 375; }
"," 	{ UpdatePosition(yytext); return 374; }
"}" 	{ UpdatePosition(yytext); return 373; }
"%=" 	{ UpdatePosition(yytext); return 359; }
"{" 	{ UpdatePosition(yytext); return 372; }
"(" 	{ UpdatePosition(yytext); return 370; }
"]" 	{ UpdatePosition(yytext); return 369; }
"[" 	{ UpdatePosition(yytext); return 368; }
"!" 	{ UpdatePosition(yytext); return 367; }
"|" 	{ UpdatePosition(yytext); return 366; }
"|=" 	{ UpdatePosition(yytext); return 365; }
"||" 	{ UpdatePosition(yytext); return 364; }
"&" 	{ UpdatePosition(yytext); return 363; }
"&=" 	{ UpdatePosition(yytext); return 362; }
"&&" 	{ UpdatePosition(yytext); return 361; }
")" 	{ UpdatePosition(yytext); return 371; }
"ulong" 	{ UpdatePosition(yytext); return 332; }
"long" 	{ UpdatePosition(yytext); return 331; }
"for" 	{ UpdatePosition(yytext); return 302; }
"else" 	{ UpdatePosition(yytext); return 301; }
"goto" 	{ UpdatePosition(yytext); return 300; }
"if" 	{ UpdatePosition(yytext); return 299; }
"where" 	{ UpdatePosition(yytext); return 298; }
"override" 	{ UpdatePosition(yytext); return 297; }
"const" 	{ UpdatePosition(yytext); return 296; }
"abstract" 	{ UpdatePosition(yytext); return 295; }
"static" 	{ UpdatePosition(yytext); return 294; }
"internal" 	{ UpdatePosition(yytext); return 293; }
"protected" 	{ UpdatePosition(yytext); return 292; }
"private" 	{ UpdatePosition(yytext); return 291; }
"public" 	{ UpdatePosition(yytext); return 290; }
"event" 	{ UpdatePosition(yytext); return 289; }
"set" 	{ UpdatePosition(yytext); return 288; }
"get" 	{ UpdatePosition(yytext); return 287; }
"interface" 	{ UpdatePosition(yytext); return 286; }
"struct" 	{ UpdatePosition(yytext); return 285; }
"enum" 	{ UpdatePosition(yytext); return 284; }
"class" 	{ UpdatePosition(yytext); return 283; }
"partial" 	{ UpdatePosition(yytext); return 282; }
"void" 	{ UpdatePosition(yytext); return 281; }
"using" 	{ UpdatePosition(yytext); return 280; }
"throw" 	{ UpdatePosition(yytext); return 303; }
"while" 	{ UpdatePosition(yytext); return 304; }
"return" 	{ UpdatePosition(yytext); return 305; }
"try" 	{ UpdatePosition(yytext); return 306; }
"int" 	{ UpdatePosition(yytext); return 329; }
"ushort" 	{ UpdatePosition(yytext); return 328; }
"short" 	{ UpdatePosition(yytext); return 327; }
"byte" 	{ UpdatePosition(yytext); return 326; }
"sbyte" 	{ UpdatePosition(yytext); return 325; }
"decimal" 	{ UpdatePosition(yytext); return 324; }
"double" 	{ UpdatePosition(yytext); return 323; }
"float" 	{ UpdatePosition(yytext); return 322; }
"char" 	{ UpdatePosition(yytext); return 321; }
"bool" 	{ UpdatePosition(yytext); return 320; }
"uint" 	{ UpdatePosition(yytext); return 330; }
"string" 	{ UpdatePosition(yytext); return 319; }
"default" 	{ UpdatePosition(yytext); return 317; }
"nameOf" 	{ UpdatePosition(yytext); return 316; }
"typeof" 	{ UpdatePosition(yytext); return 315; }
"ref" 	{ UpdatePosition(yytext); return 314; }
"out" 	{ UpdatePosition(yytext); return 313; }
@(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ UpdatePosition(yytext); return 312; }
":" 	{ UpdatePosition(yytext); return 311; }
"var" 	{ UpdatePosition(yytext); return 310; }
";" 	{ UpdatePosition(yytext); return 309; }
"finally" 	{ UpdatePosition(yytext); return 308; }
"catch" 	{ UpdatePosition(yytext); return 307; }
"new" 	{ UpdatePosition(yytext); return 318; }
(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ UpdatePosition(yytext); return 339; }

[\n]|[^\n]		{ UpdatePosition(yytext); return -1; }


	

