using System;
using System.Collections.Generic;
using System.Text;

namespace scratch
{
	partial class ExpressionParser
	{
		internal static HashSet<string> Keywords = _BuildKeywords();
		static HashSet<string> _BuildKeywords()
		{
			var result = new HashSet<string>();
			string[] sa = "abstract|as|ascending|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|equals|explicit|extern|event|false|finally|fixed|float|for|foreach|get|global|goto|if|implicit|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield".Split(new char[] { '|' });

			for (var i = 0; i < sa.Length; ++i)
				result.Add(sa[i]);

			return result;
		}
		static ParseNode _ParseCastExpression(ParserContext context)
		{
			int line = context.Line;
			int column = context.Column;
			long position = context.Position;
			var children = new List<ParseNode>();
			if ("(" != context.Value)
				context.Error("Expecting ( as start of expression or cast");
			children.Add(new ParseNode(lparen, "lparen", context.Value, context.Line, context.Column, context.Position));
			context.Advance();
			children.AddRange(ParseTypeCastExpressionPart(context).Children);
			children.Add(ParseUnaryExpression(context));
			return new ParseNode(ExpressionParser.CastExpression, "CastExpression", children.ToArray(), line, column, position);
		}
		static ParseNode _ParseArraySpec(ParserContext context)
		{
			int line = context.Line;
			int column = context.Column;
			long position = context.Position;
			if (lbracket!=context.SymbolId)
				context.Error("Expecting start of array spec");
			ParserContext pc = context.GetLookAhead(true);
			pc.Advance();
			var children = new List<ParseNode>();
			if (rbracket!=pc.SymbolId)
			{

				//children.Add(new ParseNode(lbracket, "lbracket", context.Value, context.Line, context.Column, context.Position));
				context.Advance();
				children.Add(ParseArraySpecExpressionList(context));
				while (lbracket == context.SymbolId)
					children.Add(ParseTypeArraySpec(context));
				
				return new ParseNode(ArraySpec, "ArraySpec", children.ToArray() , line, column, position);
			}
			else
			{
				while (lbracket == context.SymbolId)
					children.Add(ParseTypeArraySpec(context));
				children.Add(ParseArrayInitializer(context));
				return new ParseNode(ArraySpec, "ArraySpec", children.ToArray(), line, column, position);
			}
		}
		static bool _IsCastExpression(ParserContext context)
		{
			context = context.GetLookAhead(true);
			try
			{
				if ("(" != context.Value)
					return false;
				context.Advance();
				ParseNode type = ParseTypeCastExpressionPart(context);
				ParseNode expr = ParseUnaryExpression(context);
				return true;
			}
			catch (SyntaxException)
			{
			}
			return false;
		}
		
