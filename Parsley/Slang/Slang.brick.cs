#define BACKUP
#define STACK
#define STANDALONE
#define PERSIST
using System;
using System.Collections.Generic;
using System.CodeDom;
using System.IO;
using System.Globalization;
using System.Text;
using System.Reflection;
using CD;
using System.CodeDom.Compiler;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;
namespace Slang{/// <summary>
/// Represents a lexeme/token
/// </summary>
struct Token{/// <summary>
/// Indicates the symbol id or -1 for the error symbol, -2 for end of input, and -3 for disposed
/// </summary>
public int SymbolId;/// <summary>
/// Indicates the value of the token
/// </summary>
public string Value;/// <summary>
/// Indicates the line where the token occurs
/// </summary>
public int Line;/// <summary>
/// Indicates the column where the token occurs
/// </summary>
public int Column;/// <summary>
/// Indicates the position where the token occurs
/// </summary>
public long Position;/// <summary>
/// If supported, indicates the tokens that were 
/// skipped between here and the last read
/// </summary>
#pragma warning disable CS0649
public Token[]Skipped;
#pragma warning restore CS0649
}}namespace Slang{/// <summary>
/// An enumerator that provides lookahead without advancing the cursor
/// </summary>
/// <typeparam name="T">The type to enumerate</typeparam>
internal class LookAheadEnumerator<T>:object,IEnumerator<T>{private const int _Enumerating=0;private const int _NotStarted=-2;private const int _Ended
=-1;private const int _Disposed=-3;private IEnumerator<T>_inner;private int _state; private const int _DefaultCapacity=16;private const float _GrowthFactor
=0.9F;private T[]_queue;private int _queueHead;private int _queueCount;/// <summary>
/// Creates a new instance. Once this is created, the inner/wrapped enumerator must not be touched.
/// </summary>
/// <param name="inner"></param>
public LookAheadEnumerator(IEnumerator<T>inner){this._inner=inner;this._state=LookAheadEnumerator<T>._NotStarted;this._queue=new T[LookAheadEnumerator<T>._DefaultCapacity];
this._queueHead=0;this._queueCount=0;}/// <summary>
/// Discards the lookahead and advances the cursor to the physical position.
/// </summary>
public void DiscardLookAhead(){for(;(1<this._queueCount);){this._Dequeue();}}/// <summary>
/// Retrieves the value under the cursor
/// </summary>
public T Current{get{if((0>this._state)){if((LookAheadEnumerator<T>._NotStarted==this._state)){throw new InvalidOperationException("The cursor is before the start of the enumeration.");
}if((LookAheadEnumerator<T>._Ended==this._state)){throw new InvalidOperationException("The cursor is after the end of the enumeration.");}throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);
}return this._queue[this._queueHead];}} object System.Collections.IEnumerator.Current{get{return this.Current;}}internal int QueueCount{get{return this._queueCount;
}}/// <summary>
/// Attempts to peek the specified number of positions from the current position without advancing
/// </summary>
/// <param name="lookahead">The offset from the current position to peek at</param>
/// <param name="value">The value returned</param>
/// <returns>True if the peek could be satisfied, otherwise false</returns>
public bool TryPeek(int lookahead,out T value){if((LookAheadEnumerator<T>._Disposed==this._state)){throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);
}if((0>lookahead)){throw new ArgumentOutOfRangeException("lookahead");}if((LookAheadEnumerator<T>._Ended==this._state)){value=default(T);return false;
}if((LookAheadEnumerator<T>._NotStarted==this._state)){if((0==lookahead)){value=default(T);return false;}}if((lookahead<this._queueCount)){value=this._queue[((lookahead
+this._queueHead)%this._queue.Length)];return true;}lookahead=(lookahead-this._queueCount);value=default(T);for(;((0<=lookahead)&&this._inner.MoveNext());
){value=this._inner.Current;this._Enqueue(value);lookahead=(lookahead-1);}return(-1==lookahead);}/// <summary>
/// Peek the specified number of positions from the current position without advancing
/// </summary>
/// <param name="lookahead">The offset from the current position to peek at</param>
/// <returns>The value at the specified position</returns>
public T Peek(int lookahead){T value;if((false==this.TryPeek(lookahead,out value))){throw new InvalidOperationException("There were not enough values in the enumeration to satisfy the request");
}return value;}internal bool IsEnumerating{get{return(-1<this._state);}}internal bool IsEnded{get{return(LookAheadEnumerator<T>._Ended==this._state);}
}/// <summary>
/// Retrieves a lookahead cursor from the current cursor that can be navigated without moving the main cursor
/// </summary>
public IEnumerable<T>LookAhead{get{if((0>this._state)){if((this._state==LookAheadEnumerator<T>._NotStarted)){throw new InvalidOperationException("The cursor is before the start of the enumeration.");
}if((this._state==LookAheadEnumerator<T>._Ended)){throw new InvalidOperationException("The cursor is after the end of the enumeration.");}throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);
}return new LookAheadEnumeratorEnumerable<T>(this);}}/// <summary>
/// Advances the cursor
/// </summary>
/// <returns>True if more input was read, otherwise false</returns>
bool System.Collections.IEnumerator.MoveNext(){if((0>this._state)){if((LookAheadEnumerator<T>._Disposed==this._state)){throw new ObjectDisposedException(typeof(LookAheadEnumerator<T>).Name);
}if((LookAheadEnumerator<T>._Ended==this._state)){return false;}if((LookAheadEnumerator<T>._NotStarted==this._state)){if((0<this._queueCount)){this._state
=LookAheadEnumerator<T>._Enumerating;return true;}if((false==this._inner.MoveNext())){this._state=LookAheadEnumerator<T>._Ended;return false;}this._Enqueue(this._inner.Current);
this._state=LookAheadEnumerator<T>._Enumerating;return true;}}this._Dequeue();if((0==this._queueCount)){if((false==this._inner.MoveNext())){this._state
=LookAheadEnumerator<T>._Ended;return false;}this._Enqueue(this._inner.Current);}return true;}/// <summary>
/// Resets the cursor, and clears the queue.
/// </summary>
void System.Collections.IEnumerator.Reset(){this._inner.Reset();if(((0<this._queueCount)&&(null==default(T)))){System.Array.Clear(this._queue,this._queueHead,
(this._queue.Length-this._queueHead));if(((this._queueHead+this._queueCount)>=this._queue.Length)){System.Array.Clear(this._queue,0,(this._queueHead+(this._queueCount
%this._queue.Length)));}}this._queueHead=0;this._queueCount=0;this._state=LookAheadEnumerator<T>._NotStarted;}
#region IDisposable Support
/// <summary>
/// Disposes of this instance
/// </summary>
void System.IDisposable.Dispose(){if((false==(LookAheadEnumerator<T>._Disposed==this._state))){this._inner.Dispose();this._state=LookAheadEnumerator<T>._Disposed;
}}void _Enqueue(T item){if((this._queueCount==this._queue.Length)){T[]arr=new T[((int)((this._queue.Length*(1+LookAheadEnumerator<T>._GrowthFactor))))];
if(((this._queueHead+this._queueCount)<=this._queue.Length)){System.Array.Copy(this._queue,arr,this._queueCount);this._queueHead=0;arr[this._queueCount]
=item;this._queueCount=(this._queueCount+1);this._queue=arr;}else{System.Array.Copy(this._queue,this._queueHead,arr,0,(this._queue.Length-this._queueHead));
System.Array.Copy(this._queue,0,arr,(this._queue.Length-this._queueHead),this._queueHead);this._queueHead=0;arr[this._queueCount]=item;this._queueCount
=(this._queueCount+1);this._queue=arr;}}else{this._queue[((this._queueHead+this._queueCount)%this._queue.Length)]=item;this._queueCount=(this._queueCount
+1);}}T _Dequeue(){if((0==this._queueCount)){throw new InvalidOperationException("The queue is empty");}T result=this._queue[this._queueHead];this._queue[this._queueHead]
=default(T);this._queueHead=(this._queueHead+1);this._queueHead=(this._queueHead%this._queue.Length);this._queueCount=(this._queueCount-1);return result;
}
#endregion
}[System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley","0.1.2.0")]internal class LookAheadEnumeratorEnumerable<T>:object,IEnumerable<T>{private LookAheadEnumerator<T>
_outer;public LookAheadEnumeratorEnumerable(LookAheadEnumerator<T>outer){this._outer=outer;}public IEnumerator<T>GetEnumerator(){ LookAheadEnumeratorEnumerator<T>
result=((LookAheadEnumeratorEnumerator<T>)(System.Activator.CreateInstance(typeof(LookAheadEnumeratorEnumerator<T>),this._outer)));return result;}System.Collections.IEnumerator
 System.Collections.IEnumerable.GetEnumerator(){return this.GetEnumerator();}}[System.CodeDom.Compiler.GeneratedCodeAttribute("Parsley","0.1.2.0")]internal
 class LookAheadEnumeratorEnumerator<T>:object,IEnumerator<T>{private const int _NotStarted=-2;private const int _Ended=-1;private const int _Disposed
=-3;private LookAheadEnumerator<T>_outer;private int _index;private T _current;public LookAheadEnumeratorEnumerator(LookAheadEnumerator<T>outer){this._outer
=outer;if(this._outer.IsEnumerating){this._current=this._outer.Current;}this._index=LookAheadEnumeratorEnumerator<T>._NotStarted;}public T Current{get
{if((0>this._index)){if((this._index==LookAheadEnumeratorEnumerator<T>._NotStarted)){throw new InvalidOperationException("The cursor is before the start of the enumeration.");
}if((this._index==LookAheadEnumeratorEnumerator<T>._Ended)){throw new InvalidOperationException("The cursor is after the end of the enumeration.");}throw
 new ObjectDisposedException(typeof(LookAheadEnumeratorEnumerator<T>).Name);}return this._current;}}object System.Collections.IEnumerator.Current{get{
return this.Current;}}void System.IDisposable.Dispose(){this._index=LookAheadEnumeratorEnumerator<T>._Disposed;}bool System.Collections.IEnumerator.MoveNext()
{T value;if((0>this._index)){if((this._index==LookAheadEnumeratorEnumerator<T>._Disposed)){throw new ObjectDisposedException(typeof(LookAheadEnumeratorEnumerator<T>).Name);
}if((this._index==LookAheadEnumeratorEnumerator<T>._Ended)){return false;}this._index=-1;}this._index=(this._index+1);if((false==this._outer.TryPeek(this._index,
out value))){this._index=LookAheadEnumeratorEnumerator<T>._Ended;return false;}this._current=value;return true;}void System.Collections.IEnumerator.Reset()
{this._index=LookAheadEnumeratorEnumerator<T>._NotStarted;}}}namespace Slang{using ST=SlangTokenizer;
#if SLANGLIB
public
#endif
static partial class SlangParser{public static CodeCompileUnit ParseCompileUnit(string text){var tokenizer=new SlangTokenizer(text);return ParseCompileUnit(tokenizer);
}public static CodeCompileUnit ReadCompileUnitFrom(Stream stream){var tokenizer=new SlangTokenizer(stream);return ParseCompileUnit(tokenizer);}public static
 CodeCompileUnit ParseCompileUnit(string text,int line,int column,long position){var tokenizer=new SlangTokenizer(text);var pc=new _PC(tokenizer);pc.SetLocation(line,
column,position);return _ParseCompileUnit(pc);}public static CodeCompileUnit ReadCompileUnitFrom(Stream stream,int line,int column,long position){var tokenizer
=new SlangTokenizer(stream);var pc=new _PC(tokenizer);pc.SetLocation(line,column,position);return _ParseCompileUnit(pc);}internal static CodeCompileUnit
 ParseCompileUnit(IEnumerable<Token>tokenizer){var pc=new _PC(tokenizer);pc.Advance(false);return _ParseCompileUnit(pc);}static CodeCompileUnit _ParseCompileUnit(_PC
 pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var result=new CodeCompileUnit().Mark(l,c,p);var ns=new CodeNamespace().Mark(l,c,p);result.Namespaces.Add(ns);
while(ST.directive==pc.SymbolId||ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId){switch(pc.SymbolId){case ST.directive:var d=_ParseDirective(pc)
as CodeDirective;if(null!=d)result.StartDirectives.Add(d);break;case ST.blockComment:ns.Comments.Add(_ParseCommentStatement(pc));break;case ST.lineComment:
ns.Comments.Add(_ParseCommentStatement(pc,true));break;}}while(ST.usingKeyword==pc.SymbolId){while(ST.directive==pc.SymbolId||ST.lineComment==pc.SymbolId
||ST.blockComment==pc.SymbolId)pc.Advance(false);var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;pc.Advance();var nsi=new CodeNamespaceImport(_ParseNamespaceName(pc)).SetLoc(l2,
c2,p2);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in using declaration");pc.Advance(false);ns.Imports.Add(nsi);}while(ST.lbracket==pc.SymbolId){var
 pc2=pc.GetLookAhead(true);pc2.Advance();if(ST.assemblyKeyword!=pc2.SymbolId)break;result.AssemblyCustomAttributes.AddRange(_ParseAttributeGroup(pc,false).Value);
}while(!pc.IsEnded){var startDirs=new CodeDirectiveCollection();var comments=new CodeCommentStatementCollection();CodeLinePragma lp=null;while(ST.directive
==pc.SymbolId||ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId){switch(pc.SymbolId){case ST.directive:var d=_ParseDirective(pc);var llp=d as
 CodeLinePragma;if(null!=llp)lp=llp;else if(null!=d)startDirs.Add(d as CodeDirective);break;case ST.blockComment:comments.Add(_ParseCommentStatement(pc));
break;case ST.lineComment:comments.Add(_ParseCommentStatement(pc,true));break;}}if(ST.namespaceKeyword==pc.SymbolId){var nns=_ParseNamespace(pc);nns.Comments.AddRange(comments);
result.Namespaces.Add(nns);}else{var t=_ParseTypeDecl(pc,false,pc.Line,pc.Column,pc.Position,null);t.Comments.AddRange(comments);t.StartDirectives.AddRange(startDirs);
t.LinePragma=lp;ns.Types.Add(t);}}return result;}static CodeNamespace _ParseNamespace(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var result
=new CodeNamespace().Mark(l,c,p);while(ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId||ST.directive==pc.SymbolId){if(ST.directive!=pc.SymbolId)
result.Comments.Add(_ParseCommentStatement(pc,true));}if(ST.namespaceKeyword!=pc.SymbolId)pc.Error("Expecting namespace");pc.Advance();result.Name=_ParseNamespaceName(pc);
if(ST.lbrace!=pc.SymbolId)pc.Error("Expecing { in namespace declaration");pc.Advance(false);if(ST.directive==pc.SymbolId||ST.lineComment==pc.SymbolId||
ST.blockComment==pc.SymbolId){var pc2=pc.GetLookAhead(true);if(ST.usingKeyword==pc2.SymbolId)pc.Advance();}while(ST.usingKeyword==pc.SymbolId){while(ST.directive
==pc.SymbolId||ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId)pc.Advance(false);var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;pc.Advance();
var nsi=new CodeNamespaceImport(_ParseNamespaceName(pc)).SetLoc(l2,c2,p2);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in using declaration");pc.Advance(false);
result.Imports.Add(nsi);}while(ST.rbrace!=pc.SymbolId){result.Types.Add(_ParseTypeDecl(pc,false,pc.Line,pc.Column,pc.Position,null));}if(ST.rbrace!=pc.SymbolId)
pc.Error("Unterminated namespace declaration",l,c,p);pc.Advance(false);return result;}static string _ParseNamespaceName(_PC pc){var l=pc.Line;var c=pc.Column;
var p=pc.Position;var result="";while(!pc.IsEnded&&ST.lbrace!=pc.SymbolId&&ST.semi!=pc.SymbolId){if(0<result.Length)result+=".";result+=_ParseIdentifier(pc);
if(ST.lbrace==pc.SymbolId||ST.semi==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.dot!=pc.SymbolId)pc.Error("Expecting . in namespace name");
pc.Advance();if(ST.lbrace==pc.SymbolId||ST.semi==pc.SymbolId)pc.Error("Expecting identifier in namespace name",l2,c2,p2);}if(""==result)pc.Error("Expecting identifier in namespace name",l,c,p);
return result;}static T Mark<T>(this T obj,_PC pc,bool unresolved=false)where T:CodeObject{obj.UserData["codedomgokit:visit"]=true;if(unresolved)obj.UserData["slang:unresolved"]
=true;SetLoc(obj,pc);return obj;}static T Mark<T>(this T obj,CodeObject co,bool unresolved=false)where T:CodeObject{obj.UserData["codedomgokit:visit"]
=true;if(unresolved)obj.UserData["slang:unresolved"]=true;SetLoc(obj,co);return obj;}static T SetLoc<T>(this T obj,_PC pc)where T:CodeObject{obj.UserData["slang:line"]
=pc.Line;obj.UserData["slang:column"]=pc.Column;obj.UserData["slang:position"]=pc.Position;return obj;}static T SetLoc<T>(this T obj,CodeObject co)where
 T:CodeObject{if(co.UserData.Contains("slang:line"))obj.UserData["slang:line"]=co.UserData["slang:line"];if(co.UserData.Contains("slang:column"))obj.UserData["slang:column"]
=co.UserData["slang:column"];if(co.UserData.Contains("slang:position"))obj.UserData["slang:position"]=co.UserData["slang:position"];return obj;}static
 T Mark<T>(this T obj,int line,int column,long position,bool unresolved=false)where T:CodeObject{obj.UserData["codedomgokit:visit"]=true;if(unresolved)
obj.UserData["slang:unresolved"]=true;SetLoc(obj,line,column,position);return obj;}static T SetLoc<T>(this T obj,int line,int column,long position)where
 T:CodeObject{obj.UserData["slang:line"]=line;obj.UserData["slang:column"]=column;obj.UserData["slang:position"]=position;return obj;}
#region _PC
private class _PC:IDisposable{private int _state;private IEnumerator<Token>_e;private LookAheadEnumerator<Token>_el;private Token _t;private int _advanceCount;
private int _line;private int _column;private long _position;private List<Token>_skipped;public _PC(IEnumerable<Token>tokenizer):this(tokenizer.GetEnumerator(),
true){}private _PC(IEnumerator<Token>enumerator,bool wrap){this._e=enumerator;if(wrap){this._el=new LookAheadEnumerator<Token>(enumerator);this._e=this._el;
}this._state=-1;this._t.SymbolId=-1;this._advanceCount=0;this._skipped=new List<Token>();}public List<Token>Skipped{get{return this._skipped;}}public void
 SetLocation(int line,int column,long position){this._line=line;this._column=column;this._position=position;}public void EnsureStarted(){if((-1==this._state))
{this.Advance();}}public _PC GetLookAhead(){if((null==this._el)){throw new NotSupportedException("This parser context does not support lookahead.");}_PC
 result=new _PC(this._el.LookAhead.GetEnumerator(),true);return result;}public Token Current{get{return this._t;}}public _PC GetLookAhead(bool start){
_PC result=this.GetLookAhead();if(start){result.EnsureStarted();}return result;}public int AdvanceCount{get{return this._advanceCount;}}public void ResetAdvanceCount()
{this._advanceCount=0;}public int SymbolId{get{return this._t.SymbolId;}}public string Value{get{return this._t.Value;}}public int Line{get{return this._t.Line;
}}public int Column{get{return this._t.Column;}}public long Position{get{return this._t.Position;}}public bool IsEnded{get{return(-2==this._state);}}public
 bool Advance(bool skipCommentsAndDirectives=true){if(_Advance()){if(!skipCommentsAndDirectives)return true;while((ST.directive==SymbolId||ST.blockComment
==SymbolId||ST.lineComment==SymbolId)&&_Advance());return!IsEnded;}return false;}bool _Advance(){if((false==this._e.MoveNext())){this._t.SymbolId=-2;this._state
=-2;}else{ if((int.MaxValue==this._advanceCount)){this._advanceCount=-1;}this._advanceCount=(this._advanceCount+1);this._state=0;this._t=this._e.Current;
this._t.Line=(this._t.Line+this._line);this._t.Column=(this._t.Column+this._column);this._t.Position=(this._t.Position+this._position);if((null!=this._t.Skipped))
{this._skipped.AddRange(this._t.Skipped);}return true;}return false;}[System.Diagnostics.DebuggerNonUserCode()]public void Error(string message,int line,
int column,long position){throw new SlangSyntaxException(message,line,column,position);}[System.Diagnostics.DebuggerNonUserCode()]public void Error(string
 message){this.Error(message,this.Line,this.Column,this.Position);}public void Dispose(){this._e.Dispose();this._state=-3;}}
#endregion _PC
}}namespace Slang{using ST=SlangTokenizer;partial class SlangParser{public static CodeExpression ParseExpression(string text){var tokenizer=new SlangTokenizer(text);
return ParseExpression(tokenizer);}public static CodeExpression ReadExpressionFrom(Stream stream){var tokenizer=new SlangTokenizer(stream);return ParseExpression(tokenizer);
}public static CodeExpression ParseExpression(string text,int line,int column,long position){var tokenizer=new SlangTokenizer(text);var pc=new _PC(tokenizer);
pc.SetLocation(line,column,position);return _ParseExpression(pc);}public static CodeExpression ReadExpressionFrom(Stream stream,int line,int column,long
 position){var tokenizer=new SlangTokenizer(stream);var pc=new _PC(tokenizer);pc.SetLocation(line,column,position);return _ParseExpression(pc);}internal
 static CodeExpression ParseExpression(IEnumerable<Token>tokenizer){var pc=new _PC(tokenizer);pc.EnsureStarted();return _ParseExpression(pc);}static CodeExpression
 _ParseExpression(_PC pc){return _ParseAssignExpression(pc);}static CodeExpression _ParseAssignExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=
pc.Position;var unresolved=false;var lhs=_ParseOrExpression(pc);var op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case ST.eq:op=CodeBinaryOperatorType.Assign;
pc.Advance();return new CodeBinaryOperatorExpression(lhs,op,_ParseOrExpression(pc)).Mark(l,c,p);case ST.addAssign:unresolved=true; op=CodeBinaryOperatorType.Add;
pc.Advance();break;case ST.subAssign:unresolved=true; op=CodeBinaryOperatorType.Subtract;pc.Advance();break;case ST.mulAssign:op=CodeBinaryOperatorType.Multiply;
pc.Advance();break;case ST.divAssign:op=CodeBinaryOperatorType.Divide;pc.Advance();break;case ST.modAssign:op=CodeBinaryOperatorType.Modulus;pc.Advance();
break;case ST.bitwiseAndAssign:op=CodeBinaryOperatorType.BitwiseAnd;pc.Advance();break;case ST.bitwiseOrAssign:op=CodeBinaryOperatorType.BitwiseOr;pc.Advance();
break;default:return lhs;}return new CodeBinaryOperatorExpression(lhs,CodeBinaryOperatorType.Assign,new CodeBinaryOperatorExpression(lhs,op,_ParseOrExpression(pc)).Mark(l,
c,p)).Mark(l,c,p,unresolved);}static CodeExpression _ParseBitwiseOrExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var lhs=_ParseBitwiseAndExpression(pc);
var op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case ST.bitwiseOr:op=CodeBinaryOperatorType.BitwiseOr;pc.Advance();break;default:return lhs;
}return new CodeBinaryOperatorExpression(lhs,op,_ParseBitwiseAndExpression(pc)).Mark(l,c,p);}static CodeExpression _ParseAndExpression(_PC pc){var l=pc.Line;
var c=pc.Column;var p=pc.Position;var lhs=_ParseBitwiseOrExpression(pc);var op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case ST.and:op=CodeBinaryOperatorType.BooleanAnd;
pc.Advance();break;default:return lhs;}return new CodeBinaryOperatorExpression(lhs,op,_ParseBitwiseOrExpression(pc)).Mark(l,c,p);}static CodeExpression
 _ParseOrExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var lhs=_ParseAndExpression(pc);var op=default(CodeBinaryOperatorType);switch
