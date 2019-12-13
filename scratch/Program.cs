using CD;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;

// just an app I use for debugging
namespace scratch
{
	using C = CodeDomUtility;
	using V = CodeDomVisitor;
	using T = CodeDomVariableTracer;
	using R = CodeDomResolver;
	using B = CodeDomBinder;
	using E = CodeTypeReferenceEqualityComparer;

	class Program
	{
		static void Main(string[] args)
		{
			var ccu = SlangParser.ReadCompileUnitFrom(@"..\..\..\Rolex\Shared\Tokenizer.cs");
			SlangPatcher.Patch(ccu);
			T.Break = true;
			var res = new R();
			res.CompileUnits.Add(ccu);
			res.Refresh();
			var co = SlangPatcher.GetNextUnresolvedElement(ccu);
			if(null!=co)
			{
				
				var scope = res.GetScope(co);
				Console.WriteLine(scope);
				foreach (var decl in T.Trace(scope.Member, scope.Statement))
					Console.WriteLine(C.ToString(decl));

				//Console.WriteLine(C.ToString(co));
			}
			
			var ns = C.GetByName("Rolex", ccu.Namespaces);
			var tt = C.GetByName("TableTokenizerEnumerator", ns.Types);
			var mn= C.GetByName("MoveNext", tt.Members) as CodeMemberMethod;
			var wh = mn.Statements[11] as CodeIterationStatement;
			var det = (wh.Statements[2] as CodeConditionStatement).TrueStatements[2];
			foreach (var decl in T.Trace(mn, det))
				Console.WriteLine(C.ToString(decl));
			
		}
		
	}
}
