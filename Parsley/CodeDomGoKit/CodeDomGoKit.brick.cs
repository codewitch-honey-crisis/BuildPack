using System;
using System.Collections.Generic;
using System.Reflection;
using System.CodeDom;
using System.Globalization;
using System.Diagnostics.Contracts;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.ComponentModel.Design.Serialization;
using System.CodeDom.Compiler;
using System.Diagnostics;
namespace CD{using R=CodeDomResolver;using E=CodeTypeReferenceEqualityComparer;partial class CodeDomBinder{/// <summary>
/// Binds to a method using the specified arguments
/// </summary>
/// <param name="flags">The binding flags to use</param>
/// <param name="match">The candidates to select from</param>
/// <param name="args">The arguments to use</param>
/// <param name="modifiers">Not used</param>
/// <param name="cultureInfo">Not used</param>
/// <param name="names">The argument names or null</param>
/// <param name="state">A state object used to reorder arguments</param>
/// <returns>The method that best fits</returns>
public object BindToMethod(BindingFlags flags,object[]match,ref Object[]args,ParameterModifier[]modifiers,CultureInfo cultureInfo,String[]names,out Object
 state){if(null==match)throw new ArgumentNullException(nameof(match));if(0==match.Length)throw new ArgumentException("The match array cannot be empty.",
nameof(match));state=null;var k=_SplitMatchMethods(match);var csm=0<k.Key.Length?BindToMethod(flags,k.Key,ref args,modifiers,cultureInfo,names,out state)
:null; var rsm=0<k.Value.Length?BindToMethod(flags,k.Value,ref args,modifiers,cultureInfo,names,out state):null;if(null!=csm){if(null!=rsm)throw new AmbiguousMatchException("Multiple members matched the target argument types");
return csm;}return rsm;}/// <summary>
/// Binds to a method using the specified arguments
/// </summary>
/// <param name="flags">The binding flags to use</param>
/// <param name="match">The candidates to select from</param>
/// <param name="args">The arguments to use</param>
/// <param name="modifiers">Not used</param>
/// <param name="cultureInfo">Not used</param>
/// <param name="names">The argument names or null</param>
/// <param name="state">A state object used to reorder arguments</param>
/// <returns>The method that best fits</returns>
public MethodBase BindToMethod(BindingFlags flags,MethodBase[]match,ref Object[]args,ParameterModifier[]modifiers,CultureInfo cultureInfo,String[]names,
out Object state){if(match==null||match.Length==0)throw new ArgumentException("The array cannot be null or empty",nameof(match));Contract.EndContractBlock();
MethodBase[]candidates=(MethodBase[])match.Clone();int i;int j;state=null;
#region Map named parameters to candidate parameter postions
 int[][]paramOrder=new int[candidates.Length][];for(i=0;i<candidates.Length;i++){var par=_GetParamInfos(candidates[i].GetParameters()); paramOrder[i]=
new int[(par.Length>args.Length)?par.Length:args.Length];if(names==null){ for(j=0;j<args.Length;j++)paramOrder[i][j]=j;}else{ if(!CreateParamOrder(paramOrder[i],
par,names))candidates[i]=null;}}
#endregion
var paramArrayTypes=new CodeTypeReference[candidates.Length];var argTypes=new CodeTypeReference[args.Length];
#region Cache the type of the provided arguments
 for(i=0;i<args.Length;i++){if(args[i]!=null){argTypes[i]=new CodeTypeReference(args[i].GetType());}}
#endregion
 int CurIdx=0;bool defaultValueBinding=((flags&BindingFlags.OptionalParamBinding)!=0);CodeTypeReference paramArrayType=null;
#region Filter methods by parameter count and type
for(i=0;i<candidates.Length;i++){paramArrayType=null; if(candidates[i]==null)continue; var par=_GetParamInfos(candidates[i].GetParameters());
#region Match method by parameter count
if(par.Length==0){
#region No formal parameters
if(args.Length!=0){if((candidates[i].CallingConvention&CallingConventions.VarArgs)==0)continue;} paramOrder[CurIdx]=paramOrder[i];candidates[CurIdx++]
=candidates[i];continue;
#endregion
}else if(par.Length>args.Length){
#region Shortage of provided parameters
 for(j=args.Length;j<par.Length-1;j++){if(par[j].DefaultValue==System.DBNull.Value)break;}if(j!=par.Length-1)continue;if(par[j].DefaultValue==System.DBNull.Value)
{if(0==par[j].ParameterType.ArrayRank)continue;if(!par[j].IsDefinedParamArray)continue;paramArrayType=par[j].ParameterType.ArrayElementType;}
#endregion
}else if(par.Length<args.Length){
#region Excess provided parameters
 int lastArgPos=par.Length-1;if(0==par[lastArgPos].ParameterType.ArrayRank)continue;if(!par[lastArgPos].IsDefinedParamArray)continue;if(paramOrder[i][lastArgPos]
!=lastArgPos)continue;paramArrayType=par[lastArgPos].ParameterType.ArrayElementType;
#endregion
}else{
#region Test for paramArray, save paramArray type
int lastArgPos=par.Length-1;if(0!=par[lastArgPos].ParameterType.ArrayRank&&par[lastArgPos].IsDefinedParamArray&&paramOrder[i][lastArgPos]==lastArgPos)
{if(!_resolver.CanConvertTo(argTypes[lastArgPos],par[lastArgPos].ParameterType,_scope,false))paramArrayType=par[lastArgPos].ParameterType.ArrayElementType;
}
#endregion
}
#endregion
CodeTypeReference pCls=null;int argsToCheck=(!R.IsNullOrVoidType(paramArrayType))?par.Length-1:args.Length;
#region Match method by parameter type
for(j=0;j<argsToCheck;j++){
#region Classic argument coersion checks
 pCls=par[j].ParameterType; if(E.Equals(pCls,argTypes[paramOrder[i][j]]))continue; if(defaultValueBinding&&args[paramOrder[i][j]]==Type.Missing)continue;
 if(args[paramOrder[i][j]]==null)continue; if(E.Equals(pCls,_ObjType))continue; if(R.IsPrimitiveType(pCls)){var val=args[paramOrder[i][j]];var type=argTypes[paramOrder[i][j]];
if(type==null||!val.GetType().IsPrimitive||!_resolver.CanConvertTo(type,pCls,_scope)){break;}}else{if(argTypes[paramOrder[i][j]]==null)continue;if(_resolver.CanConvertTo(argTypes[paramOrder[i][j]],
pCls,_scope,false)){var at=argTypes[paramOrder[i][j]];var tt=_resolver.TryResolveType(at,_scope)as Type;if(null!=tt&&tt.IsCOMObject){var ct=_resolver.TryResolveType(pCls,
_scope)as Type;if(null!=ct&&ct.IsInstanceOfType(tt))continue;}break;}}
#endregion
}if(paramArrayType!=null&&j==par.Length-1){
#region Check that excess arguments can be placed in the param array
for(;j<args.Length;j++){if(R.IsPrimitiveType(paramArrayType)){var val=args[j];var type=argTypes[j];if(type==null||!_resolver.CanConvertTo(type,paramArrayType,_scope))
break;}else{if(argTypes[j]==null)continue;if(!_resolver.CanConvertTo(argTypes[j],paramArrayType,_scope)){var at=argTypes[j];var tt=_resolver.TryResolveType(at,
_scope)as Type;if(null!=tt&&tt.IsCOMObject){var pt=_resolver.TryResolveType(paramArrayType,_scope)as Type;if(null!=pt&&pt.IsInstanceOfType(args[j]))continue;
}break;}}}
#endregion
}
#endregion
if(j==args.Length){
#region This is a valid routine so we move it up the candidates list
paramOrder[CurIdx]=paramOrder[i];paramArrayTypes[CurIdx]=paramArrayType;candidates[CurIdx++]=candidates[i];
#endregion
}}
#endregion
 if(CurIdx==0)throw new MissingMethodException("A method with the specified parameters was not found");if(CurIdx==1){
#region Found only one method
if(names!=null){state=new _BindInfo((int[])paramOrder[0].Clone(),args.Length,paramArrayTypes[0]!=null);ReorderParams(paramOrder[0],args);} ParameterInfo[]
parms=candidates[0].GetParameters();if(parms.Length==args.Length){if(paramArrayTypes[0]!=null){Object[]objs=new Object[parms.Length];int lastPos=parms.Length
-1;Array.Copy(args,0,objs,0,lastPos);var t=_resolver.TryResolveType(paramArrayTypes[0],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[lastPos]=Array.CreateInstance(t,1);((Array)objs[lastPos]).SetValue(args[lastPos],0);args=objs;}}else if(parms.Length>args.Length){Object[]objs=new
 Object[parms.Length];for(i=0;i<args.Length;i++)objs[i]=args[i];for(;i<parms.Length-1;i++)objs[i]=parms[i].DefaultValue;if(paramArrayTypes[0]!=null){var
 t=_resolver.TryResolveType(paramArrayTypes[0],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[i]=Array.CreateInstance(t,0);}else objs[i]=parms[i].DefaultValue;args=objs;}else{if((candidates[0].CallingConvention&CallingConventions.VarArgs)==
0){Object[]objs=new Object[parms.Length];int paramArrayPos=parms.Length-1;Array.Copy(args,0,objs,0,paramArrayPos);var t=_resolver.TryResolveType(paramArrayTypes[0],
_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");objs[paramArrayPos]=Array.CreateInstance(t,
args.Length-paramArrayPos);Array.Copy(args,paramArrayPos,(System.Array)objs[paramArrayPos],0,args.Length-paramArrayPos);args=objs;}}
#endregion
return candidates[0];}int currentMin=0;bool ambig=false;for(i=1;i<CurIdx;i++){
#region Walk all of the methods looking the most specific method to invoke
int newMin=FindMostSpecificMethod(candidates[currentMin],paramOrder[currentMin],paramArrayTypes[currentMin],candidates[i],paramOrder[i],paramArrayTypes[i],
argTypes,args);if(newMin==0){ambig=true;}else if(newMin==2){currentMin=i;ambig=false;}
#endregion
}if(ambig)throw new AmbiguousMatchException("Multiple members matched the target argument types"); if(names!=null){state=new _BindInfo((int[])paramOrder[currentMin].Clone(),
args.Length,paramArrayTypes[currentMin]!=null);ReorderParams(paramOrder[currentMin],args);} ParameterInfo[]parameters=candidates[currentMin].GetParameters();
if(parameters.Length==args.Length){if(paramArrayTypes[currentMin]!=null){Object[]objs=new Object[parameters.Length];int lastPos=parameters.Length-1;Array.Copy(args,
0,objs,0,lastPos);var t=_resolver.TryResolveType(paramArrayTypes[currentMin],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[lastPos]=Array.CreateInstance(t,1);((Array)objs[lastPos]).SetValue(args[lastPos],0);args=objs;}}else if(parameters.Length>args.Length){Object[]objs
=new Object[parameters.Length];for(i=0;i<args.Length;i++)objs[i]=args[i];for(;i<parameters.Length-1;i++)objs[i]=parameters[i].DefaultValue;if(paramArrayTypes[currentMin]
!=null){var t=_resolver.TryResolveType(paramArrayTypes[currentMin],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[i]=Array.CreateInstance(t,0);}else{objs[i]=parameters[i].DefaultValue;}args=objs;}else{if((candidates[currentMin].CallingConvention&CallingConventions.VarArgs)
==0){Object[]objs=new Object[parameters.Length];int paramArrayPos=parameters.Length-1;Array.Copy(args,0,objs,0,paramArrayPos);var t=_resolver.TryResolveType(paramArrayTypes[currentMin],
_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");objs[paramArrayPos]=Array.CreateInstance(t,
args.Length-paramArrayPos);Array.Copy(args,paramArrayPos,(System.Array)objs[paramArrayPos],0,args.Length-paramArrayPos);args=objs;}}return candidates[currentMin];
} private static bool CreateParamOrder(int[]paramOrder,_ParamInfo[]pars,String[]names){bool[]used=new bool[pars.Length]; for(int i=0;i<pars.Length;i++)
paramOrder[i]=-1; for(int i=0;i<names.Length;i++){int j;for(j=0;j<pars.Length;j++){if(names[i].Equals(pars[j].Name)){paramOrder[j]=i;used[i]=true;break;
}} if(j==pars.Length)return false;} int pos=0;for(int i=0;i<pars.Length;i++){if(paramOrder[i]==-1){for(;pos<pars.Length;pos++){if(!used[pos]){paramOrder[i]
=pos;pos++;break;}}}}return true;}/// <summary>
/// Binds to a method using the specified arguments
/// </summary>
/// <param name="flags">The binding flags to use</param>
/// <param name="match">The candidates to select from</param>
/// <param name="args">The arguments to use</param>
/// <param name="modifiers">Not used</param>
/// <param name="cultureInfo">Not used</param>
/// <param name="names">The argument names or null</param>
/// <param name="state">A state object used to reorder arguments</param>
/// <returns>The method that best fits</returns>
public CodeMemberMethod BindToMethod(BindingFlags flags,CodeMemberMethod[]match,ref Object[]args,ParameterModifier[]modifiers,CultureInfo cultureInfo,
String[]names,out Object state){if(match==null||match.Length==0)throw new ArgumentException("The array cannot be null or empty",nameof(match));Contract.EndContractBlock();
var candidates=(CodeMemberMethod[])match.Clone();int i;int j;state=null;
#region Map named parameters to candidate parameter postions
 int[][]paramOrder=new int[candidates.Length][];for(i=0;i<candidates.Length;i++){var par=_GetParamInfos(candidates[i].Parameters,_scope); paramOrder[i]
=new int[(par.Length>args.Length)?par.Length:args.Length];if(names==null){ for(j=0;j<args.Length;j++)paramOrder[i][j]=j;}else{ if(!CreateParamOrder(paramOrder[i],
par,names))candidates[i]=null;}}
#endregion
var paramArrayTypes=new CodeTypeReference[candidates.Length];var argTypes=new CodeTypeReference[args.Length];
#region Cache the type of the provided arguments
 for(i=0;i<args.Length;i++){if(args[i]!=null){argTypes[i]=new CodeTypeReference(args[i].GetType());}}
#endregion
 int CurIdx=0;bool defaultValueBinding=((flags&BindingFlags.OptionalParamBinding)!=0);CodeTypeReference paramArrayType=null;
#region Filter methods by parameter count and type
for(i=0;i<candidates.Length;i++){paramArrayType=null; if(candidates[i]==null)continue; var par=_GetParamInfos(candidates[i].Parameters,_scope);
#region Match method by parameter count
if(par.Length==0){
#region No formal parameters
if(args.Length!=0){ continue;} paramOrder[CurIdx]=paramOrder[i];candidates[CurIdx++]=candidates[i];continue;
#endregion
}else if(par.Length>args.Length){
#region Shortage of provided parameters
 for(j=args.Length;j<par.Length-1;j++){if(par[j].DefaultValue==System.DBNull.Value)break;}if(j!=par.Length-1)continue;if(par[j].DefaultValue==System.DBNull.Value)
{if(0==par[j].ParameterType.ArrayRank)continue;if(!par[j].IsDefinedParamArray)continue;paramArrayType=par[j].ParameterType.ArrayElementType;}
#endregion
}else if(par.Length<args.Length){
#region Excess provided parameters
 int lastArgPos=par.Length-1;if(0==par[lastArgPos].ParameterType.ArrayRank)continue;if(!par[lastArgPos].IsDefinedParamArray)continue;if(paramOrder[i][lastArgPos]
!=lastArgPos)continue;paramArrayType=par[lastArgPos].ParameterType.ArrayElementType;
#endregion
}else{
#region Test for paramArray, save paramArray type
int lastArgPos=par.Length-1;if(0!=par[lastArgPos].ParameterType.ArrayRank&&par[lastArgPos].IsDefinedParamArray&&paramOrder[i][lastArgPos]==lastArgPos)
{if(!_resolver.CanConvertTo(argTypes[lastArgPos],par[lastArgPos].ParameterType,_scope,false))paramArrayType=par[lastArgPos].ParameterType.ArrayElementType;
}
#endregion
}
#endregion
CodeTypeReference pCls=null;int argsToCheck=(!R.IsNullOrVoidType(paramArrayType))?par.Length-1:args.Length;
#region Match method by parameter type
for(j=0;j<argsToCheck;j++){
#region Classic argument coersion checks
 pCls=par[j].ParameterType; if(E.Equals(pCls,argTypes[paramOrder[i][j]]))continue; if(defaultValueBinding&&args[paramOrder[i][j]]==Type.Missing)continue;
 if(args[paramOrder[i][j]]==null)continue; if(E.Equals(pCls,_ObjType))continue; if(R.IsPrimitiveType(pCls)){var val=args[paramOrder[i][j]];var type=argTypes[paramOrder[i][j]];
if(type==null||!val.GetType().IsPrimitive||!_resolver.CanConvertTo(type,pCls,_scope)){break;}}else{if(argTypes[paramOrder[i][j]]==null)continue;if(_resolver.CanConvertTo(argTypes[paramOrder[i][j]],
pCls,_scope,false)){var at=argTypes[paramOrder[i][j]];var tt=_resolver.TryResolveType(at,_scope)as Type;if(null!=tt&&tt.IsCOMObject){var ct=_resolver.TryResolveType(pCls,
_scope)as Type;if(null!=ct&&ct.IsInstanceOfType(tt))continue;}break;}}
#endregion
}if(paramArrayType!=null&&j==par.Length-1){
#region Check that excess arguments can be placed in the param array
for(;j<args.Length;j++){if(R.IsPrimitiveType(paramArrayType)){var val=args[j];var type=argTypes[j];if(type==null||!_resolver.CanConvertTo(type,paramArrayType,
_scope))break;}else{if(argTypes[j]==null)continue;if(!_resolver.CanConvertTo(argTypes[j],paramArrayType,_scope)){var at=argTypes[j];var tt=_resolver.TryResolveType(at,
_scope)as Type;if(null!=tt&&tt.IsCOMObject){var pt=_resolver.TryResolveType(paramArrayType,_scope)as Type;if(null!=pt&&pt.IsInstanceOfType(args[j]))continue;
}break;}}}
#endregion
}
#endregion
if(j==args.Length){
#region This is a valid routine so we move it up the candidates list
paramOrder[CurIdx]=paramOrder[i];paramArrayTypes[CurIdx]=paramArrayType;candidates[CurIdx++]=candidates[i];
#endregion
}}
#endregion
 if(CurIdx==0)throw new MissingMethodException("A method with the specified parameters was not found");if(CurIdx==1){
#region Found only one method
if(names!=null){state=new _BindInfo((int[])paramOrder[0].Clone(),args.Length,paramArrayTypes[0]!=null);ReorderParams(paramOrder[0],args);} var parms=_GetParamInfos(candidates[0].Parameters,
_scope);if(parms.Length==args.Length){if(paramArrayTypes[0]!=null){Object[]objs=new Object[parms.Length];int lastPos=parms.Length-1;Array.Copy(args,0,
objs,0,lastPos);var t=_resolver.TryResolveType(paramArrayTypes[0],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[lastPos]=Array.CreateInstance(t,1);((Array)objs[lastPos]).SetValue(args[lastPos],0);args=objs;}}else if(parms.Length>args.Length){Object[]objs=new
 Object[parms.Length];for(i=0;i<args.Length;i++)objs[i]=args[i];for(;i<parms.Length-1;i++)objs[i]=parms[i].DefaultValue;if(paramArrayTypes[0]!=null){var
 t=_resolver.TryResolveType(paramArrayTypes[0],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[i]=Array.CreateInstance(t,0);}else objs[i]=parms[i].DefaultValue;args=objs;}else{ Object[]objs=new Object[parms.Length];int paramArrayPos=parms.Length
-1;Array.Copy(args,0,objs,0,paramArrayPos);var t=_resolver.TryResolveType(paramArrayTypes[0],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[paramArrayPos]=Array.CreateInstance(t,args.Length-paramArrayPos);Array.Copy(args,paramArrayPos,(System.Array)objs[paramArrayPos],0,args.Length-paramArrayPos);
args=objs;}
#endregion
return candidates[0];}int currentMin=0;bool ambig=false;for(i=1;i<CurIdx;i++){
#region Walk all of the methods looking the most specific method to invoke
int newMin=FindMostSpecificMethod(candidates[currentMin],paramOrder[currentMin],paramArrayTypes[currentMin],candidates[i],paramOrder[i],paramArrayTypes[i],
argTypes,args);if(newMin==0){ambig=true;}else if(newMin==2){currentMin=i;ambig=false;}
#endregion
}if(ambig)throw new AmbiguousMatchException("Multiple members matched the target argument types"); if(names!=null){state=new _BindInfo((int[])paramOrder[currentMin].Clone(),
args.Length,paramArrayTypes[currentMin]!=null);ReorderParams(paramOrder[currentMin],args);} var parameters=_GetParamInfos(candidates[currentMin].Parameters);
if(parameters.Length==args.Length){if(paramArrayTypes[currentMin]!=null){Object[]objs=new Object[parameters.Length];int lastPos=parameters.Length-1;Array.Copy(args,
0,objs,0,lastPos);var t=_resolver.TryResolveType(paramArrayTypes[currentMin],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[lastPos]=Array.CreateInstance(t,1);((Array)objs[lastPos]).SetValue(args[lastPos],0);args=objs;}}else if(parameters.Length>args.Length){Object[]objs
=new Object[parameters.Length];for(i=0;i<args.Length;i++)objs[i]=args[i];for(;i<parameters.Length-1;i++)objs[i]=parameters[i].DefaultValue;if(paramArrayTypes[currentMin]
!=null){var t=_resolver.TryResolveType(paramArrayTypes[currentMin],_scope)as Type;if(null!=t)throw new TypeLoadException("Unable to resolve paramarray type to a runtime type");
objs[i]=Array.CreateInstance(t,0);}else{objs[i]=parameters[i].DefaultValue;}args=objs;}else{ Object[]objs=new Object[parameters.Length];int paramArrayPos
=parameters.Length-1;Array.Copy(args,0,objs,0,paramArrayPos);var t=_resolver.TryResolveType(paramArrayTypes[currentMin],_scope)as Type;if(null!=t)throw
 new TypeLoadException("Unable to resolve paramarray type to a runtime type");objs[paramArrayPos]=Array.CreateInstance(t,args.Length-paramArrayPos);Array.Copy(args,
paramArrayPos,(System.Array)objs[paramArrayPos],0,args.Length-paramArrayPos);args=objs;}return candidates[currentMin];} private static void ReorderParams<T>(int[]
paramOrder,T[]vars){T[]varsCopy=new T[vars.Length];for(int i=0;i<vars.Length;i++)varsCopy[i]=vars[i];for(int i=0;i<vars.Length;i++)vars[i]=varsCopy[paramOrder[i]];
}}}namespace CD{using R=CodeDomResolver;using E=CodeTypeReferenceEqualityComparer;
#if GOKITLIB
public
#endif
partial class CodeDomBinder{static readonly CodeTypeReference _ObjType=new CodeTypeReference(typeof(object));readonly CodeDomResolver _resolver;readonly
 CodeDomResolverScope _scope;/// <summary>
/// Initializes the binder with the given scope
/// </summary>
/// <param name="scope">The scope in which the binder is to operate</param>
public CodeDomBinder(CodeDomResolverScope scope){_resolver=scope.Resolver;_scope=scope;}internal static bool HasBindingFlag(BindingFlags flags,BindingFlags
 target){return target==(flags&target);}internal static bool HasMemberType(MemberTypes flags,MemberTypes target){return target==(flags&target);}private
 class _BindInfo{public int[]ArgumentMap;public int OriginalSize;public bool IsParamArray;internal _BindInfo(int[]argumentMap,int originalSize,bool isParamArray)
{ArgumentMap=argumentMap;OriginalSize=originalSize;IsParamArray=isParamArray;}}private struct _ParamInfo{public CodeTypeReference ParameterType;public
 Type RuntimeType;public string Name;public bool IsIn;public bool IsOut;public bool IsRetval;public bool IsOptional;public object DefaultValue;public bool
 IsCOMObject;public bool IsDefinedParamArray;}_ParamInfo[]_GetParamInfos(CodeExpressionCollection parms,CodeDomResolverScope scope=null){var result=new
 _ParamInfo[parms.Count];for(var i=0;i<result.Length;i++){CodeExpression e=parms[i];_ParamInfo p=default(_ParamInfo);p.IsOptional=false;p.IsRetval=false;
var de=e as CodeDirectionExpression;if(null!=de){switch(de.Direction){case FieldDirection.In:break;case FieldDirection.Out:p.IsOut=true;break;case FieldDirection.Ref:
p.IsIn=p.IsOut=true;break;}e=de.Expression;}p.ParameterType=_resolver.GetTypeOfExpression(e,_scope);result[i]=p;}return result;}_ParamInfo[]_GetParamInfos(CodeParameterDeclarationExpressionCollection
 parms,CodeDomResolverScope scope=null){var result=new _ParamInfo[parms.Count];for(var i=0;i<result.Length;i++){_ParamInfo p=default(_ParamInfo);p.IsOptional
=false;p.IsRetval=false;p.Name=parms[i].Name;p.DefaultValue=DBNull.Value;var pd=parms[i];switch(pd.Direction){case FieldDirection.In:break;case FieldDirection.Out:
p.IsOut=true;break;case FieldDirection.Ref:p.IsIn=p.IsOut=true;break;}p.ParameterType=pd.Type;if(null!=scope)p.ParameterType=_resolver.GetQualifiedType(pd.Type,
scope);result[i]=p;}return result;}_ParamInfo[]_GetParamInfos(ParameterInfo[]parms){var result=new _ParamInfo[parms.Length];for(var i=0;i<result.Length;
i++){_ParamInfo p=default(_ParamInfo);p.IsOptional=parms[i].IsOptional;p.IsRetval=parms[i].IsRetval;p.Name=parms[i].Name;p.RuntimeType=parms[i].ParameterType;
p.IsCOMObject=p.RuntimeType.IsCOMObject;p.DefaultValue=parms[i].DefaultValue;p.IsDefinedParamArray=parms[i].IsDefined(typeof(ParamArrayAttribute),true);
var pd=parms[i];p.ParameterType=new CodeTypeReference(parms[i].ParameterType);result[i]=p;}return result;}}}namespace CD{using R=CodeDomResolver;using
 E=CodeTypeReferenceEqualityComparer;partial class CodeDomBinder{int FindMostSpecific(_ParamInfo[]p1,int[]paramOrder1,CodeTypeReference paramArrayType1,
_ParamInfo[]p2,int[]paramOrder2,CodeTypeReference paramArrayType2,CodeTypeReference[]types,Object[]args){ if(!R.IsNullOrVoidType(paramArrayType1)&&R.IsNullOrVoidType(paramArrayType2))
return 2;if(!R.IsNullOrVoidType(paramArrayType2)&&R.IsNullOrVoidType(paramArrayType1))return 1; bool p1Less=false;bool p2Less=false;for(int i=0;i<types.Length;
i++){if(args!=null&&args[i]==Type.Missing)continue;CodeTypeReference c1,c2; if(!R.IsNullOrVoidType(paramArrayType1)&&paramOrder1[i]>=p1.Length-1)c1=paramArrayType1;
else c1=p1[paramOrder1[i]].ParameterType;if(!R.IsNullOrVoidType(paramArrayType2)&&paramOrder2[i]>=p2.Length-1)c2=paramArrayType2;else c2=p2[paramOrder2[i]].ParameterType;
if(E.Equals(c1,c2))continue;switch(FindMostSpecificType(c1,c2,types[i])){case 0:return 0;case 1:p1Less=true;break;case 2:p2Less=true;break;}} if(p1Less
==p2Less){ if(!p1Less&&args!=null){if(p1.Length>p2.Length){return 1;}else if(p2.Length>p1.Length){return 2;}}return 0;}else{return(p1Less==true)?1:2;}
}int FindMostSpecific(_ParamInfo[]p1,int[]paramOrder1,CodeTypeReference paramArrayType1,_ParamInfo[]p2,int[]paramOrder2,CodeTypeReference paramArrayType2,
CodeTypeReference[]types){if(2==p1.Length)System.Diagnostics.Debugger.Break(); if(!R.IsNullOrVoidType(paramArrayType1)&&R.IsNullOrVoidType(paramArrayType2))
return 2;if(!R.IsNullOrVoidType(paramArrayType2)&&R.IsNullOrVoidType(paramArrayType1))return 1; bool p1Less=false;bool p2Less=false;for(int i=0;i<types.Length;
i++){if(R.IsNullOrVoidType(types[i]))continue;CodeTypeReference c1,c2; if(!R.IsNullOrVoidType(paramArrayType1)&&paramOrder1[i]>=p1.Length-1)c1=paramArrayType1;
else c1=p1[paramOrder1[i]].ParameterType;if(R.IsNullOrVoidType(paramArrayType2)&&paramOrder2[i]>=p2.Length-1)c2=paramArrayType2;else c2=p2[paramOrder2[i]].ParameterType;
if(E.Equals(c1,c2))continue;switch(FindMostSpecificType(c1,c2,types[i])){case 0:return 0;case 1:p1Less=true;break;case 2:p2Less=true;break;}} if(p1Less
==p2Less){ if(!p1Less){if(p1.Length>p2.Length){return 1;}else if(p2.Length>p1.Length){return 2;}}return 0;}else{return(p1Less==true)?1:2;}}int FindMostSpecificType(CodeTypeReference
 c1,CodeTypeReference c2,CodeTypeReference t){ if(E.Equals(c1,c2))return 0;if(E.Equals(c1,t))return 1;if(E.Equals(c2,t))return 2;bool c1FromC2;bool c2FromC1;
if(R.IsPrimitiveType(c1)&&R.IsPrimitiveType(c2)){c1FromC2=_resolver.CanConvertTo(c2,c1,_scope);c2FromC1=_resolver.CanConvertTo(c1,c2,_scope);}else{c1FromC2
=_resolver.CanConvertTo(c2,c1,_scope,false);c2FromC1=_resolver.CanConvertTo(c1,c2,_scope,false);}if(c1FromC2==c2FromC1)return 0;if(c1FromC2){return 2;
}else{return 1;}}int FindMostSpecificMethod(MethodBase m1,int[]paramOrder1,CodeTypeReference paramArrayType1,MethodBase m2,int[]paramOrder2,CodeTypeReference
 paramArrayType2,CodeTypeReference[]types,Object[]args){ int res=FindMostSpecific(_GetParamInfos(m1.GetParameters()),paramOrder1,paramArrayType1,_GetParamInfos(m2.GetParameters()),
paramOrder2,paramArrayType2,types,args); if(res!=0)return res; if(CompareMethodSigAndName(m1,m2)){ int hierarchyDepth1=GetHierarchyDepth(m1.DeclaringType);
int hierarchyDepth2=GetHierarchyDepth(m2.DeclaringType); if(hierarchyDepth1==hierarchyDepth2){return 0;}else if(hierarchyDepth1<hierarchyDepth2){return
 2;}else{return 1;}} return 0;}int FindMostSpecificMethod(MethodBase m1,int[]paramOrder1,CodeTypeReference paramArrayType1,MethodBase m2,int[]paramOrder2,
CodeTypeReference paramArrayType2,CodeTypeReference[]types){ int res=FindMostSpecific(_GetParamInfos(m1.GetParameters()),paramOrder1,paramArrayType1,_GetParamInfos(m2.GetParameters()),
paramOrder2,paramArrayType2,types); if(res!=0)return res; if(CompareMethodSigAndName(m1,m2)){ int hierarchyDepth1=GetHierarchyDepth(m1.DeclaringType);
int hierarchyDepth2=GetHierarchyDepth(m2.DeclaringType); if(hierarchyDepth1==hierarchyDepth2){return 0;}else if(hierarchyDepth1<hierarchyDepth2){return
 2;}else{return 1;}} return 0;}int FindMostSpecificMethod(CodeMemberMethod m1,int[]paramOrder1,CodeTypeReference paramArrayType1,CodeMemberMethod m2,int[]
paramOrder2,CodeTypeReference paramArrayType2,CodeTypeReference[]types,Object[]args){ int res=FindMostSpecific(_GetParamInfos(m1.Parameters),paramOrder1,
paramArrayType1,_GetParamInfos(m2.Parameters),paramOrder2,paramArrayType2,types,args); if(res!=0)return res; if(CompareMethodSigAndName(m1,m2)){ int hierarchyDepth1
=GetHierarchyDepth(_resolver.GetScope(m1).DeclaringType);int hierarchyDepth2=GetHierarchyDepth(_resolver.GetScope(m2).DeclaringType); if(hierarchyDepth1
==hierarchyDepth2){return 0;}else if(hierarchyDepth1<hierarchyDepth2){return 2;}else{return 1;}} return 0;}int FindMostSpecificMethod(CodeMemberMethod
 m1,int[]paramOrder1,CodeTypeReference paramArrayType1,CodeMemberMethod m2,int[]paramOrder2,CodeTypeReference paramArrayType2,CodeTypeReference[]types)
{ int res=FindMostSpecific(_GetParamInfos(m1.Parameters),paramOrder1,paramArrayType1,_GetParamInfos(m2.Parameters),paramOrder2,paramArrayType2,types);
 if(res!=0)return res; if(CompareMethodSigAndName(m1,m2)){ int hierarchyDepth1=GetHierarchyDepth(_resolver.GetScope(m1).DeclaringType);int hierarchyDepth2
=GetHierarchyDepth(_resolver.GetScope(m2).DeclaringType); if(hierarchyDepth1==hierarchyDepth2){return 0;}else if(hierarchyDepth1<hierarchyDepth2){return
 2;}else{return 1;}} return 0;}int FindMostSpecificField(FieldInfo cur1,FieldInfo cur2){ if(cur1.Name==cur2.Name){int hierarchyDepth1=GetHierarchyDepth(cur1.DeclaringType);
int hierarchyDepth2=GetHierarchyDepth(cur2.DeclaringType);if(hierarchyDepth1==hierarchyDepth2){Contract.Assert(cur1.IsStatic!=cur2.IsStatic,"hierarchyDepth1 == hierarchyDepth2");
return 0;}else if(hierarchyDepth1<hierarchyDepth2)return 2;else return 1;} return 0;}int FindMostSpecificProperty(PropertyInfo cur1,PropertyInfo cur2)
{ if(cur1.Name==cur2.Name){int hierarchyDepth1=GetHierarchyDepth(cur1.DeclaringType);int hierarchyDepth2=GetHierarchyDepth(cur2.DeclaringType);if(hierarchyDepth1
==hierarchyDepth2){return 0;}else if(hierarchyDepth1<hierarchyDepth2)return 2;else return 1;} return 0;}int FindMostSpecificProperty(CodeMemberProperty
 cur1,CodeMemberProperty cur2){ if(cur1.Name==cur2.Name){int hierarchyDepth1=GetHierarchyDepth(_resolver.GetScope(cur1).DeclaringType);int hierarchyDepth2
=GetHierarchyDepth(_resolver.GetScope(cur2).DeclaringType);if(hierarchyDepth1==hierarchyDepth2){return 0;}else if(hierarchyDepth1<hierarchyDepth2)return
 2;else return 1;} return 0;}bool CompareMethodSigAndName(MethodBase m1,MethodBase m2){ParameterInfo[]params1=m1.GetParameters();ParameterInfo[]params2
=m2.GetParameters();if(params1.Length!=params2.Length)return false;int numParams=params1.Length;for(int i=0;i<numParams;i++){if(params1[i].ParameterType
!=params2[i].ParameterType)return false;}return true;}bool CompareMethodSigAndName(CodeMemberMethod m1,CodeMemberMethod m2){var params1=_GetParamInfos(m1.Parameters,
_scope);var params2=_GetParamInfos(m2.Parameters,_scope);if(params1.Length!=params2.Length)return false;int numParams=params1.Length;for(int i=0;i<numParams;
i++){if(!E.Equals(params1[i].ParameterType,params2[i].ParameterType))return false;}return true;}int GetHierarchyDepth(CodeTypeDeclaration t){var result
=0;var currentType=_resolver.GetType(t,_scope);do{++result;currentType=_resolver.GetBaseType(currentType,_scope);}while(currentType!=null);return result;
}static int GetHierarchyDepth(Type t){int depth=0;Type currentType=t;do{depth++;currentType=currentType.BaseType;}while(currentType!=null);return depth;
}internal static MethodBase FindMostDerivedNewSlotMeth(MethodBase[]match,int cMatches){int deepestHierarchy=0;MethodBase methWithDeepestHierarchy=null;
for(int i=0;i<cMatches;i++){ int currentHierarchyDepth=GetHierarchyDepth(match[i].DeclaringType); if(currentHierarchyDepth==deepestHierarchy){throw new
 AmbiguousMatchException("Multiple members matched the specified arguments");} if(currentHierarchyDepth>deepestHierarchy){deepestHierarchy=currentHierarchyDepth;
methWithDeepestHierarchy=match[i];}}return methWithDeepestHierarchy;}}}namespace CD{/// <summary>
/// Provides reflection and signature matching over codedom objects
/// </summary>
partial class CodeDomBinder{/// <summary>
/// Retrieves a list of members given the specified <see cref="MemberTypes"/> and <see cref="BindingFlags"/>
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="types">The member types to retrieve</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="MemberInfo"/> and <see cref="CodeTypeMember"/> objects representing the combined runtime and declared members</returns>
public object[]GetMembers(object type,MemberTypes types,BindingFlags flags){var rt=type as Type;if(null!=rt)return GetMembers(rt,types,flags);var td=type
 as CodeTypeDeclaration;if(null!=td)return GetMembers(td,types,flags);throw new ArgumentException("The type must be a runtime type or a declared type",
nameof(type));}/// <summary>
/// Retrieves a list of members given the specified <see cref="MemberTypes"/> and <see cref="BindingFlags"/>
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="types">The member types to retrieve</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="MemberInfo"/> and <see cref="CodeTypeMember"/> objects representing the combined runtime and declared members</returns>
public object[]GetMembers(CodeTypeDeclaration type,MemberTypes types,BindingFlags flags){var result=new List<object>();result.AddRange(_GetMembers(type,
types,flags|BindingFlags.DeclaredOnly));if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly)){for(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];
var t=_resolver.TryResolveType(bt,_scope);var td=t as CodeTypeDeclaration;if(null!=td){result.AddRange(GetMembers(td,types,flags));}else{var tt=t as Type;
if(null!=tt){result.AddRange(GetMembers(tt,types,flags));}}}}return result.ToArray();}/// <summary>
/// Retrieves a list of members given the specified <see cref="MemberTypes"/> and <see cref="BindingFlags"/>
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="types">The member types to retrieve</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="MemberInfo"/> objects representing the runtime members</returns>
public MemberInfo[]GetMembers(Type type,MemberTypes types,BindingFlags flags){var ma=type.GetMembers(flags);if(types==MemberTypes.All)return ma;var result
=new List<MemberInfo>(ma.Length);for(var i=0;i<ma.Length;i++){var m=ma[i]; switch(m.MemberType){case MemberTypes.Constructor:if((types&MemberTypes.Constructor)
==m.MemberType)result.Add(m);break;case MemberTypes.Custom:if((types&MemberTypes.Custom)==m.MemberType)result.Add(m);break;case MemberTypes.Event:if((types
&MemberTypes.Event)==m.MemberType)result.Add(m);break;case MemberTypes.Field:if((types&MemberTypes.Field)==m.MemberType)result.Add(m);break;case MemberTypes.Method:
if((types&MemberTypes.Method)==m.MemberType)result.Add(m);break;case MemberTypes.NestedType:if((types&MemberTypes.NestedType)==m.MemberType)result.Add(m);
break;case MemberTypes.Property:if((types&MemberTypes.Property)==m.MemberType)result.Add(m);break;case MemberTypes.TypeInfo:if((types&MemberTypes.TypeInfo)
==m.MemberType)result.Add(m);break;}}return result.ToArray();}CodeTypeMember[]_GetMembers(CodeTypeDeclaration type,MemberTypes types,BindingFlags flags)
{var ic=type.Members.Count;var result=new List<CodeTypeMember>(ic);for(var i=0;i<ic;++i){var mem=type.Members[i];var isPublic=MemberAttributes.Public==
(mem.Attributes&MemberAttributes.AccessMask);var wantPublic=HasBindingFlag(flags,BindingFlags.Public);var isNonPublic=MemberAttributes.Public!=(mem.Attributes
&MemberAttributes.AccessMask);var wantNonPublic=HasBindingFlag(flags,BindingFlags.NonPublic);if((isNonPublic&&wantNonPublic)||(isPublic&&wantPublic)){
var isStatic=MemberAttributes.Static==(mem.Attributes&MemberAttributes.ScopeMask)||MemberAttributes.Const==(mem.Attributes&MemberAttributes.ScopeMask);
var wantStatic=HasBindingFlag(flags,BindingFlags.Static);var isInst=MemberAttributes.Static!=(mem.Attributes&MemberAttributes.ScopeMask)&&MemberAttributes.Const
!=(mem.Attributes&MemberAttributes.ScopeMask);var wantInst=HasBindingFlag(flags,BindingFlags.Instance);if((isStatic&&wantStatic)||(isInst&&wantInst)){
if(HasMemberType(types,MemberTypes.Field)){var f=mem as CodeMemberField;if(null!=f){result.Add(f);continue;}}if(HasMemberType(types,MemberTypes.Property))
{var p=mem as CodeMemberProperty;if(null!=p){result.Add(p);continue;}}if(HasMemberType(types,MemberTypes.Event)){var e=mem as CodeMemberEvent;if(null!=
e){result.Add(e);continue;}}if(HasMemberType(types,MemberTypes.NestedType)){var t=mem as CodeTypeDeclaration;if(null!=t){result.Add(t);continue;}}if(HasMemberType(types,
MemberTypes.Constructor)){var c=mem as CodeConstructor;if(null!=c){result.Add(c);continue;}}if(HasMemberType(types,MemberTypes.Method)){var c=mem as CodeConstructor;
if(null==c){var m=mem as CodeMemberMethod;if(null!=m){result.Add(m);continue;}}}if(HasMemberType(types,MemberTypes.Custom)){var s=mem as CodeSnippetTypeMember;
if(null!=s){result.Add(s);continue;}}}}}if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly)){ic=type.BaseTypes.Count;for(var i=0;i<ic;++i){ var bt=type.BaseTypes[i];
var td=_resolver.TryResolveType(bt,_scope)as CodeTypeDeclaration;if(null!=td){var grp=_GetMembers(td,types,flags);if(HasBindingFlag(flags,BindingFlags.FlattenHierarchy))
{for(var j=0;j<grp.Length;j++){var m=grp[j];if((m.Attributes&MemberAttributes.ScopeMask)!=MemberAttributes.Static)result.Add(m);else if((m.Attributes&
MemberAttributes.AccessMask)!=MemberAttributes.Private)result.Add(m);}}else{for(var j=0;j<grp.Length;j++){var m=grp[i];if((m.Attributes&MemberAttributes.ScopeMask)
!=MemberAttributes.Static)result.Add(m);}}}}}return result.ToArray();}CodeMemberProperty[]_GetPropertyGroup(CodeTypeDeclaration type,string name,BindingFlags
 flags){var result=new List<CodeMemberProperty>();for(int ic=type.Members.Count,i=0;i<ic;++i){var member=type.Members[i];var prop=member as CodeMemberProperty;
if(null!=prop){if(string.IsNullOrEmpty(name)||0==string.Compare(prop.Name,name,StringComparison.InvariantCulture)){if(HasBindingFlag(flags,BindingFlags.NonPublic)
&&MemberAttributes.Public!=(prop.Attributes&MemberAttributes.AccessMask)||(HasBindingFlag(flags,BindingFlags.Public)&&MemberAttributes.Public==(prop.Attributes
&MemberAttributes.AccessMask))){if(HasBindingFlag(flags,BindingFlags.Static)&&MemberAttributes.Static==(prop.Attributes&MemberAttributes.ScopeMask)||MemberAttributes.Const
==(prop.Attributes&MemberAttributes.ScopeMask)||(HasBindingFlag(flags,BindingFlags.Instance)&&MemberAttributes.Static!=(prop.Attributes&MemberAttributes.ScopeMask)
&&MemberAttributes.Const!=(prop.Attributes&MemberAttributes.ScopeMask))){result.Add(prop);}}}}}if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly)){for
(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];var td=_resolver.TryResolveType(bt,_scope)as CodeTypeDeclaration;if(null!=td){var
 grp=_GetPropertyGroup(td,name,flags);if(HasBindingFlag(flags,BindingFlags.FlattenHierarchy)){for(var j=0;j<grp.Length;j++){var m=grp[j];if((m.Attributes
&MemberAttributes.AccessMask)!=MemberAttributes.Private)result.Add(m);}}else{for(var j=0;j<grp.Length;j++){var m=grp[i];if((m.Attributes&MemberAttributes.ScopeMask)
!=MemberAttributes.Static)result.Add(m);}}}}}return result.ToArray();}CodeMemberMethod[]_GetMethodGroup(CodeTypeDeclaration type,string name,BindingFlags
 flags){var result=new List<CodeMemberMethod>();for(int ic=type.Members.Count,i=0;i<ic;++i){var member=type.Members[i];var ctor=member as CodeConstructor;
if(null==ctor){var meth=member as CodeMemberMethod;if(null!=meth){if(string.IsNullOrEmpty(name)||0==string.Compare(meth.Name,name,StringComparison.InvariantCulture))
{if(HasBindingFlag(flags,BindingFlags.NonPublic)&&MemberAttributes.Public!=(meth.Attributes&MemberAttributes.AccessMask)||(HasBindingFlag(flags,BindingFlags.Public)
&&MemberAttributes.Public==(meth.Attributes&MemberAttributes.AccessMask))){if(HasBindingFlag(flags,BindingFlags.Static)&&MemberAttributes.Static==(meth.Attributes
&MemberAttributes.ScopeMask)||MemberAttributes.Const==(meth.Attributes&MemberAttributes.ScopeMask)||(HasBindingFlag(flags,BindingFlags.Instance)&&MemberAttributes.Static
!=(meth.Attributes&MemberAttributes.ScopeMask)&&MemberAttributes.Const!=(meth.Attributes&MemberAttributes.ScopeMask))){result.Add(meth);}}}}}}if(!HasBindingFlag(flags,
BindingFlags.DeclaredOnly)){for(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];var td=_resolver.TryResolveType(bt,_scope)as CodeTypeDeclaration;
if(null!=td){var grp=_GetMethodGroup(td,name,flags);if(HasBindingFlag(flags,BindingFlags.FlattenHierarchy)){for(var j=0;j<grp.Length;j++){var m=grp[j];
if((m.Attributes&MemberAttributes.AccessMask)!=MemberAttributes.Private)result.Add(m);}}else{for(var j=0;j<grp.Length;j++){var m=grp[i];if((m.Attributes
&MemberAttributes.ScopeMask)!=MemberAttributes.Static)result.Add(m);}}}}}return result.ToArray();}/// <summary>
/// Gets a method group of the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the method group</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="MethodInfo"/> or <see cref="CodeMemberMethod"/> objects representing the methods in the method group</returns>
public object[]GetMethodGroup(object type,string name,BindingFlags flags){var rt=type as Type;if(null!=rt)return GetMethodGroup(rt,name,flags);var td=
type as CodeTypeDeclaration;if(null!=td)return GetMethodGroup(td,name,flags);throw new ArgumentException("The type must be a runtime type or a declared type",
nameof(type));}/// <summary>
/// Gets a method group of the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the method group</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="MethodInfo"/> or <see cref="CodeMemberMethod"/> objects representing the methods in the method group</returns>
public object[]GetMethodGroup(CodeTypeDeclaration type,string name,BindingFlags flags){var result=new List<object>();result.AddRange(_GetMethodGroup(type,
name,flags|BindingFlags.DeclaredOnly));if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly)){for(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];
var t=_resolver.TryResolveType(bt,_scope);var td=t as CodeTypeDeclaration;if(null!=td){result.AddRange(GetMethodGroup(td,name,flags));}else{var tt=t as
 Type;if(null!=tt){var ma=tt.GetMethods(flags);for(var j=0;j<ma.Length;j++){var m=ma[j];if(string.IsNullOrEmpty(name)||0==string.Compare(m.Name,name,StringComparison.InvariantCulture))
if(!result.Contains(m))result.Add(m);}}}}}return result.ToArray();}/// <summary>
/// Gets a method group of the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the method group</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="MethodInfo"/> objects representing the methods in the method group</returns>
public MethodInfo[]GetMethodGroup(Type type,string name,BindingFlags flags){var result=new List<MethodInfo>();var ma=type.GetMethods(flags);for(var i=
0;i<ma.Length;++i){var m=ma[i];if(0==string.Compare(m.Name,name,StringComparison.InvariantCulture))result.Add(m);}if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly))
{var ia=type.GetInterfaces();for(var i=0;i<ia.Length;++i){result.AddRange(GetMethodGroup(ia[i],name,flags));}}return result.ToArray();}/// <summary>
/// Gets a property group of the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the property group</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="PropertyInfo"/> or <see cref="CodeMemberProperty"/> objects representing the methods in the method group</returns>
public object[]GetPropertyGroup(object type,string name,BindingFlags flags){var rt=type as Type;if(null!=rt)return GetPropertyGroup(rt,name,flags);var
 td=type as CodeTypeDeclaration;if(null!=td)return GetPropertyGroup(td,name,flags);throw new ArgumentException("The type must be a runtime type or a declared type",
nameof(type));}/// <summary>
/// Gets a property group of the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the property group</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="PropertyInfo"/> or <see cref="CodeMemberProperty"/> objects representing the methods in the method group</returns>
public object[]GetPropertyGroup(CodeTypeDeclaration type,string name,BindingFlags flags){var result=new List<object>();result.AddRange(_GetPropertyGroup(type,
name,flags|BindingFlags.DeclaredOnly));if(HasBindingFlag(flags,BindingFlags.DeclaredOnly)){for(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];
var t=_resolver.TryResolveType(bt,_scope);var td=t as CodeTypeDeclaration;if(null!=td){result.AddRange(GetPropertyGroup(td,name,flags));}else{var tt=t
 as Type;if(null!=tt){var pa=tt.GetProperties(flags);for(var j=0;j<pa.Length;j++){var p=pa[i];if(string.IsNullOrEmpty(name)||0==string.Compare(p.Name,
name,StringComparison.InvariantCulture))if(!result.Contains(p))result.Add(p);}}}}}return result.ToArray();}/// <summary>
/// Gets a property group of the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the property group</param>
/// <param name="flags">The binding flags</param>
/// <returns>An array of <see cref="PropertyInfo"/> objects representing the methods in the method group</returns>
public PropertyInfo[]GetPropertyGroup(Type type,string name,BindingFlags flags){var result=new List<PropertyInfo>();var pa=type.GetProperties(flags);for
(var i=0;i<pa.Length;++i){var p=pa[i];if(0==string.Compare(p.Name,name,StringComparison.InvariantCulture))result.Add(p);}if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly))
{var ia=type.GetInterfaces();for(var i=0;i<ia.Length;++i){result.AddRange(GetPropertyGroup(ia[i],name,flags));}}return result.ToArray();}/// <summary>
/// Gets an event by the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the event</param>
/// <param name="flags">The binding flags</param>
/// <returns>Either a <see cref="EventInfo"/> or a <see cref="CodeMemberEvent"/> representing the event</returns>
public object GetEvent(object type,string name,BindingFlags flags){var rt=type as Type;if(null!=rt)return GetEvent(rt,name,flags);var td=type as CodeTypeDeclaration;
if(null!=td)return GetEvent(td,name,flags);throw new ArgumentException("The type must be a runtime type or a declared type",nameof(type));}/// <summary>
/// Gets an event by the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the event</param>
/// <param name="flags">The binding flags</param>
/// <returns>A <see cref="EventInfo"/> representing the event</returns>
public EventInfo GetEvent(Type type,string name,BindingFlags flags){return type.GetEvent(name,flags);}/// <summary>
/// Gets an event by the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the event</param>
/// <param name="flags">The binding flags</param>
/// <returns>Either a <see cref="EventInfo"/> or a <see cref="CodeMemberEvent"/> representing the event</returns>
public object GetEvent(CodeTypeDeclaration type,string name,BindingFlags flags){var r=_GetEvent(type,name,flags|BindingFlags.DeclaredOnly);if(null!=r)
return r;if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly)){for(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];var t=_resolver.TryResolveType(bt,
_scope);var td=t as CodeTypeDeclaration;if(null!=td){return GetEvent(td,name,flags);}else{var tt=t as Type;if(null!=tt){var fld=tt.GetEvent(name,flags);
if(null!=fld)return fld;}}}}return null;}/// <summary>
/// Gets a field by the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the field</param>
/// <param name="flags">The binding flags</param>
/// <returns>Either a <see cref="FieldInfo"/> or a <see cref="CodeMemberField"/> representing the field</returns>
public object GetField(object type,string name,BindingFlags flags){var rt=type as Type;if(null!=rt)return GetField(rt,name,flags);var td=type as CodeTypeDeclaration;
if(null!=td)return GetField(td,name,flags);throw new ArgumentException("The type must be a runtime type or a declared type",nameof(type));}/// <summary>
/// Gets a field by the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the field</param>
/// <param name="flags">The binding flags</param>
/// <returns>A <see cref="FieldInfo"/> representing the field</returns>
public FieldInfo GetField(Type type,string name,BindingFlags flags){return type.GetField(name,flags);}/// <summary>
/// Gets a field by the specified name
/// </summary>
/// <param name="type">The type to bind to</param>
/// <param name="name">The name of the field</param>
/// <param name="flags">The binding flags</param>
/// <returns>Either a <see cref="FieldInfo"/> or a <see cref="CodeMemberField"/> representing the field</returns>
public object GetField(CodeTypeDeclaration type,string name,BindingFlags flags){var r=_GetField(type,name,flags|BindingFlags.DeclaredOnly);if(null!=r)
return r;if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly)){for(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];var t=_resolver.TryResolveType(bt,
_scope);var td=t as CodeTypeDeclaration;if(null!=td){return GetField(td,name,flags);}else{var tt=t as Type;if(null!=tt){var fld=tt.GetField(name,flags);
if(null!=fld)return fld;}}}}return null;}CodeMemberField _GetField(CodeTypeDeclaration type,string name,BindingFlags flags){for(int ic=type.Members.Count,
i=0;i<ic;++i){var member=type.Members[i];var fld=member as CodeMemberField;if(null!=fld){if(0==string.Compare(fld.Name,name,StringComparison.InvariantCulture))
{if(HasBindingFlag(flags,BindingFlags.NonPublic)&&MemberAttributes.Public!=(fld.Attributes&MemberAttributes.AccessMask)||(HasBindingFlag(flags,BindingFlags.Public)
&&MemberAttributes.Public==(fld.Attributes&MemberAttributes.AccessMask))){if(HasBindingFlag(flags,BindingFlags.Static)&&MemberAttributes.Static==(fld.Attributes
&MemberAttributes.ScopeMask)||MemberAttributes.Const==(fld.Attributes&MemberAttributes.ScopeMask)||(HasBindingFlag(flags,BindingFlags.Instance)&&MemberAttributes.Static
!=(fld.Attributes&MemberAttributes.ScopeMask)&&MemberAttributes.Const!=(fld.Attributes&MemberAttributes.ScopeMask))){return fld;}}}}}if(!HasBindingFlag(flags,
BindingFlags.DeclaredOnly)){for(int ic=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];var td=_resolver.TryResolveType(bt,_scope)as CodeTypeDeclaration;
if(null!=td){var fld=_GetField(td,name,flags);if(HasBindingFlag(flags,BindingFlags.FlattenHierarchy)){if((fld.Attributes&MemberAttributes.AccessMask)!=
MemberAttributes.Private)return fld;}else{if((fld.Attributes&MemberAttributes.ScopeMask)!=MemberAttributes.Static)return fld;}}}}return null;}CodeMemberEvent
 _GetEvent(CodeTypeDeclaration type,string name,BindingFlags flags){for(int ic=type.Members.Count,i=0;i<ic;++i){var member=type.Members[i];var fld=member
 as CodeMemberEvent;if(null!=fld){if(0==string.Compare(fld.Name,name,StringComparison.InvariantCulture)){if(HasBindingFlag(flags,BindingFlags.NonPublic)
&&MemberAttributes.Public!=(fld.Attributes&MemberAttributes.AccessMask)||(HasBindingFlag(flags,BindingFlags.Public)&&MemberAttributes.Public==(fld.Attributes
&MemberAttributes.AccessMask))){if(HasBindingFlag(flags,BindingFlags.Static)&&MemberAttributes.Static==(fld.Attributes&MemberAttributes.ScopeMask)||MemberAttributes.Const
==(fld.Attributes&MemberAttributes.ScopeMask)||(HasBindingFlag(flags,BindingFlags.Instance)&&MemberAttributes.Static!=(fld.Attributes&MemberAttributes.ScopeMask)
&&MemberAttributes.Const!=(fld.Attributes&MemberAttributes.ScopeMask))){return fld;}}}}}if(!HasBindingFlag(flags,BindingFlags.DeclaredOnly)){for(int ic
=type.BaseTypes.Count,i=0;i<ic;++i){ var bt=type.BaseTypes[i];var td=_resolver.TryResolveType(bt,_scope)as CodeTypeDeclaration;if(null!=td){var eve=_GetEvent(td,
name,flags);if(HasBindingFlag(flags,BindingFlags.FlattenHierarchy)){if((eve.Attributes&MemberAttributes.AccessMask)!=MemberAttributes.Private)return eve;
}else{if((eve.Attributes&MemberAttributes.ScopeMask)!=MemberAttributes.Static)return eve;}}}}return null;}}}namespace CD{using E=CodeTypeReferenceEqualityComparer;
using R=CodeDomResolver;partial class CodeDomBinder{/// <summary>
/// Selects the property that matches the given signature
/// </summary>
/// <param name="flags">The binding flags to use</param>
/// <param name="match">The properties to evaluate</param>
/// <param name="types">The parameter types to compare with the signature</param>
/// <param name="modifiers">Not used</param>
/// <returns>The property that matches the signature, or null if none could be found</returns>
public MethodBase SelectMethod(BindingFlags flags,MethodBase[]match,CodeTypeReference[]types,ParameterModifier[]modifiers){int i;int j; if(match==null
||match.Length==0)throw new ArgumentException("The array cannot be null or empty",nameof(match));MethodBase[]candidates=(MethodBase[])match.Clone(); int
 CurIdx=0;for(i=0;i<candidates.Length;i++){var par=_GetParamInfos(candidates[i].GetParameters());if(par.Length!=types.Length)continue;for(j=0;j<types.Length;
j++){var pCls=par[j].ParameterType;if(E.Equals(pCls,types[j]))continue;if(E.Equals(pCls,_ObjType))continue;if(R.IsPrimitiveType(pCls)){var type=types[j];
if(!R.IsPrimitiveType(type)||!_resolver.CanConvertTo(type,pCls,_scope))break;}else{if(!_resolver.CanConvertTo(types[j],pCls,_scope,false))break;}}if(j
==types.Length)candidates[CurIdx++]=candidates[i];}if(CurIdx==0)return null;if(CurIdx==1)return candidates[0]; int currentMin=0;bool ambig=false;int[]
paramOrder=new int[types.Length];for(i=0;i<types.Length;i++)paramOrder[i]=i;for(i=1;i<CurIdx;i++){int newMin=FindMostSpecificMethod(candidates[currentMin],
paramOrder,null,candidates[i],paramOrder,null,types,null);if(newMin==0)ambig=true;else{if(newMin==2){currentMin=i;ambig=false;currentMin=i;}}}if(ambig)
throw new AmbiguousMatchException("Multiple members matched the target argument types");return candidates[currentMin];}/// <summary>
/// Selects the property that matches the given signature
/// </summary>
/// <param name="flags">The binding flags to use</param>
/// <param name="match">The properties to evaluate</param>
/// <param name="returnType">The return type to evaluate or null to ignore</param>
/// <param name="indices">The indices to compare with the signature</param>
/// <param name="modifiers">Not used</param>
/// <returns>The property that matches the signature, or null if none could be found</returns>
public PropertyInfo SelectProperty(BindingFlags flags,PropertyInfo[]match,CodeTypeReference returnType,CodeTypeReference[]indices,ParameterModifier[]modifiers)
{ if(indices!=null&&!Contract.ForAll(indices,delegate(CodeTypeReference t){return!R.IsNullOrVoidType(t);})){Exception e; e=new ArgumentNullException("indexes");
throw e;}if(match==null||match.Length==0)throw new ArgumentException("The array cannot be null or empty",nameof(match));Contract.EndContractBlock();var
 candidates=(PropertyInfo[])match.Clone();int i,j=0; int CurIdx=0;int indexesLength=(indices!=null)?indices.Length:0;for(i=0;i<candidates.Length;i++){
if(indices!=null){var par=_GetParamInfos(candidates[i].GetIndexParameters());if(par.Length!=indexesLength)continue;for(j=0;j<indexesLength;j++){var pCls
=par[j].ParameterType; if(E.Equals(pCls,indices[j]))continue;if(E.Equals(pCls,_ObjType))continue;if(R.IsPrimitiveType(pCls)){var type=indices[j];if(!R.IsPrimitiveType(type)||
!_resolver.CanConvertTo(type,pCls,_scope))break;}else{if(!_resolver.CanConvertTo(indices[j],pCls,_scope,false))break;}}}if(j==indexesLength){if(!R.IsNullOrVoidType(returnType))
{if(candidates[i].PropertyType.IsPrimitive){if(R.IsPrimitiveType(returnType)||!_resolver.CanConvertTo(returnType,new CodeTypeReference(candidates[i].PropertyType),_scope))
continue;}else{if(!_resolver.CanConvertTo(returnType,new CodeTypeReference(candidates[i].PropertyType),_scope,false))continue;}}candidates[CurIdx++]=candidates[i];
}}if(CurIdx==0)return null;if(CurIdx==1)return candidates[0]; int currentMin=0;bool ambig=false;int[]paramOrder=new int[indexesLength];for(i=0;i<indexesLength;
i++)paramOrder[i]=i;for(i=1;i<CurIdx;i++){int newMin=FindMostSpecificType(new CodeTypeReference(candidates[currentMin].PropertyType),new CodeTypeReference(candidates[i].PropertyType),
returnType);if(newMin==0&&indices!=null)newMin=FindMostSpecific(_GetParamInfos(candidates[currentMin].GetIndexParameters()),paramOrder,null,_GetParamInfos(candidates[i].GetIndexParameters()),
paramOrder,null,indices,null);if(newMin==0){newMin=FindMostSpecificProperty(candidates[currentMin],candidates[i]);if(newMin==0)ambig=true;}if(newMin==
2){ambig=false;currentMin=i;}}if(ambig)throw new AmbiguousMatchException("Multiple members matched the target argument types");return candidates[currentMin];
}static KeyValuePair<CodeMemberMethod[],MethodInfo[]>_SplitMatchMethods(object[]match){var cml=new List<CodeMemberMethod>();var rml=new List<MethodInfo>();
for(var i=0;i<match.Length;i++){var m=match[i];var cm=m as CodeMemberMethod;if(null!=cm)cml.Add(cm);else{var rm=m as MethodInfo;if(null!=rm)rml.Add(rm);
}}var cma=cml.ToArray();var rma=rml.ToArray();return new KeyValuePair<CodeMemberMethod[],MethodInfo[]>(cma,rma);}static KeyValuePair<CodeMemberProperty[],
PropertyInfo[]>_SplitMatchProperties(object[]match){var cml=new List<CodeMemberProperty>();var rml=new List<PropertyInfo>();for(var i=0;i<match.Length;
i++){var m=match[i];var cm=m as CodeMemberProperty;if(null!=cm)cml.Add(cm);else{var rm=m as PropertyInfo;if(null!=rm)rml.Add(rm);}}var cma=cml.ToArray();
var rma=rml.ToArray();return new KeyValuePair<CodeMemberProperty[],PropertyInfo[]>(cma,rma);}/// <summary>
/// Selects the method from a given group of methods whose signature best matches the indicated signature
/// </summary>
/// <param name="bindingAttr">The <see cref="BindingFlags"/> to use</param>
/// <param name="match">The candidate members to evaluate</param>
/// <param name="types">The types to evaluate. If any are null or <see cref="System.Void"/> they are ignored</param>
/// <param name="modifiers">The parameter modifiers - not currently used</param>
/// <returns>The method that best matches the given signature, or null if not found</returns>
public object SelectMethod(BindingFlags bindingAttr,object[]match,CodeTypeReference[]types,ParameterModifier[]modifiers){if(null==match)throw new ArgumentNullException(nameof(match));
if(0==match.Length)throw new ArgumentException("The match array cannot be empty.",nameof(match));var k=_SplitMatchMethods(match);var csm=0<k.Key.Length
?SelectMethod(bindingAttr,k.Key,types,modifiers):null; var rsm=0<k.Value.Length?SelectMethod(bindingAttr,k.Value,types,modifiers):null;if(null!=csm){if
(null!=rsm)throw new AmbiguousMatchException("Multiple members matched the target argument types");return csm;}return rsm;}/// <summary>
/// Selects the property from a given group of properties whose signature best matches the indicated signature
/// </summary>
/// <param name="bindingAttr">The <see cref="BindingFlags"/> to use</param>
/// <param name="match">The candidate members to evaluate</param>
/// <param name="returnType">The return type to evaluate, or <see cref="System.Void"/> or null to ignore</param>
/// <param name="types">The types to evaluate. If any are null or <see cref="System.Void"/> they are ignored</param>
/// <param name="modifiers">The parameter modifiers - not currently used</param>
/// <returns>The property that best matches the given signature, or null if not found</returns>
public object SelectProperty(BindingFlags bindingAttr,object[]match,CodeTypeReference returnType,CodeTypeReference[]types,ParameterModifier[]modifiers)
{if(null==match)throw new ArgumentNullException(nameof(match));if(0==match.Length)throw new ArgumentException("The match array cannot be empty.",nameof(match));
var k=_SplitMatchProperties(match);var csm=0<k.Key.Length?SelectProperty(bindingAttr,k.Key,returnType,types,modifiers):null; var rsm=0<k.Value.Length?
SelectProperty(bindingAttr,k.Value,returnType,types,modifiers):null;if(null!=csm){if(null!=rsm)throw new AmbiguousMatchException("Multiple members matched the target argument types");
return csm;}return rsm;}/// <summary>
/// Selects the property that matches the given signature
/// </summary>
/// <param name="flags">The binding flags to use</param>
/// <param name="match">The properties to evaluate</param>
/// <param name="types">The parameter types to compare with the signature</param>
/// <param name="modifiers">Not used</param>
/// <returns>The property that matches the signature, or null if none could be found</returns>
public CodeMemberMethod SelectMethod(BindingFlags flags,CodeMemberMethod[]match,CodeTypeReference[]types,ParameterModifier[]modifiers){int i;int j; if
(match==null||match.Length==0)throw new ArgumentException("The array cannot be null or empty",nameof(match));CodeMemberMethod[]candidates=(CodeMemberMethod[])match.Clone();
 int CurIdx=0;for(i=0;i<candidates.Length;i++){var par=_GetParamInfos(candidates[i].Parameters,_scope);if(par.Length!=types.Length)continue;for(j=0;j<
types.Length;j++){var pCls=par[j].ParameterType;if(null==par[j].ParameterType)continue;if(E.Equals(pCls,types[j]))continue;if(0==pCls.ArrayRank&&"System.Object"
==pCls.BaseType)continue;if(R.IsPrimitiveType(pCls)){var type=types[j];if(!R.IsPrimitiveType(type)||!_resolver.CanConvertTo(type,pCls,_scope,true))break;
}else{if(!_resolver.CanConvertTo(types[j],pCls,_scope,false))break;}}if(j==types.Length)candidates[CurIdx++]=candidates[i];}if(CurIdx==0)return null;if
(CurIdx==1)return candidates[0]; int currentMin=0;bool ambig=false;int[]paramOrder=new int[types.Length];for(i=0;i<types.Length;i++)paramOrder[i]=i;for
(i=1;i<CurIdx;i++){int newMin=FindMostSpecificMethod(candidates[currentMin],paramOrder,null,candidates[i],paramOrder,null,types,null);if(newMin==0)ambig
=true;else{if(newMin==2){currentMin=i;ambig=false;currentMin=i;}}}if(ambig)throw new AmbiguousMatchException("Multiple members matched the target argument types");
return candidates[currentMin];}/// <summary>
/// Selects the property that matches the given signature
/// </summary>
/// <param name="flags">The binding flags to use</param>
/// <param name="match">The properties to evaluate</param>
/// <param name="returnType">The return type to evaluate or null to ignore</param>
/// <param name="indices">The indices to compare with the signature</param>
/// <param name="modifiers">Not used</param>
/// <returns>The property that matches the signature, or null if none could be found</returns>
public CodeMemberProperty SelectProperty(BindingFlags flags,CodeMemberProperty[]match,CodeTypeReference returnType,CodeTypeReference[]indices,ParameterModifier[]
modifiers){ if(indices!=null&&!Contract.ForAll(indices,delegate(CodeTypeReference t){return t!=null&&0!=string.Compare(t.BaseType,"System.Void",StringComparison.InvariantCulture);
})){Exception e; e=new ArgumentNullException("indexes");throw e;}if(match==null||match.Length==0)throw new ArgumentException("The array cannot be null or empty",
nameof(match));var candidates=(CodeMemberProperty[])match.Clone();int i,j=0; int CurIdx=0;int indexesLength=(indices!=null)?indices.Length:0;for(i=0;i
<candidates.Length;i++){if(indices!=null){var par=candidates[i].Parameters;if(par.Count!=indexesLength)continue;for(j=0;j<indexesLength;j++){var pCls=
par[j].Type;if(null==pCls||0==string.Compare("System.Void",pCls.BaseType,StringComparison.InvariantCulture))continue; if(pCls==indices[j])continue;if(0
==pCls.ArrayRank&&0==string.Compare("System.Object",pCls.BaseType))continue;if(CodeDomResolver.IsPrimitiveType(pCls)){var type=indices[j];if(!CodeDomResolver.IsPrimitiveType(type)
||!_resolver.CanConvertTo(type,pCls,_scope))break;}else{if(!_resolver.CanConvertTo(indices[j],pCls,_scope,false))break;}}}if(j==indexesLength){if(returnType
!=null){if(CodeDomResolver.IsPrimitiveType(candidates[i].Type)){if(CodeDomResolver.IsPrimitiveType(returnType)||!_resolver.CanConvertTo(returnType,candidates[i].Type,
_scope))continue;}else{if(!_resolver.CanConvertTo(returnType,candidates[i].Type,_scope,false))continue;}}candidates[CurIdx++]=candidates[i];}}if(CurIdx
==0)return null;if(CurIdx==1)return candidates[0]; int currentMin=0;bool ambig=false;int[]paramOrder=new int[indexesLength];for(i=0;i<indexesLength;i++)
paramOrder[i]=i;for(i=1;i<CurIdx;i++){int newMin=FindMostSpecificType(candidates[currentMin].Type,candidates[i].Type,returnType);if(newMin==0&&indices
!=null)newMin=FindMostSpecific(_GetParamInfos(candidates[currentMin].Parameters,_scope),paramOrder,null,_GetParamInfos(candidates[i].Parameters,_scope),
paramOrder,null,indices,null);if(newMin==0){newMin=FindMostSpecificProperty(candidates[currentMin],candidates[i]);if(newMin==0)ambig=true;}if(newMin==
2){ambig=false;currentMin=i;}}if(ambig)throw new AmbiguousMatchException("Multiple members matched the target argument types");return candidates[currentMin];
}}}namespace CD{class CodeDomBuilder{public static CodeParameterDeclarationExpression ParameterDeclarationExpression(CodeTypeReference type,string name,
FieldDirection direction,CodeAttributeDeclaration[]customAttributes){var result=new CodeParameterDeclarationExpression(type,name);result.Direction=direction;
result.CustomAttributes.AddRange(customAttributes);return result;}public static CodeAssignStatement AssignStatement(CodeExpression left,CodeExpression
 right,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeAssignStatement(left,right);result.StartDirectives.AddRange(startDirectives);
result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static CodeAttachEventStatement AttachEventStatement(CodeEventReferenceExpression
 eventRef,CodeExpression listener,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeAttachEventStatement(eventRef,
listener);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public
 static CodeCommentStatement CommentStatement(CodeComment comment,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma)
{var result=new CodeCommentStatement(comment);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma
=linePragma;return result;}public static CodeConditionStatement ConditionStatement(CodeExpression condition,CodeStatement[]trueStatements,CodeStatement[]
falseStatements,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeConditionStatement(condition,trueStatements,falseStatements);
result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static
 CodeExpressionStatement ExpressionStatement(CodeExpression expression,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma)
{var result=new CodeExpressionStatement(expression);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma
=linePragma;return result;}public static CodeGotoStatement GotoStatement(string label,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma
 linePragma){var result=new CodeGotoStatement(label);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma
=linePragma;return result;}public static CodeIterationStatement IterationStatement(CodeStatement initStatement,CodeExpression testExpression,CodeStatement
 incrementStatement,CodeStatement[]statements,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeIterationStatement(initStatement,
testExpression,incrementStatement,statements);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma
=linePragma;return result;}public static CodeLabeledStatement LabeledStatement(string label,CodeStatement statement,CodeDirective[]startDirectives,CodeDirective[]
endDirectives,CodeLinePragma linePragma){var result=new CodeLabeledStatement(label,statement);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);
result.LinePragma=linePragma;return result;}public static CodeMethodReturnStatement MethodReturnStatement(CodeExpression expression,CodeDirective[]startDirectives,
CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeMethodReturnStatement(expression);result.StartDirectives.AddRange(startDirectives);
result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static CodeRemoveEventStatement RemoveEventStatement(CodeEventReferenceExpression
 eventRef,CodeExpression listener,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeRemoveEventStatement(eventRef,listener);
result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static
 CodeSnippetStatement SnippetStatement(string value,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=
new CodeSnippetStatement(value);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;
return result;}public static CodeThrowExceptionStatement ThrowExceptionStatement(CodeExpression toThrow,CodeDirective[]startDirectives,CodeDirective[]
endDirectives,CodeLinePragma linePragma){var result=new CodeThrowExceptionStatement(toThrow);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);
result.LinePragma=linePragma;return result;}public static CodeTryCatchFinallyStatement TryCatchFinallyStatement(CodeStatement[]tryStatements,CodeCatchClause[]
catchClauses,CodeStatement[]finallyStatements,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeTryCatchFinallyStatement(tryStatements,
catchClauses,finallyStatements);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;
return result;}public static CodeVariableDeclarationStatement VariableDeclarationStatement(CodeTypeReference type,string name,CodeExpression initExpression,
CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeVariableDeclarationStatement(type,name,initExpression);
result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static
 CodeTypeReference TypeReference(string baseType,CodeTypeReferenceOptions options,CodeTypeReference[]typeArguments,CodeTypeReference arrayElementType,int
 arrayRank){var result=new CodeTypeReference(baseType,options);result.ArrayElementType=arrayElementType;result.ArrayRank=arrayRank;result.TypeArguments.AddRange(typeArguments);
return result;}public static CodeMemberField MemberField(CodeTypeReference type,string name,CodeExpression initExpression,MemberAttributes attributes,
CodeCommentStatement[]comments,CodeAttributeDeclaration[]customAttributes,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma)
{var result=new CodeMemberField(type,name);result.InitExpression=initExpression;result.Attributes=attributes;result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);
result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static
 CodeMemberEvent MemberEvent(CodeTypeReference type,string name,MemberAttributes attributes,CodeTypeReference[]implementationTypes,CodeTypeReference privateImplementationType,
CodeCommentStatement[]comments,CodeAttributeDeclaration[]customAttributes,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma)
{var result=new CodeMemberEvent();result.Type=type;result.Name=name;result.Attributes=attributes;result.ImplementationTypes.AddRange(implementationTypes);
result.PrivateImplementationType=privateImplementationType;result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);result.StartDirectives.AddRange(startDirectives);
result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static CodeMemberMethod MemberMethod(CodeTypeReference
 returnType,string name,MemberAttributes attributes,CodeParameterDeclarationExpression[]parameters,CodeStatement[]statements,CodeTypeReference[]implementationTypes,
CodeTypeReference privateImplementationType,CodeCommentStatement[]comments,CodeAttributeDeclaration[]customAttributes,CodeAttributeDeclaration[]returnTypeCustomAttributes,
CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeMemberMethod();result.ReturnType=returnType;
result.Name=name;result.Attributes=attributes;result.Parameters.AddRange(parameters);result.Statements.AddRange(statements);result.ImplementationTypes.AddRange(implementationTypes);
result.PrivateImplementationType=privateImplementationType;result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);result.ReturnTypeCustomAttributes.AddRange(returnTypeCustomAttributes);
result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static
 CodeEntryPointMethod EntryPointMethod(CodeTypeReference returnType,string name,MemberAttributes attributes,CodeParameterDeclarationExpression[]parameters,
CodeStatement[]statements,CodeTypeReference[]implementationTypes,CodeTypeReference privateImplementationType,CodeCommentStatement[]comments,CodeAttributeDeclaration[]
customAttributes,CodeAttributeDeclaration[]returnTypeCustomAttributes,CodeDirective[]startDirectives,CodeDirective[]endDirectives,CodeLinePragma linePragma)
{var result=new CodeEntryPointMethod();result.ReturnType=returnType;result.Name=name;result.Attributes=attributes;result.Parameters.AddRange(parameters);
result.Statements.AddRange(statements);result.ImplementationTypes.AddRange(implementationTypes);result.PrivateImplementationType=privateImplementationType;
result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);result.ReturnTypeCustomAttributes.AddRange(returnTypeCustomAttributes);
result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static
 CodeConstructor Constructor(MemberAttributes attributes,CodeParameterDeclarationExpression[]parameters,CodeExpression[]chainedConstructorArgs,CodeExpression[]
baseConstructorArgs,CodeStatement[]statements,CodeCommentStatement[]comments,CodeAttributeDeclaration[]customAttributes,CodeDirective[]startDirectives,
CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeConstructor();result.Attributes=attributes;result.Parameters.AddRange(parameters);
result.ChainedConstructorArgs.AddRange(chainedConstructorArgs);result.BaseConstructorArgs.AddRange(baseConstructorArgs);result.Statements.AddRange(statements);
result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);
result.LinePragma=linePragma;return result;}public static CodeTypeConstructor TypeConstructor(MemberAttributes attributes,CodeParameterDeclarationExpression[]
parameters,CodeStatement[]statements,CodeCommentStatement[]comments,CodeAttributeDeclaration[]customAttributes,CodeDirective[]startDirectives,CodeDirective[]
endDirectives,CodeLinePragma linePragma){var result=new CodeTypeConstructor();result.Attributes=attributes;result.Parameters.AddRange(parameters);result.Statements.AddRange(statements);
result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);
result.LinePragma=linePragma;return result;}public static CodeMemberProperty MemberProperty(CodeTypeReference type,string name,MemberAttributes attributes,
CodeParameterDeclarationExpression[]parameters,CodeStatement[]getStatements,CodeStatement[]setStatements,CodeTypeReference[]implementationTypes,CodeTypeReference
 privateImplementationType,CodeCommentStatement[]comments,CodeAttributeDeclaration[]customAttributes,CodeDirective[]startDirectives,CodeDirective[]endDirectives,
CodeLinePragma linePragma){var result=new CodeMemberProperty();result.Type=type;result.Name=name;result.Attributes=attributes;result.Parameters.AddRange(parameters);
result.GetStatements.AddRange(getStatements);result.SetStatements.AddRange(setStatements);result.ImplementationTypes.AddRange(implementationTypes);result.PrivateImplementationType
=privateImplementationType;result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);result.StartDirectives.AddRange(startDirectives);
result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static CodeTypeDeclaration TypeDeclaration(string name,
bool isClass,bool isEnum,bool isInterface,bool isStruct,bool isPartial,MemberAttributes attributes,TypeAttributes typeAttributes,CodeTypeParameter[]typeParameters,
CodeTypeReference[]baseTypes,CodeTypeMember[]members,CodeCommentStatement[]comments,CodeAttributeDeclaration[]customAttributes,CodeDirective[]startDirectives,
CodeDirective[]endDirectives,CodeLinePragma linePragma){var result=new CodeTypeDeclaration(name);result.IsClass=isClass;result.IsEnum=isEnum;result.IsInterface
=isInterface;result.IsStruct=isStruct;result.IsPartial=isPartial;result.Attributes=attributes;result.TypeAttributes=typeAttributes;result.TypeParameters.AddRange(typeParameters);
result.BaseTypes.AddRange(baseTypes);result.Members.AddRange(members);result.Comments.AddRange(comments);result.CustomAttributes.AddRange(customAttributes);
result.StartDirectives.AddRange(startDirectives);result.EndDirectives.AddRange(endDirectives);result.LinePragma=linePragma;return result;}public static
 CodeTypeParameter TypeParameter(string name,bool hasConstructorConstraint,CodeTypeReference[]constraints,CodeAttributeDeclaration[]customAttributes){
var result=new CodeTypeParameter(name);result.HasConstructorConstraint=hasConstructorConstraint;result.Constraints.AddRange(constraints);result.CustomAttributes.AddRange(customAttributes);
return result;}public static CodeNamespace Namespace(string name,CodeNamespaceImport[]imports,CodeTypeDeclaration[]types,CodeCommentStatement[]comments)
{var result=new CodeNamespace();result.Imports.AddRange(imports);result.Types.AddRange(types);result.Comments.AddRange(comments);return result;}public
 static CodeNamespaceImport NamespaceImport(string nameSpace,CodeLinePragma linePragma){var result=new CodeNamespaceImport(nameSpace);result.LinePragma
=linePragma;return result;}public static CodeCompileUnit CompileUnit(string[]referencedAssemblies,CodeNamespace[]namespaces,CodeAttributeDeclaration[]
assemblyCustomAttributes,CodeDirective[]startDirectives,CodeDirective[]endDirectives){var result=new CodeCompileUnit();result.ReferencedAssemblies.AddRange(referencedAssemblies);
result.Namespaces.AddRange(namespaces);result.AssemblyCustomAttributes.AddRange(assemblyCustomAttributes);result.StartDirectives.AddRange(startDirectives);
result.EndDirectives.AddRange(endDirectives);return result;}}}namespace CD{/// <summary>
/// Provides services for doing type and scope resolution on CodeDOM graphs
/// </summary>
#if GOKITLIB
public
#endif
partial class CodeDomResolver{const int _ResolveAssemblies=2;const int _ResolveCompileUnits=1;static readonly object _parentKey=new object();static readonly
 object _rootKey=new object();IDictionary<CodeTypeReference,Type>_typeCache=new Dictionary<CodeTypeReference,Type>(CodeTypeReferenceEqualityComparer.Default);
/// <summary>
/// Retrieves the compile units list the resolver draws on
/// </summary>
/// <remarks>Be sure to call Refresh() and possibly ClearCache() after adding and removing compile units</remarks>
public IList<CodeCompileUnit>CompileUnits{get;}=new List<CodeCompileUnit>();/// <summary>
/// Creates a new CodeDomResolver
/// </summary>
public CodeDomResolver(){}internal IDictionary<string,CodeTypeReference>GetArgumentTypes(CodeDomResolverScope scope){var result=new Dictionary<string,
CodeTypeReference>();var meth=scope.Member as CodeMemberMethod;if(null!=meth)foreach(CodeParameterDeclarationExpression arg in meth.Parameters)result.Add(arg.Name,
arg.Type);var prop=scope.Member as CodeMemberProperty;if(null!=prop)foreach(CodeParameterDeclarationExpression arg in prop.Parameters)result.Add(arg.Name,
arg.Type);return result;}internal IDictionary<string,CodeTypeReference>GetVariableTypes(CodeDomResolverScope scope){var result=new Dictionary<string,CodeTypeReference>();
if(null==scope.Member||null==scope.Statement)return result; foreach(var v in CodeDomVariableTracer.Trace(scope.Member,scope.Statement))result.Add(v.Name,
v.Type);return result;}static bool _TraceVarDecls(CodeStatement s,CodeStatement target,IDictionary<string,CodeTypeReference>result){ if(s==target)return
 true;var ls=s as CodeLabeledStatement;if(null!=ls){var l=new Dictionary<string,CodeTypeReference>();var v=ls.Statement as CodeVariableDeclarationStatement;
if(null!=v)l.Add(v.Name,v.Type);else if(_TraceVarDecls(ls.Statement,target,l)){foreach(var ll in l)result.Add(ll);return true;}}var i=s as CodeIterationStatement;
if(null!=i){var l=new Dictionary<string,CodeTypeReference>();if(i.InitStatement!=null){var v=i.InitStatement as CodeVariableDeclarationStatement;if(null
!=v)l.Add(v.Name,v.Type);else if(_TraceVarDecls(i.InitStatement,target,l)){foreach(var ll in l)result.Add(ll);return true;}}foreach(CodeStatement ts in
 i.Statements){var v=ts as CodeVariableDeclarationStatement;if(null!=v)l.Add(v.Name,v.Type);else if(_TraceVarDecls(ts,target,l)){foreach(var ll in l)result.Add(ll);
return true;}}if(i.IncrementStatement!=null){var v=i.IncrementStatement as CodeVariableDeclarationStatement;if(null!=v)l.Add(v.Name,v.Type);else if(_TraceVarDecls(i.IncrementStatement,
target,l)){foreach(var ll in l)result.Add(ll);return true;}}}var c=s as CodeConditionStatement;if(null!=c){var l=new Dictionary<string,CodeTypeReference>();
foreach(CodeStatement ts in c.TrueStatements){var v=ts as CodeVariableDeclarationStatement;if(null!=v)l.Add(v.Name,v.Type);else if(_TraceVarDecls(ts,target,
l)){foreach(var ll in l)result.Add(ll);return true;}}l.Clear();foreach(CodeStatement fs in c.FalseStatements){var v=fs as CodeVariableDeclarationStatement;
if(null!=v)l.Add(v.Name,v.Type);else if(_TraceVarDecls(fs,target,l)){foreach(var ll in l)result.Add(ll);return true;}}}return false;}bool _IsInterface(CodeTypeReference
 r,CodeDomResolverScope scope){if(0<r.ArrayRank&&null!=r.ArrayElementType)return false; var t=_ResolveType(r,scope);var td=t as CodeTypeDeclaration;if
(null!=td)return td.IsInterface;var tt=t as Type;if(null!=tt)return tt.IsInterface;throw new TypeLoadException(string.Format("Could not resolve type {0}",
CodeDomUtility.ToString(r)));}/// <summary>
/// Indicates whether or not the type is primitive
/// </summary>
/// <param name="type">The type</param>
/// <returns>True if the type is a primitive .NET type, otherwise false</returns>
public static bool IsPrimitiveType(CodeTypeReference type){if(0<type.ArrayRank&&null!=type.ArrayElementType)return false;if(0<type.TypeArguments.Count)
return false;switch(type.BaseType){case"System.Boolean":case"System.Char":case"System.String":case"System.SByte":case"System.Byte":case"System.Int16":
case"System.UInt16":case"System.Int32":case"System.UInt32":case"System.Int64":case"System.UInt64":case"System.Single":case"System.Double":case"System.Decimal":
return true;}return false;}/// <summary>
/// Translates an intrinsic Slang/C# type into a .NET type, or pass through
/// </summary>
/// <param name="typeName">The type name</param>
/// <returns>A system type name</returns>
public static string TranslateIntrinsicType(string typeName){switch(typeName){case"char":return"System.Char";case"string":return"System.String";case"sbyte":
return"System.SByte";case"byte":return"System.Byte";case"short":return"System.Int16";case"ushort":return"System.UInt16";case"int":return"System.Int32";
case"uint":return"System.UInt32";case"long":return"System.Int64";case"ulong":return"System.UInt64";case"float":return"System.Single";case"double":return
"System.Double";case"decimal":return"System.Decimal";}return typeName;}/// <summary>
/// Indicates whether or not one type can be converted to another
/// </summary>
/// <param name="from">The type to convert from</param>
/// <param name="to">The type to convert to</param>
/// <param name="scope">The scope to use for the evaluation, or null to use <paramref name="from"/>'s scope</param>
/// <param name="useTypeConversion">True to use .NET's type conversion capabilities or false to simply check if one type is polymorphic with another</param>
/// <returns>True if the conversion can be performed, otherwise false</returns>
public bool CanConvertTo(CodeTypeReference from,CodeTypeReference to,CodeDomResolverScope scope=null,bool useTypeConversion=true){if(null==from)from=new
 CodeTypeReference(typeof(void));if(null==to)to=new CodeTypeReference(typeof(void));if(CodeTypeReferenceEqualityComparer.Equals(from,to))return true;if
(null==scope)scope=GetScope(from);var t1=TryResolveType(from,scope);if(null==t1)throw new TypeLoadException(string.Format("The type {0} could not be resolved",
CodeDomUtility.ToString(from)));var t2=TryResolveType(to,scope);if(null==t2)throw new TypeLoadException(string.Format("The type {0} could not be resolved",
CodeDomUtility.ToString(to)));var type1=t1 as Type;var type2=t2 as Type;if(null!=type1&&null!=type2){if(type2.IsAssignableFrom(type1))return true;if(useTypeConversion)
{TypeConverter typeConverter=TypeDescriptor.GetConverter(type1);if(null!=typeConverter&&typeConverter.CanConvertTo(type2))return true;}return false;}var
 decl1=t1 as CodeTypeDeclaration;var decl2=t2 as CodeTypeDeclaration;if(null!=decl1){if(null==scope)scope=GetScope(decl1);if(null!=decl2&&decl1.IsPartial
&&decl2.IsPartial&&0==string.Compare(GetBaseNameOfType(decl1,scope),GetBaseNameOfType(decl2,scope),StringComparison.InvariantCulture))return true; var
 bts=new HashSet<CodeTypeReference>(CodeTypeReferenceEqualityComparer.Default);for(int ic=decl1.BaseTypes.Count,i=0;i<ic;++i)bts.Add(GetQualifiedType(decl1.BaseTypes[i],
scope));CodeTypeReference ctr=null;if(null!=decl2)ctr=GetType(decl2,scope);else ctr=new CodeTypeReference(type2);return bts.Contains(ctr);} return false;
}/// <summary>
/// Retrieves the base type of a <see cref="CodeTypeDeclaration"/> or of a <see cref="System.Type"/>
/// </summary>
/// <param name="type">The type to evaluate</param>
/// <param name="scope">The scope in which evaluation occurs, or null to use the type</param>
/// <returns>A type reference that refers to the base type</returns>
public CodeTypeReference GetBaseType(object type,CodeDomResolverScope scope=null){var tr=type as CodeTypeReference;if(null!=tr){if(null==scope)GetScope(tr);
type=_ResolveType(tr,scope);}var td=type as CodeTypeDeclaration;if(null!=td){if(0==td.BaseTypes.Count)return new CodeTypeReference((td.IsStruct||td.IsEnum)
?(td.IsEnum?typeof(ValueType):typeof(Enum)):typeof(object));if(null==scope)scope=GetScope(td);if(_IsInterface(td.BaseTypes[0],scope))return new CodeTypeReference((td.IsStruct
||td.IsEnum)?(td.IsEnum?typeof(ValueType):typeof(Enum)):typeof(object));return GetQualifiedType(td.BaseTypes[0],scope);}var t=type as Type;if(null!=t)
{var bt=t.BaseType;if(null==bt)return null;return new CodeTypeReference(bt);}throw new ArgumentException("The type must be a type, a type declartion or a type reference");
}internal static CodeTypeReference GetTypeForMember(MemberInfo member){if(null==member)throw new ArgumentNullException(nameof(member));var e=member as
 EventInfo;if(null!=e)return new CodeTypeReference(e.EventHandlerType);var f=member as FieldInfo;if(null!=f)return new CodeTypeReference(f.FieldType);
var p=member as PropertyInfo;if(null!=p)return new CodeTypeReference(p.PropertyType);var m=member as MethodInfo;if(null!=m)return new CodeTypeReference(m.ReturnType);
return null;}internal static CodeTypeReference GetTypeForMember(CodeTypeMember member){if(null==member)throw new ArgumentNullException(nameof(member));
var f=member as CodeMemberField;if(null!=f){if(null==f.Type)throw new InvalidOperationException("The field declaration's type was null.");return f.Type;
}var e=member as CodeMemberEvent;if(null!=e){if(null==e.Type)throw new InvalidOperationException("The event declaration's type was null");return e.Type;
}var m=member as CodeMemberMethod;if(null!=m){if(null==m.ReturnType)return new CodeTypeReference(typeof(void));return m.ReturnType;}var p=member as CodeMemberProperty;
if(null!=p){if(null==p.Type)throw new InvalidOperationException("The property declaration's type was null");return p.Type;}throw new InvalidOperationException("The specified member does not have a type");
}CodeDomResolverScope _FillScope(CodeDomResolverScope result){CodeCompileUnit ccu=null;object p;if(null==result.Expression){if(null!=result.TypeRef){p
=result.TypeRef;if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;while(null!=(p=_GetRef(p,_parentKey))){var expr=p as CodeExpression;if(null!=expr)
{result.Expression=expr;break;}}}}if(null==result.Statement){if(null!=result.Expression){p=result.Expression;if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;
while(null!=(p=_GetRef(p,_parentKey))){var stmt=p as CodeStatement;if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;if(null!=stmt){result.Statement
=stmt;break;}}}else if(null!=result.TypeRef){p=result.TypeRef;if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;while(null!=(p=_GetRef(p,_parentKey)))
{var stmt=p as CodeStatement;if(null!=stmt){result.Statement=stmt;break;}}}}if(null==result.Member){p=null;if(null!=result.Statement){p=result.Statement;
}else if(null!=result.Expression)p=result.Expression;if(null!=p){if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;while(null!=(p=_GetRef(p,_parentKey)))
{var mbr=p as CodeTypeMember;if(null!=mbr){result.Member=mbr;break;}}}}p=null;if(0<result.Types.Count)p=result.Types[0];else if(null!=result.Member)p=
result.Member;else if(null!=result.Statement)p=result.Statement;else if(null!=result.Expression)p=result.Expression;else if(null!=result.TypeRef)p=result.TypeRef;
if(null!=p){if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;while(null!=(p=_GetRef(p,_parentKey))){var td=p as CodeTypeDeclaration;if(null!=td)
result.Types.Add(td);}}if(null==result.Namespace){p=null;if(0!=result.Types.Count)p=result.Types[0];else if(null!=result.Member)p=result.Member;if(null
!=p){if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;while(null!=(p=_GetRef(p,_parentKey))){var ns=p as CodeNamespace;if(null!=ns){result.Namespace
=ns;break;}}}}if(null==result.CompileUnit){p=null;if(null!=result.Namespace)p=result.Namespace;while(null!=(p=_GetRef(p,_parentKey))){var cu=p as CodeCompileUnit;
if(null!=cu){result.CompileUnit=cu;break;}}}if(null==result.CompileUnit){ result.CompileUnit=ccu;}return result;}internal HashSet<string>GetPropertyNames(CodeDomResolverScope
 scope){var result=new HashSet<string>();var t=scope.DeclaringType;if(null!=t){var binder=new CodeDomBinder(scope);var members=binder.GetMembers(t,MemberTypes.Property,
BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);foreach(var m in members){var cpi=m as CodeMemberProperty;if(null
!=cpi)result.Add(cpi.Name);var pi=m as PropertyInfo;if(null!=pi){if(!pi.IsSpecialName)result.Add(pi.Name);}}}return result;}/// <summary>
/// Indicates whether the type reference is null or refers to <see cref="System.Void"/>
/// </summary>
/// <param name="type">The type to evaluate</param>
/// <returns>True if the type is null or void, otherwise false</returns>
public static bool IsNullOrVoidType(CodeTypeReference type){ return null==type||(0==type.ArrayRank&&0==string.Compare("System.Void",type.BaseType,StringComparison.InvariantCulture)
||(0==type.ArrayRank&&0==string.Compare("var",type.BaseType,StringComparison.InvariantCulture)));}/// <summary>
/// Indicates whether the specified type is a value type
/// </summary>
/// <param name="type">The type</param>
/// <param name="scope">The scope or null</param>
/// <returns>True if the type is a value type, otherwise false</returns>
public bool IsValueType(CodeTypeReference type,CodeDomResolverScope scope=null){if(IsNullOrVoidType(type))return false;if(0<type.ArrayRank)return false;
if(null==scope){scope=GetScope(type);}var t=TryResolveType(type,scope);if(null==t)throw new TypeLoadException("Unable to resolve type");var rt=t as Type;
if(null!=rt){return rt.IsValueType;}var td=t as CodeTypeDeclaration;return td.IsEnum||td.IsStruct;}internal HashSet<string>GetFieldNames(CodeDomResolverScope
 scope){var result=new HashSet<string>();var t=scope.DeclaringType;if(null!=t){var binder=new CodeDomBinder(scope);var members=binder.GetMembers(t,MemberTypes.Field,
BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance|BindingFlags.Static);foreach(var m in members){var cpi=m as CodeMemberField;if(null!=
cpi)result.Add(cpi.Name);var pi=m as FieldInfo;if(null!=pi){if(!pi.IsSpecialName)result.Add(pi.Name);}}}return result;}internal HashSet<string>GetEventNames(CodeDomResolverScope
 scope){var result=new HashSet<string>();var t=scope.DeclaringType;if(null!=t){var binder=new CodeDomBinder(scope);var members=binder.GetMembers(t,MemberTypes.Event,
BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);foreach(var m in members){var cpi=m as CodeMemberEvent;if(null!=
cpi)result.Add(cpi.Name);var pi=m as EventInfo;if(null!=pi){if(!pi.IsSpecialName)result.Add(pi.Name);}}}return result;}internal HashSet<string>GetMethodNames(CodeDomResolverScope
 scope){var result=new HashSet<string>();var t=scope.DeclaringType;if(null!=t){var binder=new CodeDomBinder(scope);var members=binder.GetMembers(t,MemberTypes.Method,
BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);foreach(var m in members){var cpi=m as CodeMemberMethod;if(null!=
(cpi as CodeConstructor))cpi=null;if(null!=cpi)result.Add(cpi.Name);var pi=m as MethodInfo;if(null!=pi&&pi.IsConstructor)pi=null;if(null!=pi){if(!pi.IsSpecialName)
result.Add(pi.Name);}}}return result;}internal IDictionary<string,CodeTypeReference>GetTypeTargets(CodeDomResolverScope scope){var result=new Dictionary<string,
CodeTypeReference>();var t=scope.DeclaringType;if(null!=t){var binder=new CodeDomBinder(scope);var members=binder.GetMembers(t,MemberTypes.All,BindingFlags.Public
|BindingFlags.NonPublic|BindingFlags.Static);foreach(var m in members){var ctm=m as CodeTypeMember;if(null!=ctm){ var cttr=new CodeTypeReference(GetBaseNameOfType(t,
scope));foreach(CodeTypeParameter ctp in t.TypeParameters)cttr.TypeArguments.Add(new CodeTypeReference(ctp)); if(!result.ContainsKey(ctm.Name))result.Add(ctm.Name,
cttr);}}}return result;}internal HashSet<string>GetBaseTargets(CodeDomResolverScope scope){ throw new NotImplementedException("Base references need to be implemented");
}internal HashSet<string>GetThisTargets(CodeDomResolverScope scope){var result=new HashSet<string>();var t=scope.DeclaringType;if(null!=t){var binder=
new CodeDomBinder(scope);var members=binder.GetMembers(t,MemberTypes.All,BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Instance);foreach(var
 m in members){var ctm=m as CodeTypeMember;if(null!=ctm){ result.Add(ctm.Name);}}}return result;}internal HashSet<string>GetMemberNames(CodeDomResolverScope
 scope){var result=new HashSet<string>();var t=scope.DeclaringType;if(null!=t){var binder=new CodeDomBinder(scope);var members=binder.GetMembers(t,MemberTypes.All,BindingFlags.Instance
|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic);foreach(var m in members){var ctm=m as CodeTypeMember;if(null!=ctm){result.Add(ctm.Name);
}else{var mi=m as MemberInfo;result.Add(mi.Name);}}}return result;}/// <summary>
/// Gets the scope for the specified object
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeObject target){var ccu=target as CodeCompileUnit;if(null!=ccu)return GetScope(target);var ns=target as CodeNamespace;
if(null!=ns)return GetScope(ns);var td=target as CodeTypeDeclaration;if(null!=td)return GetScope(td);var tm=target as CodeTypeMember;if(null!=tm)return
 GetScope(tm);var st=target as CodeStatement;if(null!=st)return GetScope(st);var ex=target as CodeExpression;if(null!=ex)return GetScope(ex);var tr=target
 as CodeTypeReference;if(null!=tr)return GetScope(tr);throw new ArgumentException("Cannot get the scope from this code object",nameof(target));}/// <summary>
/// Gets the scope for the specified type reference
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeTypeReference target){var result=new CodeDomResolverScope(this);result.TypeRef=target;return _FillScope(result);
}/// <summary>
/// Gets the scope for the specified expression
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeExpression target){var result=new CodeDomResolverScope(this);result.Expression=target;return _FillScope(result);
}/// <summary>
/// Gets the scope for the specified statement
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeStatement target){var result=new CodeDomResolverScope(this);result.Statement=target;return _FillScope(result);
}/// <summary>
/// Gets the scope for the specified member
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeTypeMember target){var result=new CodeDomResolverScope(this);result.Member=target;return _FillScope(result);}
/// <summary>
/// Gets the scope for the specified type declaration
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeTypeDeclaration target){var result=new CodeDomResolverScope(this);result.Types.Add(target);return _FillScope(result);
}/// <summary>
/// Gets the scope for the specified namespace
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeNamespace target){var result=new CodeDomResolverScope(this);result.Namespace=target;return _FillScope(result);
}/// <summary>
/// Gets the scope for the specified compile unit
/// </summary>
/// <param name="target">The target</param>
/// <returns>The scope</returns>
public CodeDomResolverScope GetScope(CodeCompileUnit target){var result=new CodeDomResolverScope(this);result.CompileUnit=target;return _FillScope(result);
}/// <summary>
/// Attempts to resolve a type at the optionally indicated scope
/// </summary>
/// <param name="type">The type to resolve</param>
/// <param name="scope">The scope at which the resolution occurs</param>
/// <param name="allowPartial">True if uninstantiated generics should be returned</param>
/// <returns>Either a runtime <see cref="Type"/> or a <see cref="CodeTypeDeclaration"/> representing the given type, or null if the type could not be resolved</returns>
/// <remarks>This routine cannot instantiate reified generic types of declared types, nor will it resolve types with declared types as generic arguments</remarks>
public object TryResolveType(CodeTypeReference type,CodeDomResolverScope scope=null,bool allowPartial=false)=>_ResolveType(type,scope,allowPartial);/// <summary>
/// Attempts to retrieve the fully qualified type for a given type, at the given scope
/// </summary>
/// <param name="type">The type to evaluate</param>
/// <param name="scope">The scope in which the evaluation occurs</param>
/// <param name="result">A value to hold the fully qualified type reference</param>
/// <returns>True if the operation was successful, otherwise false</returns>
public bool TryGetQualifiedType(CodeTypeReference type,CodeDomResolverScope scope,out CodeTypeReference result){if(0<type.ArrayRank&&null!=type.ArrayElementType)
{CodeTypeReference ctr;if(TryGetQualifiedType(type.ArrayElementType,scope,out ctr)){result=new CodeTypeReference();result.ArrayElementType=ctr;result.Options
=type.Options;result.TypeArguments.AddRange(type.TypeArguments);return true;}result=type;return false;}var r=_ResolveType(type,scope);if(null!=r){var t
=r as Type;if(null!=t){result=new CodeTypeReference(t);return true;}var td=r as CodeTypeDeclaration;if(null!=td){result=new CodeTypeReference(_GetBaseNameOfType(td),
type.Options);result.TypeArguments.AddRange(type.TypeArguments);return true;}}result=type;return false;}/// <summary>
/// Gets the fully qualified type for the specified type, at the given scope
/// </summary>
/// <param name="type">The type to resolve</param>
/// <param name="scope">The scope at which the resolution occurs</param>
/// <returns></returns>
public CodeTypeReference GetQualifiedType(CodeTypeReference type,CodeDomResolverScope scope){CodeTypeReference result;if(!TryGetQualifiedType(type,scope,
out result))throw new TypeLoadException("The type could not be resolved");return result;}static string _GetDecoratedTypeName(CodeTypeDeclaration decl)
{if(0<decl.TypeParameters.Count)return string.Concat(decl.Name,"`",decl.TypeParameters.Count);return decl.Name;}/// <summary>
/// Retrieves a <see cref="CodeTypeReference"/> that refers to the specified type declaration
/// </summary>
/// <param name="decl">The type to evaluate</param>
/// <param name="scope">The scope at which the evaluation occurs or null to use the type's scope</param>
/// <returns>A type reference that refers to the type declaraion</returns>
public CodeTypeReference GetType(CodeTypeDeclaration decl,CodeDomResolverScope scope){if(null==scope)scope=GetScope(decl);return new CodeTypeReference(GetBaseNameOfType(decl,scope));
}internal static string GetBaseNameOfType(CodeTypeDeclaration decl,CodeDomResolverScope scope){var result=scope.Namespace?.Name;if(string.IsNullOrEmpty(result))
result="";else result=string.Concat(result,".");var first=true;for(var i=scope.Types.Count-1;0<=i;--i){if(first){first=false;result=string.Concat(result,
_GetDecoratedTypeName(scope.Types[i]));}else result=string.Concat(result,"+",_GetDecoratedTypeName(scope.Types[i]));}return result;}string _GetBaseNameOfType(CodeTypeDeclaration
 decl,CodeDomResolverScope scope=null){if(null==scope)scope=GetScope(decl);return GetBaseNameOfType(decl,scope);}/// <summary>
