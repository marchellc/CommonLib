using System;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using CommonLib.EventSync;
using CommonLib.Extensions;
using CommonLib.Http.Server.Events;

namespace CommonLib.Http.Server;

public class HttpServer : IDisposable
{
    private volatile int id;

    private volatile bool http;
    private volatile bool https;
    
    private volatile string prefix;
    private volatile string realIpHeader = "X-Real-IP";
    
    private volatile HttpListener listener;
    private volatile EventSyncHandler eventSyncHandler;
    
    private volatile ConcurrentDictionary<int, HttpRoute> routes = new ConcurrentDictionary<int, HttpRoute>();
    
    public string Prefix
    {
        get => prefix;
        set
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentNullException(nameof(value));
            
            value = value.Replace("http://", "")
                         .Replace("https://", "");    
        
            if (value.EndsWith("/"))
                value = prefix.Substring(0, prefix.Length - 1);
        
            prefix = value;
        }
    }

    public string RealIpHeader
    {
        get => realIpHeader;
        set => realIpHeader = value;
    }

    public bool EnableHttp
    {
        get => http;
        set => http = value;
    }

    public bool EnableHttps
    {
        get => https;
        set => https = value;
    }
    
    public bool IsListening => listener?.IsListening ?? false;
    
    public string HttpPrefix => $"http://{prefix}/";
    public string HttpsPrefix => $"https://{prefix}/";
    
    public HttpListener Listener => listener;
    public EventSyncHandler EventSyncHandler => eventSyncHandler;
    
    public IReadOnlyDictionary<int, HttpRoute> Routes => routes;

    public HttpServer(string prefix, bool listenHttp, bool listenHttps)
    {
        if (!listenHttp && !listenHttps)
            throw new Exception("You must listen on HTTP or HTTPS");
        
        Prefix = prefix;
        
        EnableHttp = listenHttp;
        EnableHttps = listenHttps;
        
        eventSyncHandler = new EventSyncHandler();
        eventSyncHandler.OnEvent += OnEvent;
    }

    public void Start()
    {
        if (string.IsNullOrWhiteSpace(prefix))
            throw new Exception("You must provide a listening prefix");
        
        if (!EnableHttp && !EnableHttps)
            throw new Exception("You must listen on HTTP or HTTPS");
        
        listener = new HttpListener();
        
        if (EnableHttp)
            listener.Prefixes.Add(HttpPrefix);
        
        if (EnableHttps)
            listener.Prefixes.Add(HttpsPrefix);

        listener.Start();

        Task.Run(UpdateAsync);
    }

    public void Dispose()
    {
        if (listener != null)
        {
            if (listener.IsListening)
                listener.Stop();
            
            listener.Close();
            listener = null;
        }
        
        if (eventSyncHandler != null)
        {
            eventSyncHandler.OnEvent -= OnEvent;
            
            eventSyncHandler.Dispose();
            eventSyncHandler = null;
        }
        
        routes.Clear();
    }

    public void RemoveAllRoutes()
        => routes.Clear();

    public int RemoveRoutes<T>() where T : HttpRoute, new()
        => RemoveRoutes(typeof(T));
    
    public int RemoveRoutes(Type routeType)
    {
        if (routeType is null)
            throw new ArgumentNullException(nameof(routeType));
        
        if (!routeType.InheritsType<HttpRoute>())
            throw new Exception($"Route type {routeType.FullName} is not a HttpRoute");

        var count = 0;

        foreach (var route in routes)
        {
            if (route.Value.GetType() == routeType && routes.TryRemove(route.Key, out _))
            {
                count++;
            }
        }

        return count;
    }

    public bool RemoveRoute(HttpRoute route)
    {
        if (route is null)
            throw new ArgumentNullException(nameof(route));
        
        if (string.IsNullOrWhiteSpace(route.Url))
            throw new ArgumentNullException(nameof(route.Url));

        if (string.IsNullOrWhiteSpace(route.FixedUrl))
            throw new Exception($"Attempted to remove an unregistered route");

        var removed = false;
        
        foreach (var other in routes)
        {
            if (other.Value.FixedUrl == route.FixedUrl)
                removed |= routes.TryRemove(other.Key, out _);
        }

        route.FixedUrl = null;
        return removed;
    }
    
    public int AddRoute<T>() where T : HttpRoute, new()
        => AddRoute(new T());

    public int AddRoute(Type routeType)
    {
        if (routeType is null)
            throw new ArgumentNullException(nameof(routeType));
        
        if (!routeType.InheritsType<HttpRoute>())
            throw new Exception($"Route type {routeType.FullName} is not a HttpRoute");
        
        return AddRoute(Activator.CreateInstance(routeType) as HttpRoute);
    }

    public int AddRoute(HttpRoute route)
    {
        if (route is null)
            throw new ArgumentNullException(nameof(route));
        
        if (string.IsNullOrWhiteSpace(route.Url))
            throw new ArgumentNullException(nameof(route.Url));

        if (string.IsNullOrWhiteSpace(route.FixedUrl))
        {
            var url = route.Url;

            if (url.StartsWith("/"))
                url = url.Substring(1, url.Length - 1);

            if (url.EndsWith("/"))
                url = url.Substring(0, url.Length - 1);

            route.FixedUrl = url;
        }

        foreach (var other in routes)
        {
            if (other.Value.FixedUrl == route.FixedUrl)
                throw new Exception($"Another route with the same URL has already been registered ({route.Url} / {route.FixedUrl})");
        }

        var id = this.id++;

        routes.TryAdd(id, route);
        return id;
    }

    private async Task UpdateAsync()
    {
        while (IsListening)
        {
            HttpListenerContext context = null;
            
            try
            {
                CommonLog.Debug("Http Server", $"Waiting for a request ...");
                
                context = await listener.GetContextAsync();

                if (context is null)
                {
                    CommonLog.Debug("Http Server", $"Received null context");
                    continue;
                }
                
                CommonLog.Debug("Http Server", $"Received context: {context.Request.RawUrl} ({context.Request.HttpMethod}) from {context.Request.RemoteEndPoint}");
                
                var raw = context.Request.RawUrl;
                var parameters = new ConcurrentDictionary<string, string>();
                var index = raw.IndexOf('?');
                var parsedUrl = raw;

                if (index > -1)
                {
                    parsedUrl = "";
                    
                    for (int i = 0; i < raw.Length; i++)
                    {
                        if (i == index)
                            break;

                        parsedUrl += raw[i];
                    }

                    var split = raw.Split('?').Skip(1).ToArray();

                    foreach (var arg in split)
                    {
                        if (arg.TrySplit('=', true, 2, out var parts))
                            parameters.TryAdd(parts[0], parts[1]);
                        else
                            parameters.TryAdd(arg, string.Empty);
                    }
                }
                
                CommonLog.Debug("Http Server", $"Parsed URL: {parsedUrl}");

                HttpRoute targetRoute = null;

                foreach (var route in Routes)
                {
                    if (route.Value.Methods is { Length: > 0 } && route.Value.Methods.All(x => !string.Equals(x.Method, context.Request.HttpMethod, StringComparison.CurrentCultureIgnoreCase)))
                        continue;
                    
                    var routeUrl = parsedUrl;
                    
                    if (route.Value.FixedUrl.Contains("/{") && route.Value.FixedUrl.Contains("}"))
                    {
                        route.Value.FixedUrl.TrySplit('/', true, null, out var routeParts);
                        parsedUrl.TrySplit('/', true, null, out var urlParts);

                        if (routeParts != null && urlParts != null)
                        {
                            if (urlParts.Length == routeParts.Length)
                            {
                                for (int i = 0; i < urlParts.Length; i++)
                                {
                                    var routeStr = routeParts[i];
                                    var urlStr = urlParts[i];

                                    if (routeStr.StartsWith("{") && routeStr.EndsWith("}"))
                                    {
                                        var name = routeStr.Replace("{", "").Replace("}", "");
                                        var value = urlStr.Replace("%20", " ");
                                        
                                        if (string.IsNullOrWhiteSpace(value) || string.IsNullOrWhiteSpace(name) || parameters.ContainsKey(name))
                                            continue;

                                        parameters.TryAdd(name, value);
                                    }
                                }

                                routeUrl = route.Value.FixedUrl;
                            }
                        }
                    }
                    
                    if (route.Value.FixedUrl == routeUrl || route.Value.IsMatch(raw, routeUrl))
                    {
                        route.Value.ParseParameters(raw, routeUrl, parameters);
                        
                        targetRoute = route.Value;
                        
                        CommonLog.Debug("Http Server", $"Found route: {route.Value.FixedUrl}");
                        break;
                    }
                }

                if (targetRoute is null)
                {
                    CommonLog.Debug("Http Server", $"No route was found");
                    
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    context.Response.StatusDescription = "Not Found";
                    
                    context.Response.ContentType = "text/plain";

                    using (var sw = new StreamWriter(context.Response.OutputStream))
                        await sw.WriteLineAsync($"Could not find route {context.Request.RawUrl}");
                    
                    context.Response.Close();
                    continue;
                }

                var wrapper = await HttpContext.GetAsync(context, this, targetRoute);

                if (wrapper is null)
                {
                    CommonLog.Debug("Http Server", $"Wrapper content is null");
                    
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    context.Response.StatusDescription = "Null Wrapper";
                    
                    context.Response.ContentType = "text/plain";

                    using (var sw = new StreamWriter(context.Response.OutputStream))
                        await sw.WriteLineAsync($"Failed to create a context wrapper");
                    
                    context.Response.Close();
                    continue;
                }
                
                EventSyncHandler.Create(new ContextReceivedEvent(this, wrapper));
                
                CommonLog.Debug("Http Server", $"Enqueued event");
            }
            catch (Exception ex)
            {
                EventSyncHandler.Create(new ErrorReceivedEvent(this, ex));

                try
                {
                    if (context != null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        context.Response.StatusDescription = "Server Error";
                        
                        context.Response.ContentType = "text/plain";

                        using (var sw = new StreamWriter(context.Response.OutputStream))
                            await sw.WriteLineAsync(ex.ToString());
                        
                        context.Response.Close();
                    }
                }
                catch
                {
                    // ignored
                }

                CommonLog.Error("Http Server", ex);
            }
        }
    }

    private static void OnEvent(EventSyncBase eventSync)
    {
        if (eventSync is null)
            return;

        if (eventSync is not ContextReceivedEvent contextReceivedEvent) 
            return;
        
        if (contextReceivedEvent.Context?.TargetRoute is null)
            return;

        contextReceivedEvent.Context.TargetRoute.OnRequest(contextReceivedEvent);
    }
}