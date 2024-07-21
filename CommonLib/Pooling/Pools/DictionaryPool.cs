using CommonLib.Extensions;

using System.Collections.Generic;
using System.Linq;

namespace CommonLib.Pooling.Pools
{
    public class DictionaryPool<TKey, TElement> : Pool<Dictionary<TKey, TElement>>
    {
        public static DictionaryPool<TKey, TElement> Shared { get; } = new DictionaryPool<TKey, TElement>(10);

        public DictionaryPool(uint size) : base(size) { }

        public int MinSize { get; set; } = 256;

        public override Dictionary<TKey, TElement> Construct() 
            => new Dictionary<TKey, TElement>(MinSize);

        public Dictionary<TKey, TElement> Rent(IDictionary<TKey, TElement> dict)
        {
            var rentDict = Rent();
            rentDict.AddRange(dict);
            return rentDict;
        }

        public Dictionary<TKey, TElement> Rent(IEnumerable<KeyValuePair<TKey, TElement>> pairs)
        {
            var rentDict = Rent();
            rentDict.AddRange(pairs);
            return rentDict;
        }

        public KeyValuePair<TKey, TElement>[] ToArrayReturn(Dictionary<TKey, TElement> dict)
        {
            var array = dict.ToArray();
            Return(dict);
            return array;
        }

        public override void OnReturning(Dictionary<TKey, TElement> value)
            => value.Clear();
    }
}