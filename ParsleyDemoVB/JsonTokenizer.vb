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

Imports System.Collections.Generic

Namespace ParsleyDemo
    Friend Class JsonTokenizer
        Inherits TableTokenizer
        Friend Shared DfaTable() As DfaEntry = New DfaEntry() {New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(9), Global.Microsoft.VisualBasic.ChrW(10), Global.Microsoft.VisualBasic.ChrW(13), Global.Microsoft.VisualBasic.ChrW(13), Global.Microsoft.VisualBasic.ChrW(32), Global.Microsoft.VisualBasic.ChrW(32)}, 1), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(44), Global.Microsoft.VisualBasic.ChrW(44)}, 2), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(58), Global.Microsoft.VisualBasic.ChrW(58)}, 3), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(125), Global.Microsoft.VisualBasic.ChrW(125)}, 4), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(123), Global.Microsoft.VisualBasic.ChrW(123)}, 5), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(93), Global.Microsoft.VisualBasic.ChrW(93)}, 6), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(91), Global.Microsoft.VisualBasic.ChrW(91)}, 7), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(110), Global.Microsoft.VisualBasic.ChrW(110)}, 8), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(102), Global.Microsoft.VisualBasic.ChrW(102)}, 12), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(116), Global.Microsoft.VisualBasic.ChrW(116)}, 17), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(34)}, 21), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(45), Global.Microsoft.VisualBasic.ChrW(45)}, 31), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(48)}, 32), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(49), Global.Microsoft.VisualBasic.ChrW(57)}, 38)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(9), Global.Microsoft.VisualBasic.ChrW(10), Global.Microsoft.VisualBasic.ChrW(13), Global.Microsoft.VisualBasic.ChrW(13), Global.Microsoft.VisualBasic.ChrW(32), Global.Microsoft.VisualBasic.ChrW(32)}, 1)}, 25), New DfaEntry(New DfaTransitionEntry(-1) {}, 24), New DfaEntry(New DfaTransitionEntry(-1) {}, 23), New DfaEntry(New DfaTransitionEntry(-1) {}, 22), New DfaEntry(New DfaTransitionEntry(-1) {}, 21), New DfaEntry(New DfaTransitionEntry(-1) {}, 20), New DfaEntry(New DfaTransitionEntry(-1) {}, 19), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(117), Global.Microsoft.VisualBasic.ChrW(117)}, 9)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(108), Global.Microsoft.VisualBasic.ChrW(108)}, 10)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(108), Global.Microsoft.VisualBasic.ChrW(108)}, 11)}, -1), New DfaEntry(New DfaTransitionEntry(-1) {}, 18), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(97)}, 13)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(108), Global.Microsoft.VisualBasic.ChrW(108)}, 14)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(115), Global.Microsoft.VisualBasic.ChrW(115)}, 15)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(101), Global.Microsoft.VisualBasic.ChrW(101)}, 16)}, -1), New DfaEntry(New DfaTransitionEntry(-1) {}, 17), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(114), Global.Microsoft.VisualBasic.ChrW(114)}, 18)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(117), Global.Microsoft.VisualBasic.ChrW(117)}, 19)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(101), Global.Microsoft.VisualBasic.ChrW(101)}, 20)}, -1), New DfaEntry(New DfaTransitionEntry(-1) {}, 16), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(0), Global.Microsoft.VisualBasic.ChrW(9), Global.Microsoft.VisualBasic.ChrW(11), Global.Microsoft.VisualBasic.ChrW(33), Global.Microsoft.VisualBasic.ChrW(35), Global.Microsoft.VisualBasic.ChrW(91), Global.Microsoft.VisualBasic.ChrW(93), Global.Microsoft.VisualBasic.ChrW(65535)}, 21), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(92), Global.Microsoft.VisualBasic.ChrW(92)}, 22), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(34)}, 30)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(34), Global.Microsoft.VisualBasic.ChrW(47), Global.Microsoft.VisualBasic.ChrW(47), Global.Microsoft.VisualBasic.ChrW(92), Global.Microsoft.VisualBasic.ChrW(92), Global.Microsoft.VisualBasic.ChrW(98), Global.Microsoft.VisualBasic.ChrW(98), Global.Microsoft.VisualBasic.ChrW(102), Global.Microsoft.VisualBasic.ChrW(102), Global.Microsoft.VisualBasic.ChrW(110), Global.Microsoft.VisualBasic.ChrW(110), Global.Microsoft.VisualBasic.ChrW(114), Global.Microsoft.VisualBasic.ChrW(114), Global.Microsoft.VisualBasic.ChrW(116), Global.Microsoft.VisualBasic.ChrW(116)}, 21), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(117), Global.Microsoft.VisualBasic.ChrW(117)}, 23)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(65), Global.Microsoft.VisualBasic.ChrW(70), Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(102)}, 24)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(65), Global.Microsoft.VisualBasic.ChrW(70), Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(102)}, 25)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(65), Global.Microsoft.VisualBasic.ChrW(70), Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(102)}, 26)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(65), Global.Microsoft.VisualBasic.ChrW(70), Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(102)}, 27)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(65), Global.Microsoft.VisualBasic.ChrW(70), Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(102)}, 28)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(65), Global.Microsoft.VisualBasic.ChrW(70), Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(102)}, 29)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(65), Global.Microsoft.VisualBasic.ChrW(70), Global.Microsoft.VisualBasic.ChrW(97), Global.Microsoft.VisualBasic.ChrW(102)}, 21)}, -1), New DfaEntry(New DfaTransitionEntry(-1) {}, 15), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(48)}, 32), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(49), Global.Microsoft.VisualBasic.ChrW(57)}, 38)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(46), Global.Microsoft.VisualBasic.ChrW(46)}, 33), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(57)}, 34), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(69), Global.Microsoft.VisualBasic.ChrW(69), Global.Microsoft.VisualBasic.ChrW(101), Global.Microsoft.VisualBasic.ChrW(101)}, 35)}, 14), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(57)}, 34)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(57)}, 34), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(69), Global.Microsoft.VisualBasic.ChrW(69), Global.Microsoft.VisualBasic.ChrW(101), Global.Microsoft.VisualBasic.ChrW(101)}, 35)}, 14), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(43), Global.Microsoft.VisualBasic.ChrW(43), Global.Microsoft.VisualBasic.ChrW(45), Global.Microsoft.VisualBasic.ChrW(45)}, 36), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(57)}, 37)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(57)}, 37)}, -1), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(57)}, 37)}, 14), New DfaEntry(New DfaTransitionEntry() {New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(48), Global.Microsoft.VisualBasic.ChrW(57)}, 38), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(46), Global.Microsoft.VisualBasic.ChrW(46)}, 33), New DfaTransitionEntry(New Char() {Global.Microsoft.VisualBasic.ChrW(69), Global.Microsoft.VisualBasic.ChrW(69), Global.Microsoft.VisualBasic.ChrW(101), Global.Microsoft.VisualBasic.ChrW(101)}, 35)}, 14)}
        Friend Shared NodeFlags() As Integer = New Integer() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 1}
        Friend Shared BlockEnds() As String = New String() {Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing, Nothing}
        Public Sub New(ByVal input As IEnumerable(Of Char))
            MyBase.New(JsonTokenizer.DfaTable, JsonTokenizer.BlockEnds, JsonTokenizer.NodeFlags, input)
        End Sub
        Public Const number As Integer = 14
        Public Const [string] As Integer = 15
        Public Const [true] As Integer = 16
        Public Const [false] As Integer = 17
        Public Const null As Integer = 18
        Public Const lbracket As Integer = 19
        Public Const rbracket As Integer = 20
        Public Const lbrace As Integer = 21
        Public Const rbrace As Integer = 22
        Public Const colon As Integer = 23
        Public Const comma As Integer = 24
        Public Const whitespace As Integer = 25
    End Class
End Namespace
