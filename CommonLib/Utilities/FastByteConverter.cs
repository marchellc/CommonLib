using System;

namespace CommonLib.Utilities
{
    public static class FastByteConverter
    {
        public const int Int16Size = 2;
        public const int Int32Size = 4;
        public const int Int64Size = 8;

        public static byte ToByte(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, 1);
            return bytes[offset];
        }

        public static sbyte ToSByte(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, 1);
            return (sbyte)bytes[offset];
        }

        public static short ToShort(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes,  offset, Int16Size);
            return (short)(bytes[offset] | bytes[offset + 1] << 8);
        }

        public static ushort ToUShort(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, Int16Size);
            return (ushort)(bytes[offset] | bytes[offset + 1] << 8);
        }

        public static int ToInt(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, Int32Size);
            return (int)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
        }

        public static uint ToUInt(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, Int32Size);
            return (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
        }

        public static long ToLong(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, Int64Size);

            var lo = (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
            var hi = (uint)(bytes[offset + 4] | bytes[offset + 5] << 8 | bytes[offset + 6] << 16 | bytes[offset + 7] << 24);

            return (long)((ulong)hi << 32 | lo);
        }

        public static ulong ToULong(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, Int64Size);

            var lo = (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
            var hi = (uint)(bytes[offset + 4] | bytes[offset + 5] << 8 | bytes[offset + 6] << 16 | bytes[offset + 7] << 24);

            return (ulong)hi << 32 | lo;
        }

        public static unsafe float ToSingle(this byte[] bytes, int offset = 0)
        {
            CheckBytes(bytes, offset, Int32Size);

            var temp = (uint)(bytes[offset] | bytes[offset + 1] << 8 | bytes[offset + 2] << 16 | bytes[offset + 3] << 24);
            return *(float*)&temp;
        }

        public static unsafe double ToDouble(this byte[] bytes, int offset = 0)
        {
            var temp = bytes.ToULong();
            return *(double*)&temp;
        }

        public static bool ToBoolean(this byte[] bytes, int offset = 0)
            => bytes.ToByte(offset) == 1;

        public static char ToChar(this byte[] bytes, int offset = 0)
            => (char)bytes.ToByte(offset);

        public static string ToString(this byte[] bytes, int offset = 0)
        {
            var str = "";

            for (int i = offset; i < bytes.Length; i++)
                str += (char)bytes[i];

            return str;
        }

        private static void CheckBytes(byte[] bytes, int offset, int requiredSize)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length < (offset + requiredSize))
                throw new ArgumentOutOfRangeException(nameof(bytes));
        }
    }
}