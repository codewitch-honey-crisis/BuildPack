using CD;
using System;
using System.Collections.Generic;
using System.Linq;

// just an app I use for debugging
namespace scratch
{
	class Program
	{
		static void Main(string[] args)
		{
			var ccu = SlangParser.ReadCompileUnitFrom(@"..\..\Tokenizer.cs");
			SlangPatcher.Patch(ccu);
			var nu = SlangPatcher.GetNextUnresolvedElement(ccu);
			if(null!=nu)
			{
				Console.WriteLine("Unresolved:");
				Console.WriteLine(CodeDomUtility.ToString(nu));
			} else
			{
				Console.WriteLine(CodeDomUtility.ToString(ccu));
			}
		}
	}
}
