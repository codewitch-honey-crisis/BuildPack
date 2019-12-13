using System;
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom;
using System.Globalization;
using System.Diagnostics.Contracts;

namespace CD
{
	using R = CodeDomResolver;
	using E = CodeTypeReferenceEqualityComparer;
	partial class CodeDomBinder
	{
		/// <summary>
		/// Binds to a method using the specified arguments
		/// </summary>
		/// <param name="flags">The binding flags to use</param>
		/// <param name="match">The candidates to select from</param>
		/// <param name="args">The arguments to use</param>
		/// <param name="modifiers">Not used</param>
		/// <param name="cultureInfo">Not used</param>
		/// <param name="names">The argument names or null</param>
		/// <param name="state">A state object used to reorder arguments</param>
		/// <returns>The method that best fits</returns>
		public object BindToMethod(
			BindingFlags flags, object[] match, ref Object[] args,
			ParameterModifier[] modifiers, CultureInfo cultureInfo, String[] names, out Object state)
		{
			if (null == match)
				throw new ArgumentNullException(nameof(match));
			if (0 == match.Length)
				throw new ArgumentException("The match array cannot be empty.", nameof(match));
			state = null;
			var k = _SplitMatchMethods(match);
			var csm = 0 < k.Key.Length ? BindToMethod(flags, k.Key, ref args, modifiers, cultureInfo, names, out state) : null;
			// if they're not a runtime type we treat the argument as an undefined reference type
			// it won't be type checked against the arguments
			var rsm = 0 < k.Value.Length ? BindToMethod(flags, k.Value, ref args, modifiers,cultureInfo, names,out state ) : null;
			if (null != csm)
			{
				if (null != rsm)
					throw new AmbiguousMatchException("Multiple members matched the target argument types");
				return csm;
			}
			return rsm;
		}