(pc.SymbolId){case ST.or:op=CodeBinaryOperatorType.BooleanOr;pc.Advance();break;default:return lhs;}return new CodeBinaryOperatorExpression(lhs,op,_ParseAndExpression(pc)).Mark(l,
c,p);}static CodeExpression _ParseBitwiseAndExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var lhs=_ParseEqualityExpression(pc);var
 op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case ST.bitwiseAnd:op=CodeBinaryOperatorType.BitwiseAnd;pc.Advance();break;default:return lhs;
}return new CodeBinaryOperatorExpression(lhs,op,_ParseEqualityExpression(pc)).Mark(l,c,p);}static CodeExpression _ParseEqualityExpression(_PC pc){var l
=pc.Line;var c=pc.Column;var p=pc.Position;var lhs=_ParseRelationalExpression(pc);var op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case ST.eqEq:
op=CodeBinaryOperatorType.IdentityEquality;pc.Advance();break;case ST.notEq:op=CodeBinaryOperatorType.IdentityInequality;pc.Advance();break;default:return
 lhs;}return new CodeBinaryOperatorExpression(lhs,op,_ParseRelationalExpression(pc)).Mark(l,c,p,true);}static CodeExpression _ParseRelationalExpression(_PC
 pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var lhs=_ParseTermExpression(pc);var op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case ST.lt:
op=CodeBinaryOperatorType.LessThan;pc.Advance();break;case ST.lte:op=CodeBinaryOperatorType.LessThanOrEqual;pc.Advance();break;case ST.gt:op=CodeBinaryOperatorType.GreaterThan;
pc.Advance();break;case ST.gte:op=CodeBinaryOperatorType.GreaterThanOrEqual;pc.Advance();break;default:return lhs;}return new CodeBinaryOperatorExpression(lhs,
op,_ParseTermExpression(pc)).Mark(l,c,p);}static CodeExpression _ParseTermExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var lhs=_ParseFactorExpression(pc);
var op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case ST.add:op=CodeBinaryOperatorType.Add;pc.Advance();break;case ST.sub:op=CodeBinaryOperatorType.Subtract;
pc.Advance();break;default:return lhs;}return new CodeBinaryOperatorExpression(lhs,op,_ParseFactorExpression(pc)).Mark(l,c,p);}static CodeExpression _ParseFactorExpression(_PC
 pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var lhs=_ParseUnaryExpression(pc);var op=default(CodeBinaryOperatorType);switch(pc.SymbolId){case
 ST.mul:pc.Advance();op=CodeBinaryOperatorType.Multiply;break;case ST.div:pc.Advance();op=CodeBinaryOperatorType.Divide;break;case ST.mod:pc.Advance();
op=CodeBinaryOperatorType.Modulus;break;default:return lhs;}return new CodeBinaryOperatorExpression(lhs,op,_ParseUnaryExpression(pc)).Mark(l,c,p);}static
 CodeExpression _ParseUnaryExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var sid=pc.SymbolId;if(ST.lparen==sid){var pc2=pc.GetLookAhead(true);
try{_ParseCastExpression(pc2);return _ParseCastExpression(pc);}catch(SlangSyntaxException){return _ParsePrimaryExpression(pc);}}switch(pc.SymbolId){case
 ST.add:pc.Advance();return _ParseUnaryExpression(pc);case ST.sub:pc.Advance();var rhs=_ParseUnaryExpression(pc);var pp=rhs as CodePrimitiveExpression;
if(null!=pp){if(pp.Value is int)return new CodePrimitiveExpression(-(int)pp.Value).SetLoc(l,c,p);if(pp.Value is long)return new CodePrimitiveExpression(-(long)pp.Value).SetLoc(l,c,p);
if(pp.Value is float)return new CodePrimitiveExpression(-(float)pp.Value).SetLoc(l,c,p);if(pp.Value is double)return new CodePrimitiveExpression(-(double)pp.Value).SetLoc(l,c,p);
if(pp.Value is char)return new CodePrimitiveExpression(-(char)pp.Value).SetLoc(l,c,p);}return new CodeBinaryOperatorExpression(new CodePrimitiveExpression(0).SetLoc(l,c,p),
CodeBinaryOperatorType.Subtract,rhs).Mark(l,c,p);case ST.not:pc.Advance();return new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false).SetLoc(l,
c,p),CodeBinaryOperatorType.ValueEquality,_ParseUnaryExpression(pc)).Mark(l,c,p);case ST.inc:pc.Advance();var expr=_ParseUnaryExpression(pc);return new
 CodeBinaryOperatorExpression(expr,CodeBinaryOperatorType.Assign,new CodeBinaryOperatorExpression(expr,CodeBinaryOperatorType.Add,new CodePrimitiveExpression(1).Mark(l,
c,p)).Mark(l,c,p)).Mark(l,c,p);case ST.dec:pc.Advance();expr=_ParseUnaryExpression(pc);return new CodeBinaryOperatorExpression(expr,CodeBinaryOperatorType.Assign,
new CodeBinaryOperatorExpression(expr,CodeBinaryOperatorType.Subtract,new CodePrimitiveExpression(1).Mark(l,c,p)).Mark(l,c,p)).Mark(l,c,p);}return _ParsePrimaryExpression(pc);
}static CodeExpression _ParsePrimaryExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;CodeExpression result=null;switch(pc.SymbolId){
case ST.verbatimStringLiteral:result=_ParseVerbatimString(pc);break;case ST.stringLiteral:result=_ParseString(pc);break;case ST.characterLiteral:result
=_ParseChar(pc);break;case ST.integerLiteral:result=_ParseInteger(pc);break;case ST.floatLiteral:result=_ParseFloat(pc);break;case ST.boolLiteral:result
=new CodePrimitiveExpression("true"==pc.Value).SetLoc(l,c,p);pc.Advance();break;case ST.nullLiteral:pc.Advance(); return new CodePrimitiveExpression(null).SetLoc(l,c,p);
case ST.identifier:case ST.verbatimIdentifier:result=new CodeVariableReferenceExpression(_ParseIdentifier(pc)).Mark(l,c,p,true);break;case ST.typeOf:result
=_ParseTypeOf(pc);break;case ST.defaultOf:result=_ParseDefault(pc);break;case ST.newKeyword:result=_ParseNew(pc);break;case ST.thisRef:result=new CodeThisReferenceExpression().SetLoc(l,
c,p);pc.Advance();break;case ST.baseRef:result=new CodeBaseReferenceExpression().SetLoc(l,c,p);pc.Advance();break;case ST.lparen: pc.Advance();result=_ParseExpression(pc);
if(ST.rparen!=pc.SymbolId)pc.Error("Unterminated ( in subexpression",l,c,p);pc.Advance();break;case ST.objectType:case ST.boolType:case ST.stringType:
case ST.charType:case ST.byteType:case ST.sbyteType:case ST.shortType:case ST.ushortType:case ST.intType:case ST.uintType:case ST.longType:case ST.ulongType:
case ST.floatType:case ST.doubleType:case ST.decimalType:result=new CodeTypeReferenceExpression(_ParseType(pc)).Mark(l,c,p);break;default:result=_ParseTypeOrFieldRef(pc);
break;}var done=false;while(!done&&!pc.IsEnded){l=pc.Line;c=pc.Column;p=pc.Position;switch(pc.SymbolId){case ST.lparen:pc.Advance();var di=new CodeDelegateInvokeExpression(result).Mark(l,
c,p,true);di.Parameters.AddRange(_ParseMethodArgList(pc));if(ST.rparen!=pc.SymbolId)throw new SlangSyntaxException("Unterminated method or delegate invoke expression",
l,c,p);pc.Advance();result=di;break;case ST.lbracket:pc.Advance();var idxr=new CodeIndexerExpression(result).Mark(l,c,p,true);idxr.Indices.AddRange(_ParseArgList(pc));
if(ST.rbracket!=pc.SymbolId)throw new SlangSyntaxException("Unterminated indexer expression",l,c,p);pc.Advance();result=idxr;break;case ST.dot:pc.Advance();
result=new CodeFieldReferenceExpression(result,_ParseIdentifier(pc)).Mark(l,c,p,true);break;default:done=true;break;}}return result;}static CodeExpression
 _ParseCastExpression(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in cast expression");pc.Advance();
var ctr=_ParseType(pc);if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in cast expression");pc.Advance();return new CodeCastExpression(ctr,_ParseUnaryExpression(pc)).Mark(l,
c,p);}static CodeExpression _ParseNew(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.newKeyword!=pc.SymbolId)pc.Error("Expecting new");
pc.Advance();var te=_ParseTypeElement(pc);if(ST.lparen==pc.SymbolId){pc.Advance();var oc=new CodeObjectCreateExpression(te).Mark(l,c,p);oc.Parameters.AddRange(_ParseArgList(pc));
if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in new object expression");pc.Advance();return oc;}else if(ST.lbracket!=pc.SymbolId)pc.Error("Expecting [ or ( after type element in new expression");
var pc2=pc.GetLookAhead(true);pc2.Advance();if(ST.comma==pc2.SymbolId)throw new SlangSyntaxException("Instantiation of multidimensional arrays is not supported",l,c,p);
var hasSize=false;if(ST.rbracket!=pc2.SymbolId)hasSize=true;pc2=null;CodeExpression size=null;if(hasSize){pc.Advance();size=_ParseExpression(pc);if(ST.comma==pc.SymbolId)
throw new SlangSyntaxException("Instantiation of multidimensional arrays is not supported",l,c,p);if(ST.rbracket!=pc.SymbolId)pc.Error("Expecting ] in new array expression");
pc.Advance();}var ctr=new CodeTypeReference(te,1).Mark(te); if(ST.lbracket==pc.SymbolId)ctr=_ParseTypeArraySpec(pc,ctr);var ace=new CodeArrayCreateExpression(ctr).Mark(l,c,p);
if(!hasSize){if(ST.lbrace!=pc.SymbolId)pc.Error("Expecting intitializer in new array expression");pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)
{ace.Initializers.Add(_ParseExpression(pc));if(ST.rbrace==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)
pc.Error("Expecting , in array initializer expression list");pc.Advance();if(ST.lbrace==pc.SymbolId)throw new SlangSyntaxException("Expecting expression in array initializer expression list",
l2,c2,p2);}if(pc.IsEnded)throw new SlangSyntaxException("Unterminated array initializer list",l,c,p);pc.Advance();}else ace.SizeExpression=size;return
 ace;}static CodeExpressionCollection _ParseArgList(_PC pc){var result=new CodeExpressionCollection();while(!pc.IsEnded&&ST.rparen!=pc.SymbolId&&ST.rbracket!=pc.SymbolId)
{result.Add(_ParseExpression(pc));if(ST.rparen==pc.SymbolId||ST.rbracket==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma
!=pc.SymbolId)pc.Error("Expecting , in argument list");pc.Advance();if(ST.rbracket==pc.SymbolId||ST.rparen==pc.SymbolId)throw new SlangSyntaxException("Expecting expression in argument list",
l2,c2,p2);}return result;}static CodeExpressionCollection _ParseMethodArgList(_PC pc){var result=new CodeExpressionCollection();while(!pc.IsEnded&&ST.rparen
!=pc.SymbolId&&ST.rbracket!=pc.SymbolId){var fd=FieldDirection.In;if(ST.refKeyword==pc.SymbolId){fd=FieldDirection.Ref;pc.Advance();}else if(ST.outKeyword==pc.SymbolId)
{fd=FieldDirection.Out;pc.Advance();}var e=_ParseExpression(pc);if(FieldDirection.In!=fd)e=new CodeDirectionExpression(fd,e);result.Add(e);if(ST.rparen
==pc.SymbolId||ST.rbracket==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in method argument list");
pc.Advance();if(ST.rbracket==pc.SymbolId||ST.rparen==pc.SymbolId)throw new SlangSyntaxException("Expecting expression in method argument list",l2,c2,p2);
}return result;}static CodeExpression _ParseTypeOf(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.typeOf!=pc.SymbolId)pc.Error("Expecting typeof");
pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in typeof expression");pc.Advance();var ctr=_ParseType(pc);if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in typeof expression");
pc.Advance();return new CodeTypeOfExpression(ctr).Mark(l,c,p);}static CodeExpression _ParseDefault(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;
if(ST.defaultOf!=pc.SymbolId)pc.Error("Expecting default");pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in default expression");pc.Advance();
var ctr=_ParseType(pc);if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in default expression");pc.Advance();return new CodeDefaultValueExpression(ctr).Mark(l,
c,p);}static CodeExpression _ParseTypeOrFieldRef(_PC context,bool skipDot=false){var l=context.Line;var c=context.Column;var p=context.Position; var line
=context.Line;var column=context.Column;var position=context.Position;if(skipDot)context.Advance();if(context.IsEnded)context.Error("Unexpected end of stream while parsing type of field reference");
var pc2=context.GetLookAhead(true);SlangSyntaxException sx,sx2=null;var fieldAdvCount=0;try{_ParseIdentifier(pc2); if(ST.dot==pc2.SymbolId){if(pc2.IsEnded)
pc2.Error("Unexpected end of stream while parsing type of field reference");var pc4=pc2.GetLookAhead(true);while(ST.dot==pc4.SymbolId){pc4.Advance();_ParseIdentifier(pc4);
}var i=pc4.AdvanceCount;while(1<i){pc2.Advance();--i;}} if(ST.lt==pc2.SymbolId){pc2.Advance();var isTypeArg=false;try{_ParseType(pc2);if(ST.gt==pc2.SymbolId
||ST.comma==pc2.SymbolId){isTypeArg=true;}}catch(SlangSyntaxException){} if(isTypeArg)context.Error("Unexpected < found in found in FielRef");} return
 new CodeVariableReferenceExpression(_ParseIdentifier(context)).Mark(l,c,p,true);}catch(SlangSyntaxException ex){sx=ex;} fieldAdvCount=pc2.AdvanceCount;
if(context.IsEnded)context.Error("Unexpected end of stream while parsing type of field reference"); var pc3=context.GetLookAhead(true);try{ _ParseType(pc3);
 return new CodeTypeReferenceExpression(_ParseType(context)).Mark(line,column,position);}catch(SlangSyntaxException ex){sx2=ex;}var advCount=pc3.AdvanceCount;
 var typeAdvCount=advCount; var toks=new List<Token>();if(context.IsEnded)context.Error("Unexpected end of stream while parsing type of field reference");
pc2=context.GetLookAhead(true);while(0!=advCount){toks.Add(pc2.Current);pc2.Advance();--advCount;} while(1<toks.Count){var throwMemberRef=false;toks.RemoveAt(toks.Count
-1);var t=default(Token);t.SymbolId=ST.rparen;toks.Add(t);try{pc3=new _PC(toks);pc3.EnsureStarted();_ParseType(pc3); pc3=new _PC(toks);pc3.EnsureStarted();
var pn=_ParseType(pc3);var i=toks.Count-1;while(0!=i){context.Advance();--i;}return new CodeTypeReferenceExpression(pn).Mark(line,column,position);}catch
(SlangSyntaxException ex){if(throwMemberRef)throw ex;else if(fieldAdvCount<typeAdvCount)throw sx2;throw sx;}}if(fieldAdvCount<typeAdvCount)throw sx2;throw
 sx;}internal static CodeTypeReference ParseType(IEnumerable<Token>tokenizer){var pc=new _PC(tokenizer);pc.EnsureStarted();return _ParseType(pc);}static
 bool _IsIntrinsicType(_PC pc){switch(pc.SymbolId){case ST.objectType:case ST.boolType:case ST.stringType:case ST.charType:case ST.byteType:case ST.sbyteType:
case ST.shortType:case ST.ushortType:case ST.intType:case ST.uintType:case ST.longType:case ST.ulongType:case ST.floatType:case ST.doubleType:case ST.decimalType:
return true;}return false;}static CodeTypeReference _ParseIntrinsicType(_PC pc){switch(pc.SymbolId){case ST.objectType:pc.Advance();return new CodeTypeReference(typeof(object)).SetLoc(pc);
case ST.boolType:pc.Advance();return new CodeTypeReference(typeof(bool)).SetLoc(pc);case ST.stringType:pc.Advance();return new CodeTypeReference(typeof(string)).SetLoc(pc);
case ST.charType:pc.Advance();return new CodeTypeReference(typeof(char)).SetLoc(pc);case ST.byteType:pc.Advance();return new CodeTypeReference(typeof(byte)).SetLoc(pc);
case ST.sbyteType:pc.Advance();return new CodeTypeReference(typeof(sbyte)).SetLoc(pc);case ST.shortType:pc.Advance();return new CodeTypeReference(typeof(short)).SetLoc(pc);
case ST.ushortType:pc.Advance();return new CodeTypeReference(typeof(ushort)).SetLoc(pc);case ST.intType:pc.Advance();return new CodeTypeReference(typeof(int)).SetLoc(pc);
case ST.uintType:pc.Advance();return new CodeTypeReference(typeof(uint)).SetLoc(pc);case ST.longType:pc.Advance();return new CodeTypeReference(typeof(long)).SetLoc(pc);
case ST.ulongType:pc.Advance();return new CodeTypeReference(typeof(ulong)).SetLoc(pc);case ST.floatType:pc.Advance();return new CodeTypeReference(typeof(float)).SetLoc(pc);
case ST.doubleType:pc.Advance();return new CodeTypeReference(typeof(double)).SetLoc(pc);case ST.decimalType:pc.Advance();return new CodeTypeReference(typeof(decimal)).SetLoc(pc);
}pc.Error("Expecting intrinsic type");return null;}static void _ParseTypeGenerics(_PC pc,CodeTypeReference bt){var l=pc.Line;var c=pc.Column;var p=pc.Position;
if(ST.lt!=pc.SymbolId)pc.Error("Expecting < in type generic specifier");pc.Advance();var tgc=0;if(ST.comma!=pc.SymbolId&&ST.gt!=pc.SymbolId){while(!pc.IsEnded
&&ST.gt!=pc.SymbolId){bt.TypeArguments.Add(_ParseType(pc));if(ST.gt==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=
pc.SymbolId)pc.Error("Expecting , or > in type generic specifier");pc.Advance();if(ST.gt==pc.SymbolId)throw new SlangSyntaxException("Expecting type or > in type generic specifier",
l2,c2,p2);}}else{tgc=1;while(ST.comma==pc.SymbolId){++tgc;pc.Advance();}}if(pc.IsEnded||ST.gt!=pc.SymbolId)throw new SlangSyntaxException("Unterminated type generic specifier",
l,c,p);pc.Advance(); if(0!=tgc)bt.BaseType+="`"+tgc.ToString();}static CodeTypeReference _ParseTypeArraySpec(_PC pc,CodeTypeReference et){var ranks=new
 List<int>();var ctrs=new List<CodeTypeReference>();var result=et;if(ST.lbracket!=pc.SymbolId)pc.Error("Expecting [ in type array specification");var rank
=1;var inBrace=true;while(pc.Advance()){if(inBrace&&ST.comma==pc.SymbolId){++rank;continue;}else if(ST.rbracket==pc.SymbolId){ranks.Add(rank);ctrs.Add(new
 CodeTypeReference().Mark(pc));rank=1;if(!pc.Advance())break;inBrace=false;if(ST.lbracket!=pc.SymbolId)break;else inBrace=true;}else break;}for(var i=
ranks.Count-1;-1<i;--i){var ctr=ctrs[i];ctr.ArrayElementType=result;ctr.ArrayRank=ranks[i];result=ctr;}return result;}static CodeTypeReference _ParseType(_PC
 pc){var result=_ParseTypeElement(pc);if(ST.lbracket==pc.SymbolId)result=_ParseTypeArraySpec(pc,result);return result;}static CodeTypeReference _ParseTypeElement(_PC
 pc){var result=_ParseTypeBase(pc);if(ST.lt==pc.SymbolId)_ParseTypeGenerics(pc,result);return result;}static CodeTypeReference _ParseTypeBase(_PC pc){
if(_IsIntrinsicType(pc))return _ParseIntrinsicType(pc);var result=new CodeTypeReference().SetLoc(pc);if(ST.globalKeyword==pc.SymbolId){pc.Advance();if
(ST.colonColon!=pc.SymbolId)pc.Error("Expecting :: in global type reference");pc.Advance();result.Options=CodeTypeReferenceOptions.GlobalReference;}if
(ST.verbatimIdentifier!=pc.SymbolId&&ST.identifier!=pc.SymbolId)pc.Error("Expecting Identifier");result.BaseType=_ParseIdentifier(pc);if(ST.dot==pc.SymbolId)
{ result.UserData["slang:unresolved"]=true;result.BaseType+=pc.Value;pc.Advance();while(ST.verbatimIdentifier==pc.SymbolId||ST.identifier==pc.SymbolId)
{result.BaseType+=_ParseIdentifier(pc);if(ST.dot!=pc.SymbolId)break;result.BaseType+=".";pc.Advance();}}return result;}static string _ParseIdentifier(_PC
 pc){var s=pc.Value;switch(pc.SymbolId){case ST.identifier:if(Keywords.Contains(s))break;pc.Advance();return s;case ST.verbatimIdentifier:pc.Advance();
return s.Substring(1);}pc.Error("Expecting identifier");return null;}
#region Parse Primitives
static CodePrimitiveExpression _ParseString(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var sb=new StringBuilder();var e=pc.Value.GetEnumerator();
e.MoveNext();if(e.MoveNext()){while(true){if('\"'==e.Current){pc.Advance();return new CodePrimitiveExpression(sb.ToString()).SetLoc(l,c,p);}else if('\\'
==e.Current)sb.Append(_ParseEscapeChar(e,pc));else{sb.Append(e.Current);if(!e.MoveNext())break;}}}throw new SlangSyntaxException("Unterminated string in input",
l,c,p);}static CodePrimitiveExpression _ParseVerbatimString(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var sb=new StringBuilder();var e=pc.Value.GetEnumerator();
e.MoveNext();e.MoveNext();if(e.MoveNext()){while(true){if('\"'==e.Current){if(!e.MoveNext()||'\"'!=e.Current){pc.Advance();return new CodePrimitiveExpression(sb.ToString()).SetLoc(l,
c,p);}sb.Append('\"');if(!e.MoveNext())break;}else{sb.Append(e.Current);if(!e.MoveNext())break;}}}throw new SlangSyntaxException("Unterminated string in input",
l,c,p);}static CodePrimitiveExpression _ParseChar(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var s=pc.Value; s=s.Substring(1,s.Length-2);
var e=s.GetEnumerator();e.MoveNext();if('\\'==e.Current){s=_ParseEscapeChar(e,pc);pc.Advance();if(1==s.Length)return new CodePrimitiveExpression(s[0]).SetLoc(l,c,p);
else return new CodePrimitiveExpression(s).SetLoc(l,c,p);}pc.Advance();return new CodePrimitiveExpression(s[0]).SetLoc(l,c,p);}static CodePrimitiveExpression
 _ParseFloat(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var s=pc.Value;var ch=char.ToLowerInvariant(s[s.Length-1]);var isDouble='d'==ch;var
 isDecimal='m'==ch;var isFloat='f'==ch;if((isDouble||isDecimal||isFloat))s=s.Substring(0,s.Length-1);else isDouble=true;object n=null;if(isFloat)n=float.Parse(s);
else if(isDecimal)n=decimal.Parse(s);else n=double.Parse(s);pc.Advance();return new CodePrimitiveExpression(n).SetLoc(l,c,p);}static CodePrimitiveExpression
 _ParseInteger(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var s=pc.Value;var isLong=false;var isUnsigned=false;var isNeg='-'==s[0];var isHex
=s.StartsWith("-0x")||s.StartsWith("0x");var ch=char.ToLowerInvariant(s[s.Length-1]);if('l'==ch){isLong=true;s=s.Substring(0,s.Length-1);}else if('u'==
ch){isUnsigned=true;s=s.Substring(0,s.Length-1);} ch=char.ToLowerInvariant(s[s.Length-1]);if('l'==ch){isLong=true;s=s.Substring(0,s.Length-1);}else if
('u'==ch){isUnsigned=true;s=s.Substring(0,s.Length-1);} if(isHex)s=s.Substring(2);var d=(double)long.Parse(s,isHex?NumberStyles.AllowHexSpecifier:NumberStyles.Integer);
object n=null;if(isUnsigned&&(isLong||(d<=uint.MaxValue&&d>=uint.MinValue))){if(isNeg){if(!isHex)n=unchecked((ulong)long.Parse(s));else n=unchecked((ulong)-long.Parse(s.Substring(1),
NumberStyles.AllowHexSpecifier));}else n=ulong.Parse(s,isHex?NumberStyles.AllowHexSpecifier:NumberStyles.Integer);}else if(isUnsigned){if(isNeg){if(!isHex)
n=unchecked((uint)int.Parse(s));else n=unchecked((uint)-int.Parse(s.Substring(1),NumberStyles.AllowHexSpecifier));}else n=uint.Parse(s,isHex?NumberStyles.AllowHexSpecifier
:NumberStyles.Integer);}else{if(isNeg){if(!isHex)n=int.Parse(s);else n=unchecked(-int.Parse(s.Substring(1),NumberStyles.AllowHexSpecifier));}else n=int.Parse(s,
isHex?NumberStyles.AllowHexSpecifier:NumberStyles.Integer);}pc.Advance();return new CodePrimitiveExpression(n).SetLoc(l,c,p);}
#endregion
#region String/Char escapes
static string _ParseEscapeChar(IEnumerator<char>e,_PC pc){if(e.MoveNext()){switch(e.Current){case'r':e.MoveNext();return"\r";case'n':e.MoveNext();return
"\n";case't':e.MoveNext();return"\t";case'a':e.MoveNext();return"\a";case'b':e.MoveNext();return"\b";case'f':e.MoveNext();return"\f";case'v':e.MoveNext();
return"\v";case'0':e.MoveNext();return"\0";case'\\':e.MoveNext();return"\\";case'\'':e.MoveNext();return"\'";case'\"':e.MoveNext();return"\"";case'u':
var acc=0L;if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))
break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())
break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);e.MoveNext();return unchecked((char)acc).ToString();case'x':acc=0;if(!e.MoveNext())
break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(e.MoveNext()&&_IsHexChar(e.Current)){acc<<=4;acc|=_FromHexChar(e.Current);
if(e.MoveNext()&&_IsHexChar(e.Current)){acc<<=4;acc|=_FromHexChar(e.Current);if(e.MoveNext()&&_IsHexChar(e.Current)){acc<<=4;acc|=_FromHexChar(e.Current);
e.MoveNext();}}}return unchecked((char)acc).ToString();case'U':acc=0;if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);
if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc
<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if
(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);
if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc<<=4;acc|=_FromHexChar(e.Current);if(!e.MoveNext())break;if(!_IsHexChar(e.Current))break;acc
<<=4;acc|=_FromHexChar(e.Current);e.MoveNext();return char.ConvertFromUtf32(unchecked((int)acc));default:throw new NotSupportedException(string.Format("Unsupported escape sequence \\{0}",
e.Current));}}pc.Error("Unterminated escape sequence");return null;}static bool _IsHexChar(char hex){return(':'>hex&&'/'<hex)||('G'>hex&&'@'<hex)||('g'
>hex&&'`'<hex);}static byte _FromHexChar(char hex){if(':'>hex&&'/'<hex)return(byte)(hex-'0');if('G'>hex&&'@'<hex)return(byte)(hex-'7'); if('g'>hex&&'`'
<hex)return(byte)(hex-'W'); throw new ArgumentException("The value was not hex.","hex");}
#endregion
internal static HashSet<string>Keywords=_BuildKeywords();static HashSet<string>_BuildKeywords(){var result=new HashSet<string>();string[]sa="abstract|as|ascending|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|equals|explicit|extern|event|false|finally|fixed|float|for|foreach|get|global|goto|if|implicit|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield".Split(new
 char[]{'|'});for(var i=0;i<sa.Length;++i)result.Add(sa[i]);return result;}}}namespace Slang{using ST=SlangTokenizer;partial class SlangParser{public static
 CodeStatement ParseStatement(string text,bool includeComments=false){var tokenizer=new SlangTokenizer(text);return ParseStatement(tokenizer,includeComments);
}public static CodeStatement ReadStatementFrom(Stream stream,bool includeComments=false){var tokenizer=new SlangTokenizer(stream);return ParseStatement(tokenizer,includeComments);
}public static CodeStatement ParseStatement(string text,int line,int column,long position,bool includeComments=false){var tokenizer=new SlangTokenizer(text);
var pc=new _PC(tokenizer);pc.SetLocation(line,column,position);return _ParseStatement(pc,includeComments);}public static CodeStatement ReadStatementFrom(Stream
 stream,int line,int column,long position,bool includeComments=false){var tokenizer=new SlangTokenizer(stream);var pc=new _PC(tokenizer);pc.SetLocation(line,
column,position);return _ParseStatement(pc,includeComments);}public static CodeStatementCollection ParseStatements(string text,bool includeComments=false)
{var tokenizer=new SlangTokenizer(text);return ParseStatements(tokenizer,includeComments);}public static CodeStatementCollection ReadStatementsFrom(Stream
 stream,bool includeComments=false){var tokenizer=new SlangTokenizer(stream);return ParseStatements(tokenizer,includeComments);}public static CodeStatementCollection
 ParseStatements(string text,int line,int column,long position,bool includeComments=false){var tokenizer=new SlangTokenizer(text);var pc=new _PC(tokenizer);
pc.SetLocation(line,column,position);return _ParseStatements(pc,includeComments);}public static CodeStatementCollection ReadStatementsFrom(Stream stream,
int line,int column,long position,bool includeComments=false){var tokenizer=new SlangTokenizer(stream);var pc=new _PC(tokenizer);pc.SetLocation(line,column,
position);return _ParseStatements(pc,includeComments);}internal static CodeStatement ParseStatement(IEnumerable<Token>tokenizer,bool includeComments=false)
{var pc=new _PC(tokenizer);pc.Advance(false);return _ParseStatement(pc,includeComments);}internal static CodeStatementCollection ParseStatements(IEnumerable<Token>
tokenizer,bool includeComments=false){var pc=new _PC(tokenizer);pc.Advance(false);var result=new CodeStatementCollection();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)
result.Add(_ParseStatement(pc,includeComments));return result;}static CodeStatement _ParseVariableDeclarationStatement(_PC pc){var l=pc.Line;var c=pc.Column;
var p=pc.Position;CodeTypeReference ctr=null;if(ST.varType!=pc.SymbolId)ctr=_ParseType(pc);else pc.Advance();var id=_ParseIdentifier(pc);CodeExpression
 init=null;if(ST.eq==pc.SymbolId){pc.Advance();init=_ParseExpression(pc);}else if(null==ctr)pc.Error("Variable declaration using var must have an initializer",
l,c,p);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in variable declaration statement");pc.Advance();return new CodeVariableDeclarationStatement(ctr,
id,init).Mark(l,c,p,null==ctr);}static CodeStatement _ParseStatement(_PC pc,bool includeComments=false){
#region Preamble
CodeLinePragma lp=null;var startDirs=new CodeDirectiveCollection();while(ST.directive==pc.SymbolId){var d=_ParseDirective(pc);if(null!=d){var clp=d as
 CodeLinePragma;if(null!=clp)lp=clp;else startDirs.Add(d as CodeDirective);}while(!includeComments&&ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId)
pc.Advance(false);}CodeStatement stmt=null;if(includeComments&&(ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId)){stmt=_ParseCommentStatement(pc);
stmt.StartDirectives.AddRange(startDirs);if(null!=lp)stmt.LinePragma=lp;}else while(ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId)pc.Advance(false);
#endregion Preamble
var l=pc.Line;var c=pc.Column;var p=pc.Position; if(null==stmt){_PC pc2=null;switch(pc.SymbolId){case ST.semi: pc.Advance();stmt=new CodeSnippetStatement().SetLoc(l,
c,p);break;case ST.gotoKeyword:pc.Advance();if(ST.identifier!=pc.SymbolId)pc.Error("Expecting label identifier in goto statement");stmt=new CodeGotoStatement(pc.Value).SetLoc(l,
c,p);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in goto statement");pc.Advance();break;case ST.returnKeyword:pc.Advance();var expr=_ParseExpression(pc);
stmt=new CodeMethodReturnStatement(expr).Mark(l,c,p);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in return statement");pc.Advance();break;case ST.throwKeyword:
pc.Advance();expr=_ParseExpression(pc);stmt=new CodeThrowExceptionStatement(expr).Mark(l,c,p);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in throw statement");
pc.Advance();break;case ST.ifKeyword:stmt=_ParseIfStatement(pc);break;case ST.whileKeyword:stmt=_ParseWhileStatement(pc);break;case ST.forKeyword:stmt
=_ParseForStatement(pc);break;case ST.tryKeyword:stmt=_ParseTryCatchFinallyStatement(pc);break;case ST.varType:stmt=_ParseVariableDeclarationStatement(pc);
break;default: if(ST.identifier==pc.SymbolId){pc2=pc.GetLookAhead(true);pc2.Advance();if(ST.colon==pc2.SymbolId){var lbl=pc2.Value;pc.Advance();stmt=new
 CodeLabeledStatement(lbl,new CodeSnippetStatement().SetLoc(l,c,p)).SetLoc(l,c,p);pc2=null;break;}}pc2=null;pc2=pc.GetLookAhead(true);pc2.ResetAdvanceCount();
var advc=0;try{ stmt=_ParseVariableDeclarationStatement(pc2);advc=pc2.AdvanceCount;while(advc>0){pc.Advance(false);--advc;}break;}catch(SlangSyntaxException
 sx){try{pc.ResetAdvanceCount();expr=_ParseExpression(pc);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in expression statement");pc.Advance();var bo=
expr as CodeBinaryOperatorExpression;if(null!=bo&&CodeBinaryOperatorType.Assign==bo.Operator){var ur=bo.UserData.Contains("slang:unresolved");stmt=new
 CodeAssignStatement(bo.Left,bo.Right).Mark(l,c,p,ur);}else stmt=new CodeExpressionStatement(expr).Mark(l,c,p);break;}catch(SlangSyntaxException sx2){
if(pc.AdvanceCount>advc)throw sx2;throw sx;}}}}
#region Post
stmt.StartDirectives.AddRange(startDirs);if(null!=lp)stmt.LinePragma=lp;while(!includeComments&&ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId)
pc.Advance(false);while(ST.directive==pc.SymbolId&&pc.Value.StartsWith("#end",StringComparison.InvariantCulture)){stmt.EndDirectives.Add(_ParseDirective(pc)
as CodeDirective);while(!includeComments&&ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId)pc.Advance(false);}
#endregion Post
return stmt;}static CodeStatementCollection _ParseStatements(_PC pc,bool includeComments=false){var result=new CodeStatementCollection();while(!pc.IsEnded
&&ST.rbrace!=pc.SymbolId)result.Add(_ParseStatement(pc,includeComments));return result;}static CodeStatementCollection _ParseStatementOrBlock(_PC pc){
var l=pc.Line;var c=pc.Column;var p=pc.Position;CodeStatementCollection result;if(ST.lbrace==pc.SymbolId){pc.Advance();result=_ParseStatements(pc,true);
if(ST.rbrace!=pc.SymbolId)pc.Error("Unterminated statement block",l,c,p);pc.Advance();return result;}result=new CodeStatementCollection();result.Add(_ParseStatement(pc,
false));return result;}static CodeStatement _ParseTryCatchFinallyStatement(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.tryKeyword!=pc.SymbolId)
pc.Error("Expecting try");pc.Advance();var result=new CodeTryCatchFinallyStatement().Mark(l,c,p);if(ST.lbrace!=pc.SymbolId)pc.Error("Expecting { in try statement");
pc.Advance();result.TryStatements.AddRange(_ParseStatements(pc,true));if(ST.rbrace!=pc.SymbolId)pc.Error("Expecting } in try statement");pc.Advance();
while(ST.catchKeyword==pc.SymbolId)result.CatchClauses.Add(_ParseCatchClause(pc));if(0==result.CatchClauses.Count&&ST.finallyKeyword!=pc.SymbolId)pc.Error("Expecting catch or finally");
if(ST.finallyKeyword==pc.SymbolId){pc.Advance();if(ST.lbrace!=pc.SymbolId)pc.Error("Expecting { in finally statement");pc.Advance();result.FinallyStatements.AddRange(_ParseStatements(pc,
true));if(ST.rbrace!=pc.SymbolId)pc.Error("Expecting } in finally statement");pc.Advance();}return result;}static CodeCatchClause _ParseCatchClause(_PC
 pc){if(ST.catchKeyword!=pc.SymbolId)pc.Error("Expecting catch");pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in catch clause");pc.Advance();
var result=new CodeCatchClause();result.CatchExceptionType=_ParseType(pc);if(ST.rparen!=pc.SymbolId)result.LocalName=_ParseIdentifier(pc);if(ST.rparen
!=pc.SymbolId)pc.Error("Expecting ) in catch clause");pc.Advance();if(ST.lbrace!=pc.SymbolId)pc.Error("Expecting { in catch clause");pc.Advance();result.Statements.AddRange(_ParseStatements(pc,
true));if(ST.rbrace!=pc.SymbolId)pc.Error("Expecting } in catch clause");pc.Advance();return result;}static CodeStatement _ParseIfStatement(_PC pc){var
 l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.ifKeyword!=pc.SymbolId)pc.Error("Expecting if");pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in if statement");
