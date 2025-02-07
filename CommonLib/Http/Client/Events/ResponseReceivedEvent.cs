using CommonLib.EventSync;

namespace CommonLib.Http.Client.Events;

public class ResponseReceivedEvent : EventSyncBase
{
    private volatile HttpContext context;
    private volatile HttpRequest request;
    
    public HttpContext Context => context;
    public HttpRequest Request => request;

    public ResponseReceivedEvent(HttpContext context, HttpRequest request)
    {
        this.context = context;
        this.request = request;
    }
}