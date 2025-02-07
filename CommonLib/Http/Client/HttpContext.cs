using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using System.IO;
using System;

using Newtonsoft.Json;

namespace CommonLib.Http.Client;

public class HttpContext
{
    private volatile byte[] rawData;
    private volatile string stringData;

    private volatile HttpResponseMessage response;
    private volatile HttpRequestMessage request;

    private volatile Exception error;

    public byte[] Data => rawData;

    public string String => stringData;
    public string ErrorString => response?.ReasonPhrase ?? string.Empty;
    public string ContentType => response.Content?.Headers?.ContentType?.MediaType ?? string.Empty;

    public bool IsSuccessStatusCode => error is null && (response?.IsSuccessStatusCode ?? false);
    public bool IsEmpty => response?.Content is null;
    public bool IsReceived => response != null;
    
    public Exception Error => error;
    
    public HttpRequestMessage Request => request;
    public HttpResponseMessage Response => response;

    public HttpStatusCode StatusCode => response?.StatusCode ?? HttpStatusCode.InternalServerError;
    
    public void EnsureSuccessStatusCode()
        => response?.EnsureSuccessStatusCode();

    public bool TryGetHeader(string headerKey, out string headerValue)
    {
        var values = response.Headers.GetValues(headerKey);

        if (values != null)
        {
            foreach (var value in values)
            {
                headerValue = value;
                return true;
            }
        }

        headerValue = null;
        return false;
    }

    public bool HasHeader(string headerKey)
        => TryGetHeader(headerKey, out _);

    public bool HasHeader(string headerKey, string headerValue)
        => TryGetHeader(headerKey, out var value) && value == headerValue;

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

    internal async Task OnResponseReceived(HttpResponseMessage response, Exception error)
    {
        this.response = response;
        this.error = error;

        if (response?.Content != null)
        {
            var bytes = new List<byte>((int)(response.Content.Headers.ContentLength.HasValue
                ? response.Content.Headers.ContentLength.Value
                : 0));

            using (var ms = new MemoryStream())
            {
                await response.Content.CopyToAsync(ms);

                var count = -1;

                while ((count = ms.ReadByte()) != -1)
                    bytes.Add((byte)count);

                rawData = bytes.ToArray();

                ms.Seek(0, SeekOrigin.Begin);

                using (var sr = new StreamReader(ms))
                    stringData = await sr.ReadToEndAsync();
            }
        }
    }

    public static HttpContext Get(HttpRequestMessage request)
    {
        if (request is null)
            throw new ArgumentNullException(nameof(request));

        return new HttpContext() { request = request };
    }
}