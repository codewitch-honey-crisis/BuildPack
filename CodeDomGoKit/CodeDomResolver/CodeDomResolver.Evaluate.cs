using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Reflection;
namespace CD
{
	partial class CodeDomResolver
	{
		/// <summary>
		/// Evaluates the expression at the the given scope
		/// </summary>
		/// <param name="expression">The expression to evaluate</param>
		/// <param name="scope">The scope at which evaluation occurs or null to use the expression's scope</param>
		/// <returns>The result of the evaluation</returns>
		public object Evaluate(CodeExpression expression,CodeDomResolverScope scope=null)
			=>_Eval(expression,scope);
		object _Eval(CodeExpression e,CodeDomResolverScope s)
		{
			var ac = e as CodeArrayCreateExpression;
			if(null!=ac)
			{
				if (null == s)
					s = GetScope(ac);
				var type = _EvalType(ac.CreateType,s);
				var len = ac.Initializers.Count;
				if (0 == len)
				{
					if (0 < ac.Size)
						len = ac.Size;
					else
						len = (int)_Eval(ac.SizeExpression, s);
				}
				var arr = Array.CreateInstance(type, len);
				if(0<ac.Initializers.Count)
					for (int ic = ac.Initializers.Count, i = 0; i < ic; ++i)
						arr.SetValue(_Eval(ac.Initializers[i], s),i);
			}
			var ai = e as CodeArrayIndexerExpression;
			if (null != ai)
			{
				if (null == s)
					s = GetScope(e);
				var arr = (Array)_Eval(ai.TargetObject, s);
				var ind = new int[ai.Indices.Count];
				for(var i = 0;i<ind.Length;i++)
					ind[i]=(int)_Eval(ai.Indices[i], s);
				return arr.GetValue(ind);
			}
			var bo = e as CodeBinaryOperatorExpression;
			if(null!=bo)
				return _EvalBinOp(bo,s);
			var c = e as CodeCastExpression;
			if(null!=c)
			{
				if (null == s)
					s = GetScope(c);
				var type = _EvalType(c.TargetType, s);
				var rhs = _Eval(c.Expression, s);
				if (null == rhs) // cast from null allowed
				{
					if (type.IsValueType)
						throw new InvalidCastException("Cannot cast null to a value type");
					return null;
				}
				if (rhs.GetType().IsAssignableFrom(type))
					return rhs;
				throw new InvalidCastException("The value is not assignable to that target type");
			}
			var dv = e as CodeDefaultValueExpression;
			if(null!=dv)
			{
				if (null == s)
					s = GetScope(c);
				var type = _EvalType(c.TargetType,s);
				return Activator.CreateInstance(type);
			}
			var dc = e as CodeDelegateCreateExpression;
			if(null!=dc)
			{
				if (null == s)
					s = GetScope(dc);
				var type = _EvalType(dc.DelegateType, s);
				var targ = _Eval(dc.TargetObject, s);
				var m = targ.GetType().GetMethod(dc.MethodName, ((BindingFlags) (-1)) & ~BindingFlags.DeclaredOnly);
				return Delegate.CreateDelegate( type,targ, m);
			}
			var di = e as CodeDelegateInvokeExpression;
			if(null!=di)
			{
				if (null == s)
					s = GetScope(di);
				var lhs = _Eval(di.TargetObject, s);
				var parms = new object[di.Parameters.Count];
				for (var i = 0;i<parms.Length;i++) 
					parms[i] = _Eval(di.Parameters[i], s);

				var m = lhs.GetType().GetMethod("Invoke");
				try
				{
					return m.Invoke(lhs, parms);
				}
				catch(TargetInvocationException tex)
				{
					throw tex.InnerException;
				}
			}
			var de = e as CodeDirectionExpression;
			if (null != de)
			{
				if (null == s)
					s = GetScope(de);
				return _Eval(de.Expression, s);
			}
			var er = e as CodeEventReferenceExpression;
			if(null!=er)
			{
				if (null == s)
					s = GetScope(er);
				var ev = _Eval(er.TargetObject, s);
				var ei = ev.GetType().GetEvent(er.EventName);
				// i have no idea what else to return. A delegate?
				return new KeyValuePair<EventInfo, object>(ei, ev);
			}
			var fr = e as CodeFieldReferenceExpression;
			if (null != fr)
			{
				if (null == s)
					s = GetScope(fr);
				var trr = _Eval(fr.TargetObject, s);
				var type = trr as Type;
				if(null!=type)
					return type.GetField(fr.FieldName).GetValue(null);
				return trr.GetType().GetField(fr.FieldName).GetValue(trr);
			}
			var ix = e as CodeIndexerExpression;
			if (null != ix)
			{
				if (null == s)
					s = GetScope(ix);
				var ir = _Eval(ix.TargetObject, s);
				var pia = _GetParamInfos(ix.Indices, s);
				var tt = ir as Type;
				var type = null != tt ? tt : ir.GetType();
				try
				{
					if (null == tt)
						return type.InvokeMember("Item", BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.InvokeMethod| BindingFlags.Instance, null, ir, _GetParamValues(pia));
					return type.InvokeMember("Item", BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, null, _GetParamValues(pia));
				}
				catch(TargetInvocationException tex)
				{
					throw tex.InnerException;
				}
			}
			var mi = e as CodeMethodInvokeExpression;
			if(null!=mi)
			{
				if (null == s)
					s = GetScope(mi);
				var mv = _Eval(mi.Method.TargetObject, s);
				var type = mv.GetType();
				var tt = mv as Type;
				if (null != tt)
					type = tt;

				var pia =_GetParamInfos(mi.Parameters, s);
				try
				{
					if (null == tt)
						return type.InvokeMember(mi.Method.MethodName,BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Instance,null, mv, _GetParamValues(pia));
					return type.InvokeMember(mi.Method.MethodName, BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static | BindingFlags.FlattenHierarchy, null, null, _GetParamValues(pia));
				}
				catch(TargetInvocationException tex)
				{
					throw tex.InnerException;
				}
			}
			var mr = e as CodeMethodReferenceExpression;
			if(null!=mr) {
				if (null == s)
					s = GetScope(mr);
				var mv = _Eval(mr.TargetObject, s);
				var ml = new List<MethodInfo>();
				var ma = mv.GetType().GetMethods();
				for(var i = 0;i<ma.Length;++i)
				{
					var m = ma[i];
					if (0 == string.Compare(m.Name, mr.MethodName, StringComparison.InvariantCulture))
						ml.Add(m);
				}
				return new KeyValuePair<MethodInfo[], object>(ml.ToArray(), mv); // basically returning a "MethodGroup" with an attached target object. stupid but what can we do?
			}
			var oc = e as CodeObjectCreateExpression;
			if(null!=oc)
			{
				if (null == s)
					s = GetScope(oc);
				var t = _EvalType(oc.CreateType, s);
				var pia = _GetParamInfos(oc.Parameters, s);
				return Activator.CreateInstance(t, _GetParamValues(pia));
			}
			var p = e as CodePrimitiveExpression;
			if (null != p)
				return p.Value;
			var pr = e as CodePropertyReferenceExpression;
			if (null != pr)
			{
				if (null == s)
					s = GetScope(pr);
				var trr = _Eval(pr.TargetObject, s);
				var type = trr as Type;
				if (null != type)
					return type.GetProperty(pr.PropertyName).GetValue(null);
				return trr.GetType().GetProperty(pr.PropertyName).GetValue(trr);
			}
			var to = e as CodeTypeOfExpression;
			if(null!=to)
			{
				if (null == s)
					s = GetScope(to);
				return _EvalType(to.Type, s);
			}
			var tr = e as CodeTypeReferenceExpression;
			if(null!=tr)
				return _EvalType(tr.Type, s);
			throw new NotSupportedException(string.Format("Unable to evaluate expressions of type {0}", e.GetType().FullName));
		}
		private struct _ParamInfo
		{
			public Type Type;
			public bool IsIn;
			public bool IsOut;
			public bool IsRetval;
			public bool IsOptional;
			public object Value;
		}
		object[] _GetParamValues(_ParamInfo[] paramInfos)
		{
			var result = new object[paramInfos.Length];
			for (var i = 0; i < result.Length; ++i)
				result[i] = paramInfos[i].Value;
			return result;
		}
		_ParamInfo[] _GetParamInfos(CodeExpressionCollection parms,CodeDomResolverScope s)
		{
			var result = new _ParamInfo[parms.Count];
			for(var i = 0;i<result.Length;i++)
			{
				CodeExpression e = parms[i];
				_ParamInfo p=default(_ParamInfo);
				p.IsOptional = false;
				p.IsRetval = false;
				var de = e as CodeDirectionExpression;
				if (null != de)
				{
					switch (de.Direction)
					{
						case FieldDirection.In:
							break;
						case FieldDirection.Out:
							p.IsOut = true;
							break;
						case FieldDirection.Ref:
							p.IsIn = p.IsOut = true;
							break;
					}
					e = de.Expression;
				}
				
				p.Value = _Eval(e, s);
				if (null != p.Value)
					p.Type = p.Value.GetType();
				result[i] = p;
				
			}
			return result;
		}
		
