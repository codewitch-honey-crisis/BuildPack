using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using System.IO;
using System.Reflection;
using System.Text;
namespace CD
{
	/// <summary>
	/// Helper class for building CodeDOM trees
	/// </summary>
#if GOKITLIB
	public 
#endif
	static class CodeDomUtility
	{
		/// <summary>
		/// Returns <see cref="CodeThisReferenceExpression"/>
		/// </summary>
		public static CodeThisReferenceExpression This { get; } = new CodeThisReferenceExpression();
		/// <summary>
		/// Returns <see cref="CodePrimitiveExpression"/>(true)
		/// </summary>
		public static CodePrimitiveExpression True { get; } = new CodePrimitiveExpression(true);
		/// <summary>
		/// Returns <see cref="CodePrimitiveExpression"/>(false)
		/// </summary>
		public static CodePrimitiveExpression False { get; } = new CodePrimitiveExpression(false);
		/// <summary>
		/// Returns <see cref="CodePrimitiveExpression"/>(0)
		/// </summary>
		public static CodePrimitiveExpression Zero { get; } = new CodePrimitiveExpression(0);
		/// <summary>
		/// Returns <see cref="CodePrimitiveExpression"/>(1)
		/// </summary>
		public static CodePrimitiveExpression One { get; } = new CodePrimitiveExpression(1);
		/// <summary>
		/// Returns <see cref="CodePrimitiveExpression"/>(-1)
		/// </summary>
		public static CodePrimitiveExpression NegOne { get; } = new CodePrimitiveExpression(-1);
		/// <summary>
		/// Returns <see cref="CodePrimitiveExpression"/>(null)
		/// </summary>
		public static CodePrimitiveExpression Null { get; } = new CodePrimitiveExpression(null);
		/// <summary>
		/// Returns <see cref="CodeConditionStatement"/> with the specified parameters
		/// </summary>
		/// <param name="cnd">The condition</param>
		/// <param name="trueStatements">Execute if <paramref name="cnd"/> is true</param>
		/// <returns>A <see cref="CodeConditionStatement"/> with the specifed parameters</returns>
		public static CodeConditionStatement If(CodeExpression cnd, params CodeStatement[] trueStatements)
			=> new CodeConditionStatement(cnd, trueStatements);
		/// <summary>
		/// Returns <see cref="CodeConditionStatement"/> with the specified parameters
		/// </summary>
		/// <param name="cnd">The condition</param>
		/// <param name="trueStatements">Execute if <paramref name="cnd"/> is true</param>
		/// <param name="falseStatements">Execute if <paramref name="cnd"/> is false</param>
		/// <returns>A <see cref="CodeConditionStatement"/> with the specifed parameters</returns>
		public static CodeConditionStatement IfElse(CodeExpression cnd, CodeStatementCollection trueStatements, params CodeStatement[] falseStatements)
		{
			var result = new CodeConditionStatement(cnd);
			result.TrueStatements.AddRange(trueStatements);
			result.FalseStatements.AddRange(falseStatements);
			return result;
		}
		/// <summary>
		/// Returns <see cref="CodeConditionStatement"/> with the specified parameters
		/// </summary>
		/// <param name="cnd">The condition</param>
		/// <param name="trueStatements">Execute if <paramref name="cnd"/> is true</param>
		/// <param name="falseStatements">Execute if <paramref name="cnd"/> is false</param>
		/// <returns>A <see cref="CodeConditionStatement"/> with the specifed parameters</returns>
		public static CodeConditionStatement IfElse(CodeExpression cnd, IEnumerable<CodeStatement> trueStatements, params CodeStatement[] falseStatements)
		{
			var result = new CodeConditionStatement(cnd);
			foreach (var stmt in trueStatements)
				result.TrueStatements.Add(stmt);
			result.FalseStatements.AddRange(falseStatements);
			return result;
		}
		/// <summary>
		/// Returns a <see cref="CodeFieldReferenceExpression"/> with the specified parameters
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="name">The name of the member</param>
		/// <returns>A <see cref="CodeFieldReferenceExpression"/> with the specified parameters</returns>
		public static CodeFieldReferenceExpression FieldRef(CodeExpression target, string name)
			=> new CodeFieldReferenceExpression(target, name);
		/// <summary>
		/// Returns a <see cref="CodePropertyReferenceExpression"/> with the specified parameters
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="name">The name of the member</param>
		/// <returns>A <see cref="CodePropertyReferenceExpression"/> with the specified parameters</returns>
		public static CodePropertyReferenceExpression PropRef(CodeExpression target, string name)
			=> new CodePropertyReferenceExpression(target, name);
		/// <summary>
		/// Returns a <see cref="CodeMethodReferenceExpression"/> with the specified parameters
		/// </summary>
		/// <param name="target">The target object</param>
		/// <param name="name">The name of the member</param>
		/// <returns>A <see cref="CodeMethodReferenceExpression"/> with the specified parameters</returns>
		public static CodeMethodReferenceExpression MethRef(CodeExpression target, string name)
			=> new CodeMethodReferenceExpression(target, name);
		/// <summary>
		/// Returns a <see cref="CodeVariableReferenceExpression"/> with the specified name
		/// </summary>
		/// <param name="name">The name of the variable</param>
		/// <returns>A <see cref="CodeVariableReferenceExpression"/> with the specified name</returns>
		public static CodeVariableReferenceExpression VarRef(string name)
			=> new CodeVariableReferenceExpression(name);
		/// <summary>
		/// Returns a <see cref="CodeArgumentReferenceExpression"/> with the specified name
		/// </summary>
		/// <param name="name">The name of the argument</param>
		/// <returns>A <see cref="CodeArgumentReferenceExpression"/> with the specified name</returns>
		public static CodeArgumentReferenceExpression ArgRef(string name)
			=> new CodeArgumentReferenceExpression(name);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified generic parameter
		/// </summary>
		/// <param name="typeParam">The name of type parameter</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified generic parameter</returns>
		public static CodeTypeReference Type(CodeTypeParameter typeParam)
			=> new CodeTypeReference(typeParam);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified type
		/// </summary>
		/// <param name="typeName">The name of type</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified type</returns>
		public static CodeTypeReference Type(string typeName)
			=> new CodeTypeReference(typeName);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified type
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified type</returns>
		public static CodeTypeReference Type(Type type)
			=> new CodeTypeReference(type);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified type and rank
		/// </summary>
		/// <param name="arrayType">The element type of the array</param>
		/// <param name="arrayRank">The number of dimensions in the array</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified type and rank</returns>
		public static CodeTypeReference Type(CodeTypeReference arrayType, int arrayRank)
			=> new CodeTypeReference(arrayType, arrayRank);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified type and rank
		/// </summary>
		/// <param name="arrayType">The name of the element type of the array</param>
		/// <param name="arrayRank">The number of dimensions in the array</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified type and rank</returns>
		public static CodeTypeReference Type(string arrayType, int arrayRank)
			=> new CodeTypeReference(arrayType, arrayRank);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified type and options
		/// </summary>
		/// <param name="typeName">The type name</param>
		/// <param name="options">The options</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified type and options</returns>
		public static CodeTypeReference Type(string typeName, CodeTypeReferenceOptions options)
			=> new CodeTypeReference(typeName, options);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified type and options
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="options">The options</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified type and options</returns>
		public static CodeTypeReference Type(Type type, CodeTypeReferenceOptions options)
			=> new CodeTypeReference(type, options);
		/// <summary>
		/// Returns a <see cref="CodeTypeReference"/> with the specified type and type arguments
		/// </summary>
		/// <param name="typeName">The name of the type</param>
		/// <param name="typeArguments">The type arguments</param>
		/// <returns>A <see cref="CodeTypeReference"/> with the specified type and type arguments</returns>
		public static CodeTypeReference Type(string typeName, params CodeTypeReference[] typeArguments)
			=> new CodeTypeReference(typeName, typeArguments);
		/// <summary>
		/// Returns a <see cref="CodeTypeReferenceExpression"/> with the specified type
		/// </summary>
		/// <param name="typeRef">The <see cref="CodeTypeReference"/></param>
		/// <returns>A <see cref="CodeTypeReferenceExpression"/> with the specified type</returns>
		public static CodeTypeReferenceExpression TypeRef(CodeTypeReference typeRef)
			=> new CodeTypeReferenceExpression(typeRef);
		/// <summary>
		/// Returns a <see cref="CodeTypeReferenceExpression"/> with the specified type
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="CodeTypeReferenceExpression"/> with the specified type</returns>
		public static CodeTypeReferenceExpression TypeRef(Type type)
			=> new CodeTypeReferenceExpression(type);
		/// <summary>
		/// Returns a <see cref="CodeTypeReferenceExpression"/> with the specified type
		/// </summary>
		/// <param name="typeName">The type name</param>
		/// <returns>A <see cref="CodeTypeReferenceExpression"/> with the specified type</returns>
		public static CodeTypeReferenceExpression TypeRef(string typeName)
			=> new CodeTypeReferenceExpression(typeName);
		/// <summary>
		/// Returns a <see cref="CodeTypeOfExpression"/> with the specified type
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="CodeTypeOfExpression"/> with the specified type</returns>
		public static CodeTypeOfExpression TypeOf(CodeTypeReference type)
			=> new CodeTypeOfExpression(type);
		/// <summary>
		/// Returns a <see cref="CodeTypeOfExpression"/> with the specified type
		/// </summary>
		/// <param name="typeName">The type name</param>
		/// <returns>A <see cref="CodeTypeOfExpression"/> with the specified type</returns>
		public static CodeTypeOfExpression TypeOf(string typeName)
			=> new CodeTypeOfExpression(typeName);
		/// <summary>
		/// Returns a <see cref="CodeTypeOfExpression"/> with the specified type
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="CodeTypeOfExpression"/> with the specified type</returns>
		public static CodeTypeOfExpression TypeOf(Type type)
			=> new CodeTypeOfExpression(type);


