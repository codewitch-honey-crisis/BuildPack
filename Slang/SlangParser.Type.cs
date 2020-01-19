using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Reflection;
using System.IO;

namespace Slang
{
	using ST = SlangTokenizer;
	
	partial class SlangParser
	{
		public static CodeTypeDeclaration ParseTypeDecl(string text)
		{
			var tokenizer = new SlangTokenizer(text);
			return ParseTypeDecl(tokenizer);
		}
		public static CodeTypeDeclaration ReadTypeDeclFrom(Stream stream)
		{
			var tokenizer = new SlangTokenizer(stream);
			return ParseTypeDecl(tokenizer);
		}
		public static CodeTypeDeclaration ParseTypeDecl(string text, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(text);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseTypeDecl(pc,false,line,column,position,null);
		}
		public static CodeTypeDeclaration ReadTypeDeclFrom(Stream stream, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(stream);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseTypeDecl(pc, false,line,column,position,null) ;
		}

		public static CodeTypeMember ParseMember(string text)
		{
			var tokenizer = new SlangTokenizer(text);
			return ParseMember(tokenizer);
		}
		public static CodeTypeMember ReadMemberFrom(Stream stream)
		{
			var tokenizer = new SlangTokenizer(stream);
			return ParseMember(tokenizer);
		}
		public static CodeTypeMember ParseMember(string text, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(text);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseMember(pc);
		}
		public static CodeTypeMember ReadMemberFrom(Stream stream, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(stream);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseMember(pc);
		}

		public static CodeTypeMemberCollection ParseMembers(string text)
		{
			var tokenizer = new SlangTokenizer(text);
			return ParseMembers(tokenizer);
		}
		public static CodeTypeMemberCollection ReadMembersFrom(Stream stream)
		{
			var tokenizer = new SlangTokenizer(stream);
			return ParseMembers(tokenizer);
		}
		public static CodeTypeMemberCollection ParseMembers(string text, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(text);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseMembers(pc);
		}
		public static CodeTypeMemberCollection ReadMembersFrom(Stream stream, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(stream);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseMembers(pc);
		}

