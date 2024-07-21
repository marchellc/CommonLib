using CommonLib.Caching;

using System;

namespace CommonLib.Utilities.Generation
{
    public class UniqueGenerator<T>
    {
        private Func<T> generator;
        private ICache<T> cache;

        public UniqueGenerator(ICache<T> cache)
        {
            if (cache is null)
                throw new ArgumentNullException(nameof(cache));

            this.cache = cache;
        }

        public void SetGenerator(Func<T> generator)
        {
            if (generator is null)
                throw new ArgumentNullException(nameof(generator));

            this.generator = generator;
        }

        public T Next()
        {
            var value = generator();

            while (cache.Contains(value))
                value = generator();

            cache.Add(value);
            return value;
        }

        public void Free(T value)
            => cache.Remove(value);

        public void FreeAll()
            => cache.RemoveAll(value => true);

        public void FreeAll(Func<T, bool> predicate)
            => cache.RemoveAll(predicate);
    }
}