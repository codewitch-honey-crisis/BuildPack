﻿// $(SolutionDir)\Rolex\bin\Release\rolex.exe $(ProjectDir)SampleTokenizer.rl /output $(ProjectDir)SampleTokenizer.cs /namespace RolexDemo /compiled /ifstale
// simple expression tokenizer
// start our ids at one (no reason)
Identifier<id=1, ignoreCase> = '[A-Z_][A-Z_0-9]*'
Integer = '[0-9]+'
Plus = "+"
Minus = "-"
Multiply = "*"
Divide = "/"
LParen = "("
RParen = ")"
Whitespace<hidden> = '\s'
LineComment<hidden> = '//[^\n]*'
BlockComment<hidden,blockEnd="*/">="/*"
