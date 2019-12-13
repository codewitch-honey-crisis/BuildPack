// This file handles all of the ugly details of generating our classes
// It's rather long, but most of it generates static code
// See the reference implementation in Tokenizer.cs for the
// code this generates
using System.CodeDom;
using RE;
using System.Collections.Generic;
using System.Text;
using System;
using System.CodeDom.Compiler;
using System.Reflection;

namespace Rolex
{
	using CD = CD.CodeDomUtility;
	static class CodeGenerator
	{
		const int _ErrorSymbol = -1;
		const int _EosSymbol = -2;
		const int _Disposed = -4;
		const int _BeforeBegin = -3;
		const int _AfterEnd = -2;
		const int _InnerFinished = -1;
		const int _Enumerating = 0;
		const int _TabWidth = 4;

		static CodeConstructor _BuildCtor(CodeTypeDeclaration target)
		{
			var result =CD.Ctor(MemberAttributes.Public);
			foreach (var member in target.Members)
			{
				var field = member as CodeMemberField;
				if (null != field)
				{
					var n = _TitleToCamel(field.Name);
					result.Parameters.Add(CD.Param(field.Type, n));
					result.Statements.Add(CD.Let(CD.FieldRef(CD.This, field.Name), CD.ArgRef(n)));
				}
			}
			return result;
		}
		
		static string _MakeSafeName(string name)
		{
			var sb = new StringBuilder();
			if (char.IsDigit(name[0]))
				sb.Append('_');
			for(var i = 0;i<name.Length;++i)
			{
				var ch = name[i];
				if ('_' == ch || char.IsLetterOrDigit(ch))
					sb.Append(ch);
				else
					sb.Append('_');
			}
			return sb.ToString();
		}
		static string _MakeUniqueMember(CodeTypeDeclaration decl,string name)
		{
			var seen = new HashSet<string>(StringComparer.InvariantCultureIgnoreCase);
			for(int ic=decl.Members.Count,i = 0;i<ic;i++)
				seen.Add(decl.Members[i].Name);
			var result = name;
			var suffix = 2;
			while (seen.Contains(result))
			{
				result = string.Concat(name, suffix.ToString());
				++suffix;
			}
			return result;
		}

		public static CodeTypeDeclaration GenerateTableTokenizer(
			string name,
			DfaEntry[] dfaTable,
			IList<string> symbolTable,
			IList<string> blockEnds,
			IList<int> nodeFlags)
		{
			var result = CD.Class(name, false);
			result.IsPartial = true;
			result.BaseTypes.Add("TableTokenizer");
			// generate symbol constants
			for (int ic = symbolTable.Count,i=0;i<ic;++i)
			{
				var symbol = symbolTable[i];
				if (null != symbol)
				{
					var s = _MakeSafeName(symbol);
					s = _MakeUniqueMember(result, s);
					var constField = new CodeMemberField(typeof(int), s);
					constField.Attributes = MemberAttributes.Const | MemberAttributes.Public;
					constField.InitExpression = new CodePrimitiveExpression(i);
					result.Members.Add(constField);
				}
			}
			// dfa table
			var dfaTableField = new CodeMemberField(new CodeTypeReference("DfaEntry", 1), "DfaTable");
			dfaTableField.Attributes = MemberAttributes.Static | MemberAttributes.FamilyAndAssembly;
			dfaTableField.InitExpression = GenerateDfaTableInitializer(dfaTable);
			result.Members.Add(dfaTableField);
			// block ends
			var blockEndsField = new CodeMemberField(typeof(string[]), "BlockEnds");
			blockEndsField.Attributes = MemberAttributes.Static | MemberAttributes.FamilyAndAssembly;
			blockEndsField.InitExpression = GenerateBlockEndsTableInitializer(blockEnds);
			result.Members.Add(blockEndsField);
			// node flags
			var nodeFlagsField = new CodeMemberField(typeof(int[]), "NodeFlags");
			nodeFlagsField.Attributes = MemberAttributes.Static | MemberAttributes.FamilyAndAssembly;
			nodeFlagsField.InitExpression = GenerateNodeFlagsTableInitializer(nodeFlags);
			result.Members.Add(nodeFlagsField);
			// constructor
			var ctor = new CodeConstructor();
			ctor.Attributes = MemberAttributes.Public;
			ctor.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IEnumerable<char>), "input"));
			ctor.BaseConstructorArgs.Add(new CodeFieldReferenceExpression(null, dfaTableField.Name));
			ctor.BaseConstructorArgs.Add(new CodeFieldReferenceExpression(null, blockEndsField.Name));
			ctor.BaseConstructorArgs.Add(new CodeFieldReferenceExpression(null, nodeFlagsField.Name));
			ctor.BaseConstructorArgs.Add(new CodeArgumentReferenceExpression(ctor.Parameters[0].Name));
			result.Members.Add(ctor);
			result.CustomAttributes.Add(GeneratedCodeAttribute);
			return result;
		}
		// we use our own serialization here to avoid the codedom trying to reference the DfaEntry under the wrong namespace
		public static CodeExpression GenerateDfaTableInitializer(DfaEntry[] dfaTable)
		{
			var result = new CodeArrayCreateExpression("DfaEntry");
			for(var i = 0;i<dfaTable.Length;i++)
			{
				var entry = new CodeObjectCreateExpression("DfaEntry");
				var transitions = new CodeArrayCreateExpression("DfaTransitionEntry");
				var de = dfaTable[i];
				var trns = de.Transitions;
				for (var j = 0; j < trns.Length; j++)
				{
					var transition = new CodeObjectCreateExpression(transitions.CreateType);
					var ranges = new CodeArrayCreateExpression(typeof(char));
					var trn = trns[j];
					var rngs = trn.PackedRanges;
					for (var k=0;k<rngs.Length;k++)
						ranges.Initializers.Add(new CodePrimitiveExpression(rngs[k]));
					transition.Parameters.Add(ranges);
					transition.Parameters.Add(new CodePrimitiveExpression(trn.Destination));
					transitions.Initializers.Add(transition);
				}
				entry.Parameters.Add(transitions);
				entry.Parameters.Add(new CodePrimitiveExpression(de.AcceptSymbolId));
				result.Initializers.Add(entry);
			}
			return result;
		}
		public static CodeExpression GenerateBlockEndsTableInitializer(IList<string> blockEnds)
		{
			var result = new CodeArrayCreateExpression(typeof(string));
			for(int ic=blockEnds.Count,i=0;i<ic;++i)
				result.Initializers.Add(new CodePrimitiveExpression(blockEnds[i]));
			return result;
		}
		public static CodeExpression GenerateNodeFlagsTableInitializer(IList<int> nodeFlags)
		{
			var result = new CodeArrayCreateExpression(typeof(int));
			for (int ic = nodeFlags.Count, i = 0; i < ic; ++i)
				result.Initializers.Add(new CodePrimitiveExpression(nodeFlags[i]));
			return result;
		}
		// turns "FooBar" into "fooBar"
		static string _TitleToCamel(string name) {
			return string.Concat(char.ToLowerInvariant(name[0]), name.Substring(1));
		}
		public static readonly CodeAttributeDeclaration GeneratedCodeAttribute
			= new CodeAttributeDeclaration(CD.Type(typeof(GeneratedCodeAttribute)), new CodeAttributeArgument(CD.Literal("Rolex")), new CodeAttributeArgument(CD.Literal(Assembly.GetExecutingAssembly().GetName().Version.ToString())));
	}
}
