//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace RolexDemo {
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
    /// Reference Implementation for generated shared code
    /// </summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.2.0.0")]
    internal abstract class CompiledTokenizer : IEnumerable<Token> {
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
        IEnumerator<Token> IEnumerable<Token>.GetEnumerator() {
            return this.GetEnumerator();
        }
        // legacy collection support (required)
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return this.GetEnumerator();
        }
        /// <summary>
        /// Constructs a new instance
        /// </summary>
        /// <param name="input">The input character sequence</param>
        public CompiledTokenizer(IEnumerable<char> input) {
            if ((null == input)) {
                throw new ArgumentNullException("input");
            }
            this.Input = input;
        }
    }
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex", "0.2.0.0")]
    internal abstract class CompiledTokenizerEnumerator : IEnumerator<Token> {
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
        protected CompiledTokenizerEnumerator(IEnumerator<char> input) {
            // just set up our initial values
            this._input = input;
            this.State = CompiledTokenizerEnumerator.BeforeBegin;
            this.ValueBuffer = new StringBuilder();
            this._line = 1;
            this._column = 1;
            this._position = 0;
        }
        protected virtual char CurrentInput {
            get {
                return this._input.Current;
            }
        }
        public Token Current {
            get {
                // if we're not enumerating, find out what's going on
                if ((CompiledTokenizerEnumerator.Enumerating > this.State)) {
                    // check which state we're in, and throw accordingly
                    if ((CompiledTokenizerEnumerator.BeforeBegin == this.State)) {
                        throw new InvalidOperationException("The cursor is before the start of the enumeration");
                    }
                    if ((CompiledTokenizerEnumerator.AfterEnd == this.State)) {
                        throw new InvalidOperationException("The cursor is after the end of the enumeration");
                    }
                    if ((CompiledTokenizerEnumerator.Disposed == this.State)) {
                        CompiledTokenizerEnumerator._ThrowDisposed();
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
            if ((CompiledTokenizerEnumerator.Disposed == this.State)) {
                CompiledTokenizerEnumerator._ThrowDisposed();
            }
            if ((false 
                        == (CompiledTokenizerEnumerator.BeforeBegin == this.State))) {
                this._input.Reset();
            }
            this.State = CompiledTokenizerEnumerator.BeforeBegin;
            this._line = 1;
            this._column = 1;
            this._position = 0;
        }
        protected abstract bool IsHidden(int symbolId);
        protected abstract string GetBlockEnd(int symbolId);
        bool System.Collections.IEnumerator.MoveNext() {
            // if we're not enumerating
            if ((CompiledTokenizerEnumerator.Enumerating > this.State)) {
                if ((CompiledTokenizerEnumerator.Disposed == this.State)) {
                    CompiledTokenizerEnumerator._ThrowDisposed();
                }
                if ((CompiledTokenizerEnumerator.AfterEnd == this.State)) {
                    return false;
                }
            }
            this._current = default(Token);
            this._current.Line = this._line;
            this._current.Column = this._column;
            this._current.Position = this._position;
            this.ValueBuffer.Clear();
            // lex the next input
            this._current.SymbolId = this.Lex();
            // now look for hiddens and block ends
            bool done = false;
            for (
            ; (false == done); 
            ) {
                done = true;
                // if we're on a valid symbol
                if ((CompiledTokenizerEnumerator.ErrorSymbol < this._current.SymbolId)) {
                    // get the block end for our symbol
                    string be = this.GetBlockEnd(this._current.SymbolId);
                    if (((null != be) 
                                && (false 
                                == (0 == be.Length)))) {
                        // read until we find it or end of input
                        if ((false == this._TryReadUntilBlockEnd(be))) {
                            this._current.SymbolId = CompiledTokenizerEnumerator.ErrorSymbol;
                        }
                    }
                    if (this.IsHidden(this.Current.SymbolId)) {
                        // update the cursor position and lex the next input, skipping this one
                        done = false;
                        this._current.Line = this._line;
                        this._current.Column = this._column;
                        this._current.Position = this._position;
                        this._current.Skipped = null;
                        this.ValueBuffer.Clear();
                        this._current.SymbolId = this.Lex();
                    }
                }
            }
            this._current.Value = this.ValueBuffer.ToString();
            // update our state if we hit the end
            if ((CompiledTokenizerEnumerator.EosSymbol == this._current.SymbolId)) {
                this.State = CompiledTokenizerEnumerator.AfterEnd;
            }
            return (false 
                        == (CompiledTokenizerEnumerator.AfterEnd == this.State));
        }
        void IDisposable.Dispose() {
            this._input.Dispose();
            this.State = CompiledTokenizerEnumerator.Disposed;
        }
        // moves to the next position, updates the state accordingly, and tracks the cursor position
        protected virtual bool MoveNextInput() {
            if (this._input.MoveNext()) {
                if ((false 
                            == (CompiledTokenizerEnumerator.BeforeBegin == this.State))) {
                    this._position = (this._position + 1);
                    if (('\n' == this._input.Current)) {
                        this._column = 1;
                        this._line = (this._line + 1);
                    }
                    else {
                        if (('\t' == this._input.Current)) {
                            this._column = (this._column + CompiledTokenizerEnumerator._TabWidth);
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
                                        + (CompiledTokenizerEnumerator._TabWidth - 1));
                        }
                    }
                }
                return true;
            }
            this.State = CompiledTokenizerEnumerator.InnerFinished;
            return false;
        }
        // reads until the specified character, consuming it, returning false if it wasn't found
        bool _TryReadUntil(char character) {
            char ch = this._input.Current;
            this.ValueBuffer.Append(ch);
            if ((ch == character)) {
                return true;
            }
            for (
            ; (this.MoveNextInput() 
                        && (false 
                        == (this._input.Current == character))); 
            ) {
                this.ValueBuffer.Append(this._input.Current);
            }
            if ((false 
                        == (this.State == CompiledTokenizerEnumerator.InnerFinished))) {
                this.ValueBuffer.Append(this._input.Current);
                return (this._input.Current == character);
            }
            return false;
        }
        // reads until the string is encountered, capturing it.
        bool _TryReadUntilBlockEnd(string blockEnd) {
            for (
            ; ((false 
                        == (CompiledTokenizerEnumerator.InnerFinished == this.State)) 
                        && this._TryReadUntil(blockEnd[0])); 
            ) {
                bool found = true;
                for (int i = 1; (found 
                            && (i < blockEnd.Length)); i = (i + 1)) {
                    if ((false 
                                == (this.MoveNextInput() 
                                || (false 
                                == (this._input.Current == blockEnd[i]))))) {
                        found = false;
                    }
                    else {
                        if ((false 
                                    == (CompiledTokenizerEnumerator.InnerFinished == this.State))) {
                            this.ValueBuffer.Append(this._input.Current);
                        }
                    }
                }
                if (found) {
                    this.MoveNextInput();
                    return true;
                }
            }
            return false;
        }
        // lex the next token
        protected abstract int Lex();
        static void _ThrowDisposed() {
            throw new ObjectDisposedException("CompiledTokenizerEnumerator");
        }
    }
    internal class SampleTokenizer : CompiledTokenizer {
        public override IEnumerator<Token> GetEnumerator() {
            return new SampleTokenizerEnumerator(this.Input.GetEnumerator());
        }
        public SampleTokenizer(IEnumerable<char> input) : 
                base(input) {
        }
        public const int Identifier = 1;
        public const int Integer = 2;
        public const int Plus = 3;
        public const int Minus = 4;
        public const int Multiply = 5;
        public const int Divide = 6;
        public const int LParen = 7;
        public const int RParen = 8;
        public const int Whitespace = 9;
        public const int LineComment = 10;
        public const int BlockComment = 11;
    }
    internal class SampleTokenizerEnumerator : CompiledTokenizerEnumerator {
        public SampleTokenizerEnumerator(IEnumerator<char> input) : 
                base(input) {
        }
        protected override int Lex() {
            char current;
            if ((CompiledTokenizerEnumerator.BeforeBegin == this.State)) {
                if ((false == this.MoveNextInput())) {
                    return CompiledTokenizerEnumerator.ErrorSymbol;
                }
                this.State = CompiledTokenizerEnumerator.Enumerating;
            }
            else {
                if (((this.State == CompiledTokenizerEnumerator.InnerFinished) 
                            || (this.State == CompiledTokenizerEnumerator.AfterEnd))) {
                    return CompiledTokenizerEnumerator.EosSymbol;
                }
            }
            current = this.CurrentInput;
            // q0
            if (((((current >= 'A') 
                        && (current <= 'Z')) 
                        || (current == '_')) 
                        || ((current >= 'a') 
                        && (current <= 'z')))) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Identifier;
                }
                current = this.CurrentInput;
                goto q1;
            }
            if (((current >= '0') 
                        && (current <= '9'))) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Integer;
                }
                current = this.CurrentInput;
                goto q3;
            }
            if ((current == '+')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Plus;
                }
                current = this.CurrentInput;
                goto q4;
            }
            if ((current == '-')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Minus;
                }
                current = this.CurrentInput;
                goto q5;
            }
            if ((current == '*')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Multiply;
                }
                current = this.CurrentInput;
                goto q6;
            }
            if ((current == '/')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Divide;
                }
                current = this.CurrentInput;
                goto q7;
            }
            if ((current == '(')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.LParen;
                }
                current = this.CurrentInput;
                goto q10;
            }
            if ((current == ')')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.RParen;
                }
                current = this.CurrentInput;
                goto q11;
            }
            if ((((current >= '\t') 
                        && (current <= '\r')) 
                        || (current == ' '))) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Whitespace;
                }
                current = this.CurrentInput;
                goto q12;
            }
            goto error;
        q1:
            if ((((((current >= '0') 
                        && (current <= '9')) 
                        || ((current >= 'A') 
                        && (current <= 'Z'))) 
                        || (current == '_')) 
                        || ((current >= 'a') 
                        && (current <= 'z')))) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Identifier;
                }
                current = this.CurrentInput;
                goto q2;
            }
            return SampleTokenizer.Identifier;
        q2:
            if ((((((current >= '0') 
                        && (current <= '9')) 
                        || ((current >= 'A') 
                        && (current <= 'Z'))) 
                        || (current == '_')) 
                        || ((current >= 'a') 
                        && (current <= 'z')))) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Identifier;
                }
                current = this.CurrentInput;
                goto q2;
            }
            return SampleTokenizer.Identifier;
        q3:
            if (((current >= '0') 
                        && (current <= '9'))) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.Integer;
                }
                current = this.CurrentInput;
                goto q3;
            }
            return SampleTokenizer.Integer;
        q4:
            return SampleTokenizer.Plus;
        q5:
            return SampleTokenizer.Minus;
        q6:
            return SampleTokenizer.Multiply;
        q7:
            if ((current == '/')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.LineComment;
                }
                current = this.CurrentInput;
                goto q8;
            }
            if ((current == '*')) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.BlockComment;
                }
                current = this.CurrentInput;
                goto q9;
            }
            return SampleTokenizer.Divide;
        q8:
            if ((((current >= '\0') 
                        && (current <= '\t')) 
                        || ((current >= '') 
                        && (current <= '￿')))) {
                this.ValueBuffer.Append(current);
                if ((false == this.MoveNextInput())) {
                    return SampleTokenizer.LineComment;
                }
                current = this.CurrentInput;
                goto q8;
            }
            return SampleTokenizer.LineComment;
        q9:
            return SampleTokenizer.BlockComment;
        q10:
            return SampleTokenizer.LParen;
        q11:
            return SampleTokenizer.RParen;
        q12:
            return SampleTokenizer.Whitespace;
        error:
            this.ValueBuffer.Append(current);
            this.MoveNextInput();
            return CompiledTokenizerEnumerator.ErrorSymbol;
        }
        protected override string GetBlockEnd(int symbolId) {
            if ((SampleTokenizer.BlockComment == symbolId)) {
                return "*/";
            }
            return null;
        }
        protected override bool IsHidden(int symbolId) {
            return (((SampleTokenizer.Whitespace == symbolId) 
                        || (SampleTokenizer.LineComment == symbolId)) 
                        || (SampleTokenizer.BlockComment == symbolId));
        }
    }
}
