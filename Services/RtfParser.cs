using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace BookWriter.Services
{
    /// <summary>
    /// Lightweight RTF → plain text parser.
    /// Handles \uN unicode escapes (used for Cyrillic by WPF RichTextBox).
    /// Used for PDF/EPUB export when FlowDocument.Blocks returns empty.
    /// </summary>
    public static class RtfParser
    {
        public static List<string> ExtractParagraphs(string rtfBase64)
        {
            if (string.IsNullOrEmpty(rtfBase64))
                return new List<string>();

            try
            {
                var bytes = Convert.FromBase64String(rtfBase64);
                var rtf   = Encoding.GetEncoding(1252).GetString(bytes);
                return ParseRtf(rtf);
            }
            catch
            {
                return new List<string>();
            }
        }

        private static List<string> ParseRtf(string rtf)
        {
            var paragraphs = new List<string>();
            var current    = new StringBuilder();
            int i          = 0;
            int depth      = 0;
            // Track ignored groups (destinations like \fonttbl, \colortbl, \info etc.)
            var ignoreDepth = new Stack<int>();
            bool ignoring   = false;

            while (i < rtf.Length)
            {
                char c = rtf[i];

                if (c == '{')
                {
                    depth++;
                    i++;
                    // Check if next token is an ignored destination
                    int j = i;
                    while (j < rtf.Length && rtf[j] == ' ') j++;
                    if (j < rtf.Length && rtf[j] == '\\')
                    {
                        var word = ReadControlWord(rtf, j + 1, out _);
                        if (IsIgnoredDestination(word))
                        {
                            ignoreDepth.Push(depth);
                            ignoring = true;
                        }
                    }
                }
                else if (c == '}')
                {
                    if (ignoreDepth.Count > 0 && ignoreDepth.Peek() == depth)
                    {
                        ignoreDepth.Pop();
                        ignoring = ignoreDepth.Count > 0;
                    }
                    depth--;
                    i++;
                }
                else if (c == '\\' && !ignoring)
                {
                    i++; // skip backslash
                    if (i >= rtf.Length) break;

                    char next = rtf[i];

                    if (next == 'u' && i + 1 < rtf.Length && (char.IsDigit(rtf[i + 1]) || rtf[i + 1] == '-'))
                    {
                        // \uN — unicode character
                        i++;
                        bool neg = rtf[i] == '-';
                        if (neg) i++;
                        int start = i;
                        while (i < rtf.Length && char.IsDigit(rtf[i])) i++;
                        int code = int.Parse(rtf.Substring(start, i - start));
                        if (neg) code = -code;
                        if (code < 0) code += 65536;
                        current.Append((char)code);
                        // Skip optional fallback character
                        if (i < rtf.Length && rtf[i] == '?') i++;
                    }
                    else if (next == '\n' || next == '\r')
                    {
                        // \<newline> = paragraph mark in some RTF
                        i++;
                    }
                    else if (char.IsLetter(next))
                    {
                        // Control word
                        var word = ReadControlWord(rtf, i, out int advance);
                        i += advance;
                        // Skip optional space after control word
                        if (i < rtf.Length && rtf[i] == ' ') i++;

                        if (word == "par" || word == "line")
                        {
                            var para = current.ToString().Trim();
                            if (para.Length > 0)
                                paragraphs.Add(para);
                            current.Clear();
                        }
                        // else: ignore all other control words
                    }
                    else if (next == '\'')
                    {
                        // \'XX — hex encoded char (cp1252)
                        i++;
                        if (i + 1 < rtf.Length)
                        {
                            var hex = rtf.Substring(i, 2);
                            i += 2;
                            try
                            {
                                var b = Convert.ToByte(hex, 16);
                                current.Append(Encoding.GetEncoding(1252).GetString(new[] { b }));
                            }
                            catch { }
                        }
                    }
                    else if (next == '-' || next == '~' || next == '_')
                    {
                        i++; // special chars — skip
                    }
                    else if (next == '*')
                    {
                        // \* = ignorable destination marker — ignore until }
                        ignoreDepth.Push(depth);
                        ignoring = true;
                        i++;
                    }
                    else
                    {
                        // Single-char control symbol
                        if (next == ' ') current.Append(' ');
                        i++;
                    }
                }
                else if (!ignoring)
                {
                    if (c != '\r' && c != '\n')
                        current.Append(c);
                    i++;
                }
                else
                {
                    i++;
                }
            }

            // Last paragraph without \par
            var last = current.ToString().Trim();
            if (last.Length > 0)
                paragraphs.Add(last);

            return paragraphs;
        }

        private static string ReadControlWord(string rtf, int pos, out int charsRead)
        {
            int start = pos;
            while (pos < rtf.Length && char.IsLetter(rtf[pos])) pos++;
            // Optional numeric parameter
            if (pos < rtf.Length && (char.IsDigit(rtf[pos]) || rtf[pos] == '-'))
            {
                if (rtf[pos] == '-') pos++;
                while (pos < rtf.Length && char.IsDigit(rtf[pos])) pos++;
            }
            charsRead = pos - start;
            return rtf.Substring(start, charsRead > 0 ? (pos - start - (pos > start && !char.IsLetter(rtf[pos - 1]) ? 0 : 0)) : 0)
                      .TrimEnd('0', '1', '2', '3', '4', '5', '6', '7', '8', '9', '-');
        }

        private static bool IsIgnoredDestination(string word) =>
            word is "fonttbl" or "colortbl" or "stylesheet" or "info"
                 or "pict" or "object" or "fldinst" or "fldrslt"
                 or "header" or "footer" or "headerl" or "headerr"
                 or "footerl" or "footerr" or "listtable" or "listoverridetable"
                 or "rsidtbl" or "themedata" or "colorschememapping"
                 or "latentstyles" or "datastore";
    }
}
