using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;

namespace Parsley
{
	// port of PCK's XbnkToPckTransform for use with Rolex and Parsley
	// the rolex spec is written to rolexOutput
	// the CfgDocument is the return value
	using TermPriEntry = KeyValuePair<KeyValuePair<string, XbnfDocument>, int>;
	static class XbnfConvert
	{
	
		public static string ToGplexSymbolConstants(IDictionary<XbnfDocument,CfgDocument> cfgMap)
		{
			var sb = new StringBuilder();
			int termStart;
			var symtbl=GetMasterSymbolTable(cfgMap, out termStart);
			sb.AppendLine("symbol constants follow");
			sb.AppendLine("public const int ErrorSymbol = -1;");
			sb.AppendLine("public const int EosSymbol = -2;");
			var seen = new HashSet<string>();
			seen.Add("ErrorSymbol");
			seen.Add("EosSymbol");
			for(int ic=symtbl.Count,i=termStart;i<ic;++i)
			{
				var se = symtbl[i];
				var name = _EscapeKeyword(_MakeUniqueName(seen, _MakeSafeName(se.Key)));
				sb.Append("public const int ");
				sb.Append(name);
				sb.Append(" = ");
				sb.Append(i.ToString());
				sb.AppendLine(";");
				
			}
			return sb.ToString();
		}
		internal static ListDictionary<string,XbnfDocument> GetMasterSymbolTable(IDictionary<XbnfDocument,CfgDocument> cfgMap, out int termStart)
		{
			var result = new ListDictionary<string, XbnfDocument>();
			var seen = new HashSet<string>();
			XbnfDocument first = null;
			foreach (var ce in cfgMap)
			{
				if (null == first)
					first = ce.Key;

				foreach (var s in ce.Value.FillNonTerminals())
				{
					// this sucks. we have to make sure that 
					// our foreign symbols in the document
					// which we added to augment the FIRSTS
					// do not get counted toward this document's 
					// non terminals. To that end we look to see
					// if any other document contains it.
					// only if no other does does it allow 
					// to add.
					var foreign = false;
					foreach (var d in cfgMap.Keys)
					{
						if (d == ce.Key) continue;
						if(d.Productions.Contains(s))
						{
							foreign = true;
							break;
						}
					}
					if (!foreign)
					{
						if (seen.Add(s))
							result.Add(s, ce.Key);
					}

				}
			}
			termStart = result.Count;
			foreach (var ce in cfgMap)
			{
				var ts = new List<string>();
				foreach (var prod in ce.Key.Productions)
				{
					if (prod.IsTerminal)
					{
						var foreign = false;
						foreach (var d in cfgMap.Keys)
						{
							if (d == ce.Key) continue;
							if (d.Productions.Contains(prod.Name))
							{
								foreign = true;
								break;
							}
						}
						if(!foreign)
							ts.Add(prod.Name);
					}
				}
				foreach (var s in ts)
				{
					//if (0 != string.Compare("#ERROR", s, StringComparison.InvariantCulture) && 0 != string.Compare("#EOS", s, StringComparison.InvariantCulture))
					//{
					if (seen.Add(s))
						result.Add(s, ce.Key);
					//}
				}
			}
			result.Add("#EOS",first);
			result.Add("#ERROR",first);
			return result;
		}
		
		private class _TermPriorityComparer : IComparer<TermPriEntry>
		{
			ICollection<CfgDocument> _cfgs;
			public _TermPriorityComparer(ICollection<CfgDocument> cfgs) { _cfgs = cfgs; }
			public int Compare(TermPriEntry x, TermPriEntry y)
			{
				var px = _FindPriority(x.Key.Key) * 0x100000000 + x.Value;
				// aloha
				var py = _FindPriority(y.Key.Key) * 0x100000000 + y.Value;
				// TODO: verify this.
				var c = py - px;
				if (0 == c) return 0;
				else if (0 > c)
					return -1;
				return 1;
			}
			
			int _FindPriority(string x)
			{
				// not working?!
				if ("#ERROR" == x || "#EOS" == x)
					return int.MinValue;
				foreach (var cfg in _cfgs)
				{
					var o = cfg.GetAttribute(x, "priority");
					if(o is double)
					{
						return (int)(double)o;
					}
				}
				
				return 0;
			}
		}
		