/// Gets the declaring parent of the current code object
/// </summary>
/// <param name="target">The object to evaluate</param>
/// <returns>The parent, or null if none could be found</returns>
public static object GetParentOfCodeObject(object target){var co=target as CodeObject;if(null==co)return null;return _GetRef(target,_parentKey);}/// <summary>
/// Gets the root of the current code object - this is usually a <see cref="CodeCompileUnit"/>
/// </summary>
/// <param name="target">The object to evaluate</param>
/// <returns>The root, or null if none could be found</returns>
public static object GetRootOfCodeObject(object target){var co=target as CodeObject;if(null==co)return null;return _GetRef(target,_rootKey);} static object
 _GetRef(object target,object key){var co=target as CodeObject;if(null!=co){var wr=co.UserData[key]as WeakReference<object>;if(null!=wr){object result;
if(wr.TryGetTarget(out result))return result;}}return null;}static string _GetGenericName(CodeTypeDeclaration td){var result=td.Name;if(0!=td.TypeParameters.Count)
return string.Concat(result,"`",td.TypeParameters.Count);return result;}static string _BuildTypePrefix(CodeDomResolverScope scope,int numTypes){var result
="";if(null!=scope.Namespace&&!string.IsNullOrEmpty(scope.Namespace.Name))result=scope.Namespace.Name;if(null!=scope.Types){numTypes=Math.Min(numTypes,
scope.Types.Count);var first=0==result.Length;for(var i=scope.Types.Count-1;i>=Math.Max(0,(numTypes-1));--i){if(!first){if(result!=scope.Namespace.Name)
result=string.Concat(result,"+",_GetDecoratedTypeName(scope.Types[i]));else result=string.Concat(result,".",_GetDecoratedTypeName(scope.Types[i]));}else
{first=false;result=string.Concat(result,_GetDecoratedTypeName(scope.Types[i]));}}}if(result.StartsWith("SystemCollections"))System.Diagnostics.Debugger.Break();
return result;}object _ResolveType(CodeTypeReference type,CodeDomResolverScope scope,bool allowPartial=false){if(null==type)return null;if(null!=type.ArrayElementType
&&1<=type.ArrayRank){ return typeof(Array);}var nss=new List<string>();if(null!=scope.DeclaringType){nss.Add(_BuildTypePrefix(scope,scope.Types.Count));
if(1<scope.Types.Count){for(var i=0;i<scope.Types.Count-1;++i)nss.Add(_BuildTypePrefix(scope,i));}}if(null!=scope.CompileUnit){foreach(CodeNamespace ns
 in scope.CompileUnit.Namespaces){if(string.IsNullOrEmpty(ns.Name)){foreach(CodeNamespaceImport nsi in ns.Imports)nss.Add(string.Concat(nsi.Namespace));
}}}if(null!=scope.Namespace){if(!string.IsNullOrEmpty(scope.Namespace.Name)){nss.Add(scope.Namespace.Name);foreach(CodeNamespaceImport nsi in scope.Namespace.Imports)
{nss.Add(nsi.Namespace);nss.Add(string.Concat(scope.Namespace.Name,".",nsi.Namespace));}}}nss.Add("");var ctrs=new List<CodeTypeReference>();foreach(var
 pfx in nss){var s=pfx;if(0<s.Length)s=string.Concat(pfx,".",type.BaseType);else s=type.BaseType;var ctr=new CodeTypeReference();ctr.BaseType=s;ctr.TypeArguments.AddRange(type.TypeArguments);
ctrs.Add(ctr);}var t=_DualResolve(ctrs);var rt=t as Type;if(null!=rt&&0<type.TypeArguments.Count){var types=new Type[type.TypeArguments.Count];for(var
 i=0;i<types.Length;i++){types[i]=_ResolveType(type.TypeArguments[i],scope)as Type;if(null==types[i])return rt;}return rt.MakeGenericType(types);}return
 t;}/// <summary>
