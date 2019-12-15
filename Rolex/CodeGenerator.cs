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
	using C = CD.CodeDomUtility;
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

		static CodeConstructor _BuildTableCtor(CodeTypeDeclaration target)
		{
			var result =C.Ctor(MemberAttributes.Public);
			foreach (var member in target.Members)
			{
				var field = member as CodeMemberField;
				if (null != field)
				{
					var n = _TitleToCamel(field.Name);
					result.Parameters.Add(C.Param(field.Type, n));
					result.Statements.Add(C.Let(C.FieldRef(C.This, field.Name), C.ArgRef(n)));
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
		public static CodeMemberMethod GenerateLexMethod(string name,IList<string> constSymbolTable,DfaEntry[] dfaTable)
		{
			var result = C.Method(typeof(int), "Lex", MemberAttributes.Family | MemberAttributes.Override);
			result.Statements.Add(C.Var(typeof(char), "current"));
			var otref = C.TypeRef(name);
			var errorSym = C.FieldRef(C.TypeRef("CompiledTokenizerEnumerator"), "ErrorSymbol");
			// we generate labels for each state except maybe the first.
			// we only generate a label for the first state if any of the
			// states (including itself) reference it. This is to prevent
			// a compiler warning in the case of an unreferenced label
			var isRootLoop = false;
			// we also need to see if any states do not accept
			// if they don't we'll have to generate an error condition
			var hasError = false;
			for (var i = 0; i < dfaTable.Length; i++)
			{
				var trns = dfaTable[i].Transitions;
				for (var j = 0; j < trns.Length; j++)
				{
					if (0 == trns[j].Destination)
					{
						isRootLoop = true;
						break;
					}
				}
			}
			var pci = C.VarRef("current");
			var pccr = C.PropRef(C.This,"CurrentInput");
			var pccc=C.Call(C.FieldRef(C.This, "ValueBuffer"), "Append", pci);
			// valid lexers never accept on the initial state, but we still handle it as though it does
			result.Statements.Add(C.IfElse(C.Eq(C.FieldRef(C.TypeRef("CompiledTokenizerEnumerator"), "BeforeBegin"), C.FieldRef(C.This, "State")),
				new CodeStatement[] {
				C.If(C.Not(C.Invoke(C.This, "MoveNextInput")),
					C.Return(-1 == dfaTable[0].AcceptSymbolId ? errorSym :  C.FieldRef(otref,constSymbolTable[dfaTable[0].AcceptSymbolId]))
				),
				C.Let(C.FieldRef(C.This,"State"),C.FieldRef(C.TypeRef("CompiledTokenizerEnumerator"),"Enumerating"))
				},
				C.If(C.Or(C.Eq(C.FieldRef(C.This,"State"), C.FieldRef(C.TypeRef("CompiledTokenizerEnumerator"), "InnerFinished")), C.Eq(C.FieldRef(C.This, "State"), C.FieldRef(C.TypeRef("CompiledTokenizerEnumerator"), "AfterEnd"))),
					C.Return(C.FieldRef(C.TypeRef("CompiledTokenizerEnumerator"),"EosSymbol")))
			));
			result.Statements.Add(C.Let(pci, pccr));
			var exprs = new CodeExpressionCollection();
			var stmts = new CodeStatementCollection();

			for (var i = 0; i < dfaTable.Length; i++)
			{
				stmts.Clear();
				var se = dfaTable[i];
				var trns = se.Transitions;
				for (var j = 0; j < trns.Length; j++)
				{
					var cif = new CodeConditionStatement();
					stmts.Add(cif);
					exprs.Clear();

					var trn = trns[j];
					var pr = trn.PackedRanges;
					for (var k = 0; k < pr.Length; k++)
					{
						var first = pr[k];
						++k; // advance an extra place
						var last = pr[k];
						if (first != last)
						{
							exprs.Add(
								new CodeBinaryOperatorExpression(
									new CodeBinaryOperatorExpression(
										pci,
										CodeBinaryOperatorType.GreaterThanOrEqual,
										new CodePrimitiveExpression(first)
										),
									CodeBinaryOperatorType.BooleanAnd,
									new CodeBinaryOperatorExpression(
										pci,
										CodeBinaryOperatorType.LessThanOrEqual,
										new CodePrimitiveExpression(last)
										)
									)
								);
						}
						else
						{
							exprs.Add(
								new CodeBinaryOperatorExpression(
									pci,
									CodeBinaryOperatorType.ValueEquality,
									new CodePrimitiveExpression(first)
									)
								);
						}
					}
					cif.Condition = 1<exprs.Count?C.BinOp(exprs, CodeBinaryOperatorType.BooleanOr):exprs[0];
					var ds = dfaTable[trn.Destination];
					cif.TrueStatements.Add(pccc);
					if (-1 != ds.AcceptSymbolId) {
						cif.TrueStatements.Add(C.If(C.Not(C.Invoke(C.This, "MoveNextInput")),
							C.Return(C.FieldRef(otref,constSymbolTable[ds.AcceptSymbolId]))
							));
						cif.TrueStatements.Add(C.Let(pci, pccr));
					} else
					{
						hasError = true;
						cif.TrueStatements.Add(C.If(C.Not(C.Invoke(C.This, "MoveNextInput")),
							C.Goto("error")
							));
						cif.TrueStatements.Add(C.Let(pci, pccr));
					}
					
					cif.TrueStatements.Add(C.Goto(string.Concat("q", trn.Destination.ToString())));

				}
				if (-1 != se.AcceptSymbolId) // is accepting
					stmts.Add(C.Return(C.FieldRef(otref,constSymbolTable[se.AcceptSymbolId])));
				else
				{
					hasError = true;
					stmts.Add(new CodeGotoStatement("error"));
				}
				if (0 < i || isRootLoop)
				{
					result.Statements.Add(new CodeLabeledStatement(string.Concat("q", i.ToString()), stmts[0]));
					for (int jc = stmts.Count, j = 1; j < jc; ++j)
						result.Statements.Add(stmts[j]);
				}
				else
				{
					result.Statements.Add(new CodeCommentStatement("q0"));
					result.Statements.AddRange(stmts);
				}
			}
			if (hasError)
			{
				result.Statements.Add(new CodeLabeledStatement("error", pccc));
				result.Statements.Add(C.Call(C.This, "MoveNextInput"));
				result.Statements.Add(C.Return(errorSym));
			}
			return result;
		}
		public static CodeMemberMethod GenerateGetBlockEndMethod(string name,IList<string> constSymbolTable, IList<string> blockEnds)
		{
			var result = C.Method(typeof(string), "GetBlockEnd", MemberAttributes.Family | MemberAttributes.Override,C.Param(typeof(int),"symbolId"));
			var otref = C.TypeRef(name);
			for (int ic = blockEnds.Count, i = 0; i < ic; ++i)
			{
				var be = blockEnds[i];
				if(!string.IsNullOrEmpty(be))
				{
					result.Statements.Add(C.If(C.Eq(C.FieldRef(otref, constSymbolTable[i]), C.ArgRef(result.Parameters[0].Name)),
						C.Return(C.Literal(be))
						));
				}
			}
			result.Statements.Add(C.Return(C.Null));
			return result;
		}
		public static CodeMemberMethod GenerateIsHiddenMethod(string name,IList<string> constSymbolTable,IList<int> nodeFlags)
		{
			var result = C.Method(typeof(bool), "IsHidden", MemberAttributes.Family | MemberAttributes.Override, C.Param(typeof(int), "symbolId"));
			var otref = C.TypeRef(name);
			var exprs = new List<CodeExpression>();
			for (int ic = nodeFlags.Count,i=0;i<ic;++i)
			{
				var nf = nodeFlags[i];
				if(0!=(nf&1))
					exprs.Add(C.Eq(C.FieldRef(otref,constSymbolTable[i]), C.ArgRef(result.Parameters[0].Name)));
			}
			switch(exprs.Count)
			{
				case 0:
					result.Statements.Add(C.Return(C.False));
					break;
				case 1:
					result.Statements.Add(C.Return(exprs[0]));
					break;
				default:
					result.Statements.Add(C.Return(C.BinOp(exprs, CodeBinaryOperatorType.BooleanOr)));
					break;
			}
			
			return result;
		}
		public static CodeTypeDeclaration GenerateCompiledTokenizer(string name,IList<string> symbolTable)
		{
			var result = C.Class(name, false);
			result.BaseTypes.Add("CompiledTokenizer");
			var eType = new CodeTypeReference(typeof(IEnumerator<>));
			eType.TypeArguments.Add("Token");
			var iType = new CodeTypeReference(typeof(IEnumerable<char>));

			// generate symbol constants
			for (int ic = symbolTable.Count, i = 0; i < ic; ++i)
			{
				var symbol = symbolTable[i];
				if (null != symbol)
				{
					var s = _MakeSafeName(symbol);
					s = _MakeUniqueMember(result, s);
					var constField = C.Field(
						typeof(int),
						s,
						MemberAttributes.Const | MemberAttributes.Public,
						new CodePrimitiveExpression(i)
					);
					result.Members.Add(constField);
					symbolTable[i] = s;
				}
			}

			var getEnum = C.Method(eType,"GetEnumerator",MemberAttributes.Override | MemberAttributes.Public | MemberAttributes.Override);
			getEnum.Statements.Add(C.Return(C.New(name + "Enumerator", C.Invoke(C.FieldRef(C.This, "Input"), "GetEnumerator"))));
			result.Members.Add(getEnum);

			var ctor = C.Ctor(MemberAttributes.Public, C.Param(iType, "input"));
			ctor.BaseConstructorArgs.Add(C.ArgRef("input"));

			result.Members.Add(ctor);
			return result;
		}
		public static CodeTypeDeclaration GenerateCompiledTokenizerEnumerator(string name,
			IList<string> constSymbolTable,
			DfaEntry[] dfaTable,
			IList<string> blockEnds,
			IList<int> nodeFlags)
		{
			var result = C.Class(name+"Enumerator", false);
			result.IsPartial = true;
			result.BaseTypes.Add("CompiledTokenizerEnumerator");
			// lex method
			var lexMethod = GenerateLexMethod(name,constSymbolTable,dfaTable);
			result.Members.Add(lexMethod);
			// block ends
			var getBlockEndMethod = GenerateGetBlockEndMethod(name,constSymbolTable,blockEnds);
			result.Members.Add(getBlockEndMethod);
			// node flags
			var isHiddenMethod = GenerateIsHiddenMethod(name,constSymbolTable,nodeFlags);
			result.Members.Add(isHiddenMethod);
			// constructor
			var ctor = new CodeConstructor();
			ctor.Attributes = MemberAttributes.Public;
			ctor.Parameters.Add(C.Param(typeof(IEnumerator<char>), "input"));
			ctor.BaseConstructorArgs.Add(C.ArgRef(ctor.Parameters[0].Name));
			
			result.Members.Add(ctor);
			result.CustomAttributes.Add(GeneratedCodeAttribute);
			return result;
		}
		public static void GenerateSymbolConstants(CodeTypeDeclaration target,IList<string> symbolTable)
		{
			/*var e = _MakeUniqueMember(target, "ErrorSymbol");
			var errField = C.Field(typeof(int), e,MemberAttributes.Const | MemberAttributes.Public,C.Literal(_ErrorSymbol));
			target.Members.Add(errField);*/
			// generate symbol constants
			for (int ic = symbolTable.Count, i = 0; i < ic; ++i)
			{
				var symbol = symbolTable[i];
				if (null != symbol)
				{
					var s = _MakeSafeName(symbol);
					s = _MakeUniqueMember(target, s);
					var constField = C.Field(typeof(int), s,MemberAttributes.Const | MemberAttributes.Public,C.Literal(i));
					target.Members.Add(constField);
				}
			}
		}
		public static CodeTypeDeclaration GenerateTableTokenizer(
			string name,
			DfaEntry[] dfaTable,
			IList<string> symbolTable,
			IList<string> blockEnds,
			IList<int> nodeFlags)
		{
			var result = C.Class(name, false);
			result.IsPartial = true;
			result.BaseTypes.Add("TableTokenizer");
			// dfa table
			var dfaTableField = new CodeMemberField(new CodeTypeReference("DfaEntry", 1), "DfaTable");
			dfaTableField.Attributes = MemberAttributes.Static | MemberAttributes.FamilyAndAssembly;
			dfaTableField.InitExpression = GenerateDfaTableInitializer(dfaTable);
			result.Members.Add(dfaTableField);
			// block ends
			var blockEndsField = new CodeMemberField(typeof(string[]), "BlockEnds");
			blockEndsField.Attributes = MemberAttributes.Static | MemberAttributes.FamilyAndAssembly;
			blockEndsField.InitExpression = _GenerateBlockEndsTableInitializer(blockEnds);
			result.Members.Add(blockEndsField);
			// node flags
			var nodeFlagsField = new CodeMemberField(typeof(int[]), "NodeFlags");
			nodeFlagsField.Attributes = MemberAttributes.Static | MemberAttributes.FamilyAndAssembly;
			nodeFlagsField.InitExpression = _GenerateNodeFlagsTableInitializer(nodeFlags);
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
			// generate symbol constants
			for (int ic = symbolTable.Count, i = 0; i < ic; ++i)
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
		static CodeExpression _GenerateBlockEndsTableInitializer(IList<string> blockEnds)
		{
			var result = new CodeArrayCreateExpression(typeof(string));
			for(int ic=blockEnds.Count,i=0;i<ic;++i)
				result.Initializers.Add(new CodePrimitiveExpression(blockEnds[i]));
			return result;
		}
		static CodeExpression _GenerateNodeFlagsTableInitializer(IList<int> nodeFlags)
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
			= new CodeAttributeDeclaration(C.Type(typeof(GeneratedCodeAttribute)), new CodeAttributeArgument(C.Literal("Rolex")), new CodeAttributeArgument(C.Literal(Assembly.GetExecutingAssembly().GetName().Version.ToString())));
	}
}
