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
        public int VectorCount { get; set; }

        [GlobalSetup]
        public void Setup()
        {
            _byteCount = VectorCount * SizeOfVector;
            
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
            
            for (int i = 0; i < VectorCount * 8; i++)
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
            
            for (int i = 0; i < VectorCount; i++)
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
            
            for (int i = 0; i < VectorCount; i+=4)
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

            for (int i = 0; i < VectorCount; i+=4)
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
            float* sPtr = (float*) sp;
            float* dPtr = (float*) dp;
            for (int i = 0; i < VectorCount; i++, sPtr += 8, dPtr+=8)
            {
                Vector256<float> s = Avx.LoadVector256(sPtr);
                
                Vector256<float> t = Avx.Multiply(s, s);
                t = Avx.Add(t, s);
                Avx.Store(dPtr, t);
            }
        }
        
        [Benchmark]
        public void AvxIntrinsicGroupedUnaligned()
        {
            RunAvxIntrinsicGrouped(_source.UnalignedPtr, _dest.UnalignedPtr);
        }

        [Benchmark]
        public void AvxIntrinsicGroupedAligned()
        {
            RunAvxIntrinsicGrouped(_source.AlignedPtr, _dest.AlignedPtr);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void RunAvxIntrinsicGrouped(byte* sp, byte* dp)
        {
            float* sPos = (float*) sp;
            float* dPos = (float*) dp;
            
            for (int i = 0; i < VectorCount / 4; i++)
            {
                Vector256<float> s0 = Avx.LoadVector256(sPos);
                sPos += 8;
                Vector256<float> s1 = Avx.LoadVector256(sPos);
                sPos += 8;
                Vector256<float> s2 = Avx.LoadVector256(sPos);
                sPos += 8;
                Vector256<float> s3 = Avx.LoadVector256(sPos);
                sPos += 8;
                
                Vector256<float> t0 = Avx.Multiply(s0, s0);
                Vector256<float> t1 = Avx.Multiply(s1, s1);
                Vector256<float> t2 = Avx.Multiply(s2, s2);
                Vector256<float> t3 = Avx.Multiply(s3, s3);
                t0 = Avx.Add(t0, s0);
                t1 = Avx.Add(t1, s1);
                t2 = Avx.Add(t2, s2);
                t3 = Avx.Add(t3, s3);
                
                Avx.Store(dPos, t0);
                dPos += 8;
                Avx.Store(dPos, t1);
                dPos += 8;
                Avx.Store(dPos, t2);
                dPos += 8;
                Avx.Store(dPos, t3);
                dPos += 8;
            }
        }
    }
}