using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parsley
{
	// port of PCK's XbnkToPckTransform for use with Rolex and Parsley
	// the rolex spec is written to rolexOutput
	// the CfgDocument is the return value
	static class XbnfConvert
	{
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
				var prod = document.Productions[te.Value];
				sb.Append(te.Value);
				sb.Append("<id=");
				sb.Append(cfg.GetIdOfSymbol(te.Value));
				foreach (var attr in prod.Attributes)
				{
					sb.Append(", ");
					sb.Append(attr.ToString());
				}
				sb.Append(">");
				
				sb.AppendLine(string.Concat("= \'", _ToRegex(document, te.Key, true), "\'"));

			}
			return sb.ToString();
		}
		public static CfgDocument ToCfg(XbnfDocument document)
		{
			var syms = new HashSet<string>();
			var cfg = new CfgDocument();
			// gather the attributes and production names
			for (int ic = document.Productions.Count, i = 0; i < ic; ++i)
			{
				var p = document.Productions[i];
				syms.Add(p.Name);
				if (0 < p.Attributes.Count)
				{
					CfgAttributeList attrs;
					if (!cfg.AttributeSets.TryGetValue(p.Name, out attrs))
					{
						attrs = new CfgAttributeList();
						cfg.AttributeSets.Add(p.Name, attrs);
					}
					//writer.Write(string.Concat(p.Name, ": "));
					//var delim = "";
					for (int jc = p.Attributes.Count, j = 0; j < jc; ++j)
					{
						//writer.Write(string.Concat(delim, p.Attributes[j].ToString()));
						//delim = ", ";
						var attr = p.Attributes[j];
						attrs.Add(new CfgAttribute(attr.Name, attr.Value));
					}
					//writer.WriteLine();
					
				}
			}
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
			var ntd = new Dictionary<string, IList<IList<string>>>();
			// now we can use tmap and syms to help solve the rest of our productions
			for (int ic = document.Productions.Count, i = 0; i < ic; ++i)
			{
				var p = document.Productions[i];
				if (!p.IsTerminal)
				{
					var dys = _GetDysjunctions(document, syms, tmap, attrSets, rules, p, p.Expression);
					ntd.Add(p.Name, dys);
				}
			}
			// now that we've done that, build the rest of our attributes
			foreach (var sattrs in attrSets)
			{
				CfgAttributeList attrs;
				if(!cfg.AttributeSets.TryGetValue(sattrs.Key, out attrs))
				{
					attrs = new CfgAttributeList();
					cfg.AttributeSets.Add(sattrs.Key, attrs);
				}
				//writer.Write(string.Concat(sattrs.Key, ":"));
				//var delim = "";
				for (int jc = sattrs.Value.Count, j = 0; j < jc; ++j)
				{
					var attr = sattrs.Value[j];
					attrs.Add(new CfgAttribute(attr.Name, attr.Value));
				//	writer.Write(string.Concat(delim, sattrs.Value[j].ToString()));
				//	delim = ", ";
				}
				//writer.WriteLine();
			}
			// now write our main rules
			foreach (var nt in ntd)
			{
				foreach (var l in nt.Value)
				{
					cfg.Rules.Add(new CfgRule(nt.Key, l));
					//writer.Write(string.Concat(nt.Key, "->"));
					//foreach (var s in l)
					//	writer.Write(string.Concat(" ", s));
					//writer.WriteLine();
				}
			}
			// build our secondary rules
			foreach (var rule in rules)
			{
				
				cfg.Rules.Add(new CfgRule(rule.Key, rule.Value));
				//writer.Write(string.Concat(rule.Key, "->"));
				//foreach (var s in rule.Value)
				//	writer.Write(string.Concat(" ", s));
				//writer.WriteLine();
			}
			//writer.WriteLine();
			// write our terminals
			/*
			for (int ic = tmap.Count, i = 0; i < ic; ++i)
			{
				var te = tmap[i];
				var prod = document.Productions[te.Value];
				rolexOutput.Write(te.Value);
				if (0 < prod.Attributes.Count)
				{
					rolexOutput.Write("<");
					var delim = "";
					foreach (var attr in prod.Attributes)
					{
						rolexOutput.Write(delim);
						rolexOutput.Write(attr.ToString());
					}
					rolexOutput.Write(">");
				}
				rolexOutput.WriteLine(string.Concat("= \'", _ToRegex(document, te.Key,true), "\'"));

			}
			rolexOutput.Flush();
			*/
			return cfg;

		}
		static string _ToRegex(XbnfDocument d, XbnfExpression e,bool first)
		{
			var le = e as XbnfLiteralExpression;
			if (null != le)
				return _EscapeLiteral(XbnfNode.Escape(le.Value));
			var rxe = e as XbnfRegexExpression;
			if (null != rxe)
				return first?rxe.Value:string.Concat("(", rxe.Value, ")");
			var rfe = e as XbnfRefExpression;
			if (null != rfe)
				_ToRegex(d, d.Productions[rfe.Symbol].Expression,first);
			var re = e as XbnfRepeatExpression;
			if (null != re)
			{
				if (re.IsOptional)
					return string.Concat("(", _ToRegex(d, re.Expression,true), ")*");
				else
					return string.Concat("(", _ToRegex(d, re.Expression,true), ")+");
			}
			var oe = e as XbnfOrExpression;
			if (null != oe)
			{
				if (!first)
					return string.Concat("(", _ToRegex(d, oe.Left, false), "|", _ToRegex(d, oe.Right, false), ")");
				else
					return string.Concat(_ToRegex(d, oe.Left, false), "|", _ToRegex(d, oe.Right, false));
			}
			var oc = e as XbnfConcatExpression;
			if (null != oc)
				return string.Concat(_ToRegex(d, oe.Left,false), _ToRegex(d, oe.Right,false));
			var ope = e as XbnfOptionalExpression;
			if (null != ope)
				return string.Concat("(", _ToRegex(d, ope.Expression,true), ")?");
			return "";
		}
		static string _EscapeLiteral(string v)
		{
			var sb = new StringBuilder();
			for (var i = 0; i < v.Length; ++i)
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
						sid = string.Concat(sr.Symbol, "listtail");
				}
			}
			if (string.IsNullOrEmpty(sid))
				sid = "ImplicitList";
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
			var attrlist = new XbnfAttributeList();
			attrlist.Add(attr);
			attrs.Add(listId, attrlist);
			var expr =
				new XbnfOrExpression(
					new XbnfConcatExpression(
						new XbnfRefExpression(listId), re.Expression), re.Expression);
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
				terms.Add(l);
				return;
			}
			var r = expr as XbnfRegexExpression;
			if (null != r)
			{
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
	}
}