using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RolexDemo
{
	using T = SampleTokenizer;
	using V = IDictionary<string, int>;
	class Parser
	{
		private class _PC
		{
			readonly IEnumerator<Token> _e;
			int _state;
			public _PC(IEnumerator<Token> e)
			{
				_e = e;
				_state = -2;
			}
			public int SymbolId => Current.SymbolId;
			public string Value => Current.Value;
			public Token Current {
				get {
					if (0 > _state)
					{
						var t = default(Token);
						t.SymbolId = -1 == _state ? -2 : -1;
						return t;
					}
					return _e.Current;
				}
			}
			public void EnsureStarted()
			{
				if (-2 == _state)
					Advance();
			}
			public bool IsEnded { get { return -1 == _state; } }
			public bool Advance()
			{
				if (!_e.MoveNext())
					_state = -1;
				else
				{
					_state = 0;
					return true;
				}
				return false;
			}
		}
		public static int Eval(IEnumerable<char> expr,V variables = null)
		{
			var tokenizer = new SampleTokenizer(expr);
			using (var e = tokenizer.GetEnumerator())
			{
				var parseContext = new _PC(e);
				parseContext.EnsureStarted();
				var result = _Eval(parseContext, variables);
				if (!parseContext.IsEnded)
					throw new Exception("Unexpected remainder in expression");
				return result;
			}
		}
		static int _Eval(_PC pc,V vars)
		{
			return _EvalTerm(pc, vars);
		}
		static int _EvalTerm(_PC pc, V vars)
		{
			var lhs = _EvalFactor(pc, vars);
			switch(pc.SymbolId)
			{
				case T.Plus:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting factor.");
					return lhs + _EvalFactor(pc, vars);
				case T.Minus:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting factor.");
					return lhs - _EvalFactor(pc, vars);
				default:
					return lhs;
			}
		}
		static int _EvalFactor(_PC pc,V vars)
		{
			var lhs = _EvalUnary(pc, vars);
			switch (pc.SymbolId)
			{
				case T.Multiply:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting unary.");
					return lhs * _EvalUnary(pc, vars);
				case T.Divide:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting unary.");
					return lhs / _EvalUnary(pc, vars);
				default:
					return lhs;
			}
		}
		static int _EvalUnary(_PC pc, V vars)
		{
			switch (pc.SymbolId)
			{
				case T.Plus:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting unary.");
					return _EvalUnary(pc, vars);
				case T.Minus:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting factor.");
					return - _EvalUnary(pc, vars);
				default:
					return _EvalLeaf(pc, vars);
			}
		}
		static int _EvalLeaf(_PC pc,V vars)
		{
			var val = pc.Value;
			switch(pc.SymbolId)
			{
				case T.Identifier:
					pc.Advance();
					return vars[val];
				case T.Integer:
					pc.Advance();
					return int.Parse(val);
				case T.LParen:
					pc.Advance();
					var e = _Eval(pc, vars);
					if (T.RParen != pc.SymbolId)
						throw new Exception("Unclosed ( in expression");
					pc.Advance();
					return e;
				default:
					throw new Exception("Unrecognized symbol");
			}
		}
	}
}