/// Indicates whether the type reference refers to a valid type in the given scope
/// </summary>
/// <param name="type">The type reference to evaluate</param>
/// <param name="scope">The scope at which evaluation takes place, or null to use <paramref name="type"/>'s scope</param>
/// <returns></returns>
public bool IsValidType(CodeTypeReference type,CodeDomResolverScope scope=null){if(null!=type.ArrayElementType&&1<=type.ArrayRank){return IsValidType(type.ArrayElementType,scope);
}if(scope==null)scope=GetScope(type);var nss=new List<string>();if(null!=scope.DeclaringType){nss.Add(_BuildTypePrefix(scope,scope.Types.Count));if(1<
scope.Types.Count){for(var i=0;i<scope.Types.Count-1;++i)nss.Add(_BuildTypePrefix(scope,i));}}if(null!=scope.CompileUnit){foreach(CodeNamespace ns in scope.CompileUnit.Namespaces)
{if(string.IsNullOrEmpty(ns.Name)){foreach(CodeNamespaceImport nsi in ns.Imports)nss.Add(string.Concat(nsi.Namespace));}}}if(null!=scope.Namespace){if
(!string.IsNullOrEmpty(scope.Namespace.Name)){nss.Add(scope.Namespace.Name);foreach(CodeNamespaceImport nsi in scope.Namespace.Imports){nss.Add(nsi.Namespace);
nss.Add(string.Concat(scope.Namespace.Name,".",nsi.Namespace));}}}nss.Add("");var ctrs=new List<CodeTypeReference>();foreach(var pfx in nss){var s=pfx;
if(0<s.Length)s=string.Concat(pfx,".",type.BaseType);else s=type.BaseType;var ctr=new CodeTypeReference();ctr.BaseType=s;ctr.TypeArguments.AddRange(type.TypeArguments);
ctrs.Add(ctr);}var t=_DualResolve(ctrs);if(null==t)return false;if(0<type.TypeArguments.Count){var types=new Type[type.TypeArguments.Count];for(var i=
0;i<types.Length;i++){if(!IsValidType(type.TypeArguments[i],scope))return false;}}return true;}object _DualResolve(IList<CodeTypeReference>ctrs){foreach
(var ctr in ctrs){var t=_ResolveTypeImpl(ctr,_ResolveCompileUnits);if(null!=t)return t;}foreach(var ctr in ctrs){var t=_ResolveTypeImpl(ctr,_ResolveAssemblies);
if(null!=t)return t;}return null;}object _ResolveTypeImpl(CodeTypeReference type,int resolutionType=_ResolveAssemblies|_ResolveCompileUnits){object result
=null;if(null!=type.ArrayElementType&&1<=type.ArrayRank){ return typeof(Array);}if(_ResolveCompileUnits==(resolutionType&_ResolveCompileUnits)){foreach
(var ccu in CompileUnits){CodeDomVisitor.Visit(ccu,(ctx)=>{var td=ctx.Target as CodeTypeDeclaration;if(null!=td){var name=_GetGenericName(td);CodeObject
 p=td;while((p=_GetRef(p,_parentKey)as CodeObject)!=null){var ptd=p as CodeTypeDeclaration;if(null!=ptd){name=string.Concat(_GetGenericName(ptd),"+",name);
td=ptd;}var ns=p as CodeNamespace;if(null!=ns&&!string.IsNullOrEmpty(ns.Name)){name=string.Concat(ns.Name,".",name);}}if(name==type.BaseType){td=ctx.Target
 as CodeTypeDeclaration;result=td;ctx.Cancel=true;}}},CodeDomVisitTargets.Types|CodeDomVisitTargets.TypeRefs|CodeDomVisitTargets.Members);if(null!=result)
return result;}}if(_ResolveAssemblies==(resolutionType&_ResolveAssemblies)){Type t;if(_typeCache.TryGetValue(type,out t))return t;foreach(var ccu in CompileUnits)
{var corlib=typeof(string).Assembly;var rt=corlib.GetType(type.BaseType,false,false);result=rt;if(null!=result){_typeCache.Add(type,rt);return result;
}foreach(var astr in ccu.ReferencedAssemblies){var asm=_LoadAsm(astr);rt=asm.GetType(type.BaseType,false,false);result=rt;if(null!=result){_typeCache.Add(type,
rt);return result;}}}if(0==CompileUnits.Count){var corlib=typeof(string).Assembly;var rt=corlib.GetType(type.BaseType,false,false);result=rt;if(null!=
result){_typeCache.Add(type,rt);return result;}}_typeCache.Add(type,null);}return result;}Assembly _LoadAsm(string asm){if(File.Exists(asm)){return Assembly.LoadFile(Path.GetFullPath(asm));
}else if(asm.StartsWith(@"\\")){return Assembly.LoadFile(asm);}AssemblyName an=null;try{an=new AssemblyName(asm);}catch{an=null;}if(null!=an){return Assembly.Load(an);
}return Assembly.Load(asm);}/// <summary>
/// Clears the type cache
/// </summary>
public void ClearCache(){_typeCache.Clear();}/// <summary>
/// Refreshes the code after the graphs have been changed, added to, or removed from.
/// </summary>
/// <param name="customAction">A visit action to execute along with the refresh. Provided for optimization purposes</param>
/// <param name="typesOnly">Only go as far as types and their members</param>
public void Refresh(CodeDomVisitAction customAction=null,bool typesOnly=false){ for(int ic=CompileUnits.Count,i=0;i<ic;++i){var ccu=CompileUnits[i]; CodeDomVisitor.Visit(ccu,
(ctx)=>{var co=ctx.Target as CodeObject;if(null!=co){if(null!=ctx.Parent)co.UserData[_parentKey]=new WeakReference<object>(ctx.Parent);if(null!=ctx.Root)
 co.UserData[_rootKey]=new WeakReference<object>(ctx.Root);}if(null!=customAction){customAction(ctx);}},typesOnly?CodeDomVisitTargets.Types|CodeDomVisitTargets.Members
:CodeDomVisitTargets.All);}}}/// <summary>
/// Provides scope information from a particular point in the CodeDOM
/// </summary>
/// <remarks>The scope goes stale when its parent <see cref="CodeDomResolver"/> goes out of scope.</remarks>
#if GOKITLIB
public
#endif
class CodeDomResolverScope{WeakReference<CodeDomResolver>_resolver;/// <summary>
/// The resolver that spawned this scope.
/// </summary>
public CodeDomResolver Resolver{get{CodeDomResolver target;if(_resolver.TryGetTarget(out target))return target;throw new InvalidOperationException("The scope is stale");
}}/// <summary>
/// The compile unit of this scope
/// </summary>
public CodeCompileUnit CompileUnit{get;set;}/// <summary>
/// The namespace of this scope
/// </summary>
public CodeNamespace Namespace{get;set;}/// <summary>
/// The nested types of this scope, declaring first, followed by outer types of this nested type in reverse nest order
/// </summary>
public List<CodeTypeDeclaration>Types{get;}=new List<CodeTypeDeclaration>();/// <summary>
/// The declaring type of this scope
/// </summary>
public CodeTypeDeclaration DeclaringType{get{if(null!=Types&&0<Types.Count)return Types[0];return null;}}/// <summary>
/// The member associated with this scope
/// </summary>
public CodeTypeMember Member{get;set;}/// <summary>
/// The statement associated with this scope
/// </summary>
public CodeStatement Statement{get;set;}/// <summary>
/// The expression associated with this scope
/// </summary>
public CodeExpression Expression{get;set;}/// <summary>
/// The type reference associated with this scope
/// </summary>
public CodeTypeReference TypeRef{get;set;} IDictionary<string,CodeTypeReference>_variableTypes;/// <summary>
/// Indicates all the variables in this scope and their types
/// Not fully working yet
/// </summary>
public IDictionary<string,CodeTypeReference>VariableTypes{get{if(null==_variableTypes||0==_variableTypes.Count)_variableTypes=Resolver.GetVariableTypes(this);
return _variableTypes;}}HashSet<string>_memberNames;/// <summary>
/// Indicates all the members rooted at this scope
/// </summary>
public HashSet<string>MemberNames{get{if(null==_memberNames||0==_memberNames.Count)_memberNames=Resolver.GetMemberNames(this);return _memberNames;}}IDictionary<string,
CodeTypeReference>_argumentTypes;/// <summary>
/// Indicates all the arguments at this scope and their types
/// </summary>
public IDictionary<string,CodeTypeReference>ArgumentTypes{get{if(null==_argumentTypes||0==_argumentTypes.Count)_argumentTypes=Resolver.GetArgumentTypes(this);
return _argumentTypes;}}HashSet<string>_fieldNames;/// <summary>
/// Indicates all the fields available at this scope
/// </summary>
public HashSet<string>FieldNames{get{if(null==_fieldNames||0==_fieldNames.Count)_fieldNames=Resolver.GetFieldNames(this);return _fieldNames;}}HashSet<string>
_methodNames;/// <summary>
/// Indicates all the method groups at this scope
/// </summary>
public HashSet<string>MethodNames{get{if(null==_methodNames||0==_methodNames.Count)_methodNames=Resolver.GetMethodNames(this);return _methodNames;}}HashSet<string>
_propertyNames;/// <summary>
/// Indicates all the property groups at this scope
/// </summary>
public HashSet<string>PropertyNames{get{if(null==_propertyNames||0==_propertyNames.Count)_propertyNames=Resolver.GetPropertyNames(this);return _propertyNames;
}}HashSet<string>_eventNames;/// <summary>
/// Indicates all the events at this scope
/// </summary>
public HashSet<string>EventNames{get{if(null==_eventNames||0==_eventNames.Count)_eventNames=Resolver.GetEventNames(this);return _eventNames;}}HashSet<string>
_thisTargets;/// <summary>
/// Indicates all the members that are part of this instance
/// </summary>
public HashSet<string>ThisTargets{get{if(null==_thisTargets||0==_thisTargets.Count)_thisTargets=Resolver.GetThisTargets(this);return _thisTargets;}}HashSet<string>
_baseTargets;/// <summary>
/// Indicates all of the members that are part of the base instance
/// </summary>
public HashSet<string>BaseTargets{get{if(null==_baseTargets||0==_baseTargets.Count)_baseTargets=Resolver.GetBaseTargets(this);return _baseTargets;}}IDictionary<string,
CodeTypeReference>_typeTargets;/// <summary>
/// Indicates all the static members from the declaring type available at this scope
/// </summary>
public IDictionary<string,CodeTypeReference>TypeTargets{get{if(null==_typeTargets||0==_typeTargets.Count)_typeTargets=Resolver.GetTypeTargets(this);return
 _typeTargets;}}internal CodeDomResolverScope(CodeDomResolver resolver){_resolver=new WeakReference<CodeDomResolver>(resolver);}/// <summary>
/// Returns a string summarizing the scope
/// </summary>
/// <returns>A string printing a scope summary</returns>
public override string ToString(){var sb=new StringBuilder();if(null!=CompileUnit){sb.Append("Compile Unit: (");sb.Append(CompileUnit.Namespaces.Count);
sb.AppendLine(" namespaces)");}if(null!=Namespace){sb.Append("Namespace ");if(!string.IsNullOrEmpty(Namespace.Name)){sb.Append(Namespace.Name);sb.Append(" ");
}sb.Append("(");sb.Append(Namespace.Types.Count);sb.AppendLine(" types)");}if(null!=DeclaringType){sb.Append("Declaring Type: ");sb.AppendLine(CodeDomUtility.ToString(Resolver.GetType(DeclaringType,
this)));}if(null!=Member){sb.Append("Member: ");sb.Append(CodeDomUtility.ToString(CodeDomResolver.GetTypeForMember(Member)));sb.Append(" ");sb.AppendLine(Member.Name);
}if(null!=Statement){sb.Append("Statement: ");var s=CodeDomUtility.ToString(Statement).Trim();var i=s.IndexOfAny(new char[]{'\r','\n'});if(-1<i){s=s.Substring(0,
i)+"...";}sb.AppendLine(s);}if(null!=Expression){sb.Append("Expression: ");sb.AppendLine(CodeDomUtility.ToString(Expression).Trim());}if(null!=TypeRef)
{sb.Append("Type Ref: ");sb.AppendLine(CodeDomUtility.ToString(TypeRef).Trim());}return sb.ToString();}}}namespace CD{partial class CodeDomResolver{/// <summary>
/// Evaluates the expression at the the given scope
/// </summary>
/// <param name="expression">The expression to evaluate</param>
/// <param name="scope">The scope at which evaluation occurs or null to use the expression's scope</param>
/// <returns>The result of the evaluation</returns>
public object Evaluate(CodeExpression expression,CodeDomResolverScope scope=null)=>_Eval(expression,scope);object _Eval(CodeExpression e,CodeDomResolverScope
 s){var ac=e as CodeArrayCreateExpression;if(null!=ac){if(null==s)s=GetScope(ac);var type=_EvalType(ac.CreateType,s);var len=ac.Initializers.Count;if(0
==len){if(0<ac.Size)len=ac.Size;else len=(int)_Eval(ac.SizeExpression,s);}var arr=Array.CreateInstance(type,len);if(0<ac.Initializers.Count)for(int ic
=ac.Initializers.Count,i=0;i<ic;++i)arr.SetValue(_Eval(ac.Initializers[i],s),i);}var ai=e as CodeArrayIndexerExpression;if(null!=ai){if(null==s)s=GetScope(e);
var arr=(Array)_Eval(ai.TargetObject,s);var ind=new int[ai.Indices.Count];for(var i=0;i<ind.Length;i++)ind[i]=(int)_Eval(ai.Indices[i],s);return arr.GetValue(ind);
}var bo=e as CodeBinaryOperatorExpression;if(null!=bo)return _EvalBinOp(bo,s);var c=e as CodeCastExpression;if(null!=c){if(null==s)s=GetScope(c);var type
=_EvalType(c.TargetType,s);var rhs=_Eval(c.Expression,s);if(null==rhs){if(type.IsValueType)throw new InvalidCastException("Cannot cast null to a value type");
return null;}if(rhs.GetType().IsAssignableFrom(type))return rhs;throw new InvalidCastException("The value is not assignable to that target type");}var
 dv=e as CodeDefaultValueExpression;if(null!=dv){if(null==s)s=GetScope(c);var type=_EvalType(c.TargetType,s);return Activator.CreateInstance(type);}var
 dc=e as CodeDelegateCreateExpression;if(null!=dc){if(null==s)s=GetScope(dc);var type=_EvalType(dc.DelegateType,s);var targ=_Eval(dc.TargetObject,s);var
 m=targ.GetType().GetMethod(dc.MethodName,((BindingFlags)(-1))&~BindingFlags.DeclaredOnly);return Delegate.CreateDelegate(type,targ,m);}var di=e as CodeDelegateInvokeExpression;
if(null!=di){if(null==s)s=GetScope(di);var lhs=_Eval(di.TargetObject,s);var parms=new object[di.Parameters.Count];for(var i=0;i<parms.Length;i++)parms[i]
=_Eval(di.Parameters[i],s);var m=lhs.GetType().GetMethod("Invoke");try{return m.Invoke(lhs,parms);}catch(TargetInvocationException tex){throw tex.InnerException;
}}var de=e as CodeDirectionExpression;if(null!=de){if(null==s)s=GetScope(de);return _Eval(de.Expression,s);}var er=e as CodeEventReferenceExpression;if(null!=er)
{if(null==s)s=GetScope(er);var ev=_Eval(er.TargetObject,s);var ei=ev.GetType().GetEvent(er.EventName); return new KeyValuePair<EventInfo,object>(ei,ev);
}var fr=e as CodeFieldReferenceExpression;if(null!=fr){if(null==s)s=GetScope(fr);var trr=_Eval(fr.TargetObject,s);var type=trr as Type;if(null!=type)return
 type.GetField(fr.FieldName).GetValue(null);return trr.GetType().GetField(fr.FieldName).GetValue(trr);}var ix=e as CodeIndexerExpression;if(null!=ix){
if(null==s)s=GetScope(ix);var ir=_Eval(ix.TargetObject,s);var pia=_GetParamInfos(ix.Indices,s);var tt=ir as Type;var type=null!=tt?tt:ir.GetType();try
{if(null==tt)return type.InvokeMember("Item",BindingFlags.Public|BindingFlags.GetProperty|BindingFlags.InvokeMethod|BindingFlags.Instance,null,ir,_GetParamValues(pia));
return type.InvokeMember("Item",BindingFlags.Public|BindingFlags.GetProperty|BindingFlags.Static|BindingFlags.FlattenHierarchy,null,null,_GetParamValues(pia));
}catch(TargetInvocationException tex){throw tex.InnerException;}}var mi=e as CodeMethodInvokeExpression;if(null!=mi){if(null==s)s=GetScope(mi);var mv=
_Eval(mi.Method.TargetObject,s);var type=mv.GetType();var tt=mv as Type;if(null!=tt)type=tt;var pia=_GetParamInfos(mi.Parameters,s);try{if(null==tt)return
 type.InvokeMember(mi.Method.MethodName,BindingFlags.Public|BindingFlags.InvokeMethod|BindingFlags.Instance,null,mv,_GetParamValues(pia));return type.InvokeMember(mi.Method.MethodName,
BindingFlags.Public|BindingFlags.InvokeMethod|BindingFlags.Static|BindingFlags.FlattenHierarchy,null,null,_GetParamValues(pia));}catch(TargetInvocationException
 tex){throw tex.InnerException;}}var mr=e as CodeMethodReferenceExpression;if(null!=mr){if(null==s)s=GetScope(mr);var mv=_Eval(mr.TargetObject,s);var ml
=new List<MethodInfo>();var ma=mv.GetType().GetMethods();for(var i=0;i<ma.Length;++i){var m=ma[i];if(0==string.Compare(m.Name,mr.MethodName,StringComparison.InvariantCulture))
ml.Add(m);}return new KeyValuePair<MethodInfo[],object>(ml.ToArray(),mv);}var oc=e as CodeObjectCreateExpression;if(null!=oc){if(null==s)s=GetScope(oc);
var t=_EvalType(oc.CreateType,s);var pia=_GetParamInfos(oc.Parameters,s);return Activator.CreateInstance(t,_GetParamValues(pia));}var p=e as CodePrimitiveExpression;
if(null!=p)return p.Value;var pr=e as CodePropertyReferenceExpression;if(null!=pr){if(null==s)s=GetScope(pr);var trr=_Eval(pr.TargetObject,s);var type
=trr as Type;if(null!=type)return type.GetProperty(pr.PropertyName).GetValue(null);return trr.GetType().GetProperty(pr.PropertyName).GetValue(trr);}var
 to=e as CodeTypeOfExpression;if(null!=to){if(null==s)s=GetScope(to);return _EvalType(to.Type,s);}var tr=e as CodeTypeReferenceExpression;if(null!=tr)
return _EvalType(tr.Type,s);throw new NotSupportedException(string.Format("Unable to evaluate expressions of type {0}",e.GetType().FullName));}private
 struct _ParamInfo{public Type Type;public bool IsIn;public bool IsOut;public bool IsRetval;public bool IsOptional;public object Value;}object[]_GetParamValues(_ParamInfo[]
paramInfos){var result=new object[paramInfos.Length];for(var i=0;i<result.Length;++i)result[i]=paramInfos[i].Value;return result;}_ParamInfo[]_GetParamInfos(CodeExpressionCollection
 parms,CodeDomResolverScope s){var result=new _ParamInfo[parms.Count];for(var i=0;i<result.Length;i++){CodeExpression e=parms[i];_ParamInfo p=default(_ParamInfo);
p.IsOptional=false;p.IsRetval=false;var de=e as CodeDirectionExpression;if(null!=de){switch(de.Direction){case FieldDirection.In:break;case FieldDirection.Out:
p.IsOut=true;break;case FieldDirection.Ref:p.IsIn=p.IsOut=true;break;}e=de.Expression;}p.Value=_Eval(e,s);if(null!=p.Value)p.Type=p.Value.GetType();result[i]
=p;}return result;}PropertyInfo _MatchPropBySig(Type type,string name,_ParamInfo[]infos){PropertyInfo result=null;var ma=type.GetProperties(((BindingFlags)(-1))
&~BindingFlags.DeclaredOnly);for(var i=0;i<ma.Length;++i){var m=ma[i];if(0==string.Compare(name,m.Name,StringComparison.InvariantCulture)){var mpa=m.GetIndexParameters();
if(mpa.Length==infos.Length){if(0==mpa.Length){if(null!=result)throw new InvalidOperationException("Multiple matching indexer signatures were found");
result=m;}bool found=false;for(var j=0;j<mpa.Length;++j){found=true;var mp=mpa[j];var mpc=infos[j];var tc=infos[j].Type;if(!((null==tc||mp.ParameterType.IsAssignableFrom(tc))
&&(mp.IsIn==mpc.IsIn&&mp.IsOut==mpc.IsOut&&mp.IsRetval==mpc.IsRetval&&mp.IsOptional==mpc.IsOptional))){found=false;break;}}if(found){if(null!=result)throw
 new InvalidOperationException("Multiple matching indexer signatures were found");result=m;}}}}return result;}MethodInfo _MatchMethBySig(Type type,string
 name,_ParamInfo[]infos){MethodInfo result=null;var ma=type.GetMethods(((BindingFlags)(-1))&~BindingFlags.DeclaredOnly);for(var i=0;i<ma.Length;++i){var
 m=ma[i];if(0==string.Compare(name,m.Name,StringComparison.InvariantCulture)){var mpa=m.GetParameters();if(mpa.Length==infos.Length){if(0==mpa.Length)
{if(null!=result)throw new InvalidOperationException("Multiple matching method signatures were found");result=m;}bool found=false;for(var j=0;j<mpa.Length;
++j){found=true;var mp=mpa[j];var mpc=infos[j];var tc=infos[j].Type;if(!((null==tc||mp.ParameterType.IsAssignableFrom(tc))&&(mp.IsIn==mpc.IsIn&&mp.IsOut
==mpc.IsOut&&mp.IsRetval==mpc.IsRetval&&mp.IsOptional==mpc.IsOptional))){found=false;break;}}if(found){if(null!=result)throw new InvalidOperationException("Multiple matching method signatures were found");
result=m;}}}}return result;}ConstructorInfo _MatchCtorBySig(Type type,_ParamInfo[]infos){ConstructorInfo result=null;var ma=type.GetConstructors(((BindingFlags)(-1))
&~BindingFlags.DeclaredOnly);for(var i=0;i<ma.Length;++i){var m=ma[i];var mpa=m.GetParameters();if(mpa.Length==infos.Length){if(0==mpa.Length){if(null
!=result)throw new InvalidOperationException("Multiple matching constructor signatures were found");result=m;}bool found=false;for(var j=0;j<mpa.Length;
++j){found=true;var mp=mpa[j];var mpc=infos[j];var tc=infos[j].Type;if(!((null==tc||mp.ParameterType.IsAssignableFrom(tc))&&(mp.IsIn==mpc.IsIn&&mp.IsOut
==mpc.IsOut&&mp.IsRetval==mpc.IsRetval&&mp.IsOptional==mpc.IsOptional))){found=false;break;}}if(found){if(null!=result)throw new InvalidOperationException("Multiple matching constructor signatures were found");
result=m;}}}return result;}Type _EvalType(CodeTypeReference r,CodeDomResolverScope s){if(null==s)s=GetScope(r);var t=_ResolveType(r,s);if(null==t)throw
 new TypeLoadException("The type could not be resolved");var result=t as Type;if(null==result)throw new NotSupportedException("Only runtime types may be evaluated");
return result;}object _EvalBinOp(CodeBinaryOperatorExpression bo,CodeDomResolverScope s){if(null==s)s=GetScope(bo);switch(bo.Operator){case CodeBinaryOperatorType.Add:
return _Add(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.Subtract:return _Subtract(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.Multiply:
return _Multiply(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.Divide:return _Divide(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.Modulus:
return _Modulo(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.Assign:throw new NotSupportedException("Evaluate cannot change state.");
case CodeBinaryOperatorType.BitwiseAnd:return _BitwiseAnd(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.BitwiseOr:return _BitwiseOr(_Eval(bo.Left,
s),_Eval(bo.Right,s));case CodeBinaryOperatorType.BooleanAnd:return((bool)_Eval(bo.Left,s))&&((bool)_Eval(bo.Right,s));case CodeBinaryOperatorType.BooleanOr:
return((bool)_Eval(bo.Left,s))||((bool)_Eval(bo.Right,s));case CodeBinaryOperatorType.LessThan:return _LessThan(_Eval(bo.Left,s),_Eval(bo.Right,s));case
 CodeBinaryOperatorType.LessThanOrEqual:return _LessThanOrEqual(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.GreaterThan:return _GreaterThan(_Eval(bo.Left,
s),_Eval(bo.Right,s));case CodeBinaryOperatorType.GreaterThanOrEqual:return _GreaterThanOrEqual(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.IdentityEquality:
case CodeBinaryOperatorType.ValueEquality:return _Equals(_Eval(bo.Left,s),_Eval(bo.Right,s));case CodeBinaryOperatorType.IdentityInequality:return _NotEqual(_Eval(bo.Left,
s),_Eval(bo.Right,s));default:throw new NotSupportedException("The specified operation is not supported.");}}object _Add(object lhs,object rhs){_Promote(ref
 lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)+((decimal)rhs);if(typeof(double)
==lt&&typeof(double)==rt)return((double)lhs)+((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)+((float)rhs);if(typeof(ulong)==lt
&&typeof(ulong)==rt)return((ulong)lhs)+((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)+((long)rhs);if(typeof(uint)==lt&&typeof(uint)
==rt)return((uint)lhs)+((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)+((int)rhs);try{return lt.GetMethod("op_Addition").Invoke(null,new
 object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _Subtract(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)-
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)-((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)-
((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)-((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)-((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)-((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)-((int)rhs);try{return lt.GetMethod("op_Subtraction").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _Multiply(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)*
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)*((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)*
((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)*((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)*((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)*((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)*((int)rhs);try{return lt.GetMethod("op_Multiply").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _Divide(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)/
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)/((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)/
((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)/((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)/((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)/((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)/((int)rhs);try{return lt.GetMethod("op_Division").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _Modulo(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)%
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)%((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)%
((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)%((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)%((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)%((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)%((int)rhs);try{return lt.GetMethod("op_Modulus").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _BitwiseAnd(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)&((ulong)rhs);
if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)&((long)rhs);if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)&((uint)rhs);if(typeof(int)
==lt&&typeof(int)==rt)return((int)lhs)&((int)rhs);try{return lt.GetMethod("op_BitwiseAnd").Invoke(null,new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");
}}object _BitwiseOr(object lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(ulong)==lt&&typeof(ulong)==rt)
return((ulong)lhs)|((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)|((long)rhs);if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)
|((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)|((int)rhs);try{return lt.GetMethod("op_BitwiseOr").Invoke(null,new object[]{lhs,rhs});
}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _Equals(object lhs,object rhs){_Promote(ref
 lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)==((decimal)rhs);if(typeof(double)
==lt&&typeof(double)==rt)return((double)lhs)==((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)==((float)rhs);if(typeof(ulong)==
lt&&typeof(ulong)==rt)return((ulong)lhs)==((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)==((long)rhs);if(typeof(uint)==lt&&typeof(uint)
==rt)return((uint)lhs)==((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)==((int)rhs);try{return lt.GetMethod("op_Equality").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _NotEqual(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)!=
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)!=((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)
!=((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)!=((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)!=((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)!=((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)!=((int)rhs);try{return lt.GetMethod("op_Inequality").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _LessThan(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)<
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)<((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)<
((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)<((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)<((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)<((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)<((int)rhs);try{return lt.GetMethod("op_LessThan").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _GreaterThan(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)>
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)>((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)>
((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)>((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)>((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)>((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)>((int)rhs);try{return lt.GetMethod("op_GreaterThan").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _LessThanOrEqual(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)<=
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)<=((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)
<=((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)<=((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)<=((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)<=((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)<=((int)rhs);try{return lt.GetMethod("op_LessThanOrEqual").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}object _GreaterThanOrEqual(object
 lhs,object rhs){_Promote(ref lhs,ref rhs);var lt=lhs.GetType();var rt=rhs.GetType();if(typeof(decimal)==lt&&typeof(decimal)==rt)return((decimal)lhs)>=
((decimal)rhs);if(typeof(double)==lt&&typeof(double)==rt)return((double)lhs)>=((double)rhs);if(typeof(float)==lt&&typeof(float)==rt)return((float)lhs)
>=((float)rhs);if(typeof(ulong)==lt&&typeof(ulong)==rt)return((ulong)lhs)>=((ulong)rhs);if(typeof(long)==lt&&typeof(long)==rt)return((long)lhs)>=((long)rhs);
if(typeof(uint)==lt&&typeof(uint)==rt)return((uint)lhs)>=((uint)rhs);if(typeof(int)==lt&&typeof(int)==rt)return((int)lhs)>=((int)rhs);try{return lt.GetMethod("op_GreaterThanOrEqual").Invoke(null,
new object[]{lhs,rhs});}catch{throw new InvalidOperationException("The operation cannot be performed on objects on these types");}}void _Promote(ref object
 lhs,ref object rhs){if(null==lhs||null==rhs)return;var lt=lhs.GetType();var rt=rhs.GetType();if(!_IsNumeric(lt)||!_IsNumeric(rt))return;if(lt==rt)return;
 if(typeof(decimal)==lt){if(typeof(double)==rt||typeof(float)==rt)throw new InvalidOperationException("Cannot operate on types of decimal and float.");
rhs=(decimal)rhs;return;}if(typeof(decimal)==rt){if(typeof(double)==lt||typeof(float)==lt)throw new InvalidOperationException("Cannot operate on types of decimal and float.");
lhs=(decimal)lhs;return;} if(typeof(double)==lt){rhs=(double)rhs;return;}if(typeof(double)==rt){lhs=(double)lhs;return;} if(typeof(float)==lt){rhs=(float)rhs;
return;}if(typeof(float)==rt){lhs=(float)lhs;return;} if(typeof(ulong)==lt){if(typeof(sbyte)==rt||typeof(short)==rt||typeof(int)==rt||typeof(long)==rt)
throw new InvalidOperationException("Cannot operate on ulong and a signed value");rhs=(ulong)rhs;return;}if(typeof(ulong)==rt){if(typeof(sbyte)==lt||typeof(short)
==lt||typeof(int)==lt||typeof(long)==lt)throw new InvalidOperationException("Cannot operate on ulong and a signed value");lhs=(ulong)lhs;return;} if(typeof(long)
==lt){rhs=(long)rhs;return;}if(typeof(long)==rt){lhs=(long)lhs;return;} if(typeof(uint)==lt){if(typeof(sbyte)==rt||typeof(short)==rt||typeof(int)==rt)
{lhs=(long)lhs;rhs=(long)rhs;}else{rhs=(uint)rhs;}return;}if(typeof(uint)==rt){if(typeof(sbyte)==lt||typeof(short)==lt||typeof(int)==lt){lhs=(long)lhs;
rhs=(long)rhs;}else{lhs=(uint)lhs;}return;} lhs=(int)lhs;rhs=(int)rhs;}bool _IsNumeric(Type nm){if(null==nm)return false;return typeof(sbyte)==nm||typeof(byte)
==nm||typeof(short)==nm||typeof(ushort)==nm||typeof(int)==nm||typeof(uint)==nm||typeof(long)==nm||typeof(ulong)==nm||typeof(float)==nm||typeof(double)
==nm||typeof(decimal)==nm;}}}namespace CD{using CTE=CodeTypeReferenceEqualityComparer;partial class CodeDomResolver{/// <summary>
/// Gets the type of the specified expression, at the optional given scope
/// </summary>
/// <param name="expr">The expression to evaluate</param>
/// <param name="scope">The scope at which evaluation occurs, or null to use the expression's scope</param>
/// <returns>A <see cref="CodeTypeReference"/> representing the type of the expression</returns>
public CodeTypeReference GetTypeOfExpression(CodeExpression expr,CodeDomResolverScope scope=null){if(null==expr)throw new ArgumentNullException(nameof(expr));
 var cpe=expr as CodePrimitiveExpression;if(null!=cpe){if(null==cpe.Value)return new CodeTypeReference(typeof(void));return new CodeTypeReference(cpe.Value.GetType());
}var cbe=expr as CodeBinaryOperatorExpression;if(null!=cbe){switch(cbe.Operator){case CodeBinaryOperatorType.BooleanAnd:case CodeBinaryOperatorType.BooleanOr:
case CodeBinaryOperatorType.GreaterThan:case CodeBinaryOperatorType.GreaterThanOrEqual:case CodeBinaryOperatorType.IdentityEquality:case CodeBinaryOperatorType.IdentityInequality:
case CodeBinaryOperatorType.LessThan:case CodeBinaryOperatorType.LessThanOrEqual:case CodeBinaryOperatorType.ValueEquality:return new CodeTypeReference(typeof(bool));
case CodeBinaryOperatorType.Assign:case CodeBinaryOperatorType.Add:case CodeBinaryOperatorType.Subtract:case CodeBinaryOperatorType.Multiply:case CodeBinaryOperatorType.Divide:
case CodeBinaryOperatorType.Modulus:case CodeBinaryOperatorType.BitwiseAnd:case CodeBinaryOperatorType.BitwiseOr:return _PromoteType(GetTypeOfExpression(cbe.Left),
GetTypeOfExpression(cbe.Right));}}var tr=expr as CodeTypeReferenceExpression;if(null!=tr){if(null==tr.Type)throw new InvalidOperationException("The type reference expression had no target object");
return tr.Type;}var pd=expr as CodeParameterDeclarationExpression;if(null!=pd){if(null==pd.Type)throw new InvalidOperationException("The parameter declaration had no target object");
return pd.Type;}var oc=expr as CodeObjectCreateExpression;if(null!=oc){if(null==oc.CreateType)throw new InvalidOperationException("The object creation expression had no create type");
return oc.CreateType;}var ac=expr as CodeArrayCreateExpression;if(null!=ac){if(null==ac.CreateType)throw new InvalidOperationException("The array creation expression had no create type");
var ctr=new CodeTypeReference();ctr.ArrayElementType=ac.CreateType.ArrayElementType;ctr.ArrayRank=ac.CreateType.ArrayRank;ctr.BaseType=ac.CreateType.BaseType;
ctr.TypeArguments.AddRange(ac.CreateType.TypeArguments);return ctr;}var dc=expr as CodeDelegateCreateExpression;if(null!=dc){if(null==dc.DelegateType)
throw new InvalidOperationException("The delegate creation expression had no delegate type");return dc.DelegateType;}var dv=expr as CodeDefaultValueExpression;
if(null!=dv){if(null==dv.Type)throw new InvalidOperationException("The default value expression had no type");return dv.Type;}var dire=expr as CodeDirectionExpression;
if(null!=dire){if(null==dire.Expression)throw new InvalidOperationException("The direction expression had no target expression");return GetTypeOfExpression(dire.Expression,
scope);}var ai=expr as CodeArrayIndexerExpression;if(null!=ai){var aet=GetTypeOfExpression(ai.TargetObject).ArrayElementType;if(null==aet)throw new InvalidOperationException("The associated array type's array element type was null");
return aet;}var cst=expr as CodeCastExpression;if(null!=cst){if(null==cst.TargetType)throw new InvalidOperationException("The cast expression's target type was null");
return cst.TargetType;}var to=expr as CodeTypeOfExpression;if(null!=to)return new CodeTypeReference(typeof(Type)); if(null==scope)scope=GetScope(expr);
var cmi=expr as CodeMethodInvokeExpression;if(null!=cmi){var types=new CodeTypeReference[cmi.Parameters.Count];for(var i=0;i<types.Length;++i){var p=cmi.Parameters[i];
var de=p as CodeDirectionExpression;if(null!=de)p=de.Expression;types[i]=GetTypeOfExpression(p,scope);if(null==types[i])throw new InvalidOperationException(string.Format("Could not resolve parameter index {0} of method invoke expression",
i));}var mr=cmi.Method;var t=GetTypeOfExpression(mr.TargetObject,scope);var rt=TryResolveType(t,scope);if(null==rt)throw new InvalidOperationException("Could not resolve the type of the target expression of the method invoke expression");
var rtt=rt as Type;object tm=null;var binder=new CodeDomBinder(scope);var grp=binder.GetMethodGroup(rt,mr.MethodName,BindingFlags.Instance|BindingFlags.Static
|BindingFlags.Public|BindingFlags.NonPublic); tm=binder.SelectMethod(BindingFlags.InvokeMethod|BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public
|BindingFlags.NonPublic,grp,types,null);if(null==tm)throw new InvalidOperationException("Unable to find a suitable method to bind to in method invoke expression");
var mi=tm as MethodInfo;if(null!=mi)return new CodeTypeReference(mi.ReturnType);var cm=tm as CodeMemberMethod;if(null==cm.ReturnType)return new CodeTypeReference(typeof(void));
return cm.ReturnType;}var ar=expr as CodeArgumentReferenceExpression;if(null!=ar){var t=scope.ArgumentTypes[ar.ParameterName];if(null==t)throw new InvalidOperationException("The argument's type was null");
return t;}var vr=expr as CodeVariableReferenceExpression;if(null!=vr){var t=scope.VariableTypes[vr.VariableName];if(null==t)throw new InvalidOperationException("The variable's type was null. This could be due to an unresolved var declaration in Slang");
return t;}var br=expr as CodeBaseReferenceExpression;if(null!=br){var dt=scope.DeclaringType;if(null!=dt){if(0<dt.BaseTypes.Count){ var bt=dt.BaseTypes[0];
if(null==bt)throw new InvalidOperationException("The declaring type's base types contained a null entry.");return bt;}else if(dt.IsClass||dt.IsInterface)
return new CodeTypeReference(typeof(object));else if(dt.IsEnum)return new CodeTypeReference(typeof(Enum));else if(dt.IsStruct)return new CodeTypeReference(typeof(ValueType));
else throw new InvalidOperationException("The declaring type is not a class, interface, enum or struct");}throw new InvalidOperationException("There is no declarting type in the scope from which to retrieve a base reference");
}var th=expr as CodeThisReferenceExpression;if(null!=th){var dt=scope.DeclaringType;if(null!=dt){ return new CodeTypeReference(_GetBaseNameOfType(dt,scope));
}throw new InvalidOperationException("There was no declaring type in the scope from which to retrieve a this reference");}var fr=expr as CodeFieldReferenceExpression;
if(null!=fr){var t=GetTypeOfExpression(fr.TargetObject,scope);var tt=_ResolveType(t,scope);if(null==tt)throw new InvalidOperationException("The field reference's target expression type could not be resolved");
var binder=new CodeDomBinder(scope);var fl=BindingFlags.Public|BindingFlags.NonPublic;var isStatic=(fr.TargetObject as CodeTypeReferenceExpression)!=null;
if(isStatic)fl|=BindingFlags.Static;else fl|=BindingFlags.Instance;var res=binder.GetField(tt,fr.FieldName,fl);if(null!=res){var mi=res as MemberInfo;
if(null!=mi)return GetTypeForMember(mi);return GetTypeForMember(res as CodeTypeMember);}throw new InvalidOperationException("A matching field could not be found");
}var pr=expr as CodePropertyReferenceExpression;if(null!=pr){var t=GetTypeOfExpression(pr.TargetObject,scope);var tt=_ResolveType(t,scope);if(null==tt)
throw new InvalidOperationException("The property reference's target expression type could not be resolved");var binder=new CodeDomBinder(scope);var fl
=BindingFlags.Public|BindingFlags.NonPublic;var isStatic=(pr.TargetObject as CodeTypeReferenceExpression)!=null;if(isStatic)fl|=BindingFlags.Static;else
 fl|=BindingFlags.Instance;var res=binder.GetPropertyGroup(tt,pr.PropertyName,fl);if(0<res.Length){var mi=res[0]as MemberInfo;if(null!=mi)return GetTypeForMember(mi);
return GetTypeForMember(res[0]as CodeTypeMember);}throw new InvalidOperationException("A matching property could not be found");}var er=expr as CodeEventReferenceExpression;
if(null!=er){var t=GetTypeOfExpression(er.TargetObject,scope);var tt=_ResolveType(t,scope);if(null==tt)throw new InvalidOperationException("The event reference's target expression type could not be resolved");
var binder=new CodeDomBinder(scope);var fl=BindingFlags.Public|BindingFlags.NonPublic;var isStatic=(er.TargetObject as CodeTypeReferenceExpression)!=null;
if(isStatic)fl|=BindingFlags.Static;else fl|=BindingFlags.Instance;var res=binder.GetEvent(tt,er.EventName,fl);if(null!=res){var mi=res as MemberInfo;
if(null!=mi)return GetTypeForMember(mi);else return GetTypeForMember(res as CodeTypeMember);}throw new InvalidOperationException("A matching event could not be found");
}var di=expr as CodeDelegateInvokeExpression;if(null!=di){var ctr=GetTypeOfExpression(di.TargetObject,scope);var tt=_ResolveType(ctr,scope)as Type;if(null
==tt)throw new InvalidOperationException("The delegate invoke expression's target expression type could not resolved.");var ma=tt.GetMember("Invoke");
if(0<ma.Length){var mi=ma[0]as MethodInfo;if(null!=mi)return new CodeTypeReference(mi.ReturnType);}throw new InvalidOperationException("The target is not a delegate");
}var ie=expr as CodeIndexerExpression;if(null!=ie){var t=GetTypeOfExpression(ie.TargetObject,scope); if(0==t.ArrayRank&&0==string.Compare("System.String",
t.BaseType))return new CodeTypeReference(typeof(char));var types=new CodeTypeReference[ie.Indices.Count];for(var i=0;i<types.Length;++i){var p=ie.Indices[i];
var de=p as CodeDirectionExpression;if(null!=de)p=de.Expression;types[i]=GetTypeOfExpression(p,scope);if(IsNullOrVoidType(types[i]))throw new InvalidOperationException("One or more of the indexer argument types was void");
}var tt=TryResolveType(t,scope);if(null==tt)throw new InvalidOperationException("The indexer expression's target expression type could not be resolved");
var binder=new CodeDomBinder(scope);var td=tt as CodeTypeDeclaration;object tm=null;if(null!=td){var grp=binder.GetPropertyGroup(td,"Item",BindingFlags.Public
|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance);tm=binder.SelectProperty(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static
|BindingFlags.Instance,grp,null,types,null);}else{var rt=tt as Type;if(null!=rt){var grp=binder.GetPropertyGroup(rt,"Item",BindingFlags.Public|BindingFlags.NonPublic
|BindingFlags.Static|BindingFlags.Instance);tm=binder.SelectProperty(BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.Static|BindingFlags.Instance,
grp,null,types,null);}}if(null==tm)throw new InvalidOperationException("The indexer expression's target object type does not have a matching indexer property");
var pi=tm as PropertyInfo;if(null!=pi)return new CodeTypeReference(pi.PropertyType);var cm=tm as CodeMemberProperty;if(null==cm.Type)throw new InvalidOperationException("The property declaration's property type was null");
return cm.Type;}throw new InvalidOperationException(string.Format("Unsupported expression type {0}",expr.GetType().Name));}/// <summary>
/// Attempts to return the type of the specified expression using the specified scope
/// </summary>
/// <param name="expr">The expression to evaluate</param>
/// <param name="scope">The scope to use, or null to use the expression's current scope</param>
/// <returns>A <see cref="CodeTypeReference"/> representing the type of the expression or null if it could not be retrieved</returns>
public CodeTypeReference TryGetTypeOfExpression(CodeExpression expr,CodeDomResolverScope scope=null){ try{return GetTypeOfExpression(expr,scope);}catch(Exception)
{return null;}}static readonly CodeTypeReference _FloatType=new CodeTypeReference(typeof(float));static readonly CodeTypeReference _DoubleType=new CodeTypeReference(typeof(double));
static readonly CodeTypeReference _DecimalType=new CodeTypeReference(typeof(decimal));static readonly CodeTypeReference _Byte=new CodeTypeReference(typeof(byte));
static readonly CodeTypeReference _SByte=new CodeTypeReference(typeof(sbyte));static readonly CodeTypeReference _Char=new CodeTypeReference(typeof(char));
static readonly CodeTypeReference _Short=new CodeTypeReference(typeof(short));static readonly CodeTypeReference _UShort=new CodeTypeReference(typeof(ushort));
static readonly CodeTypeReference _Int=new CodeTypeReference(typeof(int));static readonly CodeTypeReference _UInt=new CodeTypeReference(typeof(uint));
static readonly CodeTypeReference _Long=new CodeTypeReference(typeof(long));static readonly CodeTypeReference _Ulong=new CodeTypeReference(typeof(ulong));
static CodeParameterDeclarationExpressionCollection _GetParametersFromMember(CodeTypeMember member){var m=member as CodeMemberMethod;if(null!=m)return
 m.Parameters;var p=member as CodeMemberProperty;if(null!=p)return p.Parameters;return null;}static CodeTypeReference _PromoteType(CodeTypeReference x,
CodeTypeReference y){if((_IsNumericType(x)||CTE.Equals(_Char,x))&&(_IsNumericType(y)||CTE.Equals(_Char,y))){ if(CTE.Equals(x,_DecimalType)){if(CTE.Equals(_FloatType,
y))throw new InvalidOperationException("Cannot convert float to decimal");if(CTE.Equals(_DoubleType,y))throw new InvalidOperationException("Cannot convert double to decimal");
return new CodeTypeReference(typeof(decimal));}else if(CTE.Equals(y,_DecimalType)){if(CTE.Equals(_FloatType,x))throw new InvalidOperationException("Cannot convert float to decimal");
if(CTE.Equals(_DoubleType,x))throw new InvalidOperationException("Cannot convert double to decimal");return new CodeTypeReference(typeof(decimal));} if
(CTE.Equals(x,_DoubleType)||CTE.Equals(y,_DoubleType))return new CodeTypeReference(typeof(double)); if(CTE.Equals(x,_FloatType)||CTE.Equals(y,_FloatType))
return new CodeTypeReference(typeof(double)); if(CTE.Equals(x,_Ulong)){if(CTE.Equals(_SByte,y)||CTE.Equals(_Short,y)||CTE.Equals(_Int,y)||CTE.Equals(_Long,
y))throw new InvalidOperationException("Cannot convert signed type to ulong");return new CodeTypeReference(typeof(ulong));}else if(CTE.Equals(y,_Ulong))
{if(CTE.Equals(_SByte,x)||CTE.Equals(_Short,x)||CTE.Equals(_Int,x)||CTE.Equals(_Long,x))throw new InvalidOperationException("Cannot convert signed type to ulong");
return new CodeTypeReference(typeof(ulong));} if(CTE.Equals(x,_Long)||CTE.Equals(y,_Long))return new CodeTypeReference(typeof(long)); if(CTE.Equals(x,
_UInt)){if(CTE.Equals(_SByte,y)||CTE.Equals(_Short,y)||CTE.Equals(_Int,y))return new CodeTypeReference(typeof(long));}else if(CTE.Equals(y,_UInt)){if(CTE.Equals(_SByte,
x)||CTE.Equals(_Short,x)||CTE.Equals(_Int,x))return new CodeTypeReference(typeof(long));} if(CTE.Equals(x,_UInt)||CTE.Equals(y,_UInt))return new CodeTypeReference(typeof(uint));
 return new CodeTypeReference(typeof(int));}if(CTE.Equals(x,y))return x;throw new InvalidCastException("Cannot promote these types");}static bool _IsNumericType(CodeTypeReference
 t){return CTE.Equals(_Byte,t)||CTE.Equals(_SByte,t)||CTE.Equals(_Short,t)||CTE.Equals(_UShort,t)||CTE.Equals(_Int,t)||CTE.Equals(_UInt,t)||CTE.Equals(_Long,
t)||CTE.Equals(_Ulong,t)||CTE.Equals(_FloatType,t)||CTE.Equals(_DoubleType,t)||CTE.Equals(_DecimalType,t);}}}namespace CD{/// <summary>
/// This class supports the framework. Provides serialization of code dom constructs to code dom constructs.
/// </summary>
public class CodeDomTypeConverter:TypeConverter{/// <summary>
/// This class supports the framework. This method is called by the serialization process to check to see if it's serializable.
/// </summary>
/// <param name="context">Not used</param>
/// <param name="destinationType"><see cref="InstanceDescriptor"/></param>
/// <returns>True if <paramref name="destinationType"/> is instance descriptor, otherwise false</returns>
public override bool CanConvertTo(ITypeDescriptorContext context,Type destinationType){return destinationType==typeof(InstanceDescriptor)||base.CanConvertTo(context,
destinationType);}/// <summary>
/// This class supports the framework. This method is called by the serialization process to get a <see cref="InstanceDescriptor"/> back that shows how to serialize the object
/// </summary>
/// <param name="context">Not used</param>
/// <param name="culture">Not used</param>
/// <param name="value">The code object</param>
/// <param name="destinationType">Should be <see cref="InstanceDescriptor"/></param>
/// <returns>A <see cref="InstanceDescriptor"/> that can be used to serialize the object or null</returns>
public override object ConvertTo(ITypeDescriptorContext context,CultureInfo culture,object value,Type destinationType){object result=null;if(null!=value)
{if(destinationType==typeof(InstanceDescriptor)){var kvp=_GetInstanceData(value);result=new InstanceDescriptor(kvp.Key,kvp.Value);}}return result??base.ConvertTo(context,
culture,value,destinationType);}static KeyValuePair<MemberInfo,object[]>_GetInstanceData(object value){var cu=value as CodeCompileUnit;if(null!=cu){return
 new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod("CompileUnit"),new object[]{_ToArray(cu.ReferencedAssemblies),_ToArray(cu.Namespaces),
_ToArray(cu.AssemblyCustomAttributes),_ToArray(cu.StartDirectives),_ToArray(cu.EndDirectives)});}var ns=value as CodeNamespace;if(null!=ns){return new
 KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod("Namespace"),new object[]{ns.Name,_ToArray(ns.Imports),_ToArray(ns.Types),_ToArray(ns.Comments)
});}var nsi=value as CodeNamespaceImport;if(null!=nsi){if(null==nsi.LinePragma){return new KeyValuePair<MemberInfo,object[]>(nsi.GetType().GetConstructor(new
 Type[]{typeof(string)}),new object[]{nsi.Namespace});}return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod("NamespaceImport"),
new object[]{nsi.Namespace,nsi.LinePragma});}var e=value as CodeExpression;if(null!=e)return _GetInstanceData(e);var s=value as CodeStatement;if(null!=
s)return _GetInstanceData(s);var tr=value as CodeTypeReference;if(null!=tr)return _GetInstanceData(tr);var td=value as CodeTypeDeclaration;if(null!=td)
{return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod("TypeDeclaration"),new object[]{td.Name,td.IsClass,td.IsEnum,td.IsInterface,
td.IsStruct,td.IsPartial,td.Attributes,td.TypeAttributes,_ToArray(td.TypeParameters),_ToArray(td.BaseTypes),_ToArray(td.Members),_ToArray(td.Comments),
_ToArray(td.CustomAttributes),_ToArray(td.StartDirectives),_ToArray(td.EndDirectives),td.LinePragma});}var tm=value as CodeTypeMember;if(null!=tm)return
 _GetInstanceData(tm);var tp=value as CodeTypeParameter;if(null!=tp){ if(!tp.HasConstructorConstraint&&0==tp.Constraints.Count&&0==tp.CustomAttributes.Count)
{return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string)}),new object[]{tp.Name});}return new KeyValuePair<MemberInfo,
object[]>(typeof(CodeDomBuilder).GetMethod("TypeParameter"),new object[]{tp.Name,tp.HasConstructorConstraint,_ToArray(tp.Constraints),_ToArray(tp.CustomAttributes)
});}var cad=value as CodeAttributeDeclaration;if(null!=cad){if(null!=cad.AttributeType){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeTypeReference),typeof(CodeAttributeArgument[])}),new object[]{cad.AttributeType,_ToArray(cad.Arguments)});}else{return new KeyValuePair<MemberInfo,
object[]>(value.GetType().GetConstructor(new Type[]{typeof(string),typeof(CodeAttributeArgument[])}),new object[]{cad.Name,_ToArray(cad.Arguments)});}
}var caa=value as CodeAttributeArgument;if(null!=caa){if(string.IsNullOrEmpty(caa.Name)){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeExpression)}),new object[]{caa.Value});}else{return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]
{typeof(string),typeof(CodeExpression)}),new object[]{caa.Name,caa.Value});}}var cc=value as CodeCatchClause;if(null!=cc){return new KeyValuePair<MemberInfo,
object[]>(value.GetType().GetConstructor(new Type[]{typeof(string),typeof(CodeTypeReference),typeof(CodeStatement[])}),new object[]{cc.LocalName,cc.CatchExceptionType,
_ToArray(cc.Statements)});}var rd=value as CodeRegionDirective;if(null!=rd){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeRegionMode),typeof(string)}),new object[]{rd.RegionMode,rd.RegionText});}var cp=value as CodeChecksumPragma;if(null!=cp){return new
 KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string),typeof(Guid),typeof(byte[])}),new object[]{cp.FileName,cp.ChecksumAlgorithmId,cp.ChecksumData});
}var lp=value as CodeLinePragma;if(null!=lp){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string),typeof(int)}),
new object[]{lp.FileName,lp.LineNumber});}var cm=value as CodeComment;if(null!=cm){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(string),typeof(bool)}),new object[]{cm.Text,cm.DocComment});}Guid g;if(value is Guid){g=(Guid)value;return new KeyValuePair<MemberInfo,
object[]>(value.GetType().GetConstructor(new Type[]{typeof(string)}),new object[]{g.ToString()});}throw new NotSupportedException("Unsupported type of code object. Could not retrieve the instance data.");
}static KeyValuePair<MemberInfo,object[]>_GetInstanceData(CodeTypeMember member){var t=member.GetType();var tc=member as CodeTypeConstructor;if(null!=tc)
{return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),new object[]{tc.Attributes,_ToArray(tc.Parameters),
_ToArray(tc.Statements),_ToArray(tc.Comments),_ToArray(tc.CustomAttributes),_ToArray(tc.StartDirectives),_ToArray(tc.EndDirectives),tc.LinePragma});}var
 c=member as CodeConstructor;if(null!=c){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),new object[]
{c.Attributes,_ToArray(c.Parameters),_ToArray(c.ChainedConstructorArgs),_ToArray(c.BaseConstructorArgs),_ToArray(c.Statements),_ToArray(c.Comments),_ToArray(c.CustomAttributes),_ToArray(c.StartDirectives),
_ToArray(c.EndDirectives),c.LinePragma});}var em=member as CodeEntryPointMethod;if(null!=em){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),
new object[]{em.ReturnType,em.Name,em.Attributes,_ToArray(em.Parameters),_ToArray(em.Statements),_ToArray(em.ImplementationTypes),em.PrivateImplementationType,_ToArray(em.Comments),
_ToArray(em.CustomAttributes),_ToArray(em.ReturnTypeCustomAttributes),_ToArray(em.StartDirectives),_ToArray(em.EndDirectives),em.LinePragma});}var m=member
 as CodeMemberMethod;if(null!=m){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),new object[]{m.ReturnType,m.Name,m.Attributes,_ToArray(m.Parameters),
_ToArray(m.Statements),_ToArray(m.ImplementationTypes),m.PrivateImplementationType,_ToArray(m.Comments),_ToArray(m.CustomAttributes),_ToArray(m.ReturnTypeCustomAttributes),
_ToArray(m.StartDirectives),_ToArray(m.EndDirectives),m.LinePragma});}var p=member as CodeMemberProperty;if(null!=p){return new KeyValuePair<MemberInfo,
object[]>(typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),new object[]{p.Type,p.Name,p.Attributes,_ToArray(p.Parameters),_ToArray(p.GetStatements),_ToArray(p.SetStatements),
_ToArray(p.ImplementationTypes),p.PrivateImplementationType,_ToArray(p.Comments),_ToArray(p.CustomAttributes),_ToArray(p.StartDirectives),_ToArray(p.EndDirectives),p.LinePragma
});}var f=member as CodeMemberField;if(null!=f){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),new
 object[]{f.Type,f.Name,f.InitExpression,f.Attributes,_ToArray(f.Comments),_ToArray(f.CustomAttributes),_ToArray(f.StartDirectives),_ToArray(f.EndDirectives),f.LinePragma
});}var e=member as CodeMemberEvent;if(null!=e){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(t.Name.Substring(4)),new
 object[]{e.Type,e.Name,e.Attributes,_ToArray(e.ImplementationTypes),e.PrivateImplementationType,_ToArray(e.Comments),_ToArray(e.CustomAttributes),_ToArray(e.StartDirectives),_ToArray(e.EndDirectives),e.LinePragma
});}throw new NotSupportedException("Unsupported member type. Can't get instance data");}static KeyValuePair<MemberInfo,object[]>_GetInstanceData(CodeTypeReference
 tr){if(0<tr.ArrayRank&&null!=tr.ArrayElementType&&0==(int)tr.Options&&0==tr.TypeArguments.Count){return new KeyValuePair<MemberInfo,object[]>(tr.GetType().GetConstructor(new
 Type[]{typeof(CodeTypeReference),typeof(int)}),new object[]{tr.ArrayElementType,tr.ArrayRank});}if(0!=(int)tr.Options){if(0==tr.TypeArguments.Count)return
 new KeyValuePair<MemberInfo,object[]>(tr.GetType().GetConstructor(new Type[]{typeof(string),typeof(CodeTypeReferenceOptions)}),new object[]{tr.BaseType,
tr.Options});}else{if(0==tr.TypeArguments.Count){var t=Type.GetType(tr.BaseType,false,false);if(null==t)return new KeyValuePair<MemberInfo,object[]>(tr.GetType().GetConstructor(new
 Type[]{typeof(string)}),new object[]{tr.BaseType});return new KeyValuePair<MemberInfo,object[]>(tr.GetType().GetConstructor(new Type[]{typeof(Type)}),
new object[]{t});}else{return new KeyValuePair<MemberInfo,object[]>(tr.GetType().GetConstructor(new Type[]{typeof(string),typeof(CodeTypeReference[])}),
new object[]{tr.BaseType,_ToArray(tr.TypeArguments)});}}return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod("TypeReference"),
new object[]{tr.BaseType,tr.Options,_ToArray(tr.TypeArguments),tr.ArrayElementType,tr.ArrayRank});}static KeyValuePair<MemberInfo,object[]>_GetInstanceData(CodeStatement
 stmt){var a=stmt as CodeAssignStatement;if(null!=a){if(_HasExtraNonsense(a)){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
new object[]{a.Left,a.Right,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new
 Type[]{typeof(CodeExpression),typeof(CodeExpression)}),new object[]{a.Left,a.Right});}var ae=stmt as CodeAttachEventStatement;if(null!=ae){if(_HasExtraNonsense(ae))
{return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{ae.Event,ae.Listener,_ToArray(a.StartDirectives),
_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(CodeEventReferenceExpression),
typeof(CodeExpression)}),new object[]{ae.Event,ae.Listener});}var cm=stmt as CodeCommentStatement;if(null!=cm){if(_HasExtraNonsense(cm)){return new KeyValuePair<MemberInfo,
object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{cm.Comment,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),
a.LinePragma});}if(!cm.Comment.DocComment)return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(string)}),new object[]
{cm.Comment.Text});return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(string),typeof(bool)}),new object[]{cm.Comment.Text,cm.Comment.DocComment});
}var c=stmt as CodeConditionStatement;if(null!=c){if(_HasExtraNonsense(c)){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
new object[]{c.Condition,_ToArray(c.TrueStatements),_ToArray(c.FalseStatements),_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}
return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(CodeStatement[]),typeof(CodeStatement[])
}),new object[]{c.Condition,_ToArray(c.TrueStatements),_ToArray(c.FalseStatements)});}var e=stmt as CodeExpressionStatement;if(null!=e){if(_HasExtraNonsense(e))
{return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{e.Expression,_ToArray(a.StartDirectives),
_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(CodeExpression)}),
new object[]{e.Expression});}var g=stmt as CodeGotoStatement;if(null!=g){if(_HasExtraNonsense(g)){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
new object[]{g.Label,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new
 Type[]{typeof(string)}),new object[]{g.Label});}var i=stmt as CodeIterationStatement;if(null!=i){if(_HasExtraNonsense(i)){return new KeyValuePair<MemberInfo,
object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{i.InitStatement,i.TestExpression,i.IncrementStatement,_ToArray(i.Statements),
_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]
{typeof(CodeStatement),typeof(CodeExpression),typeof(CodeStatement),typeof(CodeStatement[])}),new object[]{i.InitStatement,i.TestExpression,i.IncrementStatement,
_ToArray(i.Statements)});}var l=stmt as CodeLabeledStatement;if(null!=l){if(_HasExtraNonsense(l)){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
new object[]{l.Label,l.Statement,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new
 Type[]{typeof(string),typeof(CodeStatement)}),new object[]{l.Label,l.Statement});}var r=stmt as CodeMethodReturnStatement;if(null!=r){if(_HasExtraNonsense(r))
{return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{r.Expression,_ToArray(a.StartDirectives),
_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(CodeExpression)
}),new object[]{r.Expression});}var re=stmt as CodeRemoveEventStatement;if(null!=re){if(_HasExtraNonsense(re)){return new KeyValuePair<MemberInfo,object[]>(
typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{re.Event,re.Listener,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),
a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(CodeEventReferenceExpression),typeof(CodeExpression)
}),new object[]{re.Event,re.Listener});}var s=stmt as CodeSnippetStatement;if(null!=s){if(_HasExtraNonsense(s)){return new KeyValuePair<MemberInfo,object[]>(
typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{s.Value,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma
});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]{typeof(string)}),new object[]{s.Value});}var t=stmt as CodeThrowExceptionStatement;
if(null!=t){if(_HasExtraNonsense(t)){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new
 object[]{t.ToThrow,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new
 Type[]{typeof(CodeExpression)}),new object[]{t.ToThrow});}var tc=stmt as CodeTryCatchFinallyStatement;if(null!=tc){if(_HasExtraNonsense(tc)){return new
 KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{_ToArray(tc.TryStatements),_ToArray(tc.CatchClauses),_ToArray(tc.FinallyStatements),
_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]
{typeof(CodeStatement[]),typeof(CodeCatchClause[]),typeof(CodeStatement[])}),new object[]{_ToArray(tc.TryStatements),_ToArray(tc.CatchClauses),_ToArray(tc.FinallyStatements)});
}var v=stmt as CodeVariableDeclarationStatement;if(null!=v){if(_HasExtraNonsense(v)){return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),
new object[]{v.Type,v.Name,v.InitExpression,_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(
stmt.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(string),typeof(CodeExpression)}),new object[]{v.Type,v.Name,v.InitExpression
});}throw new NotSupportedException("The statement instance data could not be serialized.");}static bool _HasExtraNonsense(CodeStatement stmt){return(null
!=stmt.LinePragma||0<stmt.StartDirectives.Count||0<stmt.EndDirectives.Count);}static KeyValuePair<MemberInfo,object[]>_GetInstanceData(CodeExpression value)
{var ar=value as CodeArgumentReferenceExpression;if(null!=ar)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string)
}),new object[]{ar.ParameterName});var ac=value as CodeArrayCreateExpression;if(null!=ac){if(null!=ac.Initializers&&0<ac.Initializers.Count){return new
 KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(CodeExpression[])}),new object[]{ac.CreateType,
_ToArray(ac.Initializers)});}else if(null!=ac.SizeExpression){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),
typeof(CodeExpression)}),new object[]{ac.CreateType,ac.SizeExpression});}else return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeTypeReference),typeof(int)}),new object[]{ac.CreateType,ac.Size});}var ai=value as CodeArrayIndexerExpression;if(null!=ai)return new
 KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(CodeExpression[])}),new object[]{ai.TargetObject,_ToArray(ai.Indices)
});var br=value as CodeBaseReferenceExpression;if(null!=br)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{}),new
 object[]{});var bo=value as CodeBinaryOperatorExpression;if(null!=bo)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]
{typeof(CodeExpression),typeof(CodeBinaryOperatorType),typeof(CodeExpression)}),new object[]{bo.Left,bo.Operator,bo.Right});var c=value as CodeCastExpression;
if(null!=c)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(CodeExpression)}),new
 object[]{c.TargetType,c.Expression});var dv=value as CodeDefaultValueExpression;if(null!=dv)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeTypeReference)}),new object[]{dv.Type});var dc=value as CodeDelegateCreateExpression;if(null!=dc)return new KeyValuePair<MemberInfo,
