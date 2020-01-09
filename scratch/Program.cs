using System;
using System.IO;

namespace scratch
{
	class Program
	{
		static void Main(string[] args)
		{
			var e = "5*(2+-3)*4+5";
			var tok = new scratch.expr.ExpressionTokenizer(e);
			var pn = scratch.expr.ExpressionParser.Parse(tok);
			Console.WriteLine(pn.ToString("t"));
			Console.WriteLine("{0} = {1}", e,scratch.expr.ExpressionParser.Evaluate(pn));
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			Console.Clear();
			Stream stm = null;
			ParseNode spn = null;
			try
			{
				stm = File.OpenRead(@"..\..\Program.cs");
				var stok = new SlangTokenizer(stm);
				spn = SlangParser.Parse(stok);
			}
			finally
			{
				if (null != stm)
					stm.Close();
				stm = null;
			}
			Console.WriteLine(spn.ToString("t"));

		}
	}
}