		internal static CodeTypeDeclaration ParseTypeDecl(IEnumerable<Token> tokenizer)
		{
			var pc = new _PC(tokenizer);
			pc.Advance(false);
			return _ParseTypeDecl(pc,false,1,1,0,null);
		}
		static CodeTypeDeclaration _ParseTypeDecl(_PC pc, bool isNested, int line, int column, long position, HashSet<string> seen)
		{
			var result = new CodeTypeDeclaration().Mark(line, column, position);
			if (!isNested)
			{
				var cc = new CodeCommentStatementCollection();
				var l = line;
				var c = column;
				var p = position;

				CodeLinePragma lp;
				var startDirs = new CodeDirectiveCollection();
				var comments = new CodeCommentStatementCollection();
				while (ST.directive == pc.SymbolId || ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
				{
					switch (pc.SymbolId)
					{
						case ST.directive:
							var d = _ParseDirective(pc);
							var llp = d as CodeLinePragma;
							if (null != llp)
								lp = llp;
							else if (null != d)
								startDirs.Add(d as CodeDirective);
							break;
						case ST.blockComment:
							comments.Add(_ParseCommentStatement(pc));
							break;
						case ST.lineComment:
							comments.Add(_ParseCommentStatement(pc, true));
							break;
					}
				}
				IDictionary<string, CodeAttributeDeclarationCollection> customAttributes = null;

				if (ST.lbracket == pc.SymbolId)
					customAttributes = _ParseAttributeGroups(pc);

				seen = new HashSet<string>(StringComparer.InvariantCulture);
				line = pc.Line;
				column = pc.Column;
				position = pc.Position;
				while (ST.publicKeyword == pc.SymbolId ||
					ST.internalKeyword == pc.SymbolId ||
					ST.abstractKeyword == pc.SymbolId)
				{
					if (!seen.Add(pc.Value))
						pc.Error(string.Format("Duplicate attribute {0} specified in member", pc.Value));
					pc.Advance();
				}

				result.Comments.AddRange(comments);
				_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
				_CheckCustomAttributes(customAttributes, pc);
				result.TypeAttributes = _BuildTypeAttributes(seen, line, column, position);

				line = pc.Line;
				column = pc.Column;
				position = pc.Position;
				
			}
			else
			{
				result.TypeAttributes = _BuildMemberTypeAttributes(seen);
				// TODO: see about below
				//result.Attributes = _BuildMemberAttributes(seen);
				
			}

			if(ST.partialKeyword==pc.SymbolId)
			{
				result.IsPartial = true;
				pc.Advance();
			}

			switch(pc.SymbolId)
			{
				case ST.classKeyword:
					result.IsClass = true;
					break;
				case ST.structKeyword:
					result.IsStruct = true;
					break;
				case ST.interfaceKeyword:
					result.IsInterface = true;
					break;
				case ST.enumKeyword:
					result.IsEnum = true;
					break;
				default:
					pc.Error("Expecting class, interface, struct or enum");
					break;
			}
			pc.Advance();
			result.Name = _ParseIdentifier(pc);
			if (result.IsEnum && ST.lt == pc.SymbolId)
				pc.Error("Enums cannot have generic parameters");
			result.TypeParameters.AddRange(_ParseTypeParams(pc));

			// parse TypeDeclPart
			result.BaseTypes.AddRange(_ParseBaseTypes(pc,result.IsEnum));
			while(ST.whereKeyword==pc.SymbolId)
			{
				if (result.IsEnum)
					pc.Error("Enums cannot have generic type constraints");
				pc.Advance();
				var tpn = _ParseIdentifier(pc);
				CodeTypeParameter tp = null;
				for(int ic = result.TypeParameters.Count,i=0;i<ic;++i)
				{
					var tpt = result.TypeParameters[i];
					if(0==string.Compare(tpn,tpt.Name,StringComparison.InvariantCulture))
					{
						tp = tpt;
						break;
					}
				}
				if (null == tp)
					pc.Error("Reference to undefined type parameter " + tpn);
				if (ST.colon != pc.SymbolId)
					pc.Error("Expecting : in type constraint");
				pc.Advance();
				while (!pc.IsEnded && ST.whereKeyword != pc.SymbolId && ST.lbrace!=pc.SymbolId)
				{
					if (ST.newKeyword == pc.SymbolId)
					{
						pc.Advance();
						if (ST.lparen != pc.SymbolId)
							pc.Error("Expecting ( in constructor type constraint");
						pc.Advance();
						if (ST.rparen != pc.SymbolId)
							pc.Error("Expecting ) in constructor type constraint");
						pc.Advance();
						tp.HasConstructorConstraint = true;
					}
					else
						tp.Constraints.Add(_ParseType(pc));
					if (ST.whereKeyword== pc.SymbolId || ST.lbrace==pc.SymbolId)
						break;
					var l2 = pc.Line;
					var c2 = pc.Column;
					var p2 = pc.Position;
					if (ST.comma != pc.SymbolId)
						pc.Error("Expecting , in type constraint list");
					pc.Advance();
					if (ST.whereKeyword == pc.SymbolId || ST.lbrace==pc.SymbolId)
						pc.Error("Expecting type constraint in type constraint list", l2, c2, p2);
				}
			}
			if (ST.lbrace != pc.SymbolId)
				pc.Error("Expecting { in type declaration");
			pc.Advance(false);
			if (!result.IsEnum)
			{
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
				{
					result.Members.Add(_ParseMember(pc, result.IsInterface));
				}
			} else
			{
				while(!pc.IsEnded && ST.rbrace !=pc.SymbolId)
				{
					result.Members.Add(_ParseEnumMember(pc));
				}
			}
			if (ST.rbrace != pc.SymbolId)
				pc.Error("Unterminated type declaration", line, column, position);
			pc.Advance(false);
			while (ST.directive == pc.SymbolId && pc.Value.StartsWith("#end", StringComparison.InvariantCulture))
				result.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
			return result;
		}
		static CodeTypeParameterCollection _ParseTypeParams(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var result = new CodeTypeParameterCollection();
			if(ST.lt==pc.SymbolId)
			{
				pc.Advance();
				while(!pc.IsEnded && ST.gt!=pc.SymbolId)
				{
					result.Add(_ParseTypeParam(pc));
					if (ST.gt == pc.SymbolId)
						break;
					var l2 = pc.Line;
					var c2 = pc.Column;
					var p2 = pc.Position;
					if (ST.comma != pc.SymbolId)
						pc.Error("Expecting , in type parameter list");
					pc.Advance();
					if (ST.gt == pc.SymbolId)
						pc.Error("Expecting type parameter in type parameter list", l2, c2, p2);
				}
				if (ST.gt != pc.SymbolId)
					pc.Error("Unterminated type parameter list", l, c, p);
				pc.Advance();
			}
			return result;
		}
		static CodeTypeReferenceCollection _ParseBaseTypes(_PC pc,bool isEnum = false)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var result = new CodeTypeReferenceCollection();
			if (ST.colon == pc.SymbolId)
			{
				pc.Advance();
				while (!pc.IsEnded && ST.lbrace != pc.SymbolId && ST.whereKeyword!=pc.SymbolId)
				{
					if (isEnum && 0 < result.Count)
						pc.Error("Enums can only inherit from one base type");
					result.Add(_ParseType(pc));
					if (ST.lbrace == pc.SymbolId || ST.whereKeyword==pc.SymbolId)
						break;
					var l2 = pc.Line;
					var c2 = pc.Column;
					var p2 = pc.Position;
					if (ST.comma != pc.SymbolId)
						pc.Error("Expecting , in base type list");
					pc.Advance();
					if (ST.lbrace == pc.SymbolId || ST.whereKeyword==pc.SymbolId)
						pc.Error("Expecting type in base type list", l2, c2, p2);
				}
				if (ST.lbrace != pc.SymbolId && ST.whereKeyword!=pc.SymbolId)
					pc.Error("Unterminated base type list", l, c, p);
			}
			return result;
		}
		static CodeTypeParameter _ParseTypeParam(_PC pc)
		{
			IDictionary<string, CodeAttributeDeclarationCollection> ca = null;
			if(ST.lbracket==pc.SymbolId)
				ca=_ParseAttributeGroups(pc);
			var result = new CodeTypeParameter(_ParseIdentifier(pc));
			_AddCustomAttributes(ca, "", result.CustomAttributes);
			_CheckCustomAttributes(ca, pc);
			return result;

		}
		internal static CodeTypeMember ParseMember(IEnumerable<Token> tokenizer)
		{
			var pc = new _PC(tokenizer);
			pc.Advance(false);
			return _ParseMember(pc);
		}
		internal static CodeTypeMemberCollection ParseMembers(IEnumerable<Token> tokenizer)
		{
			var pc = new _PC(tokenizer);
			pc.Advance(false);
			return _ParseMembers(pc);
		}
		static CodeTypeMemberCollection _ParseMembers(_PC pc)
		{
			var result = new CodeTypeMemberCollection();
			while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
				result.Add(_ParseMember(pc));
			return result;
		}
		static CodeTypeMember _ParseMember(_PC pc, bool isInterfaceMember=false)
		{
			var line = pc.Line;
			var column = pc.Column;
			var position = pc.Position;
			var l = line;
			var c = column;
			var p = position;
			CodeTypeMember result = null;
			CodeLinePragma lp=null;
			var startDirs = new CodeDirectiveCollection();
			var comments = new CodeCommentStatementCollection();
			while(ST.directive==pc.SymbolId || ST.lineComment==pc.SymbolId || ST.blockComment==pc.SymbolId)
			{
				switch(pc.SymbolId)
				{
					case ST.directive:
						var d = _ParseDirective(pc);
						var llp = d as CodeLinePragma;
						if (null != llp)
							lp = llp;
						else if (null != d)
							startDirs.Add(d as CodeDirective);
						break;
					case ST.blockComment:
						comments.Add(_ParseCommentStatement(pc));
						break;
					case ST.lineComment:
						comments.Add(_ParseCommentStatement(pc,true));
						break;
				}
			}
			IDictionary<string,CodeAttributeDeclarationCollection> customAttributes=null;
			
			if (ST.lbracket == pc.SymbolId)
				customAttributes = _ParseAttributeGroups(pc);
			var seen = new HashSet<string>(StringComparer.InvariantCulture);
			line = pc.Line;
			column = pc.Column;
			position = pc.Position;
			while (ST.privateKeyword == pc.SymbolId ||
				ST.publicKeyword == pc.SymbolId ||
				ST.internalKeyword == pc.SymbolId ||
				ST.protectedKeyword == pc.SymbolId ||
				ST.staticKeyword == pc.SymbolId ||
				ST.abstractKeyword == pc.SymbolId ||
				ST.overrideKeyword == pc.SymbolId ||
				ST.virtualKeyword == pc.SymbolId ||
				ST.newKeyword == pc.SymbolId ||
				ST.constKeyword == pc.SymbolId)
			{
				if (!seen.Add(pc.Value))
					pc.Error(string.Format("Duplicate attribute {0} specified in member", pc.Value));
				pc.Advance();
			}
			if (isInterfaceMember && (1 < seen.Count || (1 == seen.Count && !seen.Contains("new"))))
				pc.Error("Interface members may not have modifiers except for new");
			if (seen.Contains("abstract"))
			{
				if (seen.Contains("private") ||
					(!seen.Contains("public") &&
					!seen.Contains("protected") &&
					!seen.Contains("internal")))
					pc.Error("Abstract members cannot be private");
				if (seen.Contains("static"))
					pc.Error("Abstract members cannot be static");
			}
			if (seen.Contains("private"))
			{
				if (seen.Contains("public") ||
					seen.Contains("protected") ||
					seen.Contains("internal"))
					pc.Error("Conflicting access modifiers specified on member");
			}
			if (seen.Contains("public"))
			{
				if (seen.Contains("protected") || seen.Contains("internal"))
					pc.Error("Conflicting access modifiers specified on member");
			}
			
			if (ST.eventKeyword == pc.SymbolId)
			{
				// event
				if (seen.Contains("const"))
					pc.Error("Events cannot be const", line, column, position);
				var eve = new CodeMemberEvent().Mark(line,column,position);
				result = eve;
				result.Comments.AddRange(comments);
				if (null != lp) result.LinePragma = lp;
				_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
				_CheckCustomAttributes(customAttributes, pc);
				result.StartDirectives.AddRange(startDirs);
				eve.Attributes = _BuildMemberAttributes(seen);
				pc.Advance();
				eve.Type = _ParseType(pc);
				eve.Name = _ParseIdentifier(pc);
				if (ST.semi != pc.SymbolId)
					pc.Error("Expecting ; in event declaration");
				pc.Advance(false);
				while (ST.directive == pc.SymbolId && pc.Value.StartsWith("#end", StringComparison.InvariantCulture))
					result.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
				return result;

			}
			if (ST.classKeyword == pc.SymbolId ||
					ST.structKeyword == pc.SymbolId ||
					ST.enumKeyword == pc.SymbolId ||
					ST.interfaceKeyword == pc.SymbolId ||
					ST.partialKeyword == pc.SymbolId)
			{
				// type or nested type
				if (seen.Contains("const"))
					pc.Error("Nested types cannot be const", line, column, position);
				if (seen.Contains("virtual"))
					pc.Error("Nested types cannot be virtual", line, column, position);
				if (seen.Contains("override"))
					pc.Error("Nested types cannot override", line, column, position);
				if (seen.Contains("new"))
					pc.Error("Nested types cannot be new", line, column, position);

				if (seen.Contains("static"))
					pc.Error("Types cannot be static in Slang", line, column, position);
				if (isInterfaceMember)
					pc.Error("Interfaces cannot contain nested types", line, column, position);
				result = _ParseTypeDecl(pc, true, line, column, position, seen);
				result.Comments.AddRange(comments);
				if (null != lp) result.LinePragma = lp;
				_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
				_CheckCustomAttributes(customAttributes, pc);
				result.StartDirectives.AddRange(startDirs);
				return result;
				
			}
			// backtrack a little to see if it's a constructor
			// not doing so makes this much more difficult
			var pc2 = pc.GetLookAhead(true);
			var isCtor = false;
			if (ST.verbatimIdentifier ==pc.SymbolId|| ST.identifier== pc.SymbolId)
			{
				var pc3 = pc2.GetLookAhead(true);
				_ParseIdentifier(pc3);
				if (ST.lparen == pc3.SymbolId)
				{
					isCtor = true;
				}
				pc3 = null;
			}
			if (!isCtor)
			{
				if (ST.voidType!=pc2.SymbolId)
				{

					_ParseType(pc2);
				}
				if (ST.lparen != pc2.SymbolId)
					pc2.Advance();
			}

			bool hasAssign = false;
			if (!isCtor && (ST.semi == pc2.SymbolId || (hasAssign = (ST.eq == pc2.SymbolId))))
			{
				// field
				if (seen.Contains("abstract"))
					pc.Error("Fields cannot be abstract", line, column, position);
				if (seen.Contains("virtual"))
					pc.Error("Fields cannot be virtual", line, column, position);
				if (seen.Contains("override"))
					pc.Error("Fields cannot override", line, column, position);
				if (isInterfaceMember)
					pc.Error("Interfaces cannot contain fields", line, column, position);
				var fld = new CodeMemberField().Mark(line,column,position);
				result = fld;
				result.Comments.AddRange(comments);
				if (null != lp) result.LinePragma = lp;
				_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
				_CheckCustomAttributes(customAttributes, pc);
				result.StartDirectives.AddRange(startDirs);
				result.Attributes = _BuildMemberAttributes(seen);
				fld.Type = _ParseType(pc);
				fld.Name = _ParseIdentifier(pc);
				if (hasAssign)
				{
					pc.Advance();
					fld.InitExpression = _ParseExpression(pc);
					if (ST.semi != pc.SymbolId)
						pc.Error("Expecting ; in field definition");
				}
				else if (seen.Contains("const"))
					pc.Error("Const fields must have initializers", line, column, position);
				pc.Advance(false);
				while (ST.directive == pc.SymbolId && pc.Value.StartsWith("#end", StringComparison.InvariantCulture))
					result.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
				return result;
			}
			if (isCtor)
			{
				// constructor
				if (seen.Contains("const"))
					pc.Error("Constructors cannot be const", line, column, position);
				if (isInterfaceMember)
					pc.Error("Interfaces cannot have constructors. Are you missing a return type?", line, column, position);
				var ctor = seen.Contains("static") ? (CodeMemberMethod)new CodeTypeConstructor().Mark(line,column,position) : new CodeConstructor().Mark(line,column,position);
				var cctor = ctor as CodeConstructor;
				result = ctor;
				result.Comments.AddRange(comments);
				if (null != lp) result.LinePragma = lp;
				_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
				_CheckCustomAttributes(customAttributes, pc);
				result.StartDirectives.AddRange(startDirs);
				result.Attributes = _BuildMemberAttributes(seen);
				result.Name = _ParseIdentifier(pc);
				// (from above)
				// we already know the next symbol is 
				// lparen so we don't check here
				pc.Advance();
				// make sure static ctors can't have args
				if (ST.rparen != pc.SymbolId && seen.Contains("static"))
					pc.Error("Static constructors cannot have arguments");
				ctor.Parameters.AddRange(_ParseParamList(pc));
				if (ST.rparen != pc.SymbolId)
					pc.Error("Expecting ) in constructor definition");
				pc.Advance();
				if (ST.colon == pc.SymbolId)
				{
					pc.Advance();
					if(seen.Contains("static"))
					{
						if(ST.baseRef == pc.SymbolId)
							pc.Error("Static constructors cannot have \"base\" constructor chains");
						else if(ST.thisRef==pc.SymbolId)
							pc.Error("Static constructors cannot have \"this\" constructor chains");
					}
					switch(pc.SymbolId)
					{
						case ST.baseRef:
							pc.Advance();
							if (ST.lparen != pc.SymbolId)
								pc.Error("Expecting ( in \"base\" constructor chain");
							pc.Advance();
							cctor.BaseConstructorArgs.AddRange(_ParseArgList(pc));
							break;
						case ST.thisRef:
							pc.Advance();
							if (ST.lparen != pc.SymbolId)
								pc.Error("Expecting ( in \"this\" constructor chain");
							pc.Advance();
							cctor.ChainedConstructorArgs.AddRange(_ParseArgList(pc));
							break;
						default:
							pc.Error("Expecting this or base");
							break;
					}
					if (ST.rparen != pc.SymbolId)
						pc.Error("Expecting ) in constructor chain");
					pc.Advance();
				}
				l = pc.Line;
				c = pc.Column;
				p = pc.Position;
				if (ST.lbrace != pc.SymbolId)
					pc.Error("Expecting body in constructor");
				pc.Advance();
				
				while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
					ctor.Statements.Add(_ParseStatement(pc,true));
				if (ST.rbrace != pc.SymbolId)
					pc.Error("Unterminated constructor body", l,c,p);
				pc.Advance(false);
				while (ST.directive == pc.SymbolId && pc.Value.StartsWith("#end", StringComparison.InvariantCulture))
					result.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
				return result;
			}
			else
			{
				CodeTypeReference type = null;
				// we've narrowed it to a property or a method
				// try methods first
				var isVoid = false;
				if (ST.voidType == pc.SymbolId)
				{
					// by now we know it's a method
					// in this case but we don't care
					// because we try methods first anyway
					pc.Advance();
					isVoid = true;
				}
				else
					type = _ParseType(pc);
				var piType = _ParsePrivateImplementationType(pc);
				var isThis = false;
				string name = "Item";
				if (ST.thisRef == pc.Current.SymbolId)
				{
					isThis = true;
					pc.Advance();
				}
				else
					name = _ParseIdentifier(pc);
				if (!isThis && ST.lparen == pc.SymbolId)
				{
					// now we can be sure it is a method
					if (seen.Contains("const"))
						pc.Error("Methods cannot be const",line, column, position);
					pc.Advance(); // advance past lparen
					// if it's public we'll have to add ImplementationTypes later
					var meth = new CodeMemberMethod().Mark(line,column,position,seen.Contains("public"));
					result = meth;
					result.Comments.AddRange(comments);
					if (null != lp) result.LinePragma = lp;
					_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
					_AddCustomAttributes(customAttributes, "return", meth.ReturnTypeCustomAttributes);
					_CheckCustomAttributes(customAttributes, pc);
					result.StartDirectives.AddRange(startDirs);
					result.Attributes = _BuildMemberAttributes(seen);
					meth.PrivateImplementationType = piType;
					meth.Parameters.AddRange(_ParseMethodParamList(pc));
					meth.ReturnType = type;
					meth.Name = name;
					if (ST.rparen != pc.SymbolId)
						pc.Error("Expecting ) in method definition");
					pc.Advance();
					if (ST.semi == pc.SymbolId)
					{
						if (!isInterfaceMember && !seen.Contains("abstract"))
							pc.Error("Non-abstract methods must declare a body");
						pc.Advance(false);
					}
					else
					{
						if (ST.lbrace != pc.SymbolId)
							pc.Error("Expecting body in method definition");
						pc.Advance();

						while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
							meth.Statements.Add(_ParseStatement(pc,true));
						if (ST.rbrace != pc.SymbolId)
							pc.Error("Unterminated method body", l, c, p);
						pc.Advance(false);
						
					}
					while (ST.directive == pc.SymbolId && pc.Value.StartsWith("#end", StringComparison.InvariantCulture))
						result.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
					return result;
				}
				// by process of elimination, this is a property
				if (isVoid)
					pc.Error("Properties must have a type",line, column, position);
				if (seen.Contains("const"))
					pc.Error("Properties cannot be const",line, column, position);
				var prop = new CodeMemberProperty().Mark(line, column, position,seen.Contains("public"));
				result = prop;
				result.Comments.AddRange(comments);
				if (null != lp) result.LinePragma = lp;
				_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
				_CheckCustomAttributes(customAttributes, pc);
				result.StartDirectives.AddRange(startDirs);
				result.Attributes = _BuildMemberAttributes(seen);
				prop.PrivateImplementationType = piType;
				prop.Name = name;
				prop.Type = type;
				if (ST.lbracket == pc.SymbolId)
				{
					if (!isThis)
						pc.Error("Only \"this\" properties may have indexers.",line,column,position);
					pc.Advance();
					prop.Parameters.AddRange(_ParseParamList(pc));
					if (ST.rbracket != pc.SymbolId)
						pc.Error("Expecting ] in property definition");
					pc.Advance();
				}
				else if (isThis)
					pc.Error("\"this\" properties must have indexers.", line,column,position);

				if (ST.lbrace != pc.SymbolId)
					pc.Error("Expecting { in property definition");
				pc.Advance();
				_ParsePropertyAccessors(pc,prop,seen.Contains("abstract") || isInterfaceMember);
				if (ST.rbrace != pc.SymbolId)
					pc.Error("Expecting } in property definition");
				pc.Advance(false);
				while (ST.directive == pc.SymbolId && pc.Value.StartsWith("#end", StringComparison.InvariantCulture))
					result.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
				return result;
			}
		}
		
