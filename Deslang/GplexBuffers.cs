using System;
using System.IO;
using System.Text;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Diagnostics.CodeAnalysis;

[Serializable]
public class BufferException : Exception
{
    public BufferException() { }
    public BufferException(string message) : base(message) { }
    public BufferException(string message, Exception innerException)
        : base(message, innerException) { }
    protected BufferException(SerializationInfo info, StreamingContext context)
        : base(info, context) { }
}

public abstract class ScanBuff
{
    public string FileName;

    public const int EndOfFile = -1;
    public const int UnicodeReplacementChar = 0xFFFD;

    public bool IsFile { get { return (FileName != null); } }
    
    public abstract int Pos { get; set; }
    public abstract int Read();
    public virtual void Mark() { }

    public abstract string GetString(int begin, int limit);

    public static ScanBuff GetBuffer(IEnumerator<char> source)
    {
        return new BuildBuffer(source);
    }
    public static ScanBuff GetBuffer(TextReader source)
    {
        return new BuildBuffer(source);
    }

    public static ScanBuff GetBuffer(Stream source)
    {
        return new BuildBuffer(source);
    }

    public static ScanBuff GetBuffer(Stream source, int fallbackCodePage)
    {
        return new BuildBuffer(source, fallbackCodePage);
    }
}

#region Buffer classes

// ==============================================================
// =====     class BuildBuff : for unicode text files    ========
// ==============================================================
// Double buffer for char stream.
class BufferElement
{
    StringBuilder bldr = new StringBuilder();
    StringBuilder next = new StringBuilder();
    int minIx;
    int maxIx;
    int brkIx;
    bool appendToNext;

    internal BufferElement() { }

    internal int MaxIndex { get { return maxIx; } }
    // internal int MinIndex { get { return minIx; } }

    internal char this[int index] {
        get {
            if (index < minIx || index >= maxIx)
                throw new BufferException("Index was outside data buffer");
            else if (index < brkIx)
                return bldr[index - minIx];
            else
                return next[index - brkIx];
        }
    }

    internal void Append(char[] block, int count)
    {
        maxIx += count;
        if (appendToNext)
            this.next.Append(block, 0, count);
        else
        {
            this.bldr.Append(block, 0, count);
            brkIx = maxIx;
            appendToNext = true;
        }
    }

    internal string GetString(int start, int limit)
    {
        if (limit <= start)
            return "";
        if (start >= minIx && limit <= maxIx)
            if (limit < brkIx) // String entirely in bldr builder
                return bldr.ToString(start - minIx, limit - start);
            else if (start >= brkIx) // String entirely in next builder
                return next.ToString(start - brkIx, limit - start);
            else // Must do a string-concatenation
                return
                    bldr.ToString(start - minIx, brkIx - start) +
                    next.ToString(0, limit - brkIx);
        else
            throw new BufferException("String was outside data buffer");
    }

    internal void Mark(int limit)
    {
        if (limit > brkIx + 16) // Rotate blocks
        {
            StringBuilder temp = bldr;
            bldr = next;
            next = temp;
            next.Length = 0;
            minIx = brkIx;
            brkIx = maxIx;
        }
    }
}


class BuildBuffer : ScanBuff
{
    TextReader _reader;
    Stream _stream;
    int _fallbackCodePage = -1;
    Encoding _encoding;
    IEnumerator<char> _enum;
    bool _first = true;
    BufferElement data = new BufferElement();

    int bPos;            // Postion index in the StringBuilder

    static int _Preamble(Stream stream)
    {
        int b0 = stream.ReadByte();
        int b1 = stream.ReadByte();

        if (b0 == 0xfe && b1 == 0xff)
            return 1201; // UTF16BE
        if (b0 == 0xff && b1 == 0xfe)
            return 1200; // UTF16LE

        int b2 = stream.ReadByte();
        if ((b0 == 0xef && b1 == 0xbb) && b2 == 0xbf)
            return 65001; // UTF8
                          //
                          // There is no unicode preamble, so we
                          // return denoter for the machine default.
                          //
        stream.Seek(0, SeekOrigin.Begin);
        return 0;
    }
    public int ReadNextBlk(char[] buffer, int index, int count)
    {

        if (null != _stream)
        {
            if (_first)
            {
                _first = false;

                int preamble = _Preamble(_stream);

                if (preamble != 0)  // There is a valid BOM here!
                    _encoding = Encoding.GetEncoding(preamble);
                else if (_fallbackCodePage == -1) // Fallback is "raw" bytes
                {
                    byte[] b = new byte[count];
                    int c = _stream.Read(b, 0, count);
                    int i = 0;
                    int j = index;
                    for (; i < c; ++i)
                    {
                        buffer[j] = (char)b[i];
                        ++j;
                    }
                    return c;
                }
                else if (_fallbackCodePage != -2) // Anything but "guess"
                    _encoding = Encoding.GetEncoding(_fallbackCodePage);
                else // This is the "guess" option
                {
                    int guess = new Guesser(_stream).GuessCodePage();
                    _stream.Seek(0, SeekOrigin.Begin);
                    if (guess == -1) // ==> this is a 7-bit file
                        _encoding = Encoding.ASCII;
                    else if (guess == 65001)
                        _encoding = Encoding.UTF8;
                    else             // ==> use the machine default
                        _encoding = Encoding.Default;
                }

            }
            StreamReader reader;
            if (null != _encoding)
                reader = new StreamReader(_stream, _encoding);
            else
                reader = new StreamReader(_stream);
            return reader.Read(buffer, index, count);
        }
        else if (null != _reader)
        {
            _first = false;
            return _reader.Read(buffer, index, count);
        }
        else if (null != _enum)
        {
            int i = count;
            count = 0;
            while (i > 0 && _enum.MoveNext())
            {
                buffer[index] = _enum.Current;
                ++index;
                ++count;
                --i;
            }
            return count;
        }
        // should never get here
        throw new NotImplementedException();
    }

