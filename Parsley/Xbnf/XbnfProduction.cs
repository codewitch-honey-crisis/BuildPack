using System;
using System.Collections.Generic;
using System.Text;

namespace Parsley
{
	public class XbnfProduction : XbnfNode, IEquatable<XbnfProduction>, ICloneable
	{
		public XbnfProduction(string name,XbnfExpression expression=null)
		{
			Name = name;
			Expression = expression;
		}
		public bool IsTerminal {
			get {
				if (null != Expression && Expression.IsTerminal)
					return true;
				var i = Attributes.IndexOf("terminal");
				if(-1<i && Attributes[i].Value is bool && (bool)Attributes[i].Value)
					return true;
				return false;
			}
		}
		public bool IsHidden {
			get {
				if(!IsTerminal)
					return false;
				var i = Attributes.IndexOf("hidden");
				if (-1 < i && Attributes[i].Value is bool && (bool)Attributes[i].Value)
					return true;
				return false;
			}
		}
		public bool IsCollapsed {
			get {
				var i = Attributes.IndexOf("collapsed");
				if (-1 < i && Attributes[i].Value is bool && (bool)Attributes[i].Value)
					return true;
				return false;
			}
		}
		public XbnfProduction() { }
		public XbnfAttributeList Attributes { get; } = new XbnfAttributeList();
		public string Name { get; set; } = null;
		public XbnfExpression Expression { get; set; } = null;
		public string Code { get; set; } = null;
		public XbnfProduction Clone()
		{
			var result = new XbnfProduction();
			result.Name = Name;
			for(int ic=Attributes.Count,i=0;i<ic;++i)
				result.Attributes.Add(Attributes[i].Clone());
			if(null!=Expression)
				result.Expression = Expression.Clone();
			result.Code = Code;
			return result;
		}
		object ICloneable.Clone()
			=> Clone();
		public override string ToString()
		{
			return ToString(null);
		}
		public string ToString(string fmt)
		{
			var sb = new StringBuilder();
			if ("xc" == fmt)
			{
				if (IsHidden)
					sb.Append(string.Concat("(", Name, ")"));
				else if (IsCollapsed)
					sb.Append(string.Concat("{", Name, "}"));
				else
					sb.Append(Name);
			}
			else
				sb.Append(Name);
			var ic = Attributes.Count;
			if(0<ic && "p"!=fmt && "xc"!=fmt)
			{
				sb.Append("<");
				var delim = "";
				for (var i = 0;i<ic;++i)
				{
					sb.Append(delim);
					sb.Append(Attributes[i]);
					delim = ", ";
				}
				sb.Append(">");
			}
			if (null != Expression) {
				sb.Append("= ");
				sb.Append(Expression);
			}
			if (null != Code && ("pc"==fmt || string.IsNullOrEmpty(fmt)))
			{
				sb.Append(" => {");
				sb.Append(Code);
				sb.Append("}");
			}
			else if("p"!=fmt)
				sb.Append(";");
			return sb.ToString();
		}
		internal static XbnfProduction Parse(ParseContext pc)
		{
			var result = new XbnfProduction();
			pc.TrySkipCCommentsAndWhiteSpace();
			var l = pc.Line;
			var c = pc.Column;
			var p = pc.Position;
			// read identifier
			result.Name=ParseIdentifier(pc);
			// read attributes
			if ('<'==pc.Current)
			{
				pc.Advance();
				while (-1 != pc.Current && '>' != pc.Current)
				{
					result.Attributes.Add(XbnfAttribute.Parse(pc));
					pc.TrySkipCCommentsAndWhiteSpace();
					pc.Expecting('>', ',');
					if (',' == pc.Current)
						pc.Advance();
				}
				pc.Expecting('>');
				pc.Advance();
			}
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting(';', '=');
			if ('='==pc.Current)
			{
				pc.Advance();
				result.Expression = XbnfExpression.Parse(pc);
			}
			pc.Expecting(';','=');
			result.SetLocation(l, c, p);
			if (';' == pc.Current)
			{
				pc.Advance();
				return result;
			}
			pc.Advance();
			pc.Expecting('>');
			pc.Advance();
			pc.TrySkipCCommentsAndWhiteSpace();
			pc.Expecting('{');
			pc.Advance();
			var sb = new StringBuilder();
			var i = 1;
			var skipRead = false;
			while(skipRead || -1!=pc.Advance())
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
				} else
					sb.Append((char)pc.Current);
				pc.ClearCapture();
				if (pc.TryReadCCommentsAndWhitespace())
					skipRead = true;
				sb.Append(pc.GetCapture());
				
			}
			pc.Expecting('}');
			pc.Advance();
			pc.TrySkipCCommentsAndWhiteSpace();
			if (';' == pc.Current)
				pc.Advance();
			result.Code = sb.ToString();
			return result;
		}
		#region Value semantics
		public bool Equals(XbnfProduction rhs)
		{
			if (ReferenceEquals(rhs, this)) return true;
			if (ReferenceEquals(rhs, null)) return false;
			if(Name == rhs.Name)
			{
				if (Code == rhs.Code)
				{
					if (Expression == rhs.Expression)
					{
						for (int ic = Attributes.Count, i = 0; i < ic; ++i)
							if (!rhs.Attributes.Contains(Attributes[i]))
								return false;
						return true;
					}
				}
			}
			return false;
		}
		public override bool Equals(object rhs)
			=> Equals(rhs as XbnfProduction);

		public override int GetHashCode()
		{
			var result = 0;
			for(int ic=Attributes.Count,i=0;i<ic;++i)
				result ^= Attributes[i].GetHashCode();
			
			if (null != Name)
				result ^=Name.GetHashCode();
			if (null != Code)
				result ^= Code.GetHashCode();
			if (null != Expression)
				result ^= Expression.GetHashCode();

			return result;
		}
		public static bool operator ==(XbnfProduction lhs, XbnfProduction rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return true;
			if (ReferenceEquals(lhs, null)) return false;
			return lhs.Equals(rhs);
		}
		public static bool operator !=(XbnfProduction lhs, XbnfProduction rhs)
		{
			if (ReferenceEquals(lhs, rhs)) return false;
			if (ReferenceEquals(lhs, null)) return true;
			return !lhs.Equals(rhs);
		}
		#endregion

	}
}
