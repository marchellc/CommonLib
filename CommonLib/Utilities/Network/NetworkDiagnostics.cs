using System;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;

using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;

namespace CommonLib.Utilities
{
    public static class NetworkDiagnostics
    {
        private static IPAddress _local4;

        public static IPAddress LocalV4
        {
            get
            {
                if (_local4 is null)
                {
                    var entry = Dns.GetHostEntry(Dns.GetHostName());

                    foreach (var address in entry.AddressList)
                    {
                        if (address.AddressFamily != AddressFamily.InterNetwork)
                            continue;

                        var str = address.ToString();

                        if (str.StartsWith("192") || str.StartsWith("127") || str.StartsWith("172") || str.StartsWith("0"))
                            continue;

                        _local4 = address;
                        break;
                    }
                }

                return _local4;
            }
        }

        public static void TraceRoute(string host, int timeout, int timeToLive, int bufferSize, Action<PingHit[]> callback)
            => Task.Run(() => TraceRoute(host, timeout, timeToLive, bufferSize)).ContinueWith(t => callback(t.Result));

        public static async Task<PingHit[]> TraceRouteAsync(string host, int timeout = 10000, int timeToLive = 30, int bufferSize = 32)
            => await Task.Run(() => TraceRoute(host, timeout, timeToLive, bufferSize));

        public static PingHit[] TraceRoute(string host, int timeout = 10000, int timeToLive = 30, int bufferSize = 32)
        {
            var routes = new List<PingHit>();
            var buffer = new byte[bufferSize];
            var options = new PingOptions(timeToLive, true);

            CommonLibrary.Random.NextBytes(buffer);
            
            using (var sender = new Ping())
            {
                for (int ttl = 1; ttl < timeToLive; ttl++)
                {
                    try
                    {
                        var reply = sender.Send(host, timeout, buffer, options);

                        if (reply.Status is IPStatus.Success || reply.Status is IPStatus.TtlExpired)
                            routes.Add(new PingHit(reply.Status, reply.Address, reply.RoundtripTime));
                        else if (reply.Status != IPStatus.TtlExpired && reply.Status != IPStatus.TimedOut)
                            break;
                    }
                    catch
                    {
                        routes.Add(new PingHit(IPStatus.Unknown, null, -1f));
                    }
                }
            }

            return routes.ToArray();
        }

        public static void Ping(string host, int size, int count, int timeout, int timeToLive, PingFlags flags, Action<PingResult> callback)
            => Task.Run(() => Ping(host, size, count, timeout, timeToLive, flags)).ContinueWith(t => callback(t.Result));

        public static async Task<PingResult> PingAsync(string host, int size = 64, int count = 10, int timeout = 500, int timeToLive = 128, PingFlags flags = PingFlags.DontFragment)
            => await Task.Run(() => Ping(host, size, count, timeout, timeToLive, flags));

        public static PingResult Ping(string host, int size = 64, int count = 10, int timeout = 500, int timeToLive = 128, PingFlags flags = PingFlags.DontFragment)
        {
            if (string.IsNullOrWhiteSpace(host))
                throw new ArgumentNullException(nameof(host));

            if (size < 1)
                throw new ArgumentOutOfRangeException(nameof(size));

            if (count < 1)
                throw new ArgumentOutOfRangeException(nameof(count));

            if (timeToLive < 1)
                throw new ArgumentOutOfRangeException(nameof(timeToLive));

            var hits = new List<PingHit>();
            var options = new PingOptions(timeToLive, (flags & PingFlags.DontFragment) != 0);
            var data = new byte[size];
            var failed = 0;
            var stats = new Statistics<float>((high, low) => (high + low) / 2f, (high, value) => value > high, (low, value) => value < low);

            CommonLibrary.Random.NextBytes(data);
            
            using (var sender = new Ping())
            {
                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        var reply = sender.Send(host, timeout, data, options);

                        if (reply.Status is IPStatus.Success)
                        {
                            hits.Add(new PingHit(IPStatus.Success, reply.Address, reply.RoundtripTime));
                            stats.Value = reply.RoundtripTime;
                        }
                        else
                        {
                            failed++;
                            hits.Add(new PingHit(reply.Status, reply.Address, reply.RoundtripTime));
                            stats.Value = reply.RoundtripTime;
                        }
                    }
                    catch
                    {
                        failed++;
                        hits.Add(new PingHit(IPStatus.Unknown, null, -1f));
                    }
                }
            }

            return new PingResult(host, Encoding.UTF8.GetString(data), hits.ToArray(), flags, count, failed, timeout, timeToLive, MathUtils.PercentageOf(failed, count), stats);
        }
    }
}