using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RE
{
	/// <summary>
	/// Represents the common functionality of all regular expression elements
	/// </summary>
#if REGEXLIB
	public 
#endif
	abstract class RegexExpression : ICloneable {
		/// <summary>
		/// Indicates the 1 based line on which the regular expression was found
		/// </summary>
		public int Line { get; set; } = 1;
		/// <summary>
		/// Indicates the 1 based column on which the regular expression was found
		/// </summary>
		public int Column { get; set; } = 1;
		/// <summary>
		/// Indicates the 0 based position on which the regular expression was found
		/// </summary>
		public long Position { get; set; } = 0L;
		/// <summary>
		/// Indicates whether or not this statement is a single element or not
		/// </summary>
		/// <remarks>If false, this statement will be wrapped in parentheses if necessary</remarks>
		public abstract bool IsSingleElement { get; }
		/// <summary>
		/// Sets the location information for the expression
		/// </summary>
		/// <param name="line">The 1 based line where the expression appears</param>
		/// <param name="column">The 1 based column where the expression appears</param>
		/// <param name="position">The 0 based position where the expression appears</param>
		public void SetLocation(int line, int column, long position)
		{
			Line = line;
			Column = column;
			Position = position;
		}
		/// <summary>
		/// Creates a copy of the expression
		/// </summary>
		/// <returns>A copy of the expression</returns>
		protected abstract RegexExpression CloneImpl();
		object ICloneable.Clone() => CloneImpl();
		/// <summary>
		/// Creates a state machine representing this expression
		/// </summary>
		/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
		/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
		public CharFA<TAccept> ToFA<TAccept>()
			=> ToFA(default(TAccept));
		/// <summary>
		/// Creates a state machine representing this expression
		/// </summary>
		/// <typeparam name="TAccept">The type of accept symbol to use for this expression</typeparam>
		/// <param name="accept">The accept symbol to use for this expression</param>
		/// <returns>A new <see cref="CharFA{TAccept}"/> finite state machine representing this expression</returns>
		public abstract CharFA<TAccept> ToFA<TAccept>(TAccept accept);
		/// <summary>
		/// EXPERIMENTAL, INCOMPLETE
		/// Builds an abstract syntax tree/DOM from the specified state machine
		/// </summary>
		/// <typeparam name="TAccept">The type of accept symbol</typeparam>
		/// <param name="fa">The state machine to analyze</param>
		/// <param name="progress">An optional <see cref="IProgress{CharFAProgress}"/> instance used to report the progress of the task</param>
		/// <returns>A regular expression syntax tree representing <paramref name="fa"/></returns>
		public static RegexExpression FromFA<TAccept>(CharFA<TAccept> fa, IProgress<CharFAProgress> progress = null)
		{
			// see https://stackoverflow.com/questions/53608410/state-elimination-dfa-to-regular-expression
			// for the concepts
			fa = _ToGnfa(fa, progress);
			var closure = fa.FillClosure();
			var tt = _GetTransitionTable(closure);
			var ttl = tt as IList<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>>;

			var fsm = closure[0];
			for(var z = 0;z<10;++z) // for testing
			//while (1 < tt.Count)
			{
				Console.WriteLine("Running state removal iteration on:");
				_DumpTransitionTable(closure,tt);
				
				var trns = _GetTransOfState(tt, fsm, true);
				// break from this loop whenever we modify tt
				for(int kc=trns.Count,k=0;k<kc;++k)
				{
					var t = trns[k];
					var ti = tt.IndexOfKey(t.Key);
					var qin = t.Key.Key;
					var qrip = t.Key.Value;
					var inExpr = t.Value;
					// breaking from this and the outer loop
					// requires modifying k
					var dstTrns = _GetTransOfState(tt, qrip, true);
					for(int mc=dstTrns.Count,m=0;m<mc;++m)
					{
						var dt = dstTrns[m];
						dt = new KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>(
							new KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>(
								dt.Key.Key, 
								dt.Key.Value), 
							tt[dt.Key]);
						var qout = dt.Key.Value;
						var ripExpr = dt.Value;
						var isLoop = closure.IndexOf(qout) <= closure.IndexOf(qrip);
						var e = inExpr;
						if (null != e)
							e = new RegexConcatExpression(e, ripExpr);
						else
							e = ripExpr;
						if (isLoop)
						{
							Console.WriteLine("Found loop on {0}", _TransToString(closure, dt));
							e = new RegexRepeatExpression(e);
							Console.WriteLine("Enum transitions of q{0}", closure.IndexOf(qout));
							var rtrns = _GetTransOfState(tt, qout,true);
							for (int qc = rtrns.Count, q = 0; q < qc; ++q)
							{
								var rt = rtrns[q];
								// don't want to modify the loop transition itself
								if (rt.Key.Value != qout)
								{
									Console.WriteLine("Prepending loop to {0}", _TransToString(closure, rt));
									tt[rt.Key] = new RegexConcatExpression(e, tt[rt.Key]);
								}
								else
								{
									Console.WriteLine("Removing loop {0}", _TransToString(closure, rt));
									if (!tt.Remove(rt.Key))
										Console.WriteLine("Failure removing {0}", _TransToString(closure, rt));
								}
							}
							Console.WriteLine("Removing loop {0}", _TransToString(closure, dt));
							if(!tt.Remove(dt.Key))
								Console.WriteLine("Failure removing {0}", _TransToString(closure, dt));
				
						}
						
						// now modify qin->qrip inexpr
						// to qin->qout inexpr+e
						var nt = new KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>(new KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>(qin, qout), e);
						RegexExpression lhs;
						if (0 == m)
						{
							if (!tt.TryGetValue(nt.Key, out lhs))
							{
								ttl[ti] = nt;
							}
							else
							{
								var tti = tt.IndexOfKey(nt.Key);
								//ttl[tti] = default(KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>);
								ttl[tti] = new KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>(nt.Key,
									new RegexOrExpression(lhs, nt.Value));
							}
						}
						else
						{
							if (!tt.TryGetValue(nt.Key, out lhs))
							{
								ttl.Insert(1, nt);
							}
							else
							{
								ttl.Insert(1, new KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>(nt.Key,
									new RegexOrExpression(lhs, nt.Value)));
							}

						}
						
						//k = int.MaxValue-1;
				
					}
				}
			}
			return ttl[0].Value;
		}
		static RegexExpression FromFA2<TAccept>(CharFA<TAccept> fa, IProgress<CharFAProgress> progress = null)
		{
			// see https://stackoverflow.com/questions/53608410/state-elimination-dfa-to-regular-expression
			// for the concepts
			fa = _ToGnfa(fa, progress);
			var closure = fa.FillClosure();
			var tt = _GetTransitionTable(closure);
			var ttl = tt as IList<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>>;
			
			var fsm = closure[0];
			while (1 < tt.Count)
			//for (var i = 0; i < 4; ++i) // for testing
			{
				Console.WriteLine("Running state removal iteration");
				Console.WriteLine();
				Console.WriteLine("Transition table:");
				_DumpTransitionTable(closure, tt);

				// always work on the start state
				var trns = _GetTransOfState(tt, fsm, true);

				for (int kc = trns.Count, k = 0; k < kc; ++k)
				{
					var t = trns[k];
					// we have to make sure we have the latest expression from tt, because trns gets stale
					t = new KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>(t.Key, tt[t.Key]);
					Console.WriteLine("Process {0}",_TransToString(closure,t));
					// get the destination state's transitions
					var trns2 = _GetTransOfState(tt, t.Key.Value, true);
					for (int mc = trns2.Count, m = 0; m < mc; ++m)
					{
						Console.WriteLine();
						var td = trns2[m];
						var isLoop = (closure.IndexOf(td.Key.Value) <= closure.IndexOf(t.Key.Value));
						// source expression
						var e = t.Value;
						// construct a new transition entry
						// from the first state to the destination
						// state's destination state
						// with a concat of the source expression
						// and the destination expression
						if (null != e)
							e = new RegexConcatExpression(e, td.Value);
						else
							e = td.Value;
						if (isLoop)
						{
							var rce = e as RegexConcatExpression;
							rce.Right = new RegexRepeatExpression(rce.Right);
						}

						// new key from the source to the destination's destination
						// skipping qrip (the middle) which is t.Key.Value
						var key = new KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>(t.Key.Key, td.Key.Value);
						RegexExpression v;
						// if it's already in here, we need to "or" it using regex | 
						if (tt.TryGetValue(key, out v))
						{
							e = new RegexOrExpression(e, v);
							Console.WriteLine("Set q{0}->q{1} to {2}", closure.IndexOf(t.Key.Key), closure.IndexOf(t.Key.Value), e);
							tt[t.Key] = e;
						}
						else
						{
							// we have to replace the key, because we're changing
							// the transition to point to a new location
							//Console.WriteLine("Remove q{0}->q{1}: {2}", closure.IndexOf(t.Key.Key), closure.IndexOf(t.Key.Value), t.Value);
							//if (!tt.Remove(t.Key))
							//	Console.WriteLine("Remove failure");
							//Console.WriteLine("Add q{0}->q{1}: {2}", closure.IndexOf(key.Key), closure.IndexOf(key.Value), e);
							var i = tt.IndexOfKey(t.Key);
							if (-1 == i) System.Diagnostics.Debugger.Break();
							var nt = new KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>(key, e); ;
							Console.WriteLine("Set {0} to {1}", _TransToString(closure,ttl[i]), _TransToString(closure,nt));
							ttl[i] = nt;
						}
						// we need to eliminate this item because we're skipping it now
						Console.WriteLine("Remove q{0}->q{1}", closure.IndexOf(t.Key.Value), closure.IndexOf(td.Key.Value));
						if (!tt.Remove(new KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>(t.Key.Value, td.Key.Value)))
							Console.WriteLine("Remove failure");
						if (isLoop)
						{
							// our loops like (foo)* or going like q2->q1 (foo)* and we don't have anything
							// to replace q1 with, so we have to prepend (foo)* to every expression starting from
							// q2 and then eliminate this transition
							// prepend to any transition that wasn't already heading into the loop
							Console.WriteLine("Prepend to q{0}", closure.IndexOf(t.Key.Key));
							foreach (var ttt in new List<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>>(tt))
							{
								if (t.Key.Key == ttt.Key.Key /*&& (t.Key.Value != ttt.Key.Value)*/)
								{
									Console.WriteLine("Found q{0} - transition table:",closure.IndexOf(t.Key.Key));
									_DumpTransitionTable(closure, tt);

									if (closure.IndexOf(ttt.Key.Value) > closure.IndexOf(t.Key.Key))
									{
										Console.WriteLine("Dump ttt: {0}", _TransToString(closure, ttt));
										Console.WriteLine("Dump t: {0}", _TransToString(closure, t));
										if (null == ttt.Value || null == e)
											Console.WriteLine("expr was null");
										var ee = new RegexConcatExpression(e, ttt.Value);
										Console.WriteLine("Set q{0}->q{1} to {2}", closure.IndexOf(ttt.Key.Key), closure.IndexOf(ttt.Key.Value), ee);
										tt[ttt.Key] = ee;
									}
									Console.Write("*");
								}
								Console.WriteLine("q{0}->q{1}: {2}", closure.IndexOf(ttt.Key.Key), closure.IndexOf(ttt.Key.Value), ttt.Value);
							}
							Console.WriteLine("(Loop) Remove q{0}->q{1}: {2}", closure.IndexOf(t.Key.Key), closure.IndexOf(td.Key.Value), t.Value);
							if (0 < tt.Count && !tt.Remove(new KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>(t.Key.Key, td.Key.Value)))
								Console.WriteLine("Removal error");
							Console.WriteLine();
							e = null;
						}

					}

				}
				Console.WriteLine("Enum after processing");
				_DumpTransitionTable(closure, tt);

			}



			// return the final transition's expression
			return tt.GetAt(0);
		}

		private static CharFA<TAccept> _ToGnfa<TAccept>(CharFA<TAccept> fa, IProgress<CharFAProgress> progress)
		{
			fa = fa.ToDfa(progress);
			fa.TrimDuplicates(progress);
			// using the state removal method 
			// first convert to a GNFA
			fa.Finalize();
			var last = fa.FirstAcceptingState;
			if (!last.IsFinal)
			{
				// sometimes our last state isn't final,
				// so we have to extend the machine to have
				// a final last state
				last.IsAccepting = false;
				last.EpsilonTransitions.Add(new CharFA<TAccept>(true));
			}
			if (!fa.IsNeutral) // should never be true but just in case
			{
				// add a neutral transition to the beginning
				var nfa = new CharFA<TAccept>();
				nfa.EpsilonTransitions.Add(fa);
				fa = nfa;
			}

			return fa;
		}

		static void _DumpTransitionTable<TAccept>(IList<CharFA<TAccept>> closure, ListDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression> tt)
		{
			foreach (var t in tt)
				Console.WriteLine("  {0}", _TransToString(closure,t));
			Console.WriteLine();
		}
		static string _TransToString<TAccept>(IList<CharFA<TAccept>> closure,KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression> trn)
		{
			return string.Format("q{0}->q{1}: {2}", closure.IndexOf(trn.Key.Key), closure.IndexOf(trn.Key.Value), trn.Value);
		}
		static ListDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression> _GetTransitionTable<TAccept>(IList<CharFA<TAccept>> closure)
		{

			// compute our transitions table (use a listdictionary to preserve the order)
			// this is pretty easy. We just gather our ranges and add each transition
			// in the state machine with it's associated range expression
			var tt = new ListDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>();
			for (int ic = closure.Count, i = 0; i < ic; ++i)
			{
				var ffa = closure[i];
				var trgs = ffa.FillInputTransitionRangesGroupedByState();
				foreach (var fffa in ffa.EpsilonTransitions)
					tt.Add(new KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>(ffa, fffa), null);

				foreach (var trg in trgs)
				{
					RegexExpression expr2;
					if (1 == trg.Value.Count && 1 == trg.Value[0].Length)
						expr2 = new RegexLiteralExpression(trg.Value[0][0]);
					else
					{
						var csel = new List<RegexCharsetEntry>();
						foreach (var rng in trg.Value)
							if (rng.First == rng.Last)
								csel.Add(new RegexCharsetCharEntry(rng.First));
							else
								csel.Add(new RegexCharsetRangeEntry(rng.First, rng.Last));
						expr2 = new RegexCharsetExpression(csel);
					}
					tt.Add(new KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>(ffa, trg.Key), expr2);
				}
			}

			return tt;
		}

		private static void _ProcessLoops<TAccept>(ListDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression> tt, IList<CharFA<TAccept>> closure, IList<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>> trns)
		{
			// for each transition
			foreach (var t in trns)
			{
				// get the destination state's transitions
				foreach (var td in _GetTransOfState(tt, t.Key.Value, true))
				{
					// is it a loop?
					if (closure.IndexOf(td.Key.Value) <= closure.IndexOf(t.Key.Value))
					{
						Console.WriteLine("Loop detected");
						// source expression
						var e = t.Value;
						// construct a new transition entry
						// from the first state to the destination
						// state's destination state
						// with a concat of the source expression
						// and the destination expression
						if (null != e)
							e = new RegexConcatExpression(e, td.Value);
						else
							e = td.Value;
						e = new RegexRepeatExpression(e);
						// prepend it to every one of the 
						// transitions that moves from it
						// except itself, then remove it
						// from tt manually since dead 
						// state removal won't pick it up
						// dst dst is actually our loop 
						// source (loop loops back)
						Console.WriteLine("Transitions from q{0}", closure.IndexOf(t.Key.Key));
						var srcs = _GetTransOfState(tt, t.Key.Key);
						foreach (var from in srcs)
						{
							Console.WriteLine("Read src");
							//if (from.Key.Key != from.Key.Value)
							//{
							// some of these are part of the loop.
							// somehow, we have to remove them
							tt[from.Key] = new RegexConcatExpression(e, tt[from.Key]);
							Console.WriteLine("Set loop q{0}->q{1}", closure.IndexOf(from.Key.Key), closure.IndexOf(from.Key.Value));

							/*}
							else {
								if (!tt.Remove(from.Key))
									Console.WriteLine("Remove Failure");
								else
									Console.WriteLine("Remove success");
							}*/
						}
					}
				}
			}
		}

		static void _EliminateSelfLoops<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression> tt, CharFA<TAccept> fa=null)
		{
			if(null==fa)
			{
				foreach (var t in new List<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>>(tt))
					_EliminateSelfLoops(tt, t.Key.Key);
				return;
			}
			var trns = _GetTransOfState(tt, fa, true);
			// for each transition of the specified state fa
			foreach (var t in trns)
			{
				// if the state loops to itself
				// we take its expression and we
				// prepend it to every other 
				// transition for that same state
				// before finally eliminating the
				// self loop
				if (t.Key.Key == t.Key.Value)
				{
					// for each transition that points to this loop state
					foreach (var t2 in _GetDstsToState(tt,t.Key.Key))
						// don't mess with itself
						if (t2.Key.Key != t2.Key.Value)
							tt[t2.Key] = new RegexConcatExpression(t2.Value, t.Value);

					tt.Remove(t.Key);
				}
			}
		}
		static IList<KeyValuePair<KeyValuePair<CharFA<TAccept>,CharFA<TAccept>>,RegexExpression>> _GetTransOfState<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression> tt,CharFA<TAccept> fa,bool includeSelf=false)
		{
			var result = new List<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>>();
			foreach (var trn in tt)
				if(trn.Key.Key==fa && (includeSelf || fa !=trn.Key.Value))
					result.Add(trn);
			return result;
		}
		static IList<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>> _GetDstsToState<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression> tt, CharFA<TAccept> fa)
		{
			var result = new List<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>>();
			foreach (var t in tt)
				if (t.Key.Value == fa)
					result.Add(t);
			return result;
		}
		static void _EliminateDeadStates<TAccept>(IDictionary<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression> tt,IList<CharFA<TAccept>> closure)
		{
		
			//var ttc = new List<KeyValuePair<KeyValuePair<CharFA<TAccept>, CharFA<TAccept>>, RegexExpression>>(tt);
			var dead = new List<CharFA<TAccept>>();
			foreach (var t in tt)
			{
				var srci = closure.IndexOf(t.Key.Key);
				//var dsti = closure.IndexOf(t.Key.Value);
				// we never eliminate the start state
				if(0!=srci)
				{
					// nothing points to this state
					var dsts = _GetDstsToState(tt, t.Key.Key);
					if (0==dsts.Count)
						if(!dead.Contains(t.Key.Key))
							dead.Add(t.Key.Key);
				}
			}
			foreach(var fa in dead)
				foreach(var t in _GetTransOfState(tt,fa,true))
					tt.Remove(t.Key);
		}
		static RegexExpression _FromFA<TAccept>(CharFA<TAccept> fa, HashSet<CharFA<TAccept>> visited)
		{
			if (!visited.Add(fa))
				return null;
			var trgs = fa.FillInputTransitionRangesGroupedByState();
			bool isAccepting = fa.IsAccepting;
			RegexExpression expr = null;
			foreach (var trg in trgs)
			{
				if (1 == trg.Value.Count && 1 == trg.Value[0].Length)
				{
					RegexExpression le = new RegexLiteralExpression(trg.Value[0][0]);
					var next = _FromFA(trg.Key, visited);
					if (null != next)
						le = new RegexConcatExpression(le, next);
					if (null == expr)
						expr = le;
					else
						expr = new RegexOrExpression(expr, le);
				}
				else
				{
					var csel = new List<RegexCharsetEntry>();
					foreach (var rng in trg.Value)
					{
						if (rng.First == rng.Last)
							csel.Add(new RegexCharsetCharEntry(rng.First));
						else
							csel.Add(new RegexCharsetRangeEntry(rng.First, rng.Last));
					}
					RegexExpression cse = new RegexCharsetExpression(csel);
					var next = _FromFA(trg.Key, visited);
					if (null != next)
						cse = new RegexConcatExpression(cse, next);
					if (null == expr)
						expr = cse;
					else
						expr = new RegexOrExpression(expr, cse);
				}
				
				
			}
			var isLoop = false;
			foreach (var val in fa.Descendants)
			{
				if (val == fa)
				{
					isLoop = true;
					break;
				}
			}

			if (isAccepting && !fa.IsFinal && !isLoop)
				expr = new RegexOptionalExpression(expr);
			
			return expr;
		}
		/// <summary>
		/// Appends the textual representation to a <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="sb">The string builder to use</param>
		/// <remarks>Used by ToString()</remarks>
		protected internal abstract void AppendTo(StringBuilder sb);
		/// <summary>
		/// Gets a textual representation of the expression
		/// </summary>
		/// <returns>A string representing the expression</returns>
		public override string ToString()
		{
			var result = new StringBuilder();
			AppendTo(result);
			return result.ToString();
		}
		/// <summary>
		/// Parses a regular expresion from the specified string
		/// </summary>
		/// <param name="string">The string</param>
		/// <returns>A new abstract syntax tree representing the expression</returns>
		public static RegexExpression Parse(IEnumerable<char> @string) 
			=> Parse(ParseContext.Create(@string));
		/// <summary>
		/// Parses a regular expresion from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The text reader</param>
		/// <returns>A new abstract syntax tree representing the expression</returns>
		public static RegexExpression ReadFrom(TextReader reader)
			=> Parse(ParseContext.CreateFrom(reader));
		/// <summary>
		/// Parses a regular expression from the specified <see cref="ParseContext"/>
		/// </summary>
		/// <param name="pc">The parse context to use</param>
		/// <returns>A new abstract syntax tree representing the expression</returns>
		public static RegexExpression Parse(ParseContext pc)
		{
			RegexExpression result=null,next=null;
			int ich;
			pc.EnsureStarted();
			var line = pc.Line;
			var column = pc.Column;
			var position = pc.Position;
			while (true)
			{
				switch (pc.Current)
				{
					case -1:
						return result;
					case '.':
						var nset = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetRangeEntry(char.MinValue, char.MaxValue) }, false);
						nset.SetLocation(line, column, position);
						if (null == result)
							result = nset;
						else
						{
							result = new RegexConcatExpression(result, nset);
							result.SetLocation(line, column, position);
						}
						pc.Advance();
						result = _ParseModifier(result, pc);
						line = pc.Line;
						column = pc.Column;
						position = pc.Position;
						break;
					case '\\':
						
						pc.Advance();
						pc.Expecting();
						var isNot = false;
						switch (pc.Current)
						{
							case 'P':
								isNot = true;
								goto case 'p';
							case 'p':
								pc.Advance();
								pc.Expecting('{');
								var uc = new StringBuilder();
								int uli = pc.Line;
								int uco = pc.Column;
								long upo = pc.Position;
								while (-1!=pc.Advance() && '}'!=pc.Current)
									uc.Append((char)pc.Current);
								pc.Expecting('}');
								pc.Advance();
								IList<CharRange> cr;
								if (!CharFA<string>.UnicodeCategories.TryGetValue(uc.ToString(), out cr))
									throw new ExpectingException(string.Format("Expecting a unicode category but found \"{0}\" at line {1}, column {2}, position {3}",uc.ToString(),uli,uco,upo), uli, uco, upo);
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetUnicodeCategoryEntry(uc.ToString())}, isNot);
								break;
							case 'd':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("digit") });
								pc.Advance();
								break;
							case 'D':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("digit") },true);
								pc.Advance();
								break;
							case 'h':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("blank") });
								pc.Advance();
								break;
							case 'l':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("lower") });
								pc.Advance();
								break;
							case 's':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("space") });
								pc.Advance();
								break;
							case 'S':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("space") },true);
								pc.Advance();
								break;
							case 'u':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("upper") });
								pc.Advance();
								break;
							case 'w':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("word") });
								pc.Advance();
								break;
							case 'W':
								next = new RegexCharsetExpression(new RegexCharsetEntry[] { new RegexCharsetClassEntry("word") },true);
								pc.Advance();
								break;
							default:
								if (-1 != (ich = _ParseEscapePart(pc)))
								{
									next = new RegexLiteralExpression((char)ich);
								}
								else
								{
									pc.Expecting(); // throw an error
									return null; // doesn't execute
								}
								break;
						}
						next.SetLocation(line, column, position);
						next = _ParseModifier(next, pc);
						if (null != result)
						{
							result = new RegexConcatExpression(result, next);
							result.SetLocation(line, column, position);
						}
						else
							result = next;
						line = pc.Line;
						column = pc.Column;
						position = pc.Position;
						break;
					case ')':
						return result;
					case '(':
						pc.Advance();
						pc.Expecting();
						next = Parse(pc);
						pc.Expecting(')');
						pc.Advance();
						next = _ParseModifier(next, pc);
						if (null == result)
							result = next;
						else
						{
							result = new RegexConcatExpression(result, next);
							result.SetLocation(line, column, position);
						}
						line = pc.Line;
						column = pc.Column;
						position = pc.Position;
						break;
					case '|':
						if (-1 != pc.Advance())
						{
							next = Parse(pc);
							result = new RegexOrExpression(result, next);
							result.SetLocation(line, column, position);
						}
						else
						{
							result = new RegexOrExpression(result, null);
							result.SetLocation(line, column, position);
						}
						line = pc.Line;
						column = pc.Column;
						position = pc.Position;
						break;
					case '[':
						pc.ClearCapture();
						pc.Advance();
						pc.Expecting();
						bool not = false;
					
						
						if ('^' == pc.Current)
						{
							not = true;
							pc.Advance();
							pc.Expecting();
						}
						var ranges = _ParseRanges(pc);
						if (ranges.Count==0)
							System.Diagnostics.Debugger.Break();
						pc.Expecting(']');
						pc.Advance();
						next = new RegexCharsetExpression(ranges, not);
						next.SetLocation(line, column, position);
						next = _ParseModifier(next, pc);
						
						if (null == result)
							result = next;
						else
						{
							result = new RegexConcatExpression(result, next);
							result.SetLocation(pc.Line, pc.Column, pc.Position);
						}
						line = pc.Line;
						column = pc.Column;
						position = pc.Position;
						break;
					default:
						ich = pc.Current;
						next = new RegexLiteralExpression((char)ich);
						next.SetLocation(line, column, position);
						pc.Advance();
						next = _ParseModifier(next, pc);
						if (null == result)
							result = next;
						else
						{
							result = new RegexConcatExpression(result, next);
							result.SetLocation(line, column, position);
						}
						line = pc.Line;
						column = pc.Column;
						position = pc.Position;
						break;
				}
			}
		}
		static IList<RegexCharsetEntry> _ParseRanges(ParseContext pc)
		{
			pc.EnsureStarted();
			var result = new List<RegexCharsetEntry>();
			RegexCharsetEntry next = null;
			bool readDash = false;
			while (-1 != pc.Current && ']' != pc.Current)
			{
				switch (pc.Current)
				{
					case '[': // char class 
						if (null != next)
						{
							result.Add(next);
							if (readDash)
								result.Add(new RegexCharsetCharEntry('-'));
							result.Add(new RegexCharsetCharEntry('-'));
						}
						pc.Advance();
						pc.Expecting(':');
						pc.Advance();
						var l = pc.CaptureBuffer.Length;
						pc.TryReadUntil(':', false);
						var n = pc.GetCapture(l);
						pc.Advance();
						pc.Expecting(']');
						pc.Advance();
						result.Add(new RegexCharsetClassEntry(n));
						readDash = false;
						next = null;
						break;
					case '\\':
						pc.Advance();
						pc.Expecting();
						switch(pc.Current)
						{
							case 'h':
								_ParseCharClassEscape(pc, "space", result, ref next, ref readDash);
								break;
							case 'd':
								_ParseCharClassEscape(pc, "digit", result, ref next, ref readDash);
								break;
							case 'D':
								_ParseCharClassEscape(pc, "^digit", result, ref next, ref readDash);
								break;
							case 'l':
								_ParseCharClassEscape(pc,"lower", result, ref next, ref readDash);
								break;
							case 's':
								_ParseCharClassEscape(pc, "space", result, ref next, ref readDash);
								break;
							case 'S':
								_ParseCharClassEscape(pc, "^space", result, ref next, ref readDash);
								break;
							case 'u':
								_ParseCharClassEscape(pc, "upper", result, ref next, ref readDash);
								break;
							case 'w':
								_ParseCharClassEscape(pc, "word", result, ref next, ref readDash);
								break;
							case 'W':
								_ParseCharClassEscape(pc, "^word", result, ref next, ref readDash);
								break;
							default:
								var ch = (char)_ParseRangeEscapePart(pc);
								if (null == next)
									next = new RegexCharsetCharEntry(ch);
								else if (readDash)
								{
									result.Add(new RegexCharsetRangeEntry(((RegexCharsetCharEntry)next).Value, ch));
									next = null;
									readDash = false;
								}
								else
								{
									result.Add(next);
									next = new RegexCharsetCharEntry(ch);
								}
								
								break;
						}
						
						break;
					case '-':
						pc.Advance();
						if (null == next)
						{
							next = new RegexCharsetCharEntry('-');
							readDash = false;
						}
						else
						{
							if (readDash)
								result.Add(next);
							readDash = true;
						}
						break;
					default:
						if (null == next)
						{
							next = new RegexCharsetCharEntry((char)pc.Current);
						}
						else
						{
							if (readDash)
							{
								result.Add(new RegexCharsetRangeEntry(((RegexCharsetCharEntry)next).Value, (char)pc.Current));
								next = null;
								readDash = false;
							}
							else
							{
								result.Add(next);
								next = new RegexCharsetCharEntry((char)pc.Current);
							}
						}
						pc.Advance();
						break;
				}
			}
			if(null!=next)
			{
				result.Add(next);
				if(readDash)
				{
					next = new RegexCharsetCharEntry('-');
					result.Add(next);
				}
			}
			return result;
		}

		static void _ParseCharClassEscape(ParseContext pc, string cls, List<RegexCharsetEntry> result, ref RegexCharsetEntry next, ref bool readDash)
		{
			if (null != next)
			{
				result.Add(next);
				if (readDash)
					result.Add(new RegexCharsetCharEntry('-'));
				result.Add(new RegexCharsetCharEntry('-'));
			}
			pc.Advance();
			result.Add(new RegexCharsetClassEntry(cls));
			next = null;
			readDash = false;
		}

		static RegexExpression _ParseModifier(RegexExpression expr,ParseContext pc)
		{
			var line = pc.Line;
			var column = pc.Column;
			var position = pc.Position;
			switch (pc.Current)
			{
				case '*':
					expr = new RegexRepeatExpression(expr);
					expr.SetLocation(line, column, position);
					pc.Advance();
					break;
				case '+':
					expr = new RegexRepeatExpression(expr, 1);
					expr.SetLocation(line, column, position);
					pc.Advance();
					break;
				case '?':
					expr = new RegexOptionalExpression(expr);
					expr.SetLocation(line, column, position);
					pc.Advance();
					break;
				case '{':
					pc.Advance();
					pc.TrySkipWhiteSpace();
					pc.Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', ',','}');
					var min = -1;
					var max = -1;
					if(','!=pc.Current && '}'!=pc.Current)
					{
						var l = pc.CaptureBuffer.Length;
						pc.TryReadDigits();
						min = int.Parse(pc.GetCapture(l));
						pc.TrySkipWhiteSpace();
					}
					if (','==pc.Current)
					{
						pc.Advance();
						pc.TrySkipWhiteSpace();
						pc.Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '}');
						if('}'!=pc.Current)
						{
							var l = pc.CaptureBuffer.Length;
							pc.TryReadDigits();
							max = int.Parse(pc.GetCapture(l));
							pc.TrySkipWhiteSpace();
						}
					} else { max = min; }
					pc.Expecting('}');
					pc.Advance();
					expr = new RegexRepeatExpression(expr, min, max);
					expr.SetLocation(line, column, position);
					break;
			}
			return expr;
		}
		/// <summary>
		/// Appends a character escape to the specified <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="ch">The character to escape</param>
		/// <param name="builder">The string builder to append to</param>
		internal static void AppendEscapedChar(char ch,StringBuilder builder)
		{
			switch (ch)
			{
				case '.':
				case '/': // js expects this
				case '(':
				case ')':
				case '[':
				case ']':
				case '<': // flex expects this
				case '>':
				case '|':
				case ';': // flex expects this
				case '\'': // pck expects this
				case '\"':
				case '{':
				case '}':
				case '?':
				case '*':
				case '+':
				case '$':
				case '^':
				case '\\':
					builder.Append('\\');
					builder.Append(ch);
					return;
				case '\t':
					builder.Append("\\t");
					return;
				case '\n':
					builder.Append("\\n");
					return;
				case '\r':
					builder.Append("\\r");
					return;
				case '\0':
					builder.Append("\\0");
					return;
				case '\f':
					builder.Append("\\f");
					return;
				case '\v':
					builder.Append("\\v");
					return;
				case '\b':
					builder.Append("\\b");
					return;
				
				default:
					if (!char.IsLetterOrDigit(ch) && !char.IsSeparator(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
					{

						builder.Append("\\u");
						builder.Append(unchecked((ushort)ch).ToString("x4"));

					}
					else
						builder.Append(ch);
					break;
			}

		}
		/// <summary>
		/// Escapes the specified character
		/// </summary>
		/// <param name="ch">The character to escape</param>
		/// <returns>A string representing the escaped character</returns>
		internal static string EscapeChar(char ch)
		{
			switch (ch)
			{
				case '.':
				case '/': // js expects this
				case '(':
				case ')':
				case '[':
				case ']':
				case '<': // flex expects this
				case '>':
				case '|':
				case ';': // flex expects this
				case '\'': // pck expects this
				case '\"':
				case '{':
				case '}':
				case '?':
				case '*':
				case '+':
				case '$':
				case '^':
				case '\\':
					return string.Concat("\\", ch.ToString());
				case '\t':
					return "\\t";
				case '\n':
					return "\\n";
				case '\r':
					return "\\r";
				case '\0':
					return "\\0";
				case '\f':
					return "\\f";
				case '\v':
					return "\\v";
				case '\b':
					return "\\b";
				default:
					if (!char.IsLetterOrDigit(ch) && !char.IsSeparator(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
					{

						return string.Concat("\\x",unchecked((ushort)ch).ToString("x4"));

					}
					else
						return string.Concat(ch);
			}
		}
		/// <summary>
		/// Appends an escaped range character to the specified <see cref="StringBuilder"/>
		/// </summary>
		/// <param name="rangeChar">The range character to escape</param>
		/// <param name="builder">The string builder to append to</param>
		internal static void AppendEscapedRangeChar(char rangeChar,StringBuilder builder)
		{
			switch (rangeChar)
			{
				case '.':
				case '/': // js expects this
				case '(':
				case ')':
				case '[':
				case ']':
				case '<': // flex expects this
				case '>':
				case '|':
				case ':': // expected by posix (sort of, Posix doesn't allow escapes in ranges, but standard extensions do)
				case ';': // flex expects this
				case '\'': // pck expects this
				case '\"':
				case '{':
				case '}':
				case '?':
				case '*':
				case '+':
				case '$':
				case '^':
				case '-':
				case '\\':
					builder.Append('\\');
					builder.Append(rangeChar);
					return;
				case '\t':
					builder.Append("\\t");
					return;
				case '\n':
					builder.Append("\\n");
					return;
				case '\r':
					builder.Append("\\r");
					return;
				case '\0':
					builder.Append("\\0");
					return;
				case '\f':
					builder.Append("\\f");
					return;
				case '\v':
					builder.Append("\\v");
					return;
				case '\b':
					builder.Append("\\b");
					return;
				default:
					if (!char.IsLetterOrDigit(rangeChar) && !char.IsSeparator(rangeChar) && !char.IsPunctuation(rangeChar) && !char.IsSymbol(rangeChar))
					{

						builder.Append("\\u");
						builder.Append(unchecked((ushort)rangeChar).ToString("x4"));

					}
					else
						builder.Append(rangeChar);
					break;
			}
		}
		/// <summary>
		/// Escapes a range character
		/// </summary>
		/// <param name="ch">The character to escape</param>
		/// <returns>A string containing the escaped character</returns>
		internal static string EscapeRangeChar(char ch)
		{
			switch (ch)
			{
				case '.':
				case '/': // js expects this
				case '(':
				case ')':
				case '[':
				case ']':
				case '<': // flex expects this
				case '>':
				case '|':
				case ':': // expected by posix (sort of, Posix doesn't allow escapes in ranges, but standard extensions do)
				case ';': // flex expects this
				case '\'': // pck expects this
				case '\"':
				case '{':
				case '}':
				case '?':
				case '*':
				case '+':
				case '$':
				case '^':
				case '-':
				case '\\':
					return string.Concat("\\", ch.ToString());
				case '\t':
					return "\\t";
				case '\n':
					return "\\n";
				case '\r':
					return "\\r";
				case '\0':
					return "\\0";
				case '\f':
					return "\\f";
				case '\v':
					return "\\v";
				case '\b':
					return "\\b";
				default:
					if (!char.IsLetterOrDigit(ch) && !char.IsSeparator(ch) && !char.IsPunctuation(ch) && !char.IsSymbol(ch))
					{

						return string.Concat("\\x", unchecked((ushort)ch).ToString("x4"));

					}
					else
						return string.Concat(ch);
			}
		}
		static byte _FromHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new ArgumentException("The value was not hex.", "hex");
		}
		static bool _IsHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return true;
			if ('G' > hex && '@' < hex)
				return true;
			if ('g' > hex && '`' < hex)
				return true;
			return false;
		}
		// return type is either char or ranges. this is kind of a union return type.
		static int _ParseEscapePart(ParseContext pc)
		{
			if (-1 == pc.Current) return -1;
			switch (pc.Current)
			{
				case 'f':
					pc.Advance();
					return '\f';
				case 'v':
					pc.Advance();
					return '\v';
				case 't':
					pc.Advance();
					return '\t';
				case 'n':
					pc.Advance();
					return '\n';
				case 'r':
					pc.Advance();
					return '\r';
				case 'x':
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return 'x';
					byte b = _FromHexChar((char)pc.Current);
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return unchecked((char)b);
					b <<= 4;
					b |= _FromHexChar((char)pc.Current);
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return unchecked((char)b);
					b <<= 4;
					b |= _FromHexChar((char)pc.Current);
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return unchecked((char)b);
					b <<= 4;
					b |= _FromHexChar((char)pc.Current);
					return unchecked((char)b);
				case 'u':
					if (-1 == pc.Advance())
						return 'u';
					ushort u = _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					return unchecked((char)u);
				default:
					int i = pc.Current;
					pc.Advance();
					return (char)i;
			}
		}
		static int _ParseRangeEscapePart(ParseContext pc)
		{
			if (-1== pc.Current)
				return -1;
			switch (pc.Current)
			{
				case 'f':
					pc.Advance();
					return '\f';
				case 'v':
					pc.Advance();
					return '\v';
				case 't':
					pc.Advance();
					return '\t';
				case 'n':
					pc.Advance();
					return '\n';
				case 'r':
					pc.Advance();
					return '\r';
				case 'x':
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return 'x';
					byte b = _FromHexChar((char)pc.Current);
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return unchecked((char)b);
					b <<= 4;
					b |= _FromHexChar((char)pc.Current);
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return unchecked((char)b);
					b <<= 4;
					b |= _FromHexChar((char)pc.Current);
					if (-1 == pc.Advance() || !_IsHexChar((char)pc.Current))
						return unchecked((char)b);
					b <<= 4;
					b |= _FromHexChar((char)pc.Current);
					return unchecked((char)b);
				case 'u':
					if (-1 == pc.Advance())
						return 'u';
					ushort u = _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					u <<= 4;
					if (-1 == pc.Advance())
						return unchecked((char)u);
					u |= _FromHexChar((char)pc.Current);
					return unchecked((char)u);
				default:
					int i = pc.Current;
					pc.Advance();
					return (char)i;
			}
		}
		static char _ReadRangeChar(IEnumerator<char> e)
		{
			char ch;
			if ('\\' != e.Current || !e.MoveNext())
			{
				return e.Current;
			}
			ch = e.Current;
			switch (ch)
			{
				case 't':
					ch = '\t';
					break;
				case 'n':
					ch = '\n';
					break;
				case 'r':
					ch = '\r';
					break;
				case '0':
					ch = '\0';
					break;
				case 'v':
					ch = '\v';
					break;
				case 'f':
					ch = '\f';
					break;
				case 'b':
					ch = '\b';
					break;
				case 'x':
					if (!e.MoveNext())
						throw new ExpectingException("Expecting input for escape \\x");
					ch = e.Current;
					byte x = _FromHexChar(ch);
					if (!e.MoveNext())
					{
						ch = unchecked((char)x);
						return ch;
					}
					x *= 0x10;
					x += _FromHexChar(e.Current);
					ch = unchecked((char)x);
					break;
				case 'u':
					if (!e.MoveNext())
						throw new ExpectingException("Expecting input for escape \\u");
					ch = e.Current;
					ushort u = _FromHexChar(ch);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					if (!e.MoveNext())
					{
						ch = unchecked((char)u);
						return ch;
					}
					u *= 0x10;
					u += _FromHexChar(e.Current);
					ch = unchecked((char)u);
					break;
				default: // return itself
					break;
			}
			return ch;
		}
		static IEnumerable<CharRange> _ParseRanges(IEnumerable<char> charRanges)
		{
			using (var e = charRanges.GetEnumerator())
			{
				var skipRead = false;

				while (skipRead || e.MoveNext())
				{
					skipRead = false;
					char first = _ReadRangeChar(e);
					if (e.MoveNext())
					{
						if ('-' == e.Current)
						{
							if (e.MoveNext())
								yield return new CharRange(first, _ReadRangeChar(e));
							else
								yield return new CharRange('-', '-');
						}
						else
						{
							yield return new CharRange(first, first);
							skipRead = true;
							continue;

						}
					}
					else
					{
						yield return new CharRange(first, first);
						yield break;
					}
				}
			}
			yield break;
		}
		static IEnumerable<CharRange> _ParseRanges(IEnumerable<char> charRanges, bool normalize)
		{
			if (!normalize)
				return _ParseRanges(charRanges);
			else
			{
				var result = new List<CharRange>(_ParseRanges(charRanges));
				CharRange.NormalizeRangeList(result);
				return result;
			}
		}
	}
}
