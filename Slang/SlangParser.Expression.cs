using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Slang
{
	using ST = SlangTokenizer;
	partial class SlangParser
	{
		public static CodeExpression ParseExpression(string text)
		{
			var tokenizer = new SlangTokenizer(text);
			return ParseExpression(tokenizer);
		}
		public static CodeExpression ReadExpressionFrom(Stream stream)
		{
			var tokenizer = new SlangTokenizer(stream);
			return ParseExpression(tokenizer);
		}
		public static CodeExpression ParseExpression(string text, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(text);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseExpression(pc);
		}
		public static CodeExpression ReadExpressionFrom(Stream stream, int line, int column, long position)
		{
			var tokenizer = new SlangTokenizer(stream);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseExpression(pc);
		}
		internal static CodeExpression ParseExpression(IEnumerable<Token> tokenizer)
		{
			var pc = new _PC(tokenizer);
			pc.EnsureStarted();
			return _ParseExpression(pc);
		}
		static CodeExpression _ParseExpression(_PC pc)
		{
			return _ParseAssignExpression(pc);
		}
		static CodeExpression _ParseAssignExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var unresolved = false;
			var lhs = _ParseOrExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.eq:
					op = CodeBinaryOperatorType.Assign;
					pc.Advance();
					return new CodeBinaryOperatorExpression(lhs, op, _ParseOrExpression(pc)).Mark(l, c, p);
				case ST.addAssign:
					unresolved = true; // might be attach event
					op = CodeBinaryOperatorType.Add;
					pc.Advance();
					break;
				case ST.subAssign:
					unresolved = true; // might be detach event
					op = CodeBinaryOperatorType.Subtract;
					pc.Advance();
					break;
				case ST.mulAssign:
					op = CodeBinaryOperatorType.Multiply;
					pc.Advance();
					break;
				case ST.divAssign:
					op = CodeBinaryOperatorType.Divide;
					pc.Advance();
					break;
				case ST.modAssign:
					op = CodeBinaryOperatorType.Modulus;
					pc.Advance();
					break;
				case ST.bitwiseAndAssign:
					op = CodeBinaryOperatorType.BitwiseAnd;
					pc.Advance();
					break;
				case ST.bitwiseOrAssign:
					op = CodeBinaryOperatorType.BitwiseOr;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs,CodeBinaryOperatorType.Assign,new CodeBinaryOperatorExpression(lhs, op, _ParseOrExpression(pc)).Mark(l, c, p)).Mark(l,c,p,unresolved);
		}
		static CodeExpression _ParseBitwiseOrExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseBitwiseAndExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.bitwiseOr:
					op = CodeBinaryOperatorType.BitwiseOr;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseBitwiseAndExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseAndExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseBitwiseOrExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.and:
					op = CodeBinaryOperatorType.BooleanAnd;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseBitwiseOrExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseOrExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseAndExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.or:
					op = CodeBinaryOperatorType.BooleanOr;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseAndExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseBitwiseAndExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseEqualityExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.bitwiseAnd:
					op = CodeBinaryOperatorType.BitwiseAnd;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseEqualityExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseEqualityExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseRelationalExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.eqEq:
					op = CodeBinaryOperatorType.IdentityEquality;
					pc.Advance();
					break;
				case ST.notEq:
					op = CodeBinaryOperatorType.IdentityInequality;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseRelationalExpression(pc)).Mark(l, c, p,true);
		}
		static CodeExpression _ParseRelationalExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseTermExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.lt:
					op = CodeBinaryOperatorType.LessThan;
					pc.Advance();
					break;
				case ST.lte:
					op = CodeBinaryOperatorType.LessThanOrEqual;
					pc.Advance();
					break;
				case ST.gt:
					op = CodeBinaryOperatorType.GreaterThan;
					pc.Advance();
					break;
				case ST.gte:
					op = CodeBinaryOperatorType.GreaterThanOrEqual;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseTermExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseTermExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseFactorExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch (pc.SymbolId)
			{
				case ST.add:
					op = CodeBinaryOperatorType.Add;
					pc.Advance();
					break;
				case ST.sub:
					op = CodeBinaryOperatorType.Subtract;
					pc.Advance();
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseFactorExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseFactorExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var lhs = _ParseUnaryExpression(pc);
			var op = default(CodeBinaryOperatorType);
			switch(pc.SymbolId)
			{
				case ST.mul:
					pc.Advance();
					op = CodeBinaryOperatorType.Multiply;
					break;
				case ST.div:
					pc.Advance();
					op = CodeBinaryOperatorType.Divide;
					break;
				case ST.mod:
					pc.Advance();
					op = CodeBinaryOperatorType.Modulus;
					break;
				default:
					return lhs;
			}
			return new CodeBinaryOperatorExpression(lhs, op, _ParseUnaryExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseUnaryExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var sid = pc.SymbolId;
			if(ST.lparen==sid)
			{
				var pc2 = pc.GetLookAhead(true);
				try
				{
					_ParseCastExpression(pc2);
					return _ParseCastExpression(pc);
				}
				catch (SlangSyntaxException)
				{
					return _ParsePrimaryExpression(pc);
				}
			}
			switch(pc.SymbolId)
			{
				case ST.add:
					pc.Advance();
					return _ParseUnaryExpression(pc);
				case ST.sub:
					pc.Advance();
					var rhs = _ParseUnaryExpression(pc);
					var pp = rhs as CodePrimitiveExpression;
					if (null != pp)
					{
						if (pp.Value is int)
							return new CodePrimitiveExpression(-(int)pp.Value).SetLoc(l,c,p);
						if (pp.Value is long)
							return new CodePrimitiveExpression(-(long)pp.Value).SetLoc(l,c,p);
						if (pp.Value is float)
							return new CodePrimitiveExpression(-(float)pp.Value).SetLoc(l,c,p);
						if (pp.Value is double)
							return new CodePrimitiveExpression(-(double)pp.Value).SetLoc(l,c,p);
						if (pp.Value is char)
							return new CodePrimitiveExpression(-(char)pp.Value).SetLoc(l, c, p);

					}
					return new CodeBinaryOperatorExpression(new CodePrimitiveExpression(0).SetLoc(l,c,p), CodeBinaryOperatorType.Subtract,
						rhs).Mark(l,c,p);
				case ST.not:
					pc.Advance();
					return new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false).SetLoc(l, c, p), CodeBinaryOperatorType.ValueEquality, _ParseUnaryExpression(pc)).Mark(l, c, p);
				case ST.inc:
					pc.Advance();
					var expr = _ParseUnaryExpression(pc);
					return new CodeBinaryOperatorExpression(expr, CodeBinaryOperatorType.Assign, new CodeBinaryOperatorExpression(expr, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1).Mark(l, c, p)).Mark(l, c, p)).Mark(l,c,p);
				case ST.dec:
					pc.Advance();
					expr = _ParseUnaryExpression(pc);
					return new CodeBinaryOperatorExpression(expr, CodeBinaryOperatorType.Assign, new CodeBinaryOperatorExpression(expr, CodeBinaryOperatorType.Subtract, new CodePrimitiveExpression(1).Mark(l, c, p)).Mark(l, c, p)).Mark(l, c, p);
			}
			return _ParsePrimaryExpression(pc);
		}
		static CodeExpression _ParsePrimaryExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			CodeExpression result=null;
			switch(pc.SymbolId)
			{
				case ST.verbatimStringLiteral:
					result = _ParseVerbatimString(pc);
					break;
				case ST.stringLiteral:
					result = _ParseString(pc);
					break;
				case ST.characterLiteral:
					result = _ParseChar(pc);
					break;
				case ST.integerLiteral:
					result = _ParseInteger(pc);
					break;
				case ST.floatLiteral:
					result = _ParseFloat(pc);
					break;
				case ST.boolLiteral:
					result = new CodePrimitiveExpression("true" == pc.Value).SetLoc(l, c, p);
					pc.Advance();
					break;
				case ST.nullLiteral:
					pc.Advance();
					// return here - member refs aren't allowed on null
					return new CodePrimitiveExpression(null).SetLoc(l,c,p);
				case ST.identifier:
				case ST.verbatimIdentifier:
					result = new CodeVariableReferenceExpression(_ParseIdentifier(pc)).Mark(l,c,p,true);
					break;
				case ST.typeOf:
					result = _ParseTypeOf(pc);
					break;
				case ST.defaultOf:
					result = _ParseDefault(pc);
					break;
				case ST.newKeyword:
					result = _ParseNew(pc);
					break;
				case ST.thisRef:
					result = new CodeThisReferenceExpression().SetLoc(l, c, p);
					pc.Advance();
					break;
				case ST.baseRef:
					result = new CodeBaseReferenceExpression().SetLoc(l, c, p);
					pc.Advance();
					break;
				case ST.lparen: // this is a subexpression. unary handles casts
					pc.Advance();
					result=_ParseExpression(pc);
					if (ST.rparen != pc.SymbolId)
						pc.Error("Unterminated ( in subexpression",l,c,p);
					pc.Advance();
					break;
				case ST.objectType:
				case ST.boolType:
				case ST.stringType:
				case ST.charType:
				case ST.byteType:
				case ST.sbyteType:
				case ST.shortType:
				case ST.ushortType:
				case ST.intType:
				case ST.uintType:
				case ST.longType:
				case ST.ulongType:
				case ST.floatType:
				case ST.doubleType:
				case ST.decimalType:
					result = new CodeTypeReferenceExpression(_ParseType(pc)).Mark(l, c, p);
					break;
				default:
					result = _ParseTypeOrFieldRef(pc);
					break;
			}
			var done = false;
			while(!done && !pc.IsEnded)
			{
				l = pc.Line;
				c = pc.Column;
				p = pc.Position;
				switch(pc.SymbolId)
				{
					case ST.lparen:
						pc.Advance();
						var di = new CodeDelegateInvokeExpression(result).Mark(l, c, p, true);
						di.Parameters.AddRange(_ParseMethodArgList(pc));
						if (ST.rparen != pc.SymbolId)
							throw new SlangSyntaxException("Unterminated method or delegate invoke expression", l, c, p);
						pc.Advance();
						result = di;
						break;
					case ST.lbracket:
						pc.Advance();
						var idxr= new CodeIndexerExpression(result).Mark(l,c,p,true);
						idxr.Indices.AddRange(_ParseArgList(pc));
						if (ST.rbracket != pc.SymbolId)
							throw new SlangSyntaxException("Unterminated indexer expression", l, c, p);
						pc.Advance();
						result = idxr;
						break;
					case ST.dot:
						pc.Advance();
						result = new CodeFieldReferenceExpression(result, _ParseIdentifier(pc)).Mark(l, c, p, true);
						break;
					default:
						done = true;
						break;
				}
			}
			return result;
		}
		static CodeExpression _ParseCastExpression(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.lparen != pc.SymbolId)
				pc.Error("Expecting ( in cast expression");
			pc.Advance();
			var ctr = _ParseType(pc);
			if (ST.rparen != pc.SymbolId)
				pc.Error("Expecting ) in cast expression");
			pc.Advance();
			
			return new CodeCastExpression(ctr, _ParseUnaryExpression(pc)).Mark(l, c, p);
		}
		static CodeExpression _ParseNew(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.newKeyword != pc.SymbolId)
				pc.Error("Expecting new");
			pc.Advance();
			var te=_ParseTypeElement(pc);
			if (ST.lparen == pc.SymbolId)
			{
				pc.Advance();
				var oc = new CodeObjectCreateExpression(te).Mark(l, c, p);
				oc.Parameters.AddRange(_ParseArgList(pc));
				if (ST.rparen != pc.SymbolId)
					pc.Error("Expecting ) in new object expression");
				pc.Advance();
				return oc;
			}
			else if (ST.lbracket != pc.SymbolId)
				pc.Error("Expecting [ or ( after type element in new expression");
			var pc2 = pc.GetLookAhead(true);
			pc2.Advance();
			if (ST.comma == pc2.SymbolId)
				throw new SlangSyntaxException("Instantiation of multidimensional arrays is not supported",l,c,p);
			var hasSize = false;
			if (ST.rbracket != pc2.SymbolId)
				hasSize = true;
			pc2 = null;
			CodeExpression size = null;
			if(hasSize)
			{
				pc.Advance();
				size=_ParseExpression(pc);
				if(ST.comma==pc.SymbolId)
					throw new SlangSyntaxException("Instantiation of multidimensional arrays is not supported", l, c, p);
				if (ST.rbracket != pc.SymbolId)
					pc.Error("Expecting ] in new array expression");
				pc.Advance();
			}
			var ctr = new CodeTypeReference(te, 1).Mark(te);
			// if above doesn't work try setting ctr to just te
			if(ST.lbracket==pc.SymbolId)
				ctr= _ParseTypeArraySpec(pc, ctr.ArrayElementType);
			var ace = new CodeArrayCreateExpression(ctr).Mark(l,c,p);
			if (!hasSize)
			{
				if (ST.lbrace != pc.SymbolId)
					pc.Error("Expecting intitializer in new array expression");
				pc.Advance();
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
				{
					ace.Initializers.Add(_ParseExpression(pc));
					if (ST.rbrace == pc.SymbolId)
						break;
					var l2 = pc.Line;
					var c2 = pc.Column;
					var p2 = pc.Position;
					if (ST.comma != pc.SymbolId)
						pc.Error("Expecting , in array initializer expression list");
					pc.Advance();
					if (ST.lbrace == pc.SymbolId)
						throw new SlangSyntaxException("Expecting expression in array initializer expression list", l2, c2, p2);
				}
				if (pc.IsEnded)
					throw new SlangSyntaxException("Unterminated array initializer list", l, c, p);
				pc.Advance();
			}
			else
				ace.SizeExpression = size;
			return ace;
		}
		static CodeExpressionCollection _ParseArgList(_PC pc)
		{
			var result = new CodeExpressionCollection();
			while(!pc.IsEnded && ST.rparen!=pc.SymbolId && ST.rbracket!=pc.SymbolId)
			{
				result.Add(_ParseExpression(pc));
				if (ST.rparen == pc.SymbolId || ST.rbracket==pc.SymbolId)
					break;
				var l2 = pc.Line;
				var c2 = pc.Column;
				var p2 = pc.Position;
				if (ST.comma != pc.SymbolId)
					pc.Error("Expecting , in argument list");
				pc.Advance();
				if (ST.rbracket == pc.SymbolId || ST.rparen==pc.SymbolId)
					throw new SlangSyntaxException("Expecting expression in argument list", l2, c2, p2);
			}
			return result;
		}
		static CodeExpressionCollection _ParseMethodArgList(_PC pc)
		{
			var result = new CodeExpressionCollection();
			while (!pc.IsEnded && ST.rparen != pc.SymbolId && ST.rbracket != pc.SymbolId)
			{
				var fd = FieldDirection.In;
				if(ST.refKeyword==pc.SymbolId)
				{
					fd = FieldDirection.Ref;
					pc.Advance();
				} else if (ST.outKeyword==pc.SymbolId)
				{
					fd = FieldDirection.Out;
					pc.Advance();
				}
				var e = _ParseExpression(pc);
				if (FieldDirection.In != fd)
					e = new CodeDirectionExpression(fd, e);
				result.Add(e);
				if (ST.rparen == pc.SymbolId || ST.rbracket == pc.SymbolId)
					break;
				var l2 = pc.Line;
				var c2 = pc.Column;
				var p2 = pc.Position;
				if (ST.comma != pc.SymbolId)
					pc.Error("Expecting , in method argument list");
				pc.Advance();
				if (ST.rbracket == pc.SymbolId || ST.rparen == pc.SymbolId)
					throw new SlangSyntaxException("Expecting expression in method argument list", l2, c2, p2);
			}
			return result;
		}
		static CodeExpression _ParseTypeOf(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.typeOf != pc.SymbolId)
				pc.Error("Expecting typeof");
			pc.Advance();
			if (ST.lparen != pc.SymbolId)
				pc.Error("Expecting ( in typeof expression");
			pc.Advance();
			var ctr=_ParseType(pc);
			if(ST.rparen!=pc.SymbolId)
				pc.Error("Expecting ) in typeof expression");
			pc.Advance();
			return new CodeTypeOfExpression(ctr).Mark(l, c, p);
		}
		static CodeExpression _ParseDefault(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.defaultOf != pc.SymbolId)
				pc.Error("Expecting default");
			pc.Advance();
			if (ST.lparen != pc.SymbolId)
				pc.Error("Expecting ( in default expression");
			pc.Advance();
			var ctr = _ParseType(pc);
			if (ST.rparen != pc.SymbolId)
				pc.Error("Expecting ) in default expression");
			pc.Advance();
			return new CodeDefaultValueExpression(ctr).Mark(l, c, p);
		}
		static CodeExpression _ParseTypeOrFieldRef(_PC context, bool skipDot = false)
		{
			var l = context.Line;
			var c = context.Column;
			var p = context.Position;
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
			if (context.IsEnded)
				context.Error("Unexpected end of stream while parsing type of field reference");
			var pc2 = context.GetLookAhead(true);
			SlangSyntaxException sx, sx2 = null;
			var fieldAdvCount = 0;
			try
			{


				_ParseIdentifier(pc2);
				// handle the case where this is a namespace.
				if (ST.dot == pc2.SymbolId)
				{
					if (pc2.IsEnded)
						pc2.Error("Unexpected end of stream while parsing type of field reference");

					var pc4 = pc2.GetLookAhead(true);
					while (ST.dot == pc4.SymbolId)
					{
						pc4.Advance();
						_ParseIdentifier(pc4);
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
				if (ST.lt == pc2.SymbolId)
				{
					pc2.Advance();
					var isTypeArg = false;
					try
					{
						_ParseType(pc2);
						if (ST.gt == pc2.SymbolId || ST.comma == pc2.SymbolId)
						{
							isTypeArg = true;
						}
					}
					catch (SlangSyntaxException)
					{

					}
					// use the original context for position info
					if (isTypeArg)
						context.Error("Unexpected < found in found in FielRef");
				}
				// otherwise treat it all as a field ref (variable ref really).
				return new CodeVariableReferenceExpression(_ParseIdentifier(context)).Mark(l, c, p, true);
			}
			catch (SlangSyntaxException ex) { sx = ex; }
			// for error reporting:
			fieldAdvCount = pc2.AdvanceCount;
			if (context.IsEnded)
				context.Error("Unexpected end of stream while parsing type of field reference");

			// parse type ref
			// we have to take this over manually
			var pc3 = context.GetLookAhead(true);
			try
			{
				// this can fail but it will advance the cursor
				_ParseType(pc3);
				// but if it doesn't this is a typeref
				return new CodeTypeReferenceExpression(_ParseType(context)).Mark(line, column, position);
			}
			catch (SlangSyntaxException ex)
			{
				sx2 = ex;
			}
			var advCount = pc3.AdvanceCount;
			// for error reporting:
			var typeAdvCount = advCount;
			// store all the tokens we read
			var toks = new List<Token>();
			if (context.IsEnded)
				context.Error("Unexpected end of stream while parsing type of field reference");

			pc2 = context.GetLookAhead(true);
			while (0 != advCount)
			{

				toks.Add(pc2.Current);
				pc2.Advance();
				--advCount;
			}
			// remove tokens until it parses
			while (1 < toks.Count)
			{
				var throwMemberRef = false;
				toks.RemoveAt(toks.Count - 1);
				var t = default(Token);
				t.SymbolId = ST.rparen;
				toks.Add(t);
				try
				{
					pc3 = new _PC(toks);
					pc3.EnsureStarted();
					_ParseType(pc3);
					// if it gets here we can finish
					pc3 = new _PC(toks);
					pc3.EnsureStarted();
					var pn = _ParseType(pc3);
					var i = toks.Count - 1;
					while (0 != i)
					{
						context.Advance();
						--i;
					}
					return new CodeTypeReferenceExpression(pn).Mark(line, column, position);
				}
				catch (SlangSyntaxException ex)
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

		internal static CodeTypeReference ParseType(IEnumerable<Token> tokenizer)
		{
			var pc = new _PC(tokenizer);
			pc.EnsureStarted();
			return _ParseType(pc);
		}
		static bool _IsIntrinsicType(_PC pc)
		{
			switch (pc.SymbolId)
			{
				case ST.objectType:
				case ST.boolType:
				case ST.stringType:
				case ST.charType:
				case ST.byteType:
				case ST.sbyteType:
				case ST.shortType:
				case ST.ushortType:
				case ST.intType:
				case ST.uintType:
				case ST.longType:
				case ST.ulongType:
				case ST.floatType:
				case ST.doubleType:
				case ST.decimalType:
					return true;
			}
			return false;
		}
		static CodeTypeReference _ParseIntrinsicType(_PC pc)
		{
			switch (pc.SymbolId)
			{
				case ST.objectType:
					pc.Advance();
					return new CodeTypeReference(typeof(object)).SetLoc(pc);
				case ST.boolType:
					pc.Advance();
					return new CodeTypeReference(typeof(bool)).SetLoc(pc);
				case ST.stringType:
					pc.Advance();
					return new CodeTypeReference(typeof(string)).SetLoc(pc);
				case ST.charType:
					pc.Advance();
					return new CodeTypeReference(typeof(char)).SetLoc(pc);
				case ST.byteType:
					pc.Advance();
					return new CodeTypeReference(typeof(byte)).SetLoc(pc);
				case ST.sbyteType:
					pc.Advance();
					return new CodeTypeReference(typeof(sbyte)).SetLoc(pc);
				case ST.shortType:
					pc.Advance();
					return new CodeTypeReference(typeof(short)).SetLoc(pc);
				case ST.ushortType:
					pc.Advance();
					return new CodeTypeReference(typeof(ushort)).SetLoc(pc);
				case ST.intType:
					pc.Advance();
					return new CodeTypeReference(typeof(int)).SetLoc(pc);
				case ST.uintType:
					pc.Advance();
					return new CodeTypeReference(typeof(uint)).SetLoc(pc);
				case ST.longType:
					pc.Advance();
					return new CodeTypeReference(typeof(long)).SetLoc(pc);
				case ST.ulongType:
					pc.Advance();
					return new CodeTypeReference(typeof(ulong)).SetLoc(pc);
				case ST.floatType:
					pc.Advance();
					return new CodeTypeReference(typeof(float)).SetLoc(pc);
				case ST.doubleType:
					pc.Advance();
					return new CodeTypeReference(typeof(double)).SetLoc(pc);
				case ST.decimalType:
					pc.Advance();
					return new CodeTypeReference(typeof(decimal)).SetLoc(pc);
			}
			pc.Error("Expecting intrinsic type");
			return null;
		}
		static void _ParseTypeGenerics(_PC pc, CodeTypeReference bt)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.lt != pc.SymbolId)
				pc.Error("Expecting < in type generic specifier");
			pc.Advance();
			var tgc = 0;
			if (ST.comma != pc.SymbolId && ST.gt != pc.SymbolId) // Foo<Bar,Baz>
			{
				while (!pc.IsEnded && ST.gt != pc.SymbolId)
				{
					bt.TypeArguments.Add(_ParseType(pc));
					if (ST.gt == pc.SymbolId)
						break;
					var l2 = pc.Line;
					var c2 = pc.Column;
					var p2 = pc.Position;
					if (ST.comma != pc.SymbolId)
						pc.Error("Expecting , or > in type generic specifier");
					pc.Advance();
					if (ST.gt == pc.SymbolId)
						throw new SlangSyntaxException("Expecting type or > in type generic specifier", l2, c2, p2);
				}
			}
			else // Foo<,>
			{
				tgc = 1;
				while (ST.comma == pc.SymbolId)
				{
					++tgc;
					pc.Advance();
				}
			}
			if (pc.IsEnded || ST.gt != pc.SymbolId)
				throw new SlangSyntaxException("Unterminated type generic specifier", l, c, p);
			pc.Advance(); // skip >
			if (0 != tgc)
				bt.BaseType += "`" + tgc.ToString();
		}
		static CodeTypeReference _ParseTypeArraySpec(_PC pc, CodeTypeReference et)
		{
			var ranks = new List<int>();
			var ctrs = new List<CodeTypeReference>();
			var result = et;

			if (ST.lbracket != pc.SymbolId)
				pc.Error("Expecting [ in type array specification");
			var rank = 1;
			var inBrace = true;
			while (pc.Advance())
			{
				if (inBrace && ST.comma == pc.SymbolId)
				{

					++rank;
					continue;
				}
				else if (ST.rbracket == pc.SymbolId)
				{

					ranks.Add(rank);
					ctrs.Add(new CodeTypeReference().Mark(pc));
					rank = 1;
					if (!pc.Advance())
						break;
					inBrace = false;
					if (ST.lbracket != pc.SymbolId)
						break;
					else
						inBrace = true;
				}
				else
					break;
			}
			for (var i = ranks.Count - 1; -1 < i; --i)
			{
				var ctr = ctrs[i];
				ctr.ArrayElementType = result;
				ctr.ArrayRank = ranks[i];
				result = ctr;
			}
			return result;
		}
		static CodeTypeReference _ParseType(_PC pc)
		{
			var result = _ParseTypeElement(pc);
			if (ST.lbracket == pc.SymbolId)
				result = _ParseTypeArraySpec(pc, result);
			return result;
		}
		static CodeTypeReference _ParseTypeElement(_PC pc)
		{
			var result = _ParseTypeBase(pc);
			if (ST.lt == pc.SymbolId)
				_ParseTypeGenerics(pc, result);
			return result;
		}
		static CodeTypeReference _ParseTypeBase(_PC pc)
		{
			if (_IsIntrinsicType(pc))
				return _ParseIntrinsicType(pc);
			var result = new CodeTypeReference().SetLoc(pc);
			if (ST.globalKeyword == pc.SymbolId)
			{
				pc.Advance();
				if (ST.colonColon != pc.SymbolId)
					pc.Error("Expecting :: in global type reference");
				pc.Advance();
				result.Options = CodeTypeReferenceOptions.GlobalReference;
			}
			if (ST.verbatimIdentifier != pc.SymbolId && ST.identifier != pc.SymbolId)
				pc.Error("Expecting Identifier");
			result.BaseType = _ParseIdentifier(pc);
			if (ST.dot == pc.SymbolId)
			{
				// might be a nested type
				result.UserData["slang:unresolved"] = true;
				result.BaseType += pc.Value;
				pc.Advance();
				while (ST.verbatimIdentifier == pc.SymbolId || ST.identifier == pc.SymbolId)
				{
					result.BaseType += _ParseIdentifier(pc);
					if (ST.dot != pc.SymbolId)
						break;
					result.BaseType += ".";
					pc.Advance();
				}
			}
			return result;
		}

		static string _ParseIdentifier(_PC pc)
		{
			var s = pc.Value;
			switch (pc.SymbolId)
			{
				case ST.identifier:
					if (Keywords.Contains(s))
						break;
					pc.Advance();
					return s;
				case ST.verbatimIdentifier:
					pc.Advance();
					return s.Substring(1);
			}
			pc.Error("Expecting identifier");
			return null;
		}

		#region Parse Primitives
		static CodePrimitiveExpression _ParseString(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var sb = new StringBuilder();
			var e = pc.Value.GetEnumerator();
			e.MoveNext();
			if (e.MoveNext())
			{
				while (true)
				{
					if ('\"' == e.Current)
					{
						pc.Advance();
						return new CodePrimitiveExpression(sb.ToString()).SetLoc(l, c, p);
					}
					else if ('\\' == e.Current)
						sb.Append(_ParseEscapeChar(e, pc));
					else
					{
						sb.Append(e.Current);
						if (!e.MoveNext())
							break;
					}
				}
			}
			throw new SlangSyntaxException("Unterminated string in input", l,c,p);
		}
		static CodePrimitiveExpression _ParseVerbatimString(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;

			var sb = new StringBuilder();
			var e = pc.Value.GetEnumerator();
			e.MoveNext();
			e.MoveNext();
			if (e.MoveNext())
			{
				while (true)
				{
					if ('\"' == e.Current)
					{

						if (!e.MoveNext() || '\"' != e.Current)
						{
							pc.Advance();
							return new CodePrimitiveExpression(sb.ToString()).SetLoc(l, c, p);
						}
						sb.Append('\"');
						if (!e.MoveNext())
							break;
					}
					else
					{
						sb.Append(e.Current);
						if (!e.MoveNext())
							break;
					}
				}
			}
			throw new SlangSyntaxException("Unterminated string in input", l, c, p);
		}
		static CodePrimitiveExpression _ParseChar(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;

			var s = pc.Value;
			// remove quotes.
			s = s.Substring(1, s.Length - 2);
			var e = s.GetEnumerator();
			e.MoveNext();
			if ('\\' == e.Current)
			{
				s = _ParseEscapeChar(e, pc);
				pc.Advance();
				if (1 == s.Length)
					return new CodePrimitiveExpression(s[0]).SetLoc(l,c,p);
				else
					return new CodePrimitiveExpression(s).SetLoc(l,c,p); // for UTF-32 this has to be a string
			}
			pc.Advance();
			return new CodePrimitiveExpression(s[0]).SetLoc(l,c,p);
		}
		static CodePrimitiveExpression _ParseFloat(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;

			var s = pc.Value;
			var ch = char.ToLowerInvariant(s[s.Length - 1]);
			var isDouble = 'd' == ch;
			var isDecimal = 'm' == ch;
			var isFloat = 'f' == ch;
			if ((isDouble || isDecimal || isFloat))
				s = s.Substring(0, s.Length - 1);
			else
				isDouble = true;
			object n = null;
			if (isFloat)
				n = float.Parse(s);
			else if (isDecimal)
				n = decimal.Parse(s);
			else
				n = double.Parse(s);
			pc.Advance();
			return new CodePrimitiveExpression(n).SetLoc(l,c,p);
		}
		static CodePrimitiveExpression _ParseInteger(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;

			var s = pc.Value;
			var isLong = false;
			var isUnsigned = false;
			var isNeg = '-' == s[0];
			var isHex = s.StartsWith("-0x") || s.StartsWith("0x");
			var ch = char.ToLowerInvariant(s[s.Length - 1]);
			if ('l' == ch)
			{
				isLong = true;
				s = s.Substring(0, s.Length - 1);
			}
			else if ('u' == ch)
			{
				isUnsigned = true;
				s = s.Substring(0, s.Length - 1);
			}
			// do it twice in case we have like, "ul" or "lu" at the end
			// this routine would accept "ll" or "uu" but it doesn't matter
			// because the lexer won't.
			ch = char.ToLowerInvariant(s[s.Length - 1]);
			if ('l' == ch)
			{
				isLong = true;
				s = s.Substring(0, s.Length - 1);
			}
			else if ('u' == ch)
			{
				isUnsigned = true;
				s = s.Substring(0, s.Length - 1);
			}
			// parse this into a double so we can do bounds checking
			if (isHex)
				s = s.Substring(2);
			var d = (double)long.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			object n = null;
			if (isUnsigned && (isLong || (d <= uint.MaxValue && d >= uint.MinValue)))
			{
				if (isNeg)
				{
					if (!isHex)
						n = unchecked((ulong)long.Parse(s));
					else
						n = unchecked((ulong)-long.Parse(s.Substring(1), NumberStyles.AllowHexSpecifier));
				}
				else
					n = ulong.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			}
			else if (isUnsigned)
			{
				if (isNeg)
				{
					if (!isHex)
						n = unchecked((uint)int.Parse(s));
					else
						n = unchecked((uint)-int.Parse(s.Substring(1), NumberStyles.AllowHexSpecifier));
				}
				else
					n = uint.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			}
			else
			{
				if (isNeg)
				{
					if (!isHex)
						n = int.Parse(s);
					else
						n = unchecked(-int.Parse(s.Substring(1), NumberStyles.AllowHexSpecifier));
				}
				else
					n = int.Parse(s, isHex ? NumberStyles.AllowHexSpecifier : NumberStyles.Integer);
			}
			pc.Advance();
			return new CodePrimitiveExpression(n).SetLoc(l,c,p);
		}
		#endregion

		#region String/Char escapes
		static string _ParseEscapeChar(IEnumerator<char> e, _PC pc)
		{
			if (e.MoveNext())
			{
				switch (e.Current)
				{
					case 'r':
						e.MoveNext();
						return "\r";
					case 'n':
						e.MoveNext();
						return "\n";
					case 't':
						e.MoveNext();
						return "\t";
					case 'a':
						e.MoveNext();
						return "\a";
					case 'b':
						e.MoveNext();
						return "\b";
					case 'f':
						e.MoveNext();
						return "\f";
					case 'v':
						e.MoveNext();
						return "\v";
					case '0':
						e.MoveNext();
						return "\0";
					case '\\':
						e.MoveNext();
						return "\\";
					case '\'':
						e.MoveNext();
						return "\'";
					case '\"':
						e.MoveNext();
						return "\"";
					case 'u':
						var acc = 0L;
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						e.MoveNext();
						return unchecked((char)acc).ToString();
					case 'x':
						acc = 0;
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (e.MoveNext() && _IsHexChar(e.Current))
						{
							acc <<= 4;
							acc |= _FromHexChar(e.Current);
							if (e.MoveNext() && _IsHexChar(e.Current))
							{
								acc <<= 4;
								acc |= _FromHexChar(e.Current);
								if (e.MoveNext() && _IsHexChar(e.Current))
								{
									acc <<= 4;
									acc |= _FromHexChar(e.Current);
									e.MoveNext();
								}
							}
						}
						return unchecked((char)acc).ToString();
					case 'U':
						acc = 0;
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						if (!e.MoveNext())
							break;
						if (!_IsHexChar(e.Current))
							break;
						acc <<= 4;
						acc |= _FromHexChar(e.Current);
						e.MoveNext();
						return char.ConvertFromUtf32(unchecked((int)acc));
					default:
						throw new NotSupportedException(string.Format("Unsupported escape sequence \\{0}", e.Current));
				}
			}
			pc.Error("Unterminated escape sequence");
			return null;
		}
		static bool _IsHexChar(char hex)
		{
			return (':' > hex && '/' < hex) ||
				('G' > hex && '@' < hex) ||
				('g' > hex && '`' < hex);
		}
		static byte _FromHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new ArgumentException("The value was not hex.", "hex");
		}
		#endregion

		internal static HashSet<string> Keywords = _BuildKeywords();
		static HashSet<string> _BuildKeywords()
		{
			var result = new HashSet<string>();
			string[] sa = "abstract|as|ascending|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|equals|explicit|extern|event|false|finally|fixed|float|for|foreach|get|global|goto|if|implicit|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield".Split(new char[] { '|' });

			for (var i = 0; i < sa.Length; ++i)
				result.Add(sa[i]);

			return result;
		}
	}
}
