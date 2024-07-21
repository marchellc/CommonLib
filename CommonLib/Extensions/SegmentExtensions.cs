using System;

namespace CommonLib.Extensions
{
    public static class SegmentExtensions
    {
        public static ArraySegment<T> ToSegment<T>(this T[] array)
            => new ArraySegment<T>(array);

        public static ArraySegment<T> ToSegment<T>(this T[] array, int offset, int count)
            => new ArraySegment<T>(array, offset, count);

        public static T[] ToArray<T>(this ArraySegment<T> segment)
        {
            var array = new T[segment.Count];
            Array.Copy(segment.Array, segment.Offset, array, 0, segment.Count);
            return array;
        }
    }
}