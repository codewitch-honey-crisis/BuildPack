//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace ParsleyDemo {
    using System;
    using System.Collections.Generic;
    
    /// <summary>
    /// 
    /// </summary>
    internal class SyntaxException : Exception {
        int _line;
        int _column;
        long _position;
        /// <summary>
        /// Creates a syntax exception with the specified arguments
        /// </summary>
        /// <param name="message">The error message</param>
        /// <param name="line">The line where the error occurred</param>
        /// <param name="column">The column where the error occured</param>
        /// <param name="position">The position where the error occured</param>
        public SyntaxException(string message, int line, int column, long position) : 
                base(SyntaxException._GetMessage(message, line, column, position)) {
            this._line = line;
            this._column = column;
            this._position = position;
        }
        /// <summary>
        /// The line where the error occurred
        /// </summary>
        public int Line {
            get {
                return this._line;
            }
        }
        /// <summary>
        /// The column where the error occurred
        /// </summary>
        public int Column {
            get {
                return this._column;
            }
        }
        /// <summary>
        /// The position where the error occurred
        /// </summary>
        public long Position {
            get {
                return this._position;
            }
        }
        static string _GetMessage(string message, int line, int column, long position) {
            return string.Format("{0} at line {1}, column {2}, position {3}", message, line, column, position);
        }
    }
    internal partial class ParseNode {
        int _symbolId;
        string _symbol;
        string _value;
        int _line;
        int _column;
        long _position;
        ParseNode[] _children;
        public ParseNode(int symbolId, string symbol, ParseNode[] children, int line, int column, long position) {
            this._symbolId = symbolId;
            this._symbol = symbol;
            this._value = null;
            this._children = children;
            this._line = line;
            this._column = column;
            this._position = position;
        }
        public ParseNode(int symbolId, string symbol, string value, int line, int column, long position) {
            this._symbolId = symbolId;
            this._symbol = symbol;
            this._value = value;
            this._children = null;
            this._line = line;
            this._column = column;
            this._position = position;
        }
        public bool IsNonTerminal {
            get {
                return (false 
                            == (null == this._children));
            }
        }
        public ParseNode[] Children {
            get {
                return this._children;
            }
        }
        public int SymbolId {
            get {
                return this._symbolId;
            }
        }
        public string Symbol {
            get {
                return this._symbol;
            }
        }
        public string Value {
            get {
                return this._value;
            }
        }
        public int Line {
            get {
                return this._line;
            }
        }
        public int Column {
            get {
                return this._column;
            }
        }
        public long Position {
            get {
                return this._position;
            }
        }
    }
    internal partial class ParserContext : IDisposable {
        int _state;
        IEnumerator<Token> _e;
        Token _t;
        public ParserContext(IEnumerable<Token> tokenizer) {
            this._e = tokenizer.GetEnumerator();
            this._state = -1;
            this._t.SymbolId = -1;
        }
        public void EnsureStarted() {
            if ((-1 == this._state)) {
                this.Advance();
            }
        }
        public int SymbolId {
            get {
                return this._t.SymbolId;
            }
        }
        public string Value {
            get {
                return this._t.Value;
            }
        }
        public int Line {
            get {
                return this._t.Line;
            }
        }
        public int Column {
            get {
                return this._t.Column;
            }
        }
        public long Position {
            get {
                return this._t.Position;
            }
        }
        public bool IsEnded {
            get {
                return (-2 == this._state);
            }
        }
        public bool Advance() {
            if ((false == this._e.MoveNext())) {
                this._t.SymbolId = -2;
                this._state = -2;
            }
            else {
                this._state = 0;
                this._t = this._e.Current;
                return true;
            }
            return false;
        }
        public void Error(string message, object arg1, object arg2, object arg3) {
            throw new SyntaxException(string.Format(message, arg1, arg2, arg3), this.Line, this.Column, this.Position);
        }
        public void Error(string message, object arg1, object arg2) {
            throw new SyntaxException(string.Format(message, arg1, arg2), this.Line, this.Column, this.Position);
        }
        public void Error(string message, object arg) {
            throw new SyntaxException(string.Format(message, arg), this.Line, this.Column, this.Position);
        }
        public void Error(string message) {
            throw new SyntaxException(message, this.Line, this.Column, this.Position);
        }
        public void Dispose() {
            this._e.Dispose();
            this._state = -3;
        }
        void IDisposable.Dispose() {
            this.Dispose();
        }
    }
    internal partial class ExpressionParser {
        internal const int ErrorSymbol = -1;
        internal const int EosSymbol = -2;
        public const int Expression = 0;
        public const int Unary = 1;
        public const int Leaf = 2;
        public const int Term = 3;
        public const int TermPart = 4;
        public const int Factor = 5;
        public const int FactorPart = 6;
        public const int add = 7;
        public const int sub = 8;
        public const int identifier = 9;
        public const int integer = 10;
        public const int lparen = 11;
        public const int rparen = 12;
        public const int mul = 13;
        public const int div = 14;
        public const int whitespace = 15;
        private static ParseNode _ParseExpression(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.identifier == context.SymbolId)) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.lparen == context.SymbolId))) {
                // Expression -> Term
                ParseNode[] children = new ParseNode[1];
                children[0] = ExpressionParser._ParseTerm(context);
                return new ParseNode(ExpressionParser.Expression, "Expression", children, line, column, position);
            }
            context.Error("Expecting add, sub, identifier, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseUnary(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.add == context.SymbolId)) {
                // Unary -> add Unary
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.add, "add", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseUnary(context);
                return new ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position);
            }
            if ((ExpressionParser.sub == context.SymbolId)) {
                // Unary -> sub Unary
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.sub, "sub", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseUnary(context);
                return new ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position);
            }
            if ((((ExpressionParser.identifier == context.SymbolId) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.lparen == context.SymbolId))) {
                // Unary -> Leaf
                ParseNode[] children = new ParseNode[1];
                children[0] = ExpressionParser._ParseLeaf(context);
                return new ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position);
            }
            context.Error("Expecting add, sub, identifier, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseLeaf(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.identifier == context.SymbolId)) {
                // Leaf -> identifier
                ParseNode[] children = new ParseNode[1];
                children[0] = new ParseNode(ExpressionParser.identifier, "identifier", context.Value, line, column, position);
                context.Advance();
                return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
            }
            if ((ExpressionParser.integer == context.SymbolId)) {
                // Leaf -> integer
                ParseNode[] children = new ParseNode[1];
                children[0] = new ParseNode(ExpressionParser.integer, "integer", context.Value, line, column, position);
                context.Advance();
                return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
            }
            if ((ExpressionParser.lparen == context.SymbolId)) {
                // Leaf -> lparen Expression rparen
                ParseNode[] children = new ParseNode[3];
                children[0] = new ParseNode(ExpressionParser.lparen, "lparen", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseExpression(context);
                children[2] = new ParseNode(ExpressionParser.rparen, "rparen", context.Value, line, column, position);
                context.Advance();
                return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
            }
            context.Error("Expecting identifier, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseTerm(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.identifier == context.SymbolId)) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.lparen == context.SymbolId))) {
                // Term -> Factor TermPart
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermPart(context).Children);
                return new ParseNode(ExpressionParser.Term, "Term", children.ToArray(), line, column, position);
            }
            context.Error("Expecting add, sub, identifier, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseTermPart(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.add == context.SymbolId)) {
                // TermPart -> add Factor
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.add, "add", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseFactor(context);
                return new ParseNode(ExpressionParser.TermPart, "TermPart", children, line, column, position);
            }
            if ((ExpressionParser.sub == context.SymbolId)) {
                // TermPart -> sub Factor
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.sub, "sub", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseFactor(context);
                return new ParseNode(ExpressionParser.TermPart, "TermPart", children, line, column, position);
            }
            if (((ExpressionParser.EosSymbol == context.SymbolId) 
                        || (ExpressionParser.rparen == context.SymbolId))) {
                // TermPart ->
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.TermPart, "TermPart", children, line, column, position);
            }
            context.Error("Expecting add, sub, #EOS, or rparen");
            return null;
        }
        private static ParseNode _ParseFactor(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.identifier == context.SymbolId)) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.lparen == context.SymbolId))) {
                // Factor -> Unary FactorPart
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorPart(context).Children);
                return new ParseNode(ExpressionParser.Factor, "Factor", children.ToArray(), line, column, position);
            }
            context.Error("Expecting add, sub, identifier, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseFactorPart(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.mul == context.SymbolId)) {
                // FactorPart -> mul Unary
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseUnary(context);
                return new ParseNode(ExpressionParser.FactorPart, "FactorPart", children, line, column, position);
            }
            if ((ExpressionParser.div == context.SymbolId)) {
                // FactorPart -> div Unary
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.div, "div", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseUnary(context);
                return new ParseNode(ExpressionParser.FactorPart, "FactorPart", children, line, column, position);
            }
            if (((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.EosSymbol == context.SymbolId)) 
                        || (ExpressionParser.rparen == context.SymbolId))) {
                // FactorPart ->
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.FactorPart, "FactorPart", children, line, column, position);
            }
            context.Error("Expecting mul, div, add, sub, #EOS, or rparen");
            return null;
        }
        public static ParseNode ParseExpression(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseExpression(context);
        }
        public static object EvaluateExpression(ParseNode node) {
            if ((ExpressionParser.Expression == node.SymbolId)) {
                return ParsleyDemo.ExpressionParser.EvaluateTerm(node.Children[0]);
            }
            throw new SyntaxException("Expecting Expression", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateTerm(ParseNode node) {
            if ((ExpressionParser.Term == node.SymbolId)) {
                if ((1 == node.Children.Length)) {
                    return ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[0]);
                }
                else {
                    if ((node.Children[1].SymbolId == ParsleyDemo.ExpressionParser.add)) {
                        return (((int)(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[0]))) + ((int)(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[2]))));
                    }
                    else {
                        return (((int)(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[0]))) - ((int)(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[2]))));
                    }
                }
            }
            throw new SyntaxException("Expecting Term", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateFactor(ParseNode node) {
            if ((ExpressionParser.Factor == node.SymbolId)) {
                if ((1 == node.Children.Length)) {
                    return ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[0]);
                }
                else {
                    if ((node.Children[1].SymbolId == ParsleyDemo.ExpressionParser.mul)) {
                        return (((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[0]))) * ((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[2]))));
                    }
                    else {
                        return (((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[0]))) / ((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[2]))));
                    }
                }
            }
            throw new SyntaxException("Expecting Factor", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateUnary(ParseNode node) {
            if ((ExpressionParser.Unary == node.SymbolId)) {
                if ((1 == node.Children.Length)) {
                    return ParsleyDemo.ExpressionParser.EvaluateLeaf(node.Children[0]);
                }
                else {
                    if ((node.Children[0].SymbolId == ParsleyDemo.ExpressionParser.add)) {
                        return ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[1]);
                    }
                    else {
                        return (0 - ((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[1]))));
                    }
                }
            }
            throw new SyntaxException("Expecting Unary", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateLeaf(ParseNode node) {
            if ((ExpressionParser.Leaf == node.SymbolId)) {
                ParseNode n = node.Children[0];
                if ((ParsleyDemo.ExpressionParser.identifier == n.SymbolId)) {
                    throw new NotImplementedException("Variables are not implemented");
                }
                else {
                    if ((ParsleyDemo.ExpressionParser.integer == n.SymbolId)) {
                        return int.Parse(n.Value);
                    }
                    else {
                        return ParsleyDemo.ExpressionParser.EvaluateExpression(n.Children[1]);
                    }
                }
            }
            throw new SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position);
        }
    }
}