using System;
namespace Parsley
{
	/// <summary>
	/// 
	/// </summary>
	class SyntaxException : Exception
	{
		private int _line;
		private int _column;
		private long _position;
		/// <summary>
		/// Creates a syntax exception with the specified arguments
		/// </summary>
		/// <param name="message">The error message</param>
		/// <param name="line">The line where the error occurred</param>
		/// <param name="column">The column where the error occured</param>
		/// <param name="position">The position where the error occured</param>
		public SyntaxException(string message, int line, int column, long position)
			: base(_GetMessage(message, line, column, position))
		{
			_line = line;
			_column = column;
			_position = position;
		}
		
		/// <summary>
		/// The line where the error occurred
		/// </summary>
		public int Line { get { return _line; } }
		/// <summary>
		/// The column where the error occurred
		/// </summary>
		public int Column { get { return _column; } }
		/// <summary>
		/// The position where the error occurred
		/// </summary>
		public long Position { get { return _position; } }

		static string _GetMessage(string message, int line, int column, long position)
		{
			return string.Format("{0} at line {1}, column {2}, position {3}", message, line, column, position);
		}
	}
}