using System;
using System.Collections.Generic;
using System.Text;

namespace PC
{
	partial class ParseContext
	{
		const string _KeywordMap = "|abstract|add|as|ascending|async|await|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|descending|do|double|dynamic|else|enum|equals|explicit|extern|false|finally|fixed|float|for|foreach|get|global|goto|if|implicit|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|partial|private|protected|public|readonly|ref|remove|return|sbyte|sealed|set|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|var|virtual|void|volatile|while|yield|";
		/// <summary>
		/// 
		/// </summary>
		/// <returns></returns>
		public bool TryReadCSharpStringOrIdentifier()
		{
			EnsureStarted();
			switch (Current)
			{
				case -1: return false;
				case '@':
					if (!TryReadCSharpString())
						return TryReadCIdentifier();
					return true;
				case '\"':
					return TryReadCSharpString();
				default:
					return TryReadCSharpIdentifier();
			}
		}
		/// <summary>
		/// Attempts to skip a C# string literal or identifier without capturing
		/// </summary>
		/// <returns>True if a literal or identifier was found and skipped, otherwise false</returns>
		public bool TrySkipCSharpStringOrIdentifier()
		{
			EnsureStarted();
			switch (Current)
			{
				case -1: return false;
				case '@':
					if (!TrySkipCSharpString())
						return TrySkipCIdentifier();
					return true;
				case '\"':
					return TrySkipCSharpString();
				default:
					return TrySkipCSharpIdentifier();
			}
		}
		/// <summary>
		/// Attempts to read a C# string literal or identifier into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The unescaped value the literal or identifer represents</param>
		/// <returns>True if the value was valid, otherwise false</returns>
		public bool TryParseCSharpStringOrIdentifier(out string result)
		{
			result = null;
			EnsureStarted();
			switch (Current)
			{
				case -1: return false;
				case '@':
					if (!TryParseCSharpString(out result))
					{
						int i = CaptureBuffer.Length;
						if (!TryReadCIdentifier())
							return false;
						result = GetCapture(i);
						return true;
					}
					return true;
				case '\"':
					return TryParseCSharpString(out result);
				default:
					return TryParseCSharpIdentifier(out result);
			}
		}
		/// <summary>
		/// Reads a C# string ord identifier into the capture buffer while parsing it
		/// </summary>
		/// <returns>The unescaped identifier or string the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public string ParseCSharpStringOrIdentifier()
		{
			EnsureStarted();
			switch (Current)
			{
				case -1: Expecting(); return null;
				case '@':
					Advance();
					var sb = new StringBuilder();
					if ('\"' == Current)
					{
						while (-1 != Advance() && '\r' != Current && '\n' != Current)
						{
							if ('\"' == Current)
							{
								Advance();
								if ('\"' != Current)
								{
									return sb.ToString();
								}
							}
							sb.Append((char)Current);
						}
						Expecting('\"');
						return null; // never executes;
					}
					else
					{
						Expecting('_', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'F', 'f', 'G', 'g', 'H', 'h', 'I', 'i', 'J', 'j', 'K', 'k', 'L', 'l', 'M', 'm', 'N', 'n', 'O', 'o', 'P', 'p', 'Q', 'q', 'R', 'r', 'S', 's', 'T', 't', 'U', 'u', 'V', 'v', 'W', 'w', 'X', 'x', 'Y', 'y', 'Z', 'z');
						sb.Append((char)Current);
						while (-1 != Advance() && ('_' == Current || char.IsLetterOrDigit((char)Current)))
							sb.Append((char)Current);
						return sb.ToString();
					}
				case '\"':
					return ParseCSharpString();
				default:
					return ParseCSharpIdentifier();
			}
		}
		/// <summary>
		/// Attempts to read a C# identifier into the capture buffer
		/// </summary>
		/// <returns>True if a valid identifier was read, otherwise false</returns>
		public bool TryReadCSharpIdentifier()
		{
			bool verbatim = false;
			EnsureStarted();
			int l = CaptureBuffer.Length; // store the start point in the capture buffer
			if ('@' == Current)
			{

				verbatim = true;
				CaptureCurrent();
				Advance();
			}
			if (-1 != Current && ('_' == Current || char.IsLetter((char)Current)))
			{
				CaptureCurrent();
				while (-1 != Advance() && ('_' == Current || char.IsLetterOrDigit((char)Current)))
				{
					CaptureCurrent();
				}
				if (verbatim) return true; // no need to check for keyword
				string id = CaptureBuffer.ToString(l, CaptureBuffer.Length - l);
				if (_KeywordMap.IndexOf(string.Concat("|", id, "|")) > -1)
					return false; // matched a keyword
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to read a C# identifier into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The unescaped value the identifier represents</param>
		/// <returns>True if the value was a valid identifier, otherwise false</returns>
		public bool TryParseCSharpIdentifier(out string result)
		{
			var sb = new StringBuilder();
			bool verbatim = false;
			EnsureStarted();
			int l = CaptureBuffer.Length; // store the start point in the capture buffer
			if ('@' == Current)
			{
				verbatim = true;
				CaptureCurrent();
				Advance();
			}
			if (-1 != Current && ('_' == Current || char.IsLetter((char)Current)))
			{
				CaptureCurrent();
				sb.Append((char)Current);
				while (-1 != Advance() && ('_' == Current || char.IsLetterOrDigit((char)Current)))
				{
					CaptureCurrent();
					sb.Append((char)Current);

				}
				if (verbatim)
				{
					result = sb.ToString();
					return true; // no need to check for keyword
				}
				string id = CaptureBuffer.ToString(l, CaptureBuffer.Length - l);
				if (_KeywordMap.IndexOf(string.Concat("|", id, "|")) > -1)
				{
					result = null;
					return false; // matched a keyword
				}
				result = sb.ToString();
				return true;
			}
			result = null;
			return false;
		}
		/// <summary>
		/// Reads a C# identifier into the capture buffer while parsing it
		/// </summary>
		/// <returns>The unescaped identifier the literal identifier represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public string ParseCSharpIdentifier()
		{
			var sb = new StringBuilder();
			bool verbatim = false;
			EnsureStarted();
			if ('@' == Current)
			{
				verbatim = true;
				Advance();
			}
			Expecting('_', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'F', 'f', 'G', 'g', 'H', 'h', 'I', 'i', 'J', 'j', 'K', 'k', 'L', 'l', 'M', 'm', 'N', 'n', 'O', 'o', 'P', 'p', 'Q', 'q', 'R', 'r', 'S', 's', 'T', 't', 'U', 'u', 'V', 'v', 'W', 'w', 'X', 'x', 'Y', 'y', 'Z', 'z');
			sb.Append((char)Current);
			while (-1 != Advance() && ('_' == Current || char.IsLetterOrDigit((char)Current)))
				sb.Append((char)Current);

			if (verbatim)
				return sb.ToString(); // no need to check for keyword

			string id = sb.ToString();
			if (_KeywordMap.IndexOf(string.Concat("|", id, "|")) > -1)
				Expecting('_', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'F', 'f', 'G', 'g', 'H', 'h', 'I', 'i', 'J', 'j', 'K', 'k', 'L', 'l', 'M', 'm', 'N', 'n', 'O', 'o', 'P', 'p', 'Q', 'q', 'R', 'r', 'S', 's', 'T', 't', 'U', 'u', 'V', 'v', 'W', 'w', 'X', 'x', 'Y', 'y', 'Z', 'z', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');

			return sb.ToString();
		}
		/// <summary>
		/// Attempts to skip a C# identifier without capturing
		/// </summary>
		/// <returns>True if an identifier was found and skipped, otherwise false</returns>
		public bool TrySkipCSharpIdentifier()
		{
			bool verbatim = false;
			EnsureStarted();
			int l = CaptureBuffer.Length; // store the start point in the capture buffer
			if ('@' == Current)
			{

				verbatim = true;
				Advance();
			}
			if (-1 != Current && ('_' == Current || char.IsLetter((char)Current)))
			{
				while (-1 != Advance() && ('_' == Current || char.IsLetterOrDigit((char)Current))) ;
				if (verbatim) return true; // no need to check for keyword
				string id = CaptureBuffer.ToString(l, CaptureBuffer.Length - l);
				if (_KeywordMap.IndexOf(string.Concat("|", id, "|")) > -1)
					return false; // matched a keyword
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to read a C# numeric literal into the capture buffer
		/// </summary>
		/// <returns>True if a valid literal was read, otherwise false</returns>
		public bool TryReadCSharpNumeric()
		{
			bool hasDigits = false;
			bool isHex = false;
			bool isReal = false;
			EnsureStarted();
			if ('-' == Current)
			{

				CaptureCurrent();
				Advance();
			}
			else if ('+' == Current)
			{
				CaptureCurrent();
				Advance();
			}
			if ('0' == Current)
			{
				CaptureCurrent();
				Advance();
				if ('X' == Current || 'x' == Current)
				{
					isHex = true;
					CaptureCurrent();
					int i = 0;
					for (i = 0; i < 16; ++i)
					{

						hasDigits = true;
						if (-1 == Advance())
							break;
						if (!IsHexChar((char)Current))
							break;
						CaptureCurrent();
					}
					if (!hasDigits) return false;
				}
				else
					hasDigits = true;
			}
			if (!isHex && char.IsDigit((char)Current))
			{
				hasDigits = true;
				while (char.IsDigit((char)Current))
				{
					CaptureCurrent();
					if (-1 == Advance())
						break;
				}
			}
			if ('.' == Current)
			{
				if (isHex)
					return false;
				CaptureCurrent();
				Advance();
				hasDigits = true;
				isReal = true;
				if (-1 == Current) return false;
				if (!char.IsDigit((char)Current))
					return false;
				while (char.IsDigit((char)Current))
				{
					CaptureCurrent();
					if (-1 == Advance())
						break;
				}
			}
			if (!isHex && ('E' == Current || 'e' == Current))
			{
				if (!hasDigits)
					return false;
				CaptureCurrent();
				Advance();
				if (-1 == Current) return false;
				isReal = true;
				if ('+' == Current)
				{
					CaptureCurrent();
					Advance();
				}
				else if ('-' == Current)
				{
					CaptureCurrent();
					Advance();
				}
				if (-1 == Current) return false;
				if (!char.IsDigit((char)Current))
					return false;
				while (char.IsDigit((char)Current))
				{
					CaptureCurrent();
					if (-1 == Advance())
						break;
				}
			}
			if (!hasDigits)
				return false;
			if (isHex && 'B' == Current || 'b' == Current)
			{
				CaptureCurrent();
				Advance();
			}
			else if ('U' == Current || 'u' == Current)
			{
				if (isReal) return false;
				CaptureCurrent();
				Advance();
				if ('L' == Current || 'l' == Current)
				{
					CaptureCurrent();
					Advance();
				}
			}
			else if ('L' == Current || 'l' == Current)
			{
				if (isReal) return false;
				CaptureCurrent();
				Advance();
				if ('U' == Current || 'u' == Current)
				{
					CaptureCurrent();
					Advance();
				}
			}
			else if (!isHex)
			{
				if ('F' == Current || 'f' == Current)
				{
					CaptureCurrent();
					Advance();
					isReal = true;
				}
				else if ('D' == Current || 'd' == Current)
				{
					CaptureCurrent();
					Advance();
					isReal = true;
				}
				else if ('M' == Current || 'm' == Current)
				{
					CaptureCurrent();
					Advance();
					isReal = true;
				}
			}
			return -1 == Current || !char.IsLetterOrDigit((char)Current);
		}
		/// <summary>
		/// Attempts to skip a C# numeric literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipCSharpNumeric()
		{
			bool hasDigits = false;
			bool isHex = false;
			bool isReal = false;
			EnsureStarted();
			if ('-' == Current)
			{

				Advance();
			}
			else if ('+' == Current)
			{
				Advance();
			}
			if ('0' == Current)
			{
				Advance();
				if ('X' == Current || 'x' == Current)
				{
					isHex = true;
					int i = 0;
					for (i = 0; i < 16; ++i)
					{
						hasDigits = true;
						if (-1 == Advance())
							break;
						if (!IsHexChar((char)Current))
							break;
					}
					if (!hasDigits) return false;
				}
				else
					hasDigits = true;
			}
			if (!isHex && char.IsDigit((char)Current))
			{
				hasDigits = true;
				while (char.IsDigit((char)Current))
				{
					if (-1 == Advance())
						break;
				}
			}
			if ('.' == Current)
			{
				if (isHex)
					return false;
				Advance();
				hasDigits = true;
				isReal = true;
				if (-1 == Current) return false;
				if (!char.IsDigit((char)Current))
					return false;
				while (char.IsDigit((char)Current))
				{
					if (-1 == Advance())
						break;
				}
			}
			if (!isHex && ('E' == Current || 'e' == Current))
			{
				if (!hasDigits)
					return false;
				Advance();
				if (-1 == Current) return false;
				isReal = true;
				if ('+' == Current)
				{
					Advance();
				}
				else if ('-' == Current)
				{
					Advance();
				}
				if (-1 == Current) return false;
				if (!char.IsDigit((char)Current))
					return false;
				while (char.IsDigit((char)Current))
				{
					if (-1 == Advance())
						break;
				}
			}
			if (!hasDigits)
				return false;
			if (isHex && 'B' == Current || 'b' == Current)
				Advance();

			else if ('U' == Current || 'u' == Current)
			{
				if (isReal) return false;
				Advance();
				if ('L' == Current || 'l' == Current)
				{
					Advance();
				}
			}
			else if ('L' == Current || 'l' == Current)
			{
				if (isReal) return false;
				Advance();
				if ('U' == Current || 'u' == Current)
				{
					Advance();
				}
			}
			else if (!isHex)
			{
				if ('F' == Current || 'f' == Current)
				{
					Advance();
					isReal = true;
				}
				else if ('D' == Current || 'd' == Current)
				{
					Advance();
					isReal = true;
				}
				else if ('M' == Current || 'm' == Current)
				{
					Advance();
					isReal = true;
				}
			}
			return -1 == Current || !char.IsLetterOrDigit((char)Current);
		}
		/// <summary>
		/// Attempts to read a C# numeric into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The value the literal represents</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseCSharpNumeric(out object result)
		{
			result = null;
			ulong l = 0;
			ulong frac = 0;
			long exp = 0;
			bool hasDigits = false;
			bool isHex = false;
			bool isReal = false;
			bool isNeg = false;

			EnsureStarted();
			if ('-' == Current)
			{
				isNeg = true;
				CaptureCurrent();
				Advance();
			}
			else if ('+' == Current)
			{
				CaptureCurrent();
				Advance();
			}
			if ('0' == Current)
			{
				CaptureCurrent();
				Advance();
				if ('X' == Current || 'x' == Current)
				{
					isHex = true;
					CaptureCurrent();
					int i = 0;
					for (i = 0; i < 16; ++i)
					{
						hasDigits = true;
						if (-1 == Advance())
							break;
						if (!IsHexChar((char)Current))
							break;
						l <<= 4;
						l += FromHexChar((char)Current);
						CaptureCurrent();
					}
					if (!hasDigits) return false;
				}
				else
					hasDigits = true;
			}
			if (!isHex && char.IsDigit((char)Current))
			{
				hasDigits = true;
				while (char.IsDigit((char)Current))
				{
					l = checked(l * 10);
					l = checked(l + unchecked((uint)Current - '0'));
					CaptureCurrent();
					if (-1 == Advance())
						break;
				}
			}
			if ('.' == Current)
			{
				if (isHex)
					return false;
				CaptureCurrent();
				Advance();
				hasDigits = true;
				isReal = true;
				if (-1 == Current) return false;
				if (!char.IsDigit((char)Current))
					return false;
				while (char.IsDigit((char)Current))
				{
					frac = checked(frac * 10);
					frac = checked(frac + unchecked((uint)Current - '0'));
					CaptureCurrent();
					if (-1 == Advance())
						break;
				}
			}
			if (!isHex && ('E' == Current || 'e' == Current))
			{
				if (!hasDigits)
					return false;
				CaptureCurrent();
				Advance();
				if (-1 == Current) return false;
				isReal = true;
				bool isNegExp = false;
				if ('+' == Current)
				{
					CaptureCurrent();
					Advance();
				}
				else if ('-' == Current)
				{
					isNegExp = true;
					CaptureCurrent();
					Advance();
				}
				if (-1 == Current) return false;
				if (!char.IsDigit((char)Current))
					return false;
				while (char.IsDigit((char)Current))
				{
					exp = checked(exp * 10);
					if (!isNegExp)
						exp = checked(exp + unchecked((uint)Current - '0'));
					else
						exp = checked(exp - unchecked((uint)Current - '0'));
					CaptureCurrent();
					if (-1 == Advance())
						break;
				}
			}
			if (!hasDigits)
				return false;
			if (isHex && 'B' == Current || 'b' == Current)
			{
				CaptureCurrent();
				Advance();
				if (l > byte.MaxValue)
					return false;
				if (isNeg)
				{
					result = unchecked((byte)-(unchecked((byte)l)));
				}
				else
					result = unchecked((byte)l);
			}
			else if ('U' == Current || 'u' == Current)
			{
				if (isReal) return false;
				CaptureCurrent();
				Advance();
				if ('L' == Current || 'l' == Current)
				{
					if (isNeg)
						return false;
					CaptureCurrent();
					Advance();
					result = l;
				}
				else
				{
					if (l <= uint.MaxValue)
						result = ((uint)l);
					else
						result = l;
				}
			}
			else if ('L' == Current || 'l' == Current)
			{
				if (isReal) return false;
				CaptureCurrent();
				Advance();
				if ('U' == Current || 'u' == Current)
				{
					if (isNeg)
						return false;
					CaptureCurrent();
					Advance();
					result = l;
				}
				else
				{
					if (isNeg)
					{
						if (l > (long.MaxValue + 1UL))
							return false;
						result = -unchecked((long)l);
					}
					else
					{
						result = unchecked((long)l);
					}
				}
			}
			else if (!isHex)
			{
				if ('F' == Current || 'f' == Current)
				{
					CaptureCurrent();
					Advance();
					isReal = true;
					float f = l;
					float fr = frac;
					while (fr > 1) fr /= 10f;
					f += fr;
					f *= (float)Math.Pow(10, exp);
					if (isNeg) f = -f;
					result = f;
				}
				else if ('D' == Current || 'd' == Current)
				{
					CaptureCurrent();
					Advance();
					isReal = true;
					double d = l;
					double fr = frac;
					while (fr > 1) fr /= 10d;
					d += fr;
					d *= Math.Pow(10, exp);
					if (isNeg) d = -d;
					result = d;
				}
				else if ('M' == Current || 'm' == Current)
				{
					CaptureCurrent();
					Advance();
					isReal = true;
					decimal m = l;
					decimal fr = frac;
					while (fr >= 1) fr /= 10m;
					m += fr;
					m *= (decimal)Math.Pow(10, exp);
					if (isNeg) m = -m;
					result = m;
				}
				else if (isReal)
				{
					isReal = true;
					double d = l;
					double fr = frac;
					while (fr >= 1) fr /= 10d;
					d += fr;
					d *= Math.Pow(10, exp);
					if (isNeg) d = -d;
					result = d;
				}
				else
				{
					if (!isNeg)
					{
						if (l <= int.MaxValue)
							result = (int)l;
						else if (l <= uint.MaxValue)
							result = (uint)l;
						else if (l <= long.MaxValue)
							result = (long)l;
						else
							result = l;
					}
					else
					{
						if (l > long.MaxValue)
							return false; // overflow
						if (l > 0L - (int.MinValue))
							result = checked(-(long)l);
						else
							result = -(int)l;
					}
				}
			}
			return -1 == Current || !char.IsLetterOrDigit((char)Current);

		}
		/// <summary>
		/// Reads a C# numeric literal into the capture buffer while parsing it
		/// </summary>
		/// <returns>The value the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public object ParseCSharpNumeric()
		{
			object result = null;
			ulong l = 0;
			ulong frac = 0;
			long exp = 0;
			bool hasDigits = false;
			bool isHex = false;
			bool isReal = false;
			bool isNeg = false;

			EnsureStarted();
			Expecting('-', '+', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			if ('-' == Current)
			{
				isNeg = true;
				Advance();
			}
			else if ('+' == Current)
			{
				Advance();
			}
			if ('0' == Current)
			{
				Advance();
				if ('X' == Current || 'x' == Current)
				{
					isHex = true;
					int i = 0;
					for (i = 0; i < 16; ++i)
					{
						hasDigits = true;
						if (-1 == Advance())
							break;
						if (!IsHexChar((char)Current))
							break;
						l <<= 4;
						l += FromHexChar((char)Current);
					}
					if (!hasDigits)
						Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f');

				}
				else
					hasDigits = true;
			}
			if (!isHex && char.IsDigit((char)Current))
			{
				hasDigits = true;
				while (char.IsDigit((char)Current))
				{
					l = checked(l * 10);
					l = checked(l + unchecked((uint)Current - '0'));
					if (-1 == Advance())
						break;
				}
			}
			if ('.' == Current)
			{
				if (isHex)
					Expecting('U', 'u', 'L', 'l', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f', -1);
				Advance();
				hasDigits = true;
				isReal = true;
				Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				while (char.IsDigit((char)Current))
				{
					frac = checked(frac * 10);
					frac = checked(frac + unchecked((uint)Current - '0'));
					if (-1 == Advance())
						break;
				}
			}
			if (!isHex && ('E' == Current || 'e' == Current))
			{
				if (!hasDigits)
					Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				Advance();
				Expecting('-', '+', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				isReal = true;
				bool isNegExp = false;
				if ('+' == Current)
				{
					Advance();
				}
				else if ('-' == Current)
				{
					isNegExp = true;
					Advance();
				}
				Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				while (char.IsDigit((char)Current))
				{
					exp = checked(exp * 10);
					if (!isNegExp)
						exp = checked(exp + unchecked((uint)Current - '0'));
					else
						exp = checked(exp - unchecked((uint)Current - '0'));
					if (-1 == Advance())
						break;
				}
			}
			if (!hasDigits)
				Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			if (isHex && 'B' == Current || 'b' == Current)
			{
				Advance();
				if (l > byte.MaxValue)
					Expecting(-1);
				if (isNeg)
				{
					result = unchecked((byte)-(unchecked((byte)l)));
				}
				else
					result = unchecked((byte)l);
			}
			else if ('U' == Current || 'u' == Current)
			{
				if (isReal)
					Expecting('d', 'f', 'm', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				Advance();
				if ('L' == Current || 'l' == Current)
				{
					if (isNeg)
						Expecting(-1);
					Advance();
					result = l;
				}
				else
				{
					if (l <= uint.MaxValue)
						result = ((uint)l);
					else
						result = l;
				}
			}
			else if ('L' == Current || 'l' == Current)
			{
				if (isReal)
					Expecting('d', 'f', 'm', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				Advance();
				if ('U' == Current || 'u' == Current)
				{
					if (isNeg)
						Expecting(-1);
					Advance();
					result = l;
				}
				else
				{
					if (isNeg)
					{
						if (l > (long.MaxValue + 1UL))
							throw new OverflowException();
						result = -unchecked((long)l);
					}
					else
					{
						result = unchecked((long)l);
					}
				}
			}
			else if (!isHex)
			{
				if ('F' == Current || 'f' == Current)
				{
					Advance();
					isReal = true;
					float f = l;
					float fr = frac;
					while (fr > 1) fr /= 10f;
					f += fr;
					f *= (float)Math.Pow(10, exp);
					if (isNeg) f = -f;
					result = f;
				}
				else if ('D' == Current || 'd' == Current)
				{
					Advance();
					isReal = true;
					double d = l;
					double fr = frac;
					while (fr > 1) fr /= 10d;
					d += fr;
					d *= Math.Pow(10, exp);
					if (isNeg) d = -d;
					result = d;
				}
				else if ('M' == Current || 'm' == Current)
				{
					Advance();
					isReal = true;
					decimal m = l;
					decimal fr = frac;
					while (fr >= 1) fr /= 10m;
					m += fr;
					m *= (decimal)Math.Pow(10, exp);
					if (isNeg) m = -m;
					result = m;
				}
				else if (isReal)
				{
					isReal = true;
					double d = l;
					double fr = frac;
					while (fr >= 1) fr /= 10d;
					d += fr;
					d *= Math.Pow(10, exp);
					if (isNeg) d = -d;
					result = d;
				}
				else
				{
					if (!isNeg)
					{
						if (l <= int.MaxValue)
							result = (int)l;
						else if (l <= uint.MaxValue)
							result = (uint)l;
						else if (l <= long.MaxValue)
							result = (long)l;
						else
							result = l;
					}
					else
					{
						if (l > long.MaxValue)
							return false; // overflow
						if (l > 0L - (int.MinValue))
							result = checked(-(long)l);
						else
							result = -(int)l;
					}
				}
			}
			if (-1 == Current || !char.IsLetterOrDigit((char)Current))
				return result;
			Expecting(-1);
			return null; // never called

		}
		/// <summary>
		/// Attempts to read a C# boolean literal into the capture buffer
		/// </summary>
		/// <returns>True if a valid literal was read, otherwise false</returns>
		public bool TryReadCSharpBool()
		{

			EnsureStarted();
			if ('t' == Current)
				return TryReadLiteral("true");
			else if ('f' == Current)
				return TryReadLiteral("false");
			return false;
		}
		/// <summary>
		/// Attempts to skip the a C# boolean literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipCSharpBool()
		{
			EnsureStarted();
			if ('t' == Current)
				return TrySkipLiteral("true");
			else if ('f' == Current)
				return TrySkipLiteral("false");
			return false;
		}
		/// <summary>
		/// Attempts to read a C# boolean literal into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">True if the value is "true", otherwise false</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseCSharpBool(out bool result)
		{
			result = false;
			EnsureStarted();
			if ('t' == Current)
			{
				if (TryReadLiteral("true"))
				{
					result = true;
					return true;
				}
			}
			else if ('f' == Current)
			{
				if (TryReadLiteral("false"))
				{
					return true;
				}
			}
			return false;
		}
		/// <summary>
		/// Reads a C# boolean value into the capture buffer while parsing it
		/// </summary>
		/// <returns>True if the cursor is over "true", and false if it's over "false"</returns>
		/// <exception cref="ExpectingException">The input was not a valid boolean</exception>
		public bool ParseCSharpBool()
		{
			Expecting('t', 'f');
			if (Current == 't')
			{
				Advance();
				Expecting('r');
				Advance();
				Expecting('u');
				Advance();
				Expecting('e');
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					throw new ExpectingException(string.Format("Unexpected character \'{0}\' at line {1}, column {2}, position {3}", (char)Current, Line, Column, Position));
				return true;
			}
			else // Current = 'f' 
			{
				Advance();
				Expecting('a');
				Advance();
				Expecting('l');
				Advance();
				Expecting('s');
				Advance();
				Expecting('e');
				if (-1 != Advance() && char.IsLetterOrDigit((char)Current))
					throw new ExpectingException(string.Format("Unexpected character \'{0}\' at line {1}, column {2}, position {3}", (char)Current, Line, Column, Position));
				return false;
			}
			throw new InvalidProgramException(); // should never get here
		}
		/// <summary>
		/// Attempts to read a C# character literal into the capture buffer
		/// </summary>
		/// <returns>True if a valid literal was read, otherwise false</returns>
		public bool TryReadCSharpChar()
		{
			EnsureStarted();
			if ('\'' != Current)
				return false;
			CaptureCurrent();
			Advance();
			switch (Current)
			{
				case -1:
				case '\r':
				case '\n':
					break;
				case '\\':
					CaptureCurrent();
					Advance();
					switch (Current)
					{
						case '0':
						case 'a':
						case 'v':
						case '\'':
						case 'f':
						case 'r':
						case 'n':
						case 't':
						case '\\':
						case '\"':
						case 'b':
							CaptureCurrent();
							Advance();
							break;
						case 'u':
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							break;
						case 'x':
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							if (!IsHexChar((char)Current))
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							if (IsHexChar((char)Current))
							{
								CaptureCurrent();
								if (-1 == Advance())
									return false;
								if (IsHexChar((char)Current))
								{
									CaptureCurrent();
									if (-1 == Advance())
										return false;
									if (IsHexChar((char)Current))
									{
										CaptureCurrent();
										if (-1 == Advance())
											return false;
									}
								}
							}
							break;
						case 'U':
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							break;
						default:
							return false;
					}
					break;
				default:
					CaptureCurrent();
					Advance();
					break;
			}
			if ('\'' != Current) return false;
			CaptureCurrent();
			Advance();
			return true;
		}
		/// <summary>
		/// Attempts to skip a C# character literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipCSharpChar()
		{
			EnsureStarted();
			if ('\'' != Current)
				return false;
			Advance();
			switch (Current)
			{
				case -1:
				case '\r':
				case '\n':
					break;
				case '\\':
					Advance();
					switch (Current)
					{
						case '0':
						case 'a':
						case 'v':
						case '\'':
						case 'f':
						case 'r':
						case 'n':
						case 't':
						case '\\':
						case '\"':
						case 'b':
							Advance();
							break;
						case 'u':
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							break;
						case 'x':
							if (-1 == Advance())
								return false;
							if (!IsHexChar((char)Current))
								return false;
							if (-1 == Advance())
								return false;
							if (IsHexChar((char)Current))
							{
								if (-1 == Advance())
									return false;
								if (IsHexChar((char)Current))
								{
									if (-1 == Advance())
										return false;
									if (IsHexChar((char)Current))
									{
										if (-1 == Advance())
											return false;
									}
								}
							}
							break;
						case 'U':
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							if (-1 == Advance())
								return false;
							break;
						default:
							return false;
					}
					break;
				default:
					Advance();
					break;
			}
			if ('\'' != Current) return false;
			Advance();
			return true;
		}
		/// <summary>
		/// Attempts to read a C# character literal into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The value the literal represents</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseCSharpChar(out string result) // result is usually single length but may be multiple chars if it's a UTF32 surrogate
		{
			result = null;
			EnsureStarted();
			if ('\'' != Current)
				return false;
			CaptureCurrent();
			Advance();
			var sb = new StringBuilder();
			switch (Current)
			{
				case -1:
				case '\r':
				case '\n':
					break;
				case '\\':
					CaptureCurrent();
					Advance();
					switch (Current)
					{
						case '0':
							sb.Append('\0');
							break;
						case 'a':
							sb.Append('\a');
							break;

						case 'v':
							sb.Append('\v');
							break;
						case '\'':
							sb.Append('\'');
							break;
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
							CaptureCurrent();
							Advance();
							break;
						case 'x':
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							ch = 0;
							if (!IsHexChar((char)Current))
								return false;
							ch <<= 4;
							ch |= FromHexChar((char)Current);
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							if (IsHexChar((char)Current))
							{
								ch <<= 4;
								ch |= FromHexChar((char)Current);
								CaptureCurrent();
								if (-1 == Advance())
									return false;
								if (IsHexChar((char)Current))
								{
									ch <<= 4;
									ch |= FromHexChar((char)Current);
									CaptureCurrent();
									if (-1 == Advance())
										return false;
									if (IsHexChar((char)Current))
									{
										ch <<= 4;
										ch |= FromHexChar((char)Current);
										CaptureCurrent();
										if (-1 == Advance())
											return false;
									}
								}
							}
							sb.Append((char)ch);
							break;
						case 'U':
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							ch = 0;
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
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							ch = 0;
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

							sb.Append(char.ConvertFromUtf32(ch));
							CaptureCurrent();
							Advance();
							break;
						default:
							return false;
					}
					break;
				default:
					CaptureCurrent();
					Advance();
					break;
			}
			if ('\'' != Current) return false;
			CaptureCurrent();
			Advance();
			result = sb.ToString();
			return true;
		}
		/// <summary>
		/// Reads a C# character literal into the capture buffer while parsing it
		/// </summary>
		/// <returns>The character the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public string ParseCSharpChar()
		{
			string result = null;
			EnsureStarted();
			Expecting('\'');
			Advance();
			Expecting();
			switch (Current)
			{
				case '\r':
				case '\n':
					throw new ExpectingException(string.Format("Unexpected newline in constant at line {0}, column {1}, position {2}.", Line, Column, Position));
				case '\\':
					Advance();
					switch (Current)
					{
						case 'b':
							result = "\b";
							break;
						case 'a':
							result = "\a";
							break;
						case 'v':
							result = "\v";
							break;
						case '0':
							result = "\0";
							break;
						case 'n':
							result = "\n";
							break;
						case 'r':
							result = "\r";
							break;
						case 't':
							result = "\t";
							break;
						case '\\':
							result = "\\";
							break;
						case '\'':
							result = "\'";
							break;
						case '\"':
							result = "\"";
							break;
						case 'f':
							result = "\f";
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
							result = ((char)ch).ToString();
							Advance();
							break;
						case 'x':
							ch = 0;
							Advance();
							Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f');
							ch <<= 4;
							ch |= FromHexChar((char)Current);
							Advance();
							Expecting();
							if (IsHexChar((char)Current))
							{
								ch <<= 4;
								ch |= FromHexChar((char)Current);
								Advance();
								Expecting();
								if (IsHexChar((char)Current))
								{
									ch <<= 4;
									ch |= FromHexChar((char)Current);
									Advance();
									Expecting();
									if (IsHexChar((char)Current))
									{
										ch <<= 4;
										ch |= FromHexChar((char)Current);
										Advance();
									}
								}
							}
							result = ((char)ch).ToString();
							break;
						case 'U':
							ch = 0;
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
							result = char.ConvertFromUtf32(ch);
							Advance();
							break;
						default:
							// must throw if we get here
							Expecting('b', 'a', 'v', '0', 'n', 'r', 't', 'f', '\\', '\'', '\"', 'u', 'U', 'x');
							break;
					}
					break;
				default:
					result = ((char)Current).ToString();
					Advance();
					break;
			}
			Expecting('\'');
			Advance();
			return result;

		}
		/// <summary>
		/// Attempts to read a C# string literal into the capture buffer
		/// </summary>
		/// <returns>True if a valid literal was read, otherwise false</returns>
		public bool TryReadCSharpString()
		{
			EnsureStarted();
			if ('@' == Current)
			{
				CaptureCurrent();
				Advance();
				if ('\"' != Current)
					return false;
				CaptureCurrent();
				while (-1 != Advance() && '\r' != Current && '\n' != Current)
				{
					if ('\"' == Current)
					{
						CaptureCurrent();
						Advance();
						if ('\"' != Current)
							return true;
					}
					CaptureCurrent();
				}
				return false;
			}
			else if ('\"' != Current)
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
		/// Attempts to skip a C# string literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipCSharpString()
		{
			EnsureStarted();
			if ('@' == Current)
			{
				Advance();
				if ('\"' != Current)
					return false;
				while (-1 != Advance() && '\r' != Current && '\n' != Current)
				{
					if ('\"' == Current)
					{
						Advance();
						if ('\"' != Current)
							return true;
					}
				}
				return false;
			}
			else if ('\"' != Current)
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
		/// Attempts to read a C# string literal into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The value the literal represents</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseCSharpString(out string result)
		{
			result = null;
			var sb = new StringBuilder();
			EnsureStarted();
			if ('@' == Current)
			{
				CaptureCurrent();
				Advance();
				if ('\"' != Current)
					return false;
				CaptureCurrent();
				while (-1 != Advance() && '\r' != Current && '\n' != Current)
				{
					if ('\"' == Current)
					{
						CaptureCurrent();
						Advance();
						if ('\"' != Current)
						{
							result = sb.ToString();
							return true;
						}
					}
					CaptureCurrent();
					sb.Append((char)Current);
				}
				return false;
			}
			else if ('\"' != Current)
				return false;
			CaptureCurrent();
			Advance();
			while (-1 != Current && '\r' != Current && '\n' != Current && Current != '\"')
			{
				CaptureCurrent();
				if ('\\' == Current)
				{
					if (-1 == Advance() || '\r' == Current || '\n' == Current)
						return false;
					CaptureCurrent();
					switch (Current)
					{
						case '0':
							sb.Append('\0');
							Advance();
							break;
						case 'a':
							sb.Append('\a');
							Advance();
							break;

						case 'v':
							sb.Append('\v');
							Advance();
							break;
						case '\'':
							sb.Append('\'');
							Advance();
							break;
						case 'f':
							sb.Append('\f');
							Advance();
							break;
						case 'r':
							sb.Append('\r');
							Advance();
							break;
						case 'n':
							sb.Append('\n');
							Advance();
							break;
						case 't':
							sb.Append('\t');
							Advance();
							break;
						case '\\':
							sb.Append('\\');
							Advance();
							break;
						case '\"':
							sb.Append('\"');
							Advance();
							break;
						case 'b':
							sb.Append('\b');
							Advance();
							break;
						case 'u':
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
							CaptureCurrent();
							Advance();

							break;
						case 'x':
							if (-1 == Advance())
								return false;
							ch = 0;
							if (!IsHexChar((char)Current))
								return false;
							ch <<= 4;
							ch |= FromHexChar((char)Current);
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							if (IsHexChar((char)Current))
							{
								ch <<= 4;
								ch |= FromHexChar((char)Current);
								CaptureCurrent();
								if (-1 == Advance())
									return false;
								if (IsHexChar((char)Current))
								{
									ch <<= 4;
									ch |= FromHexChar((char)Current);
									CaptureCurrent();
									if (-1 == Advance())
										return false;
									if (IsHexChar((char)Current))
									{
										ch <<= 4;
										ch |= FromHexChar((char)Current);
										CaptureCurrent();
										if (-1 == Advance())
											return false;
									}
								}
							}
							sb.Append((char)ch);
							break;
						case 'U':
							if (-1 == Advance())
								return false;
							ch = 0;
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
							CaptureCurrent();
							if (-1 == Advance())
								return false;
							ch = 0;
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
							sb.Append(char.ConvertFromUtf32(ch));
							CaptureCurrent();
							Advance();

							break;

						default:
							return false;
					}
				}
				else
				{
					sb.Append((char)Current);
					Advance();
				}

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
		/// Reads a C# stromg literal into the capture buffer while parsing it
		/// </summary>
		/// <returns>The string the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public string ParseCSharpString()
		{
			var sb = new StringBuilder();
			EnsureStarted();
			Expecting('\"', '@');
			if ('@' == Current)
			{
				Advance();
				Expecting('\"');
				while (-1 != Advance() && '\r' != Current && '\n' != Current)
				{
					if ('\"' == Current)
					{
						Advance();
						if ('\"' != Current)
						{
							return sb.ToString();
						}
					}
					sb.Append((char)Current);
				}
				Expecting('\"');
				return null; // never executes;
			}
			else // Current is '\"'
			{


				Advance();
				while (-1 != Current && '\r' != Current && '\n' != Current && Current != '\"')
				{
					if ('\\' == Current)
					{
						Advance();
						switch (Current)
						{
							case '0':
								sb.Append('\0');
								Advance();
								break;
							case 'a':
								sb.Append('\a');
								Advance();
								break;

							case 'v':
								sb.Append('\v');
								Advance();
								break;
							case '\'':
								sb.Append('\'');
								Advance();
								break;

							case 'b':
								sb.Append('\b');
								Advance();
								break;
							case 'f':
								sb.Append('\f');
								Advance();
								break;
							case 'n':
								sb.Append('\n');
								Advance();
								break;
							case 'r':
								sb.Append('\r');
								Advance();
								break;
							case 't':
								sb.Append('\t');
								Advance();
								break;
							case '\\':
								sb.Append('\\');
								Advance();
								break;
							case '\"':
								sb.Append('\"');
								Advance();
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
								Advance();
								break;

							case 'x':
								ch = 0;
								Advance();
								Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'a', 'B', 'b', 'C', 'c', 'D', 'd', 'E', 'e', 'F', 'f');
								ch <<= 4;
								ch |= FromHexChar((char)Current);
								Advance();
								Expecting();
								if (IsHexChar((char)Current))
								{
									ch <<= 4;
									ch |= FromHexChar((char)Current);
									Advance();
									Expecting();
									if (IsHexChar((char)Current))
									{
										ch <<= 4;
										ch |= FromHexChar((char)Current);
										Advance();
										Expecting();
										if (IsHexChar((char)Current))
										{
											ch <<= 4;
											ch |= FromHexChar((char)Current);
											Advance();

										}
									}
								}
								sb.Append((char)ch);
								break;
							case 'U':
								ch = 0;
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
								sb.Append(char.ConvertFromUtf32(ch));
								Advance();
								break;
							default:
								Expecting('b', 'a', 'v', '0', 'n', 'r', 't', 'f', '\\', '\'', '\"', 'u', 'U', 'x');
								break;
						}
					}
					else
					{
						sb.Append((char)Current);
						Advance();
					}
				}
				Expecting('\"');
				Advance();
				return sb.ToString();
			}
		}
		/// <summary>
		/// Attempts to read a C# literal into the capture buffer
		/// </summary>
		/// <returns>True if a valid literal was read, otherwise false</returns>
		public bool TryReadCSharpLiteral()
		{
			EnsureStarted();
			switch (Current)
			{
				case '@':
				case '\"':
					return TryReadCSharpString();
				case '\'':
					return TryReadCSharpChar();
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '.':
				case '-':
				case '+':
					return TryReadCSharpNumeric();
				case 't':
				case 'f':
					return TryReadCSharpBool();
				case 'n':
					return TryReadLiteral("null");
			}
			return false;
		}
		/// <summary>
		/// Attempts to skip a C# literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipCSharpLiteral()
		{
			EnsureStarted();
			switch (Current)
			{
				case '@':
				case '\"':
					return TrySkipCSharpString();
				case '\'':
					return TrySkipCSharpChar();
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '.':
				case '-':
				case '+':
					return TrySkipCSharpNumeric();
				case 't':
				case 'f':
					return TrySkipCSharpBool();
				case 'n':
					return TrySkipLiteral("null");
			}
			return false;
		}
		/// <summary>
		/// Attempts to read a C# literal into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The value the literal represents</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseCSharpLiteral(out object result)
		{
			result = null;
			EnsureStarted();
			switch (Current)
			{
				case '@':
				case '\"':
					string s;
					if (TryParseCSharpString(out s))
					{
						result = s;
						return true;
					}
					break;
				case '\'':
					if (TryParseCSharpChar(out s))
					{
						if (1 == s.Length)
							result = s[0];
						else
							result = s;
						return true;
					}
					break;
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '.':
				case '-':
				case '+':
					if (TryParseCSharpNumeric(out result))
						return true;
					break;
				case 't':
				case 'f':
					bool b;
					if (TryParseCSharpBool(out b))
					{
						result = b;
						return true;
					}
					break;
				case 'n':
					if (TryReadLiteral("null"))
						return true;
					break;
			}
			return false;
		}
		/// <summary>
		/// Reads a C# literal into the capture buffer while parsing it
		/// </summary>
		/// <returns>The value the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public object ParseCSharpLiteral()
		{
			EnsureStarted();
			switch (Current)
			{
				case '@':
				case '\"':
					return ParseCSharpString();

				case '\'':
					return ParseCSharpChar();
				case '0':
				case '1':
				case '2':
				case '3':
				case '4':
				case '5':
				case '6':
				case '7':
				case '8':
				case '9':
				case '.':
				case '-':
				case '+':
					return ParseCSharpNumeric();
				case 't':
				case 'f':
					return ParseCSharpBool();
				case 'n':
					Advance();
					Expecting('u');
					Advance();
					Expecting('l');
					Advance();
					Expecting('l');
					Advance();
					if (-1 != Current && char.IsLetterOrDigit((char)Current))
						Expecting(-1);
					return null;
			}
			Expecting('@', '\"', '\'', '.', '+', '-', 't', 'f', 'n', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			return null; // never executed
		}
	}
}
