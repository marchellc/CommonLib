using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Pooling.Pools;

using System;
using System.Net;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace CommonLib.Networking.Http.Transport
{
    public class HttpTransportPeer
    {
        private DateTime _tickTime;
        private DateTime _connTime;

        private TimeSpan _latency;

        private volatile bool _updated = false;
        private volatile ConcurrentQueue<IHttpMessage> _data;

        public IPEndPoint RemoteIp { get; }

        public HttpTransportServer Server { get; }

        public string Token { get; }

        public TimeSpan Latency => _latency;

        public HttpTransportPeer(string token, HttpTransportServer server, IPEndPoint ip)
        {
            RemoteIp = ip;
            Server = server;
            Token = token;

            _tickTime = DateTime.Now;
            _connTime = DateTime.Now;

            _data = new ConcurrentQueue<IHttpMessage>();
        }

        public virtual void OnConnected() { }
        public virtual void OnDisconnected(DisconnectReason reason) { }

        public void Send(IHttpMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            Server.Log.Debug($"Queued message: {message.GetType().FullName}");

            _data.Enqueue(message);
        }

        public void Disconnect(DisconnectReason reason = DisconnectReason.Forced)
            => Server.InternalDisconnect(Token, reason);

        internal IHttpMessage[] InternalRegister(DateTime sentAt, IEnumerable<IHttpMessage> messages)
        {
            try
            {
                _tickTime = DateTime.Now;
                _latency = DateTime.Now - sentAt;
                _updated = true;

                Server.Log.Debug($"Received update, {_latency.TotalMilliseconds} ms ({messages.Count()} messages)");

                foreach (var message in messages)
                {
                    Server.Log.Debug($"Processing message: {message.GetType().FullName}");
                    Server.InternalMessage(this, message);
                }

                if (_data.IsEmpty)
                {
                    Server.Log.Debug("Sending no messages");
                    return Array.Empty<IHttpMessage>();
                }

                var list = ListPool<IHttpMessage>.Shared.Rent();

                while (_data.TryDequeue(out var data))
                {
                    list.Add(data);
                    Server.Log.Debug($"Dequeued message: {data.GetType().FullName}");
                }

                Server.Log.Debug($"Sending {list.Count} messages");
                return ListPool<IHttpMessage>.Shared.ToArrayReturn(list);
            }
            catch (Exception ex)
            {
                Server.Log.Error($"Tick register error: {ex}");
                return Array.Empty<IHttpMessage>();
            }
        }

        internal bool InternalTick()
        {
            if (!_updated)
                return (DateTime.Now - _connTime).TotalSeconds >= 15;
            else
                return (DateTime.Now - _tickTime) >= Server.DisconnectDelay;
        }
    }
}