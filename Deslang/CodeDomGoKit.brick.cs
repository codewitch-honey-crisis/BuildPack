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
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Net;
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
}CodeDomResolverScope _FillScope(CodeDomResolverScope result){ CodeCompileUnit ccu=null;object p;if(null==result.Expression){if(null!=result.TypeRef){
p=result.TypeRef;if(null==ccu)ccu=_GetRef(p,_rootKey)as CodeCompileUnit;while(null!=(p=_GetRef(p,_parentKey))){var expr=p as CodeExpression;if(null!=expr)
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
public static bool IsNullOrVoidType(CodeTypeReference type){return null==type||(0==type.ArrayRank&&0==string.Compare("System.Void",type.BaseType,StringComparison.InvariantCulture));
}/// <summary>
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
/// <returns>Either a runtime <see cref="Type"/> or a <see cref="CodeTypeDeclaration"/> representing the given type, or null if the type could not be resolved</returns>
/// <remarks>This routine cannot instantiate reified generic types of declared types, nor will it resolve types with declared types as generic arguments</remarks>
public object TryResolveType(CodeTypeReference type,CodeDomResolverScope scope=null)=>_ResolveType(type,scope);/// <summary>
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
{first=false;result=string.Concat(result,_GetDecoratedTypeName(scope.Types[i]));}}}return result;}object _ResolveType(CodeTypeReference type,CodeDomResolverScope
 scope){if(null==type)return null;if(null!=type.ArrayElementType&&1<=type.ArrayRank){ return typeof(Array);}var nss=new List<string>();if(null!=scope.DeclaringType)
{nss.Add(_BuildTypePrefix(scope,scope.Types.Count));if(1<scope.Types.Count){for(var i=0;i<scope.Types.Count-1;++i)nss.Add(_BuildTypePrefix(scope,i));}
}if(null!=scope.CompileUnit){foreach(CodeNamespace ns in scope.CompileUnit.Namespaces){if(string.IsNullOrEmpty(ns.Name)){foreach(CodeNamespaceImport nsi
 in ns.Imports)nss.Add(string.Concat(nsi.Namespace));}}}if(null!=scope.Namespace){if(!string.IsNullOrEmpty(scope.Namespace.Name)){nss.Add(scope.Namespace.Name);
foreach(CodeNamespaceImport nsi in scope.Namespace.Imports){nss.Add(nsi.Namespace);nss.Add(string.Concat(scope.Namespace.Name,".",nsi.Namespace));}}}nss.Add("");
var ctrs=new List<CodeTypeReference>();foreach(var pfx in nss){var s=pfx;if(0<s.Length)s=string.Concat(pfx,".",type.BaseType);else s=type.BaseType;var
 ctr=new CodeTypeReference();ctr.BaseType=s;ctr.TypeArguments.AddRange(type.TypeArguments);ctrs.Add(ctr);}var t=_DualResolve(ctrs);var rt=t as Type;if(null!=rt
&&0<type.TypeArguments.Count){var types=new Type[type.TypeArguments.Count];for(var i=0;i<types.Length;i++){types[i]=_ResolveType(type.TypeArguments[i],
scope)as Type;if(null==types[i])return rt;}return rt.MakeGenericType(types);}return t;}/// <summary>
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
if(null!=t)return t;}return null;}object _ResolveTypeImpl(CodeTypeReference type,int resolutionType=_ResolveAssemblies|_ResolveCompileUnits){if(type.BaseType
=="Token")System.Diagnostics.Debugger.Break();object result=null;if(null!=type.ArrayElementType&&1<=type.ArrayRank){ return typeof(Array);}if(_ResolveCompileUnits
==(resolutionType&_ResolveCompileUnits)){foreach(var ccu in CompileUnits){CodeDomVisitor.Visit(ccu,(ctx)=>{var td=ctx.Target as CodeTypeDeclaration;if
(null!=td){var name=_GetGenericName(td);CodeObject p=td;while((p=_GetRef(p,_parentKey)as CodeObject)!=null){var ptd=p as CodeTypeDeclaration;if(null!=
ptd){name=string.Concat(_GetGenericName(ptd),"+",name);td=ptd;}var ns=p as CodeNamespace;if(null!=ns&&!string.IsNullOrEmpty(ns.Name)){name=string.Concat(ns.Name,
".",name);}}if(name==type.BaseType){td=ctx.Target as CodeTypeDeclaration;result=td;ctx.Cancel=true;}}},CodeDomVisitTargets.Types|CodeDomVisitTargets.TypeRefs
|CodeDomVisitTargets.Members);if(null!=result)return result;}}if(_ResolveAssemblies==(resolutionType&_ResolveAssemblies)){Type t;if(_typeCache.TryGetValue(type,
out t))return t;foreach(var ccu in CompileUnits){var corlib=typeof(string).Assembly;var rt=corlib.GetType(type.BaseType,false,false);result=rt;if(null
!=result){_typeCache.Add(type,rt);return result;}foreach(var astr in ccu.ReferencedAssemblies){var asm=_LoadAsm(astr);rt=asm.GetType(type.BaseType,false,
false);result=rt;if(null!=result){_typeCache.Add(type,rt);return result;}}}if(0==CompileUnits.Count){var corlib=typeof(string).Assembly;var rt=corlib.GetType(type.BaseType,
false,false);result=rt;if(null!=result){_typeCache.Add(type,rt);return result;}}_typeCache.Add(type,null);}return result;}Assembly _LoadAsm(string asm)
{if(File.Exists(asm)){return Assembly.LoadFile(Path.GetFullPath(asm));}else if(asm.StartsWith(@"\\")){return Assembly.LoadFile(asm);}AssemblyName an=null;
try{an=new AssemblyName(asm);}catch{an=null;}if(null!=an){return Assembly.Load(an);}return Assembly.Load(asm);}/// <summary>
/// Clears the type cache
/// </summary>
public void ClearCache(){_typeCache.Clear();}/// <summary>
/// Refreshes the code after the graphs have been changed, added to, or removed from.
/// </summary>
/// <param name="typesOnly">Only go as far as types and their members</param>
public void Refresh(bool typesOnly=false){ for(int ic=CompileUnits.Count,i=0;i<ic;++i){var ccu=CompileUnits[i]; CodeDomVisitor.Visit(ccu,(ctx)=>{var co
=ctx.Target as CodeObject;if(null!=co){if(null!=ctx.Parent)co.UserData[_parentKey]=new WeakReference<object>(ctx.Parent);if(null!=ctx.Root) co.UserData[_rootKey]
=new WeakReference<object>(ctx.Root);}},typesOnly?CodeDomVisitTargets.Types|CodeDomVisitTargets.Members:CodeDomVisitTargets.All);}}}/// <summary>
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
});}var cc=value as CodeCatchClause;if(null!=cc){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string),
typeof(CodeTypeReference),typeof(CodeStatement[])}),new object[]{cc.LocalName,cc.CatchExceptionType,cc.Statements});}var rd=value as CodeRegionDirective;
if(null!=rd){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeRegionMode),typeof(string)}),new object[]
{rd.RegionMode,rd.RegionText});}var cp=value as CodeChecksumPragma;if(null!=cp){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(string),typeof(Guid),typeof(byte[])}),new object[]{cp.FileName,cp.ChecksumAlgorithmId,cp.ChecksumData});}var lp=value as CodeLinePragma;
if(null!=lp){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string),typeof(int)}),new object[]{lp.FileName,lp.LineNumber
});}var cm=value as CodeComment;if(null!=cm){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(string),typeof(bool)
}),new object[]{cm.Text,cm.DocComment});}Guid g;if(value is Guid){g=(Guid)value;return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(string)}),new object[]{g.ToString()});}throw new NotSupportedException("Unsupported type of code object. Could not retrieve the instance data.");
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
{typeof(string),typeof(CodeStatement)}),new object[]{l.Label,l.Statement});}var v=stmt as CodeVariableDeclarationStatement;if(null!=v){if(_HasExtraNonsense(v))
{return new KeyValuePair<MemberInfo,object[]>(typeof(CodeDomBuilder).GetMethod(stmt.GetType().Name.Substring(4)),new object[]{v.Type,v.Name,v.InitExpression,
_ToArray(a.StartDirectives),_ToArray(a.EndDirectives),a.LinePragma});}return new KeyValuePair<MemberInfo,object[]>(stmt.GetType().GetConstructor(new Type[]
{typeof(CodeTypeReference),typeof(string),typeof(CodeExpression)}),new object[]{v.Type,v.Name,v.InitExpression});}throw new NotSupportedException("The statement instance data could not be serialized.");
}static bool _HasExtraNonsense(CodeStatement stmt){return(null!=stmt.LinePragma||0<stmt.StartDirectives.Count||0<stmt.EndDirectives.Count);}static KeyValuePair<MemberInfo,object[]>
_GetInstanceData(CodeExpression value){var ar=value as CodeArgumentReferenceExpression;if(null!=ar)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(string)}),new object[]{ar.ParameterName});var ac=value as CodeArrayCreateExpression;if(null!=ac){if(null!=ac.Initializers&&0<ac.Initializers.Count)
{return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(CodeExpression[])}),new object[]
{ac.CreateType,_ToArray(ac.Initializers)});}else if(null!=ac.SizeExpression){return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
 Type[]{typeof(CodeTypeReference),typeof(CodeExpression)}),new object[]{ac.CreateType,ac.SizeExpression});}else return new KeyValuePair<MemberInfo,object[]>(
value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(int)}),new object[]{ac.CreateType,ac.Size});}var ai=value as CodeArrayIndexerExpression;
if(null!=ai)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(CodeExpression[])}),new
 object[]{ai.TargetObject,_ToArray(ai.Indices)});var br=value as CodeBaseReferenceExpression;if(null!=br)return new KeyValuePair<MemberInfo,object[]>(
value.GetType().GetConstructor(new Type[]{}),new object[]{});var bo=value as CodeBinaryOperatorExpression;if(null!=bo)return new KeyValuePair<MemberInfo,
object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeExpression),typeof(CodeBinaryOperatorType),typeof(CodeExpression)}),new object[]{bo.Left,bo.Operator,bo.Right
});var c=value as CodeCastExpression;if(null!=c)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new Type[]{typeof(CodeTypeReference),typeof(CodeExpression)}),
new object[]{c.TargetType,c.Expression});var dv=value as CodeDefaultValueExpression;if(null!=dv)return new KeyValuePair<MemberInfo,object[]>(value.GetType().GetConstructor(new
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
 new NotSupportedException("Unsupported code type. Cannot convert to instance data.");}static CodeExpression[]_ToArray(CodeExpressionCollection exprs)
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
 in cmp.GetStatements){var r=_TraceStatement(tt,t,out found);result.AddRange(r);if(found){return result;}}found=false;return new CodeVariableDeclarationStatement[0];
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
/// A helper method to replace the current target with a new value during the visit operation
/// </summary>
/// <param name="ctx">The visit context</param>
/// <param name="newTarget">The target value to replace the current target with</param>
/// <remarks>This method is intended to be called from inside the anonymous visit method. This method uses reflection.</remarks>
public static void ReplaceTarget(CodeDomVisitContext ctx,object newTarget){try{var ma=ctx.Parent.GetType().GetMember(ctx.Member,BindingFlags.Public|BindingFlags.Instance
|BindingFlags.SetProperty|BindingFlags.GetProperty);var pi=ma[0]as PropertyInfo;if(-1!=ctx.Index){var l=pi.GetValue(ctx.Parent)as System.Collections.IList;
l[ctx.Index]=newTarget;return;}pi.SetValue(ctx.Parent,newTarget);}catch(TargetInvocationException tex){throw tex.InnerException;}}static bool _HasTarget(CodeDomVisitContext
 args,CodeDomVisitTargets target){return target==(args.Targets&target);}static void _VisitLinePragma(CodeLinePragma obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Directives))return;if(args.Cancel)return; action(args);}static void _VisitComment(CodeComment obj,CodeDomVisitContext
 args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Comments))return;if(args.Cancel)return; action(args);}static void _VisitDirective(CodeDirective
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)return;var rd=obj as CodeRegionDirective;if(null!=rd){_VisitRegionDirective(rd,
args,action);return;}var cp=obj as CodeChecksumPragma;if(null!=cp){_VisitChecksumPragma(cp,args,action);return;}throw new NotSupportedException("Unsupported directive type in graph");
}static void _VisitRegionDirective(CodeRegionDirective obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Directives))
return;if(args.Cancel)return;action(args);}static void _VisitChecksumPragma(CodeChecksumPragma obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Directives))return;if(args.Cancel)return;action(args);}static void _VisitNamespaceImport(CodeNamespaceImport obj,
CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)return; action(args);}static void _VisitAttributeDeclaration(CodeAttributeDeclaration
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Attributes))return;if(args.Cancel)return; action(args);
if(null!=obj.AttributeType)_VisitTypeReference(obj.AttributeType,args.Set(args.Root,obj,"AttributeType",-1,_BuildPath(args.Path,"AttributeType",-1),obj.AttributeType,
args.Targets),action);if(args.Cancel)return;for(int ic=obj.Arguments.Count,i=0;i<ic;++i){var arg=obj.Arguments[i];_VisitAttributeArgument(arg,args.Set(args.Root,
obj,"Arguments",i,_BuildPath(args.Path,"Arguments",i),arg,args.Targets),action);if(args.Cancel)return;}}static void _VisitAttributeArgument(CodeAttributeArgument
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Attributes))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.Value)_VisitExpression(obj.Value,args.Set(args.Root,obj,"Value",-1,_BuildPath(args.Path,"Value",-1),obj.Value,args.Targets),
action);}static void _VisitCompileUnit(CodeCompileUnit obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)return; action(args);if(args.Cancel)
return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,
args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.AssemblyCustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.AssemblyCustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"AssemblyCustomAttributes",i,_BuildPath(args.Path,"AssemblyCustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;
}}for(int ic=obj.Namespaces.Count,i=0;i<ic;++i){var ns=obj.Namespaces[i];_VisitNamespace(ns,args.Set(args.Root,obj,"Namespaces",i,_BuildPath(args.Path,"Namespaces",i),
ns,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var
 dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitNamespace(CodeNamespace obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)return; action(args);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];_VisitCommentStatement(cc,
args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Types))
{for(int ic=obj.Types.Count,i=0;i<ic;++i){var decl=obj.Types[i];_VisitTypeDeclaration(decl,args.Set(args.Root,obj,"Types",i,_BuildPath(args.Path,"Types",i),decl,
args.Targets),action);if(args.Cancel)return;}}}static void _VisitTypeDeclaration(CodeTypeDeclaration obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Types))return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int
 ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",i),
