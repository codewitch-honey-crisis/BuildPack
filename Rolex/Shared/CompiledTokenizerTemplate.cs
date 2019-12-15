using System;
using System.Collections.Generic;

namespace Rolex
{
	class CompiledTokenizerTemplate : CompiledTokenizer
	{
		public override IEnumerator<Token> GetEnumerator()
		{
			return new CompiledTokenizerEnumeratorTemplate(Input.GetEnumerator());
		}
		public CompiledTokenizerTemplate(IEnumerable<char> input) : base(input)
		{

		}
	}
	class CompiledTokenizerEnumeratorTemplate : CompiledTokenizerEnumerator
	{
		public CompiledTokenizerEnumeratorTemplate(IEnumerator<char> input) : base(input)
		{

		}
	}
}
