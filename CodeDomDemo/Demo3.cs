using CD;
using System;
using System.CodeDom;

namespace CodeDomDemo
{
	partial class Program
	{
		static void Demo3(CodeCompileUnit ccu)
		{
			/// now let's take our code and modify it
			CodeDomVisitor.Visit(ccu, (ctx) => {
				// we're looking for a method invocation
				var mi = ctx.Target as CodeMethodInvokeExpression;
				if (null != mi)
				{
					// ... calling WriteLine
					if ("WriteLine" == mi.Method?.MethodName)
					{
						// replace the passed in expression with "Hello world!"
						mi.Parameters.Clear();
						mi.Parameters.Add(new CodePrimitiveExpression("Hello world!"));
						// done after the first WriteLine so we cancel
						ctx.Cancel = true;
					}
				}
			});
			Console.WriteLine(CodeDomUtility.ToString(ccu));
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			Console.Clear();
		}
	}
}
