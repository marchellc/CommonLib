using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Http.Transport.Messages.Connection;
using CommonLib.Serialization;

using System;
using System.Net;
using System.Threading.Tasks;

namespace CommonLib.Networking.Http.Transport.Routes
{
    public class ConnectRoute : HttpRoute
    {
        private HttpTransportServer _server;

        public ConnectRoute(HttpTransportServer server)
            => _server = server;

        public override string Url => "/connect";
        public override string[] Methods { get; } = new string[] { "POST" };

        public override async Task InvokePostAsync(HttpRequest request, HttpListenerResponse response)
        {
            try
            {
                if (request.RemoteIp.Port != 0 && _server.TryGetPeer(request.RemoteIp, out _))
                {
                    response.SetCode(HttpStatusCode.Forbidden, $"An active peer has been found.");
                    await response.WriteBytesAsync(WriterUtils.Write(writer => writer.WriteSerializable(new ConnectionMessage(RejectReason.ActiveSession))));

                    _server._log.Info($"Rejected peer from {request.RemoteIp} (active session)");
                }
                else
                {
                    var token = _server.InternalAcceptPeer(request.RemoteIp);

                    response.SetCode(HttpStatusCode.OK, "Connection accepted");
                    await response.WriteBytesAsync(WriterUtils.Write(writer => writer.WriteSerializable(new ConnectionMessage(token, _server.DisconnectDelay))));

                    _server._log.Info($"Accepted peer from {request.RemoteIp} (token: {token})");
                }
            }
            catch (Exception ex)
            {
                _server.Log.Error(ex);
            }
        }
    }
}