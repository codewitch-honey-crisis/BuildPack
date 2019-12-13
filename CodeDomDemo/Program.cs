using CD;
using System;
using System.CodeDom;
using System.Globalization;
using System.Reflection;

namespace CodeDomDemo
{
	partial class Program
	{
		static void Main()
		{
			var ccu =Demo1();
			Demo2();
			Demo3(ccu);
			Demo4(ccu);
			Demo5(ccu);
			Demo6();
		}
	}
}