object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(CodeExpression),typeof(string)}),new object[]{dc.DelegateType,dc.TargetObject,dc.MethodName
});var di=value as CodeDelegateInvokeExpression;if(null!=di)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(CodeExpression[])
}),new object[]{di.TargetObject,_ToArray(di.Parameters)});var d=value as CodeDirectionExpression;if(null!=d)return new KeyValuePair<MemberInfo,object[]>(
value.GetType().GetConstructor(new Type[]{typeof(FieldDirection),typeof(CodeExpression)}),new object[]{d.Direction,d.Expression});var er=value as CodeEventReferenceExpression;
if(null!=er)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(string)}),new object[]
{er.TargetObject,er.EventName});var fr=value as CodeFieldReferenceExpression;if(null!=fr)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeExpression),typeof(string)}),new object[]{fr.TargetObject,fr.FieldName});var ci=value as CodeIndexerExpression;if(null!=ci)return new
 KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(CodeExpression[])}),new object[]{ci.TargetObject,_ToArray(ci.Indices)
});var mi=value as CodeMethodInvokeExpression;if(null!=mi)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeMethodReferenceExpression),typeof(CodeExpression[])}),
new object[]{mi.Method,_ToArray(mi.Parameters)});var mr=value as CodeMethodReferenceExpression;if(null!=mr)return new KeyValuePair<MemberInfo,object[]>(
value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(string)}),new object[]{mr.TargetObject,mr.MethodName});var oc=value as CodeObjectCreateExpression;
if(null!=oc)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(CodeExpression[])}),
new object[]{oc.CreateType,_ToArray(oc.Parameters)});var pd=value as CodeParameterDeclarationExpression;if(null!=pd){if(0==pd.CustomAttributes.Count&&
FieldDirection.In==pd.Direction){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(string)
}),new object[]{pd.Type,pd.Name});}return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(pd.GetType().Name.Substring(4)),new object[]
{pd.Type,pd.Name,pd.Direction,_ToArray(pd.CustomAttributes)});}var p=value as CodePrimitiveExpression;if(null!=p)return new KeyValuePair<MemberInfo,object[]>(
value.GetType().GetConstructor(new Type[]{typeof(object)}),new object[]{p.Value});var pr=value as CodePropertyReferenceExpression;if(null!=pr)return new
 KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(string)}),new object[]{pr.TargetObject,pr.PropertyName
});var ps=value as CodePropertySetValueReferenceExpression;if(null!=ps)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{}),new object[]{});var s=value as CodeSnippetExpression;if(null!=s)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(string)}),new object[]{s.Value});var th=value as CodeThisReferenceExpression;if(null!=th)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{}),new object[]{});var to=value as CodeTypeOfExpression;if(null!=to)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeTypeReference)}),new object[]{to.Type});var tr=value as CodeTypeReferenceExpression;if(null!=tr)return new KeyValuePair<MemberInfo,
object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference)}),new object[]{tr.Type});var vr=value as CodeVariableReferenceExpression;
if(null!=vr)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string)}),new object[]{vr.VariableName});throw
 new NotSupportedException("Unsupported code type. Cannot convert to instance data.");}static CodeAttributeArgument[]_ToArray(CodeAttributeArgumentCollection
 args){var result=new CodeAttributeArgument[args.Count];args.CopyTo(result,0);return result;}static CodeExpression[]_ToArray(CodeExpressionCollection exprs)
{var result=new CodeExpression[exprs.Count];exprs.CopyTo(result,0);return result;}static CodeCatchClause[]_ToArray(CodeCatchClauseCollection ccs){var result
=new CodeCatchClause[ccs.Count];ccs.CopyTo(result,0);return result;}static CodeStatement[]_ToArray(CodeStatementCollection stmts){var result=new CodeStatement[stmts.Count];
stmts.CopyTo(result,0);return result;}static CodeAttributeDeclaration[]_ToArray(CodeAttributeDeclarationCollection attrs){var result=new CodeAttributeDeclaration[attrs.Count];
attrs.CopyTo(result,0);return result;}static CodeDirective[]_ToArray(CodeDirectiveCollection dirs){var result=new CodeDirective[dirs.Count];dirs.CopyTo(result,
0);return result;}static CodeTypeReference[]_ToArray(CodeTypeReferenceCollection refs){var result=new CodeTypeReference[refs.Count];refs.CopyTo(result,
0);return result;}static CodeCommentStatement[]_ToArray(CodeCommentStatementCollection refs){var result=new CodeCommentStatement[refs.Count];refs.CopyTo(result,
0);return result;}static CodeTypeParameter[]_ToArray(CodeTypeParameterCollection refs){var result=new CodeTypeParameter[refs.Count];refs.CopyTo(result,
0);return result;}static CodeTypeMember[]_ToArray(CodeTypeMemberCollection refs){var result=new CodeTypeMember[refs.Count];refs.CopyTo(result,0);return
 result;}static string[]_ToArray(System.Collections.Specialized.StringCollection refs){var result=new string[refs.Count];refs.CopyTo(result,0);return result;
}static CodeNamespace[]_ToArray(CodeNamespaceCollection refs){var result=new CodeNamespace[refs.Count];refs.CopyTo(result,0);return result;}static CodeNamespaceImport[]
_ToArray(CodeNamespaceImportCollection refs){var result=new CodeNamespaceImport[refs.Count];((System.Collections.ICollection)refs).CopyTo(result,0);return
 result;}static CodeTypeDeclaration[]_ToArray(CodeTypeDeclarationCollection refs){var result=new CodeTypeDeclaration[refs.Count];refs.CopyTo(result,0);
return result;}static CodeParameterDeclarationExpression[]_ToArray(CodeParameterDeclarationExpressionCollection refs){var result=new CodeParameterDeclarationExpression[refs.Count];
refs.CopyTo(result,0);return result;}}}namespace CD{/// <summary>
/// Helper class for building CodeDOM trees
/// </summary>
#if GOKITLIB
public
#endif
static class CodeDomUtility{/// <summary>
/// Returns <see cref="CodeThisReferenceExpression"/>
/// </summary>
public static CodeThisReferenceExpression This{get;}=new CodeThisReferenceExpression();/// <summary>
/// Returns <see cref="CodePrimitiveExpression"/>(true)
/// </summary>
public static CodePrimitiveExpression True{get;}=new CodePrimitiveExpression(true);/// <summary>
/// Returns <see cref="CodePrimitiveExpression"/>(false)
/// </summary>
public static CodePrimitiveExpression False{get;}=new CodePrimitiveExpression(false);/// <summary>
/// Returns <see cref="CodePrimitiveExpression"/>(0)
/// </summary>
public static CodePrimitiveExpression Zero{get;}=new CodePrimitiveExpression(0);/// <summary>
/// Returns <see cref="CodePrimitiveExpression"/>(1)
/// </summary>
public static CodePrimitiveExpression One{get;}=new CodePrimitiveExpression(1);/// <summary>
/// Returns <see cref="CodePrimitiveExpression"/>(-1)
/// </summary>
public static CodePrimitiveExpression NegOne{get;}=new CodePrimitiveExpression(-1);/// <summary>
/// Returns <see cref="CodePrimitiveExpression"/>(null)
/// </summary>
public static CodePrimitiveExpression Null{get;}=new CodePrimitiveExpression(null);/// <summary>
/// Returns <see cref="CodeConditionStatement"/> with the specified parameters
/// </summary>
/// <param name="cnd">The condition</param>
/// <param name="trueStatements">Execute if <paramref name="cnd"/> is true</param>
/// <returns>A <see cref="CodeConditionStatement"/> with the specifed parameters</returns>
public static CodeConditionStatement If(CodeExpression cnd,params CodeStatement[]trueStatements)=>new CodeConditionStatement(cnd,trueStatements);/// <summary>
/// Returns <see cref="CodeConditionStatement"/> with the specified parameters
/// </summary>
/// <param name="cnd">The condition</param>
/// <param name="trueStatements">Execute if <paramref name="cnd"/> is true</param>
/// <param name="falseStatements">Execute if <paramref name="cnd"/> is false</param>
/// <returns>A <see cref="CodeConditionStatement"/> with the specifed parameters</returns>
public static CodeConditionStatement IfElse(CodeExpression cnd,CodeStatementCollection trueStatements,params CodeStatement[]falseStatements){var result
=new CodeConditionStatement(cnd);result.TrueStatements.AddRange(trueStatements);result.FalseStatements.AddRange(falseStatements);return result;}/// <summary>
/// Returns <see cref="CodeConditionStatement"/> with the specified parameters
/// </summary>
/// <param name="cnd">The condition</param>
/// <param name="trueStatements">Execute if <paramref name="cnd"/> is true</param>
/// <param name="falseStatements">Execute if <paramref name="cnd"/> is false</param>
/// <returns>A <see cref="CodeConditionStatement"/> with the specifed parameters</returns>
public static CodeConditionStatement IfElse(CodeExpression cnd,IEnumerable<CodeStatement>trueStatements,params CodeStatement[]falseStatements){var result
=new CodeConditionStatement(cnd);foreach(var stmt in trueStatements)result.TrueStatements.Add(stmt);result.FalseStatements.AddRange(falseStatements);return
 result;}/// <summary>
/// Returns a <see cref="CodeFieldReferenceExpression"/> with the specified parameters
/// </summary>
/// <param name="target">The target object</param>
/// <param name="name">The name of the member</param>
/// <returns>A <see cref="CodeFieldReferenceExpression"/> with the specified parameters</returns>
public static CodeFieldReferenceExpression FieldRef(CodeExpression target,string name)=>new CodeFieldReferenceExpression(target,name);/// <summary>
/// Returns a <see cref="CodePropertyReferenceExpression"/> with the specified parameters
/// </summary>
/// <param name="target">The target object</param>
/// <param name="name">The name of the member</param>
/// <returns>A <see cref="CodePropertyReferenceExpression"/> with the specified parameters</returns>
public static CodePropertyReferenceExpression PropRef(CodeExpression target,string name)=>new CodePropertyReferenceExpression(target,name);/// <summary>
/// Returns a <see cref="CodeMethodReferenceExpression"/> with the specified parameters
/// </summary>
/// <param name="target">The target object</param>
/// <param name="name">The name of the member</param>
/// <returns>A <see cref="CodeMethodReferenceExpression"/> with the specified parameters</returns>
public static CodeMethodReferenceExpression MethRef(CodeExpression target,string name)=>new CodeMethodReferenceExpression(target,name);/// <summary>
/// Returns a <see cref="CodeVariableReferenceExpression"/> with the specified name
/// </summary>
/// <param name="name">The name of the variable</param>
/// <returns>A <see cref="CodeVariableReferenceExpression"/> with the specified name</returns>
public static CodeVariableReferenceExpression VarRef(string name)=>new CodeVariableReferenceExpression(name);/// <summary>
/// Returns a <see cref="CodeArgumentReferenceExpression"/> with the specified name
/// </summary>
/// <param name="name">The name of the argument</param>
/// <returns>A <see cref="CodeArgumentReferenceExpression"/> with the specified name</returns>
public static CodeArgumentReferenceExpression ArgRef(string name)=>new CodeArgumentReferenceExpression(name);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified generic parameter
/// </summary>
/// <param name="typeParam">The name of type parameter</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified generic parameter</returns>
public static CodeTypeReference Type(CodeTypeParameter typeParam)=>new CodeTypeReference(typeParam);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified type
/// </summary>
/// <param name="typeName">The name of type</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified type</returns>
public static CodeTypeReference Type(string typeName)=>new CodeTypeReference(typeName);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified type
/// </summary>
/// <param name="type">The type</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified type</returns>
public static CodeTypeReference Type(Type type)=>new CodeTypeReference(type);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified type and rank
/// </summary>
/// <param name="arrayType">The element type of the array</param>
/// <param name="arrayRank">The number of dimensions in the array</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified type and rank</returns>
public static CodeTypeReference Type(CodeTypeReference arrayType,int arrayRank)=>new CodeTypeReference(arrayType,arrayRank);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified type and rank
/// </summary>
/// <param name="arrayType">The name of the element type of the array</param>
/// <param name="arrayRank">The number of dimensions in the array</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified type and rank</returns>
public static CodeTypeReference Type(string arrayType,int arrayRank)=>new CodeTypeReference(arrayType,arrayRank);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified type and options
/// </summary>
/// <param name="typeName">The type name</param>
/// <param name="options">The options</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified type and options</returns>
public static CodeTypeReference Type(string typeName,CodeTypeReferenceOptions options)=>new CodeTypeReference(typeName,options);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified type and options
/// </summary>
/// <param name="type">The type</param>
/// <param name="options">The options</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified type and options</returns>
public static CodeTypeReference Type(Type type,CodeTypeReferenceOptions options)=>new CodeTypeReference(type,options);/// <summary>
/// Returns a <see cref="CodeTypeReference"/> with the specified type and type arguments
/// </summary>
/// <param name="typeName">The name of the type</param>
/// <param name="typeArguments">The type arguments</param>
/// <returns>A <see cref="CodeTypeReference"/> with the specified type and type arguments</returns>
public static CodeTypeReference Type(string typeName,params CodeTypeReference[]typeArguments)=>new CodeTypeReference(typeName,typeArguments);/// <summary>
/// Returns a <see cref="CodeTypeReferenceExpression"/> with the specified type
/// </summary>
/// <param name="typeRef">The <see cref="CodeTypeReference"/></param>
/// <returns>A <see cref="CodeTypeReferenceExpression"/> with the specified type</returns>
public static CodeTypeReferenceExpression TypeRef(CodeTypeReference typeRef)=>new CodeTypeReferenceExpression(typeRef);/// <summary>
/// Returns a <see cref="CodeTypeReferenceExpression"/> with the specified type
/// </summary>
/// <param name="type">The type</param>
/// <returns>A <see cref="CodeTypeReferenceExpression"/> with the specified type</returns>
public static CodeTypeReferenceExpression TypeRef(Type type)=>new CodeTypeReferenceExpression(type);/// <summary>
/// Returns a <see cref="CodeTypeReferenceExpression"/> with the specified type
/// </summary>
/// <param name="typeName">The type name</param>
/// <returns>A <see cref="CodeTypeReferenceExpression"/> with the specified type</returns>
public static CodeTypeReferenceExpression TypeRef(string typeName)=>new CodeTypeReferenceExpression(typeName);/// <summary>
/// Returns a <see cref="CodeTypeOfExpression"/> with the specified type
/// </summary>
/// <param name="type">The type</param>
/// <returns>A <see cref="CodeTypeOfExpression"/> with the specified type</returns>
public static CodeTypeOfExpression TypeOf(CodeTypeReference type)=>new CodeTypeOfExpression(type);/// <summary>
/// Returns a <see cref="CodeTypeOfExpression"/> with the specified type
/// </summary>
/// <param name="typeName">The type name</param>
/// <returns>A <see cref="CodeTypeOfExpression"/> with the specified type</returns>
public static CodeTypeOfExpression TypeOf(string typeName)=>new CodeTypeOfExpression(typeName);/// <summary>
/// Returns a <see cref="CodeTypeOfExpression"/> with the specified type
/// </summary>
/// <param name="type">The type</param>
/// <returns>A <see cref="CodeTypeOfExpression"/> with the specified type</returns>
public static CodeTypeOfExpression TypeOf(Type type)=>new CodeTypeOfExpression(type);/// <summary>
/// Creates one or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters
/// </summary>
/// <param name="left">The left hand side expression</param>
/// <param name="type">A <see cref="CodeBinaryOperatorType"/> value indicating the type of operation</param>
/// <param name="right">The first right hand side expression</param>
/// <param name="rightN">The remainder right hand side expressions</param>
/// <returns>One or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters</returns>
public static CodeBinaryOperatorExpression BinOp(CodeExpression left,CodeBinaryOperatorType type,CodeExpression right,params CodeExpression[]rightN){var
 exprs=new CodeExpressionCollection();exprs.Add(left);exprs.Add(right);exprs.AddRange(rightN); return _MakeBinOps(exprs,type)as CodeBinaryOperatorExpression;
}/// <summary>
/// Creates one or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters
/// </summary>
/// <param name="exprs">The expressions</param>
/// <param name="type">A <see cref="CodeBinaryOperatorType"/> value indicating the type of operation</param>
/// <returns>One or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters</returns>
public static CodeBinaryOperatorExpression BinOp(IEnumerable<CodeExpression>exprs,CodeBinaryOperatorType type){CodeExpression left=null;CodeExpression
 right=null;var rightN=new List<CodeExpression>();foreach(var e in exprs){if(null==left)left=e;else if(null==right)right=e;else rightN.Add(e);}if(null
==left||null==right)throw new ArgumentException("There must be at least two expressions",nameof(exprs));return BinOp(left,type,right,rightN.ToArray());
}/// <summary>
/// Creates one or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters
/// </summary>
/// <param name="exprs">The expressions</param>
/// <param name="type">A <see cref="CodeBinaryOperatorType"/> value indicating the type of operation</param>
/// <returns>One or more potentially nested <see cref="CodeBinaryOperatorExpression"/> objects with the specified parameters</returns>
public static CodeBinaryOperatorExpression BinOp(CodeExpressionCollection exprs,CodeBinaryOperatorType type){CodeExpression left=null;CodeExpression right
=null;var rightN=new List<CodeExpression>();foreach(CodeExpression e in exprs){if(null==left)left=e;else if(null==right)right=e;else rightN.Add(e);}if
(null==right)throw new ArgumentException("There must be at least two expressions",nameof(exprs));return BinOp(left,type,right,rightN.ToArray());}/// <summary>
/// Creates a simple or complex literal expression value based on <paramref name="value"/>
/// </summary>
/// <param name="value">The instance to serialize to code</param>
/// <param name="typeConverter">An optional type converter to use. If specified, the same type converter will be used for all elements and subelements of <paramref name="value"/>.</param>
/// <returns>A <see cref="CodeExpression"/> that can be used to instantiate <paramref name="value"/></returns>
public static CodeExpression Literal(object value,TypeConverter typeConverter=null){return _Serialize(value,typeConverter);}/// <summary>
/// Creates a <see cref="CodeCastExpression"/> based on the target type and expression
/// </summary>
/// <param name="type">The type to cast to</param>
/// <param name="target">The expression to cast</param>
/// <returns>A <see cref="CodeCastExpression"/> based on the target type and expression</returns>
public static CodeCastExpression Cast(CodeTypeReference type,CodeExpression target)=>new CodeCastExpression(type,target);/// <summary>
/// Creates a <see cref="CodeCastExpression"/> based on the target type and expression
/// </summary>
/// <param name="typeName">The type to cast to</param>
/// <param name="target">The expression to cast</param>
/// <returns>A <see cref="CodeCastExpression"/> based on the target type and expression</returns>
public static CodeCastExpression Cast(string typeName,CodeExpression target)=>new CodeCastExpression(typeName,target);/// <summary>
/// Creates a <see cref="CodeCastExpression"/> based on the target type and expression
/// </summary>
/// <param name="type">The type to cast to</param>
/// <param name="target">The expression to cast</param>
/// <returns>A <see cref="CodeCastExpression"/> based on the target type and expression</returns>
public static CodeCastExpression Cast(Type type,CodeExpression target)=>new CodeCastExpression(type,target);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> adding two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> adding two or more expressions</returns>
public static CodeBinaryOperatorExpression Add(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.Add,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> assigning one or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> assigning or more expressions</returns>
public static CodeBinaryOperatorExpression AssignExpr(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.Assign,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a bitwise and on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a bitwise and on two or more expressions</returns>
public static CodeBinaryOperatorExpression BitwiseAnd(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.BitwiseAnd,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a bitwise or on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a bitwise or on two or more expressions</returns>
public static CodeBinaryOperatorExpression BitwiseOr(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.BitwiseOr,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an and on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an and on two or more expressions</returns>
public static CodeBinaryOperatorExpression And(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.BooleanAnd,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an or on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an or on two or more expressions</returns>
public static CodeBinaryOperatorExpression Or(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.BooleanOr,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> dividing two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> dividing two or more expressions</returns>
public static CodeBinaryOperatorExpression Div(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.Divide,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a greater than comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a greater than comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression Gt(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.GreaterThan,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a greater than or equal comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a greater than or equal comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression Gte(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.GreaterThanOrEqual,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a value equality comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a value equality comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression Eq(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.ValueEquality,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a value inequality comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a value inequality comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression NotEq(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>Eq(False,BinOp(left,CodeBinaryOperatorType.ValueEquality,
right,rightN));/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a less than comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a less than comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression Lt(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.LessThan,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a less than or equal comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a less than or equal comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression Lte(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.LessThanOrEqual,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a modulo on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a modulo on two or more expressions</returns>
public static CodeBinaryOperatorExpression Mod(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.Modulus,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> multiplying two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> multiplying two or more expressions</returns>
public static CodeBinaryOperatorExpression Mul(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.Multiply,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> subtracting two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> subtracting two or more expressions</returns>
public static CodeBinaryOperatorExpression Sub(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.Subtract,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an identity equality comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an identity equality comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression IdentEq(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.IdentityEquality,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing an identity inequality comparison on two or more expressions
/// </summary>
/// <param name="left">The left hand side</param>
/// <param name="right">The first right hand side</param>
/// <param name="rightN">The remainder right hand sides</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing an identity inequality comparison on two or more expressions</returns>
public static CodeBinaryOperatorExpression IdentNotEq(CodeExpression left,CodeExpression right,params CodeExpression[]rightN)=>BinOp(left,CodeBinaryOperatorType.IdentityInequality,
right,rightN);/// <summary>
/// Returns a <see cref="CodeBinaryOperatorExpression"/> doing a boolean not on the target expression
/// </summary>
/// <param name="target">The target expression</param>
/// <returns>A <see cref="CodeBinaryOperatorExpression"/> doing a boolean not on the target expression</returns>
public static CodeBinaryOperatorExpression Not(CodeExpression target)=>new CodeBinaryOperatorExpression(False,CodeBinaryOperatorType.ValueEquality,target);
/// <summary>
/// Returns a <see cref="CodeAssignStatement"/> assigning the target to the specified value
/// </summary>
/// <param name="target">The target to assign</param>
/// <param name="value">The value to assign to</param>
/// <returns>A <see cref="CodeAssignStatement"/> assigning the target to the specified value</returns>
public static CodeAssignStatement Let(CodeExpression target,CodeExpression value)=>new CodeAssignStatement(target,value);/// <summary>
/// Creates a <see cref="CodeMethodReturnStatement"/> with the optionally specified expression
/// </summary>
/// <param name="target">The target expression to return, or null for no return value</param>
/// <returns>A <see cref="CodeMethodReturnStatement"/> with the optionally specified expression</returns>
public static CodeMethodReturnStatement Return(CodeExpression target=null)=>null!=target?new CodeMethodReturnStatement(target):new CodeMethodReturnStatement();
/// <summary>
/// Creates a <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression
/// </summary>
/// <param name="method">The method to invoke</param>
/// <param name="arguments">The arguments to invoke with</param>
/// <returns>A <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression</returns>
public static CodeMethodInvokeExpression Invoke(CodeMethodReferenceExpression method,params CodeExpression[]arguments)=>new CodeMethodInvokeExpression(method,
arguments);/// <summary>
/// Creates a <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression
/// </summary>
/// <param name="target">The target object or type where the method resides</param>
/// <param name="name">The name of the method</param>
/// <param name="arguments">The arguments to invoke with</param>
/// <returns>A <see cref="CodeMethodInvokeExpression"/> that invokes a function with a return value as an expression</returns>
public static CodeMethodInvokeExpression Invoke(CodeExpression target,string name,params CodeExpression[]arguments)=>new CodeMethodInvokeExpression(MethRef(target,
name),arguments);/// <summary>
/// Creates a <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.
/// </summary>
/// <param name="method">The method to invoke</param>
/// <param name="arguments">The arguments to invoke with</param>
/// <returns>A <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.</returns>
public static CodeExpressionStatement Call(CodeMethodReferenceExpression method,params CodeExpression[]arguments)=>new CodeExpressionStatement(Invoke(method,
arguments));/// <summary>
/// Creates a <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.
/// </summary>
/// <param name="target">The target object or type where the method resides</param>
/// <param name="name">The name of the method</param>
/// <param name="arguments">The arguments to invoke with</param>
/// <returns>A <see cref="CodeExpressionStatement"/> that invokes a method as a statement without considering a return value.</returns>
public static CodeExpressionStatement Call(CodeExpression target,string name,params CodeExpression[]arguments)=>new CodeExpressionStatement(Invoke(MethRef(target,
name),arguments));/// <summary>
/// Creates a <see cref="CodeDefaultValueExpression"/> with the specified type
/// </summary>
/// <param name="type">The type</param>
/// <returns>A <see cref="CodeDefaultValueExpression"/> with the specified type</returns>
public static CodeDefaultValueExpression Default(CodeTypeReference type)=>new CodeDefaultValueExpression(type);/// <summary>
/// Creates a <see cref="CodeDefaultValueExpression"/> with the specified type
/// </summary>
/// <param name="typeName">The type</param>
/// <returns>A <see cref="CodeDefaultValueExpression"/> with the specified type</returns>
public static CodeDefaultValueExpression Default(string typeName)=>new CodeDefaultValueExpression(Type(typeName));/// <summary>
/// Creates a <see cref="CodeDefaultValueExpression"/> with the specified type
/// </summary>
/// <param name="type">The type</param>
/// <returns>A <see cref="CodeDefaultValueExpression"/> with the specified type</returns>
public static CodeDefaultValueExpression Default(Type type)=>new CodeDefaultValueExpression(Type(type));/// <summary>
/// Creates a <see cref="CodeObjectCreateExpression"/> with the specified type and arguments
/// </summary>
/// <param name="type">The type to create</param>
/// <param name="arguments">The arguments to pass to the constructor</param>
/// <returns>A <see cref="CodeObjectCreateExpression"/> with the specified type and arguments</returns>
public static CodeObjectCreateExpression New(CodeTypeReference type,params CodeExpression[]arguments)=>new CodeObjectCreateExpression(type,arguments);
/// <summary>
/// Creates a <see cref="CodeObjectCreateExpression"/> with the specified type and arguments
/// </summary>
/// <param name="type">The type to create</param>
/// <param name="arguments">The arguments to pass to the constructor</param>
/// <returns>A <see cref="CodeObjectCreateExpression"/> with the specified type and arguments</returns>
public static CodeObjectCreateExpression New(Type type,params CodeExpression[]arguments)=>new CodeObjectCreateExpression(type,arguments);/// <summary>
/// Creates a <see cref="CodeObjectCreateExpression"/> with the specified type and arguments
/// </summary>
/// <param name="typeName">The type to create</param>
/// <param name="arguments">The arguments to pass to the constructor</param>
/// <returns>A <see cref="CodeObjectCreateExpression"/> with the specified type and arguments</returns>
public static CodeObjectCreateExpression New(string typeName,params CodeExpression[]arguments)=>new CodeObjectCreateExpression(typeName,arguments);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers
/// </summary>
/// <param name="arrayType">The element type of the array</param>
/// <param name="initializers">The initializers to create the array with</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers</returns>
public static CodeArrayCreateExpression NewArr(CodeTypeReference arrayType,params CodeExpression[]initializers)=>new CodeArrayCreateExpression(arrayType,
initializers);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers
/// </summary>
/// <param name="arrayTypeName">The element type of the array</param>
/// <param name="initializers">The initializers to create the array with</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers</returns>
public static CodeArrayCreateExpression NewArr(string arrayTypeName,params CodeExpression[]initializers)=>new CodeArrayCreateExpression(arrayTypeName,
initializers);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers
/// </summary>
/// <param name="arrayType">The element type of the array</param>
/// <param name="initializers">The initializers to create the array with</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and initializers</returns>
public static CodeArrayCreateExpression NewArr(Type arrayType,params CodeExpression[]initializers)=>new CodeArrayCreateExpression(arrayType,initializers);
/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
/// </summary>
/// <param name="arrayType">The element type of the array</param>
/// <param name="size">The size of the array</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
public static CodeArrayCreateExpression NewArr(CodeTypeReference arrayType,CodeExpression size)=>new CodeArrayCreateExpression(arrayType,size);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
/// </summary>
/// <param name="arrayType">The element type of the array</param>
/// <param name="size">The size of the array</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
public static CodeArrayCreateExpression NewArr(CodeTypeReference arrayType,int size)=>new CodeArrayCreateExpression(arrayType,size);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
/// </summary>
/// <param name="arrayTypeName">The element type of the array</param>
/// <param name="size">The size of the array</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
public static CodeArrayCreateExpression NewArr(string arrayTypeName,CodeExpression size)=>new CodeArrayCreateExpression(arrayTypeName,size);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
/// </summary>
/// <param name="arrayTypeName">The element type of the array</param>
/// <param name="size">The size of the array</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
public static CodeArrayCreateExpression NewArr(string arrayTypeName,int size)=>new CodeArrayCreateExpression(arrayTypeName,size);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
/// </summary>
/// <param name="arrayType">The element type of the array</param>
/// <param name="size">The size of the array</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
public static CodeArrayCreateExpression NewArr(Type arrayType,CodeExpression size)=>new CodeArrayCreateExpression(arrayType,size);/// <summary>
/// Creates a <see cref="CodeArrayCreateExpression"/> with the specified array element type and size
/// </summary>
/// <param name="arrayType">The element type of the array</param>
/// <param name="size">The size of the array</param>
/// <returns>A <see cref="CodeArrayCreateExpression"/> with the specified array element type and size</returns>
public static CodeArrayCreateExpression NewArr(Type arrayType,int size)=>new CodeArrayCreateExpression(arrayType,size);/// <summary>
/// Creates a <see cref="CodeIndexerExpression"/> with the specified target and indices
/// </summary>
/// <param name="target">The target to index into</param>
/// <param name="indices">The indices to use</param>
/// <returns>A <see cref="CodeIndexerExpression"/> with the specified target and indices</returns>
public static CodeIndexerExpression Indexer(CodeExpression target,params CodeExpression[]indices)=>new CodeIndexerExpression(target,indices);/// <summary>
/// Creates a <see cref="CodeArrayIndexerExpression"/> with the specified target and indices
/// </summary>
/// <param name="target">The array to index into</param>
/// <param name="indices">The indices to use</param>
/// <returns>A <see cref="CodeIndexerExpression"/> with the specified target and indices</returns>
public static CodeArrayIndexerExpression ArrIndexer(CodeExpression target,params CodeExpression[]indices)=>new CodeArrayIndexerExpression(target,indices);
/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name
/// </summary>
/// <param name="type">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name</returns>
public static CodeParameterDeclarationExpression Param(CodeTypeReference type,string name)=>new CodeParameterDeclarationExpression(type,name);/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name
/// </summary>
/// <param name="typeName">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name</returns>
public static CodeParameterDeclarationExpression Param(string typeName,string name)=>new CodeParameterDeclarationExpression(typeName,name);/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name
/// </summary>
/// <param name="type">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name</returns>
public static CodeParameterDeclarationExpression Param(Type type,string name)=>new CodeParameterDeclarationExpression(type,name);/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction
/// </summary>
/// <param name="type">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction</returns>
public static CodeParameterDeclarationExpression OutParam(CodeTypeReference type,string name){var result=new CodeParameterDeclarationExpression(type,name);
result.Direction=FieldDirection.Out;return result;}/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction
/// </summary>
/// <param name="typeName">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction</returns>
public static CodeParameterDeclarationExpression OutParam(string typeName,string name){var result=new CodeParameterDeclarationExpression(typeName,name);
result.Direction=FieldDirection.Out;return result;}/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction
/// </summary>
/// <param name="type">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and out direction</returns>
public static CodeParameterDeclarationExpression OutParam(Type type,string name){var result=new CodeParameterDeclarationExpression(type,name);result.Direction
=FieldDirection.Out;return result;}/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction
/// </summary>
/// <param name="type">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction</returns>
public static CodeParameterDeclarationExpression RefParam(CodeTypeReference type,string name){var result=new CodeParameterDeclarationExpression(type,name);
result.Direction=FieldDirection.Ref;return result;}/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction
/// </summary>
/// <param name="typeName">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction</returns>
public static CodeParameterDeclarationExpression RefParam(string typeName,string name){var result=new CodeParameterDeclarationExpression(typeName,name);
result.Direction=FieldDirection.Ref;return result;}/// <summary>
/// Creates a <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction
/// </summary>
/// <param name="type">The parameter type</param>
/// <param name="name">The parameter name</param>
/// <returns>A <see cref="CodeParameterDeclarationExpression"/> with the specified type and name, and ref direction</returns>
public static CodeParameterDeclarationExpression RefParam(Type type,string name){var result=new CodeParameterDeclarationExpression(type,name);result.Direction
=FieldDirection.Ref;return result;}/// <summary>
/// Creates a <see cref="CodeVariableDeclarationStatement"/> with the specified type and name
/// </summary>
/// <param name="type">The type of the variable</param>
/// <param name="name">The name of the variable</param>
/// <param name="initializer">An optional initializer</param>
/// <returns>A <see cref="CodeVariableDeclarationStatement"/> with the specified type and name</returns>
public static CodeVariableDeclarationStatement Var(CodeTypeReference type,string name,CodeExpression initializer=null)=>new CodeVariableDeclarationStatement(type,
name,initializer);/// <summary>
/// Creates a <see cref="CodeVariableDeclarationStatement"/> with the specified type and name
/// </summary>
/// <param name="typeName">The type of the variable</param>
/// <param name="name">The name of the variable</param>
/// <param name="initializer">An optional initializer</param>
/// <returns>A <see cref="CodeVariableDeclarationStatement"/> with the specified type and name</returns>
public static CodeVariableDeclarationStatement Var(string typeName,string name,CodeExpression initializer=null)=>new CodeVariableDeclarationStatement(typeName,
name,initializer);/// <summary>
/// Creates a <see cref="CodeVariableDeclarationStatement"/> with the specified type and name
/// </summary>
/// <param name="type">The type of the variable</param>
/// <param name="name">The name of the variable</param>
/// <param name="initializer">An optional initializer</param>
/// <returns>A <see cref="CodeVariableDeclarationStatement"/> with the specified type and name</returns>
public static CodeVariableDeclarationStatement Var(Type type,string name,CodeExpression initializer=null)=>new CodeVariableDeclarationStatement(type,name,
initializer);/// <summary>
/// Creates a <see cref="CodeIterationStatement"/> statement with the specified condition and statements
/// </summary>
/// <param name="cnd">The condition</param>
/// <param name="statements">The statements in the loop</param>
/// <returns>A <see cref="CodeIterationStatement"/> statement with the specified condition and statements</returns>
public static CodeIterationStatement While(CodeExpression cnd,params CodeStatement[]statements)=>new CodeIterationStatement(new CodeSnippetStatement(),
cnd,new CodeSnippetStatement(),statements);/// <summary>
/// Creates a <see cref="CodeIterationStatement"/> statement with the specified init statement, condition, increment statement, and inner statements
/// </summary>
/// <param name="init">The init statement</param>
/// <param name="cnd">The condition</param>
/// <param name="inc">The increment statement</param>
/// <param name="statements">The statements in the loop</param>
/// <returns>A <see cref="CodeIterationStatement"/> statement with the specified init statement, condition, increment statement, and inner statements</returns>
public static CodeIterationStatement For(CodeStatement init,CodeExpression cnd,CodeStatement inc,params CodeStatement[]statements)=>new CodeIterationStatement(init
??new CodeSnippetStatement(),cnd,inc??new CodeSnippetStatement(),statements);/// <summary>
/// Creates a <see cref="CodeGotoStatement"/> with the specified target label
/// </summary>
/// <param name="label">The destination label</param>
/// <returns>A <see cref="CodeGotoStatement"/> with the specified target label</returns>
public static CodeGotoStatement Goto(string label)=>new CodeGotoStatement(label);/// <summary>
/// Creates a <see cref="CodeThrowExceptionStatement"/> with the specified target expression
/// </summary>
/// <param name="target"></param>
/// <returns>A <see cref="CodeThrowExceptionStatement"/> with the specified target expression</returns>
public static CodeThrowExceptionStatement Throw(CodeExpression target)=>new CodeThrowExceptionStatement(target);/// <summary>
/// Creates a <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members
/// </summary>
/// <param name="name">The name of the type</param>
/// <param name="isPublic">True if the type is public</param>
/// <param name="members">A list of fields, methods and properties to add to the type</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
public static CodeTypeDeclaration TypeDecl(string name,bool isPublic=false,params CodeTypeMember[]members){var result=new CodeTypeDeclaration(name);if
(isPublic)result.TypeAttributes=TypeAttributes.Public;else result.TypeAttributes=TypeAttributes.NotPublic;for(var i=0;i<members.Length;i++)result.Members.Add(members[i]);
return result;}/// <summary>
/// Creates a <see cref="CodeTypeDeclaration"/> class with the specified name, access modifiers, and members
/// </summary>
/// <param name="name">The name of the type</param>
/// <param name="isPublic">True if the type is public</param>
/// <param name="members">A list of fields, methods and properties to add to the type</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
public static CodeTypeDeclaration Class(string name,bool isPublic=false,params CodeTypeMember[]members){var result=TypeDecl(name,isPublic,members);result.IsClass
=true;return result;}/// <summary>
/// Creates a <see cref="CodeTypeDeclaration"/> struct with the specified name, access modifiers, and members
/// </summary>
/// <param name="name">The name of the type</param>
/// <param name="isPublic">True if the type is public</param>
/// <param name="members">A list of fields, methods and properties to add to the type</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
public static CodeTypeDeclaration Struct(string name,bool isPublic=false,params CodeTypeMember[]members){var result=TypeDecl(name,isPublic,members);result.IsStruct
=true;return result;}/// <summary>
/// Creates a <see cref="CodeTypeDeclaration"/> enum with the specified name, access modifiers, and members
/// </summary>
/// <param name="name">The name of the type</param>
/// <param name="isPublic">True if the type is public</param>
/// <param name="members">A list of fields to add to the type</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
public static CodeTypeDeclaration Enum(string name,bool isPublic=false,params CodeTypeMember[]members){var result=TypeDecl(name,isPublic,members);result.IsEnum
=true;return result;}/// <summary>
/// Creates a <see cref="CodeTypeDeclaration"/> interface with the specified name, access modifiers, and members
/// </summary>
/// <param name="name">The name of the type</param>
/// <param name="isPublic">True if the type is public</param>
/// <param name="members">A list of methods and properties to add to the type</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> with the specified name, access modifiers, and members</returns>
public static CodeTypeDeclaration Interface(string name,bool isPublic=false,params CodeTypeMember[]members){var result=TypeDecl(name,isPublic,members);
result.IsInterface=true;return result;}/// <summary>
/// Creates a <see cref="CodeMemberMethod"/> with the specified name, modifiers, and parameters
/// </summary>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters</param>
/// <returns>A <see cref="CodeMemberMethod"/> with the specified name, modifiers, and parameters</returns>
public static CodeMemberMethod Method(string name,MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]parameters)
{var result=new CodeMemberMethod();result.Name=name;result.Attributes=attrs;for(var i=0;i<parameters.Length;i++)result.Parameters.Add(parameters[i]);return
 result;}/// <summary>
/// Creates a <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters
/// </summary>
/// <param name="returnType">The return type of the method</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters</param>
/// <returns>A <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters</returns>
public static CodeMemberMethod Method(CodeTypeReference returnType,string name,MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]
parameters){var result=Method(name,attrs,parameters);if(null!=returnType)result.ReturnType=returnType;return result;}/// <summary>
/// Creates a <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters
/// </summary>
/// <param name="returnTypeName">The return type of the method</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters</param>
/// <returns>A <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters</returns>
public static CodeMemberMethod Method(string returnTypeName,string name,MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]
parameters)=>Method(Type(returnTypeName),name,attrs,parameters);/// <summary>
/// Creates a <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters
/// </summary>
/// <param name="returnType">The return type of the method</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters</param>
/// <returns>A <see cref="CodeMemberMethod"/> with the specified return type, name, modifiers, and parameters</returns>
public static CodeMemberMethod Method(Type returnType,string name,MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]
parameters)=>Method(Type(returnType),name,attrs,parameters);/// <summary>
/// Creates a <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer
/// </summary>
/// <param name="type">The type of the field</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="initializer">The optional initializer</param>
/// <returns>A <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer</returns>
public static CodeMemberField Field(CodeTypeReference type,string name,MemberAttributes attrs=default(MemberAttributes),CodeExpression initializer=null)
{var result=new CodeMemberField();result.Type=type;result.Name=name;result.Attributes=attrs;result.InitExpression=initializer;return result;}/// <summary>
/// Creates a <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer
/// </summary>
/// <param name="typeName">The type of the field</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="initializer">The optional initializer</param>
/// <returns>A <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer</returns>
public static CodeMemberField Field(string typeName,string name,MemberAttributes attrs=default(MemberAttributes),CodeExpression initializer=null)=>Field(Type(typeName),
name,attrs,initializer);/// <summary>
/// Creates a <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer
/// </summary>
/// <param name="type">The type of the field</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="initializer">The optional initializer</param>
/// <returns>A <see cref="CodeMemberField"/> with the specified type, name, modifiers, and initializer</returns>
public static CodeMemberField Field(Type type,string name,MemberAttributes attrs=default(MemberAttributes),CodeExpression initializer=null)=>Field(Type(type),
name,attrs,initializer);/// <summary>
/// Creates a <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters
/// </summary>
/// <param name="type">The type of the field</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters of the property (for indexers)</param>
/// <returns>A <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters</returns>
public static CodeMemberProperty Property(CodeTypeReference type,string name,MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]
parameters){var result=new CodeMemberProperty();result.Name=name;result.Attributes=attrs;result.Type=type;for(var i=0;i<parameters.Length;++i)result.Parameters.Add(parameters[i]);
return result;}/// <summary>
/// Creates a <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters
/// </summary>
/// <param name="typeName">The type of the field</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters of the property (for indexers)</param>
/// <returns>A <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters</returns>
public static CodeMemberProperty Property(string typeName,string name,MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]
parameters)=>Property(Type(typeName),name,attrs,parameters);/// <summary>
/// Creates a <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters
/// </summary>
/// <param name="type">The type of the field</param>
/// <param name="name">The name of the member</param>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters of the property (for indexers)</param>
/// <returns>A <see cref="CodeMemberProperty"/> with the specified type, name, modifiers, and parameters</returns>
public static CodeMemberProperty Property(Type type,string name,MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]
parameters)=>Property(Type(type),name,attrs,parameters);/// <summary>
/// Creates a <see cref="CodeConstructor"/> with the specified modifiers and parameters
/// </summary>
/// <param name="attrs">The modifier attributes</param>
/// <param name="parameters">The parameters</param>
/// <returns>A <see cref="CodeConstructor"/> with the specified modifiers and parameters</returns>
public static CodeConstructor Ctor(MemberAttributes attrs=default(MemberAttributes),params CodeParameterDeclarationExpression[]parameters){var result=
new CodeConstructor();result.Attributes=attrs;for(var i=0;i<parameters.Length;i++)result.Parameters.Add(parameters[i]);return result;}/// <summary>
/// Gets an item by name from the specified collection
/// </summary>
/// <param name="name">The name of the item</param>
/// <param name="items">The collection</param>
/// <returns>The first item that could be found, otherwise null</returns>
public static CodeTypeMember GetByName(string name,CodeTypeMemberCollection items){for(int ic=items.Count,i=0;i<ic;++i){var item=items[i];if(0==string.Compare(item.Name,
name,StringComparison.InvariantCulture))return item;}return null;}/// <summary>
/// Gets an item by name from the specified collection
/// </summary>
/// <param name="name">The name of the item</param>
/// <param name="items">The collection</param>
/// <returns>The first item that could be found, otherwise null</returns>
public static CodeAttributeDeclaration GetByName(string name,CodeAttributeDeclarationCollection items){for(int ic=items.Count,i=0;i<ic;++i){var item=items[i];
if(0==string.Compare(item.Name,name,StringComparison.InvariantCulture))return item;}return null;}/// <summary>
/// Gets an item by name from the specified collection
/// </summary>
/// <param name="name">The name of the item</param>
/// <param name="items">The collection</param>
/// <returns>The first item that could be found, otherwise null</returns>
public static CodeNamespace GetByName(string name,CodeNamespaceCollection items){for(int ic=items.Count,i=0;i<ic;++i){var item=items[i];if(0==string.Compare(item.Name,
name,StringComparison.InvariantCulture))return item;}return null;}/// <summary>
/// Gets an item by name from the specified collection
/// </summary>
/// <param name="name">The name of the item</param>
/// <param name="items">The collection</param>
/// <returns>The first item that could be found, otherwise null</returns>
public static CodeParameterDeclarationExpression GetByName(string name,CodeParameterDeclarationExpressionCollection items){for(int ic=items.Count,i=0;
i<ic;++i){var item=items[i];if(0==string.Compare(item.Name,name,StringComparison.InvariantCulture))return item;}return null;}/// <summary>
/// Gets an item by name from the specified collection
/// </summary>
/// <param name="name">The name of the item</param>
/// <param name="items">The collection</param>
/// <returns>The first item that could be found, otherwise null</returns>
public static CodeTypeDeclaration GetByName(string name,CodeTypeDeclarationCollection items){for(int ic=items.Count,i=0;i<ic;++i){var item=items[i];if
(0==string.Compare(item.Name,name,StringComparison.InvariantCulture))return item;}return null;}/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="expr">The expression to render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeExpression expr,string lang="cs"){var sb=new StringBuilder();var sw=new StringWriter(sb);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CodeGeneratorOptions();opts.BlankLinesBetweenMembers=false;prov.GenerateCodeFromExpression(expr,sw,opts);sw.Flush();return sb.ToString();
}/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="stmt">The statement to render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeStatement stmt,string lang="cs"){var sb=new StringBuilder();var sw=new StringWriter(sb);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CodeGeneratorOptions();opts.BlankLinesBetweenMembers=false;prov.GenerateCodeFromStatement(stmt,sw,opts);sw.Flush();return sb.ToString();}
/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="type">The type to render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeTypeReference type,string lang="cs"){return ToString(TypeRef(type),lang);}/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="type">The type declaration to render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeTypeDeclaration type,string lang="cs"){var sb=new StringBuilder();var sw=new StringWriter(sb);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CodeGeneratorOptions();opts.BlankLinesBetweenMembers=false;prov.GenerateCodeFromType(type,sw,opts);sw.Flush();return sb.ToString();}/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="namespace">The namespace to render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeNamespace@namespace,string lang="cs"){var sb=new StringBuilder();var sw=new StringWriter(sb);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CodeGeneratorOptions();opts.BlankLinesBetweenMembers=false;prov.GenerateCodeFromNamespace(@namespace,sw,opts);sw.Flush();return sb.ToString();
}/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="compileUnit">The compile unit to render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeCompileUnit compileUnit,string lang="cs"){var sb=new StringBuilder();var sw=new StringWriter(sb);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CodeGeneratorOptions();opts.BlankLinesBetweenMembers=false;prov.GenerateCodeFromCompileUnit(compileUnit,sw,opts);sw.Flush();return sb.ToString();
}/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="member">The type member render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeTypeMember member,string lang="cs"){var sb=new StringBuilder();var sw=new StringWriter(sb);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CodeGeneratorOptions();opts.BlankLinesBetweenMembers=false;prov.GenerateCodeFromMember(member,sw,opts);sw.Flush();return sb.ToString();}/// <summary>
/// Renders code to a string in the optionally specified language
/// </summary>
/// <param name="code">The <see cref="CodeObject"/> to render</param>
/// <param name="lang">The language - defaults to C#</param>
/// <returns>A string of code</returns>
public static string ToString(CodeObject code,string lang="cs"){if(null==code)throw new ArgumentNullException(nameof(code));var cc=code as CodeComment;
if(null!=cc)return ToString(new CodeCommentStatement(cc),lang);var ccu=code as CodeCompileUnit;if(null!=ccu)return ToString(ccu,lang);var ce=code as CodeExpression;
if(null!=ce)return ToString(ce,lang);var cns=code as CodeNamespace;if(null!=cns){ccu=new CodeCompileUnit();ccu.Namespaces.Add(cns);return ToString(ccu,
lang);}var cs=code as CodeStatement;if(null!=cs)return ToString(cs,lang);var ctm=code as CodeTypeMember;if(null!=ctm)return ToString(ctm,lang);var ctp
=code as CodeTypeParameter;if(null!=ctp)return ToString(new CodeTypeReference(ctp),lang);var ctr=code as CodeTypeReference;if(null!=ctr)return ToString(ctr,
lang);throw new NotSupportedException("The specified code object cannot be rendered to code directly. It must be rendered as part of a larger graph.");
}static CodeExpression _MakeBinOps(System.Collections.IEnumerable exprs,CodeBinaryOperatorType type){var result=new CodeBinaryOperatorExpression();foreach
(CodeExpression expr in exprs){result.Operator=type;if(null==result.Left){result.Left=expr;continue;}if(null==result.Right){result.Right=expr;continue;
}result=new CodeBinaryOperatorExpression(result,type,expr);}if(null==result.Right)return result.Left;return result;}
#region Type serialization
static CodeExpression _SerializeArray(Array arr,TypeConverter typeConv){if(1==arr.Rank&&0==arr.GetLowerBound(0)){var result=new CodeArrayCreateExpression(arr.GetType());
foreach(var elem in arr)result.Initializers.Add(_Serialize(elem,typeConv));return result;}throw new NotSupportedException("Only SZArrays can be serialized to code.");
}static CodeExpression _SerializeEnum(Enum value,TypeConverter converter){var t=value.GetType();var sa=value.ToString("F").Split(',');double d;if(!double.TryParse(sa[0],
out d)){var exprs=new CodeExpressionCollection();for(var i=0;i<sa.Length;i++){var s=sa[i];exprs.Add(FieldRef(TypeRef(t),s));}switch(exprs.Count){case 1:
return exprs[0];default:return BinOp(exprs,CodeBinaryOperatorType.BitwiseOr);}}else return Cast(t,Literal(Convert.ChangeType(value,System.Enum.GetUnderlyingType(t)),
converter));}static CodeExpression _Serialize(object val,TypeConverter typeConv){if(null==val)return new CodePrimitiveExpression(null); var tt=val as Type;
if(null!=tt)return new CodeTypeOfExpression(tt);if(val is char){ if(((char)val)>0x7E)return new CodeCastExpression(typeof(char),new CodePrimitiveExpression((int)(char)val));
return new CodePrimitiveExpression((char)val);}else if(val is bool||val is string||val is short||val is ushort||val is int||val is uint||val is ulong||
val is long||val is byte||val is sbyte||val is float||val is double||val is decimal){ return new CodePrimitiveExpression(val);}if(val is System.Enum){
return _SerializeEnum((Enum)val,typeConv);}if(val is Array&&1==((Array)val).Rank&&0==((Array)val).GetLowerBound(0)){return _SerializeArray((Array)val,typeConv);
}var conv=(null==typeConv)?TypeDescriptor.GetConverter(val):typeConv;if(null!=conv){if(conv.CanConvertTo(typeof(InstanceDescriptor))){var desc=conv.ConvertTo(val,
typeof(InstanceDescriptor))as InstanceDescriptor;if(!desc.IsComplete)throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.",
val.GetType().FullName));var ctor=desc.MemberInfo as ConstructorInfo;if(null!=ctor){var result=new CodeObjectCreateExpression(ctor.DeclaringType);foreach
(var arg in desc.Arguments)result.Parameters.Add(_Serialize(arg,typeConv));return result;}var meth=desc.MemberInfo as MethodInfo;if(null!=meth&&(MethodAttributes.Static
==(meth.Attributes&MethodAttributes.Static))){var result=new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(meth.DeclaringType),
meth.Name));foreach(var arg in desc.Arguments)result.Parameters.Add(_Serialize(arg,typeConv));return result;}var fld=desc.MemberInfo as FieldInfo;if(null
!=fld&&((FieldAttributes.Static==(fld.Attributes&FieldAttributes.Static))||(FieldAttributes.Literal==(fld.Attributes&FieldAttributes.Literal)))){var result
=new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(fld.DeclaringType),fld.Name);return result;}throw new NotSupportedException(string.Format(
"The instance descriptor for type \"{0}\" is not supported.",val.GetType().FullName));}else{ var t=val.GetType();if(t.IsGenericType&&t.GetGenericTypeDefinition()
==typeof(KeyValuePair<,>)){ var kvpType=new CodeTypeReference(typeof(KeyValuePair<,>));foreach(var arg in val.GetType().GetGenericArguments())kvpType.TypeArguments.Add(arg);
var result=new CodeObjectCreateExpression(kvpType);for(int ic=kvpType.TypeArguments.Count,i=0;i<ic;++i){var prop=val.GetType().GetProperty(0==i?"Key":
"Value");result.Parameters.Add(_Serialize(prop.GetValue(val),typeConv));}return result;}throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.",
val.GetType().FullName));}}else throw new NotSupportedException(string.Format("The type \"{0}\" could not be serialized.",val.GetType().FullName));}
#endregion
/// <summary>
/// Returns a collection of comments, one for each line of text
/// </summary>
/// <param name="text">The comment text</param>
/// <param name="docComment">True if these should be rendered as a doc comment, otherwise false</param>
/// <returns></returns>
public static CodeCommentStatementCollection ToComments(string text,bool docComment=false){var result=new CodeCommentStatementCollection();var sr=new StringReader(text);
string line;while(null!=(line=sr.ReadLine()))result.Add(new CodeCommentStatement(line,docComment));return result;}}}namespace CD{/// <summary>
/// Traces variable declarations throughout a method
/// </summary>
public static class CodeDomVariableTracer{static readonly object _catchClauseKey=new object();/// <summary>
/// Indicates whether this variable declaration actually represents a catch clause
/// </summary>
/// <param name="vds">The variable declaration statement to test</param>
/// <returns>True if this variable declaration entry was manufactured as part of catch clause, otherwise false</returns>
public static bool IsCatchClause(CodeVariableDeclarationStatement vds){return vds.UserData.Contains(_catchClauseKey);}/// <summary>
/// Traces a member to a particular statement, returning all variables in the scope of that statement.
/// </summary>
/// <param name="member">The member to trace</param>
/// <param name="target">The target to look for</param>
/// <returns>A list of variable declarations representing the variables that are in scope of <paramref name="target"/></returns>
public static IList<CodeVariableDeclarationStatement>Trace(CodeTypeMember member,CodeStatement target){bool found;var result=_TraceMember(member,target,
out found);if(!found)throw new ArgumentException("The member did not contain the specified target statement",nameof(target));return result;}/// <summary>
/// Traces a containing statement to a particular statement within it, returning all variables in the scope of that statement.
/// </summary>
/// <param name="from">The statment to trace</param>
/// <param name="target">The target to look for within that statement</param>
/// <returns>A list of variable declarations representing the variables that are in scope of <paramref name="target"/></returns>
public static IList<CodeVariableDeclarationStatement>Trace(CodeStatement from,CodeStatement target){bool found;var result=_TraceStatement(from,target,
out found);if(!found)throw new ArgumentException("The statement did not contain the specified target statement",nameof(target));return result;}static IList<CodeVariableDeclarationStatement>
_TraceMember(CodeTypeMember m,CodeStatement t,out bool found){var result=new List<CodeVariableDeclarationStatement>();var cmm=m as CodeMemberMethod;if
(null!=cmm){foreach(CodeStatement tt in cmm.Statements){Debug.WriteLine(CodeDomUtility.ToString(tt));var r=_TraceStatement(tt,t,out found);result.AddRange(r);
if(found){return result;}}found=false;return new CodeVariableDeclarationStatement[0];}var cmp=m as CodeMemberProperty;if(null!=cmp){foreach(CodeStatement
 tt in cmp.GetStatements){var r=_TraceStatement(tt,t,out found);result.AddRange(r);if(found){return result;}}}result.Clear(); foreach(CodeStatement tt
 in cmp.SetStatements){var r=_TraceStatement(tt,t,out found);result.AddRange(r);if(found){return result;}}found=false;return new CodeVariableDeclarationStatement[0];
}static IList<CodeVariableDeclarationStatement>_TraceStatement(CodeStatement obj,CodeStatement target,out bool found){var ca=obj as CodeAssignStatement;
if(null!=ca){return _TraceAssignStatement(ca,target,out found);}var cae=obj as CodeAttachEventStatement;if(null!=cae){return _TraceAttachEventStatement(cae,
target,out found);}var cc=obj as CodeCommentStatement;if(null!=cc){return _TraceCommentStatement(cc,target,out found);}var ccnd=obj as CodeConditionStatement;
if(null!=ccnd){return _TraceConditionStatement(ccnd,target,out found);}var ce=obj as CodeExpressionStatement;if(null!=ce){return _TraceExpressionStatement(ce,
target,out found);}var cg=obj as CodeGotoStatement;if(null!=cg){return _TraceGotoStatement(cg,target,out found);}var ci=obj as CodeIterationStatement;
if(null!=ci){return _TraceIterationStatement(ci,target,out found);}var cl=obj as CodeLabeledStatement;if(null!=cl){return _TraceLabeledStatement(cl,target,
out found);}var cm=obj as CodeMethodReturnStatement;if(null!=cm){return _TraceMethodReturnStatement(cm,target,out found);}var cre=obj as CodeRemoveEventStatement;
if(null!=cre){return _TraceRemoveEventStatement(cre,target,out found);}var cs=obj as CodeSnippetStatement;if(null!=cs){return _TraceSnippetStatement(cs,
target,out found);}var cte=obj as CodeThrowExceptionStatement;if(null!=cte){return _TraceThrowExceptionStatement(cte,target,out found);}var ctcf=obj as
 CodeTryCatchFinallyStatement;if(null!=ctcf){return _TraceTryCatchFinallyStatement(ctcf,target,out found);}var cvd=obj as CodeVariableDeclarationStatement;
if(null!=cvd){var res=new List<CodeVariableDeclarationStatement>(_TraceVariableDeclarationStatement(cvd,target,out found));res.Add(cvd);return res;}throw
 new NotSupportedException("The graph contains an unsupported statement");}private static IList<CodeVariableDeclarationStatement>_TraceVariableDeclarationStatement(CodeVariableDeclarationStatement
 s,CodeStatement t,out bool found){found=false;if(s==t){found=true;}return new CodeVariableDeclarationStatement[]{};}private static IList<CodeVariableDeclarationStatement>
_TraceTryCatchFinallyStatement(CodeTryCatchFinallyStatement s,CodeStatement t,out bool found){found=true;if(s==t)return new CodeVariableDeclarationStatement[0];
found=false;var result=new List<CodeVariableDeclarationStatement>();foreach(CodeStatement tt in s.TryStatements){var r=_TraceStatement(tt,t,out found);
result.AddRange(r);if(found){return result;}}result.Clear(); foreach(CodeCatchClause cc in s.CatchClauses){ var res2=new List<CodeVariableDeclarationStatement>();
if(null!=cc.CatchExceptionType){ var vdc=new CodeVariableDeclarationStatement(cc.CatchExceptionType,cc.LocalName); vdc.UserData.Add(_catchClauseKey,cc);
res2.Add(vdc);}foreach(CodeStatement tt in cc.Statements){var r=_TraceStatement(tt,t,out found);res2.AddRange(r);if(found){result.AddRange(res2);return
 result;}}}result.Clear(); foreach(CodeStatement tt in s.FinallyStatements){var r=_TraceStatement(tt,t,out found);result.AddRange(r);if(found){return result;
}}return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceThrowExceptionStatement(CodeThrowExceptionStatement
 s,CodeStatement t,out bool found){found=t==s;return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceSnippetStatement(CodeSnippetStatement
 s,CodeStatement t,out bool found){found=t==s;return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceRemoveEventStatement(CodeRemoveEventStatement
 s,CodeStatement t,out bool found){found=t==s;return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceMethodReturnStatement(CodeMethodReturnStatement
 s,CodeStatement t,out bool found){found=t==s;return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceLabeledStatement(CodeLabeledStatement
 s,CodeStatement t,out bool found){found=false;if(s==t){found=true;return new CodeVariableDeclarationStatement[0];}if(null!=s.Statement)return _TraceStatement(s.Statement,
t,out found);return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceIterationStatement(CodeIterationStatement
 s,CodeStatement t,out bool found){var result=new List<CodeVariableDeclarationStatement>();var r=_TraceStatement(s.InitStatement,t,out found);result.AddRange(r);
 if(found){return result;}if(s==t){found=true;return result;}foreach(CodeStatement tt in s.Statements){r=_TraceStatement(tt,t,out found);result.AddRange(r);
if(found){return result;}}r=_TraceStatement(s.IncrementStatement,t,out found);result.AddRange(r);if(found){return result;}return new CodeVariableDeclarationStatement[0];
}private static IList<CodeVariableDeclarationStatement>_TraceGotoStatement(CodeGotoStatement s,CodeStatement t,out bool found){found=t==s;return new CodeVariableDeclarationStatement[0];
}private static IList<CodeVariableDeclarationStatement>_TraceExpressionStatement(CodeExpressionStatement s,CodeStatement t,out bool found){found=t==s;
return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceConditionStatement(CodeConditionStatement s,
CodeStatement t,out bool found){if(s==t){found=true;return new CodeVariableDeclarationStatement[0];}var result=new List<CodeVariableDeclarationStatement>();
foreach(CodeStatement tt in s.TrueStatements){var r=_TraceStatement(tt,t,out found);result.AddRange(r);if(found){return result;}}result.Clear();foreach
(CodeStatement tt in s.FalseStatements){var r=_TraceStatement(tt,t,out found);result.AddRange(r);if(found){return result;}}found=false;return new CodeVariableDeclarationStatement[0];
}private static IList<CodeVariableDeclarationStatement>_TraceCommentStatement(CodeCommentStatement s,CodeStatement t,out bool found){found=t==s;return
 new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceAttachEventStatement(CodeAttachEventStatement s,
CodeStatement t,out bool found){found=t==s;return new CodeVariableDeclarationStatement[0];}private static IList<CodeVariableDeclarationStatement>_TraceAssignStatement(CodeAssignStatement
 s,CodeStatement t,out bool found){found=t==s;return new CodeVariableDeclarationStatement[0];}}}namespace CD{/// <summary>
/// Performed each time the visitor visits an element
/// </summary>
/// <param name="args">A <see cref="CodeDomVisitContext"/> object describing the current target and context</param>
#if GOKITLIB
public
#endif
delegate void CodeDomVisitAction(CodeDomVisitContext args);/// <summary>
/// Indicates the targets for the visit operation
/// </summary>
[Flags]
#if GOKITLIB
public
#endif
enum CodeDomVisitTargets{/// <summary>
/// <see cref="CodeAttributeDeclaration"/> objects should be visited
/// </summary>
Attributes=0x01,/// <summary>
/// <see cref="CodeTypeMember"/> objects should be visited
/// </summary>
Members=0x02,/// <summary>
/// <see cref="CodeStatement"/> and <see cref="CodeCatchClause"/> objects should be visited
/// </summary>
Statements=0x04,/// <summary>
/// <see cref="CodeExpression"/> objects should be visited
/// </summary>
Expressions=0x08,/// <summary>
/// <see cref="CodeTypeDeclaration"/> objects should be visited
/// </summary>
Types=0x10,/// <summary>
/// <see cref="CodeTypeReference"/> and <see cref="CodeTypeParameter"/> objects should be visited
/// </summary>
TypeRefs=0x20,/// <summary>
/// <see cref="CodeComment"/> objects should be visited
/// </summary>
Comments=0x40,/// <summary>
/// <see cref="CodeDirective"/> and <see cref="CodeLinePragma"/> objects should be visited
/// </summary>
Directives=0x80,/// <summary>
/// Indicates that only entries that have been Mark()ed are visited
/// </summary>
Marked=0x100,/// <summary>
/// All objects should be visited
/// </summary>
All=Attributes|Members|Statements|Expressions|Types|TypeRefs|Comments|Directives}/// <summary>
/// Represents the current context of the visit operatoion
/// </summary>
#if GOKITLIB
public
#endif
class CodeDomVisitContext{/// <summary>
/// Indicates root where the visit operation started
/// </summary>
public object Root;/// <summary>
/// Indicates the parent of the current target
/// </summary>
public object Parent;/// <summary>
/// The name of the parent member retrieved to navigate to the target.
/// </summary>
public string Member;/// <summary>
/// Indicates the index of the target in the parent's collection, or -1 if not in a collection
/// </summary>
public int Index;/// <summary>
/// Indicates the path to the object from the root, in C# format
/// </summary>
public string Path;/// <summary>
/// Indicates the target of the visit operation
/// </summary>
public object Target;/// <summary>
/// A <see cref="CodeDomVisitTargets"/> flag set that tells the visitor which objects should be visited
/// </summary>
public CodeDomVisitTargets Targets;/// <summary>
/// True if the visitation should immediately be canceled. No more notifications will occur.
/// </summary>
public bool Cancel;internal CodeDomVisitContext(){}internal CodeDomVisitContext Set(object root,object parent,string member,int index,string path,object
 target,CodeDomVisitTargets targets){Root=root;Parent=parent;Member=member;Index=index;Path=path;Target=target;Targets=targets;return this;}}/// <summary>
/// Visits a CodeDOM abstract syntax tree, performing the requested action at each visit.
/// </summary>
#if GOKITLIB
public
#endif
class CodeDomVisitor{/// <summary>
/// Begins a visit operation
/// </summary>
/// <param name="obj">The code dom object to visit</param>
/// <param name="action">A <see cref="CodeDomVisitAction"/> that indicates the action to perform</param>
/// <param name="targets">A <see cref="CodeDomVisitTargets"/> flag set that indicates which objects to visit</param>
public static void Visit(object obj,CodeDomVisitAction action,CodeDomVisitTargets targets){var args=new CodeDomVisitContext();args=args.Set(obj,null,null,-1,"",
obj,targets);var cc=obj as CodeComment;if(null!=cc){_VisitComment(cc,args,action);return;}var ccu=obj as CodeCompileUnit;if(null!=ccu){_VisitCompileUnit(ccu,args,
action);return;}var cd=obj as CodeDirective;if(null!=ccu){_VisitDirective(cd,args,action);return;}var ce=obj as CodeExpression;if(null!=ce){_VisitExpression(ce,
args,action);return;}var cns=obj as CodeNamespace;if(null!=cns){_VisitNamespace(cns,args,action);return;}var cni=obj as CodeNamespaceImport;if(null!=cni)
{_VisitNamespaceImport(cni,args,action);return;}var cs=obj as CodeStatement;if(null!=cs){_VisitStatement(cs,args,action);return;}var ctm=obj as CodeTypeMember;
if(null!=ctm){_VisitTypeMember(ctm,args,action);return;}var ctp=obj as CodeTypeParameter;if(null!=ctp){_VisitTypeParameter(ctp,args,action);return;}var
 ctr=obj as CodeTypeReference;if(null!=ctr){_VisitTypeReference(ctr,args,action);return;}var cad=obj as CodeAttributeDeclaration;if(null!=cad){_VisitAttributeDeclaration(cad,
args,action);return;}var ccc=obj as CodeCatchClause;if(null!=ccc){_VisitCatchClause(ccc,args,action);return;}var clp=obj as CodeLinePragma;if(null!=clp)
{_VisitLinePragma(clp,args,action);return;}}/// <summary>
/// Begins a visit operation
/// </summary>
/// <param name="obj">The code dom object to visit</param>
/// <param name="action">A <see cref="CodeDomVisitAction"/> that indicates the action to perform</param>
public static void Visit(object obj,CodeDomVisitAction action){Visit(obj,action,CodeDomVisitTargets.All);}/// <summary>
/// Marks an object for visitation
/// </summary>
/// <remarks>When the Marked visit target flag is specified, the visitor will only visit marked nodes</remarks>
/// <param name="obj">The object to mark</param>
/// <returns>Returns <paramref name="obj"/></returns>
public static T Mark<T>(T obj)where T:CodeObject{obj.UserData.Add("codedomgokit:visit",true);return obj;}/// <summary>
/// Unmarks an object for visitation
/// </summary>
/// <remarks>When the Marked visit target flag is specified, the visitor will only visit marked nodes</remarks>
/// <param name="obj">The object to mark</param>
/// <returns>Returns <paramref name="obj"/></returns>
public static T Unmark<T>(T obj)where T:CodeObject{obj.UserData.Remove("codedomgokit:visit");return obj;}/// <summary>
/// A helper method to replace the current target with a new value during the visit operation
/// </summary>
/// <param name="ctx">The visit context</param>
/// <param name="newTarget">The target value to replace the current target with</param>
/// <remarks>This method is intended to be called from inside the anonymous visit method. This method uses reflection.</remarks>
public static void ReplaceTarget(CodeDomVisitContext ctx,object newTarget){try{var ma=ctx.Parent.GetType().GetMember(ctx.Member,BindingFlags.Public|BindingFlags.Instance
|BindingFlags.SetProperty|BindingFlags.GetProperty);var pi=ma[0]as PropertyInfo;if(-1!=ctx.Index){var l=pi.GetValue(ctx.Parent)as System.Collections.IList;
l[ctx.Index]=newTarget;return;}pi.SetValue(ctx.Parent,newTarget);}catch(TargetInvocationException tex){throw tex.InnerException;}}static bool _CanVisit(CodeObject
 obj,CodeDomVisitContext args){if(!_HasTarget(args,CodeDomVisitTargets.Marked))return true;return obj.UserData.Contains("codedomgokit:visit");}static bool
 _HasTarget(CodeDomVisitContext args,CodeDomVisitTargets target){return target==(args.Targets&target);}static void _VisitLinePragma(CodeLinePragma obj,
CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Directives))return;if(args.Cancel)return; action(args);}static
 void _VisitComment(CodeComment obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Comments)||!_CanVisit(obj,args))
return;if(args.Cancel)return; action(args);}static void _VisitDirective(CodeDirective obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel
||!_CanVisit(obj,args))return;var rd=obj as CodeRegionDirective;if(null!=rd){_VisitRegionDirective(rd,args,action);return;}var cp=obj as CodeChecksumPragma;
if(null!=cp){_VisitChecksumPragma(cp,args,action);return;}throw new NotSupportedException("Unsupported directive type in graph");}static void _VisitRegionDirective(CodeRegionDirective
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Directives)||!_CanVisit(obj,args))return;if(args.Cancel)
return;action(args);}static void _VisitChecksumPragma(CodeChecksumPragma obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Directives)
||!_CanVisit(obj,args))return;if(args.Cancel)return;action(args);}static void _VisitNamespaceImport(CodeNamespaceImport obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(args.Cancel||!_CanVisit(obj,args))return; action(args);}static void _VisitAttributeDeclaration(CodeAttributeDeclaration obj,CodeDomVisitContext
 args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Attributes))return;if(args.Cancel)return; action(args);if(null!=obj.AttributeType
&&_CanVisit(obj.AttributeType,args))_VisitTypeReference(obj.AttributeType,args.Set(args.Root,obj,"AttributeType",-1,_BuildPath(args.Path,"AttributeType",-1),
obj.AttributeType,args.Targets),action);if(args.Cancel)return;for(int ic=obj.Arguments.Count,i=0;i<ic;++i){var arg=obj.Arguments[i];_VisitAttributeArgument(arg,
args.Set(args.Root,obj,"Arguments",i,_BuildPath(args.Path,"Arguments",i),arg,args.Targets),action);if(args.Cancel)return;}}static void _VisitAttributeArgument(CodeAttributeArgument
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Attributes))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.Value&&_CanVisit(obj.Value,args))_VisitExpression(obj.Value,args.Set(args.Root,obj,"Value",-1,_BuildPath(args.Path,"Value",-1),
obj.Value,args.Targets),action);}static void _VisitCompileUnit(CodeCompileUnit obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel||
!_CanVisit(obj,args))return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i)
{var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.AssemblyCustomAttributes.Count,i=
0;i<ic;++i){var attrDecl=obj.AssemblyCustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"AssemblyCustomAttributes",i,_BuildPath(args.Path,"AssemblyCustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}for(int ic=obj.Namespaces.Count,i=0;i<ic;++i){var ns=obj.Namespaces[i];if(_CanVisit(ns,args))_VisitNamespace(ns,
args.Set(args.Root,obj,"Namespaces",i,_BuildPath(args.Path,"Namespaces",i),ns,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitNamespace(CodeNamespace obj,CodeDomVisitContext args,CodeDomVisitAction action){if
(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){
var cc=obj.Comments[i];if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),
action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Types)){for(int ic=obj.Types.Count,i=0;i<ic;++i){var decl=obj.Types[i];if(_CanVisit(decl,args))
_VisitTypeDeclaration(decl,args.Set(args.Root,obj,"Types",i,_BuildPath(args.Path,"Types",i),decl,args.Targets),action);if(args.Cancel)return;}}}static
 void _VisitTypeDeclaration(CodeTypeDeclaration obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Types)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",i),
obj.LinePragma,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);if(args.Cancel)
return;}}if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];
_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);
if(args.Cancel)return;}}for(int ic=obj.TypeParameters.Count,i=0;i<ic;++i){var ctp=obj.TypeParameters[i];if(_CanVisit(ctp,args))_VisitTypeParameter(ctp,
args.Set(args.Root,obj,"TypeParameters",i,_BuildPath(args.Path,"TypeParameters",i),ctp,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,
CodeDomVisitTargets.TypeRefs)){for(int ic=obj.BaseTypes.Count,i=0;i<ic;++i){var ctr=obj.BaseTypes[i];if(_CanVisit(ctr,args))_VisitTypeReference(ctr,args.Set(args.Root,
obj,"BaseTypes",i,_BuildPath(args.Path,"BaseTypes",i),ctr,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Members))
{for(int ic=obj.Members.Count,i=0;i<ic;++i){var ctm=obj.Members[i];if(_CanVisit(ctm,args))_VisitTypeMember(ctm,args.Set(args.Root,obj,"Members",i,_BuildPath(args.Path,"Members",i),
ctm,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i)
{var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),
action);if(args.Cancel)return;}}}static void _VisitTypeMember(CodeTypeMember obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel||!_CanVisit(obj,args))
return;var ce=obj as CodeMemberEvent;if(null!=ce){_VisitMemberEvent(ce,args,action);return;}var cf=obj as CodeMemberField;if(null!=cf){_VisitMemberField(cf,
args,action);return;}var cm=obj as CodeMemberMethod;if(null!=cm){_VisitMemberMethod(cm,args,action);return;}var cp=obj as CodeMemberProperty;if(null!=
cp){_VisitMemberProperty(cp,args,action);return;}var cstm=obj as CodeSnippetTypeMember;if(null!=cstm){_VisitSnippetTypeMember(cstm,args,action);return;
}var ctd=obj as CodeTypeDeclaration;if(null!=ctd){_VisitTypeDeclaration(ctd,args,action);return;}throw new NotSupportedException("The graph contains an unsupported type declaration");
}static void _VisitMemberEvent(CodeMemberEvent obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.Type
&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Type,args))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),
obj.Type,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=obj.ImplementationTypes[i];
if(_CanVisit(ctr,args))_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),ctr,args.Targets),
action);if(args.Cancel)return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.PrivateImplementationType,args))
_VisitTypeReference(obj.PrivateImplementationType,args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),
obj.PrivateImplementationType,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i)
{var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitSnippetTypeMember(CodeSnippetTypeMember obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Members)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitMemberField(CodeMemberField
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=
obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Type,args))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),
action);if(null!=obj.InitExpression&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.InitExpression,args))_VisitExpression(obj.InitExpression,
args.Set(args.Root,obj,"InitExpression",-1,_BuildPath(args.Path,"InitExpression",-1),obj.InitExpression,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitMemberMethod(CodeMemberMethod obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Members)||!_CanVisit(obj,args))return;if(args.Cancel)return;var ctor=obj as CodeConstructor;if(null!=ctor){_VisitConstructor(ctor,
args,action);return;}var entryPoint=obj as CodeEntryPointMethod;if(null!=entryPoint){_VisitEntryPointMethod(entryPoint,args,action);return;} action(args);
if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];if(_CanVisit(cc,args))_VisitCommentStatement(cc,
args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes)){
for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.ReturnType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.ReturnType,args))
_VisitTypeReference(obj.ReturnType,args.Set(args.Root,obj,"ReturnType",-1,_BuildPath(args.Path,"ReturnType",-1),obj.ReturnType,args.Targets),action);if
(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=obj.ImplementationTypes[i];if(_CanVisit(ctr,args))
_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),ctr,args.Targets),action);if(args.Cancel)
return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.PrivateImplementationType,args))_VisitTypeReference(obj.PrivateImplementationType,
args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),obj.PrivateImplementationType,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var pd=obj.Parameters[i];if(_CanVisit(pd,args))
_VisitParameterDeclarationExpression(pd,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),pd,args.Targets),action);if(args.Cancel)
return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt=obj.Statements[i];if(_CanVisit(stmt,args))
_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitEntryPointMethod(CodeEntryPointMethod
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=
obj.ReturnType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.ReturnType,args))_VisitTypeReference(obj.ReturnType,args.Set(args.Root,obj,"ReturnType",-1,_BuildPath(args.Path,"ReturnType",-1),
obj.ReturnType,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=
obj.ImplementationTypes[i];if(_CanVisit(ctr,args))_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),
ctr,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.PrivateImplementationType,args))
_VisitTypeReference(obj.PrivateImplementationType,args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),
obj.PrivateImplementationType,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i)
{var pd=obj.Parameters[i];if(_CanVisit(pd,args))_VisitParameterDeclarationExpression(pd,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),
pd,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var
 stmt=obj.Statements[i];if(_CanVisit(stmt,args))_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),