		// This method is passed a set of methods and must choose the best
		// fit.  The methods all have the same number of arguments as the object
		// array args.  On exit, this method will choice the best fit method
		// and coerce the args to match that method.  By match, we mean all primitive
		// arguments are exact matchs and all object arguments are exact or subclasses
		// of the target.  If the target OR is an interface, the object must implement
		// that interface.  There are a couple of exceptions
		// thrown when a method cannot be returned.  If no method matchs the args and
		// ArgumentException is thrown.  If multiple methods match the args then 
		// an AmbiguousMatchException is thrown.
		// 
		// The most specific match will be selected.  
		// 
		/// <summary>
		/// Binds to a method using the specified arguments
		/// </summary>
		/// <param name="flags">The binding flags to use</param>
		/// <param name="match">The candidates to select from</param>
		/// <param name="args">The arguments to use</param>
		/// <param name="modifiers">Not used</param>
		/// <param name="cultureInfo">Not used</param>
		/// <param name="names">The argument names or null</param>
		/// <param name="state">A state object used to reorder arguments</param>
		/// <returns>The method that best fits</returns>
		public MethodBase BindToMethod(
			BindingFlags flags, MethodBase[] match, ref Object[] args,
			ParameterModifier[] modifiers, CultureInfo cultureInfo, String[] names, out Object state)
		{
			if (match == null || match.Length == 0)
				throw new ArgumentException("The array cannot be null or empty", nameof(match));
			Contract.EndContractBlock();

			MethodBase[] candidates = (MethodBase[])match.Clone();

			int i;
			int j;

			state = null;

			#region Map named parameters to candidate parameter postions
			// We are creating an paramOrder array to act as a mapping
			//  between the order of the args and the actual order of the
			//  parameters in the method.  This order may differ because
			//  named parameters (names) may change the order.  If names
			//  is not provided, then we assume the default mapping (0,1,...)
			int[][] paramOrder = new int[candidates.Length][];

			for (i = 0; i < candidates.Length; i++)
			{
				var par = _GetParamInfos(candidates[i].GetParameters());

				// args.Length + 1 takes into account the possibility of a last paramArray that can be omitted
				paramOrder[i] = new int[(par.Length > args.Length) ? par.Length : args.Length];

				if (names == null)
				{
					// Default mapping
					for (j = 0; j < args.Length; j++)
						paramOrder[i][j] = j;
				}
				else
				{
					// Named parameters, reorder the mapping.  If CreateParamOrder fails, it means that the method
					// doesn't have a name that matchs one of the named parameters so we don't consider it any further.
					if (!CreateParamOrder(paramOrder[i], par, names))
						candidates[i] = null;
				}
			}
			#endregion

			var paramArrayTypes = new CodeTypeReference[candidates.Length];

			var argTypes = new CodeTypeReference[args.Length];

			#region Cache the type of the provided arguments
			// object that contain a null are treated as if they were typeless (but match either object 
			// references or value classes).  We mark this condition by placing a null in the argTypes array.
			for (i = 0; i < args.Length; i++)
			{
				if (args[i] != null)
				{
					argTypes[i] = new CodeTypeReference(args[i].GetType());
				}
			}
			#endregion


			// Find the method that matches...
			int CurIdx = 0;
			bool defaultValueBinding = ((flags & BindingFlags.OptionalParamBinding) != 0);

			CodeTypeReference paramArrayType = null;

			#region Filter methods by parameter count and type
			for (i = 0; i < candidates.Length; i++)
			{
				paramArrayType = null;

				// If we have named parameters then we may have a hole in the candidates array.
				if (candidates[i] == null)
					continue;

				// Validate the parameters.
				var par = _GetParamInfos(candidates[i].GetParameters());

				#region Match method by parameter count
				if (par.Length == 0)
				{
					#region No formal parameters
					if (args.Length != 0)
					{
						if ((candidates[i].CallingConvention & CallingConventions.VarArgs) == 0)
							continue;
					}

					// This is a valid routine so we move it up the candidates list.
					paramOrder[CurIdx] = paramOrder[i];
					candidates[CurIdx++] = candidates[i];

					continue;
					#endregion
				}
				else if (par.Length > args.Length)
				{
					#region Shortage of provided parameters
					// If the number of parameters is greater than the number of args then 
					// we are in the situation were we may be using default values.
					for (j = args.Length; j < par.Length - 1; j++)
					{
						if (par[j].DefaultValue == System.DBNull.Value)
							break;
					}

					if (j != par.Length - 1)
						continue;

					if (par[j].DefaultValue == System.DBNull.Value)
					{
						if (0==par[j].ParameterType.ArrayRank)
							continue;

						if (!par[j].IsDefinedParamArray)
							continue;

						paramArrayType = par[j].ParameterType.ArrayElementType;
					}
					#endregion
				}
				else if (par.Length < args.Length)
				{
					#region Excess provided parameters
					// test for the ParamArray case
					int lastArgPos = par.Length - 1;

					if (0==par[lastArgPos].ParameterType.ArrayRank)
						continue;

					if (!par[lastArgPos].IsDefinedParamArray)
						continue;

					if (paramOrder[i][lastArgPos] != lastArgPos)
						continue;

					paramArrayType = par[lastArgPos].ParameterType.ArrayElementType;
					#endregion
				}
				else
				{
					#region Test for paramArray, save paramArray type
					int lastArgPos = par.Length - 1;

					if (0!=par[lastArgPos].ParameterType.ArrayRank
						&& par[lastArgPos].IsDefinedParamArray
						&& paramOrder[i][lastArgPos] == lastArgPos)
					{
						if (!_resolver.CanConvertTo(argTypes[lastArgPos], par[lastArgPos].ParameterType,_scope,false))
							paramArrayType = par[lastArgPos].ParameterType.ArrayElementType;
					}
					#endregion
				}
				#endregion

				CodeTypeReference pCls = null;
				int argsToCheck = (!R.IsNullOrVoidType(paramArrayType)) ? par.Length - 1 : args.Length;

				#region Match method by parameter type
				for (j = 0; j < argsToCheck; j++)
				{
					#region Classic argument coersion checks
					// get the formal type
					pCls = par[j].ParameterType;

					// the type is the same
					if (E.Equals(pCls , argTypes[paramOrder[i][j]]))
						continue;

					// a default value is available
					if (defaultValueBinding && args[paramOrder[i][j]] == Type.Missing)
						continue;

					// the argument was null, so it matches with everything
					if (args[paramOrder[i][j]] == null)
						continue;

					// the type is Object, so it will match everything
					if (E.Equals(pCls ,_ObjType))
						continue;

					// now do a "classic" type check
					if (R.IsPrimitiveType(pCls))
					{
						var val = args[paramOrder[i][j]];
						var type = argTypes[paramOrder[i][j]];
						
						if (type == null || !val.GetType().IsPrimitive || !_resolver.CanConvertTo(type,pCls,_scope))
						{
							break;
						}
					}
					else
					{
						if (argTypes[paramOrder[i][j]] == null)
							continue;

						if (_resolver.CanConvertTo(argTypes[paramOrder[i][j]], pCls,_scope,false))
						{
							var at = argTypes[paramOrder[i][j]];
							var tt = _resolver.TryResolveType(at, _scope) as Type;
							if (null!=tt && tt.IsCOMObject)
							{
								var ct = _resolver.TryResolveType(pCls, _scope) as Type;
								if (null!=ct && ct.IsInstanceOfType(tt))
									continue;
							}
							break;
						}
					}
					#endregion
				}

				if (paramArrayType != null && j == par.Length - 1)
				{
					#region Check that excess arguments can be placed in the param array
					for (; j < args.Length; j++)
					{
						if (R.IsPrimitiveType(paramArrayType))
						{
							var val = args[j];
							var type = argTypes[j];
							
							if (type == null || !_resolver.CanConvertTo(type,paramArrayType,_scope))
								break;
						}
						else
						{
							if (argTypes[j] == null)
								continue;

							if (!_resolver.CanConvertTo(argTypes[j], paramArrayType, _scope))
							{
								var at = argTypes[j];
								var tt = _resolver.TryResolveType(at, _scope) as Type;
								if (null != tt && tt.IsCOMObject)
								{
									var pt = _resolver.TryResolveType(paramArrayType, _scope) as Type;
									if (null != pt && pt.IsInstanceOfType(args[j]))
										continue;
								}
								break;
							}
						}
					}
					#endregion
				}
				#endregion

				if (j == args.Length)
				{
					#region This is a valid routine so we move it up the candidates list
					paramOrder[CurIdx] = paramOrder[i];
					paramArrayTypes[CurIdx] = paramArrayType;
					candidates[CurIdx++] = candidates[i];
					#endregion
				}
			}
			#endregion

			// If we didn't find a method 
			if (CurIdx == 0)
				throw new MissingMethodException("A method with the specified parameters was not found");

			if (CurIdx == 1)
			{
				#region Found only one method
				if (names != null)
				{
					state = new _BindInfo((int[])paramOrder[0].Clone(), args.Length, paramArrayTypes[0] != null);
					ReorderParams(paramOrder[0], args);
				}

				// If the parameters and the args are not the same length or there is a paramArray
				//  then we need to create a argument array.
				ParameterInfo[] parms = candidates[0].GetParameters();

				if (parms.Length == args.Length)
				{
					if (paramArrayTypes[0] != null)
					{
						Object[] objs = new Object[parms.Length];
						int lastPos = parms.Length - 1;
						Array.Copy(args, 0, objs, 0, lastPos);
						var t = _resolver.TryResolveType(paramArrayTypes[0], _scope) as Type;
						if (null != t)
							throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
						objs[lastPos] = Array.CreateInstance(t, 1);
						((Array)objs[lastPos]).SetValue(args[lastPos], 0);
						args = objs;
					}
				}
				else if (parms.Length > args.Length)
				{
					Object[] objs = new Object[parms.Length];

					for (i = 0; i < args.Length; i++)
						objs[i] = args[i];

					for (; i < parms.Length - 1; i++)
						objs[i] = parms[i].DefaultValue;

					if (paramArrayTypes[0] != null)
					{
						var t = _resolver.TryResolveType(paramArrayTypes[0], _scope) as Type;
						if (null != t)
							throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
						objs[i] = Array.CreateInstance(t, 0); // create an empty array for the type
					}
					else
						objs[i] = parms[i].DefaultValue;

					args = objs;
				}
				else
				{
					if ((candidates[0].CallingConvention & CallingConventions.VarArgs) == 0)
					{
						Object[] objs = new Object[parms.Length];
						int paramArrayPos = parms.Length - 1;
						Array.Copy(args, 0, objs, 0, paramArrayPos);
						var t = _resolver.TryResolveType(paramArrayTypes[0], _scope) as Type;
						if (null != t)
							throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
						objs[paramArrayPos] = Array.CreateInstance(t, args.Length - paramArrayPos);
						Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
						args = objs;
					}
				}
				#endregion

				return candidates[0];
			}

			int currentMin = 0;
			bool ambig = false;
			for (i = 1; i < CurIdx; i++)
			{
				#region Walk all of the methods looking the most specific method to invoke
				int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder[currentMin], paramArrayTypes[currentMin],
													candidates[i], paramOrder[i], paramArrayTypes[i], argTypes, args);

				if (newMin == 0)
				{
					ambig = true;
				}
				else if (newMin == 2)
				{
					currentMin = i;
					ambig = false;
				}
				#endregion
			}