obj.LinePragma,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}for(int ic=obj.TypeParameters.Count,i=0;i<ic;++i)
{var ctp=obj.TypeParameters[i];_VisitTypeParameter(ctp,args.Set(args.Root,obj,"TypeParameters",i,_BuildPath(args.Path,"TypeParameters",i),ctp,args.Targets),
action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.BaseTypes.Count,i=0;i<ic;++i){var ctr=obj.BaseTypes[i];
_VisitTypeReference(ctr,args.Set(args.Root,obj,"BaseTypes",i,_BuildPath(args.Path,"BaseTypes",i),ctr,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Members)){for(int ic=obj.Members.Count,i=0;i<ic;++i){var ctm=obj.Members[i];_VisitTypeMember(ctm,args.Set(args.Root,obj,"Members",i,_BuildPath(args.Path,"Members",i),
ctm,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i)
{var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitTypeMember(CodeTypeMember obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)return;var
 ce=obj as CodeMemberEvent;if(null!=ce){_VisitMemberEvent(ce,args,action);return;}var cf=obj as CodeMemberField;if(null!=cf){_VisitMemberField(cf,args,
action);return;}var cm=obj as CodeMemberMethod;if(null!=cm){_VisitMemberMethod(cm,args,action);return;}var cp=obj as CodeMemberProperty;if(null!=cp){_VisitMemberProperty(cp,
args,action);return;}var cstm=obj as CodeSnippetTypeMember;if(null!=cstm){_VisitSnippetTypeMember(cstm,args,action);return;}var ctd=obj as CodeTypeDeclaration;
if(null!=ctd){_VisitTypeDeclaration(ctd,args,action);return;}throw new NotSupportedException("The graph contains an unsupported type declaration");}static
 void _VisitMemberEvent(CodeMemberEvent obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members))return;
if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];_VisitCommentStatement(cc,
args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes)){
for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.Type,args.Set(args.Root,
obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i)
{var ctr=obj.ImplementationTypes[i];_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),
ctr,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.PrivateImplementationType,
args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),obj.PrivateImplementationType,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitSnippetTypeMember(CodeSnippetTypeMember
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members))return;if(args.Cancel)return; action(args);if
(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,
args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)
_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),action);}if(_HasTarget(args,
CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),
action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];
_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);
if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitMemberField(CodeMemberField obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members))
return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir
=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes))
{for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.Type,args.Set(args.Root,
obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);if(null!=obj.InitExpression&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.InitExpression,args.Set(args.Root,obj,"InitExpression",-1,_BuildPath(args.Path,"InitExpression",-1),obj.InitExpression,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitMemberMethod(CodeMemberMethod
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members))return;if(args.Cancel)return;var ctor=obj as
 CodeConstructor;if(null!=ctor){_VisitConstructor(ctor,args,action);return;}var entryPoint=obj as CodeEntryPointMethod;if(null!=entryPoint){_VisitEntryPointMethod(entryPoint,
args,action);return;} action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];_VisitCommentStatement(cc,
args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes)){
for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.ReturnType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.ReturnType,
args.Set(args.Root,obj,"ReturnType",-1,_BuildPath(args.Path,"ReturnType",-1),obj.ReturnType,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.TypeRefs))
{for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=obj.ImplementationTypes[i];_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),
ctr,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.PrivateImplementationType,
args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),obj.PrivateImplementationType,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var pd=obj.Parameters[i];_VisitParameterDeclarationExpression(pd,
args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),pd,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements))
{for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt=obj.Statements[i];_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),
stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i)
{var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitEntryPointMethod(CodeEntryPointMethod obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Members))return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes))
{for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.ReturnType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.ReturnType,
args.Set(args.Root,obj,"ReturnType",-1,_BuildPath(args.Path,"ReturnType",-1),obj.ReturnType,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.TypeRefs))
{for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=obj.ImplementationTypes[i];_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),
ctr,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.PrivateImplementationType,
args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),obj.PrivateImplementationType,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var pd=obj.Parameters[i];_VisitParameterDeclarationExpression(pd,
args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),pd,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements))
{for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt=obj.Statements[i];_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),
stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i)
{var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitConstructor(CodeConstructor obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members))
return;if(args.Cancel)return; action(args);if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir
=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);
if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];
_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),cc,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes))
{for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;
i<ic;++i){var ctr=obj.ImplementationTypes[i];_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),
ctr,args.Targets),action);if(args.Cancel)return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.PrivateImplementationType,
args.Set(args.Root,obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),obj.PrivateImplementationType,args.Targets),
action);if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var pd=obj.Parameters[i];_VisitParameterDeclarationExpression(pd,
args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),pd,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Expressions))
{for(int ic=obj.ChainedConstructorArgs.Count,i=0;i<ic;++i){var ce=obj.ChainedConstructorArgs[i];_VisitExpression(ce,args.Set(args.Root,obj,"ChainedConstructorArgs",i,_BuildPath(args.Path,"ChainedConstructorArgs",i),
ce,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.BaseConstructorArgs.Count,i=0;i<ic;
++i){var ce=obj.BaseConstructorArgs[i];_VisitExpression(ce,args.Set(args.Root,obj,"BaseConstructorArgs",i,_BuildPath(args.Path,"BaseConstructorArgs",i),
ce,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var
 stmt=obj.Statements[i];_VisitStatement(stmt,args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),action);if(args.Cancel)
return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitMemberProperty(CodeMemberProperty
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Members))return;if(args.Cancel)return; action(args);if
(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,
args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)
_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),action);}if(_HasTarget(args,
CodeDomVisitTargets.Comments)){for(int ic=obj.Comments.Count,i=0;i<ic;++i){var cc=obj.Comments[i];_VisitCommentStatement(cc,args.Set(args.Root,obj,"Comments",i,_BuildPath(args.Path,"Comments",i),
cc,args.Targets),action);}}if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];
_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);
if(args.Cancel)return;}}if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),
obj.Type,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.ImplementationTypes.Count,i=0;i<ic;++i){var ctr=obj.ImplementationTypes[i];
_VisitTypeReference(ctr,args.Set(args.Root,obj,"ImplementationTypes",i,_BuildPath(args.Path,"ImplementationTypes",i),ctr,args.Targets),action);if(args.Cancel)
return;}}if(null!=obj.PrivateImplementationType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.PrivateImplementationType,args.Set(args.Root,
obj,"PrivateImplementationType",-1,_BuildPath(args.Path,"PrivateImplementationType",-1),obj.PrivateImplementationType,args.Targets),action);if(_HasTarget(args,
CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var pd=obj.Parameters[i];_VisitParameterDeclarationExpression(pd,args.Set(args.Root,
obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),pd,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements))
{for(int ic=obj.GetStatements.Count,i=0;i<ic;++i){var stmt=obj.GetStatements[i];_VisitStatement(stmt,args.Set(args.Root,obj,"GetStatements",i,_BuildPath(args.Path,"GetStatements",i),
stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.SetStatements.Count,i=0;i<ic;++i)
{var stmt=obj.SetStatements[i];_VisitStatement(stmt,args.Set(args.Root,obj,"SetStatements",i,_BuildPath(args.Path,"SetStatements",i),stmt,args.Targets),
action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitExpression(CodeExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))
return;if(args.Cancel)return;var argr=obj as CodeArgumentReferenceExpression;if(null!=argr){_VisitArgumentReferenceExpression(argr,args,action);return;
}var arrc=obj as CodeArrayCreateExpression;if(null!=arrc){_VisitArrayCreateExpression(arrc,args,action);return;}var aic=obj as CodeArrayIndexerExpression;
if(null!=aic){_VisitArrayIndexerExpression(aic,args,action);return;}var br=obj as CodeBaseReferenceExpression;if(null!=br){_VisitBaseReferenceExpression(br,
args,action);return;}var bo=obj as CodeBinaryOperatorExpression;if(null!=bo){_VisitBinaryOperatorExpression(bo,args,action);return;}var cc=obj as CodeCastExpression;
if(null!=cc){_VisitCastExpression(cc,args,action);return;}var cdv=obj as CodeDefaultValueExpression;if(null!=cdv){_VisitDefaultValueExpression(cdv,args,
action);return;}var cdc=obj as CodeDelegateCreateExpression;if(null!=cdc){_VisitDelegateCreateExpression(cdc,args,action);return;}var cdi=obj as CodeDelegateInvokeExpression;
if(null!=cdi){_VisitDelegateInvokeExpression(cdi,args,action);return;}var cd=obj as CodeDirectionExpression;if(null!=cd){_VisitDirectionExpression(cd,
args,action);return;}var cer=obj as CodeEventReferenceExpression;if(null!=cer){_VisitEventReferenceExpression(cer,args,action);return;}var cfr=obj as CodeFieldReferenceExpression;
if(null!=cfr){_VisitFieldReferenceExpression(cfr,args,action);return;}var ci=obj as CodeIndexerExpression;if(null!=ci){_VisitIndexerExpression(ci,args,
action);return;}var cmi=obj as CodeMethodInvokeExpression;if(null!=cmi){_VisitMethodInvokeExpression(cmi,args,action);return;}var cmr=obj as CodeMethodReferenceExpression;
if(null!=cmr){_VisitMethodReferenceExpression(cmr,args,action);return;}var coc=obj as CodeObjectCreateExpression;if(null!=coc){_VisitObjectCreateExpression(coc,
args,action);return;}var cpd=obj as CodeParameterDeclarationExpression;if(null!=cpd){_VisitParameterDeclarationExpression(cpd,args,action);return;}var
 cp=obj as CodePrimitiveExpression;if(null!=cp){_VisitPrimitiveExpression(cp,args,action);return;}var cpr=obj as CodePropertyReferenceExpression;if(null
!=cpr){_VisitPropertyReferenceExpression(cpr,args,action);return;}var cpsvr=obj as CodePropertySetValueReferenceExpression;if(null!=cpsvr){_VisitPropertySetValueReferenceExpression(cpsvr,
args,action);return;}var cs=obj as CodeSnippetExpression;if(null!=cs){_VisitSnippetExpression(cs,args,action);return;}var cthr=obj as CodeThisReferenceExpression;
if(null!=cthr){_VisitThisReferenceExpression(cthr,args,action);return;}var cto=obj as CodeTypeOfExpression;if(null!=cto){_VisitTypeOfExpression(cto,args,
action);return;}var ctr=obj as CodeTypeReferenceExpression;if(null!=ctr){_VisitTypeReferenceExpression(ctr,args,action);return;}var cvr=obj as CodeVariableReferenceExpression;
if(null!=cvr){_VisitVariableReferenceExpression(cvr,args,action);return;}throw new NotSupportedException("An expression that is not supported was part of the code graph");
}static void _VisitVariableReferenceExpression(CodeVariableReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);}static void _VisitTypeOfExpression(CodeTypeOfExpression obj,CodeDomVisitContext
 args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;
if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),
action);}static void _VisitSnippetExpression(CodeSnippetExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))
return;if(args.Cancel)return; action(args);}static void _VisitParameterDeclarationExpression(CodeParameterDeclarationExpression obj,CodeDomVisitContext
 args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;
if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,
args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),attrDecl,args.Targets),action);if(args.Cancel)return;}}if(null!=
obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),
obj.Type,args.Targets),action);}static void _VisitArgumentReferenceExpression(CodeArgumentReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);}static void _VisitPrimitiveExpression(CodePrimitiveExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
}static void _VisitDirectionExpression(CodeDirectionExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))
return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){if(null!=obj.Expression)_VisitExpression(obj.Expression,
args.Set(args.Root,obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),obj.Expression,args.Targets),action);}}static void _VisitEventReferenceExpression(CodeEventReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.TargetObject,args.Set(args.Root,
obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);}static void _VisitFieldReferenceExpression(CodeFieldReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.TargetObject,args.Set(args.Root,
obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);}static void _VisitPropertyReferenceExpression(CodePropertyReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.TargetObject,args.Set(args.Root,
obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);}static void _VisitPropertySetValueReferenceExpression(CodePropertySetValueReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
}static void _VisitMethodReferenceExpression(CodeMethodReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);
}static void _VisitTypeReferenceExpression(CodeTypeReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))
return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.Type,
args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);}static void _VisitDelegateCreateExpression(CodeDelegateCreateExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.DelegateType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.DelegateType,args.Set(args.Root,
obj,"DelegateType",-1,_BuildPath(args.Path,"DelegateType",-1),obj.DelegateType,args.Targets),action);if(args.Cancel)return;if(null!=obj.TargetObject&&
_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,
args.Targets),action);}static void _VisitDelegateInvokeExpression(CodeDelegateInvokeExpression obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i){var ce=obj.Parameters[i];_VisitExpression(ce,
args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),ce,args.Targets),action);if(args.Cancel)return;}}}static void _VisitMethodInvokeExpression(CodeMethodInvokeExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.Method&&_HasTarget(args,CodeDomVisitTargets.Expressions)){_VisitMethodReferenceExpression(obj.Method,args.Set(args.Root,
obj,"Method",-1,_BuildPath(args.Path,"Method",-1),obj.Method,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.Expressions))
{for(var i=0;i<obj.Parameters.Count;++i){var ce=obj.Parameters[i];_VisitExpression(ce,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),ce,
args.Targets),action);if(args.Cancel)return;}}}static void _VisitBinaryOperatorExpression(CodeBinaryOperatorExpression obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.Left&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.Left,args.Set(args.Root,obj,"Left",-1,_BuildPath(args.Path,"Left",-1),obj.Left,args.Targets),action);if(args.Cancel)return;if(null
!=obj.Right&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.Right,args.Set(args.Root,obj,"Right",-1,_BuildPath(args.Path,"Right",-1),obj.Right,
args.Targets),action);}static void _VisitCastExpression(CodeCastExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.TargetType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))
_VisitTypeReference(obj.TargetType,args.Set(args.Root,obj,"TargetType",-1,_BuildPath(args.Path,"TargetType",-1),obj.TargetType,args.Targets),action);if
(args.Cancel)return;if(null!=obj.Expression&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.Expression,args.Set(args.Root,obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),
obj.Expression,args.Targets),action);}static void _VisitDefaultValueExpression(CodeDefaultValueExpression obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))
_VisitTypeReference(obj.Type,args.Set(args.Root,obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);}static void _VisitBaseReferenceExpression(CodeBaseReferenceExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
}static void _VisitThisReferenceExpression(CodeThisReferenceExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))
return;if(args.Cancel)return; action(args);}static void _VisitArrayCreateExpression(CodeArrayCreateExpression obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.CreateType
&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.CreateType,args.Set(args.Root,obj,"CreateType",-1,_BuildPath(args.Path,"CreateType",-1),
obj.CreateType,args.Targets),action);if(args.Cancel)return;if(null!=obj.SizeExpression&&_HasTarget(args,CodeDomVisitTargets.Expressions)){_VisitExpression(obj.SizeExpression,
args.Set(args.Root,obj,"SizeExpression",-1,_BuildPath(args.Path,"SizeExpression",-1),obj.SizeExpression,args.Targets),action);if(args.Cancel)return;}if
(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Initializers.Count,i=0;i<ic;++i){var ce=obj.Initializers[i];_VisitExpression(ce,args.Set(args.Root,
obj,"Initializers",i,_BuildPath(args.Path,"Initializers",i),ce,args.Targets),action);if(args.Cancel)return;}}}static void _VisitObjectCreateExpression(CodeObjectCreateExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.CreateType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.CreateType,args.Set(args.Root,obj,"CreateType",-1,_BuildPath(args.Path,"CreateType",-1),
obj.CreateType,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Parameters.Count,i=0;i<ic;++i)
{var ce=obj.Parameters[i];_VisitExpression(ce,args.Set(args.Root,obj,"Parameters",i,_BuildPath(args.Path,"Parameters",i),ce,args.Targets),action);if(args.Cancel)
return;}}}static void _VisitArrayIndexerExpression(CodeArrayIndexerExpression obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.TargetObject,args.Set(args.Root,obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions)){for(int ic=obj.Indices.Count,i=0;i<ic;++i){var ce=obj.Indices[i];_VisitExpression(ce,
args.Set(args.Root,obj,"Indices",i,_BuildPath(args.Path,"Indices",i),ce,args.Targets),action);if(args.Cancel)return;}}}static void _VisitIndexerExpression(CodeIndexerExpression
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Expressions))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(null!=obj.TargetObject&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.TargetObject,args.Set(args.Root,
obj,"TargetObject",-1,_BuildPath(args.Path,"TargetObject",-1),obj.TargetObject,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Expressions))
{for(int ic=obj.Indices.Count,i=0;i<ic;++i){var ce=obj.Indices[i];_VisitExpression(ce,args.Set(args.Root,obj,"Indices",i,_BuildPath(args.Path,"Indices",i),
ce,args.Targets),action);if(args.Cancel)return;}}}static void _VisitStatement(CodeStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if
(args.Cancel)return;var ca=obj as CodeAssignStatement;if(null!=ca){_VisitAssignStatement(ca,args,action);return;}var cae=obj as CodeAttachEventStatement;
if(null!=cae){_VisitAttachEventStatement(cae,args,action);return;}var cc=obj as CodeCommentStatement;if(null!=cc){_VisitCommentStatement(cc,args,action);
return;}var ccnd=obj as CodeConditionStatement;if(null!=ccnd){_VisitConditionStatement(ccnd,args,action);return;}var ce=obj as CodeExpressionStatement;
if(null!=ce){_VisitExpressionStatement(ce,args,action);return;}var cg=obj as CodeGotoStatement;if(null!=cg){_VisitGotoStatement(cg,args,action);return;
}var ci=obj as CodeIterationStatement;if(null!=ci){_VisitIterationStatement(ci,args,action);return;}var cl=obj as CodeLabeledStatement;if(null!=cl){_VisitLabeledStatement(cl,
args,action);return;}var cm=obj as CodeMethodReturnStatement;if(null!=cm){_VisitMethodReturnStatement(cm,args,action);return;}var cre=obj as CodeRemoveEventStatement;
if(null!=cre){_VisitRemoveEventStatement(cre,args,action);return;}var cs=obj as CodeSnippetStatement;if(null!=cs){_VisitSnippetStatement(cs,args,action);
return;}var cte=obj as CodeThrowExceptionStatement;if(null!=cte){_VisitThrowExceptionStatement(cte,args,action);return;}var ctcf=obj as CodeTryCatchFinallyStatement;
if(null!=ctcf){_VisitTryCatchFinallyStatement(ctcf,args,action);return;}var cvd=obj as CodeVariableDeclarationStatement;if(null!=cvd){_VisitVariableDeclarationStatement(cvd,
args,action);return;}throw new NotSupportedException("The graph contains an unsupported statement");}static void _VisitCatchClause(CodeCatchClause obj,CodeDomVisitContext
 args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;
if(null!=obj.CatchExceptionType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitTypeReference(obj.CatchExceptionType,args.Set(args.Root,obj,"CatchExceptionType",-1,_BuildPath(args.Path,"CatchExceptionType",-1),obj.CatchExceptionType,args.Targets),action);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt=obj.Statements[i];_VisitStatement(stmt,
args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),action);if(args.Cancel)return;}}}static void _VisitTryCatchFinallyStatement(CodeTryCatchFinallyStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.TryStatements.Count,i=0;i<ic;++i){var stmt=obj.TryStatements[i];_VisitStatement(stmt,
args.Set(args.Root,obj,"TryStatements",i,_BuildPath(args.Path,"TryStatements",i),stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,
CodeDomVisitTargets.Statements)){for(int ic=obj.CatchClauses.Count,i=0;i<ic;++i){var cl=obj.CatchClauses[i];_VisitCatchClause(cl,args.Set(args.Root,obj,"CatchClauses",-1,_BuildPath(args.Path,"CatchClauses",-1),
cl,args.Targets),action);if(args.Cancel)return;}}for(int ic=obj.FinallyStatements.Count,i=0;i<ic;++i){var stmt=obj.FinallyStatements[i];_VisitStatement(stmt,
args.Set(args.Root,obj,"FinallyStatements",i,_BuildPath(args.Path,"FinallyStatements",i),stmt,args.Targets),action);if(args.Cancel)return;}if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,
obj,"EndDirectives",-1,_BuildPath(args.Path,"EndDirectives",-1),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitRemoveEventStatement(CodeRemoveEventStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(null!=obj.Event&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))_VisitEventReferenceExpression(obj.Event,args.Set(args.Root,obj,"Event",-1,_BuildPath(args.Path,"Event",-1),
obj.Event,args.Targets),action);if(args.Cancel)return;if(null!=obj.Listener&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.Event,
args.Set(args.Root,obj,"Listener",-1,_BuildPath(args.Path,"Listener",-1),obj.Listener,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitSnippetStatement(CodeSnippetStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitVariableDeclarationStatement(CodeVariableDeclarationStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for
(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Type&&_HasTarget(args,CodeDomVisitTargets.TypeRefs)){_VisitTypeReference(obj.Type,args.Set(args.Root,
obj,"Type",-1,_BuildPath(args.Path,"Type",-1),obj.Type,args.Targets),action);if(args.Cancel)return;}if(null!=obj.InitExpression&&_HasTarget(args,CodeDomVisitTargets.Expressions))
{_VisitExpression(obj.InitExpression,args.Set(args.Root,obj,"InitExpression",-1,_BuildPath(args.Path,"InitExpression",-1),obj.InitExpression,args.Targets),
action);if(args.Cancel)return;}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitAssignStatement(CodeAssignStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))
return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Left&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.Left,args.Set(args.Root,
obj,"Left",-1,_BuildPath(args.Path,"Left",-1),obj.Left,args.Targets),action);if(args.Cancel)return;if(null!=obj.Right&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.Right,args.Set(args.Root,obj,"Right",-1,_BuildPath(args.Path,"Right",-1),obj.Right,args.Targets),action);if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitLabeledStatement(CodeLabeledStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Statement&&_HasTarget(args,CodeDomVisitTargets.Statements))_VisitStatement(obj.Statement,args.Set(args.Root,
obj,"Statement",-1,_BuildPath(args.Path,"Statement",-1),obj.Statement,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitGotoStatement(CodeGotoStatement obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",-1,_BuildPath(args.Path,"EndDirectives",-1),dir,args.Targets),action);if(args.Cancel)return;
}}}static void _VisitConditionStatement(CodeConditionStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))
return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Condition&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.Condition,args.Set(args.Root,
obj,"Condition",-1,_BuildPath(args.Path,"Condition",-1),obj.Condition,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Statements))
{for(int ic=obj.TrueStatements.Count,i=0;i<ic;++i){var stmt=obj.TrueStatements[i];_VisitStatement(stmt,args.Set(args.Root,obj,"TrueStatements",i,_BuildPath(args.Path,"TrueStatements",i),
stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.FalseStatements.Count,i=0;i<ic;++i)
{var stmt=obj.FalseStatements[i];_VisitStatement(stmt,args.Set(args.Root,obj,"FalseStatements",i,_BuildPath(args.Path,"FalseStatements",i),stmt,args.Targets),
action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}
}}static void _VisitIterationStatement(CodeIterationStatement obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))
return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,
i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.InitStatement&&_HasTarget(args,CodeDomVisitTargets.Statements))_VisitStatement(obj.InitStatement,args.Set(args.Root,
obj,"InitStatement",-1,_BuildPath(args.Path,"InitStatement",-1),obj.InitStatement,args.Targets),action);if(args.Cancel)return;if(null!=obj.TestExpression
&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.TestExpression,args.Set(args.Root,obj,"TestExpression",-1,_BuildPath(args.Path,"TestExpression",-1),obj.TestExpression,
args.Targets),action);if(args.Cancel)return;if(null!=obj.IncrementStatement&&_HasTarget(args,CodeDomVisitTargets.Statements))_VisitStatement(obj.IncrementStatement,
args.Set(args.Root,obj,"IncrementStatement",-1,_BuildPath(args.Path,"IncrementStatement",-1),obj.IncrementStatement,args.Targets),action);if(args.Cancel)
return;if(_HasTarget(args,CodeDomVisitTargets.Statements)){for(int ic=obj.Statements.Count,i=0;i<ic;++i){var stmt=obj.Statements[i];_VisitStatement(stmt,
args.Set(args.Root,obj,"Statements",i,_BuildPath(args.Path,"Statements",i),stmt,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitAttachEventStatement(CodeAttachEventStatement obj,CodeDomVisitContext args,CodeDomVisitAction
 action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Event&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitEventReferenceExpression(obj.Event,args.Set(args.Root,
obj,"Event",-1,_BuildPath(args.Path,"Event",-1),obj.Event,args.Targets),action);if(args.Cancel)return;if(null!=obj.Listener&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.Listener,args.Set(args.Root,obj,"Listener",-1,_BuildPath(args.Path,"Listener",-1),obj.Listener,args.Targets),action);if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,
obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitExpressionStatement(CodeExpressionStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(null!=obj.Expression&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.Expression,args.Set(args.Root,obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),
obj.Expression,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;
i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),
action);if(args.Cancel)return;}}}static void _VisitMethodReturnStatement(CodeMethodReturnStatement obj,CodeDomVisitContext args,CodeDomVisitAction action)
{if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),
obj.LinePragma,args.Targets),action);}if(null!=obj.Expression&&_HasTarget(args,CodeDomVisitTargets.Expressions))_VisitExpression(obj.Expression,args.Set(args.Root,
obj,"Expression",-1,_BuildPath(args.Path,"Expression",-1),obj.Expression,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives))
{for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),
dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitThrowExceptionStatement(CodeThrowExceptionStatement obj,CodeDomVisitContext args,
CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(_HasTarget(args,
CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];_VisitDirective(dir,args.Set(args.Root,
obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,
args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),action);}if(null!=obj.ToThrow&&_HasTarget(args,CodeDomVisitTargets.Expressions))
_VisitExpression(obj.ToThrow,args.Set(args.Root,obj,"ToThrow",-1,_BuildPath(args.Path,"ToThrow",-1),obj.ToThrow,args.Targets),action);if(args.Cancel)return;
if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,
args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),action);if(args.Cancel)return;}}}static void _VisitCommentStatement(CodeCommentStatement
 obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,CodeDomVisitTargets.Statements))return;if(args.Cancel)return;action(args);
