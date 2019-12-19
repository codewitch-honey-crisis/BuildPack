using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;

namespace CD
{
	/// <summary>
	/// Provides services for doing type and scope resolution on CodeDOM graphs
	/// </summary>
#if GOKITLIB
	public 
#endif
	partial class CodeDomResolver
	{
		const int _ResolveAssemblies = 2;
		const int _ResolveCompileUnits = 1;
		static readonly object _parentKey = new object();
		static readonly object _rootKey = new object();
		IDictionary<CodeTypeReference, Type> _typeCache = new Dictionary<CodeTypeReference, Type>(CodeTypeReferenceEqualityComparer.Default);
		/// <summary>
		/// Retrieves the compile units list the resolver draws on
		/// </summary>
		/// <remarks>Be sure to call Refresh() and possibly ClearCache() after adding and removing compile units</remarks>
		public IList<CodeCompileUnit> CompileUnits { get; } = new List<CodeCompileUnit>();
		/// <summary>
		/// Creates a new CodeDomResolver
		/// </summary>
		public CodeDomResolver()
		{
			
		}
		
		internal IDictionary<string, CodeTypeReference> GetArgumentTypes(CodeDomResolverScope scope)
		{
			var result = new Dictionary<string, CodeTypeReference>();
			var meth = scope.Member as CodeMemberMethod;
			if (null != meth)
				foreach (CodeParameterDeclarationExpression arg in meth.Parameters)
					result.Add(arg.Name, arg.Type);
			var prop = scope.Member as CodeMemberProperty;
			if (null != prop)
				foreach (CodeParameterDeclarationExpression arg in prop.Parameters)
					result.Add(arg.Name, arg.Type);
			return result;
		}
		
