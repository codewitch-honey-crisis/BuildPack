using System;
using System.Text;

namespace PC
{
	partial class ParseContext
	{
		
		/// <summary>
		/// Indicates if the character is hex
		/// </summary>
		/// <param name="hex">The character to examine</param>
		/// <returns>True if the character is a valid hex character, otherwise false</returns>
		internal static bool IsHexChar(char hex)
		{
			return (':' > hex && '/' < hex) ||
				('G' > hex && '@' < hex) ||
				('g' > hex && '`' < hex);
		}
		/// <summary>
		/// Converts a hex character to its byte representation
		/// </summary>
		/// <param name="hex">The character to convert</param>
		/// <returns>The byte that the character represents</returns>
		internal static byte FromHexChar(char hex)
		{
			if (':' > hex && '/' < hex)
				return (byte)(hex - '0');
			if ('G' > hex && '@' < hex)
				return (byte)(hex - '7'); // 'A'-10
			if ('g' > hex && '`' < hex)
				return (byte)(hex - 'W'); // 'a'-10
			throw new ArgumentException("The value was not hex.", "hex");
		}
		/// <summary>
		/// Attempts to read a generic integer into the capture buffer
		/// </summary>
		/// <returns>True if a valid integer was read, otherwise false</returns>
		public bool TryReadInteger()
		{
			EnsureStarted();
			bool neg = false;
			if ('-' == Current)
			{
				neg = true;
				CaptureCurrent();
				Advance();
			}
			else if ('0' == Current)
			{
				CaptureCurrent();
				Advance();
				if (-1 == Current) return true;
				return !char.IsDigit((char)Current);
			}
			if (-1 == Current || (neg && '0' == Current) || !char.IsDigit((char)Current))
				return false;
			if (!TryReadDigits())
				return false;
			return true;
		}
		/// <summary>
		/// Attempts to skip a generic integer without capturing
		/// </summary>
		/// <returns>True if an integer was found and skipped, otherwise false</returns>
		public bool TrySkipInteger()
		{
			EnsureStarted();
			bool neg = false;
			if ('-' == Current)
			{
				neg = true;
				Advance();
			}
			else if ('0' == Current)
			{
				Advance();
				if (-1 == Current) return true;
				return !char.IsDigit((char)Current);
			}
			if (-1 == Current || (neg && '0' == Current) || !char.IsDigit((char)Current))
				return false;
			if (!TrySkipDigits())
				return false;
			return true;
		}
		// must be object because we don't know the int type. To be lexically valid we must use BigInteger when necessary
		/// <summary>
		/// Attempts to read a C# integer into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The value the literal represents</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseInteger(out object result)
		{
			result = null;
			EnsureStarted();
			if (-1 == Current) return false;
			bool neg = false;
			if ('-' == Current)
			{
				CaptureCurrent();
				Advance();
				neg = true;
			}
			int l = CaptureBuffer.Length;
			if (TryReadDigits())
			{
				string num = CaptureBuffer.ToString(l, CaptureBuffer.Length - l);
				if (neg)
					num = '-' + num;
				int r;
				if (int.TryParse(num, out r))
				{
					result = r;
					return true;
				}
				long ll;
				if (long.TryParse(num, out ll))
				{
					result = ll;
					return true;
				}
				System.Numerics.BigInteger b;
				if (System.Numerics.BigInteger.TryParse(num, out b))
				{
					result = b;
					return true;
				}

			}
			return false;
		}
		/// <summary>
		/// Reads a C# integer literal into the capture buffer while parsing it
		/// </summary>
		/// <returns>The value the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>

