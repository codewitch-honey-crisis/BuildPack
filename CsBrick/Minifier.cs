using PC;
using Grimoire;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System;

namespace CSBrick
{
	class Minifier
	{
		public static void MergeMinify(TextWriter writer, int lineWidth = 0, bool defineFiles = false, params string[] sourcePaths)
		{
			MergeMinifyPreamble(writer, defineFiles, sourcePaths);
			MergeMinifyBody(writer, lineWidth, sourcePaths);
		}
		public static void MergeMinifyPreamble(TextWriter writer, bool defineFiles = false, params string[] sourcePaths)
		{
			var usings = new HashSet<string>();
			var defines = new HashSet<string>();
			foreach (string fn in sourcePaths)
			{
				using (var pc = ParseContext.CreateFrom(new StreamReader(File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.Read))))
				{
					if (defineFiles)
					{
						defines.Add(string.Join("_", StringUtility.SplitWords(Path.GetFileName(fn).ToUpperInvariant())));
					}
					pc.EnsureStarted();
					while (-1 != pc.Current)
					{
						if (!pc.TrySkipWhiteSpace() && !pc.TrySkipCComment())
							break;
					}
					pc.TrySkipWhiteSpace();
					// gather defines
					while ('#' == pc.Current)
					{
						if (-1 == pc.Advance())
							break;
						pc.ClearCapture();
						if (!pc.TryReadCIdentifier())
							break;
						if ("define" != pc.GetCapture())
							break;
						if (!pc.TrySkipCCommentsAndWhiteSpace())
							break;
						pc.ClearCapture();
						if (!pc.TryReadCIdentifier())
							break;
						defines.Add(pc.GetCapture());
						pc.ClearCapture();
						if (!pc.TrySkipCCommentsAndWhiteSpace())
							break;
					}
					pc.ClearCapture();
					// gather usings
					while ('u' == pc.Current)
					{
						pc.TryReadCIdentifier();
						if ("using" != pc.GetCapture())
							break;
						if (!pc.TrySkipCCommentsAndWhiteSpace())
							break;
						pc.ClearCapture();
						while (pc.TryReadCIdentifier())
						{
							pc.TrySkipCCommentsAndWhiteSpace();
							if ('.' != pc.Current)
								break;
							pc.CaptureCurrent();
							if (-1 == pc.Advance())
								break;
							pc.TrySkipCCommentsAndWhiteSpace();
						}
						if ('=' == pc.Current)
							throw new NotSupportedException("Cannot currently support using aliases in the root namepace. Nest them instead.");
						if (';' != pc.Current)
							break;
						usings.Add(pc.GetCapture());
						pc.ClearCapture();
						pc.Advance();
						pc.TrySkipCCommentsAndWhiteSpace();
					}
				}
			}
			foreach (string def in defines)
			{
				writer.Write("#define ");
				writer.WriteLine(def);
			}
			foreach (string use in usings)
			{
				writer.Write("using ");
				writer.Write(use);
				writer.WriteLine(";");
			}
		}

