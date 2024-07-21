using System.Collections.Generic;

namespace CommonLib.Pooling.Pools
{
    public class ListPool<TElement> : Pool<List<TElement>>
    {
        public static ListPool<TElement> Shared { get; } = new ListPool<TElement>(10);

        public ListPool(uint size) : base(size) { }

        public int MinSize { get; set; } = 256;

        public override List<TElement> Construct()
            => new List<TElement>(MinSize);

        public List<TElement> Rent(IEnumerable<TElement> elements)
        {
            var list = Rent();
            list.AddRange(elements);
            return list;
        }

        public TElement[] ToArrayReturn(List<TElement> list)
        {
            var array = list.ToArray();
            Return(list);
            return array;
        }

        public override void OnReturning(List<TElement> value)
            => value.Clear();
    }
}