using System;
using System.Collections.Generic;
using System.Text;

using BenchmarkDotNet.Attributes;

using Reaganism.FBI.Diffing;

namespace Reaganism.Paperclip.Benchmarks.FBI;

[MemoryDiagnoser]
public class TokenMapperWordsToIdsStrategy
{
    private class TokenMapperByteBuffer
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
    }

    private class TokenMapperStringBuilder
    {
        private readonly TokenMapper real = new();

        public string WordsToIds(string line)
        {
            var sb = new StringBuilder();
            {
                foreach (var r in EnumerateWords(line))
                {
                    var word = line[r];
                    sb.Append((char)real.AddWord(word));
                }
            }
            return sb.ToString();
        }
    }

    private TokenMapperByteBuffer    byteBuffer    = new();
    private TokenMapperStringBuilder stringBuilder = new();

    private const string line = "The quick brown fox jumps over the lazy dog.";

    [Params(10, 100, 1000, 100000)]
    public int N;

    [Benchmark]
    public void ByteBuffer()
    {
        for (var i = 0; i < N; i++)
        {
            _ = byteBuffer.WordsToIds(line);
        }
    }

    [Benchmark]
    public void StringBuilder()
    {
        for (var i = 0; i < N; i++)
        {
            _ = stringBuilder.WordsToIds(line);
        }
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