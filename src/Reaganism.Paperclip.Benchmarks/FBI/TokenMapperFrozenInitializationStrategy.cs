using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;

using BenchmarkDotNet.Attributes;

namespace Reaganism.Paperclip.Benchmarks.FBI;

[MemoryDiagnoser]
public class TokenMapperFrozenInitializationStrategy
{
    private class CachedTokenMapperUnfrozen
    {
        private readonly List<string> idToLine = [..cached_lines_to_ids];

        private static readonly string[] cached_lines_to_ids;

        static CachedTokenMapperUnfrozen()
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

    private class CachedTokenMapperFrozen
    {
        private readonly List<string> idToLine = [..cached_lines_to_ids];

        private static readonly FrozenSet<string> cached_lines_to_ids;

        static CachedTokenMapperFrozen()
        {
            var cachedLinesToIds = new string[0x80 + 1];
            {
                // Add a sentinel value at index 0.
                cachedLinesToIds[0] = "\0";

                // Add ASCII characters as-is.
                for (var i = 0; i < 0x80; i++)
                {
                    cachedLinesToIds[i + 1] = ((char)i).ToString();
                }
            }
            cached_lines_to_ids = cachedLinesToIds.ToFrozenSet();
        }
    }

    [Params(10, 100, 1000, 100000)]
    public int N;

    [Benchmark]
    public void InitCachedTokenMapperUnfrozen()
    {
        for (var i = 0; i < N; i++)
        {
            _ = new CachedTokenMapperUnfrozen();
        }
    }

    [Benchmark]
    public void InitCachedTokenMapperFrozen()
    {
        for (var i = 0; i < N; i++)
        {
            _ = new CachedTokenMapperFrozen();
        }
    }
}