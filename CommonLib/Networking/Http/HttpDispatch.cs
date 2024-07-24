using CommonLib.Pooling.Pools;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace CommonLib.Networking.Http
{
    public class HttpDispatch
    {
        public class HttpDispatchItem
        {
            public volatile HttpRequestMessage Request;
            public volatile HttpResponseMessage Response;

            public volatile string StringContent;
            public volatile byte[] RawContent;

            public volatile object CustomValue;

            public volatile Exception Error;

            public volatile Action<HttpDispatchItem> Callback;

            public HttpDispatchItem(HttpRequestMessage requestMessage, Action<HttpDispatchItem> callback, object customValue = null)
            {
                Request = requestMessage;
                Callback = callback;
                CustomValue = customValue;
            }
        }

        private static volatile ConcurrentQueue<HttpDispatchItem> _dispatch;
        private static volatile HttpClient _client;

        static HttpDispatch()
        {
            _dispatch = new ConcurrentQueue<HttpDispatchItem>();
            _client = new HttpClient();
        }

        public static void Create(Action<HttpRequestMessage> builder, object customValue, Action<HttpDispatchItem> callback)
        {
            var msg = new HttpRequestMessage();

            builder?.Invoke(msg);

            Create(msg, customValue, callback);
        }

        public static void Create(HttpRequestMessage message, object customValue, Action<HttpDispatchItem> callback)
            => _dispatch.Enqueue(new HttpDispatchItem(message, callback, customValue));

        public static void UpdateRequests(int maxCount = -1)
        {
            if (_dispatch.IsEmpty)
                return;

            var handled = ListPool<HttpDispatchItem>.Shared.Rent();

            Task.Run(async () =>
            {
                var count = 0;

                while (_dispatch.TryDequeue(out var item) && (maxCount < 1 || count < maxCount))
                {
                    try
                    {
                        item.Response = await _client.SendAsync(item.Request);

                        if (item.Response.Content != null)
                        {
                            item.StringContent = await item.Response.Content.ReadAsStringAsync();
                            item.RawContent = await item.Response.Content.ReadAsByteArrayAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        item.Error = ex;
                    }

                    handled.Add(item);
                    count++;
                }
            }).ContinueWith(_ =>
            {
                foreach (var req in handled)
                    req.Callback?.Invoke(req);

                ListPool<HttpDispatchItem>.Shared.Return(handled);
            }, TaskContinuationOptions.ExecuteSynchronously);
        }
    }
}