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
    using System.Text;
    using System.Collections.Generic;
    
    /// <summary>Parses the indicated grammar. Refer to C:\Users\honey\source\repos\BuildPack\ParsleyDemo\json.xbnf</summary>
    [System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.2.0")]
    internal partial class JsonParser {
        internal const int ErrorSymbol = -1;
        internal const int EosSymbol = -2;
        public const int Json = 0;
        public const int Field = 1;
        public const int Value = 2;
        public const int Boolean = 3;
        public const int Object = 10;
        public const int Array = 12;
        public const int number = 14;
        public const int @string = 15;
        public const int @true = 16;
        public const int @false = 17;
        public const int @null = 18;
        public const int lbracket = 19;
        public const int rbracket = 20;
        public const int lbrace = 21;
        public const int rbrace = 22;
        public const int colon = 23;
        public const int comma = 24;
        internal static ParseNode ParseJson(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Json -> Object
            if ((JsonParser.lbrace == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                children[0] = JsonParser.ParseObject(context);
                return new ParseNode(0, "Json", children, line, column, position);
            }
            // Json -> Array
            if ((JsonParser.lbracket == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                children[0] = JsonParser.ParseArray(context);
                return new ParseNode(0, "Json", children, line, column, position);
            }
            throw new SyntaxException("Expecting Object or Array", line, column, position);
        }
        /// <summary>
        /// Parses a production of the form:
        /// Json= Object | Array
        /// </summary>
        /// <remarks>
        /// The production rules are:
        /// Json -> Object
        /// Json -> Array
        /// </remarks>
        /// <param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        public static ParseNode Parse(System.Collections.Generic.IEnumerable<Token> tokenizer) {
            ParserContext context = new ParserContext(tokenizer);
            context.EnsureStarted();
            ParseNode result = JsonParser.ParseJson(context);
            if ((false == context.IsEnded)) {
                context.Error("Unexpected remainder in input.");
            }
            return result;
        }
        internal static ParseNode ParseField(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Field -> string colon Value
            if ((JsonParser.@string == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.@string == context.SymbolId))) {
                    context.Error("Expecting string");
                }
                children.Add(new ParseNode(JsonParser.@string, "string", context.Value, context.Line, context.Column, context.Position));
                context.Advance();
                if ((false 
                            == (JsonParser.colon == context.SymbolId))) {
                    context.Error("Expecting colon");
                }
                context.Advance();
                children.AddRange(JsonParser.ParseValue(context).Children);
                return new ParseNode(1, "Field", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting string", line, column, position);
        }
        internal static ParseNode ParseValue(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Value -> string
            if ((JsonParser.@string == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                if ((false 
                            == (JsonParser.@string == context.SymbolId))) {
                    context.Error("Expecting string");
                }
                children[0] = new ParseNode(JsonParser.@string, "string", context.Value, context.Line, context.Column, context.Position);
                context.Advance();
                return new ParseNode(2, "Value", children, line, column, position);
            }
            // Value -> number
            if ((JsonParser.number == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                if ((false 
                            == (JsonParser.number == context.SymbolId))) {
                    context.Error("Expecting number");
                }
                children[0] = new ParseNode(JsonParser.number, "number", context.Value, context.Line, context.Column, context.Position);
                context.Advance();
                return new ParseNode(2, "Value", children, line, column, position);
            }
            // Value -> Object
            if ((JsonParser.lbrace == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                children[0] = JsonParser.ParseObject(context);
                return new ParseNode(2, "Value", children, line, column, position);
            }
            // Value -> Array
            if ((JsonParser.lbracket == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                children[0] = JsonParser.ParseArray(context);
                return new ParseNode(2, "Value", children, line, column, position);
            }
            // Value -> Boolean
            if (((JsonParser.@true == context.SymbolId) 
                        || (JsonParser.@false == context.SymbolId))) {
                ParseNode[] children = new ParseNode[1];
                children[0] = JsonParser.ParseBoolean(context);
                return new ParseNode(2, "Value", children, line, column, position);
            }
            // Value -> null
            if ((JsonParser.@null == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                if ((false 
                            == (JsonParser.@null == context.SymbolId))) {
                    context.Error("Expecting null");
                }
                children[0] = new ParseNode(JsonParser.@null, "null", context.Value, context.Line, context.Column, context.Position);
                context.Advance();
                return new ParseNode(2, "Value", children, line, column, position);
            }
            throw new SyntaxException("Expecting string, number, Object, Array, Boolean, or null", line, column, position);
        }
        internal static ParseNode ParseBoolean(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Boolean -> true
            if ((JsonParser.@true == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                if ((false 
                            == (JsonParser.@true == context.SymbolId))) {
                    context.Error("Expecting true");
                }
                children[0] = new ParseNode(JsonParser.@true, "true", context.Value, context.Line, context.Column, context.Position);
                context.Advance();
                return new ParseNode(3, "Boolean", children, line, column, position);
            }
            // Boolean -> false
            if ((JsonParser.@false == context.SymbolId)) {
                ParseNode[] children = new ParseNode[1];
                if ((false 
                            == (JsonParser.@false == context.SymbolId))) {
                    context.Error("Expecting false");
                }
                children[0] = new ParseNode(JsonParser.@false, "false", context.Value, context.Line, context.Column, context.Position);
                context.Advance();
                return new ParseNode(3, "Boolean", children, line, column, position);
            }
            throw new SyntaxException("Expecting true or false", line, column, position);
        }
        internal static ParseNode ParseObjectList(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ObjectList -> comma Field ObjectListRightAssoc
            if ((JsonParser.comma == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.comma == context.SymbolId))) {
                    context.Error("Expecting comma");
                }
                context.Advance();
                children.Add(JsonParser.ParseField(context));
                children.AddRange(JsonParser.ParseObjectListRightAssoc(context).Children);
                return new ParseNode(4, "ObjectList", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting comma", line, column, position);
        }
        internal static ParseNode ParseArrayList(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ArrayList -> comma Value ArrayListRightAssoc
            if ((JsonParser.comma == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.comma == context.SymbolId))) {
                    context.Error("Expecting comma");
                }
                context.Advance();
                children.AddRange(JsonParser.ParseValue(context).Children);
                children.AddRange(JsonParser.ParseArrayListRightAssoc(context).Children);
                return new ParseNode(5, "ArrayList", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting comma", line, column, position);
        }
        internal static ParseNode ParseObjectListRightAssoc(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ObjectListRightAssoc -> comma Field ObjectListRightAssoc
            if ((JsonParser.comma == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.comma == context.SymbolId))) {
                    context.Error("Expecting comma");
                }
                context.Advance();
                children.Add(JsonParser.ParseField(context));
                children.AddRange(JsonParser.ParseObjectListRightAssoc(context).Children);
                return new ParseNode(6, "ObjectListRightAssoc", children.ToArray(), line, column, position);
            }
            // ObjectListRightAssoc ->
            if ((JsonParser.rbrace == context.SymbolId)) {
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(6, "ObjectListRightAssoc", children, line, column, position);
            }
            throw new SyntaxException("Expecting comma", line, column, position);
        }
        internal static ParseNode ParseArrayListRightAssoc(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ArrayListRightAssoc -> comma Value ArrayListRightAssoc
            if ((JsonParser.comma == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.comma == context.SymbolId))) {
                    context.Error("Expecting comma");
                }
                context.Advance();
                children.AddRange(JsonParser.ParseValue(context).Children);
                children.AddRange(JsonParser.ParseArrayListRightAssoc(context).Children);
                return new ParseNode(7, "ArrayListRightAssoc", children.ToArray(), line, column, position);
            }
            // ArrayListRightAssoc ->
            if ((JsonParser.rbracket == context.SymbolId)) {
                ParseNode[] children = new ParseNode[0];
                return new ParseNode(7, "ArrayListRightAssoc", children, line, column, position);
            }
            throw new SyntaxException("Expecting comma", line, column, position);
        }
        internal static ParseNode ParseObjectPart(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ObjectPart -> ObjectList rbrace
            if ((JsonParser.comma == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.AddRange(JsonParser.ParseObjectList(context).Children);
                if ((false 
                            == (JsonParser.rbrace == context.SymbolId))) {
                    context.Error("Expecting rbrace");
                }
                context.Advance();
                return new ParseNode(8, "ObjectPart", children.ToArray(), line, column, position);
            }
            // ObjectPart -> rbrace
            if ((JsonParser.rbrace == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.rbrace == context.SymbolId))) {
                    context.Error("Expecting rbrace");
                }
                context.Advance();
                return new ParseNode(8, "ObjectPart", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting ObjectList or rbrace", line, column, position);
        }
        internal static ParseNode ParseArrayPart(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ArrayPart -> ArrayList rbracket
            if ((JsonParser.comma == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.AddRange(JsonParser.ParseArrayList(context).Children);
                if ((false 
                            == (JsonParser.rbracket == context.SymbolId))) {
                    context.Error("Expecting rbracket");
                }
                context.Advance();
                return new ParseNode(9, "ArrayPart", children.ToArray(), line, column, position);
            }
            // ArrayPart -> rbracket
            if ((JsonParser.rbracket == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.rbracket == context.SymbolId))) {
                    context.Error("Expecting rbracket");
                }
                context.Advance();
                return new ParseNode(9, "ArrayPart", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting ArrayList or rbracket", line, column, position);
        }
        internal static ParseNode ParseObject(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Object -> lbrace ObjectPart2
            if ((JsonParser.lbrace == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.lbrace == context.SymbolId))) {
                    context.Error("Expecting lbrace");
                }
                context.Advance();
                children.AddRange(JsonParser.ParseObjectPart2(context).Children);
                return new ParseNode(10, "Object", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting lbrace", line, column, position);
        }
        internal static ParseNode ParseObjectPart2(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ObjectPart2 -> rbrace
            if ((JsonParser.rbrace == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.rbrace == context.SymbolId))) {
                    context.Error("Expecting rbrace");
                }
                context.Advance();
                return new ParseNode(11, "ObjectPart2", children.ToArray(), line, column, position);
            }
            // ObjectPart2 -> Field ObjectPart
            if ((JsonParser.@string == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.Add(JsonParser.ParseField(context));
                children.AddRange(JsonParser.ParseObjectPart(context).Children);
                return new ParseNode(11, "ObjectPart2", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting rbrace or Field", line, column, position);
        }
        internal static ParseNode ParseArray(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // Array -> lbracket ArrayPart2
            if ((JsonParser.lbracket == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.lbracket == context.SymbolId))) {
                    context.Error("Expecting lbracket");
                }
                context.Advance();
                children.AddRange(JsonParser.ParseArrayPart2(context).Children);
                return new ParseNode(12, "Array", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting lbracket", line, column, position);
        }
        internal static ParseNode ParseArrayPart2(ParserContext context) {
            int line = context.Line;
            int column = context.Column;
            long position = context.Position;
            // ArrayPart2 -> rbracket
            if ((JsonParser.rbracket == context.SymbolId)) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                if ((false 
                            == (JsonParser.rbracket == context.SymbolId))) {
                    context.Error("Expecting rbracket");
                }
                context.Advance();
                return new ParseNode(13, "ArrayPart2", children.ToArray(), line, column, position);
            }
            // ArrayPart2 -> Value ArrayPart
            if ((((((((JsonParser.@string == context.SymbolId) 
                        || (JsonParser.number == context.SymbolId)) 
                        || (JsonParser.lbrace == context.SymbolId)) 
                        || (JsonParser.lbracket == context.SymbolId)) 
                        || (JsonParser.@true == context.SymbolId)) 
                        || (JsonParser.@false == context.SymbolId)) 
                        || (JsonParser.@null == context.SymbolId))) {
                System.Collections.Generic.List<ParseNode> children = new System.Collections.Generic.List<ParseNode>();
                children.AddRange(JsonParser.ParseValue(context).Children);
                children.AddRange(JsonParser.ParseArrayPart(context).Children);
                return new ParseNode(13, "ArrayPart2", children.ToArray(), line, column, position);
            }
            throw new SyntaxException("Expecting rbracket or Value", line, column, position);
        }
    }
}
