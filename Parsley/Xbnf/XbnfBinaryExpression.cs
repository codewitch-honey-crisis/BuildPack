﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Parsley
{
	public abstract class XbnfBinaryExpression : XbnfExpression
	{
		public XbnfExpression Left { get; set; }
		public XbnfExpression Right { get; set; }

		public override bool IsTerminal {
			get {
				if (null != Left && !Left.IsTerminal)
					return false;
				if (null != Right && !Right.IsTerminal)
					return false;
				return true;
			}
		}
	}
}