pc.Advance();var test=_ParseExpression(pc);if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in if statement");pc.Advance();var result=new CodeConditionStatement(test).Mark(l,c,p);
result.TrueStatements.AddRange(_ParseStatementOrBlock(pc));if(ST.elseKeyword==pc.SymbolId){pc.Advance();result.FalseStatements.AddRange(_ParseStatementOrBlock(pc));
}return result;}static CodeStatement _ParseWhileStatement(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.whileKeyword!=pc.SymbolId)pc.Error("Expecting while");
pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in while statement");pc.Advance();var test=_ParseExpression(pc);if(ST.rparen!=pc.SymbolId)
pc.Error("Expecting ) in while statement");pc.Advance();var result=new CodeIterationStatement(new CodeSnippetStatement().SetLoc(l,c,p),test,new CodeSnippetStatement().SetLoc(l,c,p)).Mark(l,
c,p);result.Statements.AddRange(_ParseStatementOrBlock(pc));return result;}static CodeStatement _ParseForStatement(_PC pc){var l=pc.Line;var c=pc.Column;
var p=pc.Position;if(ST.forKeyword!=pc.SymbolId)pc.Error("Expecting for");pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in for statement");
pc.Advance();var init=_ParseStatement(pc,false);var test=_ParseExpression(pc);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in for statement");pc.Advance();
CodeStatement inc=null;CodeExpression ince=null;if(ST.rparen!=pc.SymbolId)ince=_ParseExpression(pc);if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in for statement");
if(null==ince)inc=new CodeSnippetStatement().SetLoc(pc);else{var bo=ince as CodeBinaryOperatorExpression;if(null!=bo&&CodeBinaryOperatorType.Assign==bo.Operator)
{ var ur=bo.UserData.Contains("slang:unresolved");inc=new CodeAssignStatement(bo.Left,bo.Right).Mark(ince,ur);}else inc=new CodeExpressionStatement(ince).Mark(ince);
}pc.Advance();var result=new CodeIterationStatement(init,test,inc).Mark(l,c,p);result.Statements.AddRange(_ParseStatementOrBlock(pc));return result;}static
 CodeCommentStatement _ParseCommentStatement(_PC pc,bool docComments=false){var s=pc.Value;switch(pc.SymbolId){case ST.lineComment:pc.Advance(false);if(docComments
&&s.StartsWith("///",StringComparison.InvariantCulture))return new CodeCommentStatement(s.Substring(3).TrimEnd('\r'),true);return new CodeCommentStatement(s.Substring(2).TrimEnd('\r'));
case ST.blockComment:pc.Advance(false);return new CodeCommentStatement(s.Substring(2,s.Length-4));}pc.Error("Expecting line comment or block comment");
return null;} static object _ParseDirective(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.directive!=pc.SymbolId)pc.Error("Expecting directive");
var kvp=_ParseDirectiveKvp(pc);pc.Advance(false);if(0==string.Compare("#region",kvp.Key,StringComparison.InvariantCulture))return new CodeRegionDirective(CodeRegionMode.Start,
kvp.Value);if(0==string.Compare("#endregion",kvp.Key,StringComparison.InvariantCulture))return new CodeRegionDirective(CodeRegionMode.End,kvp.Value);if
(0==string.Compare("#line",kvp.Key,StringComparison.InvariantCulture)){var s=kvp.Value.Trim();if(0==string.Compare("hidden",s,StringComparison.InvariantCulture)
||0==string.Compare("default",s,StringComparison.InvariantCulture))return null;var i=s.IndexOfAny(new char[]{' ','\t'});if(0>i)pc.Error("Malformed line pragma directive",
l,c,p);var lineNo=0;if(!int.TryParse(s.Substring(0,i),out lineNo))pc.Error("Malformed line pragma directive - expecting line number",l,c,p);var file=s.Substring(i
+1).Trim();file=_ParseDirectiveQuotedPart(file,l,c,p);return new CodeLinePragma(file,lineNo);}else if(0==string.Compare("#pragma",kvp.Key,StringComparison.InvariantCulture))
{var sa=kvp.Value.Split(' ','\t');var sl=new List<string>(sa.Length);for(var i=0;i<sa.Length;i++){if(!string.IsNullOrWhiteSpace(sa[i]))sl.Add(sa[i]);}
if(0==sl.Count)pc.Error("Malformed pragma directive",l,c,p);if(0!=string.Compare("checksum",sl[0],StringComparison.InvariantCulture))pc.Error("Unsupported directive "+kvp.Key,
l,c,p);if(4!=sl.Count)pc.Error("Malformed checksum pragma directive",l,c,p);var fn=_ParseDirectiveQuotedPart(sl[1],l,c,p);var guid=_ParseDirectiveQuotedPart(sl[2],
l,c,p);var bytes=_ParseDirectiveQuotedPart(sl[3],l,c,p);Guid g;if(!Guid.TryParse(guid,out g))pc.Error("Invalid guid in checksum pragma directive");if(0
!=(bytes.Length%2))pc.Error("Invalid bytes region in checksum pragma directive");var ba=new byte[bytes.Length/2];for(var i=0;i<ba.Length;i++){var ch1=
bytes[i*2];if(!_IsHexChar(ch1))pc.Error("Invalid bytes region in checksum pragma directive");var ch2=bytes[i*2+1];if(!_IsHexChar(ch2))pc.Error("Invalid bytes region in checksum pragma directive");
ba[i]=unchecked((byte)(_FromHexChar(ch1)*0x10+_FromHexChar(ch2)));}return new CodeChecksumPragma(fn,g,ba);}pc.Error("Unsupported directive "+kvp.Key,l,c,p);
return null;}static string _ParseDirectiveQuotedPart(string part,int l,int c,long p){var sb=new StringBuilder();var e=part.GetEnumerator();if(!e.MoveNext()
||'\"'!=e.Current)throw new SlangSyntaxException("Expecting \" in directive part",l,c,p);if(e.MoveNext()){while(true){if('\"'==e.Current){if(!e.MoveNext()
||'\"'!=e.Current){return sb.ToString();}sb.Append('\"');if(!e.MoveNext())break;}else{sb.Append(e.Current);if(!e.MoveNext())break;}}}throw new SlangSyntaxException("Unterminated quoted string in directive part",
l,c,p);}static KeyValuePair<string,string>_ParseDirectiveKvp(_PC pc){var s=pc.Value;var i=s.IndexOfAny(new char[]{' ','\t'});if(0>i)return new KeyValuePair<string,
string>(s,null);return new KeyValuePair<string,string>(s.Substring(0,i),s.Substring(i+1).Trim());}}}namespace Slang{using ST=SlangTokenizer;partial class
 SlangParser{public static CodeTypeDeclaration ParseTypeDecl(string text){var tokenizer=new SlangTokenizer(text);return ParseTypeDecl(tokenizer);}public
 static CodeTypeDeclaration ReadTypeDeclFrom(Stream stream){var tokenizer=new SlangTokenizer(stream);return ParseTypeDecl(tokenizer);}public static CodeTypeDeclaration
 ParseTypeDecl(string text,int line,int column,long position){var tokenizer=new SlangTokenizer(text);var pc=new _PC(tokenizer);pc.SetLocation(line,column,
position);return _ParseTypeDecl(pc,false,line,column,position,null);}public static CodeTypeDeclaration ReadTypeDeclFrom(Stream stream,int line,int column,
long position){var tokenizer=new SlangTokenizer(stream);var pc=new _PC(tokenizer);pc.SetLocation(line,column,position);return _ParseTypeDecl(pc,false,line,column,position,null)
;}public static CodeTypeMember ParseMember(string text){var tokenizer=new SlangTokenizer(text);return ParseMember(tokenizer);}public static CodeTypeMember
 ReadMemberFrom(Stream stream){var tokenizer=new SlangTokenizer(stream);return ParseMember(tokenizer);}public static CodeTypeMember ParseMember(string
 text,int line,int column,long position){var tokenizer=new SlangTokenizer(text);var pc=new _PC(tokenizer);pc.SetLocation(line,column,position);return _ParseMember(pc);
}public static CodeTypeMember ReadMemberFrom(Stream stream,int line,int column,long position){var tokenizer=new SlangTokenizer(stream);var pc=new _PC(tokenizer);
pc.SetLocation(line,column,position);return _ParseMember(pc);}public static CodeTypeMemberCollection ParseMembers(string text){var tokenizer=new SlangTokenizer(text);
return ParseMembers(tokenizer);}public static CodeTypeMemberCollection ReadMembersFrom(Stream stream){var tokenizer=new SlangTokenizer(stream);return ParseMembers(tokenizer);
}public static CodeTypeMemberCollection ParseMembers(string text,int line,int column,long position){var tokenizer=new SlangTokenizer(text);var pc=new _PC(tokenizer);
pc.SetLocation(line,column,position);return _ParseMembers(pc);}public static CodeTypeMemberCollection ReadMembersFrom(Stream stream,int line,int column,
long position){var tokenizer=new SlangTokenizer(stream);var pc=new _PC(tokenizer);pc.SetLocation(line,column,position);return _ParseMembers(pc);}internal
 static CodeTypeDeclaration ParseTypeDecl(IEnumerable<Token>tokenizer){var pc=new _PC(tokenizer);pc.Advance(false);return _ParseTypeDecl(pc,false,1,1,0,null);
}static CodeTypeDeclaration _ParseTypeDecl(_PC pc,bool isNested,int line,int column,long position,HashSet<string>seen){var result=new CodeTypeDeclaration().Mark(line,
column,position);if(!isNested){var cc=new CodeCommentStatementCollection();var l=line;var c=column;var p=position;CodeLinePragma lp;var startDirs=new CodeDirectiveCollection();
var comments=new CodeCommentStatementCollection();while(ST.directive==pc.SymbolId||ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId){switch(pc.SymbolId)
{case ST.directive:var d=_ParseDirective(pc);var llp=d as CodeLinePragma;if(null!=llp)lp=llp;else if(null!=d)startDirs.Add(d as CodeDirective);break;case
 ST.blockComment:comments.Add(_ParseCommentStatement(pc));break;case ST.lineComment:comments.Add(_ParseCommentStatement(pc,true));break;}}IDictionary<string,
CodeAttributeDeclarationCollection>customAttributes=null;if(ST.lbracket==pc.SymbolId)customAttributes=_ParseAttributeGroups(pc);seen=new HashSet<string>(StringComparer.InvariantCulture);
line=pc.Line;column=pc.Column;position=pc.Position;while(ST.publicKeyword==pc.SymbolId||ST.internalKeyword==pc.SymbolId||ST.abstractKeyword==pc.SymbolId)
{if(!seen.Add(pc.Value))pc.Error(string.Format("Duplicate attribute {0} specified in member",pc.Value));pc.Advance();}result.Comments.AddRange(comments);
_AddCustomAttributes(customAttributes,"",result.CustomAttributes);_CheckCustomAttributes(customAttributes,pc);result.TypeAttributes=_BuildTypeAttributes(seen,
line,column,position);line=pc.Line;column=pc.Column;position=pc.Position;}else{result.TypeAttributes=_BuildMemberTypeAttributes(seen);}if(ST.partialKeyword==pc.SymbolId)
{result.IsPartial=true;pc.Advance();}switch(pc.SymbolId){case ST.classKeyword:result.IsClass=true;break;case ST.structKeyword:result.IsStruct=true;break;
case ST.interfaceKeyword:result.IsInterface=true;break;case ST.enumKeyword:result.IsEnum=true;break;default:pc.Error("Expecting class, interface, struct or enum");
break;}pc.Advance();result.Name=_ParseIdentifier(pc);if(result.IsEnum&&ST.lt==pc.SymbolId)pc.Error("Enums cannot have generic parameters");result.TypeParameters.AddRange(_ParseTypeParams(pc));
 result.BaseTypes.AddRange(_ParseBaseTypes(pc,result.IsEnum));while(ST.whereKeyword==pc.SymbolId){if(result.IsEnum)pc.Error("Enums cannot have generic type constraints");
pc.Advance();var tpn=_ParseIdentifier(pc);CodeTypeParameter tp=null;for(int ic=result.TypeParameters.Count,i=0;i<ic;++i){var tpt=result.TypeParameters[i];
if(0==string.Compare(tpn,tpt.Name,StringComparison.InvariantCulture)){tp=tpt;break;}}if(null==tp)pc.Error("Reference to undefined type parameter "+tpn);
if(ST.colon!=pc.SymbolId)pc.Error("Expecting : in type constraint");pc.Advance();while(!pc.IsEnded&&ST.whereKeyword!=pc.SymbolId&&ST.lbrace!=pc.SymbolId)
{if(ST.newKeyword==pc.SymbolId){pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in constructor type constraint");pc.Advance();if(ST.rparen
!=pc.SymbolId)pc.Error("Expecting ) in constructor type constraint");pc.Advance();tp.HasConstructorConstraint=true;}else tp.Constraints.Add(_ParseType(pc));
if(ST.whereKeyword==pc.SymbolId||ST.lbrace==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in type constraint list");
pc.Advance();if(ST.whereKeyword==pc.SymbolId||ST.lbrace==pc.SymbolId)pc.Error("Expecting type constraint in type constraint list",l2,c2,p2);}}if(ST.lbrace
!=pc.SymbolId)pc.Error("Expecting { in type declaration");pc.Advance(false);if(!result.IsEnum){while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId){result.Members.Add(_ParseMember(pc,
result.IsInterface));}}else{while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId){result.Members.Add(_ParseEnumMember(pc));}}if(ST.rbrace!=pc.SymbolId)pc.Error("Unterminated type declaration",
line,column,position);pc.Advance(false);while(ST.directive==pc.SymbolId&&pc.Value.StartsWith("#end",StringComparison.InvariantCulture))result.EndDirectives.Add(_ParseDirective(pc)
as CodeDirective);return result;}static CodeTypeParameterCollection _ParseTypeParams(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var result
=new CodeTypeParameterCollection();if(ST.lt==pc.SymbolId){pc.Advance();while(!pc.IsEnded&&ST.gt!=pc.SymbolId){result.Add(_ParseTypeParam(pc));if(ST.gt
==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in type parameter list");pc.Advance();
if(ST.gt==pc.SymbolId)pc.Error("Expecting type parameter in type parameter list",l2,c2,p2);}if(ST.gt!=pc.SymbolId)pc.Error("Unterminated type parameter list",
l,c,p);pc.Advance();}return result;}static CodeTypeReferenceCollection _ParseBaseTypes(_PC pc,bool isEnum=false){var l=pc.Line;var c=pc.Column;var p=pc.Position;
var result=new CodeTypeReferenceCollection();if(ST.colon==pc.SymbolId){pc.Advance();while(!pc.IsEnded&&ST.lbrace!=pc.SymbolId&&ST.whereKeyword!=pc.SymbolId)
{if(isEnum&&0<result.Count)pc.Error("Enums can only inherit from one base type");result.Add(_ParseType(pc));if(ST.lbrace==pc.SymbolId||ST.whereKeyword==pc.SymbolId)
break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in base type list");pc.Advance();if(ST.lbrace==
pc.SymbolId||ST.whereKeyword==pc.SymbolId)pc.Error("Expecting type in base type list",l2,c2,p2);}if(ST.lbrace!=pc.SymbolId&&ST.whereKeyword!=pc.SymbolId)
pc.Error("Unterminated base type list",l,c,p);}return result;}static CodeTypeParameter _ParseTypeParam(_PC pc){IDictionary<string,CodeAttributeDeclarationCollection>
ca=null;if(ST.lbracket==pc.SymbolId)ca=_ParseAttributeGroups(pc);var result=new CodeTypeParameter(_ParseIdentifier(pc));_AddCustomAttributes(ca,"",result.CustomAttributes);
_CheckCustomAttributes(ca,pc);return result;}internal static CodeTypeMember ParseMember(IEnumerable<Token>tokenizer){var pc=new _PC(tokenizer);pc.Advance(false);
return _ParseMember(pc);}internal static CodeTypeMemberCollection ParseMembers(IEnumerable<Token>tokenizer){var pc=new _PC(tokenizer);pc.Advance(false);
return _ParseMembers(pc);}static CodeTypeMemberCollection _ParseMembers(_PC pc){var result=new CodeTypeMemberCollection();while(!pc.IsEnded&&ST.rbrace
!=pc.SymbolId)result.Add(_ParseMember(pc));return result;}static CodeTypeMember _ParseMember(_PC pc,bool isInterfaceMember=false){var line=pc.Line;var
 column=pc.Column;var position=pc.Position;var l=line;var c=column;var p=position;CodeTypeMember result=null;CodeLinePragma lp=null;var startDirs=new CodeDirectiveCollection();
var comments=new CodeCommentStatementCollection();while(ST.directive==pc.SymbolId||ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId){switch(pc.SymbolId)
{case ST.directive:var d=_ParseDirective(pc);var llp=d as CodeLinePragma;if(null!=llp)lp=llp;else if(null!=d)startDirs.Add(d as CodeDirective);break;case
 ST.blockComment:comments.Add(_ParseCommentStatement(pc));break;case ST.lineComment:comments.Add(_ParseCommentStatement(pc,true));break;}}IDictionary<string,CodeAttributeDeclarationCollection>
customAttributes=null;if(ST.lbracket==pc.SymbolId)customAttributes=_ParseAttributeGroups(pc);var seen=new HashSet<string>(StringComparer.InvariantCulture);
line=pc.Line;column=pc.Column;position=pc.Position;while(ST.privateKeyword==pc.SymbolId||ST.publicKeyword==pc.SymbolId||ST.internalKeyword==pc.SymbolId
||ST.protectedKeyword==pc.SymbolId||ST.staticKeyword==pc.SymbolId||ST.abstractKeyword==pc.SymbolId||ST.overrideKeyword==pc.SymbolId||ST.virtualKeyword
==pc.SymbolId||ST.newKeyword==pc.SymbolId||ST.constKeyword==pc.SymbolId){if(!seen.Add(pc.Value))pc.Error(string.Format("Duplicate attribute {0} specified in member",
pc.Value));pc.Advance();}if(isInterfaceMember&&(1<seen.Count||(1==seen.Count&&!seen.Contains("new"))))pc.Error("Interface members may not have modifiers except for new");
if(seen.Contains("abstract")){if(seen.Contains("private")||(!seen.Contains("public")&&!seen.Contains("protected")&&!seen.Contains("internal")))pc.Error("Abstract members cannot be private");
if(seen.Contains("static"))pc.Error("Abstract members cannot be static");}if(seen.Contains("private")){if(seen.Contains("public")||seen.Contains("protected")
||seen.Contains("internal"))pc.Error("Conflicting access modifiers specified on member");}if(seen.Contains("public")){if(seen.Contains("protected")||seen.Contains("internal"))
pc.Error("Conflicting access modifiers specified on member");}if(ST.eventKeyword==pc.SymbolId){ if(seen.Contains("const"))pc.Error("Events cannot be const",
line,column,position);var eve=new CodeMemberEvent().Mark(line,column,position);result=eve;result.Comments.AddRange(comments);if(null!=lp)result.LinePragma
=lp;_AddCustomAttributes(customAttributes,"",result.CustomAttributes);_CheckCustomAttributes(customAttributes,pc);result.StartDirectives.AddRange(startDirs);
eve.Attributes=_BuildMemberAttributes(seen);pc.Advance();eve.Type=_ParseType(pc);eve.Name=_ParseIdentifier(pc);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in event declaration");
pc.Advance(false);while(ST.directive==pc.SymbolId&&pc.Value.StartsWith("#end",StringComparison.InvariantCulture))result.EndDirectives.Add(_ParseDirective(pc)
as CodeDirective);return result;}if(ST.classKeyword==pc.SymbolId||ST.structKeyword==pc.SymbolId||ST.enumKeyword==pc.SymbolId||ST.interfaceKeyword==pc.SymbolId
||ST.partialKeyword==pc.SymbolId){ if(seen.Contains("const"))pc.Error("Nested types cannot be const",line,column,position);if(seen.Contains("virtual"))
pc.Error("Nested types cannot be virtual",line,column,position);if(seen.Contains("override"))pc.Error("Nested types cannot override",line,column,position);
if(seen.Contains("new"))pc.Error("Nested types cannot be new",line,column,position);if(seen.Contains("static"))pc.Error("Types cannot be static in Slang",
line,column,position);if(isInterfaceMember)pc.Error("Interfaces cannot contain nested types",line,column,position);result=_ParseTypeDecl(pc,true,line,
column,position,seen);result.Comments.AddRange(comments);if(null!=lp)result.LinePragma=lp;_AddCustomAttributes(customAttributes,"",result.CustomAttributes);
_CheckCustomAttributes(customAttributes,pc);result.StartDirectives.AddRange(startDirs);return result;} var pc2=pc.GetLookAhead(true);var isCtor=false;
if(ST.verbatimIdentifier==pc.SymbolId||ST.identifier==pc.SymbolId){var pc3=pc2.GetLookAhead(true);_ParseIdentifier(pc3);if(ST.lparen==pc3.SymbolId){isCtor
=true;}pc3=null;}if(!isCtor){if(ST.voidType!=pc2.SymbolId){_ParseType(pc2);}if(ST.lparen!=pc2.SymbolId)pc2.Advance();}bool hasAssign=false;if(!isCtor&&
(ST.semi==pc2.SymbolId||(hasAssign=(ST.eq==pc2.SymbolId)))){ if(seen.Contains("abstract"))pc.Error("Fields cannot be abstract",line,column,position);if
(seen.Contains("virtual"))pc.Error("Fields cannot be virtual",line,column,position);if(seen.Contains("override"))pc.Error("Fields cannot override",line,
column,position);if(isInterfaceMember)pc.Error("Interfaces cannot contain fields",line,column,position);var fld=new CodeMemberField().Mark(line,column,position);
result=fld;result.Comments.AddRange(comments);if(null!=lp)result.LinePragma=lp;_AddCustomAttributes(customAttributes,"",result.CustomAttributes);_CheckCustomAttributes(customAttributes,
pc);result.StartDirectives.AddRange(startDirs);result.Attributes=_BuildMemberAttributes(seen);fld.Type=_ParseType(pc);fld.Name=_ParseIdentifier(pc);if
(hasAssign){pc.Advance();fld.InitExpression=_ParseExpression(pc);if(ST.semi!=pc.SymbolId)pc.Error("Expecting ; in field definition");}else if(seen.Contains("const"))
pc.Error("Const fields must have initializers",line,column,position);pc.Advance(false);while(ST.directive==pc.SymbolId&&pc.Value.StartsWith("#end",StringComparison.InvariantCulture))
result.EndDirectives.Add(_ParseDirective(pc)as CodeDirective);return result;}if(isCtor){ if(seen.Contains("const"))pc.Error("Constructors cannot be const",
line,column,position);if(isInterfaceMember)pc.Error("Interfaces cannot have constructors. Are you missing a return type?",line,column,position);var ctor
=seen.Contains("static")?(CodeMemberMethod)new CodeTypeConstructor().Mark(line,column,position):new CodeConstructor().Mark(line,column,position);var cctor
=ctor as CodeConstructor;result=ctor;result.Comments.AddRange(comments);if(null!=lp)result.LinePragma=lp;_AddCustomAttributes(customAttributes,"",result.CustomAttributes);
_CheckCustomAttributes(customAttributes,pc);result.StartDirectives.AddRange(startDirs);result.Attributes=_BuildMemberAttributes(seen);result.Name=_ParseIdentifier(pc);
 pc.Advance(); if(ST.rparen!=pc.SymbolId&&seen.Contains("static"))pc.Error("Static constructors cannot have arguments");ctor.Parameters.AddRange(_ParseParamList(pc));
if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in constructor definition");pc.Advance();if(ST.colon==pc.SymbolId){pc.Advance();if(seen.Contains("static"))
{if(ST.baseRef==pc.SymbolId)pc.Error("Static constructors cannot have \"base\" constructor chains");else if(ST.thisRef==pc.SymbolId)pc.Error("Static constructors cannot have \"this\" constructor chains");
}switch(pc.SymbolId){case ST.baseRef:pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in \"base\" constructor chain");pc.Advance();cctor.BaseConstructorArgs.AddRange(_ParseArgList(pc));
break;case ST.thisRef:pc.Advance();if(ST.lparen!=pc.SymbolId)pc.Error("Expecting ( in \"this\" constructor chain");pc.Advance();cctor.ChainedConstructorArgs.AddRange(_ParseArgList(pc));
break;default:pc.Error("Expecting this or base");break;}if(ST.rparen!=pc.SymbolId)pc.Error("Expecting ) in constructor chain");pc.Advance();}l=pc.Line;
c=pc.Column;p=pc.Position;if(ST.lbrace!=pc.SymbolId)pc.Error("Expecting body in constructor");pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)ctor.Statements.Add(_ParseStatement(pc,true));
if(ST.rbrace!=pc.SymbolId)pc.Error("Unterminated constructor body",l,c,p);pc.Advance(false);while(ST.directive==pc.SymbolId&&pc.Value.StartsWith("#end",
StringComparison.InvariantCulture))result.EndDirectives.Add(_ParseDirective(pc)as CodeDirective);return result;}else{CodeTypeReference type=null; var isVoid
=false;if(ST.voidType==pc.SymbolId){ pc.Advance();isVoid=true;}else type=_ParseType(pc);var piType=_ParsePrivateImplementationType(pc);var isThis=false;
string name="Item";if(ST.thisRef==pc.Current.SymbolId){isThis=true;pc.Advance();}else name=_ParseIdentifier(pc);if(!isThis&&ST.lparen==pc.SymbolId){ if
(seen.Contains("const"))pc.Error("Methods cannot be const",line,column,position);pc.Advance(); var meth=new CodeMemberMethod().Mark(line,column,position,seen.Contains("public"));
result=meth;result.Comments.AddRange(comments);if(null!=lp)result.LinePragma=lp;_AddCustomAttributes(customAttributes,"",result.CustomAttributes);_AddCustomAttributes(customAttributes,
"return",meth.ReturnTypeCustomAttributes);_CheckCustomAttributes(customAttributes,pc);result.StartDirectives.AddRange(startDirs);result.Attributes=_BuildMemberAttributes(seen);
meth.PrivateImplementationType=piType;meth.Parameters.AddRange(_ParseMethodParamList(pc));meth.ReturnType=type;meth.Name=name;if(ST.rparen!=pc.SymbolId)
pc.Error("Expecting ) in method definition");pc.Advance();if(ST.semi==pc.SymbolId){if(!isInterfaceMember&&!seen.Contains("abstract"))pc.Error("Non-abstract methods must declare a body");
pc.Advance(false);}else{if(ST.lbrace!=pc.SymbolId)pc.Error("Expecting body in method definition");pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)
meth.Statements.Add(_ParseStatement(pc,true));if(ST.rbrace!=pc.SymbolId)pc.Error("Unterminated method body",l,c,p);pc.Advance(false);}while(ST.directive
==pc.SymbolId&&pc.Value.StartsWith("#end",StringComparison.InvariantCulture))result.EndDirectives.Add(_ParseDirective(pc)as CodeDirective);return result;
} if(isVoid)pc.Error("Properties must have a type",line,column,position);if(seen.Contains("const"))pc.Error("Properties cannot be const",line,column,position);
var prop=new CodeMemberProperty().Mark(line,column,position,seen.Contains("public"));result=prop;result.Comments.AddRange(comments);if(null!=lp)result.LinePragma
=lp;_AddCustomAttributes(customAttributes,"",result.CustomAttributes);_CheckCustomAttributes(customAttributes,pc);result.StartDirectives.AddRange(startDirs);
result.Attributes=_BuildMemberAttributes(seen);prop.PrivateImplementationType=piType;prop.Name=name;prop.Type=type;if(ST.lbracket==pc.SymbolId){if(!isThis)
pc.Error("Only \"this\" properties may have indexers.",line,column,position);pc.Advance();prop.Parameters.AddRange(_ParseParamList(pc));if(ST.rbracket
!=pc.SymbolId)pc.Error("Expecting ] in property definition");pc.Advance();}else if(isThis)pc.Error("\"this\" properties must have indexers.",line,column,position);
if(ST.lbrace!=pc.SymbolId)pc.Error("Expecting { in property definition");pc.Advance();_ParsePropertyAccessors(pc,prop,seen.Contains("abstract")||isInterfaceMember);
if(ST.rbrace!=pc.SymbolId)pc.Error("Expecting } in property definition");pc.Advance(false);while(ST.directive==pc.SymbolId&&pc.Value.StartsWith("#end",
StringComparison.InvariantCulture))result.EndDirectives.Add(_ParseDirective(pc)as CodeDirective);return result;}}static void _ParsePropertyAccessors(_PC
 pc,CodeMemberProperty prop,bool isAbstractOrInterface=false){var sawGet=false;var sawSet=false;while(ST.getKeyword==pc.SymbolId||ST.setKeyword==pc.SymbolId)
{if(ST.getKeyword==pc.SymbolId){if(sawGet)pc.Error("Only one get accessor may be specified");sawGet=true;prop.HasGet=true;pc.Advance();if(ST.semi==pc.SymbolId)
{if(!isAbstractOrInterface)pc.Error("Non abstract property gets must declare a body");pc.Advance();}else if(ST.lbrace==pc.SymbolId){if(isAbstractOrInterface)
pc.Error("Abstract and interface property gets must not declare a body");prop.GetStatements.AddRange(_ParseStatementOrBlock(pc));}else pc.Error("Unexpected token found in property get declaration");
}else if(ST.setKeyword==pc.SymbolId){if(sawSet)pc.Error("Only one set accessor may be specified");sawSet=true;prop.HasSet=true;pc.Advance();if(ST.semi
==pc.SymbolId){if(!isAbstractOrInterface)pc.Error("Non abstract property sets must declare a body");pc.Advance();}else if(ST.lbrace==pc.SymbolId){if(isAbstractOrInterface)
pc.Error("Abstract and interface property sets must not declare a body");prop.SetStatements.AddRange(_ParseStatementOrBlock(pc));}else pc.Error("Unexpected token found in property set declaration");
}else pc.Error("Expecting a get or set accessor");}}static CodeParameterDeclarationExpressionCollection _ParseParamList(_PC pc){var result=new CodeParameterDeclarationExpressionCollection();
while(!pc.IsEnded&&ST.rparen!=pc.SymbolId&&ST.rbracket!=pc.SymbolId){result.Add(new CodeParameterDeclarationExpression(_ParseType(pc),_ParseIdentifier(pc)));
if(ST.rparen==pc.SymbolId||ST.rbracket==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in parameter list");
pc.Advance();if(ST.rbracket==pc.SymbolId||ST.rparen==pc.SymbolId)pc.Error("Expecting parameter in parameter list",l2,c2,p2);}return result;}static CodeParameterDeclarationExpressionCollection
 _ParseMethodParamList(_PC pc){var result=new CodeParameterDeclarationExpressionCollection();while(ST.rparen!=pc.SymbolId&&ST.rbracket!=pc.SymbolId){var
 fd=FieldDirection.In;if(ST.refKeyword==pc.SymbolId){fd=FieldDirection.Ref;pc.Advance();}else if(ST.outKeyword==pc.SymbolId){fd=FieldDirection.Out;pc.Advance();
}var pd=new CodeParameterDeclarationExpression(_ParseType(pc),_ParseIdentifier(pc));pd.Direction=fd;result.Add(pd);if(ST.rparen==pc.SymbolId||ST.rbracket
==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in method parameter list");pc.Advance();
if(ST.rbracket==pc.SymbolId||ST.rparen==pc.SymbolId)pc.Error("Expecting parameter in method parameter list",l2,c2,p2);}return result;}static CodeTypeReference
 _ParsePrivateImplementationType(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var pc2=pc.GetLookAhead(true); var toks=new List<Token>();while
(!pc2.IsEnded&&(ST.lparen!=pc2.SymbolId)&&ST.lbrace!=pc2.SymbolId&&ST.lbracket!=pc2.SymbolId){toks.Add(pc2.Current);pc2.Advance();}if(pc2.IsEnded)pc2.Error("Unexpected end of input parsing private implementation type");
if(2<toks.Count){ toks.RemoveAt(toks.Count-1);toks.RemoveAt(toks.Count-1); var t=default(Token);t.SymbolId=ST.comma;t.Value=",";t.Line=pc2.Line;t.Column
=pc2.Column;t.Position=pc2.Position;toks.Add(t);var pc3=new _PC(toks);pc3.EnsureStarted();var type=_ParseType(pc3); var adv=0;while(adv<toks.Count){pc.Advance();
++adv;}return type;}return null;}static CodeTypeMember _ParseEnumMember(_PC pc){var line=pc.Line;var column=pc.Column;var position=pc.Position;var l=line;
var c=column;var p=position;CodeTypeMember result=null;CodeLinePragma lp=null;var startDirs=new CodeDirectiveCollection();var comments=new CodeCommentStatementCollection();
while(ST.directive==pc.SymbolId||ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId){switch(pc.SymbolId){case ST.directive:var d=_ParseDirective(pc);
var llp=d as CodeLinePragma;if(null!=llp)lp=llp;else if(null!=d)startDirs.Add(d as CodeDirective);break;case ST.blockComment:comments.Add(_ParseCommentStatement(pc));
break;case ST.lineComment:comments.Add(_ParseCommentStatement(pc,true));break;}}IDictionary<string,CodeAttributeDeclarationCollection>customAttributes
=null;if(ST.lbracket==pc.SymbolId)customAttributes=_ParseAttributeGroups(pc);line=pc.Line;column=pc.Column;position=pc.Position;var fld=new CodeMemberField().Mark(line,
column,position);result=fld;result.Comments.AddRange(comments);if(null!=lp)result.LinePragma=lp;_AddCustomAttributes(customAttributes,"",result.CustomAttributes);
_CheckCustomAttributes(customAttributes,pc);result.StartDirectives.AddRange(startDirs);fld.Type=_ParseType(pc);fld.Name=_ParseIdentifier(pc);if(ST.eq==pc.SymbolId)
{pc.Advance();fld.InitExpression=_ParseExpression(pc);}if(ST.comma==pc.SymbolId)pc.Advance(false);else if(ST.rbrace!=pc.SymbolId)pc.Error("Expecting , or } in enum declaration");
while(ST.directive==pc.SymbolId&&pc.Value.StartsWith("#end",StringComparison.InvariantCulture))result.EndDirectives.Add(_ParseDirective(pc)as CodeDirective);
return result;}[System.Diagnostics.DebuggerNonUserCode()]static void _CheckCustomAttributes(IDictionary<string,CodeAttributeDeclarationCollection>attrs,_PC
 pc){if(null!=attrs&&0<attrs.Count){foreach(var kvp in attrs){var ctr=kvp.Value[0].AttributeType;var o=ctr.UserData["slang:line"];int l=0,c=0;long p=0L;
if(o is int)l=(int)o;o=ctr.UserData["slang:column"];if(o is int)c=(int)o;o=ctr.UserData["slang:position"];if(o is long)p=(long)o;pc.Error("Attribute specified on invalid target "+kvp.Key,
l,c,p);}}}static void _AddCustomAttributes(IDictionary<string,CodeAttributeDeclarationCollection>attrs,string target,CodeAttributeDeclarationCollection
 to){if(null==attrs)return;if(null==target)target="";CodeAttributeDeclarationCollection col;if(attrs.TryGetValue(target,out col)){to.AddRange(col);attrs.Remove(target);
}}static IDictionary<string,CodeAttributeDeclarationCollection>_ParseAttributeGroups(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.lbracket
!=pc.SymbolId)pc.Error("Expecting [ in attribute group declaration");var result=new Dictionary<string,CodeAttributeDeclarationCollection>();while(ST.lbracket==pc.SymbolId)
{var kvp=_ParseAttributeGroup(pc);CodeAttributeDeclarationCollection col;if(!result.TryGetValue(kvp.Key,out col))result.Add(kvp.Key,kvp.Value);else col.AddRange(kvp.Value);
}return result;}static KeyValuePair<string,CodeAttributeDeclarationCollection>_ParseAttributeGroup(_PC pc,bool skipCommentsAndDirectives=true){var l=pc.Line;
var c=pc.Column;var p=pc.Position;if(ST.lbracket!=pc.SymbolId)pc.Error("Expecting [ in attribute group declaration");pc.Advance();var key="";switch(pc.SymbolId)
{case ST.assemblyKeyword:case ST.returnKeyword:key=pc.Value;pc.Advance();if(ST.colon!=pc.SymbolId)pc.Error("Expecting : after attribute group target");
pc.Advance();break;}var val=new CodeAttributeDeclarationCollection();while(!pc.IsEnded&&ST.rbracket!=pc.SymbolId){val.Add(_ParseAttributeDeclaration(pc));
if(ST.rbracket==pc.SymbolId)break;var l2=pc.Line;var c2=pc.Column;var p2=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in attribute group attributes list");
pc.Advance();if(ST.rbracket==pc.SymbolId)pc.Error("Expecting attribute in attribute group attributes list",l2,c2,p2);}if(0==val.Count)pc.Error("Expecting attribute declaration in attribute group");
if(ST.rbracket!=pc.SymbolId)pc.Error("Unterminated attribute group",l,c,p);pc.Advance(skipCommentsAndDirectives);return new KeyValuePair<string,CodeAttributeDeclarationCollection>(key,
val);}static CodeAttributeDeclaration _ParseAttributeDeclaration(_PC pc){var l=pc.Line;var c=pc.Column;var p=pc.Position;var ctr=_ParseTypeBase(pc); ctr.UserData["slang:unresolved"]
=true;var result=new CodeAttributeDeclaration(ctr);if(ST.lparen==pc.SymbolId){pc.Advance();result.Arguments.AddRange(_ParseAttributeArguments(pc));if(ST.rparen
!=pc.SymbolId)pc.Error("Unterminated custom attribute argument list",l,c,p);pc.Advance();}return result;}static CodeAttributeArgumentCollection _ParseAttributeArguments(_PC
 pc){var result=new CodeAttributeArgumentCollection();while(!pc.IsEnded&&ST.rparen!=pc.SymbolId){result.Add(_ParseAttributeArgument(pc));if(ST.rparen==
pc.SymbolId)break;var l=pc.Line;var c=pc.Column;var p=pc.Position;if(ST.comma!=pc.SymbolId)pc.Error("Expecting , in attribute argument list");pc.Advance();
if(ST.rparen==pc.SymbolId)pc.Error("Expecting argument in attribute argument list",l,c,p);}return result;}static CodeAttributeArgument _ParseAttributeArgument(_PC
 pc){ var pc2=pc.GetLookAhead(true);if(ST.verbatimIdentifier==pc2.SymbolId||ST.identifier==pc2.SymbolId){pc2.Advance();if(ST.eq==pc2.SymbolId){pc2=null;
 var n=_ParseIdentifier(pc);pc.Advance();return new CodeAttributeArgument(n,_ParseExpression(pc));}}pc2=null;return new CodeAttributeArgument(_ParseExpression(pc));
}static MemberAttributes _BuildMemberAttributes(HashSet<string>attrs){var result=(MemberAttributes)0;foreach(var kw in attrs){switch(kw){case"protected":
if(attrs.Contains("internal"))result=(result&~MemberAttributes.AccessMask)|MemberAttributes.FamilyOrAssembly;else result=(result&~MemberAttributes.AccessMask)
|MemberAttributes.Family;break;case"internal":if(attrs.Contains("protected"))result=(result&~MemberAttributes.AccessMask)|MemberAttributes.FamilyOrAssembly;
else result=(result&~MemberAttributes.AccessMask)|MemberAttributes.FamilyAndAssembly;break;case"const":result=(result&~MemberAttributes.ScopeMask)|MemberAttributes.Const;
break;case"new":result=(result&~MemberAttributes.VTableMask)|MemberAttributes.New;break;case"override":result=(result&~MemberAttributes.ScopeMask)|MemberAttributes.Override;
break;case"public":if(attrs.Contains("virtual"))result=(result&~MemberAttributes.AccessMask)|MemberAttributes.Public;else{result=(result&~MemberAttributes.AccessMask)
|MemberAttributes.Public;result=(result&~MemberAttributes.ScopeMask)|MemberAttributes.Final;}break;case"private":result=(result&~MemberAttributes.AccessMask)
|MemberAttributes.Private;break;case"abstract":result=(result&~MemberAttributes.ScopeMask)|MemberAttributes.Abstract;break;case"static":result=(result
&~MemberAttributes.ScopeMask)|MemberAttributes.Static;break;}}return result;}static TypeAttributes _BuildMemberTypeAttributes(HashSet<string>attrs){var
 result=TypeAttributes.NestedFamANDAssem;foreach(var attr in attrs){switch(attr){case"protected":if(attrs.Contains("internal"))result=(result&~TypeAttributes.VisibilityMask)
|TypeAttributes.NestedFamORAssem|TypeAttributes.NotPublic;else result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedFamily|TypeAttributes.NotPublic;
break;case"internal":if(attrs.Contains("protected"))result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedFamORAssem|TypeAttributes.NotPublic;
else result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedFamANDAssem|TypeAttributes.NotPublic;break;case"public":result=(result&~TypeAttributes.VisibilityMask)
|TypeAttributes.NestedPublic;break;case"private":result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedPrivate|TypeAttributes.NotPublic;
break;}}return result;}static TypeAttributes _BuildTypeAttributes(HashSet<string>attrs,int line,int column,long position){var result=(TypeAttributes)0;
foreach(var attr in attrs){switch(attr){case"public":result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.Public;break;case"internal":result=
(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NotPublic;break;case"abstract":result|=TypeAttributes.Abstract;break;case"private":throw new SlangSyntaxException("Top level types cannot be private",
line,column,position);case"virtual":throw new SlangSyntaxException("Top level types cannot be virtual",line,column,position);case"protected":throw new
 SlangSyntaxException("Top level types cannot be protected",line,column,position);case"static":throw new SlangSyntaxException("Top level types cannot be static",
line,column,position);case"new":throw new SlangSyntaxException("Top level types cannot be declared new",line,column,position);case"override":throw new
 SlangSyntaxException("Top level types cannot be declared override",line,column,position);}}return result;}}}namespace Slang{
#if SLANGLIB
public
#endif
class SlangPatcher{const BindingFlags _BindFlags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.FlattenHierarchy;
/// <summary>
/// Patches the CodeDOM tree received from the <see cref="SlangParser"/> into something more usable, by resolving type information and replacing various elements in the CodeDOM graph
/// </summary>
/// <param name="compileUnits">The <see cref="CodeCompileUnit"/> objects to patch</param>
public static void Patch(params CodeCompileUnit[]compileUnits)=>Patch((IEnumerable<CodeCompileUnit>)compileUnits);/// <summary>
/// Patches the CodeDOM tree received from the <see cref="SlangParser"/> into something more usable, by resolving type information and replacing various elements in the CodeDOM graph
/// </summary>
/// <param name="compileUnits">The <see cref="CodeCompileUnit"/> objects to patch</param>
public static void Patch(IEnumerable<CodeCompileUnit>compileUnits){var resolver=new CodeDomResolver();foreach(var ccu in compileUnits)resolver.CompileUnits.Add(ccu);
resolver.Refresh();restart:var working=-1;var oworking=0;while(0!=working&&oworking!=working){oworking=working;working=0;for(int ic=resolver.CompileUnits.Count,
i=0;i<ic;++i){CodeDomVisitor.Visit(resolver.CompileUnits[i],(ctx)=>{var co=ctx.Target as CodeObject;if(null!=co&&co.UserData.Contains("slang:unresolved"))
{++working;_Patch(ctx.Target as CodeFieldReferenceExpression,ctx,resolver);_Patch(ctx.Target as CodeVariableDeclarationStatement,ctx,resolver);_Patch(ctx.Target
 as CodeAssignStatement,ctx,resolver);_Patch(ctx.Target as CodeVariableReferenceExpression,ctx,resolver);_Patch(ctx.Target as CodeDelegateInvokeExpression,
ctx,resolver);_Patch(ctx.Target as CodeObjectCreateExpression,ctx,resolver);_Patch(ctx.Target as CodeBinaryOperatorExpression,ctx,resolver);_Patch(ctx.Target
 as CodeIndexerExpression,ctx,resolver);_Patch(ctx.Target as CodeMemberMethod,ctx,resolver);_Patch(ctx.Target as CodeMemberProperty,ctx,resolver);_Patch(ctx.Target
 as CodeTypeReferenceExpression,ctx,resolver);_Patch(ctx.Target as CodeTypeReference,ctx,resolver);}});}resolver.Refresh();}oworking=working;working=0;
if(0<oworking){ for(int ic=resolver.CompileUnits.Count,i=0;i<ic;++i){CodeDomVisitor.Visit(resolver.CompileUnits[i],(ctx)=>{var co=ctx.Target as CodeObject;
if(null!=co&&co.UserData.Contains("slang:unresolved")){++working;_Patch(ctx.Target as CodeFieldReferenceExpression,ctx,resolver);_Patch(ctx.Target as CodeVariableDeclarationStatement,
ctx,resolver);_Patch(ctx.Target as CodeAssignStatement,ctx,resolver);_Patch(ctx.Target as CodeVariableReferenceExpression,ctx,resolver);_Patch(ctx.Target
 as CodeDelegateInvokeExpression,ctx,resolver);_Patch(ctx.Target as CodeObjectCreateExpression,ctx,resolver);_Patch(ctx.Target as CodeBinaryOperatorExpression,
ctx,resolver);_Patch(ctx.Target as CodeIndexerExpression,ctx,resolver);_Patch(ctx.Target as CodeMemberMethod,ctx,resolver);_Patch(ctx.Target as CodeMemberProperty,
ctx,resolver);_Patch(ctx.Target as CodeTypeReferenceExpression,ctx,resolver);_Patch(ctx.Target as CodeTypeReference,ctx,resolver);}});}if(oworking!=working)
goto restart;}}/// <summary>
/// Gets the next element that has not been resolved
/// </summary>
/// <param name="compileUnits">The compile units to search</param>
/// <returns>A <see cref="CodeObject"/> representing the next code object that needs to be patched</returns>
public static CodeObject GetNextUnresolvedElement(params CodeCompileUnit[]compileUnits)=>GetNextUnresolvedElement((IEnumerable<CodeCompileUnit>)compileUnits);
/// <summary>
/// Gets the next element that has not been resolved
/// </summary>
/// <param name="compileUnits">The compile units to search</param>
/// <returns>A <see cref="CodeObject"/> representing the next code object that needs to be patched</returns>
public static CodeObject GetNextUnresolvedElement(IEnumerable<CodeCompileUnit>compileUnits){CodeObject result=null;foreach(var cu in compileUnits){CodeDomVisitor.Visit(cu,
(ctx)=>{var co=ctx.Target as CodeObject;if(null!=co){if(co.UserData.Contains("slang:unresolved")){result=co;ctx.Cancel=true;}}});if(null!=result)return
 result;}return null;}static string _AppendLineInfo(string msg,CodeObject co){var l=0;var c=0;var p=0L;var o=co.UserData["slang:line"];if(null!=o)l=(int)o;
o=co.UserData["slang:column"];if(null!=o)c=(int)o;o=co.UserData["slang:position"];if(null!=o)p=(long)o;if(0<(l+c+p)){msg+=string.Format(" at line {0}, column {1}, position {2}",
l,c,p);}return msg;}static void _Patch(CodeAssignStatement ast,CodeDomVisitContext ctx,CodeDomResolver res){if(null!=ast){var eventRef=ast.Left as CodeEventReferenceExpression;
if(null!=eventRef){var bo=ast.Right as CodeBinaryOperatorExpression;if(null!=bo){var trg=bo.Right;if(CodeBinaryOperatorType.Add==bo.Operator){CodeDomVisitor.ReplaceTarget(ctx,
new CodeAttachEventStatement(eventRef,trg));}else if(CodeBinaryOperatorType.Subtract==bo.Operator){CodeDomVisitor.ReplaceTarget(ctx,new CodeRemoveEventStatement(eventRef,
trg));}}}else if(!ast.Left.UserData.Contains("slang:unresolved"))ast.UserData.Remove("slang:unresolved");}}static void _Patch(CodeTypeReference tr,CodeDomVisitContext
 ctx,CodeDomResolver res){if(null!=tr){if(res.IsValidType(tr,res.GetScope(tr))){tr.UserData.Remove("slang:unresolved");return;} var n=tr.BaseType;tr.BaseType
+="Attribute";if(res.IsValidType(tr,res.GetScope(tr))){tr.UserData.Remove("slang:unresolved");return;}tr.BaseType=n; throw new NotImplementedException();
}}static void _Patch(CodeTypeReferenceExpression tr,CodeDomVisitContext ctx,CodeDomResolver res){if(null!=tr){if(res.IsValidType(tr.Type)){tr.Type.UserData.Remove("slang:unresolved");
tr.UserData.Remove("slang:unresolved");}else{ throw new ArgumentException(_AppendLineInfo(string.Format("Unable to resolve type {0}",tr.Type.BaseType),
tr.Type),"compileUnits");}}}static void _Patch(CodeObjectCreateExpression oc,CodeDomVisitContext ctx,CodeDomResolver res){if(null!=oc){oc.UserData.Remove("slang:unresolved");
if(1==oc.Parameters.Count){if(_IsDelegate(oc.Parameters[0],res)){var del=_GetDelegateFromFields(oc,oc.Parameters[0],res);CodeDomVisitor.ReplaceTarget(ctx,
del);}}}}static void _Patch(CodeMemberProperty prop,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=prop){ if(null==prop.PrivateImplementationType)
{var scope=resolver.GetScope(prop);var td=scope.DeclaringType;var binder=new CodeDomBinder(scope);for(int ic=td.BaseTypes.Count,i=0;i<ic;++i){var ctr=
td.BaseTypes[i];var t=resolver.TryResolveType(ctr,scope);if(null!=t){var ma=binder.GetPropertyGroup(t,prop.Name,BindingFlags.Instance|BindingFlags.Public
|BindingFlags.DeclaredOnly);if(0<ma.Length){var p=binder.SelectProperty(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly,ma,null,_GetParameterTypes(prop.Parameters),
null);if(null!=p)prop.ImplementationTypes.Add(ctr);}}}}prop.UserData.Remove("slang:unresolved");}}static void _Patch(CodeBinaryOperatorExpression op,CodeDomVisitContext
 ctx,CodeDomResolver resolver){if(null!=op){var scope=resolver.GetScope(op);if(CodeBinaryOperatorType.IdentityEquality==op.Operator){if(_HasUnresolved(op.Left))
return;var tr1=resolver.GetTypeOfExpression(op.Left);if(resolver.IsValueType(tr1)){if(_HasUnresolved(op.Right))return;var tr2=resolver.GetTypeOfExpression(op.Right);
if(resolver.IsValueType(tr2)){op.Operator=CodeBinaryOperatorType.ValueEquality;}}op.UserData.Remove("slang:unresolved");}else if(CodeBinaryOperatorType.IdentityInequality
==op.Operator){if(_HasUnresolved(op.Left))return;var tr1=resolver.GetTypeOfExpression(op.Left);if(resolver.IsValueType(tr1)){if(_HasUnresolved(op.Right))
return;var tr2=resolver.GetTypeOfExpression(op.Right);if(resolver.IsValueType(tr2)){ op.Operator=CodeBinaryOperatorType.ValueEquality;var newOp=new CodeBinaryOperatorExpression(new
 CodePrimitiveExpression(false),CodeBinaryOperatorType.ValueEquality,op);CodeDomVisitor.ReplaceTarget(ctx,newOp);}}op.UserData.Remove("slang:unresolved");
}}}static void _Patch(CodeMemberMethod meth,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=meth){ if(null==meth.PrivateImplementationType)
{var scope=resolver.GetScope(meth);var td=scope.DeclaringType;var binder=new CodeDomBinder(scope);for(int ic=td.BaseTypes.Count,i=0;i<ic;++i){var ctr=
td.BaseTypes[i];var t=resolver.TryResolveType(ctr,scope);if(null!=t){var ma=binder.GetMethodGroup(t,meth.Name,BindingFlags.Instance|BindingFlags.Public
|BindingFlags.DeclaredOnly);if(0<ma.Length){var m=binder.SelectMethod(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly,ma,_GetParameterTypes(meth.Parameters),
null);if(null!=m)meth.ImplementationTypes.Add(ctr);}}}}meth.UserData.Remove("slang:unresolved");if("Main"==meth.Name&&(meth.Attributes&MemberAttributes.ScopeMask)
==MemberAttributes.Static){if(0==meth.Parameters.Count&&null==meth.ReturnType||"System.Void"==meth.ReturnType.BaseType){var epm=new CodeEntryPointMethod();
epm.Attributes=meth.Attributes;epm.LinePragma=meth.LinePragma;epm.StartDirectives.AddRange(meth.StartDirectives);epm.EndDirectives.AddRange(meth.EndDirectives);
epm.Comments.AddRange(meth.Comments);epm.CustomAttributes.AddRange(meth.CustomAttributes);epm.ReturnTypeCustomAttributes.AddRange(meth.ReturnTypeCustomAttributes);
epm.TypeParameters.AddRange(meth.TypeParameters);epm.PrivateImplementationType=meth.PrivateImplementationType;epm.ImplementationTypes.AddRange(meth.ImplementationTypes);
epm.Name=meth.Name;epm.Statements.AddRange(meth.Statements);CodeDomVisitor.ReplaceTarget(ctx,epm);}}}}static CodeTypeReference[]_GetParameterTypes(CodeParameterDeclarationExpressionCollection
 parms){var result=new CodeTypeReference[parms.Count];for(var i=0;i<result.Length;i++)result[i]=parms[i].Type;return result;}static void _Patch(CodeVariableReferenceExpression
 vr,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=vr){var scope=resolver.GetScope(vr);if(0==string.Compare("value",vr.VariableName,StringComparison.InvariantCulture))
{ var p=scope.Member as CodeMemberProperty;if(null!=p){var found=false;for(int ic=p.SetStatements.Count,i=0;i<ic;++i){found=false;CodeDomVisitor.Visit(p.SetStatements[i],
(ctx2)=>{if(ctx2.Target==vr){found=true;ctx2.Cancel=true;}});if(found)break;}if(found){CodeDomVisitor.ReplaceTarget(ctx,new CodePropertySetValueReferenceExpression());
return;}}}CodeTypeReference ctr;if(scope.VariableTypes.TryGetValue(vr.VariableName,out ctr)){if(!CodeDomResolver.IsNullOrVoidType(ctr)){vr.UserData.Remove("slang:unresolved");
return;}} if(scope.ArgumentTypes.ContainsKey(vr.VariableName)){var a=new CodeArgumentReferenceExpression(vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,
a);return;}else if(scope.FieldNames.Contains(vr.VariableName)){CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName)){var f=new CodeFieldReferenceExpression(new
 CodeThisReferenceExpression(),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,f);}else if(scope.TypeTargets.TryGetValue(vr.VariableName,out tref)){
var f=new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(tref),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,f);}return;}else if(scope.MethodNames.Contains(vr.VariableName))
{CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName)){var m=new CodeMethodReferenceExpression(new CodeThisReferenceExpression(),vr.VariableName);
CodeDomVisitor.ReplaceTarget(ctx,m);return;}if(scope.TypeTargets.TryGetValue(vr.VariableName,out tref)){var m=new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(tref),
vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,m);return;}}else if(scope.PropertyNames.Contains(vr.VariableName)){CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName))
{var p=new CodePropertyReferenceExpression(new CodeThisReferenceExpression(),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,p);return;}else if(scope.TypeTargets.TryGetValue(vr.VariableName,
out tref)){var p=new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(tref),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,p);return;
}}else if(scope.EventNames.Contains(vr.VariableName)){CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName)){var e=new CodeEventReferenceExpression(new
 CodeThisReferenceExpression(),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,e);return;}else if(scope.TypeTargets.TryGetValue(vr.VariableName,out tref))
{var e=new CodeEventReferenceExpression(new CodeTypeReferenceExpression(tref),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,e);return;}}return;}return;
}static void _Patch(CodeIndexerExpression indexer,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=indexer){if(indexer.TargetObject.UserData.Contains("slang:unresolved"))
return;var ctr=resolver.GetTypeOfExpression(indexer.TargetObject);if(null!=ctr.ArrayElementType&&0<ctr.ArrayRank){var ai=new CodeArrayIndexerExpression(indexer.TargetObject);
ai.Indices.AddRange(indexer.Indices);CodeDomVisitor.ReplaceTarget(ctx,ai);}indexer.UserData.Remove("slang:unresolved");}}static void _Patch(CodeDelegateInvokeExpression
 di,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=di){ if(null!=di.TargetObject){ var mr=di.TargetObject as CodeMethodReferenceExpression;
if(null!=mr){var mi=new CodeMethodInvokeExpression(mr);mi.Parameters.AddRange(di.Parameters);CodeDomVisitor.ReplaceTarget(ctx,mi);}else{var cco=di.TargetObject
 as CodeObject;if(null==cco)System.Diagnostics.Debugger.Break();}}else{ throw new InvalidProgramException(_AppendLineInfo("Untargeted delegate invoke produced by slang parser",di));
}}}static bool _HasUnresolved(CodeObject target){if(target.UserData.Contains("slang:unresolved"))return true;var result=false;CodeDomVisitor.Visit(target,
(ctx)=>{var co=ctx.Target as CodeObject;if(null!=co&&co.UserData.Contains("slang:unresolved")){result=true;ctx.Cancel=true;}});return result;}static void
 _Patch(CodeVariableDeclarationStatement vd,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=vd){if(CodeDomResolver.IsNullOrVoidType(vd.Type)
||(0==vd.Type.ArrayRank&&0==vd.Type.TypeArguments.Count&&0==string.Compare("var",vd.Type.BaseType,StringComparison.InvariantCulture))){if(null==vd.InitExpression)
throw new ArgumentException(_AppendLineInfo("The code contains an incomplete variable declaration",vd),"resolver");if(!_HasUnresolved(vd.InitExpression))
{var t=resolver.GetTypeOfExpression(vd.InitExpression,resolver.GetScope(vd.InitExpression));vd.Type=t;if(!CodeDomResolver.IsNullOrVoidType(t)){vd.UserData.Remove("slang:unresolved");
}}}}}static void _Patch(CodeFieldReferenceExpression fr,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=fr){ if(!fr.TargetObject.UserData.Contains("slang:unresolved"))
{var scope=resolver.GetScope(fr);var binder=new CodeDomBinder(scope);var t=resolver.GetTypeOfExpression(fr.TargetObject);if(null!=t&&CodeDomResolver.IsNullOrVoidType(t)
&&fr.TargetObject is CodeVariableReferenceExpression)return; var isStatic=false;var tre=fr.TargetObject as CodeTypeReferenceExpression;if(null!=tre)isStatic
=true;var tt=resolver.TryResolveType(isStatic?tre.Type:t,scope,true);if(null==tt)throw new InvalidOperationException(_AppendLineInfo(string.Format("The type {0} could not be resolved",
t.BaseType),t));var td=tt as CodeTypeDeclaration; var m=binder.GetField(tt,fr.FieldName,_BindFlags);if(null!=m){fr.UserData.Remove("slang:unresolved");
return;}m=binder.GetEvent(tt,fr.FieldName,_BindFlags);if(null!=m){var er=new CodeEventReferenceExpression(fr.TargetObject,fr.FieldName);CodeDomVisitor.ReplaceTarget(ctx,
er);return;}var ml=binder.GetMethodGroup(tt,fr.FieldName,_BindFlags);if(0<ml.Length){var mr=new CodeMethodReferenceExpression(fr.TargetObject,fr.FieldName);
CodeDomVisitor.ReplaceTarget(ctx,mr);return;}ml=binder.GetPropertyGroup(tt,fr.FieldName,_BindFlags);if(0<ml.Length){var pr=new CodePropertyReferenceExpression(fr.TargetObject,
fr.FieldName);CodeDomVisitor.ReplaceTarget(ctx,pr);return;}throw new InvalidProgramException(_AppendLineInfo(string.Format("Cannot deterimine the target reference {0}",
fr.FieldName),fr));} var path=_GetUnresRootPathOfExpression(fr);if(null!=path){ var scope=resolver.GetScope(fr);var sa=path.Split('.');if(1==sa.Length)
{System.Diagnostics.Debugger.Break();throw new NotImplementedException();}else{object t=null;string tn=null;CodeExpression tf=fr;CodeExpression ptf=null;
CodeTypeReference ctr=null;for(var i=sa.Length-1;i>=1;--i){tn=string.Join(".",sa,0,i);ptf=tf;tf=_GetTargetOfExpression(tf);ctr=new CodeTypeReference(tn);
t=resolver.TryResolveType(ctr,scope);if(null!=t)break;}if(null!=t){var tt=t as Type;if(null!=tt)ctr=new CodeTypeReference(tt);else ctr=resolver.GetQualifiedType(ctr,
scope); _SetTargetOfExpression(ptf,new CodeTypeReferenceExpression(ctr));return;}}}}}static CodeDelegateCreateExpression _GetDelegateFromFields(CodeObjectCreateExpression
 oc,CodeExpression target,CodeDomResolver res){var v=target as CodeVariableReferenceExpression;if(null!=v){var scope=res.GetScope(v);if(scope.MemberNames.Contains(v.VariableName))
return new CodeDelegateCreateExpression(oc.CreateType,new CodeThisReferenceExpression(),v.VariableName);}throw new NotImplementedException();}static bool
 _IsDelegate(CodeExpression target,CodeDomResolver res){var v=target as CodeVariableReferenceExpression;if(null!=v&&v.UserData.Contains("slang:unresolved"))
{var scope=res.GetScope(target);if(scope.MemberNames.Contains(v.VariableName))return true;}return false;}static string _GetUnresRootPathOfExpression(CodeExpression
 t){var result=_GetNameOfExpression(t);var sawVar=false;while(null!=(t=_GetTargetOfExpression(t))){if(!t.UserData.Contains("slang:unresolved"))return null;
result=string.Concat(_GetNameOfExpression(t),".",result);sawVar=null!=(t as CodeVariableReferenceExpression);}if(!sawVar)return null;return result;}static
 CodeExpression _GetTargetOfExpression(CodeExpression e){var fr=e as CodeFieldReferenceExpression;if(null!=fr)return fr.TargetObject;var mr=e as CodeMethodReferenceExpression;
if(null!=mr)return mr.TargetObject;var pr=e as CodePropertyReferenceExpression;if(null!=pr)return pr.TargetObject;var er=e as CodeEventReferenceExpression;
if(null!=er)return er.TargetObject;return null;}static void _SetTargetOfExpression(CodeExpression e,CodeExpression t){var fr=e as CodeFieldReferenceExpression;
if(null!=fr){fr.TargetObject=t;return;}var mr=e as CodeMethodReferenceExpression;if(null!=mr){mr.TargetObject=t;return;}var pr=e as CodePropertyReferenceExpression;
if(null!=pr){pr.TargetObject=t;return;}var er=e as CodeEventReferenceExpression;if(null!=er){er.TargetObject=t;return;}throw new ArgumentException("Invalid expression",
nameof(e));}static string _GetNameOfExpression(CodeExpression e){var fr=e as CodeFieldReferenceExpression;if(null!=fr)return fr.FieldName;var mr=e as CodeMethodReferenceExpression;
if(null!=mr)return mr.MethodName;var pr=e as CodePropertyReferenceExpression;if(null!=pr)return pr.PropertyName;var er=e as CodeEventReferenceExpression;
if(null!=er)return er.EventName;var vr=e as CodeVariableReferenceExpression;if(null!=vr)return vr.VariableName;return null;}}}namespace Slang{/// <summary>
/// Preprocesses input using a simplified T4 style syntax
/// </summary>
#if SLANGLIB
public
#endif
class SlangPreprocessor{/// <summary>
/// Preprocesses the input from <paramref name="input"/> and writes the output to <paramref name="output"/>
/// </summary>
/// <param name="input">The input source to preprocess</param>
/// <param name="output">The output target for the post-processed <paramref name="input"/></param>
/// <param name="args">The arguments to pass to the T4 code. Accessed through "Arguments" in the code. If null, an empty dictionary will be created</param>
/// <param name="lang">The language to use for the T4 code - defaults to C#</param>
public static void Preprocess(TextReader input,TextWriter output,IDictionary<string,object>args=null,string lang="cs"){CompilerErrorCollection errors=
null; var method=new CodeMemberMethod();method.Attributes=MemberAttributes.Public|MemberAttributes.Static;method.Name="Preprocess";method.Parameters.Add(new
 CodeParameterDeclarationExpression(typeof(TextWriter),"Response"));method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(IDictionary<string,object>),
"Arguments"));int cur;var more=true;while(more){var text=_ReadUntilStartContext(input);if(0<text.Length){method.Statements.Add(new CodeMethodInvokeExpression(
new CodeArgumentReferenceExpression("Response"),"Write",new CodePrimitiveExpression(text)));}cur=input.Read();if(-1==cur)more=false;else if('='==cur){
method.Statements.Add(new CodeMethodInvokeExpression(new CodeArgumentReferenceExpression("Response"),"Write",new CodeSnippetExpression(_ReadUntilEndContext(-1,
input))));}else method.Statements.Add(new CodeSnippetStatement(_ReadUntilEndContext(cur,input)));}method.Statements.Add(new CodeMethodInvokeExpression(new
 CodeArgumentReferenceExpression("Response"),"Flush"));var cls=new CodeTypeDeclaration("Preprocessor");cls.TypeAttributes=TypeAttributes.Public;cls.IsClass
=true;cls.Members.Add(method);var ns=new CodeNamespace();ns.Types.Add(cls);var cu=new CodeCompileUnit();cu.Namespaces.Add(ns);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CompilerParameters();var outp=prov.CompileAssemblyFromDom(opts,cu);var asm=outp.CompiledAssembly;if(null==args)args=new Dictionary<string,
object>();var ran=false;if(null!=asm){var t=asm.GetType("Preprocessor");var m=t.GetMethod("Preprocess");if(null!=m){try{m.Invoke(null,new object[]{output,args
});ran=true;}catch(TargetInvocationException tex){throw tex.InnerException;}}}if(!ran){errors=outp.Errors;if(0<errors.Count){CompilerError err=errors[0];
throw new InvalidOperationException(err.ErrorText);}}}static string _ReadUntilStartContext(TextReader input){int cur=input.Read();var sb=new StringBuilder();
while(true){if('<'==cur){cur=input.Read();if(-1==cur){sb.Append('<');return sb.ToString();}else if('#'==cur)return sb.ToString();sb.Append('<');}else if
(-1==cur)return sb.ToString();sb.Append((char)cur);cur=input.Read();}}static string _ReadUntilEndContext(int firstChar,TextReader input){int cur;cur=firstChar;
if(-1==firstChar)cur=input.Read();var sb=new StringBuilder();while(true){if('#'==cur){cur=input.Read();if(-1==cur){sb.Append('#');return sb.ToString();
}else if('>'==cur)return sb.ToString();sb.Append('>');}else if(-1==cur)return sb.ToString();sb.Append((char)cur);cur=input.Read();}}}}namespace Slang{
/// <summary>
/// Summary Canonical example of GPLEX automaton
/// </summary>
#if STANDALONE
 internal enum Tokens{EOF=0,maxParseToken=int.MaxValue}internal abstract class ScanBase{[SuppressMessage("Microsoft.Naming","CA1709:IdentifiersShouldBeCasedCorrectly",
MessageId="yylex")][SuppressMessage("Microsoft.Naming","CA1704:IdentifiersShouldBeSpelledCorrectly",MessageId="yylex")]public abstract int yylex();[SuppressMessage("Microsoft.Naming",
"CA1709:IdentifiersShouldBeCasedCorrectly",MessageId="yywrap")][SuppressMessage("Microsoft.Naming","CA1704:IdentifiersShouldBeSpelledCorrectly",MessageId
="yywrap")]protected virtual bool yywrap(){return true;}
#if BABEL
protected abstract int CurrentSc{get;set;} public virtual int EolState{get{return CurrentSc;}set{CurrentSc=value;}}}internal interface IColorScan{void
 SetSource(string source,int offset);int GetNext(ref int state,out int start,out int end);
#endif // BABEL
}
#endif // STANDALONE
#if BABEL
internal sealed partial class Scanner:ScanBase,IColorScan{private ScanBuff buffer;int currentScOrd; protected override int CurrentSc{ get{return currentScOrd;
} set{currentScOrd=value; currentStart=startState[value];}}
#else  // BABEL
internal sealed partial class Scanner:ScanBase{private ScanBuff buffer;int currentScOrd;
#endif // BABEL
/// <summary>
/// The input buffer for this scanner.
/// </summary>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]public ScanBuff Buffer{get{return buffer;}}private static int GetMaxParseToken()
{System.Reflection.FieldInfo f=typeof(Tokens).GetField("maxParseToken");return(f==null?int.MaxValue:(int)f.GetValue(null));}static int parserMax=GetMaxParseToken();
enum Result{accept,noMatch,contextFound};const int maxAccept=323;const int initial=324;const int eofNum=0;const int goStart=-1;const int INITIAL=0;
#region user code
 public const int TabWidth=4; int _line=1;int _column=1;long _position=0;List<Token>_skipped=new List<Token>(); enum Tokens{EOF=-1} public Token Current
=_InitToken();static Token _InitToken(){var result=default(Token);result.SymbolId=-4;result.Line=1;result.Column=1;result.Position=0;result.Skipped=null;
return result;}public void UpdatePosition(string text){if(string.IsNullOrEmpty(text))return;for(var i=0;i<text.Length;++i){var ch=text[i];switch(ch){case
'\n':++_line;_column=1;break;case'\r':_column=1;break;case'\t':_column+=TabWidth;break;default:++_column;break;}++_position;}}public void Skip(int sym)
{Token t=_InitToken();t.SymbolId=sym;t.Line=Current.Line;t.Column=Current.Column;t.Position=Current.Position;t.Value=yytext;t.Skipped=null;_skipped.Add(t);
}public void Advance(){Current.SymbolId=yylex();Current.Value=yytext;Current.Line=_line;Current.Column=_column;Current.Position=_position;Current.Skipped=new
 Token[_skipped.Count];_skipped.CopyTo(Current.Skipped,0);_skipped.Clear();}public void Close(){Current.SymbolId=-3;}bool _TryReadUntil(int character,StringBuilder
 sb){if(-1==code)return false;var chcmp=character.ToString();var s=char.ConvertFromUtf32(code);sb.Append(s);if(code==character)return true;while(true)
{GetCode();if(-1==code||code==character)break;s=char.ConvertFromUtf32(code);sb.Append(s);}if(-1!=code){s=char.ConvertFromUtf32(code);sb.Append(s);if(null
==tokTxt)tokTxt=sb.ToString();else tokTxt+=sb.ToString();UpdatePosition(tokTxt);return code==character;}return false;} bool _TryReadUntilBlockEnd(string
 blockEnd){string s=yytext;var sb=new StringBuilder();int ch=-1;var isPair=false;if(char.IsSurrogatePair(blockEnd,0)){ch=char.ConvertToUtf32(blockEnd,
0);isPair=true;}else ch=blockEnd[0];while(-1!=code&&_TryReadUntil(ch,sb)){bool found=true;int i=1;if(isPair)++i;for(;found&&i<blockEnd.Length;++i){GetCode();
int scmp=blockEnd[i];if(char.IsSurrogatePair(blockEnd,i)){scmp=char.ConvertToUtf32(blockEnd,i);++i;}if(-1==code||code!=scmp)found=false;else if(-1!=code)
sb.Append(char.ConvertFromUtf32(code));}if(found){ GetCode();tokTxt=s+sb.ToString();UpdatePosition(tokTxt);return true;}}tokTxt=s+sb.ToString();UpdatePosition(tokTxt);
return false;}
#endregion user code
int state;int currentStart=startState[0];int code; int cCol; int lNum; int tokPos; int tokCol; int tokLin; int tokEPos; int tokECol; int tokELin; string
 tokTxt;
#if STACK          
private Stack<int>scStack=new Stack<int>();
#endif // STACK
#region ScannerTables
struct Table{public int min;public int rng;public int dflt;public short[]nxt;public Table(int m,int x,int d,short[]n){min=m;rng=x;dflt=d;nxt=n;}};static
 int[]startState=new int[]{324,0};
#region TwoLevelCharacterMap
 static sbyte[]mLo0=new sbyte[256]{ 24,49,49,49,49,49,49,24,24,27,68,30,30,0,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 50,39,57,25,49,43,
42,48,36,35,23,45,32,44,31,22, 63,52,52,52,52,52,52,52,55,55,28,29,47,41,46,49, 58,54,54,54,67,65,67,26,26,26,26,26,64,66,26,26, 26,26,26,26,26,56,26,
26,26,26,26,38,51,37,49,60, 49,13,14,9,8,3,19,18,2,7,59,26,16,20,10,5, 17,26,4,11,12,15,6,1,53,21,26,34,40,33,49,49, 49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,61,49,49,49,49,49, 49,49,49,49,49,61,49,49,49,49,61,49,
49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,49,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,49,61,61,61,61,61,61,61,61};static sbyte[]mLo1=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61};static sbyte[]mLo2=new sbyte[256]
{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,49,
49,49,61,61,61,61,61,61,61,61,61,61, 61,61,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,49,49,49,49,49,49,49,61,49,61,49, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo3=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,49,61,61,49,49,61,61,61,
61,49,61, 49,49,49,49,49,49,61,49,61,61,61,49,61,49,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,61,61,61,61,61,61,61,61,61};static sbyte[]mLo4=new sbyte[256]{ 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,49,49,49,49,49,49,49,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61};static sbyte[]mLo5=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,49,49,61,49,49,49,49,49,49, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,49,49,49,49,49, 61,61,61,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo6=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,
49,49,61,61, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,49,61,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,61,61,49,49,49,49,49,49,49,61,61, 62,62,62,62,62,62,62,62,62,62,61,61,61,49,49,
61};static sbyte[]mLo7=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,49,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,49, 49,61,49,49,49,49,49,49,
49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,49,49,49,49,49, 49,49,49,49,61,61,49,49,49,49,61,49,49,49,49,49};static sbyte[]mLo8=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,49,49,49,49,61,49,49,49,49,49, 49,49,49,49,61,49,49,49,61,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,
49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]
mLo9=new sbyte[256]{ 49,49,49,49,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,49,49,49,49,49,49,49,61,61,61,61,
61,61,61,61, 61,61,49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,49,49,49,49,61,61,61,61,61,61,61,61,
49,49,61, 61,49,49,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,61,61, 61,49,61,49,49,49,61,61,61,61,49,49,49,61,
49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,61,61,49,61, 61,61,49,49,49,49,62,62,62,62,62,62,62,62,62,
62, 61,61,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo10=new sbyte[256]{ 49,49,49,49,49,61,61,61,61,61,61,49,49,49,49,61, 61,49,49,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,61,61, 61,49,61,61,49,61,61,49,61,61,49,49,49,49,49,49, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,61,61,61,61,49,61,49, 49,49,49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,49,61,61,61,49,
49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,61,61,61,61,61,61,61,61,61,49,61, 61,61,49,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,49,61,61,61,61,61,61, 61,49,61,61,49,61,61,61,61,61,49,49,49,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49, 61,61,49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,49,49,49,49,49,49,49,49,61,49,49,49,49,49,49};static sbyte[]mLo11=new sbyte[256]
{ 49,49,49,49,49,61,61,61,61,61,61,61,61,49,49,61, 61,49,49,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,61,61, 61,
49,61,61,49,61,61,61,61,61,49,49,49,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,61,61,49,61, 61,61,
49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,61,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,61,49,61,61,61,61,61,61,49,49,49,61,61, 61,49,61,
61,61,61,49,49,49,61,61,49,61,49,61,61, 49,49,49,61,61,49,49,49,61,61,61,49,49,49,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,49,49,49, 49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49, 61,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo12=new sbyte[256]{ 49,49,49,49,49,61,61,61,61,61,61,61,61,49,61,61, 61,49,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,61,61,61,49,49,49,49,49, 61,61,49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,61,61,61,61,61,61,61,61,49,61,61, 61,49,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,
61,61, 61,61,61,61,49,61,61,61,61,61,49,49,49,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,61,
49, 61,61,49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,61,61,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo13=new sbyte[256]{ 49,49,49,49,
49,61,61,61,61,61,61,61,61,49,61,61, 61,49,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,49,49,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,61, 61,61,49,49,49,49,
62,62,62,62,62,62,62,62,62,62, 49,49,49,49,49,49,49,49,49,49,61,61,61,61,61,61, 49,49,49,49,49,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
49,49,49,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,61,61,61,61,61,61,61,61,61,49,61,49,49, 61,61,61,61,61,61,61,49,
49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,62,62,62,62,62,62,62,62,62,62, 49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49};static sbyte[]mLo14=new sbyte[256]{ 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,49,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,
 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,
61,61,49,61,49,49,61,61,49,61,49,49,61,49,49, 49,49,49,49,61,61,61,61,49,61,61,61,61,61,61,61, 49,61,61,61,49,61,49,61,49,49,61,61,49,61,61,61, 61,49,
61,61,49,49,49,49,49,49,49,49,49,61,49,49, 61,61,61,61,61,49,61,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,61,61,61,61, 49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo15=new sbyte[256]{ 61,49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 61,61,61,61,61,61,61,61,49,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,61,61,61,61,61,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49};static sbyte[]mLo16=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,61, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 61,61,61,61,
61,61,49,49,49,49,61,61,61,61,49,49, 49,61,49,49,49,61,61,49,49,49,49,49,49,49,61,61, 61,49,49,49,49,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,49,49,
49,49,49,49,49,49,49,49,49,61,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,61,49,49,49,49,49,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,61,61,61,61};static sbyte[]mLo18=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,49,49, 61,61,61,61,61,61,61,49,61,49,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,49,61,61,61,61,49,49,61,61,61,61,61,61,61,49, 61,49,61,61,61,61,49,49,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,49,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61};static
 sbyte[]mLo19=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,49,61,61,61,61,49,49,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,49,49,61,61,61,61,61,61,49,49};static sbyte[]mLo20=new sbyte[256]{ 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61};static sbyte[]mLo22
=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,61,61,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,49,49,49,49,
 49,61,61,61,61,61,61,61,61,49,49,49,49,49,49,49};static sbyte[]mLo23=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,49,61,61, 61,61,49,49,49,
