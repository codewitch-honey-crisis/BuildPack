using System;
using System.Collections.Generic;
using System.Text;

namespace Rolex
{
	/// <summary>
	/// Reference Implementation for generated shared code
	/// </summary>
	abstract class CompiledTokenizer : IEnumerable<Token>
	{
		// the input cursor. We can get this from a string, a char array, or some other source.
		protected IEnumerable<char> Input;
		// just create our table tokenizer's enumerator, passing all of the relevant stuff
		// it's the real workhorse.
		/// <summary>
		/// Retrieves an enumerator that can be used to iterate over the tokens
		/// </summary>
		/// <returns>An enumerator that can be used to iterate over the tokens</returns>
		public abstract IEnumerator<Token> GetEnumerator();


		// we have to implement this explicitly for language independence because Slang
		// will not set PublicImplementationTypes on public methods which some languages
		// require
		IEnumerator<Token> IEnumerable<Token>.GetEnumerator()
		{
			return GetEnumerator();
		}
		// legacy collection support (required)
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		/// <summary>
		/// Constructs a new instance
		/// </summary>
		/// <param name="input">The input character sequence</param>
		public CompiledTokenizer(IEnumerable<char> input)
		{
			if (null == input)
				throw new ArgumentNullException("input");
			Input = input;
		}
	}
	abstract class CompiledTokenizerEnumerator : IEnumerator<Token>
	{
		// our error symbol. Always -1
		public const int ErrorSymbol = -1;
		// our end of stream symbol - returned by Lex() and used internally but not reported
		protected const int EosSymbol = -2;
		// our disposed state indicator
	    protected const int Disposed = -4;
		// the state indicates the cursor is before the beginning (initial state)
		protected const int BeforeBegin = -3;
		// the state indicates the cursor is after the end
		protected const int AfterEnd = -2;
		// the state indicates that the inner input enumeration has finished (we still have one more token to report)
		protected const int InnerFinished = -1;
		// indicates we're currently enumerating. We spend most of our time and effort in this state
		protected const int Enumerating = 0;
		// indicates the tab width, used for updating the Column property when we encounter a tab
		private const int _TabWidth = 4;
		// the input cursor
		private IEnumerator<char> _input;
		// our state 
		protected int State;
		// the current token
		private Token _current;
		// a buffer used primarily by Lex() to capture matched input
		protected StringBuilder ValueBuffer;
		// the one based line
		private int _line;
		// the one based column
		private int _column;
		// the zero based position
		private long _position;
		protected CompiledTokenizerEnumerator(IEnumerator<char> input)
		{
			// just set up our initial values
			_input = input;
			State = BeforeBegin;
			ValueBuffer = new StringBuilder();
			_line = 1;
			_column = 1;
			_position = 0;
		}
		protected char CurrentInput { get { return _input.Current; } }
		public Token Current {
			get {
				// if we're not enumerating, find out what's going on
				if (Enumerating > State)
				{
					// check which state we're in, and throw accordingly
					if (BeforeBegin == State)
						throw new InvalidOperationException("The cursor is before the start of the enumeration");
					if (AfterEnd == State)
						throw new InvalidOperationException("The cursor is after the end of the enumeration");
					if (Disposed == State)
						_ThrowDisposed();
					// if we got here, the state is fine
				}
				return _current;
			}
		}
		object System.Collections.IEnumerator.Current {
			get { return Current; }
		}
		void System.Collections.IEnumerator.Reset()
		{
			if (Disposed == State)
				_ThrowDisposed();
			// don't reset if we're already before the beginning
			if (BeforeBegin != State)
				_input.Reset();
			// put our state back to the initial and reset the cursor position
			State = BeforeBegin;
			_line = 1;
			_column = 1;
			_position = 0L;
		}
		protected abstract bool IsHidden(int symbolId);
		protected abstract string GetBlockEnd(int symbolId);
		bool System.Collections.IEnumerator.MoveNext()
		{
			// if we're not enumerating
			if (Enumerating > State)
			{
				if (Disposed == State)
					_ThrowDisposed();
				if (AfterEnd == State)
					return false;
				// we're okay if we got here
			}
			_current = default(Token);
			_current.Line = _line;
			_current.Column = _column;
			_current.Position = _position;
			ValueBuffer.Clear();
			// lex the next input
			_current.SymbolId = Lex();
			// now look for hiddens and block ends
			var done = false;
			while (!done)
			{
				done = true;
				// if we're on a valid symbol
				if (ErrorSymbol < _current.SymbolId)
				{
					// get the block end for our symbol
					var be = GetBlockEnd(_current.SymbolId);
					// if it's valid
					if (null != be && 0 != be.Length)
					{
						// read until we find it or end of input
						if (!_TryReadUntilBlockEnd(be))
							_current.SymbolId = ErrorSymbol;
					}
					// node is hidden?
					if (IsHidden(Current.SymbolId))
					{
						// update the cursor position and lex the next input, skipping this one
						done = false;
						_current.Line = _line;
						_current.Column = _column;
						_current.Position = _position;
						ValueBuffer.Clear();
						_current.SymbolId = Lex();
					}
				}
			}
			// get what we captured
			_current.Value = ValueBuffer.ToString();
			// update our state if we hit the end
			if (EosSymbol == _current.SymbolId)
				State = AfterEnd;
			// return true if there's more to report
			return AfterEnd != State;
		}
		void IDisposable.Dispose()
		{
			_input.Dispose();
			State = Disposed;
		}
		// moves to the next position, updates the state accordingly, and tracks the cursor position
		protected bool MoveNextInput()
		{
			if (_input.MoveNext())
			{
				if (BeforeBegin != State)
				{
					++_position;
					if ('\n' == _input.Current)
					{
						_column = 1;
						++_line;
					}
					else if ('\t' == _input.Current)
						_column += _TabWidth;
					else
						++_column;
				}
				else
				{
					// corner case for first move
					if ('\n' == _input.Current)
					{
						_column = 1;
						++_line;
					}
					else if ('\t' == _input.Current)
						_column += _TabWidth - 1;
				}
				return true;
			}
			State = InnerFinished;
			return false;
		}
		// reads until the specified character, consuming it, returning false if it wasn't found
		bool _TryReadUntil(char character)
		{
			var ch = _input.Current;
			ValueBuffer.Append(ch);
			if (ch == character)
				return true;
			while (MoveNextInput() && _input.Current != character)
				ValueBuffer.Append(_input.Current);
			if (State != InnerFinished)
			{
				ValueBuffer.Append(_input.Current);
				return _input.Current == character;
			}
			return false;
		}
		// reads until the string is encountered, capturing it.
		bool
			_TryReadUntilBlockEnd(string blockEnd)
		{
			while (InnerFinished != State && _TryReadUntil(blockEnd[0]))
			{
				bool found = true;
				for (int i = 1; found && i < blockEnd.Length; ++i)
				{
					if (!MoveNextInput() || _input.Current != blockEnd[i])
						found = false;
					else if (InnerFinished != State)
						ValueBuffer.Append(_input.Current);
				}
				if (found)
				{
					MoveNextInput();
					return true;
				}
			}

			return false;
		}
		// lex the next token
		protected abstract int Lex();

		static void _ThrowDisposed()
		{
			throw new ObjectDisposedException("CompiledTokenizerEnumerator");
		}
	}
}
