using System;
using System.Reflection;
using System.CodeDom;
using System.Diagnostics.Contracts;

namespace CD
{
	using R = CodeDomResolver;
	using E = CodeTypeReferenceEqualityComparer;
	partial class CodeDomBinder
	{

		int FindMostSpecific(_ParamInfo[] p1, int[] paramOrder1, CodeTypeReference paramArrayType1,
											_ParamInfo[] p2, int[] paramOrder2, CodeTypeReference paramArrayType2,
											CodeTypeReference[] types, Object[] args)
		{
			// A method using params is always less specific than one not using params
			if (!R.IsNullOrVoidType(paramArrayType1) && R.IsNullOrVoidType(paramArrayType2)) return 2;
			if (!R.IsNullOrVoidType(paramArrayType2) && R.IsNullOrVoidType(paramArrayType1)) return 1;

			// now either p1 and p2 both use params or neither does.

			bool p1Less = false;
			bool p2Less = false;

			for (int i = 0; i < types.Length; i++)
			{
				if (args != null && args[i] == Type.Missing)
					continue;

				CodeTypeReference c1, c2;

				//  If a param array is present, then either
				//      the user re-ordered the parameters in which case
				//          the argument to the param array is either an array
				//              in which case the params is conceptually ignored and so paramArrayType1 == null
				//          or the argument to the param array is a single element
				//              in which case paramOrder[i] == p1.Length - 1 for that element
				//      or the user did not re-order the parameters in which case
				//          the paramOrder array could contain indexes larger than p.Length - 1 (see VSW 577286)
				//          so any index >= p.Length - 1 is being put in the param array

				if (!R.IsNullOrVoidType(paramArrayType1) && paramOrder1[i] >= p1.Length - 1)
					c1 = paramArrayType1;
				else
					c1 = p1[paramOrder1[i]].ParameterType;

				if (!R.IsNullOrVoidType(paramArrayType2) && paramOrder2[i] >= p2.Length - 1)
					c2 = paramArrayType2;
				else
					c2 = p2[paramOrder2[i]].ParameterType;

				if (E.Equals(c1 , c2)) continue;

				switch (FindMostSpecificType(c1, c2, types[i]))
				{
					case 0: return 0;
					case 1: p1Less = true; break;
					case 2: p2Less = true; break;
				}
			}

			// Two way p1Less and p2Less can be equal.  All the arguments are the
			//  same they both equal false, otherwise there were things that both
			//  were the most specific type on....
			if (p1Less == p2Less)
			{
				// if we cannot tell which is a better match based on parameter types (p1Less == p2Less),
				// let's see which one has the most matches without using the params array (the longer one wins).
				if (!p1Less && args != null)
				{
					if (p1.Length > p2.Length)
					{
						return 1;
					}
					else if (p2.Length > p1.Length)
					{
						return 2;
					}
				}

				return 0;
			}
			else
			{
				return (p1Less == true) ? 1 : 2;
			}
		}

