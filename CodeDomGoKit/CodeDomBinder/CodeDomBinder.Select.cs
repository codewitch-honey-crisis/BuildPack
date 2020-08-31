using System;
using System.Reflection;
using System.CodeDom;
using System.Diagnostics.Contracts;
using System.Collections.Generic;

namespace CD
{
	using E = CodeTypeReferenceEqualityComparer;
	using R = CodeDomResolver;
	partial class CodeDomBinder
	{
		/// <summary>
		/// Selects the property that matches the given signature
		/// </summary>
		/// <param name="flags">The binding flags to use</param>
		/// <param name="match">The properties to evaluate</param>
		/// <param name="types">The parameter types to compare with the signature</param>
		/// <param name="modifiers">Not used</param>
		/// <returns>The property that matches the signature, or null if none could be found</returns>
		public MethodBase SelectMethod(BindingFlags flags, MethodBase[] match, CodeTypeReference[] types, ParameterModifier[] modifiers)
		{
			int i;
			int j;

			// We don't automatically jump out on exact match.
			if (match == null || match.Length == 0)
				throw new ArgumentException("The array cannot be null or empty", nameof(match));

			MethodBase[] candidates = (MethodBase[])match.Clone();

			// Find all the methods that can be described by the types parameter. 
			//  Remove all of them that cannot.
			int CurIdx = 0;
			for (i = 0; i < candidates.Length; i++)
			{
				var par = _GetParamInfos(candidates[i].GetParameters());
				if (par.Length != types.Length)
					continue;
				for (j = 0; j < types.Length; j++)
				{
					var pCls = par[j].ParameterType;
					if (E.Equals(pCls ,types[j]))
						continue;
					if (E.Equals(pCls ,_ObjType))
						continue;
					if (R.IsPrimitiveType(pCls))
					{
						var type = types[j];
						if (!R.IsPrimitiveType(type) ||
							!_resolver.CanConvertTo(type,pCls,_scope))
							break;
					}
					else
					{
						if (!_resolver.CanConvertTo(types[j], pCls, _scope,false))
							break;
					}
				}
				if (j == types.Length)
					candidates[CurIdx++] = candidates[i];
			}
			if (CurIdx == 0)
				return null;
			if (CurIdx == 1)
				return candidates[0];

			// Walk all of the methods looking the most specific method to invoke
			int currentMin = 0;
			bool ambig = false;
			int[] paramOrder = new int[types.Length];
			for (i = 0; i < types.Length; i++)
				paramOrder[i] = i;
			for (i = 1; i < CurIdx; i++)
			{
				int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder, null, candidates[i], paramOrder, null, types, null);
				if (newMin == 0)
					ambig = true;
				else
				{
					if (newMin == 2)
					{
						currentMin = i;
						ambig = false;
						currentMin = i;
					}
				}
			}
			if (ambig)
				throw new AmbiguousMatchException("Multiple members matched the target argument types");
			return candidates[currentMin];
		}