if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.StartDirectives.Count,i=0;i<ic;++i){var dir=obj.StartDirectives[i];
_VisitDirective(dir,args.Set(args.Root,obj,"StartDirectives",i,_BuildPath(args.Path,"StartDirectives",i),dir,args.Targets),action);if(args.Cancel)return;
}if(null!=obj.LinePragma)_VisitLinePragma(obj.LinePragma,args.Set(args.Root,obj,"LinePragma",-1,_BuildPath(args.Path,"LinePragma",-1),obj.LinePragma,args.Targets),
action);}if(null!=obj.Comment&&_HasTarget(args,CodeDomVisitTargets.Comments))_VisitComment(obj.Comment,args.Set(args.Root,obj,"Comment",-1,_BuildPath(args.Path,"Comment",-1),
obj.Comment,args.Targets),action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Directives)){for(int ic=obj.EndDirectives.Count,i=0;i<ic;
++i){var dir=obj.EndDirectives[i];_VisitDirective(dir,args.Set(args.Root,obj,"EndDirectives",i,_BuildPath(args.Path,"EndDirectives",i),dir,args.Targets),
action);if(args.Cancel)return;}}}static void _VisitTypeParameter(CodeTypeParameter obj,CodeDomVisitContext args,CodeDomVisitAction action){if(args.Cancel)
return; action(args);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.Attributes)){for(int ic=obj.CustomAttributes.Count,i=0;i<ic;++i){var
 attrDecl=obj.CustomAttributes[i];_VisitAttributeDeclaration(attrDecl,args.Set(args.Root,obj,"CustomAttributes",i,_BuildPath(args.Path,"CustomAttributes",i),
attrDecl,args.Targets),action);if(args.Cancel)return;}}if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.Constraints.Count,i=0;i<ic;++i)
{var ctr=obj.Constraints[i];_VisitTypeReference(ctr,args.Set(args.Root,obj,"Constraints",i,_BuildPath(args.Path,"Constraints",i),ctr,args.Targets),action);
if(args.Cancel)return;}}}static void _VisitTypeReference(CodeTypeReference obj,CodeDomVisitContext args,CodeDomVisitAction action){if(!_HasTarget(args,
CodeDomVisitTargets.TypeRefs))return;if(args.Cancel)return; action(args);if(args.Cancel)return;if(null!=obj.ArrayElementType&&_HasTarget(args,CodeDomVisitTargets.TypeRefs))
_VisitTypeReference(obj.ArrayElementType,args.Set(args.Root,obj,"ArrayElementType",-1,_BuildPath(args.Path,"ArrayElementType",-1),obj.ArrayElementType,args.Targets),
action);if(args.Cancel)return;if(_HasTarget(args,CodeDomVisitTargets.TypeRefs)){for(int ic=obj.TypeArguments.Count,i=0;i<ic;++i){var ctr=obj.TypeArguments[i];
_VisitTypeReference(ctr,args.Set(args.Root,obj,"TypeArguments",i,_BuildPath(args.Path,"TypeArguments",i),ctr,args.Targets),action);if(args.Cancel)return;
}}}static string _BuildPath(string path,string member,int index){if(string.IsNullOrEmpty(path))path=member;else path=string.Concat(path,".",member);if
(-1!=index)path=string.Concat(path,"[",index.ToString(),"]");return path;}/// <summary>
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
}return result;}}}namespace CD{class ConcatEnumerator<T>:IEnumerator<T>{const int _Enumerating=0;const int _NotStarted=-2;const int _Ended=-1;const int
 _Disposed=-3;int _state;IEnumerator<IEnumerable<T>>_collections;IEnumerator<T>_e;public ConcatEnumerator(IEnumerable<IEnumerable<T>>collections){_collections
=collections.GetEnumerator();_state=_NotStarted;_e=null;}public ConcatEnumerator(params IEnumerable<T>[]collections):this((IEnumerable<IEnumerable<T>>)collections)
{}public T Current{get{switch(_state){case _NotStarted:throw new InvalidOperationException("The cursor is before the start of the enumeration.");case _Ended:
throw new InvalidOperationException("The cursor is after the end of the enumeration.");case _Disposed:throw new ObjectDisposedException(GetType().Name);
}return _e.Current;}} object System.Collections.IEnumerator.Current=>Current;public bool MoveNext(){switch(_state){case _Disposed:throw new ObjectDisposedException(GetType().Name);
case _Ended:return false;case _NotStarted:return _MoveToNextEnum();} if(!_e.MoveNext())return _MoveToNextEnum();return true;}bool _MoveToNextEnum(){var
 found=false;while(_collections.MoveNext()){var e=_collections.Current.GetEnumerator();if(e.MoveNext()){found=true;if(null!=_e)_e.Dispose();_e=e;break;
}}if(!found){_state=_Ended;return false;}_state=_Enumerating;return true;}public void Reset(){switch(_state){case _Disposed:throw new ObjectDisposedException(GetType().Name);
case _NotStarted:return;}_collections.Reset();if(null!=_e)_e.Dispose();}public void Dispose(){_state=_Disposed;if(null!=_e)_e.Dispose();_collections.Dispose();
}}}namespace CD{/// <summary>
/// An enumerator that provides lookahead without advancing the cursor
/// </summary>
/// <typeparam name="T">The type to enumerate</typeparam>
class LookAheadEnumerator<T>:IEnumerator<T>{const int _Enumerating=0;const int _NotStarted=-2;const int _Ended=-1;const int _Disposed=-3;IEnumerator<T>
_inner;int _state; const int _DefaultCapacity=16;const float _GrowthFactor=.9f;T[]_queue;int _queueHead;int _queueCount;public LookAheadEnumerator(IEnumerator<T>
inner){_inner=inner;_state=_NotStarted;_queue=new T[_DefaultCapacity];_queueHead=0;_queueCount=0;}public void DiscardLookAhead(){while(1<_queueCount)_Dequeue();
}public T Current{get{switch(_state){case _NotStarted:throw new InvalidOperationException("The cursor is before the start of the enumeration.");case _Ended:
throw new InvalidOperationException("The cursor is after the end of the enumeration.");case _Disposed:throw new ObjectDisposedException(GetType().Name);
}return _queue[_queueHead];}} object IEnumerator.Current{get{return Current;}}internal int QueueCount{get{return _queueCount;}}public bool TryPeek(int
 lookahead,out T value){if(_Disposed==_state)throw new ObjectDisposedException(GetType().Name);if(0>lookahead)throw new ArgumentOutOfRangeException(nameof(lookahead));
if(_Ended==_state){value=default(T);return false;}if(_NotStarted==_state){if(0==lookahead){value=default(T);return false;}}if(lookahead<_queueCount){value
=_queue[(lookahead+_queueHead)%_queue.Length];return true;}lookahead-=_queueCount;value=default(T);while(0<lookahead&&_inner.MoveNext()){value=_inner.Current;
_Enqueue(value);--lookahead;}return 0==lookahead;}public T Peek(int lookahead){T value;if(!TryPeek(lookahead,out value))throw new InvalidOperationException("There were not enough values in the enumeration to satisfy the request");
return value;}public IEnumerable<T>LookAhead{get{return new LookAheadEnumeratorEnumerable<T>(this);}}public bool MoveNext(){switch(_state){case _Disposed:
throw new ObjectDisposedException(GetType().Name);case _Ended:return false;case _NotStarted:if(0<_queueCount){_state=_Enumerating;return true;}if(!_inner.MoveNext())
{_state=_Ended;return false;}else{ _Enqueue(_inner.Current);_state=_Enumerating;return true;}default: _Dequeue();if(0==_queueCount){if(!_inner.MoveNext())
{_state=_Ended;return false;}_Enqueue(_inner.Current);}return true;}}public void Reset(){_inner.Reset();if(0<_queueCount&&null==default(T)){Array.Clear(_queue,
_queueHead,_queue.Length-_queueHead);if(_queueHead+_queueCount>=_queue.Length)Array.Clear(_queue,0,_queueHead+_queueCount%_queue.Length);}_queueHead=0;
_queueCount=0;_state=_NotStarted;}
#region IDisposable Support
public void Dispose(){if(_Disposed!=_state){_inner.Dispose();_state=_Disposed;GC.SuppressFinalize(this);}}void _Enqueue(T item){if(_queueCount==_queue.Length)
{var arr=new T[(int)(_queue.Length*_GrowthFactor)];if(_queueHead+_queueCount<=_queue.Length){Array.Copy(_queue,arr,_queueCount);_queueHead=0;arr[_queueCount]
=item;++_queueCount;_queue=arr;}else{Array.Copy(_queue,_queueHead,arr,0,_queue.Length-_queueHead);Array.Copy(_queue,0,arr,_queue.Length-_queueHead,_queueHead);
_queueHead=0;arr[_queueCount]=item;++_queueCount;_queue=arr;}}else{_queue[(_queueHead+_queueCount)%_queue.Length]=item;++_queueCount;}}T _Dequeue(){if
(0==_queueCount)throw new InvalidOperationException("The queue is empty");var result=_queue[_queueHead]; _queue[_queueHead]=default(T);++_queueHead;_queueHead
=_queueHead%_queue.Length;--_queueCount;return result;}~LookAheadEnumerator(){Dispose();}
#endregion
}class LookAheadEnumeratorEnumerable<T>:IEnumerable<T>{LookAheadEnumerator<T>_outer;public LookAheadEnumeratorEnumerable(LookAheadEnumerator<T>outer){
_outer=outer;}public IEnumerator<T>GetEnumerator(){return new LookAheadEnumeratorEnumerator<T>(_outer);}IEnumerator IEnumerable.GetEnumerator(){return
 GetEnumerator();}}class LookAheadEnumeratorEnumerator<T>:IEnumerator<T>{const int _NotStarted=-2;const int _Ended=-1;const int _Disposed=-3;LookAheadEnumerator<T>
_outer;int _index;T _current;public LookAheadEnumeratorEnumerator(LookAheadEnumerator<T>outer){_outer=outer;_index=_NotStarted;}public T Current{get{if
(0>_index){if(_index==_NotStarted)throw new InvalidOperationException("The cursor is before the start of the enumeration.");if(_index==_Ended)throw new
 InvalidOperationException("The cursor is after the end of the enumeration.");throw new ObjectDisposedException(GetType().Name);}return _current;}}object
 IEnumerator.Current{get{return Current;}}public void Dispose(){_index=_Disposed;}public bool MoveNext(){if(0>_index){if(_index==_Disposed)throw new ObjectDisposedException(GetType().Name);
if(_index==_Ended)return false;_index=0;}T value;++_index;if(!_outer.TryPeek(_index,out value)){_index=_Ended;return false;}_current=value;return true;
}public void Reset(){_index=_NotStarted;}}}namespace CD{using ST=SlangTokenizer;partial class SlangParser{/// <summary>
/// Reads a <see cref="CodeCompileUnit"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
public static CodeCompileUnit ReadCompileUnitFrom(TextReader reader)=>ParseCompileUnit(TextReaderEnumerable.FromReader(reader));/// <summary>
/// Reads a <see cref="CodeCompileUnit"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
public static CodeCompileUnit ReadCompileUnitFrom(string filename)=>ParseCompileUnit(new FileReaderEnumerable(filename));/// <summary>
/// Reads a <see cref="CodeCompileUnit"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
public static CodeCompileUnit ReadCompileUnitFromUrl(string url)=>ParseCompileUnit(new UrlReaderEnumerable(url));/// <summary>
/// Parses a <see cref="CodeCompileUnit"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
public static CodeCompileUnit ParseCompileUnit(IEnumerable<char>input){using(var e=new ST(input).GetEnumerator()){var pc=new _PC(e);pc.EnsureStarted();
var result=_ParseCompileUnit(pc); if(!pc.IsEnded)throw new SlangSyntaxException("Unrecognized remainder in compile unit",pc.Current.Line,pc.Current.Column,pc.Current.Position);
return result;}}static CodeCompileUnit _ParseCompileUnit(_PC pc){var result=new CodeCompileUnit();while(!pc.IsEnded)result.Namespaces.Add(_ParseNamespace(pc));
return result;}}}namespace CD{using ST=SlangTokenizer;/// <summary>
/// Represents the parser for parsing Slang, a C# subset, into a CodeDOM structure
/// </summary>
#if GOKITLIB
public
#endif
static partial class SlangParser{ private class _PC{readonly LookAheadEnumerator<Token>_e;int _state;public _PC(IEnumerator<Token>e){_e=new LookAheadEnumerator<Token>(e);
_state=-2;}public _PC GetLookAhead(){var e=new ConcatEnumerator<Token>(new Token[]{_e.Current},_e.LookAhead);return new _PC(e);}public int SymbolId=>Current.SymbolId;
public string Value=>Current.Value;public Token Current{get{if(0>_state){var t=default(Token);t.SymbolId=-1==_state?-2:-1;return t;}return _e.Current;
}}public void EnsureStarted(){if(-2==_state)Advance();}public bool IsEnded{get{return-1==_state;}}public bool Advance(){if(!_e.MoveNext())_state=-1;else
{_state=0;return true;}return false;}}static void _SkipComments(_PC pc){Token t;while((ST.blockComment==(t=pc.Current).SymbolId||ST.lineComment==t.SymbolId)
&&pc.Advance());}[System.Diagnostics.DebuggerNonUserCode()]static void _Error(string message,Token tok){throw new SlangSyntaxException(message,tok.Line,
tok.Column,tok.Position);}}}namespace CD{using ST=SlangTokenizer;partial class SlangParser{/// <summary>
/// Reads a <see cref="CodeExpression"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
public static CodeExpression ReadExpressionFrom(TextReader reader)=>ParseExpression(TextReaderEnumerable.FromReader(reader));/// <summary>
/// Reads a <see cref="CodeExpression"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
public static CodeExpression ReadExpressionFrom(string filename)=>ParseExpression(new FileReaderEnumerable(filename));/// <summary>
/// Reads a <see cref="CodeExpression"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
public static CodeExpression ReadExpressionFromUrl(string url)=>ParseExpression(new UrlReaderEnumerable(url));/// <summary>
/// Parses a <see cref="CodeExpression"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <returns>A <see cref="CodeExpression"/> representing the parsed code</returns>
public static CodeExpression ParseExpression(IEnumerable<char>input){using(var e=new ST(input).GetEnumerator()){var pc=new _PC(e);pc.EnsureStarted();var
 result=_ParseExpression(pc);if(!pc.IsEnded)throw new SlangSyntaxException("Unrecognized remainder in expression",pc.Current.Line,pc.Current.Column,pc.Current.Position);
return result;}}static CodeExpression _ParseExpression(_PC pc){return _ParseAssignment(pc);}static CodeExpression _ParseMemberRef(_PC pc){var lhs=_ParseUnary(pc);
_SkipComments(pc);while(true){switch(pc.SymbolId){case ST.dot:if(!pc.Advance())_Error("Unterminated member reference",pc.Current);if(ST.identifier!=pc.SymbolId)
_Error(string.Format("Invalid token {0} found in member reference",pc.Value),pc.Current);var fr=new CodeFieldReferenceExpression(lhs,pc.Value);fr.UserData.Add("slang:unresolved",
true);lhs=fr;pc.Advance();break;case ST.lbracket:var exprs=_ParseArguments(pc,ST.rbracket,false);var ie=new CodeIndexerExpression(lhs);ie.Indices.AddRange(exprs);
ie.UserData.Add("slang:unresolved",true);lhs=ie;break;case ST.lparen:exprs=_ParseArguments(pc,ST.rparen,true);var di=new CodeDelegateInvokeExpression(lhs);
di.Parameters.AddRange(exprs);di.UserData.Add("slang:unresolved",true);lhs=di;break;default:return lhs;}}}static CodeExpression _ParseFactor(_PC pc){var
 lhs=_ParseMemberRef(pc);while(true){var op=default(CodeBinaryOperatorType);_SkipComments(pc);switch(pc.SymbolId){case ST.mul:op=CodeBinaryOperatorType.Multiply;
break;case ST.div:op=CodeBinaryOperatorType.Divide;break;case ST.mod:op=CodeBinaryOperatorType.Modulus;break;default:return lhs;}pc.Advance();var rhs=
_ParseMemberRef(pc);lhs=new CodeBinaryOperatorExpression(lhs,op,rhs);}}static CodeExpression _ParseTerm(_PC pc){var lhs=_ParseFactor(pc);while(true){var
 op=default(CodeBinaryOperatorType);_SkipComments(pc);switch(pc.SymbolId){case ST.add:op=CodeBinaryOperatorType.Add;break;case ST.sub:op=CodeBinaryOperatorType.Subtract;
break;default:return lhs;}pc.Advance();var rhs=_ParseFactor(pc);lhs=new CodeBinaryOperatorExpression(lhs,op,rhs);}}static CodeExpression _ParseAssignment(_PC
 pc){var lhs=_ParseBooleanOr(pc);while(true){var op=default(CodeBinaryOperatorType);var assign=false;_SkipComments(pc);switch(pc.SymbolId){case ST.eq:
op=CodeBinaryOperatorType.Assign;break;case ST.addAssign:assign=true;op=CodeBinaryOperatorType.Add;break;case ST.subAssign:assign=true;op=CodeBinaryOperatorType.Subtract;
break;case ST.mulAssign:assign=true;op=CodeBinaryOperatorType.Multiply;break;case ST.modAssign:assign=true;op=CodeBinaryOperatorType.Modulus;break;case
 ST.divAssign:assign=true;op=CodeBinaryOperatorType.Divide;break;case ST.bitwiseOrAssign:assign=true;op=CodeBinaryOperatorType.BitwiseOr;break;case ST.bitwiseAndAssign:
assign=true;op=CodeBinaryOperatorType.BitwiseAnd;break;default:return lhs;}pc.Advance();var rhs=_ParseBooleanOr(pc);if(assign){lhs=new CodeBinaryOperatorExpression(lhs,
CodeBinaryOperatorType.Assign,new CodeBinaryOperatorExpression(lhs,op,rhs));}else lhs=new CodeBinaryOperatorExpression(lhs,op,rhs);}}static CodeExpression
 _ParseBooleanOr(_PC pc){var lhs=_ParseBooleanAnd(pc);while(true){var op=default(CodeBinaryOperatorType);_SkipComments(pc);switch(pc.SymbolId){case ST.or:
op=CodeBinaryOperatorType.BooleanOr;break;default:return lhs;}pc.Advance();var rhs=_ParseBooleanAnd(pc);lhs=new CodeBinaryOperatorExpression(lhs,op,rhs);
}}static CodeExpression _ParseBooleanAnd(_PC pc){var lhs=_ParseBitwiseOr(pc);while(true){var op=default(CodeBinaryOperatorType);_SkipComments(pc);switch
(pc.SymbolId){case ST.and:op=CodeBinaryOperatorType.BooleanAnd;break;default:return lhs;}pc.Advance();var rhs=_ParseBitwiseOr(pc);lhs=new CodeBinaryOperatorExpression(lhs,
op,rhs);}}static CodeExpression _ParseBitwiseOr(_PC pc){var lhs=_ParseBitwiseAnd(pc);while(true){var op=default(CodeBinaryOperatorType);_SkipComments(pc);
switch(pc.SymbolId){case ST.bitwiseOr:op=CodeBinaryOperatorType.BitwiseOr;break;default:return lhs;}pc.Advance();var rhs=_ParseBitwiseAnd(pc);lhs=new CodeBinaryOperatorExpression(lhs,
op,rhs);}}static CodeExpression _ParseBitwiseAnd(_PC pc){var lhs=_ParseEquality(pc);while(true){var op=default(CodeBinaryOperatorType);_SkipComments(pc);
switch(pc.SymbolId){case ST.bitwiseAnd:op=CodeBinaryOperatorType.BitwiseAnd;break;default:return lhs;}pc.Advance();var rhs=_ParseEquality(pc);lhs=new CodeBinaryOperatorExpression(lhs,
op,rhs);}}static CodeExpression _ParseEquality(_PC pc){var lhs=_ParseRelational(pc);while(true){var op=true;_SkipComments(pc);switch(pc.SymbolId){case
 ST.eqEq:op=true;break;case ST.notEq:op=false;break;default:return lhs;}pc.Advance();var rhs=_ParseRelational(pc);if(op)lhs=new CodeBinaryOperatorExpression(lhs,
CodeBinaryOperatorType.IdentityEquality,rhs);else lhs=new CodeBinaryOperatorExpression(lhs,CodeBinaryOperatorType.IdentityInequality,rhs); lhs.UserData.Add("slang:unresolved",true);
}}static CodeExpression _ParseRelational(_PC pc){var lhs=_ParseTerm(pc);while(true){var op=default(CodeBinaryOperatorType);_SkipComments(pc);switch(pc.SymbolId)
{case ST.lt:op=CodeBinaryOperatorType.LessThan;break;case ST.gt:op=CodeBinaryOperatorType.GreaterThan;break;case ST.lte:op=CodeBinaryOperatorType.LessThanOrEqual;
break;case ST.gte:op=CodeBinaryOperatorType.GreaterThanOrEqual;break;default:return lhs;}pc.Advance();var rhs=_ParseTerm(pc);lhs=new CodeBinaryOperatorExpression(lhs,
op,rhs);}}static CodeCastExpression _ParseCast(_PC pc){ pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated cast or subexpression.",pc.Current);
var ctr=_ParseTypeRef(pc);_SkipComments(pc);if(ST.rparen!=pc.SymbolId)_Error("Unterminated cast or subexpression.",pc.Current);pc.Advance();var expr=_ParseExpression(pc);
return new CodeCastExpression(ctr,expr);}static CodeExpression _ParseUnary(_PC pc){_SkipComments(pc);switch(pc.SymbolId){case ST.inc:pc.Advance();var rhs
=_ParseLeaf(pc);return new CodeBinaryOperatorExpression(rhs,CodeBinaryOperatorType.Assign,new CodeBinaryOperatorExpression(rhs,CodeBinaryOperatorType.Add,
new CodePrimitiveExpression(1)));case ST.dec:pc.Advance();rhs=_ParseLeaf(pc);return new CodeBinaryOperatorExpression(rhs,CodeBinaryOperatorType.Assign,
new CodeBinaryOperatorExpression(rhs,CodeBinaryOperatorType.Subtract,new CodePrimitiveExpression(1)));case ST.add:pc.Advance();return _ParseUnary(pc);
case ST.sub:pc.Advance();rhs=_ParseUnary(pc); var pe=rhs as CodePrimitiveExpression;if(null!=pe){if(pe.Value is int)return new CodePrimitiveExpression(-(int)pe.Value);
if(pe.Value is long)return new CodePrimitiveExpression(-(long)pe.Value);if(pe.Value is short)return new CodePrimitiveExpression(-(short)pe.Value);if(pe.Value
 is sbyte)return new CodePrimitiveExpression(-(sbyte)pe.Value);if(pe.Value is float)return new CodePrimitiveExpression(-(float)pe.Value);if(pe.Value is
 double)return new CodePrimitiveExpression(-(double)pe.Value);if(pe.Value is decimal)return new CodePrimitiveExpression(-(decimal)pe.Value);}return new
 CodeBinaryOperatorExpression(new CodePrimitiveExpression(0),CodeBinaryOperatorType.Subtract,rhs);case ST.not:pc.Advance();rhs=_ParseExpression(pc);return
 new CodeBinaryOperatorExpression(new CodePrimitiveExpression(false),CodeBinaryOperatorType.ValueEquality,rhs);case ST.lparen: CodeExpression expr=null;
var pc2=pc.GetLookAhead();pc2.EnsureStarted();try{expr=_ParseCast(pc2);}catch{}if(null!=expr){ return _ParseCast(pc);}else{try{if(!pc.Advance())_Error("Unterminated cast or subexpression",
pc.Current);expr=_ParseExpression(pc);_SkipComments(pc);if(ST.rparen!=pc.SymbolId)_Error("Invalid cast or subexpression",pc.Current);pc.Advance();return
 expr;}catch(Exception eex){throw eex;}}} return _ParseLeaf(pc);}static CodeExpression _ParseLeaf(_PC pc){CodeExpression e=null;_SkipComments(pc);switch
(pc.SymbolId){case ST.integerLiteral:e=_ParseInteger(pc);break;case ST.floatLiteral:e=_ParseFloat(pc);break;case ST.stringLiteral:e=_ParseString(pc);break;
case ST.characterLiteral:e=_ParseChar(pc);break;case ST.keyword:switch(pc.Value){case"true":e=new CodePrimitiveExpression(true);pc.Advance();break;case
"false":e=new CodePrimitiveExpression(false);pc.Advance();break;case"null":e=new CodePrimitiveExpression(null);pc.Advance();break;case"this":e=new CodeThisReferenceExpression();
pc.Advance();break;case"base":e=new CodeBaseReferenceExpression();pc.Advance();break;case"typeof":e=_ParseTypeOf(pc);break;case"default":e=_ParseDefault(pc);
break;case"new":if(!pc.Advance())_Error("Unterminated new expression.",pc.Current);var ctr=_ParseTypeRef(pc,true);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated new expression.",
pc.Current);switch(pc.SymbolId){case ST.lparen:var exprs=_ParseArguments(pc,ST.rparen,false);var ce=new CodeObjectCreateExpression(ctr);ce.Parameters.AddRange(exprs);
e=ce; e.UserData.Add("slang:unresolved",true);break;case ST.lbracket: e=_ParseArrayCreatePart(ctr,pc);break;default:_Error(string.Format("Unrecognized token {0} in new expression",
pc.Value),pc.Current);break;}break;case"bool":case"char":case"string":case"byte":case"sbyte":case"short":case"ushort":case"int":case"uint":case"long":
case"ulong":case"float":case"double":case"decimal":e=new CodeTypeReferenceExpression(_TranslateIntrinsicType(pc.Value,pc));pc.Advance();break;default:
_Error(string.Format("Unexpected keyword {0} found in expression",pc.Value),pc.Current);break;}break;case ST.identifier:e=new CodeVariableReferenceExpression(pc.Value);
e.UserData.Add("slang:unresolved",true);pc.Advance();_SkipComments(pc); break;}if(null==e)_Error(string.Format("Unexpected token {0} in input",pc.Value),
pc.Current);return e;}static CodeArrayCreateExpression _ParseArrayCreatePart(CodeTypeReference type,_PC pc){ var result=new CodeArrayCreateExpression();
var mods=new List<int>();var arrType=type;_SkipComments(pc);if(!pc.Advance())_Error("Unterminated array create expression",pc.Current);_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated array create expression",pc.Current);var done=false;CodeExpression expr=null; while(!done){switch(pc.SymbolId){case
 ST.comma:throw new NotSupportedException("Multidimensional arrays are not fully supported. Consider using nested arrays in the alternative."); case ST.rbracket:
pc.Advance();_SkipComments(pc);done=true;break;default: if(null!=expr) throw new NotSupportedException("Multidimensional arrays are not fully supported. Consider using nested arrays in the alternative.");
expr=_ParseExpression(pc);_SkipComments(pc);break;}}var ctr=new CodeTypeReference();ctr.ArrayElementType=type;ctr.ArrayRank=1; if(ST.lbracket==pc.SymbolId)
{ctr=_ParseArrayTypeModifiers(ctr,pc);if(1<ctr.ArrayRank) throw new NotSupportedException("Multidimensional arrays are not fully supported. Consider using nested arrays.");
}result.CreateType=ctr;result.SizeExpression=expr;if(null==expr){if(pc.IsEnded)_Error("Expecting an array initializer in the array create expression",
pc.Current);if(ST.lbrace==pc.SymbolId){if(!pc.Advance())_Error("Unterminated array create initializer",pc.Current);while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)
{_SkipComments(pc);result.Initializers.Add(_ParseExpression(pc));_SkipComments(pc);if(ST.comma==pc.SymbolId)pc.Advance();}if(pc.IsEnded)_Error("Unterminated array create initializer",
pc.Current);pc.Advance();}}return result;}static CodeExpression _ParseTypeOf(_PC pc){CodeExpression e;if(!pc.Advance())_Error("Unterminated typeof expression",
pc.Current);_SkipComments(pc);if(ST.lparen!=pc.SymbolId||!pc.Advance())_Error("Unterminated typeof expression",pc.Current);_SkipComments(pc);var ctr=_ParseTypeRef(pc);
_SkipComments(pc);if(ST.rparen!=pc.SymbolId)_Error("Unterminated typeof expression",pc.Current);pc.Advance();e=new CodeTypeOfExpression(ctr);return e;
}static CodeExpression _ParseDefault(_PC pc){CodeExpression e;if(!pc.Advance())_Error("Unterminated default() expression",pc.Current);_SkipComments(pc);
if(ST.lparen!=pc.SymbolId||!pc.Advance())_Error("Unterminated default() expression",pc.Current);_SkipComments(pc);var ctr=_ParseTypeRef(pc);_SkipComments(pc);
if(ST.rparen!=pc.SymbolId)_Error("Unterminated default expression",pc.Current);pc.Advance();e=new CodeDefaultValueExpression(ctr);return e;}static CodeExpression
 _ParseTerm(CodeExpression lhs,_PC pc){while(true){var op=default(CodeBinaryOperatorType);_SkipComments(pc);switch(pc.SymbolId){case ST.add:op=CodeBinaryOperatorType.Add;
break;case ST.sub:op=CodeBinaryOperatorType.Subtract;break;default:return lhs;}pc.Advance();var rhs=_ParseFactor(pc);lhs=new CodeBinaryOperatorExpression(lhs,
op,rhs);}}static CodeExpressionCollection _ParseArguments(_PC pc,int endSym=ST.rparen,bool allowDirection=true){var result=new CodeExpressionCollection();
if(!pc.Advance())_Error("Unterminated argument list",pc.Current);while(endSym!=pc.SymbolId){var fd=default(FieldDirection);if(allowDirection&&ST.keyword
==pc.SymbolId){if("in"==pc.Value){fd=FieldDirection.In;if(!pc.Advance())_Error("Unterminated method invocation.",pc.Current);}else if("out"==pc.Value)
{fd=FieldDirection.Out;if(!pc.Advance())_Error("Unterminated method invocation.",pc.Current);}else if("ref"==pc.Value){fd=FieldDirection.Ref;if(!pc.Advance())
_Error("Unterminated method invocation.",pc.Current);}_SkipComments(pc);}var exp=_ParseExpression(pc);if(fd!=FieldDirection.In)exp=new CodeDirectionExpression(fd,
exp);result.Add(exp);_SkipComments(pc);if(ST.comma==pc.SymbolId){if(!pc.Advance())_Error("Unterminated argument list.",pc.Current);}}if(endSym!=pc.SymbolId)
{_Error("Unterminated argument list.",pc.Current);}pc.Advance();return result;}
#region Parse Primitives
static CodePrimitiveExpression _ParseString(_PC pc){var sb=new StringBuilder();var e=pc.Value.GetEnumerator();var more=pc.Advance();e.MoveNext();if(e.MoveNext())
{while(true){if('\"'==e.Current)return new CodePrimitiveExpression(sb.ToString());else if('\\'==e.Current)sb.Append(_ParseEscapeChar(e,pc.Current));else
{sb.Append(e.Current);if(!e.MoveNext())break;}}}_Error("Unterminated string in input",pc.Current);return null;}static CodePrimitiveExpression _ParseChar(_PC
 pc){var s=pc.Value;pc.Advance(); s=s.Substring(1,s.Length-2);var e=s.GetEnumerator();e.MoveNext();if('\\'==e.Current){s=_ParseEscapeChar(e,pc.Current);
if(1==s.Length)return new CodePrimitiveExpression(s[0]);else return new CodePrimitiveExpression(s);}return new CodePrimitiveExpression(s[0]);}static CodePrimitiveExpression
 _ParseFloat(_PC pc){var s=pc.Value;pc.Advance();var ch=char.ToLowerInvariant(s[s.Length-1]);var isDouble='d'==ch;var isDecimal='m'==ch;var isFloat='f'
==ch;if((isDouble||isDecimal||isFloat))s=s.Substring(0,s.Length-1);else isDouble=true;object n=null;if(isFloat)n=float.Parse(s);else if(isDecimal)n=decimal.Parse(s);
else n=double.Parse(s);return new CodePrimitiveExpression(n);}static CodePrimitiveExpression _ParseInteger(_PC pc){var s=pc.Value;pc.Advance();var isLong
=false;var isUnsigned=false;var isNeg='-'==s[0];var isHex=s.StartsWith("-0x")||s.StartsWith("0x");var ch=char.ToLowerInvariant(s[s.Length-1]);if('l'==
ch){isLong=true;s=s.Substring(0,s.Length-1);}else if('u'==ch){isUnsigned=true;s=s.Substring(0,s.Length-1);} ch=char.ToLowerInvariant(s[s.Length-1]);if
('l'==ch){isLong=true;s=s.Substring(0,s.Length-1);}else if('u'==ch){isUnsigned=true;s=s.Substring(0,s.Length-1);} if(isHex)s=s.Substring(2);var d=(double)long.Parse(s,
isHex?NumberStyles.AllowHexSpecifier:NumberStyles.Integer);object n=null;if(isUnsigned&&(isLong||(d<=uint.MaxValue&&d>=uint.MinValue))){if(isNeg){if(!isHex)
n=unchecked((ulong)long.Parse(s));else n=unchecked((ulong)-long.Parse(s.Substring(1),NumberStyles.AllowHexSpecifier));}else n=ulong.Parse(s,isHex?NumberStyles.AllowHexSpecifier
:NumberStyles.Integer);}else if(isUnsigned){if(isNeg){if(!isHex)n=unchecked((uint)int.Parse(s));else n=unchecked((uint)-int.Parse(s.Substring(1),NumberStyles.AllowHexSpecifier));
}else n=uint.Parse(s,isHex?NumberStyles.AllowHexSpecifier:NumberStyles.Integer);}else{if(isNeg){if(!isHex)n=int.Parse(s);else n=unchecked(-int.Parse(s.Substring(1),
NumberStyles.AllowHexSpecifier));}else n=int.Parse(s,isHex?NumberStyles.AllowHexSpecifier:NumberStyles.Integer);}return new CodePrimitiveExpression(n);
}
#endregion
#region String/Char escapes
static string _ParseEscapeChar(IEnumerator<char>e,Token tok){if(e.MoveNext()){switch(e.Current){case'r':e.MoveNext();return"\r";case'n':e.MoveNext();return
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
e.Current));}}_Error("Unterminated escape sequence",tok);return null;}static bool _IsHexChar(char hex){return(':'>hex&&'/'<hex)||('G'>hex&&'@'<hex)||('g'
>hex&&'`'<hex);}static byte _FromHexChar(char hex){if(':'>hex&&'/'<hex)return(byte)(hex-'0');if('G'>hex&&'@'<hex)return(byte)(hex-'7'); if('g'>hex&&'`'
<hex)return(byte)(hex-'W'); throw new ArgumentException("The value was not hex.","hex");}
#endregion
}}namespace CD{using ST=SlangTokenizer;partial class SlangParser{/// <summary>
/// Reads a <see cref="CodeTypeMember"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>
public static CodeTypeMember ReadMemberFrom(TextReader reader)=>ParseMember(TextReaderEnumerable.FromReader(reader));/// <summary>
/// Reads a <see cref="CodeTypeMember"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>
public static CodeTypeMember ReadMemberFrom(string filename)=>ParseMember(new FileReaderEnumerable(filename));/// <summary>
/// Reads a <see cref="CodeTypeMember"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>
public static CodeTypeMember ReadMemberFromUrl(string url)=>ParseMember(new UrlReaderEnumerable(url));/// <summary>
/// Parses a <see cref="CodeTypeMember"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <returns>A <see cref="CodeTypeMember"/> representing the parsed code</returns>
public static CodeTypeMember ParseMember(IEnumerable<char>input){using(var e=new ST(input).GetEnumerator()){var pc=new _PC(e);pc.EnsureStarted();var result
=_ParseMember(pc);if(!pc.IsEnded)throw new SlangSyntaxException("Unrecognized remainder in member",pc.Current.Line,pc.Current.Column,pc.Current.Position);
return result;}}static MemberAttributes _BuildMemberAttributes(ICollection<string>modifiers){var result=(MemberAttributes)0;foreach(var kw in modifiers)
{switch(kw){case"protected":if(modifiers.Contains("internal"))result=(result&~MemberAttributes.AccessMask)|MemberAttributes.FamilyOrAssembly;else result
=(result&~MemberAttributes.AccessMask)|MemberAttributes.Family;break;case"internal":if(modifiers.Contains("protected"))result=(result&~MemberAttributes.AccessMask)
|MemberAttributes.FamilyOrAssembly;else result=(result&~MemberAttributes.AccessMask)|MemberAttributes.FamilyAndAssembly;break;case"const":result=(result
&~MemberAttributes.ScopeMask)|MemberAttributes.Const;break;case"new":result=(result&~MemberAttributes.VTableMask)|MemberAttributes.New;break;case"override":
result=(result&~MemberAttributes.ScopeMask)|MemberAttributes.Override;break;case"public":if(modifiers.Contains("virtual"))result=(result&~MemberAttributes.AccessMask)
|MemberAttributes.Public;else{result=(result&~MemberAttributes.AccessMask)|MemberAttributes.Public;result=(result&~MemberAttributes.ScopeMask)|MemberAttributes.Final;
}break;case"private":result=(result&~MemberAttributes.AccessMask)|MemberAttributes.Private;break;case"abstract":result=(result&~MemberAttributes.ScopeMask)
|MemberAttributes.Abstract;break;case"static":result=(result&~MemberAttributes.ScopeMask)|MemberAttributes.Static;break;}}return result;}static ICollection<string>
_ParseMemberAttributes(_PC pc){var result=new HashSet<string>();_SkipComments(pc);while(ST.keyword==pc.SymbolId){switch(pc.Value){case"protected":if(result.Contains("public")
||result.Contains("private"))_Error("Conflicting access modifiers on member",pc.Current);break;case"internal":if(result.Contains("public")||result.Contains("private"))
_Error("Conflicting access modifiers on member",pc.Current);break;case"const":if(result.Contains("virtual")||result.Contains("override")||result.Contains("abstract"))
_Error("Conflicting access modifiers on member",pc.Current);break;case"virtual":if(result.Contains("const"))_Error("Conflicting access modifiers on member",
pc.Current);break;case"new":break;case"override":if(result.Contains("const"))_Error("Conflicting access modifiers on member",pc.Current);break;case"public":
if(result.Contains("protected")||result.Contains("internal")||result.Contains("private"))_Error("Conflicting access modifiers on member",pc.Current);break;
case"private":if(result.Contains("protected")||result.Contains("internal")||result.Contains("public"))_Error("Conflicting access modifiers on member",
pc.Current);break;case"abstract":if(result.Contains("const")||result.Contains("static")||result.Contains("private"))_Error("Conflicting access modifiers on member",
pc.Current);break;case"static":if(result.Contains("abstract"))_Error("Conflicting access modifiers on member",pc.Current);break;default:return result;
}if(!result.Add(pc.Value))_Error(string.Format("Duplicate member modifier {0} found",pc.Value),pc.Current);pc.Advance();_SkipComments(pc);}_SkipComments(pc);
return result;}static KeyValuePair<CodeTypeReference,string>_ParsePrivateImplementationType(_PC pc){ _SkipComments(pc);CodeTypeReference ptr=null;string
 name=null;_PC pc2=pc.GetLookAhead();pc2.EnsureStarted();try{ptr=_ParseTypeRef(pc2,true);}catch{ptr=null;}if(ptr!=null){if(ST.dot==pc2.SymbolId){_ParseTypeRef(pc);
pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated private member declaration",pc.Current);var s=pc.Value;pc.Advance();return new KeyValuePair<CodeTypeReference,
string>(ptr,s);} var idx=ptr.BaseType.LastIndexOfAny(new char[]{'.','+'});if(0>idx){pc.Advance();if(ST.dot!=pc.SymbolId){ if(0<ptr.TypeArguments.Count)
_Error("Missing member name on private member declaration",pc.Current);return new KeyValuePair<CodeTypeReference,string>(null,ptr.BaseType);}else{pc.Advance();
_SkipComments(pc);if(ST.keyword==pc.SymbolId&&"this"==pc.Value){pc.Advance();return new KeyValuePair<CodeTypeReference,string>(ptr,"this");}_Error("Illegal private member implementation type.",
pc.Current);}}name=ptr.BaseType.Substring(idx+1);ptr.BaseType=ptr.BaseType.Substring(0,idx);_ParseTypeRef(pc,false); return new KeyValuePair<CodeTypeReference,
string>(ptr,name);}var n=pc.Value;pc.Advance();return new KeyValuePair<CodeTypeReference,string>(null,n);}static CodeTypeMember _ParseMember(_PC pc,string
 typeName=null){var comments=new CodeCommentStatementCollection();var dirs=_ParseDirectives(pc);while(ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId)
{comments.Add(_ParseCommentStatement(pc));}dirs.AddRange(_ParseDirectives(pc));IList<KeyValuePair<string,CodeAttributeDeclaration>>customAttrs=null;if
(ST.lbracket==pc.SymbolId)customAttrs=_ParseCustomAttributes(pc);var attrs=_ParseMemberAttributes(pc);var isEvent=false;if(ST.keyword==pc.SymbolId&&("partial"==pc.Value
||"class"==pc.Value||"struct"==pc.Value||"enum"==pc.Value)){var ctd=_ParseType(pc,true);for(var i=comments.Count-1;0<=i;--i)ctd.Comments.Insert(0,comments[i]);
_AddStartDirs(ctd,dirs);return ctd;}if(ST.keyword==pc.SymbolId&&pc.Value=="event"){pc.Advance();_SkipComments(pc);isEvent=true;}else{var pc2=pc.GetLookAhead();
pc2.EnsureStarted();pc2.Advance();_SkipComments(pc2); if(ST.identifier==pc.SymbolId&&(string.IsNullOrEmpty(typeName)||typeName==pc.Value)&&ST.lparen==pc2.SymbolId)
{if(attrs.Contains("abstract"))_Error("Constructors cannot be abstract",pc.Current);if(attrs.Contains("const"))_Error("Constructors cannot be const",pc.Current);
 var ctorName=pc.Value;pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated constructor",pc.Current);if(ST.lparen!=pc.SymbolId)_Error("Expecting ( in constructor declaration",
pc.Current);pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated constructor",pc.Current);var parms=_ParseParamDecls(pc,ST.rparen,false);
CodeTypeMember mctor=null;_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated constructor",pc.Current);if(!attrs.Contains("static")){var ctor=new CodeConstructor();
mctor=ctor;ctor.Name=ctorName;ctor.Attributes=_BuildMemberAttributes(attrs);_AddStartDirs(ctor,dirs);_AddCustomAttributes(customAttrs,null,ctor.CustomAttributes);
ctor.Parameters.AddRange(parms);if(ST.colon==pc.SymbolId){pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated constructor - expecting chained or base constructor args",
pc.Current);if(ST.keyword==pc.SymbolId){switch(pc.Value){case"base":pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated constructor - expecting base constructor args",
pc.Current);if(ST.lparen!=pc.SymbolId)_Error("Expecting ( in base constructor args",pc.Current); if(pc.IsEnded)_Error("Unterminated constructor - expecting base constructor args",
pc.Current);ctor.BaseConstructorArgs.AddRange(_ParseArguments(pc,ST.rparen,false));break;case"this":pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated constructor - expecting chained constructor args",
pc.Current);if(ST.lparen!=pc.SymbolId)_Error("Expecting ( in chained constructor args",pc.Current); if(pc.IsEnded)_Error("Unterminated constructor - expecting chained constructor args",
pc.Current);ctor.ChainedConstructorArgs.AddRange(_ParseArguments(pc,ST.rparen,false));break;default:_Error("Expecting chained or base constructor call",
pc.Current);break;}}else _Error("Expecting chained or base constructor call",pc.Current);}_SkipComments(pc);if(pc.IsEnded||ST.lbrace!=pc.SymbolId)_Error("Expecting a constructor body",
pc.Current);pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)ctor.Statements.Add(_ParseStatement(pc,true));if(ST.rbrace!=pc.SymbolId)_Error("Unterminated method body",
pc.Current);pc.Advance();dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(ctor,dirs);}else{var ctor=new CodeTypeConstructor();mctor=ctor;ctor.Name
=ctorName;ctor.Attributes=_BuildMemberAttributes(attrs);_AddStartDirs(ctor,dirs);_AddCustomAttributes(customAttrs,null,ctor.CustomAttributes);if(0<parms.Count)
_Error("Type constructors cannot have parameters.",pc.Current);_SkipComments(pc);if(pc.IsEnded||ST.lbrace!=pc.SymbolId)_Error("Expecting a constructor body",
pc.Current);pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)ctor.Statements.Add(_ParseStatement(pc,true));if(ST.rbrace!=pc.SymbolId)_Error("Unterminated method body",
pc.Current);pc.Advance();}mctor.Comments.AddRange(comments);dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(mctor,dirs);return mctor;}} CodeTypeReference
 ctr=null; if(!(ST.keyword==pc.SymbolId&&"void"==pc.Value)){ctr=_ParseTypeRef(pc);}else pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated member declaration",
