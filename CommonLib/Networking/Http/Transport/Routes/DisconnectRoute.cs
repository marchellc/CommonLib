using System;
using System.Net;
using System.Threading.Tasks;

using CommonLib.Networking.Http.Transport.Enums;

namespace CommonLib.Networking.Http.Transport.Routes
{
    public class DisconnectRoute : HttpRoute
    {
        private HttpTransportServer _server;

        public DisconnectRoute(HttpTransportServer server)
            => _server = server;

        public override string Url => "/disconnect";

        public override string[] Methods { get; } = new string[] { "POST" };
        public override string[] RequiredParameters { get; } = new string[] { "token" };

        public override async Task InvokePostAsync(HttpRequest request, HttpListenerResponse response)
        {
            try
            {
                if (!_server.TryGetPeer(request.Parameters["token"], out var peer))
                {
                    response.SetCode(HttpStatusCode.Forbidden, "Unknown token");
                    await response.WriteStringAsync("Unknown token");
                }
                else
                {
                    response.SetCode(HttpStatusCode.OK, "Disconnect received");
                    await response.WriteStringAsync("Disconnect received");

                    _server.InternalDisconnect(peer.Token, DisconnectReason.Requested);
                }
            }
            catch (Exception ex)
            {
                _server.Log.Error(ex);
            }
        }
    }
}