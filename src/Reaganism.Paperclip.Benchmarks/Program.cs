﻿using BenchmarkDotNet.Running;

namespace Reaganism.Paperclip.Benchmarks;

internal static class Program
{
    public static void Main(string[] args)
    {
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
    }
}