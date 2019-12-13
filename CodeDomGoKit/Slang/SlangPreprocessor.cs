using System.CodeDom;
using System.IO;
using System.Text;
using System.Reflection;
using System.CodeDom.Compiler;
using System;

namespace CD
{
	/// <summary>
	/// Preprocesses input using a simplified T4 style syntax
	/// </summary>
#if GOKITLIB
	public 
#endif
	class SlangPreprocessor
	{
		/// <summary>
		/// Preprocesses the input from <paramref name="input"/> and writes the output to <paramref name="output"/>
		/// </summary>
		/// <param name="input">The input source to preprocess</param>
		/// <param name="output">The output target for the post-processed <paramref name="input"/></param>
		public static void Preprocess(TextReader input,TextWriter output)
		{
			Preprocess(input, output, "cs");
		}
		/// <summary>
		/// Preprocesses the input from <paramref name="input"/> and writes the output to <paramref name="output"/>
		/// </summary>
		/// <param name="input">The input source to preprocess</param>
		/// <param name="output">The output target for the post-processed <paramref name="input"/></param>
		/// <param name="lang">The language to use for the T4 code - defaults to C#</param>
		public static void Preprocess(TextReader input, TextWriter output, string lang)
		{
			CompilerErrorCollection errors = null;
			// TODO: Add error handling, even though output codegen errors shouldn't occur with this
			var method = new CodeMemberMethod();
			method.Attributes = MemberAttributes.Public | MemberAttributes.Static;
			method.Name = "Preprocess";
			method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TextWriter), "Response"));
			int cur;
			var more = true;
			while (more)
			{
				var text = _ReadUntilStartContext(input);
				if (0 < text.Length)
				{
					method.Statements.Add(new CodeMethodInvokeExpression(
						new CodeArgumentReferenceExpression("Response"),
						"Write",
						new CodePrimitiveExpression(text)));
				}
				cur = input.Read();
				if (-1 == cur)
					more = false;
				else if ('=' == cur)
				{
					method.Statements.Add(new CodeMethodInvokeExpression(
							new CodeArgumentReferenceExpression("Response"),
							"Write",
							new CodeSnippetExpression(_ReadUntilEndContext(-1, input))));

				} else
					method.Statements.Add(new CodeSnippetStatement(_ReadUntilEndContext(cur, input)));
			}
			method.Statements.Add(new CodeMethodInvokeExpression(new CodeArgumentReferenceExpression("Response"), "Flush"));
			var cls = new CodeTypeDeclaration("Preprocessor");
			cls.TypeAttributes = TypeAttributes.Public;
			cls.IsClass = true;
			cls.Members.Add(method);
			var ns = new CodeNamespace();
			ns.Types.Add(cls);
			var cu = new CodeCompileUnit();
			cu.Namespaces.Add(ns);
			var prov = CodeDomProvider.CreateProvider(lang);
			var opts = new CompilerParameters();
			var outp = prov.CompileAssemblyFromDom(opts, cu);
			var asm = outp.CompiledAssembly;
			var ran = false;
			if (null != asm)
			{
				var t = asm.GetType("Preprocessor");
				var m = t.GetMethod("Preprocess");
				if(null!=m)
				{
					try
					{
						m.Invoke(null, new object[] { output });
						ran = true;
					}
					catch(TargetInvocationException tex)
					{
						throw tex.InnerException;
					}
				}
			}
			if(!ran)
			{
				errors=outp.Errors;
				if (0 < errors.Count)
				{
					CompilerError err = errors[0];
					throw new InvalidOperationException(err.ErrorText);
				}
			}
			
		}
		static string _ReadUntilStartContext(TextReader input)
		{
			int cur=input.Read();
			var sb = new StringBuilder();
			while (true)
			{
				if ('<' == cur)
				{
					cur = input.Read();
					if (-1 == cur)
					{
						sb.Append('<');
						return sb.ToString();
					}
					else if ('#' == cur)
						return sb.ToString();
					sb.Append('<');
				}
				else if (-1 == cur)
					return sb.ToString();
				sb.Append((char)cur);
				cur = input.Read();
			}
		}
		static string _ReadUntilEndContext(int firstChar,TextReader input)
		{
			int cur;
			cur = firstChar;
			if (-1 == firstChar)
				cur = input.Read();
			var sb = new StringBuilder();
			while (true)
			{
				if ('#' == cur)
				{
					cur = input.Read();
					if (-1 == cur)
					{
						sb.Append('#');
						return sb.ToString();
					}
					else if ('>' == cur)
						return sb.ToString();
					sb.Append('>');
				}
				else if (-1 == cur)
					return sb.ToString();
				sb.Append((char)cur);
				cur = input.Read();
			}
		}
	}
}
