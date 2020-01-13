using System;
using System.CodeDom;
using System.IO;
using CD;
using Slang;
namespace CodeDomDemo
{
	partial class Program
	{
		static void Demo2()
		{
			var sw = new StringWriter();
			using (var sr = new StreamReader(@"..\..\Test.tt"))
				SlangPreprocessor.Preprocess(sr, sw);
			var ccu = SlangParser.ParseCompileUnit(sw.ToString());
			SlangPatcher.Patch(ccu);
			Console.WriteLine(CodeDomUtility.ToString(ccu));
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			Console.Clear();
		}
	}
}
