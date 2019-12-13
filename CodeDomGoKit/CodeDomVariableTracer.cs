using System;
using System.Collections.Generic;
using System.CodeDom;
using System.Diagnostics;

namespace CD
{
	/// <summary>
	/// Traces variable declarations throughout a method
	/// </summary>
	public static class CodeDomVariableTracer
	{

		static readonly object _catchClauseKey = new object();
		/// <summary>
		/// Indicates whether this variable declaration actually represents a catch clause
		/// </summary>
		/// <param name="vds">The variable declaration statement to test</param>
		/// <returns>True if this variable declaration entry was manufactured as part of catch clause, otherwise false</returns>
		public static bool IsCatchClause(CodeVariableDeclarationStatement vds)
		{
			return vds.UserData.Contains(_catchClauseKey);
		}
		/// <summary>
		/// Traces a member to a particular statement, returning all variables in the scope of that statement.
		/// </summary>
		/// <param name="member">The member to trace</param>
		/// <param name="target">The target to look for</param>
		/// <returns>A list of variable declarations representing the variables that are in scope of <paramref name="target"/></returns>
		public static IList<CodeVariableDeclarationStatement> Trace(CodeTypeMember member, CodeStatement target)
		{
			bool found;
			var result = _TraceMember(member, target, out found);
			if (!found)
				throw new ArgumentException("The member did not contain the specified target statement", nameof(target));
			return result;
		}
		/// <summary>
		/// Traces a containing statement to a particular statement within it, returning all variables in the scope of that statement.
		/// </summary>
		/// <param name="from">The statment to trace</param>
		/// <param name="target">The target to look for within that statement</param>
		/// <returns>A list of variable declarations representing the variables that are in scope of <paramref name="target"/></returns>
		public static IList<CodeVariableDeclarationStatement> Trace(CodeStatement from, CodeStatement target)
		{
			bool found;
			var result = _TraceStatement(from, target, out found);
			if (!found)
				throw new ArgumentException("The statement did not contain the specified target statement", nameof(target));
			return result;
		}
		static IList<CodeVariableDeclarationStatement> _TraceMember(CodeTypeMember m, CodeStatement t, out bool found)
		{
			var result = new List<CodeVariableDeclarationStatement>();
			var cmm = m as CodeMemberMethod;
			if (null != cmm)
			{
				foreach (CodeStatement tt in cmm.Statements)
				{
					Debug.WriteLine(CodeDomUtility.ToString(tt));
					var r = _TraceStatement(tt, t, out found);
					result.AddRange(r);
					if (found)
					{
						return result;
					}
				}
				found = false;
				return new CodeVariableDeclarationStatement[0];
			}
			var cmp = m as CodeMemberProperty;
			if (null != cmp)
			{
				foreach (CodeStatement tt in cmp.GetStatements)
				{
					var r = _TraceStatement(tt, t, out found);
					result.AddRange(r);
					if (found)
					{
						return result;
					}
				}
			}
			result.Clear(); // new scope
			foreach (CodeStatement tt in cmp.GetStatements)
			{
				var r = _TraceStatement(tt, t, out found);
				result.AddRange(r);
				if (found)
				{
					return result;
				}
			}
			found = false;
			return new CodeVariableDeclarationStatement[0];
		}
		static IList<CodeVariableDeclarationStatement> _TraceStatement(CodeStatement obj, CodeStatement target, out bool found)
		{
			var ca = obj as CodeAssignStatement;
			if (null != ca)
			{
				return _TraceAssignStatement(ca, target, out found);
			}
			var cae = obj as CodeAttachEventStatement;
			if (null != cae)
			{
				return _TraceAttachEventStatement(cae, target, out found);

			}
			var cc = obj as CodeCommentStatement;
			if (null != cc)
			{
				return _TraceCommentStatement(cc, target, out found);

			}
			var ccnd = obj as CodeConditionStatement;
			if (null != ccnd)
			{
				return _TraceConditionStatement(ccnd, target, out found);

			}
			var ce = obj as CodeExpressionStatement;
			if (null != ce)
			{
				return _TraceExpressionStatement(ce, target, out found);

			}
			var cg = obj as CodeGotoStatement;
			if (null != cg)
			{
				return _TraceGotoStatement(cg, target, out found);

			}
			var ci = obj as CodeIterationStatement;
			if (null != ci)
			{
				return _TraceIterationStatement(ci, target, out found);

			}
			var cl = obj as CodeLabeledStatement;
			if (null != cl)
			{
				return _TraceLabeledStatement(cl, target, out found);

			}
			var cm = obj as CodeMethodReturnStatement;
			if (null != cm)
			{
				return _TraceMethodReturnStatement(cm, target, out found);

			}
			var cre = obj as CodeRemoveEventStatement;
			if (null != cre)
			{
				return _TraceRemoveEventStatement(cre, target, out found);

			}
			var cs = obj as CodeSnippetStatement;
			if (null != cs)
			{
				return _TraceSnippetStatement(cs, target, out found);

			}
			var cte = obj as CodeThrowExceptionStatement;
			if (null != cte)
			{
				return _TraceThrowExceptionStatement(cte, target, out found);

			}
			var ctcf = obj as CodeTryCatchFinallyStatement;
			if (null != ctcf)
			{
				return _TraceTryCatchFinallyStatement(ctcf, target, out found);

			}
			var cvd = obj as CodeVariableDeclarationStatement;
			if (null != cvd)
			{
				var res = new List<CodeVariableDeclarationStatement>(_TraceVariableDeclarationStatement(cvd, target, out found));
				res.Add(cvd);

				return res;
			}
			throw new NotSupportedException("The graph contains an unsupported statement");
		}