pc.Current);if(ST.identifier!=pc.SymbolId&&!(ST.keyword==pc.SymbolId&&"this"==pc.Value))_Error("Expecting identifier in member declaration",pc.Current);
 var kvp=_ParsePrivateImplementationType(pc);var name=kvp.Value;var ptr=kvp.Key;var isPriv=!(attrs.Contains("public")||attrs.Contains("protected")||attrs.Contains("internal"));
_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated member declaration",pc.Current);if(isEvent){if(ST.semi==pc.SymbolId){if(null==ctr)_Error("Events must not have a void type.",
pc.Current);var e=new CodeMemberEvent();e.Type=ctr;if(isPriv)e.PrivateImplementationType=ptr;e.Name=name;e.Attributes=_BuildMemberAttributes(attrs);_AddCustomAttributes(customAttrs,
null,e.CustomAttributes);_AddStartDirs(e,dirs);if(attrs.Contains("public")){ e.UserData.Add("slang:unresolved",true);}pc.Advance();e.Comments.AddRange(comments);
dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(e,dirs);return e;}_Error(string.Format("Unexpected token {0} found in event.",pc.Value),pc.Current);
}if(ST.semi==pc.SymbolId){if(attrs.Contains("abstract"))_Error("Fields cannot be abstract.",pc.Current);if(null==ctr)_Error("Fields must not have a void type.",
pc.Current);var f=new CodeMemberField(ctr,name);f.Attributes=_BuildMemberAttributes(attrs);_AddCustomAttributes(customAttrs,null,f.CustomAttributes);_AddStartDirs(f,
dirs);if(null!=ptr)_Error("Fields cannot have a private implementation type.",pc.Current);pc.Advance();f.Comments.AddRange(comments);dirs.AddRange(_ParseDirectives(pc,
true));_AddEndDirs(f,dirs);return f;}else if(ST.eq==pc.SymbolId){if(null==ctr)_Error("Fields must not have a void type.",pc.Current);pc.Advance();_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated field initializer",pc.Current);var init=_ParseExpression(pc);if(ST.semi!=pc.SymbolId)_Error("Invalid expression in field initializer",
pc.Current);pc.Advance();var f=new CodeMemberField(ctr,name);f.Attributes=_BuildMemberAttributes(attrs);_AddCustomAttributes(customAttrs,null,f.CustomAttributes);
_AddStartDirs(f,dirs);f.InitExpression=init;if(null!=ptr)_Error("Fields cannot have a private implementation type.",pc.Current);f.Comments.AddRange(comments);
dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(f,dirs);return f;}else if(ST.lparen==pc.SymbolId){pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated method declaration",
pc.Current);var parms=_ParseParamDecls(pc);CodeMemberMethod m=new CodeMemberMethod();m.UserData.Add("slang:unresolved",true);m.ReturnType=ctr;m.Name=name;
m.Attributes=_BuildMemberAttributes(attrs);_AddCustomAttributes(customAttrs,null,m.CustomAttributes);_AddCustomAttributes(customAttrs,"return",m.ReturnTypeCustomAttributes);
_AddStartDirs(m,dirs);m.Parameters.AddRange(parms);if(isPriv)m.PrivateImplementationType=ptr;_SkipComments(pc);if(attrs.Contains("public")){}if(attrs.Contains("abstract"))
{if(ST.semi!=pc.SymbolId)_Error("Expecting ; to terminate abstract method definition",pc.Current);pc.Advance();m.Comments.AddRange(comments);return m;
}if(ST.lbrace!=pc.SymbolId)_Error("Expecting method body for non abstract method",pc.Current);pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId){
m.Statements.Add(_ParseStatement(pc,true));}if(ST.rbrace!=pc.SymbolId)_Error("Unterminated method body",pc.Current);pc.Advance();m.Comments.AddRange(comments);
dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(m,dirs);return m;}else{var p=new CodeMemberProperty();p.Type=ctr;p.Name=name;p.Attributes=_BuildMemberAttributes(attrs);
_AddCustomAttributes(customAttrs,null,p.CustomAttributes);_AddStartDirs(p,dirs);if(isPriv)p.PrivateImplementationType=ptr;else if(attrs.Contains("public"))
{ p.UserData.Add("slang:unresolved",true);}if(ST.lbracket==pc.SymbolId){pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated indexer property declaration",
pc.Current);else if(0!=string.Compare(name,"this"))_Error("Only indexer properties can have arguments",pc.Current);p.Parameters.AddRange(_ParseParamDecls(pc,
ST.rbracket));p.Name="Item";}if(ST.lbrace!=pc.SymbolId)_Error("Expecting body for property",pc.Current);pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated property body",
pc.Current);var sawGet=false;var sawSet=false;while(ST.rbrace!=pc.SymbolId){_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated property body",pc.Current);
if(ST.keyword!=pc.SymbolId)_Error("Expecting get or set in property body.",pc.Current);if("get"==pc.Value){if(sawGet)_Error("Multiple property.get definitions are not allowed.",
pc.Current);sawGet=true;pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated property.get",pc.Current);if(ST.lbrace==pc.SymbolId){if(attrs.Contains("abstract"))
_Error("Abstract properties must not contain get bodies.",pc.Current);pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)p.GetStatements.Add(_ParseStatement(pc,true));
if(ST.rbrace!=pc.SymbolId)_Error("Unterminated property.get body",pc.Current);pc.Advance();}else if(ST.semi==pc.SymbolId){if(!attrs.Contains("abstract"))
_Error("Non abstract property.gets must have a body.",pc.Current);pc.Advance();}}else if("set"==pc.Value){if(sawSet)_Error("Multiple property.set definitions are not allowed.",
pc.Current);sawSet=true;pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated property.set",pc.Current);if(ST.lbrace==pc.SymbolId){if(attrs.Contains("abstract"))
_Error("Abstract properties must not contain set bodies.",pc.Current);pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)p.SetStatements.Add(_ParseStatement(pc,true));
if(ST.rbrace!=pc.SymbolId)_Error("Unterminated property.set body",pc.Current);pc.Advance();}else if(ST.semi==pc.SymbolId){if(!attrs.Contains("abstract"))
_Error("Non abstract property.sets must have a body.",pc.Current);pc.Advance();}}else _Error(string.Format("Unrecognized keyword {0} in property body",
pc.Value),pc.Current);}if(ST.rbrace!=pc.SymbolId)_Error("Invalid property body",pc.Current);pc.Advance();p.Comments.AddRange(comments);dirs.AddRange(_ParseDirectives(pc,
true));_AddEndDirs(p,dirs);return p;}}static void _AddStartDirs(CodeTypeMember mem,IList<object>dirs){for(int ic=dirs.Count,i=0;i<ic;++i){var dir=dirs[i];
var l=dir as CodeLinePragma;if(null!=l){mem.LinePragma=l;dirs.RemoveAt(i);--i;--ic;continue;}var d=dir as CodeDirective;if(null!=d){mem.StartDirectives.Add(d);
dirs.RemoveAt(i);--i;--ic;}}}static void _AddEndDirs(CodeTypeMember mem,IList<object>dirs){for(int ic=dirs.Count,i=0;i<ic;++i){var dir=dirs[i];var d=dir
 as CodeDirective;if(null!=d){mem.EndDirectives.Add(d);dirs.RemoveAt(i);--i;--ic;}}}static void _AddCustomAttributes(IEnumerable<KeyValuePair<string,CodeAttributeDeclaration>>
src,string target,CodeAttributeDeclarationCollection dst){if(null!=src)foreach(var kvp in src)if(kvp.Key==target)dst.Add(kvp.Value);}static IList<KeyValuePair<string,
CodeAttributeDeclaration>>_ParseCustomAttributes(_PC pc){ var result=new List<KeyValuePair<string,CodeAttributeDeclaration>>();while(ST.lbracket==pc.SymbolId)
{foreach(var kvp in _ParseCustomAttributeGroup(pc))result.Add(kvp);_SkipComments(pc);}return result;}static IList<KeyValuePair<string,CodeAttributeDeclaration>>
_ParseCustomAttributeGroup(_PC pc){ pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated custom attribute declaration group",pc.Current);
var result=new List<KeyValuePair<string,CodeAttributeDeclaration>>();var target=pc.Value;var hasTarget=false;var pc2=pc.GetLookAhead();pc2.EnsureStarted();
pc2.Advance();_SkipComments(pc2);if(ST.colon==pc2.SymbolId){hasTarget=true;pc.Advance();_SkipComments(pc);pc.Advance();_SkipComments(pc);if(pc.IsEnded)
_Error("Unterminated custom attribute declaration group",pc.Current);}while(ST.rbracket!=pc.SymbolId){var attr=_ParseCustomAttribute(pc);_SkipComments(pc);
if(!hasTarget)result.Add(new KeyValuePair<string,CodeAttributeDeclaration>(null,attr));else result.Add(new KeyValuePair<string,CodeAttributeDeclaration>(target,
attr));if(pc.IsEnded)_Error("Unterminated custom attribute declaration group",pc.Current);if(ST.comma==pc.SymbolId){pc.Advance();_SkipComments(pc);if(pc.IsEnded)
_Error("Unterminated custom attribute declaration group",pc.Current);if(ST.rbracket==pc.SymbolId)_Error("Unexpected comma found in attribute declaration group",
pc.Current);}}if(ST.rbracket!=pc.SymbolId)_Error("Invalid custom attribute declaration",pc.Current);pc.Advance();_SkipComments(pc);if(0==result.Count)
_Error("Attribute groups must not be empty.",pc.Current);return result;}static CodeAttributeArgumentCollection _ParseCustomAttributeArguments(_PC pc){
var result=new CodeAttributeArgumentCollection();if(ST.lparen!=pc.SymbolId)return result;if(!pc.Advance())_Error("Unterminated argument list",pc.Current);
var named=false;while(ST.rparen!=pc.SymbolId){var arg=new CodeAttributeArgument();if(ST.identifier==pc.SymbolId){var s=pc.Value;var pc2=pc.GetLookAhead();
pc2.EnsureStarted();pc2.Advance();_SkipComments(pc2);if(ST.eq==pc2.SymbolId){pc.Advance();_SkipComments(pc);pc.Advance();arg.Name=s;arg.Value=_ParseExpression(pc);
result.Add(arg);named=true;continue;}}if(named)_Error("Named custom attribute arguments must follow the unnamed arguments.",pc.Current);var exp=_ParseExpression(pc);
_SkipComments(pc);arg.Value=exp;result.Add(arg);if(ST.comma==pc.SymbolId){if(!pc.Advance())_Error("Unterminated argument list.",pc.Current);}}if(ST.rparen!=
pc.SymbolId){_Error("Unterminated argument list.",pc.Current);}pc.Advance();return result;}static CodeAttributeDeclaration _ParseCustomAttribute(_PC pc)
{var ctr=_ParseTypeRef(pc);ctr.UserData.Add("slang:attribute",true);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated custom attribute declaration",pc.Current);
var exprs=_ParseCustomAttributeArguments(pc);var result=new CodeAttributeDeclaration(ctr);result.Arguments.AddRange(exprs);return result;}static CodeParameterDeclarationExpressionCollection
 _ParseParamDecls(_PC pc,int endSym=ST.rparen,bool allowDirection=true){var result=new CodeParameterDeclarationExpressionCollection();while(!pc.IsEnded&&endSym!=pc.SymbolId)
{var p=_ParseParamDecl(pc,allowDirection);result.Add(p);if(ST.comma!=pc.SymbolId)break;pc.Advance();_SkipComments(pc);}if(endSym!=pc.SymbolId)_Error("Unterminated parameter declarations",
pc.Current);pc.Advance();return result;} static CodeParameterDeclarationExpression _ParseParamDecl(_PC pc,bool allowDirection=true){var attrs=new CodeAttributeDeclarationCollection();
if(ST.lbracket==pc.SymbolId)_AddCustomAttributes(_ParseCustomAttributes(pc),null,attrs);FieldDirection d=FieldDirection.In;_SkipComments(pc);if(allowDirection)
{if(ST.keyword==pc.SymbolId){switch(pc.Value){case"out":d=FieldDirection.Out;pc.Advance();_SkipComments(pc);break;case"ref":d=FieldDirection.Ref;pc.Advance();
_SkipComments(pc);break;default:break;}}}var ctr=_ParseTypeRef(pc);_SkipComments(pc);if(ST.identifier!=pc.SymbolId)_Error("Expecting identifier in parameter declaration",
pc.Current);var result=new CodeParameterDeclarationExpression(ctr,pc.Value);result.Direction=d;if(null!=attrs)result.CustomAttributes.AddRange(attrs);
pc.Advance();return result;}}}namespace CD{using ST=SlangTokenizer;partial class SlangParser{/// <summary>
/// Reads a <see cref="CodeNamespace"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
public static CodeNamespace ReadNamespaceFrom(TextReader reader)=>ParseNamespace(TextReaderEnumerable.FromReader(reader));/// <summary>
/// Reads a <see cref="CodeNamespace"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
public static CodeNamespace ReadNamespaceFrom(string filename)=>ParseNamespace(new FileReaderEnumerable(filename));/// <summary>
/// Reads a <see cref="CodeNamespace"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
public static CodeNamespace ReadNamespaceFromUrl(string url)=>ParseNamespace(new UrlReaderEnumerable(url));/// <summary>
/// Parses a <see cref="CodeNamespace"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
public static CodeNamespace ParseNamespace(IEnumerable<char>input){using(var e=new ST(input).GetEnumerator()){var pc=new _PC(e);pc.EnsureStarted();var
 result=_ParseNamespace(pc);if(!pc.IsEnded)throw new SlangSyntaxException("Unrecognized remainder in namespace",pc.Current.Line,pc.Current.Column,pc.Current.Position);
return result;}}static CodeNamespace _ParseNamespace(_PC pc){ var result=new CodeNamespace();if(ST.keyword==pc.SymbolId&&"namespace"==pc.Value){pc.Advance();
_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated namespace declaration",pc.Current);if(ST.identifier!=pc.SymbolId)_Error("Expected identifier in namespace declaration",pc.Current);
result.Name=_ParseNamespaceName(pc);if(ST.lbrace!=pc.SymbolId)_Error("Expecting { in namespace declaration",pc.Current);pc.Advance();if(pc.IsEnded)_Error("Unterminated namespace declaration",
pc.Current); if(ST.lineComment==pc.SymbolId||ST.blockComment==pc.SymbolId){var pc2=pc.GetLookAhead();pc2.EnsureStarted();_SkipComments(pc2);if(ST.keyword
==pc2.SymbolId&&"using"==pc2.Value)_SkipComments(pc);}foreach(CodeNamespaceImport nsi in _ParseNamespaceImports(pc))result.Imports.Add(nsi);while(ST.rbrace
!=pc.SymbolId)result.Types.Add(_ParseType(pc));if(pc.IsEnded)_Error("Unterminated namespace declaration",pc.Current);if(ST.rbrace!=pc.SymbolId)_Error("Invalid type declaration in namespace",
pc.Current);pc.Advance();return result;}foreach(CodeNamespaceImport nsi in _ParseNamespaceImports(pc))result.Imports.Add(nsi);_SkipComments(pc);while(!pc.IsEnded
&&!(ST.keyword==pc.SymbolId&&"namespace"==pc.Value))result.Types.Add(_ParseType(pc));return result;}static CodeNamespaceImportCollection _ParseNamespaceImports(_PC
 pc){var result=new CodeNamespaceImportCollection();while(ST.keyword==pc.SymbolId&&"using"==pc.Value){pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated using declaration",
pc.Current);if(ST.identifier!=pc.SymbolId)_Error("Expecting identifier in using declaration",pc.Current);var ns=_ParseNamespaceName(pc);if(pc.IsEnded)
_Error("Unterminated using declaration",pc.Current);if(ST.semi!=pc.SymbolId)_Error("Expecting ; in using declaration",pc.Current);pc.Advance(); if(ST.lineComment
==pc.SymbolId||ST.blockComment==pc.SymbolId){var pc2=pc.GetLookAhead();pc2.EnsureStarted();_SkipComments(pc2);if(ST.keyword==pc2.SymbolId&&"using"==pc2.Value)
_SkipComments(pc);}result.Add(new CodeNamespaceImport(ns));}return result;}static string _ParseNamespaceName(_PC pc){var result="";while(ST.identifier==pc.SymbolId)
{if(0<result.Length)result=string.Concat(result,".",pc.Value);else result=pc.Value;pc.Advance();_SkipComments(pc);if(ST.dot==pc.SymbolId){pc.Advance();
_SkipComments(pc);}}return result;}}}namespace CD{using ST=SlangTokenizer;partial class SlangParser{/// <summary>
/// Reads a <see cref="CodeStatement"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <param name="includeComments">True to include comments, or false to skip them</param>
/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>
public static CodeStatement ReadStatementFrom(TextReader reader,bool includeComments=false)=>ParseStatement(TextReaderEnumerable.FromReader(reader),includeComments);
/// <summary>
/// Reads a <see cref="CodeStatement"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <param name="includeComments">True if comments should be returned as statements, or false to skip them</param>
/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>
public static CodeStatement ReadStatementFrom(string filename,bool includeComments=false)=>ParseStatement(new FileReaderEnumerable(filename),includeComments);
/// <summary>
/// Reads a <see cref="CodeStatement"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <param name="includeComments">True to return parsed comments as statements, or false to skip them</param>
/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>
public static CodeStatement ReadStatementFromUrl(string url,bool includeComments=false)=>ParseStatement(new UrlReaderEnumerable(url),includeComments);
/// <summary>
/// Parses a <see cref="CodeStatement"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <param name="includeComments">True to return parsed comments as statements, or false to skip them</param>
/// <returns>A <see cref="CodeStatement"/> representing the parsed code</returns>
public static CodeStatement ParseStatement(IEnumerable<char>input,bool includeComments=false){using(var e=new ST(input).GetEnumerator()){var pc=new _PC(e);
pc.EnsureStarted();var result=_ParseStatement(pc,includeComments);if(!pc.IsEnded)throw new SlangSyntaxException("Unrecognized remainder in statement",
pc.Current.Line,pc.Current.Column,pc.Current.Position);return result;}}/// <summary>
/// Reads a <see cref="CodeStatementCollection"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <param name="includeComments">True to include comments, or false to skip them</param>
/// <returns>A <see cref="CodeStatementCollection"/> representing the parsed code</returns>
public static CodeStatementCollection ReadStatementsFrom(TextReader reader,bool includeComments=false)=>ParseStatements(TextReaderEnumerable.FromReader(reader),
includeComments);/// <summary>
/// Reads a <see cref="CodeStatementCollection"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <param name="includeComments">True if comments should be returned as statements, or false to skip them</param>
/// <returns>A <see cref="CodeStatementCollection"/> representing the parsed code</returns>
public static CodeStatementCollection ReadStatementsFrom(string filename,bool includeComments=false)=>ParseStatements(new FileReaderEnumerable(filename),
includeComments);/// <summary>
/// Reads a <see cref="CodeStatementCollection"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <param name="includeComments">True to return parsed comments as statements, or false to skip them</param>
/// <returns>A <see cref="CodeStatementCollection"/> representing the parsed code</returns>
public static CodeStatementCollection ReadStatementsFromUrl(string url,bool includeComments=false)=>ParseStatements(new UrlReaderEnumerable(url),includeComments);
/// <summary>
/// Parses a <see cref="CodeStatementCollection"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <param name="includeComments">True to return parsed comments as statements, or false to skip them</param>
/// <returns>A <see cref="CodeStatementCollection"/> representing the parsed code</returns>
public static CodeStatementCollection ParseStatements(IEnumerable<char>input,bool includeComments=false){using(var e=new ST(input).GetEnumerator()){var
 pc=new _PC(e);pc.EnsureStarted();var result=_ParseStatements(pc,includeComments);if(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)throw new SlangSyntaxException("Unrecognized remainder in statements",
pc.Current.Line,pc.Current.Column,pc.Current.Position);return result;}}static CodeStatementCollection _ParseStatements(_PC pc,bool includeComments=false)
{var result=new CodeStatementCollection();while(ST.rbrace!=pc.SymbolId&&!pc.IsEnded)result.Add(_ParseStatement(pc,includeComments));return result;}static
 object _ParseDirective(_PC pc){var s=pc.Value;var i=s.IndexOfAny(new char[]{' ','\t'});if(0>i)i=s.Length;var type=s.Substring(1,i-1).Trim();switch(type)
{case"region":pc.Advance();return new CodeRegionDirective(CodeRegionMode.Start,s.Substring(i).Trim());case"endregion":pc.Advance();return new CodeRegionDirective(CodeRegionMode.End,
s.Substring(i).Trim());case"line":pc.Advance();s=s.Substring(i).Trim();i=s.LastIndexOfAny(new char[]{' ','\t'});if(-1<i){var num=s.Substring(0,i).Trim();
int n;if(int.TryParse(num,out n)){s=s.Substring(i).Trim();if('\"'==s[0])s=s.Substring(1,s.Length-2).Replace("\"\"","\"");return new CodeLinePragma(s,n);
}}break;}_Error(string.Format("Invalid or unsupported directive ",pc.Value),pc.Current);return null;}static List<object>_ParseDirectives(_PC pc,bool endDirectives=false)
{var result=new List<object>();while(ST.directive==pc.SymbolId){if(endDirectives&&!pc.Value.Trim().StartsWith("#endregion"))break;else if(!endDirectives
&&pc.Value.Trim().StartsWith("#endregion"))break;result.Add(_ParseDirective(pc));}return result;}static void _AddStartDirs(CodeStatement stmt,IList<object>
dirs){for(int ic=dirs.Count,i=0;i<ic;++i){var dir=dirs[i];var l=dir as CodeLinePragma;if(null!=l){stmt.LinePragma=l;dirs.RemoveAt(i);--i;--ic;continue;
}var d=dir as CodeDirective;if(null!=d){stmt.StartDirectives.Add(d);dirs.RemoveAt(i);--i;--ic;}}}static void _AddEndDirs(CodeStatement stmt,IList<object>
dirs){for(int ic=dirs.Count,i=0;i<ic;++i){var dir=dirs[i];var d=dir as CodeDirective;if(null!=d){stmt.EndDirectives.Add(d);dirs.RemoveAt(i);--i;--ic;}
}}static CodeStatement _ParseStatement(_PC pc,bool includeComments=false){var dirs=_ParseDirectives(pc);if(includeComments&&(ST.lineComment==pc.SymbolId
||ST.blockComment==pc.SymbolId)){var c=_ParseCommentStatement(pc);_AddStartDirs(c,dirs);dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(c,dirs);return
 c;}_SkipComments(pc);dirs.AddRange(_ParseDirectives(pc));var pc2=pc.GetLookAhead();pc2.EnsureStarted();CodeVariableDeclarationStatement vs=null;try{vs
=_ParseVariableDeclaration(pc2);}catch{vs=null;}if(null!=vs){ _ParseVariableDeclaration(pc);_AddStartDirs(vs,dirs);_SkipComments(pc);dirs.AddRange(_ParseDirectives(pc,true));
return vs;}pc2=pc.GetLookAhead();pc2.EnsureStarted();CodeExpression e;try{_ParseDirectives(pc2,false);_SkipComments(pc);e=_ParseExpression(pc2);}catch
{e=null;}if(null!=e){_SkipComments(pc2);if(ST.semi==pc2.SymbolId){pc2.Advance();_ParseExpression(pc);_SkipComments(pc);pc.Advance(); CodeStatement r=null;
var bo=e as CodeBinaryOperatorExpression;if(null!=bo&&CodeBinaryOperatorType.Assign==bo.Operator)r=new CodeAssignStatement(bo.Left,bo.Right);else r=new
 CodeExpressionStatement(e);_AddStartDirs(r,dirs);dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(r,dirs);return r;}else if(ST.addAssign==pc2.SymbolId
||ST.subAssign==pc2.SymbolId){bool isAttach=ST.addAssign==pc2.SymbolId;_ParseExpression(pc);_SkipComments(pc);pc.Advance();pc2.Advance();_SkipComments(pc);
var le=_ParseExpression(pc);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated statement. Expecting ;",pc.Current);pc.Advance();var v=e as CodeVariableReferenceExpression;
CodeEventReferenceExpression er=null;if(null!=v){er=new CodeEventReferenceExpression(null,v.VariableName);}else{var f=e as CodeFieldReferenceExpression;
if(null!=f)er=new CodeEventReferenceExpression(f.TargetObject,f.FieldName);}if(null==er)_Error("The attach/remove target does not refer to a valid event",pc.Current);
er.UserData.Add("slang:unresolved",true);var r=isAttach?new CodeAttachEventStatement(er,le)as CodeStatement:new CodeRemoveEventStatement(er,le);_AddStartDirs(r,
dirs);_ParseDirectives(pc,true);_AddEndDirs(r,dirs);return r;}}switch(pc.SymbolId){case ST.keyword:CodeStatement r=null;switch(pc.Value){case"if":r=_ParseIfStatement(pc);
break;case"goto":r=_ParseGotoStatement(pc);break;case"for":r=_ParseForStatement(pc);break;case"while":r=_ParseWhileStatement(pc);break;case"return":r=
_ParseReturnStatement(pc);break;case"throw":r=_ParseThrowStatement(pc);break;case"try":r=_ParseTryCatchFinallyStatement(pc);break;case"var":case"bool":
case"char":case"string":case"sbyte":case"byte":case"short":case"ushort":case"int":case"uint":case"long":case"ulong":case"float":case"double":case"decimal":
r=_ParseVariableDeclaration(pc);break;default:throw new NotSupportedException(string.Format("The keyword {0} is not supported",pc.Value));}_AddStartDirs(r,
dirs);dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(r,dirs);return r;case ST.identifier: var s=pc.Value;pc2=pc.GetLookAhead();pc2.EnsureStarted();
pc2.Advance();if(ST.colon==pc2.SymbolId){ var ls=new CodeLabeledStatement(pc.Value);pc.Advance();_SkipComments(pc);if(pc.IsEnded||ST.colon!=pc.SymbolId)
_Error("Unterminated label. Expecting :",pc.Current);pc.Advance();_AddStartDirs(ls,dirs);dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(ls,dirs);
return ls;}throw new NotImplementedException("Not finished");default:_Error(string.Format("Unexpected token {0} found statement.",pc.Value),pc.Current);
break;}return null;}static CodeCommentStatement _ParseCommentStatement(_PC pc){ var s=pc.Value;if(ST.lineComment==pc.SymbolId){pc.Advance();if(s.StartsWith("///"))
return new CodeCommentStatement(s.Substring(3).Trim(),true);return new CodeCommentStatement(s.Substring(2).Trim());}pc.Advance();return new CodeCommentStatement(s.Substring(2,
s.Length-4).Trim());}static CodeTryCatchFinallyStatement _ParseTryCatchFinallyStatement(_PC pc){ pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated try statement",
pc.Current);var result=new CodeTryCatchFinallyStatement();if(ST.lbrace!=pc.SymbolId)_Error("Unterminated try statement",pc.Current);pc.Advance();while(!pc.IsEnded
&&ST.rbrace!=pc.SymbolId){result.TryStatements.Add(_ParseStatement(pc,true));}if(pc.IsEnded)_Error("Unterminated try statement",pc.Current);pc.Advance();
_SkipComments(pc);if(ST.keyword!=pc.SymbolId)_Error("Expecting catch or finally statement",pc.Current);while("catch"==pc.Value){pc.Advance();_SkipComments(pc);
var cc=new CodeCatchClause();if(ST.lparen==pc.SymbolId){if(!pc.Advance())_Error("Unterminated catch clause",pc.Current);cc.CatchExceptionType=_ParseTypeRef(pc);
_SkipComments(pc);if(ST.identifier==pc.SymbolId){cc.LocalName=pc.Value;if(!pc.Advance())_Error("Unterminated catch clause",pc.Current);_SkipComments(pc);
if(ST.rparen!=pc.SymbolId)_Error(string.Format("Unexpected token {0} in catch clause",pc.Value),pc.Current);pc.Advance();_SkipComments(pc);}else if(ST.rparen
==pc.SymbolId){pc.Advance();_SkipComments(pc);}else _Error(string.Format("Unexpected token {0} in catch clause",pc.Value),pc.Current);}else throw new NotSupportedException("You must specify an exception type to catch in each catch clause.");
if(ST.lbrace!=pc.SymbolId)_Error("Expecting { in catch clause",pc.Current);pc.Advance();while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId){cc.Statements.Add(_ParseStatement(pc,true));
}if(pc.IsEnded)_Error("Unterminated catch clause",pc.Current);pc.Advance();_SkipComments(pc);result.CatchClauses.Add(cc);}if(ST.keyword==pc.SymbolId&&
"finally"==pc.Value){pc.Advance();_SkipComments(pc);if(ST.lbrace!=pc.SymbolId)_Error("Expecting { in finally clause",pc.Current);pc.Advance();while(!pc.IsEnded
&&ST.rbrace!=pc.SymbolId)result.FinallyStatements.Add(_ParseStatement(pc,true));if(pc.IsEnded)_Error("Unterminated finally clause",pc.Current);pc.Advance();
if(0==result.FinallyStatements.Count){ result.FinallyStatements.Add(new CodeSnippetStatement());}}return result;}static CodeVariableDeclarationStatement
 _ParseVariableDeclaration(_PC pc){CodeTypeReference ctr=null;if(!(ST.keyword==pc.SymbolId&&"var"==pc.Value))ctr=_ParseTypeRef(pc);else pc.Advance();_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated variable declaration statement",pc.Current);if(ST.inc==pc.SymbolId)throw new NotSupportedException("Postfix increment is not supported. Consider using prefix increment instead.");
if(ST.dec==pc.SymbolId)throw new NotSupportedException("Postfix decrement is not supported. Consider using prefix decrement instead.");if(ST.identifier
!=pc.SymbolId)_Error("Expecting identifier in variable declaration",pc.Current);var result=new CodeVariableDeclarationStatement(ctr,pc.Value);if(null==
ctr)result.UserData.Add("slang:unresolved",true);pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated variable declaration statement",pc.Current);
if(ST.eq==pc.SymbolId){pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated variable declaration initializer",pc.Current);result.InitExpression
=_ParseExpression(pc);_SkipComments(pc);if(ST.semi!=pc.SymbolId)_Error("Invalid expression in variable declaration initializer",pc.Current);pc.Advance();
return result;}else if(null==ctr)_Error("Var variable declarations must have an initializer",pc.Current);_SkipComments(pc);if(ST.semi!=pc.SymbolId)_Error("Invalid expression in variable declaration initializer",
pc.Current);pc.Advance();return result;}static CodeMethodReturnStatement _ParseReturnStatement(_PC pc){ pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated return statement",
pc.Current);if(ST.semi==pc.SymbolId){pc.Advance();return new CodeMethodReturnStatement();}var e=_ParseExpression(pc);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated return statement",
pc.Current);if(ST.semi!=pc.SymbolId)_Error("Invalid expression in return statement",pc.Current);pc.Advance();return new CodeMethodReturnStatement(e);}
static CodeThrowExceptionStatement _ParseThrowStatement(_PC pc){ pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated throw statement",pc.Current);
if(ST.semi==pc.SymbolId){pc.Advance();return new CodeThrowExceptionStatement();}var e=_ParseExpression(pc);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated throw statement",
pc.Current);if(ST.semi!=pc.SymbolId)_Error("Invalid expression in throw statement",pc.Current);pc.Advance();return new CodeThrowExceptionStatement(e);
}static CodeGotoStatement _ParseGotoStatement(_PC pc){ pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated goto statement",pc.Current);if
(ST.identifier!=pc.SymbolId)_Error("Expecting identifier in goto statement",pc.Current);var g=new CodeGotoStatement(pc.Value);pc.Advance();_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated goto statement",pc.Current);if(ST.semi!=pc.SymbolId)_Error("Expecting ; after goto statement",pc.Current);pc.Advance();
return g;}static CodeConditionStatement _ParseIfStatement(_PC pc){ if(!pc.Advance())_Error("Unterminated if statement",pc.Current);_SkipComments(pc);if(ST.lparen!=pc.SymbolId
||!pc.Advance())_Error("Unterminated if statement",pc.Current);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated if statement",pc.Current);var cnd=
_ParseExpression(pc);_SkipComments(pc);if(ST.rparen!=pc.SymbolId||!pc.Advance())_Error("Unterminated if statement",pc.Current);_SkipComments(pc);if(pc.IsEnded)
_Error("Unterminated if statement",pc.Current);var result=new CodeConditionStatement(cnd);if(ST.lbrace==pc.SymbolId){if(!pc.Advance())_Error("Unterminated if statement",
pc.Current);while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)result.TrueStatements.Add(_ParseStatement(pc,true));if(ST.rbrace!=pc.SymbolId)_Error("Unterminated if statement",
pc.Current);pc.Advance();_SkipComments(pc);if(pc.IsEnded)return result;}else{result.TrueStatements.Add(_ParseStatement(pc));}_SkipComments(pc);if(ST.keyword==pc.SymbolId
&&"else"==pc.Value){pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated if/else statement",pc.Current);if(ST.lbrace==pc.SymbolId){if(!pc.Advance())
_Error("Unterminated if/else statement",pc.Current);while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)result.FalseStatements.Add(_ParseStatement(pc,true));if(ST.rbrace
!=pc.SymbolId)_Error("Unterminated if/else statement",pc.Current);pc.Advance();_SkipComments(pc);if(pc.IsEnded)return result;}else{result.FalseStatements.Add(_ParseStatement(pc));
}}return result;}static CodeIterationStatement _ParseWhileStatement(_PC pc){ if(!pc.Advance())_Error("Unterminated while statement",pc.Current);_SkipComments(pc);
if(ST.lparen!=pc.SymbolId||!pc.Advance())_Error("Unterminated while statement",pc.Current);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated while statement",
pc.Current);var cnd=_ParseExpression(pc);_SkipComments(pc);if(ST.rparen!=pc.SymbolId||!pc.Advance())_Error("Unterminated while statement",pc.Current);
_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated while statement",pc.Current);var result=new CodeIterationStatement(new CodeSnippetStatement(),cnd,new
 CodeSnippetStatement());if(ST.lbrace==pc.SymbolId){if(!pc.Advance())_Error("Unterminated while statement",pc.Current);while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)