		int FindMostSpecific(_ParamInfo[] p1, int[] paramOrder1, CodeTypeReference paramArrayType1,
											_ParamInfo[] p2, int[] paramOrder2, CodeTypeReference paramArrayType2,
											CodeTypeReference[] types)
		{
			if (2 == p1.Length)
				System.Diagnostics.Debugger.Break();
			// A method using params is always less specific than one not using params
			if (!R.IsNullOrVoidType(paramArrayType1) && R.IsNullOrVoidType(paramArrayType2)) return 2;
			if (!R.IsNullOrVoidType(paramArrayType2) && R.IsNullOrVoidType(paramArrayType1)) return 1;

			// now either p1 and p2 both use params or neither does.

			bool p1Less = false;
			bool p2Less = false;

			for (int i = 0; i < types.Length; i++)
			{
				if (R.IsNullOrVoidType(types[i]))
					continue;

				CodeTypeReference c1, c2;

				//  If a param array is present, then either
				//      the user re-ordered the parameters in which case
				//          the argument to the param array is either an array
				//              in which case the params is conceptually ignored and so paramArrayType1 == null
				//          or the argument to the param array is a single element
				//              in which case paramOrder[i] == p1.Length - 1 for that element
				//      or the user did not re-order the parameters in which case
				//          the paramOrder array could contain indexes larger than p.Length - 1 (see VSW 577286)
				//          so any index >= p.Length - 1 is being put in the param array

				if (!R.IsNullOrVoidType(paramArrayType1) && paramOrder1[i] >= p1.Length - 1)
					c1 = paramArrayType1;
				else
					c1 = p1[paramOrder1[i]].ParameterType;

				if (R.IsNullOrVoidType(paramArrayType2 )&& paramOrder2[i] >= p2.Length - 1)
					c2 = paramArrayType2;
				else
					c2 = p2[paramOrder2[i]].ParameterType;

				if (E.Equals(c1 , c2)) continue;

				switch (FindMostSpecificType(c1, c2, types[i]))
				{
					case 0: return 0;
					case 1: p1Less = true; break;
					case 2: p2Less = true; break;
				}
			}

			// Two way p1Less and p2Less can be equal.  All the arguments are the
			//  same they both equal false, otherwise there were things that both
			//  were the most specific type on....
			if (p1Less == p2Less)
			{
				// if we cannot tell which is a better match based on parameter types (p1Less == p2Less),
				// let's see which one has the most matches without using the params array (the longer one wins).
				if (!p1Less)
				{
					if (p1.Length > p2.Length)
					{
						return 1;
					}
					else if (p2.Length > p1.Length)
					{
						return 2;
					}
				}

				return 0;
			}
			else
			{
				return (p1Less == true) ? 1 : 2;
			}
		}