		public static string ToGplexSpec(XbnfGenerationInfo genInfo,string codenamespace,string codeclass)
		{
			var cfgMap = genInfo.CfgMap;
			var syms = new HashSet<string>();
			// use a list dictionary to keep these in order
			var attrSets = new Dictionary<string, XbnfAttributeList>();
			var rules = new List<KeyValuePair<string, IList<string>>>();
			var termStart = 0;
			var stbl = GetMasterSymbolTable(cfgMap,out termStart);
			var stbli = new List<KeyValuePair<KeyValuePair<string, XbnfDocument>, int>>();
			var id = 0;
			
			// assign temp ids
			foreach (var se in stbl)
			{
				if (id >= termStart)
				{
					stbli.Add(new KeyValuePair<KeyValuePair<string, XbnfDocument>, int>(se, id));
				}
				++id;
			}
			stbli.Sort( new _TermPriorityComparer(cfgMap.Values));
			string lexSpec;
			using (var sr = new StreamReader(Assembly.GetExecutingAssembly().GetManifestResourceStream("Parsley.Export.GplexTokenizer.lex")))
				lexSpec=sr.ReadToEnd();
			lexSpec=lexSpec.Replace("Parsley_codenamespace", codenamespace);
			lexSpec = lexSpec.Replace("Parsley_codeclass", codeclass);
			var decls = new StringBuilder();
			for (int ic = stbli.Count, i = 0; i < ic; ++i)
			{
				var se = stbli[i].Key;
				if ("#EOS" == se.Key || "#ERROR" == se.Key)
					continue;
				XbnfExpression e = null;
				foreach(var k in genInfo.TerminalMap)
				{
					if(k.Value ==se.Key)
					{
						e = k.Key;
						break;
					}
				}
				//var te = genInfo.TerminalMap[i];
				//var sym = te.Value;
				//var id = stbli.IndexOf(new KeyValuePair<string, XbnfDocument>(sym, d));
				id = stbli[i].Value;
				if (-1 < id) // some terminals might never be used.
				{
					// implicit terminals do not have productions and therefore attributes
					var s = _ToRegex(se.Value, e, true, true).Replace(@"\'", "");
					decls.Append(s);
					decls.Append(" ");
					var pi = se.Value.Productions.IndexOf(se.Key);
					var isHidden = false;
					string blockEnd = null;
					var isSkipped = false;
					if (-1 < pi)
					{
						var p = se.Value.Productions[pi];
						isHidden = p.IsHidden;
						var ai = p.Attributes.IndexOf("blockEnd");
						if (-1 < ai)
							blockEnd = p.Attributes[ai].Value as string;
						if (string.IsNullOrEmpty(blockEnd))
							blockEnd = null;
						ai = p.Attributes.IndexOf("skipped");
						if(-1<ai)
						{
							var o = p.Attributes[ai].Value;
							if (o is bool && (bool)o)
							{
								isHidden = false;
								isSkipped = true;
							}
						}
					}
					decls.Append("\t{ ");
					if (null!=blockEnd)
					{
						decls.Append("if(!_TryReadUntilBlockEnd(");
						decls.Append(string.Concat("\"", _EscapeLiteral(blockEnd, false), "\")"));
						decls.Append(") { UpdatePosition(yytext); return -1; } ");
					}
					decls.Append("UpdatePosition(yytext); ");
					if(isSkipped)
					{
						decls.Append("Skip(");
						decls.Append(id.ToString());
						decls.Append(");");
					} else if(!isHidden)
					{
						decls.Append("return ");
						decls.Append(id.ToString());
						decls.Append(";");
					}
					decls.AppendLine(" }");
					
				}
				else System.Diagnostics.Debugger.Break();
			}
			
			
			lexSpec = lexSpec.Replace("Parsley_declarations", decls.ToString());
			return lexSpec;
		}
		public static string ToRolexSpec(XbnfDocument document,CfgDocument cfg)
		{
			
			var syms = new HashSet<string>();
			//writer.WriteLine();
			// use a list dictionary to keep these in order
			var tmap = new ListDictionary<XbnfExpression, string>();
			var attrSets = new Dictionary<string, XbnfAttributeList>();
			var rules = new List<KeyValuePair<string, IList<string>>>();
			// below are scratch
			var working = new HashSet<XbnfExpression>();
			var done = new HashSet<XbnfExpression>();

			// now get the terminals and their ids, declaring if necessary
			for (int ic = document.Productions.Count, i = 0; i < ic; ++i)
			{
				var p = document.Productions[i];
				if (p.IsTerminal)
				{
					tmap.Add(p.Expression, p.Name);
					done.Add(p.Expression);
				}
				else
					_VisitFetchTerminals(p.Expression, working);
			}
			foreach (var term in working)
			{
				if (!done.Contains(term))
				{
					var newId = _GetImplicitTermId(syms);
					tmap.Add(term, newId);
				}
			}
			
			var sb = new StringBuilder();
			for (int ic = tmap.Count, i = 0; i < ic; ++i)
			{
				var te = tmap[i];
				var id = cfg.GetIdOfSymbol(te.Value);
				if (-1 < id) // some terminals might never be used.
				{
					// implicit terminals do not have productions and therefore attributes
					var pi = document.Productions.IndexOf(te.Value);
					if (-1 < pi)
					{
						// explicit
						var prod = document.Productions[pi];
						sb.Append(te.Value);
						sb.Append("<id=");
						sb.Append(id);
						foreach (var attr in prod.Attributes)
						{
							sb.Append(", ");
							sb.Append(attr.ToString());
						}
						sb.Append(">");
					} else 
					{
						// implicit
						sb.Append(te.Value);
						sb.Append(string.Concat("<id=", id, ">"));
					}
					sb.AppendLine(string.Concat("= \'", _ToRegex(document, te.Key, true), "\'"));
				}
			}
			return sb.ToString();
		}
		
		public static IList<CfgMessage> TryCreateXbnfImportData(XbnfDocument document,out XbnfGenerationInfo genInfo)
		{
			var imports = new XbnfImportList();
			imports.AddRange(document.Imports);
			_GatherImports(document, imports);
			var cfgMap = new ListDictionary<XbnfDocument,CfgDocument>();
			cfgMap.Add(document, new CfgDocument());
			for (int ic=imports.Count,i=0;i<ic;++i)
			{
				var cfg = new CfgDocument();
				cfgMap.Add(imports[i].Document, new CfgDocument());
			}

			return _TryToGenInfo(document,cfgMap,out genInfo);
		}
		static void _GatherImports(XbnfDocument doc,XbnfImportList result)
		{
			for(int ic=doc.Imports.Count,i=0;i<ic;++i)
			{
				var imp = doc.Imports[i];
				var found = false;
				for (int jc=result.Count,j=0;j<jc;++j)
				{
					var fn = result[i].Document.Filename;
					if (!string.IsNullOrEmpty(fn) && 0==string.Compare(fn,imp.Document.Filename))
					{
						found = true;
						break;
					}
				}
				if (!found)
				{
					result.Add(imp);
					_GatherImports(imp.Document, result);
				}
			}
		}
		
