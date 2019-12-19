using System;
using System.Collections.Generic;

namespace Parsley
{
	partial class ParserContext : Object, IDisposable
	{
		private int _state;
		private IEnumerator<object> _e;
		private Token _t;
		public ParserContext(IEnumerable<object> tokenizer)
		{
			_e = tokenizer.GetEnumerator();
			_state = -1;
			_t.SymbolId = -1;
		}
		public void EnsureStarted()
		{
			if (-1 == _state)
				Advance();
		}
		public int SymbolId { get { return _t.SymbolId; } }
		public string Value { get { return _t.Value; } }
		public int Line { get { return _t.Line; } }
		public int Column { get { return _t.Column; } }
		public long Position { get { return _t.Position; } }
		public bool IsEnded { get { return -2 == _state; } }
		public bool Advance()
		{
			if (!_e.MoveNext())
			{
				_t.SymbolId = -2;
				_state = -2;
			}
			else
			{
				_state = 0;
				_t = (Token)_e.Current;
				return true;
			}
			return false;
		}
		public void Error(string message, object arg1, object arg2,object arg3)
		{
			throw new SyntaxException(string.Format(message, arg1, arg2,arg3), Line, Column, Position);
		}
		public void Error(string message, object arg1,object arg2)
		{
			throw new SyntaxException(string.Format(message, arg1,arg2), Line, Column, Position);
		}
		public void Error(string message, object arg)
		{
			throw new SyntaxException(string.Format(message, arg), Line, Column, Position);
		}
		public void Error(string message)
		{
			throw new SyntaxException(message, Line, Column, Position);
		}
		public void Dispose()
		{
			_e.Dispose();
			_state = -3;
		}
		void IDisposable.Dispose()
		{
			Dispose();
		}
	}
}