49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,61,61, 61,49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,61,49,
49,49,49,61,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo24=new sbyte[256]
{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo25=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,62,62,62,62,62,62,
62,62,62,62, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 61,61,61,61,61,49,49,49,49,49,49,49,49,
49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,49,49,
49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,
49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo26=new sbyte[256]{ 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 62,62,62,62,62,62,62,
62,62,62,49,49,49,49,49,49, 49,49,49,49,49,49,49,61,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49};static sbyte[]mLo27=new sbyte[256]{ 49,49,49,49,49,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,61,61,61,61,61,61,61,49,49,49,49,
 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,
49,49,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,49,49,49,49,49,49,49,49,49,49,49,49,49,61,61, 62,62,
62,62,62,62,62,62,62,62,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo28=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,61,61,61, 62,62,62,62,62,62,62,62,62,62,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,61,61,61,61,49,61,61, 61,61,49,49,49,61,61,49,49,49,49,49,49,49,
49,49};static sbyte[]mLo29=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo31=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,49,49,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,49,49,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,49,61,49,61,49,61,49,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,61,61,61,61,61,61,61,49,61,49, 49,49,61,61,61,49,61,61,61,61,61,61,61,49,49,49, 61,
61,61,61,49,49,61,61,61,61,61,61,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,49, 49,49,61,61,61,49,61,61,61,61,61,61,61,49,49,49};static
 sbyte[]mLo32=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,61,49,49,49,49,49,49,49,49,49,49,49,49,49,61, 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo33=new sbyte[256]{ 49,49,61,49,49,49,49,61,49,49,61,61,61,61,61,61, 61,
61,61,61,49,61,49,49,49,61,61,61,61,61,49,49, 49,49,49,49,61,49,61,49,61,49,61,61,61,61,49,61, 61,61,61,61,61,61,61,61,61,61,49,49,61,61,61,61, 49,49,
49,49,49,61,61,61,61,61,49,49,49,49,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,61,61,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo34
=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo44=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,49,49,49,49,49,49,61,61,61,61,49, 49,49,61,61,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo45=new sbyte[256]
{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,61,49,49,49,49,49,61,49,49, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,49,49,49,49,49,49,49,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,49,61,61,61,61,61,61,61,49, 61,61,61,61,61,61,61,49,61,61,61,61,61,61,61,49, 61,61,61,61,
61,61,61,49,61,61,61,61,61,61,61,49, 61,61,61,61,61,61,61,49,61,61,61,61,61,61,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo46=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo48=new sbyte[256]{ 49,49,49,49,
49,61,61,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,61,61,61,61,
61,49,49,49,49,49,61,61,49,49,49, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
49,49,49,49,49,49,61,61,61, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,49,61,61,61,61};static sbyte[]mLo49=new sbyte[256]{ 49,49,49,49,49,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,61,61,61,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61};static sbyte[]mLo77=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49};static sbyte[]mLo159=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo164=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49};static
 sbyte[]mLo166=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 62,62,62,62,62,62,62,
