using System;
using System.Collections.Generic;
using System.Text;
using System.Globalization;
using System.IO;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.CodeDom;
using System.Collections;
using System.Reflection;
using System.Diagnostics;
using System.Net;
namespace RE{/// <summary>
/// Represents a binary expression
/// </summary>
#if REGEXLIB
public
#endif
abstract class RegexBinaryExpression:RegexExpression{/// <summary>
/// Indicates the left hand expression
/// </summary>
public RegexExpression Left{get;set;}/// <summary>
/// Indicates the right hand expression
/// </summary>
public RegexExpression Right{get;set;}}}namespace RE{/// <summary>
/// Represents the base class for regex charset entries
/// </summary>
#if REGEXLIB
public
#endif
abstract class RegexCharsetEntry:ICloneable{/// <summary>
/// Initializes the charset entry
/// </summary>
internal RegexCharsetEntry(){}/// <summary>
/// Implements the clone method
/// </summary>
/// <returns>A copy of the charset entry</returns>
protected abstract RegexCharsetEntry CloneImpl();/// <summary>
/// Creates a copy of the charset entry
/// </summary>
/// <returns>A new copy of the charset entry</returns>
object ICloneable.Clone()=>CloneImpl();}/// <summary>
/// Represents a unicode character category, such as \p{Lu} (uppercase letter)
/// </summary>
#if REGEXLIB
public
#endif
class RegexCharsetUnicodeCategoryEntry:RegexCharsetEntry{string _category;/// <summary>
/// Initializes the charset entry with the specified unicode category
/// </summary>
/// <param name="category">The unicode category</param>
public RegexCharsetUnicodeCategoryEntry(string category){Category=category;}/// <summary>
/// Indicates the Unicode category
/// </summary>
public string Category{get{return _category;}set{if(!CharFA<string>.UnicodeCategories.ContainsKey(value))throw new ArgumentException("The value was not a valid unicode category.");
_category=value;}}/// <summary>
/// Returns a string indicating the string representation of this entry
/// </summary>
/// <returns>A string representing this entry</returns>
public override string ToString(){return string.Concat("\\p{",Category,"}");}/// <summary>
/// Clones the object
/// </summary>
/// <returns>A clone of the object</returns>
protected override RegexCharsetEntry CloneImpl(){return new RegexCharsetUnicodeCategoryEntry(Category);}
#region Value semantics
/// <summary>
/// Indicates whether this unicode category entry is the same as the right hand unicode category entry
/// </summary>
/// <param name="rhs">The unicode category entry to compare</param>
/// <returns>True if the unicode category entries are the same, otherwise false</returns>
public bool Equals(RegexCharsetUnicodeCategoryEntry rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return 0==
string.Compare(Category,rhs.Category,StringComparison.InvariantCultureIgnoreCase);}/// <summary>
/// Indicates whether this unicode category entry is the same as the right hand unicode category entry
/// </summary>
/// <param name="rhs">The unicode category entry to compare</param>
/// <returns>True if the unicode category entries are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexCharsetUnicodeCategoryEntry);/// <summary>
/// Computes a hash code for this unicode category entry
/// </summary>
/// <returns>A hash code for this unicode category entry</returns>
public override int GetHashCode(){if(null==Category)return 0;return Category.ToUpperInvariant().GetHashCode();}/// <summary>
/// Indicates whether or not two unicode category entries are the same
/// </summary>
/// <param name="lhs">The left hand unicode category entry to compare</param>
/// <param name="rhs">The right hand unicode category entry to compare</param>
/// <returns>True if the unicode category entries are the same, otherwise false</returns>
public static bool operator==(RegexCharsetUnicodeCategoryEntry lhs,RegexCharsetUnicodeCategoryEntry rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,
null))return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two unicode category entries are different
/// </summary>
/// <param name="lhs">The left hand unicode category entry to compare</param>
/// <param name="rhs">The right hand unicode category entry to compare</param>
/// <returns>True if the unicode category entries are different, otherwise false</returns>
public static bool operator!=(RegexCharsetUnicodeCategoryEntry lhs,RegexCharsetUnicodeCategoryEntry rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,
null))return true;return!lhs.Equals(rhs);}
#endregion
}/// <summary>
/// Represents a character class charset entry
/// </summary>
#if REGEXLIB
public
#endif
class RegexCharsetClassEntry:RegexCharsetEntry{/// <summary>
/// Initializes a charset entry with the specified character class
/// </summary>
/// <param name="name">The name of the character class</param>
public RegexCharsetClassEntry(string name){Name=name;}/// <summary>
/// Initializes a default instance of the charset entry
/// </summary>
public RegexCharsetClassEntry(){}/// <summary>
/// Indicates the name of the character class
/// </summary>
public string Name{get;set;}/// <summary>
/// Gets a string representation of this instance
/// </summary>
/// <returns>The string representation of this character class</returns>
public override string ToString(){return string.Concat("[:",Name,":]");}/// <summary>
/// Clones the object
/// </summary>
/// <returns>A new copy of the charset entry</returns>
protected override RegexCharsetEntry CloneImpl()=>Clone();/// <summary>
/// Clones the object
/// </summary>
/// <returns>A new copy of the charset entry</returns>
public RegexCharsetClassEntry Clone(){return new RegexCharsetClassEntry(Name);}
#region Value semantics
/// <summary>
/// Indicates whether this charset entry is the same as the right hand charset entry
/// </summary>
/// <param name="rhs">The charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public bool Equals(RegexCharsetClassEntry rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return Name==rhs.Name;
}/// <summary>
/// Indicates whether this charset entry is the same as the right hand charset entry
/// </summary>
/// <param name="rhs">The charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexCharsetClassEntry);/// <summary>
/// Computes a hash code for this charset entry
/// </summary>
/// <returns>A hash code for this charset entry</returns>
public override int GetHashCode(){if(string.IsNullOrEmpty(Name))return Name.GetHashCode();return 0;}/// <summary>
/// Indicates whether or not two charset entries are the same
/// </summary>
/// <param name="lhs">The left hand charset entry to compare</param>
/// <param name="rhs">The right hand charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public static bool operator==(RegexCharsetClassEntry lhs,RegexCharsetClassEntry rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))
return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two charset entries are different
/// </summary>
/// <param name="lhs">The left hand charset entry to compare</param>
/// <param name="rhs">The right hand charset entry to compare</param>
/// <returns>True if the charset entries are different, otherwise false</returns>
public static bool operator!=(RegexCharsetClassEntry lhs,RegexCharsetClassEntry rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))
return true;return!lhs.Equals(rhs);}
#endregion
}/// <summary>
/// Represents a single character charset entry
/// </summary>
#if REGEXLIB
public
#endif
class RegexCharsetCharEntry:RegexCharsetEntry,IEquatable<RegexCharsetCharEntry>{/// <summary>
/// Initializes the entry with a character
/// </summary>
/// <param name="value">The character to use</param>
public RegexCharsetCharEntry(char value){Value=value;}/// <summary>
/// Initializes a default instance of the charset entry
/// </summary>
public RegexCharsetCharEntry(){}/// <summary>
/// Indicates the character the charset entry represents
/// </summary>
public char Value{get;set;}/// <summary>
/// Gets a string representation of the charset entry
/// </summary>
/// <returns>The string representation of this charset entry</returns>
public override string ToString(){return RegexExpression.EscapeRangeChar(Value);}/// <summary>
/// Clones the object
/// </summary>
/// <returns>A new copy of the charset entry</returns>
protected override RegexCharsetEntry CloneImpl()=>Clone();/// <summary>
/// Clones the object
/// </summary>
/// <returns>A new copy of the charset entry</returns>
public RegexCharsetCharEntry Clone(){return new RegexCharsetCharEntry(Value);}
#region Value semantics
/// <summary>
/// Indicates whether this charset entry is the same as the right hand charset entry
/// </summary>
/// <param name="rhs">The charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public bool Equals(RegexCharsetCharEntry rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return Value==rhs.Value;
}/// <summary>
/// Indicates whether this charset entry is the same as the right hand charset entry
/// </summary>
/// <param name="rhs">The charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexCharsetCharEntry);/// <summary>
/// Computes a hash code for this charset entry
/// </summary>
/// <returns>A hash code for this charset entry</returns>
public override int GetHashCode()=>Value.GetHashCode();/// <summary>
/// Indicates whether or not two charset entries are the same
/// </summary>
/// <param name="lhs">The left hand charset entry to compare</param>
/// <param name="rhs">The right hand charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public static bool operator==(RegexCharsetCharEntry lhs,RegexCharsetCharEntry rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))
return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two charset entries are different
/// </summary>
/// <param name="lhs">The left hand charset entry to compare</param>
/// <param name="rhs">The right hand charset entry to compare</param>
/// <returns>True if the charset entries are different, otherwise false</returns>
public static bool operator!=(RegexCharsetCharEntry lhs,RegexCharsetCharEntry rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))
return true;return!lhs.Equals(rhs);}
#endregion
}/// <summary>
/// Represents a character set range entry
/// </summary>
#if REGEXLIB
public
#endif
class RegexCharsetRangeEntry:RegexCharsetEntry{/// <summary>
/// Creates a new range entry with the specified first and last characters
/// </summary>
/// <param name="first">The first character in the range</param>
/// <param name="last">The last character in the range</param>
public RegexCharsetRangeEntry(char first,char last){First=first;Last=last;}/// <summary>
/// Creates a default instance of the range entry
/// </summary>
public RegexCharsetRangeEntry(){}/// <summary>
/// Indicates the first character in the range
/// </summary>
public char First{get;set;}/// <summary>
/// Indicates the last character in the range
/// </summary>
public char Last{get;set;}/// <summary>
/// Clones the object
/// </summary>
/// <returns>A new copy of the charset entry</returns>
protected override RegexCharsetEntry CloneImpl()=>Clone();/// <summary>
/// Clones the object
/// </summary>
/// <returns>A new copy of the charset entry</returns>
public RegexCharsetRangeEntry Clone(){return new RegexCharsetRangeEntry(First,Last);}/// <summary>
/// Gets a string representation of the charset entry
/// </summary>
/// <returns>The string representation of this charset entry</returns>
public override string ToString(){if(1==Last-First)return string.Concat(RegexExpression.EscapeRangeChar(First),RegexExpression.EscapeRangeChar(Last));
if(2==Last-First)return string.Concat(RegexExpression.EscapeRangeChar(First),RegexExpression.EscapeRangeChar((char)(First+1)),RegexExpression.EscapeRangeChar(Last));
return string.Concat(RegexExpression.EscapeRangeChar(First),"-",RegexExpression.EscapeRangeChar(Last));}
#region Value semantics
/// <summary>
/// Indicates whether this charset entry is the same as the right hand charset entry
/// </summary>
/// <param name="rhs">The charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public bool Equals(RegexCharsetRangeEntry rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return First==rhs.First
&&Last==rhs.Last;}/// <summary>
/// Indicates whether this charset entry is the same as the right hand charset entry
/// </summary>
/// <param name="rhs">The charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexCharsetRangeEntry);/// <summary>
/// Computes a hash code for this charset entry
/// </summary>
/// <returns>A hash code for this charset entry</returns>
public override int GetHashCode()=>First.GetHashCode()^Last.GetHashCode();/// <summary>
/// Indicates whether or not two charset entries are the same
/// </summary>
/// <param name="lhs">The left hand charset entry to compare</param>
/// <param name="rhs">The right hand charset entry to compare</param>
/// <returns>True if the charset entries are the same, otherwise false</returns>
public static bool operator==(RegexCharsetRangeEntry lhs,RegexCharsetRangeEntry rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))
return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two charset entries are different
/// </summary>
/// <param name="lhs">The left hand charset entry to compare</param>
/// <param name="rhs">The right hand charset entry to compare</param>
/// <returns>True if the charset entries are different, otherwise false</returns>
public static bool operator!=(RegexCharsetRangeEntry lhs,RegexCharsetRangeEntry rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))
return true;return!lhs.Equals(rhs);}
#endregion
}}namespace RE{/// <summary>
/// Indicates a charset expression
/// </summary>
/// <remarks>Represented by [] in regular expression syntax</remarks>
#if REGEXLIB
public
#endif
class RegexCharsetExpression:RegexExpression,IEquatable<RegexCharsetExpression>{/// <summary>
/// Indicates the <see cref="RegexCharsetEntry"/> entries in the character set
/// </summary>
public IList<RegexCharsetEntry>Entries{get;}=new List<RegexCharsetEntry>();/// <summary>
/// Creates a new charset expression with the specified entries and optionally negated
/// </summary>
/// <param name="entries">The entries to initialize the charset with</param>
/// <param name="hasNegatedRanges">True if the range is a "not range" like [^], otherwise false</param>
public RegexCharsetExpression(IEnumerable<RegexCharsetEntry>entries,bool hasNegatedRanges=false){foreach(var entry in entries)Entries.Add(entry);HasNegatedRanges
=hasNegatedRanges;}/// <summary>
/// Creates a default instance of the expression
/// </summary>
public RegexCharsetExpression(){}/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <param name="accept">The accept symbol to use for this expression</param>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
public override CharFA<TAccept>ToFA<TAccept>(TAccept accept){var ranges=new List<CharRange>();for(int ic=Entries.Count,i=0;i<ic;++i){var entry=Entries[i];
var crc=entry as RegexCharsetCharEntry;if(null!=crc)ranges.Add(new CharRange(crc.Value,crc.Value));var crr=entry as RegexCharsetRangeEntry;if(null!=crr)
ranges.Add(new CharRange(crr.First,crr.Last));var crcl=entry as RegexCharsetClassEntry;if(null!=crcl)ranges.AddRange(CharFA<TAccept>.CharacterClasses[crcl.Name]);
var cruc=entry as RegexCharsetUnicodeCategoryEntry;if(null!=cruc)ranges.AddRange(CharFA<TAccept>.UnicodeCategories[cruc.Category]);}if(HasNegatedRanges)
return CharFA<TAccept>.Set(CharRange.NotRanges(ranges),accept);return CharFA<TAccept>.Set(ranges,accept);}/// <summary>
/// Indicates whether the range is a "not range"
/// </summary>
/// <remarks>This is represented by the [^] regular expression syntax</remarks>
public bool HasNegatedRanges{get;set;}=false;/// <summary>
/// Indicates whether or not this statement is a single element or not
/// </summary>
/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
public override bool IsSingleElement=>true;/// <summary>
/// Appends the textual representation to a <see cref="StringBuilder"/>
/// </summary>
/// <param name="sb">The string builder to use</param>
/// <remarks>Used by ToString()</remarks>
protected internal override void AppendTo(StringBuilder sb){ if(1==Entries.Count){var dotE=Entries[0]as RegexCharsetRangeEntry;if(!HasNegatedRanges&&null
!=dotE&&dotE.First==char.MinValue&&dotE.Last==char.MaxValue){sb.Append(".");return;}var uc=Entries[0]as RegexCharsetUnicodeCategoryEntry;if(null!=uc){
if(!HasNegatedRanges)sb.Append(@"\p{");else sb.Append(@"\P{");sb.Append(uc.Category);sb.Append("}");return;}var cls=Entries[0]as RegexCharsetClassEntry;
if(null!=cls){switch(cls.Name){case"blank":if(!HasNegatedRanges)sb.Append(@"\h");return;case"digit":if(!HasNegatedRanges)sb.Append(@"\d");else sb.Append(@"\D");
return;case"lower":if(!HasNegatedRanges)sb.Append(@"\l");return;case"space":if(!HasNegatedRanges)sb.Append(@"\s");else sb.Append(@"\S");return;case"upper":
if(!HasNegatedRanges)sb.Append(@"\u");return;case"word":if(!HasNegatedRanges)sb.Append(@"\w");else sb.Append(@"\W");return;}}}sb.Append('[');if(HasNegatedRanges)
sb.Append('^');for(int ic=Entries.Count,i=0;i<ic;++i)sb.Append(Entries[i]);sb.Append(']');}/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
protected override RegexExpression CloneImpl()=>Clone();/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
public RegexCharsetExpression Clone(){return new RegexCharsetExpression(Entries,HasNegatedRanges);}
#region Value semantics
/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public bool Equals(RegexCharsetExpression rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;if(HasNegatedRanges==rhs.HasNegatedRanges
&&rhs.Entries.Count==Entries.Count){for(int ic=Entries.Count,i=0;i<ic;++i)if(Entries[i]!=rhs.Entries[i])return false;return true;}return false;}/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexCharsetExpression);/// <summary>
/// Computes a hash code for this expression
/// </summary>
/// <returns>A hash code for this expression</returns>
public override int GetHashCode(){var result=HasNegatedRanges.GetHashCode();for(int ic=Entries.Count,i=0;i<ic;++i)result^=Entries[i].GetHashCode();return
 result;}/// <summary>
/// Indicates whether or not two expression are the same
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public static bool operator==(RegexCharsetExpression lhs,RegexCharsetExpression rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))
return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two expression are different
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are different, otherwise false</returns>
public static bool operator!=(RegexCharsetExpression lhs,RegexCharsetExpression rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))
return true;return!lhs.Equals(rhs);}
#endregion
}}namespace RE{/// <summary>
/// Represents a concatenation between two expression. This has no operator as it is implicit.
/// </summary>
#if REGEXLIB
public
#endif
class RegexConcatExpression:RegexBinaryExpression,IEquatable<RegexConcatExpression>{/// <summary>
/// Indicates whether or not this statement is a single element or not
/// </summary>
/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
public override bool IsSingleElement{get{if(null==Left)return null==Right?false:Right.IsSingleElement;else if(null==Right)return Left.IsSingleElement;
return false;}}/// <summary>
/// Creates a new expression with the specified left and right hand sides
/// </summary>
/// <param name="left">The left expression</param>
/// <param name="right">The right expressions</param>
public RegexConcatExpression(RegexExpression left,params RegexExpression[]right){Left=left;for(int i=0;i<right.Length;i++){var r=right[i];if(null==Right)
Right=r;if(i!=right.Length-1){var c=new RegexConcatExpression();c.Left=Left;c.Right=Right;Right=null;Left=c;}}}/// <summary>
/// Creates a default instance of the expression
/// </summary>
public RegexConcatExpression(){}/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <param name="accept">The accept symbol to use for this expression</param>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
public override CharFA<TAccept>ToFA<TAccept>(TAccept accept){if(null==Left)return(null!=Right)?Right.ToFA(accept):null;else if(null==Right)return Left.ToFA(accept);
var result=CharFA<TAccept>.Concat(new CharFA<TAccept>[]{Left.ToFA(accept),Right.ToFA(accept)},accept);if(null!=Left as RegexConcatExpression||null!=Right
 as RegexConcatExpression)result.TrimNeutrals();return result;}/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
protected override RegexExpression CloneImpl()=>Clone();/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
public RegexConcatExpression Clone(){return new RegexConcatExpression(Left,Right);}
#region Value semantics
/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public bool Equals(RegexConcatExpression rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return Left==rhs.Left
&&Right==rhs.Right;}/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexConcatExpression);/// <summary>
/// Computes a hash code for this expression
/// </summary>
/// <returns>A hash code for this expression</returns>
public override int GetHashCode(){var result=0;if(null!=Left)result^=Left.GetHashCode();if(null!=Right)result^=Right.GetHashCode();return result;}/// <summary>
/// Indicates whether or not two expression are the same
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public static bool operator==(RegexConcatExpression lhs,RegexConcatExpression rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))
return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two expression are different
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are different, otherwise false</returns>
public static bool operator!=(RegexConcatExpression lhs,RegexConcatExpression rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))
return true;return!lhs.Equals(rhs);}
#endregion
/// <summary>
/// Appends the textual representation to a <see cref="StringBuilder"/>
/// </summary>
/// <param name="sb">The string builder to use</param>
/// <remarks>Used by ToString()</remarks>
protected internal override void AppendTo(StringBuilder sb){if(null!=Left){var oe=Left as RegexOrExpression;if(null!=oe)sb.Append('(');Left.AppendTo(sb);
if(null!=oe)sb.Append(')');}if(null!=Right){var oe=Right as RegexOrExpression;if(null!=oe)sb.Append('(');Right.AppendTo(sb);if(null!=oe)sb.Append(')');
}}}}namespace RE{/// <summary>
/// Represents the common functionality of all regular expression elements
/// </summary>
#if REGEXLIB
public
#endif
abstract class RegexExpression:ICloneable{/// <summary>
/// Indicates the 1 based line on which the regular expression was found
/// </summary>
public int Line{get;set;}=1;/// <summary>
/// Indicates the 1 based column on which the regular expression was found
/// </summary>
public int Column{get;set;}=1;/// <summary>
/// Indicates the 0 based position on which the regular expression was found
/// </summary>
public long Position{get;set;}=0L;/// <summary>
/// Indicates whether or not this statement is a single element or not
/// </summary>
/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
public abstract bool IsSingleElement{get;}/// <summary>
/// Sets the location information for the expression
/// </summary>
/// <param name="line">The 1 based line where the expression appears</param>
/// <param name="column">The 1 based column where the expression appears</param>
/// <param name="position">The 0 based position where the expression appears</param>
public void SetLocation(int line,int column,long position){Line=line;Column=column;Position=position;}/// <summary>
/// Creates a copy of the expression
/// </summary>
/// <returns>A copy of the expression</returns>
protected abstract RegexExpression CloneImpl();object ICloneable.Clone()=>CloneImpl();/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
public CharFA<TAccept>ToFA<TAccept>()=>ToFA(default(TAccept));/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <param name="accept">The accept symbol to use for this expression</param>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
public abstract CharFA<TAccept>ToFA<TAccept>(TAccept accept);/// <summary>
/// EXPERIMENTAL, INCOMPLETE
/// Builds an abstract syntax tree/DOM from the specified state machine
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol</typeparam>
/// <param name="fa">The state machine to analyze</param>
/// <param name="progress">An optional <see cref="IProgress{CharFAProgress}"/> instance used to report the progress of the task</param>
/// <returns>A regular expression syntax tree representing <paramref name="fa"/></returns>
public static RegexExpression FromFA<TAccept>(CharFA<TAccept>fa,IProgress<CharFAProgress>progress=null){ fa=_ToGnfa(fa,progress);var closure=fa.FillClosure();
var tt=_GetTransitionTable(closure);var ttl=tt as IList<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>>;var fsm=closure[0];
for(var z=0;z<10;++z){Console.WriteLine("Running state removal iteration on:");_DumpTransitionTable(closure,tt);var trns=_GetTransOfState(tt,fsm,true);
 for(int kc=trns.Count,k=0;k<kc;++k){var t=trns[k];var ti=tt.IndexOfKey(t.Key);var qin=t.Key.Key;var qrip=t.Key.Value;var inExpr=t.Value; var dstTrns=
_GetTransOfState(tt,qrip,true);for(int mc=dstTrns.Count,m=0;m<mc;++m){var dt=dstTrns[m];dt=new KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,
RegexExpression>(new KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(dt.Key.Key,dt.Key.Value),tt[dt.Key]);var qout=dt.Key.Value;var ripExpr=dt.Value;var
 isLoop=closure.IndexOf(qout)<=closure.IndexOf(qrip);var e=inExpr;if(null!=e)e=new RegexConcatExpression(e,ripExpr);else e=ripExpr;if(isLoop){Console.WriteLine("Found loop on {0}",
_TransToString(closure,dt));e=new RegexRepeatExpression(e);Console.WriteLine("Enum transitions of q{0}",closure.IndexOf(qout));var rtrns=_GetTransOfState(tt,
qout,true);for(int qc=rtrns.Count,q=0;q<qc;++q){var rt=rtrns[q]; if(rt.Key.Value!=qout){Console.WriteLine("Prepending loop to {0}",_TransToString(closure,
rt));tt[rt.Key]=new RegexConcatExpression(e,tt[rt.Key]);}else{Console.WriteLine("Removing loop {0}",_TransToString(closure,rt));if(!tt.Remove(rt.Key))
Console.WriteLine("Failure removing {0}",_TransToString(closure,rt));}}Console.WriteLine("Removing loop {0}",_TransToString(closure,dt));if(!tt.Remove(dt.Key))
Console.WriteLine("Failure removing {0}",_TransToString(closure,dt));} var nt=new KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>(new
 KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(qin,qout),e);RegexExpression lhs;if(0==m){if(!tt.TryGetValue(nt.Key,out lhs)){ttl[ti]=nt;}else{var tti=
tt.IndexOfKey(nt.Key); ttl[tti]=new KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>(nt.Key,new RegexOrExpression(lhs,nt.Value));
}}else{if(!tt.TryGetValue(nt.Key,out lhs)){ttl.Insert(1,nt);}else{ttl.Insert(1,new KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>(nt.Key,
new RegexOrExpression(lhs,nt.Value)));}}}}}return ttl[0].Value;}static RegexExpression FromFA2<TAccept>(CharFA<TAccept>fa,IProgress<CharFAProgress>progress
=null){ fa=_ToGnfa(fa,progress);var closure=fa.FillClosure();var tt=_GetTransitionTable(closure);var ttl=tt as IList<KeyValuePair<KeyValuePair<CharFA<TAccept>,
CharFA<TAccept>>,RegexExpression>>;var fsm=closure[0];while(1<tt.Count){Console.WriteLine("Running state removal iteration");Console.WriteLine();Console.WriteLine("Transition table:");
_DumpTransitionTable(closure,tt); var trns=_GetTransOfState(tt,fsm,true);for(int kc=trns.Count,k=0;k<kc;++k){var t=trns[k]; t=new KeyValuePair<KeyValuePair<CharFA<TAccept>,
CharFA<TAccept>>,RegexExpression>(t.Key,tt[t.Key]);Console.WriteLine("Process {0}",_TransToString(closure,t)); var trns2=_GetTransOfState(tt,t.Key.Value,
true);for(int mc=trns2.Count,m=0;m<mc;++m){Console.WriteLine();var td=trns2[m];var isLoop=(closure.IndexOf(td.Key.Value)<=closure.IndexOf(t.Key.Value));
 var e=t.Value; if(null!=e)e=new RegexConcatExpression(e,td.Value);else e=td.Value;if(isLoop){var rce=e as RegexConcatExpression;rce.Right=new RegexRepeatExpression(rce.Right);
} var key=new KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(t.Key.Key,td.Key.Value);RegexExpression v; if(tt.TryGetValue(key,out v)){e=new RegexOrExpression(e,
v);Console.WriteLine("Set q{0}->q{1} to {2}",closure.IndexOf(t.Key.Key),closure.IndexOf(t.Key.Value),e);tt[t.Key]=e;}else{ var i=tt.IndexOfKey(t.Key);
if(-1==i)System.Diagnostics.Debugger.Break();var nt=new KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>(key,e);;Console.WriteLine("Set {0} to {1}",
_TransToString(closure,ttl[i]),_TransToString(closure,nt));ttl[i]=nt;} Console.WriteLine("Remove q{0}->q{1}",closure.IndexOf(t.Key.Value),closure.IndexOf(td.Key.Value));
if(!tt.Remove(new KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(t.Key.Value,td.Key.Value)))Console.WriteLine("Remove failure");if(isLoop){ Console.WriteLine("Prepend to q{0}",
closure.IndexOf(t.Key.Key));foreach(var ttt in new List<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>>(tt)){if(t.Key.Key
==ttt.Key.Key){Console.WriteLine("Found q{0} - transition table:",closure.IndexOf(t.Key.Key));_DumpTransitionTable(closure,tt);if(closure.IndexOf(ttt.Key.Value)
>closure.IndexOf(t.Key.Key)){Console.WriteLine("Dump ttt: {0}",_TransToString(closure,ttt));Console.WriteLine("Dump t: {0}",_TransToString(closure,t));
if(null==ttt.Value||null==e)Console.WriteLine("expr was null");var ee=new RegexConcatExpression(e,ttt.Value);Console.WriteLine("Set q{0}->q{1} to {2}",
closure.IndexOf(ttt.Key.Key),closure.IndexOf(ttt.Key.Value),ee);tt[ttt.Key]=ee;}Console.Write("*");}Console.WriteLine("q{0}->q{1}: {2}",closure.IndexOf(ttt.Key.Key),
closure.IndexOf(ttt.Key.Value),ttt.Value);}Console.WriteLine("(Loop) Remove q{0}->q{1}: {2}",closure.IndexOf(t.Key.Key),closure.IndexOf(td.Key.Value),
t.Value);if(0<tt.Count&&!tt.Remove(new KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(t.Key.Key,td.Key.Value)))Console.WriteLine("Removal error");Console.WriteLine();
e=null;}}}Console.WriteLine("Enum after processing");_DumpTransitionTable(closure,tt);} return tt.GetAt(0);}private static CharFA<TAccept>_ToGnfa<TAccept>(CharFA<TAccept>
fa,IProgress<CharFAProgress>progress){fa=fa.ToDfa(progress);fa.TrimDuplicates(progress); fa.Finalize();var last=fa.FirstAcceptingState;if(!last.IsFinal)
{ last.IsAccepting=false;last.EpsilonTransitions.Add(new CharFA<TAccept>(true));}if(!fa.IsNeutral){ var nfa=new CharFA<TAccept>();nfa.EpsilonTransitions.Add(fa);
fa=nfa;}return fa;}static void _DumpTransitionTable<TAccept>(IList<CharFA<TAccept>>closure,ListDictionary<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,
RegexExpression>tt){foreach(var t in tt)Console.WriteLine("  {0}",_TransToString(closure,t));Console.WriteLine();}static string _TransToString<TAccept>(IList<CharFA<TAccept>>
closure,KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>trn){return string.Format("q{0}->q{1}: {2}",closure.IndexOf(trn.Key.Key),
closure.IndexOf(trn.Key.Value),trn.Value);}static ListDictionary<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>_GetTransitionTable<TAccept>(IList<CharFA<TAccept>>
closure){ var tt=new ListDictionary<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>();for(int ic=closure.Count,i=0;i<ic;++i){var ffa=closure[i];
var trgs=ffa.FillInputTransitionRangesGroupedByState();foreach(var fffa in ffa.EpsilonTransitions)tt.Add(new KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(ffa,
fffa),null);foreach(var trg in trgs){RegexExpression expr2;if(1==trg.Value.Count&&1==trg.Value[0].Length)expr2=new RegexLiteralExpression(trg.Value[0][0]);
else{var csel=new List<RegexCharsetEntry>();foreach(var rng in trg.Value)if(rng.First==rng.Last)csel.Add(new RegexCharsetCharEntry(rng.First));else csel.Add(new
 RegexCharsetRangeEntry(rng.First,rng.Last));expr2=new RegexCharsetExpression(csel);}tt.Add(new KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(ffa,trg.Key),
expr2);}}return tt;}private static void _ProcessLoops<TAccept>(ListDictionary<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>tt,IList<CharFA<TAccept>>
closure,IList<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>>trns){ foreach(var t in trns){ foreach(var td in _GetTransOfState(tt,
t.Key.Value,true)){ if(closure.IndexOf(td.Key.Value)<=closure.IndexOf(t.Key.Value)){Console.WriteLine("Loop detected"); var e=t.Value; if(null!=e)e=new
 RegexConcatExpression(e,td.Value);else e=td.Value;e=new RegexRepeatExpression(e); Console.WriteLine("Transitions from q{0}",closure.IndexOf(t.Key.Key));
var srcs=_GetTransOfState(tt,t.Key.Key);foreach(var from in srcs){Console.WriteLine("Read src"); tt[from.Key]=new RegexConcatExpression(e,tt[from.Key]);
Console.WriteLine("Set loop q{0}->q{1}",closure.IndexOf(from.Key.Key),closure.IndexOf(from.Key.Value));}}}}}static void _EliminateSelfLoops<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>,
CharFA<TAccept>>,RegexExpression>tt,CharFA<TAccept>fa=null){if(null==fa){foreach(var t in new List<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,
RegexExpression>>(tt))_EliminateSelfLoops(tt,t.Key.Key);return;}var trns=_GetTransOfState(tt,fa,true); foreach(var t in trns){ if(t.Key.Key==t.Key.Value)
{ foreach(var t2 in _GetDstsToState(tt,t.Key.Key)) if(t2.Key.Key!=t2.Key.Value)tt[t2.Key]=new RegexConcatExpression(t2.Value,t.Value);tt.Remove(t.Key);
}}}static IList<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>>_GetTransOfState<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>,
CharFA<TAccept>>,RegexExpression>tt,CharFA<TAccept>fa,bool includeSelf=false){var result=new List<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,
RegexExpression>>();foreach(var trn in tt)if(trn.Key.Key==fa&&(includeSelf||fa!=trn.Key.Value))result.Add(trn);return result;}static IList<KeyValuePair<KeyValuePair<CharFA<TAccept>,
CharFA<TAccept>>,RegexExpression>>_GetDstsToState<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>tt,CharFA<TAccept>
fa){var result=new List<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>>();foreach(var t in tt)if(t.Key.Value==fa)result.Add(t);
return result;}static void _EliminateDeadStates<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>tt,IList<CharFA<TAccept>>
closure){ var dead=new List<CharFA<TAccept>>();foreach(var t in tt){var srci=closure.IndexOf(t.Key.Key); if(0!=srci){ var dsts=_GetDstsToState(tt,t.Key.Key);
if(0==dsts.Count)if(!dead.Contains(t.Key.Key))dead.Add(t.Key.Key);}}foreach(var fa in dead)foreach(var t in _GetTransOfState(tt,fa,true))tt.Remove(t.Key);
}static RegexExpression _FromFA<TAccept>(CharFA<TAccept>fa,HashSet<CharFA<TAccept>>visited){if(!visited.Add(fa))return null;var trgs=fa.FillInputTransitionRangesGroupedByState();
bool isAccepting=fa.IsAccepting;RegexExpression expr=null;foreach(var trg in trgs){if(1==trg.Value.Count&&1==trg.Value[0].Length){RegexExpression le=new
 RegexLiteralExpression(trg.Value[0][0]);var next=_FromFA(trg.Key,visited);if(null!=next)le=new RegexConcatExpression(le,next);if(null==expr)expr=le;else
 expr=new RegexOrExpression(expr,le);}else{var csel=new List<RegexCharsetEntry>();foreach(var rng in trg.Value){if(rng.First==rng.Last)csel.Add(new RegexCharsetCharEntry(rng.First));
else csel.Add(new RegexCharsetRangeEntry(rng.First,rng.Last));}RegexExpression cse=new RegexCharsetExpression(csel);var next=_FromFA(trg.Key,visited);
if(null!=next)cse=new RegexConcatExpression(cse,next);if(null==expr)expr=cse;else expr=new RegexOrExpression(expr,cse);}}var isLoop=false;foreach(var val
 in fa.Descendants){if(val==fa){isLoop=true;break;}}if(isAccepting&&!fa.IsFinal&&!isLoop)expr=new RegexOptionalExpression(expr);return expr;}/// <summary>
