using System.Numerics;
using System.Runtime.CompilerServices;
using BenchmarkDotNet.Attributes;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BenchMarx
{
    /// <summary>
    /// Calculate using different strategies:
    /// dest[i] = source[i] * source[i] + source[i]
    /// </summary>
    [RyuJitX64Job]
//    [RyuJitX86Job]
    public unsafe class SimdBenchmarks
    {
        private static readonly int SizeOfVector = Unsafe.SizeOf<Vector<float>>();

        private Buffer _source;
        private Buffer _dest;
        
        private int _byteCount;
       
        [Params(512)]
        public int Count { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _byteCount = Count * SizeOfVector;
            
            _source = new Buffer(_byteCount, SizeOfVector);
            _dest = new Buffer(_byteCount, SizeOfVector);
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            _source.Dispose();
            _dest.Dispose();
        }

        [Benchmark]
        public void Scalar()
        {
            // just for reference and explanation

            float* sBase = (float*) _source.UnalignedPtr;
            float* dBase = (float*) _dest.UnalignedPtr;
            
            for (int i = 0; i < Count * SizeOfVector; i++)
            {
                float s = *(sBase + i);
                *(dBase + i) = s * s + s;
            }
        }
        
        [Benchmark(Baseline = true)]
        public void ClassicUnaligned()
        {
            RunClassic(_source.UnalignedPtr, _dest.UnalignedPtr);
        }

        
        [Benchmark]
        public void ClassicAligned()
        {
            ref Vector<float> sBase = ref Unsafe.AsRef<Vector<float>>(_source.AlignedPtr);
            ref Vector<float> dBase = ref Unsafe.AsRef<Vector<float>>(_dest.AlignedPtr);

            RunClassic(_source.AlignedPtr, _dest.AlignedPtr);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunClassic(byte* sp, byte* dp)
        {
            ref Vector<float> sBase = ref Unsafe.AsRef<Vector<float>>(sp);
            ref Vector<float> dBase = ref Unsafe.AsRef<Vector<float>>(dp);
            
            for (int i = 0; i < Count; i++)
            {
                Vector<float> s = Unsafe.Add(ref sBase, i);

                Unsafe.Add(ref dBase, i) = s * s + s;
            }
        }


        [Benchmark]
        public void ClassicUnalignedGrouped1()
        {
            RunClassicGrouped1(_source.UnalignedPtr, _dest.UnalignedPtr);
        }
        
        [Benchmark]
        public void ClassicUnalignedGrouped2()
        {   
            RunClassicGrouped2(_source.UnalignedPtr, _dest.UnalignedPtr);
        }
        
        [Benchmark]
        public void ClassicAlignedGrouped1()
        {
            RunClassicGrouped1(_source.AlignedPtr, _dest.AlignedPtr);
        }
        
        [Benchmark]
        public void ClassicAlignedGrouped2()
        {
            RunClassicGrouped2(_source.AlignedPtr, _dest.AlignedPtr);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunClassicGrouped1(byte* sp, byte* dp)
        {
            ref Vector<float> sBase = ref Unsafe.AsRef<Vector<float>>(sp);
            ref Vector<float> dBase = ref Unsafe.AsRef<Vector<float>>(dp);
            
            for (int i = 0; i < Count; i+=4)
            {
                Vector<float> s0 = Unsafe.Add(ref sBase, i);
                Vector<float> s1 = Unsafe.Add(ref sBase, i + 1);
                Vector<float> s2 = Unsafe.Add(ref sBase, i + 2);
                Vector<float> s3 = Unsafe.Add(ref sBase, i + 3);

                Unsafe.Add(ref dBase, i) = s0 * s0 + s0;
                Unsafe.Add(ref dBase, i+1) = s1 * s1 + s1;
                Unsafe.Add(ref dBase, i+2) = s2 * s2 + s2;
                Unsafe.Add(ref dBase, i+3) = s3 * s3 + s3;
            }
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunClassicGrouped2(byte* sp, byte* dp)
        {
            ref Vector<float> sBase = ref Unsafe.AsRef<Vector<float>>(sp);
            ref Vector<float> dBase = ref Unsafe.AsRef<Vector<float>>(dp);

            for (int i = 0; i < Count; i+=4)
            {
                Vector<float> s0 = Unsafe.Add(ref sBase, i);
                Vector<float> s1 = Unsafe.Add(ref sBase, i + 1);
                Vector<float> s2 = Unsafe.Add(ref sBase, i + 2);
                Vector<float> s3 = Unsafe.Add(ref sBase, i + 3);

                var s00 = s0 * s0;
                var s11 = s1 * s1;
                var s22 = s2 * s2;
                var s33 = s3 * s3;

                s00 += s0;
                s11 += s1;
                s22 += s2;
                s33 += s3;
                
                Unsafe.Add(ref dBase, i) = s00;
                Unsafe.Add(ref dBase, i+1) = s11;
                Unsafe.Add(ref dBase, i+2) = s22;
                Unsafe.Add(ref dBase, i+3) = s33;
            }
        }

        [Benchmark]
        public void AvxIntrinsicUnaligned()
        {
            RunAvxIntrinsic(_source.UnalignedPtr, _dest.UnalignedPtr);
        }

        [Benchmark]
        public void AvxIntrinsicAligned()
        {
            RunAvxIntrinsic(_source.AlignedPtr, _dest.AlignedPtr);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunAvxIntrinsic(byte* sp, byte* dp)
        {
            float* sBase = (float*) sp;
            float* dBase = (float*) dp;
            for (int i = 0; i < Count; i++)
            {
                Vector256<float> s = Avx.LoadVector256(sBase + i);
                Vector256<float> t = Avx.Multiply(s, s);
                t = Avx.Add(t, s);
                Avx.Store(dBase+i, t);
            }
        }
    }
}