62,62,62,61,61,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,61, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,
49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo167=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 49,49,49,49,49,49,49,61,61,61,61,61,61,61,61,61, 49,49,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,49,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,61,61,61,61,61,61,61,61,61};static sbyte[]mLo168
=new sbyte[256]{ 61,61,49,61,61,61,49,61,61,61,61,49,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,
49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 49,49,61,61,61,61,61,61,49,49,49,61,49,61,49,49};static sbyte[]mLo169=new sbyte[256]{ 62,62,62,62,62,62,62,62,62,62,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,61,61,61,61,49,49,49, 49,49,49,49,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,61, 62,62,62,62,62,62,62,62,62,
62,49,49,49,49,49,49, 61,61,61,61,61,49,61,61,61,61,61,61,61,61,61,61, 62,62,62,62,62,62,62,62,62,62,61,61,61,61,61,49};static sbyte[]mLo170=new sbyte[256]
{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,49,49,49,49,49,49, 49,
49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,49,61,61,61,61,61,61,61,61,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,49,49,49,61,49,49,49,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,61,49,49,49,61,61,49,49,61,61,61,61,61,49,49, 61,49,61,49,
49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,49,49,49,49,49, 49,49,61,61,61,
49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo171=new sbyte[256]{ 49,61,61,61,61,61,61,49,49,61,61,61,61,61,61,49, 49,61,61,61,61,61,61,49,49,49,
49,49,49,49,49,49, 61,61,61,61,61,61,61,49,61,61,61,61,61,61,61,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49};static sbyte[]mLo215=new sbyte[256]{ 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
49,49,49,49,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,49,49,49,49};static sbyte[]mLo250=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,49,49,49, 49,
49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mLo251=new sbyte[256]{ 61,61,61,61,61,61,
61,49,49,49,49,49,49,49,49,49, 49,49,49,61,61,61,61,61,49,49,49,49,49,61,49,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,61,61, 61,61,61,61,61,61,61,
49,61,61,61,61,61,49,61,49, 61,61,49,61,61,49,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 49,49,49,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61};static sbyte[]mLo253=new sbyte[256]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,49,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,49,49,49,49};static sbyte[]mLo254=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49, 61,61,61,61,61,49,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,
49,49};static sbyte[]mLo255=new sbyte[256]{ 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 49,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,49,49,49,49, 49,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,49,49,49,49,49, 49,49,49,49,49,49,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61,49, 49,49,61,61,61,61,61,61,49,49,61,61,61,61,61,61, 49,49,61,61,61,61,61,61,49,49,61,61,61,49,49,49, 49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[][]map=new sbyte[256][]{ mLo0,mLo1,mLo2,mLo3,mLo4,mLo5,mLo6,
mLo7,mLo8,mLo9,mLo10,mLo11,mLo12,mLo13,mLo14,mLo15, mLo16,mLo1,mLo18,mLo19,mLo20,mLo1,mLo22,mLo23,mLo24,mLo25,mLo26,mLo27,mLo28,mLo29,mLo1,mLo31, mLo32,
mLo33,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo44,mLo45,mLo46,mLo34, mLo48,mLo49,mLo34,mLo34,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,
mLo1,mLo1,mLo1,mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo77,mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,
mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,
mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,
mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo159, mLo1,mLo1,mLo1,mLo1,mLo164,mLo1,mLo166,mLo167,mLo168,mLo169,mLo170,mLo171,mLo1,mLo1,mLo1,mLo1,
 mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,
mLo1,mLo1, mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo1,mLo215,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34, mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,
mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34, mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo34,mLo1,mLo250,mLo251,mLo1,mLo253,mLo254,mLo255};
#endregion
#region CompressedCharacterMap
 static sbyte[]mapC0=new sbyte[251]{ 61,61,61,61,61,61,61,61,61,61,61,61,49,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,49,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,61,61,49,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,
61,61,61,61,61,61,61,61,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61};static sbyte[]mapC2=new sbyte[384]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,49,61,61,61,61,61,61,61,61,49,49,49,49,49,49, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,61,61,61,61,61,61,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49};static sbyte[]mapC4=new sbyte[198]{ 49,49,62,62,62,62,62,62,62,62,62,62,49,49,49,49,
 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,
49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,49,49,49, 49,49,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61};static sbyte[]mapC7=new sbyte[49]{ 49,49,49,
49,49,49,49,49,49,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,49, 49,49,49,49,49,49,49,49,49,61,61,61,61,61,61,61,61};static sbyte[]
mapC9=new sbyte[1267]{ 61,61,61,61,61,61,49,49,61,49,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61, 61,61,61,61,61,61,49,61,61,49,49,49,61,49,49,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,
49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,49,61,61,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,
49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,61,61,49,49,49,49,49,49,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,
49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,
49,61,61,61,49,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,
61,61,61,61,61,61,61,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,49, 49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,49,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,
49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,49,49,49,49,49,49,49,49,49,49,
49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61};
static sbyte[]mapC11=new sbyte[863]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49, 49,49,49,62,62,62,62,62,62,62,62,62,62,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,49, 49,49,49,49,49,49,49,49,
49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,49,49,
49,49,49,49,62,62,62, 62,62,62,62,62,62,62,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61, 61,61,61,61,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,62,62,62,62,62,62,62,62,62,62,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,
49,49,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,49,49,61,49,49,49,49,49,49,49,49,
49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,61,61, 61,61,49,49,49,49,49,49,49,49,49,49,49,62,62,62, 62,62,62,62,62,62,62,61,49,61,49,49,49,49,
49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,
 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,