			if (ambig)
				throw new AmbiguousMatchException("Multiple members matched the target argument types");

			// Reorder (if needed)
			if (names != null)
			{
				state = new _BindInfo((int[])paramOrder[currentMin].Clone(), args.Length, paramArrayTypes[currentMin] != null);
				ReorderParams(paramOrder[currentMin], args);
			}

			// If the parameters and the args are not the same length or there is a paramArray
			//  then we need to create a argument array.
			ParameterInfo[] parameters = candidates[currentMin].GetParameters();
			if (parameters.Length == args.Length)
			{
				if (paramArrayTypes[currentMin] != null)
				{
					Object[] objs = new Object[parameters.Length];
					int lastPos = parameters.Length - 1;
					Array.Copy(args, 0, objs, 0, lastPos);
					var t = _resolver.TryResolveType(paramArrayTypes[currentMin], _scope) as Type;
					if (null != t)
						throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
					objs[lastPos] = Array.CreateInstance(t, 1);
					((Array)objs[lastPos]).SetValue(args[lastPos], 0);
					args = objs;
				}
			}
			else if (parameters.Length > args.Length)
			{
				Object[] objs = new Object[parameters.Length];

				for (i = 0; i < args.Length; i++)
					objs[i] = args[i];

				for (; i < parameters.Length - 1; i++)
					objs[i] = parameters[i].DefaultValue;

				if (paramArrayTypes[currentMin] != null)
				{
					var t = _resolver.TryResolveType(paramArrayTypes[currentMin], _scope) as Type;
					if (null != t)
						throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
					objs[i] = Array.CreateInstance(t, 0);
				}
				else
				{
					objs[i] = parameters[i].DefaultValue;
				}

				args = objs;
			}
			else
			{
				if ((candidates[currentMin].CallingConvention & CallingConventions.VarArgs) == 0)
				{
					Object[] objs = new Object[parameters.Length];
					int paramArrayPos = parameters.Length - 1;
					Array.Copy(args, 0, objs, 0, paramArrayPos);
					var t = _resolver.TryResolveType(paramArrayTypes[currentMin], _scope) as Type;
					if (null != t)
						throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
					objs[paramArrayPos] = Array.CreateInstance(t, args.Length - paramArrayPos);
					Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
					args = objs;
				}
			}

