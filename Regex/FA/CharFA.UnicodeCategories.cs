using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace RE
{
	partial class CharFA<TAccept>
	{
		static IDictionary<string, IList<CharRange>> _unicodeCategories = _GetUnicodeCategories();
		/// <summary>
		/// Retrieves a dictionary indicating the unicode categores supported by this library
		/// </summary>
		public static IDictionary<string, IList<CharRange>> UnicodeCategories
					=> _unicodeCategories;
		// build the unicode categories
		static IDictionary<string, IList<CharRange>> _GetUnicodeCategories()
		{
			var working = new Dictionary<string, List<char>>(StringComparer.InvariantCultureIgnoreCase);
			for(var i = 0;i<char.MaxValue;++i)
			{
				char ch = unchecked((char)i);
				var uc = char.GetUnicodeCategory(ch);
				switch(uc)
				{
					case UnicodeCategory.ClosePunctuation:
						_AddTo(working, "Pe", ch);
						_AddTo(working, "P", ch);
						break;
					case UnicodeCategory.ConnectorPunctuation:
						_AddTo(working, "Pc", ch);
						_AddTo(working, "P", ch);
						break;
					case UnicodeCategory.Control:
						_AddTo(working, "Cc", ch);
						_AddTo(working, "C", ch);
						break;
					case UnicodeCategory.CurrencySymbol:
						_AddTo(working, "Sc", ch);
						_AddTo(working, "S", ch);
						break;
					case UnicodeCategory.DashPunctuation:
						_AddTo(working, "Pd", ch);
						_AddTo(working, "P", ch);
						break;
					case UnicodeCategory.DecimalDigitNumber:
						_AddTo(working, "Nd", ch);
						_AddTo(working, "N", ch);
						break;
					case UnicodeCategory.EnclosingMark:
						_AddTo(working, "Me", ch);
						_AddTo(working, "M", ch);
						break;
					case UnicodeCategory.FinalQuotePunctuation:
						_AddTo(working, "Pf", ch);
						_AddTo(working, "P", ch);
						break;
					case UnicodeCategory.Format:
						_AddTo(working, "Cf", ch);
						_AddTo(working, "C", ch);
						break;
					case UnicodeCategory.InitialQuotePunctuation:
						_AddTo(working, "Pi", ch);
						_AddTo(working, "P", ch);
						break;
					case UnicodeCategory.LetterNumber:
						_AddTo(working, "Nl", ch);
						_AddTo(working, "N", ch);
						break;
					case UnicodeCategory.LineSeparator:
						_AddTo(working, "Zl", ch);
						_AddTo(working, "Z", ch);
						break;
					case UnicodeCategory.LowercaseLetter:
						_AddTo(working, "Ll", ch);
						_AddTo(working, "L", ch);
						break;
					case UnicodeCategory.MathSymbol:
						_AddTo(working, "Sm", ch);
						_AddTo(working, "S", ch);
						break;
					case UnicodeCategory.ModifierLetter:
						_AddTo(working, "Lm", ch);
						_AddTo(working, "L", ch);
						break;
					case UnicodeCategory.ModifierSymbol:
						_AddTo(working, "Sk", ch);
						_AddTo(working, "S", ch);
						break;
					case UnicodeCategory.NonSpacingMark:
						_AddTo(working, "Mn", ch);
						_AddTo(working, "M", ch);
						break;
					case UnicodeCategory.OpenPunctuation:
						_AddTo(working, "Ps", ch);
						_AddTo(working, "P", ch);
						break;
					case UnicodeCategory.OtherLetter:
						_AddTo(working, "Lo", ch);
						_AddTo(working, "L", ch);
						break;
					case UnicodeCategory.OtherNotAssigned:
						_AddTo(working, "Cn", ch);
						_AddTo(working, "C", ch);
						break;
					case UnicodeCategory.OtherNumber:
						_AddTo(working, "No", ch);
						_AddTo(working, "N", ch);
						break;
					case UnicodeCategory.OtherPunctuation:
						_AddTo(working, "Po", ch);
						_AddTo(working, "P", ch);
						break;
					case UnicodeCategory.OtherSymbol:
						_AddTo(working, "So", ch);
						_AddTo(working, "S", ch);
						break;
					case UnicodeCategory.ParagraphSeparator:
						_AddTo(working, "Zp", ch);
						_AddTo(working, "Z", ch);
						break;
					case UnicodeCategory.PrivateUse:
						_AddTo(working, "Co", ch);
						_AddTo(working, "Co", ch);
						break;
					case UnicodeCategory.SpaceSeparator:
						_AddTo(working, "Zs", ch);
						_AddTo(working, "Z", ch);
						break;
					case UnicodeCategory.SpacingCombiningMark:
						_AddTo(working, "Mc", ch);
						_AddTo(working, "M", ch);
						break;
					case UnicodeCategory.Surrogate:
						_AddTo(working, "Cs", ch);
						_AddTo(working, "C", ch);
						break;
					case UnicodeCategory.TitlecaseLetter:
						_AddTo(working, "Lt", ch);
						_AddTo(working, "L", ch);
						break;
					case UnicodeCategory.UppercaseLetter:
						_AddTo(working, "Lu", ch);
						_AddTo(working, "L", ch);
						break;
				}
			}
			var result = new Dictionary<string, IList<CharRange>>();
			foreach(var kvp in working)
			{
				kvp.Value.Sort();
				result.Add(kvp.Key, new List<CharRange>(CharRange.GetRanges(kvp.Value)));
			}
			return result;
		}
		static void _AddTo(IDictionary<string,List<char>> working,string uc, char ch)
		{
			List<char> s;
			if (!working.TryGetValue(uc, out s))
			{
				s = new List<char>();
				working.Add(uc, s);
			}
			if(!s.Contains(ch))
				s.Add(ch);
		}
	}
}