49,49,49,49,49,49,49,49,49,49,49,49,61,61,61, 61,61,61,61,49,61,49,61,61,61,61,49,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,61,61,61,61, 61,61,
61,61,61,61,49,49,49,49,49,49,49,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,61,61,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,62,62,62, 62,62,62,62,62,62,62,49,49,49,49,49,49,49,49,49, 49,49,61,61,
61,61,61,61,61,61,49,49,61,61,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,61,61,61,61,61,61,61,49,61, 61,49,61,61,61,
61,61,49,49,49,61,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,61,49,49, 49,49,49,49,49,49,49,49,49,49,61,61,61,61,61};static sbyte[]mapC13=
new sbyte[90]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,61,61,49,61,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62};static sbyte[]
mapC15=new sbyte[442]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,61,61,61,
61,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49,49, 49,49,49,49,61,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,
49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,49,
 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 62,62,
62,62,62,62,62,62,62,62};static sbyte[]mapC17=new sbyte[96]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 62,62,62,62,62,62,62,62,62,62,49,49,49,49,49,
49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,61};static sbyte[]mapC30=new sbyte[343]{ 49,49,49,49,49,49,49,61,61,61,61,61,61,61,61,61, 61,61,61,61,
61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,49,62,62,62,62,62,62,62,62,62, 62,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,
49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,61,61,
61,61,49,49,49,49,49, 49,49,49,49,49,49,49,62,62,62,62,62,62,62,62,62, 62,49,49,49,49,49,49,49,49,49,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61,49, 49,49,49,49,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61};static sbyte[]mapC32=new sbyte[160]{ 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,49,49,49,49,49,49,49,49,49,49,49, 61,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,
49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,49,49,49,49,49,49,49,49,49,49,49,49,49, 49,49,49,61,61,61,61,61,61,61,61,
61,61,61,61,61};static sbyte[]mapC36=new sbyte[154]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,49,49, 61,61,
61,61,61,61,61,61,61,49,49,49,49,49,49,49, 61,61,61,61,61,61,61,61,61,61};static sbyte[]mapC38=new sbyte[338]{ 61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,
61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,61,61,
 49,49,61,49,49,61,61,49,49,61,61,61,61,49,61,61, 61,61,61,61,61,61,61,61,61,61,49,61,49,61,61,61, 61,61,61,61,49,61,61,61,61,61,61,61,61,61,61,61, 61,
61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,
61,61,61,61,49,61,61,61,61,49,49,61,61,61, 61,61,61,61,61,49,61,61,61,61,61,61,61,49,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,
61,61,61,61,61,61,61,49,61,61,61,61,49, 61,61,61,61,61,49,61,49,49,49,61,61,61,61,61,61, 61,49};static sbyte[]mapC40=new sbyte[346]{ 49,49,61,61,61,61,
61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,49,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,61,
61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,49,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61,49, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,49, 61,61,61,61,61,61,61,61,61,
61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,49,
61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,49,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,
61,61,61,61,61, 61,61,61,49,61,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,49,61,61, 61,61,61,61,61,61,49,49,62,62,62,62,
62,62,62,62, 62,62,62,62,62,62,62,62,62,62,62,62,62,62,62,62, 62,62,62,62,62,62,62,62,62,62,62,62,62,62,62,62, 62,62,62,62,62,62,62,62,62,62};static sbyte[]
mapC44=new sbyte[188]{ 61,61,61,61,49,61,61,61,61,61,61,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,61,61,61,61, 49,61,61,49,61,49,49,61,49,61,
61,61,61,61,61,61, 61,61,61,49,61,61,61,61,49,61,49,61,49,49,49,49, 49,49,61,49,49,49,49,61,49,61,49,61,49,61,61,61, 49,61,61,49,61,49,49,61,49,61,49,
61,49,61,49,61, 49,61,61,49,61,49,49,61,61,61,61,49,61,61,61,61, 61,61,61,49,61,61,61,61,49,61,61,61,61,49,61,49, 61,61,61,61,61,61,61,61,61,61,49,61,
61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61,49,49,49,49, 49,61,61,61,49,61,61,61,61,61,49,61,61,61,61,61, 61,61,61,61,61,61,61,61,61,61,61,61};static
 sbyte MapC(int code){ if(code<83527) if(code<70874) if(code<67383) if(code<66560) if(code<65787) return mapC0[code-65536];else if(code<66176) return(sbyte)49;
else return mapC2[code-66176];else if(code<66916) if(code<66718) return(sbyte)61;else return mapC4[code-66718];else if(code<67072) return(sbyte)49;else
 return(sbyte)61;else if(code<68851) if(code<67432) return mapC7[code-67383];else if(code<67584) return(sbyte)49;else return mapC9[code-67584];else if
(code<70498) if(code<69635) return(sbyte)49;else return mapC11[code-69635];else if(code<70784) return(sbyte)49;else return mapC13[code-70784];else if(code
<73728) if(code<71840) if(code<71040) return(sbyte)49;else if(code<71482) return mapC15[code-71040];else return(sbyte)49;else if(code<72384) if(code<71936)
 return mapC17[code-71840];else return(sbyte)49;else if(code<72441) return(sbyte)61;else return(sbyte)49;else if(code<75076) if(code<74650) return(sbyte)61;
else if(code<74880) return(sbyte)49;else return(sbyte)61;else if(code<78895) if(code<77824) return(sbyte)49;else return(sbyte)61;else if(code<82944) return
(sbyte)49;else return(sbyte)61;else if(code<124928) if(code<110594) if(code<93072) if(code<92160) return(sbyte)49;else if(code<92729) return(sbyte)61;
else return mapC30[code-92729];else if(code<94112) if(code<93952) return(sbyte)49;else return mapC32[code-93952];else if(code<110592) return(sbyte)49;
else return(sbyte)61;else if(code<119808) if(code<113664) return(sbyte)49;else if(code<113818) return mapC36[code-113664];else return(sbyte)49;else if
(code<120486) if(code<120146) return mapC38[code-119808];else return(sbyte)61;else if(code<120832) return mapC40[code-120486];else return(sbyte)49;else
 if(code<177973) if(code<126652) if(code<125125) return(sbyte)61;else if(code<126464) return(sbyte)49;else return mapC44[code-126464];else if(code<173783)
 if(code<131072) return(sbyte)49;else return(sbyte)61;else if(code<173824) return(sbyte)49;else return(sbyte)61;else if(code<178208) if(code<177984) return
(sbyte)49;else if(code<178206) return(sbyte)61;else return(sbyte)49;else if(code<194560) if(code<183970) return(sbyte)61;else return(sbyte)49;else if(code
<195102) return(sbyte)61;else return(sbyte)49;}
#endregion
static sbyte Map(int code){if(code<=65535)return map[code/256][code%256];else return MapC(code);}static Table[]NxS=new Table[346]{ new Table(0,0,0,null),
 new Table(27,43,-1,new short[]{1,-1,-1,1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
1,1}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,317,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,
44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,307,3,3,3,308,3,3,3,3,3,309,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,
3,3,3,3,3,3,3,3,3,-1,-1,3,3,301,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,
-1,-1,3,3,3,3,3,287,3,3,3,3,3,3,3,288,289,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,
276,3,277,3,3,3,3,3,278,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,264,
3,3,3,3,3,3,3,3,265,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,248,3,249,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,233,3,3,234,3,3,3,3,3,3,3,235,3,3,236,3,3,3,3,3,-1,-1,
-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,220,3,3,3,3,3,3,3,3,3,221,3,222,3,3,3,3,3,3,-1,-1,-1,-1,3}), new
 Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,198,199,3,3,3,3,3,3,3,3,200,3,201,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,
