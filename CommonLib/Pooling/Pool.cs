using System.Collections.Generic;
using System;

using CommonLib.Extensions;

namespace CommonLib.Pooling
{
    public class Pool<T>
    {
        public uint InitialSize
        {
            get;
        }

        public virtual uint Size
        {
            get => Stack is null ? uint.MinValue : (uint)Stack.Count;
        }

        public bool IsUsingStack
        {
            get;
        }

        public Stack<T> Stack
        {
            get;
            private set;
        }

        public Pool(uint initialSize = uint.MinValue, bool usesStack = true)
        {
            InitialSize = initialSize;
            IsUsingStack = usesStack;

            Clear();
        }

        public virtual T Rent()
        {
            var value = GetNextValue();

            if (value is null)
                return value;

            OnRenting(value);

            return value;
        }

        public virtual void Return(T value)
        {
            if (value is null)
                return;

            OnReturning(value);

            Stack.TryPush(value);
        }

        public void Clear()
            => Clear(InitialSize);

        public virtual void Clear(uint newSize)
        {
            if (Stack != null)
                Stack?.Clear();
            else
                Stack = new Stack<T>();

            for (int i = 0; i < newSize; i++)
                Return(Construct());
        }

        public virtual void OnRenting(T value) { }
        public virtual void OnReturning(T value) { }

        public virtual T Construct() => throw new NotImplementedException();

        private T GetNextValue()
        {
            if (!Stack.TryPop(out var nextValue))
                nextValue = Construct();

            return nextValue;
        }
    }
}