using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Globalization;
using System.Text;
using System.IO;

namespace CD
{
	using ST = SlangTokenizer;
	partial class SlangParser
	{
		/// <summary>
		/// Reads a <see cref="CodeExpression"/> from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
		public static CodeExpression ReadExpressionFrom(TextReader reader)
			=> ParseExpression(TextReaderEnumerable.FromReader(reader));
		/// <summary>
		/// Reads a <see cref="CodeExpression"/> from the specified file
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
		public static CodeExpression ReadExpressionFrom(string filename)
			=> ParseExpression(new FileReaderEnumerable(filename));
		/// <summary>
		/// Reads a <see cref="CodeExpression"/> from the specified URL
		/// </summary>
		/// <param name="url">The URL to read</param>
		/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
		public static CodeExpression ReadExpressionFromUrl(string url)
			=> ParseExpression(new UrlReaderEnumerable(url));
		/// <summary>
		/// Parses a <see cref="CodeExpression"/> from the specified input
		/// </summary>
		/// <param name="input">The input to parse</param>
		/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
		public static CodeExpression ParseExpression(IEnumerable<char> input)
		{
			using (var e = new ST(input).GetEnumerator())
			{
				var pc = new _PC(e);
				pc.EnsureStarted();
				var result = _ParseExpression(pc);
				if (!pc.IsEnded)
					throw new ArgumentException("Unrecognized remainder in expression", "input");
				return result;
			}
		}
		static CodeExpression _ParseExpression(_PC pc)
		{
			return _ParseAssignment(pc);
		}
		
