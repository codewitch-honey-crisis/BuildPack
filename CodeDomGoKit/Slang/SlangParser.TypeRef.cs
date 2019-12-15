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
		/// Reads a <see cref="CodeTypeReference"/> from the specified <see cref="TextReader"/>
		/// </summary>
		/// <param name="reader">The reader to read from</param>
		/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
		public static CodeTypeReference ReadTypeRefFrom(TextReader reader)
			=> ParseTypeRef(TextReaderEnumerable.FromReader(reader));
		/// <summary>
		/// Reads a <see cref="CodeTypeReference"/> from the specified file
		/// </summary>
		/// <param name="filename">The file to read</param>
		/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
		public static CodeTypeReference ReadTypeRefFrom(string filename)
			=> ParseTypeRef(new FileReaderEnumerable(filename));
		/// <summary>
		/// Reads a <see cref="CodeTypeReference"/> from the specified URL
		/// </summary>
		/// <param name="url">The URL to read</param>
		/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
		public static CodeTypeReference ReadTypeRefFromUrl(string url)
			=> ParseTypeRef(new UrlReaderEnumerable(url));
		/// <summary>
		/// Parses a <see cref="CodeTypeReference"/> from the specified input
		/// </summary>
		/// <param name="input">The input to parse</param>
		/// <returns>A <see cref="CodeTypeReference"/> representing the parsed code</returns>
		public static CodeTypeReference ParseTypeRef(IEnumerable<char> input)
		{
			using (var e = new ST(input).GetEnumerator())
			{
				var pc = new _PC(e);
				pc.EnsureStarted();
				var result = _ParseTypeRef(pc);
				if (!pc.IsEnded)
					throw new SlangSyntaxException("Unrecognized remainder in type reference", pc.Current.Line, pc.Current.Column, pc.Current.Position);
				return result;
			}
		}
		static CodeTypeReference _ParseTypeGenerics(_PC pc, CodeTypeReference result = null)
		{
			_SkipComments(pc);
			if (null == result)
				result = new CodeTypeReference();
			//result.UserData["slang:unresolved"] = true;
			while (ST.gt != pc.SymbolId)
			{
				_SkipComments(pc);
				if (!pc.Advance())
					_Error("Unterminated generic specification", pc.Current);
				_SkipComments(pc);
				var tp = _ParseTypeRef(pc);
				tp.Options = CodeTypeReferenceOptions.GenericTypeParameter;
				result.TypeArguments.Add(tp);
				if (ST.gt != pc.SymbolId && ST.comma != pc.SymbolId)
					_Error("Invalid token in generic specification", pc.Current);
			}
			if (ST.gt != pc.SymbolId)
				_Error("Unterminated generic specification", pc.Current);
			_SkipComments(pc);
			pc.Advance();
			return result;
		}
		static CodeTypeReference _ParseTypeRef(_PC pc, bool notArrayPart = false,bool once=false)
		{
			_SkipComments(pc);
			if (pc.IsEnded)
				_Error("Expecting a type reference", pc.Current);
			_PC pc2; // for lookahead
			var isIntrinsic = false;
			var result = new CodeTypeReference();
			var first = true;
			while (!pc.IsEnded)
			{
				var s = pc.Value;

				if (first)
				{
					if (ST.keyword == pc.SymbolId)
					{
						s = _TranslateIntrinsicType(s,pc);
						isIntrinsic = true;
					}
					else if (ST.identifier != pc.SymbolId)
						_Error("An identifier was expected", pc.Current);
					result.BaseType = s;
				}
				else
				{
					if (ST.identifier != pc.SymbolId)
						_Error("An identifier was expected", pc.Current);
					result.BaseType=string.Concat(result.BaseType, "+", s);
				}
				pc.Advance();
				_SkipComments(pc);
				if(pc.IsEnded)
					return result;
				if (!first || !isIntrinsic)
				{
					_SkipComments(pc);
					while (ST.dot == pc.SymbolId)
					{
						// this might be a nested type but we can't know that yet
						result.UserData["slang:unresolved"] = true;
						var bt = string.Concat(result.BaseType, ".");
						pc2 = pc.GetLookAhead();
						pc2.EnsureStarted();
						pc2.Advance();
						_SkipComments(pc2);
						if (ST.identifier != pc2.SymbolId)
							return result;
						pc.Advance();
						_SkipComments(pc);
						result.BaseType = string.Concat(bt, pc.Value);
						if (!pc.Advance())
							return result;

					}
				}
				if (ST.lt == pc.SymbolId) // generic type parameters follow
				{
					var c = result.TypeArguments.Count;
					result = _ParseTypeGenerics(pc, result);
					// HACK: Microsoft's CodeTypeReference object doesn't handle nested type references properly
					// in the case of generics so we have to fix it up. We just manually add the type argument count
					// to the end of a nested type, since it's not done in the CodeTypeReference internals like it 
					// is supposed to be. It works properly however, on the outermost type (first=true) so we don't
					// touch it until we start nesting
					// TODO: Frankly, if they ever fix this (they won't) it will break this code (which is why they won't)
					// but we could check here to see if it has been fixed before we "operate". I didn't bother, but
					// it would make things more robust in the case of Microsoft ever fixing anything.
					if(!first && result.TypeArguments.Count>c)
						result.BaseType = string.Concat(result.BaseType, "`", result.TypeArguments.Count - c);
					
				}
				_SkipComments(pc);
				if (!notArrayPart)
				{
					if (ST.lbracket == pc.SymbolId)
						result = _ParseArrayTypeModifiers(result, pc);
					_SkipComments(pc);
				}
				if (once || ST.dot != pc.SymbolId)
					break;
				pc2 = pc.GetLookAhead();
				pc2.EnsureStarted();
				pc2.Advance();
				_SkipComments(pc2);
				if (ST.identifier != pc2.SymbolId)
					return result;
				pc.Advance();
				_SkipComments(pc);
				if (pc.IsEnded)
					_Error("Unterminated type reference", pc.Current);
				first = false;
			}
			return result;
		}
		
		static CodeTypeReference _ParseArrayTypeModifiers(CodeTypeReference type, _PC pc)
		{
			
			var mods = new List<int>();
			var result = type;
			_SkipComments(pc);
			var t = pc.Current;
			var ai = 1;
			var inBrace = true;
			while (pc.Advance())
			{
				_SkipComments(pc);
				t = pc.Current;
				if (inBrace && ST.comma == t.SymbolId)
				{
					
					++ai;
					continue;
				}
				else if (ST.rbracket == t.SymbolId)
				{

					mods.Add(ai);
					ai = 1;
					if (!pc.Advance())
						break;
					inBrace = false;
					if (ST.lbracket != pc.SymbolId)
						break;
					else
						inBrace = true;
				}
				else
					break;
			}
			for (var i = mods.Count - 1; -1 < i; --i)
			{
				var ctr = new CodeTypeReference();
				ctr.ArrayElementType = result;
				ctr.ArrayRank = mods[i];
				result = ctr;
			}
			return result;
		}
		static string _TranslateIntrinsicType(string s,_PC pc)
		{
			switch (s)
			{
				case "bool":
					s = typeof(bool).FullName;
					break;
				case "char":
					s = typeof(char).FullName;
					break;
				case "string":
					s = typeof(string).FullName;
					break;
				case "object":
					s = typeof(object).FullName;
					break;
				case "byte":
					s = typeof(byte).FullName;
					break;
				case "sbyte":
					s = typeof(sbyte).FullName;
					break;
				case "short":
					s = typeof(short).FullName;
					break;
				case "ushort":
					s = typeof(ushort).FullName;
					break;
				case "int":
					s = typeof(int).FullName;
					break;
				case "uint":
					s = typeof(uint).FullName;
					break;
				case "long":
					s = typeof(long).FullName;
					break;
				case "ulong":
					s = typeof(ulong).FullName;
					break;
				case "float":
					s = typeof(float).FullName;
					break;
				case "double":
					s = typeof(double).FullName;
					break;
				case "decimal":
					s = typeof(decimal).FullName;
					break;
				default:
					_Error(string.Format("Type expected but found {0}", s), pc.Current);
					break;
			}

			return s;
		}
	}
}
