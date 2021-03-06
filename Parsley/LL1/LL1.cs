﻿using System;
using System.Collections.Generic;
using System.Text;

namespace Parsley
{
	public static class LL1
	{
		public static CfgLL1ParseTable ToLL1ParseTable(this CfgDocument cfg, IProgress<CfgLL1Progress> progress=null)
		{
			CfgLL1ParseTable result = null;
			var msgs = TryToLL1ParseTable(cfg, progress, out result);
			CfgException.ThrowIfErrors(msgs);
			return result;
		}
		public static IList<CfgMessage> TryToLL1ParseTable(this CfgDocument cfg, out CfgLL1ParseTable parseTable)
			=> TryToLL1ParseTable(cfg, null, out parseTable);
		public static IList<CfgMessage> TryToLL1ParseTable(this CfgDocument cfg,IProgress<CfgLL1Progress> progress,out CfgLL1ParseTable parseTable)
		{
			// Here we populate the outer dictionary with one non-terminal for each key
			// we populate each inner dictionary with the result terminals and associated 
			// rules of the predict tables except in the case where the predict table 
			// contains null. In that case, we use the follows to get the terminals and 
			// the rule associated with the null predict in order to compute the inner 
			// dictionary. The conflict resolution tables are always empty for LL(1)
			if (null != progress)
				progress.Report(new CfgLL1Progress(CfgLL1Status.ComputingPredicts, 0));
			var predict = cfg.FillPredict();
			if (null != progress)
				progress.Report(new CfgLL1Progress(CfgLL1Status.ComputingFollows, 0));
			var follows = cfg.FillFollows();
			var result = new List<CfgMessage>();
			parseTable = new CfgLL1ParseTable();
			var j = 0;
			foreach (var nt in cfg.EnumNonTerminals())
			{
				var d = new Dictionary<string, CfgLL1ParseTableEntry>();
				ICollection<(CfgRule Rule, string Symbol)> col;
				if (predict.TryGetValue(nt, out col))
				{
					foreach (var f in col)
					{
						if (null != f.Symbol)
						{
							CfgLL1ParseTableEntry re = new CfgLL1ParseTableEntry(new CfgRule[] { f.Rule });
							CfgLL1ParseTableEntry or;
							if (d.TryGetValue(f.Symbol, out or))
							{
								or.Rules.Add(f.Rule);
								result.Add(new CfgMessage(ErrorLevel.Error, 1,
											string.Format(
												"first first conflict between {0} and {1} on {2}",
												or.Rules[0],
												f.Rule,
												f.Symbol), f.Rule.Line, f.Rule.Column, f.Rule.Position, cfg.Filename));
							}
							else
								d.Add(f.Symbol, re);
							if (null != progress)
								progress.Report(new CfgLL1Progress(CfgLL1Status.CreatingParseTable, j));
							++j;
						}
						else
						{
							var ff = follows[nt];
							foreach (var fe in ff)
							{
								CfgLL1ParseTableEntry or;
								if (d.TryGetValue(fe, out or))
								{
									or.Rules.Add(f.Rule);
									// we can override conflict handling with the followsConflict
									// attribute. If specified (first/last/error - error is default) it will choose
									// the first or last rule respectively.
									var fc = cfg.GetAttribute(nt, "followsConflict", "error") as string;
									if ("error" == fc)
										result.Add(new CfgMessage(ErrorLevel.Error, -1,
											string.Format(
												"first follows conflict between {0} and {1} on {2}",
												or.Rules[0],
												f.Rule,
												fe), f.Rule.Line, f.Rule.Column, f.Rule.Position, cfg.Filename));
									else if ("last" == fc)
									{
										d[fe] = new CfgLL1ParseTableEntry(new CfgRule[] { f.Rule });
									}
								}
								else
								{
									d.Add(fe, new CfgLL1ParseTableEntry(new CfgRule[] { f.Rule }));
								}
								if (null != progress)
									progress.Report(new CfgLL1Progress(CfgLL1Status.CreatingParseTable, j));
								++j;
							}
						}
					}
				}
				if(0<d.Count)
					parseTable.Add(nt, d);
			}
			return result;
		}
		public static void PrepareLL1(this CfgDocument cfg,IProgress<CfgLL1Progress> progress=null)
		{
			var msgs = TryPrepareLL1(cfg, progress);
			CfgException.ThrowIfErrors(msgs);
		}
		public static IList<CfgMessage> TryPrepareLL1(this CfgDocument cfg,IProgress<CfgLL1Progress> progress=null)
		{
			const int repeat = 2;
			var result = new List<CfgMessage>();
			CfgDocument old = cfg;
			for (int j = 0; j < repeat; ++j)
			{
				if (null != progress)
					progress.Report(new CfgLL1Progress(CfgLL1Status.Factoring, j));
				// if 20 times doesn't sort out this grammar it's not LL(1)
				// the math is such that we don't know unless we try
				// and the tries can go on forever.
				for (int i = 0; i < 20; ++i)
				{
					if (cfg.IsDirectlyLeftRecursive)
						result.AddRange(EliminateLeftRecursion(cfg));
					var cc = FillLL1Conflicts(cfg);
					if (_HasFirstFollowsConflicts(cc))
						result.AddRange(EliminateFirstFollowsConflicts(cfg));
					cc = FillLL1Conflicts(cfg);
					if (_HasFirstFirstConflicts(cc))
						result.AddRange(EliminateFirstFirstConflicts(cfg));
					//result.AddRange(EliminateUnderivableRules());
					cc = cfg.FillLL1Conflicts();
					if (0 == cc.Count && !cfg.IsDirectlyLeftRecursive)
						break;
					if (old.Equals(cfg))
						break;
					old = cfg.Clone();
				}
			}
			if (cfg.IsDirectlyLeftRecursive)
			{
				// search for a directly recursive rule so we can report location info
				CfgRule r = null;
				for(int ic=cfg.Rules.Count,i=0;i<ic;++i)
				{
					var rr = cfg.Rules[i];
					if(rr.IsDirectlyLeftRecursive)
					{
						r = rr;
						break;
					}
				}
				result.Add(new CfgMessage(ErrorLevel.Error, -1, "Grammar is unresolvably and directly left recursive and cannot be parsed with an LL parser.", r.Line, r.Column, r.Position, cfg.Filename));
			}
			var fc = cfg.FillLL1Conflicts();
			foreach (var f in fc)
			{
				// if we have a custom constraint on this we don't necessarily error.
				if(null!=cfg.GetAttribute(f.Rule1.Left,"constrained") || null!=cfg.GetAttribute(f.Rule2.Left,"constrained"))
					result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Format("First-{0} conflict between {1} and {2} on symbol {3} was bypassed do to a constraint in the grammar", f.Kind == CfgLL1ConflictKind.FirstFirst ? "first" : "follows", f.Rule1, f.Rule2, f.Symbol), f.Rule2.Line, f.Rule2.Column, f.Rule2.Position, cfg.Filename));
				else
					result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Format("Grammar has unresolvable first-{0} conflict between {1} and {2} on symbol {3}", f.Kind == CfgLL1ConflictKind.FirstFirst ? "first" : "follows", f.Rule1, f.Rule2, f.Symbol), f.Rule2.Line, f.Rule2.Column, f.Rule2.Position, cfg.Filename));
			}
			cfg.TryValidateLL1(result);
			return result;
		}
		public static IList<CfgMessage> EliminateLeftRecursion(this CfgDocument cfg)
		{

			var result = new List<CfgMessage>();
			var done = false;
			while (!done)
			{
				done = true;
				var ic = cfg.Rules.Count;
				for (var i = 0; i < ic; ++i)
				{
					var rule = cfg.Rules[i];
					if (rule.IsDirectlyLeftRecursive)
					{
						cfg.Rules.Remove(rule);
						result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Format("Removed rule {0} because it is directly left recursive.", rule),rule.Line,rule.Column,rule.Position, cfg.Filename));

						var newId = _GetRightAssocId(cfg,rule.Left);

						var col = new List<string>();
						var c = rule.Right.Count;
						for (var j = 1; j < c; ++j)
							col.Add(rule.Right[j]);
						col.Add(newId);
						//var o = cfg.GetAttribute(rule.Left, "collapsed", false);
						//if (o is bool && (bool)o)
							_SetAttribute(cfg, newId, "collapsed", true);
							_SetAttribute(cfg, newId, "nowarn", true);
							_SetAttribute(cfg, newId, "factored", true);
						//else
						//	_SetAttribute(cfg, newId, "substitute", rule.Left);

						var newRule = new CfgRule(newId);
						for(int jc=col.Count,j=0;j<jc;++j)
							newRule.Right.Add(col[j]);
						if (!cfg.Rules.Contains(newRule))
							cfg.Rules.Add(newRule);
						result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Format("Added rule {1} to replace rule {0}", rule, newRule),rule.Line,rule.Column,rule.Position, cfg.Filename));

						var rr = new CfgRule(newId);
						if (!cfg.Rules.Contains(rr))
							cfg.Rules.Add(rr);
						result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Format("Added rule {1} to replace rule {0}", rule, rr),rule.Line,rule.Column,rule.Position, cfg.Filename));

						foreach (var r in cfg.Rules)
						{
							if (Equals(r.Left, rule.Left))
							{
								if (!r.IsDirectlyLeftRecursive)
								{
									r.Right.Add(newId);
								}
							}
						}


					}
					/*else if (_IsIndirectlyLeftRecursive(rule))
					{
						result.Add(new CfgMessage(CfgErrorLevel.Message, -1, string.Concat("Rule ", rule, " modified because it was indirectly left recursive.")));
						Rules.Remove(rule);
						var jc = rule.Right.Count;
						var append = new List<string>(jc - 1);
						for (var j = 1; j < jc; ++j)
							append.Add(rule.Right[j]);
						// do indirect left recursion elimination.
						// first make it directly left recursive.
						var dstRules = FillNonTerminalRules(rule.Right[0]);
						foreach (var drule in dstRules)
						{
							var newRule = new CfgRule(rule.Left);
							// now add the stuff from the dst rule;
							newRule.Right.AddRange(drule.Right);
							newRule.Right.AddRange(append);
							if (!Rules.Contains(newRule))
								Rules.Add(newRule);
							done = false;
							var nt = GetTransformId(rule.Left);
							var allRules = FillNonTerminalRules(rule.Left);
							foreach (var ar in allRules)
							{
								// Section 2.3, 3.2
								// TODO: This needs lots more testing

								if (ar.IsNil || !Equals(ar.Right[0], rule.Left))
								{
									var nar = new CfgRule(rule.Left);
									nar.Right.AddRange(ar.Right);
									nar.Right.Add(nt);
									if (!Rules.Contains(nar))
										Rules.Add(nar);
									Rules.Remove(ar);
								}
								else
								{
									ar.Right.RemoveAt(0);
									ar.Left = nt;
									ar.Right.Add(nt);
									var nr2 = new CfgRule(nt);
									if (!Rules.Contains(nr2))
										Rules.Add(nr2);
								}
								//}

							}

							result.AddRange(EliminateUnderivableRules());

							break;

						}
						if (!done)
							break;
					}*/
				}
			}
			return result;
		}
		public static IList<CfgLL1Conflict> FillLL1Conflicts(this CfgDocument cfg,IList<CfgLL1Conflict> result = null)
		{
			if (null == result)
				result = new List<CfgLL1Conflict>();
			// build a temporary parse table to check for conflicts
			var predict = cfg.FillPredict();
			var follows = cfg.FillFollows();
			foreach (var nt in cfg.EnumNonTerminals())
			{
				var d = new Dictionary<string, CfgRule>();
				ICollection<(CfgRule Rule, string Symbol)> col;
				if (predict.TryGetValue(nt, out col))
				{


					foreach (var f in col)
					{
						if (null != f.Symbol)
						{
							CfgRule r;
							if (d.TryGetValue(f.Symbol, out r))
							{
								if (r != f.Rule)
								{
									var cf = new CfgLL1Conflict(CfgLL1ConflictKind.FirstFirst, r, f.Rule, f.Symbol);
									if (!result.Contains(cf))
										result.Add(cf);
								}
							}
							else
								d.Add(f.Symbol, f.Rule);
						}
						else
						{
							foreach (var ff in follows[nt])
							{
								CfgRule r;
								if (d.TryGetValue(ff, out r))
								{
									if (r != f.Rule)
									{
										var cf = new CfgLL1Conflict(CfgLL1ConflictKind.FirstFollows, r, f.Rule, ff);
										if (!result.Contains(cf))
											result.Add(cf);
									}
								}
								else
									d.Add(ff, f.Rule);
							}
						}
					}
				}
			}
			return result;
		}
		public static IList<CfgMessage> TryValidateLL1(this CfgDocument cfg,IList<CfgMessage> result = null)
		{
			if (null == result)
				result = new List<CfgMessage>();
			_TryValidateAttributesLL1(cfg,result);
			_TryValidateRulesLL1(cfg,result);
			return result;
		}
		
		static IList<CfgMessage> _TryValidateRulesLL1(CfgDocument cfg,IList<CfgMessage> result)
		{
			if (null == result)
				result = new List<CfgMessage>();
			var ic = cfg.Rules.Count;
			if (0 == ic)
				result.Add(new CfgMessage(ErrorLevel.Error, -1, "Grammar has no rules",0,0,0, cfg.Filename));
			var deps = new List<string>();
			var nts = cfg.FillNonTerminals();
			for (int jc = nts.Count, j = 0; j < jc; ++j)
			{
				var nt = nts[j];
				var o = cfg.GetAttribute(nt, "dependency", false);
				if(o is bool && (bool)o)
				{
					cfg.FillClosure(nt, deps);
				}
			}
			var dups = new HashSet<CfgRule>();
			for (var i = 0; i < ic; ++i)
			{
				var rule = cfg.Rules[i];
				// LL specific
				if (rule.IsDirectlyLeftRecursive)
					result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat("Rule is directly left recursive: ", rule.ToString()),rule.Line,rule.Column,rule.Position, cfg.Filename));
				if (rule.Left.IsNullOrEmpty())
					result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Concat("Rule has empty left hand side:",rule.ToString()),rule.Line,rule.Column,rule.Position, cfg.Filename));
				else if ("#ERROR" == rule.Left || "#EOS" == rule.Left)
					result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Concat("Rule has reserved terminal on left hand side: ", rule.ToString()),rule.Line,rule.Column,rule.Position, cfg.Filename));
				for (int jc = rule.Right.Count, j = 0; j > jc; ++j)
					if (rule.Right[j].IsNullOrEmpty())
						result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Concat("Rule has empty symbols on the right hand side:", rule.ToString()),rule.Line,rule.Column,rule.Position, cfg.Filename));
					else if ("#ERROR" == rule.Right[j] || "#EOS" == rule.Right[j])
						result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Concat("Rule has reserved terminal on right hand side:",rule.ToString()),rule.Line,rule.Column,rule.Position, cfg.Filename));

				for (var j = 0; j < ic; ++j)
					if (i != j && cfg.Rules[j] == rule && dups.Add(rule))
						result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat("Duplicate rule:", rule.ToString()),rule.Line,rule.Column,rule.Position, cfg.Filename));

			}
			var closure = cfg.FillClosure(cfg.StartSymbol);
			var syms = cfg.FillSymbols();
			ic = syms.Count;
			for (var i = 0; i < ic; ++i)
			{
				var sym = syms[i];
				if (!closure.Contains(sym))
				{
					var found = false;
					if (!cfg.IsNonTerminal(sym))
						if ("#EOS" == sym || "#ERROR" == sym || (bool)cfg.GetAttribute(sym, "hidden", false))
							found = true;
					if (!found)
					{
						if (cfg.IsNonTerminal(sym))
						{
							object o = cfg.GetAttribute(sym, "dependency");
							if (o is bool && (bool)o)
								continue;
							o = cfg.GetAttribute(sym, "nowarn");
							if (o is bool && (bool)o)
								continue;
							o = cfg.GetAttribute(sym, "shared");
							if (o is bool && (bool)o)
								continue;
							o = cfg.GetAttribute(sym, "start");
							if (o is bool && (bool)o)
								continue;

							var rules = cfg.FillNonTerminalRules(sym);
							if (0 < rules.Count && !deps.Contains(sym))
							{
								var r = rules[0];
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat("Unreachable symbol \"", sym, "\""), r.Line, r.Column, r.Position, cfg.Filename));
							}
						}
					}
				}
			}
			// build a temporary parse table to check for conflicts
			var predict = cfg.FillPredict();
			var follows = cfg.FillFollows();
			//var pt = new LLParseTable();
			foreach (var nt in cfg.EnumNonTerminals())
			{
				var d = new Dictionary<string, CfgRule>();
				ICollection<(CfgRule Rule, string Symbol)> col;
				if (predict.TryGetValue(nt, out col))
				{
					foreach (var f in col)
					{
						if (null != f.Symbol)
						{
							CfgRule r;
							if (d.TryGetValue(f.Symbol, out r))
							{
								if (r != f.Rule)
								{
									result.Add(new CfgMessage(ErrorLevel.Message, -1,
										string.Format(
											"Rule {0} has a first first conflict with rule {1} on symbol {2} and will require additional lookahead",
											f.Rule,
											r,
											f.Symbol), f.Rule.Line, f.Rule.Column, f.Rule.Position, cfg.Filename));
								}
							}
							else
								d.Add(f.Symbol, f.Rule);
						}
						else
						{
							foreach (var ff in follows[nt])
							{
								CfgRule r;
								if (d.TryGetValue(ff, out r))
								{
									if (r != f.Rule)
									{
										result.Add(new CfgMessage(ErrorLevel.Message, -1,
										string.Format(
											"Rule {0} has a first follow conflict with rule {1} on symbol {2} and will require additional lookahead",
											f.Rule,
											r,
											ff), f.Rule.Line, f.Rule.Column, f.Rule.Position, cfg.Filename));
									}
								}
								else
									d.Add(ff, f.Rule);
							}
						}
					}
				}
			}
			return result;
		}
		static IList<CfgMessage> _TryValidateAttributesLL1(CfgDocument cfg, IList<CfgMessage> result)
		{
			if (null == result)
				result = new List<CfgMessage>();
			string start = null;
			foreach (var attrs in cfg.AttributeSets)
			{
				if (!cfg.IsSymbol(attrs.Key))
				{
					// hidden rules should never be in the grammar
					// so warnings about them not being in the grammar
					// are suppressed.
					var i = attrs.Value.IndexOf("hidden");
					if (0 > i)
						i = attrs.Value.IndexOf("nowarn");
					if (!(-1 < i && attrs.Value[i].Value is bool && ((bool)attrs.Value[i].Value)))
						result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat("Attributes declared on a symbol \"", attrs.Key, "\" that is not in the grammar"), attrs.Value[0].Line, attrs.Value[0].Column, attrs.Value[0].Position, cfg.Filename));
				}
				foreach (var attr in attrs.Value)
				{
					string s;
					var p = string.Concat("On \"", attrs.Key, "\": ");
					switch (attr.Name)
					{
						case "start":
							if (!(attr.Value is bool))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "start attribute expects a bool value and will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
							if (null != start)
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "start attribute was already specified on \"", start, "\" and this declaration will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
							else
								start = attrs.Key;
							continue;
						case "hidden":
							if (!(attr.Value is bool))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "hidden attribute expects a bool value and will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
							continue;
						case "terminal":
							if (!(attr.Value is bool))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "terminal attribute expects a bool value and will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
							continue;
						case "collapsed":
							if (!(attr.Value is bool))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "collapse attribute expects a bool value and will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
							continue;
						case "substitute":
							s = attr.Value as string;
							if (!(attr.Value is string))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "substitute attribute expects a string value and will be ignored"), attr.Line, attr.Column, attr.Position, cfg.Filename));
							else if (string.IsNullOrEmpty(s))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "substitute attribute expects a non-empty string value and will be ignored"), attr.Line, attr.Column, attr.Position, cfg.Filename));
							else if(!cfg.IsSymbol(s))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "substitute attribute expects a symbol reference and will be ignored"), attr.Line, attr.Column, attr.Position, cfg.Filename));
							continue;
						case "blockEnd":
							if (cfg.IsNonTerminal(attrs.Key))
								result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "blockEnd attribute cannot be specified on a non-terminal and will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
							else
							{
								s = attr.Value as string;
								if (!(attr.Value is string))
									result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "blockEnd attribute expects a string value and will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
								else if (string.IsNullOrEmpty(s))
									result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "blockEnd attribute expects a non-empty string value and will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
							}
							continue;
						case "followsConflict":
							s = attr.Value as string;
							switch (s)
							{
								case "error":
								case "first":
								case "last":
									break;
								default:
									result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat(p, "followsError attribute expects \"error\", \"first\", or \"last\" and will revert to \"error\"."),attr.Line,attr.Column,attr.Position, cfg.Filename));
									break;
							}
							continue;
					}
					result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Concat(p, "Unknown attribute \"", attr.Name, "\" will be ignored"),attr.Line,attr.Column,attr.Position, cfg.Filename));
				}

			}
			if (null == start)
				result.Add(new CfgMessage(ErrorLevel.Warning, -1, string.Concat("start attribute was not specified and the first non-terminal in the grammar (\"", cfg.StartSymbol, "\") will be used"),0,0,0, cfg.Filename));
			return result;
		}
		public static IList<CfgMessage> EliminateFirstFirstConflicts(this CfgDocument cfg)
		{
			var result = new List<CfgMessage>();
			foreach (var nt in new List<string>(cfg.EnumNonTerminals()))
			{
				var o = cfg.GetAttribute(nt, "constrained", false);
				if (o is bool && (bool)o)
					continue;
				var rules = cfg.FillNonTerminalRules(nt);
				var rights = new List<IList<string>>();
				foreach (var rule in rules)
					rights.Add(rule.Right);
				while (true)
				{
					var pfx = rights.GetLongestCommonPrefix();
					if (pfx.IsNullOrEmpty())
						break;
					// obv first first conflict
					var nnt = _GetLeftFactorId(cfg,nt);

					var suffixes = new List<IList<string>>();
					foreach (var rule in rules)
					{
						if (rule.Right.StartsWith(pfx))
						{
							rights.Remove(rule.Right);
							suffixes.Add(new List<string>(rule.Right.Range(pfx.Count)));
							cfg.Rules.Remove(rule);
							result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Format("Removed rule {0} because it is part of a first-first conflict.", rule),rule.Line,rule.Column,rule.Position, cfg.Filename));

						}
					}

					var newRule = new CfgRule(nt);
					newRule.Right.AddRange(pfx);
					newRule.Right.Add(nnt);

					if (!cfg.Rules.Contains(newRule))
						cfg.Rules.Add(newRule);
					result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Format("Added rule {0} to resolve first-first conflict.", newRule),0,0,0, cfg.Filename));
					foreach (var suffix in suffixes)
					{
						newRule = new CfgRule(nnt);
						newRule.Right.AddRange(suffix);

						if (!cfg.Rules.Contains(newRule))
							cfg.Rules.Add(newRule);
						result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Format("Added rule {0} to resolve first-first conflict.", newRule),0,0,0, cfg.Filename));
					}

					_SetAttribute(cfg,nnt, "collapsed", true);
					_SetAttribute(cfg, nnt, "nowarn", true);
					_SetAttribute(cfg, nnt, "factored", true);

				}
			}
			return result;
		}
		public static IList<CfgMessage> EliminateFirstFollowsConflicts(this CfgDocument cfg)
		{
			var result = new List<CfgMessage>();
			var conflicts = cfg.FillLL1Conflicts();
			if (conflicts.Count > 100)
			{
				var c = conflicts[0];
				throw new CfgException(string.Format("Too many conflicts in the grammar, first conflict is first-{0} between rule {1} and rule {2} on symbol {3}",CfgLL1ConflictKind.FirstFirst==c.Kind?"first":"follows", c.Rule1,c.Rule2,c.Symbol));
			}
				
			for (int ic = conflicts.Count, i = 0; i < ic; ++i)
			{
				var conflict = conflicts[i];
				if (CfgLL1ConflictKind.FirstFollows == conflict.Kind)
				{
					if (conflict.Rule1.IsNil || conflict.Rule2.IsNil)
					{
						var rule = conflict.Rule1.IsNil ? conflict.Rule1 : conflict.Rule2;
						// we might be able to do something about this.
						var refs = cfg.FillReferencesTo(rule.Left);
						var ntr = cfg.FillNonTerminalRules(rule.Left);
						for (int jc = refs.Count, j = 0; j < jc; ++j)
						{
							for (int kc = ntr.Count, k = 0; k < kc; ++k)
							{
								var ntrr = ntr[k];
								var r = refs[j];
								var rr = new CfgRule(r.Left, r.Right.Replace(rule.Left, ntrr.Right));
								if (!cfg.Rules.Contains(rr))
									cfg.Rules.Add(rr);
								result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Concat("Added rule ", rr.ToString(), " to resolve first-follows conflict."),rr.Line,rr.Column,rr.Position, cfg.Filename));
							}
							result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Concat("Removed rule ", refs[j].ToString(), " to resolve first-follows conflict."),refs[j].Line,refs[j].Column,refs[j].Position, cfg.Filename));
							cfg.Rules.Remove(refs[j]);
						}
						for (int jc = ntr.Count, j = 0; j < jc; ++j)
						{
							cfg.Rules.Remove(ntr[j]);
							result.Add(new CfgMessage(ErrorLevel.Message, -1, string.Concat("Removed rule ", ntr[j].ToString(), " to resolve first-follows conflict."),ntr[j].Line,ntr[j].Column,ntr[j].Position, cfg.Filename));

						}
					}

				}
			}
			return result;
		}
		static bool _HasFirstFirstConflicts(IList<CfgLL1Conflict> conflicts)
		{
			for (int ic = conflicts.Count, i = 0; i < ic; ++i)
				if (CfgLL1ConflictKind.FirstFirst == conflicts[i].Kind)
					return true;
			return false;
		}
		static bool _HasFirstFollowsConflicts(IList<CfgLL1Conflict> conflicts)
		{
			for (int ic = conflicts.Count, i = 0; i < ic; ++i)
				if (CfgLL1ConflictKind.FirstFollows == conflicts[i].Kind)
					return true;
			return false;
		}
		static string _GetRightAssocId(CfgDocument cfg,string s)
		{
			var i = 2;
			var ss = string.Concat(s, "RightAssoc");
			while (cfg.IsSymbol(ss))
			{
				ss = string.Concat(s, "RightAssoc", i.ToString());
				++i;
			}
			return ss;
		}
		static string _GetLeftFactorId(CfgDocument cfg,string s)
		{
			var i = 2;
			var ss = string.Concat(s, "Part");
			while (cfg.IsSymbol(ss))
			{
				ss = string.Concat(s, "Part", i.ToString());
				++i;
			}
			return ss;
		}
		static void _SetAttribute(CfgDocument cfg,string symbol, string name, object value)
		{
			CfgAttributeList attrs;
			if (!cfg.AttributeSets.TryGetValue(symbol, out attrs))
			{
				attrs = new CfgAttributeList();
				cfg.AttributeSets.Add(symbol, attrs);
			}
			var i = attrs.IndexOf(name);
			if (0 > i)
			{
				attrs.Add(new CfgAttribute(name, value));
			}
			else
				attrs[i].Value = value;
		}
	}
}
