using CD;
using System;
using System.CodeDom;

namespace CodeDomDemo
{
	partial class Program
	{
		static void Demo4(CodeCompileUnit ccu)
		{
			// create one of these lil guys
			var res = new CodeDomResolver();
			// add our code to it
			res.CompileUnits.Add(ccu);
			// give it a chance to build its information over our code
			res.Refresh();
			CodeDomVisitor.Visit(ccu, (ctx) => {
				// for every expression...
				var expr = ctx.Target as CodeExpression;
				if(null!=expr)
				{
					// except method reference expressions...
					var mri = expr as CodeMethodReferenceExpression;
					if (null != mri)
						return;
					// get the expression type
					var type = res.TryGetTypeOfExpression(expr);
					// write it along with the expression itself
					Console.WriteLine(
						"Expression type {0}: {1} is {2}",
						expr.GetType().Name,
						CodeDomUtility.ToString(expr),
						null!=type?CodeDomUtility.ToString(type):"unresolvable");
				}
			});
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			Console.Clear();
		}
	}
}
