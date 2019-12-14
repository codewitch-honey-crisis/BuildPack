using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Reflection;
using System.IO;

namespace CD
{
	
	using ST = SlangTokenizer;
	partial class SlangParser
	{
		/// <summary>
		/// Reads a <see cref="CodeTypeDeclaration"/> from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
		public static CodeTypeDeclaration ReadTypeFrom(TextReader reader)
			=> ParseType(TextReaderEnumerable.FromReader(reader));
		/// <summary>
		/// Reads a <see cref="CodeTypeDeclaration"/> from the specified file
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
		public static CodeTypeDeclaration ReadTypeFrom(string filename)
			=> ParseType(new FileReaderEnumerable(filename));

		/// <summary>
		/// Reads a <see cref="CodeTypeDeclaration"/> from the specified URL
		/// </summary>
		/// <param name="url">The URL to read</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
		public static CodeTypeDeclaration ReadTypeFromUrl(string url)
			=> ParseType(new UrlReaderEnumerable(url));
		/// <summary>
		/// Parses a <see cref="CodeTypeDeclaration"/> from the specified input
		/// </summary>
		/// <param name="input">The input to parse</param>
		/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
		public static CodeTypeDeclaration ParseType(IEnumerable<char> input)
		{
			using (var e = new ST(input).GetEnumerator())
			{
				var pc = new _PC(e);
				pc.EnsureStarted();
				var result = _ParseType(pc);
				if (!pc.IsEnded)
					throw new ArgumentException("Unrecognized remainder in type declaration", "input");
				return result;
			}
		}

