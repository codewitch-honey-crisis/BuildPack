using System;
using System.CodeDom;
using CD;
namespace SlangDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			// note that in the real world cases, you'd need to use SlangPatcher.Patch()
			// on whole compile units to get proper codedom objects back. This method is
			// simply "close enough for government work" and may not work for languages
			// other than VB
			while(true)
			{
				Console.Write("Slang>");
				var s= Console.ReadLine();
				if (null != s) s = s.Trim();
				if (string.IsNullOrEmpty(s))
					break;
				var isStatement = s.EndsWith(";") || s.EndsWith("}");
				CodeObject co = null;
				try
				{
					if (isStatement)
						co = SlangParser.ParseStatement(s, true);
					else
						co = SlangParser.ParseExpression(s);
				}
				catch(SlangSyntaxException ex)
				{
					Console.WriteLine("Error: " + ex.Message);
				}
				if (null!=co)
				{
					
					var ccu = _RootCode(co);
					try
					{
						SlangPatcher.Patch(ccu);
					}
					catch(Exception ex)
					{
						Console.WriteLine("Warning: Error resolving code - " + ex.Message);
					}
					
					var tc = new CodeDomTypeConverter();
					var item = (ccu.Namespaces[0].Types[0].Members[0] as CodeMemberMethod).Statements[0];

					if (!isStatement)
					{
						co = item;
						var es = item as CodeExpressionStatement;
						if (null != es)
							co = es.Expression;
					}
					else co = item;

					s = CodeDomUtility.ToString(co);
					s = s.Trim();
					Console.Write("C#: ");
					Console.WriteLine(s);
					s = CodeDomUtility.ToString(co, "vb");
					s = s.Trim();
					Console.Write("VB: ");
					Console.WriteLine(s);

					s = CodeDomUtility.ToString(CodeDomUtility.Literal(co, tc));
					s = s.Trim();
					
					Console.Write("CodeDom: ");
					Console.WriteLine(s);
					if (null != SlangPatcher.GetNextUnresolvedElement(ccu))
					{
						Console.WriteLine("Warning: Not all of the code could be resolved.");
					}
				}
			}
		}
		static CodeCompileUnit _RootCode(CodeObject obj)
		{
			var expr = obj as CodeExpression;
			if (null != expr) return _RootCode(expr);
			return _RootCode(obj as CodeStatement);
		}
		static CodeCompileUnit _RootCode(CodeExpression expr)
		{
			return _RootCode(new CodeExpressionStatement(expr));
		}
		static CodeCompileUnit _RootCode(CodeStatement stmt)
		{
			var main = new CodeEntryPointMethod();
			main.Statements.Add(stmt);
			var type = new CodeTypeDeclaration("Program");
			type.Members.Add(main);
			type.IsClass = true;
			var ns = new CodeNamespace();
			ns.Types.Add(type);
			ns.Imports.Add(new CodeNamespaceImport("System"));
			var result = new CodeCompileUnit();
			result.Namespaces.Add(ns);
			return result;
			
		}
	}
}
