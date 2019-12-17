﻿using System;
using System.Globalization;

namespace RE
{
	/// <summary>
	/// Represents the base class for regex charset entries
	/// </summary>
#if REGEXLIB
	public 
#endif
	abstract class RegexCharsetEntry : ICloneable
	{
		/// <summary>
		/// Initializes the charset entry
		/// </summary>
		internal RegexCharsetEntry() { } // nobody can make new derivations
		/// <summary>
		/// Implements the clone method
		/// </summary>
		/// <returns>A copy of the charset entry</returns>
		protected abstract RegexCharsetEntry CloneImpl();
		/// <summary>
		/// Creates a copy of the charset entry
		/// </summary>
		/// <returns>A new copy of the charset entry</returns>
		object ICloneable.Clone() => CloneImpl();
	}
	/// <summary>
	/// Represents a unicode character category, such as \p{Lu} (uppercase letter)
	/// </summary>
#if REGEXLIB
	public
#endif
	class RegexCharsetUnicodeCategoryEntry : RegexCharsetEntry
	{
		string _category;
		/// <summary>
		/// Initializes the charset entry with the specified unicode category
		/// </summary>
		/// <param name="category">The unicode category</param>
		public RegexCharsetUnicodeCategoryEntry(string category)
		{
			Category = category;
		}
		/// <summary>
		/// Indicates the Unicode category
		/// </summary>
		public string Category 
		{
			get {
				return _category;
			}
			set {
				if (!CharFA<string>.UnicodeCategories.ContainsKey(value))
					throw new ArgumentException("The value was not a valid unicode category.");
				_category = value;
			}
		}
		/// <summary>
		/// Returns a string indicating the string representation of this entry
		/// </summary>
		/// <returns>A string representing this entry</returns>
		public override string ToString()
		{
			return string.Concat("\\p{", Category, "}");	
		}
		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A clone of the object</returns>
		protected override RegexCharsetEntry CloneImpl()
		{
			return new RegexCharsetUnicodeCategoryEntry(Category);
		}

