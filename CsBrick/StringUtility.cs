
namespace Grimoire
{
	using System;
	using System.Collections.Generic;
	using System.IO;
	using System.Text;

#if GRIMOIRELIB
	public
#else
	internal
#endif
	static partial class StringUtility
	{
#if GRIMOIRELIB
		public
#else
		internal
#endif
		class StringDifferenceComparer : IComparer<string>
		{
			readonly string _target;
			public StringDifferenceComparer(string target)
			{
				
				_target = target;
			}
			public int Compare(string x, string y)
			{
				double xc = GetWeightedDifference(_target, x);
				double yc = GetWeightedDifference(_target, y);
				return Math.Sign(xc - yc);


			}
		}
		const string _HexDigits = "0123456789ABCDEF";
		internal static byte FromHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new ArgumentException("The value was not hex.", "hex");
		}
		internal static bool IsHexChar(char hex)
		{
			return (
				(':' > hex && '/' < hex) ||
				('G' > hex && '@' < hex) ||
				('g' > hex && '`' < hex)
			);
		}
		/// <summary>
		/// Compute the distance between two strings.
		/// </summary>
		/// <param name="x">The string to search for/compare</param>
		/// <param name="y">The string to search in/compare</param>
		/// <returns>The distance between two strings. Lower is closer.</returns>
		/// <remarks>Calculate the Levenshtein Distance between two strings (the number of insertions, deletions, and substitutions needed to transform the first string into the second)</remarks>
		public static int GetDistance(this string x, string y)
		{
			if (string.IsNullOrEmpty(x))
				return string.IsNullOrEmpty(y) ? 0 : y.Length;
			else if (string.IsNullOrEmpty(y))
				return x.Length;

			int n = x.Length, m = y.Length;
			int[,] d = new int[n + 1, m + 1];
			if (0 == m)
				return n;

			for (int i = 0; i <= n; d[i, 0] = i++) ;
			for (int j = 0; j <= m; d[0, j] = j++) ;

			for (int i = 1; i <= n; ++i)
				for (int j = 1; j <= m; ++j)
					d[i, j] = Math.Min(
						Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
						d[i - 1, j - 1] + ((y[j - 1] == x[i - 1]) ? 0 : 1));
			return d[n, m];
		}
		// implementation of a solution @ https://www.geeksforgeeks.org/longest-repeating-and-non-overlapping-substring/
		// Returns the longest repeating non-overlapping  
		// substring in @string
		static string GetLongestRepeatingSubstring(string @string)
		{
			int n = @string.Length;
			int[,] lcsre = new int[n + 1, n + 1];

			var result = new StringBuilder(); // To store result  
			int resultLength = 0; // To store length of result  

			// building table in bottom-up manner  
			int i, index = 0;
			for (i = 1; i <= n; ++i)
			{
				for (int j = i + 1; j <= n; ++j)
				{
					// (j-i) > lcsre[i-1][j-1] to remove  
					// overlapping  
					if (@string[i - 1] == @string[j - 1]
							&& lcsre[i - 1, j - 1] < (j - i))
					{
						lcsre[i, j] = lcsre[i - 1, j - 1] + 1;

						// updating maximum length of the  
						// substring and updating the finishing  
						// index of the suffix  
						if (lcsre[i, j] > resultLength)
						{
							resultLength = lcsre[i, j];
							index = Math.Max(i, index);
						}
					}
					else
					{
						lcsre[i, j] = 0;
					}
				}
			}

			// If we have non-empty result, then insert all  
			// characters from first character to last  
			// character of String  
			if (resultLength > 0)
			{
				for (i = index - resultLength + 1; i <= index; ++i)
				{
					result.Append(@string[i - 1]);
				}
			}

			return result.ToString();
		}
		// implementation of a solution at https://stackoverflow.com/questions/5859561/getting-the-closest-string-match
		/// <summary>
		/// Uses heuristic weighting to compute the difference between two strings as a floating point value. 
		/// </summary>
		/// <param name="x">The string to search for or compare</param>
		/// <param name="y">The string to search in or compare</param>
		/// <param name="lowWeight">The weight given to the lowest word score. The total of this and highWeight should be 1.</param>
		/// <param name="highWeight">The weight given to the highest word score. The total of this and lowWeight should be 1.</param>
		/// <returns>A value indicating the distance between the strings. Lower is closer.</returns>
		/// <remarks>The number is just a value. There is no scale, so this isn't a traditional numeric "difference" value but simply a value one can use to sort.</remarks>
		public static double GetWeightedDifference(this string x, string y, double lowWeight = .8, double highWeight = .2)
		{
			double vp = _ValuePhrase(x, y);
			double vw = _ValueWords(x, y);
			return Math.Min(vp, vw) * lowWeight + Math.Max(vp, vw) * highWeight;
		}
		public static readonly char[] WordBreakChars = new char[] { ' ', '_', '\t', '.', '+', '-', '(', ')', '[', ']', '\"', /*'\'',*/ '{', '}', '!', '<', '>', '~', '`', '*', '$', '#', '@', '!', '\\', '/', ':', ';', ',', '?', '^', '%', '&', '|', '\n', '\r', '\v', '\f', '\0' };
		static int _ValueWords(string x, string y)
		{
			if (null == x)
				x = "";
			if (null == y)
				y = "";
			string[] wordsX = x.Split(WordBreakChars);
			string[] wordsY = y.Split(WordBreakChars);
			int d;
			int iX, iY;
			int best;
			int result = 0;
			for (iX = 0; iX < wordsX.Length; iX++)
			{
				best = y.Length;
				for (iY = 0; iY < wordsY.Length; iY++)
				{
					d = GetDistance(wordsX[iX], wordsY[iY]);

					if (d < best)
						best = d;
					if (0 == d)
						break;
				}
				result += best;
			}
			return result;
		}
		static double _ValuePhrase(string x, string y)
		{
			return GetDistance(x, y) - 0.8d * Math.Abs(((null != y) ? y.Length : 0) - (null != x ? x.Length : 0));
		}

		public static IEnumerable<string> SplitWords(this string text, params char[] wordBreakChars)
		{
			if (null == wordBreakChars || 0 == wordBreakChars.Length)
				wordBreakChars = WordBreakChars;
			if (string.IsNullOrEmpty(text))
				yield break;
			int i = text.IndexOfAny(wordBreakChars);
			if (0 > i)
			{
				yield return text;
				yield break;
			}
			if (0 < i)
			{
				yield return text.Substring(0, i);
				i++;
			}
			int si = i;
			while (si < text.Length)
			{
				i = text.IndexOfAny(wordBreakChars, si);
				if (0 > i)
					i = text.Length;
				if (1 < i - si)
				{
					yield return text.Substring(si, i - si);
				}
				si = i + 1;
			}
		}
	}
}