action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitConstructor(CodeConstructor obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(!_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=obj.ImplementationTypes[i];if(_CanVisit(ctr,args))_VisitTypeReference(ctr,
args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),ctr,args.Targets),action);if(args.Cancel)return;}}if(null
!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.PrivateImplementationType,args))_VisitTypeReference(obj.PrivateImplementationType,
args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),obj.PrivateImplementationType,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var pd=obj.Parameters[i];if(_CanVisit(pd,args))
_VisitParameterDeclarationExpression(pd,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),pd,args.Targets),action);if(args.Cancel)
return;}}if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.ChainedConstructorArgs.Count,i=0;i<ic;++i){var ce=obj.ChainedConstructorArgs[i];
if(_CanVisit(ce,args))_VisitExpression(ce,args.Set(args.Root,obj,"ChainedConstructorArgs",i,_BuildPath(args.Path,"ChainedConstructorArgs",i),ce,args.Targets),
action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.BaseConstructorArgs.Count,i=0;i<ic;++i){var ce=obj.BaseConstructorArgs[i];
if(_CanVisit(ce,args))_VisitExpression(ce,args.Set(args.Root,obj,"BaseConstructorArgs",i,_BuildPath(args.Path,"BaseConstructorArgs",i),ce,args.Targets),
action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt=obj.Statements[i];
if(_CanVisit(stmt,args))_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),action);if(args.Cancel)
return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitMemberProperty(CodeMemberProperty obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
if(_CanVisit(cc,args))_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=
obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Type,args))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),
obj.Type,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=obj.ImplementationTypes[i];
if(_CanVisit(ctr,args))_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),ctr,args.Targets),
action);if(args.Cancel)return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.PrivateImplementationType,args))
_VisitTypeReference(obj.PrivateImplementationType,args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),
obj.PrivateImplementationType,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i)
{var pd=obj.Parameters[i];if(_CanVisit(pd,args))_VisitParameterDeclarationExpression(pd,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),
pd,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.GetStatements.Count,i=0;i<ic;++i){
var stmt=obj.GetStatements[i];if(_CanVisit(stmt,args))_VisitStatement(stmt,args.Set(args.Root,obj,"GetStatements",i,_BuildPath(args.Path,"GetStatements",i),
stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.SetStatements.Count,i=0;i<ic;++i)
{var stmt=obj.SetStatements[i];if(_CanVisit(stmt,args))_VisitStatement(stmt,args.Set(args.Root,obj,"SetStatements",i,_BuildPath(args.Path,"SetStatements",i),
stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i)
{var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitExpression(CodeExpression obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return;var argr=obj as CodeArgumentReferenceExpression;
if(null!=argr){_VisitArgumentReferenceExpression(argr,args,action);return;}var arrc=obj as CodeArrayCreateExpression;if(null!=arrc){_VisitArrayCreateExpression(arrc,
args,action);return;}var aic=obj as CodeArrayIndexerExpression;if(null!=aic){_VisitArrayIndexerExpression(aic,args,action);return;}var br=obj as CodeBaseReferenceExpression;
if(null!=br){_VisitBaseReferenceExpression(br,args,action);return;}var bo=obj as CodeBinaryOperatorExpression;if(null!=bo){_VisitBinaryOperatorExpression(bo,
args,action);return;}var cc=obj as CodeCastExpression;if(null!=cc){_VisitCastExpression(cc,args,action);return;}var cdv=obj as CodeDefaultValueExpression;
if(null!=cdv){_VisitDefaultValueExpression(cdv,args,action);return;}var cdc=obj as CodeDelegateCreateExpression;if(null!=cdc){_VisitDelegateCreateExpression(cdc,
args,action);return;}var cdi=obj as CodeDelegateInvokeExpression;if(null!=cdi){_VisitDelegateInvokeExpression(cdi,args,action);return;}var cd=obj as CodeDirectionExpression;
if(null!=cd){_VisitDirectionExpression(cd,args,action);return;}var cer=obj as CodeEventReferenceExpression;if(null!=cer){_VisitEventReferenceExpression(cer,
args,action);return;}var cfr=obj as CodeFieldReferenceExpression;if(null!=cfr){_VisitFieldReferenceExpression(cfr,args,action);return;}var ci=obj as CodeIndexerExpression;
if(null!=ci){_VisitIndexerExpression(ci,args,action);return;}var cmi=obj as CodeMethodInvokeExpression;if(null!=cmi){_VisitMethodInvokeExpression(cmi,
args,action);return;}var cmr=obj as CodeMethodReferenceExpression;if(null!=cmr){_VisitMethodReferenceExpression(cmr,args,action);return;}var coc=obj as
 CodeObjectCreateExpression;if(null!=coc){_VisitObjectCreateExpression(coc,args,action);return;}var cpd=obj as CodeParameterDeclarationExpression;if(null!=cpd)
{_VisitParameterDeclarationExpression(cpd,args,action);return;}var cp=obj as CodePrimitiveExpression;if(null!=cp){_VisitPrimitiveExpression(cp,args,action);
return;}var cpr=obj as CodePropertyReferenceExpression;if(null!=cpr){_VisitPropertyReferenceExpression(cpr,args,action);return;}var cpsvr=obj as CodePropertySetValueReferenceExpression;
if(null!=cpsvr){_VisitPropertySetValueReferenceExpression(cpsvr,args,action);return;}var cs=obj as CodeSnippetExpression;if(null!=cs){_VisitSnippetExpression(cs,args,action);
return;}var cthr=obj as CodeThisReferenceExpression;if(null!=cthr){_VisitThisReferenceExpression(cthr,args,action);return;}var cto=obj as CodeTypeOfExpression;
if(null!=cto){_VisitTypeOfExpression(cto,args,action);return;}var ctr=obj as CodeTypeReferenceExpression;if(null!=ctr){_VisitTypeReferenceExpression(ctr,
args,action);return;}var cvr=obj as CodeVariableReferenceExpression;if(null!=cvr){_VisitVariableReferenceExpression(cvr,args,action);return;}throw new
 NotSupportedException("An expression that is not supported was part of the code graph");}static void _VisitVariableReferenceExpression(CodeVariableReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);}static void _VisitTypeOfExpression(CodeTypeOfExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.Type&&_HasTarget(args,
CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Type,args))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),
action);}static void _VisitSnippetExpression(CodeSnippetExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);}static void _VisitParameterDeclarationExpression(CodeParameterDeclarationExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var
 attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Type,args))_VisitTypeReference(obj.Type,
args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);}static void _VisitArgumentReferenceExpression(CodeArgumentReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);}static void _VisitPrimitiveExpression(CodePrimitiveExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);}static void _VisitDirectionExpression(CodeDirectionExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){if(null!=obj.Expression&&_CanVisit(obj.Expression,args))
_VisitExpression(obj.Expression,args.Set(args.Root,obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),obj.Expression,args.Targets),action);}}static
 void _VisitEventReferenceExpression(CodeEventReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)
