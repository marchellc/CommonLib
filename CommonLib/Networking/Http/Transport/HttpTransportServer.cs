using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;

using CommonLib.Caching;
using CommonLib.Extensions;
using CommonLib.Logging;

using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Interfaces;
using CommonLib.Networking.Http.Transport.Routes;

using CommonLib.Pooling.Pools;

using CommonLib.Utilities.Generation;

using CommonLib.Networking.Http.Transport.Messages.Connection;
using CommonLib.Networking.Http.Transport.Messages.Data;

namespace CommonLib.Networking.Http.Transport
{
    public class HttpTransportServer
    {
        private volatile HttpServer _server;
        private volatile Timer _timer;

        private volatile Dictionary<string, HttpTransportPeer> _peers = new Dictionary<string, HttpTransportPeer>();
        private volatile UniqueStringGenerator _tokens = new UniqueStringGenerator(new MemoryCache<string>(), 15, false);

        internal volatile LogOutput _log;

        public event Action OnReady;
        public event Action OnStopped;

        public event Action<Exception> OnError;

        public event Action<HttpTransportPeer> OnConnected;
        public event Action<HttpTransportPeer, INetworkMessage> OnMessage;
        public event Action<HttpTransportPeer, DisconnectReason> OnDisconnected;

        public TimeSpan DisconnectDelay { get; set; } = TimeSpan.FromSeconds(1);

        public Func<string, HttpTransportServer, IPEndPoint, HttpTransportPeer> PeerConstructor { get; set; } = (token, server, ip) => new HttpTransportPeer(token, server, ip);

        public bool IsListening => _server != null && _server.IsListening;

        public int PeerCount => _peers.Count;

        public IEnumerable<HttpTransportPeer> Peers => _peers.Values;

        public HttpServer Server => _server;
        public LogOutput Log => _log;

        public void Start(string ip, int port)
        {
            if (string.IsNullOrWhiteSpace(ip))
                throw new ArgumentNullException(nameof(ip));

            if (port < 0 || port > ushort.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(port));

            Serialization.Serialization.RegisterType(typeof(ConnectionMessage));
            Serialization.Serialization.RegisterType(typeof(DataMessage));

            try
            {
                Stop();

                _log = new LogOutput($"Http Transport Server ({ip}:{port})");
                _log.Setup();

                _server = new HttpServer();
                _server.OnError += HandleError;

                _server.Start(new string[] { $"http://{ip}:{port}/" });

                _server.AddRoute(new DisconnectRoute(this));
                _server.AddRoute(new ConnectRoute(this));
                _server.AddRoute(new DataRoute(this));

                _timer = new Timer(_ => InternalUpdate());
                _timer.Change(50, 50);

                OnReady?.Invoke();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Stop()
        {
            try
            {
                if (_server != null)
                {
                    if (_timer != null)
                    {
                        _timer.Dispose();
                        _timer = null;
                    }

                    _server.OnError -= HandleError;

                    _server.Dispose();
                    _server = null;

                    foreach (var peer in _peers)
                        peer.Value?.OnDisconnected(DisconnectReason.Forced);

                    OnStopped?.Invoke();
                }

                _peers.Clear();
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public bool TryGetPeer(string token, out HttpTransportPeer peer)
            => _peers.TryGetValue(token, out peer);

        public bool TryGetPeer(IPEndPoint endPoint, out HttpTransportPeer peer)
            => (_peers.TryGetFirst(p => p.Value.RemoteIp == endPoint, out var pair) ? peer = pair.Value : peer = null) != null;

        internal void InternalDisconnect(string token, DisconnectReason reason)
        {
            try
            {
                if (_peers.TryGetValue(token, out var peer))
                {
                    OnDisconnected?.Invoke(peer, reason);
                    peer.OnDisconnected(reason);
                }

                _peers.Remove(token);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        internal string InternalAcceptPeer(IPEndPoint remote)
        {
            try
            {
                var token = _tokens.Next();
                var peer = PeerConstructor(token, this, remote);

                OnConnected?.Invoke(peer);

                _peers[token] = peer;
                return token;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
                return null;
            }
        }

        internal void InternalMessage(HttpTransportPeer peer, INetworkMessage message)
        {
            try
            {
                OnMessage?.Invoke(peer, message);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void HandleError(Exception obj)
            => OnError?.Invoke(obj);

        private void InternalUpdate()
        {
            try
            {
                var removed = ListPool<string>.Shared.Rent();

                foreach (var pair in _peers)
                {
                    if (pair.Value.InternalTick())
                        removed.Add(pair.Key);
                }

                foreach (var token in removed)
                    InternalDisconnect(token, DisconnectReason.TimedOut);

                ListPool<string>.Shared.Return(removed);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }
    }
}