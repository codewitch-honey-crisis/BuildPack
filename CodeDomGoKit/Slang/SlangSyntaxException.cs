using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CD
{
	/// <summary>
	/// Represents a syntax error raised by <see cref="SlangParser"/>
	/// </summary>
#if GOKITLIB
	public
#endif
	class SlangSyntaxException : Exception
	{
		/// <summary>
		/// Creates a syntax exception with the specified arguments
		/// </summary>
		/// <param name="message">The error message</param>
		/// <param name="line">The line where the error occurred</param>
		/// <param name="column">The column where the error occured</param>
		/// <param name="position">The position where the error occured</param>
		public SlangSyntaxException(string message,int line,int column,long position)
			: base(_GetMessage(message,line,column,position))
		{
			Line = line;
			Column = column;
			Position = position;
		}
		/// <summary>
		/// The line where the error occurred
		/// </summary>
		public int Line { get;  }
		/// <summary>
		/// The column where the error occurred
		/// </summary>
		public int Column { get; }
		/// <summary>
		/// The position where the error occurred
		/// </summary>
		public long Position { get; }

		static string _GetMessage(string message,int line,int column,long position)
		{
			return string.Format("{0} at line {1}, column {2}, position {3}",message,line,column,position); 
		}
	}
}
