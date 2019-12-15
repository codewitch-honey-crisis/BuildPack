// SlangTokenizer.rl (copy)
// This is the rolex specification file for SlangTokenizer.cs, the tokenizer the parser uses
// here is the macro to add to the build steps in order to enable:
// "$(SolutionDir)Rolex\bin\Release\rolex.exe" "$(ProjectDir)Slang\SlangTokenizer.rl" /output "$(ProjectDir)Slang\SlangTokenizer.cs" /namespace CD
// Note that edits to SlangTokenizer.cs can break the parser anyway, so it's best to leave this be
//
keyword='abstract|as|ascending|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|equals|explicit|extern|event|false|finally|fixed|float|for|foreach|get|global|goto|if|implicit|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield'
identifier='[A-Z_a-z][0-9A-Z_a-z]*'
lineComment='//[^\n]*'
blockComment<blockEnd="*/">="/*"
stringLiteral='"([^"\\]|\\.)*"'
characterLiteral='\'([^\'\\]|\\.)\''
whitespace<hidden>='\s+'
lte="<="
lt="<"
gte=">="
gt=">"
eqEq="=="
notEq="!="
eq="="
inc="++"
addAssign="+="
add="+"
dec="--"
subAssign="-="
sub="-"
mulAssign="*="
mul="*"
divAssign="/="
div="/"
modAssign="%="
mod="%"
and="&&"
bitwiseAndAssign="&="
bitwiseAnd="&"
or="||"
bitwiseOrAssign="|="
bitwiseOr="|"
not="!"
lbracket="["
rbracket="]"
lparen="("
rparen=")"
lbrace="{"
rbrace="}"
comma=","
colonColon="::"
colon=":"
semi=";"
dot="."
integerLiteral = '(0x[0-9A-Fa-f]{1,16}|(0|[1-9][0-9]*))([Uu][Ll]?|[Ll][Uu]?)?'
floatLiteral= '((0|[1-9][0-9]*)(\.[0-9]+)?([Ee][\+\-]?[0-9]+)?[DdMmFf]?)|((\.[0-9]+)([Ee][\+\-]?[0-9]+)?[DdMmFf]?)'
