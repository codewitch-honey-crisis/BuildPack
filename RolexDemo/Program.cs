using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolexDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			while (true)
			{
				Console.Write("Expr>");
				var s = Console.ReadLine();
				if (null != s) s = s.Trim();
				if (string.IsNullOrEmpty(s))
					break;
				try
				{
					Console.WriteLine(Parser.Eval(s));
				}
				catch (Exception ex)
				{
					Console.WriteLine("Error: " + ex.Message);
				}
			}
		}

	}
}
