using System.Runtime.InteropServices;

namespace Zorrendor.PointCloud
{
    [StructLayout(LayoutKind.Explicit)]
    public class ByteConverter
    {
        [FieldOffset(0)]
        public byte[] Bytes;

        [FieldOffset(0)]
        public float[] Floats;

        [FieldOffset(0)]
        public double[] Doubles;

        [FieldOffset(0)]
        public int[] Ints;

        [FieldOffset(0)]
        public uint[] UInts;

        [FieldOffset(0)]
        public short[] Shorts;

        [FieldOffset(0)]
        public ushort[] UShorts;
    }
}