&&_CanVisit(obj.TargetObject,args))_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),
obj.TargetObject,args.Targets),action);}static void _VisitFieldReferenceExpression(CodeFieldReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if
(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.TargetObject,args))_VisitExpression(obj.TargetObject,args.Set(args.Root,
obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);}static void _VisitPropertyReferenceExpression(CodePropertyReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.TargetObject,args))
_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);
}static void _VisitPropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);}static void _VisitMethodReferenceExpression(CodeMethodReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.TargetObject,args))
_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);
}static void _VisitTypeReferenceExpression(CodeTypeReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)
&&_CanVisit(obj.Type,args))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);
}static void _VisitDelegateCreateExpression(CodeDelegateCreateExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.DelegateType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)
&&_CanVisit(obj.DelegateType,args))_VisitTypeReference(obj.DelegateType,args.Set(args.Root,obj,"DelegateType",-1,_BuildPath(args.Path,"DelegateType",-1),
obj.DelegateType,args.Targets),action);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.TargetObject,args))
_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);
}static void _VisitDelegateInvokeExpression(CodeDelegateInvokeExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)
&&_CanVisit(obj.TargetObject,args))_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),
obj.TargetObject,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i)
{var ce=obj.Parameters[i];if(_CanVisit(ce,args))_VisitExpression(ce,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),ce,args.Targets),
action);if(args.Cancel)return;}}}static void _VisitMethodInvokeExpression(CodeMethodInvokeExpression obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=
obj.Method&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Method,args)){_VisitMethodReferenceExpression(obj.Method,args.Set(args.Root,
obj,"Method",-1,_BuildPath(args.Path,"Method",-1),obj.Method,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.Expressions))
{for(var i=0;i<obj.Parameters.Count;++i){var ce=obj.Parameters[i];if(_CanVisit(ce,args))_VisitExpression(ce,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),ce,
args.Targets),action);if(args.Cancel)return;}}}static void _VisitBinaryOperatorExpression(CodeBinaryOperatorExpression obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if
(null!=obj.Left&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Left,args))_VisitExpression(obj.Left,args.Set(args.Root,obj,"Left",-1,_BuildPath(args.Path,"Left",-1),
obj.Left,args.Targets),action);if(args.Cancel)return;if(null!=obj.Right&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Right,args))_VisitExpression(obj.Right,
args.Set(args.Root,obj,"Right",-1,_BuildPath(args.Path,"Right",-1),obj.Right,args.Targets),action);}static void _VisitCastExpression(CodeCastExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(null!=obj.TargetType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.TargetType,args))_VisitTypeReference(obj.TargetType,
args.Set(args.Root,obj,"TargetType",-1,_BuildPath(args.Path,"TargetType",-1),obj.TargetType,args.Targets),action);if(args.Cancel)return;if(null!=obj.Expression
&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Expression,args))_VisitExpression(obj.Expression,args.Set(args.Root,obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),
obj.Expression,args.Targets),action);}static void _VisitDefaultValueExpression(CodeDefaultValueExpression obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if
(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Type,args))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),
action);}static void _VisitBaseReferenceExpression(CodeBaseReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);}static void _VisitThisReferenceExpression(CodeThisReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);}static void _VisitArrayCreateExpression(CodeArrayCreateExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.CreateType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)
&&_CanVisit(obj.CreateType,args))_VisitTypeReference(obj.CreateType,args.Set(args.Root,obj,"CreateType",-1,_BuildPath(args.Path,"CreateType",-1),obj.CreateType,args.Targets),action);
if(args.Cancel)return;if(null!=obj.SizeExpression&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.SizeExpression,args)){_VisitExpression(obj.SizeExpression,
args.Set(args.Root,obj,"SizeExpression",-1,_BuildPath(args.Path,"SizeExpression",-1),obj.SizeExpression,args.Targets),action);if(args.Cancel)return;}if
(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Initializers.Count,i=0;i<ic;++i){var ce=obj.Initializers[i];if(_CanVisit(ce,args))_VisitExpression(ce,
args.Set(args.Root,obj,"Initializers",i,_BuildPath(args.Path,"Initializers",i),ce,args.Targets),action);if(args.Cancel)return;}}}static void _VisitObjectCreateExpression(CodeObjectCreateExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(null!=obj.CreateType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.CreateType,args))_VisitTypeReference(obj.CreateType,
args.Set(args.Root,obj,"CreateType",-1,_BuildPath(args.Path,"CreateType",-1),obj.CreateType,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,
CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var ce=obj.Parameters[i];if(_CanVisit(ce,args))_VisitExpression(ce,args.Set(args.Root,
obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),ce,args.Targets),action);if(args.Cancel)return;}}}static void _VisitArrayIndexerExpression(CodeArrayIndexerExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.TargetObject,args))
_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Indices.Count,i=0;i<ic;++i){var ce=obj.Indices[i];if(_CanVisit(ce,args))
_VisitExpression(ce,args.Set(args.Root,obj,"Indices",i,_BuildPath(args.Path,"Indices",i),ce,args.Targets),action);if(args.Cancel)return;}}}static void
 _VisitIndexerExpression(CodeIndexerExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions)
