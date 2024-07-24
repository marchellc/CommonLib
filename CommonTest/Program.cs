using CommonLib;
using CommonLib.Logging;
using CommonLib.Networking.Http;

using System.Net;
using System.Threading.Tasks;

namespace CommonTest
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            CommonLibrary.Initialize(args);

            var log = new LogOutput("Test").Setup();
            var server = new HttpServer();

            server.AddRoute(new TestRoute());
            server.Start(new string[] { "http://127.0.0.1:8080/" });

            await Task.Delay(-1);
        }
    }

    public class TestRoute : HttpRoute
    {
        public override string Url => "/api";
        public override string[] Methods { get; } = new string[] { "GET" };

        public override async Task InvokeGetAsync(HttpRequest request, HttpListenerResponse response)
        {
            response.SetCode(HttpStatusCode.OK, "OK");

            await response.WriteStringAsync("hi");
        }
    }
}