result.Statements.Add(_ParseStatement(pc,true));if(ST.rbrace!=pc.SymbolId)_Error("Unterminated while statement",pc.Current);pc.Advance();_SkipComments(pc);
if(pc.IsEnded)return result;}else result.Statements.Add(_ParseStatement(pc));_SkipComments(pc);return result;}static CodeIterationStatement _ParseForStatement(_PC
 pc){ if(!pc.Advance())_Error("Unterminated for statement",pc.Current);_SkipComments(pc);if(ST.lparen!=pc.SymbolId||!pc.Advance())_Error("Unterminated for statement",
pc.Current);_SkipComments(pc);CodeStatement init=null;if(pc.IsEnded)_Error("Unterminated for statement",pc.Current);if(ST.semi!=pc.SymbolId){var pc2=pc.GetLookAhead();
pc2.EnsureStarted();try{init=_ParseVariableDeclaration(pc2);}catch{init=null;}}if(null!=init){_ParseVariableDeclaration(pc);_SkipComments(pc);}else{_SkipComments(pc);
if(ST.semi!=pc.SymbolId){var e=_ParseExpression(pc);var bbo=e as CodeBinaryOperatorExpression;if(null==e)throw new NotImplementedException("Expression in init statement was null");
if(null!=bbo&&CodeBinaryOperatorType.Assign==bbo.Operator)init=new CodeAssignStatement(bbo.Left,bbo.Right);else init=new CodeExpressionStatement(e);_SkipComments(pc);
if(ST.semi!=pc.SymbolId)_Error("Invalid init statement in for statement",pc.Current);if(pc.IsEnded)_Error("Unterminated for statement",pc.Current);}}if
(null==init){if(ST.semi!=pc.SymbolId)_Error("Invalid for statement",pc.Current);pc.Advance();_SkipComments(pc);}if(pc.IsEnded)_Error("Unterminated for statement",
pc.Current);CodeExpression test=null;if(ST.semi!=pc.SymbolId){test=_ParseExpression(pc);_SkipComments(pc);if(ST.semi!=pc.SymbolId)_Error("Invalid test expression in for statement",
pc.Current);if(!pc.Advance())_Error("Unterminated for statement",pc.Current);}_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated for statement",pc.Current);
CodeExpression inc=null;if(ST.rparen!=pc.SymbolId){inc=_ParseExpression(pc);_SkipComments(pc);}if(ST.rparen!=pc.SymbolId)throw new ArgumentNullException("Invalid increment statement in for loop");
if(!pc.Advance())_Error("Unterminated for statement",pc.Current);_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated for statement",pc.Current);var bo
=inc as CodeBinaryOperatorExpression;CodeStatement incs=null;if(null!=inc){if(null!=bo&&CodeBinaryOperatorType.Assign==bo.Operator)incs=new CodeAssignStatement(bo.Left,
bo.Right);else incs=new CodeExpressionStatement(inc);}if(null==init)init=new CodeSnippetStatement();if(null==incs)incs=new CodeSnippetStatement();if(null
==test)test=new CodeSnippetExpression();var result=new CodeIterationStatement(init,test,incs);if(ST.lbrace==pc.SymbolId){if(!pc.Advance())_Error("Unterminated for statement",
pc.Current);while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId)result.Statements.Add(_ParseStatement(pc,true));if(ST.rbrace!=pc.SymbolId)_Error("Unterminated for statement",
pc.Current);pc.Advance();_SkipComments(pc);if(pc.IsEnded)return result;}else result.Statements.Add(_ParseStatement(pc));_SkipComments(pc);return result;
}}}namespace CD{using ST=SlangTokenizer;partial class SlangParser{/// <summary>
/// Reads a <see cref="CodeTypeDeclaration"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
public static CodeTypeDeclaration ReadTypeFrom(TextReader reader)=>ParseType(TextReaderEnumerable.FromReader(reader));/// <summary>
/// Reads a <see cref="CodeTypeDeclaration"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
public static CodeTypeDeclaration ReadTypeFrom(string filename)=>ParseType(new FileReaderEnumerable(filename));/// <summary>
/// Reads a <see cref="CodeTypeDeclaration"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
public static CodeTypeDeclaration ReadTypeFromUrl(string url)=>ParseType(new UrlReaderEnumerable(url));/// <summary>
/// Parses a <see cref="CodeTypeDeclaration"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <returns>A <see cref="CodeTypeDeclaration"/> representing the parsed code</returns>
public static CodeTypeDeclaration ParseType(IEnumerable<char>input){using(var e=new ST(input).GetEnumerator()){var pc=new _PC(e);pc.EnsureStarted();var
 result=_ParseType(pc);if(!pc.IsEnded)throw new SlangSyntaxException("Unrecognized remainder in type",pc.Current.Line,pc.Current.Column,pc.Current.Position);
return result;}}static CodeTypeDeclaration _ParseType(_PC pc,bool isNested=false){var dirs=_ParseDirectives(pc);var result=new CodeTypeDeclaration();IList<KeyValuePair<string,CodeAttributeDeclaration>>
custAttrs=null;HashSet<string>attrs=null;if(!isNested){var comments=new CodeCommentStatementCollection();while(ST.lineComment==pc.SymbolId||ST.blockComment
==pc.SymbolId)comments.Add(_ParseCommentStatement(pc));dirs.AddRange(_ParseDirectives(pc));custAttrs=_ParseCustomAttributes(pc);attrs=_ParseTypeAttributes(pc);
if(attrs.Contains("static"))throw new NotSupportedException("Explicitly static classes are not supported.");result.Attributes=_BuildMemberAttributes(attrs);
result.TypeAttributes=(isNested)?_BuildNestedTypeAttributes(attrs):_BuildTopLevelTypeAttributes(attrs,pc);result.Comments.AddRange(comments);_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated type declaration",pc.Current);if(ST.keyword==pc.SymbolId&&"partial"==pc.Value){pc.Advance();_SkipComments(pc);if(pc.IsEnded)
_Error("Unterminated type declaration",pc.Current);result.IsPartial=true;}_AddCustomAttributes(custAttrs,null,result.CustomAttributes);if(null!=custAttrs
&&custAttrs.Count>result.CustomAttributes.Count)_Error("Invalid custom attribute targets",pc.Current);_AddStartDirs(result,dirs);}if(ST.keyword!=pc.SymbolId)
_Error("Expecting class, struct, enum, or interface",pc.Current);switch(pc.Value){case"class":result.IsClass=true;break;case"struct":result.IsStruct=true;
break;case"enum":if(result.IsPartial)_Error("Enums cannot be partial",pc.Current);result.IsEnum=true;break;case"interface":result.IsInterface=true;break;
default:_Error("Expecting class, struct, enum, or interface",pc.Current);break;}pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated type declaration",
pc.Current);if(ST.identifier!=pc.SymbolId)_Error("Expecting identifier in type declaration",pc.Current);result.Name=pc.Value;pc.Advance();_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated type declaration",pc.Current);if(result.IsEnum){var e=_ParseEnum(pc,result);dirs.AddRange(_ParseDirectives(pc,true));
_AddEndDirs(e,dirs);}if(ST.lt==pc.SymbolId){ pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated generic type parameter specification",pc.Current);
if(ST.gt==pc.SymbolId)_Error("Generic type parameter specification cannot be empty",pc.Current);while(!pc.IsEnded&&ST.gt!=pc.SymbolId){var custAttrs2=
_ParseCustomAttributes(pc);if(ST.identifier!=pc.SymbolId)_Error("Expecting identifier in type parameter specification",pc.Current);var tp=new CodeTypeParameter(pc.Value);
_AddCustomAttributes(custAttrs2,null,tp.CustomAttributes);if(tp.CustomAttributes.Count<custAttrs2.Count)_Error("Invalid target in custom attribute declaration on generic type parameter",
pc.Current);result.TypeParameters.Add(tp);pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated generic type parameter specification",pc.Current);
if(ST.comma!=pc.SymbolId)break;pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated generic type parameter specification",pc.Current);}if
(pc.IsEnded)_Error("Unterminated generic type parameter specification",pc.Current);if(ST.gt!=pc.SymbolId)_Error("Illegal generic type parameter specification",
pc.Current);pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated generic type parameter specification",pc.Current);}if(ST.colon==pc.SymbolId)
{pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated type declaration",pc.Current);if(ST.lbrace==pc.SymbolId||(ST.identifier==pc.SymbolId
&&"where"==pc.Value))_Error("Empty base type specifiers",pc.Current);while(!pc.IsEnded&&!(ST.lbrace==pc.SymbolId||(ST.identifier==pc.SymbolId&&"where"
==pc.Value))){result.BaseTypes.Add(_ParseTypeRef(pc));_SkipComments(pc);if(ST.comma==pc.SymbolId){pc.Advance();if(pc.IsEnded)_Error("Expecting type",pc.Current);
}}_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated type declaration",pc.Current);}if(ST.identifier==pc.SymbolId&&"where"==pc.Value){pc.Advance();_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated type constraint",pc.Current);var moved=false;while(!pc.IsEnded&&ST.lbrace!=pc.SymbolId){moved=true;if(ST.identifier
!=pc.SymbolId)_Error("Expecting identifier in type constraint",pc.Current);var cp=_LookupTypeParameter(result.TypeParameters,pc.Value,pc);pc.Advance();
_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated type constraint",pc.Current);if(ST.colon!=pc.SymbolId)_Error("Expecting : in type constraint",pc.Current);
pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated type constraint",pc.Current);cp.Constraints.Add(_ParseTypeRef(pc));_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated type declaration",pc.Current);if(ST.comma==pc.SymbolId){pc.Advance();_SkipComments(pc);if(ST.lbrace==pc.SymbolId)_Error("Unterminated type constraint",
pc.Current);}}if(!moved)_Error("Unterminated type constraint",pc.Current);}if(ST.lbrace!=pc.SymbolId)_Error("Expecting { in type definition",pc.Current);
pc.Advance();if(pc.IsEnded)_Error("Unterminated type declaration",pc.Current);while(!pc.IsEnded&&ST.rbrace!=pc.SymbolId){result.Members.Add(_ParseMember(pc,
result.Name));}if(pc.IsEnded)_Error("Unterminated type declaration",pc.Current);if(ST.rbrace!=pc.SymbolId)_Error("Illegal member declaration in type",
pc.Current);pc.Advance();_SkipComments(pc);dirs.AddRange(_ParseDirectives(pc,true));_AddEndDirs(result,dirs);return result;}static CodeTypeParameter _LookupTypeParameter(CodeTypeParameterCollection
 parms,string name,_PC pc){foreach(CodeTypeParameter tp in parms)if(tp.Name==name)return tp;_Error("Undeclared type parameter",pc.Current);return null;
}static CodeTypeDeclaration _ParseEnum(_PC pc,CodeTypeDeclaration result){var bt=new CodeTypeReference(typeof(int));if(ST.colon==pc.SymbolId){pc.Advance();
_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated enum declaration",pc.Current);bt=_ParseTypeRef(pc);result.BaseTypes.Add(bt);_SkipComments(pc);if(pc.IsEnded)
_Error("Unterminated enum declaration",pc.Current);}if(ST.lbrace!=pc.SymbolId)_Error("Expecting enum body",pc.Current);pc.Advance();_SkipComments(pc);
if(pc.IsEnded)_Error("Unterminated enum declaration",pc.Current);while(ST.rbrace!=pc.SymbolId){_SkipComments(pc);result.Members.Add(_ParseEnumField(pc,
bt));}if(ST.rbrace!=pc.SymbolId)_Error("Unterminated enum declaration",pc.Current);pc.Advance();return result;}static CodeMemberField _ParseEnumField(_PC
 pc,CodeTypeReference enumType){_SkipComments(pc);if(pc.IsEnded)_Error("Expecting enum field declaration",pc.Current);IList<KeyValuePair<string,CodeAttributeDeclaration>>
custAttrs=null;if(ST.lbracket==pc.SymbolId)custAttrs=_ParseCustomAttributes(pc);_SkipComments(pc);if(pc.IsEnded||ST.identifier!=pc.SymbolId)_Error("Expecting enum field declaration",
pc.Current);var result=new CodeMemberField();result.Name=pc.Value;_AddCustomAttributes(custAttrs,null,result.CustomAttributes);_AddCustomAttributes(custAttrs,
"field",result.CustomAttributes);if(null!=custAttrs&&custAttrs.Count>result.CustomAttributes.Count)_Error("Invalid custom attribute targets",pc.Current);
pc.Advance();_SkipComments(pc);if(pc.IsEnded||(ST.eq!=pc.SymbolId&&ST.comma!=pc.SymbolId&&ST.rbrace!=pc.SymbolId))_Error("Expecting enum field value, }, or ,",
pc.Current);if(ST.eq==pc.SymbolId){pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Expecting enum field value",pc.Current);result.InitExpression=
_ParseExpression(pc);_SkipComments(pc);if(pc.IsEnded)_Error("Expecting , or } in enum declaration",pc.Current);}if(ST.comma==pc.SymbolId){pc.Advance();
_SkipComments(pc);if(pc.IsEnded)_Error("Expecting enum field value",pc.Current);}return result;}static TypeAttributes _BuildTopLevelTypeAttributes(ICollection<string>
attrs,_PC pc){var result=(TypeAttributes)0;foreach(var attr in attrs){switch(attr){case"public":result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.Public;
break;case"internal":result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NotPublic;break;case"abstract":result|=TypeAttributes.Abstract;break;
case"private":_Error("Top level types cannot be private",pc.Current);break;case"protected":_Error("Top level types cannot be protected",pc.Current);break;
}}return result;}static TypeAttributes _BuildNestedTypeAttributes(ICollection<string>attrs){ var result=TypeAttributes.NestedFamORAssem;foreach(var attr
 in attrs){switch(attr){case"protected":if(attrs.Contains("internal"))result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedFamORAssem|TypeAttributes.NotPublic;
else result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedFamily|TypeAttributes.NotPublic;break;case"internal":if(attrs.Contains("protected"))
result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedFamORAssem|TypeAttributes.NotPublic;else result=(result&~TypeAttributes.VisibilityMask)
|TypeAttributes.NestedFamANDAssem|TypeAttributes.NotPublic;break;case"public":result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedPublic;
break;case"private":result=(result&~TypeAttributes.VisibilityMask)|TypeAttributes.NestedPrivate|TypeAttributes.NotPublic;break;}}return result;}static
 HashSet<string>_ParseTypeAttributes(_PC pc){var result=new HashSet<string>();_SkipComments(pc);var more=true;while(more&&!pc.IsEnded&&ST.keyword==pc.SymbolId)
{switch(pc.Value){case"static":case"abstract":case"protected":case"internal":case"public":case"private":result.Add(pc.Value);pc.Advance();_SkipComments(pc);
break;default:more=false;break;}}return result;}}}namespace CD{using ST=SlangTokenizer;partial class SlangParser{/// <summary>
/// Reads a <see cref="CodeTypeReference"/> from the specified <see cref="TextReader"/>
/// </summary>
/// <param name="reader">The reader to read from</param>
/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
public static CodeTypeReference ReadTypeRefFrom(TextReader reader)=>ParseTypeRef(TextReaderEnumerable.FromReader(reader));/// <summary>
/// Reads a <see cref="CodeTypeReference"/> from the specified file
/// </summary>
/// <param name="filename">The file to read</param>
/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
public static CodeTypeReference ReadTypeRefFrom(string filename)=>ParseTypeRef(new FileReaderEnumerable(filename));/// <summary>
/// Reads a <see cref="CodeTypeReference"/> from the specified URL
/// </summary>
/// <param name="url">The URL to read</param>
/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
public static CodeTypeReference ReadTypeRefFromUrl(string url)=>ParseTypeRef(new UrlReaderEnumerable(url));/// <summary>
/// Parses a <see cref="CodeTypeReference"/> from the specified input
/// </summary>
/// <param name="input">The input to parse</param>
/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
public static CodeTypeReference ParseTypeRef(IEnumerable<char>input){using(var e=new ST(input).GetEnumerator()){var pc=new _PC(e);pc.EnsureStarted();var
 result=_ParseTypeRef(pc);if(!pc.IsEnded)throw new SlangSyntaxException("Unrecognized remainder in type reference",pc.Current.Line,pc.Current.Column,pc.Current.Position);
return result;}}static CodeTypeReference _ParseTypeGenerics(_PC pc,CodeTypeReference result=null){_SkipComments(pc);if(null==result)result=new CodeTypeReference();
 while(ST.gt!=pc.SymbolId){_SkipComments(pc);if(!pc.Advance())_Error("Unterminated generic specification",pc.Current);_SkipComments(pc);var tp=_ParseTypeRef(pc);
tp.Options=CodeTypeReferenceOptions.GenericTypeParameter;result.TypeArguments.Add(tp);if(ST.gt!=pc.SymbolId&&ST.comma!=pc.SymbolId)_Error("Invalid token in generic specification",
pc.Current);}if(ST.gt!=pc.SymbolId)_Error("Unterminated generic specification",pc.Current);_SkipComments(pc);pc.Advance();return result;}static CodeTypeReference
 _ParseTypeRef(_PC pc,bool notArrayPart=false,bool once=false){_SkipComments(pc);if(pc.IsEnded)_Error("Expecting a type reference",pc.Current);_PC pc2;
 var isIntrinsic=false;var result=new CodeTypeReference();var first=true;while(!pc.IsEnded){var s=pc.Value;if(first){if(ST.keyword==pc.SymbolId){s=_TranslateIntrinsicType(s,pc);
isIntrinsic=true;}else if(ST.identifier!=pc.SymbolId)_Error("An identifier was expected",pc.Current);result.BaseType=s;}else{if(ST.identifier!=pc.SymbolId)
_Error("An identifier was expected",pc.Current);result.BaseType=string.Concat(result.BaseType,"+",s);}pc.Advance();_SkipComments(pc);if(pc.IsEnded)return
 result;if(!first||!isIntrinsic){_SkipComments(pc);while(ST.dot==pc.SymbolId){ result.UserData["slang:unresolved"]=true;var bt=string.Concat(result.BaseType,
".");pc2=pc.GetLookAhead();pc2.EnsureStarted();pc2.Advance();_SkipComments(pc2);if(ST.identifier!=pc2.SymbolId)return result;pc.Advance();_SkipComments(pc);
result.BaseType=string.Concat(bt,pc.Value);if(!pc.Advance())return result;}}if(ST.lt==pc.SymbolId){var c=result.TypeArguments.Count;result=_ParseTypeGenerics(pc,
result); if(!first&&result.TypeArguments.Count>c)result.BaseType=string.Concat(result.BaseType,"`",result.TypeArguments.Count-c);}_SkipComments(pc);if
(!notArrayPart){if(ST.lbracket==pc.SymbolId)result=_ParseArrayTypeModifiers(result,pc);_SkipComments(pc);}if(once||ST.dot!=pc.SymbolId)break;pc2=pc.GetLookAhead();
pc2.EnsureStarted();pc2.Advance();_SkipComments(pc2);if(ST.identifier!=pc2.SymbolId)return result;pc.Advance();_SkipComments(pc);if(pc.IsEnded)_Error("Unterminated type reference",
pc.Current);first=false;}return result;}static CodeTypeReference _ParseArrayTypeModifiers(CodeTypeReference type,_PC pc){var mods=new List<int>();var result
=type;_SkipComments(pc);var t=pc.Current;var ai=1;var inBrace=true;while(pc.Advance()){_SkipComments(pc);t=pc.Current;if(inBrace&&ST.comma==t.SymbolId)
{++ai;continue;}else if(ST.rbracket==t.SymbolId){mods.Add(ai);ai=1;if(!pc.Advance())break;inBrace=false;if(ST.lbracket!=pc.SymbolId)break;else inBrace
=true;}else break;}for(var i=mods.Count-1;-1<i;--i){var ctr=new CodeTypeReference();ctr.ArrayElementType=result;ctr.ArrayRank=mods[i];result=ctr;}return
 result;}static string _TranslateIntrinsicType(string s,_PC pc){switch(s){case"bool":s=typeof(bool).FullName;break;case"char":s=typeof(char).FullName;
break;case"string":s=typeof(string).FullName;break;case"object":s=typeof(object).FullName;break;case"byte":s=typeof(byte).FullName;break;case"sbyte":s
=typeof(sbyte).FullName;break;case"short":s=typeof(short).FullName;break;case"ushort":s=typeof(ushort).FullName;break;case"int":s=typeof(int).FullName;
break;case"uint":s=typeof(uint).FullName;break;case"long":s=typeof(long).FullName;break;case"ulong":s=typeof(ulong).FullName;break;case"float":s=typeof(float).FullName;
break;case"double":s=typeof(double).FullName;break;case"decimal":s=typeof(decimal).FullName;break;default:_Error(string.Format("Type expected but found {0}",
s),pc.Current);break;}return s;}}}namespace CD{/// <summary>
/// Provides the ability to fix up a CodeDOM tree produced by <see cref="SlangParser"/>
/// </summary>
#if GOKITLIB
public
#endif
static class SlangPatcher{const BindingFlags _BindFlags=BindingFlags.Instance|BindingFlags.Static|BindingFlags.Public|BindingFlags.NonPublic|BindingFlags.FlattenHierarchy;
/// <summary>
/// Patches the CodeDOM tree received from the <see cref="SlangParser"/> into something more usable, by resolving type information and replacing various elements in the CodeDOM graph
/// </summary>
/// <param name="compileUnits">The <see cref="CodeCompileUnit"/> objects to patch</param>
public static void Patch(params CodeCompileUnit[]compileUnits)=>Patch((IEnumerable<CodeCompileUnit>)compileUnits);/// <summary>
/// Patches the CodeDOM tree received from the <see cref="SlangParser"/> into something more usable, by resolving type information and replacing various elements in the CodeDOM graph
/// </summary>
/// <param name="compileUnits">The <see cref="CodeCompileUnit"/> objects to patch</param>
public static void Patch(IEnumerable<CodeCompileUnit>compileUnits){var resolver=new CodeDomResolver();foreach(var ccu in compileUnits)resolver.CompileUnits.Add(ccu);
resolver.Refresh();restart:var working=-1;var oworking=0;while(0!=working&&oworking!=working){oworking=working;working=0;for(int ic=resolver.CompileUnits.Count,i=0;i<ic;++i)
{CodeDomVisitor.Visit(resolver.CompileUnits[i],(ctx)=>{var co=ctx.Target as CodeObject;if(null!=co&&co.UserData.Contains("slang:unresolved")){++working;
_Patch(ctx.Target as CodeFieldReferenceExpression,ctx,resolver);_Patch(ctx.Target as CodeVariableDeclarationStatement,ctx,resolver);_Patch(ctx.Target as
 CodeVariableReferenceExpression,ctx,resolver);_Patch(ctx.Target as CodeDelegateInvokeExpression,ctx,resolver);_Patch(ctx.Target as CodeObjectCreateExpression,
ctx,resolver);_Patch(ctx.Target as CodeBinaryOperatorExpression,ctx,resolver);_Patch(ctx.Target as CodeIndexerExpression,ctx,resolver);_Patch(ctx.Target
 as CodeMemberMethod,ctx,resolver);_Patch(ctx.Target as CodeMemberProperty,ctx,resolver);_Patch(ctx.Target as CodeTypeReference,ctx,resolver);}});}resolver.Refresh();
}oworking=working;working=0;if(0<oworking){ for(int ic=resolver.CompileUnits.Count,i=0;i<ic;++i){CodeDomVisitor.Visit(resolver.CompileUnits[i],(ctx)=>
{var co=ctx.Target as CodeObject;if(null!=co&&co.UserData.Contains("slang:unresolved")){++working;_Patch(ctx.Target as CodeFieldReferenceExpression,ctx,
resolver);_Patch(ctx.Target as CodeVariableDeclarationStatement,ctx,resolver);_Patch(ctx.Target as CodeVariableReferenceExpression,ctx,resolver);_Patch(ctx.Target
 as CodeDelegateInvokeExpression,ctx,resolver);_Patch(ctx.Target as CodeObjectCreateExpression,ctx,resolver);_Patch(ctx.Target as CodeBinaryOperatorExpression,
ctx,resolver);_Patch(ctx.Target as CodeIndexerExpression,ctx,resolver);_Patch(ctx.Target as CodeMemberMethod,ctx,resolver);_Patch(ctx.Target as CodeMemberProperty,
ctx,resolver);_Patch(ctx.Target as CodeTypeReference,ctx,resolver);}});}if(oworking!=working)goto restart;}}/// <summary>
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
 result;}return null;}static void _Patch(CodeTypeReference tr,CodeDomVisitContext ctx,CodeDomResolver res){if(null!=tr){if(res.IsValidType(tr,res.GetScope(tr)))
{tr.UserData.Remove("slang:unresolved");return;} throw new NotImplementedException();}}static void _Patch(CodeObjectCreateExpression oc,CodeDomVisitContext
 ctx,CodeDomResolver res){if(null!=oc){oc.UserData.Remove("slang:unresolved");if(1==oc.Parameters.Count){if(_IsDelegate(oc.Parameters[0],res)){var del
=_GetDelegateFromFields(oc,oc.Parameters[0],res);CodeDomVisitor.ReplaceTarget(ctx,del);}}}}static void _Patch(CodeMemberProperty prop,CodeDomVisitContext
 ctx,CodeDomResolver resolver){if(null!=prop){ if(null==prop.PrivateImplementationType){if(prop.Name=="Current")System.Diagnostics.Debugger.Break();var
 scope=resolver.GetScope(prop);var td=scope.DeclaringType;var binder=new CodeDomBinder(scope);for(int ic=td.BaseTypes.Count,i=0;i<ic;++i){var ctr=td.BaseTypes[i];
var t=resolver.TryResolveType(ctr,scope);if(null!=t){var ma=binder.GetPropertyGroup(t,prop.Name,BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly);
if(0<ma.Length){var p=binder.SelectProperty(BindingFlags.Instance|BindingFlags.Public|BindingFlags.DeclaredOnly,ma,null,_GetParameterTypes(prop.Parameters),
null);if(null!=p)prop.ImplementationTypes.Add(ctr);}}}}prop.UserData.Remove("slang:unresolved");}}static void _Patch(CodeBinaryOperatorExpression op,CodeDomVisitContext
 ctx,CodeDomResolver resolver){if(null!=op){var scope=resolver.GetScope(op);if(CodeBinaryOperatorType.IdentityEquality==op.Operator){if(_HasUnresolved(op.Left))
return;var tr1=resolver.GetTypeOfExpression(op.Left);if(resolver.IsValueType(tr1)){if(_HasUnresolved(op.Right))return;var tr2=resolver.GetTypeOfExpression(op.Right);
if(resolver.IsValueType(tr2)){op.Operator=CodeBinaryOperatorType.ValueEquality;}}op.UserData.Remove("slang:unresolved");}else if(CodeBinaryOperatorType.IdentityInequality==op.Operator)
{if(_HasUnresolved(op.Left))return;var tr1=resolver.GetTypeOfExpression(op.Left);if(resolver.IsValueType(tr1)){if(_HasUnresolved(op.Right))return;var tr2
=resolver.GetTypeOfExpression(op.Right);if(resolver.IsValueType(tr2)){ op.Operator=CodeBinaryOperatorType.ValueEquality;var newOp=new CodeBinaryOperatorExpression(new
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
return;}}}CodeTypeReference ctr;if(scope.VariableTypes.TryGetValue(vr.VariableName,out ctr)){if(!CodeDomResolver.IsNullOrVoidType(ctr)){if(vr.VariableName
=="done")System.Diagnostics.Debug.WriteLine("done var resolved to var");vr.UserData.Remove("slang:unresolved");return;}} if(scope.ArgumentTypes.ContainsKey(vr.VariableName))
{var a=new CodeArgumentReferenceExpression(vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,a);return;}else if(scope.FieldNames.Contains(vr.VariableName))
{CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName)){var f=new CodeFieldReferenceExpression(new CodeThisReferenceExpression(),vr.VariableName);
CodeDomVisitor.ReplaceTarget(ctx,f);}else if(scope.TypeTargets.TryGetValue(vr.VariableName,out tref)){var f=new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(tref),
vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,f);}return;}else if(scope.MethodNames.Contains(vr.VariableName)){CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName))
{var m=new CodeMethodReferenceExpression(new CodeThisReferenceExpression(),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,m);return;}if(scope.TypeTargets.TryGetValue(vr.VariableName,
out tref)){var m=new CodeMethodReferenceExpression(new CodeTypeReferenceExpression(tref),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,m);return;}
}else if(scope.PropertyNames.Contains(vr.VariableName)){CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName)){var p=new CodePropertyReferenceExpression(new
 CodeThisReferenceExpression(),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,p);return;}else if(scope.TypeTargets.TryGetValue(vr.VariableName,out tref))
{var p=new CodePropertyReferenceExpression(new CodeTypeReferenceExpression(tref),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,p);return;}}else if
(scope.EventNames.Contains(vr.VariableName)){CodeTypeReference tref; if(scope.ThisTargets.Contains(vr.VariableName)){var e=new CodeEventReferenceExpression(new
 CodeThisReferenceExpression(),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,e);return;}else if(scope.TypeTargets.TryGetValue(vr.VariableName,out tref))
{var e=new CodeEventReferenceExpression(new CodeTypeReferenceExpression(tref),vr.VariableName);CodeDomVisitor.ReplaceTarget(ctx,e);return;}}return;}return;
}static void _Patch(CodeIndexerExpression indexer,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=indexer){if(indexer.TargetObject.UserData.Contains("slang:unresolved"))
return;var ctr=resolver.GetTypeOfExpression(indexer.TargetObject);if(null!=ctr.ArrayElementType&&0<ctr.ArrayRank){var ai=new CodeArrayIndexerExpression(indexer.TargetObject);
ai.Indices.AddRange(indexer.Indices);CodeDomVisitor.ReplaceTarget(ctx,ai);}indexer.UserData.Remove("slang:unresolved");}}static void _Patch(CodeDelegateInvokeExpression
 di,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=di){ if(null!=di.TargetObject){ var mr=di.TargetObject as CodeMethodReferenceExpression;
if(null!=mr){var mi=new CodeMethodInvokeExpression(mr);mi.Parameters.AddRange(di.Parameters);CodeDomVisitor.ReplaceTarget(ctx,mi);}else{var cco=di.TargetObject
 as CodeObject;if(null==cco)System.Diagnostics.Debugger.Break();}}else{ throw new InvalidProgramException("Untargeted delegate invoke produced by slang parser!");
}}}static bool _HasUnresolved(CodeObject target){if(target.UserData.Contains("slang:unresolved"))return true;var result=false;CodeDomVisitor.Visit(target,
(ctx)=>{var co=ctx.Target as CodeObject;if(null!=co&&co.UserData.Contains("slang:unresolved")){result=true;ctx.Cancel=true;}});return result;}static void
 _Patch(CodeVariableDeclarationStatement vd,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=vd){if(vd.Name=="done")System.Diagnostics.Debug.WriteLine("debug var decl hit");
if(CodeDomResolver.IsNullOrVoidType(vd.Type)){if(null==vd.InitExpression)throw new ArgumentException("The code contains an incomplete variable declaration.",
"resolver");if(!_HasUnresolved(vd.InitExpression)){var t=resolver.GetTypeOfExpression(vd.InitExpression,resolver.GetScope(vd.InitExpression));vd.Type=
t;if(!CodeDomResolver.IsNullOrVoidType(t)){if(vd.Name=="done")System.Diagnostics.Debug.WriteLine("debug var decl resolved");vd.UserData.Remove("slang:unresolved");
}}}}}static void _Patch(CodeFieldReferenceExpression fr,CodeDomVisitContext ctx,CodeDomResolver resolver){if(null!=fr){ if(!fr.TargetObject.UserData.Contains("slang:unresolved"))
{var scope=resolver.GetScope(fr);var binder=new CodeDomBinder(scope);var t=resolver.GetTypeOfExpression(fr.TargetObject);if(null!=t&&CodeDomResolver.IsNullOrVoidType(t)
&&fr.TargetObject is CodeVariableReferenceExpression)return; var isStatic=false;var tre=fr.TargetObject as CodeTypeReferenceExpression;if(null!=tre)isStatic
=true;var tt=resolver.TryResolveType(isStatic?tre.Type:t,scope);if(null==tt)throw new InvalidOperationException(string.Format("The type {0} could not be resolved.",
t.BaseType));var td=tt as CodeTypeDeclaration; var m=binder.GetField(tt,fr.FieldName,_BindFlags);if(null!=m){fr.UserData.Remove("slang:unresolved");return;
}m=binder.GetEvent(tt,fr.FieldName,_BindFlags);if(null!=m){var er=new CodeEventReferenceExpression(fr.TargetObject,fr.FieldName);CodeDomVisitor.ReplaceTarget(ctx,
er);return;}var ml=binder.GetMethodGroup(tt,fr.FieldName,_BindFlags);if(0<ml.Length){var mr=new CodeMethodReferenceExpression(fr.TargetObject,fr.FieldName);
CodeDomVisitor.ReplaceTarget(ctx,mr);return;}ml=binder.GetPropertyGroup(tt,fr.FieldName,_BindFlags);if(0<ml.Length){var pr=new CodePropertyReferenceExpression(fr.TargetObject,
fr.FieldName);CodeDomVisitor.ReplaceTarget(ctx,pr);return;}throw new InvalidProgramException(string.Format("Cannot deterimine the target reference {0}",
fr.FieldName));} var path=_GetUnresRootPathOfExpression(fr);if(null!=path){ var scope=resolver.GetScope(fr);var sa=path.Split('.');if(1==sa.Length){System.Diagnostics.Debugger.Break();
throw new NotImplementedException();}else{object t=null;string tn=null;CodeExpression tf=fr;CodeExpression ptf=null;CodeTypeReference ctr=null;for(var
 i=sa.Length-1;i>=1;--i){tn=string.Join(".",sa,0,i);ptf=tf;tf=_GetTargetOfExpression(tf);ctr=new CodeTypeReference(tn);t=resolver.TryResolveType(ctr,scope);
if(null!=t)break;}if(null!=t){var tt=t as Type;if(null!=tt)ctr=new CodeTypeReference(tt);else ctr=resolver.GetQualifiedType(ctr,scope); _SetTargetOfExpression(ptf,
new CodeTypeReferenceExpression(ctr));return;}}}}}static CodeDelegateCreateExpression _GetDelegateFromFields(CodeObjectCreateExpression oc,CodeExpression
 target,CodeDomResolver res){var v=target as CodeVariableReferenceExpression;if(null!=v){var scope=res.GetScope(v);if(scope.MemberNames.Contains(v.VariableName))
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
if(null!=er)return er.EventName;var vr=e as CodeVariableReferenceExpression;if(null!=vr)return vr.VariableName;return null;}}}namespace CD{/// <summary>
/// Preprocesses input using a simplified T4 style syntax
/// </summary>
#if GOKITLIB
public
#endif
class SlangPreprocessor{/// <summary>
/// Preprocesses the input from <paramref name="input"/> and writes the output to <paramref name="output"/>
/// </summary>
/// <param name="input">The input source to preprocess</param>
/// <param name="output">The output target for the post-processed <paramref name="input"/></param>
public static void Preprocess(TextReader input,TextWriter output){Preprocess(input,output,"cs");}/// <summary>
/// Preprocesses the input from <paramref name="input"/> and writes the output to <paramref name="output"/>
/// </summary>
/// <param name="input">The input source to preprocess</param>
/// <param name="output">The output target for the post-processed <paramref name="input"/></param>
/// <param name="lang">The language to use for the T4 code - defaults to C#</param>
public static void Preprocess(TextReader input,TextWriter output,string lang){CompilerErrorCollection errors=null; var method=new CodeMemberMethod();method.Attributes
=MemberAttributes.Public|MemberAttributes.Static;method.Name="Preprocess";method.Parameters.Add(new CodeParameterDeclarationExpression(typeof(TextWriter),
"Response"));int cur;var more=true;while(more){var text=_ReadUntilStartContext(input);if(0<text.Length){method.Statements.Add(new CodeMethodInvokeExpression(
new CodeArgumentReferenceExpression("Response"),"Write",new CodePrimitiveExpression(text)));}cur=input.Read();if(-1==cur)more=false;else if('='==cur){
method.Statements.Add(new CodeMethodInvokeExpression(new CodeArgumentReferenceExpression("Response"),"Write",new CodeSnippetExpression(_ReadUntilEndContext(-1,
input))));}else method.Statements.Add(new CodeSnippetStatement(_ReadUntilEndContext(cur,input)));}method.Statements.Add(new CodeMethodInvokeExpression(new
 CodeArgumentReferenceExpression("Response"),"Flush"));var cls=new CodeTypeDeclaration("Preprocessor");cls.TypeAttributes=TypeAttributes.Public;cls.IsClass
=true;cls.Members.Add(method);var ns=new CodeNamespace();ns.Types.Add(cls);var cu=new CodeCompileUnit();cu.Namespaces.Add(ns);var prov=CodeDomProvider.CreateProvider(lang);
var opts=new CompilerParameters();var outp=prov.CompileAssemblyFromDom(opts,cu);var asm=outp.CompiledAssembly;var ran=false;if(null!=asm){var t=asm.GetType("Preprocessor");
var m=t.GetMethod("Preprocess");if(null!=m){try{m.Invoke(null,new object[]{output});ran=true;}catch(TargetInvocationException tex){throw tex.InnerException;
}}}if(!ran){errors=outp.Errors;if(0<errors.Count){CompilerError err=errors[0];throw new InvalidOperationException(err.ErrorText);}}}static string _ReadUntilStartContext(TextReader
 input){int cur=input.Read();var sb=new StringBuilder();while(true){if('<'==cur){cur=input.Read();if(-1==cur){sb.Append('<');return sb.ToString();}else
 if('#'==cur)return sb.ToString();sb.Append('<');}else if(-1==cur)return sb.ToString();sb.Append((char)cur);cur=input.Read();}}static string _ReadUntilEndContext(int
 firstChar,TextReader input){int cur;cur=firstChar;if(-1==firstChar)cur=input.Read();var sb=new StringBuilder();while(true){if('#'==cur){cur=input.Read();
if(-1==cur){sb.Append('#');return sb.ToString();}else if('>'==cur)return sb.ToString();sb.Append('>');}else if(-1==cur)return sb.ToString();sb.Append((char)cur);
cur=input.Read();}}}}namespace CD{/// <summary>
/// Represents a syntax error raised by <see cref="SlangParser"/>
/// </summary>
#if GOKITLIB
public
#endif
class SlangSyntaxException:Exception{/// <summary>
/// Creates a syntax exception with the specified arguments
/// </summary>
/// <param name="message">The error message</param>
/// <param name="line">The line where the error occurred</param>
/// <param name="column">The column where the error occured</param>
/// <param name="position">The position where the error occured</param>
public SlangSyntaxException(string message,int line,int column,long position):base(_GetMessage(message,line,column,position)){Line=line;Column=column;
Position=position;}/// <summary>
/// The line where the error occurred
/// </summary>
public int Line{get;}/// <summary>
/// The column where the error occurred
/// </summary>
public int Column{get;}/// <summary>
/// The position where the error occurred
/// </summary>
public long Position{get;}static string _GetMessage(string message,int line,int column,long position){return string.Format("{0} at line {1}, column {2}, position {3}",message,line,column,position);
}}}namespace CD{using System;using System.Collections.Generic;using System.Text;/// <summary>
/// Reference implementation for generated shared code
/// </summary>
[System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex","0.2.0.0")]internal struct Token{/// <summary>
/// Indicates the line where the token occurs
/// </summary>
public int Line;/// <summary>
/// Indicates the column where the token occurs
/// </summary>
public int Column;/// <summary>
/// Indicates the position where the token occurs
/// </summary>
public long Position;/// <summary>
/// Indicates the symbol id or -1 for the error symbol
/// </summary>
public int SymbolId;/// <summary>
/// Indicates the value of the token
/// </summary>
public string Value;}/// <summary>
/// Reference implementation for a DfaEntry
/// </summary>
[System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex","0.2.0.0")]internal struct DfaEntry{/// <summary>
/// The state transitions
/// </summary>
public DfaTransitionEntry[]Transitions;/// <summary>
/// The accept symbol id or -1 for non-accepting
/// </summary>
public int AcceptSymbolId;/// <summary>
/// Constructs a new instance
/// </summary>
/// <param name="transitions">The state transitions</param>
/// <param name="acceptSymbolId">The accept symbol id</param>
public DfaEntry(DfaTransitionEntry[]transitions,int acceptSymbolId){this.Transitions=transitions;this.AcceptSymbolId=acceptSymbolId;}}[System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex",
"0.2.0.0")]internal struct DfaTransitionEntry{/// <summary>
/// The character ranges, packed as adjacent pairs.
/// </summary>
public char[]PackedRanges;/// <summary>
/// The destination state
/// </summary>
public int Destination;/// <summary>
/// Constructs a new instance
/// </summary>
/// <param name="packedRanges">The packed character ranges</param>
/// <param name="destination">The destination state</param>
public DfaTransitionEntry(char[]packedRanges,int destination){this.PackedRanges=packedRanges;this.Destination=destination;}}[System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex",
"0.2.0.0")]internal class TableTokenizer:object,IEnumerable<Token>{public const int ErrorSymbol=-1; private DfaEntry[]_dfaTable; private string[]_blockEnds;
 private int[]_nodeFlags; private IEnumerable<char>_input;/// <summary>
/// Retrieves an enumerator that can be used to iterate over the tokens
/// </summary>
/// <returns>An enumerator that can be used to iterate over the tokens</returns>
public IEnumerator<Token>GetEnumerator(){ return new TableTokenizerEnumerator(this._dfaTable,this._blockEnds,this._nodeFlags,this._input.GetEnumerator());
} System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator(){return this.GetEnumerator();}/// <summary>
/// Constructs a new instance
/// </summary>
/// <param name="dfaTable">The DFA state table to use</param>
/// <param name="blockEnds">The block ends table</param>
/// <param name="nodeFlags">The node flags table</param>
/// <param name="input">The input character sequence</param>
public TableTokenizer(DfaEntry[]dfaTable,string[]blockEnds,int[]nodeFlags,IEnumerable<char>input){if((null==dfaTable)){throw new ArgumentNullException("dfaTable");
}if((null==blockEnds)){throw new ArgumentNullException("blockEnds");}if((null==nodeFlags)){throw new ArgumentNullException("nodeFlags");}if((null==input))
{throw new ArgumentNullException("input");}this._dfaTable=dfaTable;this._blockEnds=blockEnds;this._nodeFlags=nodeFlags;this._input=input;}}[System.CodeDom.Compiler.GeneratedCodeAttribute("Rolex",
"0.2.0.0")]internal class TableTokenizerEnumerator:object,IEnumerator<Token>{ public const int ErrorSymbol=-1; private const int _EosSymbol=-2; private
 const int _Disposed=-4; private const int _BeforeBegin=-3; private const int _AfterEnd=-2; private const int _InnerFinished=-1; private const int _Enumerating
=0; private const int _TabWidth=4; private DfaEntry[]_dfaTable; private string[]_blockEnds; private int[]_nodeFlags; private IEnumerator<char>_input; private
 int _state; private Token _current; private StringBuilder _buffer; private int _line; private int _column; private long _position;public TableTokenizerEnumerator(DfaEntry[]
dfaTable,string[]blockEnds,int[]nodeFlags,IEnumerator<char>input){ this._dfaTable=dfaTable;this._blockEnds=blockEnds;this._nodeFlags=nodeFlags;this._input
=input;this._state=TableTokenizerEnumerator._BeforeBegin;this._buffer=new StringBuilder();this._line=1;this._column=1;this._position=0;}public Token Current
{get{ if((TableTokenizerEnumerator._Enumerating>this._state)){ if((TableTokenizerEnumerator._BeforeBegin==this._state)){throw new InvalidOperationException("The cursor is before the start of the enumeration");
}if((TableTokenizerEnumerator._AfterEnd==this._state)){throw new InvalidOperationException("The cursor is after the end of the enumeration");}if((TableTokenizerEnumerator._Disposed
==this._state)){TableTokenizerEnumerator._ThrowDisposed();}}return this._current;}}object System.Collections.IEnumerator.Current{get{return this.Current;
}}void System.Collections.IEnumerator.Reset(){if((TableTokenizerEnumerator._Disposed==this._state)){TableTokenizerEnumerator._ThrowDisposed();}if((false
==(TableTokenizerEnumerator._BeforeBegin==this._state))){this._input.Reset();}this._state=TableTokenizerEnumerator._BeforeBegin;this._line=1;this._column
=1;this._position=0;}bool System.Collections.IEnumerator.MoveNext(){ if((TableTokenizerEnumerator._Enumerating>this._state)){if((TableTokenizerEnumerator._Disposed
==this._state)){TableTokenizerEnumerator._ThrowDisposed();}if((TableTokenizerEnumerator._AfterEnd==this._state)){return false;}}this._current=default(Token);
this._current.Line=this._line;this._current.Column=this._column;this._current.Position=this._position;this._buffer.Clear(); this._current.SymbolId=this._Lex();
 bool done=false;for(;(false==done);){done=true; if((TableTokenizerEnumerator.ErrorSymbol<this._current.SymbolId)){ string be=this._blockEnds[this._current.SymbolId];
if(((null!=be)&&(false==(0==be.Length)))){ if((false==this._TryReadUntilBlockEnd(be))){this._current.SymbolId=TableTokenizerEnumerator.ErrorSymbol;}}if
(((TableTokenizerEnumerator.ErrorSymbol<this._current.SymbolId)&&(false==(0==(this._nodeFlags[this._current.SymbolId]&1))))){ done=false;this._current.Line
=this._line;this._current.Column=this._column;this._current.Position=this._position;this._buffer.Clear();this._current.SymbolId=this._Lex();}}}this._current.Value
=this._buffer.ToString(); if((TableTokenizerEnumerator._EosSymbol==this._current.SymbolId)){this._state=TableTokenizerEnumerator._AfterEnd;}return(false
==(TableTokenizerEnumerator._AfterEnd==this._state));}void IDisposable.Dispose(){this._input.Dispose();this._state=TableTokenizerEnumerator._Disposed;
} bool _MoveNextInput(){if(this._input.MoveNext()){if((false==(TableTokenizerEnumerator._BeforeBegin==this._state))){this._position=(this._position+1);
if(('\n'==this._input.Current)){this._column=1;this._line=(this._line+1);}else{if(('\t'==this._input.Current)){this._column=(this._column+TableTokenizerEnumerator._TabWidth);
}else{this._column=(this._column+1);}}}else{ if(('\n'==this._input.Current)){this._column=1;this._line=(this._line+1);}else{if(('\t'==this._input.Current))
{this._column=(this._column+(TableTokenizerEnumerator._TabWidth-1));}}}return true;}this._state=TableTokenizerEnumerator._InnerFinished;return false;}
 bool _TryReadUntil(char character){char ch=this._input.Current;this._buffer.Append(ch);if((ch==character)){return true;}for(;(this._MoveNextInput()&&
(false==(this._input.Current==character)));){this._buffer.Append(this._input.Current);}if((false==(this._state==TableTokenizerEnumerator._InnerFinished)))
{this._buffer.Append(this._input.Current);return(this._input.Current==character);}return false;} bool _TryReadUntilBlockEnd(string blockEnd){for(;((false
==(TableTokenizerEnumerator._InnerFinished==this._state))&&this._TryReadUntil(blockEnd[0]));){bool found=true;for(int i=1;(found&&(i<blockEnd.Length));
i=(i+1)){if((false==(this._MoveNextInput()||(false==(this._input.Current==blockEnd[i]))))){found=false;}else{if((false==(TableTokenizerEnumerator._InnerFinished
==this._state))){this._buffer.Append(this._input.Current);}}}if(found){this._MoveNextInput();return true;}}return false;} int _Lex(){ int acceptSymbolId;
int dfaState=0;if((TableTokenizerEnumerator._BeforeBegin==this._state)){if((false==this._MoveNextInput())){ acceptSymbolId=this._dfaTable[dfaState].AcceptSymbolId;
if((false==(-1==acceptSymbolId))){return acceptSymbolId;}else{return TableTokenizerEnumerator.ErrorSymbol;}}this._state=TableTokenizerEnumerator._Enumerating;
}else{if(((TableTokenizerEnumerator._InnerFinished==this._state)||(TableTokenizerEnumerator._AfterEnd==this._state))){ return TableTokenizerEnumerator._EosSymbol;
}}bool done=false;for(;(false==done);){int nextDfaState=-1;for(int i=0;(i<this._dfaTable[dfaState].Transitions.Length);i=(i+1)){DfaTransitionEntry entry
=this._dfaTable[dfaState].Transitions[i];bool found=false;for(int j=0;(j<entry.PackedRanges.Length);j=(j+1)){char ch=this._input.Current;char first=entry.PackedRanges[j];
j=(j+1);char last=entry.PackedRanges[j];if((ch<=last)){if((first<=ch)){found=true;}j=(int.MaxValue-1);}}if(found){ nextDfaState=entry.Destination;i=(int.MaxValue
-1);}}if((false==(-1==nextDfaState))){ this._buffer.Append(this._input.Current); dfaState=nextDfaState;if((false==this._MoveNextInput())){ acceptSymbolId
=this._dfaTable[dfaState].AcceptSymbolId;if((false==(-1==acceptSymbolId))){return acceptSymbolId;}else{return TableTokenizerEnumerator.ErrorSymbol;}}}
else{done=true;}}acceptSymbolId=this._dfaTable[dfaState].AcceptSymbolId;if((false==(-1==acceptSymbolId))){return acceptSymbolId;}else{ this._buffer.Append(this._input.Current);
this._MoveNextInput();return TableTokenizerEnumerator.ErrorSymbol;}}static void _ThrowDisposed(){throw new ObjectDisposedException("TableTokenizerEnumerator");
}}internal class SlangTokenizer:TableTokenizer{internal static DfaEntry[]DfaTable=new DfaEntry[]{new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'a','a'},1),new DfaTransitionEntry(new char[]{'b','b'},25),new DfaTransitionEntry(new char[]{'c','c'},39),new DfaTransitionEntry(new char[]{'d',
'd'},67),new DfaTransitionEntry(new char[]{'e','e'},104),new DfaTransitionEntry(new char[]{'f','f'},131),new DfaTransitionEntry(new char[]{'g','g'},155),
new DfaTransitionEntry(new char[]{'i','i'},166),new DfaTransitionEntry(new char[]{'l','l'},187),new DfaTransitionEntry(new char[]{'n','n'},193),new DfaTransitionEntry(new
 char[]{'o','o'},207),new DfaTransitionEntry(new char[]{'p','p'},229),new DfaTransitionEntry(new char[]{'r','r'},257),new DfaTransitionEntry(new char[]
{'s','s'},270),new DfaTransitionEntry(new char[]{'t','t'},314),new DfaTransitionEntry(new char[]{'u','u'},330),new DfaTransitionEntry(new char[]{'v','v'},
358),new DfaTransitionEntry(new char[]{'w','w'},376),new DfaTransitionEntry(new char[]{'y','y'},381),new DfaTransitionEntry(new char[]{'A','Z','_','_',
'h','h','j','k','m','m','q','q','x','x','z','z'},386),new DfaTransitionEntry(new char[]{'/','/'},387),new DfaTransitionEntry(new char[]{'\"','\"'},391),
new DfaTransitionEntry(new char[]{'\'','\''},394),new DfaTransitionEntry(new char[]{'\t','\r',' ',' '},398),new DfaTransitionEntry(new char[]{'<','<'},
399),new DfaTransitionEntry(new char[]{'>','>'},401),new DfaTransitionEntry(new char[]{'=','='},403),new DfaTransitionEntry(new char[]{'!','!'},405),new
 DfaTransitionEntry(new char[]{'+','+'},407),new DfaTransitionEntry(new char[]{'-','-'},410),new DfaTransitionEntry(new char[]{'*','*'},413),new DfaTransitionEntry(new
 char[]{'%','%'},415),new DfaTransitionEntry(new char[]{'&','&'},417),new DfaTransitionEntry(new char[]{'|','|'},420),new DfaTransitionEntry(new char[]
{'[','['},423),new DfaTransitionEntry(new char[]{']',']'},424),new DfaTransitionEntry(new char[]{'(','('},425),new DfaTransitionEntry(new char[]{')',')'},
426),new DfaTransitionEntry(new char[]{'{','{'},427),new DfaTransitionEntry(new char[]{'}','}'},428),new DfaTransitionEntry(new char[]{',',','},429),new
 DfaTransitionEntry(new char[]{':',':'},430),new DfaTransitionEntry(new char[]{';',';'},432),new DfaTransitionEntry(new char[]{'.','.'},433),new DfaTransitionEntry(new
 char[]{'0','0'},439),new DfaTransitionEntry(new char[]{'1','9'},558),new DfaTransitionEntry(new char[]{'#','#'},560)},-1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'b','b'},2),new DfaTransitionEntry(new char[]{'s','s'},10),new DfaTransitionEntry(new char[]{'w','w'},21),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','a','c','r','t','v','x','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},
