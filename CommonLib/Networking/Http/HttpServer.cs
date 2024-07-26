using CommonLib.Extensions;
using CommonLib.Logging;

using System;
using System.Net;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;

namespace CommonLib.Networking.Http
{
    public class HttpServer : IDisposable
    {
        private volatile HttpListener _listener = null;
        private volatile HashSet<HttpRoute> _routes = new HashSet<HttpRoute>();
        private volatile LogOutput _log;

        public bool IsListening => _listener != null && _listener.IsListening;

        public IEnumerable<HttpRoute> Routes => _routes;

        public event Action<Exception> OnError;

        public void Start(IEnumerable<string> prefixes)
        {
            try
            {
                _listener = new HttpListener();

                _log = new LogOutput("Http Server");
                _log.Setup();

                if (prefixes != null)
                    _listener.Prefixes.AddRange(prefixes);

                if (_listener.Prefixes.Count < 1)
                    throw new InvalidOperationException($"You have to set at least one prefix to listen on.");

                _listener.Start();
                _log.Info("Server started.");

                InternalReceive();
            }
            catch (Exception ex)
            {
                _log.Error(ex);

                OnError?.Invoke(ex);
            }
        }

        public void AddRoute<TRoute>() where TRoute : HttpRoute
            => AddRoute(typeof(TRoute));

        public void AddRoute(Type routeType)
        {
            if (routeType is null)
                throw new ArgumentNullException(nameof(routeType));

            if (!routeType.InheritsType<HttpRoute>() || routeType == typeof(HttpRoute))
                throw new InvalidOperationException($"Only types that inherit from HttpRoute can be used.");

            AddRoute(routeType.Construct<HttpRoute>());
        }

        public void AddRoute(HttpRoute route)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            _routes.Add(route);
        }

        public void RemoveRoutes<TRoute>()
            => RemoveRoutes(typeof(TRoute));

        public void RemoveRoutes(Type type)
        {
            if (type is null)
                throw new ArgumentNullException(nameof(type));

            _routes.RemoveWhere(r => r.GetType() == type);
        }

        public void RemoveRoute(string route)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            _routes.RemoveWhere(r => r.Url == route);
        }

        public void RemoveRoute(HttpRoute route)
        {
            if (route is null)
                throw new ArgumentNullException(nameof(route));

            _routes.Remove(route);
        }

        public void ClearRoutes()
            => _routes.Clear();

        public void Dispose()
        {
            try
            {
                if (_listener != null)
                {
                    if (_listener.IsListening)
                        _listener.Stop();

                    _listener = null;
                }

                _routes?.Clear();
            }
            catch (Exception ex)
            {
                _log.Error(ex);
                OnError?.Invoke(ex);
            }
        }

        private void InternalHandleRequest(HttpListenerContext ctx)
        {
            if (ctx is null)
                return;

            Task.Run(async () =>
            {
                foreach (var route in _routes)
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(route?.Url))
                            continue;

                        InternalProcessUrl(ctx.Request.RawUrl, route.Url, out var url, out var parameters);

                        if (route.Url != url)
                            continue;

                        if (route.Methods != null && !route.Methods.Any(m => m.ToLower() == ctx.Request.HttpMethod.ToLower()))
                            continue;

                        if (route.RequiredParameters != null && route.RequiredParameters.Any(name => !parameters.ContainsKey(name)))
                            continue;

                        var bytes = default(byte[]);

                        if (route.ReadContent)
                            bytes = await ctx.Request.ReadBytesAsync();

                        ctx.Response.KeepAlive = true;

                        switch (ctx.Request.HttpMethod)
                        {
                            case "GET":
                                await route.InvokeGetAsync(new HttpRequest(ctx.Request, parameters, bytes), ctx.Response);
                                return;

                            case "POST":
                                await route.InvokePostAsync(new HttpRequest(ctx.Request, parameters, bytes), ctx.Response);
                                return;

                            default:
                                await route.InvokeOtherAsync(ctx.Request.HttpMethod, new HttpRequest(ctx.Request, parameters, bytes), ctx.Response);
                                return;
                        }
                    }
                    catch (Exception ex)
                    {
                        _log.Error($"An error occured while invoking HTTP route ({ctx.Request.RawUrl}):\n{ex}");

                        OnError?.Invoke(ex);

                        ctx.Response.SetCode(HttpStatusCode.InternalServerError, $"An error occured while processing your request");

                        await ctx.Response.WriteStringAsync("An error occured while processing your request");

                        return;
                    }
                }

                ctx.Response.SetCode(HttpStatusCode.NotImplemented, "The specified route has not been found.");

                await ctx.Response.WriteStringAsync($"Not Implemented: {ctx.Request.HttpMethod} {ctx.Request.RawUrl}");
            });
        }

        private void InternalReceive()
            => _listener.BeginGetContext(InternalCallback, null);

        private void InternalCallback(IAsyncResult result)
        {
            if (_listener.IsListening)
            {
                try
                {
                    InternalHandleRequest(_listener.EndGetContext(result));
                }
                catch { }

                InternalReceive();
            }
        }

        private void InternalProcessUrl(string raw, string route, out string parsedUrl, out Dictionary<string, string> parameters)
        {
            var index = raw.IndexOf('?');
            var url = "";

            parameters = new Dictionary<string, string>();

            if (index > -1)
            {
                for (int i = 0; i < raw.Length; i++)
                {
                    if (i == index)
                        break;

                    url += raw[i];
                }

                var split = raw.Split('?').Skip(1).ToArray();

                foreach (var arg in split)
                {
                    if (arg.TrySplit('=', true, 2, out var parts))
                        parameters[parts[0]] = parts[1];
                    else
                        parameters[arg] = string.Empty;
                }
            }
            else
            {
                url = raw;
            }

            if (route.Contains("/{") && route.Contains("}"))
            {
                route.TrySplit('/', true, null, out var routeParts);
                url.TrySplit('/', true, null, out var urlParts);

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

                                parameters[name] = value;
                            }
                        }

                        parsedUrl = route;
                    }
                    else
                    {
                        parsedUrl = url;
                    }
                }
                else
                {
                    parsedUrl = url;
                }
            }
            else
            {
                parsedUrl = url;
            }
        }
    }
}