		static CodeTypeDeclaration _ParseType(_PC pc, bool isNested=false)
		{
			var result = new CodeTypeDeclaration();
			IList<KeyValuePair<string,CodeAttributeDeclaration>> custAttrs=null;
			HashSet<string> attrs = null;
			if (!isNested)
			{
				var comments = new CodeCommentStatementCollection();
				while (ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
					comments.Add(_ParseCommentStatement(pc));
				custAttrs = _ParseCustomAttributes(pc);
				attrs = _ParseTypeAttributes(pc);
				if (attrs.Contains("static"))
					throw new NotSupportedException("Explicitly static classes are not supported.");
				result.Attributes = _BuildMemberAttributes(attrs);
				result.TypeAttributes = (isNested)?_BuildNestedTypeAttributes(attrs): _BuildTopLevelTypeAttributes(attrs);
				result.Comments.AddRange(comments);
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated type declaration", "input");
				if (ST.keyword == pc.SymbolId && "partial" == pc.Value)
				{
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated type declaration", "input");
					result.IsPartial = true;
				}
				_AddCustomAttributes(custAttrs, null, result.CustomAttributes);
				if (null!=custAttrs && custAttrs.Count > result.CustomAttributes.Count)
					throw new ArgumentException("Invalid custom attribute targets", "input");
			}
			if (ST.keyword != pc.SymbolId)
				throw new ArgumentException("Expecting class, struct, enum, or interface","input");
			switch(pc.Value)
			{
				case "class":
					result.IsClass = true;
					break;
				case "struct":
					result.IsStruct = true;
					break;
				case "enum":
					if (result.IsPartial)
						throw new ArgumentException("Enums cannot be partial", "input");
					result.IsEnum = true;
					break;
				case "interface":
					result.IsInterface = true;
					break;
				default:
					throw new ArgumentException("Expecting class, struct, enum, or interface", "input");
			}
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated type declaration", "input");
			if (ST.identifier != pc.SymbolId)
				throw new ArgumentException("Expecting identifier in type declaration", "input");
			result.Name = pc.Value;
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated type declaration", "input");
			if(result.IsEnum)
				return _ParseEnum(pc, result);
			if (ST.lt==pc.SymbolId)
			{
				// parse the generic type arguments
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated generic type parameter specification", "input");
				if (ST.gt == pc.SymbolId)
					throw new ArgumentException("Generic type parameter specification cannot be empty", "input");
				while (!pc.IsEnded && ST.gt!=pc.SymbolId)
				{
					var custAttrs2 = _ParseCustomAttributes(pc);
					if (ST.identifier != pc.SymbolId)
						throw new ArgumentException("Expecting identifier in type parameter specification", "input");
					var tp = new CodeTypeParameter(pc.Value);
					_AddCustomAttributes(custAttrs2, null, tp.CustomAttributes);
					if (tp.CustomAttributes.Count < custAttrs2.Count)
						throw new ArgumentException("Invalid target in custom attribute declaration on generic type parameter", "input");
					result.TypeParameters.Add(tp);
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated generic type parameter specification", "input");
					if (ST.comma != pc.SymbolId)
						break;
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated generic type parameter specification", "input");
				}
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated generic type parameter specification", "input");
				if (ST.gt!=pc.SymbolId)
					throw new ArgumentException("Illegal generic type parameter specification", "input");
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated generic type parameter specification", "input");
			}
			if(ST.colon==pc.SymbolId) // parse base types 
			{
				pc.Advance();
				_SkipComments(pc);
				if(pc.IsEnded)
					throw new ArgumentException("Unterminated type declaration", "input");
				if (ST.lbrace == pc.SymbolId || (ST.identifier == pc.SymbolId && "where" == pc.Value))
					throw new ArgumentException("Empty base type specifiers", "input");
				while (!pc.IsEnded && !(ST.lbrace == pc.SymbolId || (ST.identifier == pc.SymbolId && "where" == pc.Value)))
				{
					result.BaseTypes.Add(_ParseTypeRef(pc));
					_SkipComments(pc);
				}
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated type declaration", "input");
			}
			if(ST.identifier==pc.SymbolId && "where" == pc.Value) // parse type constrants
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated type constraint", "input");
				var moved = false;
				while (!pc.IsEnded && ST.lbrace != pc.SymbolId)
				{
					moved = true;
					if (ST.identifier != pc.SymbolId)
						throw new ArgumentException("Expecting identifier in type constraint", "input");
					var cp = _LookupTypeParameter(result.TypeParameters, pc.Value);
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated type constraint", "input");
					if (ST.colon != pc.SymbolId)
						throw new ArgumentException("Expecting : in type constraint", "input");
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated type constraint", "input");
					cp.Constraints.Add(_ParseTypeRef(pc));
					_SkipComments(pc);
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated type declaration", "input");
					if(ST.comma==pc.SymbolId)
					{
						pc.Advance();
						_SkipComments(pc);
						if(ST.lbrace==pc.SymbolId)
							throw new ArgumentException("Unterminated type constraint", "input");
					}
				}
				if(!moved)
					throw new ArgumentException("Unterminated type constraint", "input");

			}
			if (ST.lbrace != pc.SymbolId)
				throw new ArgumentException("Expecting { in type definition","input");
			pc.Advance();
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated type declaration", "input");
			while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
			{ 
				result.Members.Add(_ParseMember(pc, result.Name));
			}
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated type declaration", "input");

			if (ST.rbrace != pc.SymbolId)
				throw new ArgumentException("Illegal member declaration in type", "input");
			pc.Advance();
			return result;
		}
		
		static CodeTypeParameter _LookupTypeParameter(CodeTypeParameterCollection parms,string name)
		{
			foreach (CodeTypeParameter tp in parms)
				if (tp.Name == name)
					return tp;
			throw new ArgumentException("Undeclared type parameter", "input");
		}
		static CodeTypeDeclaration _ParseEnum(_PC pc, CodeTypeDeclaration result)
		{
			var bt = new CodeTypeReference(typeof(int));
			if (ST.colon == pc.SymbolId)
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated enum declaration", "input");
				bt = _ParseTypeRef(pc);
				result.BaseTypes.Add(bt);
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated enum declaration", "input");
			}
			if (ST.lbrace != pc.SymbolId)
				throw new ArgumentException("Expecting enum body", "input");
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated enum declaration", "input");
			while (ST.rbrace != pc.SymbolId)
			{
				_SkipComments(pc);
				result.Members.Add(_ParseEnumField(pc, bt));
			}
			if (ST.rbrace != pc.SymbolId)
				throw new ArgumentException("Unterminated enum declaration", "input");
			pc.Advance();
			return result;
		}