/// Appends the textual representation to a <see cref="StringBuilder"/>
/// </summary>
/// <param name="sb">The string builder to use</param>
/// <remarks>Used by ToString()</remarks>
protected internal abstract void AppendTo(StringBuilder sb);/// <summary>
/// Gets a textual representation of the expression
/// </summary>
/// <returns>A string representing the expression</returns>
public override string ToString(){var result=new StringBuilder();AppendTo(result);return result.ToString();}/// <summary>
/// Parses a regular expresion from the specified string
/// </summary>
/// <param name="string">The string</param>
/// <returns>A new abstract syntax tree representing the expression</returns>
public static RegexExpression Parse(IEnumerable<char>@string)=>Parse(ParseContext.Create(@string));/// <summary>
/// Parses a regular expresion from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The text reader</param>
/// <returns>A new abstract syntax tree representing the expression</returns>
public static RegexExpression ReadFrom(TextReader reader)=>Parse(ParseContext.CreateFrom(reader));/// <summary>
/// Parses a regular expression from the specified <see cref="ParseContext"/>
/// </summary>
/// <param name="pc">The parse context to use</param>
/// <returns>A new abstract syntax tree representing the expression</returns>
public static RegexExpression Parse(ParseContext pc){RegexExpression result=null,next=null;int ich;pc.EnsureStarted();var line=pc.Line;var column=pc.Column;
var position=pc.Position;while(true){switch(pc.Current){case-1:return result;case'.':var nset=new RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetRangeEntry(char.MinValue,
char.MaxValue)},false);nset.SetLocation(line,column,position);if(null==result)result=nset;else{result=new RegexConcatExpression(result,nset);result.SetLocation(line,
column,position);}pc.Advance();result=_ParseModifier(result,pc);line=pc.Line;column=pc.Column;position=pc.Position;break;case'\\':pc.Advance();pc.Expecting();
var isNot=false;switch(pc.Current){case'P':isNot=true;goto case'p';case'p':pc.Advance();pc.Expecting('{');var uc=new StringBuilder();int uli=pc.Line;int
 uco=pc.Column;long upo=pc.Position;while(-1!=pc.Advance()&&'}'!=pc.Current)uc.Append((char)pc.Current);pc.Expecting('}');pc.Advance();IList<CharRange>
cr;if(!CharFA<string>.UnicodeCategories.TryGetValue(uc.ToString(),out cr))throw new ExpectingException(string.Format("Expecting a unicode category but found \"{0}\" at line {1}, column {2}, position {3}",uc.ToString(),uli,uco,upo),
uli,uco,upo);next=new RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetUnicodeCategoryEntry(uc.ToString())},isNot);break;case'd':next=new
 RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetClassEntry("digit")});pc.Advance();break;case'D':next=new RegexCharsetExpression(new RegexCharsetEntry[]
{new RegexCharsetClassEntry("digit")},true);pc.Advance();break;case'h':next=new RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetClassEntry("blank")
});pc.Advance();break;case'l':next=new RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetClassEntry("lower")});pc.Advance();break;case's':
next=new RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetClassEntry("space")});pc.Advance();break;case'S':next=new RegexCharsetExpression(new
 RegexCharsetEntry[]{new RegexCharsetClassEntry("space")},true);pc.Advance();break;case'u':next=new RegexCharsetExpression(new RegexCharsetEntry[]{new
 RegexCharsetClassEntry("upper")});pc.Advance();break;case'w':next=new RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetClassEntry("word")
});pc.Advance();break;case'W':next=new RegexCharsetExpression(new RegexCharsetEntry[]{new RegexCharsetClassEntry("word")},true);pc.Advance();break;default:
if(-1!=(ich=_ParseEscapePart(pc))){next=new RegexLiteralExpression((char)ich);}else{pc.Expecting(); return null;}break;}next.SetLocation(line,column,position);
next=_ParseModifier(next,pc);if(null!=result){result=new RegexConcatExpression(result,next);result.SetLocation(line,column,position);}else result=next;
line=pc.Line;column=pc.Column;position=pc.Position;break;case')':return result;case'(':pc.Advance();pc.Expecting();next=Parse(pc);pc.Expecting(')');pc.Advance();
next=_ParseModifier(next,pc);if(null==result)result=next;else{result=new RegexConcatExpression(result,next);result.SetLocation(line,column,position);}
line=pc.Line;column=pc.Column;position=pc.Position;break;case'|':if(-1!=pc.Advance()){next=Parse(pc);result=new RegexOrExpression(result,next);result.SetLocation(line,
column,position);}else{result=new RegexOrExpression(result,null);result.SetLocation(line,column,position);}line=pc.Line;column=pc.Column;position=pc.Position;
break;case'[':pc.ClearCapture();pc.Advance();pc.Expecting();bool not=false;if('^'==pc.Current){not=true;pc.Advance();pc.Expecting();}var ranges=_ParseRanges(pc);
if(ranges.Count==0)System.Diagnostics.Debugger.Break();pc.Expecting(']');pc.Advance();next=new RegexCharsetExpression(ranges,not);next.SetLocation(line,
column,position);next=_ParseModifier(next,pc);if(null==result)result=next;else{result=new RegexConcatExpression(result,next);result.SetLocation(pc.Line,
pc.Column,pc.Position);}line=pc.Line;column=pc.Column;position=pc.Position;break;default:ich=pc.Current;next=new RegexLiteralExpression((char)ich);next.SetLocation(line,
column,position);pc.Advance();next=_ParseModifier(next,pc);if(null==result)result=next;else{result=new RegexConcatExpression(result,next);result.SetLocation(line,
column,position);}line=pc.Line;column=pc.Column;position=pc.Position;break;}}}static IList<RegexCharsetEntry>_ParseRanges(ParseContext pc){pc.EnsureStarted();
var result=new List<RegexCharsetEntry>();RegexCharsetEntry next=null;bool readDash=false;while(-1!=pc.Current&&']'!=pc.Current){switch(pc.Current){case
'[': if(null!=next){result.Add(next);if(readDash)result.Add(new RegexCharsetCharEntry('-'));result.Add(new RegexCharsetCharEntry('-'));}pc.Advance();pc.Expecting(':');
pc.Advance();var l=pc.CaptureBuffer.Length;pc.TryReadUntil(':',false);var n=pc.GetCapture(l);pc.Advance();pc.Expecting(']');pc.Advance();result.Add(new
 RegexCharsetClassEntry(n));readDash=false;next=null;break;case'\\':pc.Advance();pc.Expecting();switch(pc.Current){case'h':_ParseCharClassEscape(pc,"space",
result,ref next,ref readDash);break;case'd':_ParseCharClassEscape(pc,"digit",result,ref next,ref readDash);break;case'D':_ParseCharClassEscape(pc,"^digit",
result,ref next,ref readDash);break;case'l':_ParseCharClassEscape(pc,"lower",result,ref next,ref readDash);break;case's':_ParseCharClassEscape(pc,"space",
result,ref next,ref readDash);break;case'S':_ParseCharClassEscape(pc,"^space",result,ref next,ref readDash);break;case'u':_ParseCharClassEscape(pc,"upper",
result,ref next,ref readDash);break;case'w':_ParseCharClassEscape(pc,"word",result,ref next,ref readDash);break;case'W':_ParseCharClassEscape(pc,"^word",
result,ref next,ref readDash);break;default:var ch=(char)_ParseRangeEscapePart(pc);if(null==next)next=new RegexCharsetCharEntry(ch);else if(readDash){
result.Add(new RegexCharsetRangeEntry(((RegexCharsetCharEntry)next).Value,ch));next=null;readDash=false;}else{result.Add(next);next=new RegexCharsetCharEntry(ch);
}break;}break;case'-':pc.Advance();if(null==next){next=new RegexCharsetCharEntry('-');readDash=false;}else{if(readDash)result.Add(next);readDash=true;
}break;default:if(null==next){next=new RegexCharsetCharEntry((char)pc.Current);}else{if(readDash){result.Add(new RegexCharsetRangeEntry(((RegexCharsetCharEntry)next).Value,
(char)pc.Current));next=null;readDash=false;}else{result.Add(next);next=new RegexCharsetCharEntry((char)pc.Current);}}pc.Advance();break;}}if(null!=next)
{result.Add(next);if(readDash){next=new RegexCharsetCharEntry('-');result.Add(next);}}return result;}static void _ParseCharClassEscape(ParseContext pc,
string cls,List<RegexCharsetEntry>result,ref RegexCharsetEntry next,ref bool readDash){if(null!=next){result.Add(next);if(readDash)result.Add(new RegexCharsetCharEntry('-'));
result.Add(new RegexCharsetCharEntry('-'));}pc.Advance();result.Add(new RegexCharsetClassEntry(cls));next=null;readDash=false;}static RegexExpression _ParseModifier(RegexExpression
 expr,ParseContext pc){var line=pc.Line;var column=pc.Column;var position=pc.Position;switch(pc.Current){case'*':expr=new RegexRepeatExpression(expr);
expr.SetLocation(line,column,position);pc.Advance();break;case'+':expr=new RegexRepeatExpression(expr,1);expr.SetLocation(line,column,position);pc.Advance();
break;case'?':expr=new RegexOptionalExpression(expr);expr.SetLocation(line,column,position);pc.Advance();break;case'{':pc.Advance();pc.TrySkipWhiteSpace();
pc.Expecting('0','1','2','3','4','5','6','7','8','9',',','}');var min=-1;var max=-1;if(','!=pc.Current&&'}'!=pc.Current){var l=pc.CaptureBuffer.Length;
pc.TryReadDigits();min=int.Parse(pc.GetCapture(l));pc.TrySkipWhiteSpace();}if(','==pc.Current){pc.Advance();pc.TrySkipWhiteSpace();pc.Expecting('0','1',
'2','3','4','5','6','7','8','9','}');if('}'!=pc.Current){var l=pc.CaptureBuffer.Length;pc.TryReadDigits();max=int.Parse(pc.GetCapture(l));pc.TrySkipWhiteSpace();
}}else{max=min;}pc.Expecting('}');pc.Advance();expr=new RegexRepeatExpression(expr,min,max);expr.SetLocation(line,column,position);break;}return expr;
}/// <summary>
/// Appends a character escape to the specified <see cref="StringBuilder"/>
/// </summary>
/// <param name="ch">The character to escape</param>
/// <param name="builder">The string builder to append to</param>
internal static void AppendEscapedChar(char ch,StringBuilder builder){switch(ch){case'.':case'/': case'(':case')':case'[':case']':case'<': case'>':case
'|':case';': case'\'': case'\"':case'{':case'}':case'?':case'*':case'+':case'$':case'^':case'\\':builder.Append('\\');builder.Append(ch);return;case'\t':
builder.Append("\\t");return;case'\n':builder.Append("\\n");return;case'\r':builder.Append("\\r");return;case'\0':builder.Append("\\0");return;case'\f':
builder.Append("\\f");return;case'\v':builder.Append("\\v");return;case'\b':builder.Append("\\b");return;default:if(!char.IsLetterOrDigit(ch)&&!char.IsSeparator(ch)
&&!char.IsPunctuation(ch)&&!char.IsSymbol(ch)){builder.Append("\\u");builder.Append(unchecked((ushort)ch).ToString("x4"));}else builder.Append(ch);break;
}}/// <summary>
/// Escapes the specified character
/// </summary>
/// <param name="ch">The character to escape</param>
/// <returns>A string representing the escaped character</returns>
internal static string EscapeChar(char ch){switch(ch){case'.':case'/': case'(':case')':case'[':case']':case'<': case'>':case'|':case';': case'\'': case
'\"':case'{':case'}':case'?':case'*':case'+':case'$':case'^':case'\\':return string.Concat("\\",ch.ToString());case'\t':return"\\t";case'\n':return"\\n";
case'\r':return"\\r";case'\0':return"\\0";case'\f':return"\\f";case'\v':return"\\v";case'\b':return"\\b";default:if(!char.IsLetterOrDigit(ch)&&!char.IsSeparator(ch)
&&!char.IsPunctuation(ch)&&!char.IsSymbol(ch)){return string.Concat("\\x",unchecked((ushort)ch).ToString("x4"));}else return string.Concat(ch);}}/// <summary>
/// Appends an escaped range character to the specified <see cref="StringBuilder"/>
/// </summary>
/// <param name="rangeChar">The range character to escape</param>
/// <param name="builder">The string builder to append to</param>
internal static void AppendEscapedRangeChar(char rangeChar,StringBuilder builder){switch(rangeChar){case'.':case'/': case'(':case')':case'[':case']':case
'<': case'>':case'|':case':': case';': case'\'': case'\"':case'{':case'}':case'?':case'*':case'+':case'$':case'^':case'-':case'\\':builder.Append('\\');
builder.Append(rangeChar);return;case'\t':builder.Append("\\t");return;case'\n':builder.Append("\\n");return;case'\r':builder.Append("\\r");return;case
'\0':builder.Append("\\0");return;case'\f':builder.Append("\\f");return;case'\v':builder.Append("\\v");return;case'\b':builder.Append("\\b");return;default:
if(!char.IsLetterOrDigit(rangeChar)&&!char.IsSeparator(rangeChar)&&!char.IsPunctuation(rangeChar)&&!char.IsSymbol(rangeChar)){builder.Append("\\u");builder.Append(unchecked((ushort)rangeChar).ToString("x4"));
}else builder.Append(rangeChar);break;}}/// <summary>
/// Escapes a range character
/// </summary>
/// <param name="ch">The character to escape</param>
/// <returns>A string containing the escaped character</returns>
internal static string EscapeRangeChar(char ch){switch(ch){case'.':case'/': case'(':case')':case'[':case']':case'<': case'>':case'|':case':': case';':
 case'\'': case'\"':case'{':case'}':case'?':case'*':case'+':case'$':case'^':case'-':case'\\':return string.Concat("\\",ch.ToString());case'\t':return"\\t";
case'\n':return"\\n";case'\r':return"\\r";case'\0':return"\\0";case'\f':return"\\f";case'\v':return"\\v";case'\b':return"\\b";default:if(!char.IsLetterOrDigit(ch)
&&!char.IsSeparator(ch)&&!char.IsPunctuation(ch)&&!char.IsSymbol(ch)){return string.Concat("\\x",unchecked((ushort)ch).ToString("x4"));}else return string.Concat(ch);
}}static byte _FromHexChar(char hex){if(':'>hex&&'/'<hex)return(byte)(hex-'0');if('G'>hex&&'@'<hex)return(byte)(hex-'7'); if('g'>hex&&'`'<hex)return(byte)(hex
-'W'); throw new ArgumentException("The value was not hex.","hex");}static bool _IsHexChar(char hex){if(':'>hex&&'/'<hex)return true;if('G'>hex&&'@'<hex)
return true;if('g'>hex&&'`'<hex)return true;return false;} static int _ParseEscapePart(ParseContext pc){if(-1==pc.Current)return-1;switch(pc.Current){
case'f':pc.Advance();return'\f';case'v':pc.Advance();return'\v';case't':pc.Advance();return'\t';case'n':pc.Advance();return'\n';case'r':pc.Advance();return
'\r';case'x':if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return'x';byte b=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))
return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b
|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);
return unchecked((char)b);case'u':if(-1==pc.Advance())return'u';ushort u=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return unchecked((char)u);
u|=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return unchecked((char)u);u|=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return
 unchecked((char)u);u|=_FromHexChar((char)pc.Current);return unchecked((char)u);default:int i=pc.Current;pc.Advance();return(char)i;}}static int _ParseRangeEscapePart(ParseContext
 pc){if(-1==pc.Current)return-1;switch(pc.Current){case'f':pc.Advance();return'\f';case'v':pc.Advance();return'\v';case't':pc.Advance();return'\t';case
'n':pc.Advance();return'\n';case'r':pc.Advance();return'\r';case'x':if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return'x';byte b=_FromHexChar((char)pc.Current);
if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))
return unchecked((char)b);b<<=4;b|=_FromHexChar((char)pc.Current);if(-1==pc.Advance()||!_IsHexChar((char)pc.Current))return unchecked((char)b);b<<=4;b
|=_FromHexChar((char)pc.Current);return unchecked((char)b);case'u':if(-1==pc.Advance())return'u';ushort u=_FromHexChar((char)pc.Current);u<<=4;if(-1==
pc.Advance())return unchecked((char)u);u|=_FromHexChar((char)pc.Current);u<<=4;if(-1==pc.Advance())return unchecked((char)u);u|=_FromHexChar((char)pc.Current);
u<<=4;if(-1==pc.Advance())return unchecked((char)u);u|=_FromHexChar((char)pc.Current);return unchecked((char)u);default:int i=pc.Current;pc.Advance();
return(char)i;}}static char _ReadRangeChar(IEnumerator<char>e){char ch;if('\\'!=e.Current||!e.MoveNext()){return e.Current;}ch=e.Current;switch(ch){case
't':ch='\t';break;case'n':ch='\n';break;case'r':ch='\r';break;case'0':ch='\0';break;case'v':ch='\v';break;case'f':ch='\f';break;case'b':ch='\b';break;
case'x':if(!e.MoveNext())throw new ExpectingException("Expecting input for escape \\x");ch=e.Current;byte x=_FromHexChar(ch);if(!e.MoveNext()){ch=unchecked((char)x);
return ch;}x*=0x10;x+=_FromHexChar(e.Current);ch=unchecked((char)x);break;case'u':if(!e.MoveNext())throw new ExpectingException("Expecting input for escape \\u");
ch=e.Current;ushort u=_FromHexChar(ch);if(!e.MoveNext()){ch=unchecked((char)u);return ch;}u*=0x10;u+=_FromHexChar(e.Current);if(!e.MoveNext()){ch=unchecked((char)u);
return ch;}u*=0x10;u+=_FromHexChar(e.Current);if(!e.MoveNext()){ch=unchecked((char)u);return ch;}u*=0x10;u+=_FromHexChar(e.Current);ch=unchecked((char)u);
break;default: break;}return ch;}static IEnumerable<CharRange>_ParseRanges(IEnumerable<char>charRanges){using(var e=charRanges.GetEnumerator()){var skipRead
=false;while(skipRead||e.MoveNext()){skipRead=false;char first=_ReadRangeChar(e);if(e.MoveNext()){if('-'==e.Current){if(e.MoveNext())yield return new CharRange(first,
_ReadRangeChar(e));else yield return new CharRange('-','-');}else{yield return new CharRange(first,first);skipRead=true;continue;}}else{yield return new
 CharRange(first,first);yield break;}}}yield break;}static IEnumerable<CharRange>_ParseRanges(IEnumerable<char>charRanges,bool normalize){if(!normalize)
return _ParseRanges(charRanges);else{var result=new List<CharRange>(_ParseRanges(charRanges));CharRange.NormalizeRangeList(result);return result;}}}}namespace
 RE{/// <summary>
/// Represents a single character literal
/// </summary>
#if REGEXLIB
public
#endif
class RegexLiteralExpression:RegexExpression,IEquatable<RegexLiteralExpression>{/// <summary>
/// Indicates whether or not this statement is a single element or not
/// </summary>
/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
public override bool IsSingleElement=>true;/// <summary>
/// Indicates the character literal of this expression
/// </summary>
public char Value{get;set;}=default(char);/// <summary>
/// Creates a series of concatenated literals representing the specified string
/// </summary>
/// <param name="value">The string to use</param>
/// <returns>An expression representing <paramref name="value"/></returns>
public static RegexExpression CreateString(string value){if(string.IsNullOrEmpty(value))return null;RegexExpression result=new RegexLiteralExpression(value[0]);
for(var i=1;i<value.Length;i++)result=new RegexConcatExpression(result,new RegexLiteralExpression(value[i]));return result;}/// <summary>
/// Creates a literal expression with the specified character
/// </summary>
/// <param name="value">The character to represent</param>
public RegexLiteralExpression(char value){Value=value;}/// <summary>
/// Creates a default instance of the expression
/// </summary>
public RegexLiteralExpression(){}/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <param name="accept">The accept symbol to use for this expression</param>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
public override CharFA<TAccept>ToFA<TAccept>(TAccept accept)=>CharFA<TAccept>.Literal(new char[]{Value},accept);/// <summary>
/// Appends the textual representation to a <see cref="StringBuilder"/>
/// </summary>
/// <param name="sb">The string builder to use</param>
/// <remarks>Used by ToString()</remarks>
protected internal override void AppendTo(StringBuilder sb)=>AppendEscapedChar(Value,sb);/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
protected override RegexExpression CloneImpl()=>Clone();/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
public RegexLiteralExpression Clone(){return new RegexLiteralExpression(Value);}
#region Value semantics
/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public bool Equals(RegexLiteralExpression rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return Value==rhs.Value;
}/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexLiteralExpression);/// <summary>
/// Computes a hash code for this expression
/// </summary>
/// <returns>A hash code for this expression</returns>
public override int GetHashCode()=>Value.GetHashCode();/// <summary>
/// Indicates whether or not two expression are the same
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public static bool operator==(RegexLiteralExpression lhs,RegexLiteralExpression rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))
return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two expression are different
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are different, otherwise false</returns>
public static bool operator!=(RegexLiteralExpression lhs,RegexLiteralExpression rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))
return true;return!lhs.Equals(rhs);}
#endregion
}}namespace RE{/// <summary>
/// Represents an optional expression, as indicated by ?
/// </summary>
#if REGEXLIB
public
#endif
class RegexOptionalExpression:RegexUnaryExpression,IEquatable<RegexOptionalExpression>{/// <summary>
/// Indicates whether or not this statement is a single element or not
/// </summary>
/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
public override bool IsSingleElement=>true;/// <summary>
/// Creates an optional expression using the specified target expression
/// </summary>
/// <param name="expression">The target expression to make optional</param>
public RegexOptionalExpression(RegexExpression expression){Expression=expression;}/// <summary>
/// Creates a default instance of the expression
/// </summary>
public RegexOptionalExpression(){}/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <param name="accept">The accept symbol to use for this expression</param>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
public override CharFA<TAccept>ToFA<TAccept>(TAccept accept)=>null!=Expression?CharFA<TAccept>.Optional(Expression.ToFA(accept),accept):null;/// <summary>
/// Appends the textual representation to a <see cref="StringBuilder"/>
/// </summary>
/// <param name="sb">The string builder to use</param>
/// <remarks>Used by ToString()</remarks>
protected internal override void AppendTo(StringBuilder sb){if(null==Expression)sb.Append("()?");else{var ise=Expression.IsSingleElement;if(!ise)sb.Append('(');
Expression.AppendTo(sb);if(!ise)sb.Append(")?");else sb.Append('?');}}/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
protected override RegexExpression CloneImpl()=>Clone();/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
public RegexOptionalExpression Clone(){return new RegexOptionalExpression(Expression);}
#region Value semantics
/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public bool Equals(RegexOptionalExpression rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return Equals(Expression,
rhs.Expression);}/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexOptionalExpression);/// <summary>
/// Computes a hash code for this expression
/// </summary>
/// <returns>A hash code for this expression</returns>
public override int GetHashCode(){if(null!=Expression)return Expression.GetHashCode();return 0;}/// <summary>
/// Indicates whether or not two expression are the same
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public static bool operator==(RegexOptionalExpression lhs,RegexOptionalExpression rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,
null))return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two expression are different
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are different, otherwise false</returns>
public static bool operator!=(RegexOptionalExpression lhs,RegexOptionalExpression rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,
null))return true;return!lhs.Equals(rhs);}
#endregion
}}namespace RE{/// <summary>
/// Represents an "or" regular expression as indicated by |
/// </summary>
#if REGEXLIB
public
#endif
class RegexOrExpression:RegexBinaryExpression,IEquatable<RegexOrExpression>{/// <summary>
/// Indicates whether or not this statement is a single element or not
/// </summary>
/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
public override bool IsSingleElement=>false;/// <summary>
/// Creates a new expression with the specified left and right hand sides
/// </summary>
/// <param name="left">The left expression</param>
/// <param name="right">The right expressions</param>
public RegexOrExpression(RegexExpression left,params RegexExpression[]right){Left=left;for(int i=0;i<right.Length;i++){var r=right[i];if(null==Right)Right
=r;if(i!=right.Length-1){var c=new RegexOrExpression();c.Left=Left;c.Right=Right;Right=null;Left=c;}}}/// <summary>
/// Creates a default instance of the expression
/// </summary>
public RegexOrExpression(){}/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <param name="accept">The accept symbol to use for this expression</param>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
public override CharFA<TAccept>ToFA<TAccept>(TAccept accept){var left=(null!=Left)?Left.ToFA(accept):null;var right=(null!=Right)?Right.ToFA(accept):null;
return CharFA<TAccept>.Or(new CharFA<TAccept>[]{left,right},accept);}/// <summary>
/// Appends the textual representation to a <see cref="StringBuilder"/>
/// </summary>
/// <param name="sb">The string builder to use</param>
/// <remarks>Used by ToString()</remarks>
protected internal override void AppendTo(StringBuilder sb){if(null!=Left)Left.AppendTo(sb);sb.Append('|');if(null!=Right)Right.AppendTo(sb);}/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
protected override RegexExpression CloneImpl()=>Clone();/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
public RegexOrExpression Clone(){return new RegexOrExpression(Left,Right);}
#region Value semantics
/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public bool Equals(RegexOrExpression rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;return(Left==rhs.Left&&Right
==rhs.Right)||(Left==rhs.Right&&Right==rhs.Left);}/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexOrExpression);/// <summary>
/// Computes a hash code for this expression
/// </summary>
/// <returns>A hash code for this expression</returns>
public override int GetHashCode(){var result=0;if(null!=Left)result^=Left.GetHashCode();if(null!=Right)result^=Right.GetHashCode();return result;}/// <summary>
/// Indicates whether or not two expression are the same
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public static bool operator==(RegexOrExpression lhs,RegexOrExpression rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))return
 false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two expression are different
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are different, otherwise false</returns>
public static bool operator!=(RegexOrExpression lhs,RegexOrExpression rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))return
 true;return!lhs.Equals(rhs);}
