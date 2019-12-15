# Make Awesome Build Tools in C#

## CodeDom Go Kit

### Slang

Slang is a miniature language. It's a subset of C# suitable for constructing CodeDOM trees. It obviates the need to build them by hand since you can do it by writing restricted C# code instead. The reason the language is restricted is the CodeDOM does not allow for full fidelity. Many features like read only fields, and even postfix increment/decrement can't be supported. Even with these limitations it doesn't matter as it makes using the CodeDOM pretty easy. Now you can include source files in your build as templates using T4 and C# syntax and generate CodeDOM graphs that way. Or you can simply render source dependencies in your target language even though you keep them in Slang/C# instead of CodeDOM graphs. It is included as part of the Go Kit.

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