		// Given a set of properties that match the base criteria, select one.
		/// <summary>
		/// Selects the property that matches the given signature
		/// </summary>
		/// <param name="flags">The binding flags to use</param>
		/// <param name="match">The properties to evaluate</param>
		/// <param name="returnType">The return type to evaluate or null to ignore</param>
		/// <param name="indices">The indices to compare with the signature</param>
		/// <param name="modifiers">Not used</param>
		/// <returns>The property that matches the signature, or null if none could be found</returns>
		public PropertyInfo SelectProperty(BindingFlags flags, PropertyInfo[] match, CodeTypeReference returnType,
					CodeTypeReference[] indices, ParameterModifier[] modifiers)
		{
			// Allow a null indexes array. But if it is not null, every element must be non-null as well.
			if (indices != null && !Contract.ForAll(indices, delegate (CodeTypeReference t) { return !R.IsNullOrVoidType(t); }))
			{
				Exception e;  // Written this way to pass the Code Contracts style requirements.
				e = new ArgumentNullException("indexes");
				throw e;
			}
			if (match == null || match.Length == 0)
				throw new ArgumentException("The array cannot be null or empty", nameof(match));
			Contract.EndContractBlock();

			var candidates = (PropertyInfo[])match.Clone();

			int i, j = 0;

			// Find all the properties that can be described by type indexes parameter
			int CurIdx = 0;
			int indexesLength = (indices != null) ? indices.Length : 0;
			for (i = 0; i < candidates.Length; i++)
			{

				if (indices != null)
				{
					var par = _GetParamInfos(candidates[i].GetIndexParameters());
					if (par.Length != indexesLength)
						continue;

					for (j = 0; j < indexesLength; j++)
					{
						var pCls = par[j].ParameterType;

						// If the classes  exactly match continue
						if (E.Equals(pCls,indices[j]))
							continue;
						if (E.Equals(pCls ,_ObjType))
							continue;
						if (R.IsPrimitiveType(pCls))
						{
							var type = indices[j];
							
							if (!R.IsPrimitiveType(type)|| !_resolver.CanConvertTo(type,pCls,_scope))
								break;
						}
						else
						{
							if (!_resolver.CanConvertTo(indices[j], pCls,_scope,false))
								break;
						}
					}
				}

				if (j == indexesLength)
				{
					if (!R.IsNullOrVoidType(returnType))
					{
						if (candidates[i].PropertyType.IsPrimitive)
						{
							if (R.IsPrimitiveType(returnType) ||
								!_resolver.CanConvertTo(returnType,new CodeTypeReference(candidates[i].PropertyType),_scope))
								continue;
						}
						else
						{
							if (!_resolver.CanConvertTo(returnType, new CodeTypeReference(candidates[i].PropertyType), _scope,false))
								continue;
						}
					}
					candidates[CurIdx++] = candidates[i];
				}
			}
			if (CurIdx == 0)
				return null;
			if (CurIdx == 1)
				return candidates[0];

			// Walk all of the properties looking the most specific method to invoke
			int currentMin = 0;
			bool ambig = false;
			int[] paramOrder = new int[indexesLength];
			for (i = 0; i < indexesLength; i++)
				paramOrder[i] = i;
			for (i = 1; i < CurIdx; i++)
			{
				int newMin = FindMostSpecificType(new CodeTypeReference(candidates[currentMin].PropertyType), new CodeTypeReference(candidates[i].PropertyType), returnType);
				if (newMin == 0 && indices != null)
					newMin = FindMostSpecific(_GetParamInfos(candidates[currentMin].GetIndexParameters()),
											  paramOrder,
											  null,
											  _GetParamInfos(candidates[i].GetIndexParameters()),
											  paramOrder,
											  null,
											  indices,
											  null);
				if (newMin == 0)
				{
					newMin = FindMostSpecificProperty(candidates[currentMin], candidates[i]);
					if (newMin == 0)
						ambig = true;
				}
				if (newMin == 2)
				{
					ambig = false;
					currentMin = i;
				}
			}

			if (ambig)
				throw new AmbiguousMatchException("Multiple members matched the target argument types");
			return candidates[currentMin];
		}
		static KeyValuePair<CodeMemberMethod[], MethodInfo[]> _SplitMatchMethods(object[] match)
		{
			var cml = new List<CodeMemberMethod>();
			var rml = new List<MethodInfo>();
			for (var i = 0; i < match.Length; i++)
			{
				var m = match[i];
				var cm = m as CodeMemberMethod;
				if (null != cm)
					cml.Add(cm);
				else
				{
					var rm = m as MethodInfo;
					if (null != rm)
						rml.Add(rm);
				}
			}
			var cma = cml.ToArray();
			var rma = rml.ToArray();
			return new KeyValuePair<CodeMemberMethod[], MethodInfo[]>(cma, rma);
		}
		static KeyValuePair<CodeMemberProperty[], PropertyInfo[]> _SplitMatchProperties(object[] match)
		{
			var cml = new List<CodeMemberProperty>();
			var rml = new List<PropertyInfo>();
			for (var i = 0; i < match.Length; i++)
			{
				var m = match[i];
				var cm = m as CodeMemberProperty;
				if (null != cm)
					cml.Add(cm);
				else
				{
					var rm = m as PropertyInfo;
					if (null != rm)
						rml.Add(rm);
				}
			}
			var cma = cml.ToArray();
			var rma = rml.ToArray();
			return new KeyValuePair<CodeMemberProperty[], PropertyInfo[]>(cma, rma);
		}
		/// <summary>
		/// Selects the method from a given group of methods whose signature best matches the indicated signature
		/// </summary>
		/// <param name="bindingAttr">The <see cref="BindingFlags"/> to use</param>
		/// <param name="match">The candidate members to evaluate</param>
		/// <param name="types">The types to evaluate. If any are null or <see cref="System.Void"/> they are ignored</param>
		/// <param name="modifiers">The parameter modifiers - not currently used</param>
		/// <returns>The method that best matches the given signature, or null if not found</returns>
		public object SelectMethod(BindingFlags bindingAttr, object[] match, CodeTypeReference[] types, ParameterModifier[] modifiers)
		{
			if (null == match)
				throw new ArgumentNullException(nameof(match));
			if (0 == match.Length)
				throw new ArgumentException("The match array cannot be empty.", nameof(match));
			var k = _SplitMatchMethods(match);
			var csm = 0 < k.Key.Length ? SelectMethod(bindingAttr, k.Key, types, modifiers) : null;
			// if they're not a runtime type we treat the argument as an undefined reference type
			// it won't be type checked against the arguments
			var rsm = 0 < k.Value.Length ? SelectMethod(bindingAttr, k.Value, types, modifiers) : null;
			if (null != csm)
			{
				if (null != rsm)
					throw new AmbiguousMatchException("Multiple members matched the target argument types");
				return csm;
			}
			return rsm;
		}
		/// <summary>
		/// Selects the property from a given group of properties whose signature best matches the indicated signature
		/// </summary>
		/// <param name="bindingAttr">The <see cref="BindingFlags"/> to use</param>
		/// <param name="match">The candidate members to evaluate</param>
		/// <param name="returnType">The return type to evaluate, or <see cref="System.Void"/> or null to ignore</param>
		/// <param name="types">The types to evaluate. If any are null or <see cref="System.Void"/> they are ignored</param>
		/// <param name="modifiers">The parameter modifiers - not currently used</param>
		/// <returns>The property that best matches the given signature, or null if not found</returns>
		public object SelectProperty(BindingFlags bindingAttr, object[] match, CodeTypeReference returnType, CodeTypeReference[] types, ParameterModifier[] modifiers)
		{
			if (null == match)
				throw new ArgumentNullException(nameof(match));
			if (0 == match.Length)
				throw new ArgumentException("The match array cannot be empty.", nameof(match));
			var k = _SplitMatchProperties(match);
			var csm = 0 < k.Key.Length ? SelectProperty(bindingAttr, k.Key, returnType, types, modifiers) : null;
			// if they're not a runtime type we treat the argument as an undefined reference type
			// it won't be type checked against the arguments
			var rsm = 0 < k.Value.Length ? SelectProperty(bindingAttr, k.Value, returnType, types, modifiers) : null;
			if (null != csm)
			{
				if (null != rsm)
					throw new AmbiguousMatchException("Multiple members matched the target argument types");
				return csm;
			}
			return rsm;
		}
		/// <summary>
		/// Selects the property that matches the given signature
		/// </summary>
		/// <param name="flags">The binding flags to use</param>
		/// <param name="match">The properties to evaluate</param>
		/// <param name="types">The parameter types to compare with the signature</param>
		/// <param name="modifiers">Not used</param>
		/// <returns>The property that matches the signature, or null if none could be found</returns>
		public CodeMemberMethod SelectMethod(BindingFlags flags, CodeMemberMethod[] match, CodeTypeReference[] types, ParameterModifier[] modifiers)
		{
			int i;
			int j;
			// We don't automatically jump out on exact match.
			if (match == null || match.Length == 0)
				throw new ArgumentException("The array cannot be null or empty", nameof(match));

			CodeMemberMethod[] candidates = (CodeMemberMethod[])match.Clone();

			// Find all the methods that can be described by the types parameter. 
			//  Remove all of them that cannot.
			int CurIdx = 0;
			for (i = 0; i < candidates.Length; i++)
			{
				var par = _GetParamInfos(candidates[i].Parameters, _scope);
				if (par.Length != types.Length)
					continue;
				for (j = 0; j < types.Length; j++)
				{
					var pCls = par[j].ParameterType;
					if (null == par[j].ParameterType)
						continue;
					pCls = _resolver.GetQualifiedType(pCls, _scope);
					var t = _resolver.GetQualifiedType(types[j],_scope);
					if (E.Equals(pCls, t))
						continue;
					if (0 == pCls.ArrayRank && "System.Object" == pCls.BaseType)
						continue;
					if (R.IsPrimitiveType(pCls))
					{
						var type = types[j];

						if (!R.IsPrimitiveType(type) || !_resolver.CanConvertTo(type, pCls, _scope, true))
							break;
					}
					else
					{
						if (!_resolver.CanConvertTo(types[j], pCls, _scope, false))
							break;
					}
				}
				if (j == types.Length)
					candidates[CurIdx++] = candidates[i];
			}
			if (CurIdx == 0)
				return null;
			if (CurIdx == 1)
				return candidates[0];

			// Walk all of the methods looking the most specific method to invoke
			int currentMin = 0;
			bool ambig = false;
			int[] paramOrder = new int[types.Length];
			for (i = 0; i < types.Length; i++)
				paramOrder[i] = i;
			for (i = 1; i < CurIdx; i++)
			{
				int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder, null,candidates[i], paramOrder, null,types, null);
				if (newMin == 0)
					ambig = true;
				else
				{
					if (newMin == 2)
					{
						currentMin = i;
						ambig = false;
						currentMin = i;
					}
				}
			}
			if (ambig)
				throw new AmbiguousMatchException("Multiple members matched the target argument types");
			return candidates[currentMin];
		}

		// Given a set of properties that match the base criteria, select one.
		/// <summary>
		/// Selects the property that matches the given signature
		/// </summary>
		/// <param name="flags">The binding flags to use</param>
		/// <param name="match">The properties to evaluate</param>
		/// <param name="returnType">The return type to evaluate or null to ignore</param>
		/// <param name="indices">The indices to compare with the signature</param>
		/// <param name="modifiers">Not used</param>
		/// <returns>The property that matches the signature, or null if none could be found</returns>
		public CodeMemberProperty SelectProperty(BindingFlags flags, CodeMemberProperty[] match, CodeTypeReference returnType,
					CodeTypeReference[] indices, ParameterModifier[] modifiers)
		{
			// Allow a null indexes array. But if it is not null, every element must be non-null as well.
			if (indices != null && !Contract.ForAll(indices, delegate (CodeTypeReference t) { return t != null && 0 != string.Compare(t.BaseType, "System.Void", StringComparison.InvariantCulture); }))
			{
				Exception e;  // Written this way to pass the Code Contracts style requirements.
				e = new ArgumentNullException("indexes");
				throw e;
			}
			if (match == null || match.Length == 0)
				throw new ArgumentException("The array cannot be null or empty", nameof(match));


			var candidates = (CodeMemberProperty[])match.Clone();

			int i, j = 0;

			// Find all the properties that can be described by type indexes parameter
			int CurIdx = 0;
			int indexesLength = (indices != null) ? indices.Length : 0;
			for (i = 0; i < candidates.Length; i++)
			{

				if (indices != null)
				{
					var par = candidates[i].Parameters;
					if (par.Count != indexesLength)
						continue;

					for (j = 0; j < indexesLength; j++)
					{
						var pCls = par[j].Type;
						if (null == pCls || 0 == string.Compare("System.Void", pCls.BaseType, StringComparison.InvariantCulture))
							continue;
						// If the classes  exactly match continue
						if (pCls == indices[j])
							continue;
						if (0 == pCls.ArrayRank && 0 == string.Compare("System.Object", pCls.BaseType))
							continue;

						if (CodeDomResolver.IsPrimitiveType(pCls))
						{
							var type = indices[j];

							if (!CodeDomResolver.IsPrimitiveType(type) || !_resolver.CanConvertTo(type, pCls, _scope))
								break;
						}
						else
						{
							if (!_resolver.CanConvertTo(indices[j], pCls, _scope, false))
								break;
						}
					}
				}

				if (j == indexesLength)
				{
					if (returnType != null)
					{
						if (CodeDomResolver.IsPrimitiveType(candidates[i].Type))
						{
							if (CodeDomResolver.IsPrimitiveType(returnType) ||
								!_resolver.CanConvertTo(returnType, candidates[i].Type, _scope))
								continue;
						}
						else
						{
							if (!_resolver.CanConvertTo(returnType, candidates[i].Type, _scope, false))
								continue;
						}
					}
					candidates[CurIdx++] = candidates[i];
				}
			}
			if (CurIdx == 0)
				return null;
			if (CurIdx == 1)
				return candidates[0];

			// Walk all of the properties looking the most specific method to invoke
			int currentMin = 0;
			bool ambig = false;
			int[] paramOrder = new int[indexesLength];
			for (i = 0; i < indexesLength; i++)
				paramOrder[i] = i;
			for (i = 1; i < CurIdx; i++)
			{
				int newMin = FindMostSpecificType(candidates[currentMin].Type, candidates[i].Type, returnType);
				if (newMin == 0 && indices != null)
					newMin = FindMostSpecific(_GetParamInfos(candidates[currentMin].Parameters, _scope),
											  paramOrder,
											  null,
											  _GetParamInfos(candidates[i].Parameters, _scope),
											  paramOrder,
											  null,
											  indices,
											  null);
				if (newMin == 0)
				{
					newMin = FindMostSpecificProperty(candidates[currentMin], candidates[i]);
					if (newMin == 0)
						ambig = true;
				}
				if (newMin == 2)
				{
					ambig = false;
					currentMin = i;
				}
			}

			if (ambig)
				throw new AmbiguousMatchException("Multiple members matched the target argument types");
			return candidates[currentMin];
		}
	}

}
