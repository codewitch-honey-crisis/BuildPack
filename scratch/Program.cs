using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using CD;
namespace scratch
{
	// line comment
	/* block comment */
	class Program
	{
		static void Main(string[] args)
		{
			#region fetch consts
			var consts = new Dictionary<int, string>();
			foreach (var f in typeof(SlangTokenizer).GetFields(BindingFlags.Public | BindingFlags.Static))
			{
				if(f.FieldType==typeof(int) && f.IsLiteral)
				{
					var i = (int)f.GetValue(null);
					if(-3<i)
						consts[i]= f.Name;
				}
			}
			#endregion Fetch consts
			using (var stm = File.Open(@"..\..\..\Program.cs", FileMode.Open))
			{
				
				var tokenizer = new SlangTokenizer(stm);
				foreach (var tok in tokenizer)
					Console.WriteLine("{0}: {1}", consts[tok.SymbolId], tok.Value);
			}
			
		}
	}
}
