﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	/// <summary>
	/// Represents the current status of the operation
	/// </summary>
#if REGEXLIB
	public 
#endif
	enum CharFAStatus
	{
		/// <summary>
		/// The status is unknown
		/// </summary>
		Unknown,
		/// <summary>
		/// Performing a DFA transform
		/// </summary>
		DfaTransform,
		/// <summary>
		/// Trimming duplicate states
		/// </summary>
		TrimDuplicates
	}
	/// <summary>
	/// Represents the progress of the operation
	/// </summary>
#if REGEXLIB
	public 
#endif
	struct CharFAProgress
	{
		/// <summary>
		/// Constructs a new instance of the progress class with the specified status and count
		/// </summary>
		/// <param name="status">The status</param>
		/// <param name="count">The count of values in the progress</param>
		public CharFAProgress(CharFAStatus status, int count)
		{
			Status = status;
			Count = count;
		}
		/// <summary>
		/// The status
		/// </summary>
		public CharFAStatus Status { get; }
		/// <summary>
		/// The count of values in the progress.
		/// </summary>
		public int Count { get; }
	}
}
