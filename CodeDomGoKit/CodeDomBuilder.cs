using System;
using System.CodeDom;
using System.Reflection;

namespace CD
{
	class CodeDomBuilder
	{
		public static CodeParameterDeclarationExpression ParameterDeclarationExpression(
			CodeTypeReference type,
			string name,
			FieldDirection direction,
			CodeAttributeDeclaration[] customAttributes)
		{
			var result = new CodeParameterDeclarationExpression(type, name);
			result.Direction = direction;
			result.CustomAttributes.AddRange(customAttributes);
			return result;
		}
		public static CodeAssignStatement AssignStatement(
			CodeExpression left, 
			CodeExpression right, 
			CodeDirective[] startDirectives, 
			CodeDirective[] endDirectives, 
			CodeLinePragma linePragma)
		{
			var result = new CodeAssignStatement(left, right);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeAttachEventStatement AttachEventStatement(CodeEventReferenceExpression eventRef, CodeExpression listener, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeAttachEventStatement(eventRef, listener);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeCommentStatement CommentStatement(CodeComment comment, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeCommentStatement(comment);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeConditionStatement ConditionStatement(CodeExpression condition, CodeStatement[] trueStatements,CodeStatement[] falseStatements, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeConditionStatement(condition,trueStatements,falseStatements);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeExpressionStatement ExpressionStatement(CodeExpression expression, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeExpressionStatement(expression);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeGotoStatement GotoStatement(string label, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeGotoStatement(label);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeIterationStatement IterationStatement(CodeStatement initStatement,CodeExpression testExpression,CodeStatement incrementStatement, CodeStatement[] statements,CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeIterationStatement(initStatement, testExpression, incrementStatement, statements);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeLabeledStatement LabeledStatement(string label, CodeStatement statement, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeLabeledStatement(label,statement);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeMethodReturnStatement MethodReturnStatement(CodeExpression expression, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeMethodReturnStatement(expression);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeRemoveEventStatement RemoveEventStatement(CodeEventReferenceExpression eventRef,CodeExpression listener, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeRemoveEventStatement(eventRef,listener);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeSnippetStatement SnippetStatement(string value, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeSnippetStatement(value);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeThrowExceptionStatement ThrowExceptionStatement(CodeExpression toThrow, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeThrowExceptionStatement(toThrow);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeTryCatchFinallyStatement TryCatchFinallyStatement(CodeStatement[] tryStatements,CodeCatchClause[] catchClauses,CodeStatement[] finallyStatements, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeTryCatchFinallyStatement(tryStatements, catchClauses, finallyStatements);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeVariableDeclarationStatement VariableDeclarationStatement(CodeTypeReference type,string name,CodeExpression initExpression, CodeDirective[] startDirectives, CodeDirective[] endDirectives, CodeLinePragma linePragma)
		{
			var result = new CodeVariableDeclarationStatement(type, name, initExpression);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeTypeReference TypeReference(string baseType,CodeTypeReferenceOptions options,CodeTypeReference[] typeArguments,CodeTypeReference arrayElementType,int arrayRank)
		{
			var result = new CodeTypeReference(baseType, options);
			result.ArrayElementType = arrayElementType;
			result.ArrayRank = arrayRank;
			result.TypeArguments.AddRange(typeArguments);
			return result;
		}
		public static CodeMemberField MemberField(
			CodeTypeReference type,
			string name,
			CodeExpression initExpression,
			MemberAttributes attributes,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeMemberField(type, name);
			result.InitExpression = initExpression;
			result.Attributes = attributes;
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeMemberEvent MemberEvent(
			CodeTypeReference type,
			string name,
			MemberAttributes attributes,
			CodeTypeReference[] implementationTypes,
			CodeTypeReference privateImplementationType,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeMemberEvent();
			result.Type = type;
			result.Name = name;
			result.Attributes = attributes;
			result.ImplementationTypes.AddRange(implementationTypes);
			result.PrivateImplementationType = privateImplementationType;
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeMemberMethod MemberMethod(
			CodeTypeReference returnType,
			string name,
			MemberAttributes attributes,
			CodeParameterDeclarationExpression[] parameters,
			CodeStatement[] statements,
			CodeTypeReference[] implementationTypes,
			CodeTypeReference privateImplementationType,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeAttributeDeclaration[] returnTypeCustomAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeMemberMethod();
			result.ReturnType = returnType;
			result.Name = name;
			result.Attributes = attributes;
			result.Parameters.AddRange(parameters);
			result.Statements.AddRange(statements);
			result.ImplementationTypes.AddRange(implementationTypes);
			result.PrivateImplementationType = privateImplementationType;
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.ReturnTypeCustomAttributes.AddRange(returnTypeCustomAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeEntryPointMethod EntryPointMethod(
			CodeTypeReference returnType,
			string name,
			MemberAttributes attributes,
			CodeParameterDeclarationExpression[] parameters,
			CodeStatement[] statements,
			CodeTypeReference[] implementationTypes,
			CodeTypeReference privateImplementationType,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeAttributeDeclaration[] returnTypeCustomAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeEntryPointMethod();
			result.ReturnType = returnType;
			result.Name = name;
			result.Attributes = attributes;
			result.Parameters.AddRange(parameters);
			result.Statements.AddRange(statements);
			result.ImplementationTypes.AddRange(implementationTypes);
			result.PrivateImplementationType = privateImplementationType;
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.ReturnTypeCustomAttributes.AddRange(returnTypeCustomAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeConstructor Constructor(
			MemberAttributes attributes,
			CodeParameterDeclarationExpression[] parameters,
			CodeExpression[] chainedConstructorArgs,
			CodeExpression[] baseConstructorArgs,
			CodeStatement[] statements,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeConstructor();
			result.Attributes = attributes;
			result.Parameters.AddRange(parameters);
			result.ChainedConstructorArgs.AddRange(chainedConstructorArgs);
			result.BaseConstructorArgs.AddRange(baseConstructorArgs);
			result.Statements.AddRange(statements);
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeTypeConstructor TypeConstructor(
			MemberAttributes attributes,
			CodeParameterDeclarationExpression[] parameters,
			CodeStatement[] statements,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeTypeConstructor();
			result.Attributes = attributes;
			result.Parameters.AddRange(parameters);
			result.Statements.AddRange(statements);
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeMemberProperty MemberProperty(
			CodeTypeReference type,
			string name,
			MemberAttributes attributes,
			CodeParameterDeclarationExpression[] parameters,
			CodeStatement[] getStatements,
			CodeStatement[] setStatements,
			CodeTypeReference[] implementationTypes,
			CodeTypeReference privateImplementationType,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeMemberProperty();
			result.Type = type;
			result.Name = name;
			result.Attributes = attributes;
			result.Parameters.AddRange(parameters);
			result.GetStatements.AddRange(getStatements);
			result.SetStatements.AddRange(setStatements);
			result.ImplementationTypes.AddRange(implementationTypes);
			result.PrivateImplementationType = privateImplementationType;
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeTypeDeclaration TypeDeclaration(
			string name,
			bool isClass,
			bool isEnum,
			bool isInterface,
			bool isStruct,
			bool isPartial,
			MemberAttributes attributes,
			TypeAttributes typeAttributes,
			CodeTypeParameter[] typeParameters,
			CodeTypeReference[] baseTypes,
			CodeTypeMember[] members,
			CodeCommentStatement[] comments,
			CodeAttributeDeclaration[] customAttributes,
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives,
			CodeLinePragma linePragma)
		{
			var result = new CodeTypeDeclaration(name);
			result.IsClass = isClass;
			result.IsEnum = isEnum;
			result.IsInterface = isInterface;
			result.IsStruct = isStruct;
			result.IsPartial = isPartial;
			result.Attributes = attributes;
			result.TypeAttributes = typeAttributes;
			result.TypeParameters.AddRange(typeParameters);
			result.BaseTypes.AddRange(baseTypes);
			result.Members.AddRange(members);
			result.Comments.AddRange(comments);
			result.CustomAttributes.AddRange(customAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeTypeParameter TypeParameter(
			string name,
			bool hasConstructorConstraint,
			CodeTypeReference[] constraints,
			CodeAttributeDeclaration[] customAttributes
			)
		{
			var result = new CodeTypeParameter(name);
			result.HasConstructorConstraint = hasConstructorConstraint;
			result.Constraints.AddRange(constraints);
			result.CustomAttributes.AddRange(customAttributes);
			return result;
		}
		public static CodeNamespace Namespace(
			string name, 
			CodeNamespaceImport[] imports, 
			CodeTypeDeclaration[] types,
			CodeCommentStatement[] comments)
		{
			var result = new CodeNamespace();
			result.Imports.AddRange(imports);
			result.Types.AddRange(types);
			result.Comments.AddRange(comments);
			return result;
		}
		public static CodeNamespaceImport NamespaceImport(
			string nameSpace,
			CodeLinePragma linePragma)
		{
			var result = new CodeNamespaceImport(nameSpace);
			result.LinePragma = linePragma;
			return result;
		}
		public static CodeCompileUnit CompileUnit(
			string[] referencedAssemblies,
			CodeNamespace[] namespaces,
			CodeAttributeDeclaration[] assemblyCustomAttributes, 
			CodeDirective[] startDirectives,
			CodeDirective[] endDirectives
			)
		{
			var result = new CodeCompileUnit();
			result.ReferencedAssemblies.AddRange(referencedAssemblies);
			result.Namespaces.AddRange(namespaces);
			result.AssemblyCustomAttributes.AddRange(assemblyCustomAttributes);
			result.StartDirectives.AddRange(startDirectives);
			result.EndDirectives.AddRange(endDirectives);
			return result;
		}
	}
}
