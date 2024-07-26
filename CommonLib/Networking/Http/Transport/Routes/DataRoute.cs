using CommonLib.Networking.Http.Transport.Messages.Data;
using CommonLib.Serialization;

using System.Linq;
using System.Net;
using System;
using System.Threading.Tasks;

namespace CommonLib.Networking.Http.Transport.Routes
{
    public class DataRoute : HttpRoute
    {
        private HttpTransportServer _server;

        public DataRoute(HttpTransportServer server)
            => _server = server;

        public override string Url => "/data";

        public override string[] Methods { get; } = new string[] { "POST" };
        public override string[] RequiredParameters { get; } = new string[] { "token" };

        public override async Task InvokePostAsync(HttpRequest request, HttpListenerResponse response)
        {
            try
            {
                var token = request.Parameters["token"];

                if (!_server.TryGetPeer(token, out var peer))
                {
                    response.SetCode(HttpStatusCode.Forbidden, "Unknown token");
                    await response.WriteStringAsync("Unknown token");
                }
                else
                {
                    var deserializer = Deserializer.GetDeserializer(request.Content);
                    var message = deserializer.GetDeserializable<DataMessage>();
                    var queue = peer.InternalRegister(message.Sent, message.Messages);

                    var responseMessage = new DataMessage() { Sent = DateTime.Now, Messages = queue.ToList() };

                    response.SetCode(HttpStatusCode.OK, "OK");
                    await response.WriteBytesAsync(Serializer.Serialize(s => s.PutSerializable(responseMessage)));
                }
            }
            catch (Exception ex)
            {
                _server.Log.Error(ex);
            }
        }
    }
}
