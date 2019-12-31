'------------------------------------------------------------------------------
' <auto-generated>
'     This code was generated by a tool.
'     Runtime Version:4.0.30319.42000
'
'     Changes to this file may cause incorrect behavior and will be lost if
'     the code is regenerated.
' </auto-generated>
'------------------------------------------------------------------------------

Option Strict Off
Option Explicit On

Imports System
Imports System.Collections.Generic

Namespace ParsleyDemo
    '''<summary>Parses the following grammar:
    '''Json= Object | Array;
    '''Object= "{" [ Field { "," Field } ] "}";
    '''Field= string ":" Value;
    '''Array= "[" [ Value { "," Value } ] "]";
    '''{Value}= string | number | Object | Array | Boolean | null;
    '''Boolean= true | false;
    '''number= '\-?(0|[1-9][0-9]*)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?';
    '''string= '"([^\n"\\]|\\([btrnf"\\/]|(u[A-Fa-f]{4})))*"';
    '''true= "true";
    '''false= "false";
    '''null= "null";
    '''{lbracket}= "[";
    '''{rbracket}= "]";
    '''{lbrace}= "{";
    '''{rbrace}= "}";
    '''{colon}= ":";
    '''{comma}= ",";
    '''(whitespace)= '[\n\r\t ]+';
    '''</summary>
    '''<remarks>The rules for the factored grammar are as follows:
    '''Json -> Object
    '''Json -> Array
    '''Field -> string colon Value
    '''Value -> string
    '''Value -> number
    '''Value -> Object
    '''Value -> Array
    '''Value -> Boolean
    '''Value -> null
    '''Boolean -> true
    '''Boolean -> false
    '''ObjectList -> comma Field ObjectListRightAssoc
    '''ArrayList -> comma Value ArrayListRightAssoc
    '''ObjectListRightAssoc -> comma Field ObjectListRightAssoc
    '''ObjectListRightAssoc ->
    '''ArrayListRightAssoc -> comma Value ArrayListRightAssoc
    '''ArrayListRightAssoc ->
    '''ObjectPart -> ObjectList rbrace
    '''ObjectPart -> rbrace
    '''ArrayPart -> ArrayList rbracket
    '''ArrayPart -> rbracket
    '''Object -> lbrace ObjectPart2
    '''ObjectPart2 -> rbrace
    '''ObjectPart2 -> Field ObjectPart
    '''Array -> lbracket ArrayPart2
    '''ArrayPart2 -> rbracket
    '''ArrayPart2 -> Value ArrayPart
    '''</remarks>
    <System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.2.0")>  _
    Partial Friend Class JsonParser
        Friend Const ErrorSymbol As Integer = -1
        Friend Const EosSymbol As Integer = -2
        Public Const Json As Integer = 0
        Public Const Field As Integer = 1
        Public Const Value As Integer = 2
        Public Const [Boolean] As Integer = 3
        Public Const ObjectList As Integer = 4
        Public Const ArrayList As Integer = 5
        Public Const ObjectListRightAssoc As Integer = 6
        Public Const ArrayListRightAssoc As Integer = 7
        Public Const ObjectPart As Integer = 8
        Public Const ArrayPart As Integer = 9
        Public Const [Object] As Integer = 10
        Public Const ObjectPart2 As Integer = 11
        Public Const Array As Integer = 12
        Public Const ArrayPart2 As Integer = 13
        Public Const [string] As Integer = 14
        Public Const colon As Integer = 15
        Public Const number As Integer = 16
        Public Const null As Integer = 17
        Public Const [true] As Integer = 18
        Public Const [false] As Integer = 19
        Public Const comma As Integer = 20
        Public Const rbrace As Integer = 21
        Public Const rbracket As Integer = 22
        Public Const lbrace As Integer = 23
        Public Const lbracket As Integer = 24
        Public Const whitespace As Integer = 25
        Private Overloads Shared Function ParseJson(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'Json -> Object
            If (JsonParser.lbrace = context.SymbolId) Then
                Dim children(0) As ParseNode
                children(0) = JsonParser.ParseObject(context)
                Return New ParseNode(JsonParser.Json, "Json", children, line__, column__, position__)
            End If
            'Json -> Array
            If (JsonParser.lbracket = context.SymbolId) Then
                Dim children(0) As ParseNode
                children(0) = JsonParser.ParseArray(context)
                Return New ParseNode(JsonParser.Json, "Json", children, line__, column__, position__)
            End If
            context.Error("Expecting lbrace or lbracket at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Json= Object | Array
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Json -> Object
        '''Json -> Array
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Overloads Shared Function ParseJson(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return JsonParser.ParseJson(context)
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Json= Object | Array
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Json -> Object
        '''Json -> Array
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Shared Function Parse(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return JsonParser.ParseJson(context)
        End Function
        Private Overloads Shared Function ParseField(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'Field -> string colon Value
            If (JsonParser.[string] = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.[string] = context.SymbolId)) Then
                    context.Error("Expecting string at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                children.Add(New ParseNode(JsonParser.[string], "string", context.Value, context.Line, context.Column, context.Position))
                context.Advance
                If (false  _
                            = (JsonParser.colon = context.SymbolId)) Then
                    context.Error("Expecting colon at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                children.AddRange(JsonParser.ParseValue(context).Children)
                Return New ParseNode(JsonParser.Field, "Field", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting string at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Field= string ":" Value
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Field -> string colon Value
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Overloads Shared Function ParseField(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return JsonParser.ParseField(context)
        End Function
        Private Shared Function ParseValue(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'Value -> string
            If (JsonParser.[string] = context.SymbolId) Then
                Dim children(0) As ParseNode
                If (false  _
                            = (JsonParser.[string] = context.SymbolId)) Then
                    context.Error("Expecting string at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                children(0) = New ParseNode(JsonParser.[string], "string", context.Value, context.Line, context.Column, context.Position)
                context.Advance
                Return New ParseNode(JsonParser.Value, "Value", children, line__, column__, position__)
            End If
            'Value -> number
            If (JsonParser.number = context.SymbolId) Then
                Dim children(0) As ParseNode
                If (false  _
                            = (JsonParser.number = context.SymbolId)) Then
                    context.Error("Expecting number at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                children(0) = New ParseNode(JsonParser.number, "number", context.Value, context.Line, context.Column, context.Position)
                context.Advance
                Return New ParseNode(JsonParser.Value, "Value", children, line__, column__, position__)
            End If
            'Value -> Object
            If (JsonParser.lbrace = context.SymbolId) Then
                Dim children(0) As ParseNode
                children(0) = JsonParser.ParseObject(context)
                Return New ParseNode(JsonParser.Value, "Value", children, line__, column__, position__)
            End If
            'Value -> Array
            If (JsonParser.lbracket = context.SymbolId) Then
                Dim children(0) As ParseNode
                children(0) = JsonParser.ParseArray(context)
                Return New ParseNode(JsonParser.Value, "Value", children, line__, column__, position__)
            End If
            'Value -> Boolean
            If ((JsonParser.[true] = context.SymbolId)  _
                        OrElse (JsonParser.[false] = context.SymbolId)) Then
                Dim children(0) As ParseNode
                children(0) = JsonParser.ParseBoolean(context)
                Return New ParseNode(JsonParser.Value, "Value", children, line__, column__, position__)
            End If
            'Value -> null
            If (JsonParser.null = context.SymbolId) Then
                Dim children(0) As ParseNode
                If (false  _
                            = (JsonParser.null = context.SymbolId)) Then
                    context.Error("Expecting null at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                children(0) = New ParseNode(JsonParser.null, "null", context.Value, context.Line, context.Column, context.Position)
                context.Advance
                Return New ParseNode(JsonParser.Value, "Value", children, line__, column__, position__)
            End If
            context.Error("Expecting string, number, lbrace, lbracket, true, false, or null at line {0}, col"& _ 
                    "umn {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Overloads Shared Function ParseBoolean(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'Boolean -> true
            If (JsonParser.[true] = context.SymbolId) Then
                Dim children(0) As ParseNode
                If (false  _
                            = (JsonParser.[true] = context.SymbolId)) Then
                    context.Error("Expecting true at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                children(0) = New ParseNode(JsonParser.[true], "true", context.Value, context.Line, context.Column, context.Position)
                context.Advance
                Return New ParseNode(JsonParser.[Boolean], "Boolean", children, line__, column__, position__)
            End If
            'Boolean -> false
            If (JsonParser.[false] = context.SymbolId) Then
                Dim children(0) As ParseNode
                If (false  _
                            = (JsonParser.[false] = context.SymbolId)) Then
                    context.Error("Expecting false at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                children(0) = New ParseNode(JsonParser.[false], "false", context.Value, context.Line, context.Column, context.Position)
                context.Advance
                Return New ParseNode(JsonParser.[Boolean], "Boolean", children, line__, column__, position__)
            End If
            context.Error("Expecting true or false at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Boolean= true | false
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Boolean -> true
        '''Boolean -> false
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Overloads Shared Function ParseBoolean(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return JsonParser.ParseBoolean(context)
        End Function
        Private Shared Function ParseObjectList(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ObjectList -> comma Field ObjectListRightAssoc
            If (JsonParser.comma = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.comma = context.SymbolId)) Then
                    context.Error("Expecting comma at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                children.Add(JsonParser.ParseField(context))
                children.AddRange(JsonParser.ParseObjectListRightAssoc(context).Children)
                Return New ParseNode(JsonParser.ObjectList, "ObjectList", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting comma at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Shared Function ParseArrayList(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ArrayList -> comma Value ArrayListRightAssoc
            If (JsonParser.comma = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.comma = context.SymbolId)) Then
                    context.Error("Expecting comma at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                children.AddRange(JsonParser.ParseValue(context).Children)
                children.AddRange(JsonParser.ParseArrayListRightAssoc(context).Children)
                Return New ParseNode(JsonParser.ArrayList, "ArrayList", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting comma at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Shared Function ParseObjectListRightAssoc(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ObjectListRightAssoc -> comma Field ObjectListRightAssoc
            If (JsonParser.comma = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.comma = context.SymbolId)) Then
                    context.Error("Expecting comma at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                children.Add(JsonParser.ParseField(context))
                children.AddRange(JsonParser.ParseObjectListRightAssoc(context).Children)
                Return New ParseNode(JsonParser.ObjectListRightAssoc, "ObjectListRightAssoc", children.ToArray, line__, column__, position__)
            End If
            'ObjectListRightAssoc ->
            If (JsonParser.rbrace = context.SymbolId) Then
                Dim children(-1) As ParseNode
                Return New ParseNode(JsonParser.ObjectListRightAssoc, "ObjectListRightAssoc", children, line__, column__, position__)
            End If
            context.Error("Expecting comma or rbrace at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Shared Function ParseArrayListRightAssoc(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ArrayListRightAssoc -> comma Value ArrayListRightAssoc
            If (JsonParser.comma = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.comma = context.SymbolId)) Then
                    context.Error("Expecting comma at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                children.AddRange(JsonParser.ParseValue(context).Children)
                children.AddRange(JsonParser.ParseArrayListRightAssoc(context).Children)
                Return New ParseNode(JsonParser.ArrayListRightAssoc, "ArrayListRightAssoc", children.ToArray, line__, column__, position__)
            End If
            'ArrayListRightAssoc ->
            If (JsonParser.rbracket = context.SymbolId) Then
                Dim children(-1) As ParseNode
                Return New ParseNode(JsonParser.ArrayListRightAssoc, "ArrayListRightAssoc", children, line__, column__, position__)
            End If
            context.Error("Expecting comma or rbracket at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Shared Function ParseObjectPart(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ObjectPart -> ObjectList rbrace
            If (JsonParser.comma = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.AddRange(JsonParser.ParseObjectList(context).Children)
                If (false  _
                            = (JsonParser.rbrace = context.SymbolId)) Then
                    context.Error("Expecting rbrace at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                Return New ParseNode(JsonParser.ObjectPart, "ObjectPart", children.ToArray, line__, column__, position__)
            End If
            'ObjectPart -> rbrace
            If (JsonParser.rbrace = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.rbrace = context.SymbolId)) Then
                    context.Error("Expecting rbrace at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                Return New ParseNode(JsonParser.ObjectPart, "ObjectPart", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting comma or rbrace at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Shared Function ParseArrayPart(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ArrayPart -> ArrayList rbracket
            If (JsonParser.comma = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.AddRange(JsonParser.ParseArrayList(context).Children)
                If (false  _
                            = (JsonParser.rbracket = context.SymbolId)) Then
                    context.Error("Expecting rbracket at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                Return New ParseNode(JsonParser.ArrayPart, "ArrayPart", children.ToArray, line__, column__, position__)
            End If
            'ArrayPart -> rbracket
            If (JsonParser.rbracket = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.rbracket = context.SymbolId)) Then
                    context.Error("Expecting rbracket at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                Return New ParseNode(JsonParser.ArrayPart, "ArrayPart", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting comma or rbracket at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Overloads Shared Function ParseObject(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'Object -> lbrace ObjectPart2
            If (JsonParser.lbrace = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.lbrace = context.SymbolId)) Then
                    context.Error("Expecting lbrace at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                children.AddRange(JsonParser.ParseObjectPart2(context).Children)
                Return New ParseNode(JsonParser.[Object], "Object", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting lbrace at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Object= "{" [ Field { "," Field } ] "}"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Object -> lbrace ObjectPart2
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Overloads Shared Function ParseObject(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return JsonParser.ParseObject(context)
        End Function
        Private Shared Function ParseObjectPart2(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ObjectPart2 -> rbrace
            If (JsonParser.rbrace = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.rbrace = context.SymbolId)) Then
                    context.Error("Expecting rbrace at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                Return New ParseNode(JsonParser.ObjectPart2, "ObjectPart2", children.ToArray, line__, column__, position__)
            End If
            'ObjectPart2 -> Field ObjectPart
            If (JsonParser.[string] = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(JsonParser.ParseField(context))
                children.AddRange(JsonParser.ParseObjectPart(context).Children)
                Return New ParseNode(JsonParser.ObjectPart2, "ObjectPart2", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting rbrace or string at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        Private Overloads Shared Function ParseArray(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'Array -> lbracket ArrayPart2
            If (JsonParser.lbracket = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.lbracket = context.SymbolId)) Then
                    context.Error("Expecting lbracket at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                children.AddRange(JsonParser.ParseArrayPart2(context).Children)
                Return New ParseNode(JsonParser.Array, "Array", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting lbracket at line {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Array= "[" [ Value { "," Value } ] "]"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Array -> lbracket ArrayPart2
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Overloads Shared Function ParseArray(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return JsonParser.ParseArray(context)
        End Function
        Private Shared Function ParseArrayPart2(ByVal context As ParserContext) As ParseNode
            Dim line__ As Integer = context.Line
            Dim column__ As Integer = context.Column
            Dim position__ As Long = context.Position
            'ArrayPart2 -> rbracket
            If (JsonParser.rbracket = context.SymbolId) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                If (false  _
                            = (JsonParser.rbracket = context.SymbolId)) Then
                    context.Error("Expecting rbracket at line {0}, column {1}, position {2}", context.Line, context.Column, context.Position)
                End If
                context.Advance
                Return New ParseNode(JsonParser.ArrayPart2, "ArrayPart2", children.ToArray, line__, column__, position__)
            End If
            'ArrayPart2 -> Value ArrayPart
            If (((((((JsonParser.[string] = context.SymbolId)  _
                        OrElse (JsonParser.number = context.SymbolId))  _
                        OrElse (JsonParser.lbrace = context.SymbolId))  _
                        OrElse (JsonParser.lbracket = context.SymbolId))  _
                        OrElse (JsonParser.[true] = context.SymbolId))  _
                        OrElse (JsonParser.[false] = context.SymbolId))  _
                        OrElse (JsonParser.null = context.SymbolId)) Then
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.AddRange(JsonParser.ParseValue(context).Children)
                children.AddRange(JsonParser.ParseArrayPart(context).Children)
                Return New ParseNode(JsonParser.ArrayPart2, "ArrayPart2", children.ToArray, line__, column__, position__)
            End If
            context.Error("Expecting rbracket, string, number, lbrace, lbracket, true, false, or null at lin"& _ 
                    "e {0}, column {1}, position {2}", line__, column__, position__)
            Return Nothing
        End Function
    End Class
End Namespace