&&_CanVisit(obj.TargetObject,args))_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),
obj.TargetObject,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Indices.Count,i=0;i<ic;
++i){var ce=obj.Indices[i];if(_CanVisit(ce,args))_VisitExpression(ce,args.Set(args.Root,obj,"Indices",i,_BuildPath(args.Path,"Indices",i),ce,args.Targets),
action);if(args.Cancel)return;}}}static void _VisitStatement(CodeStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)return;
var ca=obj as CodeAssignStatement;if(null!=ca){_VisitAssignStatement(ca,args,action);return;}var cae=obj as CodeAttachEventStatement;if(null!=cae){_VisitAttachEventStatement(cae,
args,action);return;}var cc=obj as CodeCommentStatement;if(null!=cc){_VisitCommentStatement(cc,args,action);return;}var ccnd=obj as CodeConditionStatement;
if(null!=ccnd){_VisitConditionStatement(ccnd,args,action);return;}var ce=obj as CodeExpressionStatement;if(null!=ce){_VisitExpressionStatement(ce,args,
action);return;}var cg=obj as CodeGotoStatement;if(null!=cg){_VisitGotoStatement(cg,args,action);return;}var ci=obj as CodeIterationStatement;if(null!=
ci){_VisitIterationStatement(ci,args,action);return;}var cl=obj as CodeLabeledStatement;if(null!=cl){_VisitLabeledStatement(cl,args,action);return;}var
 cm=obj as CodeMethodReturnStatement;if(null!=cm){_VisitMethodReturnStatement(cm,args,action);return;}var cre=obj as CodeRemoveEventStatement;if(null!=cre)
{_VisitRemoveEventStatement(cre,args,action);return;}var cs=obj as CodeSnippetStatement;if(null!=cs){_VisitSnippetStatement(cs,args,action);return;}var
 cte=obj as CodeThrowExceptionStatement;if(null!=cte){_VisitThrowExceptionStatement(cte,args,action);return;}var ctcf=obj as CodeTryCatchFinallyStatement;
