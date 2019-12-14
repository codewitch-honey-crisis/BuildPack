using System;
using System.CodeDom;
using CD;
namespace SlangDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			// note that in the real world cases, you'd need to use SlangPatcher.Patch()
			// on whole compile units to get proper codedom objects back. This method is
			// simply "close enough for government work" and may not work for languages
			// other than VB
			while(true)
			{
				Console.Write("Slang>");
				var s= Console.ReadLine();
				if (null != s) s = s.Trim();
				if (string.IsNullOrEmpty(s))
					break;
				var isStatement = s.EndsWith(";") || s.EndsWith("}");
				CodeObject co = null;
				try
				{
					if (isStatement)
						co = SlangParser.ParseStatement(s, true);
					else
						co = SlangParser.ParseExpression(s);
				}
				catch(ArgumentException ex)
				{
					Console.WriteLine("Error: " + ex.Message);
				}
				if (null!=co)
				{
					s = CodeDomUtility.ToString(co, "vb");
					s = s.Trim();
					Console.WriteLine(s);
				}
			}
			
		}
	}
}
