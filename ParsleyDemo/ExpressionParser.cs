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
    /// Expression= Term;
    /// Term= Factor ( "+" | "-" ) Factor | Factor;
    /// Factor= Unary ( "*" | "/" ) Unary | Unary;
    /// Unary= ( "+" | "-" ) Unary | Leaf;
    /// Leaf= identifier | integer | "(" Expression ")";
    /// add= "+";
    /// sub= "-";
    /// mul= "*";
    /// div= "/";
    /// lparen= "(";
    /// rparen= ")";
    /// integer= '[0-9]+';
    /// identifier= '[A-Z_a-z][0-9A-Z_a-z]*';
    /// (whitespace)= '\s+';
    /// </summary>
    /// <remarks>The rules for the factored grammar are as follows:
    /// Expression -> Term
    /// Unary -> add Unary
    /// Unary -> sub Unary
    /// Unary -> Leaf
    /// Leaf -> identifier
    /// Leaf -> integer
    /// Leaf -> lparen Expression rparen
    /// Term -> Factor TermPart
    /// TermPart -> add Factor
    /// TermPart -> sub Factor
    /// TermPart ->
    /// Factor -> Unary FactorPart
    /// FactorPart -> mul Unary
    /// FactorPart -> div Unary
    /// FactorPart ->
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")]
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
        
        #line 1 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
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
        
        #line default
        #line hidden
        
        #line 18 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
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
        
        #line default
        #line hidden
        
        #line 26 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
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
                if ((ExpressionParser.rparen == context.SymbolId)) {
                    context.Advance();
                    return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
                }
                context.Error("Expecting rparen");
            }
            context.Error("Expecting identifier, integer, or lparen");
            return null;
        }
        
        #line default
        #line hidden
        
        #line 2 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
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
        
        #line default
        #line hidden
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
        
        #line 10 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
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
        
        #line default
        #line hidden
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
        /// <summary>
        /// Parses a production of the form:
        /// Expression= Term
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Expression -> Term
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        
        #line 1 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static ParseNode ParseExpression(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseExpression(context);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Parses a production of the form:
        /// Term= Factor ( "+" | "-" ) Factor | Factor
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        
        #line 2 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static ParseNode ParseTerm(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseTerm(context);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Parses a production of the form:
        /// Factor= Unary ( "*" | "/" ) Unary | Unary
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Factor -> Unary FactorPart
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        
        #line 10 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static ParseNode ParseFactor(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseFactor(context);
        }
        
        #line default
        #line hidden
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
        
        #line 18 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static ParseNode ParseUnary(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseUnary(context);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Parses a production of the form:
        /// Leaf= identifier | integer | "(" Expression ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> identifier
        /// Leaf -> integer
        /// Leaf -> lparen Expression rparen
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        
        #line 26 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static ParseNode ParseLeaf(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseLeaf(context);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Parses a derivation of the form:
        /// Expression= Term
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Expression -> Term
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        
        #line 1 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static ParseNode Parse(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseExpression(context);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Expression= Term
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Expression -> Term
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 1 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int Evaluate(ParseNode node) {
            return ExpressionParser.EvaluateExpression(node);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Expression= Term
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Expression -> Term
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 1 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int Evaluate(ParseNode node, object state) {
            return ExpressionParser.EvaluateExpression(node, state);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Expression= Term
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Expression -> Term
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 1 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateExpression(ParseNode node, object state) {
            if ((ExpressionParser.Expression == node.SymbolId)) {
                return ((int)(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateTerm(node.Children[0]), typeof(int))));
            }
            throw new SyntaxException("Expecting Expression", node.Line, node.Column, node.Position);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Expression= Term
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Expression -> Term
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 1 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateExpression(ParseNode node) {
            return ExpressionParser.EvaluateExpression(node, null);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Term= Factor ( "+" | "-" ) Factor | Factor
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 2 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static object EvaluateTerm(ParseNode node, object state) {
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
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Term= Factor ( "+" | "-" ) Factor | Factor
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 2 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static object EvaluateTerm(ParseNode node) {
            return ExpressionParser.EvaluateTerm(node, null);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Factor= Unary ( "*" | "/" ) Unary | Unary
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Factor -> Unary FactorPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 10 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateFactor(ParseNode node, object state) {
            if ((ExpressionParser.Factor == node.SymbolId)) {
                if ((1 == node.Children.Length)) {
                    return ((int)(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[0]), typeof(int))));
                }
                else {
                    if ((node.Children[1].SymbolId == ParsleyDemo.ExpressionParser.mul)) {
                        return ((int)(ExpressionParser._ChangeType((((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[0]))) * ((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[2])))), typeof(int))));
                    }
                    else {
                        return ((int)(ExpressionParser._ChangeType((((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[0]))) / ((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[2])))), typeof(int))));
                    }
                }
            }
            throw new SyntaxException("Expecting Factor", node.Line, node.Column, node.Position);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Factor= Unary ( "*" | "/" ) Unary | Unary
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Factor -> Unary FactorPart
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 10 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateFactor(ParseNode node) {
            return ExpressionParser.EvaluateFactor(node, null);
        }
        
        #line default
        #line hidden
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
        
        #line 18 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateUnary(ParseNode node, object state) {
            if ((ExpressionParser.Unary == node.SymbolId)) {
                if ((1 == node.Children.Length)) {
                    return ((int)(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateLeaf(node.Children[0]), typeof(int))));
                }
                else {
                    if ((node.Children[0].SymbolId == ParsleyDemo.ExpressionParser.add)) {
                        return ((int)(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[1]), typeof(int))));
                    }
                    else {
                        return ((int)(ExpressionParser._ChangeType((0 - ((int)(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children[1])))), typeof(int))));
                    }
                }
            }
            throw new SyntaxException("Expecting Unary", node.Line, node.Column, node.Position);
        }
        
        #line default
        #line hidden
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
        
        #line 18 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateUnary(ParseNode node) {
            return ExpressionParser.EvaluateUnary(node, null);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Leaf= identifier | integer | "(" Expression ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> identifier
        /// Leaf -> integer
        /// Leaf -> lparen Expression rparen
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 26 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateLeaf(ParseNode node, object state) {
            if ((ExpressionParser.Leaf == node.SymbolId)) {
                ParseNode n = node.Children[0];
                if ((ParsleyDemo.ExpressionParser.identifier == n.SymbolId)) {
                    throw new NotImplementedException("Variables are not implemented");
                }
                else {
                    if ((ParsleyDemo.ExpressionParser.integer == n.SymbolId)) {
                        return ((int)(ExpressionParser._ChangeType(n.Value, typeof(int))));
                    }
                    else {
                        return ((int)(ParsleyDemo.ExpressionParser.EvaluateExpression(n.Children[1])));
                    }
                }
            }
            throw new SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position);
        }
        
        #line default
        #line hidden
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Leaf= identifier | integer | "(" Expression ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> identifier
        /// Leaf -> integer
        /// Leaf -> lparen Expression rparen
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        
        #line 26 "C:\dev\BuildPack\ParsleyDemo\Expression.xbnf"
        public static int EvaluateLeaf(ParseNode node) {
            return ExpressionParser.EvaluateLeaf(node, null);
        }
        
        #line default
        #line hidden
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