    private string EncodingName {
        get {
            if (null != _encoding)
                return _encoding.BodyName;
            if (_stream != null)
                return "raw-bytes";
            if (null != _enum)
                return "UTF-16";
            else if (null != _reader)
            {
                try
                {
                    var sr = (StreamReader)_reader;
                    return sr.CurrentEncoding.BodyName;
                }
                catch (InvalidCastException) { }
            }
            return "UTF-16";
        }
    }

    public BuildBuffer(Stream stream)
    {
        try
        {
            FileStream fStrm = (FileStream)stream;
            FileName = fStrm.Name;
        }
        catch (InvalidCastException) { }
        _stream = stream;
        _fallbackCodePage = -1;
    }
    public BuildBuffer(IEnumerator<char> input)
    {
        FileName = null;
        _enum = input;
    }
    public BuildBuffer(TextReader reader)
    {
        try
        {
            var sr = (StreamReader)reader;
            if (null != sr)
            {
                FileStream fStrm = (FileStream)sr.BaseStream;
                FileName = fStrm.Name;
            }
        }
        catch (InvalidCastException) { }
        _reader = reader;
    }
    public BuildBuffer(Stream stream, int fallbackCodePage)
    {
        try
        {
            FileStream fStrm = (FileStream)stream;
            FileName = fStrm.Name;
        }
        catch (InvalidCastException) { }

        _stream = stream;
        _fallbackCodePage = fallbackCodePage;
    }

    /// <summary>
    /// Marks a conservative lower bound for the buffer,
    /// allowing space to be reclaimed.  If an application 
    /// needs to call GetString at arbitrary past locations 
    /// in the input stream, Mark() is not called.
    /// </summary>
    public override void Mark() { data.Mark(bPos - 2); }

    public override int Pos {
        get { return bPos; }
        set { bPos = value; }
    }


    /// <summary>
    /// Read returns the ordinal number of the next char, or 
    /// EOF (-1) for an end of stream.  Note that the next
    /// code point may require *two* calls of Read().
    /// </summary>
    /// <returns></returns>
    public override int Read()
    {
        //
        //  Characters at positions 
        //  [data.offset, data.offset + data.bldr.Length)
        //  are available in data.bldr.
        //
        if (bPos < data.MaxIndex)
        {
            // ch0 cannot be EOF
            int result = data[bPos];
            ++bPos;
            return result;
        }
        else // Read from underlying stream
        {
            // Experimental code, blocks of page size
            char[] chrs = new char[4096];
            int count = ReadNextBlk(chrs, 0, chrs.Length);
            if (count == 0)
                return EndOfFile;
            else
            {
                data.Append(chrs, count);
                int result = data[bPos];
                ++bPos;
                return result;
            }
        }
    }

    public override string GetString(int begin, int limit)
    {
        return data.GetString(begin, limit);
    }

    public override string ToString()
    {
        return "StringBuilder buffer, encoding: " + this.EncodingName;
    }
}

// =============== End ScanBuff-derived classes ==================

#endregion Buffer classes

// ==============================================================
// ============      class CodePageHandling         =============
// ==============================================================
public class CodePageHandling
{
    public static int GetCodePage(string option)
    {
        string command = option.ToUpperInvariant();
        if (command.StartsWith("CodePage:", StringComparison.OrdinalIgnoreCase))
            command = command.Substring(9);
        var opt = option;
        try
        {
            if (command.Equals("RAW"))
                return -1;
            else if (command.Equals("GUESS"))
                return -2;
            else if (command.Equals("DEFAULT"))
                return 0;
            else if (char.IsDigit(command[0]))
                return int.Parse(command, CultureInfo.InvariantCulture);
            else
            {
                Encoding enc = Encoding.GetEncoding(command);
                return enc.CodePage;
            }
        }
        
        catch (FormatException)
        {
            // deslanged won't resolve opt but okay because defaults to var
            Console.Error.WriteLine(string.Concat("Invalid format ",opt,", using machine default"));
        }
        catch (ArgumentException)
        {
            // deslanged won't resolve opt but okay because defaults to var
            Console.Error.WriteLine(
                string.Concat("Unknown code page ",opt,", using machine default"));
        }
        return 0;
    }
}
#region guesser
// ==============================================================
// ============          Encoding Guesser           =============
// ==============================================================

