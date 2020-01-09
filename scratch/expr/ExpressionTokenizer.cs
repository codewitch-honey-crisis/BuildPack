//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace scratch.expr {
    using System;
    using System.Collections.Generic;
    using System.Text;
    
    /// <summary>
    /// Reference implementation for generated shared code
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.2.0.0")]
    internal struct Token {
        /// <summary>
        /// Indicates the line where the token occurs
        /// </summary>
        public int Line;
        /// <summary>
        /// Indicates the column where the token occurs
        /// </summary>
        public int Column;
        /// <summary>
        /// Indicates the position where the token occurs
        /// </summary>
        public long Position;
        /// <summary>
        /// Indicates the symbol id or -1 for the error symbol
        /// </summary>
        public int SymbolId;
        /// <summary>
        /// Indicates the value of the token
        /// </summary>
        public string Value;
        /// <summary>
        /// Always null in Rolex
        /// </summary>
        public Token[] Skipped;
    }
    /// <summary>
    /// Reference implementation for a DfaEntry
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.2.0.0")]
    internal struct DfaEntry {
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
        public DfaEntry(DfaTransitionEntry[] transitions, int acceptSymbolId) {
            this.Transitions = transitions;
            this.AcceptSymbolId = acceptSymbolId;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.2.0.0")]
    internal struct DfaTransitionEntry {
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
        public DfaTransitionEntry(char[] packedRanges, int destination) {
            this.PackedRanges = packedRanges;
            this.Destination = destination;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.2.0.0")]
    internal class TableTokenizer : object, IEnumerable<Token> {
        public const int ErrorSymbol = -1;
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
        public IEnumerator<Token> GetEnumerator() {
            // just create our table tokenizer's enumerator, passing all of the relevant stuff
            // it's the real workhorse.
            return new TableTokenizerEnumerator(this._dfaTable, this._blockEnds, this._nodeFlags, this._input.GetEnumerator());
        }
        // legacy collection support (required)
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="dfaTable">The DFA state table to use</param>
        /// <param name="blockEnds">The block ends table</param>
        /// <param name="nodeFlags">The node flags table</param>
        /// <param name="input">The input character sequence</param>
        public TableTokenizer(DfaEntry[] dfaTable, string[] blockEnds, int[] nodeFlags, IEnumerable<char> input) {
            if ((null == dfaTable)) {
                throw new ArgumentNullException("dfaTable");
            }
            if ((null == blockEnds)) {
                throw new ArgumentNullException("blockEnds");
            }
            if ((null == nodeFlags)) {
                throw new ArgumentNullException("nodeFlags");
            }
            if ((null == input)) {
                throw new ArgumentNullException("input");
            }
            this._dfaTable = dfaTable;
            this._blockEnds = blockEnds;
            this._nodeFlags = nodeFlags;
            this._input = input;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.2.0.0")]
    internal class TableTokenizerEnumerator : object, IEnumerator<Token> {
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
        public TableTokenizerEnumerator(DfaEntry[] dfaTable, string[] blockEnds, int[] nodeFlags, IEnumerator<char> input) {
            // just set up our initial values
            this._dfaTable = dfaTable;
            this._blockEnds = blockEnds;
            this._nodeFlags = nodeFlags;
            this._input = input;
            this._state = TableTokenizerEnumerator._BeforeBegin;
            this._buffer = new StringBuilder();
            this._line = 1;
            this._column = 1;
            this._position = 0;
        }
        public Token Current {
            get {
                // if we're not enumerating, find out what's going on
                if ((TableTokenizerEnumerator._Enumerating > this._state)) {
                    // check which state we're in, and throw accordingly
                    if ((TableTokenizerEnumerator._BeforeBegin == this._state)) {
                        throw new InvalidOperationException("The cursor is before the start of the enumeration");
                    }
                    if ((TableTokenizerEnumerator._AfterEnd == this._state)) {
                        throw new InvalidOperationException("The cursor is after the end of the enumeration");
                    }
                    if ((TableTokenizerEnumerator._Disposed == this._state)) {
                        TableTokenizerEnumerator._ThrowDisposed();
                    }
                }
                return this._current;
            }
        }
        object System.Collections.IEnumerator.Current {
            get {
                return this.Current;
            }
        }
        void System.Collections.IEnumerator.Reset() {
            if ((TableTokenizerEnumerator._Disposed == this._state)) {
                TableTokenizerEnumerator._ThrowDisposed();
            }
            if ((false 
                        == (TableTokenizerEnumerator._BeforeBegin == this._state))) {
                this._input.Reset();
            }
            this._state = TableTokenizerEnumerator._BeforeBegin;
            this._line = 1;
            this._column = 1;
            this._position = 0;
        }
        bool System.Collections.IEnumerator.MoveNext() {
            // if we're not enumerating
            if ((TableTokenizerEnumerator._Enumerating > this._state)) {
                if ((TableTokenizerEnumerator._Disposed == this._state)) {
                    TableTokenizerEnumerator._ThrowDisposed();
                }
                if ((TableTokenizerEnumerator._AfterEnd == this._state)) {
                    return false;
                }
            }
            this._current = default(Token);
            this._current.Line = this._line;
            this._current.Column = this._column;
            this._current.Position = this._position;
            this._current.Skipped = null;
            this._buffer.Clear();
            // lex the next input
            this._current.SymbolId = this._Lex();
            // now look for hiddens and block ends
            bool done = false;
            for (
            ; (false == done); 
            ) {
                done = true;
                // if we're on a valid symbol
                if ((TableTokenizerEnumerator.ErrorSymbol < this._current.SymbolId)) {
                    // get the block end for our symbol
                    string be = this._blockEnds[this._current.SymbolId];
                    if (((null != be) 
                                && (false 
                                == (0 == be.Length)))) {
                        // read until we find it or end of input
                        if ((false == this._TryReadUntilBlockEnd(be))) {
                            this._current.SymbolId = TableTokenizerEnumerator.ErrorSymbol;
                        }
                    }
                    if (((TableTokenizerEnumerator.ErrorSymbol < this._current.SymbolId) 
                                && (false 
                                == (0 
                                == (this._nodeFlags[this._current.SymbolId] & 1))))) {
                        // update the cursor position and lex the next input, skipping this one
                        done = false;
                        this._current.Line = this._line;
                        this._current.Column = this._column;
                        this._current.Position = this._position;
                        this._buffer.Clear();
                        this._current.SymbolId = this._Lex();
                    }
                }
            }
            this._current.Value = this._buffer.ToString();
            // update our state if we hit the end
            if ((TableTokenizerEnumerator._EosSymbol == this._current.SymbolId)) {
                this._state = TableTokenizerEnumerator._AfterEnd;
            }
            return (false 
                        == (TableTokenizerEnumerator._AfterEnd == this._state));
        }
        void IDisposable.Dispose() {
            this._input.Dispose();
            this._state = TableTokenizerEnumerator._Disposed;
        }
        // moves to the next position, updates the state accordingly, and tracks the cursor position
        bool _MoveNextInput() {
            if (this._input.MoveNext()) {
                if ((false 
                            == (TableTokenizerEnumerator._BeforeBegin == this._state))) {
                    this._position = (this._position + 1);
                    if (('\n' == this._input.Current)) {
                        this._column = 1;
                        this._line = (this._line + 1);
                    }
                    else {
                        if (('\t' == this._input.Current)) {
                            this._column = (this._column + TableTokenizerEnumerator._TabWidth);
                        }
                        else {
                            this._column = (this._column + 1);
                        }
                    }
                }
                else {
                    // corner case for first move
                    if (('\n' == this._input.Current)) {
                        this._column = 1;
                        this._line = (this._line + 1);
                    }
                    else {
                        if (('\t' == this._input.Current)) {
                            this._column = (this._column 
                                        + (TableTokenizerEnumerator._TabWidth - 1));
                        }
                    }
                }
                return true;
            }
            this._state = TableTokenizerEnumerator._InnerFinished;
            return false;
        }
        // reads until the specified character, consuming it, returning false if it wasn't found
        bool _TryReadUntil(char character) {
            char ch = this._input.Current;
            this._buffer.Append(ch);
            if ((ch == character)) {
                return true;
            }
            for (
            ; (this._MoveNextInput() 
                        && (false 
                        == (this._input.Current == character))); 
            ) {
                this._buffer.Append(this._input.Current);
            }
            if ((false 
                        == (this._state == TableTokenizerEnumerator._InnerFinished))) {
                this._buffer.Append(this._input.Current);
                return (this._input.Current == character);
            }
            return false;
        }
        // reads until the string is encountered, capturing it.
        bool _TryReadUntilBlockEnd(string blockEnd) {
            for (
            ; ((false 
                        == (TableTokenizerEnumerator._InnerFinished == this._state)) 
                        && this._TryReadUntil(blockEnd[0])); 
            ) {
                bool found = true;
                for (int i = 1; (found 
                            && (i < blockEnd.Length)); i = (i + 1)) {
                    if ((false 
                                == (this._MoveNextInput() 
                                || (false 
                                == (this._input.Current == blockEnd[i]))))) {
                        found = false;
                    }
                    else {
                        if ((false 
                                    == (TableTokenizerEnumerator._InnerFinished == this._state))) {
                            this._buffer.Append(this._input.Current);
                        }
                    }
                }
                if (found) {
                    this._MoveNextInput();
                    return true;
                }
            }
            return false;
        }
        // lex the next token
        int _Lex() {
            // our accepting symbol id
            int acceptSymbolId;
            int dfaState = 0;
            if ((TableTokenizerEnumerator._BeforeBegin == this._state)) {
                if ((false == this._MoveNextInput())) {
                    // if we're on an accepting state, return that
                    // otherwise, error
                    acceptSymbolId = this._dfaTable[dfaState].AcceptSymbolId;
                    if ((false 
                                == (-1 == acceptSymbolId))) {
                        return acceptSymbolId;
                    }
                    else {
                        return TableTokenizerEnumerator.ErrorSymbol;
                    }
                }
                this._state = TableTokenizerEnumerator._Enumerating;
            }
            else {
                if (((TableTokenizerEnumerator._InnerFinished == this._state) 
                            || (TableTokenizerEnumerator._AfterEnd == this._state))) {
                    // if we're at the end just return the end symbol
                    return TableTokenizerEnumerator._EosSymbol;
                }
            }
            bool done = false;
            for (
            ; (false == done); 
            ) {
                int nextDfaState = -1;
                for (int i = 0; (i < this._dfaTable[dfaState].Transitions.Length); i = (i + 1)) {
                    DfaTransitionEntry entry = this._dfaTable[dfaState].Transitions[i];
                    bool found = false;
                    for (int j = 0; (j < entry.PackedRanges.Length); j = (j + 1)) {
                        char ch = this._input.Current;
                        char first = entry.PackedRanges[j];
                        j = (j + 1);
                        char last = entry.PackedRanges[j];
                        if ((ch <= last)) {
                            if ((first <= ch)) {
                                found = true;
                            }
                            j = (int.MaxValue - 1);
                            // break
                        }
                    }
                    if (found) {
                        // set the transition destination
                        nextDfaState = entry.Destination;
                        i = (int.MaxValue - 1);
                        // break
                    }
                }
                if ((false 
                            == (-1 == nextDfaState))) {
                    // capture our character
                    this._buffer.Append(this._input.Current);
                    // and iterate to our next state
                    dfaState = nextDfaState;
                    if ((false == this._MoveNextInput())) {
                        // end of stream, if we're on an accepting state,
                        // return that, just like we do on empty string
                        // if we're not, then we error, just like before
                        acceptSymbolId = this._dfaTable[dfaState].AcceptSymbolId;
                        if ((false 
                                    == (-1 == acceptSymbolId))) {
                            return acceptSymbolId;
                        }
                        else {
                            return TableTokenizerEnumerator.ErrorSymbol;
                        }
                    }
                }
                else {
                    done = true;
                }
                // no valid transition, we can exit the loop
            }
            acceptSymbolId = this._dfaTable[dfaState].AcceptSymbolId;
            if ((false 
                        == (-1 == acceptSymbolId))) {
                return acceptSymbolId;
            }
            else {
                // handle the error condition
                // we have to capture the input
                // here and then advance or the
                // machine will never halt
                this._buffer.Append(this._input.Current);
                this._MoveNextInput();
                return TableTokenizerEnumerator.ErrorSymbol;
            }
        }
        static void _ThrowDisposed() {
            throw new ObjectDisposedException("TableTokenizerEnumerator");
        }
    }
    internal class ExpressionTokenizer : TableTokenizer {
        internal static DfaEntry[] DfaTable = new DfaEntry[] {
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new char[] {
                                        '\t',
                                        '\r',
                                        ' ',
                                        ' '}, 1),
                            new DfaTransitionEntry(new char[] {
                                        'A',
                                        'Z',
                                        '_',
                                        '_',
                                        'a',
                                        'z'}, 2),
                            new DfaTransitionEntry(new char[] {
                                        '0',
                                        '9'}, 4),
                            new DfaTransitionEntry(new char[] {
                                        ')',
                                        ')'}, 5),
                            new DfaTransitionEntry(new char[] {
                                        '(',
                                        '('}, 6),
                            new DfaTransitionEntry(new char[] {
                                        '/',
                                        '/'}, 7),
                            new DfaTransitionEntry(new char[] {
                                        '*',
                                        '*'}, 8),
                            new DfaTransitionEntry(new char[] {
                                        '-',
                                        '-'}, 9),
                            new DfaTransitionEntry(new char[] {
                                        '+',
                                        '+'}, 10)}, -1),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new char[] {
                                        '\t',
                                        '\r',
                                        ' ',
                                        ' '}, 1)}, 20),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new char[] {
                                        '0',
                                        '9',
                                        'A',
                                        'Z',
                                        '_',
                                        '_',
                                        'a',
                                        'z'}, 3)}, 19),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new char[] {
                                        '0',
                                        '9',
                                        'A',
                                        'Z',
                                        '_',
                                        '_',
                                        'a',
                                        'z'}, 3)}, 19),
                new DfaEntry(new DfaTransitionEntry[] {
                            new DfaTransitionEntry(new char[] {
                                        '0',
                                        '9'}, 4)}, 18),
                new DfaEntry(new DfaTransitionEntry[0], 17),
                new DfaEntry(new DfaTransitionEntry[0], 16),
                new DfaEntry(new DfaTransitionEntry[0], 15),
                new DfaEntry(new DfaTransitionEntry[0], 14),
                new DfaEntry(new DfaTransitionEntry[0], 13),
                new DfaEntry(new DfaTransitionEntry[0], 12)};
        internal static int[] NodeFlags = new int[] {
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                0,
                1};
        internal static string[] BlockEnds = new string[] {
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null,
                null};
        public ExpressionTokenizer(IEnumerable<char> input) : 
                base(ExpressionTokenizer.DfaTable, ExpressionTokenizer.BlockEnds, ExpressionTokenizer.NodeFlags, input) {
        }
        public const int add = 12;
        public const int sub = 13;
        public const int mul = 14;
        public const int div = 15;
        public const int lparen = 16;
        public const int rparen = 17;
        public const int integer = 18;
        public const int identifier = 19;
        public const int whitespace = 20;
    }
}
