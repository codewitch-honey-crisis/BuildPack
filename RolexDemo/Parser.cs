using System;
using System.Collections.Generic;

namespace RolexDemo
{
	// save typing
	using T = SampleTokenizer;
	using VARS = IDictionary<string, int>;

	// a simple expression parser and evaluator
	class Parser
	{
		// This little parse context class just makes 
		// our parsing a little bit easier. I tend to
		// use something like this over IEnumerator<T>
		// interfaces
		private class _PC
		{
			readonly IEnumerator<Token> _e;
			int _state;
			public _PC(IEnumerator<Token> e)
			{
				_e = e;
				_state = -2;
			}
			// our current symbol. Match with T.ConstantName
			public int SymbolId => Current.SymbolId;
			// our current value
			public string Value => Current.Value;
			// our current token
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
			// make sure we're past the initial advance so the cursor is valid
			public void EnsureStarted()
			{
				if (-2 == _state)
					Advance();
			}
			// reports whether the input cursor is past the end
			public bool IsEnded { get { return -1 == _state; } }
			// advances by one 
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
		// evaluate an expression of the form
		// Expr -> Term
		// Term -> Factor (+|-) Factor | Factor
		// Factor -> Unary (*|/) Unary | Unary
		// Unary -> (+|-) Unary | Leaf
		// Leaf -> Identifier | Integer | ( Expr )
		public static int Eval(IEnumerable<char> expr,VARS variables = null)
		{
			// This routine just sets up the prerequisites for the parse
			// the actual parse starts in _Eval()

			// create a tokenizer and wrap it with a parse context
			var tokenizer = new SampleTokenizer(expr);
			using (var e = tokenizer.GetEnumerator())
			{
				var parseContext = new _PC(e);
				parseContext.EnsureStarted(); // kick start it
				// do our evaluation
				var result = _EvalExpr(parseContext, variables);
				// something went wrong if we didn't finish parsing
				if (!parseContext.IsEnded)
					throw new Exception("Unexpected remainder in expression");
				return result;
			}
		}

		// Expr -> Term
		static int _EvalExpr(_PC pc,VARS vars)
		{
			return _EvalTerm(pc, vars);
		}
		// Term -> Factor (+|-) Factor | Factor
		static int _EvalTerm(_PC pc, VARS vars)
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
		// Factor -> Unary (*|/) Unary | Unary
		static int _EvalFactor(_PC pc,VARS vars)
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
		// Unary -> (+|-) Unary | Leaf
		static int _EvalUnary(_PC pc, VARS vars)
		{
			switch (pc.SymbolId)
			{
				case T.Plus:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting unary.");
					return _EvalUnary(pc, vars);
				case T.Minus:
					if (!pc.Advance())
						throw new Exception("Unterminated expression. Expecting unary.");
					return - _EvalUnary(pc, vars);
				default:
					return _EvalLeaf(pc, vars);
			}
		}
		// Leaf -> Identifier | Integer | ( Expr )
		static int _EvalLeaf(_PC pc,VARS vars)
		{
			var val = pc.Value;
			switch(pc.SymbolId)
			{
				case T.Identifier:
					pc.Advance();
					if (null == vars)
						throw new Exception("No variables were declared, but variables were referenced");
					int result;
					if (!vars.TryGetValue(val, out result))
						throw new Exception(string.Format("Reference to undefined variable {0}", val));
					return result;
				case T.Integer:
					pc.Advance();
					return int.Parse(val);
				case T.LParen:
					pc.Advance();
					var e = _EvalExpr(pc, vars);
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
