using System.Collections.Generic;

namespace Rolex
{
	class PrototypeTokenizerTemplate : PrototypeTokenizer
	{
		internal static NfaEntry[] NfaTable;
		internal static int[] NodeFlags;
		internal static int[][] BlockEnds;
		public PrototypeTokenizerTemplate(IEnumerable<char> input) :
			   base(NfaTable, BlockEnds, NodeFlags, input)
		{
		}
	}
}
