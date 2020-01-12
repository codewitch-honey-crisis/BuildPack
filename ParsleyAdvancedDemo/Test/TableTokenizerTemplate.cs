using System.Collections.Generic;

namespace Rolex
{
	class TableTokenizerTemplate : TableTokenizer
	{
		internal static DfaEntry[] DfaTable;
		internal static int[] NodeFlags;
		internal static string[] BlockEnds;
		public TableTokenizerTemplate(IEnumerable<char> input) :
			   base(DfaTable, BlockEnds, NodeFlags, input)
		{
		}
	}
}
