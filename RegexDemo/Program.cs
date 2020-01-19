using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using RE;
namespace RegexDemo
{
	class Program
	{
		
		private class _ConsoleProgress : IProgress<CharFAProgress>
		{
			public void Report(CharFAProgress value)
			{
				Console.Write('.');
			}
		}
		static void Main()
		{
			var id = RegexExpression.Parse(@"[A-Z_a-z][0-9A-Z_a-z]*").ToFA("Id");
			var @int = RegexExpression.Parse(@"0|(\-?[1-9][0-9]*)").ToFA("Int");
			var space = RegexExpression.Parse(@"[ \t\r\n\v\f]").ToFA("Space");
			var l = CharFA<string>.ToLexer(id, @int, space);
			var dfaTable = l.ToDfaStateTable();
			var text = "fubar bar 123 1foo bar -243 @#*! 0";
			Console.WriteLine("Lex: " + text);
			var pc = ParseContext.Create(text);
			while (-1 != pc.Current)
			{
				pc.ClearCapture();
				// Lex using our DFA table. This is a little different
				// because it's a static method that takes CharDfaEntry[]
				// as its first parameter. It also uses symbol ids instead
				// of the actual symbol. You must map them back using the
				// symbol table you created earlier.
				var acc = CharFA<string>.LexDfa(dfaTable, pc, -1);
				// when we write this, we map our symbol id back to the
				// symbol using our symbol table
				Console.WriteLine("{0}: {1}", acc, pc.GetCapture());
			}
			var sw = new Stopwatch();
			const int ITER = 1000;
			for(var i = 0;i<ITER;++i)
			{
				pc = ParseContext.Create(text);
				while (-1 != pc.Current)
				{
					pc.ClearCapture();
					// Lex using our DFA table. This is a little different
					// because it's a static method that takes CharDfaEntry[]
					// as its first parameter. It also uses symbol ids instead
					// of the actual symbol. You must map them back using the
					// symbol table you created earlier.
					sw.Start();
					var acc = CharFA<string>.LexDfa(dfaTable, pc, -1);
					sw.Stop();
				}
			}
			Console.WriteLine("Lexed in " + sw.ElapsedMilliseconds / (float)ITER + " msec");
			return;
			// _BuildArticleImages() // requires GraphViz
			// _RunCompiledLexCodeGen()
			_RunLexer();
			_RunMatch();
			_RunDom();
			// the following require GraphViz
			_RunStress();
			_RunStress2();
		}
		static void _RunFromFA()
		{

			var expr = RegexExpression.Parse("(0*11*0[01]*)");
			var fa = expr.ToFA<string>();
			
			fa.IsAccepting = true; // modify the expression by changing the FSM
			var x = new CharFA<string>();
			var y = new CharFA<string>();
			var z = new CharFA<string>(true);
			x.InputTransitions.Add('a', y);
			y.InputTransitions.Add('b', y);
			y.InputTransitions.Add('c', z);
			fa = x;
			var test = "[A-Z_a-z][0-9A-Z_a-z]*";
			//test = "ab*c";
			test = "(foo)*bar";
			fa = RegexExpression.Parse(test).ToFA<string>();
			fa.RenderToFile(@"..\..\..\test_expr_nfa.jpg");
			var ffa = fa.ToDfa();
			ffa.RenderToFile(@"..\..\..\test_expr.jpg");
			expr = RegexExpression.FromFA(fa);
			Console.WriteLine(expr);
			fa.RenderToFile(@"..\..\..\test_nfa.jpg");
			var dfa = fa.ToDfa();
			dfa.RenderToFile(@"..\..\..\test.jpg");
			
		}
		static void _RunStress()
		{
			// C# keywords
			const string cskw = "abstract|add|as|ascending|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|equals|explicit|extern|false|finally|fixed|float|for|foreach|get|global|goto|if|implicit|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|remove|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield";
			var expr = RegexExpression.Parse(cskw);
			var fa = expr.ToFA("");
			Console.WriteLine("C# keyword NFA has {0} states.",fa.FillClosure().Count);
			Console.WriteLine("Reducing C# keywords");
			// very expensive in this case
			fa = fa.Reduce(new _ConsoleProgress());
			Console.WriteLine();
			Console.WriteLine("C# keyword DFA has {0} states.", fa.FillClosure().Count);
			Console.WriteLine("First Expression: {0}", cskw);
			//Console.WriteLine("Final Expression: {0}", RegexExpression.FromFA(fa));
			var dopt = new CharFA<string>.DotGraphOptions();
			dopt.Dpi = 150; // make the image smaller
			Console.WriteLine("Rendering stress.jpg");
			fa.RenderToFile(@"..\..\..\stress.jpg",dopt);
		}
		static void _RunStress2()
		{
			CharFA<string> fa = null;
			var min = 599;
			var max = 639;
			Console.Write("Building NFA matching integer values {0}-{1} ",min,max);
			for(var i = min;i<=max;++i)
			{
				if (null == fa)
					fa = CharFA<string>.Literal(i.ToString());
				else
					fa = CharFA<string>.Or(new CharFA<string>[] { fa, CharFA<string>.Literal(i.ToString()) });
				// for perf reasons we can reduce every 12 times
				if (0 == (i % 12))
					Console.Write('.');
				// replace the above "Console.Write('.');" line with below is MUCH faster
				//	fa=fa.Reduce(new _ConsoleProgress());
			}
			Console.WriteLine();
			fa.TrimNeutrals();
			//fa.TrimDuplicates();
			Console.WriteLine("C# integer NFA has {0} states.", fa.FillClosure().Count);
			fa.RenderToFile(@"..\..\..\stress2_nfa.jpg");
			fa =fa.Reduce(new _ConsoleProgress());
			Console.WriteLine();
			Console.WriteLine("C# integer DFA has {0} states.", fa.FillClosure().Count);
			//var expr = RegexExpression.FromFA(fa);
			//Console.WriteLine("Final Expression: {0}", expr);
			Console.WriteLine("Rendering stress2.jpg");
			fa.RenderToFile(@"..\..\..\stress2.jpg");
		}
		static void _RunCompiledLexCodeGen()
		{
			// create our expressions
			var digits = CharFA<string>.Repeat(
				CharFA<string>.Set("0123456789"),
				1, -1
				, "Digits");
			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var whitespace = CharFA<string>.Repeat(
				CharFA<string>.Set(" \t\r\n\v\f"),
				1, -1
				, "Whitespace");
			// initialize our lexer
			var lexer = CharFA<string>.ToLexer(digits, word, whitespace);
			// create the symbol table (include the error symbol at index/id 3)
			var symbolTable = new string[] { "Digits", "Word", "Whitespace", "#ERROR" };
			// create the DFA table we'll use to generate code
			var dfaTable = lexer.ToDfaStateTable(symbolTable);
			// create our new class
			var compClass = new CodeTypeDeclaration("RegexGenerated");
			compClass.TypeAttributes = System.Reflection.TypeAttributes.Class;
			compClass.Attributes = MemberAttributes.Final | MemberAttributes.Static;
			// add the symbol table field - in production we'll set the name
			// to something more appropriate
			var symtblField = new CodeMemberField(typeof(string[]), "LexSymbols");
			symtblField.Attributes = MemberAttributes.Static | MemberAttributes.Public;
			// generate the symbol table init code
			symtblField.InitExpression = CharFA<string>.GenerateSymbolTableInitializer(symbolTable);
			compClass.Members.Add(symtblField);
			// Generate and add the compiled lex method code
			compClass.Members.Add(CharFA<string>.GenerateLexMethod(dfaTable, 3));
			// in production we'd change the name of the returned method
			// above
			// add the DFA table field - in production we'd change the name
			var dfatblField = new CodeMemberField(typeof(CharDfaEntry[]), "LexDfaTable");
			dfatblField.Attributes = MemberAttributes.Static | MemberAttributes.Public;
			// generate the DFA state table init code
			dfatblField.InitExpression = CharFA<string>.GenerateDfaStateTableInitializer(dfaTable);
			compClass.Members.Add(dfatblField);
			// create the C# provider and generate the code
			// we'll usually want to put this in a namespace
			// but we haven't here
			var prov = CodeDomProvider.CreateProvider("cs");
			prov.GenerateCodeFromType(compClass, Console.Out, new CodeGeneratorOptions());
		}
		static void _RunDom()
		{
			var test = "(ABC|DEF)*";
			var dom = RegexExpression.Parse(test);
			Console.WriteLine(dom.ToString());
			var rep = dom as RegexRepeatExpression;
			rep.MinOccurs = 1;
			Console.WriteLine(dom.ToString());
			dom.ToFA("Accept");
			Console.WriteLine();
		}
		static void _RunMatch()
		{
			var test = "foo123_ _bar";

			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var dfaWord = word.ToDfa();
			var dfaTableWord = word.ToDfaStateTable();
			CharFAMatch match;
			var pc = ParseContext.Create(test);
			Console.WriteLine("Matching words with an NFA:");
			while (null != (match = word.Match(pc)))
				Console.WriteLine("Found match at {0}: {1}", match.Position, match.Value);
			Console.WriteLine();
			pc = ParseContext.Create(test);
			Console.WriteLine("Matching words with a DFA:");
			while (null != (match = dfaWord.MatchDfa(pc)))
				Console.WriteLine("Found match at {0}: {1}", match.Position, match.Value);
			Console.WriteLine();
			pc = ParseContext.Create(test);
			Console.WriteLine("Matching words with a DFA state table:");
			while (null != (match = CharFA<string>.MatchDfa(dfaTableWord, pc)))
				Console.WriteLine("Found match at {0}: {1}", match.Position, match.Value);
			Console.WriteLine();
			pc = ParseContext.Create(test);
			Console.WriteLine("Matching words with a compiled DFA:");
			while (null != (match = Match(pc)))
				Console.WriteLine("Found match at {0}: {1}", match.Position, match.Value);
			Console.WriteLine();
		}
		static void _RunLexer()
		{
			var digits = CharFA<string>.Repeat(
				CharFA<string>.Set("0123456789"),
				1, -1
				, "Digits");
			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var whitespace = CharFA<string>.Repeat(
				CharFA<string>.Set(" \t\r\n\v\f"),
				1, -1
				, "Whitespace");
			var lexer = CharFA<string>.ToLexer(digits, word, whitespace);
			var lexerDfa = lexer.ToDfa();
			lexerDfa.TrimDuplicates();
			// we use a symbol table with the DFA state table to map ids back to strings
			var symbolTable = new string[] { "Digits", "Word", "Whitespace", "#ERROR" };
			// make sure to pass the symbol table if you're using one
			var dfaTable = lexer.ToDfaStateTable(symbolTable);
			var test = "foo123_ _bar";
			Console.WriteLine("Lex using the NFA");
			// create a parse context over our test string
			var pc = ParseContext.Create(test);
			// while not end of input
			while (-1 != pc.Current)
			{
				// clear the capture so that we don't keep appending the token data
				pc.ClearCapture();
				// lex the next token
				var acc = lexer.Lex(pc, "#ERROR");
				// write the result
				Console.WriteLine("{0}: {1}", acc, pc.GetCapture());
			}
			Console.WriteLine();
			Console.WriteLine("Lex using the DFA");
			// create a new parse context over our test string
			// because our old parse context is now past the end
			pc = ParseContext.Create(test);
			while (-1 != pc.Current)
			{
				pc.ClearCapture();
				// lex using the DFA. This works exactly like 
				// the previous Lex method except that it's
				// optimized for DFA traversal.
				// DO NOT use this with an NFA. It won't work
				// but won't error (can't check for perf reasons)
				var acc = lexerDfa.LexDfa(pc, "#ERROR");
				// write the result
				Console.WriteLine("{0}: {1}", acc, pc.GetCapture());
			}
			Console.WriteLine();
			Console.WriteLine("Lex using the DFA state table");
			pc = ParseContext.Create(test);
			while (-1 != pc.Current)
			{
				pc.ClearCapture();
				// Lex using our DFA table. This is a little different
				// because it's a static method that takes CharDfaEntry[]
				// as its first parameter. It also uses symbol ids instead
				// of the actual symbol. You must map them back using the
				// symbol table you created earlier.
				var acc = CharFA<string>.LexDfa(dfaTable, pc, 3);
				// when we write this, we map our symbol id back to the
				// symbol using our symbol table
				Console.WriteLine("{0}: {1}", symbolTable[acc], pc.GetCapture());
			}
			Console.WriteLine();
			Console.WriteLine("Lex using our compiled lex method");
			pc = ParseContext.Create(test);
			while (-1 != pc.Current)
			{
				pc.ClearCapture();
				// Lex using our compiledDFA. Like the table driven lex
				// this also uses symbol ids instead of the actual symbol.
				var acc = Lex(pc);
				// when we write this, we map our symbol id back to the
				// symbol using our symbol table
				Console.WriteLine("{0}: {1}", symbolTable[acc], pc.GetCapture());
			}
			Console.WriteLine();
		}
		static void _BuildArticleImages()
		{
			// this generates the figures used in the code project article
			// at https://www.codeproject.com/Articles/5251476/How-to-Build-a-Regex-Engine-in-Csharp
			var litA = CharFA<string>.Literal("ABC", "Accept");
			litA.RenderToFile(@"..\..\..\literal.jpg");
			var litAa = CharFA<string>.CaseInsensitive(litA, "Accept");
			litAa.RenderToFile(@"..\..\..\literal_ci.jpg");
			var opt = CharFA<string>.Optional(litA, "Accept");
			opt.RenderToFile(@"..\..\..\optional.jpg");
			var litB = CharFA<string>.Literal("DEF");
			var or = CharFA<string>.Or(new CharFA<string>[] { litA, litB }, "Accept");
			or.RenderToFile(@"..\..\..\or.jpg");
			var set = CharFA<string>.Set("ABC", "Accept");
			set.RenderToFile(@"..\..\..\set.jpg");
			var loop = CharFA<string>.Repeat(litA, 1, -1, "Accept");
			loop.RenderToFile(@"..\..\..\repeat.jpg");
			var concat = CharFA<string>.Concat(new CharFA<string>[] { litA, litB }, "Accept");
			concat.RenderToFile(@"..\..\..\concat.jpg");
			var foobar = CharFA<string>.Or(new CharFA<string>[] { CharFA<string>.Literal("foo"), CharFA<string>.Literal("bar") }, "Accept");
			foobar.RenderToFile(@"..\..\..\foobar_nfa.jpg");
			var rfoobar = foobar.Reduce();
			rfoobar.RenderToFile(@"..\..\..\foobar.jpg");
			var lfoobar = CharFA<string>.Repeat(foobar, 1, -1, "Accept");
			lfoobar.RenderToFile(@"..\..\..\foobar_loop_nfa.jpg");
			var rlfoobar = lfoobar.Reduce();
			rlfoobar.RenderToFile(@"..\..\..\foobar_loop.jpg");

			var digits = CharFA<string>.Repeat(
				CharFA<string>.Set("0123456789"),
				1, -1
				, "Digits");
			var word = CharFA<string>.Repeat(
				CharFA<string>.Set(new CharRange[] { new CharRange('A', 'Z'), new CharRange('a', 'z') }),
				1, -1
				, "Word");
			var whitespace = CharFA<string>.Repeat(
				CharFA<string>.Set(" \t\r\n\v\f"),
				1, -1
				, "Whitespace");
			var lexer = CharFA<string>.ToLexer(digits, word, whitespace);
			lexer.RenderToFile(@"..\..\..\lexer.jpg");
			var dopt = new CharFA<string>.DotGraphOptions();
			dopt.DebugSourceNfa = lexer;
			var dlexer = lexer.ToDfa();
			dlexer.RenderToFile(@"..\..\..\dlexer.jpg", dopt
				);
			dlexer.RenderToFile(@"..\..\..\dlexer2.jpg");
			var dom = RegexExpression.Parse("(ABC|DEF)+");
			var fa = dom.ToFA("Accept");
			fa.RenderToFile(@"..\..\..\ABCorDEFloop.jpg");

		}
		internal static int Lex(RE.ParseContext context)
		{
			context.EnsureStarted();
			// q0
			if (((context.Current >= '0')
						&& (context.Current <= '9')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q1;
			}
			if ((((context.Current >= 'A')
						&& (context.Current <= 'Z'))
						|| ((context.Current >= 'a')
						&& (context.Current <= 'z'))))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q2;
			}
			if (((((context.Current == '\t')
						|| ((context.Current >= '\n')
						&& (context.Current <= '')))
						|| (context.Current == '\r'))
						|| (context.Current == ' ')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q3;
			}
			goto error;
		q1:
			if (((context.Current >= '0')
						&& (context.Current <= '9')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q1;
			}
			return 0;
		q2:
			if ((((context.Current >= 'A')
						&& (context.Current <= 'Z'))
						|| ((context.Current >= 'a')
						&& (context.Current <= 'z'))))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q2;
			}
			return 1;
		q3:
			if (((((context.Current == '\t')
						|| ((context.Current >= '\n')
						&& (context.Current <= '')))
						|| (context.Current == '\r'))
						|| (context.Current == ' ')))
			{
				context.CaptureCurrent();
				context.Advance();
				goto q3;
			}
			return 2;
		error:
			context.CaptureCurrent();
			context.Advance();
			return 3;
		}
		internal static RE.CharFAMatch Match(RE.ParseContext context)
		{
			context.EnsureStarted();
			int line = context.Line;
			int column = context.Column;
			long position = context.Position;
			int l = context.CaptureBuffer.Length;
			bool success = false;
			for (//
			; ((false == success)
						&& (-1 != context.Current)); //
			)
			{
				// q0
				if ((((context.Current >= 'A')
							&& (context.Current <= 'Z'))
							|| ((context.Current >= 'a')
							&& (context.Current <= 'z'))))
				{
					context.CaptureCurrent();
					context.Advance();
					goto q1;
				}
				goto error;
			q1:
				if ((((context.Current >= 'A')
							&& (context.Current <= 'Z'))
							|| ((context.Current >= 'a')
							&& (context.Current <= 'z'))))
				{
					context.CaptureCurrent();
					context.Advance();
					goto q1;
				}
				success = true;
				goto done;
			error:
				success = false;
				context.Advance();
			done:
				if ((false == success))
				{
					line = context.Line;
					column = context.Column;
					position = context.Position;
					l = context.CaptureBuffer.Length;
				}
			}
			if (success)
			{
				return new RE.CharFAMatch(line, column, position, context.GetCapture(l));
			}
			return null;
		}
	}
}
