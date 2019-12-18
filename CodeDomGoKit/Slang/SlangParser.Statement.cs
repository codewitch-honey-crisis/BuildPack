using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;

namespace CD
{
	using ST = SlangTokenizer;
	partial class SlangParser
	{
		/// <summary>
		/// Reads a <see cref="CodeStatement"/> from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <param name="includeComments">True to include comments, or false to skip them</param>
		/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>
		public static CodeStatement ReadStatementFrom(TextReader reader,bool includeComments=false)
			=> ParseStatement(TextReaderEnumerable.FromReader(reader),includeComments);
		/// <summary>
		/// Reads a <see cref="CodeStatement"/> from the specified file
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <param name="includeComments">True if comments should be returned as statements, or false to skip them</param>
		/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>
		public static CodeStatement ReadStatementFrom(string filename,bool includeComments=false)
			=> ParseStatement(new FileReaderEnumerable(filename),includeComments);
		/// <summary>
		/// Reads a <see cref="CodeStatement"/> from the specified URL
		/// </summary>
		/// <param name="url">The URL to read</param>
		/// <param name="includeComments">True to return parsed comments as statements, or false to skip them</param>
		/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>

		public static CodeStatement ReadStatementFromUrl(string url,bool includeComments=false)
			=> ParseStatement(new UrlReaderEnumerable(url),includeComments);
		/// <summary>
		/// Parses a <see cref="CodeStatement"/> from the specified input
		/// </summary>
		/// <param name="input">The input to parse</param>
		/// <param name="includeComments">True to return parsed comments as statements, or false to skip them</param>
		/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>
		public static CodeStatement ParseStatement(IEnumerable<char> input,bool includeComments=false) {
			using (var e = new ST(input).GetEnumerator())
			{
				var pc = new _PC(e);
				pc.EnsureStarted();
				var result = _ParseStatement(pc,includeComments);
				if (!pc.IsEnded)
					throw new SlangSyntaxException("Unrecognized remainder in statement", pc.Current.Line, pc.Current.Column, pc.Current.Position);
				return result;
			}
		}
		static object _ParseDirective(_PC pc)
		{
			var s = pc.Value;
			var i = s.IndexOfAny(new char[] { ' ', '\t' });
			if(0>i)
				i = s.Length;
			var type = s.Substring(1, i - 1).Trim();
			switch(type)
			{
				case "region":
					pc.Advance();
					return new CodeRegionDirective(CodeRegionMode.Start, s.Substring(i).Trim());
				case "endregion":
					pc.Advance();
					return new CodeRegionDirective(CodeRegionMode.End, s.Substring(i).Trim());
				case "line":
					pc.Advance();
					s = s.Substring(i).Trim();
					i = s.LastIndexOfAny(new char[] { ' ', '\t' });
					if(-1<i)
					{
						var num = s.Substring(0,i).Trim();
						int n;
						if(int.TryParse(num, out n))
						{
							s = s.Substring(i).Trim();
							if('\"'==s[0])
								s = s.Substring(1, s.Length - 2).Replace("\"\"", "\"");
							return new CodeLinePragma(s, n);
						}
					}
					break;
			}
			_Error(string.Format("Invalid or unsupported directive ", pc.Value), pc.Current);
			return null;
		}
		static List<object> _ParseDirectives(_PC pc,bool endDirectives=false)
		{
			var result = new List<object>();
			while(ST.directive==pc.SymbolId)
			{
				if (endDirectives && !pc.Value.Trim().StartsWith("#endregion"))
					break;
				else if (!endDirectives && pc.Value.Trim().StartsWith("#endregion"))
					break;
				result.Add(_ParseDirective(pc));
			}
			return result;
		}
		static void _AddStartDirs(CodeStatement stmt,IList<object> dirs)
		{
			for (int ic = dirs.Count, i = 0; i < ic; ++i)
			{
				var dir = dirs[i];
				var l = dir as CodeLinePragma;
				if (null != l)
				{
					stmt.LinePragma = l;
					dirs.RemoveAt(i);
					--i;
					--ic;
					continue;
				}
				var d = dir as CodeDirective;
				if (null != d)
				{
					stmt.StartDirectives.Add(d);
					dirs.RemoveAt(i);
					--i;
					--ic;
				}
			}
		}
		static void _AddEndDirs(CodeStatement stmt, IList<object> dirs)
		{
			for (int ic = dirs.Count, i = 0; i < ic; ++i)
			{
				var dir = dirs[i];
				var d = dir as CodeDirective;
				if (null != d)
				{
					stmt.EndDirectives.Add(d);
					dirs.RemoveAt(i);
					--i;
					--ic;
				}
			}
		}
		static CodeStatement _ParseStatement(_PC pc,bool includeComments=false)
		{
			var dirs = _ParseDirectives(pc);
			if (includeComments && (ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId))
			{
				var c= _ParseCommentStatement(pc);
				_AddStartDirs(c, dirs);
				dirs.AddRange(_ParseDirectives(pc,true));
				_AddEndDirs(c, dirs);
			}
			_SkipComments(pc);
			dirs.AddRange(_ParseDirectives(pc));
			var pc2 = pc.GetLookAhead();
			pc2.EnsureStarted();
			CodeVariableDeclarationStatement vs = null;
			try
			{
				vs = _ParseVariableDeclaration(pc2);
			}
			catch { vs = null; }
			if(null!=vs)
			{
				// advance 
				_ParseVariableDeclaration(pc);
				_AddStartDirs(vs,dirs);
				_SkipComments(pc);
				dirs.AddRange(_ParseDirectives(pc,true));
				return vs;
			}
			pc2 = pc.GetLookAhead();
			pc2.EnsureStarted();
			CodeExpression e;
			try
			{
				_ParseDirectives(pc2, false);
				_SkipComments(pc);
				e = _ParseExpression(pc2);

			}
			catch { e = null; }
			if (null != e)
			{
				_SkipComments(pc2);
				if (ST.semi == pc2.SymbolId)
				{
					pc2.Advance();
					_ParseExpression(pc);
					_SkipComments(pc);
					pc.Advance();

					// c# treats a=1; as an expression-statement using an assign expression, linguistically
					// so that's how we parsed it. However, CodeDOM has a special case object for this
					// called CodeAssignStatement. For maximum language portability, we don't want to rely
					// on assign expressions when we don't have to as they can get weird. So what we do is
					// whenever we parse one of these we detect it and turn it into an assign statement
					CodeStatement r = null;
					var bo = e as CodeBinaryOperatorExpression;
					if (null!=bo && CodeBinaryOperatorType.Assign == bo.Operator)
						r=new CodeAssignStatement(bo.Left, bo.Right);
					else
						r=new CodeExpressionStatement(e);
					_AddStartDirs(r,dirs);
					dirs.AddRange(_ParseDirectives(pc, true));
					_AddEndDirs(r, dirs);
					return r;
				} else if(ST.addAssign==pc2.SymbolId || ST.subAssign==pc2.SymbolId)
				{
					bool isAttach = ST.addAssign == pc2.SymbolId;
					_ParseExpression(pc);
					_SkipComments(pc);
					pc.Advance();

					pc2.Advance();
					_SkipComments(pc);
					var le = _ParseExpression(pc);
					_SkipComments(pc);
					if (pc.IsEnded)
						_Error("Unterminated statement. Expecting ;", pc.Current);
					pc.Advance();
					var v = e as CodeVariableReferenceExpression;
					CodeEventReferenceExpression er =null;
					if (null!=v)
					{
						er = new CodeEventReferenceExpression(null, v.VariableName);
					} else
					{
						var f = e as CodeFieldReferenceExpression;
						if (null != f)
							er = new CodeEventReferenceExpression(f.TargetObject, f.FieldName);
					}
					if (null == er)
						_Error("The attach/remove target does not refer to a valid event",pc.Current);
					er.UserData.Add("slang:unresolved", true);
					var r = isAttach ? new CodeAttachEventStatement(er, le) as CodeStatement : new CodeRemoveEventStatement(er, le);
					_AddStartDirs(r, dirs);
					_ParseDirectives(pc, true);
					_AddEndDirs(r, dirs);
					return r;
				}
			}
			switch (pc.SymbolId)
			{
				case ST.keyword:
					CodeStatement r=null;
					switch(pc.Value)
					{
						case "if":
							r=_ParseIfStatement(pc);
							break;
						case "goto":
							r=_ParseGotoStatement(pc);
							break;
						case "for":
							r= _ParseForStatement(pc);
							break;
						case "while":
							r= _ParseWhileStatement(pc);
							break;
						case "return":
							r= _ParseReturnStatement(pc);
							break;
						case "throw":
							r= _ParseThrowStatement(pc);
							break;
						case "try":
							r= _ParseTryCatchFinallyStatement(pc);
							break;
						case "var":
						case "bool":
						case "char":
						case "string":
						case "sbyte":
						case "byte":
						case "short":
						case "ushort":
						case "int":
						case "uint":
						case "long":
						case "ulong":
						case "float":
						case "double":
						case "decimal":
							r= _ParseVariableDeclaration(pc);
							break;
						default:
							
							throw new NotSupportedException(string.Format("The keyword {0} is not supported", pc.Value));
					}
					_AddStartDirs(r, dirs);
					dirs.AddRange(_ParseDirectives(pc, true));
					_AddEndDirs(r, dirs);
					return r;
				case ST.identifier: // we already know it isn't an expression
					var s = pc.Value;
					pc2 = pc.GetLookAhead();
					pc2.EnsureStarted();
					pc2.Advance();
					if(ST.colon==pc2.SymbolId)
					{
						// CodeDOM for some reason wants us to attach a statement to a label.
						// we don't like that for a number of reasons so we don't do it.
						var ls = new CodeLabeledStatement(pc.Value);
						pc.Advance();
						_SkipComments(pc);
						if (pc.IsEnded || ST.colon != pc.SymbolId)
							_Error("Unterminated label. Expecting :", pc.Current);
						pc.Advance();
						_AddStartDirs(ls, dirs);
						dirs.AddRange(_ParseDirectives(pc, true));
						_AddEndDirs(ls, dirs);
						return ls;
					}
					
					throw new NotImplementedException("Not finished");
				default:
					
					_Error(string.Format("Unexpected token {0} found statement.", pc.Value), pc.Current);
					break;
			}
			return null; // should never get here.
		}
		static CodeCommentStatement _ParseCommentStatement(_PC pc)
		{
			// expects to be on comment
			var s = pc.Value;
			if (ST.lineComment == pc.SymbolId)
			{
				pc.Advance();
				if (s.StartsWith("///"))
					return new CodeCommentStatement(s.Substring(3).Trim(), true);
				return new CodeCommentStatement(s.Substring(2).Trim());
			}
			pc.Advance();
			return new CodeCommentStatement(s.Substring(2, s.Length - 4).Trim());
		}
		static CodeTryCatchFinallyStatement _ParseTryCatchFinallyStatement(_PC pc)
		{
			// expects on try
			pc.Advance();
			_SkipComments(pc);
			if(pc.IsEnded)
				_Error("Unterminated try statement", pc.Current);
			var result = new CodeTryCatchFinallyStatement();
			if (ST.lbrace!=pc.SymbolId)
				_Error("Unterminated try statement", pc.Current);
			pc.Advance();
			while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
			{
				result.TryStatements.Add(_ParseStatement(pc,true));
			}
			if(pc.IsEnded)
				_Error("Unterminated try statement", pc.Current);
			pc.Advance();
			_SkipComments(pc);
			if (ST.keyword!=pc.SymbolId)
				_Error("Expecting catch or finally statement", pc.Current);
			while("catch"==pc.Value)
			{
				pc.Advance();
				_SkipComments(pc);
				var cc = new CodeCatchClause();
				if (ST.lparen == pc.SymbolId)
				{
					if (!pc.Advance())
						_Error("Unterminated catch clause", pc.Current);
					cc.CatchExceptionType = _ParseTypeRef(pc);
					_SkipComments(pc);
					if (ST.identifier == pc.SymbolId)
					{
						cc.LocalName = pc.Value;
						if (!pc.Advance())
							_Error("Unterminated catch clause", pc.Current);
						_SkipComments(pc);
						if (ST.rparen != pc.SymbolId)
							_Error(string.Format("Unexpected token {0} in catch clause", pc.Value), pc.Current);
						pc.Advance();
						_SkipComments(pc);
					}
					else if (ST.rparen == pc.SymbolId)
					{
						pc.Advance();
						_SkipComments(pc);
					} else
						_Error(string.Format("Unexpected token {0} in catch clause", pc.Value), pc.Current);
				}
				else
					throw new NotSupportedException("You must specify an exception type to catch in each catch clause.");
				
				if (ST.lbrace != pc.SymbolId)
					_Error("Expecting { in catch clause", pc.Current);
				pc.Advance();
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
				{
					cc.Statements.Add(_ParseStatement(pc,true));
				}
				if (pc.IsEnded)
					_Error("Unterminated catch clause", pc.Current);
				pc.Advance();
				_SkipComments(pc);
				result.CatchClauses.Add(cc);
			}
			if(ST.keyword==pc.SymbolId&& "finally"==pc.Value)
			{
				pc.Advance();
				_SkipComments(pc);
				if (ST.lbrace != pc.SymbolId)
					_Error("Expecting { in finally clause", pc.Current);
				pc.Advance();
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
					result.FinallyStatements.Add(_ParseStatement(pc,true));
				if (pc.IsEnded)
					_Error("Unterminated finally clause", pc.Current);
				pc.Advance();
				if(0==result.FinallyStatements.Count)
				{
					// force the codedom to render it anyway
					result.FinallyStatements.Add(new CodeSnippetStatement());
				}
			}
			return result;
		}
		static CodeVariableDeclarationStatement _ParseVariableDeclaration(_PC pc)
		{
			CodeTypeReference ctr=null;
			if (!(ST.keyword == pc.SymbolId && "var" == pc.Value))
				ctr = _ParseTypeRef(pc);
			else
				pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated variable declaration statement", pc.Current);
			if (ST.inc == pc.SymbolId)
				throw new NotSupportedException("Postfix increment is not supported. Consider using prefix increment instead.");
			if (ST.dec == pc.SymbolId)
				throw new NotSupportedException("Postfix decrement is not supported. Consider using prefix decrement instead.");
			if (ST.identifier != pc.SymbolId)
				_Error("Expecting identifier in variable declaration",pc.Current);
			var result = new CodeVariableDeclarationStatement(ctr, pc.Value);
			if (null == ctr)
				result.UserData.Add("slang:unresolved", true);
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated variable declaration statement", pc.Current);
			if (ST.eq == pc.SymbolId)
			{
				
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated variable declaration initializer", pc.Current);
				result.InitExpression = _ParseExpression(pc);
				_SkipComments(pc);
				if (ST.semi != pc.SymbolId)
					_Error("Invalid expression in variable declaration initializer", pc.Current);
				pc.Advance();
				return result;
			}
			else if (null == ctr)
				_Error("Var variable declarations must have an initializer",pc.Current);
			_SkipComments(pc);
			if (ST.semi != pc.SymbolId)
				_Error("Invalid expression in variable declaration initializer", pc.Current);
			pc.Advance();
			return result;
		}
		static CodeMethodReturnStatement _ParseReturnStatement(_PC pc)
		{
			// expects to be on return
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated return statement", pc.Current);
			if (ST.semi == pc.SymbolId)
			{
				pc.Advance();
				return new CodeMethodReturnStatement();
			}
			var e = _ParseExpression(pc);
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated return statement", pc.Current);
			if (ST.semi != pc.SymbolId)
				_Error("Invalid expression in return statement", pc.Current);
			pc.Advance();
			return new CodeMethodReturnStatement(e);
		}
		static CodeThrowExceptionStatement _ParseThrowStatement(_PC pc)
		{
			// expects to be on throw
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated throw statement", pc.Current);
			if (ST.semi == pc.SymbolId)
			{
				pc.Advance();
				return new CodeThrowExceptionStatement();
			}
			var e = _ParseExpression(pc);
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated throw statement", pc.Current);
			if (ST.semi != pc.SymbolId)
				_Error("Invalid expression in throw statement", pc.Current);
			pc.Advance();
			return new CodeThrowExceptionStatement(e);
		}
		static CodeGotoStatement _ParseGotoStatement(_PC pc)
		{
			// expects to be on goto.
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated goto statement", pc.Current);
			if (ST.identifier != pc.SymbolId)
				_Error("Expecting identifier in goto statement", pc.Current);
			var g = new CodeGotoStatement(pc.Value);
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated goto statement", pc.Current);
			if (ST.semi != pc.SymbolId)
				_Error("Expecting ; after goto statement", pc.Current);
			pc.Advance();
			return g;
		}