#endregion
}}namespace RE{/// <summary>
/// Represents a repeat regular expression as indicated by *, +, or {min,max}
/// </summary>
#if REGEXLIB
public
#endif
class RegexRepeatExpression:RegexUnaryExpression,IEquatable<RegexRepeatExpression>{/// <summary>
/// Indicates whether or not this statement is a single element or not
/// </summary>
/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
public override bool IsSingleElement=>true;/// <summary>
/// Creates a repeat expression with the specifed target expression, and minimum and maximum occurances
/// </summary>
/// <param name="expression">The target expression</param>
/// <param name="minOccurs">The minimum number of times the target expression can occur or -1</param>
/// <param name="maxOccurs">The maximum number of times the target expression can occur or -1</param>
public RegexRepeatExpression(RegexExpression expression,int minOccurs=-1,int maxOccurs=-1){Expression=expression;MinOccurs=minOccurs;MaxOccurs=maxOccurs;
}/// <summary>
/// Creates a default instance of the expression
/// </summary>
public RegexRepeatExpression(){}/// <summary>
/// Indicates the minimum number of times the target expression can occur, or 0 or -1 for no minimum
/// </summary>
public int MinOccurs{get;set;}=-1;/// <summary>
/// Indicates the maximum number of times the target expression can occur, or 0 or -1 for no maximum
/// </summary>
public int MaxOccurs{get;set;}=-1;/// <summary>
/// Creates a state machine representing this expression
/// </summary>
/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
/// <param name="accept">The accept symbol to use for this expression</param>
/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>		
public override CharFA<TAccept>ToFA<TAccept>(TAccept accept)=>null!=Expression?CharFA<TAccept>.Repeat(Expression.ToFA(accept),MinOccurs,MaxOccurs,accept)
:null;/// <summary>
/// Appends the textual representation to a <see cref="StringBuilder"/>
/// </summary>
/// <param name="sb">The string builder to use</param>
/// <remarks>Used by ToString()</remarks>
protected internal override void AppendTo(StringBuilder sb){var ise=null!=Expression&&Expression.IsSingleElement;if(!ise)sb.Append('(');if(null!=Expression)
Expression.AppendTo(sb);if(!ise)sb.Append(')');switch(MinOccurs){case-1:case 0:switch(MaxOccurs){case-1:case 0:sb.Append('*');break;default:sb.Append('{');
if(-1!=MinOccurs)sb.Append(MinOccurs);sb.Append(',');sb.Append(MaxOccurs);sb.Append('}');break;}break;case 1:switch(MaxOccurs){case-1:case 0:sb.Append('+');
break;default:sb.Append("{1,");sb.Append(MaxOccurs);sb.Append('}');break;}break;default:sb.Append('{');if(-1!=MinOccurs)sb.Append(MinOccurs);sb.Append(',');
if(-1!=MaxOccurs)sb.Append(MaxOccurs);sb.Append('}');break;}}/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
protected override RegexExpression CloneImpl()=>Clone();/// <summary>
/// Creates a new copy of this expression
/// </summary>
/// <returns>A new copy of this expression</returns>
public RegexRepeatExpression Clone(){return new RegexRepeatExpression(Expression,MinOccurs,MaxOccurs);}
#region Value semantics
/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public bool Equals(RegexRepeatExpression rhs){if(ReferenceEquals(rhs,this))return true;if(ReferenceEquals(rhs,null))return false;if(Equals(Expression,
rhs.Expression)){var lmio=Math.Max(0,MinOccurs);var lmao=Math.Max(0,MaxOccurs);var rmio=Math.Max(0,rhs.MinOccurs);var rmao=Math.Max(0,rhs.MaxOccurs);return
 lmio==rmio&&lmao==rmao;}return false;}/// <summary>
/// Indicates whether this expression is the same as the right hand expression
/// </summary>
/// <param name="rhs">The expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public override bool Equals(object rhs)=>Equals(rhs as RegexRepeatExpression);/// <summary>
/// Computes a hash code for this expression
/// </summary>
/// <returns>A hash code for this expression</returns>
public override int GetHashCode(){var result=MinOccurs^MaxOccurs;if(null!=Expression)return result^Expression.GetHashCode();return result;}/// <summary>
/// Indicates whether or not two expression are the same
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are the same, otherwise false</returns>
public static bool operator==(RegexRepeatExpression lhs,RegexRepeatExpression rhs){if(ReferenceEquals(lhs,rhs))return true;if(ReferenceEquals(lhs,null))
return false;return lhs.Equals(rhs);}/// <summary>
/// Indicates whether or not two expression are different
/// </summary>
/// <param name="lhs">The left hand expression to compare</param>
/// <param name="rhs">The right hand expression to compare</param>
/// <returns>True if the expressions are different, otherwise false</returns>
public static bool operator!=(RegexRepeatExpression lhs,RegexRepeatExpression rhs){if(ReferenceEquals(lhs,rhs))return false;if(ReferenceEquals(lhs,null))
return true;return!lhs.Equals(rhs);}
#endregion
}}namespace RE{/// <summary>
/// Represents an expression with a single target expression
/// </summary>
#if REGEXLIB
public
#endif
abstract class RegexUnaryExpression:RegexExpression{/// <summary>
/// Indicates the target expression
/// </summary>
public RegexExpression Expression{get;set;}}}namespace RE{/// <summary>
/// Represents an ascending range of characters.
/// </summary>
#if REGEXLIB
public
#endif
struct CharRange:IComparable<CharRange>,IEquatable<CharRange>,IList<char>{/// <summary>
/// Initializes the character range with the specified first and last characters
/// </summary>
/// <param name="first">The first character</param>
/// <param name="last">The last character</param>
public CharRange(char first,char last){First=(first<=last)?first:last;Last=(first<=last)?last:first;}/// <summary>
/// Gets a character at the specified index
/// </summary>
/// <param name="index">The index within the range</param>
/// <returns>The character at the specified index</returns>
char IList<char>.this[int index]{get=>this[index];set{_ThrowReadOnly();}}/// <summary>
/// Gets a character at the specified index
/// </summary>
/// <param name="index">The index within the range</param>
/// <returns>The character at the specified index</returns>
public char this[int index]{get{if(0>index||Length<=index)throw new IndexOutOfRangeException();return(char)(First+index);}}/// <summary>
/// Gets the length of the range
/// </summary>
public int Length{get{return Last-First+1;}}/// <summary>
/// Gets the first character in the range
/// </summary>
public char First{get;}/// <summary>
/// Gets the last character in the range
/// </summary>
public char Last{get;}/// <summary>
/// Gets ranges for a series of characters.
/// </summary>
/// <param name="sortedString">The sorted characters</param>
/// <returns>A series of ranges representing the specified characters</returns>
public static IEnumerable<CharRange>GetRanges(IEnumerable<char>sortedString){char first='\0';char last='\0';using(IEnumerator<char>e=sortedString.GetEnumerator())
{bool moved=e.MoveNext();while(moved){first=last=e.Current;while((moved=e.MoveNext())&&(e.Current==last||e.Current==last+1)){last=e.Current;}yield return
 new CharRange(first,last);}}}/// <summary>
/// Returns an array of character pairs representing the ranges
/// </summary>
/// <param name="ranges">The ranges to pack</param>
/// <returns>A packed array of ranges</returns>
public static char[]ToPackedChars(IEnumerable<CharRange>ranges){var rl=new List<CharRange>(ranges);NormalizeRangeList(rl);var result=new char[rl.Count
*2];int j=0;for(var i=0;i<result.Length;i++){result[i]=rl[j].First;++i;result[i]=rl[j].Last;++j;}return result;}/// <summary>
/// Returns an array of int pairs representing the ranges
/// </summary>
/// <param name="ranges">The ranges to pack</param>
/// <returns>A packed array of ranges</returns>
public static int[]ToPackedInts(IEnumerable<CharRange>ranges){var rl=new List<CharRange>(ranges);NormalizeRangeList(rl);var result=new int[rl.Count*2];
int j=0;for(var i=0;i<result.Length;i++){result[i]=rl[j].First;++i;result[i]=rl[j].Last;++j;}return result;}/// <summary>
/// Returns a packed string of character pairs representing the ranges
/// </summary>
/// <param name="ranges">The ranges to pack</param>
/// <returns>A string containing the packed ranges</returns>
public static string ToPackedString(IEnumerable<CharRange>ranges){var rl=new List<CharRange>(ranges);NormalizeRangeList(rl);int j=0;var result=new StringBuilder();
for(int ic=rl.Count*2,i=0;i<ic;++i){result.Append(rl[j].First);++i;result.Append(rl[j].Last);++j;}return result.ToString();}/// <summary>
/// Expands the ranges into a collection of characters
/// </summary>
/// <param name="ranges">The ranges to expand</param>
/// <returns>A collection of characters representing the ranges</returns>
public static IEnumerable<char>ExpandRanges(IEnumerable<CharRange>ranges){var seen=new HashSet<char>();foreach(var range in ranges)foreach(char ch in range)
if(seen.Add(ch))yield return ch;}/// <summary>
/// Negates the character ranges
/// </summary>
/// <param name="ranges">The ranges to negate</param>
/// <returns>The inverse set of ranges. Every character not in <paramref name="ranges"/> becomes part of a range.</returns>
public static IEnumerable<CharRange>NotRanges(IEnumerable<CharRange>ranges){ var last=char.MaxValue;using(var e=ranges.GetEnumerator()){if(!e.MoveNext())
{yield return new CharRange(char.MinValue,char.MaxValue);yield break;}if(e.Current.First>char.MinValue){yield return new CharRange(char.MinValue,unchecked((char)(e.Current.First
-1)));last=e.Current.Last;if(char.MaxValue==last)yield break;}while(e.MoveNext()){if(char.MaxValue==last)yield break;if(unchecked((char)(last+1))<e.Current.First)
yield return new CharRange(unchecked((char)(last+1)),unchecked((char)(e.Current.First-1)));last=e.Current.Last;}if(char.MaxValue>last)yield return new
 CharRange(unchecked((char)(last+1)),char.MaxValue);}}/// <summary>
/// Takes a list of ranges and ensures each range is in sorted order and contiguous ranges are combined
/// </summary>
/// <param name="ranges">The ranges to normalize</param>
/// <remarks>The list is modified</remarks>
public static void NormalizeRangeList(IList<CharRange>ranges){_Sort(ranges,0,ranges.Count-1);var or=default(CharRange);for(int i=1;i<ranges.Count;++i)
{if(ranges[i-1].Last>=ranges[i].First){var nr=new CharRange(ranges[i-1].First,ranges[i].Last);ranges[i-1]=or=nr;ranges.RemoveAt(i);--i;}}}/// <summary>
/// Returns the count of characters in the range
/// </summary>
int ICollection<char>.Count=>Length;/// <summary>
/// Indicates that the range is read only
/// </summary>
bool ICollection<char>.IsReadOnly=>true;/// <summary>
/// Indicates whether this range equals another range
/// </summary>
/// <param name="rhs">The range to compare</param>
/// <returns>True if the ranges are equal, otherwise false</returns>
public bool Equals(CharRange rhs)=>First==rhs.First&&Last==rhs.Last;/// <summary>
/// Indicates whether this range equals another range
/// </summary>
/// <param name="obj">The range to compare</param>
/// <returns>True if the ranges are equal, otherwise false</returns>
public override bool Equals(object obj)=>obj is CharRange&&Equals((CharRange)obj);/// <summary>
/// Gets the hash code for the range
/// </summary>
/// <returns>The hash code</returns>
public override int GetHashCode()=>First^Last;/// <summary>
/// Returns a string representation of a range
/// </summary>
/// <returns>A string representing the range</returns>
public override string ToString(){if(First==Last)return _Escape(First);if(2==Length)return string.Concat(_Escape(First),_Escape(Last));if(3==Length)return
 string.Concat(_Escape(First),_Escape((char)(First+1)),_Escape(Last));return string.Concat(_Escape(First),"-",_Escape(Last));} void _ThrowReadOnly(){throw
 new NotSupportedException("The collection is read-only.");} void ICollection<char>.Add(char item){_ThrowReadOnly();} void ICollection<char>.Clear(){_ThrowReadOnly();
} bool ICollection<char>.Contains(char item)=>item>=First&&item<=Last; void ICollection<char>.CopyTo(char[]array,int arrayIndex){char ch=First;for(int
 ic=Length,i=arrayIndex;i<ic;++i){array[i]=ch;++ch;}} IEnumerator<char>IEnumerable<char>.GetEnumerator(){if(First!=Last){for(char ch=First;ch<Last;++ch)
yield return ch;yield return Last;}else yield return First;} System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()=>((IEnumerable<char>)this).GetEnumerator();
 int IList<char>.IndexOf(char item){if(First<=item&&Last>=item)return item-First;return-1;} void IList<char>.Insert(int index,char item){_ThrowReadOnly();
} bool ICollection<char>.Remove(char item){_ThrowReadOnly();return false;} void IList<char>.RemoveAt(int index){_ThrowReadOnly();}
#region _Escape
 string _Escape(char ch){switch(ch){case'\n':return@"\n";case'\r':return@"\r";case'\t':return@"\t";case'\f':return@"\f";case'\b':return@"\b";case'-':return
@"\-";case'[':return@"\[";case']':return@"\]";case'(':return@"\(";case')':return@"\)";case'?':return@"\?";case'+':return@"\+";case'*':return@"\*";case
'.':return@"\.";case'^':return@"\^";case' ':return" ";default:if(char.IsControl(ch)||char.IsWhiteSpace(ch))return@"\u"+((int)ch).ToString("x4");break;
}return ch.ToString();}
#endregion
/// <summary>
/// Indicates whether or not two character ranges are the same
/// </summary>
/// <param name="lhs">The left hand range to compare</param>
/// <param name="rhs">The right hand range to compare</param>
/// <returns>True if the ranges are the same, otherwise false</returns>
public static bool operator==(CharRange lhs,CharRange rhs)=>lhs.Equals(rhs);/// <summary>
/// Indicates whether or not two character ranges are different
/// </summary>
/// <param name="lhs">The left hand range to compare</param>
/// <param name="rhs">The right hand range to compare</param>
/// <returns>True if the ranges are different, otherwise false</returns>
public static bool operator!=(CharRange lhs,CharRange rhs)=>!lhs.Equals(rhs);/// <summary>
/// Indicates whether one character range is less than the other
/// </summary>
/// <param name="lhs">The left hand range</param>
/// <param name="rhs">The right hand range</param>
/// <returns>True if <paramref name="lhs"/> is less than <paramref name="rhs"/>, otherwise false</returns>
public static bool operator<(CharRange lhs,CharRange rhs){if(lhs.First==rhs.First)return lhs.Last<rhs.Last;return lhs.First<rhs.First;}/// <summary>
/// Indicates whether one character range is greater than the other
/// </summary>
/// <param name="lhs">The left hand range</param>
/// <param name="rhs">The right hand range</param>
/// <returns>True if <paramref name="lhs"/> is greater than <paramref name="rhs"/>, otherwise false</returns>
public static bool operator>(CharRange lhs,CharRange rhs){if(lhs.First==rhs.First)return lhs.Last>rhs.Last;return lhs.First>rhs.First;}/// <summary>
/// Indicates whether one character range is less than or equal to the other
/// </summary>
/// <param name="lhs">The left hand range</param>
/// <param name="rhs">The right hand range</param>
/// <returns>True if <paramref name="lhs"/> is less than or equal to <paramref name="rhs"/>, otherwise false</returns>
public static bool operator<=(CharRange lhs,CharRange rhs){if(lhs.First==rhs.First)return lhs.Last<=rhs.Last;return lhs.First<=rhs.First;}/// <summary>
/// Indicates whether one character range is greater than or equal to the other
/// </summary>
/// <param name="lhs">The left hand range</param>
/// <param name="rhs">The right hand range</param>
/// <returns>True if <paramref name="lhs"/> is greater than or equal to <paramref name="rhs"/>, otherwise false</returns>
public static bool operator>=(CharRange lhs,CharRange rhs){if(lhs.First==rhs.First)return lhs.Last>=rhs.Last;return lhs.First>=rhs.First;}static void _Sort(IList<CharRange>
arr,int left,int right){if(left<right){int pivot=_Partition(arr,left,right);if(1<pivot){_Sort(arr,left,pivot-1);}if(pivot+1<right){_Sort(arr,pivot+1,right);
}}}static int _Partition(IList<CharRange>arr,int left,int right){CharRange pivot=arr[left];while(true){while(arr[left]<pivot){left++;}while(arr[right]
>pivot){right--;}if(left<right){if(arr[left]==arr[right])return right;CharRange swap=arr[left];arr[left]=arr[right];arr[right]=swap;}else{return right;
}}}public int CompareTo(CharRange other){var c=First.CompareTo(other.First);if(0==c)c=Last.CompareTo(other.Last);return c;}}}namespace RE{/// <summary>
/// This is an internal class that helps the code serializer know how to serialize DFA entries
/// </summary>
class CharDfaEntryConverter:TypeConverter{ public override bool CanConvertTo(ITypeDescriptorContext context,Type destinationType){if(typeof(InstanceDescriptor)
==destinationType)return true;return base.CanConvertTo(context,destinationType);} public override object ConvertTo(ITypeDescriptorContext context,CultureInfo
 culture,object value,Type destinationType){if(typeof(InstanceDescriptor)==destinationType){ var dte=(CharDfaEntry)value;return new InstanceDescriptor(typeof(CharDfaEntry).GetConstructor(new
 Type[]{typeof(int),typeof(CharDfaTransitionEntry[])}),new object[]{dte.AcceptSymbolId,dte.Transitions});}return base.ConvertTo(context,culture,value,
destinationType);}}/// <summary>
/// Represents an entry in a DFA state table
/// </summary>
[TypeConverter(typeof(CharDfaEntryConverter))]
#if REGEXLIB
public
#endif
struct CharDfaEntry{/// <summary>
/// Constructs a new instance of the DFA state table with the specified parameters
/// </summary>
/// <param name="acceptSymbolId">The symbolId to accept or -1 for non-accepting</param>
/// <param name="transitions">The transition entries</param>
public CharDfaEntry(int acceptSymbolId,CharDfaTransitionEntry[]transitions){AcceptSymbolId=acceptSymbolId;Transitions=transitions;}/// <summary>
/// Indicates the accept symbol's id or -1 for non-accepting
/// </summary>
public int AcceptSymbolId;/// <summary>
/// Indicates the transition entries
/// </summary>
public CharDfaTransitionEntry[]Transitions;}/// <summary>
/// This is an internal class that helps the code serializer serialize a DfaTransitionEntry
/// </summary>
class CharDfaTransitionEntryConverter:TypeConverter{ public override bool CanConvertTo(ITypeDescriptorContext context,Type destinationType){if(typeof(InstanceDescriptor)
==destinationType)return true;return base.CanConvertTo(context,destinationType);} public override object ConvertTo(ITypeDescriptorContext context,CultureInfo
 culture,object value,Type destinationType){if(typeof(InstanceDescriptor)==destinationType){var dte=(CharDfaTransitionEntry)value;return new InstanceDescriptor(typeof(CharDfaTransitionEntry).GetConstructor(new
 Type[]{typeof(char[]),typeof(int)}),new object[]{dte.PackedRanges,dte.Destination});}return base.ConvertTo(context,culture,value,destinationType);}}/// <summary>
/// Indicates a transition entry in the DFA state table
/// </summary>
[TypeConverter(typeof(CharDfaTransitionEntryConverter))]public struct CharDfaTransitionEntry{/// <summary>
/// Constructs a DFA transition entry with the specified parameters
/// </summary>
/// <param name="transitions">Packed character range pairs as a flat array</param>
/// <param name="destination">The destination state id</param>
public CharDfaTransitionEntry(char[]transitions,int destination){PackedRanges=transitions;Destination=destination;}/// <summary>
/// Indicates the packed range characters. Each range is specified by two array entries, first and last in that order.
/// </summary>
public char[]PackedRanges;/// <summary>
/// Indicates the destination state id
/// </summary>
public int Destination;}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Retrieves all the states reachable from this state that are accepting.
/// </summary>
/// <param name="result">The list of accepting states. Will be filled after the call.</param>
/// <returns>The resulting list of accepting states. This is the same value as the result parameter, if specified.</returns>
public IList<CharFA<TAccept>>FillAcceptingStates(IList<CharFA<TAccept>>result=null)=>FillAcceptingStates(FillClosure(),result);/// <summary>
/// Retrieves all the accept symbols from this state machine.
/// </summary>
/// <param name="result">The list of accept symbols. Will be filled after the call.</param>
/// <returns>The resulting list of accept symbols. This is the same value as the result parameter, if specified.</returns>
public IList<TAccept>FillAcceptSymbols(IList<TAccept>result=null)=>FillAcceptSymbols(FillClosure(),result);/// <summary>
/// Retrieves all the states in this closure that are accepting
/// </summary>
/// <param name="closure">The closure to examine</param>
/// <param name="result">The list of accepting states. Will be filled after the call.</param>
/// <returns>The resulting list of accepting states. This is the same value as the result parameter, if specified.</returns>
public static IList<CharFA<TAccept>>FillAcceptingStates(IList<CharFA<TAccept>>closure,IList<CharFA<TAccept>>result=null){if(null==result)result=new List<CharFA<TAccept>>();
for(int ic=closure.Count,i=0;i<ic;++i){var fa=closure[i];if(fa.IsAccepting)if(!result.Contains(fa))result.Add(fa);}return result;}/// <summary>
/// Retrieves all the accept symbols states in this closure
/// </summary>
/// <param name="closure">The closure to examine</param>
/// <param name="result">The list of accept symbols. Will be filled after the call.</param>
/// <returns>The resulting list of accept symbols. This is the same value as the result parameter, if specified.</returns>
public static IList<TAccept>FillAcceptSymbols(IList<CharFA<TAccept>>closure,IList<TAccept>result=null){if(null==result)result=new List<TAccept>();for(int
 ic=closure.Count,i=0;i<ic;++i){var fa=closure[i];if(fa.IsAccepting)if(!result.Contains(fa.AcceptSymbol))result.Add(fa.AcceptSymbol);}return result;}/// <summary>
/// Returns the first state that accepts from a given FA, or null if none do.
/// </summary>
public CharFA<TAccept>FirstAcceptingState{get{foreach(var fa in Closure)if(fa.IsAccepting)return fa;return null;}}/// <summary>
/// Returns the first accept symbol from a given FA, or the default value if none.
/// </summary>
public TAccept FirstAcceptSymbol{get{var fas=FirstAcceptingState;if(null!=fas)return fas.AcceptSymbol;return default(TAccept);}}/// <summary>
/// Indicates whether any of the states in the specified collection are accepting
/// </summary>
/// <param name="states">The state collection to examine</param>
/// <returns>True if one or more of the states is accepting, otherwise false.</returns>
public static bool IsAnyAccepting(IEnumerable<CharFA<TAccept>>states){foreach(var fa in states)if(fa.IsAccepting)return true;return false;}/// <summary>
/// Retrieves the first accept symbol from the collection of states
/// </summary>
/// <param name="states">The states to examine</param>
/// <param name="result">The accept symbol, if the method returned true</param>
/// <returns>True if an accept symbol was found, otherwise false</returns>
public static bool TryGetAnyAcceptSymbol(IEnumerable<CharFA<TAccept>>states,out TAccept result){foreach(var fa in states){if(fa.IsAccepting){result=fa.AcceptSymbol;
return true;}}result=default(TAccept);return false;}}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Indicates whether the state machine is a loop or not
/// </summary>
public bool IsLoop{get{foreach(var fa in Descendants)if(fa==this)return true;return false;}}}}namespace RE{ partial class CharFA<TAccept>{static IDictionary<string,
IList<CharRange>>_charClasses=_GetCharacterClasses();/// <summary>
/// Retrieves a dictionary indicating the character classes supported by this library
/// </summary>
public static IDictionary<string,IList<CharRange>>CharacterClasses=>_charClasses; static IDictionary<string,IList<CharRange>>_GetCharacterClasses(){var
 result=new Dictionary<string,IList<CharRange>>();result.Add("alnum",new List<CharRange>(new CharRange[]{new CharRange('A','Z'),new CharRange('a','z'),
new CharRange('0','9')}));result.Add("alpha",new List<CharRange>(new CharRange[]{new CharRange('A','Z'),new CharRange('a','z')}));result.Add("ascii",new
 List<CharRange>(new CharRange[]{new CharRange('\0','\x7F')}));result.Add("blank",new List<CharRange>(new CharRange[]{new CharRange(' ',' '),new CharRange('\t','\t')
}));result.Add("cntrl",new List<CharRange>(new CharRange[]{new CharRange('\0','\x1F'),new CharRange('\x7F','\x7F')}));result.Add("digit",new List<CharRange>(
new CharRange[]{new CharRange('0','9')}));result.Add("^digit",new List<CharRange>(CharRange.NotRanges(result["digit"])));result.Add("graph",new List<CharRange>(
new CharRange[]{new CharRange('\x21','\x7E')}));result.Add("lower",new List<CharRange>(new CharRange[]{new CharRange('a','z')}));result.Add("print",new
 List<CharRange>(new CharRange[]{new CharRange('\x20','\x7E')})); result.Add("punct",new List<CharRange>(CharRange.GetRanges("!\"#$%&\'()*+,-./:;<=>?@[\\]^_`{|}~")
)); result.Add("space",new List<CharRange>(CharRange.GetRanges(" \t\r\n\v\f")));result.Add("^space",new List<CharRange>(CharRange.NotRanges(result["space"])));
result.Add("upper",new List<CharRange>(new CharRange[]{new CharRange('A','Z')}));result.Add("word",new List<CharRange>(new CharRange[]{new CharRange('0',
'9'),new CharRange('A','Z'),new CharRange('_','_'),new CharRange('a','z')}));result.Add("^word",new List<CharRange>(CharRange.NotRanges(result["word"])));
result.Add("xdigit",new List<CharRange>(new CharRange[]{new CharRange('0','9'),new CharRange('A','F'),new CharRange('a','f')}));return result;}}}namespace
 RE{partial class CharFA<TAccept>:ICloneable{/// <summary>
/// Deep copies the finite state machine to a new state machine
/// </summary>
/// <returns>The new machine</returns>
public CharFA<TAccept>Clone(){var closure=FillClosure();var nclosure=new CharFA<TAccept>[closure.Count];for(var i=0;i<nclosure.Length;i++){nclosure[i]
=new CharFA<TAccept>(closure[i].IsAccepting,closure[i].AcceptSymbol);nclosure[i].Tag=closure[i].Tag;}for(var i=0;i<nclosure.Length;i++){var t=nclosure[i].InputTransitions;
var e=nclosure[i].EpsilonTransitions;foreach(var trns in closure[i].InputTransitions){var id=closure.IndexOf(trns.Value);t.Add(trns.Key,nclosure[id]);
}foreach(var trns in closure[i].EpsilonTransitions){var id=closure.IndexOf(trns);e.Add(nclosure[id]);}}return nclosure[0];}object ICloneable.Clone()=>
Clone();/// <summary>
/// Returns a duplicate state machine, except one that only goes from this state to the state specified in <paramref name="to"/>. Any state that does not lead to that state is eliminated from the resulting graph.
/// </summary>
/// <param name="to">The state to track the path to</param>
/// <returns>A new state machine that only goes from this state to the state indicated by <paramref name="to"/></returns>
public CharFA<TAccept>ClonePathTo(CharFA<TAccept>to){var closure=FillClosure();var nclosure=new CharFA<TAccept>[closure.Count];for(var i=0;i<nclosure.Length;
i++){nclosure[i]=new CharFA<TAccept>(closure[i].IsAccepting,closure[i].AcceptSymbol);nclosure[i].Tag=closure[i].Tag;}for(var i=0;i<nclosure.Length;i++)
{var t=nclosure[i].InputTransitions;var e=nclosure[i].EpsilonTransitions;foreach(var trns in closure[i].InputTransitions){if(trns.Value.FillClosure().Contains(to))
{var id=closure.IndexOf(trns.Value);t.Add(trns.Key,nclosure[id]);}}foreach(var trns in closure[i].EpsilonTransitions){if(trns.FillClosure().Contains(to))
{var id=closure.IndexOf(trns);e.Add(nclosure[id]);}}}return nclosure[0];}/// <summary>
/// Returns a duplicate state machine, except one that only goes from this state to any state specified in <paramref name="to"/>. Any state that does not lead to one of those states is eliminated from the resulting graph.
/// </summary>
/// <param name="to">The collection of destination states</param>
/// <returns>A new state machine that only goes from this state to the states indicated by <paramref name="to"/></returns>
public CharFA<TAccept>ClonePathToAny(IEnumerable<CharFA<TAccept>>to){var closure=FillClosure();var nclosure=new CharFA<TAccept>[closure.Count];for(var
 i=0;i<nclosure.Length;i++){nclosure[i]=new CharFA<TAccept>(closure[i].IsAccepting,closure[i].AcceptSymbol);nclosure[i].Tag=closure[i].Tag;}for(var i=
0;i<nclosure.Length;i++){var t=nclosure[i].InputTransitions;var e=nclosure[i].EpsilonTransitions;foreach(var trns in closure[i].InputTransitions){if(_ContainsAny(trns.Value.FillClosure(),
to)){var id=closure.IndexOf(trns.Value);t.Add(trns.Key,nclosure[id]);}}foreach(var trns in closure[i].EpsilonTransitions){if(_ContainsAny(trns.FillClosure(),
to)){var id=closure.IndexOf(trns);e.Add(nclosure[id]);}}}return nclosure[0];}static bool _ContainsAny(ICollection<CharFA<TAccept>>col,IEnumerable<CharFA<TAccept>>
any){foreach(var fa in any)if(col.Contains(fa))return true;return false;}}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Generates a <see cref="CodeExpression"/> that can be used to initialize a symbol table
/// </summary>
/// <param name="symbols">The symbols to generate the symbol table code for</param>
/// <returns>The expression used to initialize the symbol table array of element type indicated by TAccept</returns>
public static CodeExpression GenerateSymbolTableInitializer(params TAccept[]symbols)=>_Serialize(symbols);/// <summary>
/// Generates a <see cref="CodeExpression"/> that can be used to initialize a DFA state table
/// </summary>
/// <param name="dfaTable">The DFA state table to generate the code for</param>
/// <returns>The expression used to initialize the DFA state table array of element type <see cref="CharDfaEntry"/></returns>
public static CodeExpression GenerateDfaStateTableInitializer(CharDfaEntry[]dfaTable)=>_Serialize(dfaTable);/// <summary>
/// Generates a <see cref="CodeMemberMethod"/> that can be compiled and used to lex input
/// </summary>
/// <param name="dfaTable">The DFA table to use</param>
/// <param name="errorSymbol">Indicates the error symbol id to use</param>
/// <returns>A <see cref="CodeMemberMethod"/> representing the lexing procedure</returns>
public static CodeMemberMethod GenerateLexMethod(CharDfaEntry[]dfaTable,int errorSymbol){var result=new CodeMemberMethod();result.Name="Lex";result.Attributes
=MemberAttributes.FamilyAndAssembly|MemberAttributes.Static;result.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ParseContext),"context"));
result.ReturnType=new CodeTypeReference(typeof(int));result.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression("context"),
"EnsureStarted"))); var isRootLoop=false; var hasError=false;for(var i=0;i<dfaTable.Length;i++){var trns=dfaTable[i].Transitions;for(var j=0;j<trns.Length;
j++){if(0==trns[j].Destination){isRootLoop=true;break;}}}var pcr=new CodeArgumentReferenceExpression(result.Parameters[0].Name);var pccr=new CodePropertyReferenceExpression(pcr,
"Current");var pccc=new CodeExpressionStatement(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(pcr,"CaptureCurrent")));var exprs=new
 CodeExpressionCollection();var stmts=new CodeStatementCollection();for(var i=0;i<dfaTable.Length;i++){stmts.Clear();var se=dfaTable[i];var trns=se.Transitions;
for(var j=0;j<trns.Length;j++){var cif=new CodeConditionStatement();stmts.Add(cif);exprs.Clear();var trn=trns[j];var pr=trn.PackedRanges;for(var k=0;k
<pr.Length;k++){var first=pr[k];++k; var last=pr[k];if(first!=last){exprs.Add(new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(pccr,CodeBinaryOperatorType.GreaterThanOrEqual,
new CodePrimitiveExpression(first)),CodeBinaryOperatorType.BooleanAnd,new CodeBinaryOperatorExpression(pccr,CodeBinaryOperatorType.LessThanOrEqual,new
 CodePrimitiveExpression(last))));}else{exprs.Add(new CodeBinaryOperatorExpression(pccr,CodeBinaryOperatorType.ValueEquality,new CodePrimitiveExpression(first)
));}}cif.Condition=_MakeBinOps(exprs,CodeBinaryOperatorType.BooleanOr);cif.TrueStatements.Add(pccc);cif.TrueStatements.Add(new CodeExpressionStatement(new
 CodeMethodInvokeExpression(pcr,"Advance")));cif.TrueStatements.Add(new CodeGotoStatement(string.Concat("q",trn.Destination.ToString())));}if(-1!=se.AcceptSymbolId)
 stmts.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(se.AcceptSymbolId)));else{hasError=true;stmts.Add(new CodeGotoStatement("error"));
}if(0<i||isRootLoop){result.Statements.Add(new CodeLabeledStatement(string.Concat("q",i.ToString()),stmts[0]));for(int jc=stmts.Count,j=1;j<jc;++j)result.Statements.Add(stmts[j]);
}else{result.Statements.Add(new CodeCommentStatement("q0"));result.Statements.AddRange(stmts);}}if(hasError){result.Statements.Add(new CodeLabeledStatement("error",
pccc));result.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(pcr,"Advance")));result.Statements.Add(new CodeMethodReturnStatement(new
 CodePrimitiveExpression(errorSymbol)));}return result;}/// <summary>
