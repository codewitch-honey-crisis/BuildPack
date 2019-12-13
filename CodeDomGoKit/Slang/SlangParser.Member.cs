using System;
using System.Collections.Generic;
using System.CodeDom;
using System.IO;

namespace CD
{
	using ST = SlangTokenizer;
	partial class SlangParser
	{
		/// <summary>
		/// Reads a <see cref="CodeTypeMember"/> from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>
		public static CodeTypeMember ReadMemberFrom(TextReader reader)
			=> ParseMember(TextReaderEnumerable.FromReader(reader));
		/// <summary>
		/// Reads a <see cref="CodeTypeMember"/> from the specified file
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>
		public static CodeTypeMember ReadMemberFrom(string filename)
			=> ParseMember(new FileReaderEnumerable(filename));
		/// <summary>
		/// Reads a <see cref="CodeTypeMember"/> from the specified URL
		/// </summary>
		/// <param name="url">The URL to read</param>
		/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>

		public static CodeTypeMember ReadMemberFromUrl(string url)
			=> ParseMember(new UrlReaderEnumerable(url));
		/// <summary>
		/// Parses a <see cref="CodeTypeMember"/> from the specified input
		/// </summary>
		/// <param name="input">The input to parse</param>
		/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>
		public static CodeTypeMember ParseMember(IEnumerable<char> input)
		{
			using (var e = new ST(input).GetEnumerator())
			{
				var pc = new _PC(e);
				pc.EnsureStarted();
				var result = _ParseMember(pc);
				if (!pc.IsEnded)
					throw new ArgumentException("Unrecognized remainder in member declaration", "input");
				return result;
			}
		}
		static MemberAttributes _BuildMemberAttributes(ICollection<string> modifiers)
		{
			var result = (MemberAttributes)0;
			foreach(var kw in modifiers)
			{
				switch (kw)
				{
					case "protected":
						if (modifiers.Contains("internal"))
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyOrAssembly;
						else
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Family;
						break;
					case "internal":
						if (modifiers.Contains("protected"))
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyOrAssembly;
						else
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyAndAssembly;
						break;
					case "const":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Const;
						break;
					case "new":
						result = (result & ~MemberAttributes.VTableMask) | MemberAttributes.New;
						break;
					case "override":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Override;
						break;
					case "public":
						if (modifiers.Contains("virtual"))
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
						else
						{
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Public;
							result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Final;
						}
						break;
					case "private":
						result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Private;
						break;
					case "abstract":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Abstract;
						break;
					case "static":
						result = (result & ~MemberAttributes.ScopeMask) | MemberAttributes.Static;
						break;
				}
			}
			return result;
		}
		static ICollection<string> _ParseMemberAttributes(_PC pc)
		{
			var result = new HashSet<string>();
			_SkipComments(pc);
			while (ST.keyword==pc.SymbolId)
			{
				switch(pc.Value)
				{
					case "protected":
						if (result.Contains("public") || result.Contains("private"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "internal":
						if (result.Contains("public") || result.Contains("private"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "const":
						if(result.Contains("virtual") || result.Contains("override") || result.Contains("abstract"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "virtual":
						if (result.Contains("const"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "new":
						break;
					case "override":
						if (result.Contains("const"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "public":
						if (result.Contains("protected") || result.Contains("internal") || result.Contains("private"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "private":
						if (result.Contains("protected") || result.Contains("internal") || result.Contains("public"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "abstract":
						if (result.Contains("const") || result.Contains("static") || result.Contains("private"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					case "static":
						if(result.Contains("abstract"))
							throw new ArgumentException("Conflicting access modifiers on member", "input");
						break;
					default:
						return result;
				}
				if (!result.Add(pc.Value))
					throw new ArgumentException(string.Format("Duplicate member modifier {0} found", pc.Value), "input");
				pc.Advance();
				_SkipComments(pc);
			}
			_SkipComments(pc);
			return result;
		}
		static KeyValuePair<CodeTypeReference, string> _ParsePrivateImplementationType(_PC pc)
		{
			// i really hate this routine
			_SkipComments(pc);
			CodeTypeReference ptr = null;
			string name = null;
			_PC pc2 = pc.GetLookAhead();
			pc2.EnsureStarted();
			try
			{
				ptr=_ParseTypeRef(pc2, true);
			}
			catch { ptr = null; }
			if(ptr!=null)
			{
				if(ST.dot==pc2.SymbolId) // we didn't finish parsing the next field. This is probably what we wanted to begin with so yay
				{
					_ParseTypeRef(pc);
					pc.Advance();
					_SkipComments(pc);
					if(pc.IsEnded)
						throw new ArgumentException("Unterminated private member declaration");
					
					var s = pc.Value;
					pc.Advance();
					return new KeyValuePair<CodeTypeReference, string>(ptr, s);
				}
				// HACK: it will have parsed the name as part of the typeref, so we just trim it off.
				var idx = ptr.BaseType.LastIndexOfAny(new char[] { '.', '+' });
				if(0>idx) {
					pc.Advance();
					if (ST.dot != pc.SymbolId)
					{
						// the entire thing is the name. There is no private implementation type
						if (0 < ptr.TypeArguments.Count)
							throw new ArgumentException("Missing member name on private member declaration", "input");
						return new KeyValuePair<CodeTypeReference, string>(null, ptr.BaseType);
					}
					else // this is probably a "this"
					{
						pc.Advance();
						_SkipComments(pc);
						if (ST.keyword == pc.SymbolId && "this" == pc.Value)
						{
							pc.Advance();
							return new KeyValuePair<CodeTypeReference, string>(ptr, "this");
						}
						throw new ArgumentException("Illegal private member implementation type.", "input");
					}
				}
				name = ptr.BaseType.Substring(idx+1);
				ptr.BaseType = ptr.BaseType.Substring(0, idx);
				_ParseTypeRef(pc, false); // advance
				return new KeyValuePair<CodeTypeReference, string>(ptr, name);
			}
			var n = pc.Value;
			pc.Advance();
			return new KeyValuePair<CodeTypeReference, string>(null, n);
			//throw new ArgumentException("Expecting identifier on private member declaration", "input");

		}

		static CodeTypeMember _ParseMember(_PC pc,string typeName=null)
		{
			var comments = new CodeCommentStatementCollection();
			while(ST.lineComment==pc.SymbolId || ST.blockComment==pc.SymbolId)
			{
				comments.Add(_ParseCommentStatement(pc));
			}
			// TODO: First check for class/enum/struct and if we find that, forward the parse
			IList<KeyValuePair<string, CodeAttributeDeclaration>> customAttrs = null;
			if (ST.lbracket==pc.SymbolId)
				customAttrs = _ParseCustomAttributes(pc);
			var attrs = _ParseMemberAttributes(pc);
			var isEvent = false;
			if(ST.keyword==pc.SymbolId && ("partial"==pc.Value || "class"==pc.Value || "struct"==pc.Value || "enum"==pc.Value)) {
				var ctd = _ParseType(pc, true);
				for(var i = comments.Count-1;0<=i;--i)
					ctd.Comments.Insert(0, comments[i]);
				return ctd;
			}
			if(ST.keyword==pc.SymbolId && pc.Value=="event")
			{
				pc.Advance();
				_SkipComments(pc);
				isEvent = true;
			} else
			{
				// this is a constructor
				if(ST.identifier== pc.SymbolId && (string.IsNullOrEmpty(typeName)||typeName==pc.Value))
				{
					if (attrs.Contains("abstract"))
						throw new ArgumentException("Constructors cannot be abstract", "input");
					if (attrs.Contains("const"))
						throw new ArgumentException("Constructors cannot be const", "input");
					// store the name of the class in the constructor name just so we have it
					// we don't use it right now, but it keeps things more flexible just in case
					var ctorName = pc.Value;
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated constructor", "input");
					if (ST.lparen != pc.SymbolId)
						throw new ArgumentException("Expecting ( in constructor declaration", "input");
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated constructor", "input");
					var parms = _ParseParamDecls(pc, ST.rparen, false);
					CodeTypeMember mctor = null;
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated constructor", "input");
					if(!attrs.Contains("static"))
					{
						var ctor = new CodeConstructor();
						mctor = ctor;
						ctor.Name = ctorName;
						ctor.Attributes = _BuildMemberAttributes(attrs);
						_AddCustomAttributes(customAttrs, null, ctor.CustomAttributes);
						ctor.Parameters.AddRange(parms);
						if(ST.colon==pc.SymbolId)
						{
							pc.Advance();
							_SkipComments(pc);
							if (pc.IsEnded)
								throw new ArgumentException("Unterminated constructor - expecting chained or base constructor args", "input");
							if (ST.keyword == pc.SymbolId)
							{
								switch (pc.Value)
								{
									case "base":
										pc.Advance();
										_SkipComments(pc);
										if (pc.IsEnded)
											throw new ArgumentException("Unterminated constructor - expecting base constructor args", "input");
										if (ST.lparen != pc.SymbolId)
											throw new ArgumentException("Expecting ( in base constructor args", "input");
										//pc.Advance();
										//_SkipComments(pc);
										if (pc.IsEnded)
											throw new ArgumentException("Unterminated constructor - expecting base constructor args", "input");
										ctor.BaseConstructorArgs.AddRange(_ParseArguments(pc, ST.rparen, false));
										break;
									case "this":
										pc.Advance();
										_SkipComments(pc);
										if (pc.IsEnded)
											throw new ArgumentException("Unterminated constructor - expecting chained constructor args", "input");
										if (ST.lparen != pc.SymbolId)
											throw new ArgumentException("Expecting ( in chained constructor args", "input");
										//pc.Advance();
										//_SkipComments(pc);
										if (pc.IsEnded)
											throw new ArgumentException("Unterminated constructor - expecting chained constructor args", "input");
										ctor.ChainedConstructorArgs.AddRange(_ParseArguments(pc, ST.rparen, false));
										break;
									default:
										throw new ArgumentException("Expecting chained or base constructor call", "input");
								}
							}
							else
								throw new ArgumentException("Expecting chained or base constructor call", "input");
						}
						_SkipComments(pc);
						if (pc.IsEnded || ST.lbrace != pc.SymbolId)
							throw new ArgumentException("Expecting a constructor body", "input");
						pc.Advance();
						while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
							ctor.Statements.Add(_ParseStatement(pc,true));
						if (ST.rbrace != pc.SymbolId)
							throw new ArgumentException("Unterminated method body", "input");
						pc.Advance();
					} else
					{
						var ctor = new CodeTypeConstructor();
						mctor = ctor;
						ctor.Name = ctorName;
						ctor.Attributes = _BuildMemberAttributes(attrs);
						_AddCustomAttributes(customAttrs, null, ctor.CustomAttributes);
						if (0 < parms.Count)
							throw new ArgumentException("Type constructors cannot have parameters.", "input");
						_SkipComments(pc);
						if (pc.IsEnded || ST.lbrace != pc.SymbolId)
							throw new ArgumentException("Expecting a constructor body", "input");
						pc.Advance();
						while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
							ctor.Statements.Add(_ParseStatement(pc,true));
						if (ST.rbrace != pc.SymbolId)
							throw new ArgumentException("Unterminated method body", "input");
						pc.Advance();
					}
					mctor.Comments.AddRange(comments);
					return mctor;
				}
			}
			// expects to be on beginning of decl
			CodeTypeReference ctr=null;
			// special case for void type
			if (!(ST.keyword == pc.SymbolId && "void" == pc.Value))
			{
				ctr = _ParseTypeRef(pc);
			}
			else
				pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated member declaration", "input");
			if(ST.identifier!=pc.SymbolId && !(ST.keyword == pc.SymbolId && "this"== pc.Value))
				throw new ArgumentException("Expecting identifier in member declaration", "input");
			// this might be a private implementation type of the form 
			// IEnumerable ICollection.GetEnumerator() 
			var kvp = _ParsePrivateImplementationType(pc);
			var name = kvp.Value;
			var ptr = kvp.Key;
			var isPriv = !(attrs.Contains("public")||attrs.Contains("protected") || attrs.Contains("internal"));
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated member declaration", "input");
			if(isEvent) // events are basically parsed like fields with no initializers. The only difference here is the error messages we throw
			{
				if (ST.semi == pc.SymbolId) 
				{
					if (null == ctr)
						throw new ArgumentException("Events must not have a void type.", "input");
					var e = new CodeMemberEvent();
					e.Type = ctr;
					if (isPriv)
						e.PrivateImplementationType = ptr;
					e.Name = name;
					e.Attributes = _BuildMemberAttributes(attrs);
					_AddCustomAttributes(customAttrs, null, e.CustomAttributes);
					if (attrs.Contains("public"))
					{
						// this potentially implements one or more interfaces but we don't know what they are yet
						e.UserData.Add("slang:unresolved", true);
					}
					pc.Advance();
					e.Comments.AddRange(comments);
					return e;
				}
				throw new ArgumentException(string.Format("Unexpected token {0} found in event.",pc.Value),"input");
			}
			if (ST.semi==pc.SymbolId) // this is a field
			{
				if (attrs.Contains("abstract"))
					throw new ArgumentException("Fields cannot be abstract.", "input");
				if (null == ctr)
					throw new ArgumentException("Fields must not have a void type.", "input");
				var f = new CodeMemberField(ctr, name);
				f.Attributes = _BuildMemberAttributes(attrs);
				_AddCustomAttributes(customAttrs, null, f.CustomAttributes);
				if (null != ptr)
					throw new ArgumentException("Fields cannot have a private implementation type.","input");
				pc.Advance();
				f.Comments.AddRange(comments);
				return f;
			} else if(ST.eq==pc.SymbolId) // this is a field with a value
			{
				if (null == ctr)
					throw new ArgumentException("Fields must not have a void type.", "input");
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated field initializer", "input");
				var init = _ParseExpression(pc);
				if (ST.semi != pc.SymbolId)
					throw new ArgumentException("Invalid expression in field initializer", "input");
				pc.Advance();
				var f =new CodeMemberField(ctr, name);
				f.Attributes = _BuildMemberAttributes(attrs);
				_AddCustomAttributes(customAttrs, null,f.CustomAttributes);
				f.InitExpression = init;
				if (null != ptr)
					throw new ArgumentException("Fields cannot have a private implementation type.", "input");
				f.Comments.AddRange(comments);
				return f;

			} else if(ST.lparen==pc.SymbolId) // this is a method
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated method declaration", "input");
				var parms = _ParseParamDecls(pc);
				CodeMemberMethod m = new CodeMemberMethod();
				m.UserData.Add("slang:unresolved",true);
				m.ReturnType = ctr;
				m.Name = name;
				m.Attributes = _BuildMemberAttributes(attrs);
				_AddCustomAttributes(customAttrs, null, m.CustomAttributes);
				_AddCustomAttributes(customAttrs, "return", m.ReturnTypeCustomAttributes);
				m.Parameters.AddRange(parms);
				if (isPriv)
					m.PrivateImplementationType = ptr;
				_SkipComments(pc);
				if(attrs.Contains("public"))
				{
					// this potentially implements one or more interfaces but we don't know what they are yet
					//m.UserData.Add("slang:unresolved",true);
				}
				if (attrs.Contains("abstract"))
				{
					if (ST.semi != pc.SymbolId)
						throw new ArgumentException("Expecting ; to terminate abstract method definition", "input");
					pc.Advance();
					m.Comments.AddRange(comments);
					return m;
				}
				if (ST.lbrace != pc.SymbolId)
					throw new ArgumentException("Expecting method body for non abstract method", "input");
				pc.Advance();
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
				{
					m.Statements.Add(_ParseStatement(pc,true));
				}
				if (ST.rbrace != pc.SymbolId)
					throw new ArgumentException("Unterminated method body", "input");
				pc.Advance();
				m.Comments.AddRange(comments);
				return m;
			} else // must be a property
			{
				var p = new CodeMemberProperty();
				p.Type = ctr;
				p.Name = name;
				p.Attributes = _BuildMemberAttributes(attrs);
				_AddCustomAttributes(customAttrs, null, p.CustomAttributes);
				if (isPriv)
					p.PrivateImplementationType = ptr;
				else if (attrs.Contains("public"))
				{
					// this potentially implements one or more interfaces but we don't know what they are yet
					p.UserData.Add("slang:unresolved", true);
				}
				if (ST.lbracket==pc.SymbolId)
				{
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated indexer property declaration", "input");
					else if (0 != string.Compare(name, "this"))
						throw new ArgumentException("Only indexer properties can have arguments", "input");
					p.Parameters.AddRange(_ParseParamDecls(pc, ST.rbracket));
					p.Name = "Item";
				}
				if (ST.lbrace != pc.SymbolId)
					throw new ArgumentException("Expecting body for property", "input");
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated property body", "input");
				var sawGet = false;
				var sawSet = false;
				while (ST.rbrace != pc.SymbolId)
				{
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated property body", "input");
					if (ST.keyword != pc.SymbolId)
						throw new ArgumentException("Expecting get or set in property body.", "input");

					if ("get" == pc.Value)
					{
						if (sawGet)
							throw new ArgumentException("Multiple property.get definitions are not allowed.", "input");
						sawGet = true;
						pc.Advance();
						_SkipComments(pc);
						if (pc.IsEnded)
							throw new ArgumentException("Unterminated property.get", "input");
						if (ST.lbrace == pc.SymbolId)
						{
							if (attrs.Contains("abstract"))
								throw new ArgumentException("Abstract properties must not contain get bodies.", "input");
							pc.Advance();
							while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
								p.GetStatements.Add(_ParseStatement(pc,true));
							if (ST.rbrace != pc.SymbolId)
								throw new ArgumentException("Unterminated property.get body", "input");
							pc.Advance();
						}
						else if (ST.semi == pc.SymbolId)
						{
							if (!attrs.Contains("abstract"))
								throw new ArgumentException("Non abstract property.gets must have a body.", "input");
							pc.Advance();
						}
					}
					else if ("set" == pc.Value)
					{
						if (sawSet)
							throw new ArgumentException("Multiple property.set definitions are not allowed.", "input");
						sawSet = true;
						pc.Advance();
						_SkipComments(pc);
						if (pc.IsEnded)
							throw new ArgumentException("Unterminated property.set", "input");
						if (ST.lbrace == pc.SymbolId)
						{
							if (attrs.Contains("abstract"))
								throw new ArgumentException("Abstract properties must not contain set bodies.", "input");
							pc.Advance();
							while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
								p.SetStatements.Add(_ParseStatement(pc,true));
							if (ST.rbrace != pc.SymbolId)
								throw new ArgumentException("Unterminated property.set body", "input");
							pc.Advance();
						} else if(ST.semi==pc.SymbolId)
						{
							if (!attrs.Contains("abstract"))
								throw new ArgumentException("Non abstract property.sets must have a body.", "input");
							pc.Advance();
						}
					}
					else
						throw new ArgumentException(string.Format("Unrecognized keyword {0} in property body", pc.Value));
				}
				if (ST.rbrace != pc.SymbolId)
					throw new ArgumentException("Invalid property body", "input");
				pc.Advance();
				p.Comments.AddRange(comments);
				return p;
			}
			throw new ArgumentException("Illegal member declaration.", "input");
		}
		static void _AddCustomAttributes(IEnumerable<KeyValuePair<string, CodeAttributeDeclaration>> src,string target,CodeAttributeDeclarationCollection dst)
		{
			if(null!=src)
				foreach (var kvp in src)
					if (kvp.Key==target)
						dst.Add(kvp.Value);
		}

		static IList<KeyValuePair<string, CodeAttributeDeclaration>> _ParseCustomAttributes(_PC pc)
		{
			// expects to be on [
			var result = new List<KeyValuePair<string, CodeAttributeDeclaration>>();
			while (ST.lbracket == pc.SymbolId)
			{
				foreach (var kvp in _ParseCustomAttributeGroup(pc))
					result.Add(kvp);
				_SkipComments(pc);
			}
			return result;
		}

		static IList<KeyValuePair<string, CodeAttributeDeclaration>> _ParseCustomAttributeGroup(_PC pc)
		{
			// expects to be on [
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated custom attribute declaration group");
			var result = new List<KeyValuePair<string, CodeAttributeDeclaration>>();
			var target = pc.Value;
			var hasTarget = false;
			var pc2 = pc.GetLookAhead();
			pc2.EnsureStarted();
			pc2.Advance();
			_SkipComments(pc2);
			if (ST.colon == pc2.SymbolId)
			{
				hasTarget = true;
				pc.Advance();
				_SkipComments(pc);
				pc.Advance();
				_SkipComments(pc);
				if(pc.IsEnded)
					throw new ArgumentException("Unterminated custom attribute declaration group","input");
			}
			while(ST.rbracket!=pc.SymbolId)
			{
				var attr = _ParseCustomAttribute(pc);
				_SkipComments(pc);
				if (!hasTarget)
					result.Add(new KeyValuePair<string, CodeAttributeDeclaration>(null, attr));
				else
					result.Add(new KeyValuePair<string, CodeAttributeDeclaration>(target, attr));
				if(pc.IsEnded)
					throw new ArgumentException("Unterminated custom attribute declaration group","input");
				if(ST.comma==pc.SymbolId)
				{
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated custom attribute declaration group", "input");
					if (ST.rbracket == pc.SymbolId)
						throw new ArgumentException("Unexpected comma found in attribute declaration group", "input");
				}
			}
			if(ST.rbracket!=pc.SymbolId)
				throw new ArgumentException("Invalid custom attribute declaration", "input");
			pc.Advance();
			_SkipComments(pc);
			if (0 == result.Count)
				throw new ArgumentException("Attribute groups must not be empty.", "input");
			return result;
		}
		static CodeAttributeArgumentCollection _ParseCustomAttributeArguments(_PC pc)
		{		
			var result = new CodeAttributeArgumentCollection();
			if (ST.lparen != pc.SymbolId)
				return result;
			if (!pc.Advance())
				throw new ArgumentException("Unterminated argument list", "input");
			var named = false;
			while (ST.rparen != pc.SymbolId)
			{
				var arg = new CodeAttributeArgument();
				if (ST.identifier == pc.SymbolId)
				{
					var s = pc.Value;
					var pc2 = pc.GetLookAhead();
					pc2.EnsureStarted();
					pc2.Advance();
					_SkipComments(pc2);
					if(ST.eq==pc2.SymbolId)
					{
						pc.Advance();
						_SkipComments(pc);
						pc.Advance();
						arg.Name = s;
						arg.Value=_ParseExpression(pc);
						result.Add(arg);
						named = true;
						continue;
					}
				}
				if (named)
					throw new ArgumentException("Named custom attribute arguments must follow the unnamed arguments.", "input");
				var exp = _ParseExpression(pc);
				_SkipComments(pc);
				arg.Value = exp;
				result.Add(arg);
				if (ST.comma == pc.SymbolId)
				{
					if (!pc.Advance())
						throw new ArgumentException("Unterminated argument list.", "input");
				}
			}
			if (ST.rparen!= pc.SymbolId)
			{
				throw new ArgumentException("Unterminated argument list.", "input");
			}
			pc.Advance();
			return result;
		}
		static CodeAttributeDeclaration _ParseCustomAttribute(_PC pc)
		{
			var ctr = _ParseTypeRef(pc);
			ctr.UserData.Add("slang:attribute", true);
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated custom attribute declaration","input");
			var exprs=_ParseCustomAttributeArguments(pc);
			var result = new CodeAttributeDeclaration( ctr );
			result.Arguments.AddRange(exprs);
			return result;
		}
		static CodeParameterDeclarationExpressionCollection _ParseParamDecls(_PC pc,int endSym = ST.rparen, bool allowDirection=true)
		{
			var result = new CodeParameterDeclarationExpressionCollection();
			while(!pc.IsEnded&&endSym!=pc.SymbolId)
			{
				var p = _ParseParamDecl(pc, allowDirection);
				result.Add(p);
				if (ST.comma != pc.SymbolId)
					break;
				pc.Advance();
				_SkipComments(pc);
			}
			if (endSym != pc.SymbolId)
				throw new ArgumentException("Unterminated parameter declarations", "input");
			pc.Advance();
			return result;
		}
		// these aren't true expressions, they're just part of method and indexer parameter declarations
		// that's why we parse them here
		static CodeParameterDeclarationExpression _ParseParamDecl(_PC pc, bool allowDirection=true) 
		{
			var attrs = new CodeAttributeDeclarationCollection();
			if (ST.lbracket == pc.SymbolId)
				_AddCustomAttributes(_ParseCustomAttributes(pc), null, attrs);
			FieldDirection d = FieldDirection.In;
			_SkipComments(pc);
			if (allowDirection)
			{
				if (ST.keyword==pc.SymbolId)
				{
					switch(pc.Value)
					{
						case "out":
							d = FieldDirection.Out;
							pc.Advance();
							_SkipComments(pc);
							break;
						case "ref":
							d = FieldDirection.Ref;
							pc.Advance();
							_SkipComments(pc);
							break;
						default:
							break;
					}
				}
			}
			var ctr = _ParseTypeRef(pc);
			_SkipComments(pc);
			if (ST.identifier != pc.SymbolId)
				throw new ArgumentException("Expecting identifier in parameter declaration", "input");
			var result = new CodeParameterDeclarationExpression(ctr, pc.Value);
			result.Direction = d;
			if (null != attrs)
				result.CustomAttributes.AddRange(attrs);
			pc.Advance();
			return result;
		}
	}
}
