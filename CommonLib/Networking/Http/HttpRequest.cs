using System.Collections.Generic;
using System.Collections.Specialized;
using System.Net;
using System.Text;

namespace CommonLib.Networking.Http
{
    public struct HttpRequest
    {
        public readonly string[] AcceptedTypes;
        public readonly string ContentType;

        public readonly long ContentLength;

        public readonly bool IsAuthentificated;
        public readonly bool IsLocal;
        public readonly bool IsSecure;

        public readonly bool HasBody;

        public readonly Encoding ContentEncoding;

        public readonly CookieCollection Cookies;

        public readonly NameValueCollection Headers;
        public readonly NameValueCollection QueryString;

        public readonly IPEndPoint LocalIp;
        public readonly IPEndPoint RemoteIp;

        public readonly Dictionary<string, string> Parameters;

        public readonly byte[] Content;

        public HttpRequest(HttpListenerRequest req, Dictionary<string, string> parameters, byte[] rawContent)
        {
            AcceptedTypes = req.AcceptTypes;

            ContentType = req.ContentType;
            ContentLength = req.ContentLength64;
            ContentEncoding = req.ContentEncoding;

            IsAuthentificated = req.IsAuthenticated;
            IsLocal = req.IsLocal;
            IsSecure = req.IsSecureConnection;

            HasBody = req.HasEntityBody;

            Cookies = req.Cookies;
            Headers = req.Headers;
            QueryString = req.QueryString;

            LocalIp = req.LocalEndPoint;

            var remoteIpStr = Headers.Get("X-Real-IP");

            if (!string.IsNullOrWhiteSpace(remoteIpStr))
                RemoteIp = new IPEndPoint(IPAddress.Parse(remoteIpStr), 0);
            else
                RemoteIp = req.RemoteEndPoint;

            Parameters = parameters;
            Content = rawContent;
        }
    }
}
