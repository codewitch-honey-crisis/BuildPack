// We extend parse context with regular expression capabilities
namespace RE
{
	/// <summary>
	/// Extends the parse context
	/// </summary>
#if REGEXLIB
	public 
#endif
		static partial class ParseContextExtensions
	{
		/// <summary>
		/// Lexes a token out of the input
		/// </summary>
		/// <param name="context">The input parse context</param>
		/// <param name="dfaTable">The DFA state table</param>
		/// <param name="errorSymbol">The optional symbol id to report on error</param>
		/// <returns>A symbol id representing the next token. The capture buffer contains the captured content.</returns>
		public static int Lex(this ParseContext context,CharDfaEntry[] dfaTable, int errorSymbol = -1)
		{
			return CharFA<string>.LexDfa(dfaTable, context, errorSymbol);
		}
	}
}
