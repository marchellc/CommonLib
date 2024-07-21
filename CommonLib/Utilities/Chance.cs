using CommonLib.Extensions;

using System;
using System.Collections.Generic;
using System.Linq;

namespace CommonLib.Utilities
{
    public static class Chance
    {
        private static readonly bool[] boolArray = [true, false];

        public static KeyValuePair<TKey, TValue> PickPair<TKey, TValue>(IDictionary<TKey, TValue> dict, Func<TKey, TValue, int> weightPicker, bool validateWeight = false)
            => Pick(dict, pair => weightPicker.Call(pair.Key, pair.Value), validateWeight);

        public static TKey PickKey<TKey, TValue>(IDictionary<TKey, TValue> dict, Func<TKey, TValue, int> weightPicker, bool validateWeight = false)
            => PickPair(dict, weightPicker, validateWeight).Key;

        public static TValue PickValue<TKey, TValue>(IDictionary<TKey, TValue> dict, Func<TKey, TValue, int> weightPicker, bool validateWeight = false)
            => PickPair(dict, weightPicker, validateWeight).Value;

        public static bool PickBool(int trueChance = 50, int falseChance = 50, bool validateChance = false)
            => Pick(boolArray, value => value ? trueChance : falseChance, validateChance);

        public static T Pick<T>(IEnumerable<T> items, Func<T, int> weightPicker, bool validateWeight = false)
        {
            if (items is null)
                throw new ArgumentNullException(nameof(items));

            var list = items.ToList();

            if (list.Count <= 0)
                throw new ArgumentException($"Cannot pick from an empty list.");

            if (list.Count == 1)
                return list[0];

            var total = list.Sum(val => weightPicker.Call(val));

            if (total != 100 && validateWeight)
                throw new InvalidOperationException($"Cannot pick from list; it's chance sum is not equal to a hundred ({total}).");

            return list[PickIndex(total, list.Count, index => weightPicker.Call(list[index]))];
        }

        public static int PickIndex(int total, int size, Func<int, int> picker)
        {
            var choice = Generator.Instance.GetInt32(0, total);
            var sum = 0;

            for (int i = 0; i < size; i++)
            {
                var weight = picker.Call(i);

                for (int x = sum; x < weight + sum; x++)
                {
                    if (x >= choice)
                        return i;
                }

                sum += weight;
            }

            return 0;
        }
    }
}