		#region Value semantics
		/// <summary>
		/// Indicates whether this unicode category entry is the same as the right hand unicode category entry
		/// </summary>
		/// <param name="rhs">The unicode category entry to compare</param>
		/// <returns>True if the unicode category entries are the same, otherwise false</returns>
		public bool Equals(RegexCharsetUnicodeCategoryEntry rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return 0 == string.Compare(Category, rhs.Category, StringComparison.InvariantCultureIgnoreCase);
		}
		/// <summary>
		/// Indicates whether this unicode category entry is the same as the right hand unicode category entry
		/// </summary>
		/// <param name="rhs">The unicode category entry to compare</param>
		/// <returns>True if the unicode category entries are the same, otherwise false</returns>
		public override bool Equals(object rhs)
			=> Equals(rhs as RegexCharsetUnicodeCategoryEntry);
		/// <summary>
		/// Computes a hash code for this unicode category entry
		/// </summary>
		/// <returns>A hash code for this unicode category entry</returns>
		public override int GetHashCode()
		{
			if (null == Category) return 0;
			return Category.ToUpperInvariant().GetHashCode();
		}
		/// <summary>
		/// Indicates whether or not two unicode category entries are the same
		/// </summary>
		/// <param name="lhs">The left hand unicode category entry to compare</param>
		/// <param name="rhs">The right hand unicode category entry to compare</param>
		/// <returns>True if the unicode category entries are the same, otherwise false</returns>
		public static bool operator ==(RegexCharsetUnicodeCategoryEntry lhs, RegexCharsetUnicodeCategoryEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates whether or not two unicode category entries are different
		/// </summary>
		/// <param name="lhs">The left hand unicode category entry to compare</param>
		/// <param name="rhs">The right hand unicode category entry to compare</param>
		/// <returns>True if the unicode category entries are different, otherwise false</returns>
		public static bool operator !=(RegexCharsetUnicodeCategoryEntry lhs, RegexCharsetUnicodeCategoryEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
	}
	/// <summary>
	/// Represents a character class charset entry
	/// </summary>
#if REGEXLIB
	public 
#endif
	class RegexCharsetClassEntry : RegexCharsetEntry
	{
		/// <summary>
		/// Initializes a charset entry with the specified character class
		/// </summary>
		/// <param name="name">The name of the character class</param>
		public RegexCharsetClassEntry(string name)
		{
			Name = name;
		}
		/// <summary>
		/// Initializes a default instance of the charset entry
		/// </summary>
		public RegexCharsetClassEntry() { }
		/// <summary>
		/// Indicates the name of the character class
		/// </summary>
		public string Name { get; set; }
		/// <summary>
		/// Gets a string representation of this instance
		/// </summary>
		/// <returns>The string representation of this character class</returns>
		public override string ToString()
		{
			return string.Concat("[:", Name, ":]");
		}
		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A new copy of the charset entry</returns>
		protected override RegexCharsetEntry CloneImpl()
			=> Clone();
		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A new copy of the charset entry</returns>
		public RegexCharsetClassEntry Clone()
		{
			return new RegexCharsetClassEntry(Name);
		}

		#region Value semantics
		/// <summary>
		/// Indicates whether this charset entry is the same as the right hand charset entry
		/// </summary>
		/// <param name="rhs">The charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public bool Equals(RegexCharsetClassEntry rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Name == rhs.Name;
		}
		/// <summary>
		/// Indicates whether this charset entry is the same as the right hand charset entry
		/// </summary>
		/// <param name="rhs">The charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public override bool Equals(object rhs)
			=> Equals(rhs as RegexCharsetClassEntry);
		/// <summary>
		/// Computes a hash code for this charset entry
		/// </summary>
		/// <returns>A hash code for this charset entry</returns>
		public override int GetHashCode()
		{
			if(string.IsNullOrEmpty(Name)) return Name.GetHashCode();
			return 0;
		}
		/// <summary>
		/// Indicates whether or not two charset entries are the same
		/// </summary>
		/// <param name="lhs">The left hand charset entry to compare</param>
		/// <param name="rhs">The right hand charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public static bool operator ==(RegexCharsetClassEntry lhs, RegexCharsetClassEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates whether or not two charset entries are different
		/// </summary>
		/// <param name="lhs">The left hand charset entry to compare</param>
		/// <param name="rhs">The right hand charset entry to compare</param>
		/// <returns>True if the charset entries are different, otherwise false</returns>
		public static bool operator !=(RegexCharsetClassEntry lhs, RegexCharsetClassEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
	}
	/// <summary>
	/// Represents a single character charset entry
	/// </summary>
#if REGEXLIB
	public 
#endif
	class RegexCharsetCharEntry : RegexCharsetEntry,IEquatable<RegexCharsetCharEntry>
	{
		/// <summary>
		/// Initializes the entry with a character
		/// </summary>
		/// <param name="value">The character to use</param>
		public RegexCharsetCharEntry(char value)
		{
			Value = value;
		}
		/// <summary>
		/// Initializes a default instance of the charset entry
		/// </summary>
		public RegexCharsetCharEntry() { }
		/// <summary>
		/// Indicates the character the charset entry represents
		/// </summary>
		public char Value { get; set; }
		/// <summary>
		/// Gets a string representation of the charset entry
		/// </summary>
		/// <returns>The string representation of this charset entry</returns>
		public override string ToString()
		{
			return RegexExpression.EscapeRangeChar(Value);
		}
		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A new copy of the charset entry</returns>
		protected override RegexCharsetEntry CloneImpl()
			=> Clone();
		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A new copy of the charset entry</returns>
		public RegexCharsetCharEntry Clone()
		{
			return new RegexCharsetCharEntry(Value);
		}

		#region Value semantics
		/// <summary>
		/// Indicates whether this charset entry is the same as the right hand charset entry
		/// </summary>
		/// <param name="rhs">The charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public bool Equals(RegexCharsetCharEntry rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return Value == rhs.Value;
		}
		/// <summary>
		/// Indicates whether this charset entry is the same as the right hand charset entry
		/// </summary>
		/// <param name="rhs">The charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public override bool Equals(object rhs)
			=> Equals(rhs as RegexCharsetCharEntry);
		/// <summary>
		/// Computes a hash code for this charset entry
		/// </summary>
		/// <returns>A hash code for this charset entry</returns>
		public override int GetHashCode()
			=> Value.GetHashCode();
		/// <summary>
		/// Indicates whether or not two charset entries are the same
		/// </summary>
		/// <param name="lhs">The left hand charset entry to compare</param>
		/// <param name="rhs">The right hand charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public static bool operator ==(RegexCharsetCharEntry lhs, RegexCharsetCharEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates whether or not two charset entries are different
		/// </summary>
		/// <param name="lhs">The left hand charset entry to compare</param>
		/// <param name="rhs">The right hand charset entry to compare</param>
		/// <returns>True if the charset entries are different, otherwise false</returns>
		public static bool operator !=(RegexCharsetCharEntry lhs, RegexCharsetCharEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
	}
	/// <summary>
	/// Represents a character set range entry
	/// </summary>
#if REGEXLIB
	public 
#endif
	class RegexCharsetRangeEntry : RegexCharsetEntry
	{
		/// <summary>
		/// Creates a new range entry with the specified first and last characters
		/// </summary>
		/// <param name="first">The first character in the range</param>
		/// <param name="last">The last character in the range</param>
		public RegexCharsetRangeEntry(char first, char last)
		{
			First = first;
			Last = last;
		}
		/// <summary>
		/// Creates a default instance of the range entry
		/// </summary>
		public RegexCharsetRangeEntry()
		{
		}
		/// <summary>
		/// Indicates the first character in the range
		/// </summary>
		public char First { get; set; }
		/// <summary>
		/// Indicates the last character in the range
		/// </summary>
		public char Last { get; set;  }
		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A new copy of the charset entry</returns>
		protected override RegexCharsetEntry CloneImpl()
			=> Clone();
		/// <summary>
		/// Clones the object
		/// </summary>
		/// <returns>A new copy of the charset entry</returns>
		public RegexCharsetRangeEntry Clone()
		{
			return new RegexCharsetRangeEntry(First,Last);
		}
		/// <summary>
		/// Gets a string representation of the charset entry
		/// </summary>
		/// <returns>The string representation of this charset entry</returns>
		public override string ToString()
		{
			if (1 == Last - First)
				return string.Concat(RegexExpression.EscapeRangeChar(First), RegexExpression.EscapeRangeChar(Last));
			if(2==Last-First)
				return string.Concat(RegexExpression.EscapeRangeChar(First), RegexExpression.EscapeRangeChar((char)(First+1)), RegexExpression.EscapeRangeChar(Last));
			return string.Concat(RegexExpression.EscapeRangeChar(First), "-", RegexExpression.EscapeRangeChar(Last));
		}
		#region Value semantics
		/// <summary>
		/// Indicates whether this charset entry is the same as the right hand charset entry
		/// </summary>
		/// <param name="rhs">The charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public bool Equals(RegexCharsetRangeEntry rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			return First == rhs.First && Last==rhs.Last;
		}
		/// <summary>
		/// Indicates whether this charset entry is the same as the right hand charset entry
		/// </summary>
		/// <param name="rhs">The charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public override bool Equals(object rhs)
			=> Equals(rhs as RegexCharsetRangeEntry);
		/// <summary>
		/// Computes a hash code for this charset entry
		/// </summary>
		/// <returns>A hash code for this charset entry</returns>
		public override int GetHashCode()
			=> First.GetHashCode() ^ Last.GetHashCode();
		/// <summary>
		/// Indicates whether or not two charset entries are the same
		/// </summary>
		/// <param name="lhs">The left hand charset entry to compare</param>
		/// <param name="rhs">The right hand charset entry to compare</param>
		/// <returns>True if the charset entries are the same, otherwise false</returns>
		public static bool operator ==(RegexCharsetRangeEntry lhs, RegexCharsetRangeEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		/// <summary>
		/// Indicates whether or not two charset entries are different
		/// </summary>
		/// <param name="lhs">The left hand charset entry to compare</param>
		/// <param name="rhs">The right hand charset entry to compare</param>
		/// <returns>True if the charset entries are different, otherwise false</returns>
		public static bool operator !=(RegexCharsetRangeEntry lhs, RegexCharsetRangeEntry rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
	}
}
