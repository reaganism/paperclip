using System.Collections.Generic;

using BenchmarkDotNet.Attributes;

namespace Reaganism.Paperclip.Benchmarks.FBI;

[MemoryDiagnoser]
public class TokenMapperInitializationStrategy
{
    private class UncachedTokenMapperWithoutCapacity
    {
        private readonly List<string> idToLine = [];

        public UncachedTokenMapperWithoutCapacity()
        {
            // Add a sentinel value at index 0.
            idToLine.Add("\0");

            // Add ASCII characters as-is.
            for (var i = 0; i < 0x80; i++)
            {
                idToLine.Add(((char)i).ToString());
            }
        }
    }

    private class UncachedTokenMapperWithCapacity
    {
        private readonly List<string> idToLine = new(0x80 + 1);

        public UncachedTokenMapperWithCapacity()
        {
            // Add a sentinel value at index 0.
            idToLine.Add("\0");

            // Add ASCII characters as-is.
            for (var i = 0; i < 0x80; i++)
            {
                idToLine.Add(((char)i).ToString());
            }
        }
    }

    private class CachedTokenMapper
    {
        private readonly List<string> idToLine = [..cached_lines_to_ids];

        private static readonly string[] cached_lines_to_ids;

        static CachedTokenMapper()
        {
            cached_lines_to_ids = new string[0x80 + 1];
            {
                // Add a sentinel value at index 0.
                cached_lines_to_ids[0] = "\0";

                // Add ASCII characters as-is.
                for (var i = 0; i < 0x80; i++)
                {
                    cached_lines_to_ids[i + 1] = ((char)i).ToString();
                }
            }
        }
    }

    [Params(10, 100, 1000, 100000)]
    public int N;

    [Benchmark]
    public void InitUncachedTokenMapperWithoutCapacity()
    {
        for (var i = 0; i < N; i++)
        {
            _ = new UncachedTokenMapperWithoutCapacity();
        }
    }

    [Benchmark]
    public void InitUncachedTokenMapperWithCapacity()
    {
        for (var i = 0; i < N; i++)
        {
            _ = new UncachedTokenMapperWithoutCapacity();
        }
    }

    [Benchmark]
    public void InitCachedTokenMapper()
    {
        for (var i = 0; i < N; i++)
        {
            _ = new CachedTokenMapper();
        }
    }
}