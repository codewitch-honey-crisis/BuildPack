using System;
using System.Collections.Generic;
using System.Text;

namespace Rolex
{

	/// <summary>
	/// Reference implementation for a DfaEntry
	/// </summary>
	struct DfaEntry
	{
		/// <summary>
		/// The state transitions
		/// </summary>
		public DfaTransitionEntry[] Transitions;
		/// <summary>
		/// The accept symbol id or -1 for non-accepting
		/// </summary>
		public int AcceptSymbolId;
		/// <summary>
		/// Constructs a new instance
		/// </summary>
		/// <param name="transitions">The state transitions</param>
		/// <param name="acceptSymbolId">The accept symbol id</param>
		public DfaEntry(DfaTransitionEntry[] transitions, int acceptSymbolId)
		{
			Transitions = transitions;
			AcceptSymbolId = acceptSymbolId;
		}
	}
	/// <summary>
	/// The state transition entry
	/// </summary>
	struct DfaTransitionEntry
	{
		/// <summary>
		/// The character ranges, packed as adjacent pairs.
		/// </summary>
		public char[] PackedRanges;
		/// <summary>
		/// The destination state
		/// </summary>
		public int Destination;
		/// <summary>
		/// Constructs a new instance
		/// </summary>
		/// <param name="packedRanges">The packed character ranges</param>
		/// <param name="destination">The destination state</param>
		public DfaTransitionEntry(char[] packedRanges, int destination)
		{
			PackedRanges = packedRanges;
			Destination = destination;
		}
	}
	/// <summary>
	/// Reference Implementation for generated shared code
	/// </summary>
	class TableTokenizer : object, IEnumerable<Token>
	{
		public const int ErrorSymbol= -1;
		// our state table
		private DfaEntry[] _dfaTable;
		// our block ends (specified like comment<blockEnd="*/">="/*" in a rolex spec file)
		private string[] _blockEnds;
		// our node flags. Currently only used for the hidden attribute
		private int[] _nodeFlags;
		// the input cursor. We can get this from a string, a char array, or some other source.
		private IEnumerable<char> _input;
		/// <summary>
		/// Retrieves an enumerator that can be used to iterate over the tokens
		/// </summary>
		/// <returns>An enumerator that can be used to iterate over the tokens</returns>
		public IEnumerator<Token> GetEnumerator()
		{
			// just create our table tokenizer's enumerator, passing all of the relevant stuff
			// it's the real workhorse.
			return new TableTokenizerEnumerator(_dfaTable, _blockEnds, _nodeFlags, _input.GetEnumerator());
		}
		// legacy collection support (required)
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
		/// <summary>
		/// Constructs a new instance
		/// </summary>
		/// <param name="dfaTable">The DFA state table to use</param>
		/// <param name="blockEnds">The block ends table</param>
		/// <param name="nodeFlags">The node flags table</param>
		/// <param name="input">The input character sequence</param>
		public TableTokenizer(DfaEntry[] dfaTable, string[] blockEnds, int[] nodeFlags, IEnumerable<char> input)
		{
			if (null == dfaTable)
				throw new ArgumentNullException("dfaTable");
			if (null == blockEnds)
				throw new ArgumentNullException("blockEnds");
			if (null == nodeFlags)
				throw new ArgumentNullException("nodeFlags");
			if (null == input)
				throw new ArgumentNullException("input");
			_dfaTable = dfaTable;
			_blockEnds = blockEnds;
			_nodeFlags = nodeFlags;
			_input = input;
		}
	}
	class TableTokenizerEnumerator : object, IEnumerator<Token>
	{
		// our error symbol. Always -1
		public const int ErrorSymbol = -1;
		// our end of stream symbol - returned by _Lex() and used internally but not reported
		private const int _EosSymbol = -2;
		// our disposed state indicator
		private const int _Disposed = -4;
		// the state indicates the cursor is before the beginning (initial state)
		private const int _BeforeBegin = -3;
		// the state indicates the cursor is after the end
		private const int _AfterEnd = -2;
		// the state indicates that the inner input enumeration has finished (we still have one more token to report)
		private const int _InnerFinished = -1;
		// indicates we're currently enumerating. We spend most of our time and effort in this state
		private const int _Enumerating = 0;
		// indicates the tab width, used for updating the Column property when we encounter a tab
		private const int _TabWidth = 4;
		// the DFA state table to use.
		private DfaEntry[] _dfaTable;
		// the blockEnds to use
		private string[] _blockEnds;
		// the nodeFlags to use
		private int[] _nodeFlags;
		// the input cursor
		private IEnumerator<char> _input;
		// our state 
		private int _state;
		// the current token
		private Token _current;
		// a buffer used primarily by _Lex() to capture matched input
		private StringBuilder _buffer;
		// the one based line
		private int _line;
		// the one based column
		private int _column;
		// the zero based position
		private long _position;
		public TableTokenizerEnumerator(DfaEntry[] dfaTable, string[] blockEnds, int[] nodeFlags, IEnumerator<char> input)
		{
			// just set up our initial values
			_dfaTable = dfaTable;
			_blockEnds = blockEnds;
			_nodeFlags = nodeFlags;
			_input = input;
			_state = _BeforeBegin;
			_buffer = new StringBuilder();
			_line = 1;
			_column = 1;
			_position = 0;
		}
		public Token Current {
			get {
				// if we're not enumerating, find out what's going on
				if (_Enumerating > _state)
				{
					// check which state we're in, and throw accordingly
					if (_BeforeBegin == _state)
						throw new InvalidOperationException("The cursor is before the start of the enumeration");
					if (_AfterEnd == _state)
						throw new InvalidOperationException("The cursor is after the end of the enumeration");
					if (_Disposed == _state)
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
			if (_Disposed == _state)
				_ThrowDisposed();
			// don't reset if we're already before the beginning
			if (_BeforeBegin != _state)
				_input.Reset();
			// put our state back to the initial and reset the cursor position
			_state = _BeforeBegin;
			_line = 1;
			_column = 1;
			_position = 0L;
		}
		bool System.Collections.IEnumerator.MoveNext()
		{
			// if we're not enumerating
			if (_Enumerating > _state)
			{
				if (_Disposed == _state)
					_ThrowDisposed();
				if (_AfterEnd == _state)
					return false;
				// we're okay if we got here
			}
			_current = default(Token);
			_current.Line = _line;
			_current.Column = _column;
			_current.Position = _position;
			_current.Skipped = null;
			_buffer.Clear();
			// lex the next input
			_current.SymbolId = _Lex();
			// now look for hiddens and block ends
			var done = false;
			while (!done)
			{
				done = true;
				// if we're on a valid symbol
				if (ErrorSymbol < _current.SymbolId)
				{
					// get the block end for our symbol
					var be = _blockEnds[_current.SymbolId];
					// if it's valid
					if (null != be && 0 != be.Length)
					{
						// read until we find it or end of input
						if (!_TryReadUntilBlockEnd(be))
							_current.SymbolId = ErrorSymbol;
					}
					// node is hidden?
					if (ErrorSymbol < _current.SymbolId && 0 != (_nodeFlags[_current.SymbolId] & 1))
					{
						// update the cursor position and lex the next input, skipping this one
						done = false;
						_current.Line = _line;
						_current.Column = _column;
						_current.Position = _position;
						_buffer.Clear();
						_current.SymbolId = _Lex();
					}
				}
			}
			// get what we captured
			_current.Value = _buffer.ToString();
			// update our state if we hit the end
			if (_EosSymbol == _current.SymbolId)
				_state = _AfterEnd;
			// return true if there's more to report
			return _AfterEnd != _state;
		}
		void IDisposable.Dispose()
		{
			_input.Dispose();
			_state = _Disposed;
		}
		// moves to the next position, updates the state accordingly, and tracks the cursor position
		bool _MoveNextInput()
		{
			if (_input.MoveNext())
			{
				if (_BeforeBegin != _state)
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
			_state = _InnerFinished;
			return false;
		}
		// reads until the specified character, consuming it, returning false if it wasn't found
		bool _TryReadUntil(char character)
		{
			var ch = _input.Current;
			_buffer.Append(ch);
			if (ch == character)
				return true;
			while (_MoveNextInput() && _input.Current != character)
				_buffer.Append(_input.Current);
			if (_state != _InnerFinished)
			{
				_buffer.Append(_input.Current);
				return _input.Current == character;
			}
			return false;
		}
		// reads until the string is encountered, capturing it.
		bool 
			_TryReadUntilBlockEnd(string blockEnd)
		{
			while (_InnerFinished != _state && _TryReadUntil(blockEnd[0]))
			{
				bool found = true;
				for (int i = 1; found && i < blockEnd.Length; ++i)
				{
					if (!_MoveNextInput() || _input.Current != blockEnd[i])
						found = false;
					else if (_InnerFinished != _state)
						_buffer.Append(_input.Current);
				}
				if (found)
				{
					_MoveNextInput();
					return true;
				}
			}

			return false;
		}
		// lex the next token
		int _Lex()
		{
			// our accepting symbol id
			int acceptSymbolId;
			// the DFA state we're currently in (start at zero)
			var dfaState = 0;
			// corner case for beginning
			if (_BeforeBegin == _state)
			{
				if (!_MoveNextInput()) // empty input.
				{
					// if we're on an accepting state, return that
					// otherwise, error
					acceptSymbolId = _dfaTable[dfaState].AcceptSymbolId;
					if (-1 != acceptSymbolId)
						return acceptSymbolId;
					else
						return ErrorSymbol;
				}
				// we're enumerating now
				_state = _Enumerating;
			}
			else if (_InnerFinished == _state || _AfterEnd == _state)
			{
				// if we're at the end just return the end symbol
				return _EosSymbol;
			}
			// Here's where we run most of the match. we run one interation of the DFA state machine.
			// We match until we can't match anymore (greedy matching) and then report the symbol of the last 
			// match we found, or an error ("#ERROR") if we couldn't find one.
			var done = false;
			while (!done)
			{
				var nextDfaState = -1;
				// go through all the transitions
				for (var i = 0; i < _dfaTable[dfaState].Transitions.Length; ++i)
				{
					var entry = _dfaTable[dfaState].Transitions[i];
					var found = false;
					// go through all the ranges to see if we matched anything.
					for (var j = 0; j < entry.PackedRanges.Length; ++j)
					{
						var ch = _input.Current;
						// grab our range from the packed ranges into first and last
						var first = entry.PackedRanges[j];
						++j;
						var last = entry.PackedRanges[j];
						// do a quick search through our ranges
						if (ch <= last)
						{
							if (first <= ch)
								found = true;
							j = int.MaxValue - 1; // break
						}
					}
					if (found)
					{
						// set the transition destination
						nextDfaState = entry.Destination;
						i = int.MaxValue - 1; // break
					}
				}

				if (-1 != nextDfaState) // found a valid transition
				{
					// capture our character
					_buffer.Append(_input.Current);
					// and iterate to our next state
					dfaState = nextDfaState;
					if (!_MoveNextInput())
					{
						// end of stream, if we're on an accepting state,
						// return that, just like we do on empty string
						// if we're not, then we error, just like before
						acceptSymbolId = _dfaTable[dfaState].AcceptSymbolId;
						if (-1 != acceptSymbolId) // do we accept?
							return acceptSymbolId;
						else
							return ErrorSymbol;
					}
				}
				else
					done = true; // no valid transition, we can exit the loop
			}
			// once again, if the state we're on is accepting, return that
			// otherwise, error, almost as before with one minor exception
			acceptSymbolId = _dfaTable[dfaState].AcceptSymbolId;
			if (-1 != acceptSymbolId)
			{
				return acceptSymbolId;
			}
			else
			{
				// handle the error condition
				// we have to capture the input 
				// here and then advance or the 
				// machine will never halt
				_buffer.Append(_input.Current);
				_MoveNextInput();
				return ErrorSymbol;
			}
		}
		static void _ThrowDisposed()
		{
			throw new ObjectDisposedException("TableTokenizerEnumerator");
		}
	}
}
