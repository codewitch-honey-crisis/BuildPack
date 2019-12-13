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
			var vars = new Dictionary<string, int>();
			vars.Add("a", 1);
			vars.Add("c", 3);
			var test = "((a + 2) * -c) / 3";
			Console.WriteLine(Parser.Eval(test,vars));
		}

	}
}
