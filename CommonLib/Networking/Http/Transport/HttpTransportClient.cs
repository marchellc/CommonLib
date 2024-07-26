using CommonLib.Extensions;
using CommonLib.Logging;

using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Http.Transport.Messages.Connection;
using CommonLib.Networking.Http.Transport.Messages.Data;
using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Pooling.Pools;
using CommonLib.Serialization;

using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CommonLib.Networking.Http.Transport
{
    public class HttpTransportClient
    {
        private volatile HttpClient _client;
        private volatile LogOutput _log = new LogOutput("Http Transport Client").Setup();

        private volatile ConcurrentQueue<IHttpMessage> _data = new ConcurrentQueue<IHttpMessage>();

        private volatile Timer _timer;

        private TimeSpan _delay;
        private TimeSpan _latency;

        private DateTime _lastUpdate;

        private volatile string _token;
        private volatile string _url;

        private volatile bool _waiting;
        private volatile bool _connecting;

        public string Token => _token;

        public string BaseUrl => _url;

        public string DataUrl => $"{_url}/data";
        public string ConnectUrl => $"{_url}/connect";
        public string DisconnectUrl => $"{_url}/disconnect";

        public bool IsConnected => !string.IsNullOrWhiteSpace(_token) && !string.IsNullOrWhiteSpace(_url);
        public bool IsConnecting => _connecting;

        public bool IsWaiting => _waiting;

        public TimeSpan ServerDelay => _delay;
        public TimeSpan Latency => _latency;

        public TimeSpan DisconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

        public LogOutput Log => _log;

        public event Action OnDisconnecting;
        public event Action OnDisconnected;

        public event Action OnConnected;

        public event Action<string> OnConnecting;
        public event Action<string> OnConnectionFailed;

        public event Action<IHttpMessage> OnMessage;
        public event Action<RejectReason> OnRejected;

        public Func<HttpClient> ClientFactory { get; set; } = () => new HttpClient();

        public void Connect(string url)
        {
            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            if (_connecting)
                return;

            try
            {
                Stop();

                _url = url;
                _client = ClientFactory();
                _data = new ConcurrentQueue<IHttpMessage>();
                _connecting = true;
                _client.Timeout = DisconnectDelay;

                OnConnecting?.Invoke(_url);

                InternalSend<ConnectionMessage>(null, ConnectUrl, false, true, msg =>
                {
                    _connecting = false;
                    _log.Debug($"Callback invoked");

                    if (msg.IsRejected)
                    {
                        _log.Error($"Server rejected connection: {msg.Reason}");

                        _token = null;
                        _url = null;

                        OnRejected?.Invoke(msg.Reason);
                        OnConnectionFailed?.Invoke(msg.Reason.ToString());
                    }
                    else
                    {
                        _log.Info($"Server accepted connection: {msg.Token} (delay: {msg.Delay})");
                        _token = msg.Token;

                        SetDelay(TimeSpan.FromMilliseconds(msg.Delay.TotalMilliseconds / 2));

                        OnConnected?.Invoke();
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);

                OnConnectionFailed?.Invoke(ex.ToString());
            }
        }

        public void Disconnect()
        {
            try
            {
                if (!IsConnected)
                    return;

                OnDisconnecting?.Invoke();

                InternalSend<ConnectionMessage>(null, DisconnectUrl, true, false, null);

                _url = null;
                _token = null;

                _waiting = false;
                _connecting = false;

                OnDisconnected?.Invoke();
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
                Disconnect();

                _timer?.Dispose();
                _timer = null;

                _client?.Dispose();
                _client = null;

                _data?.Clear();
                _data = null;

                _connecting = false;
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void SetDelay(TimeSpan delay)
        {
            try
            {
                _delay = delay;

                if (_timer is null)
                {
                    _timer ??= new Timer(_ => InternalUpdate());
                    _timer.Change(50, 50);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Send(IHttpMessage message)
        {
            if (message is null)
                throw new ArgumentNullException(nameof(message));

            Log.Debug($"Queued message: {message.GetType().FullName}");

            _data.Enqueue(message);
        }

        private void InternalUpdate()
        {
            try
            {
                if ((DateTime.Now - _lastUpdate) < _delay)
                    return;

                if (_waiting)
                    return;

                var messages = ListPool<IHttpMessage>.Shared.Rent();
                var message = new DataMessage() { Messages = messages, Sent = DateTime.Now };

                while (_data.TryDequeue(out var msg))
                    messages.Add(msg);

                _lastUpdate = DateTime.Now;

                InternalSend<DataMessage>(message, DataUrl, true, true, msg =>
                {
                    ListPool<IHttpMessage>.Shared.Return(messages);

                    Log.Debug($"Received {msg.Messages.Count} messages");

                    foreach (var message in msg.Messages)
                    {
                        Log.Debug($"Invoking message {message.GetType().FullName}");

                        try
                        {
                            OnMessage?.Invoke(message);
                        }
                        catch (Exception ex)
                        {
                            Log.Error(ex);
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        private void InternalSend<T>(IHttpMessage message, string url, bool token, bool response, Action<T> callback) where T : IHttpMessage
            => Task.Run(async () => await InternalSendAsync(message, url, token, response, callback));

        private async Task InternalSendAsync<T>(IHttpMessage message, string url, bool token, bool recvResponse, Action<T> callback) where T : IHttpMessage
        {
            if (token)
                url += $"?token={_token}";

            while (_waiting)
                await Task.Delay(100);

            _waiting = true;
            _log.Debug($"Requesting {url}");

            try
            {
                _log.Debug("Sending request");

                using (var request = new HttpRequestMessage(HttpMethod.Post, url))
                {
                    if (message != null)
                        await request.WriteBytesAsync(Serializer.Serialize(s => s.PutSerializable(message)));

                    using (var response = await _client.SendAsync(request))
                    {
                        _waiting = false;
                        _connecting = false;

                        _log.Debug($"Response: {response.StatusCode}");

                        if (!response.IsSuccessStatusCode)
                        {
                            _log.Error($"Server returned error ({url}): {response.StatusCode} {response.ReasonPhrase}");
                        }
                        else
                        {
                            if (recvResponse && response.Content != null)
                            {
                                var bytes = await response.ReadBytesAsync();

                                if (bytes.Length > 4)
                                {
                                    Deserializer.Deserialize(bytes, deserializer =>
                                    {
                                        callback?.Invoke(deserializer.GetDeserializable<T>());
                                    });
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _log.Error($"Send exception: {ex}");

                _waiting = false;
                _connecting = false;

                Disconnect();
            }
        }
    }
}