/// <summary>
/// This class provides a simple finite state automaton that
/// scans the file looking for (1) valid UTF-8 byte patterns,
/// (2) bytes >= 0x80 which are not part of a UTF-8 sequence.
/// The method then guesses whether it is UTF-8 or maybe some 
/// local machine default encoding.  This works well for the
/// various Latin encodings.
/// </summary>
internal class Guesser
{
    ScanBuff buffer;

    public int GuessCodePage() { return Scan(); }

    const int maxAccept = 10;
    const int initial = 0;
    const int eofNum = 0;
    const int goStart = -1;
    const int INITIAL = 0;
    const int EndToken = 0;

    #region user code
    /* 
     *  Reads the bytes of a file to determine if it is 
     *  UTF-8 or a single-byte code page file.
     */
    public long utfX;
    public long uppr;
    #endregion user code

    int state;
    int currentStart = startState[0];
    int code;

    #region ScannerTables
    static int[] startState = new int[] { 11, 0 };

    #region CharacterMap
    static sbyte[] map = new sbyte[] {
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0,
2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2, 2,
1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1, 1,
3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 3, 4, 4, 4, 4, 4, 4, 4, 4, 5, 5, 5, 5, 5, 5, 5, 5 };
    #endregion

    static sbyte[][] nextState = new sbyte[][] {
            new sbyte[] {0, 0, 0, 0, 0, 0},
            new sbyte[] {-1, -1, 10, -1, -1, -1},
            new sbyte[] {-1, -1, -1, -1, -1, -1},
            new sbyte[] {-1, -1, 8, -1, -1, -1},
            new sbyte[] {-1, -1, 5, -1, -1, -1},
            new sbyte[] {-1, -1, 6, -1, -1, -1},
            new sbyte[] {-1, -1, 7, -1, -1, -1},
            null,
            new sbyte[] {-1, -1, 9, -1, -1, -1},
            null,
            null,
            new sbyte[] {-1, 1, 2, 3, 4, 2}
        };
    [SuppressMessage("Microsoft.Performance", "CA1810:InitializeReferenceTypeStaticFieldsInline")]
    // Reason for suppression: cannot have self-reference in array initializer.
    static Guesser()
    {
        nextState[7] = nextState[2];
        nextState[9] = nextState[2];
        nextState[10] = nextState[2];
    }

    int NextState()
    {
        if (code == ScanBuff.EndOfFile)
            return eofNum;
        else
            return nextState[state][map[code]];
    }
    #endregion

    public Guesser(System.IO.Stream file) { SetSource(file); }

    public void SetSource(System.IO.Stream source)
    {
        this.buffer = new BuildBuffer(source);
        code = buffer.Read();
    }

    int Scan()
    {
        while (true)
        {

            int next;
            state = currentStart;

            while ((next = NextState()) == goStart)
                code = buffer.Read();

            state = next;

            code = buffer.Read();

            while ((next = NextState()) > eofNum)
            {
                state = next;
                code = buffer.Read();
            }
            if (state <= maxAccept)
            {
                #region ActionSwitch

                if (state == eofNum)
                {
                    if (currentStart == 11)
                    {
                        if (utfX == 0 && uppr == 0) return -1; /* raw ascii */
                        else if (uppr * 10 > utfX) return 0;   /* default code page */
                        else return 65001;                     /* UTF-8 encoding */
                    }
                    return EndToken;
                }
                if (state > 0 && state < 5)
                {
                    // 1: Recognized '{Upper128}',	Shortest string "\xC0"
                    // 2: Recognized '{Upper128}',	Shortest string "\x80"
                    // 3: Recognized '{Upper128}',	Shortest string "\xE0"
                    // 4: Recognized '{Upper128}',	Shortest string "\xF0"   
                    ++uppr;
                }
                else if (state == 5)
                {
                    // Recognized '{Utf8pfx4}{Utf8cont}',	Shortest string "\xF0\x80"
                    uppr += 2;
                }
                else if (state == 6)
                {
                    // Recognized '{Utf8pfx4}{Utf8cont}{2}',	Shortest string "\xF0\x80\x80"
                    uppr += 3;
                }
                else if (state == 7)
                {
                    // Recognized '{Utf8pfx4}{Utf8cont}{3}',	Shortest string "\xF0\x80\x80\x80"
                    utfX += 3;
                }
                else if (state == 8)
                {
                    // Recognized '{Utf8pfx3}{Utf8cont}',	Shortest string "\xE0\x80"
                    uppr += 2;
                }
                else if (state == 9)
                {
                    // Recognized '{Utf8pfx3}{Utf8cont}{2}',	Shortest string "\xE0\x80\x80"
                    utfX += 2;
                }
                else if (state == 10)
                {
                    // Recognized '{Utf8pfx2}{Utf8cont}',	Shortest string "\xC0\x80"
                    ++utfX;

                }


                #endregion
            }
        }
    }
} // end class Guesser

#endregion
