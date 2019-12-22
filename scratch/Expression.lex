%using System;
%using System.Collections.Generic;
%using System.IO;
%namespace ParsleyDemo

%visibility internal

%option stack, classes, minimize, noparser, verbose, persistbuffer, noembedbuffers, out:ExpressionScanner.cs

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
				bool _TryReadUntil(int character,System.Text.StringBuilder sb)
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
				Current.Value += sb.ToString();
				return code == character;
			}
			return false;
		}
		// reads until the string is encountered, capturing it.
		bool _TryReadUntilBlockEnd(string blockEnd)
		{
			var sb = new System.Text.StringBuilder();
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
					Current.Value += sb.ToString();
					return true;
				}
			}
			Current.Value += sb.ToString();
			return false;
		}
		
%}

%%
// TODO: Need to figure out what the error symbol is - how to catch it
// hopefully below works
<<EOF>>		{ return -2; }
"+" 	{ return 12; }
"*" 	{ return 18; }
[0-9]+ 	{ return 14; }
[A-Z_a-z][0-9A-Z_a-z]* 	{ return 15; }
[ \t\r\n]+ 	 { return yylex(); }
"-" 	{ return 13; }
"/" 	{ return 19; }
"(" 	{ return 16; }
")" 	{ return 17; }

[.]		{ return -1; }


	

