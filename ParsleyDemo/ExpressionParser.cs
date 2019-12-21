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
    
    /// <summary>Parses the following grammar:
    /// Term= Factor { ( "+" | "-" ) Factor };
    /// Factor= Unary { ( "*" | "/" ) Unary };
    /// Unary= ( "+" | "-" ) Unary | Leaf;
    /// Leaf= integer | "(" Term ")";
    /// add= "+";
    /// sub= "-";
    /// mul= "*";
    /// div= "/";
    /// lparen= "(";
    /// rparen= ")";
    /// integer= '[0-9]+';
    /// (whitespace)= '\s+';
    /// </summary>
    /// <remarks>The rules for the factored grammar are as follows:
    /// Unary -> add Unary
    /// Unary -> sub Unary
    /// Unary -> Leaf
    /// Leaf -> integer
    /// Leaf -> lparen Term rparen
    /// TermList -> add Factor TermListRightAssoc TermListRightAssoc2
    /// TermList -> sub Factor TermListRightAssoc TermListRightAssoc2
    /// FactorList -> mul Unary FactorListRightAssoc FactorListRightAssoc2
    /// FactorList -> div Unary FactorListRightAssoc FactorListRightAssoc2
    /// TermListRightAssoc -> add Factor TermListRightAssoc
    /// TermListRightAssoc ->
    /// FactorListRightAssoc -> mul Unary FactorListRightAssoc
    /// FactorListRightAssoc ->
    /// Term -> Factor TermPart
    /// TermPart -> TermList
    /// TermPart ->
    /// Factor -> Unary FactorPart
    /// FactorPart -> FactorList
    /// FactorPart ->
    /// TermListRightAssoc2 -> sub Factor TermListRightAssoc2
    /// TermListRightAssoc2 ->
    /// FactorListRightAssoc2 -> div Unary FactorListRightAssoc2
    /// FactorListRightAssoc2 ->
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")]
    internal partial class ExpressionParser {
        internal const int ErrorSymbol = -1;
        internal const int EosSymbol = -2;
        public const int Unary = 0;
        public const int Leaf = 1;
        public const int TermList = 2;
        public const int FactorList = 3;
        public const int TermListRightAssoc = 4;
        public const int FactorListRightAssoc = 5;
        public const int Term = 6;
        public const int TermPart = 7;
        public const int Factor = 8;
        public const int FactorPart = 9;
        public const int TermListRightAssoc2 = 10;
        public const int FactorListRightAssoc2 = 11;
        public const int add = 12;
        public const int sub = 13;
        public const int integer = 14;
        public const int lparen = 15;
        public const int rparen = 16;
        public const int mul = 17;
        public const int div = 18;
        public const int whitespace = 19;
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
            if (((ExpressionParser.integer == context.SymbolId) 
                        || (ExpressionParser.lparen == context.SymbolId))) {
                // Unary -> Leaf
                ParseNode[] children = new ParseNode[1];
                children[0] = ExpressionParser._ParseLeaf(context);
                return new ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position);
            }
            context.Error("Expecting add, sub, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseLeaf(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.integer == context.SymbolId)) {
                // Leaf -> integer
                ParseNode[] children = new ParseNode[1];
                children[0] = new ParseNode(ExpressionParser.integer, "integer", context.Value, line, column, position);
                context.Advance();
                return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
            }
            if ((ExpressionParser.lparen == context.SymbolId)) {
                // Leaf -> lparen Term rparen
                ParseNode[] children = new ParseNode[3];
                children[0] = new ParseNode(ExpressionParser.lparen, "lparen", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseTerm(context);
                children[2] = new ParseNode(ExpressionParser.rparen, "rparen", context.Value, line, column, position);
                if ((ExpressionParser.rparen == context.SymbolId)) {
                    context.Advance();
                    return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
                }
                context.Error("Expecting rparen");
            }
            context.Error("Expecting integer or lparen");
            return null;
        }
        private static ParseNode _ParseTermList(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.add == context.SymbolId)) {
                // TermList -> add Factor TermListRightAssoc TermListRightAssoc2
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.add, "add", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.TermList, "TermList", children.ToArray(), line, column, position);
            }
            if ((ExpressionParser.sub == context.SymbolId)) {
                // TermList -> sub Factor TermListRightAssoc TermListRightAssoc2
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.sub, "sub", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.TermList, "TermList", children.ToArray(), line, column, position);
            }
            context.Error("Expecting add or sub");
            return null;
        }
        private static ParseNode _ParseFactorList(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.mul == context.SymbolId)) {
                // FactorList -> mul Unary FactorListRightAssoc FactorListRightAssoc2
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.FactorList, "FactorList", children.ToArray(), line, column, position);
            }
            if ((ExpressionParser.div == context.SymbolId)) {
                // FactorList -> div Unary FactorListRightAssoc FactorListRightAssoc2
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.div, "div", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.FactorList, "FactorList", children.ToArray(), line, column, position);
            }
            context.Error("Expecting mul or div");
            return null;
        }
        private static ParseNode _ParseTermListRightAssoc(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.add == context.SymbolId)) {
                // TermListRightAssoc -> add Factor TermListRightAssoc
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.add, "add", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children);
                return new ParseNode(ExpressionParser.TermListRightAssoc, "TermListRightAssoc", children.ToArray(), line, column, position);
            }
            if ((((ExpressionParser.sub == context.SymbolId) 
                        || (ExpressionParser.EosSymbol == context.SymbolId)) 
                        || (ExpressionParser.rparen == context.SymbolId))) {
                // TermListRightAssoc ->
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.TermListRightAssoc, "TermListRightAssoc", children, line, column, position);
            }
            context.Error("Expecting add, sub, #EOS, or rparen");
            return null;
        }
        private static ParseNode _ParseFactorListRightAssoc(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.mul == context.SymbolId)) {
                // FactorListRightAssoc -> mul Unary FactorListRightAssoc
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children);
                return new ParseNode(ExpressionParser.FactorListRightAssoc, "FactorListRightAssoc", children.ToArray(), line, column, position);
            }
            if ((((((ExpressionParser.div == context.SymbolId) 
                        || (ExpressionParser.add == context.SymbolId)) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.EosSymbol == context.SymbolId)) 
                        || (ExpressionParser.rparen == context.SymbolId))) {
                // FactorListRightAssoc ->
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.FactorListRightAssoc, "FactorListRightAssoc", children, line, column, position);
            }
            context.Error("Expecting mul, div, add, sub, #EOS, or rparen");
            return null;
        }
        private static ParseNode _ParseTerm(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if (((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.lparen == context.SymbolId))) {
                // Term -> Factor TermPart
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermPart(context).Children);
                return new ParseNode(ExpressionParser.Term, "Term", children.ToArray(), line, column, position);
            }
            context.Error("Expecting add, sub, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseTermPart(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if (((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId))) {
                // TermPart -> TermList
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.AddRange(ExpressionParser._ParseTermList(context).Children);
                return new ParseNode(ExpressionParser.TermPart, "TermPart", children.ToArray(), line, column, position);
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
            if (((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.lparen == context.SymbolId))) {
                // Factor -> Unary FactorPart
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorPart(context).Children);
                return new ParseNode(ExpressionParser.Factor, "Factor", children.ToArray(), line, column, position);
            }
            context.Error("Expecting add, sub, integer, or lparen");
            return null;
        }
        private static ParseNode _ParseFactorPart(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if (((ExpressionParser.mul == context.SymbolId) 
                        || (ExpressionParser.div == context.SymbolId))) {
                // FactorPart -> FactorList
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.AddRange(ExpressionParser._ParseFactorList(context).Children);
                return new ParseNode(ExpressionParser.FactorPart, "FactorPart", children.ToArray(), line, column, position);
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
        private static ParseNode _ParseTermListRightAssoc2(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.sub == context.SymbolId)) {
                // TermListRightAssoc2 -> sub Factor TermListRightAssoc2
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.sub, "sub", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.TermListRightAssoc2, "TermListRightAssoc2", children.ToArray(), line, column, position);
            }
            if (((ExpressionParser.EosSymbol == context.SymbolId) 
                        || (ExpressionParser.rparen == context.SymbolId))) {
                // TermListRightAssoc2 ->
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.TermListRightAssoc2, "TermListRightAssoc2", children, line, column, position);
            }
            context.Error("Expecting sub, #EOS, or rparen");
            return null;
        }
        private static ParseNode _ParseFactorListRightAssoc2(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            if ((ExpressionParser.div == context.SymbolId)) {
                // FactorListRightAssoc2 -> div Unary FactorListRightAssoc2
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.div, "div", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.FactorListRightAssoc2, "FactorListRightAssoc2", children.ToArray(), line, column, position);
            }
            if (((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.sub == context.SymbolId)) 
                        || (ExpressionParser.EosSymbol == context.SymbolId)) 
                        || (ExpressionParser.rparen == context.SymbolId))) {
                // FactorListRightAssoc2 ->
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.FactorListRightAssoc2, "FactorListRightAssoc2", children, line, column, position);
            }
            context.Error("Expecting div, add, sub, #EOS, or rparen");
            return null;
        }
        /// <summary>
        /// Parses a production of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseTerm(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseTerm(context);
        }
        /// <summary>
        /// Parses a production of the form:
        /// Factor= Unary { ( "*" | "/" ) Unary }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Factor -> Unary FactorPart
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseFactor(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseFactor(context);
        }
        /// <summary>
        /// Parses a production of the form:
        /// Unary= ( "+" | "-" ) Unary | Leaf
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Unary -> add Unary
        /// Unary -> sub Unary
        /// Unary -> Leaf
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseUnary(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseUnary(context);
        }
        /// <summary>
        /// Parses a production of the form:
        /// Leaf= integer | "(" Term ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> integer
        /// Leaf -> lparen Term rparen
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseLeaf(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseLeaf(context);
        }
        /// <summary>
        /// Parses a derivation of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode Parse(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseTerm(context);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        public static int Evaluate(ParseNode node) {
            return ExpressionParser.EvaluateTerm(node);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        public static int Evaluate(ParseNode node, object state) {
            return ExpressionParser.EvaluateTerm(node, state);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateTerm(ParseNode node, object state) {
            if ((ExpressionParser.Term == node.SymbolId)) {
                int result = ExpressionParser.EvaluateFactor(node.Children[0], state);
                int i = 2;
                for (
                ; (i < node.Children.Length); 
                ) {
                    if ((node.Children[(i - 1)].SymbolId == ParsleyDemo.ExpressionParser.add)) {
                        result = (result + ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[i], state));
                    }
                    else {
                        result = (result - ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[i], state));
                    }
                    i = (i + 2);
                }
                return ((int)(ExpressionParser._ChangeType(result, typeof(int))));
            }
            throw new SyntaxException("Expecting Term", node.Line, node.Column, node.Position);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateTerm(ParseNode node) {
            return ExpressionParser.EvaluateTerm(node, null);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Factor= Unary { ( "*" | "/" ) Unary }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Factor -> Unary FactorPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateFactor(ParseNode node, object state) {
            if ((ExpressionParser.Factor == node.SymbolId)) {
                int result = ExpressionParser.EvaluateUnary(node.Children[0], state);
                int i = 2;
                for (
                ; (i < node.Children.Length); 
                ) {
                    if ((node.Children[i].SymbolId == ParsleyDemo.ExpressionParser.Unary)) {
                        if ((node.Children[(i - 1)].SymbolId == ParsleyDemo.ExpressionParser.mul)) {
                            result = (result * ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[i], state));
                        }
                        else {
                            result = (result / ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[i], state));
                        }
                    }
                    else {
                        if ((node.Children[(i - 1)].SymbolId == ParsleyDemo.ExpressionParser.mul)) {
                            result = (result * ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[i], state));
                        }
                        else {
                            result = (result / ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children[(i - 1)], state));
                        }
                    }
                    i = (i + 2);
                }
                return ((int)(ExpressionParser._ChangeType(result, typeof(int))));
            }
            throw new SyntaxException("Expecting Factor", node.Line, node.Column, node.Position);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Factor= Unary { ( "*" | "/" ) Unary }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Factor -> Unary FactorPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateFactor(ParseNode node) {
            return ExpressionParser.EvaluateFactor(node, null);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Unary= ( "+" | "-" ) Unary | Leaf
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Unary -> add Unary
        /// Unary -> sub Unary
        /// Unary -> Leaf
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateUnary(ParseNode node, object state) {
            if ((ExpressionParser.Unary == node.SymbolId)) {
                if ((node.Children.Length == 1)) {
                    return ((int)(ExpressionParser._ChangeType(ExpressionParser.EvaluateLeaf(node.Children[0], state), typeof(int))));
                }
                if ((node.Children[0].SymbolId == ParsleyDemo.ExpressionParser.add)) {
                    return ((int)(ExpressionParser._ChangeType(ExpressionParser.EvaluateUnary(node.Children[1], state), typeof(int))));
                }
                else {
                    return ((int)(ExpressionParser._ChangeType((0 - ExpressionParser.EvaluateUnary(node.Children[1], state)), typeof(int))));
                }
            }
            throw new SyntaxException("Expecting Unary", node.Line, node.Column, node.Position);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Unary= ( "+" | "-" ) Unary | Leaf
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Unary -> add Unary
        /// Unary -> sub Unary
        /// Unary -> Leaf
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateUnary(ParseNode node) {
            return ExpressionParser.EvaluateUnary(node, null);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Leaf= integer | "(" Term ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> integer
        /// Leaf -> lparen Term rparen
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateLeaf(ParseNode node, object state) {
            if ((ExpressionParser.Leaf == node.SymbolId)) {
                if ((node.Children.Length == 1)) {
                    return ((int)(ExpressionParser._ChangeType(node.Children[0].Value, typeof(int))));
                }
                else {
                    return ((int)(ExpressionParser._ChangeType(ExpressionParser.EvaluateTerm(node.Children[1], state), typeof(int))));
                }
            }
            throw new SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Leaf= integer | "(" Term ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> integer
        /// Leaf -> lparen Term rparen
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateLeaf(ParseNode node) {
            return ExpressionParser.EvaluateLeaf(node, null);
        }
        private static object _ChangeType(object obj, System.Type type) {
            System.ComponentModel.TypeConverter typeConverter = System.ComponentModel.TypeDescriptor.GetConverter(obj);
            if (((null == typeConverter) 
                        || (false == typeConverter.CanConvertTo(type)))) {
                return System.Convert.ChangeType(obj, type);
            }
            return typeConverter.ConvertTo(obj, type);
        }
    }
}
