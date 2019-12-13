# CodeDOM Go Kit - Make the CodeDOM amazing

[See my Code Project article](https://www.codeproject.com/Articles/5253617/CodeDOM-Go-Kit-The-CodeDOM-is-Dead-Long-Live-the-C)

A suite of tools for code generation using the CodeDOM

I really hate the CodeDOM, and you should, too. Well, let's back up. I like the idea of the CodeDOM, but not the execution. It's clunky, kludgy and extremely limited. If I never cared about language agnostic code generation in .NET I'd never use it.

However, I do care about those things. I also care about tapping into things like Microsoft's XML serialization code generator and mutilating its output into something less awful.

Furthermore, Roslyn looks to be limited in terms of its platform availability and its language support, which seems set in stone at VB and C#. Meanwhile, the F# team produced a CodeDOM provider, this time for their language, so it looks like the CodeDOM can still generate in languages Roslyn can't, and on devices where Roslyn can't currently operate. I might be wrong about the platforms, and maybe .NET 5 will change everything, but this is where we are now.

Here's a few of the things the CodeDOM always needed:

* A `Parse()` function. Gosh that would have been cool.
* An `Eval()` function, even if it could only do certain expressions
* A `Visit()` function, or otherwise a way to search the graph

I decided to finally do something about that, and with it comes a really quick and clean way to build and edit CodeDOM trees.

This readme can't do it justice, so just visit the link
