using CommonLib.Networking.Interfaces;
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
        private volatile ConcurrentQueue<INetworkMessage> _data;

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

            _data = new ConcurrentQueue<INetworkMessage>();
        }

        public virtual void OnConnected() { }
        public virtual void OnDisconnected(DisconnectReason reason) { }

        public void Send(INetworkMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            _data.Enqueue(message);
        }

        public void Disconnect(DisconnectReason reason = DisconnectReason.Forced)
            => Server.InternalDisconnect(Token, reason);

        internal INetworkMessage[] InternalRegister(DateTime sentAt, IEnumerable<INetworkMessage> messages)
        {
            try
            {
                _tickTime = DateTime.Now;
                _latency = DateTime.Now - sentAt;
                _updated = true;

                Server.Log.Debug($"Received update, {_latency.TotalMilliseconds} ms ({messages.Count()} messages)");

                foreach (var message in messages)
                    Server.InternalMessage(this, message);

                if (_data.IsEmpty)
                    return Array.Empty<INetworkMessage>();

                var list = ListPool<INetworkMessage>.Shared.Rent();

                while (_data.TryDequeue(out var data))
                    list.Add(data);

                Server.Log.Debug($"Sending {list.Count} messages");
                return ListPool<INetworkMessage>.Shared.ToArrayReturn(list);
            }
            catch (Exception ex)
            {
                Server.Log.Error($"Tick register error: {ex}");
                return Array.Empty<INetworkMessage>();
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