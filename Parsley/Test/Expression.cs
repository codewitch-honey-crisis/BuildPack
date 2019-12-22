namespace ParsleyDemo
{
using System;
using System.Collections.Generic;
using System.IO;
	
	internal partial class ExpressionTokenizer : IEnumerable<Token>
	{
		// symbol constants follow
public const int ErrorSymbol = -1;
public const int EosSymbol = -2;
public const int add = 12;
public const int mul = 18;
public const int integer = 14;
public const int identifier = 15;
public const int whitespace = 20;

		string _fallbackCodePage;
		Stream _input;
		bool _called; // we can only return an enumerator once if there is no backing file
		public ExpressionTokenizer(Stream input, string fallbackCodePage = null)
		{
			if (null == input)
				throw new ArgumentNullException(nameof(input));
			if (null == fallbackCodePage)
				fallbackCodePage = "default";
			_fallbackCodePage = fallbackCodePage;
			_input = input;
			_called = false;
		}
		public IEnumerator<Token> GetEnumerator()
		{
			if (_called)
			{
				throw new NotSupportedException("A stream cannot support multiple cursors. A new enumerator cannot be returned.");
			}
			var result = new ExpressionTokenizerEnumerator(_input, _fallbackCodePage);
			_called = true;
			return result;
		}
		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
			=> GetEnumerator();
	}
	internal partial class ExpressionTokenizerEnumerator : IEnumerator<Token>
	{
		const int _Error = -1;// scan error
		const int _Eos = -2; // end of input
		const int _Disposed = -3; // Dispose() has been called
									// -4 or any other neg value other than above = _Initial
									// non-negative (0) = _Enumerating

		Scanner _outer;
		public ExpressionTokenizerEnumerator(Stream input, string fallbackCodePage)
		{
			_outer = new Scanner(input, fallbackCodePage);
		}
		Token IEnumerator<Token>.Current => _GetCurrent();
		object System.Collections.IEnumerator.Current => _GetCurrent();
		Token _GetCurrent()
		{
			var cur = _outer.Current;
			#region Error Handling
			if (0 > cur.SymbolId)
			{
				switch (cur.SymbolId)
				{
					case _Disposed:
						throw new ObjectDisposedException(GetType().Name);
					case _Eos:
						throw new InvalidOperationException("The cursor is after the end of the enumeration");
					case _Error: // we need to report this so just break
						break;
					default: // initial state
						throw new InvalidOperationException("The cursor is before the start of the enumeration.");
				}
			}
			#endregion Error Handling
			return cur;
		}
		bool System.Collections.IEnumerator.MoveNext()
		{
			var cur = _outer.Current;
			if (_Disposed == cur.SymbolId)
				throw new ObjectDisposedException(GetType().Name);
			if (_Eos == cur.SymbolId)
				return false;
			_outer.Advance();
			return _Eos != _outer.Current.SymbolId;	
		}
		void System.Collections.IEnumerator.Reset()
		{
			throw new NotSupportedException("Gplex tokenizers cannot be reset.");
		}
		void IDisposable.Dispose()
		{
			_outer.Close();
		}
	}

}