		internal IDictionary<string, CodeTypeReference> GetVariableTypes(CodeDomResolverScope scope)
		{
			var result = new Dictionary<string, CodeTypeReference>();
			if (null == scope.Member || null == scope.Statement)
				return result;
			// we have to trace to get the ones in scope - they may be partially resolved if they're "var"
			foreach(var v in CodeDomVariableTracer.Trace(scope.Member,scope.Statement))
				result.Add(v.Name, v.Type);
			return result;
		}
		static bool _TraceVarDecls(CodeStatement s, CodeStatement target, IDictionary<string, CodeTypeReference> result)
		{

			// TODO: I don't think this works
			if (s == target)
				return true;
			var ls = s as CodeLabeledStatement;
			if (null != ls)
			{
				var l = new Dictionary<string, CodeTypeReference>();
				var v = ls.Statement as CodeVariableDeclarationStatement;
				if (null != v)
					l.Add(v.Name, v.Type);
				else if (_TraceVarDecls(ls.Statement, target, l))
				{
					foreach (var ll in l)
						result.Add(ll);
					return true;
				}

			}
			var i = s as CodeIterationStatement;
			if (null != i)
			{
				var l = new Dictionary<string, CodeTypeReference>();
				if (i.InitStatement != null)
				{
					var v = i.InitStatement as CodeVariableDeclarationStatement;
					if (null != v)
						l.Add(v.Name, v.Type);
					else if (_TraceVarDecls(i.InitStatement, target, l))
					{
						foreach (var ll in l)
							result.Add(ll);
						return true;
					}
				}
				foreach (CodeStatement ts in i.Statements)
				{
					var v = ts as CodeVariableDeclarationStatement;
					if (null != v)
						l.Add(v.Name, v.Type);
					else if (_TraceVarDecls(ts, target, l))
					{
						foreach (var ll in l)
							result.Add(ll);
						return true;
					}
				}
				if (i.IncrementStatement != null)
				{
					var v = i.IncrementStatement as CodeVariableDeclarationStatement;
					if (null != v)
						l.Add(v.Name, v.Type);
					else if (_TraceVarDecls(i.IncrementStatement, target, l))
					{
						foreach (var ll in l)
							result.Add(ll);
						return true;
					}
				}
			}
			var c = s as CodeConditionStatement;
			if (null != c)
			{
				var l = new Dictionary<string, CodeTypeReference>();
				foreach (CodeStatement ts in c.TrueStatements)
				{
					var v = ts as CodeVariableDeclarationStatement;
					if (null != v)
						l.Add(v.Name, v.Type);
					else if (_TraceVarDecls(ts, target, l))
					{
						foreach (var ll in l)
							result.Add(ll);
						return true;
					}
				}
				l.Clear();
				foreach (CodeStatement fs in c.FalseStatements)
				{
					var v = fs as CodeVariableDeclarationStatement;
					if (null != v)
						l.Add(v.Name, v.Type);
					else if (_TraceVarDecls(fs, target, l))
					{
						foreach (var ll in l)
							result.Add(ll);
						return true;
					}
				}
			}
			return false;
		}
		bool _IsInterface(CodeTypeReference r, CodeDomResolverScope scope)
		{
			if (0 < r.ArrayRank && null != r.ArrayElementType)
				return false; // arrays are not interfaces;
			var t = _ResolveType(r, scope);
			var td = t as CodeTypeDeclaration;
			if (null != td)
				return td.IsInterface;
			var tt = t as Type;
			if (null != tt)
				return tt.IsInterface;
			throw new TypeLoadException(string.Format("Could not resolve type {0}", CodeDomUtility.ToString(r)));
		}
		/// <summary>
		/// Indicates whether or not the type is primitive
		/// </summary>
		/// <param name="type">The type</param>
		/// <returns>True if the type is a primitive .NET type, otherwise false</returns>
		public static bool IsPrimitiveType(CodeTypeReference type)
		{
			if (0 < type.ArrayRank && null != type.ArrayElementType)
				return false;
			if (0 < type.TypeArguments.Count)
				return false;

			switch (type.BaseType)
			{
				case "System.Boolean":
				case "System.Char":
				case "System.String":
				case "System.SByte":
				case "System.Byte":
				case "System.Int16":
				case "System.UInt16":
				case "System.Int32":
				case "System.UInt32":
				case "System.Int64":
				case "System.UInt64":
				case "System.Single":
				case "System.Double":
				case "System.Decimal":
					return true;
			}
			return false;
		}
		/// <summary>
		/// Translates an intrinsic Slang/C# type into a .NET type, or pass through
		/// </summary>
		/// <param name="typeName">The type name</param>
		/// <returns>A system type name</returns>
		public static string TranslateIntrinsicType(string typeName)
		{
			switch(typeName)
			{
				case "char":
					return "System.Char";
				case "string":
					return "System.String";
				case "sbyte":
					return "System.SByte";
				case "byte":
					return "System.Byte";
				case "short":
					return "System.Int16";
				case "ushort":
					return "System.UInt16";
				case "int":
					return "System.Int32";
				case "uint":
					return "System.UInt32";
				case "long":
					return "System.Int64";
				case "ulong":
					return "System.UInt64";
				case "float":
					return "System.Single";
				case "double":
					return "System.Double";
				case "decimal":
					return "System.Decimal";
			}
			return typeName;
		}
		/// <summary>
		/// Indicates whether or not one type can be converted to another
		/// </summary>
		/// <param name="from">The type to convert from</param>
		/// <param name="to">The type to convert to</param>
		/// <param name="scope">The scope to use for the evaluation, or null to use <paramref name="from"/>'s scope</param>
		/// <param name="useTypeConversion">True to use .NET's type conversion capabilities or false to simply check if one type is polymorphic with another</param>
		/// <returns>True if the conversion can be performed, otherwise false</returns>
		public bool CanConvertTo(CodeTypeReference from, CodeTypeReference to, CodeDomResolverScope scope = null, bool useTypeConversion = true)
		{
			if (null == from)
				from = new CodeTypeReference(typeof(void));
			if (null == to)
				to = new CodeTypeReference(typeof(void));
			if (CodeTypeReferenceEqualityComparer.Equals(from, to))
				return true;
			if (null == scope)
				scope = GetScope(from);
			var t1 = TryResolveType(from, scope);
			if (null == t1)
				throw new TypeLoadException(string.Format("The type {0} could not be resolved", CodeDomUtility.ToString(from)));
			var t2 = TryResolveType(to, scope);
			if (null == t2)
				throw new TypeLoadException(string.Format("The type {0} could not be resolved", CodeDomUtility.ToString(to)));

			var type1 = t1 as Type;
			var type2 = t2 as Type;
			if (null != type1 && null != type2)
			{
				if (type2.IsAssignableFrom(type1))
					return true;
				if (useTypeConversion)
				{
					TypeConverter typeConverter = TypeDescriptor.GetConverter(type1);
					if (null != typeConverter && typeConverter.CanConvertTo(type2))
						return true;
				}
				return false;
			}
			var decl1 = t1 as CodeTypeDeclaration;
			var decl2 = t2 as CodeTypeDeclaration;
			if (null != decl1)
			{
				if (null == scope)
					scope = GetScope(decl1);
				if (null != decl2 && decl1.IsPartial && decl2.IsPartial && 0 == string.Compare(GetBaseNameOfType(decl1, scope), GetBaseNameOfType(decl2, scope), StringComparison.InvariantCulture))
					return true; // support for partial classes
				var bts = new HashSet<CodeTypeReference>(CodeTypeReferenceEqualityComparer.Default);

				for (int ic = decl1.BaseTypes.Count, i = 0; i < ic; ++i)
					bts.Add(GetQualifiedType(decl1.BaseTypes[i], scope));
				CodeTypeReference ctr = null;
				if (null != decl2)
					ctr = GetType(decl2, scope);
				else
					ctr = new CodeTypeReference(type2);
				return bts.Contains(ctr);
			}
			//if(null!=decl2)
			// a runtime type cannot be converted to a declared type,obviously since it doesn't even know about it
			return false;
		}
		/// <summary>
		/// Retrieves the base type of a <see cref="CodeTypeDeclaration"/> or of a <see cref="System.Type"/>
		/// </summary>
		/// <param name="type">The type to evaluate</param>
		/// <param name="scope">The scope in which evaluation occurs, or null to use the type</param>
		/// <returns>A type reference that refers to the base type</returns>
		public CodeTypeReference GetBaseType(object type, CodeDomResolverScope scope = null)
		{
			var tr = type as CodeTypeReference;
			if (null != tr)
			{
				if (null == scope)
					GetScope(tr);
				type = _ResolveType(tr, scope);
			}
			var td = type as CodeTypeDeclaration;
			if (null != td)
			{
				if (0 == td.BaseTypes.Count)
					return new CodeTypeReference((td.IsStruct || td.IsEnum) ? (td.IsEnum ? typeof(ValueType) : typeof(Enum)) : typeof(object));
				if (null == scope)
					scope = GetScope(td);
				if (_IsInterface(td.BaseTypes[0], scope))
					return new CodeTypeReference((td.IsStruct || td.IsEnum) ? (td.IsEnum ? typeof(ValueType) : typeof(Enum)) : typeof(object));
				return GetQualifiedType(td.BaseTypes[0], scope);
			}
			var t = type as Type;
			if (null != t)
			{
				var bt = t.BaseType;
				if (null == bt)
					return null;
				return new CodeTypeReference(bt);
			}
			throw new ArgumentException("The type must be a type, a type declartion or a type reference");
		}
		internal static CodeTypeReference GetTypeForMember(MemberInfo member)
		{
			if (null == member)
				throw new ArgumentNullException(nameof(member));
			var e = member as EventInfo;
			if (null != e)
				return new CodeTypeReference(e.EventHandlerType);
			var f = member as FieldInfo;
			if (null != f)
				return new CodeTypeReference(f.FieldType);
			var p = member as PropertyInfo;
			if (null != p)
				return new CodeTypeReference(p.PropertyType);
			var m = member as MethodInfo;
			if (null != m)
				return new CodeTypeReference(m.ReturnType);
			return null;
		}
		internal static CodeTypeReference GetTypeForMember(CodeTypeMember member)
		{
			if (null == member)
				throw new ArgumentNullException(nameof(member));
			var f = member as CodeMemberField;
			if (null != f)
			{
				if (null == f.Type)
					throw new InvalidOperationException("The field declaration's type was null.");
				return f.Type;
			}
			var e = member as CodeMemberEvent;
			if (null != e)
			{
				if (null == e.Type)
					throw new InvalidOperationException("The event declaration's type was null");
				return e.Type;
			}
			var m = member as CodeMemberMethod;
			if (null != m)
			{
				if (null == m.ReturnType)
					return new CodeTypeReference(typeof(void));
				return m.ReturnType;
			}
			var p = member as CodeMemberProperty;
			if (null != p)
			{
				if (null == p.Type)
					throw new InvalidOperationException("The property declaration's type was null");
				return p.Type;
			}
			throw new InvalidOperationException("The specified member does not have a type");
		}

