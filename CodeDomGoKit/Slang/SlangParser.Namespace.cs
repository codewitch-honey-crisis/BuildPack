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
		/// Reads a <see cref="CodeNamespace"/> from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
		public static CodeNamespace ReadNamespaceFrom(TextReader reader)
			=> ParseNamespace(TextReaderEnumerable.FromReader(reader));
		/// <summary>
		/// Reads a <see cref="CodeNamespace"/> from the specified file
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
		public static CodeNamespace ReadNamespaceFrom(string filename)
			=> ParseNamespace(new FileReaderEnumerable(filename));
		/// <summary>
		/// Reads a <see cref="CodeNamespace"/> from the specified URL
		/// </summary>
		/// <param name="url">The URL to read</param>
		/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
		public static CodeNamespace ReadNamespaceFromUrl(string url)
			=> ParseNamespace(new UrlReaderEnumerable(url));
		/// <summary>
		/// Parses a <see cref="CodeNamespace"/> from the specified input
		/// </summary>
		/// <param name="input">The input to parse</param>
		/// <returns>A <see cref="CodeNamespace"/> representing the parsed code</returns>
		public static CodeNamespace ParseNamespace(IEnumerable<char> input)
		{
			using (var e = new ST(input).GetEnumerator())
			{
				var pc = new _PC(e);
				pc.EnsureStarted();
				var result = _ParseNamespace(pc);
				if (!pc.IsEnded)
					throw new ArgumentException("Unrecognized remainder in namespace", "input");
				return result;
			}
		}
		static CodeNamespace _ParseNamespace(_PC pc)
		{
			// this doesn't have to be an actual namespace declaration
			// it can just be a series of types, in which case Name
			// is null
			var result = new CodeNamespace();
			if (ST.keyword == pc.SymbolId && "namespace" == pc.Value)
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated namespace declaration", "input");
				if (ST.identifier != pc.SymbolId)
					throw new ArgumentException("Expected identifier in namespace declaration");
				result.Name = _ParseNamespaceName(pc);
				if (ST.lbrace != pc.SymbolId)
					throw new ArgumentException("Expecting { in namespace declaration");
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated namespace declaration", "input");
				foreach (CodeNamespaceImport nsi in _ParseNamespaceImports(pc))
					result.Imports.Add(nsi);
				while (ST.rbrace != pc.SymbolId)
					result.Types.Add(_ParseType(pc));
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated namespace declaration", "input");
				if (ST.rbrace != pc.SymbolId)
					throw new ArgumentException("Invalid type declaration in namespace", "input");
				pc.Advance();
				return result;
			}
			foreach (CodeNamespaceImport nsi in _ParseNamespaceImports(pc))
				result.Imports.Add(nsi);
			_SkipComments(pc);
			while (!pc.IsEnded && !(ST.keyword==pc.SymbolId && "namespace"==pc.Value))
				result.Types.Add(_ParseType(pc));

			return result;
		}
		static CodeNamespaceImportCollection _ParseNamespaceImports(_PC pc)
		{
			var result = new CodeNamespaceImportCollection();
			while (ST.keyword == pc.SymbolId && "using" == pc.Value)
			{
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated using declaration", "input");
				if (ST.identifier != pc.SymbolId)
					throw new ArgumentException("Expecting identifier in using declaration", "input");
				var ns = _ParseNamespaceName(pc);
				if (pc.IsEnded)
					throw new ArgumentException("Unterminated using declaration", "input");
				if (ST.semi != pc.SymbolId)
					throw new ArgumentException("Expecting ; in using declaration", "input");
				pc.Advance();
				_SkipComments(pc);
				result.Add(new CodeNamespaceImport(ns));
			}
			return result;
		}
		static string _ParseNamespaceName(_PC pc)
		{
			var result = "";
			while(ST.identifier==pc.SymbolId)
			{
				if (0 < result.Length)
					result = string.Concat(result, ".", pc.Value);
				else
					result = pc.Value;
				pc.Advance();
				_SkipComments(pc);
				if (ST.dot == pc.SymbolId)
				{
					pc.Advance();
					_SkipComments(pc);
				}
			}
			return result;
		}
	}
}
