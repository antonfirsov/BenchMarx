using System;
using BenchmarkDotNet.Running;

namespace BenchMarx
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<SimdBenchmarks>();
        }
    }
}