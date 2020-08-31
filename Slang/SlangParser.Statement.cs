using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Slang
{
	using ST = SlangTokenizer;
	partial class SlangParser
	{
		public static CodeStatement ParseStatement(string text,bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(text);
			return ParseStatement(tokenizer,includeComments);
		}
		public static CodeStatement ReadStatementFrom(Stream stream,bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(stream);
			return ParseStatement(tokenizer,includeComments);
		}
		public static CodeStatement ParseStatement(string text, int line, int column, long position,bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(text);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseStatement(pc,includeComments);
		}
		public static CodeStatement ReadStatementFrom(Stream stream, int line, int column, long position,bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(stream);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseStatement(pc,includeComments);
		}

		public static CodeStatementCollection ParseStatements(string text, bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(text);
			return ParseStatements(tokenizer, includeComments);
		}
		public static CodeStatementCollection ReadStatementsFrom(Stream stream, bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(stream);
			return ParseStatements(tokenizer, includeComments);
		}
		public static CodeStatementCollection ParseStatements(string text, int line, int column, long position, bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(text);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseStatements(pc, includeComments);
		}
		public static CodeStatementCollection ReadStatementsFrom(Stream stream, int line, int column, long position, bool includeComments = false)
		{
			var tokenizer = new SlangTokenizer(stream);
			var pc = new _PC(tokenizer);
			pc.SetLocation(line, column, position);
			return _ParseStatements(pc, includeComments);
		}

		internal static CodeStatement ParseStatement(IEnumerable<Token> tokenizer,bool includeComments=false)
		{
			var pc = new _PC(tokenizer);
			pc.Advance(false);
			return _ParseStatement(pc,includeComments);
		}
		internal static CodeStatementCollection ParseStatements(IEnumerable<Token> tokenizer, bool includeComments = false)
		{
			var pc = new _PC(tokenizer);
			pc.Advance(false);
			var result = new CodeStatementCollection();
			while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
				result.Add(_ParseStatement(pc, includeComments));
			return result;
		}
		static CodeStatement _ParseVariableDeclarationStatement(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			CodeTypeReference ctr = null;
			if (ST.varType != pc.SymbolId)
				ctr = _ParseType(pc);
			else
				pc.Advance();
			var id = _ParseIdentifier(pc);
			CodeExpression init = null;
			if (ST.eq == pc.SymbolId)
			{
				pc.Advance();
				init = _ParseExpression(pc);
			}
			else if (null == ctr)
				pc.Error("Variable declaration using var must have an initializer", l, c, p);
			if (ST.semi != pc.SymbolId)
				pc.Error("Expecting ; in variable declaration statement");
			pc.Advance();
			return new CodeVariableDeclarationStatement(ctr, id, init).Mark(l,c,p,null==ctr);
		}
		static CodeStatement _ParseStatement(_PC pc, bool includeComments = false)
		{
			#region Preamble
			CodeLinePragma lp = null;
			var startDirs = new CodeDirectiveCollection();
			while (ST.directive == pc.SymbolId)
			{
				var d = _ParseDirective(pc);
				if(null!=d)
				{
					var clp = d as CodeLinePragma;
					if (null != clp)
						lp = clp;
					else
						startDirs.Add(d as CodeDirective);
				}
				while (!includeComments && ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
					pc.Advance(false);
			}
			CodeStatement stmt = null;
			if (includeComments && (ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId))
			{
				stmt = _ParseCommentStatement(pc);
				stmt.StartDirectives.AddRange(startDirs);
				if (null != lp)
					stmt.LinePragma = lp;
			}
			else while (ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
					pc.Advance(false);
			#endregion Preamble
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			// if we got here we've parsed our start directives and this isn't a comment statement
			if (null==stmt)
			{
				_PC pc2 = null;
				switch(pc.SymbolId)
				{
					case ST.semi:
						// can't do much with empty statements
						pc.Advance();
						stmt=new CodeSnippetStatement().SetLoc(l, c, p);
						break;
					case ST.gotoKeyword:
						pc.Advance();
						if (ST.identifier != pc.SymbolId)
							pc.Error("Expecting label identifier in goto statement");
						stmt = new CodeGotoStatement(pc.Value).SetLoc(l, c, p);
						if (ST.semi != pc.SymbolId)
							pc.Error("Expecting ; in goto statement");
						pc.Advance();
						break;
					case ST.returnKeyword:
						pc.Advance();
						if (ST.semi != pc.SymbolId)
						{
							stmt = new CodeMethodReturnStatement(_ParseExpression(pc)).Mark(l, c, p);
						} else
						{
							stmt = new CodeMethodReturnStatement().SetLoc(l, c, p);
						}
						if (ST.semi != pc.SymbolId)
							pc.Error("Expecting ; in return statement");
						pc.Advance();
						break;
					case ST.throwKeyword:
						pc.Advance();
						var expr = _ParseExpression(pc);
						stmt = new CodeThrowExceptionStatement(expr).Mark(l, c, p);
						if (ST.semi != pc.SymbolId)
							pc.Error("Expecting ; in throw statement");
						pc.Advance();
						break;
					case ST.ifKeyword:
						stmt = _ParseIfStatement(pc);
						break;
					case ST.whileKeyword:
						stmt = _ParseWhileStatement(pc);
						break;
					case ST.forKeyword:
						stmt = _ParseForStatement(pc);
						break;
					case ST.tryKeyword:
						stmt = _ParseTryCatchFinallyStatement(pc);
						break;
					case ST.varType:
						stmt = _ParseVariableDeclarationStatement(pc);
						break;
					default:
						// possibly a var decl, a label statement, or an expression statement
						if (ST.identifier == pc.SymbolId)
						{
							pc2 = pc.GetLookAhead(true);
							pc2.Advance();
							if (ST.colon == pc2.SymbolId) // label
							{
								var lbl = pc2.Value;
								pc.Advance();
								stmt = new CodeLabeledStatement(lbl, new CodeSnippetStatement().SetLoc(l, c, p)).SetLoc(l, c, p);
								pc2 = null;
								break;
							}
						}
						pc2 = null;
						pc2 = pc.GetLookAhead(true);
						pc2.ResetAdvanceCount();
						var advc = 0;
						try
						{
							// possibly a var decl
							stmt = _ParseVariableDeclarationStatement(pc2);
							advc = pc2.AdvanceCount;
							while(advc>0)
							{
								pc.Advance(false) ;
								--advc;
							}
							break;
						}
						catch(SlangSyntaxException sx)
						{
							try
							{
								pc.ResetAdvanceCount();
								expr = _ParseExpression(pc);
								if (ST.semi != pc.SymbolId)
									pc.Error("Expecting ; in expression statement");
								pc.Advance();
								var bo = expr as CodeBinaryOperatorExpression;
								if (null != bo && CodeBinaryOperatorType.Assign == bo.Operator)
								{
									var ur = bo.UserData.Contains("slang:unresolved");
									stmt = new CodeAssignStatement(bo.Left, bo.Right).Mark(l, c, p,ur);
								}
								else
									stmt = new CodeExpressionStatement(expr).Mark(l, c, p);
								break;
							}
							catch(SlangSyntaxException sx2)
							{
								if (pc.AdvanceCount > advc)
									throw sx2;
								throw sx;
							}
						}
					
						

				}
			}
			#region Post
			stmt.StartDirectives.AddRange(startDirs);
			if (null != lp)
				stmt.LinePragma = lp;

			while (!includeComments && ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
				pc.Advance(false);
			while(ST.directive==pc.SymbolId && pc.Value.StartsWith("#end",StringComparison.InvariantCulture))
			{
				stmt.EndDirectives.Add(_ParseDirective(pc) as CodeDirective);
				while (!includeComments && ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
					pc.Advance(false);
			}
			#endregion Post
			return stmt;
		}
		static CodeStatementCollection _ParseStatements(_PC pc, bool includeComments=false)
		{
			var result = new CodeStatementCollection();
			pc.EnsureStarted();
			while (!pc.IsEnded && ST.rbrace!=pc.SymbolId)
				result.Add(_ParseStatement(pc, includeComments));
			return result;
		}
		static CodeStatementCollection _ParseStatementOrBlock(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			CodeStatementCollection result;
			if(ST.lbrace==pc.SymbolId)
			{
				pc.Advance();
				result = _ParseStatements(pc, true);
				if (ST.rbrace != pc.SymbolId)
					pc.Error("Unterminated statement block",l,c,p);
				pc.Advance();
				return result;
			}
			result = new CodeStatementCollection();
			result.Add(_ParseStatement(pc, false));
			return result;
		}
		static CodeStatement _ParseTryCatchFinallyStatement(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.tryKeyword!= pc.SymbolId)
				pc.Error("Expecting try");
			pc.Advance();
			var result = new CodeTryCatchFinallyStatement().Mark(l, c, p);
			if (ST.lbrace != pc.SymbolId)
				pc.Error("Expecting { in try statement");
			pc.Advance();
			result.TryStatements.AddRange(_ParseStatements(pc, true));
			if (ST.rbrace != pc.SymbolId)
				pc.Error("Expecting } in try statement");
			pc.Advance();
			while(ST.catchKeyword==pc.SymbolId)
				result.CatchClauses.Add(_ParseCatchClause(pc));
			if (0 == result.CatchClauses.Count && ST.finallyKeyword != pc.SymbolId)
				pc.Error("Expecting catch or finally");
			if(ST.finallyKeyword == pc.SymbolId)
			{
				pc.Advance();
				if (ST.lbrace != pc.SymbolId)
					pc.Error("Expecting { in finally statement");
				pc.Advance();
				result.FinallyStatements.AddRange(_ParseStatements(pc, true));
				if (ST.rbrace != pc.SymbolId)
					pc.Error("Expecting } in finally statement");
				pc.Advance();
			}
			return result;
		}
		static CodeCatchClause _ParseCatchClause(_PC pc)
		{
			if (ST.catchKeyword != pc.SymbolId)
				pc.Error("Expecting catch");
			pc.Advance();
			if (ST.lparen != pc.SymbolId)
				pc.Error("Expecting ( in catch clause");
			pc.Advance();
			var result = new CodeCatchClause();
			result.CatchExceptionType = _ParseType(pc);
			if(ST.rparen!=pc.SymbolId)
				result.LocalName = _ParseIdentifier(pc);
			if (ST.rparen != pc.SymbolId)
				pc.Error("Expecting ) in catch clause");
			pc.Advance();
			if (ST.lbrace != pc.SymbolId)
				pc.Error("Expecting { in catch clause");
			pc.Advance();
			result.Statements.AddRange(_ParseStatements(pc, true));
			if (ST.rbrace != pc.SymbolId)
				pc.Error("Expecting } in catch clause");
			pc.Advance();
			return result;
		}
		static CodeStatement _ParseIfStatement(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.ifKeyword != pc.SymbolId)
				pc.Error("Expecting if");
			pc.Advance();
			if (ST.lparen != pc.SymbolId)
				pc.Error("Expecting ( in if statement");
			pc.Advance();
			var test = _ParseExpression(pc);
			if (ST.rparen != pc.SymbolId)
				pc.Error("Expecting ) in if statement");
			pc.Advance();
			var result = new CodeConditionStatement(test).Mark(l,c,p);
			result.TrueStatements.AddRange(_ParseStatementOrBlock(pc));
			if(ST.elseKeyword==pc.SymbolId)
			{
				pc.Advance();
				result.FalseStatements.AddRange(_ParseStatementOrBlock(pc));
			}
			return result;
		}
		static CodeStatement _ParseWhileStatement(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.whileKeyword != pc.SymbolId)
				pc.Error("Expecting while");
			pc.Advance();
			if (ST.lparen != pc.SymbolId)
				pc.Error("Expecting ( in while statement");
			pc.Advance();
			var test = _ParseExpression(pc);
			if (ST.rparen != pc.SymbolId)
				pc.Error("Expecting ) in while statement");
			pc.Advance();
			var result = new CodeIterationStatement(new CodeSnippetStatement().SetLoc(l,c,p),test,new CodeSnippetStatement().SetLoc(l,c,p)).Mark(l, c, p);
			result.Statements.AddRange(_ParseStatementOrBlock(pc));
			return result;
		}
		static CodeStatement _ParseForStatement(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.forKeyword != pc.SymbolId)
				pc.Error("Expecting for");
			pc.Advance();
			if (ST.lparen != pc.SymbolId)
				pc.Error("Expecting ( in for statement");
			pc.Advance();
			var init = _ParseStatement(pc, false);
			var test = _ParseExpression(pc);
			if (ST.semi != pc.SymbolId)
				pc.Error("Expecting ; in for statement");
			pc.Advance();
			CodeStatement inc = null;
			CodeExpression ince = null;
			if (ST.rparen != pc.SymbolId)
				ince = _ParseExpression(pc);
			if (ST.rparen != pc.SymbolId)
				pc.Error("Expecting ) in for statement");
			if (null == ince)
				inc = new CodeSnippetStatement().SetLoc(pc);
			else
			{
				var bo = ince as CodeBinaryOperatorExpression;
				if (null != bo && CodeBinaryOperatorType.Assign == bo.Operator)
				{
					// probably not an attach or detach statement but we can't rule it out
					var ur = bo.UserData.Contains("slang:unresolved");
					inc = new CodeAssignStatement(bo.Left, bo.Right).Mark(ince,ur);
				}
				else
					inc = new CodeExpressionStatement(ince).Mark(ince);
			}
			pc.Advance();
			var result = new CodeIterationStatement(init, test, inc).Mark(l, c, p);
			result.Statements.AddRange(_ParseStatementOrBlock(pc));
			return result;
		}
		static CodeCommentStatement _ParseCommentStatement(_PC pc,bool docComments = false)
		{
			var s = pc.Value;
			switch(pc.SymbolId)
			{
				case ST.lineComment:
					pc.Advance(false);
					if(docComments && s.StartsWith("///",StringComparison.InvariantCulture))
						return new CodeCommentStatement(s.Substring(3).TrimEnd('\r'),true);
					return new CodeCommentStatement(s.Substring(2).TrimEnd('\r'));
				case ST.blockComment:
					pc.Advance(false);
					return new CodeCommentStatement(s.Substring(2, s.Length - 4));
			}
			pc.Error("Expecting line comment or block comment");
			return null;
		}
		// warning - can return null!:
		static object _ParseDirective(_PC pc)
		{
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			if (ST.directive != pc.SymbolId)
				pc.Error("Expecting directive");
			var kvp = _ParseDirectiveKvp(pc);
			pc.Advance(false);
			if (0 == string.Compare("#region", kvp.Key, StringComparison.InvariantCulture))
				return new CodeRegionDirective(CodeRegionMode.Start, kvp.Value);
			if (0 == string.Compare("#endregion", kvp.Key, StringComparison.InvariantCulture))
				return new CodeRegionDirective(CodeRegionMode.End, kvp.Value);
			if (0 == string.Compare("#line", kvp.Key, StringComparison.InvariantCulture))
			{
				var s = kvp.Value.Trim();
				if (0 == string.Compare("hidden", s, StringComparison.InvariantCulture) ||
					0 == string.Compare("default", s, StringComparison.InvariantCulture))
					return null;
				var i = s.IndexOfAny(new char[] { ' ', '\t' });
				if (0 > i)
					pc.Error("Malformed line pragma directive", l, c, p);
				var lineNo = 0;
				if (!int.TryParse(s.Substring(0, i), out lineNo))
					pc.Error("Malformed line pragma directive - expecting line number",l,c,p);
				var file = s.Substring(i + 1).Trim();
				file = _ParseDirectiveQuotedPart(file,l,c,p);
				return new CodeLinePragma(file, lineNo);
			} else if(0==string.Compare("#pragma",kvp.Key,StringComparison.InvariantCulture))
			{
				var sa = kvp.Value.Split(' ', '\t');
				var sl = new List<string>(sa.Length);
				for(var i = 0;i<sa.Length;i++)
				{
					if (!string.IsNullOrWhiteSpace(sa[i]))
						sl.Add(sa[i]);
				}
				if (0 == sl.Count)
					pc.Error("Malformed pragma directive", l, c, p);
				if (0 != string.Compare("checksum", sl[0], StringComparison.InvariantCulture))
					pc.Error("Unsupported directive "+kvp.Key, l, c, p);
				if(4!=sl.Count)
					pc.Error("Malformed checksum pragma directive", l, c, p);
				var fn = _ParseDirectiveQuotedPart(sl[1],l,c,p);
				var guid = _ParseDirectiveQuotedPart(sl[2], l, c, p);
				var bytes = _ParseDirectiveQuotedPart(sl[3], l, c, p);
				Guid g;
				if (!Guid.TryParse(guid, out g))
					pc.Error("Invalid guid in checksum pragma directive");
				if (0 != (bytes.Length % 2))
					pc.Error("Invalid bytes region in checksum pragma directive");
				var ba = new byte[bytes.Length / 2];
				for(var i = 0;i<ba.Length;i++)
				{
					var ch1 = bytes[i * 2];
					if (!_IsHexChar(ch1))
						pc.Error("Invalid bytes region in checksum pragma directive");
					var ch2 = bytes[i * 2 + 1];
					if (!_IsHexChar(ch2))
						pc.Error("Invalid bytes region in checksum pragma directive");

					ba[i] = unchecked((byte)(_FromHexChar(ch1) * 0x10 + _FromHexChar(ch2)));

				}
				return new CodeChecksumPragma(fn, g, ba);
			}
			pc.Error("Unsupported directive "+kvp.Key,l,c,p);
			return null;
		}
		static string _ParseDirectiveQuotedPart(string part,int l,int c,long p)
		{
			var sb = new StringBuilder();
			var e = part.GetEnumerator();
			if (!e.MoveNext() || '\"'!=e.Current)
				throw new SlangSyntaxException("Expecting \" in directive part",l,c,p);
			if (e.MoveNext())
			{
				while (true)
				{
					if ('\"' == e.Current)
					{

						if (!e.MoveNext() || '\"' != e.Current)
						{
							return sb.ToString();
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
			throw new SlangSyntaxException("Unterminated quoted string in directive part", l, c, p);
		}
		static KeyValuePair<string,string> _ParseDirectiveKvp(_PC pc)
		{
			var s = pc.Value;
			var i = s.IndexOfAny(new char[] { ' ', '\t' });
			if (0 > i)
				return new KeyValuePair<string, string>(s, null);
			return new KeyValuePair<string, string>(s.Substring(0, i), s.Substring(i + 1).Trim());
		}
	}
}
