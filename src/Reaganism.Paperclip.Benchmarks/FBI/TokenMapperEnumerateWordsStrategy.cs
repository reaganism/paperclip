using System;
using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

using Reaganism.FBI.Diffing;

namespace Reaganism.Paperclip.Benchmarks.FBI;

public class TokenMapperEnumerateWordsStrategy
{
    private class TokenMapperEnumerateWordsOld
    {
        private readonly TokenMapper real = new();

        private char[] buf = new char[4096];

        public string WordsToIds(string line)
        {
            var b = 0;

            foreach (var r in EnumerateWords(line))
            {
                var word = line[r];

                if (b >= buf.Length)
                {
                    Array.Resize(ref buf, buf.Length * 2);
                }

                buf[b++] = (char)real.AddWord(word);
            }

            return new string(buf, 0, b);
        }

        private static IEnumerable<Range> EnumerateWords(string line)
        {
            for (var i = 0; i < line.Length;)
            {
                var start = i;
                var c     = line[i++];

                if (char.IsLetter(c))
                {
                    while (i < line.Length && char.IsLetterOrDigit(line, i))
                    {
                        i++;
                    }
                }
                else if (char.IsDigit(c))
                {
                    while (i < line.Length && char.IsDigit(line, i))
                    {
                        i++;
                    }
                }
                else if (c is ' ' or '\t')
                {
                    while (i < line.Length && line[i] == c)
                    {
                        i++;
                    }
                }

                yield return new Range(start, i);
            }
        }
    }

    private class TokenMapperEnumerateWordsNew
    {
        private readonly TokenMapper real = new();

        private char[] buf = new char[4096];

        public string WordsToIds(string line)
        {
            var b = 0;

            foreach (var r in EnumerateWords(line))
            {
                var word = line[r];

                if (b >= buf.Length)
                {
                    Array.Resize(ref buf, buf.Length * 2);
                }

                buf[b++] = (char)real.AddWord(word);
            }

            return new string(buf, 0, b);
        }

        private static IEnumerable<Range> EnumerateWords(string line)
        {
            var start  = 0;
            var length = line.Length;

            while (start < length)
            {
                // Mark the end at the start.
                var end       = start;
                var startChar = line[start];

                // Handle whitespace.  We only match space (' ') and tab ('\t') to
                // match behavior with Chicken-Bones/DiffPatch.  While we want to
                // concatenate whitespace, different whitespace characters should be
                // understood as different contexts.  So a mixture of tabs and
                // spaces should be separated.
                if (startChar is ' ' or '\t')
                {
                    while (end < length && startChar == line[end])
                    {
                        end++;
                    }

                    // If we've reached or exceeded the length, and it ends on
                    // whitespace, then we'll consider it insignificant and consider
                    // ourselves done.
                    if (end >= length)
                    {
                        yield break;
                    }

                    // Now that we've found the end of the whitespace, reposition
                    // ourselves.  We reset our starting position to the end of the
                    // whitespace and find the next character.
                    start     = --end;
                    startChar = line[start];
                }

                if (char.IsLetter(startChar))
                {
                    // If the character is a letter, begin searching for an actual
                    // word.  Words may contain digits.
                    while (end < length && char.IsLetterOrDigit(line[end]))
                    {
                        end++;
                    }
                }
                else if (char.IsDigit(startChar))
                {
                    // If the character is a digit, begin searching for a number.
                    // Numbers may only contain digits.  Words may not start with
                    // numbers.
                    while (end < length && char.IsDigit(line[end]))
                    {
                        end++;
                    }
                }
                else
                {
                    // If the character is neither a letter, a digit, nor
                    // whitespace, then it is a symbol or a single-character word,
                    // which we treat as a regular character.
                    end = start + 1;
                }

                // Return our resolved range and set the next start position to the
                // end of our resolved range.
                yield return new Range(start, end);
                start = end;
            }
        }
    }

    private TokenMapperEnumerateWordsOld old  = new();
    private TokenMapperEnumerateWordsNew @new = new();

    private const string line = "The quick brown fox jumps over the lazy dog.";

    // [Params(10, 100, 1000, 100000)]
    [Params(100000)]
    public int N;

    [Benchmark]
    public void Old()
    {
        for (var i = 0; i < N; i++)
        {
            _ = old.WordsToIds(line);
        }
    }

    [Benchmark]
    public void New()
    {
        for (var i = 0; i < N; i++)
        {
            _ = @new.WordsToIds(line);
        }
    }
}