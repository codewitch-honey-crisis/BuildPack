// handles parsing compile units (whole C# files)
using System;
using System.Collections.Generic;
using System.CodeDom;
using System.IO;

namespace CD
{
	using ST = SlangTokenizer;
	partial class SlangParser
	{
		/// <summary>
		/// Reads a <see cref="CodeCompileUnit"/> from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
		public static CodeCompileUnit ReadCompileUnitFrom(TextReader reader)
			=> ParseCompileUnit(TextReaderEnumerable.FromReader(reader));
		/// <summary>
		/// Reads a <see cref="CodeCompileUnit"/> from the specified file
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
		public static CodeCompileUnit ReadCompileUnitFrom(string filename)
			=> ParseCompileUnit(new FileReaderEnumerable(filename));
		/// <summary>
		/// Reads a <see cref="CodeCompileUnit"/> from the specified URL
		/// </summary>
		/// <param name="url">The URL to read</param>
		/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
		public static CodeCompileUnit ReadCompileUnitFromUrl(string url)
			=> ParseCompileUnit(new UrlReaderEnumerable(url));
		/// <summary>
		/// Parses a <see cref="CodeCompileUnit"/> from the specified input
		/// </summary>
		/// <param name="input">The input to parse</param>
		/// <returns>A <see cref="CodeCompileUnit"/> representing the parsed code</returns>
		public static CodeCompileUnit ParseCompileUnit(IEnumerable<char> input)
		{
			using (var e = new ST(input).GetEnumerator())
			{
				var pc = new _PC(e);
				pc.EnsureStarted();
				var result = _ParseCompileUnit(pc);
				// should never happen
				if (!pc.IsEnded)
					throw new ArgumentException("Unrecognized remainder in namespace", "input");
				return result;
			}
		}
		static CodeCompileUnit _ParseCompileUnit(_PC pc)
		{
			var result = new CodeCompileUnit();
			while(!pc.IsEnded)
				result.Namespaces.Add(_ParseNamespace(pc));
			return result;
		}
	}
}
