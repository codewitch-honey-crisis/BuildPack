using CD;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

namespace DeslangDemo
{
	class Program
	{
		static void Main(string[] args)
		{
			CodeDomVisitor.Visit(Deslanged.Widget, (ctx) =>
			{
				// look for our _payload field
				var f = ctx.Target as CodeMemberField;
				if(null!=f && "_payload"==f.Name)
				{
					// give it some data
					f.InitExpression = CodeDomUtility.Literal(_Hash(DateTime.UtcNow.ToString()));
					// we're done searching
					ctx.Cancel = true;
				}
			});

			Console.WriteLine(CodeDomUtility.ToString(Deslanged.Widget));
		}
		static byte[] _Hash(string text)
		{
			// Create a SHA256   
			using (SHA256 sha256Hash = SHA256.Create())
			{
				return sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(text));
			}
		}
	}
}
