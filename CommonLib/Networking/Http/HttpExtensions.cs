using CommonLib.Utilities;

using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;

using System.Net.Http.Headers;
using CommonLib.Serialization;

namespace CommonLib.Networking.Http
{
    public static class HttpExtensions
    {
        public static readonly MediaTypeHeaderValue OctetStreamHeader = new MediaTypeHeaderValue("application/octet-stream");

        public static void SetCode(this HttpListenerResponse response, HttpStatusCode code, string description = null)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            if (!string.IsNullOrWhiteSpace(description))
                response.StatusDescription = description;

            response.StatusCode = (int)code;
        }

        public static void Read(this HttpListenerRequest request, Action<BinaryReader> action)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            if (request.InputStream is null)
                return;

            using (var reader = new BinaryReader(request.InputStream, (request.ContentEncoding ?? WriterUtils.Encoding), true))
                action(reader);
        }

        public static void Write(this HttpListenerResponse response, Action<BinaryWriter> action)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            if (response.OutputStream is null)
                return;

            response.ContentType = "application/octet-stream";

            using (var writer = new BinaryWriter(response.OutputStream, (response.ContentEncoding ?? WriterUtils.Encoding), true))
                action(writer);
        }

        public static async Task<byte[]> ReadBytesAsync(this HttpListenerRequest request)
        {
            if (request is null)
                throw new ArgumentNullException(nameof(request));

            using (var reader = new StreamReader(request.InputStream))
                return JsonUtils.JsonDeserialize<byte[]>(await reader.ReadToEndAsync());
        }

        public static async Task<byte[]> ReadBytesAsync(this HttpResponseMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            return JsonUtils.JsonDeserialize<byte[]>(await message.Content.ReadAsStringAsync());
        }

        public static void Write(this HttpRequestMessage message, Action<BinaryWriter> action)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF32, true))
            {
                action(writer);

                var content = new StreamContent(stream);

                content.Headers.ContentType = OctetStreamHeader;
                message.Content = content;
            }
        }

        public static async Task ReadAsync(this HttpResponseMessage message, Action<BinaryReader> action)
        {
            using (var stream = await message.Content.ReadAsStreamAsync())
            using (var reader = new BinaryReader(stream, Encoding.UTF32, false))
                action(reader);
        }

        public static async Task WriteBytesAsync(this HttpRequestMessage message, IEnumerable<byte> bytes)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            var str = JsonUtils.JsonSerialize(bytes.ToArray());

            message.Content = new StringContent(str);
        }

        public static async Task WriteBytesAsync(this HttpListenerResponse response, IEnumerable<byte> bytes)
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            if (bytes is null)
                throw new ArgumentNullException(nameof(bytes));

            response.ContentType = "text/plain";
            response.ContentEncoding = Encoding.UTF32;

            var str = JsonUtils.JsonSerialize(bytes.ToArray());

            using (var writer = new StreamWriter(response.OutputStream))
                await writer.WriteAsync(str);
        }

        public static async Task WriteStringAsync(this HttpListenerResponse response, string str, string format = "text/plain")
        {
            if (response is null)
                throw new ArgumentNullException(nameof(response));

            if (string.IsNullOrWhiteSpace(str))
                throw new ArgumentNullException(nameof(str));

            if (!string.IsNullOrWhiteSpace(format))
                response.ContentType = format;

            using (var writer = new StreamWriter(response.OutputStream))
                await writer.WriteAsync(str);
        }

        public static async Task WriteJsonAsync(this HttpListenerResponse response, string jsonStr)
            => await WriteStringAsync(response, jsonStr, "application/json");

        public static async Task WriteJsonAsync(this HttpListenerResponse response, object obj)
            => await WriteStringAsync(response, obj.JsonSerialize(), "application/json");
    }
}