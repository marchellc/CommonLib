using System;

using CommonLib.EventSync;
using CommonLib.Extensions;
using CommonLib.Http.Client.Events;

using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;

namespace CommonLib.Http.Client;

public class HttpClient
{
    private volatile System.Net.Http.HttpClient client;
    private volatile EventSyncHandler eventSyncHandler;
    
    private volatile ConcurrentQueue<HttpRequest> requestQueue = new();

    public EventSyncHandler EventSyncHandler => eventSyncHandler;
    
    public int RequestCount => requestQueue.Count;

    public HttpClient(Func<System.Net.Http.HttpClient> httpClientFactory = null)
    {
        if (httpClientFactory is null)
            client = new System.Net.Http.HttpClient();
        else
            client = httpClientFactory();
        
        eventSyncHandler = new EventSyncHandler();
        eventSyncHandler.OnEvent += OnEvent;

        Task.Run(UpdateAsync);
    }
    
    public void PostWithMultipart(string url, Action<MultipartContent> multipartBuilder, Action<HttpRequest> callback,
        object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        
        if (multipartBuilder is null)
            throw new ArgumentNullException(nameof(multipartBuilder));
        
        CreateWithPayload(_ =>
        {
            var content = new MultipartContent();

            multipartBuilder(content);
            return content;
        }, url, HttpMethod.Post, callback, state, headers);
    }

    public void PostWithStream(string url, Stream payload, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));
        
        CreateWithPayload(_ => new StreamContent(payload), url, HttpMethod.Post, callback, state, headers);
    }

    public void PostWithBytes(string url, byte[] payload, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));
        
        CreateWithPayload(_ => new ByteArrayContent(payload), url, HttpMethod.Post, callback, state, headers);
    }

    public void PostWithText(string url, string payload, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));
        
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentNullException(nameof(payload));
        
        CreateWithPayload(_ => new StringContent(payload), url, HttpMethod.Post, callback, state, headers);
    }

    public void GetWithMultipart(string url, Action<MultipartContent> multipartBuilder, Action<HttpRequest> callback,
        object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        
        if (multipartBuilder is null)
            throw new ArgumentNullException(nameof(multipartBuilder));
        
        CreateWithPayload(_ =>
        {
            var content = new MultipartContent();

            multipartBuilder(content);
            return content;
        }, url, HttpMethod.Get, callback, state, headers);
    }

    public void GetWithStream(string url, Stream payload, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));
        
        CreateWithPayload(_ => new StreamContent(payload), url, HttpMethod.Get, callback, state, headers);
    }

    public void GetWithBytes(string url, byte[] payload, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));
        
        if (payload is null)
            throw new ArgumentNullException(nameof(payload));
        
        CreateWithPayload(_ => new ByteArrayContent(payload), url, HttpMethod.Get, callback, state, headers);
    }

    public void GetWithText(string url, string payload, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));
        
        if (string.IsNullOrWhiteSpace(payload))
            throw new ArgumentNullException(nameof(payload));
        
        CreateWithPayload(_ => new StringContent(payload), url, HttpMethod.Get, callback, state, headers);
    }

    public void Get(string url, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));
        
        Create(new HttpRequestMessage(HttpMethod.Get, url), callback, state, headers);
    }
    
    public void CreateWithPayload(Func<HttpRequestMessage, HttpContent> contentFactory, string url,
        HttpMethod method, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (string.IsNullOrEmpty(url))
            throw new ArgumentNullException(nameof(url));
        
        if (contentFactory is null)
            throw new ArgumentNullException(nameof(contentFactory));
        
        if (method is null)
            throw new ArgumentNullException(nameof(method));
        
        var message = new HttpRequestMessage(method, url);
        var content = contentFactory(message);
        
        if (content != null)
            message.Content = content;
        
        Create(message, callback, state, headers);
    }

    public void Create(HttpRequestMessage message, Action<HttpRequest> callback, object state = null, IEnumerable<KeyValuePair<string, string>> headers = null)
    {
        if (message is null)
            throw new ArgumentNullException(nameof(message));
        
        var context = HttpContext.Get(message);

        if (headers != null)
        {
            foreach (var header in headers)
            {
                context.Request.Headers.Add(header.Key, header.Value);
            }
        }
        
        requestQueue.Enqueue(new HttpRequest
        {
            Context = context,
            Callback = callback,
            State = state
        });
    }

    public void Dispose()
    {
        if (client != null)
        {
            var instance = client;

            client = null;
            
            instance.Dispose();
        }

        if (eventSyncHandler != null)
        {
            eventSyncHandler.OnEvent -= OnEvent;

            eventSyncHandler.Dispose();
            eventSyncHandler = null;
        }

        if (requestQueue != null)
        {
            requestQueue.Clear();
            requestQueue = null;
        }
    }

    private async Task UpdateAsync()
    {
        while (client != null)
        {
            try
            {
                HttpRequest request = null;
                
                while (requestQueue.TryDequeue(out request))
                {
                    try
                    {
                        if (request?.Context?.Request is null)
                            continue;

                        var response = await client.SendAsync(request.Context.Request);
                        
                        if (response is null)
                            continue;
                        
                        await request.Context.OnResponseReceived(response, null);
                    }
                    catch (Exception ex)
                    {
                        await request?.Context?.OnResponseReceived(null, ex);
                    }

                    if (request?.Context?.Response != null)
                        eventSyncHandler.Create(new ResponseReceivedEvent(request.Context, request));
                }
            }
            catch
            {
                // ignored
            }
        }
    }

    private static void OnEvent(EventSyncBase eventSync)
    {
        if (eventSync is null)
            return;

        if (eventSync is ResponseReceivedEvent responseReceivedEvent)
        {
            responseReceivedEvent.Request?.Callback?.Invoke(responseReceivedEvent.Request);
        }
    }
}