		PropertyInfo _MatchPropBySig(Type type,string name,_ParamInfo[] infos)
		{
			PropertyInfo result = null;
			var ma = type.GetProperties(((BindingFlags)(-1)) & ~BindingFlags.DeclaredOnly);
			for (var i = 0; i < ma.Length; ++i)
			{
				var m = ma[i];
				if (0 == string.Compare(name, m.Name, StringComparison.InvariantCulture))
				{
					var mpa = m.GetIndexParameters();
					if (mpa.Length == infos.Length)
					{
						if (0 == mpa.Length)
						{
							if (null != result)
								throw new InvalidOperationException("Multiple matching indexer signatures were found");
							result = m;
						}
						bool found = false;
						for (var j = 0; j < mpa.Length; ++j)
						{
							found = true;
							var mp = mpa[j];
							var mpc = infos[j];
							var tc = infos[j].Type;
							if (!((null == tc || mp.ParameterType.IsAssignableFrom(tc)) &&
								(mp.IsIn == mpc.IsIn &&
								mp.IsOut == mpc.IsOut &&
								mp.IsRetval == mpc.IsRetval &&
								mp.IsOptional == mpc.IsOptional)))
							{
								found = false;
								break;
							}
						}
						if (found)
						{
							if (null != result)
								throw new InvalidOperationException("Multiple matching indexer signatures were found");
							result = m;
						}
					}
				}
			}
			return result;
		}
		MethodInfo _MatchMethBySig(Type type, string name, _ParamInfo[] infos)
		{
			MethodInfo result = null;
			var ma = type.GetMethods(((BindingFlags)(-1))&~BindingFlags.DeclaredOnly);
			for (var i = 0; i < ma.Length; ++i)
			{
				var m = ma[i];
				if (0 == string.Compare(name, m.Name, StringComparison.InvariantCulture))
				{
					var mpa = m.GetParameters();
					if (mpa.Length == infos.Length)
					{
						if(0==mpa.Length)
						{
							if (null != result)
								throw new InvalidOperationException("Multiple matching method signatures were found");
							result = m;
						}
						bool found = false;
						for (var j = 0; j < mpa.Length; ++j)
						{
							found = true;
							var mp = mpa[j];
							var mpc = infos[j];
							var tc = infos[j].Type;
							if (!((null == tc || mp.ParameterType.IsAssignableFrom(tc)) &&
								(mp.IsIn == mpc.IsIn &&
								mp.IsOut == mpc.IsOut &&
								mp.IsRetval == mpc.IsRetval &&
								mp.IsOptional == mpc.IsOptional)))
							{
								found = false;
								break;
							}
						}
						if (found)
						{
							if (null != result)
								throw new InvalidOperationException("Multiple matching method signatures were found");
							result = m;
						}
					}
				}
			}
			return result;
		}
		ConstructorInfo _MatchCtorBySig(Type type, _ParamInfo[] infos)
		{
			ConstructorInfo result = null;
			var ma = type.GetConstructors(((BindingFlags)(-1)) & ~BindingFlags.DeclaredOnly);
			for (var i = 0; i < ma.Length; ++i)
			{
				var m = ma[i];
				var mpa = m.GetParameters();
				if (mpa.Length == infos.Length)
				{
					if (0 == mpa.Length)
					{
						if (null != result)
							throw new InvalidOperationException("Multiple matching constructor signatures were found");
						result = m;
					}
					bool found = false;
					for (var j = 0; j < mpa.Length; ++j)
					{
						found = true;
						var mp = mpa[j];
						var mpc = infos[j];
						var tc = infos[j].Type;
						if (!((null == tc || mp.ParameterType.IsAssignableFrom(tc)) &&
							(mp.IsIn == mpc.IsIn &&
							mp.IsOut == mpc.IsOut &&
							mp.IsRetval == mpc.IsRetval &&
							mp.IsOptional == mpc.IsOptional)))
						{
							found = false;
							break;
						}
					}
					if (found)
					{
						if (null != result)
							throw new InvalidOperationException("Multiple matching constructor signatures were found");
						result = m;
					}
				}
				
			}
			return result;
		}
		Type _EvalType(CodeTypeReference r, CodeDomResolverScope s)
		{
			if (null == s)
				s = GetScope(r);
			var t = _ResolveType(r, s);
			if (null == t)
				throw new TypeLoadException("The type could not be resolved");
			var result = t as Type;
			if (null == result)
				throw new NotSupportedException("Only runtime types may be evaluated");
			return result;
		}

