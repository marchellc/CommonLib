using System;

using CommonLib.EventSync;

namespace CommonLib.Http.Server.Events;

public class ErrorReceivedEvent : EventSyncBase
{
    private volatile HttpServer server;
    private volatile Exception error;
    
    public HttpServer Server => server;
    public Exception Error => error;

    public ErrorReceivedEvent(HttpServer server, Exception error)
    {
        this.server = server;
        this.error = error;
    }
}