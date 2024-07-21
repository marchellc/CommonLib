using System;
using System.Collections.Generic;

namespace CommonLib.Caching
{
    public interface ICache<T>
    {
        int Size { get; }

        T Find(Func<T, bool> predicate);

        IEnumerable<T> FindAll(Func<T, bool> predicate);
        IEnumerable<T> GetAll();

        bool TryFind(Func<T, bool> predicate, out T value);

        bool Contains(T value);
        bool Remove(T value);
        bool Add(T value);

        int RemoveAll(Func<T, bool> predicate);
        int RemoveAll(IEnumerable<T> values);
    }
}