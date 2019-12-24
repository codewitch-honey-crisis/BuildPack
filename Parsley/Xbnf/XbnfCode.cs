using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Parsley
{
	public class XbnfCode : XbnfNode
	{
		public XbnfCode(string value)
		{
			Value = value;
		}
		public XbnfCode()
		{
			Value = null;
		}
		public string Value { get; set; }
	}
}
