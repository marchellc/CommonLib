namespace CommonLib.Utilities
{
    public class PingResult
    {
        public string Host { get; }
        public string Data { get; }

        public PingHit[] Hits { get; }
        public PingFlags Flags { get; }

        public int Count { get; }
        public int Fails { get; }
        public int Timeout { get; }
        public int TimeToLive { get; }

        public float Loss { get; }

        public Statistics<float> Latency { get; }

        public PingResult(string host, string data, PingHit[] hits, PingFlags flags, int count, int fails, int timeout, int timeToLive, float loss, Statistics<float> latency)
        {
            Host = host;
            Data = data;
            Hits = hits;
            Flags = flags;
            Count = count;
            Fails = fails;
            Timeout = timeout;
            TimeToLive = timeToLive;
            Loss = loss;
            Latency = latency;
        }
    }
}