/// Generates a <see cref="CodeMemberMethod"/> that can be compiled and used to match input
/// </summary>
/// <param name="dfaTable">The DFA table to use</param>
/// <returns>A <see cref="CodeMemberMethod"/> representing the matching procedure</returns>
public static CodeMemberMethod GenerateMatchMethod(CharDfaEntry[]dfaTable){var result=new CodeMemberMethod();result.Name="Match";result.Attributes=MemberAttributes.FamilyAndAssembly
|MemberAttributes.Static;result.Parameters.Add(new CodeParameterDeclarationExpression(typeof(ParseContext),"context"));result.ReturnType=new CodeTypeReference(typeof(CharFAMatch));
result.Statements.Add(new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeArgumentReferenceExpression("context"),"EnsureStarted")));
var pcr=new CodeArgumentReferenceExpression(result.Parameters[0].Name);var pccr=new CodePropertyReferenceExpression(pcr,"Current");var pccc=new CodeExpressionStatement(new
 CodeMethodInvokeExpression(new CodeMethodReferenceExpression(pcr,"CaptureCurrent")));var vsuc=new CodeVariableReferenceExpression("success");result.Statements.Add(new
 CodeVariableDeclarationStatement(typeof(int),"line",new CodePropertyReferenceExpression(pcr,"Line")));result.Statements.Add(new CodeVariableDeclarationStatement(typeof(int),
"column",new CodePropertyReferenceExpression(pcr,"Column")));result.Statements.Add(new CodeVariableDeclarationStatement(typeof(long),"position",new CodePropertyReferenceExpression(pcr,
"Position")));result.Statements.Add(new CodeVariableDeclarationStatement(typeof(int),"l",new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(pcr,
"CaptureBuffer"),"Length")));result.Statements.Add(new CodeVariableDeclarationStatement(typeof(bool),"success",new CodePrimitiveExpression(false)));var
 w=new CodeIterationStatement(new CodeCommentStatement(""),new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false),
CodeBinaryOperatorType.ValueEquality,vsuc),CodeBinaryOperatorType.BooleanAnd,new CodeBinaryOperatorExpression(new CodePrimitiveExpression(-1),CodeBinaryOperatorType.IdentityInequality,
pccr)),new CodeCommentStatement(""));result.Statements.Add(w); var isRootLoop=false; var hasError=false;for(var i=0;i<dfaTable.Length;i++){var trns=dfaTable[i].Transitions;
for(var j=0;j<trns.Length;j++){if(0==trns[j].Destination){isRootLoop=true;break;}}}var exprs=new CodeExpressionCollection();var stmts=new CodeStatementCollection();
for(var i=0;i<dfaTable.Length;i++){stmts.Clear();var se=dfaTable[i];var trns=se.Transitions;for(var j=0;j<trns.Length;j++){var cif=new CodeConditionStatement();
stmts.Add(cif);exprs.Clear();var trn=trns[j];var pr=trn.PackedRanges;for(var k=0;k<pr.Length;k++){var first=pr[k];++k; var last=pr[k];if(first!=last){
exprs.Add(new CodeBinaryOperatorExpression(new CodeBinaryOperatorExpression(pccr,CodeBinaryOperatorType.GreaterThanOrEqual,new CodePrimitiveExpression(first)
),CodeBinaryOperatorType.BooleanAnd,new CodeBinaryOperatorExpression(pccr,CodeBinaryOperatorType.LessThanOrEqual,new CodePrimitiveExpression(last))));
}else{exprs.Add(new CodeBinaryOperatorExpression(pccr,CodeBinaryOperatorType.ValueEquality,new CodePrimitiveExpression(first)));}}cif.Condition=_MakeBinOps(exprs,
CodeBinaryOperatorType.BooleanOr);cif.TrueStatements.Add(pccc);cif.TrueStatements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(pcr,"Advance")));
cif.TrueStatements.Add(new CodeGotoStatement(string.Concat("q",trn.Destination.ToString())));}if(-1!=se.AcceptSymbolId){stmts.Add(new CodeAssignStatement(vsuc,
new CodePrimitiveExpression(true)));stmts.Add(new CodeGotoStatement("done"));}else{hasError=true;stmts.Add(new CodeGotoStatement("error"));}if(0<i||isRootLoop)
{w.Statements.Add(new CodeLabeledStatement(string.Concat("q",i.ToString()),stmts[0]));for(int jc=stmts.Count,j=1;j<jc;++j)w.Statements.Add(stmts[j]);}
else{w.Statements.Add(new CodeCommentStatement("q0"));w.Statements.AddRange(stmts);}}if(hasError){w.Statements.Add(new CodeLabeledStatement("error",new
 CodeAssignStatement(vsuc,new CodePrimitiveExpression(false))));w.Statements.Add(new CodeExpressionStatement(new CodeMethodInvokeExpression(pcr,"Advance")));
}var ccif=new CodeConditionStatement(new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false),CodeBinaryOperatorType.ValueEquality,vsuc));ccif.TrueStatements.Add(new
 CodeAssignStatement(new CodeVariableReferenceExpression("line"),new CodePropertyReferenceExpression(pcr,"Line")));ccif.TrueStatements.Add(new CodeAssignStatement(new
 CodeVariableReferenceExpression("column"),new CodePropertyReferenceExpression(pcr,"Column")));ccif.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("position"),
new CodePropertyReferenceExpression(pcr,"Position")));ccif.TrueStatements.Add(new CodeAssignStatement(new CodeVariableReferenceExpression("l"),new CodePropertyReferenceExpression(new
 CodePropertyReferenceExpression(pcr,"CaptureBuffer"),"Length")));w.Statements.Add(new CodeLabeledStatement("done",ccif));var cccif=new CodeConditionStatement(vsuc);
cccif.TrueStatements.Add(new CodeMethodReturnStatement(new CodeObjectCreateExpression(typeof(CharFAMatch),new CodeVariableReferenceExpression("line"),
new CodeVariableReferenceExpression("column"),new CodeVariableReferenceExpression("position"),new CodeMethodInvokeExpression(pcr,"GetCapture",new CodeVariableReferenceExpression("l"))
)));result.Statements.Add(cccif);result.Statements.Add(new CodeMethodReturnStatement(new CodePrimitiveExpression(null)));return result;}static CodeExpression
 _MakeBinOps(IEnumerable exprs,CodeBinaryOperatorType type){var result=new CodeBinaryOperatorExpression();foreach(CodeExpression expr in exprs){result.Operator
=type;if(null==result.Left){result.Left=expr;continue;}if(null==result.Right){result.Right=expr;continue;}result=new CodeBinaryOperatorExpression(result,
type,expr);}if(null==result.Right)return result.Left;return result;}
#region Type serialization
static CodeExpression _SerializeArray(Array arr){if(1==arr.Rank&&0==arr.GetLowerBound(0)){var result=new CodeArrayCreateExpression(arr.GetType());foreach
(var elem in arr)result.Initializers.Add(_Serialize(elem));return result;}throw new NotSupportedException("Only SZArrays can be serialized to code.");
}static CodeExpression _Serialize(object val){if(null==val)return new CodePrimitiveExpression(null);if(val is char){ if(((char)val)>0x7E)return new CodeCastExpression(typeof(char),
new CodePrimitiveExpression((int)(char)val));return new CodePrimitiveExpression((char)val);}else if(val is bool||val is string||val is short||val is ushort
||val is int||val is uint||val is ulong||val is long||val is byte||val is sbyte||val is float||val is double||val is decimal){ return new CodePrimitiveExpression(val);
}if(val is Array&&1==((Array)val).Rank&&0==((Array)val).GetLowerBound(0)){return _SerializeArray((Array)val);}var conv=TypeDescriptor.GetConverter(val);
if(null!=conv){if(conv.CanConvertTo(typeof(InstanceDescriptor))){var desc=conv.ConvertTo(val,typeof(InstanceDescriptor))as InstanceDescriptor;if(!desc.IsComplete)
throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.",val.GetType().FullName));var ctor=desc.MemberInfo as ConstructorInfo;
if(null!=ctor){var result=new CodeObjectCreateExpression(ctor.DeclaringType);foreach(var arg in desc.Arguments)result.Parameters.Add(_Serialize(arg));
return result;}throw new NotSupportedException(string.Format("The instance descriptor for type \"{0}\" is not supported.",val.GetType().FullName));}else
{ var t=val.GetType();if(t.IsGenericType&&t.GetGenericTypeDefinition()==typeof(KeyValuePair<,>)){ var kvpType=new CodeTypeReference(typeof(KeyValuePair<,>));
foreach(var arg in val.GetType().GetGenericArguments())kvpType.TypeArguments.Add(arg);var result=new CodeObjectCreateExpression(kvpType);for(int ic=kvpType.TypeArguments.Count,
i=0;i<ic;++i){var prop=val.GetType().GetProperty(0==i?"Key":"Value");result.Parameters.Add(_Serialize(prop.GetValue(val)));}return result;}throw new NotSupportedException(
string.Format("The type \"{0}\" could not be serialized.",val.GetType().FullName));}}else throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.",
val.GetType().FullName));}
#endregion
}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Retrieves all states reachable from this state
/// </summary>
/// <param name="result">The collection to hold the result, or null to create one.</param>
/// <returns>A collection containing the closure of items</returns>
public IList<CharFA<TAccept>>FillClosure(IList<CharFA<TAccept>>result=null){if(null==result)result=new List<CharFA<TAccept>>();else if(result.Contains(this))
return result;result.Add(this); foreach(var trns in InputTransitions as IDictionary<CharFA<TAccept>,ICollection<char>>)trns.Key.FillClosure(result);foreach
(var fa in EpsilonTransitions)fa.FillClosure(result);return result;}/// <summary>
/// Retrieves an enumeration that indicates the closure of the state
/// </summary>
/// <remarks>This uses lazy evaluation.</remarks>
public IEnumerable<CharFA<TAccept>>Closure=>_EnumClosure(new HashSet<CharFA<TAccept>>()); IEnumerable<CharFA<TAccept>>_EnumClosure(HashSet<CharFA<TAccept>>
visited){if(visited.Add(this)){yield return this;foreach(var trns in InputTransitions as IDictionary<CharFA<TAccept>,ICollection<char>>)foreach(var fa
 in trns.Key._EnumClosure(visited))yield return fa;foreach(var fa in EpsilonTransitions)foreach(var ffa in fa._EnumClosure(visited))yield return ffa;}
}/// <summary>
/// Retrieves all states reachable from this state on no input.
/// </summary>
/// <param name="result">A collection to hold the result or null to create one</param>
/// <returns>A collection containing the epsilon closure of this state</returns>
public IList<CharFA<TAccept>>FillEpsilonClosure(IList<CharFA<TAccept>>result=null){if(null==result)result=new List<CharFA<TAccept>>();else if(result.Contains(this))
return result;result.Add(this);foreach(var fa in EpsilonTransitions)fa.FillEpsilonClosure(result);return result;}/// <summary>
/// Retrieves an enumeration that indicates the epsilon closure of this state
/// </summary>
/// <remarks>This uses lazy evaluation.</remarks>
public IEnumerable<CharFA<TAccept>>EpsilonClosure=>_EnumEpsilonClosure(new HashSet<CharFA<TAccept>>()); IEnumerable<CharFA<TAccept>>_EnumEpsilonClosure(HashSet<CharFA<TAccept>>
visited){if(visited.Add(this)){yield return this;foreach(var fa in EpsilonTransitions)foreach(var ffa in fa._EnumEpsilonClosure(visited))yield return ffa;
}}/// <summary>
/// Takes a set of states and computes the total epsilon closure as a set of states
/// </summary>
/// <param name="states">The states to examine</param>
/// <param name="result">The result to be filled</param>
/// <returns>The epsilon closure of <paramref name="states"/></returns>
public static IList<CharFA<TAccept>>FillEpsilonClosure(IEnumerable<CharFA<TAccept>>states,IList<CharFA<TAccept>>result=null){if(null==result)result=new
 List<CharFA<TAccept>>();foreach(var fa in states)fa.FillEpsilonClosure(result);return result;}/// <summary>
/// Retrieves all states that are descendants of this state
/// </summary>
/// <param name="result">A collection to hold the result or null to create one</param>
/// <returns>A collection containing the descendants of this state</returns>
public IList<CharFA<TAccept>>FillDescendants(IList<CharFA<TAccept>>result=null){if(null==result)result=new List<CharFA<TAccept>>();foreach(var trns in
 InputTransitions as IDictionary<CharFA<TAccept>,ICollection<char>>)trns.Key.FillClosure(result);foreach(var fa in EpsilonTransitions)fa.FillClosure(result);
return result;}/// <summary>
/// Retrieves an enumeration that indicates the descendants of the state
/// </summary>
/// <remarks>This uses lazy evaluation.</remarks>
public IEnumerable<CharFA<TAccept>>Descendants=>_EnumDescendants(new HashSet<CharFA<TAccept>>()); IEnumerable<CharFA<TAccept>>_EnumDescendants(HashSet<CharFA<TAccept>>
visited){foreach(var trns in InputTransitions as IDictionary<CharFA<TAccept>,ICollection<char>>)foreach(var fa in trns.Key._EnumClosure(visited))yield
 return fa;foreach(var fa in EpsilonTransitions)foreach(var ffa in fa._EnumClosure(visited))yield return ffa;}/// <summary>
