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
    '''Term= Factor { ( "+" | "-" ) Factor };
    '''Factor= Unary { ( "*" | "/" ) Unary };
    '''Unary= ( "+" | "-" ) Unary | Leaf;
    '''Leaf= integer | identifier | "(" Term ")";
    '''add= "+";
    '''mul= "*";
    '''integer= '[0-9]+';
    '''identifier= '[A-Z_a-z][0-9A-Z_a-z]*';
    '''(whitespace)= '[ \t\r\n]+';
    '''(lineComment)= "//";
    '''(blockComment)= "/*";
    '''</summary>
    '''<remarks>The rules for the factored grammar are as follows:
    '''Unary -> add Unary
    '''Unary -> Implicit Unary
    '''Unary -> Leaf
    '''Leaf -> integer
    '''Leaf -> identifier
    '''Leaf -> Implicit3 Term Implicit4
    '''TermList -> add Factor TermListRightAssoc TermListRightAssoc2
    '''TermList -> Implicit Factor TermListRightAssoc TermListRightAssoc2
    '''FactorList -> mul Unary FactorListRightAssoc FactorListRightAssoc2
    '''FactorList -> Implicit2 Unary FactorListRightAssoc FactorListRightAssoc2
    '''TermListRightAssoc -> add Factor TermListRightAssoc
    '''TermListRightAssoc ->
    '''FactorListRightAssoc -> mul Unary FactorListRightAssoc
    '''FactorListRightAssoc ->
    '''Term -> Factor TermPart
    '''TermPart -> TermList
    '''TermPart ->
    '''Factor -> Unary FactorPart
    '''FactorPart -> FactorList
    '''FactorPart ->
    '''TermListRightAssoc2 -> Implicit Factor TermListRightAssoc2
    '''TermListRightAssoc2 ->
    '''FactorListRightAssoc2 -> Implicit2 Unary FactorListRightAssoc2
    '''FactorListRightAssoc2 ->
    '''</remarks>
    <System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")>  _
    Partial Friend Class ExpressionParser
        Friend Const ErrorSymbol As Integer = -1
        Friend Const EosSymbol As Integer = -2
        Public Const Unary As Integer = 0
        Public Const Leaf As Integer = 1
        Public Const TermList As Integer = 2
        Public Const FactorList As Integer = 3
        Public Const TermListRightAssoc As Integer = 4
        Public Const FactorListRightAssoc As Integer = 5
        Public Const Term As Integer = 6
        Public Const TermPart As Integer = 7
        Public Const Factor As Integer = 8
        Public Const FactorPart As Integer = 9
        Public Const TermListRightAssoc2 As Integer = 10
        Public Const FactorListRightAssoc2 As Integer = 11
        Public Const add As Integer = 12
        Public Const Implicit As Integer = 13
        Public Const [integer] As Integer = 14
        Public Const identifier As Integer = 15
        Public Const Implicit3 As Integer = 16
        Public Const Implicit4 As Integer = 17
        Public Const mul As Integer = 18
        Public Const Implicit2 As Integer = 19
        Public Const whitespace As Integer = 20
        Public Const lineComment As Integer = 21
        Public Const blockComment As Integer = 22
        Private Shared Function _ParseUnary(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.add = context.SymbolId) Then
                'Unary -> add Unary
                Dim children(1) As ParseNode
                children(0) = New ParseNode(ExpressionParser.add, "add", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseUnary(context)
                Return New ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position)
            End If
            If (ExpressionParser.Implicit = context.SymbolId) Then
                'Unary -> Implicit Unary
                Dim children(1) As ParseNode
                children(0) = New ParseNode(ExpressionParser.Implicit, "Implicit", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseUnary(context)
                Return New ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position)
            End If
            If (((ExpressionParser.[integer] = context.SymbolId)  _
                        OrElse (ExpressionParser.identifier = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit3 = context.SymbolId)) Then
                'Unary -> Leaf
                Dim children(0) As ParseNode
                children(0) = ExpressionParser._ParseLeaf(context)
                Return New ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position)
            End If
            context.Error("Expecting add, Implicit, integer, identifier, or Implicit3")
            Return Nothing
        End Function
        Private Shared Function _ParseLeaf(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.[integer] = context.SymbolId) Then
                'Leaf -> integer
                Dim children(0) As ParseNode
                children(0) = New ParseNode(ExpressionParser.[integer], "integer", context.Value, line, column, position)
                context.Advance
                Return New ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position)
            End If
            If (ExpressionParser.identifier = context.SymbolId) Then
                'Leaf -> identifier
                Dim children(0) As ParseNode
                children(0) = New ParseNode(ExpressionParser.identifier, "identifier", context.Value, line, column, position)
                context.Advance
                Return New ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position)
            End If
            If (ExpressionParser.Implicit3 = context.SymbolId) Then
                'Leaf -> Implicit3 Term Implicit4
                Dim children(2) As ParseNode
                children(0) = New ParseNode(ExpressionParser.Implicit3, "Implicit3", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseTerm(context)
                children(2) = New ParseNode(ExpressionParser.Implicit4, "Implicit4", context.Value, line, column, position)
                If (ExpressionParser.Implicit4 = context.SymbolId) Then
                    context.Advance
                    Return New ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position)
                End If
                context.Error("Expecting Implicit4")
            End If
            context.Error("Expecting integer, identifier, or Implicit3")
            Return Nothing
        End Function
        Private Shared Function _ParseTermList(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.add = context.SymbolId) Then
                'TermList -> add Factor TermListRightAssoc TermListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.add, "add", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.TermList, "TermList", children.ToArray, line, column, position)
            End If
            If (ExpressionParser.Implicit = context.SymbolId) Then
                'TermList -> Implicit Factor TermListRightAssoc TermListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.Implicit, "Implicit", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.TermList, "TermList", children.ToArray, line, column, position)
            End If
            context.Error("Expecting add or Implicit")
            Return Nothing
        End Function
        Private Shared Function _ParseFactorList(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.mul = context.SymbolId) Then
                'FactorList -> mul Unary FactorListRightAssoc FactorListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseUnary(context))
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.FactorList, "FactorList", children.ToArray, line, column, position)
            End If
            If (ExpressionParser.Implicit2 = context.SymbolId) Then
                'FactorList -> Implicit2 Unary FactorListRightAssoc FactorListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.Implicit2, "Implicit2", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseUnary(context))
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.FactorList, "FactorList", children.ToArray, line, column, position)
            End If
            context.Error("Expecting mul or Implicit2")
            Return Nothing
        End Function
        Private Shared Function _ParseTermListRightAssoc(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.add = context.SymbolId) Then
                'TermListRightAssoc -> add Factor TermListRightAssoc
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.add, "add", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseTermListRightAssoc(context).Children)
                Return New ParseNode(ExpressionParser.TermListRightAssoc, "TermListRightAssoc", children.ToArray, line, column, position)
            End If
            If (((ExpressionParser.Implicit = context.SymbolId)  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit4 = context.SymbolId)) Then
                'TermListRightAssoc ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.TermListRightAssoc, "TermListRightAssoc", children, line, column, position)
            End If
            context.Error("Expecting add, Implicit, #EOS, or Implicit4")
            Return Nothing
        End Function
        Private Shared Function _ParseFactorListRightAssoc(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.mul = context.SymbolId) Then
                'FactorListRightAssoc -> mul Unary FactorListRightAssoc
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseUnary(context))
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc(context).Children)
                Return New ParseNode(ExpressionParser.FactorListRightAssoc, "FactorListRightAssoc", children.ToArray, line, column, position)
            End If
            If (((((ExpressionParser.Implicit2 = context.SymbolId)  _
                        OrElse (ExpressionParser.add = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit = context.SymbolId))  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit4 = context.SymbolId)) Then
                'FactorListRightAssoc ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.FactorListRightAssoc, "FactorListRightAssoc", children, line, column, position)
            End If
            context.Error("Expecting mul, Implicit2, add, Implicit, #EOS, or Implicit4")
            Return Nothing
        End Function
        Private Shared Function _ParseTerm(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit = context.SymbolId))  _
                        OrElse (ExpressionParser.[integer] = context.SymbolId))  _
                        OrElse (ExpressionParser.identifier = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit3 = context.SymbolId)) Then
                'Term -> Factor TermPart
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseTermPart(context).Children)
                Return New ParseNode(ExpressionParser.Term, "Term", children.ToArray, line, column, position)
            End If
            context.Error("Expecting add, Implicit, integer, identifier, or Implicit3")
            Return Nothing
        End Function
        Private Shared Function _ParseTermPart(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If ((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit = context.SymbolId)) Then
                'TermPart -> TermList
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.AddRange(ExpressionParser._ParseTermList(context).Children)
                Return New ParseNode(ExpressionParser.TermPart, "TermPart", children.ToArray, line, column, position)
            End If
            If ((ExpressionParser.EosSymbol = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit4 = context.SymbolId)) Then
                'TermPart ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.TermPart, "TermPart", children, line, column, position)
            End If
            context.Error("Expecting add, Implicit, #EOS, or Implicit4")
            Return Nothing
        End Function
        Private Shared Function _ParseFactor(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit = context.SymbolId))  _
                        OrElse (ExpressionParser.[integer] = context.SymbolId))  _
                        OrElse (ExpressionParser.identifier = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit3 = context.SymbolId)) Then
                'Factor -> Unary FactorPart
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(ExpressionParser._ParseUnary(context))
                children.AddRange(ExpressionParser._ParseFactorPart(context).Children)
                Return New ParseNode(ExpressionParser.Factor, "Factor", children.ToArray, line, column, position)
            End If
            context.Error("Expecting add, Implicit, integer, identifier, or Implicit3")
            Return Nothing
        End Function
        Private Shared Function _ParseFactorPart(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If ((ExpressionParser.mul = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit2 = context.SymbolId)) Then
                'FactorPart -> FactorList
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.AddRange(ExpressionParser._ParseFactorList(context).Children)
                Return New ParseNode(ExpressionParser.FactorPart, "FactorPart", children.ToArray, line, column, position)
            End If
            If ((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit = context.SymbolId))  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit4 = context.SymbolId)) Then
                'FactorPart ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.FactorPart, "FactorPart", children, line, column, position)
            End If
            context.Error("Expecting mul, Implicit2, add, Implicit, #EOS, or Implicit4")
            Return Nothing
        End Function
        Private Shared Function _ParseTermListRightAssoc2(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.Implicit = context.SymbolId) Then
                'TermListRightAssoc2 -> Implicit Factor TermListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.Implicit, "Implicit", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseTermListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.TermListRightAssoc2, "TermListRightAssoc2", children.ToArray, line, column, position)
            End If
            If ((ExpressionParser.EosSymbol = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit4 = context.SymbolId)) Then
                'TermListRightAssoc2 ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.TermListRightAssoc2, "TermListRightAssoc2", children, line, column, position)
            End If
            context.Error("Expecting Implicit, #EOS, or Implicit4")
            Return Nothing
        End Function
        Private Shared Function _ParseFactorListRightAssoc2(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.Implicit2 = context.SymbolId) Then
                'FactorListRightAssoc2 -> Implicit2 Unary FactorListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.Implicit2, "Implicit2", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseUnary(context))
                children.AddRange(ExpressionParser._ParseFactorListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.FactorListRightAssoc2, "FactorListRightAssoc2", children.ToArray, line, column, position)
            End If
            If ((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.Implicit = context.SymbolId))  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.Implicit4 = context.SymbolId)) Then
                'FactorListRightAssoc2 ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.FactorListRightAssoc2, "FactorListRightAssoc2", children, line, column, position)
            End If
            context.Error("Expecting Implicit2, add, Implicit, #EOS, or Implicit4")
            Return Nothing
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Term= Factor { ( "+" | "-" ) Factor }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Term -> Factor TermPart
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Shared Function ParseTerm(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseTerm(context)
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Factor= Unary { ( "*" | "/" ) Unary }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Factor -> Unary FactorPart
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Shared Function ParseFactor(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseFactor(context)
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Unary= ( "+" | "-" ) Unary | Leaf
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Unary -> add Unary
        '''Unary -> Implicit Unary
        '''Unary -> Leaf
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Shared Function ParseUnary(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseUnary(context)
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Leaf= integer | identifier | "(" Term ")"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Leaf -> integer
        '''Leaf -> identifier
        '''Leaf -> Implicit3 Term Implicit4
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Shared Function ParseLeaf(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseLeaf(context)
        End Function
        '''<summary>
        '''Parses a derivation of the form:
        '''Term= Factor { ( "+" | "-" ) Factor }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Term -> Factor TermPart
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Shared Function Parse(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseTerm(context)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Term= Factor { ( "+" | "-" ) Factor }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Term -> Factor TermPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function Evaluate(ByVal node As ParseNode) As Object
            Return ExpressionParser.EvaluateTerm(node)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Term= Factor { ( "+" | "-" ) Factor }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Term -> Factor TermPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function Evaluate(ByVal node As ParseNode, ByVal state As Object) As Object
            Return ExpressionParser.EvaluateTerm(node, state)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Term= Factor { ( "+" | "-" ) Factor }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Term -> Factor TermPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateTerm(ByVal node As ParseNode, ByVal state As Object) As Object
            If (ExpressionParser.Term = node.SymbolId) Then
                Dim result As Integer = CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(0)),Integer)
                Dim i As Integer = 2

                Do While (i < node.Children.Length)
                    If (node.Children((i - 1)).SymbolId = ParsleyDemo.ExpressionParser.add) Then
                        result = (result + CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(i)),Integer))
                    Else
                        result = (result - CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(i)),Integer))
                    End If
                    i = (i + 2)

                Loop
                Return result
            End If
            Throw New SyntaxException("Expecting Term", node.Line, node.Column, node.Position)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Term= Factor { ( "+" | "-" ) Factor }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Term -> Factor TermPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateTerm(ByVal node As ParseNode) As Object
            Return ExpressionParser.EvaluateTerm(node, Nothing)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Factor= Unary { ( "*" | "/" ) Unary }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Factor -> Unary FactorPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateFactor(ByVal node As ParseNode, ByVal state As Object) As Object
            If (ExpressionParser.Factor = node.SymbolId) Then
                Dim result As Integer = CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(0)),Integer)
                Dim i As Integer = 2

                Do While (i < node.Children.Length)
                    If (node.Children(i).SymbolId = ParsleyDemo.ExpressionParser.Unary) Then
                        If (node.Children((i - 1)).SymbolId = ParsleyDemo.ExpressionParser.mul) Then
                            result = (result * CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(i)),Integer))
                        Else
                            result = (result / CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(i)),Integer))
                        End If
                    Else
                        If (node.Children((i - 1)).SymbolId = ParsleyDemo.ExpressionParser.mul) Then
                            result = (result * CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(i)),Integer))
                        Else
                            result = (result / CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(i)),Integer))
                        End If
                    End If
                    i = (i + 2)

                Loop
                Return result
            End If
            Throw New SyntaxException("Expecting Factor", node.Line, node.Column, node.Position)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Factor= Unary { ( "*" | "/" ) Unary }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Factor -> Unary FactorPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateFactor(ByVal node As ParseNode) As Object
            Return ExpressionParser.EvaluateFactor(node, Nothing)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Unary= ( "+" | "-" ) Unary | Leaf
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Unary -> add Unary
        '''Unary -> Implicit Unary
        '''Unary -> Leaf
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateUnary(ByVal node As ParseNode, ByVal state As Object) As Object
            If (ExpressionParser.Unary = node.SymbolId) Then
                If (node.Children.Length = 1) Then
                    Return ParsleyDemo.ExpressionParser.EvaluateLeaf(node.Children(0))
                End If
                If (node.Children(0).SymbolId = ParsleyDemo.ExpressionParser.add) Then
                    Return ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(1))
                Else
                    Return (0 - CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(1)),Integer))
                End If
            End If
            Throw New SyntaxException("Expecting Unary", node.Line, node.Column, node.Position)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Unary= ( "+" | "-" ) Unary | Leaf
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Unary -> add Unary
        '''Unary -> Implicit Unary
        '''Unary -> Leaf
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateUnary(ByVal node As ParseNode) As Object
            Return ExpressionParser.EvaluateUnary(node, Nothing)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Leaf= integer | identifier | "(" Term ")"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Leaf -> integer
        '''Leaf -> identifier
        '''Leaf -> Implicit3 Term Implicit4
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateLeaf(ByVal node As ParseNode, ByVal state As Object) As Object
            If (ExpressionParser.Leaf = node.SymbolId) Then
                If (node.Children.Length = 1) Then
                    If (node.Children(1).SymbolId = ParsleyDemo.ExpressionParser.[integer]) Then
                        Return Integer.Parse(node.Children(0).Value)
                    Else
                        Throw New NotImplementedException("Variables are not implemented.")
                    End If
                Else
                    Return ParsleyDemo.ExpressionParser.EvaluateTerm(node.Children(1))
                End If
            End If
            Throw New SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Leaf= integer | identifier | "(" Term ")"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Leaf -> integer
        '''Leaf -> identifier
        '''Leaf -> Implicit3 Term Implicit4
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateLeaf(ByVal node As ParseNode) As Object
            Return ExpressionParser.EvaluateLeaf(node, Nothing)
        End Function
    End Class
End Namespace
