using CommonLib.Extensions;

using System.Collections.Generic;
using System.Linq;

namespace CommonLib.Pooling.Pools
{
    public class HashSetPool<TElement> : Pool<HashSet<TElement>>
    {
        public static HashSetPool<TElement> Shared { get; } = new HashSetPool<TElement>(10);

        public HashSetPool(uint size) : base(size) { }

        public int MinSize { get; set; } = 256;

        public override HashSet<TElement> Construct()
            => new HashSet<TElement>(MinSize);

        public HashSet<TElement> Rent(IEnumerable<TElement> elements)
        {
            var set = Rent();
            set.AddRange(elements);
            return set;
        }

        public TElement[] ToArrayReturn(HashSet<TElement> set)
        {
            var array = set.ToArray();
            Return(set);
            return array;
        }

        public override void OnReturning(HashSet<TElement> value)
            => value.Clear();
    }
}