using System;

namespace CommonLib.Serialization.Buffers
{
    public class DeserializerBuffer : IDisposable
    {
        private bool _disposed;

        private byte[] _buffer;
        private byte[] _data;

        private int _index;

        public int Size => _data?.Length ?? -1;
        public int Index => _index;

        public byte[] Buffer => _buffer;

        public bool IsDisposed => _disposed;

        public void SetData(byte[] data)
        {
            if (_disposed)
                throw new ObjectDisposedException("DeserializerBuffer");

            if (data is null)
                throw new ArgumentNullException(nameof(data));

            _data = data;
            _index = 0;
        }

        public byte[] Take(int count)
        {
            if (_disposed)
                throw new ObjectDisposedException("DeserializerBuffer");

            if (_data is null)
                throw new InvalidOperationException($"This buffer has not been set up properly.");

            var newIndex = _index + count;

            if (newIndex > _data.Length)
                throw new InvalidOperationException($"The requested amount was bigger than the data pack ({count} / {_index} / {_data.Length})");

            if (_buffer is null || _buffer.Length != count)
            {
                if (_buffer is null)
                    _buffer = new byte[count];
                else
                    Array.Resize(ref _buffer, count);
            }

            for (int i = 0; i < count; i++)
                _buffer[i] = _data[_index++];

            return _buffer;
        }

        public void Dispose()
        {
            if (_disposed)
                throw new ObjectDisposedException("DeserializerBuffer");

            _data = null;
            _index = 0;
            _buffer = null;
        }
    }
}