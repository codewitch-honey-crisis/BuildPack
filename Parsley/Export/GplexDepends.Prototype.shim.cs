// this file exists purely to shut the compiler up in case one wants to 
// modify the prototype. Set GplexDepends.Prototype.cs to compile before 
// you do to make sure there are no errors. Then set it back to embedded 
// resource once done
using System.IO;
namespace Parsley_codenamespace
{
	class ScanBuff
	{
		public void Close() { }
	}
	// shut the compiler up
	struct Token
	{
		public int SymbolId;
		public string Value;
		public int Line;
		public int Column;
		public long Position;
	}
	partial class Scanner
	{
		ScanBuff buffer;
		public Scanner(Stream stream,string fallbackCodePage)
		{
			
		}
		public int yylex() { return -1; }
		public string yytext;
		int yyline;
		int yycol;
		int yypos; // stupid that this is int. oh well

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
		int code;
		int Read()
		{
			return -1;
		}
		void GetCode() { }
		// reads until the specified character, consuming it, returning false if it wasn't found
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
				yytext += sb.ToString();
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
					yytext += sb.ToString();
					return true;
				}
			}
			yytext += sb.ToString();
			return false;
		}
	}
}
