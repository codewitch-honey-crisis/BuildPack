// This code was generated by the Gardens Point Parser Generator
// Copyright (c) Wayne Kelly, QUT 2005-2010
// (see accompanying GPPGcopyright.rtf)

// GPPG version 1.4.5
// Machine:  DESKTOP-U8QJ4Q2
// DateTime: 12/22/2019 5:58:45 PM
// UserName: gazto
// Input file <gplex.y - 12/22/2019 5:54:26 PM>

// options: no-lines gplex

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using QUT.Gppg;
using System.Collections;

namespace QUT.Gplex.Parser
{
internal enum Tokens {
    error=1,EOF=2,csKeyword=3,csIdent=4,csNumber=5,csLitstr=6,
    csVerbstr=7,csLitchr=8,csOp=9,csBar=10,csDot=11,semi=12,
    csStar=13,csLT=14,csGT=15,comma=16,csSlash=17,csLBrac=18,
    csRBrac=19,csLPar=20,csRPar=21,csLBrace=22,csRBrace=23,verbatim=24,
    pattern=25,name=26,lCond=27,rCond=28,lxLBrace=29,lxRBrace=30,
    lxBar=31,defCommentS=32,defCommentE=33,csCommentS=34,csCommentE=35,usingTag=36,
    namespaceTag=37,optionTag=38,charSetPredTag=39,inclTag=40,exclTag=41,lPcBrace=42,
    rPcBrace=43,visibilityTag=44,PCPC=45,userCharPredTag=46,scanbaseTag=47,tokentypeTag=48,
    scannertypeTag=49,lxIndent=50,lxEndIndent=51,maxParseToken=52,EOL=53,csCommentL=54,
    errTok=55,repErr=56};

// Abstract base class for GPLEX scanners
internal abstract class ScanBase : AbstractScanner<int,LexSpan> {
  private LexSpan __yylloc = new LexSpan();
  public override LexSpan yylloc { get { return __yylloc; } set { __yylloc = value; } }
  protected virtual bool yywrap() { return true; }
}

internal partial class Parser: ShiftReduceParser<int, LexSpan>
{
#pragma warning disable 649
  private static Dictionary<int, string> aliasses;
#pragma warning restore 649
  private static Rule[] rules = new Rule[113];
  private static State[] states = new State[161];
  private static string[] nonTerms = new string[] {
      "Program", "$accept", "DefinitionSection", "Rules", "RulesSection", "UserCodeSection", 
      "DefinitionSeq", "CSharp", "Definition", "NameList", "DottedName", "PcBraceSection", 
      "DefComment", "IndentedCode", "NameSeq", "CSharpN", "DefComStart", "CommentEnd", 
      "BlockComment", "CsComStart", "RuleList", "Rule", "Production", "ProductionGroup", 
      "ARule", "StartCondition", "PatActionList", "Action", "NonPairedToken", 
      "WFCSharpN", };

