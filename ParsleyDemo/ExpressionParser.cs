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
    /// Leaf= integer | identifier | "(" Term ")";
    /// add= "+";
    /// mul= "*";
    /// integer= '[0-9]+';
    /// identifier= '[A-Z_a-z][0-9A-Z_a-z]*';
    /// (whitespace)= '\s+';
    /// </summary>
    /// <remarks>The rules for the factored grammar are as follows:
    /// Term -> Factor TermList
    /// Term -> Factor
    /// Factor -> Unary FactorList
    /// Factor -> Unary
    /// Unary -> add Unary
    /// Unary -> Implicit Unary
    /// Unary -> Leaf
    /// Leaf -> integer
    /// Leaf -> identifier
    /// Leaf -> Implicit3 Term Implicit4
    /// TermList -> add Factor TermListRightAssoc TermListRightAssoc2
    /// TermList -> Implicit Factor TermListRightAssoc TermListRightAssoc2
    /// FactorList -> mul Unary FactorListRightAssoc FactorListRightAssoc2
    /// FactorList -> Implicit2 Unary FactorListRightAssoc FactorListRightAssoc2
    /// TermListRightAssoc -> add Factor TermListRightAssoc
    /// TermListRightAssoc ->
    /// FactorListRightAssoc -> mul Unary FactorListRightAssoc
    /// FactorListRightAssoc ->
    /// TermListRightAssoc2 -> Implicit Factor TermListRightAssoc2
    /// TermListRightAssoc2 ->
    /// FactorListRightAssoc2 -> Implicit2 Unary FactorListRightAssoc2
    /// FactorListRightAssoc2 ->
    /// </remarks>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")]
    internal partial class ExpressionParser {
        internal const int ErrorSymbol = -1;
        internal const int EosSymbol = -2;
        public const int Term = 0;
        public const int Factor = 1;
        public const int Unary = 2;
        public const int Leaf = 3;
        public const int TermList = 4;
        public const int FactorList = 5;
        public const int TermListRightAssoc = 6;
        public const int FactorListRightAssoc = 7;
        public const int TermListRightAssoc2 = 8;
        public const int FactorListRightAssoc2 = 9;
        public const int add = 10;
        public const int Implicit = 11;
        public const int integer = 12;
        public const int identifier = 13;
        public const int Implicit3 = 14;
        public const int Implicit4 = 15;
        public const int mul = 16;
        public const int Implicit2 = 17;
        public const int whitespace = 18;
        private static ParseNode _ParseTerm(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Term -> Factor TermList
            // Term -> Factor
            if ((((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.Implicit == context.SymbolId)) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.identifier == context.SymbolId)) 
                        || (ExpressionParser.Implicit3 == context.SymbolId))) {
                ParserContext pc2;
                System.Exception lastExcept = null;
                pc2 = context.GetLookAhead();
                pc2.EnsureStarted();
                // Term -> Factor TermList
                try {
                    if ((((((ExpressionParser.add == pc2.SymbolId) 
                                || (ExpressionParser.Implicit == pc2.SymbolId)) 
                                || (ExpressionParser.integer == pc2.SymbolId)) 
                                || (ExpressionParser.identifier == pc2.SymbolId)) 
                                || (ExpressionParser.Implicit3 == pc2.SymbolId))) {
                        System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                        children.Add(ExpressionParser._ParseFactor(pc2));
                        children.AddRange(ExpressionParser._ParseTermList(pc2).Children);
                        int adv = 0;
                        for (
                        ; (adv < pc2.AdvanceCount); 
                        ) {
                            context.Advance();
                            adv = (adv + 1);
                        }
                        return new ParseNode(ExpressionParser.Term, "Term", children.ToArray(), line, column, position);
                    }
                    context.Error("Expecting add, Implicit, integer, identifier, or Implicit3");
                }
                catch (SyntaxException ex) {
                    if ((lastExcept == null)) {
                        lastExcept = ex;
                    }
                }
                finally {

                }
                pc2 = context.GetLookAhead();
                pc2.EnsureStarted();
                // Term -> Factor
                try {
                    if ((((((ExpressionParser.add == pc2.SymbolId) 
                                || (ExpressionParser.Implicit == pc2.SymbolId)) 
                                || (ExpressionParser.integer == pc2.SymbolId)) 
                                || (ExpressionParser.identifier == pc2.SymbolId)) 
                                || (ExpressionParser.Implicit3 == pc2.SymbolId))) {
                        ParseNode[] children = new ParseNode[1];
                        children[0] = ExpressionParser._ParseFactor(pc2);
                        int adv = 0;
                        for (
                        ; (adv < pc2.AdvanceCount); 
                        ) {
                            context.Advance();
                            adv = (adv + 1);
                        }
                        return new ParseNode(ExpressionParser.Term, "Term", children, line, column, position);
                    }
                    context.Error("Expecting add, Implicit, integer, identifier, or Implicit3");
                }
                catch (SyntaxException ex) {
                    if ((lastExcept == null)) {
                        lastExcept = ex;
                    }
                }
                finally {

                }
                throw lastExcept;
            }
            context.Error("Expecting add, Implicit, integer, identifier, or Implicit3");
            return null;
        }
        /// <summary>
        /// Parses a production of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermList
        /// Term -> Factor
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseTerm(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseTerm(context);
        }
        /// <summary>
        /// Parses a production of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermList
        /// Term -> Factor
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode Parse(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseTerm(context);
        }
        private static ParseNode _ParseFactor(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Factor -> Unary FactorList
            // Factor -> Unary
            if ((((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.Implicit == context.SymbolId)) 
                        || (ExpressionParser.integer == context.SymbolId)) 
                        || (ExpressionParser.identifier == context.SymbolId)) 
                        || (ExpressionParser.Implicit3 == context.SymbolId))) {
                ParserContext pc2;
                System.Exception lastExcept = null;
                pc2 = context.GetLookAhead();
                pc2.EnsureStarted();
                // Factor -> Unary FactorList
                try {
                    if ((((((ExpressionParser.add == pc2.SymbolId) 
                                || (ExpressionParser.Implicit == pc2.SymbolId)) 
                                || (ExpressionParser.integer == pc2.SymbolId)) 
                                || (ExpressionParser.identifier == pc2.SymbolId)) 
                                || (ExpressionParser.Implicit3 == pc2.SymbolId))) {
                        System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                        children.Add(ExpressionParser._ParseUnary(pc2));
                        children.AddRange(ExpressionParser._ParseFactorList(pc2).Children);
                        int adv = 0;
                        for (
                        ; (adv < pc2.AdvanceCount); 
                        ) {
                            context.Advance();
                            adv = (adv + 1);
                        }
                        return new ParseNode(ExpressionParser.Factor, "Factor", children.ToArray(), line, column, position);
                    }
                    context.Error("Expecting add, Implicit, integer, identifier, or Implicit3");
                }
                catch (SyntaxException ex) {
                    if ((lastExcept == null)) {
                        lastExcept = ex;
                    }
                }
                finally {

                }
                pc2 = context.GetLookAhead();
                pc2.EnsureStarted();
                // Factor -> Unary
                try {
                    if ((((((ExpressionParser.add == pc2.SymbolId) 
                                || (ExpressionParser.Implicit == pc2.SymbolId)) 
                                || (ExpressionParser.integer == pc2.SymbolId)) 
                                || (ExpressionParser.identifier == pc2.SymbolId)) 
                                || (ExpressionParser.Implicit3 == pc2.SymbolId))) {
                        ParseNode[] children = new ParseNode[1];
                        children[0] = ExpressionParser._ParseUnary(pc2);
                        int adv = 0;
                        for (
                        ; (adv < pc2.AdvanceCount); 
                        ) {
                            context.Advance();
                            adv = (adv + 1);
                        }
                        return new ParseNode(ExpressionParser.Factor, "Factor", children, line, column, position);
                    }
                    context.Error("Expecting add, Implicit, integer, identifier, or Implicit3");
                }
                catch (SyntaxException ex) {
                    if ((lastExcept == null)) {
                        lastExcept = ex;
                    }
                }
                finally {

                }
                throw lastExcept;
            }
            context.Error("Expecting add, Implicit, integer, identifier, or Implicit3");
            return null;
        }
        /// <summary>
        /// Parses a production of the form:
        /// Factor= Unary { ( "*" | "/" ) Unary }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Factor -> Unary FactorList
        /// Factor -> Unary
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseFactor(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseFactor(context);
        }
        private static ParseNode _ParseUnary(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Unary -> add Unary
            if ((ExpressionParser.add == context.SymbolId)) {
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.add, "add", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseUnary(context);
                return new ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position);
            }
            // Unary -> Implicit Unary
            if ((ExpressionParser.Implicit == context.SymbolId)) {
                ParseNode[] children = new ParseNode[2];
                children[0] = new ParseNode(ExpressionParser.Implicit, "Implicit", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseUnary(context);
                return new ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position);
            }
            // Unary -> Leaf
            if ((((ExpressionParser.integer == context.SymbolId) 
                        || (ExpressionParser.identifier == context.SymbolId)) 
                        || (ExpressionParser.Implicit3 == context.SymbolId))) {
                ParseNode[] children = new ParseNode[1];
                children[0] = ExpressionParser._ParseLeaf(context);
                return new ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position);
            }
            context.Error("Expecting add, Implicit, integer, identifier, or Implicit3");
            return null;
        }
        /// <summary>
        /// Parses a production of the form:
        /// Unary= ( "+" | "-" ) Unary | Leaf
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Unary -> add Unary
        /// Unary -> Implicit Unary
        /// Unary -> Leaf
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseUnary(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseUnary(context);
        }
        private static ParseNode _ParseLeaf(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Leaf -> integer
            if ((ExpressionParser.integer == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                children[0] = new ParseNode(ExpressionParser.integer, "integer", context.Value, line, column, position);
                context.Advance();
                return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
            }
            // Leaf -> identifier
            if ((ExpressionParser.identifier == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                children[0] = new ParseNode(ExpressionParser.identifier, "identifier", context.Value, line, column, position);
                context.Advance();
                return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
            }
            // Leaf -> Implicit3 Term Implicit4
            if ((ExpressionParser.Implicit3 == context.SymbolId)) {
                ParseNode[] children = new ParseNode[3];
                children[0] = new ParseNode(ExpressionParser.Implicit3, "Implicit3", context.Value, line, column, position);
                context.Advance();
                children[1] = ExpressionParser._ParseTerm(context);
                if ((ExpressionParser.Implicit4 == context.SymbolId)) {
                    children[2] = new ParseNode(ExpressionParser.Implicit4, "Implicit4", context.Value, line, column, position);
                    context.Advance();
                }
                return new ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position);
            }
            context.Error("Expecting integer, identifier, or Implicit3");
            return null;
        }
        /// <summary>
        /// Parses a production of the form:
        /// Leaf= integer | identifier | "(" Term ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> integer
        /// Leaf -> identifier
        /// Leaf -> Implicit3 Term Implicit4
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode ParseLeaf(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            return ExpressionParser._ParseLeaf(context);
        }
        private static ParseNode _ParseTermList(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // TermList -> add Factor TermListRightAssoc TermListRightAssoc2
            if ((ExpressionParser.add == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.add, "add", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.TermList, "TermList", children.ToArray(), line, column, position);
            }
            // TermList -> Implicit Factor TermListRightAssoc TermListRightAssoc2
            if ((ExpressionParser.Implicit == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.Implicit, "Implicit", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.TermList, "TermList", children.ToArray(), line, column, position);
            }
            context.Error("Expecting add or Implicit");
            return null;
        }
        private static ParseNode _ParseFactorList(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // FactorList -> mul Unary FactorListRightAssoc FactorListRightAssoc2
            if ((ExpressionParser.mul == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.FactorList, "FactorList", children.ToArray(), line, column, position);
            }
            // FactorList -> Implicit2 Unary FactorListRightAssoc FactorListRightAssoc2
            if ((ExpressionParser.Implicit2 == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.Implicit2, "Implicit2", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children);
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.FactorList, "FactorList", children.ToArray(), line, column, position);
            }
            context.Error("Expecting mul or Implicit2");
            return null;
        }
        private static ParseNode _ParseTermListRightAssoc(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // TermListRightAssoc -> add Factor TermListRightAssoc
            if ((ExpressionParser.add == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.add, "add", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children);
                return new ParseNode(ExpressionParser.TermListRightAssoc, "TermListRightAssoc", children.ToArray(), line, column, position);
            }
            // TermListRightAssoc ->
            if ((((ExpressionParser.Implicit == context.SymbolId) 
                        || (ExpressionParser.EosSymbol == context.SymbolId)) 
                        || (ExpressionParser.Implicit4 == context.SymbolId))) {
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.TermListRightAssoc, "TermListRightAssoc", children, line, column, position);
            }
            context.Error("Expecting add, Implicit, #EOS, or Implicit4");
            return null;
        }
        private static ParseNode _ParseFactorListRightAssoc(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // FactorListRightAssoc -> mul Unary FactorListRightAssoc
            if ((ExpressionParser.mul == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children);
                return new ParseNode(ExpressionParser.FactorListRightAssoc, "FactorListRightAssoc", children.ToArray(), line, column, position);
            }
            // FactorListRightAssoc ->
            if ((((((ExpressionParser.Implicit2 == context.SymbolId) 
                        || (ExpressionParser.add == context.SymbolId)) 
                        || (ExpressionParser.Implicit == context.SymbolId)) 
                        || (ExpressionParser.EosSymbol == context.SymbolId)) 
                        || (ExpressionParser.Implicit4 == context.SymbolId))) {
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.FactorListRightAssoc, "FactorListRightAssoc", children, line, column, position);
            }
            context.Error("Expecting mul, Implicit2, add, Implicit, #EOS, or Implicit4");
            return null;
        }
        private static ParseNode _ParseTermListRightAssoc2(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // TermListRightAssoc2 -> Implicit Factor TermListRightAssoc2
            if ((ExpressionParser.Implicit == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.Implicit, "Implicit", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseFactor(context));
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.TermListRightAssoc2, "TermListRightAssoc2", children.ToArray(), line, column, position);
            }
            // TermListRightAssoc2 ->
            if (((ExpressionParser.EosSymbol == context.SymbolId) 
                        || (ExpressionParser.Implicit4 == context.SymbolId))) {
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.TermListRightAssoc2, "TermListRightAssoc2", children, line, column, position);
            }
            context.Error("Expecting Implicit, #EOS, or Implicit4");
            return null;
        }
        private static ParseNode _ParseFactorListRightAssoc2(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // FactorListRightAssoc2 -> Implicit2 Unary FactorListRightAssoc2
            if ((ExpressionParser.Implicit2 == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(new ParseNode(ExpressionParser.Implicit2, "Implicit2", context.Value, line, column, position));
                context.Advance();
                children.Add(ExpressionParser._ParseUnary(context));
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children);
                return new ParseNode(ExpressionParser.FactorListRightAssoc2, "FactorListRightAssoc2", children.ToArray(), line, column, position);
            }
            // FactorListRightAssoc2 ->
            if (((((ExpressionParser.add == context.SymbolId) 
                        || (ExpressionParser.Implicit == context.SymbolId)) 
                        || (ExpressionParser.EosSymbol == context.SymbolId)) 
                        || (ExpressionParser.Implicit4 == context.SymbolId))) {
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(ExpressionParser.FactorListRightAssoc2, "FactorListRightAssoc2", children, line, column, position);
            }
            context.Error("Expecting Implicit2, add, Implicit, #EOS, or Implicit4");
            return null;
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Term= Factor { ( "+" | "-" ) Factor }
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Term -> Factor TermList
        /// Term -> Factor
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
        /// Term -> Factor TermList
        /// Term -> Factor
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
        /// Term -> Factor TermList
        /// Term -> Factor
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
                        result = (result + ExpressionParser.EvaluateFactor(node.Children[i], state));
                    }
                    else {
                        result = (result - ExpressionParser.EvaluateFactor(node.Children[i], state));
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
        /// Term -> Factor TermList
        /// Term -> Factor
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
        /// Factor -> Unary FactorList
        /// Factor -> Unary
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
                    // Child always returns an object type so
                    // be sure to cast as necessary
                    if ((node.Children[(i - 1)].SymbolId == ParsleyDemo.ExpressionParser.mul)) {
                        result = (result * ((int)(ExpressionParser._EvaluateAny(node.Children[i], state))));
                    }
                    else {
                        result = (result / ((int)(ExpressionParser._EvaluateAny(node.Children[i], state))));
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
        /// Factor -> Unary FactorList
        /// Factor -> Unary
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
        /// Unary -> Implicit Unary
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
        /// Unary -> Implicit Unary
        /// Unary -> Leaf
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateUnary(ParseNode node) {
            return ExpressionParser.EvaluateUnary(node, null);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Leaf= integer | identifier | "(" Term ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> integer
        /// Leaf -> identifier
        /// Leaf -> Implicit3 Term Implicit4
        /// </remarks>
        /// <param name="node">The <see cref="ParseNode"/> to evaluate</param>
        /// <param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        /// <returns>The result of the evaluation</returns>
        public static int EvaluateLeaf(ParseNode node, object state) {
            if ((ExpressionParser.Leaf == node.SymbolId)) {
                if ((node.Children.Length == 1)) {
                    if ((node.Children[0].SymbolId == ParsleyDemo.ExpressionParser.integer)) {
                        return ((int)(ExpressionParser._ChangeType(node.Children[0].Value, typeof(int))));
                    }
                    else {
                        if ((state != null)) {
                            int val;
                            IDictionary<string, int> d = ((IDictionary<string, int>)(state));
                            if (d.TryGetValue(node.Children[0].Value, out val)) {
                                return ((int)(ExpressionParser._ChangeType(val, typeof(int))));
                            }
                        }
                        throw new SyntaxException(string.Format("Reference to undefined variable {0}", node.Children[0].Value), node.Line, node.Column, node.Position);
                    }
                }
                else {
                    return ((int)(ExpressionParser._ChangeType(ExpressionParser.EvaluateTerm(node.Children[1], state), typeof(int))));
                }
            }
            throw new SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position);
        }
        /// <summary>
        /// Evaluates a derivation of the form:
        /// Leaf= integer | identifier | "(" Term ")"
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Leaf -> integer
        /// Leaf -> identifier
        /// Leaf -> Implicit3 Term Implicit4
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
        private static object _EvaluateAny(ParseNode node, object state) {
            if ((node.SymbolId == ExpressionParser.Term)) {
                return ExpressionParser.EvaluateTerm(node, state);
            }
            if ((node.SymbolId == ExpressionParser.Factor)) {
                return ExpressionParser.EvaluateFactor(node, state);
            }
            if ((node.SymbolId == ExpressionParser.Unary)) {
                return ExpressionParser.EvaluateUnary(node, state);
            }
            if ((node.SymbolId == ExpressionParser.Leaf)) {
                return ExpressionParser.EvaluateLeaf(node, state);
            }
            if ((node.SymbolId == ExpressionParser.add)) {
                return node.Value;
            }
            if ((node.SymbolId == ExpressionParser.mul)) {
                return node.Value;
            }
            if ((node.SymbolId == ExpressionParser.integer)) {
                return node.Value;
            }
            if ((node.SymbolId == ExpressionParser.identifier)) {
                return node.Value;
            }
            return null;
        }
    }
}