		static IList<CfgMessage> _TryToGenInfo(XbnfDocument document,IDictionary<XbnfDocument, CfgDocument> cfgMap,out XbnfGenerationInfo genInfo)
		{
			genInfo = default(XbnfGenerationInfo);
			var hasErrors = false;
			var result = new List<CfgMessage>();
			var syms = new HashSet<string>();
			// gather the attributes and production names
			foreach (var ce in cfgMap)
			{
				for (int ic = ce.Key.Productions.Count, i = 0; i < ic; ++i)
				{
					var p = ce.Key.Productions[i];
					if(!syms.Add(p.Name))
					{
						result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Format("Duplicate symbol {0} defined.", p.Name), p.Line, p.Column, p.Position, ce.Value.Filename));
						hasErrors = true;
					}
					if (0 < p.Attributes.Count)
					{
						CfgAttributeList attrs;
						if (!ce.Value.AttributeSets.TryGetValue(p.Name, out attrs))
						{
							attrs = new CfgAttributeList();
							ce.Value.AttributeSets.Add(p.Name, attrs);
						}
						for (int jc = p.Attributes.Count, j = 0; j < jc; ++j)
						{
							var attr = p.Attributes[j];
							attrs.Add(new CfgAttribute(attr.Name, attr.Value));
						}
					}
				}
			}
			// use a list dictionary to keep these in order
			var tmap = new ListDictionary<XbnfExpression, string>();
			var attrSets = new Dictionary<string, XbnfAttributeList>();
			var rules = new List<KeyValuePair<string, IList<string>>>();
			IDictionary<string, ICollection<(CfgRule Rule, string Symbol)>> predict = null;
			// below are scratch
			var working = new HashSet<XbnfExpression>();
			var done = new HashSet<XbnfExpression>();
			var firstsLookup = new Dictionary<string, string>();
			var followsLookup = new Dictionary<string, string>();
			var refMap = new Dictionary<string, KeyValuePair<XbnfDocument, XbnfProduction>>();
			var docRefMap = new Dictionary<XbnfDocument, HashSet<string>>();
			// now get the terminals and their ids, declaring if necessary
			foreach (var doc in cfgMap.Keys)
			{

				for (int ic = doc.Productions.Count, i = 0; i < ic; ++i)
				{
					
					var p = doc.Productions[i];
					if (p.IsTerminal)
					{
						string name;
						if (!tmap.TryGetValue(p.Expression, out name))
						{
							tmap.Add(p.Expression, p.Name);

						}
						else
						{
							if (name != p.Name)
							{
								result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Format("{0} attempts to redefine terminal {1}", name, p.Name), p.Line, p.Column, p.Position, doc.Filename));
								hasErrors = true;
							}
						}
						done.Add(p.Expression);
					}
					else
						_VisitFetchTerminals(p.Expression, working);
				}
			}
			if (hasErrors)
				return result ;
			foreach (var term in working)
			{
				if (!done.Contains(term))
				{
					var newId = _GetImplicitTermId(syms);
					var found = false;
					foreach (var d in cfgMap.Keys)
					{
						var prod = d.GetProductionForExpression(term);
						if (null != prod)
						{
							found = true;
							// recycle this symbol
							newId = prod.Name;
							break;
						}
					}
					if(!found)
					{
						document.Productions.Add(new XbnfProduction(newId, term));

					}
					tmap.Add(term, newId);
				}
			}
			// tmap now contains ALL of our terminal definitions from all of our imports
			// now we can use tmap and syms to help solve the rest of our productions
			foreach (var ce in cfgMap)
			{
				var ntd = new Dictionary<string, IList<IList<string>>>();
				var doc = ce.Key;
				var cfg = ce.Value;
				var unref = new HashSet<string>();
				// get all the symbols in the doc which are referred to but 
				// which we can't find 
				for (int ic = doc.Productions.Count, i = 0; i < ic; ++i)
					if (null != doc.Productions[i].Expression)
						_VisitUnreferenced(doc, doc.Productions[i].Expression, unref);
				// now build a map of that data for later
				foreach (var s in unref)
				{
					if(!refMap.ContainsKey(s))
					{
						foreach (var d in cfgMap.Keys)
						{
							var pi = d.Productions.IndexOf(s);
							if (-1 < pi)
							{
								refMap.Add(s, new KeyValuePair<XbnfDocument, XbnfProduction>(d, d.Productions[pi]));
							}
						}
					}
				}
				docRefMap.Add(doc, unref);
				for (int ic = doc.Productions.Count, i = 0; i < ic; ++i)
				{
					var p = doc.Productions[i];
					if (p.IsAbstract)
					{
						// mark this symbol so the cfg "sees" it even
						// though it's never referenced in the grammar
						CfgAttributeList attrs;
						if (!cfg.AttributeSets.TryGetValue(p.Name, out attrs))
						{
							attrs = new CfgAttributeList();
							cfg.AttributeSets.Add(p.Name, attrs);
						}
						var ai = attrs.IndexOf("abstract");
						if (0 > ai)
						{
							attrs.Add(new CfgAttribute("abstract", true));
						}
					}

					if (!p.IsTerminal)
					{
						if (!p.IsVirtual && !p.IsAbstract)
						{

							var dys = _GetDysjunctions(doc, syms, tmap, attrSets, rules, p, p.Expression);
							IList<IList<string>> odys;
							if (ntd.TryGetValue(p.Name, out odys))
							{
								result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Format("The {0} production was specified more than once", p.Name), p.Line, p.Column, p.Position, doc.Filename));
								hasErrors = true;
								throw new InvalidOperationException(string.Format("The {0} production was specified more than once at line {1}, column {2}, position {2}", p.Name, p.Line, p.Column, p.Position));
							}
							var ai = p.Attributes.IndexOf("follows");
							if (-1 < ai)
							{
								var follows = p.Attributes[ai].Value as string;
								if (null != follows)
								{
									followsLookup.Add(p.Name, follows);
								}
							}
							ai = p.Attributes.IndexOf("firsts");
							if (-1 < ai)
							{
								var firsts = p.Attributes[ai].Value as string;
								if (null != firsts)
								{
									firstsLookup.Add(p.Name, firsts);
								}
							}

							ntd.Add(p.Name, dys);
							if (hasErrors)
								return result;
						}
						else if (!p.IsAbstract) // is virtual or grammar
						{

							var ai = p.Attributes.IndexOf("follows");
							if (-1 < ai)
							{
								var dys = new List<IList<string>>();
								var follows = p.Attributes[ai].Value as string;
								if (null != follows)
								{
									if (p.Name == "Type")
										System.Diagnostics.Debugger.Break();
									followsLookup.Add(p.Name, follows);

								}
								else
									dys.Add(new List<string>());

								ntd.Add(p.Name, dys);
							}

							ai = p.Attributes.IndexOf("firsts");
							if (0 > ai)
							{
								var dys = new List<IList<string>>();
								dys.Add(new List<string>());
								ntd.Add(p.Name, dys);
							}
							else
							{
								var dys = new List<IList<string>>();
								var firsts = p.Attributes[ai].Value as string;
								if (null != firsts)
								{
									firstsLookup.Add(p.Name, firsts);
								}
								else
									dys.Add(new List<string>());
								if (!ntd.ContainsKey(p.Name))
									ntd.Add(p.Name, dys);
							}
						}
					}
				}
				// now that we've done that, build the rest of our attributes
				foreach (var sattrs in attrSets)
				{
					CfgAttributeList attrs;
					if (!cfg.AttributeSets.TryGetValue(sattrs.Key, out attrs))
					{
						attrs = new CfgAttributeList();
						cfg.AttributeSets.Add(sattrs.Key, attrs);
					}
					for (int jc = sattrs.Value.Count, j = 0; j < jc; ++j)
					{
						var attr = sattrs.Value[j];
						attrs.Add(new CfgAttribute(attr.Name, attr.Value));
					}

				}
				for (int ic = doc.Productions.Count, i = 0; i < ic; ++i)
				{
					var prod = doc.Productions[i];
					if (prod.Where != null && null != prod.Where.Value)
					{
						CfgAttributeList attrs;
						if (!cfg.AttributeSets.TryGetValue(prod.Name, out attrs))
						{
							attrs = new CfgAttributeList();
							cfg.AttributeSets.Add(prod.Name, attrs);
						}
						if (!attrs.Contains("constrained"))
							attrs.Add(new CfgAttribute("constrained", true));
					}
				}
				// now write our main rules
				foreach (var nt in ntd)
				{
					foreach (var l in nt.Value)
					{
						cfg.Rules.Add(new CfgRule(nt.Key, l));

					}
				}
				// build our secondary rules
				foreach (var rule in rules)
				{

					cfg.Rules.Add(new CfgRule(rule.Key, rule.Value));

				}
				// finally resolve our extra firsts and follows.
				var f = cfg.FillTerminals();
				if (0 < firstsLookup.Count)
				{
					predict = cfg.FillPredict();
					foreach (var fl in firstsLookup)
					{
						
						if (!cfg.IsSymbol(fl.Key))
							continue;
						var sa = fl.Value.Split(' ');
						var seen = new HashSet<string>();
						for (var i = 0; i < sa.Length; i++)
						{
							if (cfg.IsNonTerminal(sa[i]))
							{
								ICollection<(CfgRule Rule, string Symbol)> col;
								if (!predict.TryGetValue(sa[i], out col))
								{
									// it could be stale.
									cfg.ClearCache();
									cfg.RebuildCache();
									predict = cfg.FillPredict();
									if (!predict.TryGetValue(sa[i], out col))
									{
										// last ditch hunt through all cfgs
										var found = false;
										foreach (var ccfg in cfgMap.Values)
										{
											if (!ReferenceEquals(cfg, ccfg) && ccfg.IsSymbol(sa[i]))
											{
												found = true;
												predict = ccfg.FillPredict();
												break;
											}
										}
										if (!found)
										{
											// see if tmap has it so we know it's a terminal

											if (tmap.Values.Contains(sa[i]))
											{
												XbnfExpression expr = null;
												foreach (var t in tmap)
												{
													if (t.Value == sa[i])
													{
														expr = t.Key;
														break;
													}
												}
												XbnfDocument dd = null;
												foreach (var d in cfgMap.Keys)
												{
													var p = d.GetProductionForExpression(expr);
													if (null != p)
													{
														dd = d;
														break;
													}
												}
												if (null != dd)
												{
													// get the associated cfg
													var ccfg = cfgMap[dd];
													CfgAttributeList attrs;
													if (!ccfg.AttributeSets.TryGetValue(sa[i], out attrs))
													{
														attrs = new CfgAttributeList();
														ccfg.AttributeSets.Add(sa[i], attrs);
													}
													var ai = attrs.IndexOf("terminal");
													if (-1 < ai)
														attrs[ai].Value = true;
													else
														attrs.Add(new CfgAttribute("terminal", true));
													predict = ccfg.FillPredict();

												}

											}
										}
										if (!found || !predict.TryGetValue(sa[i], out col)) // error
										{
											var pi = doc.Productions.IndexOf(fl.Key);
											var l = 0;
											var c = 0;
											var p = 0L;
											string fn = null;
											if (-1 < pi)
											{
												var prod = doc.Productions[pi];
												l = prod.Line;
												c = prod.Column;
												p = prod.Position;
												fn = doc.Filename;
											}

											result.Add(new CfgMessage(ErrorLevel.Error, -1, string.Format("Firsts symbol from production {0}, {1} not found in the grammar", fl.Key, sa[i]), l, c, p, fn));
											hasErrors = true;
										}
									}
								}
								if (hasErrors)
									return result;
								foreach (var p in col)
								{
									if (null != p.Symbol && seen.Add(p.Symbol))
									{
										cfg.Rules.Add(new CfgRule(fl.Key, p.Symbol));
									}
								}
							} else
							{
								// it's a terminal, we can simply add it.
								cfg.Rules.Add(new CfgRule(fl.Key, sa[i]));
							}
							
						}
					}
				}
				
				if (hasErrors)
					return result;
				if (0 < followsLookup.Count)
				{

					var firsts = cfg.FillFirsts();
					foreach (var fl in followsLookup)
					{
						if (!cfg.IsSymbol(fl.Key))
							continue;
						var sa = fl.Value.Split(' ');
						var seen = new HashSet<string>();
						for (var i = 0; i < sa.Length; i++)
						{
							if (cfg.IsNonTerminal(sa[i]))
							{
								var fol = firsts[sa[i]];

								foreach (var sym in fol)
								{
									if (null != sym)
									{
										var s = string.Concat(fl.Key, "Follows");
										s = cfg.GetUniqueSymbolName(s);
										cfg.Rules.Add(new CfgRule(s, fl.Key, sym));
										CfgAttributeList attrs;
										
										attrs = new CfgAttributeList();
										cfg.AttributeSets.Add(s, attrs);
										attrs.Add(new CfgAttribute("terminal", false));
										attrs.Add(new CfgAttribute("nowarn", true));
										attrs.Add(new CfgAttribute("nocode", true));
										attrs.Add(new CfgAttribute("constrained", true));


									}
								}
							}
							else
							{
								var s = string.Concat(fl.Key, "Follows");
								s = cfg.GetUniqueSymbolName(s);
								cfg.Rules.Add(new CfgRule(s, fl.Key, sa[i]));
								CfgAttributeList attrs;
								if (!cfg.AttributeSets.TryGetValue(s, out attrs))
								{
									attrs = new CfgAttributeList();
									cfg.AttributeSets.Add(s, attrs);
									attrs.Add(new CfgAttribute("nowarn", true));
									attrs.Add(new CfgAttribute("nocode", true));
								}
							}
						}
					}
					// below was doing above (but badly) twice?
					// seemes like dead code 12/30/2019
					// commented out
					/*foreach (var fl in firstsLookup)
					{
						if (!cfg.IsSymbol(fl.Key))
							continue;
						var sa = fl.Value.Split(' ');
						var seen = new HashSet<string>();
						for (var i = 0; i < sa.Length; i++)
						{
							var pred = predict[sa[i]];

							foreach (var p in pred)
							{
								if (null != p.Symbol && seen.Add(p.Symbol))
								{
									cfg.Rules.Add(new CfgRule(fl.Key, p.Symbol));
								}
							}
						}
					}*/
				}
				if(0<cfg.FillNonTerminals().Count)
				{
					var msgs = cfg.TryPrepareLL1();
					for(int ic=msgs.Count,i=0;i<ic;++i)
					{
						result.Add(msgs[i]);
						if (ErrorLevel.Error== msgs[i].ErrorLevel)
							hasErrors = true;
					}
				}
			}
			// now resolve all of our unreferenced symbols
			// we must creates rules for them in the referencing grammar
			var firstMap = new Dictionary<XbnfDocument, IDictionary<string,ICollection<string>>>();
			foreach (var drme in docRefMap)
			{
				var cfg = cfgMap[drme.Key];
				foreach(var s in drme.Value)
				{
					var refTarget = refMap[s];
					var cfgTarget = cfgMap[refTarget.Key];
					if(!refTarget.Value.IsTerminal)
					{
						IDictionary<string,ICollection<string>> firsts;
						if(!firstMap.TryGetValue(refTarget.Key, out firsts))
						{
							firsts = cfgTarget.FillFirsts();
							firstMap.Add(refTarget.Key, firsts);
						}
						// manufacure some firsts
						foreach(var term in firsts[s])
						{
							cfg.Rules.Add(new CfgRule(s, term));
						}

					}
				}
			}
			// finally, factor the grammars
			foreach(var ce in cfgMap)
			{
				// but only for documents that have non-term productions
				if (ce.Key.HasNonTerminalProductions)
				{
					var res = ce.Value.TryPrepareLL1();
					for (int ic = res.Count, i = 0; i < ic; ++i)
					{
						if (ErrorLevel.Error == res[i].ErrorLevel)
							hasErrors = true;
						result.Add(res[i]);
					}
				}
				
			}
			if (hasErrors)
				return result;
			genInfo.Document = document;
			genInfo.TerminalMap = tmap;
			genInfo.AllExternals = refMap;
			genInfo.ExternalsMap = docRefMap;
			genInfo.CfgMap = cfgMap;		
			return result;

		}
		static string _MergeFirstsFollows(string lhs, ICollection<string> rhs)
		{
			var result = new List<string>();
			if (null != lhs)
			{
				var sa = lhs.Trim().Split(' ');
				foreach (var s in sa)
					if (!result.Contains(s))
						result.Add(s);
			}
			foreach (var s in rhs)
			{
				if (!result.Contains(s))
					result.Add(s);
			}
			return string.Join(" ", result.ToArray());
		}
		static string _MakeSafeName(string name)
		{
			var sb = new StringBuilder();
			if (char.IsDigit(name[0]))
				sb.Append('_');
			for (var i = 0; i < name.Length; ++i)
			{
				var ch = name[i];
				if ('_' == ch || char.IsLetterOrDigit(ch))
					sb.Append(ch);
				else
					sb.Append('_');
			}
			return sb.ToString();
		}
		static string _EscapeKeyword(string name)
		{
			if (name.StartsWith("__") || _FixedStringLookup(_keywords, name))
				return "@" + name;
			return name;
		}
		static bool _FixedStringLookup(string[][] lookupTable, string value)
		{
			int length = value.Length;
			if (length <= 0 || length - 1 >= lookupTable.Length)
			{
				return false;
			}

			string[] subArray = lookupTable[length - 1];
			if (subArray == null)
			{
				return false;
			}
			return _FixedStringLookupContains(subArray, value);
		}
		// TODO: Change below to use HashSet to avoid licensing issues
		#region Lookup Tables
		// from microsoft's reference implementation of the c# code dom provider
		// This routine finds a hit within a single sorted array, with the assumption that the
		// value and all the strings are of the same length.
		private static bool _FixedStringLookupContains(string[] array, string value)
		{
			int min = 0;
			int max = array.Length;
			int pos = 0;
			char searchChar;
			while (pos < value.Length)
			{

				searchChar = value[pos];

				if ((max - min) <= 1)
				{
					// we are down to a single item, so we can stay on this row until the end.
					if (searchChar != array[min][pos])
					{
						return false;
					}
					pos++;
					continue;
				}

				// There are multiple items to search, use binary search to find one of the hits
				if (!_FindCharacter(array, searchChar, pos, ref min, ref max))
				{
					return false;
				}
				// and move to next char
				pos++;
			}
			return true;
		}

		// Do a binary search on the character array at the specific position and constrict the ranges appropriately.
		static bool _FindCharacter(string[] array, char value, int pos, ref int min, ref int max)
		{
			int index = min;
			while (min < max)
			{
				index = (min + max) / 2;
				char comp = array[index][pos];
				if (value == comp)
				{
					// We have a match. Now adjust to any adjacent matches
					int newMin = index;
					while (newMin > min && array[newMin - 1][pos] == value)
					{
						newMin--;
					}
					min = newMin;

					int newMax = index + 1;
					while (newMax < max && array[newMax][pos] == value)
					{
						newMax++;
					}
					max = newMax;
					return true;
				}
				if (value < comp)
				{
					max = index;
				}
				else
				{
					min = index + 1;
				}
			}
			return false;
		}
		static readonly string[][] _keywords = new string[][] {
			null,           // 1 character
            new string[] {  // 2 characters
                "as",
				"do",
				"if",
				"in",
				"is",
			},
			new string[] {  // 3 characters
                "for",
				"int",
				"new",
				"out",
				"ref",
				"try",
			},
			new string[] {  // 4 characters
                "base",
				"bool",
				"byte",
				"case",
				"char",
				"else",
				"enum",
				"goto",
				"lock",
				"long",
				"null",
				"this",
				"true",
				"uint",
				"void",
			},
			new string[] {  // 5 characters
                "break",
				"catch",
				"class",
				"const",
				"event",
				"false",
				"fixed",
				"float",
				"sbyte",
				"short",
				"throw",
				"ulong",
				"using",
				"while",
			},
			new string[] {  // 6 characters
                "double",
				"extern",
				"object",
				"params",
				"public",
				"return",
				"sealed",
				"sizeof",
				"static",
				"string",
				"struct",
				"switch",
				"typeof",
				"unsafe",
				"ushort",
			},
			new string[] {  // 7 characters
                "checked",
				"decimal",
				"default",
				"finally",
				"foreach",
				"private",
				"virtual",
			},
			new string[] {  // 8 characters
                "abstract",
				"continue",
				"delegate",
				"explicit",
				"implicit",
				"internal",
				"operator",
				"override",
				"readonly",
				"volatile",
			},
			new string[] {  // 9 characters
                "__arglist",
				"__makeref",
				"__reftype",
				"interface",
				"namespace",
				"protected",
				"unchecked",
			},
			new string[] {  // 10 characters
                "__refvalue",
				"stackalloc",
			},
		};
