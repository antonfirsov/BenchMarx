using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;

namespace BenchMarx
{
    [RyuJitX64Job]
    [RyuJitX86Job]
    public class SimdBenchmarks
    {
        private float[] source1;
        private float[] source2;
        private float[] dest;
       
        [Params(1024)]
        public int Count { get; set; }


        [GlobalSetup]
        public void Setup()
        {
            this.source1 = new float[this.Count];
            this.source2 = new float[this.Count];
            this.dest = new float[this.Count];
        }
        
        [Benchmark]
        public void Basic()
        {
            
        }
    }
}