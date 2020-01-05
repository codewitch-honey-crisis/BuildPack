using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsley
{
	struct XbnfGenerationInfo
	{
		public XbnfDocument Document;
		public ListDictionary<XbnfExpression, string> TerminalMap;
		public IDictionary<XbnfDocument, CfgDocument> CfgMap;
		public IDictionary<string, KeyValuePair<XbnfDocument, XbnfProduction>> AllExternals;
		public IDictionary<XbnfDocument, HashSet<string>> ExternalsMap;
		public T GetCfgAttribute<T>(string sym,string name,T @default=default(T))
		{
			foreach(var cfg in CfgMap)
			{
				var o = cfg.Value.GetAttribute(sym, name, @default);
				if (!Equals(@default ,o))
					if (o is T)
						return (T)o;
			}
			return @default;
		}
	}
}
