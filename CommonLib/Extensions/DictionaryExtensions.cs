using CommonLib.Pooling.Pools;

using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;

namespace CommonLib.Extensions
{
    public static class DictionaryExtensions
    {
        public static void ReturnToShared<TKey, TValue>(this Dictionary<TKey, TValue> dict)
            => DictionaryPool<TKey, TValue>.Shared.Return(dict);

        public static void ExternalModify<TKey, TValue>(this IDictionary<TKey, TValue> dict, Action<TKey, TValue, IDictionary<TKey, TValue>> action)
        {
            var copy = DictionaryPool<TKey, TValue>.Shared.Rent(dict);

            foreach (var key in copy.Keys)
                action.Call(key, copy[key], dict);

            copy.ReturnToShared();
        }

        public static void SetValues<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> predicate, TValue value)
        {
            foreach (var key in dict.Keys)
            {
                if (predicate.Call(key))
                    dict[key] = value;
            }
        }

        public static int RemoveKeys<TKey, TValue>(this IDictionary<TKey, TValue> dict, Func<TKey, bool> predicate)
        {
            var keys = dict.Keys.Where(predicate);
            var count = 0;

            foreach (var key in keys)
            {
                if (dict.Remove(key))
                    count++;
            }

            return count;
        }

        public static int FindKeyIndex(this IDictionary dictionary, object key)
        {
            for (int i = 0; i < dictionary.Count; i++)
            {
                if (dictionary.Keys.ElementOfIndex(i) == key)
                    return i;
            }

            return -1;
        }

        public static int FindKeyIndex<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key)
        {
            for (int i = 0; i < dictionary.Count; i++)
            {
                if (dictionary.Keys.ElementAt(i).Equals(key))
                    return i;
            }

            return -1;
        }

        public static int FindValueIndex(this IDictionary dictionary, object value)
        {
            for (int i = 0; i < dictionary.Count; i++)
            {
                if (dictionary.Values.ElementOfIndex(i) == value)
                    return i;
            }

            return -1;
        }

        public static KeyValuePair<TKey, TValue> PairOfIndex<TKey, TValue>(this IDictionary<TKey, TValue> dict, int index)
        {
            if (index >= dict.Count)
                return default;

            return dict.ElementAtOrDefault(index);
        }

        public static void SetIndex<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, int index, TKey key, TValue value)
        {
            if (index < 1 || index >= dictionary.Count)
                return;

            var copy = DictionaryPool<TKey, TValue>.Shared.Rent();

            for (int i = 0; i < dictionary.Count; i++)
            {
                if (i != index)
                {
                    var pair = dictionary.ElementAtOrDefault(i);
                    copy[pair.Key] = pair.Value;
                }
                else
                {
                    copy[key] = value;
                }
            }

            dictionary.Clear();
            dictionary.CopyFrom(copy);

            copy.ReturnToShared();
        }

        public static void SetIndexKey<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, int index, TKey key)
        {
            if (index < 1 || index >= dictionary.Count)
                return;

            var copy = DictionaryPool<TKey, TValue>.Shared.Rent();

            for (int i = 0; i < dictionary.Count; i++)
            {
                if (i != index)
                {
                    var pair = dictionary.ElementAtOrDefault(i);
                    copy[pair.Key] = pair.Value;
                }
                else
                {
                    var pair = dictionary.ElementAtOrDefault(i);
                    copy[key] = pair.Value;
                }
            }

            dictionary.Clear();
            dictionary.CopyFrom(copy);

            copy.ReturnToShared();
        }

        public static void SetIndexValue<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, int index, TValue value)
        {
            if (index < 1 || index >= dictionary.Count)
                return;

            var copy = DictionaryPool<TKey, TValue>.Shared.Rent();

            for (int i = 0; i < dictionary.Count; i++)
            {
                if (i != index)
                {
                    var pair = dictionary.ElementAtOrDefault(i);
                    copy[pair.Key] = pair.Value;
                }
                else
                {
                    var pair = dictionary.ElementAtOrDefault(i);
                    copy[pair.Key] = value;
                }
            }

            dictionary.Clear();
            dictionary.CopyFrom(copy);

            copy.ReturnToShared();
        }

        public static Dictionary<TKey, TValue> Copy<TKey, TValue>(this IDictionary<TKey, TValue> source)
            => new Dictionary<TKey, TValue>(source);

        public static void CopyFrom<TKey, TValue>(this IDictionary<TKey, TValue> target, IDictionary<TKey, TValue> source)
        {
            target.Clear();
            source.ForEach(p => target[p.Key] = p.Value);
        }

        public static void CopyFrom<TKey, TValue>(this IDictionary<TKey, TValue> target, IEnumerable<KeyValuePair<TKey, TValue>> source)
        {
            target.Clear();
            source.ForEach(p => target[p.Key] = p.Value);
        }

        public static void InitValue<TKey, TValue>(this IDictionary<TKey, TValue> target, TKey key) where TValue : new()
        {
            if (!target.ContainsKey(key))
                target[key] = new TValue();
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> collection)
        {
            var dict = new Dictionary<TKey, TValue>();

            foreach (var pair in collection)
            {
                if (pair.Key is null)
                    continue;

                if (dict.ContainsKey(pair.Key))
                    dict[pair.Key] = pair.Value;

                else dict.Add(pair.Key, pair.Value);
            }

            return dict;
        }

        public static Dictionary<TKey, TValue> ToDictionary<TKey, TValue>(this IEnumerable<TValue> values, Func<TValue, TKey> keySelector)
        {
            if (keySelector is null)
                throw new ArgumentNullException($"This method requires the key selector.");

            var dict = new Dictionary<TKey, TValue>();

            foreach (var value in values)
            {
                var key = keySelector(value);

                if (key is null)
                    continue;

                dict[key] = value;
            }

            return dict;
        }

        public static bool ContainsValue<TKey, TValue>(this IDictionary<TKey, TValue> dict, TValue value)
            => dict.Values.Contains(value);

        public static bool TryGetKey<TKey, TValue>(this IDictionary<TKey, TValue> dict, TValue value, out TKey key)
        {
            if (dict is null)
                throw new ArgumentNullException(nameof(dict));

            if (value is null)
                throw new ArgumentNullException(nameof(value));

            foreach (var pair in dict)
            {
                if (pair.Value.Equals(value))
                {
                    key = pair.Key;
                    return true;
                }
            }

            key = default;
            return false;
        }
    }
}