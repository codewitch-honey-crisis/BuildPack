using System;
using System.CodeDom;
using System.Collections.Generic;
using System.IO;

namespace Slang
{
    using ST = SlangTokenizer;
#if SLANGLIB
    public 
#endif
        static partial class SlangParser
	{
        public static CodeCompileUnit ParseCompileUnit(string text)
        {
            var tokenizer = new SlangTokenizer(text);
            return ParseCompileUnit(tokenizer);
        }
        public static CodeCompileUnit ReadCompileUnitFrom(Stream stream)
        {
            var tokenizer = new SlangTokenizer(stream);
            return ParseCompileUnit(tokenizer);
        }
        public static CodeCompileUnit ParseCompileUnit(string text, int line, int column, long position)
        {
            var tokenizer = new SlangTokenizer(text);
            var pc = new _PC(tokenizer);
            pc.SetLocation(line, column, position);
            return _ParseCompileUnit(pc);
        }
        public static CodeCompileUnit ReadCompileUnitFrom(Stream stream, int line, int column, long position)
        {
            var tokenizer = new SlangTokenizer(stream);
            var pc = new _PC(tokenizer);
            pc.SetLocation(line, column, position);
            return _ParseCompileUnit(pc);
        }
        internal static CodeCompileUnit ParseCompileUnit(IEnumerable<Token> tokenizer)
        {
            var pc = new _PC(tokenizer);
            pc.Advance(false);
            return _ParseCompileUnit(pc);
        }
        static CodeCompileUnit _ParseCompileUnit(_PC pc)
        {
            var l = pc.Line;
            var c = pc.Column;
            var p = pc.Position;
            var result = new CodeCompileUnit().Mark(l, c, p);
            var ns = new CodeNamespace().Mark(l, c, p);
            result.Namespaces.Add(ns);
            while (ST.directive == pc.SymbolId || ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
            {
                switch (pc.SymbolId)
                {
                    case ST.directive:
                        var d = _ParseDirective(pc) as CodeDirective;
                        if (null!=d)
                            result.StartDirectives.Add(d);
                        break;
                    case ST.blockComment:
                        ns.Comments.Add(_ParseCommentStatement(pc));
                        break;
                    case ST.lineComment:
                        ns.Comments.Add(_ParseCommentStatement(pc, true));
                        break;
                }
            }
            while (ST.usingKeyword == pc.SymbolId)
            {
                while (ST.directive == pc.SymbolId || ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
                    pc.Advance(false);
                var l2 = pc.Line;
                var c2 = pc.Column;
                var p2 = pc.Position;
                pc.Advance();
                var nsi = new CodeNamespaceImport(_ParseNamespaceName(pc)).SetLoc(l2, c2, p2);
                if (ST.semi != pc.SymbolId)
                    pc.Error("Expecting ; in using declaration");
                pc.Advance(false);
                ns.Imports.Add(nsi);
            }
            while(ST.lbracket==pc.SymbolId)
            {
                var pc2 = pc.GetLookAhead(true);
                pc2.Advance();
                if (ST.assemblyKeyword != pc2.SymbolId)
                    break;
                result.AssemblyCustomAttributes.AddRange(_ParseAttributeGroup(pc,false).Value);
            }
            while(!pc.IsEnded)
            {
                var startDirs = new CodeDirectiveCollection();
                var comments = new CodeCommentStatementCollection();
                CodeLinePragma lp = null;
                while (ST.directive == pc.SymbolId || ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
                {
                    switch (pc.SymbolId)
                    {
                        case ST.directive:
                            var d = _ParseDirective(pc);
                            var llp = d as CodeLinePragma;
                            if (null != llp)
                                lp = llp;
                            else if (null != d)
                                startDirs.Add(d as CodeDirective);
                            break;
                        case ST.blockComment:
                            comments.Add(_ParseCommentStatement(pc));
                            break;
                        case ST.lineComment:
                            comments.Add(_ParseCommentStatement(pc, true));
                            break;
                    }
                }
                if (ST.namespaceKeyword==pc.SymbolId)
                {
                    var nns = _ParseNamespace(pc);
                    nns.Comments.AddRange(comments);
                    result.Namespaces.Add(nns);
                } else
                {
                    var t = _ParseTypeDecl(pc, false, pc.Line, pc.Column, pc.Position, null);
                    t.Comments.AddRange(comments);
                    t.StartDirectives.AddRange(startDirs);
                    t.LinePragma = lp;
                    ns.Types.Add(t);
                }
            }
            return result;
        }
        static CodeNamespace _ParseNamespace(_PC pc)
        {
            var l = pc.Line;
            var c = pc.Column;
            var p = pc.Position;

            var result = new CodeNamespace().Mark(l,c,p);
            while(ST.lineComment==pc.SymbolId || ST.blockComment==pc.SymbolId || ST.directive ==pc.SymbolId)
            {
                if (ST.directive != pc.SymbolId)
                    result.Comments.Add(_ParseCommentStatement(pc, true));
            }
            if (ST.namespaceKeyword != pc.SymbolId)
                pc.Error("Expecting namespace");
            pc.Advance();
            result.Name=_ParseNamespaceName(pc);
            if (ST.lbrace != pc.SymbolId)
                pc.Error("Expecing { in namespace declaration");
            pc.Advance(false);
            if(ST.directive == pc.SymbolId || ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
            {
                var pc2 = pc.GetLookAhead(true);
                if (ST.usingKeyword == pc2.SymbolId)
                    pc.Advance();
            }
            while (ST.usingKeyword==pc.SymbolId)
            {
                while (ST.directive == pc.SymbolId || ST.lineComment == pc.SymbolId || ST.blockComment == pc.SymbolId)
                    pc.Advance(false);
                var l2 = pc.Line;
                var c2 = pc.Column;
                var p2 = pc.Position;
                pc.Advance();
                var nsi = new CodeNamespaceImport(_ParseNamespaceName(pc)).SetLoc(l2, c2, p2);
                if (ST.semi != pc.SymbolId)
                    pc.Error("Expecting ; in using declaration");
                pc.Advance(false);
                result.Imports.Add(nsi);
            }
            while(ST.rbrace!=pc.SymbolId)
            {
                result.Types.Add(_ParseTypeDecl(pc, false, pc.Line, pc.Column, pc.Position, null));
            }
            if (ST.rbrace != pc.SymbolId)
                pc.Error("Unterminated namespace declaration", l, c, p);
            pc.Advance(false);
            return result;
        }
        static string _ParseNamespaceName(_PC pc)
        {
            var l = pc.Line;
            var c = pc.Column;
            var p = pc.Position;
            var result = "";
            while (!pc.IsEnded && ST.lbrace != pc.SymbolId && ST.semi != pc.SymbolId)
            {
                if (0 < result.Length)
                    result += ".";
                result += _ParseIdentifier(pc);
                if (ST.lbrace == pc.SymbolId || ST.semi == pc.SymbolId)
                    break;
                var l2 = pc.Line;
                var c2 = pc.Column;
                var p2 = pc.Position;
                if (ST.dot != pc.SymbolId)
                    pc.Error("Expecting . in namespace name");
                pc.Advance();
                if (ST.lbrace == pc.SymbolId || ST.semi == pc.SymbolId)
                    pc.Error("Expecting identifier in namespace name", l2, c2, p2);
            }
            if ("" == result)
                pc.Error("Expecting identifier in namespace name",l,c,p);
            return result;
        }
        static T Mark<T>(this T obj, _PC pc, bool unresolved = false) where T : CodeObject
        {
            obj.UserData["codedomgokit:visit"] = true;
            if (unresolved)
                obj.UserData["slang:unresolved"] = true;
            SetLoc(obj, pc);
            return obj;
        }
        static T Mark<T>(this T obj, CodeObject co, bool unresolved = false) where T : CodeObject
        {
            obj.UserData["codedomgokit:visit"] = true;
            if (unresolved)
                obj.UserData["slang:unresolved"] = true;
            SetLoc(obj, co);
            return obj;
        }
        static T SetLoc<T>(this T obj, _PC pc) where T : CodeObject
        {
            obj.UserData["slang:line"] = pc.Line;
            obj.UserData["slang:column"] = pc.Column;
            obj.UserData["slang:position"] = pc.Position;
            return obj;
        }
        static T SetLoc<T>(this T obj, CodeObject co) where T: CodeObject
        {
            if (co.UserData.Contains("slang:line"))
                obj.UserData["slang:line"] = co.UserData["slang:line"];
            if (co.UserData.Contains("slang:column"))
                obj.UserData["slang:column"] = co.UserData["slang:column"];
            if (co.UserData.Contains("slang:position"))
                obj.UserData["slang:position"] = co.UserData["slang:position"];
            return obj;
        }
        static T Mark<T>(this T obj, int line,int column,long position, bool unresolved = false) where T : CodeObject
        {
            obj.UserData["codedomgokit:visit"] = true;
            if (unresolved)
                obj.UserData["slang:unresolved"] = true;
            SetLoc(obj, line,column,position);
            return obj;
        }
        static T SetLoc<T>(this T obj, int line,int column,long position) where T : CodeObject
        {
            obj.UserData["slang:line"] = line;
            obj.UserData["slang:column"] = column;
            obj.UserData["slang:position"] = position;
            return obj;
        }

#region _PC
        private class _PC : IDisposable
        {
            private int _state;
            private IEnumerator<Token> _e;
            private LookAheadEnumerator<Token> _el;
            private Token _t;
            private int _advanceCount;
            private int _line;
            private int _column;
            private long _position;
            private List<Token> _skipped;
            public _PC(IEnumerable<Token> tokenizer) :
                    this(tokenizer.GetEnumerator(), true)
            {
            }
            private _PC(IEnumerator<Token> enumerator, bool wrap)
            {
                this._e = enumerator;
                if (wrap)
                {
                    this._el = new LookAheadEnumerator<Token>(enumerator);
                    this._e = this._el;
                    // we need both pointers to point to the lookahead
                }
                this._state = -1;
                this._t.SymbolId = -1;
                this._advanceCount = 0;
                this._skipped = new List<Token>();
            }
            public List<Token> Skipped {
                get {
                    return this._skipped;
                }
            }
            public void SetLocation(int line, int column, long position)
            {
                this._line = line;
                this._column = column;
                this._position = position;
            }
            public void EnsureStarted()
            {
                if ((-1 == this._state))
                {
                    this.Advance();
                }
            }
            public _PC GetLookAhead()
            {
                if ((null == this._el))
                {
                    throw new NotSupportedException("This parser context does not support lookahead.");
                }
                _PC result = new _PC(this._el.LookAhead.GetEnumerator(), true);
                return result;
            }
            public Token Current {
                get {
                    return this._t;
                }
            }
            public _PC GetLookAhead(bool start)
            {
                _PC result = this.GetLookAhead();
                if (start)
                {
                    result.EnsureStarted();
                }
                return result;
            }
            public int AdvanceCount {
                get {
                    return this._advanceCount;
                }
            }
            public void ResetAdvanceCount()
            {
                this._advanceCount = 0;
            }
            public int SymbolId {
                get {
                    return this._t.SymbolId;
                }
            }
            public string Value {
                get {
                    return this._t.Value;
                }
            }
            public int Line {
                get {
                    return this._t.Line;
                }
            }
            public int Column {
                get {
                    return this._t.Column;
                }
            }
            public long Position {
                get {
                    return this._t.Position;
                }
            }
            public bool IsEnded {
                get {
                    return (-2 == this._state);
                }
            }
            public bool Advance(bool skipCommentsAndDirectives = true)
            {
                if (_Advance())
                {
                    if (!skipCommentsAndDirectives)
                        return true;
                    while ((ST.directive == SymbolId || ST.blockComment == SymbolId || ST.lineComment == SymbolId) && _Advance()) ;
                    return !IsEnded;
                }
                return false;
            }
            bool _Advance()
            {
                if ((false == this._e.MoveNext()))
                {
                    this._t.SymbolId = -2;
                    this._state = -2;
                }
                else
                {
                    // sanity check. should never happen
                    if ((int.MaxValue == this._advanceCount))
                    {
                        this._advanceCount = -1;
                    }
                    this._advanceCount = (this._advanceCount + 1);
                    this._state = 0;
                    this._t = this._e.Current;
                    this._t.Line = (this._t.Line + this._line);
                    this._t.Column = (this._t.Column + this._column);
                    this._t.Position = (this._t.Position + this._position);
                    if ((null != this._t.Skipped))
                    {
                        this._skipped.AddRange(this._t.Skipped);
                    }
                    return true;
                }
                return false;
            }
            [System.Diagnostics.DebuggerNonUserCode()]
            public void Error(string message, int line, int column, long position)
            {
                throw new SlangSyntaxException(message, line, column, position);
            }
            [System.Diagnostics.DebuggerNonUserCode()]
            public void Error(string message)
            {
                this.Error(message, this.Line, this.Column, this.Position);
            }
            public void Dispose()
            {
                this._e.Dispose();
                this._state = -3;
            }
        }
#endregion _PC

    }
}
