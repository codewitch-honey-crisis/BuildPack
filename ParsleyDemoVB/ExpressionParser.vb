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
    Partial Friend Class ExpressionParser
        Friend Const ErrorSymbol As Integer = -1
        Friend Const EosSymbol As Integer = -2
        Public Const Expression As Integer = 0
        Public Const Unary As Integer = 1
        Public Const Leaf As Integer = 2
        Public Const Term As Integer = 3
        Public Const TermPart As Integer = 4
        Public Const Factor As Integer = 5
        Public Const FactorPart As Integer = 6
        Public Const add As Integer = 7
        Public Const [sub] As Integer = 8
        Public Const identifier As Integer = 9
        Public Const [integer] As Integer = 10
        Public Const lparen As Integer = 11
        Public Const rparen As Integer = 12
        Public Const mul As Integer = 13
        Public Const div As Integer = 14
        Public Const whitespace As Integer = 15
        Private Shared Function _ParseExpression(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.[sub] = context.SymbolId))  _
                        OrElse (ExpressionParser.identifier = context.SymbolId))  _
                        OrElse (ExpressionParser.[integer] = context.SymbolId))  _
                        OrElse (ExpressionParser.lparen = context.SymbolId)) Then
                'Expression -> Term
                Dim children(0) As ParseNode
                children(0) = ExpressionParser._ParseTerm(context)
                Return New ParseNode(ExpressionParser.Expression, "Expression", children, line, column, position)
            End If
            context.Error("Expecting add, sub, identifier, integer, or lparen")
            Return Nothing
        End Function
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
            If (ExpressionParser.[sub] = context.SymbolId) Then
                'Unary -> sub Unary
                Dim children(1) As ParseNode
                children(0) = New ParseNode(ExpressionParser.[sub], "sub", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseUnary(context)
                Return New ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position)
            End If
            If (((ExpressionParser.identifier = context.SymbolId)  _
                        OrElse (ExpressionParser.[integer] = context.SymbolId))  _
                        OrElse (ExpressionParser.lparen = context.SymbolId)) Then
                'Unary -> Leaf
                Dim children(0) As ParseNode
                children(0) = ExpressionParser._ParseLeaf(context)
                Return New ParseNode(ExpressionParser.Unary, "Unary", children, line, column, position)
            End If
            context.Error("Expecting add, sub, identifier, integer, or lparen")
            Return Nothing
        End Function
        Private Shared Function _ParseLeaf(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.identifier = context.SymbolId) Then
                'Leaf -> identifier
                Dim children(0) As ParseNode
                children(0) = New ParseNode(ExpressionParser.identifier, "identifier", context.Value, line, column, position)
                context.Advance
                Return New ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position)
            End If
            If (ExpressionParser.[integer] = context.SymbolId) Then
                'Leaf -> integer
                Dim children(0) As ParseNode
                children(0) = New ParseNode(ExpressionParser.[integer], "integer", context.Value, line, column, position)
                context.Advance
                Return New ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position)
            End If
            If (ExpressionParser.lparen = context.SymbolId) Then
                'Leaf -> lparen Expression rparen
                Dim children(2) As ParseNode
                children(0) = New ParseNode(ExpressionParser.lparen, "lparen", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseExpression(context)
                children(2) = New ParseNode(ExpressionParser.rparen, "rparen", context.Value, line, column, position)
                context.Advance
                Return New ParseNode(ExpressionParser.Leaf, "Leaf", children, line, column, position)
            End If
            context.Error("Expecting identifier, integer, or lparen")
            Return Nothing
        End Function
        Private Shared Function _ParseTerm(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.[sub] = context.SymbolId))  _
                        OrElse (ExpressionParser.identifier = context.SymbolId))  _
                        OrElse (ExpressionParser.[integer] = context.SymbolId))  _
                        OrElse (ExpressionParser.lparen = context.SymbolId)) Then
                'Term -> Factor TermPart
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(ExpressionParser._ParseFactor(context))
                children.AddRange(ExpressionParser._ParseTermPart(context).Children)
                Return New ParseNode(ExpressionParser.Term, "Term", children.ToArray, line, column, position)
            End If
            context.Error("Expecting add, sub, identifier, integer, or lparen")
            Return Nothing
        End Function
        Private Shared Function _ParseTermPart(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.add = context.SymbolId) Then
                'TermPart -> add Factor
                Dim children(1) As ParseNode
                children(0) = New ParseNode(ExpressionParser.add, "add", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseFactor(context)
                Return New ParseNode(ExpressionParser.TermPart, "TermPart", children, line, column, position)
            End If
            If (ExpressionParser.[sub] = context.SymbolId) Then
                'TermPart -> sub Factor
                Dim children(1) As ParseNode
                children(0) = New ParseNode(ExpressionParser.[sub], "sub", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseFactor(context)
                Return New ParseNode(ExpressionParser.TermPart, "TermPart", children, line, column, position)
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
            If (((((ExpressionParser.add = context.SymbolId)  _
                        OrElse (ExpressionParser.[sub] = context.SymbolId))  _
                        OrElse (ExpressionParser.identifier = context.SymbolId))  _
                        OrElse (ExpressionParser.[integer] = context.SymbolId))  _
                        OrElse (ExpressionParser.lparen = context.SymbolId)) Then
                'Factor -> Unary FactorPart
                Dim children As System.Collections.Generic.List(Of ParseNode) = New System.Collections.Generic.List(Of ParseNode)()
                children.Add(ExpressionParser._ParseUnary(context))
                children.AddRange(ExpressionParser._ParseFactorPart(context).Children)
                Return New ParseNode(ExpressionParser.Factor, "Factor", children.ToArray, line, column, position)
            End If
            context.Error("Expecting add, sub, identifier, integer, or lparen")
            Return Nothing
        End Function
        Private Shared Function _ParseFactorPart(ByVal context As ParserContext) As ParseNode
            Dim line As Integer = context.Line
            Dim column As Integer = context.Column
            Dim position As Long = context.Position
            If (ExpressionParser.mul = context.SymbolId) Then
                'FactorPart -> mul Unary
                Dim children(1) As ParseNode
                children(0) = New ParseNode(ExpressionParser.mul, "mul", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseUnary(context)
                Return New ParseNode(ExpressionParser.FactorPart, "FactorPart", children, line, column, position)
            End If
            If (ExpressionParser.div = context.SymbolId) Then
                'FactorPart -> div Unary
                Dim children(1) As ParseNode
                children(0) = New ParseNode(ExpressionParser.div, "div", context.Value, line, column, position)
                context.Advance
                children(1) = ExpressionParser._ParseUnary(context)
                Return New ParseNode(ExpressionParser.FactorPart, "FactorPart", children, line, column, position)
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
        Public Shared Function ParseExpression(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseExpression(context)
        End Function
        Public Shared Function ParseTerm(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseTerm(context)
        End Function
        Public Shared Function ParseFactor(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseFactor(context)
        End Function
        Public Shared Function ParseUnary(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseUnary(context)
        End Function
        Public Shared Function ParseLeaf(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseLeaf(context)
        End Function
        Public Shared Function Parse(ByVal tokenizer As System.Collections.Generic.IEnumerable(Of Token)) As ParseNode
            Dim context As ParserContext = New ParserContext(tokenizer)
            context.EnsureStarted
            Return ExpressionParser._ParseExpression(context)
        End Function
        Public Overloads Shared Function Evaluate(ByVal node As ParseNode) As Integer
            Return ExpressionParser.EvaluateExpression(node)
        End Function
        Public Overloads Shared Function Evaluate(ByVal node As ParseNode, ByVal state As Object) As Integer
            Return ExpressionParser.EvaluateExpression(node, state)
        End Function
        Public Overloads Shared Function EvaluateExpression(ByVal node As ParseNode, ByVal state As Object) As Integer
            If (ExpressionParser.Expression = node.SymbolId) Then
                Return CType(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateTerm(node.Children(0), state), GetType(Integer)),Integer)
            End If
            Throw New SyntaxException("Expecting Expression", node.Line, node.Column, node.Position)
        End Function
        Public Overloads Shared Function EvaluateExpression(ByVal node As ParseNode) As Integer
            Return ExpressionParser.EvaluateExpression(node, Nothing)
        End Function
        Public Overloads Shared Function EvaluateTerm(ByVal node As ParseNode, ByVal state As Object) As Object
            If (ExpressionParser.Term = node.SymbolId) Then
                If (1 = node.Children.Length) Then
                    Return CType(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(0), state), GetType(Object)),Object)
                Else
                    If (node.Children(1).SymbolId = ParsleyDemo.ExpressionParser.add) Then
                        Return CType(ExpressionParser._ChangeType((CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(0), state),Integer) + CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(2), state),Integer)), GetType(Object)),Object)
                    Else
                        Return CType(ExpressionParser._ChangeType((CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(0), state),Integer) - CType(ParsleyDemo.ExpressionParser.EvaluateFactor(node.Children(2), state),Integer)), GetType(Object)),Object)
                    End If
                End If
            End If
            Throw New SyntaxException("Expecting Term", node.Line, node.Column, node.Position)
        End Function
        Public Overloads Shared Function EvaluateTerm(ByVal node As ParseNode) As Object
            Return ExpressionParser.EvaluateTerm(node, Nothing)
        End Function
        Public Overloads Shared Function EvaluateFactor(ByVal node As ParseNode, ByVal state As Object) As Integer
            If (ExpressionParser.Factor = node.SymbolId) Then
                If (1 = node.Children.Length) Then
                    Return CType(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(0), state), GetType(Integer)),Integer)
                Else
                    If (node.Children(1).SymbolId = ParsleyDemo.ExpressionParser.mul) Then
                        Return CType(ExpressionParser._ChangeType((CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(0), state),Integer) * CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(2), state),Integer)), GetType(Integer)),Integer)
                    Else
                        Return CType(ExpressionParser._ChangeType((CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(0), state),Integer) / CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(2), state),Integer)), GetType(Integer)),Integer)
                    End If
                End If
            End If
            Throw New SyntaxException("Expecting Factor", node.Line, node.Column, node.Position)
        End Function
        Public Overloads Shared Function EvaluateFactor(ByVal node As ParseNode) As Integer
            Return ExpressionParser.EvaluateFactor(node, Nothing)
        End Function
        Public Overloads Shared Function EvaluateUnary(ByVal node As ParseNode, ByVal state As Object) As Integer
            If (ExpressionParser.Unary = node.SymbolId) Then
                If (1 = node.Children.Length) Then
                    Return CType(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateLeaf(node.Children(0), state), GetType(Integer)),Integer)
                Else
                    If (node.Children(0).SymbolId = ParsleyDemo.ExpressionParser.add) Then
                        Return CType(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(1), state), GetType(Integer)),Integer)
                    Else
                        Return CType(ExpressionParser._ChangeType((0 - CType(ParsleyDemo.ExpressionParser.EvaluateUnary(node.Children(1), state),Integer)), GetType(Integer)),Integer)
                    End If
                End If
            End If
            Throw New SyntaxException("Expecting Unary", node.Line, node.Column, node.Position)
        End Function
        Public Overloads Shared Function EvaluateUnary(ByVal node As ParseNode) As Integer
            Return ExpressionParser.EvaluateUnary(node, Nothing)
        End Function
        Public Overloads Shared Function EvaluateLeaf(ByVal node As ParseNode, ByVal state As Object) As Integer
            If (ExpressionParser.Leaf = node.SymbolId) Then
                Dim n As ParseNode = node.Children(0)
                If (ParsleyDemo.ExpressionParser.identifier = n.SymbolId) Then
                    If (Nothing Is state) Then
                        Throw New InvalidOperationException("Variables were not defined.")
                    End If
                    Return CType(ExpressionParser._ChangeType(CType(state,IDictionary(Of String, Integer))(n.Value), GetType(Integer)),Integer)
                Else
                    If (ParsleyDemo.ExpressionParser.[integer] = n.SymbolId) Then
                        Return CType(ExpressionParser._ChangeType(Integer.Parse(n.Value), GetType(Integer)),Integer)
                    Else
                        Return CType(ExpressionParser._ChangeType(ParsleyDemo.ExpressionParser.EvaluateExpression(n.Children(1), state), GetType(Integer)),Integer)
                    End If
                End If
            End If
            Throw New SyntaxException("Expecting Leaf", node.Line, node.Column, node.Position)
        End Function
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
End Namespace