		static CodeMemberField _ParseEnumField(_PC pc,CodeTypeReference enumType)
		{
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Expecting enum field declaration", "input");
			IList<KeyValuePair<string, CodeAttributeDeclaration>> custAttrs = null;
			if (ST.lbracket==pc.SymbolId)
				custAttrs = _ParseCustomAttributes(pc);
			_SkipComments(pc);
			if (pc.IsEnded || ST.identifier!=pc.SymbolId)
				throw new ArgumentException("Expecting enum field declaration", "input");
			var result = new CodeMemberField();
			result.Name = pc.Value;
			_AddCustomAttributes(custAttrs, null, result.CustomAttributes);
			_AddCustomAttributes(custAttrs, "field", result.CustomAttributes);
			if (null!=custAttrs && custAttrs.Count > result.CustomAttributes.Count)
				throw new ArgumentException("Invalid custom attribute targets", "input");
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded || (ST.eq!= pc.SymbolId &&ST.comma!=pc.SymbolId && ST.rbrace!=pc.SymbolId))
				throw new ArgumentException("Expecting enum field value, }, or ,", "input");
			if (ST.eq == pc.SymbolId) {
				pc.Advance();
				_SkipComments(pc);

				if (pc.IsEnded)
					throw new ArgumentException("Expecting enum field value", "input");
				result.InitExpression = _ParseExpression(pc);
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Expecting , or } in enum declaration", "input");
			}
			if (ST.comma==pc.SymbolId)
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Expecting enum field value", "input");
			}
			return result;
		}
		static TypeAttributes _BuildTopLevelTypeAttributes(ICollection<string> attrs)
		{
			var result = (TypeAttributes)0;
			foreach(var attr in attrs)
			{
				switch(attr)
				{
					case "public":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.Public;
						break;
					case "internal":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NotPublic;
						break;
					case "abstract":
						result |= TypeAttributes.Abstract;
						break;
					case "private":
						throw new ArgumentException("Top level types cannot be private", "input");
					case "protected":
						throw new ArgumentException("Top level types cannot be protected", "input");
					
				}
			}
			return result;
		}
		static TypeAttributes _BuildNestedTypeAttributes(ICollection<string> attrs)
		{
			// TODO: I've tried everything i can think of to get this to work and it just doesn't want to do it
			var result = TypeAttributes.NestedFamORAssem;
			foreach (var attr in attrs)
			{
				switch (attr)
				{
					case "protected":
						if (attrs.Contains("internal"))
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamORAssem | TypeAttributes.NotPublic;
						else
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamily | TypeAttributes.NotPublic;
						break;
					case "internal":
						if (attrs.Contains("protected"))
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamORAssem | TypeAttributes.NotPublic;
						else
							result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedFamANDAssem | TypeAttributes.NotPublic;
						break;
					case "public":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPublic;
						break;
					case "private":
						result = (result & ~TypeAttributes.VisibilityMask) | TypeAttributes.NestedPrivate | TypeAttributes.NotPublic;
						break;

				}
			}
			return result;
		}
		static HashSet<string> _ParseTypeAttributes(_PC pc)
		{
			var result = new HashSet<string>();
			_SkipComments(pc);
			var more = true;
			while(more && !pc.IsEnded && ST.keyword==pc.SymbolId)
			{
				switch(pc.Value)
				{
					case "static":
					case "abstract":
					case "protected":
					case "internal":
					case "public":
					case "private":
						result.Add(pc.Value);
						pc.Advance();
						_SkipComments(pc);
						break;
					default:
						more = false;
						break;
				}
			}
			return result;
		}
	}
}
