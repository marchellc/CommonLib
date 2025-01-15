using System.Collections.Concurrent;
using System.Collections.Generic;

namespace CommonLib.Extensions
{
    public static class QueueExtensions
    {
        public static Queue<T> ToQueue<T>(this IEnumerable<T> values)
            => new Queue<T>(values);

        public static ConcurrentQueue<T> ToConcurrentQueue<T>(this IEnumerable<T> values)
            => new ConcurrentQueue<T>(values);

        public static bool TryDequeue<T>(this Queue<T> queue, out T value)
        {
            if (queue.Count <= 0)
            {
                value = default;
                return false;
            }

            value = queue.Dequeue();
            return true;
        }

        public static void EnqueueMany<T>(this ConcurrentQueue<T> queue, IEnumerable<T> source)
        {
            foreach (var item in source)
                queue.Enqueue(item);
        }

        public static void EnqueueMany<T>(this Queue<T> queue, IEnumerable<T> source)
        {
            foreach (var item in source)
                queue.Enqueue(item);
        }

        public static void Remove<T>(this Queue<T> queue, T value)
        {
            var queueValues = new List<T>(queue.Count);

            queueValues.Remove(value);

            foreach (var queueValue in queueValues)
                queue.Enqueue(queueValue);
            
            queueValues.Clear();
        }

        public static void Clear<T>(this ConcurrentQueue<T> queue)
        {
            while (queue.TryDequeue(out _))
                continue;
        }
    }
}