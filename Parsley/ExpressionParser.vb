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
    '''Factor= Leaf { ( "*" | "/" ) Leaf };
    '''Leaf= integer | "(" Term ")";
    '''add= "+";
    '''sub= "-";
    '''mul= "*";
    '''div= "/";
    '''lparen= "(";
    '''rparen= ")";
    '''integer= '[0-9]+';
    '''(whitespace)= '\s+';
    '''</summary>
    '''<remarks>The rules for the factored grammar are as follows:
    '''Leaf -> integer
    '''Leaf -> lparen Term rparen
    '''ImplicitList -> add Factor ImplicitListRightAssoc ImplicitListRightAssoc2
    '''ImplicitList -> sub Factor ImplicitListRightAssoc ImplicitListRightAssoc2
    '''ImplicitList2 -> mul Leaf ImplicitList2RightAssoc ImplicitList2RightAssoc2
    '''ImplicitList2 -> div Leaf ImplicitList2RightAssoc ImplicitList2RightAssoc2
    '''ImplicitListRightAssoc -> add Factor ImplicitListRightAssoc
    '''ImplicitListRightAssoc ->
    '''ImplicitList2RightAssoc -> mul Leaf ImplicitList2RightAssoc
    '''ImplicitList2RightAssoc ->
    '''Term -> Factor TermPart
    '''TermPart -> ImplicitList
    '''TermPart ->
    '''Factor -> Leaf FactorPart
    '''FactorPart -> ImplicitList2
    '''FactorPart ->
    '''ImplicitListRightAssoc2 -> sub Factor ImplicitListRightAssoc2
    '''ImplicitListRightAssoc2 ->
    '''ImplicitList2RightAssoc2 -> div Leaf ImplicitList2RightAssoc2
    '''ImplicitList2RightAssoc2 ->
    '''</remarks>
    <System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")>  _
    Partial Friend Class ExpressionParser
        Friend Const ErrorSymbol As Integer = -1
        Friend Const EosSymbol As Integer = -2
        Public Const Leaf As Integer = 0
        Public Const ImplicitList As Integer = 1
        Public Const ImplicitList2 As Integer = 2
        Public Const ImplicitListRightAssoc As Integer = 3
        Public Const ImplicitList2RightAssoc As Integer = 4
        Public Const Term As Integer = 5
        Public Const TermPart As Integer = 6
        Public Const Factor As Integer = 7
        Public Const FactorPart As Integer = 8
        Public Const ImplicitListRightAssoc2 As Integer = 9
        Public Const ImplicitList2RightAssoc2 As Integer = 10
        Public Const [integer] As Integer = 11
        Public Const lparen As Integer = 12
        Public Const rparen As Integer = 13
        Public Const add As Integer = 14
        Public Const [sub] As Integer = 15
        Public Const mul As Integer = 16
        Public Const div As Integer = 17
        Public Const whitespace As Integer = 18
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
            If (ExpressionParser.lparen = context.SymbolId) Then
                'Leaf -> lparen Term rparen
                Dim children(2) As ParseNode
                children(0) = New ParseNode(ExpressionParser.lparen, "lparen", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseTerm(context)
                children(2) = New ParseNode(ExpressionParser.rparen, "rparen", context.Value, line, column, position)
                If (ExpressionParser.rparen = context.SymbolId) Then
                    context.Advance
                    Return New ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position)
                End If
                context.Error("Expecting rparen")
            End If
            context.Error("Expecting integer or lparen")
            Return Nothing
        End Function
        Private Shared Function _ParseImplicitList(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.add = context.SymbolId) Then
                'ImplicitList -> add Factor ImplicitListRightAssoc ImplicitListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.add, "add", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseImplicitListRightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseImplicitListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitList, "ImplicitList", children.ToArray, line, column, position)
            End If
            If (ExpressionParser.[sub] = context.SymbolId) Then
                'ImplicitList -> sub Factor ImplicitListRightAssoc ImplicitListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.[sub], "sub", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseImplicitListRightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseImplicitListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitList, "ImplicitList", children.ToArray, line, column, position)
            End If
            context.Error("Expecting add or sub")
            Return Nothing
        End Function
        Private Shared Function _ParseImplicitList2(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.mul = context.SymbolId) Then
                'ImplicitList2 -> mul Leaf ImplicitList2RightAssoc ImplicitList2RightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseLeaf(context))
                children.AddRange(ExpressionParser._ParseImplicitList2RightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseImplicitList2RightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitList2, "ImplicitList2", children.ToArray, line, column, position)
            End If
            If (ExpressionParser.div = context.SymbolId) Then
                'ImplicitList2 -> div Leaf ImplicitList2RightAssoc ImplicitList2RightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.div, "div", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseLeaf(context))
                children.AddRange(ExpressionParser._ParseImplicitList2RightAssoc(context).Children)
                children.AddRange(ExpressionParser._ParseImplicitList2RightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitList2, "ImplicitList2", children.ToArray, line, column, position)
            End If
            context.Error("Expecting mul or div")
            Return Nothing
        End Function
        Private Shared Function _ParseImplicitListRightAssoc(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.add = context.SymbolId) Then
                'ImplicitListRightAssoc -> add Factor ImplicitListRightAssoc
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.add, "add", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseImplicitListRightAssoc(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitListRightAssoc, "ImplicitListRightAssoc", children.ToArray, line, column, position)
            End If
            If (((ExpressionParser.[sub] = context.SymbolId)  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.rparen = context.SymbolId)) Then
                'ImplicitListRightAssoc ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.ImplicitListRightAssoc, "ImplicitListRightAssoc", children, line, column, position)
            End If
            context.Error("Expecting add, sub, #EOS, or rparen")
            Return Nothing
        End Function
        Private Shared Function _ParseImplicitList2RightAssoc(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.mul = context.SymbolId) Then
                'ImplicitList2RightAssoc -> mul Leaf ImplicitList2RightAssoc
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseLeaf(context))
                children.AddRange(ExpressionParser._ParseImplicitList2RightAssoc(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitList2RightAssoc, "ImplicitList2RightAssoc", children.ToArray, line, column, position)
            End If
            If (((((ExpressionParser.div = context.SymbolId)  _
                        OrElse (ExpressionParser.add = context.SymbolId))  _
                        OrElse (ExpressionParser.[sub] = context.SymbolId))  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.rparen = context.SymbolId)) Then
                'ImplicitList2RightAssoc ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.ImplicitList2RightAssoc, "ImplicitList2RightAssoc", children, line, column, position)
            End If
            context.Error("Expecting mul, div, add, sub, #EOS, or rparen")
            Return Nothing
        End Function
        Private Shared Function _ParseTerm(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If ((ExpressionParser.[integer] = context.SymbolId)  _
                        OrElse (ExpressionParser.lparen = context.SymbolId)) Then
                'Term -> Factor TermPart
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseTermPart(context).Children)
                Return New ParseNode(ExpressionParser.Term, "Term", children.ToArray, line, column, position)
            End If
            context.Error("Expecting integer or lparen")
            Return Nothing
        End Function
        Private Shared Function _ParseTermPart(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If ((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.[sub] = context.SymbolId)) Then
                'TermPart -> ImplicitList
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.AddRange(ExpressionParser._ParseImplicitList(context).Children)
                Return New ParseNode(ExpressionParser.TermPart, "TermPart", children.ToArray, line, column, position)
            End If
            If ((ExpressionParser.EosSymbol = context.SymbolId)  _
                        OrElse (ExpressionParser.rparen = context.SymbolId)) Then
                'TermPart ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.TermPart, "TermPart", children, line, column, position)
            End If
            context.Error("Expecting add, sub, #EOS, or rparen")
            Return Nothing
        End Function
        Private Shared Function _ParseFactor(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If ((ExpressionParser.[integer] = context.SymbolId)  _
                        OrElse (ExpressionParser.lparen = context.SymbolId)) Then
                'Factor -> Leaf FactorPart
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(ExpressionParser._ParseLeaf(context))
                children.AddRange(ExpressionParser._ParseFactorPart(context).Children)
                Return New ParseNode(ExpressionParser.Factor, "Factor", children.ToArray, line, column, position)
            End If
            context.Error("Expecting integer or lparen")
            Return Nothing
        End Function
        Private Shared Function _ParseFactorPart(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If ((ExpressionParser.mul = context.SymbolId)  _
                        OrElse (ExpressionParser.div = context.SymbolId)) Then
                'FactorPart -> ImplicitList2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.AddRange(ExpressionParser._ParseImplicitList2(context).Children)
                Return New ParseNode(ExpressionParser.FactorPart, "FactorPart", children.ToArray, line, column, position)
            End If
            If ((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.[sub] = context.SymbolId))  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.rparen = context.SymbolId)) Then
                'FactorPart ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.FactorPart, "FactorPart", children, line, column, position)
            End If
            context.Error("Expecting mul, div, add, sub, #EOS, or rparen")
            Return Nothing
        End Function
        Private Shared Function _ParseImplicitListRightAssoc2(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.[sub] = context.SymbolId) Then
                'ImplicitListRightAssoc2 -> sub Factor ImplicitListRightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.[sub], "sub", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseImplicitListRightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitListRightAssoc2, "ImplicitListRightAssoc2", children.ToArray, line, column, position)
            End If
            If ((ExpressionParser.EosSymbol = context.SymbolId)  _
                        OrElse (ExpressionParser.rparen = context.SymbolId)) Then
                'ImplicitListRightAssoc2 ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.ImplicitListRightAssoc2, "ImplicitListRightAssoc2", children, line, column, position)
            End If
            context.Error("Expecting sub, #EOS, or rparen")
            Return Nothing
        End Function
        Private Shared Function _ParseImplicitList2RightAssoc2(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.div = context.SymbolId) Then
                'ImplicitList2RightAssoc2 -> div Leaf ImplicitList2RightAssoc2
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(New ParseNode(ExpressionParser.div, "div", context.Value, line, column, position))
                context.Advance
                children.Add(ExpressionParser._ParseLeaf(context))
                children.AddRange(ExpressionParser._ParseImplicitList2RightAssoc2(context).Children)
                Return New ParseNode(ExpressionParser.ImplicitList2RightAssoc2, "ImplicitList2RightAssoc2", children.ToArray, line, column, position)
            End If
            If ((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.[sub] = context.SymbolId))  _
                        OrElse (ExpressionParser.EosSymbol = context.SymbolId))  _
                        OrElse (ExpressionParser.rparen = context.SymbolId)) Then
                'ImplicitList2RightAssoc2 ->
                Dim children(-1) As ParseNode
                Return New ParseNode(ExpressionParser.ImplicitList2RightAssoc2, "ImplicitList2RightAssoc2", children, line, column, position)
            End If
            context.Error("Expecting div, add, sub, #EOS, or rparen")
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
        '''Factor= Leaf { ( "*" | "/" ) Leaf }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Factor -> Leaf FactorPart
        '''</remarks>
        '''<param name="tokenizer">The tokenizer to parse with</param><returns>A <see cref="ParseNode" /> representing the parsed tokens</returns>
        Public Shared Function ParseFactor(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseFactor(context)
        End Function
        '''<summary>
        '''Parses a production of the form:
        '''Leaf= integer | "(" Term ")"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Leaf -> integer
        '''Leaf -> lparen Term rparen
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
        Public Overloads Shared Function Evaluate(ByVal node As ParseNode) As Integer
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
        Public Overloads Shared Function Evaluate(ByVal node As ParseNode, ByVal state As Object) As Integer
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
        Public Overloads Shared Function EvaluateTerm(ByVal node As ParseNode, ByVal state As Object) As Integer
            If (ExpressionParser.Term = node.SymbolId) Then
                Dim result As Integer = ExpressionParser.EvaluateFactor(node.Children(0), state)
                Dim i As Integer = 2

                Do While (i < node.Children.Length)
                    If (node.Children((i - 1)).SymbolId = ParsleyDemo.ExpressionParser.add) Then
                        result = (result + ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(i), state))
                    Else
                        result = (result - ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(i), state))
                    End If
                    i = (i + 2)

                Loop
                Return CType(ExpressionParser._ChangeType(result, GetType(Integer)),Integer)
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
        Public Overloads Shared Function EvaluateTerm(ByVal node As ParseNode) As Integer
            Return ExpressionParser.EvaluateTerm(node, Nothing)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Factor= Leaf { ( "*" | "/" ) Leaf }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Factor -> Leaf FactorPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateFactor(ByVal node As ParseNode, ByVal state As Object) As Integer
            If (ExpressionParser.Factor = node.SymbolId) Then
                Dim result As Integer = ExpressionParser.EvaluateLeaf(node.Children(0), state)
                Dim i As Integer = 2

                Do While (i < node.Children.Length)
                    If (node.Children(i).SymbolId = ParsleyDemo.ExpressionParser.Leaf) Then
                        If (node.Children((i - 1)).SymbolId = ParsleyDemo.ExpressionParser.mul) Then
                            result = (result * ParsleyDemo.ExpressionParser.EvaluateLeaf(node.Children(i), state))
                        Else
                            result = (result / ParsleyDemo.ExpressionParser.EvaluateLeaf(node.Children(i), state))
                        End If
                    Else
                        If (node.Children((i - 1)).SymbolId = ParsleyDemo.ExpressionParser.mul) Then
                            result = (result * ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(i), state))
                        Else
                            result = (result / ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children((i - 1)), state))
                        End If
                    End If
                    i = (i + 2)

                Loop
                Return CType(ExpressionParser._ChangeType(result, GetType(Integer)),Integer)
            End If
            Throw New SyntaxException("Expecting Factor", node.Line, node.Column, node.Position)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Factor= Leaf { ( "*" | "/" ) Leaf }
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Factor -> Leaf FactorPart
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateFactor(ByVal node As ParseNode) As Integer
            Return ExpressionParser.EvaluateFactor(node, Nothing)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Leaf= integer | "(" Term ")"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Leaf -> integer
        '''Leaf -> lparen Term rparen
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<param name="state">A user supplied state object. What it should be depends on the production's associated code block</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateLeaf(ByVal node As ParseNode, ByVal state As Object) As Integer
            If (ExpressionParser.Leaf = node.SymbolId) Then
                If (node.Children.Length = 1) Then
                    Return CType(ExpressionParser._ChangeType(node.Children(0).Value, GetType(Integer)),Integer)
                Else
                    Return CType(ExpressionParser._ChangeType(ExpressionParser.EvaluateTerm(node.Children(1), state), GetType(Integer)),Integer)
                End If
            End If
            Throw New SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position)
        End Function
        '''<summary>
        '''Evaluates a derivation of the form:
        '''Leaf= integer | "(" Term ")"
        '''</summary>
        '''<remarks>
        '''The production rules are:
        '''Leaf -> integer
        '''Leaf -> lparen Term rparen
        '''</remarks>
        '''<param name="node">The <see cref="ParseNode"/> to evaluate</param>
        '''<returns>The result of the evaluation</returns>
        Public Overloads Shared Function EvaluateLeaf(ByVal node As ParseNode) As Integer
            Return ExpressionParser.EvaluateLeaf(node, Nothing)
        End Function
        Private Shared Function _ChangeType(ByVal obj As Object, ByVal type As System.Type) As Object
            Dim typeConverter As System.ComponentModel.TypeConverter = System.ComponentModel.TypeDescriptor.GetConverter(obj)
            If ((Nothing Is typeConverter)  _
                        OrElse (false = typeConverter.CanConvertTo(type))) Then
                Return System.Convert.ChangeType(obj, type)
            End If
            Return typeConverter.ConvertTo(obj, type)
        End Function
    End Class
    '''<summary>
    '''
    '''</summary>
    <System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")>  _
    Friend Class SyntaxException
        Inherits Exception
        Private _line As Integer
        Private _column As Integer
        Private _position As Long
        '''<summary>
        '''Creates a syntax exception with the specified arguments
        '''</summary>
        '''<param name="message">The error message</param>
        '''<param name="line">The line where the error occurred</param>
        '''<param name="column">The column where the error occured</param>
        '''<param name="position">The position where the error occured</param>
        Public Sub New(ByVal message As String, ByVal line As Integer, ByVal column As Integer, ByVal position As Long)
            MyBase.New(SyntaxException._GetMessage(message, line, column, position))
            Me._line = line
            Me._column = column
            Me._position = position
        End Sub
        '''<summary>
        '''The line where the error occurred
        '''</summary>
        Public ReadOnly Property Line() As Integer
            Get
                Return Me._line
            End Get
        End Property
        '''<summary>
        '''The column where the error occurred
        '''</summary>
        Public ReadOnly Property Column() As Integer
            Get
                Return Me._column
            End Get
        End Property
        '''<summary>
        '''The position where the error occurred
        '''</summary>
        Public ReadOnly Property Position() As Long
            Get
                Return Me._position
            End Get
        End Property
        Shared Function _GetMessage(ByVal message As String, ByVal line As Integer, ByVal column As Integer, ByVal position As Long) As String
            Return String.Format("{0} at line {1}, column {2}, position {3}", message, line, column, position)
        End Function
    End Class
    <System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")>  _
    Partial Friend Class ParseNode
        Private _symbolId As Integer
        Private _symbol As String
        Private _value As String
        Private _line As Integer
        Private _column As Integer
        Private _position As Long
        Private _children() As ParseNode
        Public Sub New(ByVal symbolId As Integer, ByVal symbol As String, ByVal children() As ParseNode, ByVal line As Integer, ByVal column As Integer, ByVal position As Long)
            MyBase.New
            Me._symbolId = symbolId
            Me._symbol = symbol
            Me._value = Nothing
            Me._children = children
            Me._line = line
            Me._column = column
            Me._position = position
        End Sub
        Public Sub New(ByVal symbolId As Integer, ByVal symbol As String, ByVal value As String, ByVal line As Integer, ByVal column As Integer, ByVal position As Long)
            MyBase.New
            Me._symbolId = symbolId
            Me._symbol = symbol
            Me._value = value
            Me._children = Nothing
            Me._line = line
            Me._column = column
            Me._position = position
        End Sub
        Public ReadOnly Property IsNonTerminal() As Boolean
            Get
                Return (Not (Me._children) Is Nothing)
            End Get
        End Property
        Public ReadOnly Property Children() As ParseNode()
            Get
                Return Me._children
            End Get
        End Property
        Public ReadOnly Property SymbolId() As Integer
            Get
                Return Me._symbolId
            End Get
        End Property
        Public ReadOnly Property Symbol() As String
            Get
                Return Me._symbol
            End Get
        End Property
        Public ReadOnly Property Value() As String
            Get
                Return Me._value
            End Get
        End Property
        Public ReadOnly Property Line() As Integer
            Get
                Return Me._line
            End Get
        End Property
        Public ReadOnly Property Column() As Integer
            Get
                Return Me._column
            End Get
        End Property
        Public ReadOnly Property Position() As Long
            Get
                Return Me._position
            End Get
        End Property
    End Class
    <System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley", "0.1.0.0")>  _
    Partial Friend Class ParserContext
        Inherits [Object]
        Implements IDisposable
        Private _state As Integer
        Private _e As IEnumerator(Of Token)
        Private _t As Token
        Public Sub New(ByVal tokenizer As IEnumerable(Of Token))
            MyBase.New
            Me._e = tokenizer.GetEnumerator
            Me._state = -1
            Me._t.SymbolId = -1
        End Sub
        Public Sub EnsureStarted()
            If (-1 = Me._state) Then
                Me.Advance
            End If
        End Sub
        Public ReadOnly Property SymbolId() As Integer
            Get
                Return Me._t.SymbolId
            End Get
        End Property
        Public ReadOnly Property Value() As String
            Get
                Return Me._t.Value
            End Get
        End Property
        Public ReadOnly Property Line() As Integer
            Get
                Return Me._t.Line
            End Get
        End Property
        Public ReadOnly Property Column() As Integer
            Get
                Return Me._t.Column
            End Get
        End Property
        Public ReadOnly Property Position() As Long
            Get
                Return Me._t.Position
            End Get
        End Property
        Public ReadOnly Property IsEnded() As Boolean
            Get
                Return (-2 = Me._state)
            End Get
        End Property
        Public Function Advance() As Boolean
            If (false = Me._e.MoveNext) Then
                Me._t.SymbolId = -2
                Me._state = -2
            Else
                Me._state = 0
                Me._t = Me._e.Current
                Return true
            End If
            Return false
        End Function
        Public Overloads Sub [Error](ByVal message As String, ByVal arg1 As Object, ByVal arg2 As Object, ByVal arg3 As Object)
            Throw New SyntaxException(String.Format(message, arg1, arg2, arg3), Me.Line, Me.Column, Me.Position)
        End Sub
        Public Overloads Sub [Error](ByVal message As String, ByVal arg1 As Object, ByVal arg2 As Object)
            Throw New SyntaxException(String.Format(message, arg1, arg2), Me.Line, Me.Column, Me.Position)
        End Sub
        Public Overloads Sub [Error](ByVal message As String, ByVal arg As Object)
            Throw New SyntaxException(String.Format(message, arg), Me.Line, Me.Column, Me.Position)
        End Sub
        Public Overloads Sub [Error](ByVal message As String)
            Throw New SyntaxException(message, Me.Line, Me.Column, Me.Position)
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            Me._e.Dispose
            Me._state = -3
        End Sub
    End Class
End Namespace