		static ParseNode _ParseTypeOrFieldRef(ParserContext context,bool skipDot=false)
		{
			// The logic for this is ridiculous.

			// first check for an identifier followed by a . that is NOT a namespace 
			// followed by a generic type definition (our special case)
			// if we find the type, we error just so we can break the loop
			// fill the error context with something other than null, and 
			// continue. Otherwise we parse a type reference followed by 
			// member references.
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			if (skipDot)
				context.Advance();
			var pc2 = context.GetLookAhead(true);
			SyntaxException sx , sx2= null;
			var fieldAdvCount = 0;
			try
			{


				ParseIdentifier(pc2);
				// handle the case where this is a namespace.
				if (dot == pc2.SymbolId)
				{
					var pc4 = pc2.GetLookAhead(true);
					while (dot == pc4.SymbolId)
					{
						pc4.Advance();
						ParseIdentifier(pc4);
					}
					var i = pc4.AdvanceCount;
					while (1 < i)
					{
						pc2.Advance();
						--i;
					}
				}
				// now check for a generic type argument
				// (disambiguates from 'less than' by trying to parse 
				// the next element as a type.
				// the error will propagate and most
				// certainly be overridden downstream
				// to the type parsing bit, which will
				// then try to parse it as a type
				if (lt==pc2.SymbolId)
				{
					pc2.Advance();
					var isTypeArg=false;
					try
					{
						ParseType(pc2);
						if (gt == pc2.SymbolId || comma == pc2.SymbolId)
						{
							isTypeArg = true;
						}
					}
					catch(SyntaxException)
					{
						
					}
					// use the original context for position info
					if(isTypeArg)
						context.Error("Unexpected < found in found in FielRef");
				}
				// otherwise treat it all as a field ref.
				var children = new List<ParseNode>();
				children.AddRange(ParseIdentifier(context).Children);
				return new ParseNode(FieldRef, "FieldRef", children.ToArray(), line, column, position);
			}
			catch (SyntaxException ex) { sx = ex; }
			// for error reporting:
			fieldAdvCount = pc2.AdvanceCount;
			// parse type ref
			// we have to take this over manually
			var pc3 = context.GetLookAhead(true);
			try
			{
				// this can fail but it will advance the cursor
				ParseType(pc3);
				// but if it doesn't this is a typeref
				return new ParseNode(TypeRef, "TypeRef", new ParseNode[] { ParseType(context) }, line, column, position);
				
			}
			catch(SyntaxException ex)
			{
				sx2 = ex;
			}
			var advCount = pc3.AdvanceCount;
			// for error reporting:
			var typeAdvCount = advCount;
			// store all the tokens we read
			var toks = new List<Token>();
			pc2 = context.GetLookAhead(true);
			while(0!=advCount)
			{

				toks.Add(pc2.Current);
				pc2.Advance();
				--advCount;
			}
			// remove tokens until it parses
			while (1<toks.Count)
			{
				var throwMemberRef = false;
				toks.RemoveAt(toks.Count - 1);
				var t = default(Token);
				t.SymbolId = rparen;
				toks.Add(t);
				try
				{
					pc3 = new ParserContext(toks);
					pc3.EnsureStarted();
					ParseType(pc3);
					// if it gets here we can finish
					pc3 = new ParserContext(toks);
					pc3.EnsureStarted();
					var pn = ParseType(pc3);
					var i = toks.Count-1;
					pn = new ParseNode(pn.SymbolId, pn.Symbol, pn.Children, context.Line, context.Column, context.Position);
					while (0!=i)
					{
						context.Advance();
						--i;
					}
					
					var children = new ParseNode[2];
					children[0] = pn;
					return new ParseNode(TypeRef, "TypeRef", new ParseNode[] { pn }, line, column, position);
				}
				catch (SyntaxException ex)
				{
					if (throwMemberRef)
						throw ex;
					else if (fieldAdvCount < typeAdvCount)
						throw sx2;
					throw sx;
				}
			}
			if (fieldAdvCount < typeAdvCount)
				throw sx2;
			throw sx;
		}
		
		
	}
	partial class TypeDeclParser
	{
		/*static ParseNode _ParseWhereClauses(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			if (identifier2 != context.SymbolId)
				context.Error("Expecting identifier in where clause");
			var children = new List<ParseNode>();
			while (EosSymbol != context.SymbolId && lbrace != context.SymbolId)
			{
				children.Add(ParseWhereClause(context));
				if (comma == context.SymbolId)
					context.Advance();
			}
			if (lbrace != context.SymbolId)
				throw new SyntaxException(string.Format("Unterminated where clause at line {0}, column {1}, position {2}", line, column, position), line, column, position);
			return new ParseNode(WhereClauses, "WhereClauses", children.ToArray(), line, column, position);
		}*/
		static ParseNode _ParseMember(ParserContext context)
		{
			// this just wraps below with a Member node
			var pn = _ParseMemberImpl(context);
			return new ParseNode(Member, "Member", new ParseNode[] { pn }, pn.Line, pn.Column, pn.Position);
		}
		static ParseNode _ParseMemberImpl(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var cc = new List<ParseNode>();
			var l = line;
			var c = column;
			var p = position;
			
			if (null != context.Skipped)
			{
				for (var i = 0; i < context.Skipped.Count; i++)
				{
					var t = context.Skipped[i];
					if (StatementParser.lineComment == t.SymbolId)
					{
						cc.Add(new ParseNode(t.SymbolId, "lineComment", t.Value, t.Line, t.Column, t.Position));
					}
					else if (StatementParser.blockComment == t.SymbolId)
						cc.Add(new ParseNode(t.SymbolId, "blockComment", t.Value, t.Line, t.Column, t.Position));
				}
				if (0 < context.Skipped.Count)
				{
					var tt = context.Skipped[0];
					l = tt.Line;
					c = tt.Column;
					p = tt.Position;
				}
			}
			
			var cpn = new ParseNode(StatementParser.Comments, "Comments", cc.ToArray(), l, c, p);
			line = context.Line;
			column = context.Column;
			position = context.Position;

			var attrs = new List<ParseNode>();
			var seen = new HashSet<string>();
			ParseNode customAttributes;
			if (lbracket==context.SymbolId)
			{
				customAttributes = ParseCustomAttributeGroups(context);
			} else
				customAttributes = new ParseNode(CustomAttributeGroups, "CustomAttributeGroups", new ParseNode[0], line, column, position);
			while (privateKeyword == context.SymbolId ||
				publicKeyword == context.SymbolId ||
				internalKeyword == context.SymbolId ||
				protectedKeyword == context.SymbolId ||
				staticKeyword == context.SymbolId ||
				abstractKeyword == context.SymbolId ||
				overrideKeyword == context.SymbolId ||
				newKeyword == context.SymbolId ||
				constKeyword == context.SymbolId)
			{
				if (!seen.Add(context.Value))
					context.Error(string.Format("Duplicate attribute {0} specified in member", context.Value));
				attrs.Add(new ParseNode(context.SymbolId, string.Concat(context.Value, "Keyword"), context.Value, context.Line, context.Column, context.Position));
				context.Advance();
			}
			if (seen.Contains("abstract"))
			{
				if (seen.Contains("private") ||
					(!seen.Contains("public") &&
					!seen.Contains("protected") &&
					!seen.Contains("internal")))
					context.Error("Abstract members cannot be private.");
				if (seen.Contains("static"))
					context.Error("Abstract members cannot be static.");
			}
			if (seen.Contains("private"))
			{
				if (seen.Contains("public") ||
					seen.Contains("protected") ||
					seen.Contains("internal"))
					context.Error("Conflicting access modifiers specified on member");
			}
			if (seen.Contains("public"))
			{
				if (seen.Contains("protected") || seen.Contains("internal"))
					context.Error("Conflicting access modifiers specified on member");
			}
			if (eventKeyword == context.SymbolId)
			{
				// event
				if (seen.Contains("const"))
					context.Error("Events cannot be const.");
				var children = new List<ParseNode>(6);
				children.Add(cpn);
				children.Add(customAttributes);
				children.Add(new ParseNode(MemberAttributes, "MemberAttributes", attrs.ToArray(), line, column, position));
				children.Add(new ParseNode(eventKeyword, "eventKeyword", context.Value, context.Line, context.Column, context.Position));
				context.Advance();
				children.Add(ExpressionParser.ParseType(context));
				children.AddRange(ExpressionParser.ParseIdentifier(context).Children);
				if (semi != context.SymbolId)
					context.Error("Expecting ; in event declaration");
				children.Add(new ParseNode(semi, "semi", ";", context.Line, context.Column, context.Position));
				context.Advance();
				return new ParseNode(Event, "Event", children.ToArray(), line, column, position);
			}
			if (classKeyword == context.SymbolId ||
					structKeyword == context.SymbolId ||
					enumKeyword == context.SymbolId ||
					interfaceKeyword == context.SymbolId ||
					partialKeyword == context.SymbolId)
			{
				// type or nested type
				if (seen.Contains("const"))
					context.Error("Nested types cannot be const.");
				if (seen.Contains("static"))
					context.Error("Types cannot be static in Slang.");
				return ParseTypeDecl(context, true,cpn,customAttributes, line, column, position, attrs);
			}
			// backtrack a little to see if it's a constructor
			// not doing so makes this much more difficult
			var pc2 = context.GetLookAhead(true);
			var isCtor = false;
			if (context.SymbolId == verbatimIdentifier || context.SymbolId == identifier2)
			{
				var pc3 = pc2.GetLookAhead(true);
				ExpressionParser.ParseIdentifier(pc3);
				if (lparen == pc3.SymbolId)
				{
					isCtor = true;
				}
				pc3 = null;
			}
			if (!isCtor)
			{
				if (pc2.SymbolId != voidType)
				{

					ExpressionParser.ParseType(pc2);
				}
				if (lparen != pc2.SymbolId)
					pc2.Advance();
			}
			
			bool hasAssign = false;
			if (!isCtor && (semi == pc2.SymbolId || (hasAssign = (eq == pc2.SymbolId))))
			{
				// field
				if (seen.Contains("abstract"))
					throw new SyntaxException(string.Format("Fields cannot be abstract at line {0}, column {1}, position {2}", line, column, position), line, column, position);
				var children = new List<ParseNode>();
				children.Add(cpn);
				children.Add(customAttributes);
				children.Add(new ParseNode(MemberAttributes, "MemberAttributes", attrs.ToArray(), line, column, position));
				children.Add(ExpressionParser.ParseType(context));
				children.AddRange(ExpressionParser.ParseIdentifier(context).Children);
				if (hasAssign)
				{
					children.Add(new ParseNode(eq, "eq", "=", context.Line, context.Column, context.Position));
					context.Advance();
					children.Add(ExpressionParser.ParseExpression(context));
					if (semi != context.SymbolId)
						context.Error("Expecting ; in field definition", context.Line, context.Column, context.Position);
				}
				children.Add(new ParseNode(semi, "semi", ";", context.Line, context.Column, context.Position));
				context.Advance();
				return new ParseNode(Field, "Field", children.ToArray(), context.Line, context.Column, context.Position);
			}
			if (isCtor)
			{
				// constructor
				if (seen.Contains("const"))
					throw new SyntaxException(string.Format("Constructors cannot be const at line {0}, column {1}, position {2}", line, column, position), line, column, position);
				var children = new List<ParseNode>();
				children.Add(cpn);
				children.Add(customAttributes);
				children.Add(new ParseNode(MemberAttributes, "MemberAttributes", attrs.ToArray(), line, column, position));
				children.AddRange(ExpressionParser.ParseIdentifier(context).Children);
				// (from above)
				// we already know the next symbol is 
				// lparen so we don't check here
				children.Add(new ParseNode(lparen, "lparen", "(", context.Line, context.Column, context.Position));
				context.Advance();
				// make sure static ctors can't have args
				if (rparen != context.SymbolId && seen.Contains("static"))
					context.Error("Static constructors cannot have arguments");
				children.Add(ParseParamList(context));
				
				if (rparen != context.SymbolId)
					context.Error("Expecting ) in constructor definition");
				children.Add(new ParseNode(rparen, "rparen", ")", context.Line, context.Column, context.Position));
				context.Advance();
				if (colon == context.SymbolId)
				{
					children.Add(new ParseNode(colon, "colon", ":", context.Line, context.Column, context.Position));
					context.Advance();
					var ccc = ParseConstructorChain(context);
					if ("base" == ccc.Children[0].Value && seen.Contains("static"))
						context.Error("Static constructors cannot have \"base\" constructor chains.");
					if ("this" == ccc.Children[0].Value && seen.Contains("static"))
						context.Error("Static constructors cannot have \"this\" constructor chains.");

					children.Add(ccc);
				}
				if (lbrace != context.SymbolId)
					context.Error("Expecting body in constructor");
				children.Add(StatementParser.ParseStatementBlock(context));

				return new ParseNode(Constructor, "Constructor", children.ToArray(), line, column, position);
			}
			else
			{
				// inside an else to fix "children" scope error because we used it above
				var children = new List<ParseNode>();
				children.Add(cpn);
				children.Add(customAttributes);
				children.Add(new ParseNode(MemberAttributes, "MemberAttributes", attrs.ToArray(), line, column, position));
				// we've narrowed it to a property or a method
				// try methods first
				var isVoid = false;
				if (voidType == context.SymbolId)
				{
					// by now we know it's a method
					// in this case but we don't care
					// because we try methods first anyway
					children.Add(new ParseNode(voidType, "voidType", "void", context.Line, context.Column, context.Position));
					context.Advance();
					isVoid = true;
				}
				else
					children.Add(ExpressionParser.ParseType(context));
				children.Add(ParsePrivateImplementationType(context));
				var isThis = false;
				if (thisRef == context.Current.SymbolId)
				{
					isThis = true;
					children.Add(new ParseNode(thisRef, "thisRef", "this", context.Line, context.Column, context.Position));
					context.Advance();
				}
				else
					children.AddRange(ExpressionParser.ParseIdentifier(context).Children);
				if (!isThis && lparen == context.SymbolId)
				{
					// now we can be sure it is a method
					if (seen.Contains("const"))
						throw new SyntaxException(string.Format("Methods cannot be const at line {0}, column {1}, position {2}", line, column, position), line, column, position);
					children.Add(new ParseNode(lparen, "lparen", "(", context.Line, context.Column, context.Position));
					context.Advance();
					children.Add(ParseMethodParamList(context));
					if (rparen != context.SymbolId)
						context.Error("Expecting ) in method definition.");
					children.Add(new ParseNode(rparen, "rparen", ")", context.Line, context.Column, context.Position));
					context.Advance();
					if (semi == context.SymbolId)
					{

						children.Add(new ParseNode(semi, "semi", ";", context.Line, context.Column, context.Position));
						context.Advance();
					}
					else
						children.Add(StatementParser.ParseStatementBlock(context));
					return new ParseNode(Method, "Method", children.ToArray(), line, column, position);
				}
				// by process of elimination, this is a property
				if (isVoid)
					throw new SyntaxException(string.Format("Properties must have a type at line {0}, column {1}, position {2}", line, column, position), line, column, position);
				if (seen.Contains("const"))
					throw new SyntaxException(string.Format("Properties cannot be const at line {0}, column {1}, position {2}", line, column, position), line, column, position);
				if (ExpressionParser.lbracket == context.SymbolId)
				{
					if (!isThis)
						throw new SyntaxException("Only \"this\" properties may have indexers.", context.Line, context.Column, context.Position);
					children.Add(new ParseNode(ExpressionParser.lbracket, "lbracket", "[", context.Line, context.Column, context.Position));
					context.Advance();
					children.Add(ParseParamList(context));
					if (ExpressionParser.rbracket != context.SymbolId)
						context.Error("Expecting ] in property definition");
					children.Add(new ParseNode(ExpressionParser.rbracket, "rbracket", "]", context.Line, context.Column, context.Position));
					context.Advance();
				} else if (isThis)
					throw new SyntaxException("\"this\" properties must have indexers.", context.Line, context.Column, context.Position);

				if (lbrace != context.SymbolId)
					context.Error("Expecting { in property definition");
				children.Add(new ParseNode(lbrace, "lbrace", "{", context.Line, context.Column, context.Position));
				context.Advance();
				var pn = ParsePropertyAccessors(context);
				// now validate it
				for (var i = 0; i < pn.Children.Length; ++i)
				{
					if (semi != pn.Children[i].Children[1].SymbolId)
					{
						if (seen.Contains("abstract"))
							throw new SyntaxException(string.Format("Abstract properties cannot contain get/set accessor bodies at {0}, column {1}, position {2}", line, column, position), line, column, position);
					}
					else if (!seen.Contains("abstract"))
						throw new SyntaxException(string.Format("Properties must contain get/set accessor bodies at {0}, column {1}, position {2}", line, column, position), line, column, position);
				}
				children.Add(pn);
				if (rbrace != context.SymbolId)
					context.Error("Expecting } in property definition.");
				children.Add(new ParseNode(rbrace, "rbrace", "}", context.Line, context.Column, context.Position));
				context.Advance();
				return new ParseNode(Property, "Property", children.ToArray(), line, column, position);
			}

		}
		static ParseNode _ParseCustomAttributeGroups(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var children = new List<ParseNode>();
			while (lbracket==context.SymbolId)
				children.Add(ParseCustomAttributeGroup(context));
			return new ParseNode(CustomAttributeGroups, "CustomAttributeGroups", children.ToArray(), line, column, position);
		}
		static ParseNode _ParseTypeDeclPart(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var children = new List<ParseNode>();
			// BaseTypes
			if (colon == context.SymbolId)
			{
				context.Advance();
				var bts = new List<ParseNode>();
				while (SlangParser.EosSymbol != context.SymbolId &&
					whereKeyword != context.SymbolId &&
					lbrace != context.SymbolId)
				{
					// collapse BaseType
					bts.Add(ParseBaseType(context).Children[0]);
					// just collapse the commas. We don't need them
					if (comma == context.SymbolId)
						context.Advance();
				}
				if (SlangParser.EosSymbol == context.SymbolId)
					throw new SyntaxException(string.Format("Unterminated base types list at line {0}, column {1}, position {2}", line, column, position), line, column, position);
				children.Add(new ParseNode(BaseTypes, "BaseTypes", bts.ToArray(), line, column, position));
			}
			else
				children.Add(new ParseNode(BaseTypes, "BaseTypes", new ParseNode[0], line, column, position));
			//if (whereKeyword == context.SymbolId)
				children.Add(ParseWhereClauses(context));
			//else
			//	children.Add(new ParseNode(WhereClauses, "WhereClauses", new ParseNode[0], context.Line, context.Column, context.Position));
			if (lbrace != context.SymbolId)
				context.Error("Expecting lbrace");
			context.Advance();
			children.Add(ParseMembers(context));
			if (rbrace != context.SymbolId)
				context.Error("Expecting rbrace");
			context.Advance();
			return new ParseNode(TypeDeclPart, "TypeDeclPart", children.ToArray(), line, column, position);
		}
		static ParseNode _ParseMembers(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var children = new List<ParseNode>();
			while (EosSymbol != context.SymbolId && rbrace != context.SymbolId)
				children.Add(ParseMember(context));
			return new ParseNode(Members, "Members", children.ToArray(), line, column, position);
		}
		internal static ParseNode ParseTypeDecl(ParserContext context, bool isNested, ParseNode comments,ParseNode customAttributes, int line, int column, long position, List<ParseNode> attrs)
		{
			
			var children = new List<ParseNode>();
			if (!isNested)
			{
				var cc = new List<ParseNode>();
				var l = line;
				var c = column;
				var p = position;

				if (null!=context.Skipped)
				{
					for(var i = 0;i<context.Skipped.Count;i++)
					{
						var t = context.Skipped[i];
						if(StatementParser.lineComment==t.SymbolId)
						{
							cc.Add(new ParseNode(t.SymbolId, "lineComment", t.Value, t.Line, t.Column, t.Position));
						} else if(StatementParser.blockComment==t.SymbolId)
							cc.Add(new ParseNode(t.SymbolId, "blockComment", t.Value, t.Line, t.Column, t.Position));
					}
					if(0<context.Skipped.Count)
					{
						var tt = context.Skipped[0];
						l = tt.Line;
						c = tt.Column;
						p = tt.Position;
					}
				}

				children.Add(new ParseNode(StatementParser.Comments, "Comments", cc.ToArray(), l, c, p));
				line = context.Line;
				column = context.Column;
				position = context.Position;
				if (customAttributes == null)
				{
					if (lbracket == context.SymbolId)
					{
						children.Add(ParseCustomAttributeGroups(context));
					}
					else
					{
						children.Add(new ParseNode(CustomAttributeGroups, "CustomAttributeGroups", new ParseNode[0], line, column, position));
					}
				}
				else
					children.Add(customAttributes);
				children.Add(ParseTypeAttributes(context));

			}
			else
			{
				if(null!=comments)
				{
					children.Add(comments);
				}
				if (null != customAttributes)
					children.Add(customAttributes);
				else
					children.Add(new ParseNode(CustomAttributeGroups, "CustomAttributeGroups", new ParseNode[0], line, column, position));
				children.Add(new ParseNode(MemberAttributes, "MemberAttributes", attrs.ToArray(), line, column, position));
			}

			children.Add(ParsePartial(context));
			
			var sym = context.SymbolId;
			if (classKeyword == sym)
				children.Add(new ParseNode(context.SymbolId, "classKeyword", context.Value, context.Line, context.Column, context.Position));
			if (structKeyword == sym)
				children.Add(new ParseNode(context.SymbolId, "structKeyword", context.Value, context.Line, context.Column, context.Position));
			if (enumKeyword == sym)
				children.Add(new ParseNode(context.SymbolId, "enumKeyword", context.Value, context.Line, context.Column, context.Position));
			if (interfaceKeyword == sym)
				children.Add(new ParseNode(context.SymbolId, "interfaceKeyword", context.Value, context.Line, context.Column, context.Position));
			context.Advance();
			// collapse Identifier
			children.AddRange(ExpressionParser.ParseIdentifier(context).Children);
			children.Add(ParseTypeParams(context));
			// collapse TypeDeclPart
			children.AddRange(_ParseTypeDeclPart(context).Children);
			if (classKeyword == sym)
				return new ParseNode(Class, "Class", children.ToArray(), line, column, position);
			if (structKeyword == sym)
				return new ParseNode(Struct, "Struct", children.ToArray(), line, column, position);
			if (enumKeyword == sym)
				return new ParseNode(Enum, "Enum", children.ToArray(), line, column, position);
			// interface
			// validate it first
			var pn = children[5]; // Members
			for (var i = 0; i < pn.Children.Length; ++i)
			{
				if (Class == pn.SymbolId || Struct == pn.SymbolId || Enum == pn.SymbolId || Interface == pn.SymbolId)
					throw new SyntaxException(string.Format("Interfaces cannot have nested types at line {0}, column {1}, position {2}", pn.Line, pn.Column, pn.Position), pn.Line, pn.Column, pn.Position);
				var ma = pn.Children[i].Children[0].Children;
				if (0 < ma.Length && (1 != ma.Length || ma[0].SymbolId != newKeyword))
				{
					var ppn = ma[0];
					throw new SyntaxException(string.Format("Interfaces members cannot have access, scope or vtbl modifiers except for \"new\" at line {0}, column {1}, position {2}", ppn.Line, ppn.Column, ppn.Position), ppn.Line, ppn.Column, ppn.Position);
				}
			}
			return new ParseNode(Interface, "Interface", children.ToArray(), line, column, position);
		}
		static ParseNode _ParsePrivateImplementationType(ParserContext context)
		{
			var l = context.Line;
			var c = context.Column;
			var p = context.Position;
			var pc2 = context.GetLookAhead(true);
			// here's a trick. We're going to capture a subset of tokens and then
			// create a new ParserContext, feeding it our subset.
			var toks = new List<Token>();
			while (EosSymbol != pc2.SymbolId &&
				(lparen != pc2.SymbolId) && lbrace != pc2.SymbolId && lbracket!=pc2.SymbolId)
			{
				toks.Add(pc2.Current);
				pc2.Advance();
			}
			if (EosSymbol == pc2.SymbolId)
				pc2.Error("Unexpected end of file parsing private implementation type");
			if (2 < toks.Count)
			{
				// remove the last two tokens
				toks.RemoveAt(toks.Count - 1);
				toks.RemoveAt(toks.Count - 1);
				// now manufacture a comma token 
				// to get ParseType to terminate
				var t = default(Token);
				t.SymbolId = comma;
				t.Value = ",";
				t.Line = pc2.Line;
				t.Column = pc2.Column;
				t.Position = pc2.Position;
				toks.Add(t);

				var pc3 = new ParserContext(toks);
				pc3.EnsureStarted();
				var type = ExpressionParser.ParseType(pc3);
				// advance an extra position to clear the trailing ".", which we discard
				var adv = 0;
				while (adv < toks.Count)
				{
					context.Advance();
					++adv;
				}
				return new ParseNode(PrivateImplementationType, "PrivateImplementationType", new ParseNode[] { type }, l, c, p);
			}
			return new ParseNode(PrivateImplementationType, "PrivateImplementationType", new ParseNode[0], l, c, p);
		}

