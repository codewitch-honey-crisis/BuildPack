namespace ParsleyDemo
{
	/// <summary>
	/// Represents a lexeme/token
	/// </summary>
	struct Token
	{
		/// <summary>
		/// Indicates the symbol id or -1 for the error symbol, -2 for end of input, and -3 for disposed
		/// </summary>
		public int SymbolId;
		/// <summary>
		/// Indicates the value of the token
		/// </summary>
		public string Value;
		/// <summary>
		/// Indicates the line where the token occurs
		/// </summary>
		public int Line;
		/// <summary>
		/// Indicates the column where the token occurs
		/// </summary>
		public int Column;
		/// <summary>
		/// Indicates the position where the token occurs
		/// </summary>
		public long Position;
	}
}