/// Fills a collection with the result of moving each of the specified <paramref name="states"/> by the specified input.
/// </summary>
/// <param name="states">The states to examine</param>
/// <param name="input">The input to use</param>
/// <param name="result">The states that are now entered as a result of the move</param>
/// <returns><paramref name="result"/> or a new collection if it wasn't specified.</returns>
public static IList<CharFA<TAccept>>FillMove(IEnumerable<CharFA<TAccept>>states,char input,IList<CharFA<TAccept>>result=null){if(null==result)result=new
 List<CharFA<TAccept>>();var ec=FillEpsilonClosure(states);for(int ic=ec.Count,i=0;i<ic;++i){var fa=ec[i]; CharFA<TAccept>ofa; if(fa.InputTransitions.TryGetValue(input,
out ofa)){var ec2=ofa.FillEpsilonClosure();for(int jc=ec2.Count,j=0;j<jc;++j){var efa=ec2[j];if(!result.Contains(efa)) result.Add(efa);}}}return result;
}/// <summary>
/// Moves from the specified state to a destination state in a DFA by moving along the specified input.
/// </summary>
/// <param name="input">The input to move on</param>
/// <returns>The state which the machine moved to or null if no state could be found.</returns>
public CharFA<TAccept>MoveDfa(char input){CharFA<TAccept>fa;if(InputTransitions.TryGetValue(input,out fa))return fa;return null;}/// <summary>
/// Returns a dictionary keyed by state, that contains all of the outgoing local input transitions, expressed as a series of ranges
/// </summary>
/// <param name="result">The dictionary to fill, or null to create one.</param>
/// <returns>A dictionary containing the result of the query</returns>
public IDictionary<CharFA<TAccept>,IList<CharRange>>FillInputTransitionRangesGroupedByState(IDictionary<CharFA<TAccept>,IList<CharRange>>result=null){
if(null==result)result=new Dictionary<CharFA<TAccept>,IList<CharRange>>(); foreach(var trns in(IDictionary<CharFA<TAccept>,ICollection<char>>)InputTransitions)
{var sl=new List<char>(trns.Value);sl.Sort();result.Add(trns.Key,new List<CharRange>(CharRange.GetRanges(sl)));}return result;}}}namespace RE{/// <summary>
/// Represents a single state in a character based finite state machine.
/// </summary>
/// <typeparam name="TAccept">The type of the accepting symbols</typeparam>
#if REGEXLIB
public
#endif
partial class CharFA<TAccept>{/// <summary>
/// Indicates the input transitions. These are the states that will be transitioned to on the specified input key.
/// </summary>
public IDictionary<char,CharFA<TAccept>>InputTransitions{get;}=new _InputTransitionDictionary();/// <summary>
/// Indicates the epsilon transitions. These are the states that are transitioned to without consuming input.
/// </summary>
public IList<CharFA<TAccept>>EpsilonTransitions{get;}=new List<CharFA<TAccept>>();/// <summary>
/// Indicates whether or not this is an accepting state. When an accepting state is landed on, this indicates a potential match.
/// </summary>
public bool IsAccepting{get;set;}=false;/// <summary>
/// The symbol to associate with this accepting state. Upon accepting a match, the specified symbol is returned which can identify it.
/// </summary>
public TAccept AcceptSymbol{get;set;}=default(TAccept);/// <summary>
/// Indicates a user-defined value to associate with this state
/// </summary>
public object Tag{get;set;}=null;/// <summary>
/// Constructs a new instance with the specified accepting value and accept symbol.
/// </summary>
/// <param name="isAccepting">Indicates whether or not the state is accepting</param>
/// <param name="acceptSymbol">Indicates the associated symbol to be used when accepting.</param>
public CharFA(bool isAccepting,TAccept acceptSymbol=default(TAccept)){IsAccepting=isAccepting;AcceptSymbol=acceptSymbol;}/// <summary>
/// Constructs a new non-accepting state
/// </summary>
public CharFA(){}}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Moves from the specified state to a destination state in a DFA by moving along the specified input.
/// </summary>
/// <param name="dfaTable">The DFA state table to use</param>
/// <param name="state">The current state id</param>
/// <param name="input">The input to move on</param>
/// <returns>The state id which the machine moved to or -1 if no state could be found.</returns>
public static int MoveDfa(CharDfaEntry[]dfaTable,int state,char input){ for(var i=0;i<dfaTable[state].Transitions.Length;i++){var entry=dfaTable[state].Transitions[i];
var found=false; for(var j=0;j<entry.PackedRanges.Length;j++){var first=entry.PackedRanges[j];++j;var last=entry.PackedRanges[j];if(input>last)continue;
if(first>input)break;found=true;break;}if(found){ return entry.Destination;}}return-1;}/// <summary>
/// Returns a DFA table that can be used to lex or match
/// </summary>
/// <param name="symbolTable">The symbol table to use, or null to just implicitly tag symbols with integer ids</param>
/// <param name="progress">The progress object used to report the progress of the task</param>
/// <returns>A DFA table that can be used to efficiently match or lex input</returns>
public CharDfaEntry[]ToDfaStateTable(IList<TAccept>symbolTable=null,IProgress<CharFAProgress>progress=null){ var dfa=IsDfa?this:ToDfa(progress);var closure
=dfa.FillClosure();var symbolLookup=new ListDictionary<TAccept,int>(); if(null==symbolTable){ var i=0;for(int jc=closure.Count,j=0;j<jc;++j){var fa=closure[j];
if(fa.IsAccepting&&!symbolLookup.ContainsKey(fa.AcceptSymbol)){symbolLookup.Add(fa.AcceptSymbol,i);++i;}}}else for(int ic=symbolTable.Count,i=0;i<ic;++i)
if(null!=symbolTable[i])symbolLookup.Add(symbolTable[i],i); var result=new CharDfaEntry[closure.Count];for(var i=0;i<result.Length;i++){var fa=closure[i];
 var trgs=fa.FillInputTransitionRangesGroupedByState(); var trns=new CharDfaTransitionEntry[trgs.Count];var j=0; foreach(var trg in trgs){ trns[j]=new
 CharDfaTransitionEntry(CharRange.ToPackedChars(trg.Value),closure.IndexOf(trg.Key));++j;} result[i]=new CharDfaEntry(fa.IsAccepting?symbolLookup[fa.AcceptSymbol]
:-1,trns);}return result;}}}namespace RE{ partial class CharFA<TAccept>{/// <summary>
/// Indicates whether this state is a duplicate of another state.
/// </summary>
/// <param name="rhs">The state to compare with</param>
/// <returns>True if the states are duplicates (one can be removed without changing the language of the machine)</returns>
public bool IsDuplicate(CharFA<TAccept>rhs){return null!=rhs&&IsAccepting==rhs.IsAccepting&&_SetComparer.Default.Equals(EpsilonTransitions,rhs.EpsilonTransitions)
&&_SetComparer.Default.Equals((IDictionary<CharFA<TAccept>,ICollection<char>>)InputTransitions,(IDictionary<CharFA<TAccept>,ICollection<char>>)rhs.InputTransitions);
}/// <summary>
/// Fills a dictionary of duplicates by state for any duplicates found in the state graph
/// </summary>
/// <param name="result">The resulting dictionary to be filled.</param>
/// <returns>The resulting dictionary of duplicates</returns>
public IDictionary<CharFA<TAccept>,ICollection<CharFA<TAccept>>>FillDuplicatesGroupedByState(IDictionary<CharFA<TAccept>,ICollection<CharFA<TAccept>>>
result=null)=>FillDuplicatesGroupedByState(FillClosure());/// <summary>
/// Fills a dictionary of duplicates by state for any duplicates found in the state graph
/// </summary>
/// <param name="closure">The closure to examine</param>
/// <param name="result">The resulting dictionary to be filled.</param>
/// <returns>The resulting dictionary of duplicates</returns>
public static IDictionary<CharFA<TAccept>,ICollection<CharFA<TAccept>>>FillDuplicatesGroupedByState(IList<CharFA<TAccept>>closure,IDictionary<CharFA<TAccept>,
ICollection<CharFA<TAccept>>>result=null){if(null==result)result=new Dictionary<CharFA<TAccept>,ICollection<CharFA<TAccept>>>();var cl=closure;int c=cl.Count;
for(int i=0;i<c;i++){var s=cl[i];for(int j=i+1;j<c;j++){var cmp=cl[j];if(s.IsDuplicate(cmp)){ICollection<CharFA<TAccept>>col=new List<CharFA<TAccept>>();
if(!result.ContainsKey(s))result.Add(s,col);else col=result[s];if(!col.Contains(cmp))col.Add(cmp);}}}return result;}/// <summary>
/// Trims duplicate states from the graph.
/// </summary>
public void TrimDuplicates(IProgress<CharFAProgress>progress=null)=>TrimDuplicates(FillClosure(),progress);/// <summary>
/// Trims duplicate states from the graph
/// </summary>
/// <param name="closure">The closure to alter.</param>
/// <param name="progress">The progress object used to report the progress of the task</param>
public static void TrimDuplicates(IList<CharFA<TAccept>>closure,IProgress<CharFAProgress>progress=null){var lclosure=closure;var dups=new Dictionary<CharFA<TAccept>,
ICollection<CharFA<TAccept>>>();int oc=0;int c=-1;var k=0; while(c<oc){if(null!=progress)progress.Report(new CharFAProgress(CharFAStatus.TrimDuplicates,
k));c=lclosure.Count;FillDuplicatesGroupedByState(lclosure,dups);if(0<dups.Count){ foreach(KeyValuePair<CharFA<TAccept>,ICollection<CharFA<TAccept>>>de
 in dups){var replacement=de.Key;var targets=de.Value;for(int i=0;i<c;++i){var s=lclosure[i];var repls=new List<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>>();
var td=(IDictionary<CharFA<TAccept>,ICollection<char>>)s.InputTransitions;foreach(var trns in td)if(targets.Contains(trns.Key))repls.Add(new KeyValuePair<CharFA<TAccept>,
CharFA<TAccept>>(trns.Key,replacement));foreach(var repl in repls){var inps=td[repl.Key];td.Remove(repl.Key);ICollection<char>v;if(!td.TryGetValue(repl.Value,
out v))td.Add(repl.Value,inps);else foreach(var inp in inps)if(!v.Contains(inp))v.Add(inp);}int lc=s.EpsilonTransitions.Count;for(int j=0;j<lc;++j)if(targets.Contains(s.EpsilonTransitions[j]))
s.EpsilonTransitions[j]=de.Key;}}dups.Clear();}else break;oc=c;var f=lclosure[0];lclosure=f.FillClosure();c=lclosure.Count;++k;}}}}namespace RE{partial
 class CharFA<TAccept>{/// <summary>
/// Indicates whether or not the state has any outgoing transitions
/// </summary>
public bool IsFinal{get{return 0==InputTransitions.Count&&0==EpsilonTransitions.Count;}}/// <summary>
/// Retrieves all the states reachable from this state that are final.
/// </summary>
/// <param name="result">The list of final states. Will be filled after the call.</param>
/// <returns>The resulting list of final states. This is the same value as the result parameter, if specified.</returns>
public IList<CharFA<TAccept>>FillFinalStates(IList<CharFA<TAccept>>result=null)=>FillFinalStates(FillClosure(),result);/// <summary>
/// Retrieves all the states in this closure that are final
/// </summary>
/// <param name="closure">The closure to examine</param>
/// <param name="result">The list of final states. Will be filled after the call.</param>
/// <returns>The resulting list of final states. This is the same value as the result parameter, if specified.</returns>
public static IList<CharFA<TAccept>>FillFinalStates(IList<CharFA<TAccept>>closure,IList<CharFA<TAccept>>result=null){if(null==result)result=new List<CharFA<TAccept>>();
for(int ic=closure.Count,i=0;i<ic;++i){var fa=closure[i];if(fa.IsFinal)if(!result.Contains(fa))result.Add(fa);}return result;}/// <summary>
/// Makes all accepting states transition to a new accepting final state, and sets them as non-accepting
/// </summary>
/// <param name="accept">The symbol to accept</param>
public void Finalize(TAccept accept=default(TAccept)){var asc=FillAcceptingStates();var ascc=asc.Count;if(1==ascc)return; var final=new CharFA<TAccept>(true,
accept);for(var i=0;i<ascc;++i){var fa=asc[i];fa.IsAccepting=false;fa.EpsilonTransitions.Add(final);}}}}namespace RE{ partial class CharFA<TAccept>{
#region DotGraphOptions
/// <summary>
/// Represents optional rendering parameters for a dot graph.
/// </summary>
public sealed class DotGraphOptions{/// <summary>
/// The resolution, in dots-per-inch to render at
/// </summary>
public int Dpi{get;set;}=300;/// <summary>
/// The prefix used for state labels
/// </summary>
public string StatePrefix{get;set;}="q";/// <summary>
/// If non-null, specifies a debug render using the specified input string.
/// </summary>
/// <remarks>The debug render is useful for tracking the transitions in a state machine</remarks>
public IEnumerable<char>DebugString{get;set;}=null;/// <summary>
/// If non-null, specifies the source NFA from which this DFA was derived - used for debug view
/// </summary>
public CharFA<TAccept>DebugSourceNfa{get;set;}=null;}
#endregion
/// <summary>
/// Writes a Graphviz dot specification to the specified <see cref="TextWriter"/>
/// </summary>
/// <param name="writer">The writer</param>
/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
public void WriteDotTo(TextWriter writer,DotGraphOptions options=null){_WriteDotTo(FillClosure(),writer,options);}/// <summary>
/// Writes a Graphviz dot specification of the specified closure to the specified <see cref="TextWriter"/>
/// </summary>
/// <param name="closure">The closure of all states</param>
/// <param name="writer">The writer</param>
/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
static void _WriteDotTo(IList<CharFA<TAccept>>closure,TextWriter writer,DotGraphOptions options=null){if(null==options)options=new DotGraphOptions();string
 spfx=null==options.StatePrefix?"q":options.StatePrefix;writer.WriteLine("digraph FA {");writer.WriteLine("rankdir=LR");writer.WriteLine("node [shape=circle]");
var finals=new List<CharFA<TAccept>>();var neutrals=new List<CharFA<TAccept>>();var accepting=FillAcceptingStates(closure,null);foreach(var ffa in closure)
if(ffa.IsFinal&&!ffa.IsAccepting)finals.Add(ffa);IList<CharFA<TAccept>>fromStates=null;IList<CharFA<TAccept>>toStates=null;char tchar=default(char);if
(null!=options.DebugString){toStates=closure[0].FillEpsilonClosure();if(null==fromStates)fromStates=toStates;foreach(char ch in options.DebugString){tchar
=ch;toStates=FillMove(fromStates,ch);if(0==toStates.Count)break;fromStates=toStates;}}if(null!=toStates){toStates=FillEpsilonClosure(toStates,null);}int
 i=0;foreach(var ffa in closure){if(!finals.Contains(ffa)){if(ffa.IsAccepting)accepting.Add(ffa);else if(ffa.IsNeutral)neutrals.Add(ffa);}var rngGrps=
ffa.FillInputTransitionRangesGroupedByState(null);foreach(var rngGrp in rngGrps){var di=closure.IndexOf(rngGrp.Key);writer.Write(spfx);writer.Write(i);
writer.Write("->");writer.Write(spfx);writer.Write(di.ToString());writer.Write(" [label=\"");var sb=new StringBuilder();foreach(var range in rngGrp.Value)
_AppendRangeTo(sb,range);if(sb.Length!=1||" "==sb.ToString()){writer.Write('[');writer.Write(_EscapeLabel(sb.ToString()));writer.Write(']');}else writer.Write(_EscapeLabel(sb.ToString()));
writer.WriteLine("\"]");} foreach(var fffa in ffa.EpsilonTransitions){writer.Write(spfx);writer.Write(i);writer.Write("->");writer.Write(spfx);writer.Write(closure.IndexOf(fffa));
writer.WriteLine(" [style=dashed,color=gray]");}++i;}string delim="";i=0;foreach(var ffa in closure){writer.Write(spfx);writer.Write(i);writer.Write(" [");
if(null!=options.DebugString){if(null!=toStates&&toStates.Contains(ffa))writer.Write("color=green,");else if(null!=fromStates&&fromStates.Contains(ffa)
&&(null==toStates||!toStates.Contains(ffa)))writer.Write("color=darkgreen,");}writer.Write("label=<");writer.Write("<TABLE BORDER=\"0\"><TR><TD>");writer.Write(spfx);
writer.Write("<SUB>");writer.Write(i);writer.Write("</SUB></TD></TR>");if(null!=options.DebugSourceNfa&&null!=ffa.Tag){var tags=ffa.Tag as IEnumerable;
if(null!=tags||ffa.Tag is CharFA<TAccept>){writer.Write("<TR><TD>{");if(null==tags){writer.Write(" q<SUB>");writer.Write(options.DebugSourceNfa.FillClosure().IndexOf((CharFA<TAccept>)ffa.Tag).ToString());
writer.Write("</SUB>");}else{delim="";foreach(var tag in tags){writer.Write(delim);if(tag is CharFA<TAccept>){writer.Write(delim);writer.Write(" q<SUB>");
writer.Write(options.DebugSourceNfa.FillClosure().IndexOf((CharFA<TAccept>)tag).ToString());writer.Write("</SUB>"); delim=@" ";}}}writer.Write(" }</TD></TR>");
}}if(ffa.IsAccepting){writer.Write("<TR><TD>");writer.Write(Convert.ToString(ffa.AcceptSymbol).Replace("\"","&quot;"));writer.Write("</TD></TR>");}writer.Write("</TABLE>");
writer.Write(">");bool isfinal=false;if(accepting.Contains(ffa)||(isfinal=finals.Contains(ffa)))writer.Write(",shape=doublecircle");if(isfinal||neutrals.Contains(ffa))
{if((null==fromStates||!fromStates.Contains(ffa))&&(null==toStates||!toStates.Contains(ffa))){writer.Write(",color=gray");}}writer.WriteLine("]");++i;
}delim="";if(0<accepting.Count){foreach(var ntfa in accepting){writer.Write(delim);writer.Write(spfx);writer.Write(closure.IndexOf(ntfa));delim=",";}writer.WriteLine(" [shape=doublecircle]");
}delim="";if(0<neutrals.Count){foreach(var ntfa in neutrals){if((null==fromStates||!fromStates.Contains(ntfa))&&(null==toStates||!toStates.Contains(ntfa))
){writer.Write(delim);writer.Write(spfx);writer.Write(closure.IndexOf(ntfa));delim=",";}}writer.WriteLine(" [color=gray]");delim="";}delim="";if(0<finals.Count)
{foreach(var ntfa in finals){writer.Write(delim);writer.Write(spfx);writer.Write(closure.IndexOf(ntfa));delim=",";}writer.WriteLine(" [shape=doublecircle,color=gray]");
}writer.WriteLine("}");}/// <summary>
/// Renders Graphviz output for this machine to the specified file
/// </summary>
/// <param name="filename">The output filename. The format to render is indicated by the file extension.</param>
/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
public void RenderToFile(string filename,DotGraphOptions options=null){if(null==options)options=new DotGraphOptions();string args="-T";string ext=Path.GetExtension(filename);
if(0==string.Compare(".png",ext,StringComparison.InvariantCultureIgnoreCase))args+="png";else if(0==string.Compare(".jpg",ext,StringComparison.InvariantCultureIgnoreCase))
args+="jpg";else if(0==string.Compare(".bmp",ext,StringComparison.InvariantCultureIgnoreCase))args+="bmp";else if(0==string.Compare(".svg",ext,StringComparison.InvariantCultureIgnoreCase))
args+="svg";if(0<options.Dpi)args+=" -Gdpi="+options.Dpi.ToString();args+=" -o\""+filename+"\"";var psi=new ProcessStartInfo("dot",args){CreateNoWindow
=true,UseShellExecute=false,RedirectStandardInput=true};using(var proc=Process.Start(psi)){WriteDotTo(proc.StandardInput,options);proc.StandardInput.Close();
proc.WaitForExit();}}/// <summary>
/// Renders Graphviz output for this machine to a stream
/// </summary>
/// <param name="format">The output format. The format to render can be any supported dot output format. See dot command line documation for details.</param>
/// <param name="copy">True to copy the stream, otherwise false</param>
/// <param name="options">A <see cref="DotGraphOptions"/> instance with any options, or null to use the defaults</param>
/// <returns>A stream containing the output. The caller is expected to close the stream when finished.</returns>
public Stream RenderToStream(string format,bool copy=false,DotGraphOptions options=null){if(null==options)options=new DotGraphOptions();string args="-T";
args+=string.Concat(" ",format);if(0<options.Dpi)args+=" -Gdpi="+options.Dpi.ToString();var psi=new ProcessStartInfo("dot",args){CreateNoWindow=true,UseShellExecute
=false,RedirectStandardInput=true,RedirectStandardOutput=true};using(var proc=Process.Start(psi)){WriteDotTo(proc.StandardInput,options);proc.StandardInput.Close();
if(!copy)return proc.StandardOutput.BaseStream;else{MemoryStream stm=new MemoryStream();proc.StandardOutput.BaseStream.CopyTo(stm);proc.StandardOutput.BaseStream.Close();
proc.WaitForExit();return stm;}}}static void _AppendRangeTo(StringBuilder builder,CharRange range){_AppendRangeCharTo(builder,range.First);if(0==range.Last.CompareTo(range.First))
return;if(range.Last==range.First+1){_AppendRangeCharTo(builder,range.Last);return;}builder.Append('-');_AppendRangeCharTo(builder,range.Last);}static
 void _AppendRangeCharTo(StringBuilder builder,char rangeChar){switch(rangeChar){case'-':case'\\':builder.Append('\\');builder.Append(rangeChar);return;
case'\t':builder.Append("\\t");return;case'\n':builder.Append("\\n");return;case'\r':builder.Append("\\r");return;case'\0':builder.Append("\\0");return;
case'\f':builder.Append("\\f");return;case'\v':builder.Append("\\v");return;case'\b':builder.Append("\\b");return;default:if(!char.IsLetterOrDigit(rangeChar)
&&!char.IsSeparator(rangeChar)&&!char.IsPunctuation(rangeChar)&&!char.IsSymbol(rangeChar)){builder.Append("\\u");builder.Append(unchecked((ushort)rangeChar).ToString("x4"));
}else builder.Append(rangeChar);break;}}static string _EscapeLabel(string label){if(string.IsNullOrEmpty(label))return label;string result=label.Replace("\\",
@"\\");result=result.Replace("\"","\\\"");result=result.Replace("\n","\\n");result=result.Replace("\r","\\r");result=result.Replace("\0","\\0");result
=result.Replace("\v","\\v");result=result.Replace("\t","\\t");result=result.Replace("\f","\\f");return result;}}}namespace RE{ partial class CharFA<TAccept>
{/// <summary>
/// This is a specialized transition container that can return its transitions in 3 different ways:
/// 1. a dictionary where each transition state is keyed by an individual input character (default)
/// 2. a dictionary where each collection of inputs is keyed by the transition state (used mostly by optimizations)
/// 3. an indexable list of pairs where the key is the transition state and the value is the collection of inputs
/// use casts to get at the appropriate interface for your operation.
/// </summary>
private class _InputTransitionDictionary:IDictionary<char,CharFA<TAccept>>, IDictionary<CharFA<TAccept>,ICollection<char>>, IList<KeyValuePair<CharFA<TAccept>,
ICollection<char>>>{IDictionary<CharFA<TAccept>,ICollection<char>>_inner=new ListDictionary<CharFA<TAccept>,ICollection<char>>();public CharFA<TAccept>
this[char key]{get{foreach(var trns in _inner){if(trns.Value.Contains(key))return trns.Key;}throw new KeyNotFoundException();}set{Remove(key);ICollection<char>
hs;if(_inner.TryGetValue(value,out hs)){hs.Add(key);}else{hs=new HashSet<char>();hs.Add(key);_inner.Add(value,hs);}}}public ICollection<char>Keys{get{
return new _KeysCollection(_inner);}}sealed class _KeysCollection:ICollection<char>{IDictionary<CharFA<TAccept>,ICollection<char>>_inner;public _KeysCollection(IDictionary<CharFA<TAccept>,
ICollection<char>>inner){_inner=inner;}public int Count{get{var result=0;foreach(var val in _inner.Values)result+=val.Count;return result;}}void _ThrowReadOnly()
{throw new NotSupportedException("The collection is read-only.");}public bool IsReadOnly=>true;public void Add(char item){_ThrowReadOnly();}public void
 Clear(){_ThrowReadOnly();}public bool Contains(char item){foreach(var val in _inner.Values)if(val.Contains(item))return true;return false;}public void
 CopyTo(char[]array,int arrayIndex){var si=arrayIndex;foreach(var val in _inner.Values){val.CopyTo(array,si);si+=val.Count;}}public IEnumerator<char>GetEnumerator()
{foreach(var val in _inner.Values)foreach(var ch in val)yield return ch;}public bool Remove(char item){_ThrowReadOnly();return false;}IEnumerator IEnumerable.GetEnumerator()
=>GetEnumerator();}sealed class _ValuesCollection:ICollection<CharFA<TAccept>>{IDictionary<CharFA<TAccept>,ICollection<char>>_inner;public _ValuesCollection(IDictionary<CharFA<TAccept>,
ICollection<char>>inner){_inner=inner;}public int Count{get{var result=0;foreach(var val in _inner.Values)result+=val.Count;return result;}}void _ThrowReadOnly()
{throw new NotSupportedException("The collection is read-only.");}public bool IsReadOnly=>true;public void Add(CharFA<TAccept>item){_ThrowReadOnly();}
public void Clear(){_ThrowReadOnly();}public bool Contains(CharFA<TAccept>item){return _inner.Keys.Contains(item);}public void CopyTo(CharFA<TAccept>[]
array,int arrayIndex){var si=arrayIndex;foreach(var trns in _inner){foreach(var ch in trns.Value){array[si]=trns.Key;++si;}}}public IEnumerator<CharFA<TAccept>>
GetEnumerator(){foreach(var trns in _inner)foreach(var ch in trns.Value)yield return trns.Key;}public bool Remove(CharFA<TAccept>item){_ThrowReadOnly();
return false;}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}public ICollection<CharFA<TAccept>>Values=>new _ValuesCollection(_inner);public
 int Count{get{var result=0;foreach(var trns in _inner)result+=trns.Value.Count;return result;}}IList<KeyValuePair<CharFA<TAccept>,ICollection<char>>>
_InnerList=>_inner as IList<KeyValuePair<CharFA<TAccept>,ICollection<char>>>;ICollection<CharFA<TAccept>>IDictionary<CharFA<TAccept>,ICollection<char>>.Keys
=>_inner.Keys;ICollection<ICollection<char>>IDictionary<CharFA<TAccept>,ICollection<char>>.Values=>_inner.Values;int ICollection<KeyValuePair<CharFA<TAccept>,
ICollection<char>>>.Count=>_inner.Count;public bool IsReadOnly=>_inner.IsReadOnly;KeyValuePair<CharFA<TAccept>,ICollection<char>>IList<KeyValuePair<CharFA<TAccept>,
ICollection<char>>>.this[int index]{get=>_InnerList[index];set{_InnerList[index]=value;}}ICollection<char>IDictionary<CharFA<TAccept>,ICollection<char>>.this[CharFA<TAccept>
key]{get{return _inner[key];}set{_inner[key]=value;}}public void Add(char key,CharFA<TAccept>value){if(null==value)throw new ArgumentNullException(nameof(value));
if(ContainsKey(key))throw new InvalidOperationException("The key is already present in the dictionary.");if(null==value)throw new ArgumentNullException(nameof(value));
ICollection<char>hs;if(_inner.TryGetValue(value,out hs)){hs.Add(key);}else{hs=new HashSet<char>();hs.Add(key);_inner.Add(value,hs);}}public void Add(KeyValuePair<char,
CharFA<TAccept>>item)=>Add(item.Key,item.Value);public void Clear()=>_inner.Clear();public bool Contains(KeyValuePair<char,CharFA<TAccept>>item){ICollection<char>
hs;return _inner.TryGetValue(item.Value,out hs)&&hs.Contains(item.Key);}public bool ContainsKey(char key){foreach(var trns in _inner){if(trns.Value.Contains(key))
return true;}return false;}public void CopyTo(KeyValuePair<char,CharFA<TAccept>>[]array,int arrayIndex){using(var e=((IEnumerable<KeyValuePair<char,CharFA<TAccept>>>)this).GetEnumerator())
{var i=arrayIndex;while(e.MoveNext()){array[i]=e.Current;++i;}}}public IEnumerator<KeyValuePair<char,CharFA<TAccept>>>GetEnumerator(){foreach(var trns
 in _inner)foreach(var ch in trns.Value)yield return new KeyValuePair<char,CharFA<TAccept>>(ch,trns.Key);}public bool Remove(char key){CharFA<TAccept>
rem=null;foreach(var trns in _inner){if(trns.Value.Contains(key)){trns.Value.Remove(key);if(0==trns.Value.Count){rem=trns.Key;break;}return true;}}if(null
!=rem){_inner.Remove(rem);return true;}return false;}public bool Remove(KeyValuePair<char,CharFA<TAccept>>item){ICollection<char>hs;if(_inner.TryGetValue(item.Value,
out hs)){if(hs.Contains(item.Key)){if(1==hs.Count)_inner.Remove(item.Value);else hs.Remove(item.Key);return true;}}return false;}public bool TryGetValue(char
 key,out CharFA<TAccept>value){foreach(var trns in _inner){if(trns.Value.Contains(key)){value=trns.Key;return true;}}value=null;return false;}IEnumerator
 IEnumerable.GetEnumerator()=>GetEnumerator();void IDictionary<CharFA<TAccept>,ICollection<char>>.Add(CharFA<TAccept>key,ICollection<char>value){if(null
==value)throw new ArgumentNullException(nameof(value));_inner.Add(key,value);}bool IDictionary<CharFA<TAccept>,ICollection<char>>.ContainsKey(CharFA<TAccept>
key)=>_inner.ContainsKey(key);bool IDictionary<CharFA<TAccept>,ICollection<char>>.Remove(CharFA<TAccept>key)=>_inner.Remove(key);bool IDictionary<CharFA<TAccept>,
ICollection<char>>.TryGetValue(CharFA<TAccept>key,out ICollection<char>value)=>_inner.TryGetValue(key,out value);void ICollection<KeyValuePair<CharFA<TAccept>,
ICollection<char>>>.Add(KeyValuePair<CharFA<TAccept>,ICollection<char>>item){if(null==item.Key)throw new ArgumentNullException(nameof(item),"The state cannot be null");
if(null==item.Value)throw new ArgumentNullException(nameof(item),"The collection cannot be null");_inner.Add(item);}bool ICollection<KeyValuePair<CharFA<TAccept>,
ICollection<char>>>.Contains(KeyValuePair<CharFA<TAccept>,ICollection<char>>item)=>_inner.Contains(item);void ICollection<KeyValuePair<CharFA<TAccept>,
ICollection<char>>>.CopyTo(KeyValuePair<CharFA<TAccept>,ICollection<char>>[]array,int arrayIndex)=>_inner.CopyTo(array,arrayIndex);bool ICollection<KeyValuePair<CharFA<TAccept>,
ICollection<char>>>.Remove(KeyValuePair<CharFA<TAccept>,ICollection<char>>item)=>_inner.Remove(item);IEnumerator<KeyValuePair<CharFA<TAccept>,ICollection<char>>>
IEnumerable<KeyValuePair<CharFA<TAccept>,ICollection<char>>>.GetEnumerator()=>_inner.GetEnumerator();int IList<KeyValuePair<CharFA<TAccept>,ICollection<char>>>.IndexOf(KeyValuePair<CharFA<TAccept>,
ICollection<char>>item)=>_InnerList.IndexOf(item);void IList<KeyValuePair<CharFA<TAccept>,ICollection<char>>>.Insert(int index,KeyValuePair<CharFA<TAccept>,
ICollection<char>>item){if(null==item.Key)throw new ArgumentNullException(nameof(item),"The state cannot be null");if(null==item.Value)throw new ArgumentNullException(nameof(item),
"The collection cannot be null");_InnerList.Insert(index,item);}void IList<KeyValuePair<CharFA<TAccept>,ICollection<char>>>.RemoveAt(int index)=>_InnerList.RemoveAt(index);
}}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Creates a lexer out of the specified FSM "expressions"
/// </summary>
/// <param name="exprs">The expressions to compose the lexer with</param>
/// <returns>An FSM representing the lexer.</returns>
public static CharFA<TAccept>ToLexer(params CharFA<TAccept>[]exprs){var result=new CharFA<TAccept>();for(var i=0;i<exprs.Length;i++)result.EpsilonTransitions.Add(exprs[i]);
return result;}/// <summary>
/// Lexes the next input from the parse context.
/// </summary>
/// <param name="context">The <see cref="ParseContext"/> to use.</param>
/// <param name="errorSymbol">The symbol to report in the case of an error</param>
/// <returns>The next symbol matched - <paramref name="context"/> contains the capture and line information</returns>
public TAccept Lex(ParseContext context,TAccept errorSymbol=default(TAccept)){TAccept acc; var states=FillEpsilonClosure(); context.EnsureStarted();while
(true){ if(-1==context.Current){ if(TryGetAnyAcceptSymbol(states,out acc))return acc; return errorSymbol;} var newStates=FillMove(states,(char)context.Current);
 if(0==newStates.Count){ if(TryGetAnyAcceptSymbol(states,out acc))return acc; context.CaptureCurrent(); context.Advance();return errorSymbol;} context.CaptureCurrent();
 context.Advance(); states=newStates;}}/// <summary>
/// Lexes the next input from the parse context.
/// </summary>
/// <param name="context">The <see cref="ParseContext"/> to use.</param>
/// <param name="errorSymbol">The symbol to report in the case of an error</param>
/// <returns>The next symbol matched - <paramref name="context"/> contains the capture and line information</returns>
/// <remarks>This method will not work properly on an NFA but will not error in that case, so take care to only use this with a DFA</remarks>
public TAccept LexDfa(ParseContext context,TAccept errorSymbol=default(TAccept)){ var state=this; context.EnsureStarted();while(true){ if(-1==context.Current)
{ if(state.IsAccepting)return state.AcceptSymbol; return errorSymbol;} var newState=state.MoveDfa((char)context.Current); if(null==newState){ if(state.IsAccepting)
return state.AcceptSymbol; context.CaptureCurrent(); context.Advance();return errorSymbol;} context.CaptureCurrent(); context.Advance(); state=newState;
}}/// <summary>
/// Lexes the next input from the parse context.
/// </summary>
/// <param name="dfaTable">The DFA state table to use</param>
/// <param name="context">The <see cref="ParseContext"/> to use.</param>
/// <param name="errorSymbol">The symbol id to report in the case of an error</param>
/// <returns>The next symbol id matched - <paramref name="context"/> contains the capture and line information</returns>
public static int LexDfa(CharDfaEntry[]dfaTable,ParseContext context,int errorSymbol=-1){ var state=0; context.EnsureStarted();while(true){ if(-1==context.Current)
{var sid=dfaTable[state].AcceptSymbolId; if(-1!=sid)return sid; return errorSymbol;} var newState=MoveDfa(dfaTable,state,(char)context.Current); if(-1
==newState){ if(-1!=dfaTable[state].AcceptSymbolId)return dfaTable[state].AcceptSymbolId; context.CaptureCurrent(); context.Advance();return errorSymbol;
} context.CaptureCurrent(); context.Advance(); state=newState;}}}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Pattern matches through a string of text
/// </summary>
/// <param name="context">The parse context to search</param>
/// <returns>A <see cref="CharFAMatch"/> that contains the match information, or null if the match is not found.</returns>
public CharFAMatch Match(ParseContext context){context.EnsureStarted();var line=context.Line;var column=context.Column;var position=context.Position;var
 l=context.CaptureBuffer.Length;var success=false; while(-1!=context.Current&&!(success=_DoMatch(context))){line=context.Line;column=context.Column;position
=context.Position;l=context.CaptureBuffer.Length;}if(success)return new CharFAMatch(line,column,position,context.GetCapture(l));return null;}/// <summary>
/// Pattern matches through a string of text using a DFA
/// </summary>
/// <param name="context">The parse context to search</param>
/// <returns>A <see cref="CharFAMatch"/> that contains the match information, or null if the match is not found.</returns>
/// <remarks>An NFA will not work with this method, but for performance reasons we cannot verify that the state machine is a DFA before running. Be sure to only use DFAs with this method.</remarks>
public CharFAMatch MatchDfa(ParseContext context){context.EnsureStarted();var line=context.Line;var column=context.Column;var position=context.Position;
var l=context.CaptureBuffer.Length;var success=false; while(-1!=context.Current&&!(success=_DoMatchDfa(context))){line=context.Line;column=context.Column;
position=context.Position;l=context.CaptureBuffer.Length;}if(success)return new CharFAMatch(line,column,position,context.GetCapture(l));return null;}/// <summary>
/// Pattern matches through a string of text using a DFA
/// </summary>
/// <param name="dfaTable">The DFA state table to use</param>
/// <param name="context">The parse context to search</param>
/// <returns>A <see cref="CharFAMatch"/> that contains the match information, or null if the match is not found.</returns>
public static CharFAMatch MatchDfa(CharDfaEntry[]dfaTable,ParseContext context){context.EnsureStarted();var line=context.Line;var column=context.Column;
var position=context.Position;var l=context.CaptureBuffer.Length;var success=false; while(-1!=context.Current&&!(success=_DoMatchDfa(dfaTable,context)))
{line=context.Line;column=context.Column;position=context.Position;l=context.CaptureBuffer.Length;}if(success)return new CharFAMatch(line,column,position,
context.GetCapture(l));return null;} bool _DoMatch(ParseContext context){ var states=FillEpsilonClosure();while(true){ if(-1==context.Current){ return
 IsAnyAccepting(states);} var newStates=FillMove(states,(char)context.Current); if(0==newStates.Count){ if(IsAnyAccepting(states))return true; context.Advance();
return false;} context.CaptureCurrent(); context.Advance(); states=newStates;}}bool _DoMatchDfa(ParseContext context){ var state=this;while(true){ if(-1
==context.Current){ return state.IsAccepting;} var newState=state.MoveDfa((char)context.Current); if(null==newState){ if(state.IsAccepting)return true;
 context.Advance();return false;} context.CaptureCurrent(); context.Advance(); state=newState;}}static bool _DoMatchDfa(CharDfaEntry[]dfaTable,ParseContext
 context){ var state=0; context.EnsureStarted();while(true){ if(-1==context.Current) return-1!=dfaTable[state].AcceptSymbolId; var newState=MoveDfa(dfaTable,
state,(char)context.Current); if(-1==newState){ if(-1!=dfaTable[state].AcceptSymbolId)return true; context.Advance();return false;} context.CaptureCurrent();
 context.Advance(); state=newState;}}}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Indicates whether or not the state is neutral
