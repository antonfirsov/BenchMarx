using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace BenchMarx
{
    internal unsafe struct Buffer : IDisposable
    {
        public readonly byte[] Array;

        public readonly GCHandle Handle;

        public readonly IntPtr Address;

        public readonly byte* AlignedPtr;

        public readonly byte* UnalignedPtr;

        public Buffer(int count, int alingment)
        {
            Array = new byte[count + 2 * alingment];
            Handle = GCHandle.Alloc(Array, GCHandleType.Pinned);
            Address = Handle.AddrOfPinnedObject();
            long addressVal = (long)Address;
            int padding = alingment - (int)(addressVal % alingment);

            AlignedPtr = (byte*)(Address + padding);
            UnalignedPtr = AlignedPtr + 3;
        }

        public void Dispose()
        {
            Handle.Free();
        }
    }
    
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
        
        [Benchmark(Baseline = true)]
        public void ClassicAligned()
        {
            ref Vector<float> sBase = ref Unsafe.AsRef<Vector<float>>(_source.AlignedPtr);
            ref Vector<float> dBase = ref Unsafe.AsRef<Vector<float>>(_dest.AlignedPtr);

            for (int i = 0; i < Count; i++)
            {
                Vector<float> s = Unsafe.Add(ref sBase, i);

                Unsafe.Add(ref dBase, i) = s * s + s;
            }
        }
    }
}