-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,185,3,186,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,187,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]
{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,171,3,3,172,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,
3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,162,3,3,3,3,3,3,3,163,3,3,3,3,3,3,3,164,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,
3,3,-1,-1,3,3,3,3,3,3,147,3,3,3,148,3,3,3,3,149,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,144,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,120,3,3,3,3,3,3,
3,3,121,3,122,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,110,3,111,3,3,3,3,3,3,3,3,3,3,112,
3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,94,3,95,3,3,3,3,3,96,3,3,97,3,3,3,3,3,-1,-1,
-1,-1,3}), new Table(22,20,-1,new short[]{91,92,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,93}), new Table(41,1,-1,new short[]{90}), new Table(0,
0,-1,null), new Table(53,43,-1,new short[]{89,89,-1,89,-1,-1,89,-1,-1,-1,-1,89,89,89,89,-1,-1,89,89,89,89,89,89,89,89,89,89,89,89,89,89,89,89,89,89,89,
89,89,-1,-1,-1,-1,89}), new Table(28,1,-1,new short[]{88}), new Table(0,0,-1,null), new Table(52,12,-1,new short[]{68,-1,-1,68,-1,-1,-1,-1,-1,-1,-1,68}),
 new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,
0,-1,null), new Table(41,1,-1,new short[]{87}), new Table(40,2,-1,new short[]{85,86}), new Table(41,1,-1,new short[]{84}), new Table(41,2,-1,new short[]
{82,83}), new Table(41,1,-1,new short[]{81}), new Table(41,4,-1,new short[]{79,-1,-1,80}), new Table(41,5,-1,new short[]{77,-1,-1,-1,78}), new Table(41,
1,-1,new short[]{76}), new Table(41,1,-1,new short[]{75}), new Table(24,46,332,new short[]{-1,332,332,-1,332,332,-1,332,332,332,332,332,332,332,332,332,
332,332,332,332,332,332,332,332,332,332,332,333,332,332,332,332,332,-1,332,332,332,332,332,332,332,332,332,332,-1,-1}), new Table(52,49,-1,new short[]
{45,-1,-1,45,50,-1,-1,-1,-1,-1,-1,45,51,325,49,49,-1,-1,-1,-1,325,-1,-1,-1,-1,49,-1,-1,-1,-1,-1,-1,50,51,-1,-1,49,49,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,326}),
 new Table(24,46,330,new short[]{-1,330,330,-1,330,330,-1,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,331,330,330,
330,330,330,73,330,330,330,330,330,330,330,330,330,330,-1,-1}), new Table(53,43,-1,new short[]{71,71,-1,71,329,-1,71,71,71,-1,-1,71,71,71,71,-1,-1,71,
71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,-1,-1,-1,-1,71}), new Table(52,49,-1,new short[]{45,327,-1,45,50,-1,-1,-1,-1,-1,-1,45,51,325,
49,49,-1,-1,-1,-1,325,-1,-1,-1,-1,49,-1,-1,-1,-1,-1,-1,50,51,-1,-1,49,49,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,326}), new Table(0,0,-1,null), new Table(64,22,
-1,new short[]{69,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,69}), new Table(56,29,-1,new short[]{69,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,
-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,69}), new Table(52,37,-1,new short[]{53,-1,53,53,50,-1,-1,-1,-1,-1,-1,53,51,53,-1,53,-1,-1,-1,-1,53,-1,
-1,-1,-1,53,53,-1,-1,-1,53,53,50,51,-1,-1,53}), new Table(52,37,-1,new short[]{54,-1,54,54,50,-1,-1,-1,-1,-1,-1,54,51,54,-1,54,-1,-1,-1,-1,54,-1,-1,-1,
-1,54,54,-1,-1,-1,54,54,50,51,-1,-1,54}), new Table(52,37,-1,new short[]{55,-1,55,55,50,-1,-1,-1,-1,-1,-1,55,51,55,-1,55,-1,-1,-1,-1,55,-1,-1,-1,-1,55,
55,-1,-1,-1,55,55,50,51,-1,-1,55}), new Table(52,37,-1,new short[]{56,-1,56,56,50,-1,-1,-1,-1,-1,-1,56,51,56,-1,56,-1,-1,-1,-1,56,-1,-1,-1,-1,56,56,-1,
-1,-1,56,56,50,51,-1,-1,56}), new Table(52,37,-1,new short[]{57,-1,57,57,50,-1,-1,-1,-1,-1,-1,57,51,57,-1,57,-1,-1,-1,-1,57,-1,-1,-1,-1,57,57,-1,-1,-1,
57,57,50,51,-1,-1,57}), new Table(52,37,-1,new short[]{58,-1,58,58,50,-1,-1,-1,-1,-1,-1,58,51,58,-1,58,-1,-1,-1,-1,58,-1,-1,-1,-1,58,58,-1,-1,-1,58,58,
50,51,-1,-1,58}), new Table(52,37,-1,new short[]{59,-1,59,59,50,-1,-1,-1,-1,-1,-1,59,51,59,-1,59,-1,-1,-1,-1,59,-1,-1,-1,-1,59,59,-1,-1,-1,59,59,50,51,
-1,-1,59}), new Table(52,37,-1,new short[]{60,-1,60,60,50,-1,-1,-1,-1,-1,-1,60,51,60,-1,60,-1,-1,-1,-1,60,-1,-1,-1,-1,60,60,-1,-1,-1,60,60,50,51,-1,-1,
60}), new Table(52,37,-1,new short[]{61,-1,61,61,50,-1,-1,-1,-1,-1,-1,61,51,61,-1,61,-1,-1,-1,-1,61,-1,-1,-1,-1,61,61,-1,-1,-1,61,61,50,51,-1,-1,61}),
 new Table(52,37,-1,new short[]{62,-1,62,62,50,-1,-1,-1,-1,-1,-1,62,51,62,-1,62,-1,-1,-1,-1,62,-1,-1,-1,-1,62,62,-1,-1,-1,62,62,50,51,-1,-1,62}), new Table(52,
37,-1,new short[]{63,-1,63,63,50,-1,-1,-1,-1,-1,-1,63,51,63,-1,63,-1,-1,-1,-1,63,-1,-1,-1,-1,63,63,-1,-1,-1,63,63,50,51,-1,-1,63}), new Table(52,37,-1,
new short[]{64,-1,64,64,50,-1,-1,-1,-1,-1,-1,64,51,64,-1,64,-1,-1,-1,-1,64,-1,-1,-1,-1,64,64,-1,-1,-1,64,64,50,51,-1,-1,64}), new Table(52,37,-1,new short[]
{65,-1,65,65,50,-1,-1,-1,-1,-1,-1,65,51,65,-1,65,-1,-1,-1,-1,65,-1,-1,-1,-1,65,65,-1,-1,-1,65,65,50,51,-1,-1,65}), new Table(52,37,-1,new short[]{66,-1,
66,66,50,-1,-1,-1,-1,-1,-1,66,51,66,-1,66,-1,-1,-1,-1,66,-1,-1,-1,-1,66,66,-1,-1,-1,66,66,50,51,-1,-1,66}), new Table(52,37,-1,new short[]{67,-1,67,67,
50,-1,-1,-1,-1,-1,-1,67,51,67,-1,67,-1,-1,-1,-1,67,-1,-1,-1,-1,67,67,-1,-1,-1,67,67,50,51,-1,-1,67}), new Table(56,30,-1,new short[]{50,-1,-1,-1,-1,-1,
-1,-1,51,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,50,51}), new Table(52,38,-1,new short[]{68,-1,-1,68,-1,-1,-1,-1,-1,-1,-1,68,-1,325,49,
49,-1,-1,-1,-1,325,-1,-1,-1,-1,49,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,49,49}), new Table(0,0,-1,null), new Table(52,38,-1,new short[]{70,-1,-1,70,-1,-1,-1,-1,
-1,-1,-1,70,-1,-1,49,49,-1,-1,-1,-1,-1,-1,-1,-1,-1,49,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,49,49}), new Table(52,44,-1,new short[]{71,71,71,71,71,-1,-1,71,71,
71,71,71,71,71,71,71,-1,-1,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,71,-1,-1,-1,-1,71}), new Table(0,0,-1,null), new Table(0,0,-1,null),
 new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,
0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(0,0,-1,null),
 new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(68,2,89,new short[]{-1,-1}), new Table(0,0,-1,null), new Table(68,2,91,new short[]{-1,-1}),
 new Table(0,0,-1,null), new Table(0,0,-1,null), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,109,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,104,3,3,3,3,3,3,3,3,3,3,3,-1,-1,
-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,101,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,
44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,98,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,99,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,
3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,100,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,
102,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,103,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,105,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,106,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,107,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,108,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,119,3,3,3,3,3,3,3,3,
3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,117,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,113,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,114,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,115,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,116,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,118,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new
 Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,132,3,133,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,127,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,123,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,124,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,3,3,3,3,125,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,126,
3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,128,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,129,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,130,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,131,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,3,138,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,134,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,135,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,136,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,137,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,
3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,139,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,140,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,3,141,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,142,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,143,3,3,3,3,3,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,145,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,146,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,160,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,153,
3,3,3,3,154,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,150,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,151,3,3,3,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,152,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,157,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,155,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,156,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,158,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,159,3,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,161,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,
3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,169,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,167,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,3,165,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,166,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,168,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,170,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,179,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,173,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
174,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,175,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,176,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,177,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,178,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,180,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,181,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
3,3,182,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,183,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,184,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,193,3,3,194,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,
3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,102,3,3,3,3,3,192,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,188,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,
3,3,189,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,190,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
191,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,
3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,
-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,196,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,
3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,195,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,
3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,197,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,217,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,216,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,205,3,3,3,3,3,3,3,3,206,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,
-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,202,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,
3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,203,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,
3,3,3,3,3,3,-1,-1,3,3,204,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,210,3,3,
3,3,3,3,3,211,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,207,3,3,3,3,3,
3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,208,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,
3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,209,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,
44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,214,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,212,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,
3,3,3,3,3,3,3,3,3,3,3,213,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,215,
3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,218,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,219,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,232,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,225,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,223,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,224,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,226,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,227,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,228,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,3,3,3,3,3,3,3,3,3,3,229,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,230,
3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,231,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,246,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,243,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,240,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,3,3,3,3,3,3,3,3,3,3,237,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,
238,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,239,3,3,3,3,3,3,3,
3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,241,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,242,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,244,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,3,245,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,247,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new
 Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,254,3,3,3,3,3,3,3,3,3,255,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,250,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,251,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,252,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,253,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,260,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,256,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,257,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,258,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,259,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,261,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
3,262,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,263,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,266,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,
3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,267,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,3,268,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,269,3,3,3,3,3,3,3,3,270,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,
3,3,274,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,271,3,3,3,3,3,
3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,272,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,
3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,273,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,
44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,275,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,
3,3,3,3,3,3,285,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,280,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,279,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,
3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,281,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,
44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,282,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,283,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,284,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,286,3,
3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,295,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,291,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,290,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,292,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,293,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
294,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,296,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,297,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,298,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,299,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,
3,3,3,3,3,-1,-1,3,3,300,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,
302,3,3,3,3,3,3,303,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,304,3,3,3,
3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}),
 new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,305,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,
new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,306,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,
3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,-1,-1,3,3,314,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,312,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,310,
3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,311,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new
 Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,313,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new
 short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,
-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,315,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,
3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,316,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,318,3,3,3,319,3,3,3,3,3,3,
3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,322,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,
-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,320,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,
44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,321,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,
3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,
3,3,3,3,3,3,-1,-1,3,3,323,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(52,44,-1,new short[]{3,3,3,3,3,-1,-1,3,3,3,3,3,3,3,3,3,-1,-1,
3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,-1,-1,-1,-1,3}), new Table(68,65,3,new short[]{1,1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,3,3,21,22,
23,24,3,1,25,26,1,27,28,29,30,31,32,33,34,35,36,37,38,39,40,41,42,43,44,23,1,23,45,3,3,45,3,46,47,3,3,3,23,48}), new Table(44,20,-1,new short[]{328,328,
-1,-1,-1,-1,-1,-1,70,-1,-1,70,-1,-1,-1,-1,-1,-1,-1,70}), new Table(52,12,-1,new short[]{68,-1,-1,68,-1,-1,-1,-1,-1,-1,-1,68}), new Table(52,37,-1,new short[]
{52,-1,52,52,-1,-1,-1,-1,-1,-1,-1,52,-1,52,-1,52,-1,-1,-1,-1,52,-1,-1,-1,-1,52,52,-1,-1,-1,52,52,-1,-1,-1,-1,52}), new Table(52,12,-1,new short[]{70,-1,
-1,70,-1,-1,-1,-1,-1,-1,-1,70}), new Table(40,18,329,new short[]{-1,329,329,329,329,329,329,329,329,329,329,329,329,329,329,329,329,72}), new Table(24,
46,330,new short[]{-1,330,330,-1,330,330,-1,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,330,331,330,330,330,330,330,73,
330,330,330,330,330,330,330,330,330,330,-1,-1}), new Table(68,2,330,new short[]{-1,-1}), new Table(48,1,-1,new short[]{74}), new Table(52,33,332,new short[]
{335,336,332,332,337,332,332,332,332,332,332,335,332,332,332,332,-1,-1,332,332,332,332,332,332,332,332,332,332,332,332,332,332,334}), new Table(48,41,
-1,new short[]{74,-1,-1,-1,342,-1,342,342,-1,-1,-1,-1,-1,-1,-1,342,-1,342,-1,342,-1,-1,-1,-1,342,-1,-1,-1,-1,342,342,-1,-1,-1,342,342,-1,-1,-1,-1,342}),
 new Table(48,16,-1,new short[]{74,-1,-1,-1,345,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,345}), new Table(48,41,-1,new short[]{74,-1,-1,-1,344,-1,344,344,-1,-1,-1,
-1,-1,-1,-1,344,-1,344,-1,344,-1,-1,-1,-1,344,-1,-1,-1,-1,344,344,-1,-1,-1,344,344,-1,-1,-1,-1,344}), new Table(48,41,-1,new short[]{74,-1,-1,-1,338,-1,
338,338,-1,-1,-1,-1,-1,-1,-1,338,-1,338,-1,338,-1,-1,-1,-1,338,-1,-1,-1,-1,338,338,-1,-1,-1,338,338,-1,-1,-1,-1,338}), new Table(52,37,-1,new short[]{339,
-1,339,339,-1,-1,-1,-1,-1,-1,-1,339,-1,339,-1,339,-1,-1,-1,-1,339,-1,-1,-1,-1,339,339,-1,-1,-1,339,339,-1,-1,-1,-1,339}), new Table(52,37,-1,new short[]
{340,-1,340,340,-1,-1,-1,-1,-1,-1,-1,340,-1,340,-1,340,-1,-1,-1,-1,340,-1,-1,-1,-1,340,340,-1,-1,-1,340,340,-1,-1,-1,-1,340}), new Table(52,37,-1,new short[]
{341,-1,341,341,-1,-1,-1,-1,-1,-1,-1,341,-1,341,-1,341,-1,-1,-1,-1,341,-1,-1,-1,-1,341,341,-1,-1,-1,341,341,-1,-1,-1,-1,341}), new Table(52,37,-1,new short[]
{342,-1,342,342,-1,-1,-1,-1,-1,-1,-1,342,-1,342,-1,342,-1,-1,-1,-1,342,-1,-1,-1,-1,342,342,-1,-1,-1,342,342,-1,-1,-1,-1,342}), new Table(52,37,-1,new short[]
{343,-1,343,343,-1,-1,-1,-1,-1,-1,-1,343,-1,343,-1,343,-1,-1,-1,-1,343,-1,-1,-1,-1,343,343,-1,-1,-1,343,343,-1,-1,-1,-1,343}), new Table(52,37,-1,new short[]
{344,-1,344,344,-1,-1,-1,-1,-1,-1,-1,344,-1,344,-1,344,-1,-1,-1,-1,344,-1,-1,-1,-1,344,344,-1,-1,-1,344,344,-1,-1,-1,-1,344}), new Table(52,37,-1,new short[]
{332,-1,332,332,-1,-1,-1,-1,-1,-1,-1,332,-1,332,-1,332,-1,-1,-1,-1,332,-1,-1,-1,-1,332,332,-1,-1,-1,332,332,-1,-1,-1,-1,332}), new Table(52,12,-1,new short[]
{332,-1,-1,-1,-1,-1,-1,-1,-1,-1,-1,332}),};int NextState(){if(code==ScanBuff.EndOfFile)return eofNum;else unchecked{int rslt;int idx=Map(code)-NxS[state].min;
if(idx<0)idx+=69;if((uint)idx>=(uint)NxS[state].rng)rslt=NxS[state].dflt;else rslt=NxS[state].nxt[idx];return rslt;}}
#endregion
#if BACKUP
 struct Context{public int bPos;public int rPos; public int cCol;public int lNum; public int state;public int cChr;}private Context ctx=new Context();
#endif // BACKUP
 struct BufferContext{internal ScanBuff buffSv;internal int chrSv;internal int cColSv;internal int lNumSv;}/// <summary>
/// This method creates a buffer context record from
/// the current buffer object, together with some
/// scanner state values. 
/// </summary>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]BufferContext MkBuffCtx(){BufferContext rslt;rslt.buffSv=this.buffer;rslt.chrSv
=this.code;rslt.cColSv=this.cCol;rslt.lNumSv=this.lNum;return rslt;}/// <summary>
/// This method restores the buffer value and allied
/// scanner state from the given context record value.
/// </summary>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]void RestoreBuffCtx(BufferContext value){this.buffer=value.buffSv;this.code
=value.chrSv;this.cCol=value.cColSv;this.lNum=value.lNumSv;}
#if !NOFILES
internal Scanner(Stream file){SetSource(file,-1);}public Scanner(Stream file,string codepage){SetSource(file,CodePageHandling.GetCodePage(codepage));}
#endif // !NOFILES
internal Scanner(){}private int readPos;void GetCode(){if(code=='\n'){cCol=-1;lNum++;}readPos=buffer.Pos; code=buffer.Read();if(code>ScanBuff.EndOfFile)
{
#if (!BYTEMODE)
if(code>=0xD800&&code<=0xDBFF){int next=buffer.Read();if(next<0xDC00||next>0xDFFF)code=ScanBuff.UnicodeReplacementChar;else code=(0x10000+((code&0x3FF)
<<10)+(next&0x3FF));}
#endif
cCol++;}}void MarkToken(){
#if (!PERSIST)
buffer.Mark();
#endif
tokPos=readPos;tokLin=lNum;tokCol=cCol;}void MarkEnd(){tokTxt=null;tokEPos=readPos;tokELin=lNum;tokECol=cCol;}[SuppressMessage("Microsoft.Performance",
"CA1811:AvoidUncalledPrivateCode")]int Peek(){int rslt,codeSv=code,cColSv=cCol,lNumSv=lNum,bPosSv=buffer.Pos;GetCode();rslt=code;lNum=lNumSv;cCol=cColSv;
code=codeSv;buffer.Pos=bPosSv;return rslt;}/// <summary>
/// Create and initialize a StringBuff buffer object for this scanner
/// </summary>
/// <param name="source">the input string</param>
/// <param name="offset">starting offset in the string</param>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]public void SetSource(string source,int offset){this.buffer=ScanBuff.GetBuffer(source);
this.buffer.Pos=offset;this.lNum=0;this.code='\n'; GetCode();}/// <summary>
/// Create and initialize a LineBuff buffer object for this scanner
/// </summary>
/// <param name="source">the list of input strings</param>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]public void SetSource(IList<string>source){this.buffer=ScanBuff.GetBuffer(source);
this.code='\n'; this.lNum=0;GetCode();}
#if !NOFILES        
/// <summary>
/// Create and initialize a StreamBuff buffer object for this scanner.
/// StreamBuff is buffer for 8-bit byte files.
/// </summary>
/// <param name="source">the input byte stream</param>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]public void SetSource(Stream source){this.buffer=ScanBuff.GetBuffer(source);
this.lNum=0;this.code='\n'; GetCode();}
#if !BYTEMODE
/// <summary>
/// Create and initialize a TextBuff buffer object for this scanner.
/// TextBuff is a buffer for encoded unicode files.
/// </summary>
/// <param name="source">the input text file</param>
/// <param name="fallbackCodePage">Code page to use if file has
/// no BOM. For 0, use machine default; for -1, 8-bit binary</param>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]public void SetSource(Stream source,int fallbackCodePage){this.buffer=ScanBuff.GetBuffer(source,
fallbackCodePage);this.lNum=0;this.code='\n'; GetCode();}
#endif // !BYTEMODE
#endif // !NOFILES
#if BABEL
 public int GetNext(ref int state,out int start,out int end){Tokens next;int s,e;s=state; EolState=state;next=(Tokens)Scan();state=EolState;e=state; start
=tokPos;end=tokEPos-1; return(int)next;}
#endif // BABEL
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")][SuppressMessage("Microsoft.Naming","CA1709:IdentifiersShouldBeCasedCorrectly",
MessageId="yylex")][SuppressMessage("Microsoft.Naming","CA1704:IdentifiersShouldBeSpelledCorrectly",MessageId="yylex")]public override int yylex(){ int
 next;do{next=Scan();}while(next>=parserMax);return next;}[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]int yypos{get{return
 tokPos;}}[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]int yyline{get{return tokLin;}}[SuppressMessage("Microsoft.Performance",
"CA1811:AvoidUncalledPrivateCode")]int yycol{get{return tokCol;}}[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")][SuppressMessage("Microsoft.Naming",
"CA1709:IdentifiersShouldBeCasedCorrectly",MessageId="yytext")][SuppressMessage("Microsoft.Naming","CA1704:IdentifiersShouldBeSpelledCorrectly",MessageId
="yytext")]public string yytext{get{if(tokTxt==null)tokTxt=buffer.GetString(tokPos,tokEPos);return tokTxt;}}/// <summary>
/// Discards all but the first "n" codepoints in the recognized pattern.
/// Resets the buffer position so that only n codepoints have been consumed;
/// yytext is also re-evaluated. 
/// </summary>
/// <param name="n">The number of codepoints to consume</param>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]void yyless(int n){buffer.Pos=tokPos; cCol=tokCol-1;GetCode(); lNum=tokLin;
 for(int i=0;i<n;i++)GetCode();MarkEnd();}/// <summary>
