using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Parsley
{
	public class XbnfDocument : IEquatable<XbnfDocument>, ICloneable
	{
		string _filename;
		public string Filename { get { return _filename; } }
		public void SetFilename(string filename) { _filename = filename; }
		public XbnfImportList Imports { get; } = new XbnfImportList();
		public XbnfProduction StartProduction {
			get {
				var ic = Productions.Count;
				var firstNT = -1;
				for (var i = 0;i<ic;++i)
				{
					var prod = Productions[i];
					var hi = prod.Attributes.IndexOf("start");
					if(-1<hi)
					{
						var o = prod.Attributes[hi].Value;
						if (o is bool && (bool)o)
							return prod;
					}
					if (-1 == firstNT && !prod.IsTerminal)
						firstNT = i;
				}
				if (-1!=firstNT)
					return Productions[firstNT];
				return null;
			}
			set {
				if (null!=value && !Productions.Contains(value))
					throw new InvalidOperationException(string.Concat("The production \"",value.Name,"\" is not the grammar."));
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
				{
					if (null != value && Productions[i] == value)
					{
						var prod = Productions[i];
						prod.Attributes.Remove("start");
						prod.Attributes.Add(new XbnfAttribute("start", true));
					}
					else
					{
						var prod = Productions[i];
						var hi = prod.Attributes.IndexOf("start");
						if (-1 < hi)
							prod.Attributes.RemoveAt(hi);
					}
				}
			}
		}
		public static string[] GetResources(string fileOrUrl)
		{
			if (!fileOrUrl.Contains("://") && !Path.IsPathRooted(fileOrUrl))
				fileOrUrl = Path.GetFullPath(fileOrUrl);
			return _GatherImports(fileOrUrl);
		}
		static string[] _GatherImports(string res)
		{
			var result = new List<string>();
			var imps = new List<string>();
			result.Add(res);
			if (res.Contains("://"))
				using (var pc = ParseContext.CreateFromUrl(res))
					_ParseImports(pc, result);
			else
			{
				using (var pc = ParseContext.CreateFrom(res))
				{
					_ParseImports(pc, result);
					
				}
			}
			for(var i = 1;i<result.Count;++i)
			{
				var s = result[i];
				if(!s.Contains("://"))
				{
					if(!Path.IsPathRooted(s))
					{
						s=Path.Combine(Path.GetDirectoryName(res), s);
					} 
				}
				var gi = _GatherImports(s);
				for (var j = 0; j < gi.Length; j++)
					if (!result.Contains(gi[j]))
						result.Add(gi[j]);
			}
			for(int ic = result.Count,i=0;i<ic;++i)
			{
				if(!Path.IsPathRooted(result[i]))
				{
					result.RemoveAt(i);
					--i;
					--ic;
				}
			}
			return result.ToArray();
		}
		static void _ParseImports(ParseContext pc,IList<string> result)
		{
			pc.TrySkipCCommentsAndWhiteSpace();
			while('@'==pc.Current)
			{
				pc.Advance();
				var s = XbnfNode.ParseIdentifier(pc);
				if("import"==s)
				{
					pc.TrySkipCCommentsAndWhiteSpace();
					var lit = XbnfExpression.Parse(pc) as XbnfLiteralExpression;
					if (!result.Contains(lit.Value))
						result.Add(lit.Value);
					pc.TryReadCCommentsAndWhitespace();
					pc.Advance();
					pc.TryReadCCommentsAndWhitespace();
				}
				else
				{
					while (-1 != pc.Current && ';' != pc.Current) pc.Advance();
					if (';' == pc.Current)
						pc.Advance();
					pc.TrySkipCCommentsAndWhiteSpace();
				}
			}
		}
		public IList<XbnfCode> Code { get; } = new List<XbnfCode>();

		public bool HasNonTerminalProductions {
			get {
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					if (!Productions[i].IsTerminal)
						return true;
				return false;
			}
		}
		static XbnfExpression _FindNonTerminal(XbnfExpression expr)
		{
			var re = expr as XbnfRefExpression;
			if(null!=re)
				return re;
			var bo = expr as XbnfBinaryExpression;
			if(bo!=null)
			{
				var res = _FindNonTerminal(bo.Left);
				if (null != res)
					return res;
				res = _FindNonTerminal(bo.Right);
				if (null != res)
					return res;
			}
			var ue = expr as XbnfUnaryExpression;
			if(null!=ue)
				return _FindNonTerminal(ue.Expression);
			return null;
		}
		public XbnfProductionList Productions { get; } = new XbnfProductionList();
		public IList<XbnfMessage> TryValidate(IList<XbnfMessage> result = null)
		{
			if (null == result)
				result = new List<XbnfMessage>();
			var refCounts = new Dictionary<string, int>(EqualityComparer<string>.Default);

			foreach (var prod in Productions)
			{
				if (refCounts.ContainsKey(prod.Name))
					result.Add(new XbnfMessage(ErrorLevel.Error, -1, string.Concat("The production \"", prod.Name, "\" was specified more than once."), prod.Line, prod.Column, prod.Position, Filename));
				else
					refCounts.Add(prod.Name, 0);
			}
			foreach (var prod in Productions)
			{
				_ValidateExpression(prod.Expression, refCounts, result);
			}
			foreach (var rc in refCounts)
			{
				if (0 == rc.Value)
				{
					var prod = Productions[rc.Key];
					object o;
					var i = prod.Attributes.IndexOf("hidden");
					var isHidden = false;
					if (-1<i)
					{
						o = prod.Attributes[i].Value;
						isHidden = (o is bool && (bool)o);
					}
					var sp = StartProduction;
					if(null!=sp)
						if (!isHidden && !Equals(rc.Key, sp.Name))
							result.Add(new XbnfMessage(ErrorLevel.Warning, -1, string.Concat("Unreferenced production \"", prod.Name, "\""),
								prod.Line, prod.Column, prod.Position, Filename));
				}
			}
			return result;
		}
		public XbnfDocument Clone()
		{
			var result = new XbnfDocument();
			for(int ic=Productions.Count,i=0;i<ic;++i)
				result.Productions.Add(Productions[i].Clone());
			return result;
		}
		object ICloneable.Clone()
			=> Clone();
		public string ToString(string fmt)
		{
			var sb = new StringBuilder();
			if ("gnc" == fmt)
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Productions[i].ToString("pnc"));
			else if ("xc" == fmt)
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Productions[i].ToString("xc"));
			else
				for (int ic = Productions.Count, i = 0; i < ic; ++i)
					sb.AppendLine(Productions[i].ToString());
			return sb.ToString();
		}
		public override string ToString()
		{
			return ToString(null);
		}

		internal static XbnfDocument Parse(ParseContext pc)
		{
			var result = new XbnfDocument();
			if(!string.IsNullOrEmpty(pc.Source))
				result.SetFilename(pc.Source);
			while (-1 != pc.Current && '}'!=pc.Current)
			{
				pc.TrySkipCCommentsAndWhiteSpace();
				while ('@' == pc.Current) // imports
				{
					result.Imports.Add(_ParseImport(result, pc));

					pc.TrySkipCCommentsAndWhiteSpace();
				}
				if (pc.Current == '{')
				{
					pc.Advance();
					var l = pc.Line;
					var c = pc.Column;
					var p = pc.Position;
					var s = ReadCode(pc);
					pc.Expecting('}');
					pc.Advance();
					var code = new XbnfCode(s);
					code.SetLocation(l, c, p);
					result.Code.Add(code);
				}
				else if (-1 != pc.Current)
				{
					if ('@' == pc.Current)
					{
						var ee = new ExpectingException(string.Format("Expecting productions. Imports must be specified before any productions at line {0}, column {1}, position {2} in {3}.", pc.Line, pc.Column, pc.Position, !string.IsNullOrEmpty(pc.Source) ? pc.Source : "in-memory document"));
						ee.Line = pc.Line;
						ee.Column = pc.Column;
						ee.Position = pc.Position;
						ee.Expecting = new string[] { "Production" };
						throw ee;
					}
					result.Productions.Add(XbnfProduction.Parse(pc));
				}
				else // end of input
					return result;
				// have to do this so trailing whitespace
				// doesn't get read as a production
				pc.TryReadCCommentsAndWhitespace();
			} 
			return result;
		}
		public static XbnfDocument Parse(IEnumerable<char> @string)
			=> Parse(ParseContext.Create(@string));
		public static XbnfDocument ReadFrom(TextReader reader)
			=> Parse(ParseContext.CreateFrom(reader));
		public static XbnfDocument ReadFrom(string file)
		{
			using (var pc = ParseContext.CreateFrom(file))
			{
				var result = Parse(pc);
				result._filename = Path.GetFullPath(file);
				return result;
			}
		}
		public static XbnfDocument ReadFromUrl(string url)
		{
			using (var pc = ParseContext.CreateFromUrl(url))
				return Parse(pc);
		}
		static internal string ReadCode(ParseContext pc)
		{
			var sb = new StringBuilder();
			var i = 1;
			var skipRead = true;
			while (skipRead || -1 != pc.Advance())
			{
				skipRead = false;
				if ('{' == pc.Current)
				{
					sb.Append((char)pc.Current);
					++i;
				}
				else if ('}' == pc.Current)
				{
					--i;
					if (0 == i)
						break;
					sb.Append((char)pc.Current);
				}
				else if ('\"' == pc.Current)
				{
					pc.ClearCapture();
					pc.TryReadCString();
					sb.Append(pc.GetCapture());
					skipRead = true;
				}
				else
					sb.Append((char)pc.Current);
				pc.ClearCapture();
				if (pc.TryReadCCommentsAndWhitespace())
					skipRead = true;
				sb.Append(pc.GetCapture());

			}

			return sb.ToString();
		}
		#region Value semantics
		public bool Equals(XbnfDocument rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			var lc = Productions.Count;
			var rc = rhs.Productions.Count;
			if (lc != rc) return false;
			for (var i=0;i<lc;++i)
				if (Productions[i] != rhs.Productions[i])
					return false;
			return true;
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as XbnfDocument);

		public override int GetHashCode()
		{
			var result = 0;
			for(int ic=Productions.Count,i=0;i<ic;++i)
				if (null != Productions[i])
					result ^=Productions[i].GetHashCode();
			
			return result;
		}
		public static bool operator==(XbnfDocument lhs, XbnfDocument rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(XbnfDocument lhs, XbnfDocument rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion
		void _ValidateExpression(XbnfExpression expr, IDictionary<string, int> refCounts, IList<XbnfMessage> messages)
		{
			var l = expr as XbnfLiteralExpression;
			if (null != l)
			{

				string id = null;
				for(int ic = Productions.Count,i=0;i<ic;++i)
				{
					var ll = Productions[i].Expression as XbnfLiteralExpression;
					if(ll==l)
					{
						id = Productions[i].Name;
						break;
					}
				}
				// don't count itself. only things just like itself
				if (!string.IsNullOrEmpty(id) && !ReferenceEquals(Productions[id].Expression, l))
					refCounts[id] += 1;
			}
			
			var r = expr as XbnfRefExpression;
			if (null != r)
			{
				int rc;
				if (null == r.Symbol)
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Error, -1,
							"Null reference expression",
							expr.Line, expr.Column, expr.Position, Filename));
					return;
				}
				if (!refCounts.TryGetValue(r.Symbol, out rc))
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Error, -1,
							string.Concat(
								"Reference to undefined symbol \"",
								r.Symbol,
								"\""),
							expr.Line, expr.Column, expr.Position, Filename));
					return;
				}
				refCounts[r.Symbol] = rc + 1;
				return;
			}
			var b = expr as XbnfBinaryExpression;
			if (null != b)
			{
				if (null == b.Left && null == b.Right)
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Warning, -1,
								"Nil expression",
							expr.Line, expr.Column, expr.Position, Filename));
					return;
				}
				_ValidateExpression(b.Left, refCounts, messages);
				_ValidateExpression(b.Right, refCounts, messages);
				return;
			}
			var u = expr as XbnfUnaryExpression;
			if (null != u)
			{
				if (null == u.Expression)
				{
					messages.Add(
						new XbnfMessage(
							ErrorLevel.Warning, -1,
								"Nil expression",
							expr.Line, expr.Column, expr.Position, Filename));
					return;
				}
				_ValidateExpression(u.Expression, refCounts, messages);
			}
		}
		public XbnfProduction GetProductionForExpression(XbnfExpression expr)
		{
			for (int ic = Productions.Count, i = 0; i < ic; ++i)
			{
				var prod = Productions[i];
				if (Equals(expr , prod.Expression))
					return prod;
			}
			return null;
		}
		static XbnfImport _ParseImport(XbnfDocument doc, ParseContext pc)
		{
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting('@');
			pc.Advance();
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			var str = XbnfNode.ParseIdentifier(pc);
			if (0 != string.Compare("import", str, StringComparison.InvariantCulture))
			{
				var ee = new ExpectingException(string.Format("Expecting \"import\" at line {0}, column {1}, position {2}", l, c, p));
				ee.Expecting = new string[] { "import" };
				ee.Line = l;
				ee.Column = c;
				ee.Position = p;
			}
			pc.TrySkipCCommentsAndWhiteSpace();
			l = pc.Line;
			c = pc.Column;
			p = pc.Position;
			pc.Expecting('\"');
			// borrow the parsing from XbnfExpression for this.
			var le = XbnfExpression.Parse(pc) as XbnfLiteralExpression;
			if (null == le)
			{
				var ee = new ExpectingException(string.Format("Expecting string literal import argument at line {0}, column {1}, position {2}", l, c, p));
				ee.Expecting = new string[] { "string literal" };
				ee.Line = l;
				ee.Column = c;
				ee.Position = p;
			}
			var res = le.Value;
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting(';');
			pc.Advance();
			var cmp = res.ToLowerInvariant();
			var result = new XbnfImport();
			if (-1 < cmp.IndexOf("://"))
				result.Document = XbnfDocument.ReadFromUrl(cmp);
			else
			{
				string mdir = null;
				if (null != doc && !string.IsNullOrEmpty(doc.Filename))
				{
					mdir = doc.Filename;
					if (!Path.IsPathRooted(mdir))
						mdir = Path.GetFullPath(mdir);
					mdir = Path.GetDirectoryName(mdir);
				}
				var path = res;
				if (!Path.IsPathRooted(path))
				{
					if (null != mdir)
						path = Path.Combine(mdir, path);
					else
						path = Path.GetFullPath(path);
				}
				result.Document = XbnfDocument.ReadFrom(path);
			}
			result.SetLocation(l, c, p);
			return result;
		}
	}
}
