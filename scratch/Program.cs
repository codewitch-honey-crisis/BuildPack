using System;
using System.IO;
using ParsleyDemo;
namespace scratch
{
	class Program
	{
		static void Main(string[] args)
		{

			var tokenizer = new ExpressionTokenizer("/* foo*/ 2+2 * 5 +1// foo");
			var pt = ExpressionParser.Parse(tokenizer);
			Console.WriteLine(ExpressionParser.Evaluate(pt));
		}
	}
}