		static CodeConditionStatement _ParseIfStatement(_PC pc)
		{
			// expects to be on if.
			if (!pc.Advance())
				_Error("Unterminated if statement", pc.Current);
			_SkipComments(pc);
			if(ST.lparen!=pc.SymbolId || !pc.Advance())
				_Error("Unterminated if statement", pc.Current);
			_SkipComments(pc);
			if(pc.IsEnded)
				_Error("Unterminated if statement", pc.Current);
			var cnd = _ParseExpression(pc);
			_SkipComments(pc);
			if (ST.rparen!=pc.SymbolId || !pc.Advance())
				_Error("Unterminated if statement", pc.Current);
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated if statement", pc.Current);
			var result = new CodeConditionStatement(cnd);
			if (ST.lbrace==pc.SymbolId)
			{
				if(!pc.Advance())
					_Error("Unterminated if statement", pc.Current);
				while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
					result.TrueStatements.Add(_ParseStatement(pc,true));
				if(ST.rbrace!=pc.SymbolId)
					_Error("Unterminated if statement", pc.Current);
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					return result;
			} else
			{
				result.TrueStatements.Add(_ParseStatement(pc));
			}
			_SkipComments(pc);
			if(ST.keyword==pc.SymbolId && "else"==pc.Value)
			{
				pc.Advance();
				_SkipComments(pc);
				if(pc.IsEnded)
					_Error("Unterminated if/else statement", pc.Current);
				if (ST.lbrace == pc.SymbolId)
				{
					if (!pc.Advance())
						_Error("Unterminated if/else statement", pc.Current);
					while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
						result.FalseStatements.Add(_ParseStatement(pc,true));
					if (ST.rbrace != pc.SymbolId)
						_Error("Unterminated if/else statement", pc.Current);
					pc.Advance();
					_SkipComments(pc);
					if (pc.IsEnded)
						return result;
				}
				else
				{
					result.FalseStatements.Add(_ParseStatement(pc));
				}
			}
			return result;
		}
		static CodeIterationStatement _ParseWhileStatement(_PC pc)
		{
			// expects to be on while.
			if (!pc.Advance())
				_Error("Unterminated while statement", pc.Current);
			_SkipComments(pc);
			if (ST.lparen != pc.SymbolId || !pc.Advance())
				_Error("Unterminated while statement", pc.Current);
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated while statement", pc.Current);
			var cnd = _ParseExpression(pc);
			_SkipComments(pc);
			if (ST.rparen != pc.SymbolId || !pc.Advance())
				_Error("Unterminated while statement", pc.Current);
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated while statement", pc.Current);
			var result = new CodeIterationStatement(new CodeSnippetStatement(),cnd,new CodeSnippetStatement());
			if (ST.lbrace == pc.SymbolId)
			{
				if (!pc.Advance())
					_Error("Unterminated while statement", pc.Current);
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
					result.Statements.Add(_ParseStatement(pc,true));
				if (ST.rbrace != pc.SymbolId)
					_Error("Unterminated while statement", pc.Current);
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					return result;
			}
			else
				result.Statements.Add(_ParseStatement(pc));
			
			_SkipComments(pc);
			return result;
		}
		static CodeIterationStatement _ParseForStatement(_PC pc)
		{
			// expects to be on for
			if (!pc.Advance())
				_Error("Unterminated for statement", pc.Current);
			_SkipComments(pc);
			if (ST.lparen != pc.SymbolId || !pc.Advance())
				_Error("Unterminated for statement", pc.Current);
			_SkipComments(pc);
			CodeStatement init = null;
			if (pc.IsEnded)
				_Error("Unterminated for statement", pc.Current);
			if (ST.semi != pc.SymbolId)
			{
				var pc2 = pc.GetLookAhead();
				pc2.EnsureStarted();
				try
				{
					init = _ParseVariableDeclaration(pc2);
				}
				catch { init = null; }
			}
			if(null!=init)
			{
				_ParseVariableDeclaration(pc);
				_SkipComments(pc);
			} else
			{
				_SkipComments(pc);
				if (ST.semi != pc.SymbolId)
				{
					var e = _ParseExpression(pc);
					var bbo = e as CodeBinaryOperatorExpression;
					if (null == e)
						throw new NotImplementedException("Expression in init statement was null");
					if (null != bbo && CodeBinaryOperatorType.Assign == bbo.Operator)
						init = new CodeAssignStatement(bbo.Left, bbo.Right);
					else
						init = new CodeExpressionStatement(e);
					_SkipComments(pc);
					if (ST.semi != pc.SymbolId)
						_Error("Invalid init statement in for statement", pc.Current);
					if (pc.IsEnded)
						_Error("Unterminated for statement", pc.Current);
				}
			}
			if (null == init)
			{
				if (ST.semi != pc.SymbolId)
					_Error("Invalid for statement", pc.Current);
				pc.Advance();
				_SkipComments(pc);
			}
			if (pc.IsEnded)
				_Error("Unterminated for statement", pc.Current);
			CodeExpression test = null;
			if (ST.semi != pc.SymbolId)
			{
				test = _ParseExpression(pc);
				_SkipComments(pc);
				if (ST.semi != pc.SymbolId)
					_Error("Invalid test expression in for statement", pc.Current);
				if (!pc.Advance())
					_Error("Unterminated for statement", pc.Current);
			}
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated for statement", pc.Current);
			CodeExpression inc = null;
			if (ST.rparen != pc.SymbolId)
			{
				inc=_ParseExpression(pc);
				_SkipComments(pc);
			}
			if (ST.rparen != pc.SymbolId)
				throw new ArgumentNullException("Invalid increment statement in for loop");
			if (!pc.Advance())
				_Error("Unterminated for statement", pc.Current);
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Unterminated for statement", pc.Current);
			var bo = inc as CodeBinaryOperatorExpression;
			CodeStatement incs = null;
			if (null != inc)
			{
				if (null != bo && CodeBinaryOperatorType.Assign == bo.Operator)
					incs = new CodeAssignStatement(bo.Left, bo.Right);
				else
					incs = new CodeExpressionStatement(inc);
			}
			if (null == init)
				init = new CodeSnippetStatement();
			if (null == incs)
				incs = new CodeSnippetStatement();
			if (null == test)
				test = new CodeSnippetExpression();
			var result = new CodeIterationStatement(init, test, incs);
			if (ST.lbrace == pc.SymbolId)
			{
				if (!pc.Advance())
					_Error("Unterminated for statement", pc.Current);
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
					result.Statements.Add(_ParseStatement(pc,true));
				if (ST.rbrace != pc.SymbolId)
					_Error("Unterminated for statement", pc.Current);
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					return result;
			}
			else
				result.Statements.Add(_ParseStatement(pc));

			_SkipComments(pc);
			return result;
		}
	}
}