/// </summary>
public bool IsNeutral{get{return!IsAccepting&&0==InputTransitions.Count&&1==EpsilonTransitions.Count;}}/// <summary>
/// Retrieves all the states reachable from this state that are neutral.
/// </summary>
/// <param name="result">The list of neutral states. Will be filled after the call.</param>
/// <returns>The resulting list of neutral states. This is the same value as the result parameter, if specified.</returns>
public IList<CharFA<TAccept>>FillNeutralStates(IList<CharFA<TAccept>>result=null)=>FillNeutralStates(FillClosure(),result);/// <summary>
/// Retrieves all the states in this closure that are neutral
/// </summary>
/// <param name="closure">The closure to examine</param>
/// <param name="result">The list of neutral states. Will be filled after the call.</param>
/// <returns>The resulting list of neutral states. This is the same value as the result parameter, if specified.</returns>
public static IList<CharFA<TAccept>>FillNeutralStates(IList<CharFA<TAccept>>closure,IList<CharFA<TAccept>>result=null){if(null==result)result=new List<CharFA<TAccept>>();
for(int ic=closure.Count,i=0;i<ic;++i){var fa=closure[i];if(fa.IsNeutral)if(!result.Contains(fa))result.Add(fa);}return result;}static bool _TryForwardNeutral(CharFA<TAccept>
fa,out CharFA<TAccept>result){if(!fa.IsNeutral){result=fa;return false;}result=fa.EpsilonTransitions[0];return fa!=result;}static CharFA<TAccept>_ForwardNeutrals(CharFA<TAccept>
fa){if(null==fa)throw new ArgumentNullException(nameof(fa));var result=fa;while(_TryForwardNeutral(result,out result));return result;}/// <summary>
/// Trims the neutral states from this machine
/// </summary>
public void TrimNeutrals(){TrimNeutrals(FillClosure());}/// <summary>
/// Trims the neutral states from the specified closure
/// </summary>
/// <param name="closure">The set of all states</param>
public static void TrimNeutrals(IEnumerable<CharFA<TAccept>>closure){var cl=new List<CharFA<TAccept>>(closure);foreach(var s in cl){var repls=new List<KeyValuePair<CharFA<TAccept>,
CharFA<TAccept>>>();var td=s.InputTransitions as IDictionary<CharFA<TAccept>,ICollection<char>>;foreach(var trns in td){var fa=trns.Key;var fa2=_ForwardNeutrals(fa);
if(null==fa2)throw new InvalidProgramException("null in forward neutrals support code");if(fa!=fa2)repls.Add(new KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>(fa,
fa2));}foreach(var repl in repls){var inps=td[repl.Key];td.Remove(repl.Key);td.Add(repl.Value,inps);}var ec=s.EpsilonTransitions.Count;for(int j=0;j<ec;
++j)s.EpsilonTransitions[j]=_ForwardNeutrals(s.EpsilonTransitions[j]);}}}}namespace RE{/// <summary>
/// Extends the parse context
/// </summary>
#if REGEXLIB
public
#endif
static partial class ParseContextExtensions{/// <summary>
/// Lexes a token out of the input
/// </summary>
/// <param name="context">The input parse context</param>
/// <param name="dfaTable">The DFA state table</param>
/// <param name="errorSymbol">The optional symbol id to report on error</param>
/// <returns>A symbol id representing the next token. The capture buffer contains the captured content.</returns>
public static int Lex(this ParseContext context,CharDfaEntry[]dfaTable,int errorSymbol=-1){return CharFA<string>.LexDfa(dfaTable,context,errorSymbol);
}}}namespace RE{partial class CharFA<TAccept>{/// <summary>
/// Transforms an NFA to a DFA
/// </summary>
/// <param name="progress">The optional progress object used to report the progress of the operation</param>
/// <returns>A new finite state machine equivilent to this state machine but with no epsilon transitions</returns>
public CharFA<TAccept>ToDfa(IProgress<CharFAProgress>progress=null){ if(IsDfa)return Clone(); var dfaMap=new Dictionary<List<CharFA<TAccept>>,CharFA<TAccept>>(_SetComparer.Default);
var unmarked=new HashSet<CharFA<TAccept>>(); var states=new List<CharFA<TAccept>>();FillEpsilonClosure(states); CharFA<TAccept>dfa=new CharFA<TAccept>();
var al=new List<TAccept>(); foreach(var fa in states)if(fa.IsAccepting)if(!al.Contains(fa.AcceptSymbol))al.Add(fa.AcceptSymbol); int ac=al.Count;if(1==
ac)dfa.AcceptSymbol=al[0];else if(1<ac)dfa.AcceptSymbol=al[0]; dfa.IsAccepting=0<ac;CharFA<TAccept>result=dfa; dfaMap.Add(states,dfa);dfa.Tag=new List<CharFA<TAccept>>(states);
 unmarked.Add(dfa);bool done=false;var j=0;while(!done){ if(null!=progress)progress.Report(new CharFAProgress(CharFAStatus.DfaTransform,j));done=true;
 var mapKeys=new HashSet<List<CharFA<TAccept>>>(dfaMap.Keys,_SetComparer.Default);foreach(var mapKey in mapKeys){dfa=dfaMap[mapKey];if(unmarked.Contains(dfa))
{ var inputs=new HashSet<char>();foreach(var state in mapKey){var dtrns=state.InputTransitions as IDictionary<CharFA<TAccept>,ICollection<char>>;foreach
(var trns in dtrns)foreach(var inp in trns.Value)inputs.Add(inp);} foreach(var input in inputs){var acc=new List<TAccept>();var ns=new List<CharFA<TAccept>>();
foreach(var state in mapKey){CharFA<TAccept>dst=null;if(state.InputTransitions.TryGetValue(input,out dst)){foreach(var d in dst.FillEpsilonClosure()){
 if(d.IsAccepting)if(!acc.Contains(d.AcceptSymbol))acc.Add(d.AcceptSymbol);if(!ns.Contains(d))ns.Add(d);}}}CharFA<TAccept>ndfa;if(!dfaMap.TryGetValue(ns,
out ndfa)){ac=acc.Count;ndfa=new CharFA<TAccept>(0<ac); if(1==ac)ndfa.AcceptSymbol=acc[0];else if(1<ac)ndfa.AcceptSymbol=acc[0]; dfaMap.Add(ns,ndfa); unmarked.Add(ndfa);
ndfa.Tag=new List<CharFA<TAccept>>(ns);done=false;}dfa.InputTransitions.Add(input,ndfa);} unmarked.Remove(dfa);}}++j;}return result;}/// <summary>
/// Indicates whether or not this state machine is a DFA
/// </summary>
public bool IsDfa{get{ foreach(var fa in FillClosure())if(0!=fa.EpsilonTransitions.Count)return false;return true;}}}}namespace RE{partial class CharFA<TAccept>
{/// <summary>
/// Reduces the complexity of the graph, and returns the result as a new graph
/// </summary>
/// <returns>A new graph with a complexity of 1</returns>
public CharFA<TAccept>Reduce(IProgress<CharFAProgress>progress=null){var fa=Clone();while(true){var cc=fa.FillClosure().Count;fa.Finalize();fa=fa.ToDfa(progress);
fa.TrimDuplicates(progress);if(fa.FillClosure().Count==cc)return fa;}}}}namespace RE{partial class CharFA<TAccept>{ private sealed class _SetComparer:
IEqualityComparer<IList<CharFA<TAccept>>>,IEqualityComparer<ICollection<CharFA<TAccept>>>,IEqualityComparer<IDictionary<char,CharFA<TAccept>>>{ public
 bool Equals(IList<CharFA<TAccept>>lhs,IList<CharFA<TAccept>>rhs){if(ReferenceEquals(lhs,rhs))return true;else if(ReferenceEquals(null,lhs)||ReferenceEquals(null,
rhs))return false;if(lhs.Count!=rhs.Count)return false;using(var xe=lhs.GetEnumerator())using(var ye=rhs.GetEnumerator())while(xe.MoveNext()&&ye.MoveNext())
if(!rhs.Contains(xe.Current)||!lhs.Contains(ye.Current))return false;return true;} public bool Equals(ICollection<CharFA<TAccept>>lhs,ICollection<CharFA<TAccept>>
rhs){if(ReferenceEquals(lhs,rhs))return true;else if(ReferenceEquals(null,lhs)||ReferenceEquals(null,rhs))return false;if(lhs.Count!=rhs.Count)return false;
using(var xe=lhs.GetEnumerator())using(var ye=rhs.GetEnumerator())while(xe.MoveNext()&&ye.MoveNext())if(!rhs.Contains(xe.Current)||!lhs.Contains(ye.Current))
return false;return true;}public bool Equals(IDictionary<char,CharFA<TAccept>>lhs,IDictionary<char,CharFA<TAccept>>rhs){if(ReferenceEquals(lhs,rhs))return
 true;else if(ReferenceEquals(null,lhs)||ReferenceEquals(null,rhs))return false;if(lhs.Count!=rhs.Count)return false;using(var xe=lhs.GetEnumerator())
using(var ye=rhs.GetEnumerator())while(xe.MoveNext()&&ye.MoveNext())if(!rhs.Contains(xe.Current)||!lhs.Contains(ye.Current))return false;return true;}
public bool Equals(IDictionary<CharFA<TAccept>,ICollection<char>>lhs,IDictionary<CharFA<TAccept>,ICollection<char>>rhs){if(ReferenceEquals(lhs,rhs))return
 true;else if(ReferenceEquals(null,lhs)||ReferenceEquals(null,rhs))return false;if(lhs.Count!=rhs.Count)return false;foreach(var trns in lhs){ICollection<char>
col;if(!rhs.TryGetValue(trns.Key,out col))return false;using(var xe=trns.Value.GetEnumerator())using(var ye=col.GetEnumerator())while(xe.MoveNext()&&ye.MoveNext())
if(!col.Contains(xe.Current)||!trns.Value.Contains(ye.Current))return false;}return true;}public static bool _EqualsInput(ICollection<char>lhs,ICollection<char>
rhs){if(ReferenceEquals(lhs,rhs))return true;else if(ReferenceEquals(null,lhs)||ReferenceEquals(null,rhs))return false;if(lhs.Count!=rhs.Count)return false;
using(var xe=lhs.GetEnumerator())using(var ye=rhs.GetEnumerator())while(xe.MoveNext()&&ye.MoveNext())if(!rhs.Contains(xe.Current)||!lhs.Contains(ye.Current))
return false;return true;}public int GetHashCode(IList<CharFA<TAccept>>lhs){var result=0;for(int ic=lhs.Count,i=0;i<ic;++i){var fa=lhs[i];if(null!=fa)
result^=fa.GetHashCode();}return result;}public int GetHashCode(ICollection<CharFA<TAccept>>lhs){var result=0;foreach(var fa in lhs)if(null!=fa)result
^=fa.GetHashCode();return result;}public int GetHashCode(IDictionary<char,CharFA<TAccept>>lhs){var result=0;foreach(var kvp in lhs)result^=kvp.GetHashCode();
return result;}public static readonly _SetComparer Default=new _SetComparer();}}}namespace RE{ partial class CharFA<TAccept>{/// <summary>
/// Creates an FA that matches a literal string
/// </summary>
/// <param name="string">The string to match</param>
/// <param name="accept">The symbol to accept</param>
/// <returns>A new FA machine that will match this literal</returns>
public static CharFA<TAccept>Literal(IEnumerable<char>@string,TAccept accept=default(TAccept)){var result=new CharFA<TAccept>();var current=result;foreach
(var ch in@string){current.IsAccepting=false;var fa=new CharFA<TAccept>(true,accept);current.InputTransitions.Add(ch,fa);current=fa;}return result;}/// <summary>
/// Creates an FA that will match any one of a set of a characters
/// </summary>
/// <param name="set">The set of characters that will be matched</param>
/// <param name="accept">The symbol to accept</param>
/// <returns>An FA that will match the specified set</returns>
public static CharFA<TAccept>Set(IEnumerable<char>set,TAccept accept=default(TAccept)){var result=new CharFA<TAccept>();var final=new CharFA<TAccept>(true,
accept);foreach(var ch in set)result.InputTransitions[ch]=final;return result;}/// <summary>
/// Creates a new FA that is a concatenation of two other FA expressions
/// </summary>
/// <param name="exprs">The FAs to concatenate</param>
/// <param name="accept">The symbol to accept</param>
/// <returns>A new FA that is the concatenation of the specified FAs</returns>
public static CharFA<TAccept>Concat(IEnumerable<CharFA<TAccept>>exprs,TAccept accept=default(TAccept)){CharFA<TAccept>left=null;var right=left;foreach
(var val in exprs){if(null==val)continue; var nval=val.Clone(); if(null==left){left=nval; continue;}else if(null==right){right=nval;}else{ _Concat(right,
nval);} _Concat(left,right.Clone());}if(null!=right){right.FirstAcceptingState.AcceptSymbol=accept;}else{left.FirstAcceptingState.AcceptSymbol=accept;
}return left;}static void _Concat(CharFA<TAccept>lhs,CharFA<TAccept>rhs){ var f=lhs.FirstAcceptingState; f.IsAccepting=false;f.EpsilonTransitions.Add(rhs);
}/// <summary>
/// Creates an FA that will match any one of a set of a characters
/// </summary>
/// <param name="ranges">The set ranges of characters that will be matched</param>
/// <param name="accept">The symbol to accept</param>
/// <returns>An FA that will match the specified set</returns>
public static CharFA<TAccept>Set(IEnumerable<CharRange>ranges,TAccept accept=default(TAccept)){var result=new CharFA<TAccept>();var final=new CharFA<TAccept>(true,
accept);foreach(var ch in CharRange.ExpandRanges(ranges))result.InputTransitions[ch]=final;return result;}/// <summary>
/// Creates a new FA that matches any one of the FA expressions passed
/// </summary>
/// <param name="exprs">The expressions to match</param>
/// <param name="accept">The symbol to accept</param>
/// <returns>A new FA that will match the union of the FA expressions passed</returns>
public static CharFA<TAccept>Or(IEnumerable<CharFA<TAccept>>exprs,TAccept accept=default(TAccept)){var result=new CharFA<TAccept>();var final=new CharFA<TAccept>(true,
accept);foreach(var fa in exprs){if(null!=fa){var nfa=fa.Clone();result.EpsilonTransitions.Add(nfa);var nffa=nfa.FirstAcceptingState;nffa.IsAccepting=
false;nffa.EpsilonTransitions.Add(final);}else if(!result.EpsilonTransitions.Contains(final))result.EpsilonTransitions.Add(final);}return result;}/// <summary>
/// Creates a new FA that will match a repetition of the specified FA expression
/// </summary>
/// <param name="expr">The expression to repeat</param>
/// <param name="minOccurs">The minimum number of times to repeat or -1 for unspecified (0)</param>
/// <param name="maxOccurs">The maximum number of times to repeat or -1 for unspecified (unbounded)</param>
/// <param name="accept">The symbol to accept</param>
/// <returns>A new FA that matches the specified FA one or more times</returns>
public static CharFA<TAccept>Repeat(CharFA<TAccept>expr,int minOccurs=-1,int maxOccurs=-1,TAccept accept=default(TAccept)){expr=expr.Clone();if(minOccurs
>0&&maxOccurs>0&&minOccurs>maxOccurs)throw new ArgumentOutOfRangeException(nameof(maxOccurs));CharFA<TAccept>result;switch(minOccurs){case-1:case 0:switch
(maxOccurs){case-1:case 0:result=new CharFA<TAccept>();var final=new CharFA<TAccept>(true,accept);final.EpsilonTransitions.Add(result);foreach(var afa
 in expr.FillAcceptingStates()){afa.IsAccepting=false;afa.EpsilonTransitions.Add(final);}result.EpsilonTransitions.Add(expr);result.EpsilonTransitions.Add(final);
 return result;case 1:result=Optional(expr,accept); return result;default:var l=new List<CharFA<TAccept>>();expr=Optional(expr);l.Add(expr);for(int i=
1;i<maxOccurs;++i){l.Add(expr.Clone());}result=Concat(l,accept); return result;}case 1:switch(maxOccurs){case-1:case 0:result=new CharFA<TAccept>();var
 final=new CharFA<TAccept>(true,accept);final.EpsilonTransitions.Add(result);foreach(var afa in expr.FillAcceptingStates()){afa.IsAccepting=false;afa.EpsilonTransitions.Add(final);
}result.EpsilonTransitions.Add(expr); return result;case 1: return expr;default:result=Concat(new CharFA<TAccept>[]{expr,Repeat(expr.Clone(),0,maxOccurs
-1)},accept); return result;}default:switch(maxOccurs){case-1:case 0:result=Concat(new CharFA<TAccept>[]{Repeat(expr,minOccurs,minOccurs,accept),Repeat(expr,
0,0,accept)},accept); return result;case 1:throw new ArgumentOutOfRangeException(nameof(maxOccurs));default:if(minOccurs==maxOccurs){var l=new List<CharFA<TAccept>>();
l.Add(expr); for(int i=1;i<minOccurs;++i){var e=expr.Clone(); l.Add(e);}result=Concat(l,accept); return result;}result=Concat(new CharFA<TAccept>[]{Repeat(expr.Clone(),
minOccurs,minOccurs,accept),Repeat(Optional(expr.Clone()),maxOccurs-minOccurs,maxOccurs-minOccurs,accept)},accept); return result;}} throw new NotImplementedException();
}/// <summary>
/// Creates a new FA that matches the specified FA expression or empty
/// </summary>
/// <param name="expr">The expression to make optional</param>
/// <param name="accept">The symbol to accept</param>
/// <returns>A new FA that will match the specified expression or empty</returns>
public static CharFA<TAccept>Optional(CharFA<TAccept>expr,TAccept accept=default(TAccept)){var result=expr.Clone();var f=result.FirstAcceptingState;f.AcceptSymbol
=accept;result.EpsilonTransitions.Add(f);return result;}/// <summary>
/// Makes the specified expression case insensitive
/// </summary>
/// <param name="expr">The target expression</param>
/// <param name="accept">The accept symbol</param>
/// <returns>A new expression that is the case insensitive equivelent of <paramref name="expr"/></returns>
public static CharFA<TAccept>CaseInsensitive(CharFA<TAccept>expr,TAccept accept=default(TAccept)){var fa=expr.Clone();var closure=fa.FillClosure();for(int
 ic=closure.Count,i=0;i<ic;++i){var ffa=closure[i];if(ffa.IsAccepting)ffa.AcceptSymbol=accept;foreach(var trns in ffa.InputTransitions as IDictionary<CharFA<TAccept>,
ICollection<char>>){foreach(var ch in new List<char>(trns.Value)){if(char.IsLower(ch)){var cch=char.ToUpperInvariant(ch);if(!trns.Value.Contains(cch))
ffa.InputTransitions.Add(cch,trns.Key);}else if(char.IsUpper(ch)){var cch=char.ToLowerInvariant(ch);if(!trns.Value.Contains(cch))ffa.InputTransitions.Add(cch,
trns.Key);}}}}return fa;}}}namespace RE{partial class CharFA<TAccept>{static IDictionary<string,IList<CharRange>>_unicodeCategories=_GetUnicodeCategories();
/// <summary>
/// Retrieves a dictionary indicating the unicode categores supported by this library
/// </summary>
public static IDictionary<string,IList<CharRange>>UnicodeCategories=>_unicodeCategories; static IDictionary<string,IList<CharRange>>_GetUnicodeCategories()
{var working=new Dictionary<string,List<char>>(StringComparer.InvariantCultureIgnoreCase);for(var i=0;i<char.MaxValue;++i){char ch=unchecked((char)i);
var uc=char.GetUnicodeCategory(ch);switch(uc){case UnicodeCategory.ClosePunctuation:_AddTo(working,"Pe",ch);_AddTo(working,"P",ch);break;case UnicodeCategory.ConnectorPunctuation:
_AddTo(working,"Pc",ch);_AddTo(working,"P",ch);break;case UnicodeCategory.Control:_AddTo(working,"Cc",ch);_AddTo(working,"C",ch);break;case UnicodeCategory.CurrencySymbol:
_AddTo(working,"Sc",ch);_AddTo(working,"S",ch);break;case UnicodeCategory.DashPunctuation:_AddTo(working,"Pd",ch);_AddTo(working,"P",ch);break;case UnicodeCategory.DecimalDigitNumber:
_AddTo(working,"Nd",ch);_AddTo(working,"N",ch);break;case UnicodeCategory.EnclosingMark:_AddTo(working,"Me",ch);_AddTo(working,"M",ch);break;case UnicodeCategory.FinalQuotePunctuation:
_AddTo(working,"Pf",ch);_AddTo(working,"P",ch);break;case UnicodeCategory.Format:_AddTo(working,"Cf",ch);_AddTo(working,"C",ch);break;case UnicodeCategory.InitialQuotePunctuation:
_AddTo(working,"Pi",ch);_AddTo(working,"P",ch);break;case UnicodeCategory.LetterNumber:_AddTo(working,"Nl",ch);_AddTo(working,"N",ch);break;case UnicodeCategory.LineSeparator:
_AddTo(working,"Zl",ch);_AddTo(working,"Z",ch);break;case UnicodeCategory.LowercaseLetter:_AddTo(working,"Ll",ch);_AddTo(working,"L",ch);break;case UnicodeCategory.MathSymbol:
_AddTo(working,"Sm",ch);_AddTo(working,"S",ch);break;case UnicodeCategory.ModifierLetter:_AddTo(working,"Lm",ch);_AddTo(working,"L",ch);break;case UnicodeCategory.ModifierSymbol:
_AddTo(working,"Sk",ch);_AddTo(working,"S",ch);break;case UnicodeCategory.NonSpacingMark:_AddTo(working,"Mn",ch);_AddTo(working,"M",ch);break;case UnicodeCategory.OpenPunctuation:
_AddTo(working,"Ps",ch);_AddTo(working,"P",ch);break;case UnicodeCategory.OtherLetter:_AddTo(working,"Lo",ch);_AddTo(working,"L",ch);break;case UnicodeCategory.OtherNotAssigned:
_AddTo(working,"Cn",ch);_AddTo(working,"C",ch);break;case UnicodeCategory.OtherNumber:_AddTo(working,"No",ch);_AddTo(working,"N",ch);break;case UnicodeCategory.OtherPunctuation:
_AddTo(working,"Po",ch);_AddTo(working,"P",ch);break;case UnicodeCategory.OtherSymbol:_AddTo(working,"So",ch);_AddTo(working,"S",ch);break;case UnicodeCategory.ParagraphSeparator:
_AddTo(working,"Zp",ch);_AddTo(working,"Z",ch);break;case UnicodeCategory.PrivateUse:_AddTo(working,"Co",ch);_AddTo(working,"Co",ch);break;case UnicodeCategory.SpaceSeparator:
_AddTo(working,"Zs",ch);_AddTo(working,"Z",ch);break;case UnicodeCategory.SpacingCombiningMark:_AddTo(working,"Mc",ch);_AddTo(working,"M",ch);break;case
 UnicodeCategory.Surrogate:_AddTo(working,"Cs",ch);_AddTo(working,"C",ch);break;case UnicodeCategory.TitlecaseLetter:_AddTo(working,"Lt",ch);_AddTo(working,
"L",ch);break;case UnicodeCategory.UppercaseLetter:_AddTo(working,"Lu",ch);_AddTo(working,"L",ch);break;}}var result=new Dictionary<string,IList<CharRange>>();
foreach(var kvp in working){kvp.Value.Sort();result.Add(kvp.Key,new List<CharRange>(CharRange.GetRanges(kvp.Value)));}return result;}static void _AddTo(IDictionary<string,List<char>>
working,string uc,char ch){List<char>s;if(!working.TryGetValue(uc,out s)){s=new List<char>();working.Add(uc,s);}if(!s.Contains(ch))s.Add(ch);}}}namespace
 RE{/// <summary>
/// Represents a regular expression match
/// </summary>
/// <remarks>Returned from the Match() and MatchDfa() methods</remarks>
#if REGEXLIB
public
#endif
sealed class CharFAMatch{/// <summary>
/// Indicates the 1 based line where the match was found
/// </summary>
public int Line{get;}/// <summary>
/// Indicates the 1 based column where the match was found
/// </summary>
public int Column{get;}/// <summary>
/// Indicates the 0 based position where the match was found
/// </summary>
public long Position{get;}/// <summary>
/// Indicates the value of the match
/// </summary>
public string Value{get;}/// <summary>
/// Creates a new instance with the specified values
/// </summary>
/// <param name="line">The 1 based line where the match occured</param>
/// <param name="column">The 1 based columns where the match occured</param>
/// <param name="position">The 0 based position where the match occured</param>
/// <param name="value">The value of the match</param>
public CharFAMatch(int line,int column,long position,string value){Line=line;Column=column;Position=position;Value=value;}}}namespace RE{/// <summary>
/// Represents the current status of the operation
/// </summary>
#if REGEXLIB
public
#endif
enum CharFAStatus{/// <summary>
/// The status is unknown
/// </summary>
Unknown,/// <summary>
/// Performing a DFA transform
/// </summary>
DfaTransform,/// <summary>
/// Trimming duplicate states
/// </summary>
TrimDuplicates}/// <summary>
/// Represents the progress of the operation
/// </summary>
#if REGEXLIB
public
#endif
struct CharFAProgress{/// <summary>
/// Constructs a new instance of the progress class with the specified status and count
/// </summary>
/// <param name="status">The status</param>
/// <param name="count">The count of values in the progress</param>
public CharFAProgress(CharFAStatus status,int count){Status=status;Count=count;}/// <summary>
/// The status
/// </summary>
public CharFAStatus Status{get;}/// <summary>
/// The count of values in the progress.
/// </summary>
public int Count{get;}}}namespace RE{/// <summary>
/// Represents a dictionary over a <see cref="IList{T}"/>. Allows null for a key and is explicitely ordered, but unindexed. All searches are linear time.
/// </summary>
/// <remarks>Best only to use this for small dictionaries or where indexing by key is infrequent.</remarks>
/// <typeparam name="TKey">The key type.</typeparam>
/// <typeparam name="TValue">The value type.</typeparam>
class ListDictionary<TKey,TValue>:IDictionary<TKey,TValue>,IList<KeyValuePair<TKey,TValue>>{IEqualityComparer<TKey>_equalityComparer;IList<KeyValuePair<TKey,
TValue>>_inner;public ListDictionary(IList<KeyValuePair<TKey,TValue>>inner=null,IEqualityComparer<TKey>equalityComparer=null){if(null==inner)inner=new
 List<KeyValuePair<TKey,TValue>>();_inner=inner;_equalityComparer=equalityComparer;}public TValue this[TKey key]{get{int i=IndexOfKey(key);if(0>i)throw
 new KeyNotFoundException();return _inner[i].Value;}set{int i=IndexOfKey(key);if(0>i)_inner.Add(new KeyValuePair<TKey,TValue>(key,value));else _inner[i]
=new KeyValuePair<TKey,TValue>(key,value);}}public TKey GetKeyAt(int index)=>_inner[index].Key;public TValue GetAt(int index)=>_inner[index].Value;public
 void SetAt(int index,TValue value){_inner[index]=new KeyValuePair<TKey,TValue>(_inner[index].Key,value);}public void SetAt(int index,KeyValuePair<TKey,TValue>
item){if(ContainsKey(item.Key))throw new ArgumentException("An item with the specified key already exists in the dictionary.",nameof(item));_inner[index]
=item;}public void SetKeyAt(int index,TKey key){if(ContainsKey(key)&&index!=IndexOfKey(key))throw new ArgumentException("An item with the specified key already exists in the dictionary.");
_inner[index]=new KeyValuePair<TKey,TValue>(key,_inner[index].Value);}public void Insert(int index,TKey key,TValue value){if(ContainsKey(key))throw new
 ArgumentException("The key already exists in the dictionary.");_inner.Insert(index,new KeyValuePair<TKey,TValue>(key,value));}public ICollection<TKey>
Keys{get=>new _KeysCollection(_inner,_equalityComparer);}public ICollection<TValue>Values{get=>new _ValuesCollection(_inner);}public int Count{get=>_inner.Count;
}public bool IsReadOnly{get=>_inner.IsReadOnly;}KeyValuePair<TKey,TValue>IList<KeyValuePair<TKey,TValue>>.this[int index]{get=>_inner[index];set{var i
=IndexOfKey(value.Key);if(0>i||i==index){_inner[index]=value;}else throw new InvalidOperationException("An item with the specified key already exists in the collection.");
}}public void Add(TKey key,TValue value)=>Add(new KeyValuePair<TKey,TValue>(key,value));public void Add(KeyValuePair<TKey,TValue>item){if(ContainsKey(item.Key))
throw new InvalidOperationException("An item with the specified key already exists in the collection.");_inner.Add(item);}public void Clear()=>_inner.Clear();
public bool Contains(KeyValuePair<TKey,TValue>item)=>_inner.Contains(item);public bool ContainsKey(TKey key)=>-1<IndexOfKey(key);public void CopyTo(KeyValuePair<TKey,
TValue>[]array,int arrayIndex)=>_inner.CopyTo(array,arrayIndex);public IEnumerator<KeyValuePair<TKey,TValue>>GetEnumerator()=>_inner.GetEnumerator();public
 bool Remove(TKey key){var i=IndexOfKey(key);if(0>i)return false;_inner.RemoveAt(i);return true;}public int IndexOfKey(TKey key){var c=_inner.Count;if
(null==_equalityComparer){for(var i=0;i<c;++i)if(Equals(_inner[i].Key,key))return i;}else for(var i=0;i<c;++i)if(_equalityComparer.Equals(_inner[i].Key,
key))return i;return-1;}public bool Remove(KeyValuePair<TKey,TValue>item)=>_inner.Remove(item);public bool TryGetValue(TKey key,out TValue value){var c
=_inner.Count;if(null==_equalityComparer)for(var i=0;i<c;++i){var kvp=_inner[i];if(Equals(kvp.Key,key)){value=kvp.Value;return true;}}else for(var i=0;
i<c;++i){var kvp=_inner[i];if(_equalityComparer.Equals(kvp.Key,key)){value=kvp.Value;return true;}}value=default(TValue);return false;}IEnumerator IEnumerable.GetEnumerator()
=>GetEnumerator();public int IndexOf(KeyValuePair<TKey,TValue>item){return _inner.IndexOf(item);}void IList<KeyValuePair<TKey,TValue>>.Insert(int index,
KeyValuePair<TKey,TValue>item){if(0>IndexOfKey(item.Key))_inner.Insert(index,item);else throw new InvalidOperationException("An item with the specified key already exists in the collection.");
}public void RemoveAt(int index){_inner.RemoveAt(index);}
#region _KeysCollection
sealed class _KeysCollection:ICollection<TKey>{IEqualityComparer<TKey>_equalityComparer;IList<KeyValuePair<TKey,TValue>>_inner;public _KeysCollection(IList<KeyValuePair<TKey,
TValue>>inner,IEqualityComparer<TKey>equalityComparer){_inner=inner;_equalityComparer=equalityComparer;}public int Count{get=>_inner.Count;}public bool
 IsReadOnly{get=>true;}void ICollection<TKey>.Add(TKey item){throw new InvalidOperationException("The collection is read only.");}void ICollection<TKey>.Clear()
{throw new InvalidOperationException("The collection is read only.");}public bool Contains(TKey item){var c=_inner.Count;if(null==_equalityComparer){for
(var i=0;i<c;++i)if(Equals(_inner[i].Key,item))return true;}else for(var i=0;i<c;++i)if(_equalityComparer.Equals(_inner[i].Key,item))return true;return
 false;}public void CopyTo(TKey[]array,int arrayIndex){var c=_inner.Count;if(c>(array.Length-arrayIndex))throw new ArgumentOutOfRangeException("arrayIndex");
for(var i=0;i<c;++i)array[i+arrayIndex]=_inner[i].Key;}public IEnumerator<TKey>GetEnumerator(){foreach(var kvp in _inner)yield return kvp.Key;}bool ICollection<TKey>.Remove(TKey
 item){throw new InvalidOperationException("The collection is read only.");}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}
#endregion
#region _ValuesCollection
sealed class _ValuesCollection:ICollection<TValue>{IList<KeyValuePair<TKey,TValue>>_inner;public _ValuesCollection(IList<KeyValuePair<TKey,TValue>>inner)
{_inner=inner;}public int Count{get=>_inner.Count;}public bool IsReadOnly{get=>true;}void ICollection<TValue>.Add(TValue item){throw new InvalidOperationException("The collection is read only.");
}void ICollection<TValue>.Clear(){throw new InvalidOperationException("The collection is read only.");}public bool Contains(TValue item){var c=_inner.Count;
for(var i=0;i<c;++i)if(Equals(_inner[i].Value,item))return true;return false;}public void CopyTo(TValue[]array,int arrayIndex){var c=_inner.Count;if(c
>(array.Length-arrayIndex))throw new ArgumentOutOfRangeException("arrayIndex");for(var i=0;i<c;++i)array[i+arrayIndex]=_inner[i].Value;}public IEnumerator<TValue>
GetEnumerator(){foreach(var kvp in _inner)yield return kvp.Value;}bool ICollection<TValue>.Remove(TValue item){throw new InvalidOperationException("The collection is read only.");
}IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();}
#endregion
}}namespace RE{partial class ParseContext{/// <summary>
/// Indicates if the character is hex
/// </summary>
/// <param name="hex">The character to examine</param>
/// <returns>True if the character is a valid hex character, otherwise false</returns>
internal static bool IsHexChar(char hex){return(':'>hex&&'/'<hex)||('G'>hex&&'@'<hex)||('g'>hex&&'`'<hex);}/// <summary>
/// Converts a hex character to its byte representation
/// </summary>
/// <param name="hex">The character to convert</param>
/// <returns>The byte that the character represents</returns>
internal static byte FromHexChar(char hex){if(':'>hex&&'/'<hex)return(byte)(hex-'0');if('G'>hex&&'@'<hex)return(byte)(hex-'7'); if('g'>hex&&'`'<hex)return
(byte)(hex-'W'); throw new ArgumentException("The value was not hex.","hex");}/// <summary>
/// Attempts to read a generic integer into the capture buffer
/// </summary>
/// <returns>True if a valid integer was read, otherwise false</returns>
public bool TryReadInteger(){EnsureStarted();bool neg=false;if('-'==Current){neg=true;CaptureCurrent();Advance();}else if('0'==Current){CaptureCurrent();
Advance();if(-1==Current)return true;return!char.IsDigit((char)Current);}if(-1==Current||(neg&&'0'==Current)||!char.IsDigit((char)Current))return false;
if(!TryReadDigits())return false;return true;}/// <summary>
/// Attempts to skip a generic integer without capturing
/// </summary>
/// <returns>True if an integer was found and skipped, otherwise false</returns>
public bool TrySkipInteger(){EnsureStarted();bool neg=false;if('-'==Current){neg=true;Advance();}else if('0'==Current){Advance();if(-1==Current)return
 true;return!char.IsDigit((char)Current);}if(-1==Current||(neg&&'0'==Current)||!char.IsDigit((char)Current))return false;if(!TrySkipDigits())return false;
return true;}/// <summary>
/// Attempts to read a C# integer into the capture buffer while parsing it
/// </summary>
/// <param name="result">The value the literal represents</param>
/// <returns>True if the value was a valid literal, otherwise false</returns>
public bool TryParseInteger(out object result){result=null;EnsureStarted();if(-1==Current)return false;bool neg=false;if('-'==Current){CaptureCurrent();
Advance();neg=true;}int l=CaptureBuffer.Length;if(TryReadDigits()){string num=CaptureBuffer.ToString(l,CaptureBuffer.Length-l);if(neg)num='-'+num;int r;
if(int.TryParse(num,out r)){result=r;return true;}long ll;if(long.TryParse(num,out ll)){result=ll;return true;}}return false;}/// <summary>
/// Reads a C# integer literal into the capture buffer while parsing it
/// </summary>
/// <returns>The value the literal represents</returns>
/// <exception cref="ExpectingException">The input was not valid</exception>
public object ParseInteger(){EnsureStarted();Expecting('-','0','1','2','3','4','5','6','7','8','9');bool neg=('-'==Current);if(neg){Advance();Expecting('0',
'1','2','3','4','5','6','7','8','9');}long i=0;if(!neg){i+=((char)Current)-'0';while(-1!=Advance()&&char.IsDigit((char)Current)){i*=10;i+=((char)Current)
-'0';}}else{i-=((char)Current)-'0';while(-1!=Advance()&&char.IsDigit((char)Current)){i*=10;i-=((char)Current)-'0';}}if(i<=int.MaxValue&&i>=int.MinValue)
return(int)i;return i;}/// <summary>
/// Attempts to read a generic floating point number into the capture buffer
/// </summary>
/// <returns>True if a valid floating point number was read, otherwise false</returns>
public bool TryReadReal(){EnsureStarted();bool readAny=false;if('-'==Current){CaptureCurrent();Advance();}if(char.IsDigit((char)Current)){if(!TryReadDigits())
return false;readAny=true;}if('.'==Current){CaptureCurrent();Advance();if(!TryReadDigits())return false;readAny=true;}if('E'==Current||'e'==Current){CaptureCurrent();
Advance();if('-'==Current||'+'==Current){CaptureCurrent();Advance();}return TryReadDigits();}return readAny;}/// <summary>
/// Attempts to skip a generic floating point literal without capturing
/// </summary>
/// <returns>True if a literal was found and skipped, otherwise false</returns>
public bool TrySkipReal(){bool readAny=false;EnsureStarted();if('-'==Current)Advance();if(char.IsDigit((char)Current)){if(!TrySkipDigits())return false;
readAny=true;}if('.'==Current){Advance();if(!TrySkipDigits())return false;readAny=true;}if('E'==Current||'e'==Current){Advance();if('-'==Current||'+'==
Current)Advance();return TrySkipDigits();}return readAny;}/// <summary>
/// Attempts to read a floating point literal into the capture buffer while parsing it
/// </summary>
/// <param name="result">The value the literal represents</param>
/// <returns>True if the value was a valid literal, otherwise false</returns>
public bool TryParseReal(out double result){result=default(double);int l=CaptureBuffer.Length;if(!TryReadReal())return false;return double.TryParse(CaptureBuffer.ToString(l,
CaptureBuffer.Length-l),out result);}/// <summary>
/// Reads a floating point literal into the capture buffer while parsing it
/// </summary>
/// <returns>The value the literal represents</returns>
/// <exception cref="ExpectingException">The input was not valid</exception>
public double ParseReal(){EnsureStarted();var sb=new StringBuilder();Expecting('-','.','0','1','2','3','4','5','6','7','8','9');bool neg=('-'==Current);
if(neg){sb.Append((char)Current);Advance();Expecting('.','0','1','2','3','4','5','6','7','8','9');}while(-1!=Current&&char.IsDigit((char)Current)){sb.Append((char)Current);
Advance();}if('.'==Current){sb.Append((char)Current);Advance();Expecting('0','1','2','3','4','5','6','7','8','9');sb.Append((char)Current);while(-1!=Advance()
&&char.IsDigit((char)Current)){sb.Append((char)Current);}}if('E'==Current||'e'==Current){sb.Append((char)Current);Advance();Expecting('+','-','0','1',
'2','3','4','5','6','7','8','9');switch(Current){case'+':case'-':sb.Append((char)Current);Advance();break;}Expecting('0','1','2','3','4','5','6','7','8',
'9');sb.Append((char)Current);while(-1!=Advance()){Expecting('0','1','2','3','4','5','6','7','8','9');sb.Append((char)Current);}}return double.Parse(sb.ToString());
}/// <summary>
/// Attempts to read the specified literal from the input, optionally checking if it is terminated
/// </summary>
/// <param name="literal">The literal to attempt to read</param>
/// <param name="checkTerminated">If true, will check the end to make sure it's not a letter or digit</param>
/// <returns></returns>
public bool TryReadLiteral(string literal,bool checkTerminated=true){foreach(char ch in literal){if(Current==ch){CaptureCurrent();if(-1==Advance())break;
}}if(checkTerminated)return-1==Current||!char.IsLetterOrDigit((char)Current);return true;}/// <summary>
/// Attempts to skip the specified literal without capturing, optionally checking for termination
/// </summary>
/// <param name="literal">The literal to skip</param>
/// <param name="checkTerminated">True if the literal should be checked for termination by a charcter other than a letter or digit, otherwise false</param>
/// <returns>True if the literal was found and skipped, otherwise false</returns>
public bool TrySkipLiteral(string literal,bool checkTerminated=true){foreach(char ch in literal){if(Current==ch){if(-1==Advance())break;}}if(checkTerminated)
return-1==Current||!char.IsLetterOrDigit((char)Current);return true;}/// <summary>
/// Attempts to read a C style line comment into the capture buffer
/// </summary>
/// <returns>True if a valid comment was read, otherwise false</returns>
public bool TryReadCLineComment(){EnsureStarted();if('/'!=Current)return false;CaptureCurrent();if('/'!=Advance())return false;CaptureCurrent();while(-1
!=Advance()&&'\r'!=Current&&'\n'!=Current)CaptureCurrent();return true;}/// <summary>
/// Attempts to skip the a C style line comment without capturing
/// </summary>
/// <returns>True if a comment was found and skipped, otherwise false</returns>
public bool TrySkipCLineComment(){EnsureStarted();if('/'!=Current)return false;if('/'!=Advance())return false;while(-1!=Advance()&&'\r'!=Current&&'\n'
!=Current);return true;}/// <summary>
/// Attempts to read a C style block comment into the capture buffer
/// </summary>
/// <returns>True if a valid comment was read, otherwise false</returns>
public bool TryReadCBlockComment(){EnsureStarted();if('/'!=Current)return false;CaptureCurrent();if('*'!=Advance())return false;CaptureCurrent();if(-1
==Advance())return false;return TryReadUntil("*/");}/// <summary>
/// Attempts to skip the C style block comment without capturing
/// </summary>
/// <returns>True if a comment was found and skipped, otherwise false</returns>
public bool TrySkipCBlockComment(){EnsureStarted();if('/'!=Current)return false;if('*'!=Advance())return false;if(-1==Advance())return false;return TrySkipUntil("*/");
}/// <summary>
/// Attempts to read a C style comment into the capture buffer
/// </summary>
/// <returns>True if a valid comment was read, otherwise false</returns>
public bool TryReadCComment(){EnsureStarted();if('/'!=Current)return false;CaptureCurrent();if('*'==Advance()){CaptureCurrent();if(-1==Advance())return
 false;return TryReadUntil("*/");}if('/'==Current){CaptureCurrent();while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current)CaptureCurrent();return true;}return
 false;}/// <summary>
/// Attempts to skip the a C style comment value without capturing
/// </summary>
/// <returns>True if a comment was found and skipped, otherwise false</returns>
public bool TrySkipCComment(){EnsureStarted();if('/'!=Current)return false;if('*'==Advance()){if(-1==Advance())return false;return TrySkipUntil("*/");
}if('/'==Current){while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current);return true;}return false;}/// <summary>
/// Attempts to read C style comments or whitespace into the capture buffer
/// </summary>
/// <returns>True if a valid comment or whitespace was read, otherwise false</returns>
public bool TryReadCCommentsAndWhiteSpace(){bool result=false;while(-1!=Current){if(!TryReadWhiteSpace()&&!TryReadCComment())break;result=true;}if(TryReadWhiteSpace())
result=true;return result;}/// <summary>
/// Attempts to skip the a C style comment or whitespace value without capturing
/// </summary>
/// <returns>True if a comment or whitespace was found and skipped, otherwise false</returns>
public bool TrySkipCCommentsAndWhiteSpace(){bool result=false;while(-1!=Current){if(!TrySkipWhiteSpace()&&!TrySkipCComment())break;result=true;}if(TrySkipWhiteSpace())
result=true;return result;}/// <summary>
/// Attempts to read a C style identifier into the capture buffer
/// </summary>
/// <returns>True if a valid identifier was read, otherwise false</returns>
public bool TryReadCIdentifier(){EnsureStarted();if(-1==Current||!('_'==Current||char.IsLetter((char)Current)))return false;CaptureCurrent();while(-1!=
Advance()&&('_'==Current||char.IsLetterOrDigit((char)Current)))CaptureCurrent();return true;}/// <summary>
/// Attempts to skip the a C style identifier value without capturing
/// </summary>
/// <returns>True if an identifier was found and skipped, otherwise false</returns>
public bool TrySkipCIdentifier(){EnsureStarted();if(-1==Current||!('_'==Current||char.IsLetter((char)Current)))return false;while(-1!=Advance()&&('_'==
Current||char.IsLetterOrDigit((char)Current)));return true;}/// <summary>
/// Attempts to read a C style string into the capture buffer
/// </summary>
/// <returns>True if a valid string was read, otherwise false</returns>
public bool TryReadCString(){EnsureStarted();if('\"'!=Current)return false;CaptureCurrent();while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current&&'\"'!=Current)
{CaptureCurrent();if('\\'==Current){if(-1==Advance()||'\r'==Current||'\n'==Current)return false;CaptureCurrent();}}if('\"'==Current){CaptureCurrent();
Advance(); return true;}return false;}/// <summary>
/// Attempts to skip a C style string literal without capturing
/// </summary>
/// <returns>True if a literal was found and skipped, otherwise false</returns>
public bool TrySkipCString(){EnsureStarted();if('\"'!=Current)return false;while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current&&'\"'!=Current)if('\\'==Current)
if(-1==Advance()||'\r'==Current||'\n'==Current)return false;if('\"'==Current){Advance(); return true;}return false;}}}namespace RE{
#region ExpectingException
/// <summary>
/// An exception encountered during parsing where the stream contains one thing, but another is expected
/// </summary>
[Serializable]
#if REGEXLIB
public
#endif
sealed class ExpectingException:Exception{/// <summary>
/// Creates the exception with the specified message.
/// </summary>
/// <param name="message">The message</param>
public ExpectingException(string message):base(message){}/// <summary>
/// Creates the exception with the specified arguments
/// </summary>
/// <param name="message">The message</param>
/// <param name="line">The line</param>
/// <param name="column">The column</param>
/// <param name="position">The position</param>
/// <param name="expecting">What was expected</param>
public ExpectingException(string message,int line,int column,long position,params string[]expecting):base(message){Line=line;Column=column;Position=position;
Expecting=expecting;}/// <summary>
/// The list of expected strings.
/// </summary>
public string[]Expecting{get;internal set;}/// <summary>
/// The position when the error was realized.
/// </summary>
public long Position{get;internal set;}/// <summary>
/// The line of the error
/// </summary>
public int Line{get;internal set;}/// <summary>
/// The column of the error
/// </summary>
public int Column{get;internal set;}}
#endregion ExpectingException
/// <summary>
/// see https://www.codeproject.com/Articles/5162847/ParseContext-2-0-Easier-Hand-Rolled-Parsers
/// </summary>
#if REGEXLIB
public
#endif
partial class ParseContext:IDisposable{/// <summary>
/// Attempts to read whitespace from the current input, capturing it
/// </summary>
/// <returns>True if whitespace was read, otherwise false</returns>
public bool TryReadWhiteSpace(){EnsureStarted();if(-1==Current||!char.IsWhiteSpace((char)Current))return false;CaptureCurrent();while(-1!=Advance()&&char.IsWhiteSpace((char)Current))
CaptureCurrent();return true;}/// <summary>
/// Attempts to skip whitespace in the current input without capturing it
/// </summary>
/// <returns>True if whitespace was skipped, otherwise false</returns>
public bool TrySkipWhiteSpace(){EnsureStarted();if(-1==Current||!char.IsWhiteSpace((char)Current))return false;while(-1!=Advance()&&char.IsWhiteSpace((char)Current))
;return true;}/// <summary>
/// Attempts to read up until the specified character, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TryReadUntil(int character,bool readCharacter=true){EnsureStarted();if(0>character)character=-1;CaptureCurrent();if(Current==character){return
 true;}while(-1!=Advance()&&Current!=character)CaptureCurrent(); if(Current==character){if(readCharacter){CaptureCurrent();Advance();}return true;}return
 false;}/// <summary>