		int FindMostSpecificType(CodeTypeReference c1, CodeTypeReference c2, CodeTypeReference t)
		{
			// If the two types are exact move on...
				if (E.Equals(c1 , c2))
				return 0;

			if (E.Equals(c1 , t))
				return 1;

			if (E.Equals(c2 ,t))
				return 2;

			bool c1FromC2;
			bool c2FromC1;

			if (R.IsPrimitiveType(c1) && R.IsPrimitiveType(c2))
			{
				c1FromC2 = _resolver.CanConvertTo(c2,c1,_scope);
				c2FromC1 = _resolver.CanConvertTo(c1,c2,_scope);
			}
			else
			{
				c1FromC2 = _resolver.CanConvertTo(c2, c1, _scope,false);
				c2FromC1 = _resolver.CanConvertTo(c1, c2, _scope,false);
			}

			if (c1FromC2 == c2FromC1)
				return 0;

			if (c1FromC2)
			{
				return 2;
			}
			else
			{
				return 1;
			}
		}
		int FindMostSpecificMethod(MethodBase m1, int[] paramOrder1, CodeTypeReference paramArrayType1,
												  MethodBase m2, int[] paramOrder2, CodeTypeReference paramArrayType2,
												  CodeTypeReference[] types, Object[] args)
		{
			// Find the most specific method based on the parameters.
			int res = FindMostSpecific(_GetParamInfos(m1.GetParameters()), paramOrder1, paramArrayType1,
									   _GetParamInfos(m2.GetParameters()), paramOrder2, paramArrayType2, types, args);

			// If the match was not ambigous then return the result.
			if (res != 0)
				return res;

			// Check to see if the methods have the exact same name and signature.
			if (CompareMethodSigAndName(m1, m2))
			{
				// Determine the depth of the declaring types for both methods.
				int hierarchyDepth1 = GetHierarchyDepth(m1.DeclaringType);
				int hierarchyDepth2 = GetHierarchyDepth(m2.DeclaringType);

				// The most derived method is the most specific one.
				if (hierarchyDepth1 == hierarchyDepth2)
				{
					return 0;
				}
				else if (hierarchyDepth1 < hierarchyDepth2)
				{
					return 2;
				}
				else
				{
					return 1;
				}
			}

			// The match is ambigous.
			return 0;
		}
		int FindMostSpecificMethod(MethodBase m1, int[] paramOrder1, CodeTypeReference paramArrayType1,
												  MethodBase m2, int[] paramOrder2, CodeTypeReference paramArrayType2,
												  CodeTypeReference[] types)
		{
			// Find the most specific method based on the parameters.
			int res = FindMostSpecific(_GetParamInfos(m1.GetParameters()), paramOrder1, paramArrayType1,
									   _GetParamInfos(m2.GetParameters()), paramOrder2, paramArrayType2, types);

			// If the match was not ambigous then return the result.
			if (res != 0)
				return res;

			// Check to see if the methods have the exact same name and signature.
			if (CompareMethodSigAndName(m1, m2))
			{
				// Determine the depth of the declaring types for both methods.
				int hierarchyDepth1 = GetHierarchyDepth(m1.DeclaringType);
				int hierarchyDepth2 = GetHierarchyDepth(m2.DeclaringType);

				// The most derived method is the most specific one.
				if (hierarchyDepth1 == hierarchyDepth2)
				{
					return 0;
				}
				else if (hierarchyDepth1 < hierarchyDepth2)
				{
					return 2;
				}
				else
				{
					return 1;
				}
			}

			// The match is ambigous.
			return 0;
		}
		int FindMostSpecificMethod(CodeMemberMethod m1, int[] paramOrder1, CodeTypeReference paramArrayType1,
												  CodeMemberMethod m2, int[] paramOrder2, CodeTypeReference paramArrayType2,
												  CodeTypeReference[] types, Object[] args)
		{
			// Find the most specific method based on the parameters.
			int res = FindMostSpecific(_GetParamInfos(m1.Parameters), paramOrder1, paramArrayType1,
									   _GetParamInfos(m2.Parameters), paramOrder2, paramArrayType2, types, args);

			// If the match was not ambigous then return the result.
			if (res != 0)
				return res;

			// Check to see if the methods have the exact same name and signature.
			if (CompareMethodSigAndName(m1, m2))
			{
				// Determine the depth of the declaring types for both methods.
				int hierarchyDepth1 = GetHierarchyDepth(_resolver.GetScope(m1).DeclaringType);
				int hierarchyDepth2 = GetHierarchyDepth(_resolver.GetScope(m2).DeclaringType);

				// The most derived method is the most specific one.
				if (hierarchyDepth1 == hierarchyDepth2)
				{
					return 0;
				}
				else if (hierarchyDepth1 < hierarchyDepth2)
				{
					return 2;
				}
				else
				{
					return 1;
				}
			}

			// The match is ambigous.
			return 0;
		}
		int FindMostSpecificMethod(CodeMemberMethod m1, int[] paramOrder1, CodeTypeReference paramArrayType1,
												  CodeMemberMethod m2, int[] paramOrder2, CodeTypeReference paramArrayType2,
												  CodeTypeReference[] types)
		{
			// Find the most specific method based on the parameters.
			int res = FindMostSpecific(_GetParamInfos(m1.Parameters), paramOrder1, paramArrayType1,
									   _GetParamInfos(m2.Parameters), paramOrder2, paramArrayType2, types);

			// If the match was not ambigous then return the result.
			if (res != 0)
				return res;

			// Check to see if the methods have the exact same name and signature.
			if (CompareMethodSigAndName(m1, m2))
			{
				// Determine the depth of the declaring types for both methods.
				int hierarchyDepth1 = GetHierarchyDepth(_resolver.GetScope(m1).DeclaringType);
				int hierarchyDepth2 = GetHierarchyDepth(_resolver.GetScope(m2).DeclaringType);

				// The most derived method is the most specific one.
				if (hierarchyDepth1 == hierarchyDepth2)
				{
					return 0;
				}
				else if (hierarchyDepth1 < hierarchyDepth2)
				{
					return 2;
				}
				else
				{
					return 1;
				}
			}

			// The match is ambigous.
			return 0;
		}

		int FindMostSpecificField(FieldInfo cur1, FieldInfo cur2)
		{
			// Check to see if the fields have the same name.
			if (cur1.Name == cur2.Name)
			{
				int hierarchyDepth1 = GetHierarchyDepth(cur1.DeclaringType);
				int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType);

				if (hierarchyDepth1 == hierarchyDepth2)
				{
					Contract.Assert(cur1.IsStatic != cur2.IsStatic, "hierarchyDepth1 == hierarchyDepth2");
					return 0;
				}
				else if (hierarchyDepth1 < hierarchyDepth2)
					return 2;
				else
					return 1;
			}

