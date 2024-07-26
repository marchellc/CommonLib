using System;

namespace CommonLib.Utilities
{
    public struct Optional<T>
    {
        private readonly T _value;
        private readonly bool _status;

        public T Value
        {
            get
            {
                if (!_status)
                    throw new Exception($"No value has been set.");

                return _value;
            }
        }

        public bool HasValue => _status;

        public Optional()
        {
            _value = default;
            _status = false;
        }

        public Optional(T value)
        {
            _value = value;
            _status = true;
        }

        public T GetValueOrDefault(T defaultValue = default)
            => _status ? _value : defaultValue;
    }
}