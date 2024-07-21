using System;

namespace CommonLib.Utilities
{
    public static class FastByteConverter
    {
        public const int Int16Size = 2;
        public const int Int32Size = 4;
        public const int Int64Size = 8;

        public static byte ToByte(this byte[] bytes)
        {
            CheckBytes(bytes, 1);
            return bytes[0];
        }

        public static sbyte ToSByte(this byte[] bytes)
        {
            CheckBytes(bytes, 1);
            return (sbyte)bytes[0];
        }

        public static short ToShort(this byte[] bytes)
        {
            CheckBytes(bytes, Int16Size);
            return (short)(bytes[0] | bytes[1] << 8);
        }

        public static ushort ToUShort(this byte[] bytes)
        {
            CheckBytes(bytes, Int16Size);
            return (ushort)(bytes[0] | bytes[1] << 8);
        }

        public static int ToInt(this byte[] bytes)
        {
            CheckBytes(bytes, Int32Size);
            return (int)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
        }

        public static uint ToUInt(this byte[] bytes)
        {
            CheckBytes(bytes, Int32Size);
            return (uint)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
        }

        public static long ToLong(this byte[] bytes)
        {
            CheckBytes(bytes, Int64Size);

            var lo = (uint)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
            var hi = (uint)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24);

            return (long)((ulong)hi << 32 | lo);
        }

        public static ulong ToULong(this byte[] bytes)
        {
            CheckBytes(bytes, Int64Size);

            var lo = (uint)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
            var hi = (uint)(bytes[4] | bytes[5] << 8 | bytes[6] << 16 | bytes[7] << 24);

            return (ulong)hi << 32 | lo;
        }

        public static unsafe float ToFloating(this byte[] bytes)
        {
            CheckBytes(bytes, Int32Size);

            var temp = (uint)(bytes[0] | bytes[1] << 8 | bytes[2] << 16 | bytes[3] << 24);
            
            return *(float*)&temp;
        }

        public static unsafe double ToDouble(this byte[] bytes)
        {
            var temp = bytes.ToULong();
            return *(double*)&temp;
        }

        public static bool ToBoolean(this byte[] bytes)
            => bytes.ToByte() == 1;

        public static char ToChar(this byte[] bytes)
            => (char)bytes.ToByte();

        public static string ToString(this byte[] bytes)
        {
            var str = "";

            for (int i = 0; i < bytes.Length; i++)
                str += (char)bytes[i];

            return str;
        }

        private static void CheckBytes(byte[] bytes, int requiredSize)
        {
            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            if (bytes.Length != requiredSize)
                throw new ArgumentOutOfRangeException(nameof(bytes));
        }
    }
}