#endregion
		static string _MakeUniqueName(ICollection<string> seen, string name)
		{
			var result = name;
			var suffix = 2;
			while (seen.Contains(result))
			{
				result = string.Concat(name, suffix.ToString());
				++suffix;
			}
			seen.Add(result);
			return result;
		}
		static string _ToRegex(XbnfDocument d, XbnfExpression e,bool first,bool gplex=false)
		{
			var le = e as XbnfLiteralExpression;
			if (null != le)
			{
				var s = _EscapeLiteral(XbnfNode.Escape(le.Value),!gplex);
				if (gplex) {
					s = string.Concat("\"", s, "\"");
				}
				return s;
			}
			var rxe = e as XbnfRegexExpression;
			if (null != rxe)
			{
				var r = rxe.Value;
				if (gplex)
					r = r.Replace("\"", "\\\"");
				return first ? r : string.Concat("(", r, ")");
			}
			var rfe = e as XbnfRefExpression;
			if (null != rfe)
				_ToRegex(d, d.Productions[rfe.Symbol].Expression,first,gplex);
			var re = e as XbnfRepeatExpression;
			if (null != re)
			{
				if (re.IsOptional)
					return string.Concat("(", _ToRegex(d, re.Expression,true,gplex), ")*");
				else
					return string.Concat("(", _ToRegex(d, re.Expression,true,gplex), ")+");
			}
			var oe = e as XbnfOrExpression;
			if (null != oe)
			{
				if (!first)
					return string.Concat("(", _ToRegex(d, oe.Left, false,gplex), "|", _ToRegex(d, oe.Right, false,gplex), ")");
				else
					return string.Concat(_ToRegex(d, oe.Left, false,gplex), "|", _ToRegex(d, oe.Right, false,gplex));
			}
			var oc = e as XbnfConcatExpression;
			if (null != oc)
				return string.Concat(_ToRegex(d, oc.Left,false,gplex), _ToRegex(d, oc.Right,false,gplex));
			var ope = e as XbnfOptionalExpression;
			if (null != ope)
				return string.Concat("(", _ToRegex(d, ope.Expression,true,gplex), ")?");
			return "";
		}
		static string _EscapeLiteral(string v, bool regex = true)
		{
			var sb = new StringBuilder();
			
			for (var i = 0; i < v.Length; ++i)
			{
				if (regex)
				{
					switch (v[i])
					{
						case '[':
						case ']':
						case '-':
						case '{':
						case '}':
						case '(':
						case ')':
						case '.':
						case '+':
						case '*':
						case '?':
						case '\'':
						case '|':
						case '<':
						case '>':
						case ';':
							//case '\\':
							sb.Append(string.Concat("\\", v[i].ToString()));
							break;
						default:
							sb.Append(v[i]);
							break;
					}
				} else
				{
					switch (v[i])
					{
						case '\t':
							sb.Append(@"\t");
							break;
						case '\v':
							sb.Append(@"\v");
							break;
						case '\f':
							sb.Append(@"\f");
							break;
						case '\r':
							sb.Append(@"\r");
							break;
						case '\n':
							sb.Append(@"\n");
							break;
						case '\a':
							sb.Append(@"\a");
							break;
						case '\b':
							sb.Append(@"\b");
							break;
						case '\0':
							sb.Append(@"\0");
							break;
						case '\\':
							sb.Append(@"\\");
							break;
						case '\"':
							sb.Append("\"");
							break;
						default:
							sb.Append(v[i]);
							break;
					}
				}
			}
			return sb.ToString();
		}
		static IList<IList<string>> _GetDysjunctions(
			XbnfDocument d,
			ICollection<string> syms,
			IDictionary<XbnfExpression, string> tmap,
			IDictionary<string, XbnfAttributeList> attrs,
			IList<KeyValuePair<string, IList<string>>> rules,
			XbnfProduction p,
			XbnfExpression e
			)
		{
			var le = e as XbnfLiteralExpression;
			if (null != le)
			{
				var res = new List<IList<string>>();
				var l = new List<string>();
				l.Add(tmap[le]);
				res.Add(l);
				return res;
			}
			var rxe = e as XbnfRegexExpression;
			if (null != rxe)
			{
				var res = new List<IList<string>>();
				var l = new List<string>();
				l.Add(tmap[rxe]);
				res.Add(l);
				return res;
			}
			var rfe = e as XbnfRefExpression;
			if (null != rfe)
			{
				var res = new List<IList<string>>();
				var l = new List<string>();
				l.Add(rfe.Symbol);
				res.Add(l);
				return res;
			}
			var ce = e as XbnfConcatExpression;
			if (null != ce)
				return _GetDysConcat(d, syms, tmap, attrs, rules, p, ce);

			var oe = e as XbnfOrExpression;
			if (null != oe)
				return _GetDysOr(d, syms, tmap, attrs, rules, p, oe);
			var ope = e as XbnfOptionalExpression;
			if (null != ope)
			{
				return _GetDysOptional(d, syms, tmap, attrs, rules, p, ope);
			}
			var re = e as XbnfRepeatExpression;
			if (null != re)
				return _GetDysRepeat(d, syms, tmap, attrs, rules, p, re);
			throw new NotSupportedException("The specified expression type is not supported.");
		}

		static IList<IList<string>> _GetDysOptional(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfOptionalExpression ope)
		{
			var l = new List<IList<string>>();
			if (null != ope.Expression)
			{
				l.AddRange(_GetDysjunctions(d, syms, tmap, attrs, rules, p, ope.Expression));
				var ll = new List<string>();
				if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
					l.Add(ll);
			}
			return l;
		}

		static IList<IList<string>> _GetDysRepeat(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfRepeatExpression re)
		{
			string sid = null;
			var sr = re.Expression as XbnfRefExpression;
			if (null != d && null != sr)
				sid = string.Concat(sr.Symbol, "List");
			if (string.IsNullOrEmpty(sid))
			{
				var cc = re.Expression as XbnfConcatExpression;
				if (null != cc)
				{
					sr = cc.Right as XbnfRefExpression;
					if (null != sr)
						sid = string.Concat(sr.Symbol, "ListTail");
				}
			}
			if (string.IsNullOrEmpty(sid))
				sid = string.Concat(p.Name,"List");
			var listId = sid;
			var i = 2;
			var ss = listId;
			while (syms.Contains(ss))
			{
				ss = string.Concat(listId, i.ToString());
				++i;
			}
			syms.Add(ss);
			listId = ss;
			var attr = new XbnfAttribute("collapsed", true);
			var attr2 = new XbnfAttribute("nowarn", true);
			var attr3 = new XbnfAttribute("factored", true);
			var attrlist = new XbnfAttributeList();
			attrlist.Add(attr);
			attrlist.Add(attr2);
			attrlist.Add(attr3);
			attrs.Add(listId, attrlist);
			var expr =
				new XbnfOrExpression(
					new XbnfConcatExpression(
						new XbnfRefExpression(listId), re.Expression), re.Expression); ;
			foreach (var nt in _GetDysjunctions(d, syms, tmap, attrs, rules, p, expr))
			{
				var l = new List<string>();
				var r = new KeyValuePair<string, IList<string>>(listId, l);
				foreach (var s in nt)
				{
					if (1 < r.Value.Count && null == s)
						continue;
					r.Value.Add(s);
				}
				rules.Add(r);
			}
			if (!re.IsOptional)
				return new List<IList<string>>(new IList<string>[] { new List<string>(new string[] { listId }) });
			else
			{
				var res = new List<IList<string>>();
				res.Add(new List<string>(new string[] { listId }));
				res.Add(new List<string>());
				return res;
			}
		}

		static IList<IList<string>> _GetDysOr(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfOrExpression oe)
		{
			var l = new List<IList<string>>();
			if (null == oe.Left)
				l.Add(new List<string>());
			else
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, oe.Left))
					if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
						l.Add(ll);
			if (null == oe.Right)
			{
				var ll = new List<string>();
				if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
					l.Add(ll);
			}
			else
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, oe.Right))
					if (!l.Contains(ll, OrderedCollectionEqualityComparer<string>.Default))
						l.Add(ll);
			return l;
		}

		static IList<IList<string>> _GetDysConcat(XbnfDocument d, ICollection<string> syms, IDictionary<XbnfExpression, string> tmap, IDictionary<string, XbnfAttributeList> attrs, IList<KeyValuePair<string, IList<string>>> rules, XbnfProduction p, XbnfConcatExpression ce)
		{
			var l = new List<IList<string>>();
			if (null == ce.Right)
			{
				if (null == ce.Left) return l;
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Left))
					l.Add(new List<string>(ll));
				return l;
			}
			else if (null == ce.Left)
			{
				foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Right))
					l.Add(new List<string>(ll));
				return l;
			}
			foreach (var ll in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Left))
			{
				foreach (var ll2 in _GetDysjunctions(d, syms, tmap, attrs, rules, p, ce.Right))
				{
					var ll3 = new List<string>();
					ll3.AddRange(ll);
					ll3.AddRange(ll2);
					if (!l.Contains(ll3, OrderedCollectionEqualityComparer<string>.Default))
						l.Add(ll3);
				}
			}
			return l;
		}
		static string _GetImplicitTermId(ICollection<string> syms)
		{
			var result = "Implicit";
			var i = 2;
			while (syms.Contains(result))
			{
				result = string.Concat("Implicit", i.ToString());
				++i;
			}
			syms.Add(result);
			return result;
		}
		static void _VisitFetchTerminals(XbnfExpression expr, HashSet<XbnfExpression> terms)
		{
			var l = expr as XbnfLiteralExpression;
			if (null != l)
			{
				if (!terms.Contains(l))
					terms.Add(l);
				return;
			}
			var r = expr as XbnfRegexExpression;
			if (null != r)
			{
				if(!terms.Contains(r))
					terms.Add(r);
				return;
			}
			var u = expr as XbnfUnaryExpression;
			if (null != u)
			{
				_VisitFetchTerminals(u.Expression, terms);
				return;
			}
			var b = expr as XbnfBinaryExpression;
			if (null != b)
			{
				_VisitFetchTerminals(b.Left, terms);
				_VisitFetchTerminals(b.Right, terms);
				return;
			}

		}
		static void _VisitUnreferenced(XbnfDocument doc,XbnfExpression expr, HashSet<string> result)
		{
			var r = expr as XbnfRefExpression;
			if (null != r)
			{
				if (!doc.Productions.Contains(r.Symbol))
					result.Add(r.Symbol);
				return;
			}
			var u = expr as XbnfUnaryExpression;
			if (null != u)
			{
				_VisitUnreferenced(doc,u.Expression, result);
				return;
			}
			var b = expr as XbnfBinaryExpression;
			if (null != b)
			{
				_VisitUnreferenced(doc,b.Left, result);
				_VisitUnreferenced(doc,b.Right, result);
				return;
			}

		}
	}
}