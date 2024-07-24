using System;
using System.Net;
using System.Threading.Tasks;

namespace CommonLib.Networking.Http.Routes
{
    public class DynamicRoute : HttpRoute
    {
        public DynamicRoute(string url, string[] methods, Func<HttpRequest, HttpListenerResponse, Task> asyncHandler)
        {
            Url = url;
            Methods = methods;
            AsyncHandler = asyncHandler;
        }

        public override string Url { get; }
        public override string[] Methods { get; }

        public Func<HttpRequest, HttpListenerResponse, Task> AsyncHandler { get; }

        public override async Task InvokeGetAsync(HttpRequest request, HttpListenerResponse response)
            => await AsyncHandler?.Invoke(request, response);

        public override async Task InvokePostAsync(HttpRequest request, HttpListenerResponse response)
            => await AsyncHandler?.Invoke(request, response);

        public override async Task InvokeOtherAsync(string method, HttpRequest request, HttpListenerResponse response)
            => await AsyncHandler?.Invoke(request, response);
    }
}