		private static IList<CodeVariableDeclarationStatement> _TraceVariableDeclarationStatement(CodeVariableDeclarationStatement s, CodeStatement t, out bool found)
		{
			found = false;
			if (s == t)
			{
				found = true;
				
			}
			return new CodeVariableDeclarationStatement[] { };
		}

		private static IList<CodeVariableDeclarationStatement> _TraceTryCatchFinallyStatement(CodeTryCatchFinallyStatement s, CodeStatement t, out bool found)
		{
			found = true;
			if (s == t) return new CodeVariableDeclarationStatement[0];
			found = false;
			var result = new List<CodeVariableDeclarationStatement>();
			foreach (CodeStatement tt in s.TryStatements)
			{
				var r = _TraceStatement(tt, t, out found);
				result.AddRange(r);
				if (found)
				{
					return result;
				}
			}
			result.Clear(); // new scope
			foreach (CodeCatchClause cc in s.CatchClauses)
			{
				// each clause we're in a new scope
				var res2 = new List<CodeVariableDeclarationStatement>();
				if (null != cc.CatchExceptionType)
				{
					// treat catch(Exception e) as a variable declaration of e
					var vdc = new CodeVariableDeclarationStatement(cc.CatchExceptionType, cc.LocalName);
					// but flag it later so we know, in case we need to
					vdc.UserData.Add(_catchClauseKey, cc);
					res2.Add(vdc);
				}
				foreach (CodeStatement tt in cc.Statements)
				{
					var r = _TraceStatement(tt, t, out found);
					res2.AddRange(r);
					if (found)
					{
						result.AddRange(res2);
						return result;
					}
				}
			}
			result.Clear(); // new scope
			foreach (CodeStatement tt in s.FinallyStatements)
			{
				var r = _TraceStatement(tt, t, out found);
				result.AddRange(r);
				if (found)
				{
					return result;
				}
			}
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceThrowExceptionStatement(CodeThrowExceptionStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceSnippetStatement(CodeSnippetStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceRemoveEventStatement(CodeRemoveEventStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceMethodReturnStatement(CodeMethodReturnStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceLabeledStatement(CodeLabeledStatement s, CodeStatement t, out bool found)
		{
			found = false;
			if (s == t)
			{
				found = true;
				return new CodeVariableDeclarationStatement[0];
			}
			if (null != s.Statement)
				return _TraceStatement(s.Statement, t, out found);
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceIterationStatement(CodeIterationStatement s, CodeStatement t, out bool found)
		{
			var result = new List<CodeVariableDeclarationStatement>();
			var r = _TraceStatement(s.InitStatement, t,out found);
			result.AddRange(r); // we're in the same scope
			if (found)
			{
				return result;
			}
			if (s == t)
			{
				found = true;
				return result;
			}
			foreach (CodeStatement tt in s.Statements)
			{
				r = _TraceStatement(tt, t , out found);
				result.AddRange(r);
				if (found)
				{
					return result;
				}
			}
			r = _TraceStatement(s.IncrementStatement, t, out found);
			result.AddRange(r);
			if (found)
			{
				return result;
			}
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceGotoStatement(CodeGotoStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceExpressionStatement(CodeExpressionStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceConditionStatement(CodeConditionStatement s, CodeStatement t, out bool found)
		{
			if (s == t)
			{
				found = true;
				return new CodeVariableDeclarationStatement[0];
			}
			var result = new List<CodeVariableDeclarationStatement>();
			foreach (CodeStatement tt in s.TrueStatements)
			{
				var r = _TraceStatement(tt, t, out found);
				result.AddRange(r);
				if (found)
				{
					return result;
				}
			}
			result.Clear();
			foreach (CodeStatement tt in s.FalseStatements)
			{
				var r = _TraceStatement(tt, t, out found);
				result.AddRange(r);
				if (found)
				{
					return result;
				}
			}
			found = false;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceCommentStatement(CodeCommentStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceAttachEventStatement(CodeAttachEventStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}

		private static IList<CodeVariableDeclarationStatement> _TraceAssignStatement(CodeAssignStatement s, CodeStatement t, out bool found)
		{
			found = t == s;
			return new CodeVariableDeclarationStatement[0];
		}
	}
}
