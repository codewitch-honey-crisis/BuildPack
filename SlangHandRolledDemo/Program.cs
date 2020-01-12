using System;
using System.CodeDom;
using System.Diagnostics;
using System.IO;
using CD;
/// <summary>
/// Test
/// </summary>
namespace SlangHandRolledDemo
{
	/// <summary>
	/// Program
	/// </summary>
	class Program
	{
		/// <summary>
		/// Main
		/// </summary>
		/// <param name="args">The args</param>
		static void Main()
		{
			Demo3();
		}
		static void Demo3()
		{
			Stream stm = null;
			CodeCompileUnit ccu = null;
			try
			{
				stm = File.OpenRead(@"..\..\Test\PatchTest.cs");
				var tok = new SlangTokenizer(stm);
				ccu = SlangParser.ParseCompileUnit(tok);
			}
			finally
			{
				stm.Close();
			}
			SlangPatcher.Patch(ccu);
			var co = SlangPatcher.GetNextUnresolvedElement(ccu);
			if (null != co)
			{
				Console.WriteLine("Next unresolved element is:");
				Console.WriteLine(CodeDomUtility.ToString(co).TrimEnd());
			}
			else
			{
				Console.WriteLine(CodeDomUtility.ToString(ccu));
			}
		}
		static void Demo2()
		{
			var files = Directory.GetFiles(@"..\..\Test", "*.cs");
			for (var i = 0; i < files.Length; ++i)
			{
				_Test(files[i]);
			}
		}
		static void _Test2()
		{
			var test = "IEnumerator<Token> IEnumerable<Token>.GetEnumerator()\r\n" +
			"{\r\n" +
				"return GetEnumerator();\r\n" +
			"}\r\n";
			test = "public CompiledTokenizerTemplate(IEnumerable<char> input) : base(input)" +
			"{\r\n" +
			"}\r\n";
			test = "((Token)_t).Line += _line";
			var tok = new SlangTokenizer(test);
			//CodeObject co = SlangParser.ParseMember(tok);
			CodeObject co = SlangParser.ParseExpression(tok);
			Console.WriteLine(CodeDomUtility.ToString(co).TrimEnd());
		}
		static void _Test(string file)
		{
			Console.WriteLine("Parsing file: " + file);
			
			// don't read directly from the file for perf testing.
			StreamReader sr = null;
			string text = null;
			try
			{
				sr = new StreamReader(file);
				text = sr.ReadToEnd();
			}
			finally
			{
				if (null != sr)
					sr.Close();
			}
			var sw = new Stopwatch();
			sw.Start();
			for (var i = 0; i < 100; ++i)
			{
				var tok = new SlangTokenizer(text);
				CodeObject co = SlangParser.ParseCompileUnit(tok);
			}
			sw.Stop();
			Console.WriteLine("Parsed "+Path.GetFileName(file)+" in " + (sw.ElapsedMilliseconds/100d) + " msec");

			//Console.WriteLine(CodeDomUtility.ToString(co).TrimEnd());
		}
	}
}
