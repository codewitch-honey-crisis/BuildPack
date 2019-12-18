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
					throw new SlangSyntaxException("Unrecognized remainder in type", pc.Current.Line, pc.Current.Column, pc.Current.Position);
				return result;
			}
		}

		static CodeTypeDeclaration _ParseType(_PC pc, bool isNested=false)
		{
			var dirs = _ParseDirectives(pc);

			var result = new CodeTypeDeclaration();
			IList<KeyValuePair<string,CodeAttributeDeclaration>> custAttrs=null;
			HashSet<string> attrs = null;
			if (!isNested)
			{
				var comments = new CodeCommentStatementCollection();
				while (ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
					comments.Add(_ParseCommentStatement(pc));
				dirs.AddRange(_ParseDirectives(pc));
				custAttrs = _ParseCustomAttributes(pc);
				attrs = _ParseTypeAttributes(pc);
				if (attrs.Contains("static"))
					throw new NotSupportedException("Explicitly static classes are not supported.");
				result.Attributes = _BuildMemberAttributes(attrs);
				result.TypeAttributes = (isNested)?_BuildNestedTypeAttributes(attrs): _BuildTopLevelTypeAttributes(attrs,pc);
				result.Comments.AddRange(comments);
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated type declaration", pc.Current);
				if (ST.keyword == pc.SymbolId && "partial" == pc.Value)
				{
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						_Error("Unterminated type declaration", pc.Current);
					result.IsPartial = true;
				}
				_AddCustomAttributes(custAttrs, null, result.CustomAttributes);
				if (null!=custAttrs && custAttrs.Count > result.CustomAttributes.Count)
					_Error("Invalid custom attribute targets", pc.Current);
				_AddStartDirs(result, dirs);
			}
			if (ST.keyword != pc.SymbolId)
				_Error("Expecting class, struct, enum, or interface",pc.Current);
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
						_Error("Enums cannot be partial", pc.Current);
					result.IsEnum = true;
					break;
				case "interface":
					result.IsInterface = true;
					break;
				default:
					_Error("Expecting class, struct, enum, or interface", pc.Current);
					break;
			}
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated type declaration", pc.Current);
			if (ST.identifier != pc.SymbolId)
				_Error("Expecting identifier in type declaration", pc.Current);
			result.Name = pc.Value;
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated type declaration", pc.Current);
			if (result.IsEnum)
			{
				var e = _ParseEnum(pc, result);
				dirs.AddRange(_ParseDirectives(pc, true));
				_AddEndDirs(e, dirs);
			}
			if (ST.lt==pc.SymbolId)
			{
				// parse the generic type arguments
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated generic type parameter specification", pc.Current);
				if (ST.gt == pc.SymbolId)
					_Error("Generic type parameter specification cannot be empty", pc.Current);
				while (!pc.IsEnded && ST.gt!=pc.SymbolId)
				{
					var custAttrs2 = _ParseCustomAttributes(pc);
					if (ST.identifier != pc.SymbolId)
						_Error("Expecting identifier in type parameter specification", pc.Current);
					var tp = new CodeTypeParameter(pc.Value);
					_AddCustomAttributes(custAttrs2, null, tp.CustomAttributes);
					if (tp.CustomAttributes.Count < custAttrs2.Count)
						_Error("Invalid target in custom attribute declaration on generic type parameter", pc.Current);
					result.TypeParameters.Add(tp);
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						_Error("Unterminated generic type parameter specification", pc.Current);
					if (ST.comma != pc.SymbolId)
						break;
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						_Error("Unterminated generic type parameter specification", pc.Current);
				}
				if (pc.IsEnded)
					_Error("Unterminated generic type parameter specification", pc.Current);
				if (ST.gt!=pc.SymbolId)
					_Error("Illegal generic type parameter specification", pc.Current);
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated generic type parameter specification", pc.Current);
			}
			if(ST.colon==pc.SymbolId) // parse base types 
			{
				pc.Advance();
				_SkipComments(pc);
				if(pc.IsEnded)
					_Error("Unterminated type declaration", pc.Current);
				if (ST.lbrace == pc.SymbolId || (ST.identifier == pc.SymbolId && "where" == pc.Value))
					_Error("Empty base type specifiers", pc.Current);
				while (!pc.IsEnded && !(ST.lbrace == pc.SymbolId || (ST.identifier == pc.SymbolId && "where" == pc.Value)))
				{
					result.BaseTypes.Add(_ParseTypeRef(pc));
					_SkipComments(pc);
				}
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated type declaration", pc.Current);
			}
			if(ST.identifier==pc.SymbolId && "where" == pc.Value) // parse type constrants
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated type constraint", pc.Current);
				var moved = false;
				while (!pc.IsEnded && ST.lbrace != pc.SymbolId)
				{
					moved = true;
					if (ST.identifier != pc.SymbolId)
						_Error("Expecting identifier in type constraint", pc.Current);
					var cp = _LookupTypeParameter(result.TypeParameters, pc.Value,pc);
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						_Error("Unterminated type constraint", pc.Current);
					if (ST.colon != pc.SymbolId)
						_Error("Expecting : in type constraint", pc.Current);
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						_Error("Unterminated type constraint", pc.Current);
					cp.Constraints.Add(_ParseTypeRef(pc));
					_SkipComments(pc);
					if (pc.IsEnded)
						_Error("Unterminated type declaration", pc.Current);
					if(ST.comma==pc.SymbolId)
					{
						pc.Advance();
						_SkipComments(pc);
						if(ST.lbrace==pc.SymbolId)
							_Error("Unterminated type constraint", pc.Current);
					}
				}
				if(!moved)
					_Error("Unterminated type constraint", pc.Current);

			}
			if (ST.lbrace != pc.SymbolId)
				_Error("Expecting { in type definition",pc.Current);
			pc.Advance();
			if (pc.IsEnded)
				_Error("Unterminated type declaration", pc.Current);
			while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
			{ 
				result.Members.Add(_ParseMember(pc, result.Name));
			}
			if (pc.IsEnded)
				_Error("Unterminated type declaration", pc.Current);

			if (ST.rbrace != pc.SymbolId)
				_Error("Illegal member declaration in type", pc.Current);
			pc.Advance();
			_SkipComments(pc);
			dirs.AddRange(_ParseDirectives(pc, true));
			_AddEndDirs(result,dirs);
			return result;
		}
		
		static CodeTypeParameter _LookupTypeParameter(CodeTypeParameterCollection parms,string name,_PC pc)
		{
			foreach (CodeTypeParameter tp in parms)
				if (tp.Name == name)
					return tp;
			_Error("Undeclared type parameter", pc.Current);
			return null;
		}
		static CodeTypeDeclaration _ParseEnum(_PC pc, CodeTypeDeclaration result)
		{
			var bt = new CodeTypeReference(typeof(int));
			if (ST.colon == pc.SymbolId)
			{
				
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated enum declaration", pc.Current);
				bt = _ParseTypeRef(pc);
				result.BaseTypes.Add(bt);
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated enum declaration", pc.Current);
			}
			if (ST.lbrace != pc.SymbolId)
				_Error("Expecting enum body", pc.Current);
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated enum declaration", pc.Current);
			while (ST.rbrace != pc.SymbolId)
			{
				_SkipComments(pc);
				result.Members.Add(_ParseEnumField(pc, bt));
			}
			if (ST.rbrace != pc.SymbolId)
				_Error("Unterminated enum declaration", pc.Current);
			pc.Advance();
			return result;
		}

		static CodeMemberField _ParseEnumField(_PC pc,CodeTypeReference enumType)
		{
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Expecting enum field declaration", pc.Current);
			IList<KeyValuePair<string, CodeAttributeDeclaration>> custAttrs = null;
			if (ST.lbracket==pc.SymbolId)
				custAttrs = _ParseCustomAttributes(pc);
			_SkipComments(pc);
			if (pc.IsEnded || ST.identifier!=pc.SymbolId)
				_Error("Expecting enum field declaration", pc.Current);
			var result = new CodeMemberField();
			result.Name = pc.Value;
			_AddCustomAttributes(custAttrs, null, result.CustomAttributes);
			_AddCustomAttributes(custAttrs, "field", result.CustomAttributes);
			if (null!=custAttrs && custAttrs.Count > result.CustomAttributes.Count)
				_Error("Invalid custom attribute targets", pc.Current);
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded || (ST.eq!= pc.SymbolId &&ST.comma!=pc.SymbolId && ST.rbrace!=pc.SymbolId))
				_Error("Expecting enum field value, }, or ,", pc.Current);
			if (ST.eq == pc.SymbolId) {
				pc.Advance();
				_SkipComments(pc);

				if (pc.IsEnded)
					_Error("Expecting enum field value", pc.Current);
				result.InitExpression = _ParseExpression(pc);
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Expecting , or } in enum declaration", pc.Current);
			}
			if (ST.comma==pc.SymbolId)
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Expecting enum field value", pc.Current);
			}
			return result;
		}
		static TypeAttributes _BuildTopLevelTypeAttributes(ICollection<string> attrs,_PC pc)
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
						_Error("Top level types cannot be private", pc.Current);
						break;
					case "protected":
						_Error("Top level types cannot be protected", pc.Current);
						break;
					
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
