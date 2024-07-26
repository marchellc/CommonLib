using CommonLib.Networking.Http.Transport.Messages.Data;
using CommonLib.Serialization;

using System.Linq;
using System.Net;
using System;
using System.Threading.Tasks;
using System.IO;
using System.Text;

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

        public override bool ReadContent => true;

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
                    using (var stream = new MemoryStream(request.Content))
                    using (var reader = new BinaryReader(stream, (request.ContentEncoding ?? WriterUtils.Encoding), false))
                    {
                        var message = reader.ReadDeserializable(new DataMessage());
                        var queue = peer.InternalRegister(message.Sent, message.Messages);

                        var responseMessage = new DataMessage() { Sent = DateTime.Now, Messages = queue.ToList() };

                        response.SetCode(HttpStatusCode.OK, "OK");
                        await response.WriteBytesAsync(WriterUtils.Write(writer => writer.WriteSerializable(responseMessage)));
                    }
                }
            }
            catch (Exception ex)
            {
                _server.Log.Error(ex);
            }
        }
    }
}