		CodeDomResolverScope _FillScope(CodeDomResolverScope result)
		{
			//if (result.TypeRef != null && "System.Collections.Generic.IEnumerable`1" == result.TypeRef.BaseType)
			//	System.Diagnostics.Debugger.Break();
			CodeCompileUnit ccu = null;
			object p;
			if (null == result.Expression)
			{
				if (null != result.TypeRef)
				{
					p = result.TypeRef;
					if (null == ccu)
						ccu = _GetRef(p, _rootKey) as CodeCompileUnit;
					while (null != (p = _GetRef(p, _parentKey)))
					{
						var expr = p as CodeExpression;
						if (null != expr)
						{
							result.Expression = expr;
							break;
						}
					}
				}
			}
			if (null == result.Statement)
			{
				if (null != result.Expression)
				{
					p = result.Expression;
					if (null == ccu)
						ccu = _GetRef(p, _rootKey) as CodeCompileUnit;
					while (null != (p = _GetRef(p, _parentKey)))
					{
						var stmt = p as CodeStatement;
						if (null == ccu)
							ccu = _GetRef(p, _rootKey) as CodeCompileUnit;
						if (null != stmt)
						{
							result.Statement = stmt;
							break;
						}
					}
				} else if (null != result.TypeRef)
				{
					p = result.TypeRef;
					if (null == ccu)
						ccu = _GetRef(p, _rootKey) as CodeCompileUnit;
					while (null != (p = _GetRef(p, _parentKey)))
					{
						var stmt = p as CodeStatement;
						if (null != stmt)
						{
							result.Statement = stmt;
							break;
						}
					}
				}
			}
			if (null == result.Member)
			{
				p = null;
				if (null != result.Statement)
				{
					p = result.Statement;
				}
				else if (null != result.Expression)
					p = result.Expression;
				if (null != p)
				{
					if (null == ccu)
						ccu = _GetRef(p, _rootKey) as CodeCompileUnit;
					while (null != (p = _GetRef(p, _parentKey)))
					{
						var mbr = p as CodeTypeMember;
						if (null != mbr)
						{
							result.Member = mbr;
							break;
						}
					}
				}
			}
			p = null;
			if (0 < result.Types.Count)
				p = result.Types[0];
			else if (null != result.Member)
				p = result.Member;
			else if (null != result.Statement)
				p = result.Statement;
			else if (null != result.Expression)
				p = result.Expression;
			else if (null != result.TypeRef)
				p = result.TypeRef;
			if (null != p)
			{
				if (null == ccu)
					ccu = _GetRef(p, _rootKey) as CodeCompileUnit;
				while (null != (p = _GetRef(p, _parentKey)))
				{
					var td = p as CodeTypeDeclaration;
					if (null != td)
						result.Types.Add(td);
				}
			}
			if (null == result.Namespace)
			{
				p = null;
				if (0 != result.Types.Count)
					p = result.Types[0];
				else if (null != result.Member)
					p = result.Member;
				if (null != p)
				{
					if (null == ccu)
						ccu = _GetRef(p, _rootKey) as CodeCompileUnit;
					while (null != (p = _GetRef(p, _parentKey)))
					{
						var ns = p as CodeNamespace;
						if (null != ns)
						{
							result.Namespace = ns;
							break;
						}
					}
				}
			}
			if (null == result.CompileUnit)
			{
				p = null;
				if (null != result.Namespace)
					p = result.Namespace;
				while (null != (p = _GetRef(p, _parentKey)))
				{
					var cu = p as CodeCompileUnit;
					if (null != cu)
					{
						result.CompileUnit = cu;
						break;
					}
				}
			}
			if (null == result.CompileUnit)
			{
				// last ditch we go for the root key
				result.CompileUnit = ccu;
			}

			return result;
		}
		internal HashSet<string> GetPropertyNames(CodeDomResolverScope scope)
		{
			var result = new HashSet<string>();
			var t = scope.DeclaringType;
			if (null != t)
			{
				var binder = new CodeDomBinder(scope);
				var members = binder.GetMembers(t, MemberTypes.Property, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var m in members)
				{
					var cpi = m as CodeMemberProperty;
					if (null != cpi)
						result.Add(cpi.Name);
					var pi = m as PropertyInfo;
					if (null != pi)
					{
						if (!pi.IsSpecialName)
							result.Add(pi.Name);
					}
				}
			}
			return result;
		}
		/// <summary>
		/// Indicates whether the type reference is null or refers to <see cref="System.Void"/>
		/// </summary>
		/// <param name="type">The type to evaluate</param>
		/// <returns>True if the type is null or void, otherwise false</returns>
		public static bool IsNullOrVoidType(CodeTypeReference type)
		{
			return null == type || (0 == type.ArrayRank && 0 == string.Compare("System.Void", type.BaseType,StringComparison.InvariantCulture));
				
		}
		/// <summary>
		/// Indicates whether the specified type is a value type
		/// </summary>
		/// <param name="type">The type</param>
		/// <param name="scope">The scope or null</param>
		/// <returns>True if the type is a value type, otherwise false</returns>
		public bool IsValueType(CodeTypeReference type,CodeDomResolverScope scope=null)
		{
			if (IsNullOrVoidType(type))
				return false;
			if (0 < type.ArrayRank)
				return false;
			if(null==scope)
			{
				scope = GetScope(type);
			}
			var t = TryResolveType(type, scope);
			if (null == t)
				throw new TypeLoadException("Unable to resolve type");
			var rt = t as Type;
			if(null!=rt)
			{
				return rt.IsValueType;
			}
			var td = t as CodeTypeDeclaration;
			return td.IsEnum || td.IsStruct;
			
		}
		internal HashSet<string> GetFieldNames(CodeDomResolverScope scope)
		{
			var result = new HashSet<string>();
			var t = scope.DeclaringType;
			if (null != t)
			{
				var binder = new CodeDomBinder(scope);
				
				var members = binder.GetMembers(t, MemberTypes.Field, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
				foreach (var m in members)
				{
					var cpi = m as CodeMemberField;
					if (null != cpi)
						result.Add(cpi.Name);
					var pi = m as FieldInfo;
					if (null != pi)
					{
						if (!pi.IsSpecialName)
							result.Add(pi.Name);
					}
				}
			}
			return result;
		}
		internal HashSet<string> GetEventNames(CodeDomResolverScope scope)
		{
			var result = new HashSet<string>();
			var t = scope.DeclaringType;
			if (null != t)
			{
				var binder = new CodeDomBinder(scope);
				var members = binder.GetMembers(t, MemberTypes.Event, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var m in members)
				{
					var cpi = m as CodeMemberEvent;
					if (null != cpi)
						result.Add(cpi.Name);
					var pi = m as EventInfo;
					if (null != pi)
					{
						if (!pi.IsSpecialName)
							result.Add(pi.Name);
					}
				}
			}
			return result;
		}
		internal HashSet<string> GetMethodNames(CodeDomResolverScope scope)
		{
			var result = new HashSet<string>();
			var t = scope.DeclaringType;
			if (null != t)
			{
				var binder = new CodeDomBinder(scope);
				var members = binder.GetMembers(t, MemberTypes.Method, BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var m in members)
				{
					var cpi = m as CodeMemberMethod;
					if (null != (cpi as CodeConstructor))
						cpi = null;
					if (null != cpi)
						result.Add(cpi.Name);
					var pi = m as MethodInfo;
					if (null!=pi && pi.IsConstructor)
						pi = null;
					if (null != pi)
					{
						if (!pi.IsSpecialName)
							result.Add(pi.Name);
					}
				}
			}
			return result;
		}
		internal IDictionary<string,CodeTypeReference> GetTypeTargets(CodeDomResolverScope scope)
		{
			var result = new Dictionary<string, CodeTypeReference>();
			var t = scope.DeclaringType;
			if (null != t)
			{
				var binder = new CodeDomBinder(scope);
				var members = binder.GetMembers(t, MemberTypes.All, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static);
				foreach (var m in members)
				{
					var ctm = m as CodeTypeMember;
					if (null != ctm)
					{
						// is static
						// build the type reference to our class
						var cttr = new CodeTypeReference(GetBaseNameOfType(t, scope));
						foreach (CodeTypeParameter ctp in t.TypeParameters)
							cttr.TypeArguments.Add(new CodeTypeReference(ctp));
						// TODO: we need to change this entire thing in order to support method overloads
						if (!result.ContainsKey(ctm.Name))
							result.Add(ctm.Name, cttr);
						
					}
				}
			}
			return result;
		}
		internal HashSet<string> GetBaseTargets(CodeDomResolverScope scope)
		{
			// TODO: implement base references
			throw new NotImplementedException("Base references need to be implemented");
		}
		internal HashSet<string> GetThisTargets(CodeDomResolverScope scope)
		{
			var result = new HashSet<string>();
			var t = scope.DeclaringType;
			if (null != t)
			{
				var binder = new CodeDomBinder(scope);
				var members = binder.GetMembers(t, MemberTypes.All, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
				foreach (var m in members)
				{
					var ctm = m as CodeTypeMember;
					if (null != ctm)
					{
						
						
						// is instance
						result.Add(ctm.Name);
						
						
					}
				}
			}
			return result;
		}
		internal HashSet<string> GetMemberNames(CodeDomResolverScope scope)
		{
			var result = new HashSet<string>();
			var t = scope.DeclaringType;
			if (null != t)
			{
				var binder = new CodeDomBinder(scope);

				var members = binder.GetMembers(t,MemberTypes.All,BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
				foreach (var m in members)
				{
					var ctm = m as CodeTypeMember;
					if (null != ctm)
					{
						result.Add(ctm.Name);
					}
					else
					{
						var mi = m as MemberInfo;
						result.Add(mi.Name);
					}
				}
			}
			return result;
		}
		
		/// <summary>
		/// Gets the scope for the specified object
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeObject target)
		{
			var ccu = target as CodeCompileUnit;
			if (null != ccu)
				return GetScope(target);
			var ns = target as CodeNamespace;
			if (null != ns)
				return GetScope(ns);
			var td = target as CodeTypeDeclaration;
			if (null != td)
				return GetScope(td);
			var tm = target as CodeTypeMember;
			if (null != tm)
				return GetScope(tm);
			var st = target as CodeStatement;
			if (null != st)
				return GetScope(st);
			var ex = target as CodeExpression;
			if (null != ex)
				return GetScope(ex);
			var tr = target as CodeTypeReference;
			if (null != tr)
				return GetScope(tr);
			throw new ArgumentException("Cannot get the scope from this code object", nameof(target));
		}
		/// <summary>
		/// Gets the scope for the specified type reference
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeTypeReference target)
		{
			var result = new CodeDomResolverScope(this);
			result.TypeRef = target;
			return _FillScope(result);
		}
		/// <summary>
		/// Gets the scope for the specified expression
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeExpression target)
		{
			var result = new CodeDomResolverScope(this);
			result.Expression = target;
			return _FillScope(result);
		}
		/// <summary>
		/// Gets the scope for the specified statement
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeStatement target)
		{
			var result = new CodeDomResolverScope(this);
			result.Statement = target;
			return _FillScope(result);
		}
		/// <summary>
		/// Gets the scope for the specified member
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeTypeMember target)
		{
			var result = new CodeDomResolverScope(this);
			result.Member = target;
			return _FillScope(result);
		}
		/// <summary>
		/// Gets the scope for the specified type declaration
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeTypeDeclaration target)
		{
			var result = new CodeDomResolverScope(this);
			result.Types.Add(target);
			return _FillScope(result);
		}
		/// <summary>
		/// Gets the scope for the specified namespace
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeNamespace target)
		{
			var result = new CodeDomResolverScope(this);
			result.Namespace = target;
			return _FillScope(result);
		}
		/// <summary>
		/// Gets the scope for the specified compile unit
		/// </summary>
		/// <param name="target">The target</param>
		/// <returns>The scope</returns>
		public CodeDomResolverScope GetScope(CodeCompileUnit target)
		{
			var result = new CodeDomResolverScope(this);
			result.CompileUnit= target;
			return _FillScope(result);
		}
		/// <summary>
		/// Attempts to resolve a type at the optionally indicated scope
		/// </summary>
		/// <param name="type">The type to resolve</param>
		/// <param name="scope">The scope at which the resolution occurs</param>
		/// <returns>Either a runtime <see cref="Type"/> or a <see cref="CodeTypeDeclaration"/> representing the given type, or null if the type could not be resolved</returns>
		/// <remarks>This routine cannot instantiate reified generic types of declared types, nor will it resolve types with declared types as generic arguments</remarks>
		public object TryResolveType(CodeTypeReference type,CodeDomResolverScope scope=null)
			=> _ResolveType(type,scope);
		/// <summary>
		/// Attempts to retrieve the fully qualified type for a given type, at the given scope
		/// </summary>
		/// <param name="type">The type to evaluate</param>
		/// <param name="scope">The scope in which the evaluation occurs</param>
		/// <param name="result">A value to hold the fully qualified type reference</param>
		/// <returns>True if the operation was successful, otherwise false</returns>
		public bool TryGetQualifiedType(CodeTypeReference type,CodeDomResolverScope scope,out CodeTypeReference result)
		{
			if (0 < type.ArrayRank && null != type.ArrayElementType)
			{
				CodeTypeReference ctr;
				if(TryGetQualifiedType(type.ArrayElementType,scope,out ctr))
				{
					result = new CodeTypeReference();
					result.ArrayElementType = ctr;
					result.Options = type.Options;
					result.TypeArguments.AddRange(type.TypeArguments);
					return true;
				}
				result = type;
				return false;
			}
			var r = _ResolveType(type, scope);
			if (null != r)
			{
				var t = r as Type;
				if (null != t)
				{
					result = new CodeTypeReference(t);
					return true;
				}
				var td = r as CodeTypeDeclaration;
				if(null!=td)
				{
					
					result = new CodeTypeReference(_GetBaseNameOfType(td), type.Options);
					
					result.TypeArguments.AddRange(type.TypeArguments);
					return true;
				}
			}
			result = type;
			return false;
		}
		/// <summary>
		/// Gets the fully qualified type for the specified type, at the given scope
		/// </summary>
		/// <param name="type">The type to resolve</param>
		/// <param name="scope">The scope at which the resolution occurs</param>
		/// <returns></returns>
		public CodeTypeReference GetQualifiedType(CodeTypeReference type,CodeDomResolverScope scope)
		{
			CodeTypeReference result;
			if (!TryGetQualifiedType(type, scope, out result))
				throw new TypeLoadException("The type could not be resolved");
			return result;
		}
		static string _GetDecoratedTypeName(CodeTypeDeclaration decl)
		{
			if(0<decl.TypeParameters.Count)
				return string.Concat(decl.Name, "`", decl.TypeParameters.Count);
			return decl.Name;
		}
		/// <summary>
		/// Retrieves a <see cref="CodeTypeReference"/> that refers to the specified type declaration
		/// </summary>
		/// <param name="decl">The type to evaluate</param>
		/// <param name="scope">The scope at which the evaluation occurs or null to use the type's scope</param>
		/// <returns>A type reference that refers to the type declaraion</returns>

		public CodeTypeReference GetType(CodeTypeDeclaration decl,CodeDomResolverScope scope)
		{
			if (null == scope)
				scope = GetScope(decl);
			return new CodeTypeReference(GetBaseNameOfType(decl,scope));
		}
		internal static string GetBaseNameOfType(CodeTypeDeclaration decl,CodeDomResolverScope scope)
		{
			var result = scope.Namespace?.Name;
			if (string.IsNullOrEmpty(result)) result = "";
			else result = string.Concat(result, ".");
			var first = true;
			for (var i = scope.Types.Count - 1; 0 <= i; --i)
			{
				if (first)
				{
					first = false;
					result = string.Concat(result, _GetDecoratedTypeName(scope.Types[i]));
				}
				else
					result = string.Concat(result, "+", _GetDecoratedTypeName(scope.Types[i]));
			}
			return result;
		}
		string _GetBaseNameOfType(CodeTypeDeclaration decl,CodeDomResolverScope scope=null)
		{
			if(null==scope)
				scope = GetScope(decl);
			return GetBaseNameOfType(decl, scope);
		}
		/// <summary>
		/// Gets the declaring parent of the current code object
		/// </summary>
		/// <param name="target">The object to evaluate</param>
		/// <returns>The parent, or null if none could be found</returns>
		public static object GetParentOfCodeObject(object target)
		{
			var co = target as CodeObject;
			if (null == co) return null;
			return _GetRef(target, _parentKey);
		}
		/// <summary>
		/// Gets the root of the current code object - this is usually a <see cref="CodeCompileUnit"/>
		/// </summary>
		/// <param name="target">The object to evaluate</param>
		/// <returns>The root, or null if none could be found</returns>
		public static object GetRootOfCodeObject(object target)
		{
			var co = target as CodeObject;
			if (null == co) return null;
			return _GetRef(target, _rootKey);
		}
		// expects the type to be fully qualified
		static object _GetRef(object target,object key)
		{
			var co = target as CodeObject;
			if(null!=co)
			{
				var wr = co.UserData[key] as WeakReference<object>;
				if(null!=wr)
				{
					object result;
					if (wr.TryGetTarget(out result))
						return result;
				}
			}
			return null;
		}
		static string _GetGenericName(CodeTypeDeclaration td)
		{
			var result = td.Name;
			if (0 != td.TypeParameters.Count)
				return string.Concat(result, "`", td.TypeParameters.Count);
			return result;
		}
		static string _BuildTypePrefix(CodeDomResolverScope scope,int numTypes)
		{
			var result = "";
			if(null!=scope.Namespace && !string.IsNullOrEmpty(scope.Namespace.Name))
				result = scope.Namespace.Name;
			if(null!=scope.Types)
			{
				numTypes = Math.Min(numTypes, scope.Types.Count);
				var first = 0==result.Length;
				for (var i = scope.Types.Count-1;i>=Math.Max(0,(numTypes-1));--i)
				{
					if (!first)
					{
						if(result!=scope.Namespace.Name)
							result = string.Concat(result, "+", _GetDecoratedTypeName(scope.Types[i]));
						else
							result = string.Concat(result, ".",_GetDecoratedTypeName(scope.Types[i]));
					}
					else
					{
						first = false;
						result = string.Concat(result, _GetDecoratedTypeName(scope.Types[i]));
					}
				}
			}
			return result;
		}
		object _ResolveType(CodeTypeReference type,CodeDomResolverScope scope)
		{
			if (null == type)
				return null;
			if (null != type.ArrayElementType && 1 <= type.ArrayRank)
			{
				// we can't return anything better because the underlying type might not be "real"
				return typeof(Array);
			}
			var nss = new List<string>();
			if(null!=scope.DeclaringType)
			{
				nss.Add(_BuildTypePrefix(scope, scope.Types.Count));
				if(1<scope.Types.Count)
				{
					for(var i =0;i<scope.Types.Count-1;++i)
						nss.Add(_BuildTypePrefix(scope, i));
				}
			}
			if(null!=scope.CompileUnit)
			{
				foreach(CodeNamespace ns in scope.CompileUnit.Namespaces)
				{
					if(string.IsNullOrEmpty(ns.Name))
					{
						foreach (CodeNamespaceImport nsi in ns.Imports)
							nss.Add(string.Concat(nsi.Namespace));
					}
				}
			}
			if (null != scope.Namespace)
			{
				if (!string.IsNullOrEmpty(scope.Namespace.Name))
				{
					nss.Add(scope.Namespace.Name);
					foreach (CodeNamespaceImport nsi in scope.Namespace.Imports)
					{
						nss.Add(nsi.Namespace);
						nss.Add(string.Concat(scope.Namespace.Name, ".", nsi.Namespace));
					}
				}
			}
			
			nss.Add("");
			var ctrs = new List<CodeTypeReference>();
			foreach (var pfx in nss)
			{
				var s = pfx;
				if (0 < s.Length)
					s = string.Concat(pfx, ".", type.BaseType);
				else
					s = type.BaseType;
				var ctr = new CodeTypeReference();
				ctr.BaseType = s;
				ctr.TypeArguments.AddRange(type.TypeArguments);
				ctrs.Add(ctr);
			}
			var t = _DualResolve(ctrs);
			var rt = t as Type;
			if(null!=rt && 0<type.TypeArguments.Count)
			{
				var types = new Type[type.TypeArguments.Count];
				for(var i = 0;i<types.Length;i++)
				{
					types[i] = _ResolveType(type.TypeArguments[i], scope) as Type;
					if (null == types[i])
						return rt; // we just return the unreified generic since we can't do any better
				}
				return rt.MakeGenericType(types);
			}
			
			return t;
		}
		/// <summary>
		/// Indicates whether the type reference refers to a valid type in the given scope
		/// </summary>
		/// <param name="type">The type reference to evaluate</param>
		/// <param name="scope">The scope at which evaluation takes place, or null to use <paramref name="type"/>'s scope</param>
		/// <returns></returns>
		public bool IsValidType(CodeTypeReference type, CodeDomResolverScope scope=null)
		{
			if (null != type.ArrayElementType && 1 <= type.ArrayRank)
			{
				return IsValidType(type.ArrayElementType,scope);
			}
			if (scope == null)
				scope = GetScope(type);
			var nss = new List<string>();
			if (null != scope.DeclaringType)
			{
				nss.Add(_BuildTypePrefix(scope, scope.Types.Count));
				if (1 < scope.Types.Count)
				{
					for (var i = 0; i < scope.Types.Count - 1; ++i)
						nss.Add(_BuildTypePrefix(scope, i));
				}
			}
			if (null != scope.CompileUnit)
			{
				foreach (CodeNamespace ns in scope.CompileUnit.Namespaces)
				{
					if (string.IsNullOrEmpty(ns.Name))
					{
						foreach (CodeNamespaceImport nsi in ns.Imports)
							nss.Add(string.Concat(nsi.Namespace));
					}
				}
			}
			if (null != scope.Namespace)
			{
				if (!string.IsNullOrEmpty(scope.Namespace.Name))
				{
					nss.Add(scope.Namespace.Name);
					foreach (CodeNamespaceImport nsi in scope.Namespace.Imports)
					{
						nss.Add(nsi.Namespace);
						nss.Add(string.Concat(scope.Namespace.Name, ".", nsi.Namespace));
					}
				}
			}

			nss.Add("");
			var ctrs = new List<CodeTypeReference>();
			foreach (var pfx in nss)
			{
				var s = pfx;
				if (0 < s.Length)
					s = string.Concat(pfx, ".", type.BaseType);
				else
					s = type.BaseType;
				var ctr = new CodeTypeReference();
				ctr.BaseType = s;
				ctr.TypeArguments.AddRange(type.TypeArguments);
				ctrs.Add(ctr);
			}
			var t = _DualResolve(ctrs);
			if (null == t)
				return false;
			if (0 < type.TypeArguments.Count)
			{
				var types = new Type[type.TypeArguments.Count];
				for (var i = 0; i < types.Length; i++)
				{
					if (!IsValidType(type.TypeArguments[i], scope))
						return false;
				}
			}
			return true;
		}
		object _DualResolve(IList<CodeTypeReference> ctrs)
		{
			foreach (var ctr in ctrs)
			{
				var t = _ResolveTypeImpl(ctr, _ResolveCompileUnits);
				if (null != t)
					return t;
			}
			foreach (var ctr in ctrs)
			{
				var t = _ResolveTypeImpl(ctr, _ResolveAssemblies);
				if (null != t)
					return t;
			}
			return null;
		}
		object _ResolveTypeImpl(CodeTypeReference type,int resolutionType = _ResolveAssemblies | _ResolveCompileUnits)
		{
			if (type.BaseType == "Token")
				System.Diagnostics.Debugger.Break();
			object result = null;
			if(null!=type.ArrayElementType && 1<=type.ArrayRank)
			{
				// we can't return anything better because the underlying type might not be "real"
				return typeof(Array);
			}
			if (_ResolveCompileUnits == (resolutionType & _ResolveCompileUnits))
			{
				foreach (var ccu in CompileUnits)
				{
					CodeDomVisitor.Visit(ccu, (ctx) =>
					{
						var td = ctx.Target as CodeTypeDeclaration;
						if (null != td)
						{
							var name = _GetGenericName(td);
							CodeObject p = td;
							while ((p = _GetRef(p, _parentKey) as CodeObject) != null)
							{
								var ptd = p as CodeTypeDeclaration;
								if (null != ptd)
								{
									name = string.Concat(_GetGenericName(ptd), "+", name);
									td = ptd;
								}
								var ns = p as CodeNamespace;
								if (null != ns && !string.IsNullOrEmpty(ns.Name))
								{
									name = string.Concat(ns.Name, ".", name);
								}
							}
							if (name == type.BaseType)
							{
								td = ctx.Target as CodeTypeDeclaration;
								result = td;
								ctx.Cancel = true;
							}
						}
					}, CodeDomVisitTargets.Types | CodeDomVisitTargets.TypeRefs | CodeDomVisitTargets.Members);
					if (null != result)
						return result;
				}
			}
			if (_ResolveAssemblies == (resolutionType & _ResolveAssemblies))
			{
				Type t;
				if (_typeCache.TryGetValue(type, out t))
					return t;
				
				foreach (var ccu in CompileUnits) { 
					var corlib = typeof(string).Assembly;
					var rt = corlib.GetType(type.BaseType, false, false);
					result = rt;
					if (null != result)
					{
						_typeCache.Add(type, rt);
						return result;
					}
					foreach (var astr in ccu.ReferencedAssemblies)
					{
						var asm = _LoadAsm(astr);
						rt = asm.GetType(type.BaseType, false, false);
						result = rt;
						if (null != result)
						{
							_typeCache.Add(type, rt);
							return result;
						}
					}
				} 
				if(0==CompileUnits.Count)
				{
					var corlib = typeof(string).Assembly;
					var rt = corlib.GetType(type.BaseType, false, false);
					result = rt;
					if (null != result)
					{
						_typeCache.Add(type, rt);
						return result;
					}
				}
				_typeCache.Add(type, null);
			}
			return result;
		}
		Assembly _LoadAsm(string asm)
		{
			if (File.Exists(asm))
			{
				return Assembly.LoadFile(Path.GetFullPath(asm));
			} else if(asm.StartsWith(@"\\")) // UNC path
			{
				return Assembly.LoadFile(asm);
			} 
			AssemblyName an = null;
			try
			{
				an = new AssemblyName(asm);
			}
			catch { an = null; }
			if(null!=an)
			{
				return Assembly.Load(an);
			}
			return Assembly.Load(asm);
		}
		/// <summary>
		/// Clears the type cache
		/// </summary>
		public void ClearCache()
		{
			_typeCache.Clear();
		}
		/// <summary>
		/// Refreshes the code after the graphs have been changed, added to, or removed from.
		/// </summary>
		/// <param name="typesOnly">Only go as far as types and their members</param>
		public void Refresh(bool typesOnly = false)
		{
			// make sure each object is weak rooted to its parent and has a weak reference to the root
			for(int ic=CompileUnits.Count,i=0;i<ic;++i)
			{
				var ccu = CompileUnits[i];
				// just set some parents
				CodeDomVisitor.Visit(ccu, (ctx) =>
				{
					var co = ctx.Target as CodeObject;
					if (null != co)
					{
						if (null != ctx.Parent)
							co.UserData[_parentKey] = new WeakReference<object>(ctx.Parent);
						if (null != ctx.Root) // sanity check
							co.UserData[_rootKey] = new WeakReference<object>(ctx.Root);
					}
				}, typesOnly ? CodeDomVisitTargets.Types | CodeDomVisitTargets.Members : CodeDomVisitTargets.All);
			}
		}
	}
	/// <summary>
	/// Provides scope information from a particular point in the CodeDOM
	/// </summary>
	/// <remarks>The scope goes stale when its parent <see cref="CodeDomResolver"/> goes out of scope.</remarks>
