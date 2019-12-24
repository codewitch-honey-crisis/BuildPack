%namespace CD

%visibility internal

%option stack, classes, minimize, noparser, verbose, persistbuffer, noembedbuffers, out:SlangScanner.cs

/* 
 * Expected file format is Unicode. In the event that no 
 * byte order mark prefix is found, revert to raw bytes.
 */
%option unicode, codepage:raw


%{

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
			return result;
		}
		public void Advance()
		{
			Current.Line = yyline;
			Current.Column = yycol;
			Current.Position = yypos;
			Current.SymbolId = yylex();
			Current.Value = yytext;
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
					return true;
				}
			}
			tokTxt = s+sb.ToString();
			return false;
		}
%}

%%
<<EOF>>		{ return -2; }
@(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ return 21; }
@\"([^\"|\"\"])*\" 	{ return 15; }
(_|[[:IsLetter:]])(_|[[:IsLetterOrDigit:]])* 	{ return 22; }
\"([^\\\"\a\b\f\n\r\t\v\0]|\\[^\r\n]|\\[0-7]{3}|\\x[0-9A-Fa-f]{2}|\\u[0-9A-Fa-f]{4}|\\U[0-9A-Fa-f]{8})*\" 	{ return 18; }
\/\/[^\n]* 	 { return yylex(); }
"/*" 	{ if(!_TryReadUntilBlockEnd("*/")) return -1; return yylex(); }
[ \t\r\n\v\f]+ 	 { return yylex(); }
"+" 	{ return 13; }
"-" 	{ return 14; }
"*" 	{ return 23; }
"/" 	{ return 24; }
"(" 	{ return 19; }
")" 	{ return 20; }
(0x[0-9A-Fa-f]{1,16}|(0|[1-9][0-9]*))([Uu][Ll]?|[Ll][Uu]?)? 	{ return 16; }
((0|[1-9][0-9]*)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?[DdMmFf]?)|((\.[0-9]+)([Ee][\+\-]?[0-9]+)?[DdMmFf]?) 	{ return 17; }
#[ \t]*[a-z]+[ \t]* 	{ if(!_TryReadUntilBlockEnd("\n")) return -1;return 28; }

[\n]|[^\n]		{ return -1; }


	