  static Parser() {
    states[0] = new State(new int[]{26,117,41,120,40,122,36,124,37,128,44,130,38,132,42,98,32,140,33,141,50,106,39,143,47,145,48,147,49,149,46,151,45,158,1,159},new int[]{-1,1,-3,3,-7,114,-9,157,-12,134,-13,135,-17,136,-14,142});
    states[1] = new State(new int[]{2,2});
    states[2] = new State(-1);
    states[3] = new State(new int[]{27,81,25,93,1,95,42,98,50,106,34,19,35,112,45,-53,2,-53},new int[]{-4,4,-5,6,-21,60,-22,113,-23,62,-25,63,-26,64,-24,96,-12,97,-14,105,-19,111,-20,13});
    states[4] = new State(new int[]{45,5,2,-2});
    states[5] = new State(-7);
    states[6] = new State(new int[]{34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50,1,59,2,-9},new int[]{-6,7,-8,8,-16,9,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[7] = new State(-3);
    states[8] = new State(-8);
    states[9] = new State(new int[]{34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50,2,-72,27,-72,25,-72,1,-72,42,-72,50,-72,45,-72,30,-72,51,-72},new int[]{-29,10,-30,11,-19,12,-20,13,-11,20});
    states[10] = new State(-74);
    states[11] = new State(-75);
    states[12] = new State(-89);
    states[13] = new State(new int[]{34,15,32,16,33,17,35,18},new int[]{-18,14});
    states[14] = new State(-48);
    states[15] = new State(-49);
    states[16] = new State(-50);
    states[17] = new State(-43);
    states[18] = new State(-44);
    states[19] = new State(-51);
    states[20] = new State(new int[]{11,21,34,-90,4,-90,3,-90,35,-90,54,-90,5,-90,6,-90,7,-90,8,-90,9,-90,13,-90,14,-90,15,-90,12,-90,16,-90,17,-90,10,-90,22,-90,20,-90,18,-90,2,-90,27,-90,25,-90,1,-90,42,-90,50,-90,45,-90,30,-90,51,-90,23,-90,21,-90,19,-90,43,-90});
    states[21] = new State(new int[]{4,22});
    states[22] = new State(-88);
    states[23] = new State(-86);
    states[24] = new State(new int[]{11,25,34,-93,4,-93,3,-93,35,-93,54,-93,5,-93,6,-93,7,-93,8,-93,9,-93,13,-93,14,-93,15,-93,12,-93,16,-93,17,-93,10,-93,22,-93,20,-93,18,-93,2,-93,27,-93,25,-93,1,-93,42,-93,50,-93,45,-93,30,-93,51,-93,23,-93,21,-93,19,-93,43,-93});
    states[25] = new State(new int[]{4,26});
    states[26] = new State(-87);
    states[27] = new State(-91);
    states[28] = new State(-92);
    states[29] = new State(-94);
    states[30] = new State(-95);
    states[31] = new State(-96);
    states[32] = new State(-97);
    states[33] = new State(-98);
    states[34] = new State(-99);
    states[35] = new State(-100);
    states[36] = new State(-101);
    states[37] = new State(-102);
    states[38] = new State(-103);
    states[39] = new State(-104);
    states[40] = new State(-105);
    states[41] = new State(-106);
    states[42] = new State(new int[]{23,43,1,58,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-16,44,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[43] = new State(-77);
    states[44] = new State(new int[]{23,45,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-29,10,-30,11,-19,12,-20,13,-11,20});
    states[45] = new State(-78);
    states[46] = new State(new int[]{21,47,1,57,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-16,48,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[47] = new State(-79);
    states[48] = new State(new int[]{21,49,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-29,10,-30,11,-19,12,-20,13,-11,20});
    states[49] = new State(-80);
    states[50] = new State(new int[]{19,51,1,54,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-16,52,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[51] = new State(-81);
    states[52] = new State(new int[]{19,53,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-29,10,-30,11,-19,12,-20,13,-11,20});
    states[53] = new State(-82);
    states[54] = new State(-84);
    states[55] = new State(-73);
    states[56] = new State(-76);
    states[57] = new State(-83);
    states[58] = new State(-85);
    states[59] = new State(-10);
    states[60] = new State(new int[]{27,81,25,93,1,95,42,98,50,106,34,19,35,112,45,-52,2,-52},new int[]{-22,61,-23,62,-25,63,-26,64,-24,96,-12,97,-14,105,-19,111,-20,13});
    states[61] = new State(-54);
    states[62] = new State(-56);
    states[63] = new State(-62);
    states[64] = new State(new int[]{25,65,29,76});
    states[65] = new State(new int[]{29,67,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50,31,74,1,75},new int[]{-28,66,-8,73,-16,9,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[66] = new State(-67);
    states[67] = new State(new int[]{30,70,1,71,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-8,68,-16,9,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[68] = new State(new int[]{30,69});
    states[69] = new State(-107);
    states[70] = new State(-110);
    states[71] = new State(new int[]{30,72});
    states[72] = new State(-111);
    states[73] = new State(-108);
    states[74] = new State(-109);
    states[75] = new State(-112);
    states[76] = new State(-64,new int[]{-27,77});
    states[77] = new State(new int[]{30,78,27,81,25,93,1,95},new int[]{-25,79,-24,80,-26,64});
    states[78] = new State(-63);
    states[79] = new State(-65);
    states[80] = new State(-66);
    states[81] = new State(new int[]{13,84,26,90,5,91,1,92},new int[]{-10,82,-15,86});
    states[82] = new State(new int[]{28,83});
    states[83] = new State(-70);
    states[84] = new State(new int[]{28,85});
    states[85] = new State(-71);
    states[86] = new State(new int[]{16,87,28,-31,45,-31,26,-31,41,-31,40,-31,36,-31,37,-31,44,-31,38,-31,42,-31,32,-31,33,-31,50,-31,39,-31,47,-31,48,-31,49,-31,46,-31});
    states[87] = new State(new int[]{26,88,1,89});
    states[88] = new State(-33);
    states[89] = new State(-36);
    states[90] = new State(-34);
    states[91] = new State(-35);
    states[92] = new State(-32);
    states[93] = new State(new int[]{29,67,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50,31,74,1,75},new int[]{-28,94,-8,73,-16,9,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[94] = new State(-68);
    states[95] = new State(-69);
    states[96] = new State(-57);
    states[97] = new State(-58);
    states[98] = new State(new int[]{43,99,1,102,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-16,100,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[99] = new State(-37);
    states[100] = new State(new int[]{43,101,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-29,10,-30,11,-19,12,-20,13,-11,20});
    states[101] = new State(-38);
    states[102] = new State(new int[]{43,103,45,104});
    states[103] = new State(-39);
    states[104] = new State(-40);
    states[105] = new State(-59);
    states[106] = new State(new int[]{1,109,34,19,4,23,3,24,35,27,54,28,5,29,6,30,7,31,8,32,9,33,11,34,13,35,14,36,15,37,12,38,16,39,17,40,10,41,22,42,20,46,18,50},new int[]{-8,107,-16,9,-29,55,-19,12,-20,13,-11,20,-30,56});
    states[107] = new State(new int[]{51,108});
    states[108] = new State(-29);
    states[109] = new State(new int[]{51,110});
    states[110] = new State(-30);
    states[111] = new State(-60);
    states[112] = new State(-61);
    states[113] = new State(-55);
    states[114] = new State(new int[]{45,115,26,117,41,120,40,122,36,124,37,128,44,130,38,132,42,98,32,140,33,141,50,106,39,143,47,145,48,147,49,149,46,151},new int[]{-9,116,-12,134,-13,135,-17,136,-14,142});
    states[115] = new State(-4);
    states[116] = new State(-11);
    states[117] = new State(new int[]{24,118,25,119});
    states[118] = new State(-13);
    states[119] = new State(-14);
    states[120] = new State(new int[]{26,90,5,91,1,92},new int[]{-10,121,-15,86});
    states[121] = new State(-15);
    states[122] = new State(new int[]{26,90,5,91,1,92},new int[]{-10,123,-15,86});
    states[123] = new State(-16);
    states[124] = new State(new int[]{4,23,3,127},new int[]{-11,125});
    states[125] = new State(new int[]{12,126,11,21});
    states[126] = new State(-17);
    states[127] = new State(new int[]{11,25});
    states[128] = new State(new int[]{4,23,3,127},new int[]{-11,129});
    states[129] = new State(new int[]{11,21,45,-18,26,-18,41,-18,40,-18,36,-18,37,-18,44,-18,38,-18,42,-18,32,-18,33,-18,50,-18,39,-18,47,-18,48,-18,49,-18,46,-18});
    states[130] = new State(new int[]{3,131});
    states[131] = new State(-19);
    states[132] = new State(new int[]{24,133});
    states[133] = new State(-20);
    states[134] = new State(-21);
    states[135] = new State(-22);
    states[136] = new State(new int[]{32,138,34,139,33,17,35,18},new int[]{-18,137});
    states[137] = new State(-41);
    states[138] = new State(-45);
    states[139] = new State(-46);
    states[140] = new State(-47);
    states[141] = new State(-42);
    states[142] = new State(-23);
    states[143] = new State(new int[]{26,90,5,91,1,92},new int[]{-10,144,-15,86});
    states[144] = new State(-24);
    states[145] = new State(new int[]{4,146});
    states[146] = new State(-25);
    states[147] = new State(new int[]{4,148});
    states[148] = new State(-26);
    states[149] = new State(new int[]{4,150});
    states[150] = new State(-27);
    states[151] = new State(new int[]{4,152});
    states[152] = new State(new int[]{18,153});
    states[153] = new State(new int[]{4,23,3,127},new int[]{-11,154});
    states[154] = new State(new int[]{19,155,11,21});
    states[155] = new State(new int[]{4,23,3,127},new int[]{-11,156});
    states[156] = new State(new int[]{11,21,45,-28,26,-28,41,-28,40,-28,36,-28,37,-28,44,-28,38,-28,42,-28,32,-28,33,-28,50,-28,39,-28,47,-28,48,-28,49,-28,46,-28});
    states[157] = new State(-12);
    states[158] = new State(-5);
    states[159] = new State(new int[]{45,160});
    states[160] = new State(-6);

    for (int sNo = 0; sNo < states.Length; sNo++) states[sNo].number = sNo;

    rules[1] = new Rule(-2, new int[]{-1,2});
    rules[2] = new Rule(-1, new int[]{-3,-4});
    rules[3] = new Rule(-1, new int[]{-3,-5,-6});
    rules[4] = new Rule(-3, new int[]{-7,45});
    rules[5] = new Rule(-3, new int[]{45});
    rules[6] = new Rule(-3, new int[]{1,45});
    rules[7] = new Rule(-5, new int[]{-4,45});
    rules[8] = new Rule(-6, new int[]{-8});
    rules[9] = new Rule(-6, new int[]{});
    rules[10] = new Rule(-6, new int[]{1});
    rules[11] = new Rule(-7, new int[]{-7,-9});
    rules[12] = new Rule(-7, new int[]{-9});
    rules[13] = new Rule(-9, new int[]{26,24});
    rules[14] = new Rule(-9, new int[]{26,25});
    rules[15] = new Rule(-9, new int[]{41,-10});
    rules[16] = new Rule(-9, new int[]{40,-10});
    rules[17] = new Rule(-9, new int[]{36,-11,12});
    rules[18] = new Rule(-9, new int[]{37,-11});
    rules[19] = new Rule(-9, new int[]{44,3});
    rules[20] = new Rule(-9, new int[]{38,24});
    rules[21] = new Rule(-9, new int[]{-12});
    rules[22] = new Rule(-9, new int[]{-13});
    rules[23] = new Rule(-9, new int[]{-14});
    rules[24] = new Rule(-9, new int[]{39,-10});
    rules[25] = new Rule(-9, new int[]{47,4});
    rules[26] = new Rule(-9, new int[]{48,4});
    rules[27] = new Rule(-9, new int[]{49,4});
    rules[28] = new Rule(-9, new int[]{46,4,18,-11,19,-11});
    rules[29] = new Rule(-14, new int[]{50,-8,51});
    rules[30] = new Rule(-14, new int[]{50,1,51});
    rules[31] = new Rule(-10, new int[]{-15});
    rules[32] = new Rule(-10, new int[]{1});
    rules[33] = new Rule(-15, new int[]{-15,16,26});
    rules[34] = new Rule(-15, new int[]{26});
    rules[35] = new Rule(-15, new int[]{5});
    rules[36] = new Rule(-15, new int[]{-15,16,1});
    rules[37] = new Rule(-12, new int[]{42,43});
    rules[38] = new Rule(-12, new int[]{42,-16,43});
    rules[39] = new Rule(-12, new int[]{42,1,43});
    rules[40] = new Rule(-12, new int[]{42,1,45});
    rules[41] = new Rule(-13, new int[]{-17,-18});
    rules[42] = new Rule(-13, new int[]{33});
    rules[43] = new Rule(-18, new int[]{33});
    rules[44] = new Rule(-18, new int[]{35});
    rules[45] = new Rule(-17, new int[]{-17,32});
    rules[46] = new Rule(-17, new int[]{-17,34});
    rules[47] = new Rule(-17, new int[]{32});
    rules[48] = new Rule(-19, new int[]{-20,-18});
    rules[49] = new Rule(-20, new int[]{-20,34});
    rules[50] = new Rule(-20, new int[]{-20,32});
    rules[51] = new Rule(-20, new int[]{34});
    rules[52] = new Rule(-4, new int[]{-21});
    rules[53] = new Rule(-4, new int[]{});
    rules[54] = new Rule(-21, new int[]{-21,-22});
    rules[55] = new Rule(-21, new int[]{-22});
    rules[56] = new Rule(-22, new int[]{-23});
    rules[57] = new Rule(-22, new int[]{-24});
    rules[58] = new Rule(-22, new int[]{-12});
    rules[59] = new Rule(-22, new int[]{-14});
    rules[60] = new Rule(-22, new int[]{-19});
    rules[61] = new Rule(-22, new int[]{35});
    rules[62] = new Rule(-23, new int[]{-25});
    rules[63] = new Rule(-24, new int[]{-26,29,-27,30});
    rules[64] = new Rule(-27, new int[]{});
    rules[65] = new Rule(-27, new int[]{-27,-25});
    rules[66] = new Rule(-27, new int[]{-27,-24});
    rules[67] = new Rule(-25, new int[]{-26,25,-28});
    rules[68] = new Rule(-25, new int[]{25,-28});
    rules[69] = new Rule(-25, new int[]{1});
    rules[70] = new Rule(-26, new int[]{27,-10,28});
    rules[71] = new Rule(-26, new int[]{27,13,28});
    rules[72] = new Rule(-8, new int[]{-16});
    rules[73] = new Rule(-16, new int[]{-29});
    rules[74] = new Rule(-16, new int[]{-16,-29});
    rules[75] = new Rule(-16, new int[]{-16,-30});
    rules[76] = new Rule(-16, new int[]{-30});
    rules[77] = new Rule(-30, new int[]{22,23});
    rules[78] = new Rule(-30, new int[]{22,-16,23});
    rules[79] = new Rule(-30, new int[]{20,21});
    rules[80] = new Rule(-30, new int[]{20,-16,21});
    rules[81] = new Rule(-30, new int[]{18,19});
    rules[82] = new Rule(-30, new int[]{18,-16,19});
    rules[83] = new Rule(-30, new int[]{20,1});
    rules[84] = new Rule(-30, new int[]{18,1});
    rules[85] = new Rule(-30, new int[]{22,1});
    rules[86] = new Rule(-11, new int[]{4});
    rules[87] = new Rule(-11, new int[]{3,11,4});
    rules[88] = new Rule(-11, new int[]{-11,11,4});
    rules[89] = new Rule(-29, new int[]{-19});
    rules[90] = new Rule(-29, new int[]{-11});
    rules[91] = new Rule(-29, new int[]{35});
    rules[92] = new Rule(-29, new int[]{54});
    rules[93] = new Rule(-29, new int[]{3});
    rules[94] = new Rule(-29, new int[]{5});
    rules[95] = new Rule(-29, new int[]{6});
    rules[96] = new Rule(-29, new int[]{7});
    rules[97] = new Rule(-29, new int[]{8});
    rules[98] = new Rule(-29, new int[]{9});
    rules[99] = new Rule(-29, new int[]{11});
    rules[100] = new Rule(-29, new int[]{13});
    rules[101] = new Rule(-29, new int[]{14});
    rules[102] = new Rule(-29, new int[]{15});
    rules[103] = new Rule(-29, new int[]{12});
    rules[104] = new Rule(-29, new int[]{16});
    rules[105] = new Rule(-29, new int[]{17});
    rules[106] = new Rule(-29, new int[]{10});
    rules[107] = new Rule(-28, new int[]{29,-8,30});
    rules[108] = new Rule(-28, new int[]{-8});
    rules[109] = new Rule(-28, new int[]{31});
    rules[110] = new Rule(-28, new int[]{29,30});
    rules[111] = new Rule(-28, new int[]{29,1,30});
    rules[112] = new Rule(-28, new int[]{1});

    aliasses = new Dictionary<int, string>();
    aliasses.Add(10, "\"|\"");
    aliasses.Add(11, "\".\"");
    aliasses.Add(12, "\";\"");
    aliasses.Add(13, "\"*\"");
    aliasses.Add(14, "\"<\"");
    aliasses.Add(15, "\">\"");
    aliasses.Add(16, "\",\"");
    aliasses.Add(17, "\"/\"");
    aliasses.Add(18, "\"[\"");
    aliasses.Add(19, "\"]\"");
    aliasses.Add(20, "\"(\"");
    aliasses.Add(21, "\")\"");
    aliasses.Add(22, "\"{\"");
    aliasses.Add(23, "\"}\"");
    aliasses.Add(27, "\"<\"");
    aliasses.Add(28, "\">\"");
    aliasses.Add(29, "\"{\"");
    aliasses.Add(30, "\"}\"");
    aliasses.Add(31, "\"|\"");
    aliasses.Add(32, "\"/*\"");
    aliasses.Add(33, "\"/*...*/\"");
    aliasses.Add(34, "\"/*\"");
    aliasses.Add(35, "\"/*...*/\"");
    aliasses.Add(36, "\"%using\"");
    aliasses.Add(37, "\"%namespace\"");
    aliasses.Add(38, "\"%option\"");
    aliasses.Add(39, "\"%charClassPredicate\"");
    aliasses.Add(40, "\"%s\"");
    aliasses.Add(41, "\"%x\"");
    aliasses.Add(42, "\"%{\"");
    aliasses.Add(43, "\"%}\"");
    aliasses.Add(44, "\"%visibility\"");
    aliasses.Add(45, "\"%%\"");
    aliasses.Add(46, "\"userCharPredicate\"");
    aliasses.Add(47, "\"%scanbasetype\"");
    aliasses.Add(48, "\"%tokentype\"");
    aliasses.Add(49, "\"scannertype\"");
  }

  protected override void Initialize() {
    this.InitSpecialTokens((int)Tokens.error, (int)Tokens.EOF);
    this.InitStates(states);
    this.InitRules(rules);
    this.InitNonTerminals(nonTerms);
  }

  protected override void DoAction(int action)
  {
    switch (action)
    {
      case 4: // DefinitionSection -> DefinitionSeq, "%%"
{
                       isBar = false; 
                       typedeclOK = false;
                       if (aast.nameString == null) 
                           handler.ListError(LocationStack[LocationStack.Depth-1], 73); 
                     }
        break;
      case 5: // DefinitionSection -> "%%"
{ isBar = false; }
        break;
      case 6: // DefinitionSection -> error, "%%"
{ handler.ListError(LocationStack[LocationStack.Depth-2], 62, "%%"); }
        break;
      case 7: // RulesSection -> Rules, "%%"
{ typedeclOK = true; }
        break;
      case 8: // UserCodeSection -> CSharp
{ aast.UserCode = LocationStack[LocationStack.Depth-1]; }
        break;
      case 9: // UserCodeSection -> /* empty */
{  /* empty */  }
        break;
      case 10: // UserCodeSection -> error
{ handler.ListError(LocationStack[LocationStack.Depth-1], 62, "EOF"); }
        break;
      case 13: // Definition -> name, verbatim
{ AddLexCategory(LocationStack[LocationStack.Depth-2], LocationStack[LocationStack.Depth-1]); }
        break;
      case 14: // Definition -> name, pattern
{ AddLexCategory(LocationStack[LocationStack.Depth-2], LocationStack[LocationStack.Depth-1]); }
        break;
      case 15: // Definition -> "%x", NameList
{ AddNames(true); }
        break;
      case 16: // Definition -> "%s", NameList
{ AddNames(false); }
        break;
      case 17: // Definition -> "%using", DottedName, ";"
{ aast.usingStrs.Add(LocationStack[LocationStack.Depth-2].Merge(LocationStack[LocationStack.Depth-1])); }
        break;
      case 18: // Definition -> "%namespace", DottedName
{ aast.nameString = LocationStack[LocationStack.Depth-1]; }
        break;
      case 19: // Definition -> "%visibility", csKeyword
{ aast.AddVisibility(LocationStack[LocationStack.Depth-1]); }
        break;
      case 20: // Definition -> "%option", verbatim
{ ParseOption(LocationStack[LocationStack.Depth-1]); }
        break;
      case 21: // Definition -> PcBraceSection
{ aast.AddCodeSpan(Dest,LocationStack[LocationStack.Depth-1]); }
        break;
      case 22: // Definition -> DefComment
{ aast.AddCodeSpan(Dest,LocationStack[LocationStack.Depth-1]); }
        break;
      case 23: // Definition -> IndentedCode
{ aast.AddCodeSpan(Dest,LocationStack[LocationStack.Depth-1]); }
        break;
      case 24: // Definition -> "%charClassPredicate", NameList
{ AddCharSetPredicates(); }
        break;
      case 25: // Definition -> "%scanbasetype", csIdent
{ aast.SetScanBaseName(LocationStack[LocationStack.Depth-1].ToString()); }
        break;
      case 26: // Definition -> "%tokentype", csIdent
{ aast.SetTokenTypeName(LocationStack[LocationStack.Depth-1].ToString()); }
        break;
      case 27: // Definition -> "scannertype", csIdent
{ aast.SetScannerTypeName(LocationStack[LocationStack.Depth-1].ToString()); }
        break;
      case 28: // Definition -> "userCharPredicate", csIdent, "[", DottedName, "]", DottedName
{
                                 aast.AddUserPredicate(LocationStack[LocationStack.Depth-5].ToString(), LocationStack[LocationStack.Depth-3], LocationStack[LocationStack.Depth-1]);
                               }
        break;
      case 29: // IndentedCode -> lxIndent, CSharp, lxEndIndent
{ CurrentLocationSpan = LocationStack[LocationStack.Depth-2]; }
        break;
      case 30: // IndentedCode -> lxIndent, error, lxEndIndent
{ handler.ListError(LocationStack[LocationStack.Depth-2], 64); }
        break;
      case 32: // NameList -> error
{ handler.ListError(LocationStack[LocationStack.Depth-1], 67); }
        break;
      case 33: // NameSeq -> NameSeq, ",", name
{ AddName(LocationStack[LocationStack.Depth-1]); }
        break;
      case 34: // NameSeq -> name
{ AddName(LocationStack[LocationStack.Depth-1]); }
        break;
      case 35: // NameSeq -> csNumber
{ AddName(LocationStack[LocationStack.Depth-1]); }
        break;
      case 36: // NameSeq -> NameSeq, ",", error
{ handler.ListError(LocationStack[LocationStack.Depth-2], 67); }
        break;
      case 37: // PcBraceSection -> "%{", "%}"
{ CurrentLocationSpan = BlankSpan; /* skip blank lines */ }
        break;
      case 38: // PcBraceSection -> "%{", CSharpN, "%}"
{ 
                                 CurrentLocationSpan = LocationStack[LocationStack.Depth-2]; 
                               }
        break;
      case 39: // PcBraceSection -> "%{", error, "%}"
{ handler.ListError(LocationStack[LocationStack.Depth-2], 62, "%}"); }
        break;
      case 40: // PcBraceSection -> "%{", error, "%%"
{ handler.ListError(LocationStack[LocationStack.Depth-2], 62, "%%"); }
        break;
      case 52: // Rules -> RuleList
{ 
                                rb.FinalizeCode(aast); 
                                aast.FixupBarActions(); 
                              }
        break;
      case 57: // Rule -> ProductionGroup
{ scope.ClearScope(); /* for error recovery */ }
        break;
      case 58: // Rule -> PcBraceSection
{ rb.AddSpan(LocationStack[LocationStack.Depth-1]); }
        break;
      case 59: // Rule -> IndentedCode
{ rb.AddSpan(LocationStack[LocationStack.Depth-1]); }
        break;
      case 60: // Rule -> BlockComment
{ /* ignore */ }
        break;
      case 61: // Rule -> "/*...*/"
{ /* ignore */ }
        break;
      case 62: // Production -> ARule
{
			                    int thisLine = LocationStack[LocationStack.Depth-1].startLine;
			                    rb.LLine = thisLine;
			                    if (rb.FLine == 0) rb.FLine = thisLine;
		                      }
        break;
      case 63: // ProductionGroup -> StartCondition, "{", PatActionList, "}"
{
                                scope.ExitScope();
                              }
        break;
      case 64: // PatActionList -> /* empty */
{ 
                                int thisLine = CurrentLocationSpan.startLine;
			                    rb.LLine = thisLine;
			                    if (rb.FLine == 0) 
			                        rb.FLine = thisLine;
			                  }
        break;
      case 67: // ARule -> StartCondition, pattern, Action
{
			                    RuleDesc rule = new RuleDesc(LocationStack[LocationStack.Depth-2], LocationStack[LocationStack.Depth-1], scope.Current, isBar);
			                    aast.ruleList.Add(rule);
			                    rule.ParseRE(aast);
			                    isBar = false; // Reset the flag ...
			                    scope.ExitScope();
		                      }
        break;
      case 68: // ARule -> pattern, Action
{
			                    RuleDesc rule = new RuleDesc(LocationStack[LocationStack.Depth-2], LocationStack[LocationStack.Depth-1], scope.Current, isBar); 
			                    aast.ruleList.Add(rule);
			                    rule.ParseRE(aast); 
			                    isBar = false; // Reset the flag ...
		                      }
        break;
      case 69: // ARule -> error
{ handler.ListError(LocationStack[LocationStack.Depth-1], 68); scope.ClearScope(); }
        break;
      case 70: // StartCondition -> "<", NameList, ">"
{ 
                                List<StartState> list =  new List<StartState>();
                                AddNameListToStateList(list);
                                scope.EnterScope(list); 
                              }
        break;
      case 71: // StartCondition -> "<", "*", ">"
{
                                List<StartState> list =  new List<StartState>(); 
                                list.Add(StartState.allState);
                                scope.EnterScope(list); 
                              }
        break;
      case 83: // WFCSharpN -> "(", error
{ handler.ListError(LocationStack[LocationStack.Depth-1], 61, "')'"); }
        break;
      case 84: // WFCSharpN -> "[", error
{ handler.ListError(LocationStack[LocationStack.Depth-1], 61, "']'"); }
        break;
      case 85: // WFCSharpN -> "{", error
{ handler.ListError(LocationStack[LocationStack.Depth-1], 61, "'}'"); }
        break;
      case 86: // DottedName -> csIdent
{ /* skip1 */ }
        break;
      case 87: // DottedName -> csKeyword, ".", csIdent
{ /* skip2 */ }
        break;
      case 88: // DottedName -> DottedName, ".", csIdent
{ /* skip3 */ }
        break;
      case 93: // NonPairedToken -> csKeyword
{ 
                                 string text = aast.scanner.yytext;
                                 if (text.Equals("using")) {
                                     handler.ListError(LocationStack[LocationStack.Depth-1], 56);
                                 } else if (text.Equals("namespace")) {
                                     handler.ListError(LocationStack[LocationStack.Depth-1], 57);
                                 } else {
                                     if ((text.Equals("class") || text.Equals("struct") ||
                                          text.Equals("enum")) && !typedeclOK) handler.ListError(LocationStack[LocationStack.Depth-1],58);
                                 }
                               }
        break;
      case 107: // Action -> "{", CSharp, "}"
{ CurrentLocationSpan = LocationStack[LocationStack.Depth-2]; }
        break;
      case 109: // Action -> "|"
{ isBar = true; }
        break;
      case 111: // Action -> "{", error, "}"
{ handler.ListError(CurrentLocationSpan, 65); }
        break;
      case 112: // Action -> error
{ handler.ListError(LocationStack[LocationStack.Depth-1], 63); }
        break;
    }
  }

  protected override string TerminalToString(int terminal)
  {
    if (aliasses != null && aliasses.ContainsKey(terminal))
        return aliasses[terminal];
    else if (((Tokens)terminal).ToString() != terminal.ToString(CultureInfo.InvariantCulture))
        return ((Tokens)terminal).ToString();
    else
        return CharToString((char)terminal);
  }

}
}