		object _EvalBinOp(CodeBinaryOperatorExpression bo,CodeDomResolverScope s)
		{
			if(null==s)
				s= GetScope(bo);
			switch(bo.Operator)
			{
				case CodeBinaryOperatorType.Add:
					return _Add(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.Subtract:
					return _Subtract(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.Multiply:
					return _Multiply(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.Divide:
					return _Divide(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.Modulus:
					return _Modulo(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.Assign:
					throw new NotSupportedException("Evaluate cannot change state.");
				case CodeBinaryOperatorType.BitwiseAnd:
					return _BitwiseAnd(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.BitwiseOr:
					return _BitwiseOr(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.BooleanAnd:
					return ((bool)_Eval(bo.Left, s)) && ((bool)_Eval(bo.Right, s));
				case CodeBinaryOperatorType.BooleanOr:
					return ((bool)_Eval(bo.Left, s)) || ((bool)_Eval(bo.Right, s));
				case CodeBinaryOperatorType.LessThan:
					return _LessThan(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.LessThanOrEqual:
					return _LessThanOrEqual(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.GreaterThan:
					return _GreaterThan(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.GreaterThanOrEqual:
					return _GreaterThanOrEqual(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.IdentityEquality:
				case CodeBinaryOperatorType.ValueEquality:
					return _Equals(_Eval(bo.Left, s), _Eval(bo.Right, s));
				case CodeBinaryOperatorType.IdentityInequality:
					return _NotEqual(_Eval(bo.Left, s), _Eval(bo.Right, s));
				default:
					throw new NotSupportedException("The specified operation is not supported.");
			}
		}
		object _Add(object lhs,object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) + ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) + ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) + ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) + ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) + ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) + ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) + ((int)rhs);
			try
			{
				return lt.GetMethod("op_Addition").Invoke(null,new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _Subtract(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) - ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) - ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) - ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) - ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) - ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) - ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) - ((int)rhs);
			try
			{
				return lt.GetMethod("op_Subtraction").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _Multiply(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) * ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) * ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) * ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) * ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) * ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) * ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) * ((int)rhs);
			try
			{
				return lt.GetMethod("op_Multiply").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _Divide(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) / ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) / ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) / ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) / ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) / ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) / ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) / ((int)rhs);
			try
			{
				return lt.GetMethod("op_Division").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _Modulo(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) % ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) % ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) % ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) % ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) % ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) % ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) % ((int)rhs);
			try
			{
				return lt.GetMethod("op_Modulus").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _BitwiseAnd(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) & ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) & ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) & ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) & ((int)rhs);
			try
			{
				return lt.GetMethod("op_BitwiseAnd").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _BitwiseOr(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) | ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) | ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) | ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) | ((int)rhs);
			try
			{
				return lt.GetMethod("op_BitwiseOr").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _Equals(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) == ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) == ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) == ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) == ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) == ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) == ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) == ((int)rhs);
			try
			{
				return lt.GetMethod("op_Equality").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _NotEqual(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) != ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) != ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) != ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) != ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) != ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) != ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) != ((int)rhs);
			try
			{
				return lt.GetMethod("op_Inequality").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _LessThan(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) < ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) < ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) < ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) < ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) < ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) < ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) < ((int)rhs);
			try
			{
				return lt.GetMethod("op_LessThan").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _GreaterThan(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) > ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) > ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) > ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) > ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) > ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) > ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) > ((int)rhs);
			try
			{
				return lt.GetMethod("op_GreaterThan").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _LessThanOrEqual(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) <= ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) <= ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) <= ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) <= ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) <= ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) <= ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) <= ((int)rhs);
			try
			{
				return lt.GetMethod("op_LessThanOrEqual").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		object _GreaterThanOrEqual(object lhs, object rhs)
		{
			_Promote(ref lhs, ref rhs);
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (typeof(decimal) == lt && typeof(decimal) == rt)
				return ((decimal)lhs) >= ((decimal)rhs);
			if (typeof(double) == lt && typeof(double) == rt)
				return ((double)lhs) >= ((double)rhs);
			if (typeof(float) == lt && typeof(float) == rt)
				return ((float)lhs) >= ((float)rhs);
			if (typeof(ulong) == lt && typeof(ulong) == rt)
				return ((ulong)lhs) >= ((ulong)rhs);
			if (typeof(long) == lt && typeof(long) == rt)
				return ((long)lhs) >= ((long)rhs);
			if (typeof(uint) == lt && typeof(uint) == rt)
				return ((uint)lhs) >= ((uint)rhs);
			if (typeof(int) == lt && typeof(int) == rt)
				return ((int)lhs) >= ((int)rhs);
			try
			{
				return lt.GetMethod("op_GreaterThanOrEqual").Invoke(null, new object[] { lhs, rhs });
			}
			catch
			{
				throw new InvalidOperationException("The operation cannot be performed on objects on these types");
			}
		}
		void _Promote(ref object lhs,ref object rhs)
		{
			if (null == lhs || null == rhs)
				return;
			
			var lt = lhs.GetType();
			var rt = rhs.GetType();
			if (!_IsNumeric(lt) || !_IsNumeric(rt))
				return;
			if (lt == rt) return;
			//If either operand is of type decimal, the other operand is converted to type decimal, 
			//  or a compile-time error occurs if the other operand is of type float or double.
			if (typeof(decimal) == lt)
			{
				if (typeof(double) == rt || typeof(float) == rt)
					throw new InvalidOperationException("Cannot operate on types of decimal and float.");
				rhs = (decimal)rhs;
				return;
			}
			if (typeof(decimal) == rt)
			{
				if (typeof(double) == lt || typeof(float) == lt)
					throw new InvalidOperationException("Cannot operate on types of decimal and float.");
				lhs = (decimal)lhs;
				return;
			}
			//Otherwise, if either operand is of type double, the other operand is converted to type double.
			if (typeof(double) == lt)
			{
				rhs = (double)rhs;
				return;
			}
			if (typeof(double) == rt)
			{
				lhs = (double)lhs;
				return;
			}
			//Otherwise, if either operand is of type float, the other operand is converted to type float.
			if (typeof(float) == lt)
			{
				rhs = (float)rhs;
				return;
			}
			if (typeof(float) == rt)
			{
				lhs = (float)lhs;
				return;
			}
			//Otherwise, if either operand is of type ulong, the other operand is converted to type ulong, 
			// or a compile-time error occurs if the other operand is of type sbyte, short, int, or long.
			if (typeof(ulong) == lt)
			{
				if (typeof(sbyte) == rt || typeof(short) == rt || typeof(int) == rt || typeof(long) == rt)
					throw new InvalidOperationException("Cannot operate on ulong and a signed value");
				rhs = (ulong)rhs;
				return;
			}
			if (typeof(ulong) == rt)
			{
				if (typeof(sbyte) == lt || typeof(short) == lt || typeof(int) == lt || typeof(long) == lt)
					throw new InvalidOperationException("Cannot operate on ulong and a signed value");
				lhs = (ulong)lhs;
				return;
			}
			//Otherwise, if either operand is of type long, the other operand is converted to type long.
			if (typeof(long) == lt)
			{
				rhs = (long)rhs;
				return;
			}
			if (typeof(long) == rt)
			{
				lhs = (long)lhs;
				return;
			}
			//Otherwise, if either operand is of type uint and the other operand is of type sbyte, short, or int, both operands are converted to type long.
			//Otherwise, if either operand is of type uint, the other operand is converted to type uint.
			if (typeof(uint) == lt)
			{
				if (typeof(sbyte) == rt || typeof(short) == rt || typeof(int) == rt)
				{
					lhs = (long)lhs;
					rhs = (long)rhs;
				} else
				{
					rhs = (uint)rhs;
				}
				return;
			}
			if (typeof(uint) == rt)
			{
				if (typeof(sbyte) == lt || typeof(short) == lt || typeof(int) == lt)
				{
					lhs = (long)lhs;
					rhs = (long)rhs;
				}
				else
				{
					lhs = (uint)lhs;
				}
				return;
			}
			//Otherwise, both operands are converted to type int.
			lhs = (int)lhs;
			rhs = (int)rhs;
		}
		bool _IsNumeric(Type nm)
		{
			if (null == nm) return false;
			return typeof(sbyte) == nm || typeof(byte) == nm ||
				typeof(short) == nm || typeof(ushort) == nm ||
				typeof(int) == nm || typeof(uint) == nm ||
				typeof(long) == nm || typeof(ulong) == nm ||
				typeof(float) == nm || typeof(double) == nm ||
				typeof(decimal) == nm;
		}
	}
}
