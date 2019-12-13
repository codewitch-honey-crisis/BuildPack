using System.Collections.Generic;
using System.CodeDom;

namespace CD
{
	/// <summary>
	/// Compares <see cref="CodeTypeReference"/> objects for equality
	/// </summary>
	public class CodeTypeReferenceEqualityComparer : IEqualityComparer<CodeTypeReference>
	{
		/// <summary>
		/// Provides access to the default instance of this class
		/// </summary>
		public static readonly IEqualityComparer<CodeTypeReference> Default = new CodeTypeReferenceEqualityComparer();
		bool IEqualityComparer<CodeTypeReference>.Equals(CodeTypeReference x, CodeTypeReference y)
		{
			return Equals(x, y);
		}
		/// <summary>
		/// Indicates whether the two types are equal
		/// </summary>
		/// <param name="x">The first type to compare</param>
		/// <param name="y">The second type tp compare</param>
		/// <returns>True if the types are equal, otherwise false</returns>
		public static bool Equals(CodeTypeReference x, CodeTypeReference y)
		{
			if (ReferenceEquals(x, y)) return true;
			if (ReferenceEquals(null, x)) return false;
			if (ReferenceEquals(null, y)) return false;
			if (x.Options != y.Options)
				return false;
			if (!Equals(x.ArrayElementType, y.ArrayElementType) ||
				x.ArrayRank != y.ArrayRank)
				return false;
			else
			{
				if (0 != string.Compare(x.BaseType, y.BaseType))
					return false;
				var c = x.TypeArguments.Count;
				if (c != y.TypeArguments.Count) return false;
				for (var i = 0; i < c; ++i)
				{
					if (!Equals(x.TypeArguments[i], y.TypeArguments[i]))
						return false;
				}
			}
			return true;
		}
		int IEqualityComparer<CodeTypeReference>.GetHashCode(CodeTypeReference obj)
		{
			if (null == obj) return 0;
			var result = obj.Options.GetHashCode();
			if (null != obj.ArrayElementType)
			{
				result ^= obj.ArrayRank.GetHashCode();
				result ^= Default.GetHashCode(obj.ArrayElementType);
			}
			else
			{
				if (null != obj.BaseType)
					result ^= obj.BaseType.GetHashCode();
				for (int ic = obj.TypeArguments.Count, i = 0; i < ic; ++i)
					result ^= Default.GetHashCode(obj.TypeArguments[i]);
			}
			return result;
		}
	}
}
