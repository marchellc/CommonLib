using CommonLib.EventSync;

namespace CommonLib.Http.Server.Events;

public class ContextReceivedEvent : EventSyncBase
{
    private volatile HttpServer server;
    private volatile HttpContext context;
    
    public HttpServer Server => server;
    public HttpContext Context => context;

    public ContextReceivedEvent(HttpServer server, HttpContext context)
    {
        this.server = server;
        this.context = context;
    }
}