		static void _ParsePropertyAccessors(_PC pc,CodeMemberProperty prop, bool isAbstractOrInterface=false)
		{
			var sawGet = false;
			var sawSet = false;
			while (ST.getKeyword == pc.SymbolId || ST.setKeyword == pc.SymbolId)
			{
				if (ST.getKeyword == pc.SymbolId)
				{
					if (sawGet)
						pc.Error("Only one get accessor may be specified");
					sawGet = true;
					prop.HasGet = true;
					pc.Advance();
					if (ST.semi == pc.SymbolId)
					{
						if (!isAbstractOrInterface)
							pc.Error("Non abstract property gets must declare a body");
						pc.Advance();
					}
					else if (ST.lbrace == pc.SymbolId)
					{
						if(isAbstractOrInterface)
							pc.Error("Abstract and interface property gets must not declare a body");
						prop.GetStatements.AddRange(_ParseStatementOrBlock(pc));
					}
					else
						pc.Error("Unexpected token found in property get declaration");
				}
				else if (ST.setKeyword == pc.SymbolId)
				{
					if (sawSet)
						pc.Error("Only one set accessor may be specified");
					sawSet = true;
					prop.HasSet = true;
					pc.Advance();
					if (ST.semi == pc.SymbolId)
					{
						if (!isAbstractOrInterface)
							pc.Error("Non abstract property sets must declare a body");
						pc.Advance();
					}
					else if (ST.lbrace == pc.SymbolId)
					{
						if (isAbstractOrInterface)
							pc.Error("Abstract and interface property sets must not declare a body");
						prop.SetStatements.AddRange(_ParseStatementOrBlock(pc));
					}
					else
						pc.Error("Unexpected token found in property set declaration");
				}
				else
					pc.Error("Expecting a get or set accessor");
			}
		}