		public object ParseInteger()
		{
			EnsureStarted();
			Expecting('-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			bool neg = ('-' == Current);
			if (neg)
			{
				Advance();
				Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			}
			System.Numerics.BigInteger i = 0;
			if (!neg)
			{
				i += ((char)Current) - '0';
				while (-1 != Advance() && char.IsDigit((char)Current))
				{
					i *= 10;
					i += ((char)Current) - '0';
				}

			}
			else
			{
				i -= ((char)Current) - '0';
				while (-1 != Advance() && char.IsDigit((char)Current))
				{
					i *= 10;
					i -= ((char)Current) - '0';
				}
			}
			if (i <= int.MaxValue && i >= int.MinValue)
				return (int)i;
			else if (i <= long.MaxValue && i >= long.MinValue)
				return (long)i;
			return i;
		}
		/// <summary>
		/// Attempts to read a generic floating point number into the capture buffer
		/// </summary>
		/// <returns>True if a valid floating point number was read, otherwise false</returns>
		public bool TryReadReal()
		{
			EnsureStarted();
			bool readAny = false;
			if ('-' == Current)
			{
				CaptureCurrent();
				Advance();
			}
			if (char.IsDigit((char)Current))
			{
				if (!TryReadDigits())
					return false;
				readAny = true;
			}
			if ('.' == Current)
			{
				CaptureCurrent();
				Advance();
				if (!TryReadDigits())
					return false;
				readAny = true;
			}
			if ('E' == Current || 'e' == Current)
			{
				CaptureCurrent();
				Advance();
				if ('-' == Current || '+' == Current)
				{
					CaptureCurrent();
					Advance();
				}
				return TryReadDigits();
			}

			return readAny;
		}
		/// <summary>
		/// Attempts to skip a generic floating point literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipReal()
		{
			bool readAny = false;
			EnsureStarted();
			if ('-' == Current)
				Advance();
			if (char.IsDigit((char)Current))
			{
				if (!TrySkipDigits())
					return false;
				readAny = true;
			}
			if ('.' == Current)
			{
				Advance();
				if (!TrySkipDigits())
					return false;
				readAny = true;
			}
			if ('E' == Current || 'e' == Current)
			{
				Advance();
				if ('-' == Current || '+' == Current)
					Advance();
				return TrySkipDigits();
			}

			return readAny;
		}
		/// <summary>
		/// Attempts to read a floating point literal into the capture buffer while parsing it
		/// </summary>
		/// <param name="result">The value the literal represents</param>
		/// <returns>True if the value was a valid literal, otherwise false</returns>
		public bool TryParseReal(out double result)
		{
			result = default(double);
			int l = CaptureBuffer.Length;
			if (!TryReadReal())
				return false;
			return double.TryParse(CaptureBuffer.ToString(l, CaptureBuffer.Length - l), out result);
		}
		/// <summary>
		/// Reads a floating point literal into the capture buffer while parsing it
		/// </summary>
		/// <returns>The value the literal represents</returns>
		/// <exception cref="ExpectingException">The input was not valid</exception>
		public double ParseReal()
		{
			EnsureStarted();
			var sb = new StringBuilder();
			Expecting('-', '.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			bool neg = ('-' == Current);
			if (neg)
			{
				sb.Append((char)Current);
				Advance();
				Expecting('.', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
			}
			while (-1 != Current && char.IsDigit((char)Current))
			{
				sb.Append((char)Current);
				Advance();
			}
			if ('.' == Current)
			{
				sb.Append((char)Current);
				Advance();
				Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				sb.Append((char)Current);
				while (-1 != Advance() && char.IsDigit((char)Current))
				{
					sb.Append((char)Current);
				}
			}
			if ('E' == Current || 'e' == Current)
			{
				sb.Append((char)Current);
				Advance();
				Expecting('+', '-', '0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				switch (Current)
				{
					case '+':
					case '-':
						sb.Append((char)Current);
						Advance();
						break;
				}
				Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
				sb.Append((char)Current);
				while (-1 != Advance())
				{
					Expecting('0', '1', '2', '3', '4', '5', '6', '7', '8', '9');
					sb.Append((char)Current);
				}
			}
			return double.Parse(sb.ToString());

		}
		/// <summary>
		/// Attempts to read the specified literal from the input, optionally checking if it is terminated
		/// </summary>
		/// <param name="literal">The literal to attempt to read</param>
		/// <param name="checkTerminated">If true, will check the end to make sure it's not a letter or digit</param>
		/// <returns></returns>
		public bool TryReadLiteral(string literal, bool checkTerminated = true)
		{
			foreach (char ch in literal)
			{
				if (Current == ch)
				{
					CaptureCurrent();
					if (-1 == Advance())
						break;
				}
			}
			if (checkTerminated)
				return -1 == Current || !char.IsLetterOrDigit((char)Current);
			return true;
		}
		/// <summary>
		/// Attempts to skip the specified literal without capturing, optionally checking for termination
		/// </summary>
		/// <param name="literal">The literal to skip</param>
		/// <param name="checkTerminated">True if the literal should be checked for termination by a charcter other than a letter or digit, otherwise false</param>
		/// <returns>True if the literal was found and skipped, otherwise false</returns>
		public bool TrySkipLiteral(string literal, bool checkTerminated = true)
		{
			foreach (char ch in literal)
			{
				if (Current == ch)
				{
					if (-1 == Advance())
						break;
				}
			}
			if (checkTerminated)
				return -1 == Current || !char.IsLetterOrDigit((char)Current);
			return true;
		}
		/// <summary>
		/// Attempts to read a C style line comment into the capture buffer
		/// </summary>
		/// <returns>True if a valid comment was read, otherwise false</returns>
		public bool TryReadCLineComment()
		{
			EnsureStarted();
			if ('/' != Current)
				return false;
			CaptureCurrent();
			if ('/' != Advance())
				return false;
			CaptureCurrent();
			while (-1 != Advance() && '\r' != Current && '\n' != Current)
				CaptureCurrent();
			return true;
		}
		/// <summary>
		/// Attempts to skip the a C style line comment without capturing
		/// </summary>
		/// <returns>True if a comment was found and skipped, otherwise false</returns>
		public bool TrySkipCLineComment()
		{
			EnsureStarted();
			if ('/' != Current)
				return false;
			if ('/' != Advance())
				return false;
			while (-1 != Advance() && '\r' != Current && '\n' != Current) ;
			return true;
		}
		/// <summary>
		/// Attempts to read a C style block comment into the capture buffer
		/// </summary>
		/// <returns>True if a valid comment was read, otherwise false</returns>
		public bool TryReadCBlockComment()
		{
			EnsureStarted();
			if ('/' != Current)
				return false;
			CaptureCurrent();
			if ('*' != Advance())
				return false;
			CaptureCurrent();
			if (-1 == Advance())
				return false;
			return TryReadUntil("*/");
		}
		/// <summary>
		/// Attempts to skip the C style block comment without capturing
		/// </summary>
		/// <returns>True if a comment was found and skipped, otherwise false</returns>
		public bool TrySkipCBlockComment()
		{
			EnsureStarted();
			if ('/' != Current)
				return false;
			if ('*' != Advance())
				return false;
			if (-1 == Advance())
				return false;
			return TrySkipUntil("*/");
		}
		/// <summary>
		/// Attempts to read a C style comment into the capture buffer
		/// </summary>
		/// <returns>True if a valid comment was read, otherwise false</returns>
		public bool TryReadCComment()
		{
			EnsureStarted();
			if ('/' != Current)
				return false;
			CaptureCurrent();
			if ('*' == Advance())
			{
				CaptureCurrent();
				if (-1 == Advance())
					return false;
				return TryReadUntil("*/");
			}
			if ('/' == Current)
			{
				CaptureCurrent();
				while (-1 != Advance() && '\r' != Current && '\n' != Current)
					CaptureCurrent();
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to skip the a C style comment value without capturing
		/// </summary>
		/// <returns>True if a comment was found and skipped, otherwise false</returns>
		public bool TrySkipCComment()
		{
			EnsureStarted();
			if ('/' != Current)
				return false;
			if ('*' == Advance())
			{
				if (-1 == Advance())
					return false;
				return TrySkipUntil("*/");
			}
			if ('/' == Current)
			{
				while (-1 != Advance() && '\r' != Current && '\n' != Current) ;
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to read C style comments or whitespace into the capture buffer
		/// </summary>
		/// <returns>True if a valid comment or whitespace was read, otherwise false</returns>
		public bool TryReadCCommentsAndWhitespace()
		{
			bool result = false;
			while (-1 != Current)
			{
				if (!TryReadWhiteSpace() && !TryReadCComment())
					break;
				result = true;
			}
			if (TryReadWhiteSpace())
				result = true;
			return result;
		}
		/// <summary>
		/// Attempts to skip the a C style comment or whitespace value without capturing
		/// </summary>
		/// <returns>True if a comment or whitespace was found and skipped, otherwise false</returns>
		public bool TrySkipCCommentsAndWhiteSpace()
		{
			bool result = false;
			while (-1 != Current)
			{
				if (!TrySkipWhiteSpace() && !TrySkipCComment())
					break;
				result = true;
			}
			if (TrySkipWhiteSpace())
				result = true;
			return result;
		}
		/// <summary>
		/// Attempts to read a C style identifier into the capture buffer
		/// </summary>
		/// <returns>True if a valid identifier was read, otherwise false</returns>
		public bool TryReadCIdentifier()
		{
			EnsureStarted();
			if (-1 == Current || !('_' == Current || char.IsLetter((char)Current)))
				return false;
			CaptureCurrent();
			while (-1 != Advance() && ('_' == Current || char.IsLetterOrDigit((char)Current)))
				CaptureCurrent();
			return true;
		}
		/// <summary>
		/// Attempts to skip the a C style identifier value without capturing
		/// </summary>
		/// <returns>True if an identifier was found and skipped, otherwise false</returns>
		public bool TrySkipCIdentifier()
		{
			EnsureStarted();
			if (-1 == Current || !('_' == Current || char.IsLetter((char)Current)))
				return false;
			while (-1 != Advance() && ('_' == Current || char.IsLetterOrDigit((char)Current))) ;
			return true;
		}
		/// <summary>
		/// Attempts to read a C style string into the capture buffer
		/// </summary>
		/// <returns>True if a valid string was read, otherwise false</returns>
		public bool TryReadCString()
		{
			EnsureStarted();
			if ('\"' != Current)
				return false;
			CaptureCurrent();
			while (-1 != Advance() && '\r' != Current && '\n' != Current && '\"' != Current)
			{
				CaptureCurrent();
				if ('\\' == Current)
				{
					if (-1 == Advance() || '\r' == Current || '\n' == Current)
						return false;
					CaptureCurrent();

				}
			}
			if ('\"' == Current)
			{
				CaptureCurrent();
				Advance(); // move past the string
				return true;
			}
			return false;
		}
		/// <summary>
		/// Attempts to skip a C style string literal without capturing
		/// </summary>
		/// <returns>True if a literal was found and skipped, otherwise false</returns>
		public bool TrySkipCString()
		{
			EnsureStarted();
			if ('\"' != Current)
				return false;
			while (-1 != Advance() && '\r' != Current && '\n' != Current && '\"' != Current)
				if ('\\' == Current)
					if (-1 == Advance() || '\r' == Current || '\n' == Current)
						return false;

			if ('\"' == Current)
			{
				Advance(); // move past the string
				return true;
			}
			return false;
		}
	}
}