		/// <summary>
		/// Creates one or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters
		/// </summary>
		/// <param name="left">The left hand side expression</param>
		/// <param name="type">A <see cref="CodeBinaryOperatorType"/> value indicating the type of operation</param>
		/// <param name="right">The first right hand side expression</param>
		/// <param name="rightN">The remainder right hand side expressions</param>
		/// <returns>One or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters</returns>
		public static CodeBinaryOperatorExpression BinOp(CodeExpression left, CodeBinaryOperatorType type, CodeExpression right, params CodeExpression[] rightN)
		{
			var exprs = new CodeExpressionCollection();
			exprs.Add(left);
			exprs.Add(right);
			exprs.AddRange(rightN);
			// we can get away with the cast only because we guarantee at least two arguments
			return _MakeBinOps(exprs, type) as CodeBinaryOperatorExpression;
		}
		/// <summary>
		/// Creates one or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters
		/// </summary>
		/// <param name="exprs">The expressions</param>
		/// <param name="type">A <see cref="CodeBinaryOperatorType"/> value indicating the type of operation</param>
		/// <returns>One or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters</returns>
		public static CodeBinaryOperatorExpression BinOp(IEnumerable<CodeExpression> exprs, CodeBinaryOperatorType type)
		{
			CodeExpression left = null;
			CodeExpression right = null;
			var rightN = new List<CodeExpression>();
			foreach (var e in exprs)
			{
				if (null == left)
					left = e;
				else if (null == right)
					right = e;
				else
					rightN.Add(e);
			}
			if (null == left || null == right)
				throw new ArgumentException("There must be at least two expressions", nameof(exprs));
			return BinOp(left, type, right, rightN.ToArray());
		}
		/// <summary>
		/// Creates one or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters
		/// </summary>
		/// <param name="exprs">The expressions</param>
		/// <param name="type">A <see cref="CodeBinaryOperatorType"/> value indicating the type of operation</param>
		/// <returns>One or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters</returns>
		public static CodeBinaryOperatorExpression BinOp(CodeExpressionCollection exprs, CodeBinaryOperatorType type)
		{
			CodeExpression left = null;
			CodeExpression right = null;
			var rightN = new List<CodeExpression>();
			foreach (CodeExpression e in exprs)
			{
				if (null == left)
					left = e;
				else if (null == right)
					right = e;
				else
					rightN.Add(e);
			}
			if (null == right)
				throw new ArgumentException("There must be at least two expressions", nameof(exprs));
			return BinOp(left, type, right, rightN.ToArray());
		}
		/// <summary>
		/// Creates a simple or complex literal expression value based on <paramref name="value"/>
		/// </summary>
		/// <param name="value">The instance to serialize to code</param>
		/// <param name="typeConverter">An optional type converter to use. If specified, the same type converter will be used for all elements and subelements of <paramref name="value"/>.</param>
		/// <returns>A <see cref="CodeExpression"/> that can be used to instantiate <paramref name="value"/></returns>
		public static CodeExpression Literal(object value,TypeConverter typeConverter=null)
		{
			return _Serialize(value,typeConverter);
		}
		/// <summary>
		/// Creates a <see cref="CodeCastExpression"/> based on the target type and expression
		/// </summary>
		/// <param name="type">The type to cast to</param>
		/// <param name="target">The expression to cast</param>
		/// <returns>A <see cref="CodeCastExpression"/> based on the target type and expression</returns>
		public static CodeCastExpression Cast(CodeTypeReference type, CodeExpression target)
			=> new CodeCastExpression(type, target);
		/// <summary>
		/// Creates a <see cref="CodeCastExpression"/> based on the target type and expression
		/// </summary>
		/// <param name="typeName">The type to cast to</param>
		/// <param name="target">The expression to cast</param>
		/// <returns>A <see cref="CodeCastExpression"/> based on the target type and expression</returns>
		public static CodeCastExpression Cast(string typeName, CodeExpression target)
			=> new CodeCastExpression(typeName, target);
		/// <summary>
		/// Creates a <see cref="CodeCastExpression"/> based on the target type and expression
		/// </summary>
		/// <param name="type">The type to cast to</param>
		/// <param name="target">The expression to cast</param>
		/// <returns>A <see cref="CodeCastExpression"/> based on the target type and expression</returns>
		public static CodeCastExpression Cast(Type type, CodeExpression target)
			=> new CodeCastExpression(type, target);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> adding two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> adding two or more expressions</returns>
		public static CodeBinaryOperatorExpression Add(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.Add, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> assigning one or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> assigning or more expressions</returns>
		public static CodeBinaryOperatorExpression AssignExpr(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.Assign, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a bitwise and on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a bitwise and on two or more expressions</returns>
		public static CodeBinaryOperatorExpression BitwiseAnd(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.BitwiseAnd, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a bitwise or on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a bitwise or on two or more expressions</returns>
		public static CodeBinaryOperatorExpression BitwiseOr(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.BitwiseOr, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an and on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an and on two or more expressions</returns>
		public static CodeBinaryOperatorExpression And(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.BooleanAnd, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an or on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an or on two or more expressions</returns>
		public static CodeBinaryOperatorExpression Or(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.BooleanOr, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> dividing two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> dividing two or more expressions</returns>
		public static CodeBinaryOperatorExpression Div(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.Divide, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a greater than comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a greater than comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression Gt(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.GreaterThan, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a greater than or equal comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a greater than or equal comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression Gte(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.GreaterThanOrEqual, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a value equality comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a value equality comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression Eq(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.ValueEquality, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a value inequality comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a value inequality comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression NotEq(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> Eq(False, BinOp(left, CodeBinaryOperatorType.ValueEquality, right, rightN));
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a less than comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a less than comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression Lt(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.LessThan, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a less than or equal comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a less than or equal comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression Lte(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.LessThanOrEqual, right, rightN);
				/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a modulo on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a modulo on two or more expressions</returns>
		public static CodeBinaryOperatorExpression Mod(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.Modulus, right, rightN);
				/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> multiplying two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> multiplying two or more expressions</returns>
		public static CodeBinaryOperatorExpression Mul(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.Multiply, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> subtracting two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> subtracting two or more expressions</returns>
		public static CodeBinaryOperatorExpression Sub(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.Subtract, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an identity equality comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an identity equality comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression IdentEq(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.IdentityEquality, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an identity inequality comparison on two or more expressions
		/// </summary>
		/// <param name="left">The left hand side</param>
		/// <param name="right">The first right hand side</param>
		/// <param name="rightN">The remainder right hand sides</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an identity inequality comparison on two or more expressions</returns>
		public static CodeBinaryOperatorExpression IdentNotEq(CodeExpression left, CodeExpression right, params CodeExpression[] rightN)
			=> BinOp(left, CodeBinaryOperatorType.IdentityInequality, right, rightN);
		/// <summary>
		/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a boolean not on the target expression
		/// </summary>
		/// <param name="target">The target expression</param>
		/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a boolean not on the target expression</returns>
		public static CodeBinaryOperatorExpression Not(CodeExpression target)
			=> new CodeBinaryOperatorExpression(False, CodeBinaryOperatorType.ValueEquality, target);
		/// <summary>
		/// Returns a <see cref="CodeAssignStatement"/> assigning the target to the specified value
		/// </summary>
		/// <param name="target">The target to assign</param>
		/// <param name="value">The value to assign to</param>
		/// <returns>A <see cref="CodeAssignStatement"/> assigning the target to the specified value</returns>
		public static CodeAssignStatement Let(CodeExpression target, CodeExpression value)
			=> new CodeAssignStatement(target, value);
		/// <summary>
		/// Creates a <see cref="CodeMethodReturnStatement"/> with the optionally specified expression
		/// </summary>
		/// <param name="target">The target expression to return, or null for no return value</param>
		/// <returns>A <see cref="CodeMethodReturnStatement"/> with the optionally specified expression</returns>
		public static CodeMethodReturnStatement Return(CodeExpression target = null)
			=> null != target ? new CodeMethodReturnStatement(target) : new CodeMethodReturnStatement();
		/// <summary>
		/// Creates a <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression
		/// </summary>
		/// <param name="method">The method to invoke</param>
		/// <param name="arguments">The arguments to invoke with</param>
		/// <returns>A <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression</returns>
		public static CodeMethodInvokeExpression Invoke(CodeMethodReferenceExpression method, params CodeExpression[] arguments)
			=> new CodeMethodInvokeExpression(method, arguments);
		/// <summary>
		/// Creates a <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression
		/// </summary>
		/// <param name="target">The target object or type where the method resides</param>
		/// <param name="name">The name of the method</param>
		/// <param name="arguments">The arguments to invoke with</param>
		/// <returns>A <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression</returns>
		public static CodeMethodInvokeExpression Invoke(CodeExpression target, string name, params CodeExpression[] arguments)
			=> new CodeMethodInvokeExpression(MethRef(target, name), arguments);
		/// <summary>
		/// Creates a <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.
		/// </summary>
		/// <param name="method">The method to invoke</param>
		/// <param name="arguments">The arguments to invoke with</param>
		/// <returns>A <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.</returns>
		public static CodeExpressionStatement Call(CodeMethodReferenceExpression method, params CodeExpression[] arguments)
			=> new CodeExpressionStatement(Invoke(method, arguments));
		/// <summary>
		/// Creates a <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.
		/// </summary>
		/// <param name="target">The target object or type where the method resides</param>
		/// <param name="name">The name of the method</param>
		/// <param name="arguments">The arguments to invoke with</param>
		/// <returns>A <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.</returns>
		public static CodeExpressionStatement Call(CodeExpression target, string name, params CodeExpression[] arguments)
			=> new CodeExpressionStatement(Invoke(MethRef(target, name), arguments));
		/// <summary>
		/// Creates a <see cref="CodeDefaultValueExpression"/> with the specified type
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="CodeDefaultValueExpression"/> with the specified type</returns>
		public static CodeDefaultValueExpression Default(CodeTypeReference type)
			=> new CodeDefaultValueExpression(type);
		/// <summary>
		/// Creates a <see cref="CodeDefaultValueExpression"/> with the specified type
		/// </summary>
		/// <param name="typeName">The type</param>
		/// <returns>A <see cref="CodeDefaultValueExpression"/> with the specified type</returns>
		public static CodeDefaultValueExpression Default(string typeName)
			=> new CodeDefaultValueExpression(Type(typeName));
		/// <summary>
		/// Creates a <see cref="CodeDefaultValueExpression"/> with the specified type
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>A <see cref="CodeDefaultValueExpression"/> with the specified type</returns>
		public static CodeDefaultValueExpression Default(Type type)
			=> new CodeDefaultValueExpression(Type(type));
		/// <summary>
		/// Creates a <see cref="CodeObjectCreateExpression"/> with the specified type and arguments
		/// </summary>
		/// <param name="type">The type to create</param>
		/// <param name="arguments">The arguments to pass to the constructor</param>
		/// <returns>A <see cref="CodeObjectCreateExpression"/> with the specified type and arguments</returns>
		public static CodeObjectCreateExpression New(CodeTypeReference type, params CodeExpression[] arguments)
			=> new CodeObjectCreateExpression(type, arguments);
		/// <summary>
		/// Creates a <see cref="CodeObjectCreateExpression"/> with the specified type and arguments
		/// </summary>
		/// <param name="type">The type to create</param>
		/// <param name="arguments">The arguments to pass to the constructor</param>
		/// <returns>A <see cref="CodeObjectCreateExpression"/> with the specified type and arguments</returns>
		public static CodeObjectCreateExpression New(Type type, params CodeExpression[] arguments)
			=> new CodeObjectCreateExpression(type, arguments);
		/// <summary>
		/// Creates a <see cref="CodeObjectCreateExpression"/> with the specified type and arguments
		/// </summary>
		/// <param name="typeName">The type to create</param>
		/// <param name="arguments">The arguments to pass to the constructor</param>
		/// <returns>A <see cref="CodeObjectCreateExpression"/> with the specified type and arguments</returns>
		public static CodeObjectCreateExpression New(string typeName, params CodeExpression[] arguments)
			=> new CodeObjectCreateExpression(typeName, arguments);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers
		/// </summary>
		/// <param name="arrayType">The element type of the array</param>
		/// <param name="initializers">The initializers to create the array with</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers</returns>
		public static CodeArrayCreateExpression NewArr(CodeTypeReference arrayType, params CodeExpression[] initializers)
			=> new CodeArrayCreateExpression(arrayType, initializers);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers
		/// </summary>
		/// <param name="arrayTypeName">The element type of the array</param>
		/// <param name="initializers">The initializers to create the array with</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers</returns>
		public static CodeArrayCreateExpression NewArr(string arrayTypeName, params CodeExpression[] initializers)
			=> new CodeArrayCreateExpression(arrayTypeName, initializers);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers
		/// </summary>
		/// <param name="arrayType">The element type of the array</param>
		/// <param name="initializers">The initializers to create the array with</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers</returns>
		public static CodeArrayCreateExpression NewArr(Type arrayType, params CodeExpression[] initializers)
			=> new CodeArrayCreateExpression(arrayType, initializers);

		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
		/// </summary>
		/// <param name="arrayType">The element type of the array</param>
		/// <param name="size">The size of the array</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
		public static CodeArrayCreateExpression NewArr(CodeTypeReference arrayType, CodeExpression size)
			=> new CodeArrayCreateExpression(arrayType, size);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
		/// </summary>
		/// <param name="arrayType">The element type of the array</param>
		/// <param name="size">The size of the array</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
		public static CodeArrayCreateExpression NewArr(CodeTypeReference arrayType, int size)
			=> new CodeArrayCreateExpression(arrayType, size);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
		/// </summary>
		/// <param name="arrayTypeName">The element type of the array</param>
		/// <param name="size">The size of the array</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
		public static CodeArrayCreateExpression NewArr(string arrayTypeName, CodeExpression size)
			=> new CodeArrayCreateExpression(arrayTypeName, size);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
		/// </summary>
		/// <param name="arrayTypeName">The element type of the array</param>
		/// <param name="size">The size of the array</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
		public static CodeArrayCreateExpression NewArr(string arrayTypeName, int size)
			=> new CodeArrayCreateExpression(arrayTypeName, size);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
		/// </summary>
		/// <param name="arrayType">The element type of the array</param>
		/// <param name="size">The size of the array</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
		public static CodeArrayCreateExpression NewArr(Type arrayType, CodeExpression size)
			=> new CodeArrayCreateExpression(arrayType, size);
		/// <summary>
		/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
		/// </summary>
		/// <param name="arrayType">The element type of the array</param>
		/// <param name="size">The size of the array</param>
		/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
		public static CodeArrayCreateExpression NewArr(Type arrayType, int size)
			=> new CodeArrayCreateExpression(arrayType, size);
		/// <summary>
		/// Creates a <see cref="CodeIndexerExpression"/> with the specified target and indices
		/// </summary>
		/// <param name="target">The target to index into</param>
		/// <param name="indices">The indices to use</param>
		/// <returns>A <see cref="CodeIndexerExpression"/> with the specified target and indices</returns>
		public static CodeIndexerExpression Indexer(CodeExpression target, params CodeExpression[] indices)
			=> new CodeIndexerExpression(target, indices);
		/// <summary>
		/// Creates a <see cref="CodeArrayIndexerExpression"/> with the specified target and indices
		/// </summary>
		/// <param name="target">The array to index into</param>
		/// <param name="indices">The indices to use</param>
		/// <returns>A <see cref="CodeIndexerExpression"/> with the specified target and indices</returns>
		public static CodeArrayIndexerExpression ArrIndexer(CodeExpression target,params CodeExpression[] indices)
			=>new CodeArrayIndexerExpression(target, indices);
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name
		/// </summary>
		/// <param name="type">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name</returns>
		public static CodeParameterDeclarationExpression Param(CodeTypeReference type, string name)
			=> new CodeParameterDeclarationExpression(type, name);
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name
		/// </summary>
		/// <param name="typeName">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name</returns>
		public static CodeParameterDeclarationExpression Param(string typeName, string name)
			=> new CodeParameterDeclarationExpression(typeName, name);
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name
		/// </summary>
		/// <param name="type">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name</returns>
		public static CodeParameterDeclarationExpression Param(Type type, string name)
			=> new CodeParameterDeclarationExpression(type, name);
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction
		/// </summary>
		/// <param name="type">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction</returns>
		public static CodeParameterDeclarationExpression OutParam(CodeTypeReference type, string name)
		{
			var result = new CodeParameterDeclarationExpression(type, name);
			result.Direction = FieldDirection.Out;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction
		/// </summary>
		/// <param name="typeName">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction</returns>
		public static CodeParameterDeclarationExpression OutParam(string typeName, string name)
		{
			var result = new CodeParameterDeclarationExpression(typeName, name);
			result.Direction = FieldDirection.Out;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction
		/// </summary>
		/// <param name="type">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction</returns>
		public static CodeParameterDeclarationExpression OutParam(Type type, string name)
		{
			var result = new CodeParameterDeclarationExpression(type, name);
			result.Direction = FieldDirection.Out;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction
		/// </summary>
		/// <param name="type">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction</returns>
		public static CodeParameterDeclarationExpression RefParam(CodeTypeReference type, string name)
		{
			var result = new CodeParameterDeclarationExpression(type, name);
			result.Direction = FieldDirection.Ref;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction
		/// </summary>
		/// <param name="typeName">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction</returns>
		public static CodeParameterDeclarationExpression RefParam(string typeName, string name)
		{
			var result = new CodeParameterDeclarationExpression(typeName, name);
			result.Direction = FieldDirection.Ref;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction
		/// </summary>
		/// <param name="type">The parameter type</param>
		/// <param name="name">The parameter name</param>
		/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction</returns>
		public static CodeParameterDeclarationExpression RefParam(Type type, string name)
		{
			var result = new CodeParameterDeclarationExpression(type, name);
			result.Direction = FieldDirection.Ref;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeVariableDeclarationStatement"/> with the specified type and name
		/// </summary>
		/// <param name="type">The type of the variable</param>
		/// <param name="name">The name of the variable</param>
		/// <param name="initializer">An optional initializer</param>
		/// <returns>A <see cref="CodeVariableDeclarationStatement"/> with the specified type and name</returns>
		public static CodeVariableDeclarationStatement Var(CodeTypeReference type, string name, CodeExpression initializer = null)
			=> new CodeVariableDeclarationStatement(type, name, initializer);
		/// <summary>
		/// Creates a <see cref="CodeVariableDeclarationStatement"/> with the specified type and name
		/// </summary>
		/// <param name="typeName">The type of the variable</param>
		/// <param name="name">The name of the variable</param>
		/// <param name="initializer">An optional initializer</param>
		/// <returns>A <see cref="CodeVariableDeclarationStatement"/> with the specified type and name</returns>
		public static CodeVariableDeclarationStatement Var(string typeName, string name, CodeExpression initializer = null)
			=> new CodeVariableDeclarationStatement(typeName, name, initializer);
		/// <summary>
		/// Creates a <see cref="CodeVariableDeclarationStatement"/> with the specified type and name
		/// </summary>
		/// <param name="type">The type of the variable</param>
		/// <param name="name">The name of the variable</param>
		/// <param name="initializer">An optional initializer</param>
		/// <returns>A <see cref="CodeVariableDeclarationStatement"/> with the specified type and name</returns>
		public static CodeVariableDeclarationStatement Var(Type type, string name, CodeExpression initializer = null)
			=> new CodeVariableDeclarationStatement(type, name, initializer);
		/// <summary>
		/// Creates a <see cref="CodeIterationStatement"/> statement with the specified condition and statements
		/// </summary>
		/// <param name="cnd">The condition</param>
		/// <param name="statements">The statements in the loop</param>
		/// <returns>A <see cref="CodeIterationStatement"/> statement with the specified condition and statements</returns>
		public static CodeIterationStatement While(CodeExpression cnd, params CodeStatement[] statements)
			=> new CodeIterationStatement(new CodeSnippetStatement(), cnd, new CodeSnippetStatement(), statements);
		/// <summary>
		/// Creates a <see cref="CodeIterationStatement"/> statement with the specified init statement, condition, increment statement, and inner statements
		/// </summary>
		/// <param name="init">The init statement</param>
		/// <param name="cnd">The condition</param>
		/// <param name="inc">The increment statement</param>
		/// <param name="statements">The statements in the loop</param>
		/// <returns>A <see cref="CodeIterationStatement"/> statement with the specified init statement, condition, increment statement, and inner statements</returns>
		public static CodeIterationStatement For(CodeStatement init, CodeExpression cnd, CodeStatement inc, params CodeStatement[] statements)
			=> new CodeIterationStatement(init ?? new CodeSnippetStatement(), cnd, inc ?? new CodeSnippetStatement(), statements);
		/// <summary>
		/// Creates a <see cref="CodeGotoStatement"/> with the specified target label
		/// </summary>
		/// <param name="label">The destination label</param>
		/// <returns>A <see cref="CodeGotoStatement"/> with the specified target label</returns>
		public static CodeGotoStatement Goto(string label)
			=> new CodeGotoStatement(label);
		/// <summary>
		/// Creates a <see cref="CodeThrowExceptionStatement"/> with the specified target expression
		/// </summary>
		/// <param name="target"></param>
		/// <returns>A <see cref="CodeThrowExceptionStatement"/> with the specified target expression</returns>
		public static CodeThrowExceptionStatement Throw(CodeExpression target)
			=> new CodeThrowExceptionStatement(target);
		/// <summary>
		/// Creates a <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members
		/// </summary>
		/// <param name="name">The name of the type</param>
		/// <param name="isPublic">True if the type is public</param>
		/// <param name="members">A list of fields, methods and properties to add to the type</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
		public static CodeTypeDeclaration TypeDecl(string name, bool isPublic = false, params CodeTypeMember[] members)
		{
			var result = new CodeTypeDeclaration(name);
			if (isPublic)
				result.TypeAttributes = TypeAttributes.Public;
			else
				result.TypeAttributes = TypeAttributes.NotPublic;
			for (var i = 0; i < members.Length; i++)
				result.Members.Add(members[i]);
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeTypeDeclaration"/> class with the specified name, access modifiers, and members
		/// </summary>
		/// <param name="name">The name of the type</param>
		/// <param name="isPublic">True if the type is public</param>
		/// <param name="members">A list of fields, methods and properties to add to the type</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
		public static CodeTypeDeclaration Class(string name, bool isPublic = false, params CodeTypeMember[] members)
		{
			var result = TypeDecl(name, isPublic, members);
			result.IsClass = true;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeTypeDeclaration"/> struct with the specified name, access modifiers, and members
		/// </summary>
		/// <param name="name">The name of the type</param>
		/// <param name="isPublic">True if the type is public</param>
		/// <param name="members">A list of fields, methods and properties to add to the type</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
		public static CodeTypeDeclaration Struct(string name, bool isPublic = false, params CodeTypeMember[] members)
		{
			var result = TypeDecl(name, isPublic, members);
			result.IsStruct = true;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeTypeDeclaration"/> enum with the specified name, access modifiers, and members
		/// </summary>
		/// <param name="name">The name of the type</param>
		/// <param name="isPublic">True if the type is public</param>
		/// <param name="members">A list of fields to add to the type</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
		public static CodeTypeDeclaration Enum(string name, bool isPublic = false, params CodeTypeMember[] members)
		{
			var result = TypeDecl(name, isPublic, members);
			result.IsEnum = true;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeTypeDeclaration"/> interface with the specified name, access modifiers, and members
		/// </summary>
		/// <param name="name">The name of the type</param>
		/// <param name="isPublic">True if the type is public</param>
		/// <param name="members">A list of methods and properties to add to the type</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
		public static CodeTypeDeclaration Interface(string name, bool isPublic = false, params CodeTypeMember[] members)
		{
			var result = TypeDecl(name, isPublic, members);
			result.IsInterface = true;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeMemberMethod"/> with the specified name, modifiers, and parameters
		/// </summary>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters</param>
		/// <returns>A <see cref="CodeMemberMethod"/> with the specified name, modifiers, and parameters</returns>
		public static CodeMemberMethod Method(string name, MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
		{
			var result = new CodeMemberMethod();
			result.Name = name;
			result.Attributes = attrs;
			for (var i = 0; i < parameters.Length; i++)
				result.Parameters.Add(parameters[i]);
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters
		/// </summary>
		/// <param name="returnType">The return type of the method</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters</param>
		/// <returns>A <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters</returns>
		public static CodeMemberMethod Method(CodeTypeReference returnType, string name, MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
		{
			var result = Method(name, attrs, parameters);
			if (null != returnType)
				result.ReturnType = returnType;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters
		/// </summary>
		/// <param name="returnTypeName">The return type of the method</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters</param>
		/// <returns>A <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters</returns>
		public static CodeMemberMethod Method(string returnTypeName, string name, MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
			=> Method(Type(returnTypeName), name, attrs, parameters);
		/// <summary>
		/// Creates a <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters
		/// </summary>
		/// <param name="returnType">The return type of the method</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters</param>
		/// <returns>A <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters</returns>
		public static CodeMemberMethod Method(Type returnType, string name, MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
			=> Method(Type(returnType), name, attrs, parameters);
		/// <summary>
		/// Creates a <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer
		/// </summary>
		/// <param name="type">The type of the field</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="initializer">The optional initializer</param>
		/// <returns>A <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer</returns>
		public static CodeMemberField Field(CodeTypeReference type, string name, MemberAttributes attrs = default(MemberAttributes), CodeExpression initializer = null)
		{
			var result = new CodeMemberField();
			result.Type = type;
			result.Name = name;
			result.Attributes = attrs;
			result.InitExpression = initializer;
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer
		/// </summary>
		/// <param name="typeName">The type of the field</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="initializer">The optional initializer</param>
		/// <returns>A <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer</returns>
		public static CodeMemberField Field(string typeName, string name, MemberAttributes attrs = default(MemberAttributes), CodeExpression initializer = null)
			=> Field(Type(typeName), name, attrs, initializer);
		/// <summary>
		/// Creates a <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer
		/// </summary>
		/// <param name="type">The type of the field</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="initializer">The optional initializer</param>
		/// <returns>A <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer</returns>
		public static CodeMemberField Field(Type type, string name, MemberAttributes attrs = default(MemberAttributes), CodeExpression initializer = null)
			=> Field(Type(type), name, attrs, initializer);
		/// <summary>
		/// Creates a <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters
		/// </summary>
		/// <param name="type">The type of the field</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters of the property (for indexers)</param>
		/// <returns>A <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters</returns>
		public static CodeMemberProperty Property(CodeTypeReference type, string name, MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
		{
			var result = new CodeMemberProperty();
			result.Name = name;
			result.Attributes = attrs;
			result.Type = type;
			for (var i = 0; i < parameters.Length; ++i)
				result.Parameters.Add(parameters[i]);
			return result;
		}
		/// <summary>
		/// Creates a <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters
		/// </summary>
		/// <param name="typeName">The type of the field</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters of the property (for indexers)</param>
		/// <returns>A <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters</returns>
		public static CodeMemberProperty Property(string typeName, string name, MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
			=> Property(Type(typeName), name, attrs, parameters);
		/// <summary>
		/// Creates a <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters
		/// </summary>
		/// <param name="type">The type of the field</param>
		/// <param name="name">The name of the member</param>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters of the property (for indexers)</param>
		/// <returns>A <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters</returns>
		public static CodeMemberProperty Property(Type type, string name, MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
			=> Property(Type(type), name, attrs, parameters);
		/// <summary>
		/// Creates a <see cref="CodeConstructor"/> with the specified modifiers and parameters
		/// </summary>
		/// <param name="attrs">The modifier attributes</param>
		/// <param name="parameters">The parameters</param>
		/// <returns>A <see cref="CodeConstructor"/> with the specified modifiers and parameters</returns>
		public static CodeConstructor Ctor(MemberAttributes attrs = default(MemberAttributes), params CodeParameterDeclarationExpression[] parameters)
		{
			var result = new CodeConstructor();
			result.Attributes = attrs;
			for (var i = 0; i < parameters.Length; i++)
				result.Parameters.Add(parameters[i]);
			return result;
		}
		/// <summary>
		/// Gets an item by name from the specified collection
		/// </summary>
		/// <param name="name">The name of the item</param>
		/// <param name="items">The collection</param>
		/// <returns>The first item that could be found, otherwise null</returns>
		public static CodeTypeMember GetByName(string name,CodeTypeMemberCollection items)
		{
			for(int ic = items.Count,i=0;i<ic;++i)
			{
				var item = items[i];
				if (0 == string.Compare(item.Name, name, StringComparison.InvariantCulture))
					return item;
			}
			return null;
		}
		/// <summary>
		/// Gets an item by name from the specified collection
		/// </summary>
		/// <param name="name">The name of the item</param>
		/// <param name="items">The collection</param>
		/// <returns>The first item that could be found, otherwise null</returns>
		public static CodeAttributeDeclaration GetByName(string name, CodeAttributeDeclarationCollection items)
		{
			for (int ic = items.Count, i = 0; i < ic; ++i)
			{
				var item = items[i];
				if (0 == string.Compare(item.Name, name, StringComparison.InvariantCulture))
					return item;
			}
			return null;
		}
		/// <summary>
		/// Gets an item by name from the specified collection
		/// </summary>
		/// <param name="name">The name of the item</param>
		/// <param name="items">The collection</param>
		/// <returns>The first item that could be found, otherwise null</returns>
		public static CodeNamespace GetByName(string name, CodeNamespaceCollection items)
		{
			for (int ic = items.Count, i = 0; i < ic; ++i)
			{
				var item = items[i];
				if (0 == string.Compare(item.Name, name, StringComparison.InvariantCulture))
					return item;
			}
			return null;
		}
		/// <summary>
		/// Gets an item by name from the specified collection
		/// </summary>
		/// <param name="name">The name of the item</param>
		/// <param name="items">The collection</param>
		/// <returns>The first item that could be found, otherwise null</returns>
		public static CodeParameterDeclarationExpression GetByName(string name, CodeParameterDeclarationExpressionCollection items)
		{
			for (int ic = items.Count, i = 0; i < ic; ++i)
			{
				var item = items[i];
				if (0 == string.Compare(item.Name, name, StringComparison.InvariantCulture))
					return item;
			}
			return null;
		}
		/// <summary>
		/// Gets an item by name from the specified collection
		/// </summary>
		/// <param name="name">The name of the item</param>
		/// <param name="items">The collection</param>
		/// <returns>The first item that could be found, otherwise null</returns>
		public static CodeTypeDeclaration GetByName(string name, CodeTypeDeclarationCollection items)
		{
			for (int ic = items.Count, i = 0; i < ic; ++i)
			{
				var item = items[i];
				if (0 == string.Compare(item.Name, name, StringComparison.InvariantCulture))
					return item;
			}
			return null;
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="expr">The expression to render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeExpression expr, string lang = "cs")
		{
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			var prov = CodeDomProvider.CreateProvider(lang);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			prov.GenerateCodeFromExpression(expr, sw, opts);
			sw.Flush();
			return sb.ToString();
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="stmt">The statement to render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeStatement stmt, string lang = "cs")
		{
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			var prov = CodeDomProvider.CreateProvider(lang);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			prov.GenerateCodeFromStatement(stmt, sw, opts);
			sw.Flush();
			return sb.ToString();
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="type">The type to render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeTypeReference type , string lang = "cs")
		{
			return ToString(TypeRef(type),lang);
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="type">The type declaration to render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeTypeDeclaration type, string lang = "cs")
		{
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			var prov = CodeDomProvider.CreateProvider(lang);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			prov.GenerateCodeFromType(type, sw, opts);
			sw.Flush();
			return sb.ToString();
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="namespace">The namespace to render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeNamespace @namespace, string lang = "cs")
		{
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			var prov = CodeDomProvider.CreateProvider(lang);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			prov.GenerateCodeFromNamespace(@namespace, sw, opts);
			sw.Flush();
			return sb.ToString();
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="compileUnit">The compile unit to render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeCompileUnit compileUnit, string lang = "cs")
		{
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			var prov = CodeDomProvider.CreateProvider(lang);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			prov.GenerateCodeFromCompileUnit(compileUnit, sw, opts);
			sw.Flush();
			return sb.ToString();
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="member">The type member render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeTypeMember member, string lang = "cs")
		{
			var sb = new StringBuilder();
			var sw = new StringWriter(sb);
			var prov = CodeDomProvider.CreateProvider(lang);
			var opts = new CodeGeneratorOptions();
			opts.BlankLinesBetweenMembers = false;
			prov.GenerateCodeFromMember(member, sw, opts);
			sw.Flush();
			return sb.ToString();
		}
		/// <summary>
		/// Renders code to a string in the optionally specified language
		/// </summary>
		/// <param name="code">The <see cref="CodeObject"/> to render</param>
		/// <param name="lang">The language - defaults to C#</param>
		/// <returns>A string of code</returns>
		public static string ToString(CodeObject code, string lang = "cs")
		{
			if (null == code)
				throw new ArgumentNullException(nameof(code));
			var cc = code as CodeComment;
			if (null != cc)
				return ToString(new CodeCommentStatement(cc), lang);
			var ccu = code as CodeCompileUnit;
			if (null != ccu)
				return ToString(ccu, lang);
			var ce = code as CodeExpression;
			if (null != ce)
				return ToString(ce, lang);
			var cns = code as CodeNamespace;
			if (null != cns)
			{
				ccu = new CodeCompileUnit();
				ccu.Namespaces.Add(cns);
				return ToString(ccu, lang);
			}
			var cs = code as CodeStatement;
			if (null != cs)
				return ToString(cs, lang);
			var ctm = code as CodeTypeMember;
			if (null != ctm)
				return ToString(ctm, lang);
			var ctp = code as CodeTypeParameter;
			if (null != ctp)
				return ToString(new CodeTypeReference(ctp), lang);
			var ctr = code as CodeTypeReference;
			if (null != ctr)
				return ToString(ctr, lang);
			throw new NotSupportedException("The specified code object cannot be rendered to code directly. It must be rendered as part of a larger graph.");
		}
		static CodeExpression _MakeBinOps(System.Collections.IEnumerable exprs, CodeBinaryOperatorType type)
		{
			var result = new CodeBinaryOperatorExpression();
			foreach (CodeExpression expr in exprs)
			{
				result.Operator = type;
				if (null == result.Left)
				{
					result.Left = expr;
					continue;
				}
				if (null == result.Right)
				{
					result.Right = expr;
					continue;
				}
				result = new CodeBinaryOperatorExpression(result, type, expr);
			}
			if (null == result.Right)
				return result.Left;
			return result;
		}
		#region Type serialization
		static CodeExpression _SerializeArray(Array arr,TypeConverter typeConv)
		{
			if (1 == arr.Rank && 0 == arr.GetLowerBound(0))
			{
				var result = new CodeArrayCreateExpression(arr.GetType());
				foreach (var elem in arr)
					result.Initializers.Add(_Serialize(elem,typeConv));
				return result;
			}
			throw new NotSupportedException("Only SZArrays can be serialized to code.");
		}
		static CodeExpression _SerializeEnum(Enum value,TypeConverter converter)
		{
			var t = value.GetType();
			var sa = value.ToString("F").Split(',');
			double d;
			if (!double.TryParse(sa[0], out d))
			{
				var exprs = new CodeExpressionCollection();
				for (var i = 0; i < sa.Length; i++)
				{
					var s = sa[i];
					exprs.Add(FieldRef(TypeRef(t), s));
				}
				switch (exprs.Count)
				{
					case 1:
						return exprs[0];
					default:
						return BinOp(exprs, CodeBinaryOperatorType.BitwiseOr);
				}
			}
			else
				return Cast(t,Literal(Convert.ChangeType(value, System.Enum.GetUnderlyingType(t)), converter));
			
		}
		static CodeExpression _Serialize(object val,TypeConverter typeConv)
		{
			if (null == val)
				return new CodePrimitiveExpression(null);
			// we serialize type to a typeof expression because it makes sense
			// and makes serializing arrays with types in them possible
			var tt = val as Type;
			if(null!=tt)
				return new CodeTypeOfExpression(tt);
			if (val is char) // special case for unicode nonsense
			{
				// console likes to cook unicode characters
				// so we render them as ints cast to the character
				if (((char)val) > 0x7E)
					return new CodeCastExpression(typeof(char), new CodePrimitiveExpression((int)(char)val));
				return new CodePrimitiveExpression((char)val);
			}
			else
			if (val is bool ||
				val is string ||
				val is short ||
				val is ushort ||
				val is int ||
				val is uint ||
				val is ulong ||
				val is long ||
				val is byte ||
				val is sbyte ||
				val is float ||
				val is double ||
				val is decimal)
			{
				// TODO: mess with strings to make them console safe.
				return new CodePrimitiveExpression(val);
			}
			if(val is System.Enum)
			{
				return _SerializeEnum((Enum)val, typeConv);
			}
			if (val is Array && 1 == ((Array)val).Rank && 0 == ((Array)val).GetLowerBound(0))
			{
				return _SerializeArray((Array)val,typeConv);
			}
			var conv = (null==typeConv)? TypeDescriptor.GetConverter(val):typeConv;
			if (null != conv)
			{
				if (conv.CanConvertTo(typeof(InstanceDescriptor)))
				{
					var desc = conv.ConvertTo(val, typeof(InstanceDescriptor)) as InstanceDescriptor;
					if (!desc.IsComplete)
						throw new NotSupportedException(
							string.Format(
								"The type \"{0}\" could not be serialized.",
								val.GetType().FullName));
					var ctor = desc.MemberInfo as ConstructorInfo;
					if (null != ctor)
					{
						var result = new CodeObjectCreateExpression(ctor.DeclaringType);
						foreach (var arg in desc.Arguments)
							result.Parameters.Add(_Serialize(arg,typeConv));
						return result;
					}
					var meth = desc.MemberInfo as MethodInfo;
					if(null!=meth && (MethodAttributes.Static == (meth.Attributes & MethodAttributes.Static)))
					{
						var result = new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(meth.DeclaringType), meth.Name));
						foreach (var arg in desc.Arguments)
							result.Parameters.Add(_Serialize(arg, typeConv));
						return result;
					}
					var fld = desc.MemberInfo as FieldInfo;
					if (null != fld && ((FieldAttributes.Static == (fld.Attributes & FieldAttributes.Static)) || (FieldAttributes.Literal == (fld.Attributes & FieldAttributes.Literal))))
					{
						var result = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(fld.DeclaringType), fld.Name);
						return result;
					}
					throw new NotSupportedException(
						string.Format(
							"The instance descriptor for type \"{0}\" is not supported.",
							val.GetType().FullName));
				}
				else
				{
					// we special case for KeyValuePair types.
					var t = val.GetType();
					if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
					{
						// TODO: Find a workaround for the bug with VBCodeProvider
						// may need to modify the reference source
						var kvpType = new CodeTypeReference(typeof(KeyValuePair<,>));
						foreach (var arg in val.GetType().GetGenericArguments())
							kvpType.TypeArguments.Add(arg);
						var result = new CodeObjectCreateExpression(kvpType);
						for (int ic = kvpType.TypeArguments.Count, i = 0; i < ic; ++i)
						{
							var prop = val.GetType().GetProperty(0 == i ? "Key" : "Value");
							result.Parameters.Add(_Serialize(prop.GetValue(val),typeConv));
						}
						return result;
					}
					throw new NotSupportedException(
						string.Format("The type \"{0}\" could not be serialized.",
						val.GetType().FullName));
				}
			}
			else
				throw new NotSupportedException(
					string.Format(
						"The type \"{0}\" could not be serialized.",
						val.GetType().FullName));
		}
		#endregion
	}

}