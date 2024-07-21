using CommonLib.Extensions;

using System;

namespace CommonLib.Pooling.Pools
{
    public class ObjectPool<T> : Pool<T>
        where T : PoolableObject
    {
        private static readonly Type cachedType = typeof(T);

        public static ObjectPool<T> Shared { get; } = new ObjectPool<T>(64);

        public ObjectPool(uint size) : base(size) { }

        public override T Construct()
            => cachedType.Construct() as T;

        public override void OnRenting(T value)
        {
            value.isPooled = false;
            value.OnUnPooled();
        }

        public override void OnReturning(T value)
        {
            value.isPooled = false;
            value.OnPooled();
        }
    }
}