		static CodeExpression _ParseMemberRef(_PC pc)
		{
			var lhs = _ParseUnary(pc);
			_SkipComments(pc);
			while(true)
			{
				switch(pc.SymbolId)
				{
					case ST.dot:
						if (!pc.Advance())
							throw new ArgumentException("Unterminated member reference", "input");
						if (ST.identifier != pc.SymbolId)
							throw new ArgumentException(string.Format("Invalid token {0} found in member reference", pc.Value), "input");
						var fr = new CodeFieldReferenceExpression(lhs, pc.Value);
						fr.UserData.Add("slang:unresolved", true);
						lhs = fr;
						pc.Advance();
						break;
					case ST.lbracket:
						var exprs = _ParseArguments(pc, ST.rbracket, false);
						var ie = new CodeIndexerExpression(lhs);
						ie.Indices.AddRange(exprs);
						ie.UserData.Add("slang:unresolved", true);
						lhs = ie;
						break;
					case ST.lparen:
						exprs = _ParseArguments(pc, ST.rparen, true);
						var di = new CodeDelegateInvokeExpression(lhs);
						di.Parameters.AddRange(exprs);
						di.UserData.Add("slang:unresolved", true);
						lhs = di;
						break;
					default:
						return lhs;

				}
			}
		}
		static CodeExpression _ParseFactor(_PC pc)
		{
			var lhs = _ParseMemberRef(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{
					case ST.mul:
						op = CodeBinaryOperatorType.Multiply;
						break;
					case ST.div:
						op = CodeBinaryOperatorType.Divide;
						break;
					case ST.mod:
						op = CodeBinaryOperatorType.Modulus;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseMemberRef(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpression _ParseTerm(_PC pc)
		{
			var lhs = _ParseFactor(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{
					case ST.add:
						op = CodeBinaryOperatorType.Add;
						break;
					case ST.sub:
						op = CodeBinaryOperatorType.Subtract;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseFactor(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpression _ParseAssignment(_PC pc)
		{
			var lhs = _ParseBooleanOr(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				var assign = false;
				_SkipComments(pc);
				switch (pc.SymbolId)
				{
					case ST.eq:
						op = CodeBinaryOperatorType.Assign;
						break;
					case ST.addAssign:
						assign = true;
						op = CodeBinaryOperatorType.Add;
						break;
					case ST.subAssign:
						assign = true;
						op = CodeBinaryOperatorType.Subtract;
						break;
					case ST.mulAssign:
						assign = true;
						op = CodeBinaryOperatorType.Multiply;
						break;
					case ST.modAssign:
						assign = true;
						op = CodeBinaryOperatorType.Modulus;
						break;
					case ST.divAssign:
						assign = true;
						op = CodeBinaryOperatorType.Divide;
						break;
					case ST.bitwiseOrAssign:
						assign = true;
						op = CodeBinaryOperatorType.BitwiseOr;
						break;
					case ST.bitwiseAndAssign:
						assign = true;
						op = CodeBinaryOperatorType.BitwiseAnd;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseBooleanOr(pc);
				if(assign)
				{
					lhs = new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.Assign,
						new CodeBinaryOperatorExpression(lhs, op, rhs));
				} else
					lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpression _ParseBooleanOr(_PC pc)
		{
			var lhs = _ParseBooleanAnd(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{

					case ST.or:
						op = CodeBinaryOperatorType.BooleanOr;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseBooleanAnd(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpression _ParseBooleanAnd(_PC pc)
		{
			var lhs = _ParseBitwiseOr(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{

					case ST.and:
						op = CodeBinaryOperatorType.BooleanAnd;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseBitwiseOr(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpression _ParseBitwiseOr(_PC pc)
		{
			var lhs = _ParseBitwiseAnd(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{

					case ST.bitwiseOr:
						op = CodeBinaryOperatorType.BitwiseOr;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseBitwiseAnd(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpression _ParseBitwiseAnd(_PC pc)
		{
			var lhs = _ParseEquality(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{
					
					case ST.bitwiseAnd:
						op = CodeBinaryOperatorType.BitwiseAnd;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseEquality(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpression _ParseEquality(_PC pc)
		{
			var lhs = _ParseRelational(pc);
			while (true)
			{
				var op = true;
				_SkipComments(pc);
				switch (pc.SymbolId)
				{
					case ST.eqEq:
						op = true;
						break;
					case ST.notEq:
						op = false;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseRelational(pc);
				
				lhs = new CodeBinaryOperatorExpression(lhs, CodeBinaryOperatorType.ValueEquality, rhs);
				if (!op) // have to hack below because CodeDOM is inexplicably missing value inequality
					lhs = new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false), CodeBinaryOperatorType.ValueEquality, lhs);
			}
		}
		static CodeExpression _ParseRelational(_PC pc)
		{
			var lhs = _ParseTerm(pc);
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{
					case ST.lt:
						op = CodeBinaryOperatorType.LessThan;
						break;
					case ST.gt:
						op = CodeBinaryOperatorType.GreaterThan;
						break;
					case ST.lte:
						op = CodeBinaryOperatorType.LessThanOrEqual;
						break;
					case ST.gte:
						op = CodeBinaryOperatorType.GreaterThanOrEqual;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseTerm(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeCastExpression _ParseCast(_PC pc)
		{
			// expects on (
			pc.Advance();
			_SkipComments(pc);
			if(pc.IsEnded)
				throw new ArgumentException("Unterminated cast or subexpression.");
			var ctr = _ParseTypeRef(pc);
			_SkipComments(pc);
			if(ST.rparen!=pc.SymbolId)
				throw new ArgumentException("Unterminated cast or subexpression.");
			pc.Advance();
			var expr=_ParseExpression(pc);
			return new CodeCastExpression(ctr, expr);
		}
		static CodeExpression _ParseUnary(_PC pc)
		{
			_SkipComments(pc);
			switch (pc.SymbolId)
			{
				case ST.inc:
					pc.Advance();
					var rhs = _ParseLeaf(pc);
					return new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Assign,
						new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Add, new CodePrimitiveExpression(1)));
				case ST.dec:
					pc.Advance();
					rhs = _ParseLeaf(pc);
					return new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Assign,
						new CodeBinaryOperatorExpression(rhs, CodeBinaryOperatorType.Subtract, new CodePrimitiveExpression(1)));

				case ST.add:
					pc.Advance();
					return _ParseUnary(pc);
				case ST.sub:
					pc.Advance();
					rhs = _ParseUnary(pc);
					// simulate the negation unary operator in the codedom
					var pe = rhs as CodePrimitiveExpression;
					if(null!=pe)
					{
						if (pe.Value is int)
							return new CodePrimitiveExpression(-(int)pe.Value);
						if (pe.Value is long)
							return new CodePrimitiveExpression(-(long)pe.Value);
						if (pe.Value is short)
							return new CodePrimitiveExpression(-(short)pe.Value);
						if (pe.Value is sbyte)
							return new CodePrimitiveExpression(-(sbyte)pe.Value);
						if (pe.Value is float)
							return new CodePrimitiveExpression(-(float)pe.Value);
						if (pe.Value is double)
							return new CodePrimitiveExpression(-(double)pe.Value);
						if (pe.Value is decimal)
							return new CodePrimitiveExpression(-(decimal)pe.Value);
					}
					return new CodeBinaryOperatorExpression(new CodePrimitiveExpression(0), CodeBinaryOperatorType.Subtract, rhs);
				case ST.not:
					pc.Advance();
					rhs = _ParseExpression(pc);
					return new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false), CodeBinaryOperatorType.ValueEquality, rhs);
				case ST.lparen:
					// possibly a cast, or possibly a subexpression
					// we can't know for sure so this gets *really* complicated
					// basically we need to backtrack.
					CodeExpression expr = null;
					var pc2 = pc.GetLookAhead();
					pc2.EnsureStarted();
					try
					{
						expr = _ParseCast(pc2);
					}
					catch { }
					if(null!=expr)
					{
						// now advance our actual pc
						// TODO: see if we can't add a dump feature
						// to the lookahead so we don't have to 
						// parse again. Minor, but sloppy.
						return _ParseCast(pc);

					} else
					{
						try
						{
							if (!pc.Advance())
								throw new ArgumentException("Unterminated cast or subexpression", "input");
							expr=_ParseExpression(pc);
							_SkipComments(pc);
							if(ST.rparen!=pc.SymbolId)
								throw new ArgumentException("Invalid cast or subexpression", "input");
							pc.Advance();
							return expr;
						}
						catch(Exception eex)
						{
							throw eex;
						}
					}
			}

			// No positive/negative operator so parse a leaf node
			return _ParseLeaf(pc);
		}
		static CodeExpression _ParseLeaf(_PC pc)
		{
			CodeExpression e = null;
			_SkipComments(pc);
			switch (pc.SymbolId)
			{
				case ST.integerLiteral:
					e = _ParseInteger(pc);
					break;
				case ST.floatLiteral:
					e = _ParseFloat(pc);
					break;
				case ST.stringLiteral:
					e = _ParseString(pc);
					break;
				case ST.characterLiteral:
					e = _ParseChar(pc);
					break;
				case ST.keyword:
					switch (pc.Value)
					{
						case "true":
							e = new CodePrimitiveExpression(true);
							pc.Advance();
							break;
						case "false":
							e = new CodePrimitiveExpression(false);
							pc.Advance();
							break;
						case "null":
							e = new CodePrimitiveExpression(null);
							pc.Advance();
							break;
						case "this":
							e = new CodeThisReferenceExpression();
							pc.Advance();
							break;
						case "base":
							e = new CodeBaseReferenceExpression();
							pc.Advance();
							break;
						case "typeof":
							e = _ParseTypeOf(pc);
							break;
						case "default":
							e = _ParseDefault(pc);
							break;
						case "new":
							if (!pc.Advance())
								throw new ArgumentException("Unterminated new expression.", "input");
							var ctr=_ParseTypeRef(pc,true);
							_SkipComments(pc);
							if(pc.IsEnded)
								throw new ArgumentException("Unterminated new expression.", "input");
							switch(pc.SymbolId)
							{
								case ST.lparen:
									var exprs=_ParseArguments(pc, ST.rparen, false);
									var ce = new CodeObjectCreateExpression(ctr);
									ce.Parameters.AddRange(exprs);
									e = ce;
									// might be a delegate create expression
									e.UserData.Add("slang:unresolved", true);
									break;
								case ST.lbracket:
									// this can be either [,...,]...[] {} or [X,..,Z][]
									// we have to be careful because depending on what the square
									// brackets contain we may or may not accept an initializer here.
									e=_ParseArrayCreatePart(ctr, pc);
									break;
								default:
									throw new ArgumentException(string.Format("Unrecognized token {0} in new expression", pc.Value), "input");
							}

							break;
						case "bool":
						case "char":
						case "string":
						case "byte":
						case "sbyte":
						case "short":
						case "ushort":
						case "int":
						case "uint":
						case "long":
						case "ulong":
						case "float":
						case "double":
						case "decimal":
							e = new CodeTypeReferenceExpression(_TranslateIntrinsicType(pc.Value));
							pc.Advance();
							break;
						default:
							throw new ArgumentException(string.Format("Unexpected keyword {0} found in expression", pc.Value), "input");
					}
					break;
				case ST.identifier:
					e = new CodeVariableReferenceExpression(pc.Value);
					e.UserData.Add("slang:unresolved", true);
					pc.Advance();
					_SkipComments(pc);
					// see if this is a typeref since we're top level
					break;
			}
			if (null == e)
				throw new ArgumentException(string.Format("Unexpected token {0} in input", pc.Value), "input");
			return e;
		}
		static CodeArrayCreateExpression _ParseArrayCreatePart(CodeTypeReference type, _PC pc)
		{
			// expects started on [
			var result = new CodeArrayCreateExpression();
			var mods = new List<int>();
			var arrType = type;
			_SkipComments(pc);
			if (!pc.Advance())
				throw new ArgumentException("Unterminated array create expression", "input");
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated array create expression", "input");
			var done = false;
			CodeExpression expr = null;
			// this is structured as though multidim arrays are supported so if we ever make a hack to make it work
			// then we don't have to change much of this code. The loop will be necessary even though right now
			// it will just cause throws with an unsupported messages
			while (!done)
			{
				switch(pc.SymbolId)
				{
					case ST.comma:
						throw new NotSupportedException("Multidimensional arrays are not fully supported. Consider using nested arrays in the alternative.");
						// we'd increment the array rank here
					case ST.rbracket:
						pc.Advance();
						_SkipComments(pc);
						done = true;
						break;
					default: // is a size expression
						if (null != expr) // CodeDOM limitation here
							throw new NotSupportedException("Multidimensional arrays are not fully supported. Consider using nested arrays in the alternative.");
						expr = _ParseExpression(pc);
						_SkipComments(pc);
						break;
				}
			}
			
			var ctr = new CodeTypeReference();
			ctr.ArrayElementType = type;
			ctr.ArrayRank = 1; // see notes about multidim above
			if(ST.lbracket==pc.SymbolId)
			{
				ctr=_ParseArrayTypeModifiers(ctr, pc);
				if(1<ctr.ArrayRank)
					// tried to support this but CodeDOM says no. I guess we could "try" because the AST data (arrayRank) is there for it
					// but i think it's better to fail rather than lull the user into a false sense of security in terms of silently allowing
					// something that the CodeDOM will not support. We can parse these, we just can't do anything meaningful with them.
					throw new NotSupportedException("Multidimensional arrays are not fully supported. Consider using nested arrays.");
			}
			result.CreateType = ctr;
			result.SizeExpression = expr;
			if(null==expr) // expect an initializer here
			{
				if (pc.IsEnded)
					throw new ArgumentException("Expecting an array initializer in the array create expression", "input");
				if(ST.lbrace==pc.SymbolId)
				{
					if (!pc.Advance())
						throw new ArgumentException("Unterminated array create initializer", "input");
					while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
					{
						_SkipComments(pc);
						result.Initializers.Add(_ParseExpression(pc));
						_SkipComments(pc);
						if(ST.comma==pc.SymbolId)
							pc.Advance();
					}
					if(pc.IsEnded)
						throw new ArgumentException("Unterminated array create initializer", "input");
					pc.Advance();
				}
			}
			return result;
		}
		static CodeExpression _ParseTypeOf(_PC pc)
		{
			CodeExpression e;
			if (!pc.Advance())
				throw new ArgumentException("Unterminated typeof expression", "input");
			_SkipComments(pc);
			if (ST.lparen != pc.SymbolId || !pc.Advance())
				throw new ArgumentException("Unterminated typeof expression", "input");
			_SkipComments(pc);
			var ctr = _ParseTypeRef(pc);
			_SkipComments(pc);
			if (ST.rparen != pc.SymbolId)
				throw new ArgumentException("Unterminated typeof expression", "input");
			pc.Advance();
			e = new CodeTypeOfExpression(ctr);
			return e;
		}
		static CodeExpression _ParseDefault(_PC pc)
		{
			CodeExpression e;
			if (!pc.Advance())
				throw new ArgumentException("Unterminated default() expression", "input");
			_SkipComments(pc);
			if (ST.lparen != pc.SymbolId || !pc.Advance())
				throw new ArgumentException("Unterminated default() expression", "input");
			_SkipComments(pc);
			var ctr = _ParseTypeRef(pc);
			_SkipComments(pc);
			if (ST.rparen != pc.SymbolId)
				throw new ArgumentException("Unterminated default expression", "input");
			pc.Advance();
			e = new CodeDefaultValueExpression(ctr);
			return e;
		}
		static CodeExpression _ParseTerm(CodeExpression lhs,_PC pc)
		{
			while (true)
			{
				var op = default(CodeBinaryOperatorType);
				_SkipComments(pc);
				switch (pc.SymbolId)
				{
					case ST.add:
						op = CodeBinaryOperatorType.Add;
						break;
					case ST.sub:
						op = CodeBinaryOperatorType.Subtract;
						break;
					default:
						return lhs;
				}
				pc.Advance();
				var rhs = _ParseFactor(pc);
				lhs = new CodeBinaryOperatorExpression(lhs, op, rhs);
			}
		}
		static CodeExpressionCollection _ParseArguments(_PC pc, int endSym = ST.rparen, bool allowDirection = true)
		{
			var result = new CodeExpressionCollection();
			if (!pc.Advance())
				throw new ArgumentException("Unterminated argument list", "input");
			while (endSym != pc.SymbolId)
			{
				var fd = default(FieldDirection);
				if (allowDirection && ST.keyword == pc.SymbolId)
				{
					if ("in" == pc.Value)
					{
						fd = FieldDirection.In;
						if (!pc.Advance())
							throw new ArgumentException("Unterminated method invocation.", "input");
					}
					else if ("out" == pc.Value)
					{
						fd = FieldDirection.Out;
						if (!pc.Advance())
							throw new ArgumentException("Unterminated method invocation.", "input");
					}
					else if ("ref" == pc.Value)
					{
						fd = FieldDirection.Ref;
						if (!pc.Advance())
							throw new ArgumentException("Unterminated method invocation.", "input");
					}
					
					_SkipComments(pc);
				}
				var exp = _ParseExpression(pc);
				if (fd != FieldDirection.In)
					exp = new CodeDirectionExpression(fd, exp);
				result.Add(exp);
				_SkipComments(pc);
				if (ST.comma == pc.SymbolId)
				{
					if (!pc.Advance())
						throw new ArgumentException("Unterminated argument list.", "input");
				}
			}
			if (endSym != pc.SymbolId)
			{
				throw new ArgumentException("Unterminated argument list.", "input");
			}
			pc.Advance();
			return result;
		}
		#region Parse Primitives
		static CodePrimitiveExpression _ParseString(_PC pc)
		{
			var sb = new StringBuilder();
			var e = pc.Value.GetEnumerator();
			var more = pc.Advance();

			e.MoveNext();
			if (e.MoveNext())
			{
				while (true)
				{
					if ('\"' == e.Current)
						return new CodePrimitiveExpression(sb.ToString());
					else if ('\\' == e.Current)
						sb.Append(_ParseEscapeChar(e));
					else
					{
						sb.Append(e.Current);
						if (!e.MoveNext())
							break;
					}
				}
			}
			throw new ArgumentException("Unterminated string in input", "input");
		}
		static CodePrimitiveExpression _ParseChar(_PC pc)
		{
			var s = pc.Value;
			pc.Advance();
			// remove quotes.
			s = s.Substring(1, s.Length - 2);
			var e = s.GetEnumerator();
			e.MoveNext();
			if ('\\' == e.Current)
			{
				s = _ParseEscapeChar(e);
				if (1 == s.Length)
					return new CodePrimitiveExpression(s[0]);
				else // for UTF-32 this has to be a string
					return new CodePrimitiveExpression(s);
			}
			return new CodePrimitiveExpression(s[0]);
		}
		static CodePrimitiveExpression _ParseFloat(_PC pc)
		{
			var s = pc.Value;
			pc.Advance();
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
			return new CodePrimitiveExpression(n);
		}
		static CodePrimitiveExpression _ParseInteger(_PC pc)
		{
			var s = pc.Value;
			pc.Advance();
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
			return new CodePrimitiveExpression(n);
		}
		#endregion

		#region String/Char escapes
		static string _ParseEscapeChar(IEnumerator<char> e)
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
			throw new ArgumentException("Unterminated escape sequence", "input");
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
	}
}