			// The match is ambigous.
			return 0;
		}

		int FindMostSpecificProperty(PropertyInfo cur1, PropertyInfo cur2)
		{
			// Check to see if the fields have the same name.
			if (cur1.Name == cur2.Name)
			{
				int hierarchyDepth1 = GetHierarchyDepth(cur1.DeclaringType);
				int hierarchyDepth2 = GetHierarchyDepth(cur2.DeclaringType);

				if (hierarchyDepth1 == hierarchyDepth2)
				{
					return 0;
				}
				else if (hierarchyDepth1 < hierarchyDepth2)
					return 2;
				else
					return 1;
			}

			// The match is ambigous.
			return 0;
		}
		int FindMostSpecificProperty(CodeMemberProperty cur1, CodeMemberProperty cur2)
		{
			// Check to see if the fields have the same name.
			if (cur1.Name == cur2.Name)
			{
				int hierarchyDepth1 = GetHierarchyDepth(_resolver.GetScope(cur1).DeclaringType);
				int hierarchyDepth2 = GetHierarchyDepth(_resolver.GetScope(cur2).DeclaringType);

				if (hierarchyDepth1 == hierarchyDepth2)
				{
					return 0;
				}
				else if (hierarchyDepth1 < hierarchyDepth2)
					return 2;
				else
					return 1;
			}

			// The match is ambigous.
			return 0;
		}

		bool CompareMethodSigAndName(MethodBase m1, MethodBase m2)
		{
			ParameterInfo[] params1 = m1.GetParameters();
			ParameterInfo[] params2 = m2.GetParameters();

			if (params1.Length != params2.Length)
				return false;

			int numParams = params1.Length;
			for (int i = 0; i < numParams; i++)
			{
				if (params1[i].ParameterType != params2[i].ParameterType)
					return false;
			}

			return true;
		}
		bool CompareMethodSigAndName(CodeMemberMethod m1, CodeMemberMethod m2)
		{
			var params1 = _GetParamInfos(m1.Parameters, _scope);
			var params2 = _GetParamInfos(m2.Parameters, _scope);

			if (params1.Length != params2.Length)
				return false;

			int numParams = params1.Length;
			for (int i = 0; i < numParams; i++)
			{
				if (!E.Equals(params1[i].ParameterType , params2[i].ParameterType))
					return false;
			}

			return true;
		}
		int GetHierarchyDepth(CodeTypeDeclaration t)
		{
			var result = 0;

			var currentType = _resolver.GetType(t, _scope);
			do
			{
				++result;
				currentType = _resolver.GetBaseType(currentType, _scope);
			} while (currentType != null);

			return result;
		}
		static int GetHierarchyDepth(Type t)
		{
			int depth = 0;

			Type currentType = t;
			do
			{
				depth++;
				currentType = currentType.BaseType;
			} while (currentType != null);

			return depth;
		}

		internal static MethodBase FindMostDerivedNewSlotMeth(MethodBase[] match, int cMatches)
		{
			int deepestHierarchy = 0;
			MethodBase methWithDeepestHierarchy = null;

			for (int i = 0; i < cMatches; i++)
			{
				// Calculate the depth of the hierarchy of the declaring type of the
				// current method.
				int currentHierarchyDepth = GetHierarchyDepth(match[i].DeclaringType);

				// The two methods have the same name, signature, and hierarchy depth.
				// This can only happen if at least one is vararg or generic.
				if (currentHierarchyDepth == deepestHierarchy)
				{
					throw new AmbiguousMatchException("Multiple members matched the specified arguments");
				}

				// Check to see if this method is on the most derived class.
				if (currentHierarchyDepth > deepestHierarchy)
				{
					deepestHierarchy = currentHierarchyDepth;
					methWithDeepestHierarchy = match[i];
				}
			}

			return methWithDeepestHierarchy;
		}

	}
}
