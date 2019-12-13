﻿using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	/// <summary>
	/// Represents a concatenation between two expression. This has no operator as it is implicit.
	/// </summary>
#if REGEXLIB
	public 
#endif
	class RegexConcatExpression : RegexBinaryExpression, IEquatable<RegexConcatExpression>
	{
		/// <summary>
		/// Indicates whether or not this statement is a single element or not
		/// </summary>
		/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
		public override bool IsSingleElement {
			get {
				if (null == Left)
					return null == Right ? false : Right.IsSingleElement;
				else if (null == Right)
					return Left.IsSingleElement;
				return false;
			}
		}
		/// <summary>
		/// Creates a new expression with the specified left and right hand sides
		/// </summary>
		/// <param name="left">The left expression</param>
		/// <param name="right">The right expressions</param>
		public RegexConcatExpression(RegexExpression left, params RegexExpression[] right)
		{
			Left = left;
			for (int i = 0; i < right.Length; i++)
			{
				var r = right[i];
				if (null == Right)
					Right = r;
				if (i != right.Length - 1)
				{
					var c = new RegexConcatExpression();
					c.Left = Left;
					c.Right = Right;
					Right = null;
					Left = c;
				}
				
			}
		}
		/// <summary>
		/// Creates a default instance of the expression
		/// </summary>
		public RegexConcatExpression() { }
		/// <summary>
		/// Creates a state machine representing this expression
		/// </summary>
		/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
		/// <param name="accept">The accept symbol to use for this expression</param>
		/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
		public override CharFA<TAccept> ToFA<TAccept>(TAccept accept)
		{
			if (null == Left)
				return (null != Right) ? Right.ToFA(accept) : null;
			else if (null == Right)
				return Left.ToFA(accept);
			var result = CharFA<TAccept>.Concat(new CharFA<TAccept>[] { Left.ToFA(accept), Right.ToFA(accept) }, accept);
			if(null!=Left as RegexConcatExpression || null!=Right as RegexConcatExpression)
				result.TrimNeutrals();
			return result;
		}
		/// <summary>
		/// Creates a new copy of this expression
		/// </summary>
		/// <returns>A new copy of this expression</returns>
		protected override RegexExpression CloneImpl()
			=> Clone();
		/// <summary>
		/// Creates a new copy of this expression
		/// </summary>
		/// <returns>A new copy of this expression</returns>
		public RegexConcatExpression Clone()
		{
			return new RegexConcatExpression(Left, Right);
		}
		#region Value semantics
		/// <summary>
		/// Indicates whether this expression is the same as the right hand expression
		/// </summary>
		/// <param name="rhs">The expression to compare</param>
		/// <returns>True if the expressions are the same, otherwise false</returns>
		public bool Equals(RegexConcatExpression rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Left == rhs.Left && Right == rhs.Right;
		}
		/// <summary>
		/// Indicates whether this expression is the same as the right hand expression
		/// </summary>
		/// <param name="rhs">The expression to compare</param>
		/// <returns>True if the expressions are the same, otherwise false</returns>
		public override bool Equals(object rhs)
			=> Equals(rhs as RegexConcatExpression);
		/// <summary>
		/// Computes a hash code for this expression
		/// </summary>
		/// <returns>A hash code for this expression</returns>
		public override int GetHashCode()
		{
			var result = 0;
			if (null != Left)
				result ^= Left.GetHashCode();
			if (null != Right)
				result ^= Right.GetHashCode();
			return result;
		}
		/// <summary>
		/// Indicates whether or not two expression are the same
		/// </summary>
		/// <param name="lhs">The left hand expression to compare</param>
		/// <param name="rhs">The right hand expression to compare</param>
		/// <returns>True if the expressions are the same, otherwise false</returns>
		public static bool operator ==(RegexConcatExpression lhs, RegexConcatExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates whether or not two expression are different
		/// </summary>
		/// <param name="lhs">The left hand expression to compare</param>
		/// <param name="rhs">The right hand expression to compare</param>
		/// <returns>True if the expressions are different, otherwise false</returns>
		public static bool operator !=(RegexConcatExpression lhs, RegexConcatExpression rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
		/// <summary>
		/// Appends the textual representation to a <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="sb">The string builder to use</param>
		/// <remarks>Used by ToString()</remarks>
		protected internal override void AppendTo(StringBuilder sb)
		{
			if (null != Left)
			{
				var oe = Left as RegexOrExpression;
				if (null != oe)
					sb.Append('(');
				Left.AppendTo(sb);
				if (null != oe)
					sb.Append(')');
			}
			if (null != Right)
			{
				var oe = Right as RegexOrExpression;
				if (null != oe)
					sb.Append('(');
				Right.AppendTo(sb);
				if (null != oe)
					sb.Append(')');
			}
		}
	}
}
