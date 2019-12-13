// simple expression tokenizer
// start our ids at one
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