		static ParseNode _ParsePropertyAccessors(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			ParseNode propGet = null;
			ParseNode propSet = null;
			while (getKeyword == context.SymbolId || setKeyword == context.SymbolId)
			{
				if (getKeyword == context.SymbolId)
				{
					if (null != propGet)
						context.Error("Only one get accessor may be specified.");
					propGet = ParsePropertyGet(context);
				}
				else if (setKeyword == context.SymbolId)
				{
					if (null != propSet)
						context.Error("Only one set accessor may be specified.");
					propSet = ParsePropertySet(context);
				}
				else
					context.Error("Expecting a get or set accessor");
			}
			if (null == propSet && null == propGet)
				context.Error("Expecting a get or set accessor but none was specified");
			if (ExpressionParser.rbrace != context.SymbolId)
				context.Error("Expecting a get or set accesor");
			if (null == propSet)
				return new ParseNode(PropertyAccessors, "PropertyAccessors", new ParseNode[] { propGet }, line, column, position);
			else if (null == propGet)
				return new ParseNode(PropertyAccessors, "PropertyAccessors", new ParseNode[] { propSet }, line, column, position);
			return new ParseNode(PropertyAccessors, "PropertyAccessors", new ParseNode[] { propGet, propSet }, line, column, position);
		}

	}
	partial class StatementParser
	{
		static ParseNode _ParseStatement(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var cc = new List<ParseNode>();
			var l = line;
			var c = column;
			var p = position;
			
			for (var i = 0; i < context.Skipped.Count; i++)
			{
				var t = context.Skipped[i];
				if (StatementParser.lineComment == t.SymbolId)
				{
					cc.Add(new ParseNode(t.SymbolId, "lineComment", t.Value, t.Line, t.Column, t.Position));
				}
				else if (StatementParser.blockComment == t.SymbolId)
					cc.Add(new ParseNode(t.SymbolId, "blockComment", t.Value, t.Line, t.Column, t.Position));
				
			}
			if (0 < context.Skipped.Count)
			{
				var tt = context.Skipped[0];
				l = tt.Line;
				c = tt.Column;
				p = tt.Position;
			}
			context.Skipped.Clear();
			var children = new List<ParseNode>();
			children.Add(new ParseNode(Comments, "Comments", cc.ToArray(), l, c, p));
			children.AddRange(ParseInnerStatement(context).Children);
			return new ParseNode(Statement, "Statement", children.ToArray(), line, column, position);
		}
		static ParseNode _ParseIfStatement(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var children = new List<ParseNode>();
			children.AddRange(ParseIfStatementPart(context).Children);
			if(elseKeyword==context.SymbolId)
				children.AddRange(ParseElsePart(context).Children);
			return new ParseNode(IfStatement, "IfStatement", children.ToArray(), line, column, position);

		}
		static bool _WhereExpressionStatement(ParserContext context)
		{
			context = context.GetLookAhead(true);
			try
			{
				ParseVariableDeclarationStatement(context);
				return false;
			}
			catch (SyntaxException)
			{
				return true;
			}
		}
		static ParseNode _ParseForStatement(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var children = new List<ParseNode>();
			if (forKeyword != context.SymbolId)
				context.Error("Expecting for");
			children.Add(new ParseNode(forKeyword, "forKeyword", "for", context.Line, context.Column, context.Position));
			context.Advance();
			if (lparen != context.SymbolId)
				context.Error("Expecting ( in for loop");
			children.Add(new ParseNode(lparen, "lparen", "(", context.Line, context.Column, context.Position));
			context.Advance();
			if (semi == context.SymbolId)
			{
				children.AddRange(ParseEmptyStatement(context).Children);
			}
			else
				children.AddRange(ParseLocalAssignStatement(context).Children);
			if (semi == context.SymbolId)
			{
				children.AddRange(ParseEmptyStatement(context).Children);
			}
			else
			{
				children.Add(ParseExpressionStatement(context));
			}
			children.AddRange(ParseForIncPart(context).Children);
			children.AddRange(ParseStatementOrBlock(context).Children);
			return new ParseNode(ForStatement, "ForStatement", children.ToArray(), line, column, position);
		}
	}
	partial class SlangParser
	{
		static ParseNode _ParseCompileUnit(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var children = new List<ParseNode>();
			// root unnamed namespace
			while (usingKeyword == context.SymbolId)
			{
				children.Add(ParseUsingDirective(context));
			}
			var l = context.Line;
			var c = context.Column;
			var p = context.Position;
			var asmAttrs = new List<ParseNode>();
			var attrs = new List<ParseNode>();
			if(TypeDeclParser.lbracket==context.SymbolId)
			{
				var ca = TypeDeclParser.ParseCustomAttributeGroups(context);
				attrs.AddRange(ca.Children);
			}
			for(int ic=attrs.Count,i = 0;i<ic;++i)
			{
				var n = attrs[i].Children[1];
				if (0 == n.Children.Length)
					break;
				n = n.Children[0];
				if (TypeDeclParser.assemblyKeyword != n.SymbolId)
					break;
				asmAttrs.Add(attrs[i]);
				attrs.RemoveAt(i);
				--i;
				--ic;
			}
			children.Add(new ParseNode(TypeDeclParser.CustomAttributeGroups, "CustomAttributeGroups", asmAttrs.ToArray(), l, c, p));

			while (EosSymbol != context.SymbolId)
			{
				if (namespaceKeyword == context.SymbolId)
				{
					if (0 != attrs.Count) {
						var a = attrs[0];
						throw new SyntaxException("Invalid attribute target in compile unit", a.Line, a.Column, a.Position);
					}
					children.Add(_ParseNamespace(context));
				}
				else {
					var ll = context.Line;
					var cc = context.Column;
					var pp = context.Position;
					if (0 != attrs.Count)
					{
						var a = attrs[0];
						ll = a.Line;
						cc = a.Column;
						pp = a.Position;
					}
					ParseNode ca=new ParseNode(TypeDeclParser.CustomAttributeGroups,"CustomAttributeGroups",attrs.ToArray(),ll,cc,pp);
					children.Add(TypeDeclParser.ParseTypeDecl(context, false, null,ca,context.Line,context.Column,context.Position,null));
				}
			}

			return new ParseNode(CompileUnit, "CompileUnit", children.ToArray(), line, column, position);
		}

		static ParseNode _ParseNamespace(ParserContext context)
		{
			var line = context.Line;
			var column = context.Column;
			var position = context.Position;
			var children = new List<ParseNode>();
			if (namespaceKeyword != context.SymbolId)
				context.Error("Expecting namespace");
			children.Add(new ParseNode(namespaceKeyword, "namespaceKeyword", context.Value, context.Column, context.Line, context.Position));
			context.Advance();
			children.Add(ParseNamespaceName(context));
			if (lbrace != context.SymbolId)
				context.Error("Expecting { in namespace declaration");
			context.Advance();
			while (usingKeyword == context.SymbolId)
			{
				children.Add(ParseUsingDirective(context));
			}
			while (EosSymbol != context.SymbolId && ExpressionParser.rbrace != context.SymbolId)
			{
				children.Add(TypeDeclParser.ParseTypeDecl(context));
			}
			if (rbrace != context.SymbolId)
				context.Error("Expecting } in namespace declaration");
			context.Advance();

			return new ParseNode(Namespace, "Namespace", children.ToArray(), line, column, position);
		}
	}
}