3),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'t','t'},4),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'r','r'},5),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'a','a'},6),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'c','c'},7),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'t','t'},8),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},
9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},11),new DfaTransitionEntry(new char[]{'y','y'},18),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','x','z','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},12),new
 DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'n','n'},13),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'d','d'},14),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'i','i'},15),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'n','n'},16),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'g','g'},17),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','f','h','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},19),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},20),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},22),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},23),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},24),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},26),new DfaTransitionEntry(new char[]{'o','o'},29),new
 DfaTransitionEntry(new char[]{'r','r'},32),new DfaTransitionEntry(new char[]{'y','y'},36),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b',
'n','p','q','s','x','z','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},27),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},28),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},30),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},31),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},
9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},33),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},34),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'k','k'},35),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','j','l','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},
9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},37),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},38),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},40),new DfaTransitionEntry(new char[]{'h','h'},46),new DfaTransitionEntry(new
 char[]{'l','l'},54),new DfaTransitionEntry(new char[]{'o','o'},58),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','g','i','k','m','n',
'p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},41),new DfaTransitionEntry(new char[]{'t','t'},43),new
 DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','r','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'e','e'},42),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},44),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'h','h'},45),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','g','i','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},47),new DfaTransitionEntry(new char[]{'e','e'},49),new
 DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'r','r'},48),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},50),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'k','k'},51),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','j','l','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},52),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},53),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},55),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},56),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},57),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},59),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},60),new DfaTransitionEntry(new char[]
{'t','t'},62),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','r','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'t','t'},61),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},63),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},64),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'u','u'},65),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','t','v','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},66),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},68),new DfaTransitionEntry(new char[]{'o','o'},93),new
 DfaTransitionEntry(new char[]{'y','y'},98),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','n','p','x','z','z'},9)},1),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},69),new DfaTransitionEntry(new char[]{'f','f'},74),new DfaTransitionEntry(new char[]{
