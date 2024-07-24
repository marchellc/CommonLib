using System.Net;
using System.Threading.Tasks;

namespace CommonLib.Networking.Http
{
    public class HttpRoute
    {
        public virtual string Url { get; }

        public virtual string[] Methods { get; }
        public virtual string[] RequiredParameters { get; }

        public virtual async Task InvokeGetAsync(HttpRequest request, HttpListenerResponse response)
        {

        }

        public virtual async Task InvokePostAsync(HttpRequest request, HttpListenerResponse response)
        {

        }

        public virtual async Task InvokeOtherAsync(string method, HttpRequest request, HttpListenerResponse response)
        {

        }
    }
}