/// Attempts to skip up until the specified character, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TrySkipUntil(int character,bool skipCharacter=true){EnsureStarted();if(0>character)character=-1;if(Current==character)return true;while(-1
!=Advance()&&Current!=character);if(Current==character){if(skipCharacter)Advance();return true;}return false;}/// <summary>
/// Attempts to read up until the specified character, using the specified escape, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="escapeChar">The escape indicator character to use</param>
/// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TryReadUntil(int character,int escapeChar,bool readCharacter=true){EnsureStarted();if(0>character)character=-1;if(-1==Current)return false;
if(Current==character){if(readCharacter){CaptureCurrent();Advance();}return true;}do{if(escapeChar==Current){CaptureCurrent();if(-1==Advance())return false;
CaptureCurrent();}else{if(character==Current){if(readCharacter){CaptureCurrent();Advance();}return true;}else CaptureCurrent();}}while(-1!=Advance());
return false;}/// <summary>
/// Attempts to skip up until the specified character, using the specified escape, optionally consuming it
/// </summary>
/// <param name="character">The character to halt at</param>
/// <param name="escapeChar">The escape indicator character to use</param>
/// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
/// <returns>True if the character was found, otherwise false</returns>
public bool TrySkipUntil(int character,int escapeChar,bool skipCharacter=true){EnsureStarted();if(0>character)character=-1;if(Current==character)return
 true;while(-1!=Advance()&&Current!=character){if(character==escapeChar)if(-1==Advance())break;}if(Current==character){if(skipCharacter)Advance();return
 true;}return false;}private static bool _ContainsChar(char[]chars,char ch){foreach(char cmp in chars)if(cmp==ch)return true;return false;}/// <summary>