'l','l'},79),new DfaTransitionEntry(new char[]{'s','s'},85),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','e','g','k','m','r',
't','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},70),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'m','m'},71),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','l','n','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},72),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},73),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},75),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'u','u'},76),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','a','t','v','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},77),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},78),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},80),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'g','g'},81),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','f','h','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},82),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},83),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},84),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},86),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},87),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},88),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},89),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},90),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},91),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'g','g'},92),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','f','h','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'u','u'},94),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','t','v','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'b','b'},95),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','a','c','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},96),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},97),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},99),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},100),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'m','m'},101),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','l','n','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},102),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},103),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},105),new DfaTransitionEntry(new char[]{'n','n'},108),
new DfaTransitionEntry(new char[]{'q','q'},111),new DfaTransitionEntry(new char[]{'x','x'},116),new DfaTransitionEntry(new char[]{'v','v'},127),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','m','o','p','r','u','w','w','y','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'s','s'},106),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'e','e'},107),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'u','u'},109),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','t','v','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'m','m'},110),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','l','n','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'u','u'},112),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','t','v','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},113),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},114),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},115),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'p','p'},117),new DfaTransitionEntry(new char[]{'t','t'},123),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','o','q','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'l','l'},118),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'i','i'},119),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'c','c'},120),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'i','i'},121),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'t','t'},122),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},124),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},125),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},126),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},128),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},129),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},130),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},132),new DfaTransitionEntry(new char[]{'i','i'},136),
new DfaTransitionEntry(new char[]{'l','l'},145),new DfaTransitionEntry(new char[]{'o','o'},149),new DfaTransitionEntry(new char[]{'0','9','A','Z','_',
'_','b','h','j','k','m','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},133),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},134),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},135),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},137),new DfaTransitionEntry(new char[]{'x','x'},142),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','m','o','w','y','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'a','a'},138),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'l','l'},139),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'l','l'},140),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'y','y'},141),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','x','z','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},143),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},144),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},146),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},147),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},148),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},150),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},151),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','d','f','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},152),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},153),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'h','h'},154),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','g','i','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},156),new DfaTransitionEntry(new char[]{'l','l'},158),
new DfaTransitionEntry(new char[]{'o','o'},163),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','k','m','n','p','z'},9)},1),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},157),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{
new DfaTransitionEntry(new char[]{'o','o'},159),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'b','b'},160),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','a','c','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'a','a'},161),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'l','l'},162),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t',
't'},164),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'o','o'},165),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'f','f'},167),new DfaTransitionEntry(new
 char[]{'m','m'},168),new DfaTransitionEntry(new char[]{'n','n'},175),new DfaTransitionEntry(new char[]{'s','s'},186),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','e','g','l','o','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'p','p'},169),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','a','o','q','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},170),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},171),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},172),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},173),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},174),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},176),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},177),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','d','f','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},178),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'f','f'},179),new DfaTransitionEntry(new
 char[]{'n','n'},183),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','e','g','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{
new DfaTransitionEntry(new char[]{'a','a'},180),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'c','c'},181),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'e','e'},182),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a',
'a'},184),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'l','l'},185),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},
9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},188),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},189),new DfaTransitionEntry(new char[]{'n','n'},
191),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'k','k'},190),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','j','l','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'g','g'},192),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','f','h','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},194),new DfaTransitionEntry(new char[]{'e','e'},202),
new DfaTransitionEntry(new char[]{'u','u'},204),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','d','f','t','v','z'},9)},1),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'m','m'},195),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','l','n','z'},9)},1),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},196),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d',
'f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},197),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'p','p'},198),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','o','q','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},199),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},200),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},201),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'w','w'},203),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','v','x','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},205),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},206),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k',
'm','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'b','b'},208),new DfaTransitionEntry(new char[]{'p','p'},213),new DfaTransitionEntry(new char[]{'u','u'},220),new DfaTransitionEntry(new
 char[]{'v','v'},222),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','a','c','o','q','t','w','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'j','j'},209),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','i','k','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'e','e'},210),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'c','c'},211),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'t','t'},212),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e',
