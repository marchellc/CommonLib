using CommonLib.Serialization.Buffers;

using System;

namespace CommonLib.Serialization
{
    public static class Compression
    {
        public static void CompressLong(this SerializerBuffer buffer, long value)
        {
            var ulongValue = (ulong)((value >> 63) ^ (value << 1));
            CompressULong(buffer, ulongValue);
        }

        public static void CompressULong(this SerializerBuffer buffer, ulong value)
        {
            if (value <= 240)
            {
                buffer.Write((byte)value);
                return;
            }

            if (value <= 2287)
            {
                var a = (byte)(((value - 240) >> 8) + 241);
                var b = (byte)((value - 240) & 0xFF);
                var c = (ushort)(b << 8 | a);

                buffer.Write((byte)c);
                buffer.Write((byte)(c >> 8));

                return;
            }

            if (value <= 67823)
            {
                var a = (byte)249;
                var b = (byte)((value - 2288) >> 8);
                var c = (byte)((value - 2288) & 0xFF);
                var d = (ushort)(c << 8 | b);

                buffer.Write(a);
                buffer.Write((byte)d);
                buffer.Write((byte)(d >> 8));

                return;
            }

            if (value <= 16777215)
            {
                var a = (byte)250;
                var b = (uint)(value << 8);
                var c = b | a;

                buffer.Write((byte)c);
                buffer.Write((byte)(c >> 8));
                buffer.Write((byte)(c >> 16));
                buffer.Write((byte)(c >> 24));

                return;
            }

            if (value <= 4294967295)
            {
                var a = (byte)251;
                var b = (uint)value;

                buffer.Write(a);
                buffer.Write((byte)b);
                buffer.Write((byte)(b >> 8));
                buffer.Write((byte)(b >> 16));
                buffer.Write((byte)(b >> 24));

                return;
            }

            if (value <= 1099511627775)
            {
                var a = (byte)252;
                var b = (byte)(value & 0xFF);
                var c = (uint)(value >> 8);
                var d = (ushort)(b << 8 | a);

                buffer.Write((byte)d);
                buffer.Write((byte)(d >> 8));

                buffer.Write((byte)c);
                buffer.Write((byte)(c >> 8));
                buffer.Write((byte)(c >> 16));
                buffer.Write((byte)(c >> 24));

                return;
            }

            if (value <= 281474976710655)
            {
                var a = (byte)253;
                var b = (byte)(value & 0xFF);
                var c = (byte)((value >> 8) & 0xFF);
                var d = (uint)(value >> 16);
                var e = (ushort)(c << 8 | b);

                buffer.Write(a);

                buffer.Write((byte)e);
                buffer.Write((byte)(e >> 8));

                buffer.Write((byte)d);
                buffer.Write((byte)(d >> 8));
                buffer.Write((byte)(d >> 16));
                buffer.Write((byte)(d >> 24));

                return;
            }

            if (value <= 72057594037927935)
            {
                var a = (byte)254;
                var b = value << 8;
                var c = b | a;

                buffer.Write((byte)c);
                buffer.Write((byte)(c >> 8));
                buffer.Write((byte)(c >> 16));
                buffer.Write((byte)(c >> 24));
                buffer.Write((byte)(c >> 32));
                buffer.Write((byte)(c >> 40));
                buffer.Write((byte)(c >> 48));
                buffer.Write((byte)(c >> 56));

                return;
            }

            buffer.Write(byte.MaxValue);

            buffer.Write((byte)value);
            buffer.Write((byte)(value >> 8));
            buffer.Write((byte)(value >> 16));
            buffer.Write((byte)(value >> 24));
            buffer.Write((byte)(value >> 32));
            buffer.Write((byte)(value >> 40));
            buffer.Write((byte)(value >> 48));
            buffer.Write((byte)(value >> 56));
        }

        public static long DecompressLong(this DeserializerBuffer deserializerBuffer)
        {
            var data = DecompressULong(deserializerBuffer);
            return ((long)(data >> 1)) ^ -((long)data & 1);
        }

        public static ulong DecompressULong(this DeserializerBuffer deserializerBuffer)
        {
            var a0 = deserializerBuffer.Take(1)[0];

            if (a0 < 241)
                return a0;

            var a1 = deserializerBuffer.Take(1)[0];

            if (a0 <= 248)
                return 240 + ((a0 - (ulong)241) << 8) + a1;

            var a2 = deserializerBuffer.Take(1)[0];

            if (a0 == 249)
                return 2288 + ((ulong)a1 << 8) + a2;

            var a3 = deserializerBuffer.Take(1)[0];

            if (a0 == 250)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16);

            var a4 = deserializerBuffer.Take(1)[0];

            if (a0 == 251)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24);

            var a5 = deserializerBuffer.Take(1)[0];

            if (a0 == 252)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32);

            var a6 = deserializerBuffer.Take(1)[0];

            if (a0 == 253)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40);

            var a7 = deserializerBuffer.Take(1)[0];

            if (a0 == 254)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48);

            var a8 = deserializerBuffer.Take(1)[0];

            if (a0 == 255)
                return a1 + (((ulong)a2) << 8) + (((ulong)a3) << 16) + (((ulong)a4) << 24) + (((ulong)a5) << 32) + (((ulong)a6) << 40) + (((ulong)a7) << 48) + (((ulong)a8) << 56);

            throw new IndexOutOfRangeException($"DecompressULong failed: {a0}");
        }
    }
}