		public static void MergeMinifyBody(TextWriter writer, int lineWidth = 0, params string[] sourcePaths)
		{
			int ocol = 0;
			foreach (string fn in sourcePaths)
			{
				using (var pc = ParseContext.CreateFrom(new StreamReader(File.Open(fn, FileMode.Open, FileAccess.Read, FileShare.Read))))
				{
					pc.EnsureStarted();
					while (-1 != pc.Current)
					{
						if (!pc.TrySkipWhiteSpace() && !pc.TrySkipCComment())
							break;
					}
					pc.TrySkipWhiteSpace();
					// gather defines
					while ('#' == pc.Current)
					{
						if (-1 == pc.Advance())
							break;
						pc.ClearCapture();
						if (!pc.TryReadCIdentifier())
							break;
						if ("define" != pc.GetCapture())
							break;
						if (!pc.TrySkipCCommentsAndWhiteSpace())
							break;
						pc.ClearCapture();
						if (!pc.TryReadCIdentifier())
							break;
						pc.ClearCapture();
						if (!pc.TrySkipCCommentsAndWhiteSpace())
							break;
					}
					pc.ClearCapture();
					// gather usings
					while ('u' == pc.Current)
					{
						pc.TryReadCIdentifier();
						if ("using" != pc.GetCapture())
							break;
						if (!pc.TrySkipCCommentsAndWhiteSpace())
							break;
						pc.ClearCapture();
						while (pc.TryReadCIdentifier())
						{
							pc.TrySkipCCommentsAndWhiteSpace();
							if ('.' != pc.Current)
								break;
							pc.CaptureCurrent();
							if (-1 == pc.Advance())
								break;
							pc.TrySkipCCommentsAndWhiteSpace();
						}
						if ('=' == pc.Current)
							throw new NotSupportedException("Cannot currently support using aliases in the root namepace. Nest them instead.");
						if (';' != pc.Current)
							break;
						pc.ClearCapture();
						pc.Advance();
						pc.TrySkipCCommentsAndWhiteSpace();
					}
					bool isIdentOrNum = false;
					// done skipping preamble
					while (-1 != pc.Current)
					{
						pc.TrySkipWhiteSpace();
						pc.ClearCapture();
						switch (pc.Current)
						{
							case '#':
								isIdentOrNum = false;
								pc.TryReadUntil(false, '\r', '\n');
								if (0 != ocol)
									writer.WriteLine();
								writer.WriteLine(pc.GetCapture());
								ocol = 0;
								break;
							case '/':
								isIdentOrNum = false;
								if (!pc.TryReadCComment())
								{
									writer.Write(pc.GetCapture());
									ocol += pc.CaptureBuffer.Length;

									break;
								}
								if (pc.GetCapture().StartsWith("///"))
								{ // doc comment
									writer.WriteLine(pc.GetCapture());
									ocol = 0;
								}
								else
									isIdentOrNum = true; // force a space
								break;
							case '@':
								isIdentOrNum = true;
								pc.TryReadCSharpStringOrIdentifier();
								writer.Write(pc.GetCapture());
								ocol += pc.CaptureBuffer.Length;
								break;
							case '$':
								isIdentOrNum = false;
								pc.CaptureCurrent();
								if ('\"' == pc.Advance())
									pc.TryReadCSharpString();
								writer.Write(pc.GetCapture());
								ocol += pc.CaptureBuffer.Length;
								break;
							case '\"':
								isIdentOrNum = false;
								pc.TryReadCSharpString();
								writer.Write(pc.GetCapture());
								ocol += pc.CaptureBuffer.Length;
								break;
							case '\'':
								isIdentOrNum = false;
								pc.TryReadCSharpChar();
								writer.Write(pc.GetCapture());
								ocol += pc.CaptureBuffer.Length;
								break;
							case -1:
								isIdentOrNum = false;
								break;
							default:
								pc.ClearCapture();
								if (pc.TryReadCIdentifier())
								{
									if (isIdentOrNum)
									{
										writer.Write(' ');
										++ocol;
									}
									var s = pc.GetCapture();
									isIdentOrNum = true;

									writer.Write(s);
									ocol += pc.CaptureBuffer.Length;
								}
								else
								{
									if (0 > pc.CaptureBuffer.Length && isIdentOrNum && char.IsDigit(pc.GetCapture(0, 1)[0]))
									{
										writer.Write(' ');
										++ocol;
									}

									writer.Write(pc.GetCapture());
									ocol += pc.CaptureBuffer.Length;
									if (isIdentOrNum && char.IsDigit((char)pc.Current))
									{
										++ocol;
										writer.Write(' ');
									}
									isIdentOrNum = false;
									writer.Write((char)pc.Current);
									++ocol;
									pc.Advance();
								}
								break;

						}
						if (-1 != pc.Current && char.IsWhiteSpace((char)pc.Current) && lineWidth > 0 && ocol >= lineWidth)
						{
							writer.WriteLine();
							ocol = 0;
						}
					}
				}
			}
		}
	}
}
