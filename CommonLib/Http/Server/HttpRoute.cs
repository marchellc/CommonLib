using System.Collections.Concurrent;
using System.Net.Http;
using CommonLib.Http.Server.Events;

namespace CommonLib.Http.Server;

public abstract class HttpRoute
{
    internal string FixedUrl;
    
    public abstract string Url { get; }
    public abstract HttpMethod[] Methods { get; }

    public virtual bool IsMatch(string rawUrl, string parsedUrl) => false;
    public virtual void ParseParameters(string rawUrl, string parsedUrl, ConcurrentDictionary<string, string> parameters) { }

    public abstract void OnRequest(ContextReceivedEvent contextReceivedEvent);
}