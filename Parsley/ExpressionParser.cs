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
                return null;
            }
            throw new SyntaxException("Expecting Expression", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateTerm(ParseNode node) {
            if ((ExpressionParser.Term == node.SymbolId)) {
                return null;
            }
            throw new SyntaxException("Expecting Term", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateFactor(ParseNode node) {
            if ((ExpressionParser.Factor == node.SymbolId)) {
                System.Console.WriteLine("Factor");
                return null;
            }
            throw new SyntaxException("Expecting Factor", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateUnary(ParseNode node) {
            if ((ExpressionParser.Unary == node.SymbolId)) {
                System.Console.WriteLine("Unary");
                return null;
            }
            throw new SyntaxException("Expecting Unary", node.Line, node.Column, node.Position);
        }
        protected internal static object EvaluateLeaf(ParseNode node) {
            if ((ExpressionParser.Leaf == node.SymbolId)) {
                System.Console.WriteLine("Leaf");
                return null;
            }
            throw new SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position);
        }
    }
}