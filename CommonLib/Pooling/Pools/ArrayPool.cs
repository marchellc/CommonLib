using CommonLib.Extensions;

using System.Collections.Generic;
using System.Linq;
using System;

namespace CommonLib.Pooling.Pools
{
    public class ArrayPool<T> : Pool<T[]>
    {
        public static ArrayPool<T> Shared { get; } = new ArrayPool<T>(uint.MinValue);

        private Dictionary<int, Stack<T[]>> arrayStacks;

        public override uint Size
        {
            get => (uint)(arrayStacks is null ? 0 : arrayStacks.Count);
        }

        public uint DefaultCapacity { get; set; } = 256;

        public ArrayPool(uint size) : base(size, false) { }

        public T[] Rent(int arraySize)
        {
            if (arraySize < 0)
                throw new ArgumentNullException(nameof(arraySize));

            if (!arrayStacks.TryGetValue(arraySize, out var arrayStack))
                arrayStack = arrayStacks[arraySize] = new Stack<T[]>();

            if (!arrayStack.TryPop(out var array))
                array = new T[arraySize];

            return array;
        }

        public T[] Rent(IEnumerable<T> values)
        {
            var array = Rent(values.Count());

            for (int i = 0; i < array.Length; i++)
                array[i] = values.ElementAt(i);

            return array;
        }

        public override T[] Rent()
            => Rent((int)DefaultCapacity);

        public T ReturnIndex(T[] value, int valueIndex)
        {
            var indexValue = value[valueIndex];
            Return(value);
            return indexValue;
        }

        public override void Return(T[] value)
        {
            if (!arrayStacks.TryGetValue(value.Length, out var arrayStack))
                throw new InvalidOperationException($"Cannot return an array that is not from this pool.");

            OnReturning(value);

            arrayStack.TryPush(value);
        }

        public override void OnReturning(T[] value)
        {
            for (int i = 0; i < value.Length; i++)
                value[i] = default;
        }

        public override void Clear(uint newSize)
        {
            if (arrayStacks != null)
                arrayStacks.Clear();
            else
                arrayStacks = new Dictionary<int, Stack<T[]>>((int)newSize);

            for (int i = 0; i < newSize; i++)
                arrayStacks[i] = new Stack<T[]>();
        }
    }
}