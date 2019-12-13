using System;
using System.CodeDom;
using System.Reflection;
using CD;
namespace CodeDomDemo
{
	partial class Program
	{
		static  void Demo5(CodeCompileUnit ccu)
		{
			// once again, we need one of these
			var res = new CodeDomResolver();
			res.CompileUnits.Add(ccu);
			res.Refresh();
			// we happen to know Program is the 1st type in the 2nd namespace*
			var prg = ccu.Namespaces[1].Types[0];
			// we need the scope where we're at
			var scope = res.GetScope(prg);
			// because our binder attaches to it
			var binder = new CodeDomBinder(scope);
			// now get all the methods with the specified name and flags
			var members = binder.GetMethodGroup(prg, "TestOverload",BindingFlags.Public | BindingFlags.Static);
			Console.WriteLine("There are {0} TestOverload method overloads.", members.Length);
			// try selecting one that takes a single string parameter
			var argTypes1 = new CodeTypeReference[] { new CodeTypeReference(typeof(string)) };
			var m = binder.SelectMethod(BindingFlags.Public | BindingFlags.Static, members, argTypes1, null);
			if (null != m)
			{
				Console.WriteLine("Select TestOverload(string) returned:");
				_DumpMethod(m);
			}
			else
				Console.WriteLine("Unable to bind to method");
			// try selecting one that takes a single it parameter
			var argTypes2 = new CodeTypeReference[] { new CodeTypeReference(typeof(int)) };
			m = binder.SelectMethod(BindingFlags.Public | BindingFlags.Static, members, argTypes2, null);
			if (null != m)
			{
				Console.WriteLine("Select TestOverload(int) returned:");
				_DumpMethod(m);
			}
			else
				Console.WriteLine("Unable to bind to method");
			Console.WriteLine("Press any key...");
			Console.ReadKey();
			Console.Clear();
		}
		static void _DumpMethod(object m)
		{
			// dump the method based on its type
			// can either be a MethodInfo or a
			// CodeMemberMethod
			var mi = m as MethodInfo;
			if(null!=mi)
			{
				Console.WriteLine("{0}.{1}", mi.DeclaringType.Name, mi.Name);
				return;
			}
			var mm = m as CodeMemberMethod;
			Console.WriteLine(CodeDomUtility.ToString(mm));
		}
	}
}
