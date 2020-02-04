using System.Collections.Generic;

namespace Rolex
{
	class TableTokenizerTemplate : TableTokenizer
	{
		internal static DfaEntry[] DfaTable;
		internal static int[] NodeFlags;
		internal static int[][] BlockEnds;
		public TableTokenizerTemplate(IEnumerable<char> input) :
			   base(DfaTable, BlockEnds, NodeFlags, input)
		{
		}
	}
}