		static CodeParameterDeclarationExpressionCollection _ParseParamList(_PC pc)
		{
			var result = new CodeParameterDeclarationExpressionCollection();
			while(!pc.IsEnded && ST.rparen!=pc.SymbolId && ST.rbracket!=pc.SymbolId)
			{
				result.Add(new CodeParameterDeclarationExpression(_ParseType(pc), _ParseIdentifier(pc)));
				if (ST.rparen == pc.SymbolId || ST.rbracket == pc.SymbolId)
					break;
				var l2 = pc.Line;
				var c2 = pc.Column;
				var p2 = pc.Position;
				if (ST.comma != pc.SymbolId)
					pc.Error("Expecting , in parameter list");
				pc.Advance();
				if (ST.rbracket == pc.SymbolId || ST.rparen == pc.SymbolId)
					pc.Error("Expecting parameter in parameter list", l2, c2, p2);
			}
			return result;
		}
		static CodeParameterDeclarationExpressionCollection _ParseMethodParamList(_PC pc)
		{
			var result = new CodeParameterDeclarationExpressionCollection();
			while (ST.rparen != pc.SymbolId && ST.rbracket != pc.SymbolId)
			{
				var fd = FieldDirection.In;
				if(ST.refKeyword==pc.SymbolId)
				{
					fd = FieldDirection.Ref;
					pc.Advance();
				} else if(ST.outKeyword==pc.SymbolId)
				{
					fd = FieldDirection.Out;
					pc.Advance();
				}
				var pd = new CodeParameterDeclarationExpression(_ParseType(pc), _ParseIdentifier(pc));
				pd.Direction = fd;
				result.Add(pd);
				if (ST.rparen == pc.SymbolId || ST.rbracket == pc.SymbolId)
					break;
				var l2 = pc.Line;
				var c2 = pc.Column;
				var p2 = pc.Position;
				if (ST.comma != pc.SymbolId)
					pc.Error("Expecting , in method parameter list");
				pc.Advance();
				if (ST.rbracket == pc.SymbolId || ST.rparen == pc.SymbolId)
					pc.Error("Expecting parameter in method parameter list", l2, c2, p2);
			}
			return result;
		}
		static CodeTypeReference _ParsePrivateImplementationType(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var pc2 = pc.GetLookAhead(true);
			// here's a trick. We're going to capture a subset of tokens and then
			// create a new parse context, feeding it our subset.
			var toks = new List<Token>();
			while (!pc2.IsEnded && (ST.lparen != pc2.SymbolId) && ST.lbrace != pc2.SymbolId && ST.lbracket != pc2.SymbolId)
			{
				toks.Add(pc2.Current);
				pc2.Advance();
			}
			if (pc2.IsEnded)
				pc2.Error("Unexpected end of input parsing private implementation type");
			if (2 < toks.Count)
			{
				// remove the last two tokens
				toks.RemoveAt(toks.Count - 1);
				toks.RemoveAt(toks.Count - 1);
				// now manufacture a comma token 
				// to get ParseType to terminate
				// (don't actually need this for 
				// the hand rolled parser but keep
				// it to keep the code similar)
				var t = default(Token);
				t.SymbolId = ST.comma;
				t.Value = ",";
				t.Line = pc2.Line;
				t.Column = pc2.Column;
				t.Position = pc2.Position;
				toks.Add(t);

				var pc3 = new _PC(toks);
				pc3.EnsureStarted();
				var type = _ParseType(pc3);
				// advance an extra position to clear the trailing ".", which we discard
				var adv = 0;
				while (adv < toks.Count)
				{
					pc.Advance();
					++adv;
				}
				return type;
			}
			return null;
		}
		static CodeTypeMember _ParseEnumMember(_PC pc)
		{
			var line = pc.Line;
			var column = pc.Column;
			var position = pc.Position;
			var l = line;
			var c = column;
			var p = position;
			CodeTypeMember result = null;
			CodeLinePragma lp = null;
			var startDirs = new CodeDirectiveCollection();
			var comments = new CodeCommentStatementCollection();
			while (ST.directive == pc.SymbolId || ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
			{
				switch (pc.SymbolId)
				{
					case ST.directive:
						var d = _ParseDirective(pc);
						var llp = d as CodeLinePragma;
						if (null != llp)
							lp = llp;
						else if (null != d)
							startDirs.Add(d as CodeDirective);
						break;
					case ST.blockComment:
						comments.Add(_ParseCommentStatement(pc));
						break;
					case ST.lineComment:
						comments.Add(_ParseCommentStatement(pc, true));
						break;
				}
			}
			IDictionary<string, CodeAttributeDeclarationCollection> customAttributes = null;

			if (ST.lbracket == pc.SymbolId)
				customAttributes = _ParseAttributeGroups(pc);
			line = pc.Line;
			column = pc.Column;
			position = pc.Position;
			var fld = new CodeMemberField().Mark(line, column, position);
			result = fld;
			result.Comments.AddRange(comments);
			if (null != lp) result.LinePragma = lp;
			_AddCustomAttributes(customAttributes, "", result.CustomAttributes);
			_CheckCustomAttributes(customAttributes, pc);
			result.StartDirectives.AddRange(startDirs);
			fld.Type = _ParseType(pc);
			fld.Name = _ParseIdentifier(pc);
			if (ST.eq==pc.SymbolId)
			{
				pc.Advance();
				fld.InitExpression = _ParseExpression(pc);
			}
			if (ST.comma == pc.SymbolId)
				pc.Advance(false);
			else if (ST.rbrace != pc.SymbolId)
				pc.Error("Expecting , or } in enum declaration");
			while (ST.directive == pc.SymbolId && pc.Value.StartsWith("#end", StringComparison.InvariantCulture))
				result.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
			return result;
			
		}

		[System.Diagnostics.DebuggerNonUserCode()]
		static void _CheckCustomAttributes(IDictionary<string,CodeAttributeDeclarationCollection> attrs,_PC pc)
		{
			if(null!=attrs && 0<attrs.Count)
			{
				foreach(var kvp in attrs)
				{
					var ctr = kvp.Value[0].AttributeType;
					var o = ctr.UserData["slang:line"];
					int l=0, c=0;
					long p=0L;
					if (o is int)
						l = (int)o;
					o = ctr.UserData["slang:column"];
					if (o is int)
						c = (int)o;
					o = ctr.UserData["slang:position"];
					if (o is long)
						p = (long)o;

					pc.Error("Attribute specified on invalid target "+kvp.Key, l, c, p);
				}
			}
		}
		static void _AddCustomAttributes(IDictionary<string,CodeAttributeDeclarationCollection> attrs, string target,CodeAttributeDeclarationCollection to)
		{
			if (null == attrs) return;
			if (null == target) target = "";
			CodeAttributeDeclarationCollection col;
			if(attrs.TryGetValue(target,out col))
			{
				to.AddRange(col);
				attrs.Remove(target);
			}
		}
		static IDictionary<string,CodeAttributeDeclarationCollection> _ParseAttributeGroups(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;

			if (ST.lbracket != pc.SymbolId)
				pc.Error("Expecting [ in attribute group declaration");

			var result = new Dictionary<string, CodeAttributeDeclarationCollection>();
			while(ST.lbracket==pc.SymbolId)
			{
				var kvp = _ParseAttributeGroup(pc);
				CodeAttributeDeclarationCollection col;
				if (!result.TryGetValue(kvp.Key, out col))
					result.Add(kvp.Key, kvp.Value);
				else
					col.AddRange(kvp.Value);
			}
			return result;
		}
		static KeyValuePair<string,CodeAttributeDeclarationCollection> _ParseAttributeGroup(_PC pc,bool skipCommentsAndDirectives=true)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.lbracket != pc.SymbolId)
				pc.Error("Expecting [ in attribute group declaration");
			pc.Advance();
			var key = "";
			switch(pc.SymbolId)
			{
				case ST.assemblyKeyword:
				case ST.returnKeyword:
					key = pc.Value;
					pc.Advance();
					if (ST.colon != pc.SymbolId)
						pc.Error("Expecting : after attribute group target");
					pc.Advance();
					break;
			}
			var val = new CodeAttributeDeclarationCollection();
			while (!pc.IsEnded && ST.rbracket != pc.SymbolId)
			{
				val.Add(_ParseAttributeDeclaration(pc));
				if (ST.rbracket == pc.SymbolId)
					break;
				var l2 = pc.Line;
				var c2 = pc.Column;
				var p2 = pc.Position;
				if (ST.comma != pc.SymbolId)
					pc.Error("Expecting , in attribute group attributes list");
				pc.Advance();
				if (ST.rbracket == pc.SymbolId)
					pc.Error("Expecting attribute in attribute group attributes list", l2, c2, p2);
			}
			if (0 == val.Count)
				pc.Error("Expecting attribute declaration in attribute group");
			if (ST.rbracket != pc.SymbolId)
				pc.Error("Unterminated attribute group", l, c, p);
			pc.Advance(skipCommentsAndDirectives);
			return new KeyValuePair<string, CodeAttributeDeclarationCollection>(key, val);
		}
		static CodeAttributeDeclaration _ParseAttributeDeclaration(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var ctr = _ParseTypeBase(pc);
			// don't know if we need to append "Attribute" to type or not yet
			ctr.UserData["slang:unresolved"] = true;
			var result = new CodeAttributeDeclaration(ctr);
			if (ST.lparen==pc.SymbolId)
			{
				pc.Advance();
				result.Arguments.AddRange(_ParseAttributeArguments(pc));
				if (ST.rparen != pc.SymbolId)
					pc.Error("Unterminated custom attribute argument list", l, c, p);
				pc.Advance();
			}
			return result;
		}
		static CodeAttributeArgumentCollection _ParseAttributeArguments(_PC pc)
		{
			var result = new CodeAttributeArgumentCollection();
			while (!pc.IsEnded && ST.rparen != pc.SymbolId)
			{
				result.Add(_ParseAttributeArgument(pc));
				if (ST.rparen == pc.SymbolId)
					break;
				var l = pc.Line;
				var c = pc.Column;
				var p = pc.Position;
				if (ST.comma != pc.SymbolId)
					pc.Error("Expecting , in attribute argument list");
				pc.Advance();
				if (ST.rparen == pc.SymbolId)
					pc.Error("Expecting argument in attribute argument list", l, c, p);
			}
			return result;
		}
		static CodeAttributeArgument _ParseAttributeArgument(_PC pc)
		{
			// can be a named or unnamed argument so we have to backtrack
			var pc2 = pc.GetLookAhead(true);
			if(ST.verbatimIdentifier==pc2.SymbolId || ST.identifier==pc2.SymbolId)
			{
				pc2.Advance();
				if(ST.eq==pc2.SymbolId)
				{
					pc2 = null;
					// named argument
					var n = _ParseIdentifier(pc);
					pc.Advance();
					return new CodeAttributeArgument(n, _ParseExpression(pc));
				}
			}
			pc2 = null;
			return new CodeAttributeArgument(_ParseExpression(pc));
		}
		static MemberAttributes _BuildMemberAttributes(HashSet<string> attrs)
		{
			var result = (MemberAttributes)0;
			foreach (var kw in attrs)
			{
				switch (kw)
				{
					case "protected":
						if (attrs.Contains("internal"))
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.FamilyOrAssembly;
						else
							result = (result & ~MemberAttributes.AccessMask) | MemberAttributes.Family;
						break;
					case "internal":
						if (attrs.Contains("protected"))
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
						if (attrs.Contains("virtual"))
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
		static TypeAttributes _BuildMemberTypeAttributes(HashSet<string> attrs)
		{
			var result = TypeAttributes.NestedFamANDAssem;
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
		static TypeAttributes _BuildTypeAttributes(HashSet<string> attrs, int line,int column,long position)
		{
			var result = (TypeAttributes)0;
			foreach (var attr in attrs)
			{
				switch (attr)
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
						throw new SlangSyntaxException("Top level types cannot be private", line, column, position);
					case "virtual":
						throw new SlangSyntaxException("Top level types cannot be virtual", line, column, position);
					case "protected":
						throw new SlangSyntaxException("Top level types cannot be protected", line, column, position);
					case "static":
						throw new SlangSyntaxException("Top level types cannot be static", line, column, position);
					case "new":
						throw new SlangSyntaxException("Top level types cannot be declared new", line, column, position);
					case "override":
						throw new SlangSyntaxException("Top level types cannot be declared override", line, column, position);

				}
			}
			return result;
		}
	}
}
