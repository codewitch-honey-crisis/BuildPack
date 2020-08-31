using System;
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom;
namespace CD
{
	/// <summary>
	/// Provides reflection and signature matching over codedom objects
	/// </summary>
	partial class CodeDomBinder
	{
		/// <summary>
		/// Retrieves a list of members given the specified <see cref="MemberTypes"/> and <see cref="BindingFlags"/>
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="types">The member types to retrieve</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="MemberInfo"/> and <see cref="CodeTypeMember"/> objects representing the combined runtime and declared members</returns>
		public object[] GetMembers(object type, MemberTypes types, BindingFlags flags)
		{
			var rt = type as Type;
			if (null != rt)
				return GetMembers(rt, types, flags);
			var td = type as CodeTypeDeclaration;
			if (null != td)
				return GetMembers(td, types, flags);
			throw new ArgumentException("The type must be a runtime type or a declared type", nameof(type));
		}
		/// <summary>
		/// Retrieves a list of members given the specified <see cref="MemberTypes"/> and <see cref="BindingFlags"/>
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="types">The member types to retrieve</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="MemberInfo"/> and <see cref="CodeTypeMember"/> objects representing the combined runtime and declared members</returns>
		public object[] GetMembers(CodeTypeDeclaration type, MemberTypes types, BindingFlags flags)
		{
			var result = new List<object>();
			result.AddRange(_GetMembers(type, types, flags | BindingFlags.DeclaredOnly));
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we must return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var t = _resolver.TryResolveType(bt, _scope);
					var td = t as CodeTypeDeclaration;
					if (null != td)
					{
						result.AddRange(GetMembers(td, types, flags));
					}
					else
					{
						var tt = t as Type;
						if (null != tt)
						{
							result.AddRange(GetMembers(tt, types, flags));
						}
					}
				}
			}
			return result.ToArray();
		}
		/// <summary>
		/// Retrieves a list of members given the specified <see cref="MemberTypes"/> and <see cref="BindingFlags"/>
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="types">The member types to retrieve</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="MemberInfo"/> objects representing the runtime members</returns>
		public MemberInfo[] GetMembers(Type type, MemberTypes types, BindingFlags flags)
		{
			var ma = type.GetMembers(flags);
			if (types == MemberTypes.All)
				return ma;
			var result = new List<MemberInfo>(ma.Length);
			for (var i = 0; i < ma.Length; i++)
			{
				var m = ma[i];
				// TODO: can do this without a switch, but the bitwise math isn't coming to me just now
				switch (m.MemberType)
				{
					case MemberTypes.Constructor:
						if ((types & MemberTypes.Constructor) == m.MemberType)
							result.Add(m);
						break;
					case MemberTypes.Custom:
						if ((types & MemberTypes.Custom) == m.MemberType)
							result.Add(m);
						break;
					case MemberTypes.Event:
						if ((types & MemberTypes.Event) == m.MemberType)
							result.Add(m);
						break;
					case MemberTypes.Field:
						if ((types & MemberTypes.Field) == m.MemberType)
							result.Add(m);
						break;
					case MemberTypes.Method:
						if ((types & MemberTypes.Method) == m.MemberType)
							result.Add(m);
						break;
					case MemberTypes.NestedType:
						if ((types & MemberTypes.NestedType) == m.MemberType)
							result.Add(m);
						break;
					case MemberTypes.Property:
						if ((types & MemberTypes.Property) == m.MemberType)
							result.Add(m);
						break;
					case MemberTypes.TypeInfo:
						if ((types & MemberTypes.TypeInfo) == m.MemberType)
							result.Add(m);
						break;
				}
			}
			return result.ToArray();
		}
		CodeTypeMember[] _GetMembers(CodeTypeDeclaration type, MemberTypes types, BindingFlags flags)
		{
			var ic = type.Members.Count;
			var result = new List<CodeTypeMember>(ic);
			for (var i = 0; i < ic; ++i)
			{
				var mem = type.Members[i];
				var isPublic = MemberAttributes.Public == (mem.Attributes & MemberAttributes.AccessMask);
				var wantPublic = HasBindingFlag(flags, BindingFlags.Public);
				var isNonPublic = MemberAttributes.Public != (mem.Attributes & MemberAttributes.AccessMask);
				var wantNonPublic = HasBindingFlag(flags, BindingFlags.NonPublic);
				if ((isNonPublic && wantNonPublic) || (isPublic && wantPublic))
				{
					var isStatic = MemberAttributes.Static == (mem.Attributes & MemberAttributes.ScopeMask) || MemberAttributes.Const == (mem.Attributes & MemberAttributes.ScopeMask);
					var wantStatic = HasBindingFlag(flags, BindingFlags.Static);
					var isInst = MemberAttributes.Static != (mem.Attributes & MemberAttributes.ScopeMask) && MemberAttributes.Const != (mem.Attributes & MemberAttributes.ScopeMask);
					var wantInst = HasBindingFlag(flags, BindingFlags.Instance);
					if ((isStatic && wantStatic) || (isInst && wantInst))
					{
						if (HasMemberType(types, MemberTypes.Field))
						{
							var f = mem as CodeMemberField;
							if (null != f)
							{
								result.Add(f);
								continue;
							}
						}
						if (HasMemberType(types, MemberTypes.Property))
						{
							var p = mem as CodeMemberProperty;
							if (null != p)
							{
								result.Add(p);
								continue;
							}
						}
						if (HasMemberType(types, MemberTypes.Event))
						{
							var e = mem as CodeMemberEvent;
							if (null != e)
							{
								result.Add(e);
								continue;
							}
						}
						if (HasMemberType(types, MemberTypes.NestedType))
						{
							var t = mem as CodeTypeDeclaration;
							if (null != t)
							{
								result.Add(t);
								continue;
							}
						}
						if (HasMemberType(types, MemberTypes.Constructor))
						{
							var c = mem as CodeConstructor;
							if (null != c)
							{
								result.Add(c);
								continue;
							}
						}
						if (HasMemberType(types, MemberTypes.Method))
						{
							var c = mem as CodeConstructor;
							if (null == c)
							{
								var m = mem as CodeMemberMethod;
								if (null != m)
								{
									result.Add(m);
									continue;
								}
							}
						}
						if (HasMemberType(types, MemberTypes.Custom))
						{
							var s = mem as CodeSnippetTypeMember;
							if (null != s)
							{
								result.Add(s);
								continue;
							}
						}
					}
				}
			}
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				ic = type.BaseTypes.Count;
				for (var i = 0; i < ic; ++i)
				{
					// we don't return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var td = _resolver.TryResolveType(bt, _scope) as CodeTypeDeclaration;
					if (null != td)
					{
						var grp = _GetMembers(td, types, flags);
						if (HasBindingFlag(flags, BindingFlags.FlattenHierarchy))
						{
							for (var j = 0; j < grp.Length; j++)
							{
								var m = grp[j];
								if ((m.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Static)
									result.Add(m);
								else if ((m.Attributes & MemberAttributes.AccessMask) != MemberAttributes.Private)
									result.Add(m);
							}
						}
						else
						{
							for (var j = 0; j < grp.Length; j++)
							{
								var m = grp[i];
								if ((m.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Static)
									result.Add(m);
							}
						}
					}
				}
			}
			return result.ToArray();
		}
		CodeMemberProperty[] _GetPropertyGroup(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			var result = new List<CodeMemberProperty>();
			for (int ic = type.Members.Count, i = 0; i < ic; ++i)
			{
				var member = type.Members[i];

				var prop = member as CodeMemberProperty;
				if (null != prop)
				{
					if (string.IsNullOrEmpty(name) || 0 == string.Compare(prop.Name, name, StringComparison.InvariantCulture))
					{

						if (HasBindingFlag(flags, BindingFlags.NonPublic) && MemberAttributes.Public != (prop.Attributes & MemberAttributes.AccessMask) ||
							(HasBindingFlag(flags, BindingFlags.Public) && MemberAttributes.Public == (prop.Attributes & MemberAttributes.AccessMask)))
						{
							if (HasBindingFlag(flags, BindingFlags.Static) && MemberAttributes.Static == (prop.Attributes & MemberAttributes.ScopeMask) || MemberAttributes.Const == (prop.Attributes & MemberAttributes.ScopeMask) ||
							(HasBindingFlag(flags, BindingFlags.Instance) && MemberAttributes.Static != (prop.Attributes & MemberAttributes.ScopeMask) && MemberAttributes.Const != (prop.Attributes & MemberAttributes.ScopeMask)))
							{
								result.Add(prop);
							}
						}
					}
				}
			}
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we don't return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var td = _resolver.TryResolveType(bt, _scope) as CodeTypeDeclaration;
					if (null != td)
					{
						var grp = _GetPropertyGroup(td, name, flags);
						if (HasBindingFlag(flags, BindingFlags.FlattenHierarchy))
						{
							for (var j = 0; j < grp.Length; j++)
							{
								var m = grp[j];
								if ((m.Attributes & MemberAttributes.AccessMask) != MemberAttributes.Private)
									result.Add(m);
							}
						}
						else
						{
							for (var j = 0; j < grp.Length; j++)
							{
								var m = grp[i];
								if ((m.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Static)
									result.Add(m);
							}
						}
					}
				}
			}
			return result.ToArray();
		}
		CodeMemberMethod[] _GetMethodGroup(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			var result = new List<CodeMemberMethod>();
			for (int ic = type.Members.Count, i = 0; i < ic; ++i)
			{
				var member = type.Members[i];
				var ctor = member as CodeConstructor;
				if (null == ctor)
				{
					var meth = member as CodeMemberMethod;
					if (null != meth)
					{
						if (string.IsNullOrEmpty(name) || 0 == string.Compare(meth.Name, name, StringComparison.InvariantCulture))
						{

							if (HasBindingFlag(flags, BindingFlags.NonPublic) && MemberAttributes.Public != (meth.Attributes & MemberAttributes.AccessMask) ||
								(HasBindingFlag(flags, BindingFlags.Public) && MemberAttributes.Public == (meth.Attributes & MemberAttributes.AccessMask)))
							{
								if (HasBindingFlag(flags, BindingFlags.Static) && MemberAttributes.Static == (meth.Attributes & MemberAttributes.ScopeMask) || MemberAttributes.Const == (meth.Attributes & MemberAttributes.ScopeMask) ||
								(HasBindingFlag(flags, BindingFlags.Instance) && MemberAttributes.Static != (meth.Attributes & MemberAttributes.ScopeMask) && MemberAttributes.Const != (meth.Attributes & MemberAttributes.ScopeMask)))
								{
									result.Add(meth);
								}
							}
						}
					}
				}
			}
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we don't return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var td = _resolver.TryResolveType(bt, _scope) as CodeTypeDeclaration;
					if (null != td)
					{
						var grp = _GetMethodGroup(td, name, flags);
						if (HasBindingFlag(flags, BindingFlags.FlattenHierarchy))
						{
							for (var j = 0; j < grp.Length; j++)
							{
								var m = grp[j];
								if ((m.Attributes & MemberAttributes.AccessMask) != MemberAttributes.Private)
									result.Add(m);
							}
						}
						else
						{
							for (var j = 0; j < grp.Length; j++)
							{
								var m = grp[i];
								if ((m.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Static)
									result.Add(m);
							}
						}
					}
				}
			}
			return result.ToArray();
		}
		/// <summary>
		/// Gets a method group of the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the method group</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="MethodInfo"/> or <see cref="CodeMemberMethod"/> objects representing the methods in the method group</returns>
		public object[] GetMethodGroup(object type, string name, BindingFlags flags)
		{
			var rt = type as Type;
			if (null != rt)
				return GetMethodGroup(rt, name, flags);
			var td = type as CodeTypeDeclaration;
			if (null != td)
				return GetMethodGroup(td, name, flags);
			throw new ArgumentException("The type must be a runtime type or a declared type", nameof(type));
		}
		/// <summary>
		/// Gets a method group of the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the method group</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="MethodInfo"/> or <see cref="CodeMemberMethod"/> objects representing the methods in the method group</returns>
		public object[] GetMethodGroup(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			var result = new List<object>();
			result.AddRange(_GetMethodGroup(type, name, flags | BindingFlags.DeclaredOnly));
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we must return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var t = _resolver.TryResolveType(bt, _scope);
					var td = t as CodeTypeDeclaration;
					if (null != td)
					{
						result.AddRange(GetMethodGroup(td, name, flags));
					}
					else
					{
						var tt = t as Type;
						if (null != tt)
						{
							var ma = tt.GetMethods(flags);
							for (var j = 0; j < ma.Length; j++)
							{
								var m = ma[j];
								if (string.IsNullOrEmpty(name) || 0 == string.Compare(m.Name, name, StringComparison.InvariantCulture))
									if (!result.Contains(m))
										result.Add(m);
							}
						}
					}
				}
			}
			return result.ToArray();
		}
		/// <summary>
		/// Gets a method group of the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the method group</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="MethodInfo"/> objects representing the methods in the method group</returns>
		public MethodInfo[] GetMethodGroup(Type type, string name, BindingFlags flags)
		{
			var result = new List<MethodInfo>();
			var ma = type.GetMethods(flags);
			for (var i = 0; i < ma.Length; ++i)
			{
				var m = ma[i];
				if (0 == string.Compare(m.Name, name, StringComparison.InvariantCulture))
					result.Add(m);
			}
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				var ia = type.GetInterfaces();
				for (var i = 0; i < ia.Length; ++i)
				{
					result.AddRange(GetMethodGroup(ia[i], name, flags));
				}
			}
			return result.ToArray();
		}
		/// <summary>
		/// Gets a property group of the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the property group</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="PropertyInfo"/> or <see cref="CodeMemberProperty"/> objects representing the methods in the method group</returns>
		public object[] GetPropertyGroup(object type, string name, BindingFlags flags)
		{
			var rt = type as Type;
			if (null != rt)
				return GetPropertyGroup(rt, name, flags);
			var td = type as CodeTypeDeclaration;
			if (null != td)
				return GetPropertyGroup(td, name, flags);
			throw new ArgumentException("The type must be a runtime type or a declared type", nameof(type));
		}
		/// <summary>
		/// Gets a property group of the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the property group</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="PropertyInfo"/> or <see cref="CodeMemberProperty"/> objects representing the methods in the method group</returns>
		public object[] GetPropertyGroup(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			var result = new List<object>();
			result.AddRange(_GetPropertyGroup(type, name, flags | BindingFlags.DeclaredOnly));
			if (HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we must return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var t = _resolver.TryResolveType(bt, _scope);
					var td = t as CodeTypeDeclaration;
					if (null != td)
					{
						result.AddRange(GetPropertyGroup(td, name, flags));
					}
					else
					{
						var tt = t as Type;
						if (null != tt)
						{
							var pa = tt.GetProperties(flags);
							for (var j = 0; j < pa.Length; j++)
							{
								var p = pa[i];
								if (string.IsNullOrEmpty(name) || 0 == string.Compare(p.Name, name, StringComparison.InvariantCulture))
									if (!result.Contains(p))
										result.Add(p);
							}
						}
					}
				}
			}
			return result.ToArray();
		}
		/// <summary>
		/// Gets a property group of the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the property group</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>An array of <see cref="PropertyInfo"/> objects representing the methods in the method group</returns>
		public PropertyInfo[] GetPropertyGroup(Type type, string name, BindingFlags flags)
		{
			var result = new List<PropertyInfo>();
			var pa = type.GetProperties(flags);
			for (var i = 0; i < pa.Length; ++i)
			{
				var p = pa[i];
				if (0 == string.Compare(p.Name, name, StringComparison.InvariantCulture))
					result.Add(p);
			}
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				var ia = type.GetInterfaces();
				for (var i = 0; i < ia.Length; ++i)
				{
					result.AddRange(GetPropertyGroup(ia[i], name, flags));
				}
			}
			return result.ToArray();
		}
		/// <summary>
		/// Gets an event by the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the event</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>Either a <see cref="EventInfo"/> or a <see cref="CodeMemberEvent"/> representing the event</returns>
		public object GetEvent(object type, string name, BindingFlags flags)
		{
			var rt = type as Type;
			if (null != rt)
				return GetEvent(rt, name, flags);
			var td = type as CodeTypeDeclaration;
			if (null != td)
				return GetEvent(td, name, flags);
			throw new ArgumentException("The type must be a runtime type or a declared type", nameof(type));
		}
		/// <summary>
		/// Gets an event by the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the event</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>A <see cref="EventInfo"/> representing the event</returns>
		public EventInfo GetEvent(Type type, string name, BindingFlags flags)
		{
			return type.GetEvent(name, flags);
		}
		/// <summary>
		/// Gets an event by the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the event</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>Either a <see cref="EventInfo"/> or a <see cref="CodeMemberEvent"/> representing the event</returns>
		public object GetEvent(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			var r = _GetEvent(type, name, flags | BindingFlags.DeclaredOnly);
			if (null != r)
				return r;
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we must return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var t = _resolver.TryResolveType(bt, _scope);
					var td = t as CodeTypeDeclaration;
					if (null != td)
					{
						return GetEvent(td, name, flags);
					}
					else
					{
						var tt = t as Type;
						if (null != tt)
						{
							var fld = tt.GetEvent(name, flags);
							if (null != fld)
								return fld;
						}
					}
				}
			}
			return null;
		}
		/// <summary>
		/// Gets a field by the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the field</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>Either a <see cref="FieldInfo"/> or a <see cref="CodeMemberField"/> representing the field</returns>
		public object GetField(object type, string name, BindingFlags flags)
		{
			var rt = type as Type;
			if (null != rt)
				return GetField(rt, name, flags);
			var td = type as CodeTypeDeclaration;
			if (null != td)
				return GetField(td, name, flags);
			throw new ArgumentException("The type must be a runtime type or a declared type", nameof(type));
		}
		/// <summary>
		/// Gets a field by the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the field</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>A <see cref="FieldInfo"/> representing the field</returns>
		public FieldInfo GetField(Type type, string name, BindingFlags flags)
		{
			return type.GetField(name, flags);
		}
		/// <summary>
		/// Gets a field by the specified name
		/// </summary>
		/// <param name="type">The type to bind to</param>
		/// <param name="name">The name of the field</param>
		/// <param name="flags">The binding flags</param>
		/// <returns>Either a <see cref="FieldInfo"/> or a <see cref="CodeMemberField"/> representing the field</returns>
		public object GetField(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			var r = _GetField(type, name, flags | BindingFlags.DeclaredOnly);
			if (null != r)
				return r;
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we must return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var t = _resolver.TryResolveType(bt, _scope);
					var td = t as CodeTypeDeclaration;
					if (null != td)
					{
						return GetField(td, name, flags);
					}
					else
					{
						var tt = t as Type;
						if (null != tt)
						{
							var fld = tt.GetField(name, flags);
							if (null != fld)
								return fld;
						}
					}
				}
			}
			return null;
		}

		CodeMemberField _GetField(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			for (int ic = type.Members.Count, i = 0; i < ic; ++i)
			{
				var member = type.Members[i];

				var fld = member as CodeMemberField;
				if (null != fld)
				{
					if (0 == string.Compare(fld.Name, name, StringComparison.InvariantCulture))
					{

						if (HasBindingFlag(flags, BindingFlags.NonPublic) && MemberAttributes.Public != (fld.Attributes & MemberAttributes.AccessMask) ||
							(HasBindingFlag(flags, BindingFlags.Public) && MemberAttributes.Public == (fld.Attributes & MemberAttributes.AccessMask)))
						{
							if (type.IsEnum || (HasBindingFlag(flags, BindingFlags.Static) && MemberAttributes.Static == (fld.Attributes & MemberAttributes.ScopeMask) || MemberAttributes.Const == (fld.Attributes & MemberAttributes.ScopeMask)) ||
							(HasBindingFlag(flags, BindingFlags.Instance) && MemberAttributes.Static != (fld.Attributes & MemberAttributes.ScopeMask) && MemberAttributes.Const != (fld.Attributes & MemberAttributes.ScopeMask)))
							{
								return fld;
							}
						}
					}
				}
			}
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we don't return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var td = _resolver.TryResolveType(bt, _scope) as CodeTypeDeclaration;
					if (null != td)
					{
						var fld = _GetField(td, name, flags);
						if (HasBindingFlag(flags, BindingFlags.FlattenHierarchy))
						{
							if ((fld.Attributes & MemberAttributes.AccessMask) != MemberAttributes.Private)
								return fld;
						}
						else
						{
							if ((fld.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Static)
								return fld;

						}
					}
				}
			}
			return null;
		}
		CodeMemberEvent _GetEvent(CodeTypeDeclaration type, string name, BindingFlags flags)
		{
			for (int ic = type.Members.Count, i = 0; i < ic; ++i)
			{
				var member = type.Members[i];

				var fld = member as CodeMemberEvent;
				if (null != fld)
				{
					if (0 == string.Compare(fld.Name, name, StringComparison.InvariantCulture))
					{

						if (HasBindingFlag(flags, BindingFlags.NonPublic) && MemberAttributes.Public != (fld.Attributes & MemberAttributes.AccessMask) ||
							(HasBindingFlag(flags, BindingFlags.Public) && MemberAttributes.Public == (fld.Attributes & MemberAttributes.AccessMask)))
						{
							if (HasBindingFlag(flags, BindingFlags.Static) && MemberAttributes.Static == (fld.Attributes & MemberAttributes.ScopeMask) || MemberAttributes.Const == (fld.Attributes & MemberAttributes.ScopeMask) ||
							(HasBindingFlag(flags, BindingFlags.Instance) && MemberAttributes.Static != (fld.Attributes & MemberAttributes.ScopeMask) && MemberAttributes.Const != (fld.Attributes & MemberAttributes.ScopeMask)))
							{
								return fld;
							}
						}
					}
				}
			}
			if (!HasBindingFlag(flags, BindingFlags.DeclaredOnly))
			{
				for (int ic = type.BaseTypes.Count, i = 0; i < ic; ++i)
				{
					// we don't return reflected members from this.
					// you have to bind to reflected members seperately
					var bt = type.BaseTypes[i];
					var td = _resolver.TryResolveType(bt, _scope) as CodeTypeDeclaration;
					if (null != td)
					{
						var eve = _GetEvent(td, name, flags);
						if (HasBindingFlag(flags, BindingFlags.FlattenHierarchy))
						{
							if ((eve.Attributes & MemberAttributes.AccessMask) != MemberAttributes.Private)
								return eve;
						}
						else
						{
							if ((eve.Attributes & MemberAttributes.ScopeMask) != MemberAttributes.Static)
								return eve;

						}
					}
				}
			}
			return null;
		}
	}
}
