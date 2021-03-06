# Make Awesome Build Tools in C#

**Note**: You must first build this solution a couple of times in Release mode to get the prebuild steps built. The reason you need to do it twice is because of circular dependencies. Switch to Debug once you're done

## CodeDom Go Kit

### Slang

Slang is a miniature language. It's a subset of C# suitable for constructing CodeDOM trees. It obviates the need to build them by hand since you can do it by writing restricted C# code instead. The reason the language is restricted is the CodeDOM does not allow for full fidelity. Many features like read only fields, and even postfix increment/decrement can't be supported. Even with these limitations it doesn't matter as it makes using the CodeDOM pretty easy. Now you can include source files in your build as templates using T4 and C# syntax and generate CodeDOM graphs that way. Or you can simply render source dependencies in your target language even though you keep them in Slang/C# instead of CodeDOM graphs. It is included as part of the Go Kit. See the **SlangDemo** project and watch Slang convert your C# into VB and into CodeDOM graphs

### Other CodeDOM stuff in the Go Kit

[See my Code Project article](https://www.codeproject.com/Articles/5253617/CodeDOM-Go-Kit-The-CodeDOM-is-Dead-Long-Live-the-C)

A suite of tools for code generation using the CodeDOM

I really hate the CodeDOM, and you should, too. Well, let's back up. I like the idea of the CodeDOM, but not the execution. It's clunky, kludgy and extremely limited. If I never cared about language agnostic code generation in .NET I'd never use it.

However, I do care about those things. I also care about tapping into things like Microsoft's XML serialization code generator and mutilating its output into something less awful.

Furthermore, Roslyn looks to be limited in terms of its platform availability and its language support, which seems set in stone at VB and C#. Meanwhile, the F# team produced a CodeDOM provider, this time for their language, so it looks like the CodeDOM can still generate in languages Roslyn can't, and on devices where Roslyn can't currently operate. I might be wrong about the platforms, and maybe .NET 5 will change everything, but this is where we are now.

Here's a few of the things the CodeDOM always needed:

* A `Parse()` function. Gosh that would have been cool. (See *Slang*/*SlangParser*)
* An `Eval()` function, even if it could only do certain expressions (See *CodeDomResolver.Evaluate()*)
* A `Visit()` function, or otherwise a way to search the graph (See *CodeDomVisitor.Visit()*)

I decided to finally do something about that, and with it comes a really quick and clean way to build and edit CodeDOM trees.

This readme can't do it justice, so just visit the link

## Parsley

A powerful recursive descent LL(1) parser generator in C#. Supports actions in the grammar. See my [Code Project article](https://www.codeproject.com/Articles/5254538/Parsley-A-Recursive-Descent-Parser-Generator-in-Csharp) for information on using it.

Like my other parsers, this parser uses XBNF for its grammar format. It supports automatic backtracking, virtual productions, syntactic constraints/predicates, and syntax directed actions.


## Rolex

A neat little tokenizer generator in C#. I use it all the time for parsers, and slang uses it for its tokenization, as does the RolexDemo parser. I've included this as lexers/tokenizers are often indespensible for build tools, plus it showcases the other included tools pretty extensively in its own implementation.
```
// Lexers rules are of the form
RuleName<attr1Name=attrValue,attr2name,attr3name=attrValue>= 'escapedposixregex'
...
```
RuleName becomes a constant. The attributes can be `id`, `ignoreCase`, `hidden`, and `blockEnd`. See the included sample files.

## Deslang

Deslang is kind of a precooker for Slang code. Using Slang, or really the CodeDOM Go Kit, requires a rather sizeable DLL, or 200K of binary worth of source includes to make function. That's not a show stopper but it's not really desirable. Also, slang is expensive for what it does because of the way the codedom works. It does a lot of reflection and unindexed searches. Most of that is unavoidable. The CodeDom wasn't meant to be used the way it's being used by Slang so slang works hard.

Deslang basically makes it so you can take some slang code and output it as a static field of type CodeCompileUnit. Basically it will create code to reinitialize the compile unit you made with slang. So if you can use that, you don't need to include slang, you can just use this tool over your slang source files to get the code you need and forgo actually including slang in your project.

This is helpful in conjunction with the use of CodeDomVisitor.cs (adds about 30k of compiled binary size) from the Go Kit, so that you can take that static tree and search and modify it, after the fact, inserting dynamic code. Rolex uses a simple variant of this approach to great effect. 2b startup time is lightning fast compared to 2a which had to resolve the slang code every time. Now it doesn't. Win.

Or you can just use deslang to take any slang and turn it into static codedom for your own nefarious purposes. The sky is the limit.

See the **DeslangDemo** project for a simple, contrived example, and **Rolex** for a more complicated real world example.

## CSBrick

CSBrick takes a C# Visual Studio Project, gathering all compilable source files in the project (except AssemblyInfo.cs) and merges them all into one minified C# source file. The reason for this is as a workaround for lack of out of the box static linking in .NET. Basically, you can include this merged source file instead of referencing the project it came from so you can create a project without 3rd party dependencies on other non-system assemblies. This is important for build tools since nobody wants to drag DLL's around with their exe that performs a pre-build step. So use this to make your build tools when you must reference an external library (like **CodeDOM Go Kit**). For a simple example, just run **CSBrick** and watch it spit out the **scratch** project to the console, minified. For a real world example, **Deslang** uses it to include **CodeDomGoKit** and **Rolex** uses it to include **Regex**

## Gplex

### Gplex is not my software.

I've included it for completeness since my projects use it and because the one on GitHub I could find will not build without the binaries which are hard to find.


Gardens Point LEX Copyright

Copyright © 2006-2011 Queensland University of Technology (QUT). All rights reserved.

Redistribution and use in source and binary forms, with or without modification are permitted provided that the following conditions are met:

1.	Redistribution of source code must retain the above copyright notice, this list of conditions and the following disclaimer.
2.	Redistributions in binary form must reproduce the above copyright notice, this list of conditions and the following disclaimer in the documentation and/or other materials with the distribution.

THIS SOFTWARE IS PROVIDED BY THE GPLEX PROJECT “AS IS’ AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE HEREBY DISCLAIMED. IN NO EVENT SHALL THE GPPG PROJECT OR QUT BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.

The views and conclusions contained in the software and documentation are those of the authors and should not be interpreted as representing official policies, either expressed or implied, of the GPLEX project or QUT.
