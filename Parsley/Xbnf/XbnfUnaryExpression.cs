using System;
using System.Collections.Generic;
using System.Text;

namespace Parsley
{
	public abstract class XbnfUnaryExpression : XbnfExpression
	{
		public XbnfExpression Expression { get; set; } = null;
		public override bool IsTerminal {
			get {
				if (null == Expression)
					return true;
				return Expression.IsTerminal;
			}
		}
	}
}