#if GOKITLIB
	public 
#endif
	class CodeDomResolverScope
	{
		WeakReference<CodeDomResolver> _resolver;
		/// <summary>
		/// The resolver that spawned this scope.
		/// </summary>
		public CodeDomResolver Resolver {
			get {
				CodeDomResolver target;
				if (_resolver.TryGetTarget(out target))
					return target;
				throw new InvalidOperationException("The scope is stale");
			}
		}
		/// <summary>
		/// The compile unit of this scope
		/// </summary>
		public CodeCompileUnit CompileUnit { get; set; }
		/// <summary>
		/// The namespace of this scope
		/// </summary>
		public CodeNamespace Namespace { get; set; }
		/// <summary>
		/// The nested types of this scope, declaring first, followed by outer types of this nested type in reverse nest order
		/// </summary>
		public List<CodeTypeDeclaration> Types { get; } = new List<CodeTypeDeclaration>();
		/// <summary>
		/// The declaring type of this scope
		/// </summary>
		public CodeTypeDeclaration DeclaringType {
			get {
				if (null != Types && 0 < Types.Count)
					return Types[0];
				return null;
			}
		}
		/// <summary>
		/// The member associated with this scope
		/// </summary>
		public CodeTypeMember Member { get; set; }
		/// <summary>
		/// The statement associated with this scope
		/// </summary>
		public CodeStatement Statement { get; set; }
		/// <summary>
		/// The expression associated with this scope
		/// </summary>
		public CodeExpression Expression { get; set; }
		/// <summary>
		/// The type reference associated with this scope
		/// </summary>
		public CodeTypeReference TypeRef { get; set; }
		// can't use Lazy<T> lazy init here because we need to conditionally reinit
		IDictionary<string, CodeTypeReference> _variableTypes;
		/// <summary>
		/// Indicates all the variables in this scope and their types
		/// Not fully working yet
		/// </summary>
		public IDictionary<string, CodeTypeReference> VariableTypes {
			get {
				if (null == _variableTypes || 0 == _variableTypes.Count)
					_variableTypes = Resolver.GetVariableTypes(this);
				return _variableTypes;
			}
		}
		HashSet<string> _memberNames;
		/// <summary>
		/// Indicates all the members rooted at this scope
		/// </summary>
		public HashSet<string> MemberNames {
			get {
				if (null == _memberNames|| 0 == _memberNames.Count)
					_memberNames= Resolver.GetMemberNames(this);
				return _memberNames;
			}
		}
		IDictionary<string, CodeTypeReference> _argumentTypes;
		/// <summary>
		/// Indicates all the arguments at this scope and their types
		/// </summary>
		public IDictionary<string, CodeTypeReference> ArgumentTypes {
			get {
				if (null == _argumentTypes || 0 == _argumentTypes.Count)
					_argumentTypes = Resolver.GetArgumentTypes(this);
				return _argumentTypes;
			}
		}
		HashSet<string> _fieldNames;
		/// <summary>
		/// Indicates all the fields available at this scope
		/// </summary>
		public HashSet<string> FieldNames {
			get {
				if (null == _fieldNames || 0 == _fieldNames.Count)
					_fieldNames = Resolver.GetFieldNames(this);
				return _fieldNames;
			}
		}
		HashSet<string> _methodNames;
		/// <summary>
		/// Indicates all the method groups at this scope
		/// </summary>
		public HashSet<string> MethodNames {
			get {
				if (null == _methodNames || 0 == _methodNames.Count)
					_methodNames = Resolver.GetMethodNames(this);
				return _methodNames;
			}
		}
		HashSet<string> _propertyNames;
		/// <summary>
		/// Indicates all the property groups at this scope
		/// </summary>
		public HashSet<string> PropertyNames {
			get {
				if (null == _propertyNames || 0 == _propertyNames.Count)
					_propertyNames = Resolver.GetPropertyNames(this);
				return _propertyNames;
			}
		}
		HashSet<string> _eventNames;
		/// <summary>
		/// Indicates all the events at this scope
		/// </summary>
		public HashSet<string> EventNames {
			get {
				if (null == _eventNames || 0 == _eventNames.Count)
					_eventNames = Resolver.GetEventNames(this);
				return _eventNames;
			}
		}
		HashSet<string> _thisTargets;
		/// <summary>
		/// Indicates all the members that are part of this instance
		/// </summary>
		public HashSet<string> ThisTargets {
			get {
				if (null == _thisTargets|| 0 == _thisTargets.Count)
					_thisTargets= Resolver.GetThisTargets(this);
				return _thisTargets;
			}
		}
		HashSet<string> _baseTargets;
		/// <summary>
		/// Indicates all of the members that are part of the base instance
		/// </summary>
		public HashSet<string> BaseTargets {
			get {
				if (null == _baseTargets || 0 == _baseTargets.Count)
					_baseTargets= Resolver.GetBaseTargets(this);
				return _baseTargets;
			}
		}
		IDictionary<string, CodeTypeReference> _typeTargets;
		/// <summary>
		/// Indicates all the static members from the declaring type available at this scope
		/// </summary>
		public IDictionary<string, CodeTypeReference> TypeTargets {
			get {
				if (null == _typeTargets || 0 == _typeTargets.Count)
					_typeTargets= Resolver.GetTypeTargets(this);
				return _typeTargets;
			}
		}
		internal CodeDomResolverScope(CodeDomResolver resolver)
		{
			_resolver = new WeakReference<CodeDomResolver>(resolver);
		}
		/// <summary>
		/// Returns a string summarizing the scope
		/// </summary>
		/// <returns>A string printing a scope summary</returns>
		public override string ToString()
		{
			var sb = new StringBuilder();
			if(null!=CompileUnit)
			{
				sb.Append("Compile Unit: (");
				sb.Append(CompileUnit.Namespaces.Count);
				sb.AppendLine(" namespaces)");
			}
			if(null!=Namespace)
			{
				sb.Append("Namespace ");
				if(!string.IsNullOrEmpty(Namespace.Name))
				{
					sb.Append(Namespace.Name);
					sb.Append(" ");
				}
				sb.Append("(");
				sb.Append(Namespace.Types.Count);
				sb.AppendLine(" types)");
			}
			if(null!=DeclaringType)
			{
				sb.Append("Declaring Type: ");
				sb.AppendLine(CodeDomUtility.ToString(Resolver.GetType(DeclaringType, this)));
			}
			if(null!=Member)
			{
				sb.Append("Member: ");
				sb.Append(CodeDomUtility.ToString(CodeDomResolver.GetTypeForMember(Member)));
				sb.Append(" ");
				sb.AppendLine(Member.Name);
			}
			if(null!=Statement)
			{
				sb.Append("Statement: ");
				var s = CodeDomUtility.ToString(Statement).Trim();
				var i = s.IndexOfAny(new char[] { '\r', '\n' });
				if(-1<i)
				{
					s = s.Substring(0, i) + "...";
				}
				sb.AppendLine(s);
			}
			if (null != Expression)
			{
				sb.Append("Expression: ");
				sb.AppendLine(CodeDomUtility.ToString(Expression).Trim());
			}
			if(null!=TypeRef)
			{
				sb.Append("Type Ref: ");
				sb.AppendLine(CodeDomUtility.ToString(TypeRef).Trim());
			}
			return sb.ToString();
		}
	}
}