/// Attempts to read until any of the specified characters, optionally consuming it
/// </summary>
/// <param name="readCharacter">True if the character should be consumed, otherwise false</param>
/// <param name="anyOf">A list of characters that signal the end of the scan</param>
/// <returns>True if one of the characters was found, otherwise false</returns>
public bool TryReadUntil(bool readCharacter=true,params char[]anyOf){EnsureStarted();if(null==anyOf)anyOf=Array.Empty<char>();CaptureCurrent();if(-1!=
Current&&_ContainsChar(anyOf,(char)Current)){if(readCharacter){CaptureCurrent();Advance();}return true;}while(-1!=Advance()&&!_ContainsChar(anyOf,(char)Current))
CaptureCurrent();if(-1!=Current&&_ContainsChar(anyOf,(char)Current)){if(readCharacter){CaptureCurrent();Advance();}return true;}return false;}/// <summary>
/// Attempts to skip until any of the specified characters, optionally consuming it
/// </summary>
/// <param name="skipCharacter">True if the character should be consumed, otherwise false</param>
/// <param name="anyOf">A list of characters that signal the end of the scan</param>
/// <returns>True if one of the characters was found, otherwise false</returns>
public bool TrySkipUntil(bool skipCharacter=true,params char[]anyOf){EnsureStarted();if(null==anyOf)anyOf=Array.Empty<char>();if(-1!=Current&&_ContainsChar(anyOf,
(char)Current)){if(skipCharacter)Advance();return true;}while(-1!=Advance()&&!_ContainsChar(anyOf,(char)Current));if(-1!=Current&&_ContainsChar(anyOf,
(char)Current)){if(skipCharacter)Advance();return true;}return false;}/// <summary>
/// Reads up to the specified text string, consuming it
/// </summary>
/// <param name="text">The text to read until</param>
/// <returns>True if the text was found, otherwise false</returns>
public bool TryReadUntil(string text){EnsureStarted();if(string.IsNullOrEmpty(text))return false;while(-1!=Current&&TryReadUntil(text[0],false)){bool found
=true;for(int i=1;i<text.Length;++i){if(Advance()!=text[i]){found=false;break;}CaptureCurrent();}if(found){Advance();return true;}}return false;}/// <summary>
/// Skips up to the specified text string, consuming it
/// </summary>
/// <param name="text">The text to skip until</param>
/// <returns>True if the text was found, otherwise false</returns>
public bool TrySkipUntil(string text){EnsureStarted();if(string.IsNullOrEmpty(text))return false;while(-1!=Current&&TrySkipUntil(text[0],false)){bool found
=true;for(int i=1;i<text.Length;++i){if(Advance()!=text[i]){found=false;break;}}if(found){Advance();return true;}}return false;}/// <summary>
/// Attempts to read a series of digits, consuming them
/// </summary>
/// <returns>True if digits were consumed, otherwise false</returns>
public bool TryReadDigits(){EnsureStarted();if(-1==Current||!char.IsDigit((char)Current))return false;CaptureCurrent();while(-1!=Advance()&&char.IsDigit((char)Current))
CaptureCurrent();return true;}/// <summary>
/// Attempts to skip a series of digits, consuming them
/// </summary>
/// <returns>True if digits were consumed, otherwise false</returns>
public bool TrySkipDigits(){EnsureStarted();if(-1==Current||!char.IsDigit((char)Current))return false;while(-1!=Advance()&&char.IsDigit((char)Current))
;return true;}/// <summary>
/// Attempts to read a series of letters, consuming them
/// </summary>
/// <returns>True if letters were consumed, otherwise false</returns>
public bool TryReadLetters(){EnsureStarted();if(-1==Current||!char.IsLetter((char)Current))return false;CaptureCurrent();while(-1!=Advance()&&char.IsLetter((char)Current))
CaptureCurrent();return true;}/// <summary>
/// Attempts to skip a series of letters, consuming them
/// </summary>
/// <returns>True if letters were consumed, otherwise false</returns>
public bool TrySkipLetters(){EnsureStarted();if(-1==Current||!char.IsLetter((char)Current))return false;while(-1!=Advance()&&char.IsLetter((char)Current))
;return true;}/// <summary>
/// Attempts to read a series of letters or digits, consuming them
/// </summary>
/// <returns>True if letters or digits were consumed, otherwise false</returns>
public bool TryReadLettersOrDigits(){EnsureStarted();if(-1==Current||!char.IsLetterOrDigit((char)Current))return false;CaptureCurrent();while(-1!=Advance()
&&char.IsLetterOrDigit((char)Current))CaptureCurrent();return true;}/// <summary>
/// Attempts to skip a series of letters or digits, consuming them
/// </summary>
/// <returns>True if letters or digits were consumed, otherwise false</returns>
public bool TrySkipLettersOrDigits(){EnsureStarted();if(-1==Current||!char.IsLetterOrDigit((char)Current))return false;while(-1!=Advance()&&char.IsLetterOrDigit((char)Current))
;return true;}ParseContext(IEnumerable<char>inner){_inner=inner.GetEnumerator();}ParseContext(TextReader inner){_inner=new _TextReaderEnumerator(inner);
}Queue<char>_input=new Queue<char>();IEnumerator<char>_inner=null;/// <summary>
/// Indicates the capture buffer used to hold gathered input
/// </summary>
public StringBuilder CaptureBuffer{get;}=new StringBuilder();/// <summary>
/// Indicates the 0 based position of the parse context
/// </summary>
public long Position{get;private set;}=-2;/// <summary>
/// Indicates the 1 based column of the parse context
/// </summary>
public int Column{get;private set;}=1;/// <summary>
/// Indicates the 1 based line of the parse context
/// </summary>
public int Line{get;private set;}=1;/// <summary>
/// Indicates the current status, -1 if end of input (like <see cref="TextReader"/>) or -2 if before the beginning.
/// </summary>
public int Current{get;private set;}=-2;/// <summary>
/// Indicates the width of tabs on the output device.
/// </summary>
/// <remarks>Used for tracking column position</remarks>
public int TabWidth{get;set;}=8;bool _EnsureInput(){if(0==_input.Count){if(!_inner.MoveNext())return false;_input.Enqueue(_inner.Current);return true;
}return true;}/// <summary>
/// Ensures that the parse context is started and the input cursor is valid
/// </summary>
public void EnsureStarted(){_CheckDisposed();if(-2==Current)Advance();}/// <summary>
/// Peeks the specified number of characters ahead in the input without advancing
/// </summary>
/// <param name="lookAhead">Indicates the number of characters to look ahead. Zero is the current position.</param>
/// <returns>An integer representing the character at the position, or -1 if past the end of the input.</returns>
public int Peek(int lookAhead=1){_CheckDisposed();if(-2==Current)throw new InvalidOperationException("The parse context has not been started.");if(0>lookAhead)
lookAhead=0;if(!EnsureLookAhead(0!=lookAhead?lookAhead:1))return-1;int i=0;foreach(var result in _input){if(i==lookAhead)return result;++i;}return-1;}
/// <summary>
/// Pre-reads the specified amount of lookahead characters
/// </summary>
/// <param name="lookAhead">The number of lookahead characters to read</param>
/// <returns>True if the entire lookahead request could be satisfied, otherwise false</returns>
public bool EnsureLookAhead(int lookAhead=1){_CheckDisposed();if(1>lookAhead)lookAhead=1;while(_input.Count<lookAhead&&_inner.MoveNext())_input.Enqueue(_inner.Current);
return _input.Count>=lookAhead;}/// <summary>
/// Advances the input cursor by one
/// </summary>
/// <returns>An integer representing the next character</returns>
public int Advance(){_CheckDisposed();if(0!=_input.Count)_input.Dequeue();if(_EnsureInput()){if(-2==Current){Position=-1;Column=0;}Current=_input.Peek();
++Column;++Position;if('\n'==Current){++Line;Column=0;}else if('\r'==Current){Column=0;}else if('\t'==Current&&0<TabWidth){Column=((Column/TabWidth)+1)
*TabWidth;} return Current;}if(-1!=Current){++Position;++Column;}Current=-1;return-1;}/// <summary>
/// Disposes of the parse context and closes any resources used
/// </summary>
public void Dispose(){if(null!=_inner){Current=-3;_inner.Dispose();_inner=null;}}/// <summary>
/// Clears the capture buffer
/// </summary>
public void ClearCapture(){_CheckDisposed();CaptureBuffer.Clear();}/// <summary>
/// Captures the current character if available
/// </summary>
public void CaptureCurrent(){_CheckDisposed();if(-2==Current)throw new InvalidOperationException("The parse context has not been started.");if(-1!=Current)
CaptureBuffer.Append((char)Current);}/// <summary>
/// Gets the capture buffer at the specified start index
/// </summary>
/// <param name="startIndex">The index to begin copying</param>
/// <param name="count">The number of characters to copy</param>
/// <returns>A string representing the specified subset of the capture buffer</returns>
public string GetCapture(int startIndex,int count=0){_CheckDisposed();if(0==count)count=CaptureBuffer.Length-startIndex;return CaptureBuffer.ToString(startIndex,
count);}/// <summary>
/// Gets the capture buffer at the specified start index
/// </summary>
/// <param name="startIndex">The index to begin copying</param>
/// <returns>A string representing the specified subset of the capture buffer</returns>
public string GetCapture(int startIndex=0){_CheckDisposed();return CaptureBuffer.ToString(startIndex,CaptureBuffer.Length-startIndex);}/// <summary>
/// Sets the location information for the parse context
/// </summary>
/// <remarks>This does not move the cursor. It simply updates the position information.</remarks>
/// <param name="line">The 1 based current line</param>
/// <param name="column">The 1 based current column</param>
/// <param name="position">The zero based current position</param>
public void SetLocation(int line,int column,long position){switch(Current){case-3:throw new ObjectDisposedException(GetType().Name);case-2:throw new InvalidOperationException("The cursor is before the start of the stream.");
case-1:throw new InvalidOperationException("The cursor is after the end of the stream.");}Position=position;Line=line;Column=column;}/// <summary>
/// Throws a <see cref="ExpectingException"/> with a set of packed int ranges where the ints are pairs indicating first and last
/// </summary>
/// <param name="expecting">The packed ranges</param>
[DebuggerHidden()]public void ThrowExpectingRanges(int[]expecting){ExpectingException ex=null;ex=new ExpectingException(_GetExpectingMessageRanges(expecting));
ex.Position=Position;ex.Line=Line;ex.Column=Column;ex.Expecting=null;throw ex;}void _CheckDisposed(){if(-3==Current)throw new ObjectDisposedException(GetType().Name);
}string _GetExpectingMessageRanges(int[]expecting){StringBuilder sb=new StringBuilder();sb.Append('[');for(var i=0;i<expecting.Length;i++){var first=expecting[i];
++i;var last=expecting[i];if(first==last){if(-1==first)sb.Append("(end of stream)");else sb.Append((char)first);}else{sb.Append((char)first);sb.Append('-');
sb.Append((char)last);}}sb.Append(']');string at=string.Concat(" at line ",Line,", column ",Column,", position ",Position);if(-1==Current){if(0==expecting.Length)
return string.Concat("Unexpected end of input",at,".");return string.Concat("Unexpected end of input. Expecting ",sb.ToString(),at,".");}if(0==expecting.Length)
return string.Concat("Unexpected character \"",(char)Current,"\" in input",at,".");return string.Concat("Unexpected character \"",(char)Current,"\" in input. Expecting ",
sb.ToString(),at,".");}string _GetExpectingMessage(int[]expecting){StringBuilder sb=null;switch(expecting.Length){case 0:break;case 1:sb=new StringBuilder();
if(-1==expecting[0])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[0]);sb.Append("\"");}break;case 2:sb=new StringBuilder();
if(-1==expecting[0])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[0]);sb.Append("\"");}sb.Append(" or ");if(-1==expecting[1])
sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[1]);sb.Append("\"");}break;default: sb=new StringBuilder();if(-1==expecting[0])
sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[0]);sb.Append("\"");}int l=expecting.Length-1;int i=1;for(;i<l;++i){sb.Append(", ");
if(-1==expecting[i])sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[i]);sb.Append("\"");}}sb.Append(", or ");if(-1==expecting[i])
sb.Append("end of input");else{sb.Append("\"");sb.Append((char)expecting[i]);sb.Append("\"");}break;}string at=string.Concat(" at line ",Line,", column ",
Column,", position ",Position);if(-1==Current){if(0==expecting.Length)return string.Concat("Unexpected end of input",at,".");return string.Concat("Unexpected end of input. Expecting ",
sb.ToString(),at,".");}if(0==expecting.Length)return string.Concat("Unexpected character \"",(char)Current,"\" in input",at,".");return string.Concat("Unexpected character \"",
(char)Current,"\" in input. Expecting ",sb.ToString(),at,".");}/// <summary>
/// Throws an exception indicating the expected characters if the current character is not one of the specified characters
/// </summary>
/// <param name="expecting">The characters to check for, or -1 for end of input. If the characters are empty, any character other than the end of input is accepted.</param>
/// <exception cref="ExpectingException">Raised when the current character doesn't match any of the specified characters</exception>
[DebuggerHidden()]public void Expecting(params int[]expecting){ExpectingException ex=null;switch(expecting.Length){case 0:if(-1==Current)ex=new ExpectingException(_GetExpectingMessage(expecting));
break;case 1:if(expecting[0]!=Current)ex=new ExpectingException(_GetExpectingMessage(expecting));break;default:if(0>Array.IndexOf(expecting,Current))ex
=new ExpectingException(_GetExpectingMessage(expecting));break;}if(null!=ex){ex.Position=Position;ex.Line=Line;ex.Column=Column;ex.Expecting=new string[expecting.Length];
for(int i=0;i<ex.Expecting.Length;i++)ex.Expecting[i]=Convert.ToString(expecting[i]);throw ex;}}/// <summary>
/// Creates a parse context over a string (<see cref="IEnumerable{Char}"/>)
/// </summary>
/// <param name="string">The input string to use</param>
/// <returns>A parse context over the specified input</returns>
public static ParseContext Create(IEnumerable<char>@string){return new ParseContext(@string);}/// <summary>
/// Creates a parse context over a <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The text reader to use</param>
/// <returns>A parse context over the specified input</returns>
public static ParseContext CreateFrom(TextReader reader){return new ParseContext(reader);}/// <summary>
/// Creates a parse context over the specified file
/// </summary>
/// <param name="filename">The filename to use</param>
/// <returns>A parse context over the specified file</returns>
public static ParseContext CreateFrom(string filename){return new ParseContext(File.OpenText(filename));}/// <summary>
/// Creates a parse context over the specified url
/// </summary>
/// <param name="url">The url to use</param>
/// <returns>A parse context over the specified file</returns>
public static ParseContext CreateFromUrl(string url){var wreq=WebRequest.Create(url);var wresp=wreq.GetResponse();return CreateFrom(new StreamReader(wresp.GetResponseStream()));
}class _TextReaderEnumerator:IEnumerator<char>{int _current=-2;TextReader _inner;internal _TextReaderEnumerator(TextReader inner){_inner=inner;}public
 char Current{get{switch(_current){case-1:throw new InvalidOperationException("The enumerator is past the end of the stream.");case-2:throw new InvalidOperationException("The enumerator has not been started.");
}return unchecked((char)_current);}}object IEnumerator.Current=>Current;public void Dispose(){_current=-3;if(null!=_inner){_inner.Dispose();_inner=null;
}}public bool MoveNext(){switch(_current){case-1:return false;case-3:throw new ObjectDisposedException(GetType().Name);}_current=_inner.Read();if(-1==
_current)return false;return true;}public void Reset(){throw new NotImplementedException();}}}}namespace RE{partial class ParseContext{/// <summary>
/// Attempts to read a JSON string into the capture buffer
/// </summary>
/// <returns>True if a valid string was read, otherwise false</returns>
public bool TryReadJsonString(){EnsureStarted();if('\"'!=Current)return false;CaptureCurrent();while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current&&Current
!='\"'){CaptureCurrent();if('\\'==Current){if(-1==Advance()||'\r'==Current||'\n'==Current)return false;CaptureCurrent();}}if(Current=='\"'){CaptureCurrent();
Advance(); return true;}return false;}/// <summary>
/// Attempts to skip a JSON string literal without capturing
/// </summary>
/// <returns>True if a literal was found and skipped, otherwise false</returns>
public bool TrySkipJsonString(){EnsureStarted();if('\"'!=Current)return false;while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current&&Current!='\"')if('\\'
==Current)if(-1==Advance()||'\r'==Current||'\n'==Current)return false;if(Current=='\"'){Advance(); return true;}return false;}/// <summary>
/// Attempts to read a JSON string literal into the capture buffer while parsing it
/// </summary>
/// <param name="result">The value the literal represents</param>
/// <returns>True if the value was a valid literal, otherwise false</returns>
public bool TryParseJsonString(out string result){result=null;var sb=new StringBuilder();EnsureStarted();if('\"'!=Current)return false;CaptureCurrent();
while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current&&Current!='\"'){CaptureCurrent();if('\\'==Current){if(-1==Advance()||'\r'==Current||'\n'==Current)return
 false;CaptureCurrent();switch(Current){case'f':sb.Append('\f');break;case'r':sb.Append('\r');break;case'n':sb.Append('\n');break;case't':sb.Append('\t');
break;case'\\':sb.Append('\\');break;case'/':sb.Append('/');break;case'\"':sb.Append('\"');break;case'b':sb.Append('\b');break;case'u':CaptureCurrent();
if(-1==Advance())return false;int ch=0;if(!IsHexChar((char)Current))return false;ch<<=4;ch|=FromHexChar((char)Current);CaptureCurrent();if(-1==Advance())
return false;if(!IsHexChar((char)Current))return false;ch<<=4;ch|=FromHexChar((char)Current);CaptureCurrent();if(-1==Advance())return false;if(!IsHexChar((char)Current))
return false;ch<<=4;ch|=FromHexChar((char)Current);CaptureCurrent();if(-1==Advance())return false;if(!IsHexChar((char)Current))return false;ch<<=4;ch|=
FromHexChar((char)Current);sb.Append((char)ch);break;default:return false;}}else sb.Append((char)Current);}if(Current=='\"'){CaptureCurrent();Advance();
 result=sb.ToString();return true;}return false;}/// <summary>
/// Reads a JSON string literal into the capture buffer while parsing it
/// </summary>
/// <returns>The value the literal represents</returns>
/// <exception cref="ExpectingException">The input was not valid</exception>
public string ParseJsonString(){var sb=new StringBuilder();EnsureStarted();Expecting('\"');while(-1!=Advance()&&'\r'!=Current&&'\n'!=Current&&Current!=
'\"'){if('\\'==Current){Advance();switch(Current){case'b':sb.Append('\b');break;case'f':sb.Append('\f');break;case'n':sb.Append('\n');break;case'r':sb.Append('\r');
break;case't':sb.Append('\t');break;case'\\':sb.Append('\\');break;case'\"':sb.Append('\"');break;case'u':int ch=0;Advance();Expecting('0','1','2','3',
'4','5','6','7','8','9','A','a','B','b','C','c','D','d','E','e','F','f');ch<<=4;ch|=FromHexChar((char)Current);Advance();Expecting('0','1','2','3','4',
'5','6','7','8','9','A','a','B','b','C','c','D','d','E','e','F','f');ch<<=4;ch|=FromHexChar((char)Current);Advance();Expecting('0','1','2','3','4','5',
'6','7','8','9','A','a','B','b','C','c','D','d','E','e','F','f');ch<<=4;ch|=FromHexChar((char)Current);Advance();Expecting('0','1','2','3','4','5','6',
'7','8','9','A','a','B','b','C','c','D','d','E','e','F','f');ch<<=4;ch|=FromHexChar((char)Current);sb.Append((char)ch);break;default:Expecting('b','n',
'r','t','\\','/','\"','u');break;}}else sb.Append((char)Current);}Expecting('\"');Advance();return sb.ToString();}/// <summary>
/// Attempts to read a JSON value into the capture buffer
/// </summary>
/// <returns>True if a valid value was read, otherwise false</returns>
public bool TryReadJsonValue(){TryReadWhiteSpace();if('t'==Current){CaptureCurrent();if(Advance()!='r')return false;CaptureCurrent();if(Advance()!='u')
return false;CaptureCurrent();if(Advance()!='e')return false;if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))return false;return true;}if('f'==Current)
{CaptureCurrent();if(Advance()!='a')return false;CaptureCurrent();if(Advance()!='l')return false;CaptureCurrent();if(Advance()!='s')return false;CaptureCurrent();
if(Advance()!='e')return false;CaptureCurrent();if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))return false;return true;}if('n'==Current){CaptureCurrent();
if(Advance()!='u')return false;CaptureCurrent();if(Advance()!='l')return false;CaptureCurrent();if(Advance()!='l')return false;CaptureCurrent();if(-1!=
Advance()&&char.IsLetterOrDigit((char)Current))return false;return true;}if('-'==Current||'.'==Current||char.IsDigit((char)Current))return TryReadReal();
if('\"'==Current)return TryReadJsonString();if('['==Current){CaptureCurrent();Advance();if(TryReadJsonValue()){TryReadWhiteSpace();while(','==Current)
{CaptureCurrent();Advance();if(!TryReadJsonValue())return false;TryReadWhiteSpace();}}TryReadWhiteSpace();if(']'!=Current)return false;CaptureCurrent();
Advance();return true;}if('{'==Current){CaptureCurrent();Advance();TryReadWhiteSpace();if(TryReadJsonString()){TryReadWhiteSpace();if(':'!=Current)return
 false;CaptureCurrent();Advance();if(!TryReadJsonValue())return false;TryReadWhiteSpace();while(','==Current){CaptureCurrent();Advance();TryReadWhiteSpace();
if(!TryReadJsonString())return false;TryReadWhiteSpace();if(':'!=Current)return false;CaptureCurrent();Advance();if(!TryReadJsonValue())return false;TryReadWhiteSpace();
}}TryReadWhiteSpace();if('}'!=Current)return false;CaptureCurrent();Advance();return true;}return false;}/// <summary>
/// Attempts to skip the a JSON value without capturing
/// </summary>
/// <returns>True if a value was found and skipped, otherwise false</returns>
public bool TrySkipJsonValue(){TrySkipWhiteSpace();if('t'==Current){if(Advance()!='r')return false;if(Advance()!='u')return false;if(Advance()!='e')return
 false;if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))return false;return true;}if('f'==Current){if(Advance()!='a')return false;if(Advance()!='l')
return false;if(Advance()!='s')return false;if(Advance()!='e')return false;if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))return false;return true;
}if('n'==Current){if(Advance()!='u')return false;if(Advance()!='l')return false;if(Advance()!='l')return false;if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))
return false;return true;}if('-'==Current||'.'==Current||char.IsDigit((char)Current))return TrySkipReal();if('\"'==Current)return TrySkipJsonString();
if('['==Current){Advance();if(TrySkipJsonValue()){TrySkipWhiteSpace();while(','==Current){Advance();if(!TrySkipJsonValue())return false;TrySkipWhiteSpace();
}}TrySkipWhiteSpace();if(']'!=Current)return false;Advance();return true;}if('{'==Current){Advance();TrySkipWhiteSpace();if(TrySkipJsonString()){TrySkipWhiteSpace();
if(':'!=Current)return false;Advance();if(!TrySkipJsonValue())return false;TrySkipWhiteSpace();while(','==Current){Advance();TrySkipWhiteSpace();if(!TrySkipJsonString())
return false;TrySkipWhiteSpace();if(':'!=Current)return false;Advance();if(!TrySkipJsonValue())return false;TrySkipWhiteSpace();}}TrySkipWhiteSpace();
if('}'!=Current)return false;Advance();return true;}return false;}/// <summary>
/// Attempts to read a JSON value into the capture buffer while parsing it
/// </summary>
/// <param name="result"><see cref="IDictionary{String,Object}"/> for a JSON object, <see cref="IList{Object}"/> for a JSON array, or the appropriate scalar value</param>
/// <returns>True if the value was a valid value, otherwise false</returns>
public bool TryParseJsonValue(out object result){result=null;TryReadWhiteSpace();if('t'==Current){CaptureCurrent();if(Advance()!='r')return false;CaptureCurrent();
if(Advance()!='u')return false;CaptureCurrent();if(Advance()!='e')return false;if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))return false;result
=true;return true;}if('f'==Current){CaptureCurrent();if(Advance()!='a')return false;CaptureCurrent();if(Advance()!='l')return false;CaptureCurrent();if
(Advance()!='s')return false;CaptureCurrent();if(Advance()!='e')return false;CaptureCurrent();if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))return
 false;result=false;return true;}if('n'==Current){CaptureCurrent();if(Advance()!='u')return false;CaptureCurrent();if(Advance()!='l')return false;CaptureCurrent();
if(Advance()!='l')return false;CaptureCurrent();if(-1!=Advance()&&char.IsLetterOrDigit((char)Current))return false;return true;}if('-'==Current||'.'==
Current||char.IsDigit((char)Current)){double r;if(TryParseReal(out r)){result=r;return true;}return false;}if('\"'==Current){string s;if(TryParseJsonString(out
 s)){result=s;return true;}return false;}if('['==Current){CaptureCurrent();Advance();var l=new List<object>();object v;if(TryParseJsonValue(out v)){l.Add(v);
TryReadWhiteSpace();while(','==Current){CaptureCurrent();Advance();if(!TryParseJsonValue(out v))return false;l.Add(v);TryReadWhiteSpace();}}TryReadWhiteSpace();
if(']'!=Current)return false;CaptureCurrent();Advance();result=l;return true;}if('{'==Current){CaptureCurrent();Advance();TryReadWhiteSpace();string n;
object v;var d=new Dictionary<string,object>();if(TryParseJsonString(out n)){TryReadWhiteSpace();if(':'!=Current)return false;CaptureCurrent();Advance();
if(!TryParseJsonValue(out v))return false;d.Add(n,v);TryReadWhiteSpace();while(','==Current){CaptureCurrent();Advance();TryReadWhiteSpace();if(!TryParseJsonString(out
 n))return false;TryReadWhiteSpace();if(':'!=Current)return false;CaptureCurrent();Advance();if(!TryParseJsonValue(out v))return false;d.Add(n,v);TryReadWhiteSpace();
}}TryReadWhiteSpace();if('}'!=Current)return false;CaptureCurrent();Advance();result=d;return true;}return false;}/// <summary>
/// Reads a JSON value into the capture buffer while parsing it
/// </summary>
/// <returns><see cref="IDictionary{String,Object}"/> for a JSON object, <see cref="IList{Object}"/> for a JSON array, or the appropriate scalar value</returns>
/// <exception cref="ExpectingException">The input was not valid</exception>
public object ParseJsonValue(){TrySkipWhiteSpace();if('t'==Current){Advance();Expecting('r');Advance();Expecting('u');Advance();Expecting('e');Advance();
return true;}if('f'==Current){Advance();Expecting('a');Advance();Expecting('l');Advance();Expecting('s');Advance();Expecting('e');Advance();return true;
}if('n'==Current){Advance();Expecting('u');Advance();Expecting('l');Advance();Expecting('l');Advance();return null;}if('-'==Current||'.'==Current||char.IsDigit((char)Current))
return ParseReal();if('\"'==Current)return ParseJsonString();if('['==Current){Advance();TrySkipWhiteSpace();var l=new List<object>();if(']'!=Current){
l.Add(ParseJsonValue());TrySkipWhiteSpace();while(','==Current){Advance();l.Add(ParseJsonValue());TrySkipWhiteSpace();}}TrySkipWhiteSpace();Expecting(']');
Advance();return l;}if('{'==Current){Advance();TrySkipWhiteSpace();var d=new Dictionary<string,object>();if('}'!=Current){string n=ParseJsonString();TrySkipWhiteSpace();
Expecting(':');Advance();object v=ParseJsonValue();d.Add(n,v);TrySkipWhiteSpace();while(','==Current){Advance();TrySkipWhiteSpace();n=ParseJsonString();
TrySkipWhiteSpace();Expecting(':');Advance();v=ParseJsonValue();d.Add(n,v);TrySkipWhiteSpace();}}TrySkipWhiteSpace();if('}'!=Current)return false;Advance();
return d;}return false;}}}