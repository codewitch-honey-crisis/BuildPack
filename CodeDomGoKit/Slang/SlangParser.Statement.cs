﻿using System;
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
					throw new ArgumentException("Unrecognized remainder in statement", "input");
				return result;
			}
		}
		
		static CodeStatement _ParseStatement(_PC pc,bool includeComments=false)
		{
			if(includeComments && (ST.lineComment==pc.SymbolId || ST.blockComment==pc.SymbolId))
				return _ParseCommentStatement(pc);
			
			_SkipComments(pc);
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
				return vs;
			}
			pc2 = pc.GetLookAhead();
			pc2.EnsureStarted();
			CodeExpression e;
			try
			{
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
					var bo = e as CodeBinaryOperatorExpression;
					if (null!=bo && CodeBinaryOperatorType.Assign == bo.Operator)
						return new CodeAssignStatement(bo.Left, bo.Right);
					return new CodeExpressionStatement(e);
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
						throw new ArgumentException("Unterminated statement. Expecting ;", "input");
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
						throw new ArgumentNullException("The attach/remove target does not refer to a valid event","input");
					er.UserData.Add("slang:unresolved", true);
					return isAttach ? new CodeAttachEventStatement(er, le) as CodeStatement : new CodeRemoveEventStatement(er, le);
				}
			}
			switch (pc.SymbolId)
			{
				case ST.keyword:
					switch(pc.Value)
					{
						case "if":
							return _ParseIfStatement(pc);
						case "goto":
							return _ParseGotoStatement(pc);
						case "for":
							return _ParseForStatement(pc);
						case "while":
							return _ParseWhileStatement(pc);
						case "return":
							return _ParseReturnStatement(pc);
						case "throw":
							return _ParseThrowStatement(pc);
						case "try":
							return _ParseTryCatchFinallyStatement(pc);
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
							return _ParseVariableDeclaration(pc);
						default:
							
							throw new NotSupportedException(string.Format("The keyword {0} is not supported", pc.Value));
							
							
					}
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
							throw new ArgumentException("Unterminated label. Expecting :", "input");
						pc.Advance();
						return ls;
					}
					
					throw new NotImplementedException("Not finished");
				default:
					
					throw new ArgumentException(string.Format("Unexpected token {0} found statement.", pc.Value), "input");
			}
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
				throw new ArgumentException("Unterminated try statement", "input");
			var result = new CodeTryCatchFinallyStatement();
			if (ST.lbrace!=pc.SymbolId)
				throw new ArgumentException("Unterminated try statement", "input");
			pc.Advance();
			while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
			{
				result.TryStatements.Add(_ParseStatement(pc,true));
			}
			if(pc.IsEnded)
				throw new ArgumentException("Unterminated try statement", "input");
			pc.Advance();
			_SkipComments(pc);
			if (ST.keyword!=pc.SymbolId)
				throw new ArgumentException("Expecting catch or finally statement", "input");
			while("catch"==pc.Value)
			{
				pc.Advance();
				_SkipComments(pc);
				var cc = new CodeCatchClause();
				if (ST.lparen == pc.SymbolId)
				{
					if (!pc.Advance())
						throw new ArgumentException("Unterminated catch clause", "input");
					cc.CatchExceptionType = _ParseTypeRef(pc);
					_SkipComments(pc);
					if (ST.identifier == pc.SymbolId)
					{
						cc.LocalName = pc.Value;
						if (!pc.Advance())
							throw new ArgumentException("Unterminated catch clause", "input");
						_SkipComments(pc);
						if (ST.rparen != pc.SymbolId)
							throw new ArgumentException(string.Format("Unexpected token {0} in catch clause", pc.Value), "input");
						pc.Advance();
						_SkipComments(pc);
					}
					else if (ST.rparen == pc.SymbolId)
					{
						pc.Advance();
						_SkipComments(pc);
					} else
						throw new ArgumentException(string.Format("Unexpected token {0} in catch clause", pc.Value), "input");
				}
				else
					throw new NotSupportedException("You must specify an exception type to catch in each catch clause.");
				
				if (ST.lbrace != pc.SymbolId)
					throw new ArgumentException("Expecting { in catch clause", "input");
				pc.Advance();
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
				{
					cc.Statements.Add(_ParseStatement(pc,true));
				}
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated catch clause", "input");
				pc.Advance();
				_SkipComments(pc);
				result.CatchClauses.Add(cc);
			}
			if(ST.keyword==pc.SymbolId&& "finally"==pc.Value)
			{
				pc.Advance();
				_SkipComments(pc);
				if (ST.lbrace != pc.SymbolId)
					throw new ArgumentException("Expecting { in finally clause", "input");
				pc.Advance();
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
					result.FinallyStatements.Add(_ParseStatement(pc,true));
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated finally clause", "input");
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
				throw new ArgumentException("Unterminated variable declaration statement", "input");
			if (ST.inc == pc.SymbolId)
				throw new NotSupportedException("Postfix increment is not supported. Consider using prefix increment instead.");
			if (ST.dec == pc.SymbolId)
				throw new NotSupportedException("Postfix decrement is not supported. Consider using prefix decrement instead.");
			if (ST.identifier != pc.SymbolId)
				throw new ArgumentException("Expecting identifier in variable declaration");
			var result = new CodeVariableDeclarationStatement(ctr, pc.Value);
			if (null == ctr)
				result.UserData.Add("slang:unresolved", true);
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated variable declaration statement", "input");
			if (ST.eq == pc.SymbolId)
			{
				
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated variable declaration initializer", "input");
				result.InitExpression = _ParseExpression(pc);
				_SkipComments(pc);
				if (ST.semi != pc.SymbolId)
					throw new ArgumentException("Invalid expression in variable declaration initializer", "input");
				pc.Advance();
				return result;
			}
			else if (null == ctr)
				throw new ArgumentException("Var variable declarations must have an initializer");
			_SkipComments(pc);
			if (ST.semi != pc.SymbolId)
				throw new ArgumentException("Invalid expression in variable declaration initializer", "input");
			pc.Advance();
			return result;
		}
		static CodeMethodReturnStatement _ParseReturnStatement(_PC pc)
		{
			// expects to be on return
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated return statement", "input");
			if (ST.semi == pc.SymbolId)
			{
				pc.Advance();
				return new CodeMethodReturnStatement();
			}
			var e = _ParseExpression(pc);
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated return statement", "input");
			if (ST.semi != pc.SymbolId)
				throw new ArgumentException("Invalid expression in return statement", "input");
			pc.Advance();
			return new CodeMethodReturnStatement(e);
		}
		static CodeThrowExceptionStatement _ParseThrowStatement(_PC pc)
		{
			// expects to be on throw
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated throw statement", "input");
			if (ST.semi == pc.SymbolId)
			{
				pc.Advance();
				return new CodeThrowExceptionStatement();
			}
			var e = _ParseExpression(pc);
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated throw statement", "input");
			if (ST.semi != pc.SymbolId)
				throw new ArgumentException("Invalid expression in throw statement", "input");
			pc.Advance();
			return new CodeThrowExceptionStatement(e);
		}
		static CodeGotoStatement _ParseGotoStatement(_PC pc)
		{
			// expects to be on goto.
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated goto statement", "input");
			if (ST.identifier != pc.SymbolId)
				throw new ArgumentException("Expecting identifier in goto statement", "input");
			var g = new CodeGotoStatement(pc.Value);
			pc.Advance();
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated goto statement", "input");
			if (ST.semi != pc.SymbolId)
				throw new ArgumentException("Expecting ; after goto statement", "input");
			pc.Advance();
			return g;
		}

		static CodeConditionStatement _ParseIfStatement(_PC pc)
		{
			// expects to be on if.
			if (!pc.Advance())
				throw new ArgumentException("Unterminated if statement", "input");
			_SkipComments(pc);
			if(ST.lparen!=pc.SymbolId || !pc.Advance())
				throw new ArgumentException("Unterminated if statement", "input");
			_SkipComments(pc);
			if(pc.IsEnded)
				throw new ArgumentException("Unterminated if statement", "input");
			var cnd = _ParseExpression(pc);
			_SkipComments(pc);
			if (ST.rparen!=pc.SymbolId || !pc.Advance())
				throw new ArgumentException("Unterminated if statement", "input");
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated if statement", "input");
			var result = new CodeConditionStatement(cnd);
			if (ST.lbrace==pc.SymbolId)
			{
				if(!pc.Advance())
					throw new ArgumentException("Unterminated if statement", "input");
				while(!pc.IsEnded && ST.rbrace!=pc.SymbolId)
					result.TrueStatements.Add(_ParseStatement(pc,true));
				if(ST.rbrace!=pc.SymbolId)
					throw new ArgumentException("Unterminated if statement", "input");
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
					throw new ArgumentException("Unterminated if/else statement", "input");
				if (ST.lbrace == pc.SymbolId)
				{
					if (!pc.Advance())
						throw new ArgumentException("Unterminated if/else statement", "input");
					while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
						result.FalseStatements.Add(_ParseStatement(pc,true));
					if (ST.rbrace != pc.SymbolId)
						throw new ArgumentException("Unterminated if/else statement", "input");
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
				throw new ArgumentException("Unterminated while statement", "input");
			_SkipComments(pc);
			if (ST.lparen != pc.SymbolId || !pc.Advance())
				throw new ArgumentException("Unterminated while statement", "input");
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated while statement", "input");
			var cnd = _ParseExpression(pc);
			_SkipComments(pc);
			if (ST.rparen != pc.SymbolId || !pc.Advance())
				throw new ArgumentException("Unterminated while statement", "input");
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated while statement", "input");
			var result = new CodeIterationStatement(new CodeSnippetStatement(),cnd,new CodeSnippetStatement());
			if (ST.lbrace == pc.SymbolId)
			{
				if (!pc.Advance())
					throw new ArgumentException("Unterminated while statement", "input");
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
					result.Statements.Add(_ParseStatement(pc,true));
				if (ST.rbrace != pc.SymbolId)
					throw new ArgumentException("Unterminated while statement", "input");
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
				throw new ArgumentException("Unterminated for statement", "input");
			_SkipComments(pc);
			if (ST.lparen != pc.SymbolId || !pc.Advance())
				throw new ArgumentException("Unterminated for statement", "input");
			_SkipComments(pc);
			CodeStatement init = null;
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated for statement", "input");
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
						throw new ArgumentException("Invalid init statement in for statement", "input");
					if (pc.IsEnded)
						throw new ArgumentException("Unterminated for statement", "input");
				}
			}
			if (null == init)
			{
				if (ST.semi != pc.SymbolId)
					throw new ArgumentException("Invalid for statement", "input");
				pc.Advance();
				_SkipComments(pc);
			}
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated for statement", "input");
			CodeExpression test = null;
			if (ST.semi != pc.SymbolId)
			{
				test = _ParseExpression(pc);
				_SkipComments(pc);
				if (ST.semi != pc.SymbolId)
					throw new ArgumentException("Invalid test expression in for statement", "input");
				if (!pc.Advance())
					throw new ArgumentException("Unterminated for statement", "input");
			}
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated for statement", "input");
			CodeExpression inc = null;
			if (ST.rparen != pc.SymbolId)
			{
				inc=_ParseExpression(pc);
				_SkipComments(pc);
			}
			if (ST.rparen != pc.SymbolId)
				throw new ArgumentNullException("Invalid increment statement in for loop");
			if (!pc.Advance())
				throw new ArgumentException("Unterminated for statement", "input");
			_SkipComments(pc);
			if (pc.IsEnded)
				throw new ArgumentException("Unterminated for statement", "input");
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
					throw new ArgumentException("Unterminated for statement", "input");
				while (!pc.IsEnded && ST.rbrace != pc.SymbolId)
					result.Statements.Add(_ParseStatement(pc,true));
				if (ST.rbrace != pc.SymbolId)
					throw new ArgumentException("Unterminated for statement", "input");
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