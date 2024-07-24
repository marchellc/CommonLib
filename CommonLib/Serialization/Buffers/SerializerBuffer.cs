using CommonLib.Pooling.Pools;

using System;
using System.Collections.Generic;

namespace CommonLib.Serialization.Buffers
{
    public class SerializerBuffer : IDisposable
    {
        public enum BufferStatus
        {
            Writing,
            Disposed
        }

        public BufferStatus Status { get; private set; }

        public List<byte> Buffer { get; private set; }
        public byte[] Data { get; private set; }

        public int Size => Buffer?.Count ?? -1;
        public int DataSize => Data?.Length ?? -1;

        public bool IsDisposed => Status is BufferStatus.Disposed;
        public bool IsWriting => Status is BufferStatus.Writing;

        public SerializerBuffer()
        {
            Data = null;
            Buffer = ListPool<byte>.Shared.Rent();
            Status = BufferStatus.Writing;
        }

        public void Write(byte value)
        {
            if (!IsWriting)
                throw new InvalidOperationException($"Cannot write data to an inactive buffer");

            Buffer.Add(value);
        }

        public void Retrieve()
        {
            if (!IsDisposed)
                throw new InvalidOperationException($"This buffer has not been disposed yet");

            Data = null;
            Buffer = ListPool<byte>.Shared.Rent();
            Status = BufferStatus.Writing;
        }

        public void Dispose()
        {
            if (Buffer != null)
            {
                Data = ListPool<byte>.Shared.ToArrayReturn(Buffer);
                Buffer = null;
            }

            Status = BufferStatus.Disposed;
        }
    }
}