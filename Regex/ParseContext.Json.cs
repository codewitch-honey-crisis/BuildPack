using System;
using System.Collections.Generic;
using System.Text;

namespace RE
{
	partial class ParseContext
	{
		/// <summary>
		/// Attempts to read a JSON string into the capture buffer
		/// </summary>
		/// <returns>True if a valid string was read, otherwise false</returns>
		public bool TryReadJsonString()
		{
			EnsureStarted();
			if ('\"' != Current)
				return false;
			CaptureCurrent();
			while (-1 != Advance() && '\r' != Current && '\n' != Current && Current != '\"')
			{
				CaptureCurrent();
				if ('\\' == Current)
				{
					if (-1 == Advance() || '\r' == Current || '\n' == Current)
						return false;
					CaptureCurrent();

				}
			}
			if (Current == '\"')
			{
				CaptureCurrent();
				Advance(); // move past the string
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to skip a JSON string literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipJsonString()
		{
			EnsureStarted();
			if ('\"' != Current)
				return false;
			while (-1 != Advance() && '\r' != Current && '\n' != Current && Current != '\"')
				if ('\\' == Current)
					if (-1 == Advance() || '\r' == Current || '\n' == Current)
						return false;

			if (Current == '\"')
			{
				Advance(); // move past the string
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to read a JSON string literal into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The value the literal represents</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseJsonString(out string result)
		{
			result = null;
			var sb = new StringBuilder();
			EnsureStarted();
			if ('\"' != Current)
				return false;
			CaptureCurrent();
			while (-1 != Advance() && '\r' != Current && '\n' != Current && Current != '\"')
			{
				CaptureCurrent();
				if ('\\' == Current)
				{
					if (-1 == Advance() || '\r' == Current || '\n' == Current)
						return false;
					CaptureCurrent();
					switch (Current)
					{
						case 'f':
							sb.Append('\f');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 't':
							sb.Append('\t');
							break;
						case '\\':
							sb.Append('\\');
							break;
						case '/':
							sb.Append('/');
							break;
						case '\"':
							sb.Append('\"');
							break;
						case 'b':
							sb.Append('\b');
							break;
						case 'u':
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							int ch = 0;
							if (!IsHexChar((char)Current))
								return false;
							ch <<= 4;
							ch |= FromHexChar((char)Current);

							CaptureCurrent();
							if (-1 == Advance())
								return false;
							if (!IsHexChar((char)Current))
								return false;
							ch <<= 4;
							ch |= FromHexChar((char)Current);

							CaptureCurrent();
							if (-1 == Advance())
								return false;
							if (!IsHexChar((char)Current))
								return false;
							ch <<= 4;
							ch |= FromHexChar((char)Current);

							CaptureCurrent();
							if (-1 == Advance())
								return false;
							if (!IsHexChar((char)Current))
								return false;
							ch <<= 4;
							ch |= FromHexChar((char)Current);

							sb.Append((char)ch);
							break;
						default:
							return false;
					}
				}
				else
					sb.Append((char)Current);

			}
			if (Current == '\"')
			{
				CaptureCurrent();
				Advance(); // move past the string
				result = sb.ToString();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Reads a JSON string literal into the capture buffer while parsing it
		/// </summary>
		/// <returns>The value the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public string ParseJsonString()
		{
			var sb = new StringBuilder();
			EnsureStarted();
			Expecting('\"');
			while (-1 != Advance() && '\r' != Current && '\n' != Current && Current != '\"')
			{
				if ('\\' == Current)
				{
					Advance();
					switch (Current)
					{
						case 'b':
							sb.Append('\b');
							break;
						case 'f':
							sb.Append('\f');
							break;
						case 'n':
							sb.Append('\n');
							break;
						case 'r':
							sb.Append('\r');
							break;
						case 't':
							sb.Append('\t');
							break;
						case '\\':
							sb.Append('\\');
							break;
						case '\"':
							sb.Append('\"');
							break;
						case 'u':
							int ch = 0;
							Advance();
							Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f');
							ch <<= 4;
							ch |= FromHexChar((char)Current);
							Advance();
							Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f');
							ch <<= 4;
							ch |= FromHexChar((char)Current);
							Advance();
							Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f');
							ch <<= 4;
							ch |= FromHexChar((char)Current);
							Advance();
							Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f');
							ch <<= 4;
							ch |= FromHexChar((char)Current);
							sb.Append((char)ch);
							break;
						default:
							Expecting('b', 'n', 'r', 't', '\\', '/', '\"', 'u');
							break;
					}
				}
				else
					sb.Append((char)Current);
			}
			Expecting('\"');
			Advance();
			return sb.ToString();
		}
		/// <summary>
		/// Attempts to read a JSON value into the capture buffer
		/// </summary>
		/// <returns>True if a valid value was read, otherwise false</returns>
		public bool TryReadJsonValue()
		{
			TryReadWhiteSpace();
			if ('t' == Current)
			{
				CaptureCurrent();
				if (Advance() != 'r')
					return false;
				CaptureCurrent();
				if (Advance() != 'u')
					return false;
				CaptureCurrent();
				if (Advance() != 'e')
					return false;
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				return true;
			}
			if ('f' == Current)
			{
				CaptureCurrent();
				if (Advance() != 'a')
					return false;
				CaptureCurrent();
				if (Advance() != 'l')
					return false;
				CaptureCurrent();
				if (Advance() != 's')
					return false;
				CaptureCurrent();
				if (Advance() != 'e')
					return false;
				CaptureCurrent();
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				return true;
			}
			if ('n' == Current)
			{
				CaptureCurrent();
				if (Advance() != 'u')
					return false;
				CaptureCurrent();
				if (Advance() != 'l')
					return false;
				CaptureCurrent();
				if (Advance() != 'l')
					return false;
				CaptureCurrent();
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				return true;
			}
			if ('-' == Current || '.' == Current || char.IsDigit((char)Current))
				return TryReadReal();
			if ('\"' == Current)
				return TryReadJsonString();

			if ('[' == Current)
			{
				CaptureCurrent();
				Advance();
				if (TryReadJsonValue())
				{
					TryReadWhiteSpace();
					while (',' == Current)
					{
						CaptureCurrent();
						Advance();
						if (!TryReadJsonValue()) return false;
						TryReadWhiteSpace();
					}
				}
				TryReadWhiteSpace();
				if (']' != Current)
					return false;
				CaptureCurrent();
				Advance();
				return true;
			}
			if ('{' == Current)
			{
				CaptureCurrent();
				Advance();
				TryReadWhiteSpace();
				if (TryReadJsonString())
				{
					TryReadWhiteSpace();
					if (':' != Current) return false;
					CaptureCurrent();
					Advance();
					if (!TryReadJsonValue())
						return false;

					TryReadWhiteSpace();
					while (',' == Current)
					{
						CaptureCurrent();
						Advance();
						TryReadWhiteSpace();
						if (!TryReadJsonString())
							return false;
						TryReadWhiteSpace();
						if (':' != Current) return false;
						CaptureCurrent();
						Advance();
						if (!TryReadJsonValue())
							return false;
						TryReadWhiteSpace();
					}
				}
				TryReadWhiteSpace();
				if ('}' != Current)
					return false;
				CaptureCurrent();
				Advance();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to skip the a JSON value without capturing
		/// </summary>
		/// <returns>True if a value was found and skipped, otherwise false</returns>
		public bool TrySkipJsonValue()
		{
			TrySkipWhiteSpace();
			if ('t' == Current)
			{
				if (Advance() != 'r')
					return false;
				if (Advance() != 'u')
					return false;
				if (Advance() != 'e')
					return false;
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				return true;
			}
			if ('f' == Current)
			{
				if (Advance() != 'a')
					return false;
				if (Advance() != 'l')
					return false;
				if (Advance() != 's')
					return false;
				if (Advance() != 'e')
					return false;
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				return true;
			}
			if ('n' == Current)
			{
				if (Advance() != 'u')
					return false;
				if (Advance() != 'l')
					return false;
				if (Advance() != 'l')
					return false;
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				return true;
			}
			if ('-' == Current || '.' == Current || char.IsDigit((char)Current))
				return TrySkipReal();
			if ('\"' == Current)
				return TrySkipJsonString();

			if ('[' == Current)
			{
				Advance();
				if (TrySkipJsonValue())
				{
					TrySkipWhiteSpace();
					while (',' == Current)
					{
						Advance();
						if (!TrySkipJsonValue()) return false;
						TrySkipWhiteSpace();
					}
				}
				TrySkipWhiteSpace();
				if (']' != Current)
					return false;
				Advance();
				return true;
			}
			if ('{' == Current)
			{
				Advance();
				TrySkipWhiteSpace();
				if (TrySkipJsonString())
				{
					TrySkipWhiteSpace();
					if (':' != Current) return false;
					Advance();
					if (!TrySkipJsonValue())
						return false;
					TrySkipWhiteSpace();
					while (',' == Current)
					{
						Advance();
						TrySkipWhiteSpace();
						if (!TrySkipJsonString())
							return false;
						TrySkipWhiteSpace();
						if (':' != Current) return false;
						Advance();
						if (!TrySkipJsonValue())
							return false;
						TrySkipWhiteSpace();
					}
				}
				TrySkipWhiteSpace();
				if ('}' != Current)
					return false;
				Advance();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to read a JSON value into the capture buffer while parsing it
		/// </summary>
		/// <param name="result"><see cref="IDictionary{String,Object}"/> for a JSON object, <see cref="IList{Object}"/> for a JSON array, or the appropriate scalar value</param>
		/// <returns>True if the value was a valid value, otherwise false</returns>
		public bool TryParseJsonValue(out object result)
		{
			result = null;
			TryReadWhiteSpace();
			if ('t' == Current)
			{
				CaptureCurrent();
				if (Advance() != 'r')
					return false;
				CaptureCurrent();
				if (Advance() != 'u')
					return false;
				CaptureCurrent();
				if (Advance() != 'e')
					return false;
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				result = true;
				return true;
			}
			if ('f' == Current)
			{
				CaptureCurrent();
				if (Advance() != 'a')
					return false;
				CaptureCurrent();
				if (Advance() != 'l')
					return false;
				CaptureCurrent();
				if (Advance() != 's')
					return false;
				CaptureCurrent();
				if (Advance() != 'e')
					return false;
				CaptureCurrent();
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				result = false;
				return true;
			}
			if ('n' == Current)
			{
				CaptureCurrent();
				if (Advance() != 'u')
					return false;
				CaptureCurrent();
				if (Advance() != 'l')
					return false;
				CaptureCurrent();
				if (Advance() != 'l')
					return false;
				CaptureCurrent();
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					return false;
				return true;
			}
			if ('-' == Current || '.' == Current || char.IsDigit((char)Current))
			{
				double r;
				if (TryParseReal(out r))
				{
					result = r;
					return true;
				}
				return false;
			}
			if ('\"' == Current)
			{
				string s;
				if (TryParseJsonString(out s))
				{
					result = s;
					return true;
				}
				return false;
			}
			if ('[' == Current)
			{
				CaptureCurrent();
				Advance();
				var l = new List<object>();
				object v;
				if (TryParseJsonValue(out v))
				{
					l.Add(v);
					TryReadWhiteSpace();
					while (',' == Current)
					{
						CaptureCurrent();
						Advance();
						if (!TryParseJsonValue(out v)) return false;
						l.Add(v);
						TryReadWhiteSpace();
					}
				}
				TryReadWhiteSpace();
				if (']' != Current)
					return false;
				CaptureCurrent();
				Advance();
				result = l;
				return true;
			}
			if ('{' == Current)
			{
				CaptureCurrent();
				Advance();
				TryReadWhiteSpace();
				string n;
				object v;
				var d = new Dictionary<string, object>();
				if (TryParseJsonString(out n))
				{
					TryReadWhiteSpace();
					if (':' != Current) return false;
					CaptureCurrent();
					Advance();
					if (!TryParseJsonValue(out v))
						return false;
					d.Add(n, v);
					TryReadWhiteSpace();
					while (',' == Current)
					{
						CaptureCurrent();
						Advance();
						TryReadWhiteSpace();
						if (!TryParseJsonString(out n))
							return false;
						TryReadWhiteSpace();
						if (':' != Current) return false;
						CaptureCurrent();
						Advance();
						if (!TryParseJsonValue(out v))
							return false;
						d.Add(n, v);
						TryReadWhiteSpace();
					}
				}
				TryReadWhiteSpace();
				if ('}' != Current)
					return false;
				CaptureCurrent();
				Advance();
				result = d;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Reads a JSON value into the capture buffer while parsing it
		/// </summary>
		/// <returns><see cref="IDictionary{String,Object}"/> for a JSON object, <see cref="IList{Object}"/> for a JSON array, or the appropriate scalar value</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public object ParseJsonValue()
		{
			TrySkipWhiteSpace();
			if ('t' == Current)
			{
				Advance(); Expecting('r');
				Advance(); Expecting('u');
				Advance(); Expecting('e');
				Advance();
				return true;
			}
			if ('f' == Current)
			{
				Advance(); Expecting('a');
				Advance(); Expecting('l');
				Advance(); Expecting('s');
				Advance(); Expecting('e');
				Advance();
				return true;
			}
			if ('n' == Current)
			{
				Advance(); Expecting('u');
				Advance(); Expecting('l');
				Advance(); Expecting('l');
				Advance();
				return null;
			}
			if ('-' == Current || '.' == Current || char.IsDigit((char)Current))
				return ParseReal();
			if ('\"' == Current)
				return ParseJsonString();
			if ('[' == Current)
			{
				Advance();
				TrySkipWhiteSpace();
				var l = new List<object>();
				if (']' != Current)
				{
					l.Add(ParseJsonValue());
					TrySkipWhiteSpace();
					while (',' == Current)
					{
						Advance();
						l.Add(ParseJsonValue());
						TrySkipWhiteSpace();
					}
				}
				TrySkipWhiteSpace();
				Expecting(']');
				Advance();
				return l;
			}
			if ('{' == Current)
			{
				Advance();
				TrySkipWhiteSpace();
				var d = new Dictionary<string, object>();
				if ('}' != Current)
				{

					string n = ParseJsonString();
					TrySkipWhiteSpace();
					Expecting(':');
					Advance();
					object v = ParseJsonValue();
					d.Add(n, v);
					TrySkipWhiteSpace();
					while (',' == Current)
					{
						Advance();
						TrySkipWhiteSpace();
						n = ParseJsonString();
						TrySkipWhiteSpace();
						Expecting(':');
						Advance();
						v = ParseJsonValue();
						d.Add(n, v);
						TrySkipWhiteSpace();
					}
				}
				TrySkipWhiteSpace();
				if ('}' != Current)
					return false;
				Advance();
				return d;
			}
			return false;
		}
	}
}