/// Removes the last "n" code points from the pattern.
/// </summary>
/// <param name="n">The number to remove</param>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]void _yytrunc(int n){yyless(yyleng-n);}/// <summary>
/// The length of the pattern in codepoints (not the same as 
/// string-length if the pattern contains any surrogate pairs).
/// </summary>
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")][SuppressMessage("Microsoft.Naming","CA1709:IdentifiersShouldBeCasedCorrectly",
MessageId="yyleng")][SuppressMessage("Microsoft.Naming","CA1704:IdentifiersShouldBeSpelledCorrectly",MessageId="yyleng")]public int yyleng{get{if(tokELin
==tokLin)return tokECol-tokCol;else
#if BYTEMODE
return tokEPos-tokPos;
#else
{int ch;int count=0;int save=buffer.Pos;buffer.Pos=tokPos;do{ch=buffer.Read();if(!char.IsHighSurrogate((char)ch))count++;}while(buffer.Pos<tokEPos&&ch
!=ScanBuff.EndOfFile);buffer.Pos=save;return count;}
#endif // BYTEMODE
}}[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]internal int YY_START{get{return currentScOrd;}set{currentScOrd=value;currentStart
=startState[value];}}[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]internal void BEGIN(int next){currentScOrd=next;currentStart
=startState[next];} int Scan(){for(;;){int next;
#if LEFTANCHORS
for(;;){ state=((cCol==0)?anchorState[currentScOrd]:currentStart);if((next=NextState())!=goStart)break; GetCode();}
#else // !LEFTANCHORS
state=currentStart;while((next=NextState())==goStart){ GetCode();}
#endif // LEFTANCHORS                    
 MarkToken();state=next;GetCode();
#if BACKUP
bool contextSaved=false;while((next=NextState())>eofNum){ if(state<=maxAccept&&next>maxAccept){ SaveStateAndPos(ref ctx);contextSaved=true;}state=next;
GetCode();}if(state>maxAccept&&contextSaved)RestoreStateAndPos(ref ctx);
#else  // BACKUP
while((next=NextState())>eofNum){ state=next;GetCode();}
#endif // BACKUP
if(state<=maxAccept){MarkEnd();
#region ActionSwitch
#pragma warning disable 162, 1522
switch(state){case eofNum:switch(currentStart){case 324:return-2;break;}if(yywrap())return(int)Tokens.EOF;break;case 1: UpdatePosition(yytext);break;case
 2: case 3: case 4: case 5: case 6: case 7: case 8: case 9: case 10: case 11: case 12: case 13: case 14: case 15: case 16: case 17: case 18: case 19: case
 20: case 94: case 95: case 96: case 97: case 98: case 99: case 101: case 102: case 104: case 105: case 106: case 107: case 110: case 111: case 112: case
 113: case 114: case 115: case 117: case 120: case 121: case 122: case 123: case 124: case 125: case 127: case 128: case 129: case 130: case 132: case
 133: case 134: case 135: case 136: case 138: case 139: case 140: case 141: case 142: case 144: case 145: case 147: case 148: case 149: case 150: case
 151: case 153: case 154: case 155: case 157: case 158: case 160: case 162: case 163: case 164: case 165: case 167: case 169: case 171: case 172: case
 173: case 174: case 175: case 176: case 177: case 179: case 180: case 181: case 182: case 183: case 185: case 186: case 187: case 188: case 189: case
 190: case 193: case 194: case 196: case 198: case 199: case 200: case 201: case 202: case 203: case 205: case 206: case 207: case 208: case 210: case
 211: case 212: case 214: case 217: case 218: case 220: case 221: case 222: case 223: case 225: case 226: case 227: case 228: case 229: case 230: case
 233: case 234: case 235: case 236: case 237: case 238: case 240: case 241: case 243: case 244: case 246: case 248: case 249: case 250: case 251: case
 252: case 254: case 255: case 256: case 257: case 258: case 260: case 261: case 262: case 264: case 267: case 268: case 269: case 270: case 271: case
 272: case 274: case 276: case 277: case 278: case 280: case 281: case 282: case 283: case 285: case 287: case 288: case 289: case 291: case 292: case
 293: case 295: case 296: case 297: case 298: case 299: case 301: case 302: case 304: case 305: case 307: case 308: case 309: case 310: case 312: case
 314: case 315: case 317: case 318: case 319: case 320: case 322: UpdatePosition(yytext);return 425;break;case 21: UpdatePosition(yytext);return 444;break;
case 22: UpdatePosition(yytext);return 442;break;case 23: case 24: case 44: case 46: case 47: UpdatePosition(yytext);return-1;break;case 25: UpdatePosition(yytext);
return 478;break;case 26: UpdatePosition(yytext);return 476;break;case 27: UpdatePosition(yytext);return 462;break;case 28: UpdatePosition(yytext);return
 460;break;case 29: UpdatePosition(yytext);return 459;break;case 30: UpdatePosition(yytext);return 458;break;case 31: UpdatePosition(yytext);return 457;
break;case 32: UpdatePosition(yytext);return 456;break;case 33: UpdatePosition(yytext);return 455;break;case 34: UpdatePosition(yytext);return 454;break;
case 35: UpdatePosition(yytext);return 453;break;case 36: UpdatePosition(yytext);return 452;break;case 37: UpdatePosition(yytext);return 434;break;case
 38: UpdatePosition(yytext);return 449;break;case 39: UpdatePosition(yytext);return 446;break;case 40: UpdatePosition(yytext);return 440;break;case 41:
 UpdatePosition(yytext);return 437;break;case 42: UpdatePosition(yytext);return 431;break;case 43: UpdatePosition(yytext);return 429;break;case 45: case
 48: case 50: case 51: case 52: case 53: case 54: case 55: case 56: case 57: case 58: case 59: case 60: case 61: case 62: case 63: case 64: case 65: case
 66: case 67: case 69: UpdatePosition(yytext);return 463;break;case 49: case 68: case 70: UpdatePosition(yytext);return 464;break;case 71: UpdatePosition(yytext);
return 398;break;case 72: UpdatePosition(yytext);return 424;break;case 73: UpdatePosition(yytext);return 426;break;case 74: UpdatePosition(yytext);return
 427;break;case 75: UpdatePosition(yytext);return 428;break;case 76: UpdatePosition(yytext);return 430;break;case 77: UpdatePosition(yytext);return 436;
break;case 78: UpdatePosition(yytext);return 435;break;case 79: UpdatePosition(yytext);return 439;break;case 80: UpdatePosition(yytext);return 438;break;
case 81: UpdatePosition(yytext);return 445;break;case 82: UpdatePosition(yytext);return 448;break;case 83: UpdatePosition(yytext);return 447;break;case
 84: UpdatePosition(yytext);return 432;break;case 85: UpdatePosition(yytext);return 450;break;case 86: UpdatePosition(yytext);return 451;break;case 87:
 UpdatePosition(yytext);return 433;break;case 88: UpdatePosition(yytext);return 461;break;case 89: UpdatePosition(yytext);return 479;break;case 90: UpdatePosition(yytext);
return 441;break;case 91: UpdatePosition(yytext);return 480;break;case 92: if(!_TryReadUntilBlockEnd("*/")){UpdatePosition(yytext);return-1;}UpdatePosition(yytext);
return 481;break;case 93: UpdatePosition(yytext);return 443;break;case 100: UpdatePosition(yytext);return 408;break;case 103: UpdatePosition(yytext);return
 420;break;case 108: UpdatePosition(yytext);return 475;break;case 109: UpdatePosition(yytext);return 469;break;case 116: UpdatePosition(yytext);return
 404;break;case 118: UpdatePosition(yytext);return 467;break;case 119: UpdatePosition(yytext);return 489;break;case 126: UpdatePosition(yytext);return
 492;break;case 131: UpdatePosition(yytext);return 484;break;case 137: UpdatePosition(yytext);return 493;break;case 143: UpdatePosition(yytext);return
 494;break;case 146: UpdatePosition(yytext);return 417;break;case 152: UpdatePosition(yytext);return 418;break;case 156: UpdatePosition(yytext);return
 397;break;case 159: UpdatePosition(yytext);return 414;break;case 161: UpdatePosition(yytext);return 416;break;case 166: UpdatePosition(yytext);return
 412;break;case 168: UpdatePosition(yytext);return 423;break;case 170: UpdatePosition(yytext);return 406;break;case 178: UpdatePosition(yytext);return
 498;break;case 184: UpdatePosition(yytext);return 482;break;case 191: UpdatePosition(yytext);return 401;break;case 192: UpdatePosition(yytext);return
 473;break;case 195: UpdatePosition(yytext);return 422;break;case 197: UpdatePosition(yytext);return 470;break;case 204: UpdatePosition(yytext);return
 411;break;case 209: UpdatePosition(yytext);return 496;break;case 213: UpdatePosition(yytext);return 487;break;case 215: UpdatePosition(yytext);return
 405;break;case 216: UpdatePosition(yytext);return 490;break;case 219: UpdatePosition(yytext);return 413;break;case 224: UpdatePosition(yytext);return
 421;break;case 231: UpdatePosition(yytext);return 396;break;case 232: UpdatePosition(yytext);return 403;break;case 239: UpdatePosition(yytext);return
 485;break;case 242: UpdatePosition(yytext);return 474;break;case 245: UpdatePosition(yytext);return 499;break;case 247: UpdatePosition(yytext);return
 407;break;case 253: UpdatePosition(yytext);return 409;break;case 259: UpdatePosition(yytext);return 402;break;case 263: UpdatePosition(yytext);return
 410;break;case 265: UpdatePosition(yytext);return 466;break;case 266: UpdatePosition(yytext);return 415;break;case 273: UpdatePosition(yytext);return
 488;break;case 275: UpdatePosition(yytext);return 495;break;case 279: UpdatePosition(yytext);return 477;break;case 284: UpdatePosition(yytext);return
 497;break;case 286: UpdatePosition(yytext);return 483;break;case 290: UpdatePosition(yytext);return 399;break;case 294: UpdatePosition(yytext);return
 419;break;case 300: UpdatePosition(yytext);return 500;break;case 303: UpdatePosition(yytext);return 400;break;case 306: UpdatePosition(yytext);return
 472;break;case 311: UpdatePosition(yytext);return 468;break;case 313: UpdatePosition(yytext);return 486;break;case 316: UpdatePosition(yytext);return
 491;break;case 321: UpdatePosition(yytext);return 471;break;case 323: UpdatePosition(yytext);return 501;break;default:break;}
#pragma warning restore 162, 1522
#endregion
}}}
#if BACKUP
void SaveStateAndPos(ref Context ctx){ctx.bPos=buffer.Pos;ctx.rPos=readPos;ctx.cCol=cCol;ctx.lNum=lNum;ctx.state=state;ctx.cChr=code;}void RestoreStateAndPos(ref
 Context ctx){buffer.Pos=ctx.bPos;readPos=ctx.rPos;cCol=ctx.cCol;lNum=ctx.lNum;state=ctx.state;code=ctx.cChr;}
#endif  // BACKUP
#if STACK        
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]internal void yy_clear_stack(){scStack.Clear();}[SuppressMessage("Microsoft.Performance",
"CA1811:AvoidUncalledPrivateCode")]internal int yy_top_state(){return scStack.Peek();}internal void yy_push_state(int state){scStack.Push(currentScOrd);
BEGIN(state);}internal void yy_pop_state(){ if(scStack.Count>0){int newSc=scStack.Pop();BEGIN(newSc);}}
#endif // STACK
[SuppressMessage("Microsoft.Performance","CA1811:AvoidUncalledPrivateCode")]internal void ECHO(){Console.Out.Write(yytext);}}[Serializable]public class
 BufferException:Exception{public BufferException(){}public BufferException(string message):base(message){}public BufferException(string message,Exception
 innerException):base(message,innerException){}protected BufferException(SerializationInfo info,StreamingContext context):base(info,context){}}public abstract
 class ScanBuff{private string fileNm;public const int EndOfFile=-1;public const int UnicodeReplacementChar=0xFFFD;public bool IsFile{get{return(fileNm
!=null);}}public string FileName{get{return fileNm;}set{fileNm=value;}}public abstract int Pos{get;set;}public abstract int Read();public virtual void
 Mark(){}public abstract string GetString(int begin,int limit);public static ScanBuff GetBuffer(string source){return new StringBuffer(source);}public
 static ScanBuff GetBuffer(IList<string>source){return new LineBuffer(source);}
#if (!NOFILES)
public static ScanBuff GetBuffer(Stream source){return new BuildBuffer(source);}
#if (!BYTEMODE)
public static ScanBuff GetBuffer(Stream source,int fallbackCodePage){return new BuildBuffer(source,fallbackCodePage);}
#endif // !BYTEMODE
#endif // !NOFILES
}
#region Buffer classes
/// <summary>
/// This class reads characters from a single string as
/// required, for example, by Visual Studio language services
/// </summary>
sealed class StringBuffer:ScanBuff{string str; int bPos; int sLen;public StringBuffer(string source){this.str=source;this.sLen=source.Length;this.FileName
=null;}public override int Read(){if(bPos<sLen)return str[bPos++];else if(bPos==sLen){bPos++;return'\n';} else{bPos++;return EndOfFile;}}public override
 string GetString(int begin,int limit){ if(limit>sLen)limit=sLen;if(limit<=begin)return"";else return str.Substring(begin,limit-begin);}public override
 int Pos{get{return bPos;}set{bPos=value;}}public override string ToString(){return"StringBuffer";}} sealed class LineBuffer:ScanBuff{IList<string>line;
 int numLines; string curLine; int cLine; int curLen; int curLineStart; int curLineEnd; int maxPos; int cPos; public LineBuffer(IList<string>lineList)
{line=lineList;numLines=line.Count;cPos=curLineStart=0;curLine=(numLines>0?line[0]:"");maxPos=curLineEnd=curLen=curLine.Length;cLine=1;FileName=null;}
public override int Read(){if(cPos<curLineEnd)return curLine[cPos++-curLineStart];if(cPos++==curLineEnd)return'\n';if(cLine>=numLines)return EndOfFile;
curLine=line[cLine];curLen=curLine.Length;curLineStart=curLineEnd+1;curLineEnd=curLineStart+curLen;if(curLineEnd>maxPos)maxPos=curLineEnd;cLine++;return
 curLen>0?curLine[0]:'\n';} private int cachedPosition;private int cachedIxdex;private int cachedLineStart; private void findIndex(int pos,out int ix,
out int lstart){if(pos>=cachedPosition){ix=cachedIxdex;lstart=cachedLineStart;}else{ix=lstart=0;}while(ix<numLines){int len=line[ix].Length+1;if(pos<lstart
+len)break;lstart+=len;ix++;}cachedPosition=pos;cachedIxdex=ix;cachedLineStart=lstart;}public override string GetString(int begin,int limit){if(begin>=
maxPos||limit<=begin)return"";int endIx,begIx,endLineStart,begLineStart;findIndex(begin,out begIx,out begLineStart);int begCol=begin-begLineStart;findIndex(limit,
out endIx,out endLineStart);int endCol=limit-endLineStart;string s=line[begIx];if(begIx==endIx){ return(endCol<=s.Length)?s.Substring(begCol,endCol-begCol)
:s.Substring(begCol)+"\n";} StringBuilder sb=new StringBuilder();if(begCol<s.Length)sb.Append(s.Substring(begCol));for(;;){sb.Append("\n");s=line[++begIx];
if(begIx>=endIx)break;sb.Append(s);}if(endCol<=s.Length){sb.Append(s.Substring(0,endCol));}else{sb.Append(s);sb.Append("\n");}return sb.ToString();}public
 override int Pos{get{return cPos;}set{cPos=value;findIndex(cPos,out cLine,out curLineStart); curLine=(cLine<numLines?line[cLine++]:"");curLineEnd=curLineStart
+curLine.Length;}}public override string ToString(){return"LineBuffer";}}
#if (!NOFILES)
 class BuildBuffer:ScanBuff{ class BufferElement{StringBuilder bldr=new StringBuilder();StringBuilder next=new StringBuilder();int minIx;int maxIx;int
 brkIx;bool appendToNext;internal BufferElement(){}internal int MaxIndex{get{return maxIx;}} internal char this[int index]{get{if(index<minIx||index>=
maxIx)throw new BufferException("Index was outside data buffer");else if(index<brkIx)return bldr[index-minIx];else return next[index-brkIx];}}internal
 void Append(char[]block,int count){maxIx+=count;if(appendToNext)this.next.Append(block,0,count);else{this.bldr.Append(block,0,count);brkIx=maxIx;appendToNext
=true;}}internal string GetString(int start,int limit){if(limit<=start)return"";if(start>=minIx&&limit<=maxIx)if(limit<brkIx) return bldr.ToString(start
-minIx,limit-start);else if(start>=brkIx) return next.ToString(start-brkIx,limit-start);else return bldr.ToString(start-minIx,brkIx-start)+next.ToString(0,
limit-brkIx);else throw new BufferException("String was outside data buffer");}internal void Mark(int limit){if(limit>brkIx+16){StringBuilder temp=bldr;
bldr=next;next=temp;next.Length=0;minIx=brkIx;brkIx=maxIx;}}}BufferElement data=new BufferElement();int bPos; BlockReader NextBlk; private string EncodingName
{get{StreamReader rdr=NextBlk.Target as StreamReader;return(rdr==null?"raw-bytes":rdr.CurrentEncoding.BodyName);}}public BuildBuffer(Stream stream){FileStream
 fStrm=(stream as FileStream);if(fStrm!=null)FileName=fStrm.Name;NextBlk=BlockReaderFactory.Raw(stream);}
#if (!BYTEMODE)
public BuildBuffer(Stream stream,int fallbackCodePage){FileStream fStrm=(stream as FileStream);if(fStrm!=null)FileName=fStrm.Name;NextBlk=BlockReaderFactory.Get(stream,
fallbackCodePage);}
#endif
/// <summary>
/// Marks a conservative lower bound for the buffer,
/// allowing space to be reclaimed.  If an application 
/// needs to call GetString at arbitrary past locations 
/// in the input stream, Mark() is not called.
/// </summary>
public override void Mark(){data.Mark(bPos-2);}public override int Pos{get{return bPos;}set{bPos=value;}}/// <summary>
/// Read returns the ordinal number of the next char, or 
/// EOF (-1) for an end of stream.  Note that the next
/// code point may require *two* calls of Read().
/// </summary>
/// <returns></returns>
public override int Read(){ if(bPos<data.MaxIndex){ return(int)data[bPos++];}else{ char[]chrs=new char[4096];int count=NextBlk(chrs,0,4096);if(count==
0)return EndOfFile;else{data.Append(chrs,count);return(int)data[bPos++];}}}public override string GetString(int begin,int limit){return data.GetString(begin,
limit);}public override string ToString(){return"StringBuilder buffer, encoding: "+this.EncodingName;}} public delegate int BlockReader(char[]block,int
 index,int number); public static class BlockReaderFactory{public static BlockReader Raw(Stream stream){return delegate(char[]block,int index,int number)
{byte[]b=new byte[number];int count=stream.Read(b,0,number);int i=0;int j=index;for(;i<count;i++,j++)block[j]=(char)b[i];return count;};}
#if (!BYTEMODE)
public static BlockReader Get(Stream stream,int fallbackCodePage){Encoding encoding;int preamble=Preamble(stream);if(preamble!=0) encoding=Encoding.GetEncoding(preamble);
else if(fallbackCodePage==-1) return Raw(stream);else if(fallbackCodePage!=-2) encoding=Encoding.GetEncoding(fallbackCodePage);else{int guess=new Guesser(stream).GuessCodePage();
stream.Seek(0,SeekOrigin.Begin);if(guess==-1) encoding=Encoding.ASCII;else if(guess==65001)encoding=Encoding.UTF8;else encoding=Encoding.Default;}StreamReader
 reader=new StreamReader(stream,encoding);return reader.Read;}static int Preamble(Stream stream){int b0=stream.ReadByte();int b1=stream.ReadByte();if(b0
==0xfe&&b1==0xff)return 1201; if(b0==0xff&&b1==0xfe)return 1200; int b2=stream.ReadByte();if(b0==0xef&&b1==0xbb&&b2==0xbf)return 65001; stream.Seek(0,
SeekOrigin.Begin);return 0;}
#endif // !BYTEMODE
}
#endif // !NOFILES
#endregion Buffer classes
#if (!NOFILES)
public static class CodePageHandling{public static int GetCodePage(string option){string command=option.ToUpperInvariant();if(command.StartsWith("CodePage:",
StringComparison.OrdinalIgnoreCase))command=command.Substring(9);try{if(command.Equals("RAW"))return-1;else if(command.Equals("GUESS"))return-2;else if
(command.Equals("DEFAULT"))return 0;else if(char.IsDigit(command[0]))return int.Parse(command,CultureInfo.InvariantCulture);else{Encoding enc=Encoding.GetEncoding(command);
return enc.CodePage;}}catch(FormatException){Console.Error.WriteLine("Invalid format \"{0}\", using machine default",option);}catch(ArgumentException)
{Console.Error.WriteLine("Unknown code page \"{0}\", using machine default",option);}return 0;}}
#region guesser
#if (!BYTEMODE)
/// <summary>
/// This class provides a simple finite state automaton that
/// scans the file looking for (1) valid UTF-8 byte patterns,
/// (2) bytes >= 0x80 which are not part of a UTF-8 sequence.
/// The method then guesses whether it is UTF-8 or maybe some 
/// local machine default encoding.  This works well for the
/// various Latin encodings.
/// </summary>
internal class Guesser{ScanBuff buffer;public int GuessCodePage(){return Scan();}const int maxAccept=10;const int initial=0;const int eofNum=0;const int
 goStart=-1;const int INITIAL=0;const int EndToken=0;
#region user code
 public long utfX;public long uppr;
#endregion user code
int state;int currentStart=startState[0];int code;
#region ScannerTables
static int[]startState=new int[]{11,0};
#region CharacterMap
static sbyte[]map=new sbyte[256]{ 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,
0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0, 2,2,
2,2,2,2,2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2, 2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2, 1,1,1,1,1,1,1,1,1,1,1,
1,1,1,1,1, 1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1, 3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3, 4,4,4,4,4,4,4,4,5,5,5,5,5,5,5,5};
#endregion
static sbyte[][]nextState=new sbyte[][]{new sbyte[]{0,0,0,0,0,0},new sbyte[]{-1,-1,10,-1,-1,-1},new sbyte[]{-1,-1,-1,-1,-1,-1},new sbyte[]{-1,-1,8,-1,
-1,-1},new sbyte[]{-1,-1,5,-1,-1,-1},new sbyte[]{-1,-1,6,-1,-1,-1},new sbyte[]{-1,-1,7,-1,-1,-1},null,new sbyte[]{-1,-1,9,-1,-1,-1},null,null,new sbyte[]
{-1,1,2,3,4,2}};[SuppressMessage("Microsoft.Performance","CA1810:InitializeReferenceTypeStaticFieldsInline")] static Guesser(){nextState[7]=nextState[2];
nextState[9]=nextState[2];nextState[10]=nextState[2];}int NextState(){if(code==ScanBuff.EndOfFile)return eofNum;else return nextState[state][map[code]];
}
#endregion
public Guesser(System.IO.Stream file){SetSource(file);}public void SetSource(System.IO.Stream source){this.buffer=new BuildBuffer(source);code=buffer.Read();
}int Scan(){for(;;){int next;state=currentStart;while((next=NextState())==goStart)code=buffer.Read();state=next;code=buffer.Read();while((next=NextState())
>eofNum){state=next;code=buffer.Read();}if(state<=maxAccept){
#region ActionSwitch
#pragma warning disable 162
switch(state){case eofNum:switch(currentStart){case 11:if(utfX==0&&uppr==0)return-1; else if(uppr*10>utfX)return 0; else return 65001; break;}return EndToken;
case 1: case 2: case 3: case 4: uppr++;break;case 5: uppr+=2;break;case 6: uppr+=3;break;case 7: utfX+=3;break;case 8: uppr+=2;break;case 9: utfX+=2;break;
case 10: utfX++;break;default:break;}
#pragma warning restore 162
#endregion
}}}}
#endif // !BYTEMODE
#endregion
#endif // !NOFILES
}namespace Slang{public class SlangSyntaxException:Exception{private int _line;private int _column;private long _position;/// <summary>
/// Creates a syntax exception with the specified arguments
/// </summary>
/// <param name="message">The error message</param>
/// <param name="line">The line where the error occurred</param>
/// <param name="column">The column where the error occured</param>
/// <param name="position">The position where the error occured</param>
public SlangSyntaxException(string message,int line,int column,long position):base(SlangSyntaxException._GetMessage(message,line,column,position)){this._line
=line;this._column=column;this._position=position;}/// <summary>
/// The line where the error occurred
/// </summary>
public int Line{get{return this._line;}}/// <summary>
/// The column where the error occurred
/// </summary>
public int Column{get{return this._column;}}/// <summary>
/// The position where the error occurred
/// </summary>
public long Position{get{return this._position;}}static string _GetMessage(string message,int line,int column,long position){return string.Format("{0} at line {1}, column {2}, position {3}",
message,line,column,position);}}}namespace Slang{using System;using System.Collections.Generic;using System.IO;internal partial class SlangTokenizer:IEnumerable<Token>
{ public const int ErrorSymbol=-1;public const int EosSymbol=-2;public const int namespaceKeyword=396;public const int usingKeyword=397;public const int
 verbatimIdentifier=398;public const int outKeyword=399;public const int refKeyword=400;public const int typeOf=401;public const int defaultOf=402;public
 const int newKeyword=403;public const int globalKeyword=404;public const int stringType=405;public const int boolType=406;public const int charType=407;
public const int floatType=408;public const int doubleType=409;public const int decimalType=410;public const int sbyteType=411;public const int byteType
=412;public const int shortType=413;public const int ushortType=414;public const int intType=415;public const int uintType=416;public const int longType
=417;public const int ulongType=418;public const int objectType=419;public const int boolLiteral=420;public const int nullLiteral=421;public const int
 thisRef=422;public const int baseRef=423;public const int verbatimStringLiteral=424;public const int identifier=425;public const int stringLiteral=426;
public const int characterLiteral=427;public const int lte=428;public const int lt=429;public const int gte=430;public const int gt=431;public const int
 eqEq=432;public const int notEq=433;public const int eq=434;public const int inc=435;public const int addAssign=436;public const int add=437;public const
 int dec=438;public const int subAssign=439;public const int sub=440;public const int mulAssign=441;public const int mul=442;public const int divAssign
=443;public const int div=444;public const int modAssign=445;public const int mod=446;public const int and=447;public const int bitwiseAndAssign=448;public
 const int bitwiseAnd=449;public const int or=450;public const int bitwiseOrAssign=451;public const int bitwiseOr=452;public const int not=453;public const
 int lbracket=454;public const int rbracket=455;public const int lparen=456;public const int rparen=457;public const int lbrace=458;public const int rbrace
=459;public const int comma=460;public const int colonColon=461;public const int dot=462;public const int integerLiteral=463;public const int floatLiteral
=464;public const int whitespace=465;public const int ifKeyword=466;public const int gotoKeyword=467;public const int elseKeyword=468;public const int
 forKeyword=469;public const int throwKeyword=470;public const int whileKeyword=471;public const int returnKeyword=472;public const int tryKeyword=473;
public const int catchKeyword=474;public const int finallyKeyword=475;public const int semi=476;public const int varType=477;public const int colon=478;
public const int directive=479;public const int lineComment=480;public const int blockComment=481;public const int assemblyKeyword=482;public const int
 voidType=483;public const int partialKeyword=484;public const int classKeyword=485;public const int enumKeyword=486;public const int structKeyword=487;
public const int interfaceKeyword=488;public const int getKeyword=489;public const int setKeyword=490;public const int eventKeyword=491;public const int
 publicKeyword=492;public const int privateKeyword=493;public const int protectedKeyword=494;public const int internalKeyword=495;public const int staticKeyword
=496;public const int virtualKeyword=497;public const int abstractKeyword=498;public const int constKeyword=499;public const int overrideKeyword=500;public
 const int whereKeyword=501;public const int _EOS=502;public const int _ERROR=503;string _fallbackCodePage;Stream _input;string _inputText;bool _called;
 public SlangTokenizer(Stream input,string fallbackCodePage=null){if(null==input)throw new ArgumentNullException(nameof(input));if(null==fallbackCodePage)
fallbackCodePage="default";_fallbackCodePage=fallbackCodePage;_input=input;_inputText=null;_called=false;}public SlangTokenizer(string text){if(null==
text)throw new ArgumentNullException("text");_input=null;_inputText=text;}public IEnumerator<Token>GetEnumerator(){if(null!=_input){if(_called)throw new
 NotSupportedException("A stream cannot support multiple cursors. A new enumerator cannot be returned.");var result=new SlangTokenizerEnumerator(_input,
_fallbackCodePage);_called=true;return result;}return new SlangTokenizerEnumerator(_inputText);}System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
=>GetEnumerator();}internal partial class SlangTokenizerEnumerator:IEnumerator<Token>{const int _Error=-1; const int _Eos=-2; const int _Disposed=-3; Scanner
 _outer;public SlangTokenizerEnumerator(Stream input,string fallbackCodePage){_outer=new Scanner(input,fallbackCodePage);}public SlangTokenizerEnumerator(string
 text){_outer=new Scanner(MemoryStream.Null,"default");_outer.SetSource(text,0);}Token IEnumerator<Token>.Current=>_GetCurrent();object System.Collections.IEnumerator.Current
=>_GetCurrent();Token _GetCurrent(){var cur=_outer.Current;
#region Error Handling
if(0>cur.SymbolId){switch(cur.SymbolId){case _Disposed:throw new ObjectDisposedException(GetType().Name);case _Eos:throw new InvalidOperationException("The cursor is after the end of the enumeration");
case _Error: break;default: throw new InvalidOperationException("The cursor is before the start of the enumeration.");}}
#endregion Error Handling
return cur;}bool System.Collections.IEnumerator.MoveNext(){var cur=_outer.Current;if(_Disposed==cur.SymbolId)throw new ObjectDisposedException(GetType().Name);
if(_Eos==cur.SymbolId)return false;_outer.Advance();return _Eos!=_outer.Current.SymbolId;}void System.Collections.IEnumerator.Reset(){throw new NotSupportedException("Gplex tokenizers cannot be reset.");
}void IDisposable.Dispose(){_outer.Close();}}}