'e'},214),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'r','r'},215),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'a','a'},216),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'t','t'},217),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'o','o'},218),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'r','r'},219),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},221),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},223),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},224),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},225),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},226),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},227),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},228),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},230),new DfaTransitionEntry(new char[]{'r','r'},239),
new DfaTransitionEntry(new char[]{'u','u'},252),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','q','s','t','v','z'},9)},1),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},231),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},232),new DfaTransitionEntry(new char[]{'t','t'},235),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','b','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'m','m'},233),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','l','n','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},234),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},236),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},237),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},238),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},240),new DfaTransitionEntry(new char[]{'o','o'},245),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','h','j','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'v','v'},241),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','u','w','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'a','a'},242),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'t','t'},243),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'e','e'},244),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},246),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},247),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},248),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},249),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},250),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},251),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'b','b'},253),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','a','c','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},254),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},255),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},256),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},258),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},259),new DfaTransitionEntry(new char[]
{'f','f'},265),new DfaTransitionEntry(new char[]{'t','t'},266),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','e','g','s','u','z'},9)},
1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},260),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a',
'c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},261),new DfaTransitionEntry(new char[]{'0','9','A',
'Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},262),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},263),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'y','y'},264),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','x','z','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'u','u'},267),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','t','v','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'r','r'},268),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'n','n'},269),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'b',
'b'},271),new DfaTransitionEntry(new char[]{'e','e'},275),new DfaTransitionEntry(new char[]{'h','h'},281),new DfaTransitionEntry(new char[]{'i','i'},285),
new DfaTransitionEntry(new char[]{'t','t'},290),new DfaTransitionEntry(new char[]{'w','w'},309),new DfaTransitionEntry(new char[]{'0','9','A','Z','_',
'_','a','a','c','d','f','g','j','s','u','v','x','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'y','y'},272),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','x','z','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},273),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},274),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},276),new DfaTransitionEntry(new char[]{'t','t'},280),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'l','l'},277),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'e','e'},278),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'d','d'},279),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},
9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},282),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},283),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},284),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'z','z'},286),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','y'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},287),new DfaTransitionEntry(new char[]{'0','9',
'A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},288),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'f','f'},289),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','e','g','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},291),new DfaTransitionEntry(new char[]{'r','r'},302),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'c','c'},292),new DfaTransitionEntry(new char[]{'t','t'},299),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','s','u','z'},9)},
1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'k','k'},293),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a',
'j','l','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},294),new DfaTransitionEntry(new char[]{'0','9','A',
'Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},295),new DfaTransitionEntry(new char[]{'0',
'9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},296),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},297),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},298),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},300),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},301),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},
9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},303),new DfaTransitionEntry(new char[]{'u','u'},306),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','t','v','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},304),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'g','g'},305),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','f','h','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},307),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},308),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},310),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},311),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},312),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'h','h'},313),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','g','i','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'h','h'},315),new DfaTransitionEntry(new char[]{'r','r'},321),
new DfaTransitionEntry(new char[]{'y','y'},325),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','g','i','q','s','x','z','z'},9)},1),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},316),new DfaTransitionEntry(new char[]{'r','r'},318),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','h','j','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'s','s'},317),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},319),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'w','w'},320),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','v','x','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},
9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'u','u'},322),new DfaTransitionEntry(new char[]{'y','y'},324),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','t','v','x','z','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},323),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},
0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'p','p'},326),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a',
'o','q','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},327),new DfaTransitionEntry(new char[]{'0','9','A',
'Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},328),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'f','f'},329),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','e','g','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},331),new DfaTransitionEntry(new char[]{'l','l'},334),
new DfaTransitionEntry(new char[]{'n','n'},338),new DfaTransitionEntry(new char[]{'s','s'},350),new DfaTransitionEntry(new char[]{'0','9','A','Z','_',
'_','a','h','j','k','m','m','o','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},332),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'t','t'},333),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'o','o'},335),new DfaTransitionEntry(new char[]{'0','9','A','Z',
'_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},336),new DfaTransitionEntry(new char[]
{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'g','g'},337),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','f','h','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'c','c'},339),new DfaTransitionEntry(new char[]{'s','s'},346),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','r','t','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'h','h'},340),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','g','i','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'e','e'},341),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'c','c'},342),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','b','d','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'k','k'},343),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','j','l','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'e','e'},344),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'d','d'},345),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},347),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'f','f'},348),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','e','g','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},349),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'h','h'},351),new DfaTransitionEntry(new char[]{'i','i'},355),
new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','g','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'o','o'},352),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','n','p','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'r','r'},353),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'t','t'},354),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'n','n'},356),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','m','o','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'g','g'},357),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','f','h','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','a'},359),new DfaTransitionEntry(new char[]{'i','i'},361),
new DfaTransitionEntry(new char[]{'o','o'},367),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','h','j','n','p','z'},9)},1),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'r','r'},360),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{
new DfaTransitionEntry(new char[]{'r','r'},362),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','q','s','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'t','t'},363),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'u','u'},364),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','t','v','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'a','a'},365),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'l','l'},366),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i',
'i'},368),new DfaTransitionEntry(new char[]{'l','l'},370),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','h','j','k','m','z'},9)},1),new
 DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},369),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','c','e',
'z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'a','a'},371),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','b','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'t','t'},372),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','s','u','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'i','i'},373),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'l','l'},374),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'e','e'},375),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'h',
'h'},377),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','g','i','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'i','i'},378),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'l','l'},379),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'e','e'},380),new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'i','i'},382),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','h','j','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'e','e'},383),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','d','f','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'l','l'},384),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','k','m','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'d','d'},385),new DfaTransitionEntry(new
 char[]{'0','9','A','Z','_','_','a','c','e','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_',
'a','z'},9)},0),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','Z','_','_','a','z'},9)},1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'/','/'},388),new DfaTransitionEntry(new char[]{'*','*'},389),new DfaTransitionEntry(new char[]{'=','='},390)},23),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\0','\t','','￿'},388)},2),new DfaEntry(new DfaTransitionEntry[0],3),new DfaEntry(new
 DfaTransitionEntry[0],22),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\0','!','#','[',']','￿'},391),new DfaTransitionEntry(new
 char[]{'\\','\\'},392),new DfaTransitionEntry(new char[]{'\"','\"'},393)},-1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'\0','￿'},391)},-1),new DfaEntry(new DfaTransitionEntry[0],4),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\0','&','(','[',
']','￿'},395),new DfaTransitionEntry(new char[]{'\\','\\'},397)},-1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\'','\''},
396)},-1),new DfaEntry(new DfaTransitionEntry[0],5),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\0','￿'},395)},-1),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\t','\r',' ',' '},398)},6),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]
{'=','='},400)},8),new DfaEntry(new DfaTransitionEntry[0],7),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'=','='},402)},10),
new DfaEntry(new DfaTransitionEntry[0],9),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'=','='},404)},13),new DfaEntry(new DfaTransitionEntry[0],
11),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'=','='},406)},32),new DfaEntry(new DfaTransitionEntry[0],12),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'+','+'},408),new DfaTransitionEntry(new char[]{'=','='},409)},16),new DfaEntry(new DfaTransitionEntry[0],
14),new DfaEntry(new DfaTransitionEntry[0],15),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'-','-'},411),new DfaTransitionEntry(new
 char[]{'=','='},412)},19),new DfaEntry(new DfaTransitionEntry[0],17),new DfaEntry(new DfaTransitionEntry[0],18),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'=','='},414)},21),new DfaEntry(new DfaTransitionEntry[0],20),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'=','='},416)},25),new DfaEntry(new DfaTransitionEntry[0],24),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'&','&'},
418),new DfaTransitionEntry(new char[]{'=','='},419)},28),new DfaEntry(new DfaTransitionEntry[0],26),new DfaEntry(new DfaTransitionEntry[0],27),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'|','|'},421),new DfaTransitionEntry(new char[]{'=','='},422)},31),new DfaEntry(new DfaTransitionEntry[0],
29),new DfaEntry(new DfaTransitionEntry[0],30),new DfaEntry(new DfaTransitionEntry[0],33),new DfaEntry(new DfaTransitionEntry[0],34),new DfaEntry(new DfaTransitionEntry[0],
35),new DfaEntry(new DfaTransitionEntry[0],36),new DfaEntry(new DfaTransitionEntry[0],37),new DfaEntry(new DfaTransitionEntry[0],38),new DfaEntry(new DfaTransitionEntry[0],
39),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{':',':'},431)},41),new DfaEntry(new DfaTransitionEntry[0],40),new DfaEntry(new
 DfaTransitionEntry[0],42),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9'},434)},43),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9'},434),new DfaTransitionEntry(new char[]{'E','E','e','e'},435),new DfaTransitionEntry(new char[]{'D','D','F',
'F','M','M','d','d','f','f','m','m'},438)},45),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'+','+','-','-'},436),new DfaTransitionEntry(new
 char[]{'0','9'},437)},-1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9'},437)},-1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9'},437),new DfaTransitionEntry(new char[]{'D','D','F','F','M','M','d','d','f','f','m','m'},438)},45),new DfaEntry(new
 DfaTransitionEntry[0],45),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'x','x'},440),new DfaTransitionEntry(new char[]{'U',
'U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550),new DfaTransitionEntry(new char[]{'.','.'},552),new DfaTransitionEntry(new char[]
{'0','9'},553),new DfaTransitionEntry(new char[]{'E','E','e','e'},554),new DfaTransitionEntry(new char[]{'D','D','F','F','M','M','d','d','f','f','m','m'},
557)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},441)},-1),new DfaEntry(new DfaTransitionEntry[]
{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},442),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]
{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},443),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},444),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},445),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},446),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},447),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},448),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},449),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},450),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},451),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},452),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},453),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},454),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},455),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},456),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},457),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},458),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},459),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},460),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},461),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},462),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},463),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},464),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},465),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},466),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},467),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},468),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},469),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},470),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},471),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},472),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},473),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},474),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},475),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},476),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},477),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},478),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},479),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},480),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},481),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},482),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},483),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},484),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},485),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},486),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},487),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},488),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},489),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},490),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},491),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},492),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},493),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},494),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},495),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},496),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},497),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},498),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},499),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},500),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},501),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},502),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},503),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},504),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},505),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},506),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},507),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},508),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},509),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},510),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},511),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},512),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},513),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},514),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},515),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},516),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},517),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},518),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},519),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},520),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},521),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},522),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},523),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},524),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},525),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},526),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},527),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},528),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},529),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},530),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},531),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},532),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},533),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},534),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},535),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},536),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},537),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},538),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},539),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},540),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},541),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},542),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},543),new DfaTransitionEntry(new
 char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9','A','F','a','f'},544),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},545),new DfaTransitionEntry(new char[]{'U','U','u',
'u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9',
'A','F','a','f'},546),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550)},44),new DfaEntry(new
 DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9','A','F','a','f'},547),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'L','L','l','l'},549)},44),new DfaEntry(new
 DfaTransitionEntry[0],44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'U','U','u','u'},551)},44),new DfaEntry(new DfaTransitionEntry[0],
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9'},553)},-1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9'},553),new DfaTransitionEntry(new char[]{'E','E','e','e'},554),new DfaTransitionEntry(new char[]{'D','D','F','F','M','M','d','d','f','f',
'm','m'},557)},45),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'+','+','-','-'},555),new DfaTransitionEntry(new char[]{'0',
'9'},556)},-1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9'},556)},-1),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new
 char[]{'0','9'},556),new DfaTransitionEntry(new char[]{'D','D','F','F','M','M','d','d','f','f','m','m'},557)},45),new DfaEntry(new DfaTransitionEntry[0],
45),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9'},559),new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new
 char[]{'L','L','l','l'},550),new DfaTransitionEntry(new char[]{'.','.'},552),new DfaTransitionEntry(new char[]{'E','E','e','e'},554),new DfaTransitionEntry(new
 char[]{'D','D','F','F','M','M','d','d','f','f','m','m'},557)},44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'0','9'},559),
new DfaTransitionEntry(new char[]{'U','U','u','u'},548),new DfaTransitionEntry(new char[]{'L','L','l','l'},550),new DfaTransitionEntry(new char[]{'.',
'.'},552),new DfaTransitionEntry(new char[]{'E','E','e','e'},554),new DfaTransitionEntry(new char[]{'D','D','F','F','M','M','d','d','f','f','m','m'},557)},
44),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\t','\t',' ',' '},560),new DfaTransitionEntry(new char[]{'a','z'},561)},-1),
new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'a','z'},561),new DfaTransitionEntry(new char[]{'\t','\t',' ',' '},562),new DfaTransitionEntry(new
 char[]{'\0','','','','!','`','{','￿'},563)},46),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\t','\t',' ',' '},562),new
 DfaTransitionEntry(new char[]{'\0','','','','!','￿'},563)},46),new DfaEntry(new DfaTransitionEntry[]{new DfaTransitionEntry(new char[]{'\0','\t','',
'￿'},563)},46)};internal static int[]NodeFlags=new int[]{0,0,0,0,0,0,1,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0,0};
internal static string[]BlockEnds=new string[]{null,null,null,"*/",null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,
null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null,null};public SlangTokenizer(IEnumerable<char>
input):base(SlangTokenizer.DfaTable,SlangTokenizer.BlockEnds,SlangTokenizer.NodeFlags,input){}public const int keyword=0;public const int identifier=1;
public const int lineComment=2;public const int blockComment=3;public const int stringLiteral=4;public const int characterLiteral=5;public const int whitespace
=6;public const int lte=7;public const int lt=8;public const int gte=9;public const int gt=10;public const int eqEq=11;public const int notEq=12;public
 const int eq=13;public const int inc=14;public const int addAssign=15;public const int add=16;public const int dec=17;public const int subAssign=18;public
 const int sub=19;public const int mulAssign=20;public const int mul=21;public const int divAssign=22;public const int div=23;public const int modAssign
=24;public const int mod=25;public const int and=26;public const int bitwiseAndAssign=27;public const int bitwiseAnd=28;public const int or=29;public const
 int bitwiseOrAssign=30;public const int bitwiseOr=31;public const int not=32;public const int lbracket=33;public const int rbracket=34;public const int
 lparen=35;public const int rparen=36;public const int lbrace=37;public const int rbrace=38;public const int comma=39;public const int colonColon=40;public
 const int colon=41;public const int semi=42;public const int dot=43;public const int integerLiteral=44;public const int floatLiteral=45;public const int
 directive=46;}}namespace CD{sealed class FileReaderEnumerable:TextReaderEnumerable{protected override bool CanCreateReader=>true;readonly string _filename;
public FileReaderEnumerable(string filename){if(null==filename)throw new ArgumentNullException("filename");if(0==filename.Length)throw new ArgumentException("The filename must not be empty.",
"filename");_filename=filename;}protected override TextReader CreateTextReader(){return File.OpenText(_filename);}}sealed class ConsoleReaderEnumerable
:TextReaderEnumerable{protected override bool CanCreateReader=>false;public ConsoleReaderEnumerable(){}protected override TextReader CreateTextReader()
{return Console.In;}}sealed class UrlReaderEnumerable:TextReaderEnumerable{protected override bool CanCreateReader=>true;readonly string _url;public UrlReaderEnumerable(string
 url){if(null==url)throw new ArgumentNullException("url");if(0==url.Length)throw new ArgumentException("The url must not be empty.","url");_url=url;}protected
 override TextReader CreateTextReader(){var wq=WebRequest.Create(_url);var wr=wq.GetResponse();return new StreamReader(wr.GetResponseStream());}}abstract
 class TextReaderEnumerable:IEnumerable<char>{
#region _OnceReaderEnumerable
sealed class _OnceTextReaderEnumerable:TextReaderEnumerable{TextReader _reader;internal _OnceTextReaderEnumerable(TextReader reader){_reader=reader;}protected
 override TextReader CreateTextReader(){if(null==_reader)throw new NotSupportedException("This method can only be called once.");var r=_reader;_reader
=null;return r;}protected override bool CanCreateReader=>false;}
#endregion
public static TextReaderEnumerable FromReader(TextReader reader){if(null==reader)throw new ArgumentNullException("reader");return new _OnceTextReaderEnumerable(reader);
}public IEnumerator<char>GetEnumerator(){return new TextReaderEnumerator(this);}protected abstract bool CanCreateReader{get;}protected abstract TextReader
 CreateTextReader();IEnumerator IEnumerable.GetEnumerator()=>GetEnumerator();sealed class TextReaderEnumerator:IEnumerator<char>{TextReaderEnumerable _outer;
TextReader _reader;int _state;char _current;internal TextReaderEnumerator(TextReaderEnumerable outer){_outer=outer;_reader=null;if(_outer.CanCreateReader)
Reset();else{_state=-1;_reader=_outer.CreateTextReader();}}public char Current{get{switch(_state){case-3:throw new ObjectDisposedException(GetType().Name);
case-2:throw new InvalidOperationException("The cursor is past the end of input.");case-1:throw new InvalidOperationException("The cursor is before the start of input.");
}return _current;}}object IEnumerator.Current=>Current;public void Dispose(){ _Dispose(true); GC.SuppressFinalize(this);}~TextReaderEnumerator(){_Dispose(false);
} void _Dispose(bool disposing){if(null==_reader)return;if(disposing){_reader.Close();_reader=null;_state=-3;}}public bool MoveNext(){switch(_state){case
-3:throw new ObjectDisposedException(GetType().Name);case-2:return false;}int i=_reader.Read();if(-1==_state&&((BitConverter.IsLittleEndian&&'\uFEFF'==
i)||(!BitConverter.IsLittleEndian&&'\uFFFE'==i))) i=_reader.Read();_state=0;if(-1==i){_state=-2;return false;}_current=unchecked((char)i);return true;
}public void Reset(){ if(-1==_state)return;try{ var sr=_reader as StreamReader;if(null!=sr&&null!=sr.BaseStream&&sr.BaseStream.CanSeek&&0L==sr.BaseStream.Seek(0,
SeekOrigin.Begin)){_state=-1;return;}}catch(IOException){}if(!_outer.CanCreateReader)throw new NotSupportedException();_Dispose(true);_reader=_outer.CreateTextReader();
_state=-1;}}}}