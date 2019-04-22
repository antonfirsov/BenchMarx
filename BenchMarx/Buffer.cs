
using System;
using System.Runtime.InteropServices;

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
}