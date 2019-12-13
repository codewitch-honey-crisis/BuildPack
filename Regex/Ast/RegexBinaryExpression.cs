using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	/// <summary>
	/// Represents a binary expression
	/// </summary>
#if REGEXLIB
	public 
#endif
	abstract class RegexBinaryExpression : RegexExpression
	{
		/// <summary>
		/// Indicates the left hand expression
		/// </summary>
		public RegexExpression Left { get; set; }
		/// <summary>
		/// Indicates the right hand expression
		/// </summary>
		public RegexExpression Right { get; set; }
	}
}
