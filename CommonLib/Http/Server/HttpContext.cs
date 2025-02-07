using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Net;
using System.IO;
using System;

using Newtonsoft.Json;

namespace CommonLib.Http.Server;

public class HttpContext
{
    private volatile byte[] rawData;
    private volatile string stringData;

    private volatile IPEndPoint origin;
    private volatile HttpListenerContext context;
    private volatile ConcurrentDictionary<string, string> parameters;

    private volatile HttpRoute targetRoute;
    
    public byte[] Data => rawData;
    
    public string String => stringData;

    public IPEndPoint Origin => origin;
    
    public HttpListenerContext Context => context;
    public HttpListenerRequest Request => context.Request;
    public HttpListenerResponse Response => context.Response;

    public IReadOnlyDictionary<string, string> Parameters => parameters;
    
    public HttpRoute TargetRoute
    {
        get => targetRoute;
        internal set => targetRoute = value;
    }
    
    public string ResponseContentType
    {
        get => context.Response.ContentType;
        set => context.Response.ContentType = value;
    }
    
    public HttpStatusCode ResponseCode
    {
        get => (HttpStatusCode)context.Response.StatusCode;
        set => context.Response.StatusCode = (int)value;
    }
    
    public bool TryGetHeader(string headerKey, out string headerValue)
        => (headerValue = context.Request.Headers.Get(headerKey)) != null;
    
    public bool TryGetParameter(string parameterKey, out string parameterValue)
        => parameters.TryGetValue(parameterKey, out parameterValue);
    
    public bool HasHeader(string headerKey)
        => context.Request.Headers.Get(headerKey) != null;

    public bool HasHeader(string headerKey, string headerValue)
        => context.Request.Headers.Get(headerKey) == headerValue;
    
    public bool HasParameter(string parameterKey)
        => parameters.ContainsKey(parameterKey);
    
    public bool HasParameter(string parameterKey, string parameterValue)
        => parameters.ContainsKey(parameterKey) && parameters[parameterKey] == parameterValue;

    public bool IsJson<T>()
        => IsJson<T>(out _);
    
    public bool IsJson(Type jsonType)
        => jsonType != null && IsJson(jsonType, out _);

    public bool IsJson(Type jsonType, out object jsonValue)
    {
        if (jsonType is null)
            throw new ArgumentNullException(nameof(jsonType));
        
        jsonValue = default;

        if (string.IsNullOrWhiteSpace(stringData))
            return false;

        try
        {
            jsonValue = JsonConvert.DeserializeObject(stringData, jsonType);
            return jsonValue != null;
        }
        catch
        {
            return false;
        }
    }
    
    public bool IsJson<T>(out T jsonValue)
    {
        jsonValue = default;

        if (string.IsNullOrWhiteSpace(stringData))
            return false;

        try
        {
            jsonValue = JsonConvert.DeserializeObject<T>(stringData);
            return jsonValue != null;
        }
        catch
        {
            return false;
        }
    }

    public T GetJson<T>()
    {
        if (!IsJson<T>(out var json))
            throw new Exception("Could not parse JSON");

        return json;
    }

    public object GetJson(Type type)
    {
        if (!IsJson(type, out var json))
            throw new Exception("Could not parse JSON");
        
        return json;
    }

    public bool ReadRaw(Action<BinaryReader> reader)
    {
        if (reader is null)
            throw new ArgumentNullException(nameof(reader));
        
        if (rawData is null || rawData.Length == 0)
            return false;
        
        using (var ms = new MemoryStream(rawData))
        using (var br = new BinaryReader(ms))
        {
            reader(br);
        }

        return true;
    }

    public void RespondJson(object json, bool indented = false)
    {
        var serialized = JsonConvert.SerializeObject(json, indented ? Formatting.Indented : Formatting.None);
        
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.StatusDescription = "OK";
        
        context.Response.ContentType = "application/json";
        
        using (var sw = new StreamWriter(context.Response.OutputStream))
            sw.Write(serialized);
        
        context.Response.Close();
    }

    public void RespondWrite(Action<BinaryWriter> writer, string contentType = "application/octet-stream")
    {
        if (writer is null)
            throw new ArgumentNullException(nameof(writer));
        
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentNullException(nameof(contentType));
        
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            writer(bw);
            
            ms.CopyTo(context.Response.OutputStream);

            context.Response.ContentLength64 = ms.Length;
        }
        
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.StatusDescription = "OK";

        context.Response.ContentType = contentType;
        context.Response.Close();
    }

    public void RespondBytes(byte[] content, string contentType = "application/octet-stream")
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));
        
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentNullException(nameof(contentType));
        
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.StatusDescription = "OK";
        
        context.Response.ContentType = contentType;
        context.Response.ContentLength64 = content.Length;
        
        for (int i = 0; i < content.Length; i++)
            context.Response.OutputStream.WriteByte(content[i]);

        context.Response.Close();
    }

    public void RespondText(string content, string contentType = "text/plain")
    {
        if (content is null)
            throw new ArgumentNullException(nameof(content));
        
        if (string.IsNullOrWhiteSpace(contentType))
            throw new ArgumentNullException(nameof(contentType));
            
        context.Response.StatusCode = (int)HttpStatusCode.OK;
        context.Response.StatusDescription = "OK";

        context.Response.ContentType = contentType;
        
        using (var sw = new StreamWriter(context.Response.OutputStream))
            sw.Write(content);
        
        context.Response.Close();
    }

    public void RespondError(string message, HttpStatusCode errorCode = HttpStatusCode.InternalServerError)
    {
        if (string.IsNullOrWhiteSpace(message))
            throw new ArgumentNullException(nameof(message));

        if (errorCode is HttpStatusCode.OK)
            throw new Exception($"Cannot send error response with OK status code");
        
        context.Response.StatusCode = (int)errorCode;
        context.Response.StatusDescription = message;
        
        context.Response.ContentType = "text/plain";
        
        using (var writer = new StreamWriter(context.Response.OutputStream))
            writer.Write(message);
        
        context.Response.Close();
    }

    public static async Task<HttpContext> GetAsync(HttpListenerContext ctx, HttpServer server, HttpRoute route)
    {
        var context = new HttpContext();

        context.context = ctx;
        context.targetRoute = route;

        if (!string.IsNullOrWhiteSpace(server.RealIpHeader) && context.TryGetHeader(server.RealIpHeader, out var realIpValue) 
                                                            && IPAddress.TryParse(realIpValue, out var realIp))
            context.origin = new IPEndPoint(realIp, ctx.Request.RemoteEndPoint?.Port ?? 0);
        else
            context.origin = ctx.Request.RemoteEndPoint;

        var bytes = new List<byte>((int)ctx.Request.ContentLength64);
        
        using (var ms = new MemoryStream())
        {
            await context.Request.InputStream.CopyToAsync(ms);

            var count = -1;
            
            while ((count = ms.ReadByte()) != -1)
                bytes.Add((byte)count);
            
            context.rawData = bytes.ToArray();
            
            ms.Seek(0, SeekOrigin.Begin);
            
            using (var sr = new StreamReader(ms))
                context.stringData = await sr.ReadToEndAsync();
        }
        
        return context;
    }
}