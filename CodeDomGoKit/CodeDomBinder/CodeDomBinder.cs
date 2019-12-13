using System;
using System.Reflection;
using System.Diagnostics.Contracts;
using CultureInfo = System.Globalization.CultureInfo;
using System.ComponentModel;
using System.CodeDom;
using System.Collections.Generic;

namespace CD
{
	
	using R = CodeDomResolver;
	using E = CodeTypeReferenceEqualityComparer;
#if GOKITLIB
	public 
#endif
	partial class CodeDomBinder
	{
		static readonly CodeTypeReference _ObjType = new CodeTypeReference(typeof(object));
		readonly CodeDomResolver _resolver;
		readonly CodeDomResolverScope _scope;
		/// <summary>
		/// Initializes the binder with the given scope
		/// </summary>
		/// <param name="scope">The scope in which the binder is to operate</param>
		public CodeDomBinder(CodeDomResolverScope scope)
		{
			_resolver = scope.Resolver;
			_scope = scope;
		}
		internal static bool HasBindingFlag(BindingFlags flags, BindingFlags target)
		{
			return target == (flags & target);
		}
		internal static bool HasMemberType(MemberTypes flags, MemberTypes target)
		{
			return target == (flags & target);
		}
		private class _BindInfo

		{
			public int[] ArgumentMap;
			public int OriginalSize;
			public bool IsParamArray;

			internal _BindInfo(int[] argumentMap, int originalSize, bool isParamArray)
			{
				ArgumentMap = argumentMap;
				OriginalSize = originalSize;
				IsParamArray = isParamArray;
			}

		}
		private struct _ParamInfo
		{
			public CodeTypeReference ParameterType;
			public Type RuntimeType;
			public string Name;
			public bool IsIn;
			public bool IsOut;
			public bool IsRetval;
			public bool IsOptional;
			public object DefaultValue;
			public bool IsCOMObject;
			public bool IsDefinedParamArray;
		}
		_ParamInfo[] _GetParamInfos(CodeExpressionCollection parms, CodeDomResolverScope scope = null)
		{
			var result = new _ParamInfo[parms.Count];
			for (var i = 0; i < result.Length; i++)
			{
				CodeExpression e = parms[i];
				_ParamInfo p = default(_ParamInfo);
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
				p.ParameterType = _resolver.GetTypeOfExpression(e, _scope);
				result[i] = p;
			}
			return result;
		}
		_ParamInfo[] _GetParamInfos(CodeParameterDeclarationExpressionCollection parms, CodeDomResolverScope scope = null)
		{
			var result = new _ParamInfo[parms.Count];
			for (var i = 0; i < result.Length; i++)
			{
				_ParamInfo p = default(_ParamInfo);
				p.IsOptional = false;
				p.IsRetval = false;
				p.Name = parms[i].Name;
				p.DefaultValue = DBNull.Value;
				var pd = parms[i];
				switch (pd.Direction)
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
				p.ParameterType = pd.Type;
				if (null != scope)
					p.ParameterType = _resolver.GetQualifiedType(pd.Type, scope);
				result[i] = p;

			}
			return result;
		}
		_ParamInfo[] _GetParamInfos(ParameterInfo[] parms)
		{
			var result = new _ParamInfo[parms.Length];
			for (var i = 0; i < result.Length; i++)
			{
				_ParamInfo p = default(_ParamInfo);
				p.IsOptional = parms[i].IsOptional;
				p.IsRetval = parms[i].IsRetval;
				p.Name = parms[i].Name;
				p.RuntimeType = parms[i].ParameterType;
				p.IsCOMObject = p.RuntimeType.IsCOMObject;
				p.DefaultValue = parms[i].DefaultValue;
				p.IsDefinedParamArray = parms[i].IsDefined(typeof(ParamArrayAttribute),true);
				var pd = parms[i];
				p.ParameterType = new CodeTypeReference(parms[i].ParameterType);
				result[i] = p;

			}
			return result;
		}
	}
}