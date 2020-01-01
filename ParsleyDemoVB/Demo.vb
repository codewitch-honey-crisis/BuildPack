Imports System.IO
Namespace ParsleyDemo
    Module Demo

        Public Sub RunDemo()
            Dim text As String = "3*5+a*2"
            Dim vars As IDictionary(Of String, Integer) = New Dictionary(Of String, Integer)()
            vars("a") = 1
            Dim exprTokenizer As ExpressionTokenizer = New ExpressionTokenizer(text)
            Dim pt As ParseNode = ExpressionParser.Parse(exprTokenizer)
            Console.WriteLine("{0} = {1}", text, ExpressionParser.Evaluate(pt, vars))
            Console.WriteLine()
            _WriteTree(pt, Console.Out)
            Console.WriteLine("Press any key...")
            Console.ReadKey()
            Dim sr As TextReader = Nothing
            Try
                sr = File.OpenText("..\..\data.json")
                text = sr.ReadToEnd()
            Finally
                If Not sr Is Nothing Then
                    sr.Close()
                End If
            End Try
            Dim jsonTokenizer As JsonTokenizer = New JsonTokenizer(text)
            pt = JsonParser.Parse(jsonTokenizer)
            _WriteTree(pt, Console.Out)
            Console.WriteLine("Press any key...")
            Console.ReadKey()
            Return
        End Sub
        Private Sub _WriteTree(node As ParseNode, writer As TextWriter)
            ' adapted from https://stackoverflow.com/questions/1649027/how-do-i-print-out-a-tree-structure
            Dim firstStack As List(Of ParseNode) = New List(Of ParseNode)()
            firstStack.Add(node)

            Dim childListStack As List(Of List(Of ParseNode)) = New List(Of List(Of ParseNode))()
            childListStack.Add(firstStack)
            Do While (childListStack.Count > 0)
                Dim childStack As List(Of ParseNode) = childListStack(childListStack.Count - 1)

                If (childStack.Count = 0) Then

                    childListStack.RemoveAt(childListStack.Count - 1)
                Else
                    node = childStack(0)
                    childStack.RemoveAt(0)
                    Dim indent As String = ""
                    For i As Integer = 0 To childListStack.Count - 2
                        If childListStack(i).Count > 0 Then
                            indent = indent + "|  "
                        Else
                            indent = indent + "   "
                        End If
                    Next
                    Dim s As String = node.Symbol
                    Dim v As String = node.Value
                    If v Is Nothing Then v = ""
                    writer.Write(String.Concat(indent, "+- ", s, " ", v).TrimEnd())
                    writer.WriteLine()
                    If (node.IsNonTerminal AndAlso 0 < node.Children.Length) Then
                        childListStack.Add(New List(Of ParseNode)(node.Children))
                    End If
                End If
            Loop
        End Sub
    End Module
End Namespace