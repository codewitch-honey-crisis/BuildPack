using CD;
using System;
using System.CodeDom;


namespace CodeDomDemo
{
	partial class Program
	{
		public static CodeCompileUnit Demo1()
		{
			// evaluates a simple expression 
			var res = new CodeDomResolver();
			Console.WriteLine(res.Evaluate(SlangParser.ParseExpression("5*4-7")));

			// takes this file and get a codedom from it
			var ccu = SlangParser.ReadCompileUnitFrom("..\\..\\Demo1.cs");
			ccu.ReferencedAssemblies.Add("CodeDomGoKit.dll");
			ccu.ReferencedAssemblies.Add(typeof(CodeObject).Assembly.GetName().ToString());
			// now patch the parsed codedom so it's correct
			// NOTE: slang can't actually bind to this because it is a paramarray method
			// but we don't care here because it's not necessary. Slang only binds to what
			// it needs to and we never need the return type anyway (which is void)
			SlangPatcher.Patch(ccu); 
			// now write it out in VB
			Console.WriteLine(CodeDomUtility.ToString(ccu, "vb"));
			// return the code we generated so we can use it for other demos
			// yay recycling
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			Console.Clear();
			return ccu;
		}
		public static string TestOverload(int val)
		{
			Console.WriteLine(val);
			return val.ToString();
		}
		public static int TestOverload(string val)
		{
			Console.WriteLine(val);
			return val.GetHashCode();
		}
	}
}
