using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace CD
{
	using CTE = CodeTypeReferenceEqualityComparer;
	partial class CodeDomResolver
	{
		/// <summary>
		/// Gets the type of the specified expression, at the optional given scope
		/// </summary>
		/// <param name="expr">The expression to evaluate</param>
		/// <param name="scope">The scope at which evaluation occurs, or null to use the expression's scope</param>
		/// <returns>A <see cref="CodeTypeReference"/> representing the type of the expression</returns>
		public CodeTypeReference GetTypeOfExpression(CodeExpression expr,CodeDomResolverScope scope = null)
		{
			if (null == expr)
				throw new ArgumentNullException(nameof(expr));
			// first let's do the easy ones.
			var cpe = expr as CodePrimitiveExpression;
			if (null!=cpe)
			{
				if (null == cpe.Value)
					return new CodeTypeReference(typeof(void));
				return new CodeTypeReference(cpe.Value.GetType());
			}
			var cbe = expr as CodeBinaryOperatorExpression;
			if(null!=cbe)
			{
				switch(cbe.Operator)
				{
					case CodeBinaryOperatorType.BooleanAnd:
					case CodeBinaryOperatorType.BooleanOr:
					case CodeBinaryOperatorType.GreaterThan:
					case CodeBinaryOperatorType.GreaterThanOrEqual:
					case CodeBinaryOperatorType.IdentityEquality:
					case CodeBinaryOperatorType.IdentityInequality:
					case CodeBinaryOperatorType.LessThan:
					case CodeBinaryOperatorType.LessThanOrEqual:
					case CodeBinaryOperatorType.ValueEquality:
						return new CodeTypeReference(typeof(bool));
					case CodeBinaryOperatorType.Assign:
					case CodeBinaryOperatorType.Add:
					case CodeBinaryOperatorType.Subtract:
					case CodeBinaryOperatorType.Multiply:
					case CodeBinaryOperatorType.Divide:
					case CodeBinaryOperatorType.Modulus:
					case CodeBinaryOperatorType.BitwiseAnd:
					case CodeBinaryOperatorType.BitwiseOr:
						return _PromoteType(GetTypeOfExpression(cbe.Left), GetTypeOfExpression(cbe.Right));
				}
			}
			var tr = expr as CodeTypeReferenceExpression;
			if (null != tr)
			{
				if (null == tr.Type)
					throw new InvalidOperationException("The type reference expression had no target object");
				return tr.Type;
			}
			var pd = expr as CodeParameterDeclarationExpression;
			if (null != pd)
			{
				if (null == pd.Type)
					throw new InvalidOperationException("The parameter declaration had no target object");
				return pd.Type;
			}
			var oc = expr as CodeObjectCreateExpression;
			if (null != oc)
			{
				if (null == oc.CreateType)
					throw new InvalidOperationException("The object creation expression had no create type");
				return oc.CreateType;
			}
			var ac = expr as CodeArrayCreateExpression;
			if (null != ac)
			{
				if (null == ac.CreateType)
					throw new InvalidOperationException("The array creation expression had no create type");
				var ctr = new CodeTypeReference();
				ctr.ArrayElementType = ac.CreateType.ArrayElementType;
				ctr.ArrayRank = ac.CreateType.ArrayRank;
				ctr.BaseType = ac.CreateType.BaseType;
				ctr.TypeArguments.AddRange(ac.CreateType.TypeArguments);
				return ctr;
			}
			var dc = expr as CodeDelegateCreateExpression;
			if (null != dc)
			{
				if (null == dc.DelegateType)
					throw new InvalidOperationException("The delegate creation expression had no delegate type");
				return dc.DelegateType;
			}
			var dv = expr as CodeDefaultValueExpression;
			if (null != dv)
			{
				if (null == dv.Type)
					throw new InvalidOperationException("The default value expression had no type");
				return dv.Type;
			}
			var dire = expr as CodeDirectionExpression;
			if (null != dire)
			{
				if (null == dire.Expression)
					throw new InvalidOperationException("The direction expression had no target expression");
				return GetTypeOfExpression(dire.Expression, scope);
			}
			var ai = expr as CodeArrayIndexerExpression;
			if (null != ai)
			{ 
				var aet= GetTypeOfExpression(ai.TargetObject).ArrayElementType;
				if (null == aet)
					throw new InvalidOperationException("The associated array type's array element type was null");
				return aet;
			}
			var cst = expr as CodeCastExpression;
			if (null != cst)
			{
				if (null == cst.TargetType)
					throw new InvalidOperationException("The cast expression's target type was null");
				return cst.TargetType;
			}
			var to = expr as CodeTypeOfExpression;
			if (null != to)
				return new CodeTypeReference(typeof(Type));
			// now things get complicated
			if (null==scope)
				scope = GetScope(expr);
			var cmi = expr as CodeMethodInvokeExpression;
			if (null != cmi)
			{
				var types = new CodeTypeReference[cmi.Parameters.Count];
				for (var i = 0; i < types.Length; ++i)
				{ 
					var p = cmi.Parameters[i];
					var de = p as CodeDirectionExpression;
					if (null != de)
						p = de.Expression;
					types[i] = GetTypeOfExpression(p, scope);
					if (null == types[i])
						throw new InvalidOperationException(string.Format("Could not resolve parameter index {0} of method invoke expression", i));
				}
				var mr = cmi.Method;
				var t = GetTypeOfExpression(mr.TargetObject, scope);
				var rt = TryResolveType(t, scope);
				if (null == rt)
					throw new InvalidOperationException("Could not resolve the type of the target expression of the method invoke expression");
				var rtt = rt as Type;
				object tm = null;
				var binder = new CodeDomBinder(scope);
				var grp = binder.GetMethodGroup(rt, mr.MethodName, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);

				//tm =binder.BindToMethod(BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, grp,types, null,null,null,out state);
				tm = binder.SelectMethod(BindingFlags.InvokeMethod | BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic, grp, types, null);
				if (null == tm)
					throw new InvalidOperationException("Unable to find a suitable method to bind to in method invoke expression");
				var mi = tm as MethodInfo;
				if (null != mi)
					return new CodeTypeReference(mi.ReturnType);
				var cm = tm as CodeMemberMethod;
				if (null == cm.ReturnType)
					return new CodeTypeReference(typeof(void));
				return cm.ReturnType;
			}
			var ar = expr as CodeArgumentReferenceExpression;
			if (null != ar)
			{
				var t= scope.ArgumentTypes[ar.ParameterName];
				if (null == t)
					throw new InvalidOperationException("The argument's type was null");
				return t;
			}
			
			var vr = expr as CodeVariableReferenceExpression;
			if(null!=vr)
			{
				var t = scope.VariableTypes[vr.VariableName];
				if (null == t)
					throw new InvalidOperationException("The variable's type was null. This could be due to an unresolved var declaration in Slang");
				return t;
			}
			var br = expr as CodeBaseReferenceExpression;
			if(null!=br)
			{
				var dt = scope.DeclaringType;
				if(null!=dt)
				{

					if (0 < dt.BaseTypes.Count)
					{   // this isn't exactly right. See notes below
						var bt = dt.BaseTypes[0];
						if (null == bt)
							throw new InvalidOperationException("The declaring type's base types contained a null entry.");
						return bt;
					}
					else if (dt.IsClass || dt.IsInterface)
						return new CodeTypeReference(typeof(object));
					else if (dt.IsEnum)
						return new CodeTypeReference(typeof(Enum));
					else if (dt.IsStruct)
						return new CodeTypeReference(typeof(ValueType));
					else
						throw new InvalidOperationException("The declaring type is not a class, interface, enum or struct");
				}
				throw new InvalidOperationException("There is no declarting type in the scope from which to retrieve a base reference");
			}
			var th = expr as CodeThisReferenceExpression;
			if(null!=th)
			{
				var dt = scope.DeclaringType;
				if (null != dt)
				{
					// TODO: We have no way to fully resolve this because we'd need to know
					// what expression created this object. If it's a template we can't know
					// what the template args are. The best we can return is something like
					// Foo<> so we do that. This is hateful but I don't know what else to do
					// here
					return new CodeTypeReference(_GetBaseNameOfType(dt, scope));
				}
				throw new InvalidOperationException("There was no declaring type in the scope from which to retrieve a this reference");
			}
			var fr = expr as CodeFieldReferenceExpression;
			if(null!=fr)
			{
				var t = GetTypeOfExpression(fr.TargetObject, scope);
				var tt =_ResolveType(t, scope);
				if (null == tt)
					throw new InvalidOperationException("The field reference's target expression type could not be resolved");
				var binder = new CodeDomBinder(scope);
				var fl = BindingFlags.Public | BindingFlags.NonPublic;
				var isStatic = (fr.TargetObject as CodeTypeReferenceExpression) != null;
				if (isStatic)
					fl |= BindingFlags.Static;
				else
					fl |= BindingFlags.Instance;
				var res = binder.GetField(tt, fr.FieldName, fl);
				if(null!=res)
				{
					var mi = res as MemberInfo;
					if (null != mi)
						return GetTypeForMember(mi);
					return GetTypeForMember(res as CodeTypeMember);
				}
				throw new InvalidOperationException("A matching field could not be found");
			}
			var pr = expr as CodePropertyReferenceExpression;
			if (null != pr)
			{
				var t = GetTypeOfExpression(pr.TargetObject, scope);
				var tt = _ResolveType(t, scope);
				if (null == tt)
					throw new InvalidOperationException("The property reference's target expression type could not be resolved");
				var binder = new CodeDomBinder(scope);
				var fl = BindingFlags.Public | BindingFlags.NonPublic;
				var isStatic = (pr.TargetObject as CodeTypeReferenceExpression) != null;
				if (isStatic)
					fl |= BindingFlags.Static;
				else
					fl |= BindingFlags.Instance;
				var res = binder.GetPropertyGroup(tt,pr.PropertyName, fl);
				if (0 < res.Length)
				{
					var mi = res[0] as MemberInfo;
					if (null != mi)
						return GetTypeForMember(mi);
					return GetTypeForMember(res[0] as CodeTypeMember);
				}
				throw new InvalidOperationException("A matching property could not be found");
				
			}
			var er = expr as CodeEventReferenceExpression;
			if (null != er)
			{
				var t = GetTypeOfExpression(er.TargetObject, scope);
				var tt = _ResolveType(t, scope);
				if (null == tt)
					throw new InvalidOperationException("The event reference's target expression type could not be resolved");
				var binder = new CodeDomBinder(scope);
				var fl = BindingFlags.Public | BindingFlags.NonPublic;
				var isStatic = (er.TargetObject as CodeTypeReferenceExpression) != null;
				if (isStatic)
					fl |= BindingFlags.Static;
				else
					fl |= BindingFlags.Instance;
				var res = binder.GetEvent(tt, er.EventName, fl);
				if (null!=res)
				{
					var mi = res as MemberInfo;
					if (null != mi)
						return GetTypeForMember(mi);
					else
						return GetTypeForMember(res as CodeTypeMember);
				}
				throw new InvalidOperationException("A matching event could not be found");
			}
			
			var di = expr as CodeDelegateInvokeExpression;
			if (null != di)
			{
				var ctr = GetTypeOfExpression(di.TargetObject, scope);
				
				var tt = _ResolveType(ctr, scope) as Type;
				if (null == tt)
					throw new InvalidOperationException("The delegate invoke expression's target expression type could not resolved.");
				var ma = tt.GetMember("Invoke");
				if (0 < ma.Length)
				{
					var mi = ma[0] as MethodInfo;
					if (null != mi)
						return new CodeTypeReference(mi.ReturnType);
				}
				throw new InvalidOperationException("The target is not a delegate");
			}
			var ie = expr as CodeIndexerExpression;
			if(null!=ie)
			{
				var t = GetTypeOfExpression(ie.TargetObject, scope);
				// we have to special case for string because there
				// is no "real" indexer property on it.
				if (0 == t.ArrayRank && 0 == string.Compare("System.String", t.BaseType))
					return new CodeTypeReference(typeof(char));
				var types = new CodeTypeReference[ie.Indices.Count];
				for (var i = 0; i < types.Length; ++i)
				{
					var p = ie.Indices[i];
					var de = p as CodeDirectionExpression;
					if (null != de)
						p = de.Expression;
					types[i] = GetTypeOfExpression(p, scope);
					if (IsNullOrVoidType(types[i]))
						throw new InvalidOperationException("One or more of the indexer argument types was void");
				}
				var tt = TryResolveType(t, scope);
				if (null == tt)
					throw new InvalidOperationException("The indexer expression's target expression type could not be resolved");
				var binder = new CodeDomBinder(scope);
				var td = tt as CodeTypeDeclaration;
				object tm = null;
				if (null!=td)
				{
					var grp = binder.GetPropertyGroup(td, "Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
					tm=binder.SelectProperty(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance,grp,null,types,null);
				} else
				{
					var rt = tt as Type;
					if(null!=rt)
					{
						var grp = binder.GetPropertyGroup(rt, "Item", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
						tm = binder.SelectProperty(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance, grp, null, types, null);
					}
				}
				if (null == tm)
					throw new InvalidOperationException("The indexer expression's target object type does not have a matching indexer property");
				var pi = tm as PropertyInfo;
				if (null != pi)
					return new CodeTypeReference(pi.PropertyType);
				var cm = tm as CodeMemberProperty;
				if (null == cm.Type)
					throw new InvalidOperationException("The property declaration's property type was null");
				return cm.Type;
			}
			throw new InvalidOperationException(string.Format("Unsupported expression type {0}", expr.GetType().Name));
		}
		/// <summary>
		/// Attempts to return the type of the specified expression using the specified scope
		/// </summary>
		/// <param name="expr">The expression to evaluate</param>
		/// <param name="scope">The scope to use, or null to use the expression's current scope</param>
		/// <returns>A <see cref="CodeTypeReference"/> representing the type of the expression or null if it could not be retrieved</returns>
		public CodeTypeReference TryGetTypeOfExpression(CodeExpression expr, CodeDomResolverScope scope = null)
		{
			// reimplementing a try version of this is a lot of code, and frankly, the resolution takes so much
			// longer than any exception handling that this is acceptable in this case.
			try
			{
				return GetTypeOfExpression(expr, scope);
			}
			catch(Exception) { return null; }
		}
		static readonly CodeTypeReference _FloatType = new CodeTypeReference(typeof(float));
		static readonly CodeTypeReference _DoubleType = new CodeTypeReference(typeof(double));
		static readonly CodeTypeReference _DecimalType = new CodeTypeReference(typeof(decimal));
		static readonly CodeTypeReference _Byte = new CodeTypeReference(typeof(byte));
		static readonly CodeTypeReference _SByte = new CodeTypeReference(typeof(sbyte));
		static readonly CodeTypeReference _Char = new CodeTypeReference(typeof(char));
		static readonly CodeTypeReference _Short = new CodeTypeReference(typeof(short));
		static readonly CodeTypeReference _UShort = new CodeTypeReference(typeof(ushort));
		static readonly CodeTypeReference _Int = new CodeTypeReference(typeof(int));
		static readonly CodeTypeReference _UInt = new CodeTypeReference(typeof(uint));
		static readonly CodeTypeReference _Long = new CodeTypeReference(typeof(long));
		static readonly CodeTypeReference _Ulong = new CodeTypeReference(typeof(ulong));
		static CodeParameterDeclarationExpressionCollection _GetParametersFromMember(CodeTypeMember member)
		{
			var m = member as CodeMemberMethod;
			if (null != m)
				return m.Parameters;
			var p = member as CodeMemberProperty;
			if (null != p)
				return p.Parameters;
			return null;
		}
		static CodeTypeReference _PromoteType(CodeTypeReference x, CodeTypeReference y)
		{
			if ((_IsNumericType(x) || CTE.Equals(_Char,x)) && (_IsNumericType(y) || CTE.Equals(_Char,y)))
			{
				//If either operand is of type decimal, the other operand is converted to type decimal, or a compile-time error occurs if the other operand is of type float or double.
				if (CTE.Equals(x, _DecimalType))
				{
					if (CTE.Equals(_FloatType, y))
						throw new InvalidOperationException("Cannot convert float to decimal");
					if (CTE.Equals(_DoubleType, y))
						throw new InvalidOperationException("Cannot convert double to decimal");
					return new CodeTypeReference(typeof(decimal));
				}
				else if (CTE.Equals(y, _DecimalType))
				{
					if (CTE.Equals(_FloatType, x))
						throw new InvalidOperationException("Cannot convert float to decimal");
					if (CTE.Equals(_DoubleType, x))
						throw new InvalidOperationException("Cannot convert double to decimal");
					return new CodeTypeReference(typeof(decimal));
				}
				//Otherwise, if either operand is of type double, the other operand is converted to type double.
				if (CTE.Equals(x, _DoubleType) || CTE.Equals(y, _DoubleType))
					return new CodeTypeReference(typeof(double));
				//Otherwise, if either operand is of type float, the other operand is converted to type float.
				if (CTE.Equals(x, _FloatType) || CTE.Equals(y, _FloatType))
					return new CodeTypeReference(typeof(double));
				//Otherwise, if either operand is of type ulong, the other operand is converted to type ulong, 
				// or a compile-time error occurs if the other operand is of type sbyte, short, int, or long.
				if (CTE.Equals(x, _Ulong))
				{
					if (CTE.Equals(_SByte, y) ||
						CTE.Equals(_Short, y) ||
						CTE.Equals(_Int, y) ||
						CTE.Equals(_Long, y))
						throw new InvalidOperationException("Cannot convert signed type to ulong");
					return new CodeTypeReference(typeof(ulong));
				}
				else if (CTE.Equals(y, _Ulong))
				{
					if (CTE.Equals(_SByte, x) ||
						CTE.Equals(_Short, x) ||
						CTE.Equals(_Int, x) ||
						CTE.Equals(_Long, x))
						throw new InvalidOperationException("Cannot convert signed type to ulong");
					return new CodeTypeReference(typeof(ulong));
				}
				//Otherwise, if either operand is of type long, the other operand is converted to type long.
				if (CTE.Equals(x, _Long) || CTE.Equals(y, _Long))
					return new CodeTypeReference(typeof(long));
				//Otherwise, if either operand is of type uint and the other operand is of type sbyte, short, or int, both operands are converted to type long.
				if (CTE.Equals(x, _UInt))
				{
					if (CTE.Equals(_SByte, y) ||
						CTE.Equals(_Short, y) ||
						CTE.Equals(_Int, y))
						return new CodeTypeReference(typeof(long));
				}
				else if (CTE.Equals(y, _UInt))
				{
					if (CTE.Equals(_SByte, x) ||
						CTE.Equals(_Short, x) ||
						CTE.Equals(_Int, x))
						return new CodeTypeReference(typeof(long));
				}
				//Otherwise, if either operand is of type uint, the other operand is converted to type uint.
				if (CTE.Equals(x, _UInt) || CTE.Equals(y, _UInt))
					return new CodeTypeReference(typeof(uint));

				//Otherwise, both operands are converted to type int.
				return new CodeTypeReference(typeof(int));
			}
			if (CTE.Equals(x, y)) return x;
			throw new InvalidCastException("Cannot promote these types");
		}
		static bool _IsNumericType(CodeTypeReference t)
		{
			return CTE.Equals(_Byte, t) || CTE.Equals(_SByte, t) ||
				CTE.Equals(_Short, t) || CTE.Equals(_UShort, t) ||
				CTE.Equals(_Int, t) || CTE.Equals(_UInt, t) ||
				CTE.Equals(_Long, t) || CTE.Equals(_Ulong, t) ||
				CTE.Equals(_FloatType, t) || CTE.Equals(_DoubleType, t) ||
				CTE.Equals(_DecimalType,t);
		}
	}
}
