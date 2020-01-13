using System;
using System.Collections.Generic;

namespace Parsley
{
	partial class ParserContext : Object, IDisposable
	{
		private int _state;
		private IEnumerator<object> _e;
		private LookAheadEnumerator<object> _el;
		private Token _t;
		private int _advanceCount;
		private int _line;
		private int _column;
		private long _position;
		private List<object> _skipped;
		public ParserContext(IEnumerable<object> tokenizer) : this(tokenizer.GetEnumerator(),true)
		{
			
		}
		
		private ParserContext(IEnumerator<object> enumerator,bool wrap)
		{
			_e = enumerator;
			if (wrap)
			{
				_el = new LookAheadEnumerator<object>(enumerator);
				_e = _el; // we need both pointers to point to the lookahead
			}
			_state = -1;
			_t.SymbolId = -1;
			_advanceCount = 0;
			_skipped = new List<object>();
		}
		public List<object> Skipped 
		{
			get {
				return _skipped;
			}
		}
		public void SetLocation(int line,int column,long position)
		{
			_line = line;
			_column = column;
			_position = position;
		}
		public void EnsureStarted()
		{
			if (-1 == _state)
				Advance();
		}
		public ParserContext GetLookAhead()
		{
			if (null == _el)
				throw new NotSupportedException("This parser context does not support lookahead.");
			var result = new ParserContext(_el.LookAhead.GetEnumerator(), true);
			return result;
		}
		public Token Current { get { return _t; } }
		public ParserContext GetLookAhead(bool start)
		{
			var result= GetLookAhead();
			if (start)
				result.EnsureStarted();
			return result;
		}
		public int AdvanceCount { get { return _advanceCount; } }
		public void ResetAdvanceCount() { _advanceCount = 0; }
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
				// sanity check. should never happen
				if (int.MaxValue == _advanceCount)
					_advanceCount = -1;
				++_advanceCount;
				_state = 0;
				_t = (Token)_e.Current;
				((Token)_t).Line += _line;
				((Token)_t).Column += _column;
				((Token)_t).Position += _position;
				if(null!=((Token)_t).Skipped)
					_skipped.AddRange(((Token)_t).Skipped);
				return true;
			}
			return false;
		}
		public void Error(string message, int line, int column,long position)
		{
			throw new SyntaxException(string.Format(message+" at line {0}, column {1}, position",line,column,position), line, column, position);
		}
		public void Error(string message)
		{
			Error(message, Line, Column, Position);
		}
		
		public void Dispose()
		{
			_e.Dispose();
			_state = -3;
		}
		
	}
}
