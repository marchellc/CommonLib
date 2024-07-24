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
        private readonly HttpClient _client = new HttpClient();
        private readonly LogOutput _log = new LogOutput("Http Transport Client").Setup();

        private volatile ConcurrentQueue<IHttpMessage> _data = new ConcurrentQueue<IHttpMessage>();

        private volatile Timer _timer;

        private TimeSpan _delay;
        private TimeSpan _latency;

        private DateTime _lastSuccess;
        private DateTime _lastUpdate;

        private volatile string _token;
        private volatile string _url;

        private volatile bool _waiting;

        public string Token => _token;

        public string BaseUrl => _url;

        public string DataUrl => $"{_url}/data";
        public string ConnectUrl => $"{_url}/connect";
        public string DisconnectUrl => $"{_url}/disconnect";

        public bool IsConnected => !string.IsNullOrWhiteSpace(_token) && !string.IsNullOrWhiteSpace(_url);
        public bool IsWaiting => _waiting;

        public TimeSpan ServerDelay => _delay;
        public TimeSpan Latency => _latency;

        public TimeSpan DisconnectDelay { get; set; } = TimeSpan.FromSeconds(5);

        public LogOutput Log => _log;

        public event Action OnDisconnected;
        public event Action OnConnected;

        public event Action<IHttpMessage> OnMessage;
        public event Action<RejectReason> OnRejected;

        public void Connect(string url)
        {
            Disconnect();

            if (string.IsNullOrWhiteSpace(url))
                throw new ArgumentNullException(nameof(url));

            try
            {
                _url = url;

                InternalSend(null, ConnectUrl, false, msg =>
                {
                    if (msg is null || msg is not ConnectionMessage connectionMessage)
                        return;

                    if (connectionMessage.IsRejected)
                    {
                        _log.Error($"Server rejected connection: {connectionMessage.Reason}");
                        OnRejected?.Invoke(connectionMessage.Reason);
                    }
                    else
                    {
                        _log.Info($"Server accepted connection: {connectionMessage.Token} (delay: {connectionMessage.Delay})");
                        _token = connectionMessage.Token;

                        SetDelay(TimeSpan.FromMilliseconds(connectionMessage.Delay.TotalMilliseconds / 2));

                        OnConnected?.Invoke();
                    }
                });
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        public void Disconnect(bool sendDisconnect = true)
        {
            try
            {
                if (sendDisconnect && !string.IsNullOrWhiteSpace(_token) && !string.IsNullOrWhiteSpace(_url))
                    InternalSend(null, DisconnectUrl, true, null);

                OnDisconnected?.Invoke();

                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }

                _waiting = false;
                _token = null;
                _url = null;

                _data.Clear();
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

                InternalSend(message, DataUrl, true, msg =>
                {
                    ListPool<IHttpMessage>.Shared.Return(messages);

                    if (msg is null || msg is not DataMessage dataMessage)
                        return;

                    Log.Debug($"Received {dataMessage.Messages.Count} messages");

                    foreach (var message in dataMessage.Messages)
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

        private void InternalSend(IHttpMessage message, string url, bool token, Action<object> callback)
            => Task.Run(async () => await InternalSendAsync(message, url, token, callback));

        private async Task InternalSendAsync(IHttpMessage message, string url, bool token, Action<IHttpMessage> callback)
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
                        await request.WriteBytesAsync(Serializer.Serialize(s => s.PutObject(message)));

                    using (var response = await _client.SendAsync(request))
                    {
                        _waiting = false;
                        _log.Debug($"Response: {response.StatusCode}");

                        if (!response.IsSuccessStatusCode)
                        {
                            _log.Error($"Server returned error ({url}): {response.StatusCode} {response.ReasonPhrase}");

                            if ((DateTime.Now - _lastSuccess) >= DisconnectDelay)
                                Disconnect(false);
                        }
                        else
                        {
                            _lastSuccess = DateTime.Now;

                            if (response.Content != null)
                            {
                                var bytes = await response.ReadBytesAsync();

                                if (bytes.Length > 4)
                                {
                                    Deserializer.Deserialize(bytes, deserializer =>
                                    {
                                        var obj = deserializer.GetObject();

                                        if (obj != null && obj is IHttpMessage httpMessage)
                                            callback?.Invoke(httpMessage);
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
            }
        }
    }
}