			return candidates[currentMin];
		}
		
		// This method will create the mapping between the Parameters and the underlying
		//  data based upon the names array.  The names array is stored in the same order
		//  as the values and maps to the parameters of the method.  We store the mapping
		//  from the parameters to the names in the paramOrder array.  All parameters that
		//  don't have matching names are then stored in the array in order.
		private static bool CreateParamOrder(int[] paramOrder, _ParamInfo[] pars, String[] names)
		{
			bool[] used = new bool[pars.Length];

			// Mark which parameters have not been found in the names list
			for (int i = 0; i < pars.Length; i++)
				paramOrder[i] = -1;
			// Find the parameters with names. 
			for (int i = 0; i < names.Length; i++)
			{
				int j;
				for (j = 0; j < pars.Length; j++)
				{
					if (names[i].Equals(pars[j].Name))
					{
						paramOrder[j] = i;
						used[i] = true;
						break;
					}
				}
				// This is an error condition.  The name was not found.  This
				//  method must not match what we sent.
				if (j == pars.Length)
					return false;
			}

			// Now we fill in the holes with the parameters that are unused.
			int pos = 0;
			for (int i = 0; i < pars.Length; i++)
			{
				if (paramOrder[i] == -1)
				{
					for (; pos < pars.Length; pos++)
					{
						if (!used[pos])
						{
							paramOrder[i] = pos;
							pos++;
							break;
						}
					}
				}
			}
			return true;
		}
		/// <summary>
		/// Binds to a method using the specified arguments
		/// </summary>
		/// <param name="flags">The binding flags to use</param>
		/// <param name="match">The candidates to select from</param>
		/// <param name="args">The arguments to use</param>
		/// <param name="modifiers">Not used</param>
		/// <param name="cultureInfo">Not used</param>
		/// <param name="names">The argument names or null</param>
		/// <param name="state">A state object used to reorder arguments</param>
		/// <returns>The method that best fits</returns>
		public CodeMemberMethod BindToMethod(
			BindingFlags flags, CodeMemberMethod[] match, ref Object[] args,
			ParameterModifier[] modifiers, CultureInfo cultureInfo, String[] names, out Object state)
		{
			if (match == null || match.Length == 0)
				throw new ArgumentException("The array cannot be null or empty", nameof(match));
			Contract.EndContractBlock();

			var candidates = (CodeMemberMethod[])match.Clone();

			int i;
			int j;

			state = null;

			#region Map named parameters to candidate parameter postions
			// We are creating an paramOrder array to act as a mapping
			//  between the order of the args and the actual order of the
			//  parameters in the method.  This order may differ because
			//  named parameters (names) may change the order.  If names
			//  is not provided, then we assume the default mapping (0,1,...)
			int[][] paramOrder = new int[candidates.Length][];

			for (i = 0; i < candidates.Length; i++)
			{
				var par = _GetParamInfos(candidates[i].Parameters, _scope) ;

				// args.Length + 1 takes into account the possibility of a last paramArray that can be omitted
				paramOrder[i] = new int[(par.Length > args.Length) ? par.Length : args.Length];

				if (names == null)
				{
					// Default mapping
					for (j = 0; j < args.Length; j++)
						paramOrder[i][j] = j;
				}
				else
				{
					// Named parameters, reorder the mapping.  If CreateParamOrder fails, it means that the method
					// doesn't have a name that matchs one of the named parameters so we don't consider it any further.
					if (!CreateParamOrder(paramOrder[i], par, names))
						candidates[i] = null;
				}
			}
			#endregion

			var paramArrayTypes = new CodeTypeReference[candidates.Length];

			var argTypes = new CodeTypeReference[args.Length];

			#region Cache the type of the provided arguments
			// object that contain a null are treated as if they were typeless (but match either object 
			// references or value classes).  We mark this condition by placing a null in the argTypes array.
			for (i = 0; i < args.Length; i++)
			{
				if (args[i] != null)
				{
					argTypes[i] = new CodeTypeReference(args[i].GetType());
				}
			}
			#endregion

			// Find the method that matches...
			int CurIdx = 0;
			bool defaultValueBinding = ((flags & BindingFlags.OptionalParamBinding) != 0);

			CodeTypeReference paramArrayType = null;

			#region Filter methods by parameter count and type
			for (i = 0; i < candidates.Length; i++)
			{
				paramArrayType = null;

				// If we have named parameters then we may have a hole in the candidates array.
				if (candidates[i] == null)
					continue;

				// Validate the parameters.
				var par = _GetParamInfos(candidates[i].Parameters,_scope);

				#region Match method by parameter count
				if (par.Length == 0)
				{
					#region No formal parameters
					if (args.Length != 0)
					{
						// CodeMemberMethods never have varargs
						//if ((candidates[i].CallingConvention & CallingConventions.VarArgs) == 0)
							continue;
					}

					// This is a valid routine so we move it up the candidates list.
					paramOrder[CurIdx] = paramOrder[i];
					candidates[CurIdx++] = candidates[i];

					continue;
					#endregion
				}
				else if (par.Length > args.Length)
				{
					#region Shortage of provided parameters
					// If the number of parameters is greater than the number of args then 
					// we are in the situation were we may be using default values.
					for (j = args.Length; j < par.Length - 1; j++)
					{
						if (par[j].DefaultValue == System.DBNull.Value)
							break;
					}

					if (j != par.Length - 1)
						continue;

					if (par[j].DefaultValue == System.DBNull.Value)
					{
						if (0 == par[j].ParameterType.ArrayRank)
							continue;

						if (!par[j].IsDefinedParamArray)
							continue;

						paramArrayType = par[j].ParameterType.ArrayElementType;
					}
					#endregion
				}
				else if (par.Length < args.Length)
				{
					#region Excess provided parameters
					// test for the ParamArray case
					int lastArgPos = par.Length - 1;

					if (0 == par[lastArgPos].ParameterType.ArrayRank)
						continue;

					if (!par[lastArgPos].IsDefinedParamArray)
						continue;

					if (paramOrder[i][lastArgPos] != lastArgPos)
						continue;

					paramArrayType = par[lastArgPos].ParameterType.ArrayElementType;
					#endregion
				}
				else
				{
					#region Test for paramArray, save paramArray type
					int lastArgPos = par.Length - 1;

					if (0 != par[lastArgPos].ParameterType.ArrayRank
						&& par[lastArgPos].IsDefinedParamArray
						&& paramOrder[i][lastArgPos] == lastArgPos)
					{
						if (!_resolver.CanConvertTo(argTypes[lastArgPos], par[lastArgPos].ParameterType, _scope, false))
							paramArrayType = par[lastArgPos].ParameterType.ArrayElementType;
					}
					#endregion
				}
				#endregion

				CodeTypeReference pCls = null;
				int argsToCheck = (!R.IsNullOrVoidType(paramArrayType)) ? par.Length - 1 : args.Length;

				#region Match method by parameter type
				for (j = 0; j < argsToCheck; j++)
				{
					#region Classic argument coersion checks
					// get the formal type
					pCls = par[j].ParameterType;

					// the type is the same
					if (E.Equals(pCls, argTypes[paramOrder[i][j]]))
						continue;

					// a default value is available
					if (defaultValueBinding && args[paramOrder[i][j]] == Type.Missing)
						continue;

					// the argument was null, so it matches with everything
					if (args[paramOrder[i][j]] == null)
						continue;

					// the type is Object, so it will match everything
					if (E.Equals(pCls, _ObjType))
						continue;

					// now do a "classic" type check
					if (R.IsPrimitiveType(pCls))
					{
						var val = args[paramOrder[i][j]];
						var type = argTypes[paramOrder[i][j]];

						if (type == null || !val.GetType().IsPrimitive || !_resolver.CanConvertTo(type, pCls, _scope))
						{
							break;
						}
					}
					else
					{
						if (argTypes[paramOrder[i][j]] == null)
							continue;

						if (_resolver.CanConvertTo(argTypes[paramOrder[i][j]], pCls, _scope, false))
						{
							var at = argTypes[paramOrder[i][j]];
							var tt = _resolver.TryResolveType(at, _scope) as Type;
							if (null != tt && tt.IsCOMObject)
							{
								var ct = _resolver.TryResolveType(pCls, _scope) as Type;
								if (null != ct && ct.IsInstanceOfType(tt))
									continue;
							}
							break;
						}
					}
					#endregion
				}

				if (paramArrayType != null && j == par.Length - 1)
				{
					#region Check that excess arguments can be placed in the param array
					for (; j < args.Length; j++)
					{
						if (R.IsPrimitiveType(paramArrayType))
						{
							var val = args[j];
							var type = argTypes[j];

							if (type == null || !_resolver.CanConvertTo(type, paramArrayType, _scope))
								break;
						}
						else
						{
							if (argTypes[j] == null)
								continue;

							if (!_resolver.CanConvertTo(argTypes[j], paramArrayType, _scope))
							{
								var at = argTypes[j];
								var tt = _resolver.TryResolveType(at, _scope) as Type;
								if (null != tt && tt.IsCOMObject)
								{
									var pt = _resolver.TryResolveType(paramArrayType, _scope) as Type;
									if (null != pt && pt.IsInstanceOfType(args[j]))
										continue;
								}
								break;
							}
						}
					}
					#endregion
				}
				#endregion

				if (j == args.Length)
				{
					#region This is a valid routine so we move it up the candidates list
					paramOrder[CurIdx] = paramOrder[i];
					paramArrayTypes[CurIdx] = paramArrayType;
					candidates[CurIdx++] = candidates[i];
					#endregion
				}
			}
			#endregion

			// If we didn't find a method 
			if (CurIdx == 0)
				throw new MissingMethodException("A method with the specified parameters was not found");

			if (CurIdx == 1)
			{
				#region Found only one method
				if (names != null)
				{
					state = new _BindInfo((int[])paramOrder[0].Clone(), args.Length, paramArrayTypes[0] != null);
					ReorderParams(paramOrder[0], args);
				}

				// If the parameters and the args are not the same length or there is a paramArray
				//  then we need to create a argument array.
				var parms = _GetParamInfos(candidates[0].Parameters, _scope);

				if (parms.Length == args.Length)
				{
					if (paramArrayTypes[0] != null)
					{
						Object[] objs = new Object[parms.Length];
						int lastPos = parms.Length - 1;
						Array.Copy(args, 0, objs, 0, lastPos);
						var t = _resolver.TryResolveType(paramArrayTypes[0], _scope) as Type;
						if (null != t)
							throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
						objs[lastPos] = Array.CreateInstance(t, 1);
						((Array)objs[lastPos]).SetValue(args[lastPos], 0);
						args = objs;
					}
				}
				else if (parms.Length > args.Length)
				{
					Object[] objs = new Object[parms.Length];

					for (i = 0; i < args.Length; i++)
						objs[i] = args[i];

					for (; i < parms.Length - 1; i++)
						objs[i] = parms[i].DefaultValue;

					if (paramArrayTypes[0] != null)
					{
						var t = _resolver.TryResolveType(paramArrayTypes[0], _scope) as Type;
						if (null != t)
							throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
						objs[i] = Array.CreateInstance(t, 0); // create an empty array for the type
					}
					else
						objs[i] = parms[i].DefaultValue;

					args = objs;
				}
				else
				{
					// CodeMemberMethods never have varargs
					//if ((candidates[0].CallingConvention & CallingConventions.VarArgs) == 0)
					//{
						Object[] objs = new Object[parms.Length];
						int paramArrayPos = parms.Length - 1;
						Array.Copy(args, 0, objs, 0, paramArrayPos);
						var t = _resolver.TryResolveType(paramArrayTypes[0], _scope) as Type;
						if (null != t)
							throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
						objs[paramArrayPos] = Array.CreateInstance(t, args.Length - paramArrayPos);
						Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
						args = objs;
					//}
				}
				#endregion

				return candidates[0];
			}

			int currentMin = 0;
			bool ambig = false;
			for (i = 1; i < CurIdx; i++)
			{
				#region Walk all of the methods looking the most specific method to invoke
				int newMin = FindMostSpecificMethod(candidates[currentMin], paramOrder[currentMin], paramArrayTypes[currentMin],
													candidates[i], paramOrder[i], paramArrayTypes[i], argTypes, args);

				if (newMin == 0)
				{
					ambig = true;
				}
				else if (newMin == 2)
				{
					currentMin = i;
					ambig = false;
				}
				#endregion
			}

			if (ambig)
				throw new AmbiguousMatchException("Multiple members matched the target argument types");

			// Reorder (if needed)
			if (names != null)
			{
				state = new _BindInfo((int[])paramOrder[currentMin].Clone(), args.Length, paramArrayTypes[currentMin] != null);
				ReorderParams(paramOrder[currentMin], args);
			}

			// If the parameters and the args are not the same length or there is a paramArray
			//  then we need to create a argument array.
			var parameters = _GetParamInfos(candidates[currentMin].Parameters);
			if (parameters.Length == args.Length)
			{
				if (paramArrayTypes[currentMin] != null)
				{
					Object[] objs = new Object[parameters.Length];
					int lastPos = parameters.Length - 1;
					Array.Copy(args, 0, objs, 0, lastPos);
					var t = _resolver.TryResolveType(paramArrayTypes[currentMin], _scope) as Type;
					if (null != t)
						throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
					objs[lastPos] = Array.CreateInstance(t, 1);
					((Array)objs[lastPos]).SetValue(args[lastPos], 0);
					args = objs;
				}
			}
			else if (parameters.Length > args.Length)
			{
				Object[] objs = new Object[parameters.Length];

				for (i = 0; i < args.Length; i++)
					objs[i] = args[i];

				for (; i < parameters.Length - 1; i++)
					objs[i] = parameters[i].DefaultValue;

				if (paramArrayTypes[currentMin] != null)
				{
					var t = _resolver.TryResolveType(paramArrayTypes[currentMin], _scope) as Type;
					if (null != t)
						throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
					objs[i] = Array.CreateInstance(t, 0);
				}
				else
				{
					objs[i] = parameters[i].DefaultValue;
				}

				args = objs;
			}
			else
			{
				// CodeMemberMethods don't have varargs
				//if ((candidates[currentMin].CallingConvention & CallingConventions.VarArgs) == 0)
				//{
					Object[] objs = new Object[parameters.Length];
					int paramArrayPos = parameters.Length - 1;
					Array.Copy(args, 0, objs, 0, paramArrayPos);
					var t = _resolver.TryResolveType(paramArrayTypes[currentMin], _scope) as Type;
					if (null != t)
						throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
					objs[paramArrayPos] = Array.CreateInstance(t, args.Length - paramArrayPos);
					Array.Copy(args, paramArrayPos, (System.Array)objs[paramArrayPos], 0, args.Length - paramArrayPos);
					args = objs;
				//}
			}

			return candidates[currentMin];
		}

		// This method will sort the vars array into the mapping order stored
		//  in the paramOrder array.
		private static void ReorderParams<T>(int[] paramOrder, T[] vars)
		{
			T[] varsCopy = new T[vars.Length];
			for (int i = 0; i < vars.Length; i++)
				varsCopy[i] = vars[i];

			for (int i = 0; i < vars.Length; i++)
				vars[i] = varsCopy[paramOrder[i]];
		}

	}
}