if(null!=ctcf){_VisitTryCatchFinallyStatement(ctcf,args,action);return;}var cvd=obj as CodeVariableDeclarationStatement;if(null!=cvd){_VisitVariableDeclarationStatement(cvd,
args,action);return;}throw new NotSupportedException("The graph contains an unsupported statement");}static void _VisitCatchClause(CodeCatchClause obj,CodeDomVisitContext
 args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;
if(null!=obj.CatchExceptionType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.CatchExceptionType,args))_VisitTypeReference(obj.CatchExceptionType,args.Set(args.Root,obj,"CatchExceptionType",-1,_BuildPath(args.Path,"CatchExceptionType",-1),obj.CatchExceptionType,args.Targets),action);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt=obj.Statements[i];if(_CanVisit(stmt,args))
_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),action);if(args.Cancel)return;}}}static
 void _VisitTryCatchFinallyStatement(CodeTryCatchFinallyStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic
=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.TryStatements.Count,i=0;i<ic;++i){var stmt=obj.TryStatements[i];
if(_CanVisit(stmt,args))_VisitStatement(stmt,args.Set(args.Root,obj,"TryStatements",i,_BuildPath(args.Path,"TryStatements",i),stmt,args.Targets),action);
if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.CatchClauses.Count,i=0;i<ic;++i){var cl=obj.CatchClauses[i];
_VisitCatchClause(cl,args.Set(args.Root,obj,"CatchClauses",-1,_BuildPath(args.Path,"CatchClauses",-1),cl,args.Targets),action);if(args.Cancel)return;}
}for(int ic=obj.FinallyStatements.Count,i=0;i<ic;++i){var stmt=obj.FinallyStatements[i];if(_CanVisit(stmt,args))_VisitStatement(stmt,args.Set(args.Root,
obj,"FinallyStatements",i,_BuildPath(args.Path,"FinallyStatements",i),stmt,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",-1,_BuildPath(args.Path,"EndDirectives",-1),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitRemoveEventStatement(CodeRemoveEventStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if
(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(null!=obj.Event&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Event,args))_VisitEventReferenceExpression(obj.Event,args.Set(args.Root,
obj,"Event",-1,_BuildPath(args.Path,"Event",-1),obj.Event,args.Targets),action);if(args.Cancel)return;if(null!=obj.Listener&&_HasTarget(args,CodeDomVisitTargets.Expressions)
&&_CanVisit(obj.Listener,args))_VisitExpression(obj.Event,args.Set(args.Root,obj,"Listener",-1,_BuildPath(args.Path,"Listener",-1),obj.Listener,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitSnippetStatement(CodeSnippetStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic
=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitVariableDeclarationStatement(CodeVariableDeclarationStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if
(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.Type,args)){_VisitTypeReference(obj.Type,args.Set(args.Root,obj,
"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);if(args.Cancel)return;}if(null!=obj.InitExpression&&_HasTarget(args,CodeDomVisitTargets.Expressions)
&&_CanVisit(obj.InitExpression,args)){_VisitExpression(obj.InitExpression,args.Set(args.Root,obj,"InitExpression",-1,_BuildPath(args.Path,"InitExpression",-1),
obj.InitExpression,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,
i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitAssignStatement(CodeAssignStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if
(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(null!=obj.Left&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Left,args))_VisitExpression(obj.Left,args.Set(args.Root,obj,"Left",-1,_BuildPath(args.Path,"Left",-1),
obj.Left,args.Targets),action);if(args.Cancel)return;if(null!=obj.Right&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Right,args))_VisitExpression(obj.Right,
args.Set(args.Root,obj,"Right",-1,_BuildPath(args.Path,"Right",-1),obj.Right,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitLabeledStatement(CodeLabeledStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if
(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(null!=obj.Statement&&_HasTarget(args,CodeDomVisitTargets.Statements)&&_CanVisit(obj.Statement,args))_VisitStatement(obj.Statement,args.Set(args.Root,
obj,"Statement",-1,_BuildPath(args.Path,"Statement",-1),obj.Statement,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitGotoStatement(CodeGotoStatement obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)
_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),action);}if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",-1,_BuildPath(args.Path,"EndDirectives",-1),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitConditionStatement(CodeConditionStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir
=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Condition&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Condition,args))_VisitExpression(obj.Condition,
args.Set(args.Root,obj,"Condition",-1,_BuildPath(args.Path,"Condition",-1),obj.Condition,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,
CodeDomVisitTargets.Statements)){for(int ic=obj.TrueStatements.Count,i=0;i<ic;++i){var stmt=obj.TrueStatements[i];if(_CanVisit(stmt,args))_VisitStatement(stmt,
args.Set(args.Root,obj,"TrueStatements",i,_BuildPath(args.Path,"TrueStatements",i),stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Statements)){for(int ic=obj.FalseStatements.Count,i=0;i<ic;++i){var stmt=obj.FalseStatements[i];if(_CanVisit(stmt,args))_VisitStatement(stmt,
args.Set(args.Root,obj,"FalseStatements",i,_BuildPath(args.Path,"FalseStatements",i),stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitIterationStatement(CodeIterationStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir
=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.InitStatement&&_HasTarget(args,CodeDomVisitTargets.Statements)&&_CanVisit(obj.InitStatement,args))_VisitStatement(obj.InitStatement,
args.Set(args.Root,obj,"InitStatement",-1,_BuildPath(args.Path,"InitStatement",-1),obj.InitStatement,args.Targets),action);if(args.Cancel)return;if(null!=obj.TestExpression
&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.TestExpression,args))_VisitExpression(obj.TestExpression,args.Set(args.Root,obj,"TestExpression",-1,_BuildPath(args.Path,"TestExpression",-1),obj.TestExpression,
args.Targets),action);if(args.Cancel)return;if(null!=obj.IncrementStatement&&_HasTarget(args,CodeDomVisitTargets.Statements)&&_CanVisit(obj.IncrementStatement,args))
_VisitStatement(obj.IncrementStatement,args.Set(args.Root,obj,"IncrementStatement",-1,_BuildPath(args.Path,"IncrementStatement",-1),obj.IncrementStatement,
args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt
=obj.Statements[i];if(_CanVisit(stmt,args))_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),
action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitAttachEventStatement(CodeAttachEventStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Event&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Event,args))_VisitEventReferenceExpression(obj.Event,
args.Set(args.Root,obj,"Event",-1,_BuildPath(args.Path,"Event",-1),obj.Event,args.Targets),action);if(args.Cancel)return;if(null!=obj.Listener&&_HasTarget(args,CodeDomVisitTargets.Expressions)
&&_CanVisit(obj.Listener,args))_VisitExpression(obj.Listener,args.Set(args.Root,obj,"Listener",-1,_BuildPath(args.Path,"Listener",-1),obj.Listener,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitExpressionStatement(CodeExpressionStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)
||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic
=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Expression&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Expression,args))_VisitExpression(obj.Expression,
args.Set(args.Root,obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),obj.Expression,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitMethodReturnStatement(CodeMethodReturnStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir
=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Expression&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.Expression,args))_VisitExpression(obj.Expression,
args.Set(args.Root,obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),obj.Expression,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitThrowExceptionStatement(CodeThrowExceptionStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir
=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.ToThrow&&_HasTarget(args,CodeDomVisitTargets.Expressions)&&_CanVisit(obj.ToThrow,args))_VisitExpression(obj.ToThrow,
args.Set(args.Root,obj,"ToThrow",-1,_BuildPath(args.Path,"ToThrow",-1),obj.ToThrow,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitCommentStatement(CodeCommentStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements)||!_CanVisit(obj,args))return;if(args.Cancel)return;action(args);if(args.Cancel)return;if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,
args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)
_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),action);}if(null!=obj.Comment
&&_HasTarget(args,CodeDomVisitTargets.Comments))_VisitComment(obj.Comment,args.Set(args.Root,obj,"Comment",-1,_BuildPath(args.Path,"Comment",-1),obj.Comment,
args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir
=obj.EndDirectives[i];if(_CanVisit(dir,args))_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),
action);if(args.Cancel)return;}}}static void _VisitTypeParameter(CodeTypeParameter obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var
 attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.Constraints.Count,i=0;i<ic;++i)
{var ctr=obj.Constraints[i];if(_CanVisit(ctr,args))_VisitTypeReference(ctr,args.Set(args.Root,obj,"Constraints",i,_BuildPath(args.Path,"Constraints",i),
ctr,args.Targets),action);if(args.Cancel)return;}}}static void _VisitTypeReference(CodeTypeReference obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.TypeRefs)||!_CanVisit(obj,args))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.ArrayElementType
&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)&&_CanVisit(obj.ArrayElementType,args))_VisitTypeReference(obj.ArrayElementType,args.Set(args.Root,obj,"ArrayElementType",-1,_BuildPath(args.Path,"ArrayElementType",-1),
obj.ArrayElementType,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.TypeArguments.Count,i=0;i<ic;++i)
{var ctr=obj.TypeArguments[i];if(_CanVisit(ctr,args))_VisitTypeReference(ctr,args.Set(args.Root,obj,"TypeArguments",i,_BuildPath(args.Path,"TypeArguments",i),
ctr,args.Targets),action);if(args.Cancel)return;}}}static string _BuildPath(string path,string member,int index){if(string.IsNullOrEmpty(path))path=member;
else path=string.Concat(path,".",member);if(-1!=index)path=string.Concat(path,"[",index.ToString(),"]");return path;}/// <summary>
/// Returns the path from <paramref name="root"/> to <paramref name="target"/>
/// </summary>
/// <param name="root">The containing object to start the search from</param>
/// <param name="target">The target object to search for</param>
/// <param name="visitTargets">The targets to examine and traverse</param>
/// <returns>The path, or null if the object could not be found</returns>
public static string GetPathToObject(object root,object target,CodeDomVisitTargets visitTargets=CodeDomVisitTargets.All){string result=null;CodeDomVisitor.Visit(root,
(ctx)=>{if(ReferenceEquals(ctx.Target,target)){result=ctx.Path;ctx.Cancel=true;}},visitTargets);return result;}}}namespace CD{/// <summary>
/// Compares <see cref="CodeTypeReference"/> objects for equality
/// </summary>
public class CodeTypeReferenceEqualityComparer:IEqualityComparer<CodeTypeReference>{/// <summary>
/// Provides access to the default instance of this class
/// </summary>
public static readonly IEqualityComparer<CodeTypeReference>Default=new CodeTypeReferenceEqualityComparer();bool IEqualityComparer<CodeTypeReference>.Equals(CodeTypeReference
 x,CodeTypeReference y){return Equals(x,y);}/// <summary>
/// Indicates whether the two types are equal
/// </summary>
/// <param name="x">The first type to compare</param>
/// <param name="y">The second type tp compare</param>
/// <returns>True if the types are equal, otherwise false</returns>
public static bool Equals(CodeTypeReference x,CodeTypeReference y){if(ReferenceEquals(x,y))return true;if(ReferenceEquals(null,x))return false;if(ReferenceEquals(null,
y))return false;if(x.Options!=y.Options)return false;if(!Equals(x.ArrayElementType,y.ArrayElementType)||x.ArrayRank!=y.ArrayRank)return false;else{if(0
!=string.Compare(x.BaseType,y.BaseType))return false;var c=x.TypeArguments.Count;if(c!=y.TypeArguments.Count)return false;for(var i=0;i<c;++i){if(!Equals(x.TypeArguments[i],
y.TypeArguments[i]))return false;}}return true;}int IEqualityComparer<CodeTypeReference>.GetHashCode(CodeTypeReference obj){if(null==obj)return 0;var result
=obj.Options.GetHashCode();if(null!=obj.ArrayElementType){result^=obj.ArrayRank.GetHashCode();result^=Default.GetHashCode(obj.ArrayElementType);}else{
if(null!=obj.BaseType)result^=obj.BaseType.GetHashCode();for(int ic=obj.TypeArguments.Count,i=0;i<ic;++i)result^=Default.GetHashCode(obj.TypeArguments[i]);
}return result;}}}