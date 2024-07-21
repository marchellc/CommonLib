using CommonLib.Extensions;
using CommonLib.Pooling.Pools;

using System;
using System.Reflection;

namespace CommonLib.Pooling
{
    public class PoolableObject : IDisposable
    {
        private readonly object poolInstance;
        private readonly MethodInfo poolMethod;

        public PoolableObject()
        {
            var thisType = this.GetType();
            var poolType = typeof(ObjectPool<>).MakeGenericType(thisType);

            poolInstance = poolType.Property("Shared").Get();
            poolMethod = poolType.Method("Return");
        }

        internal bool isPooled;

        public bool IsPooled => isPooled;

        public virtual void OnPooled() { }
        public virtual void OnUnPooled() { }

        public void ToPool()
        {
            if (isPooled)
                return;

            poolMethod.Call(poolInstance, this);
        }

        public void Dispose()
        {
            if (isPooled)
                throw new ObjectDisposedException(GetType().Name);

            ToPool();
        }
    }
}