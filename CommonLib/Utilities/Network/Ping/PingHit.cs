using System.Net;
using System.Net.NetworkInformation;

namespace CommonLib.Utilities
{
    public struct PingHit
    {
        public readonly IPStatus Status;
        public readonly IPAddress Address;
        public readonly float Latency;

        public PingHit(IPStatus status, IPAddress address, float latency)
        {
            Status = status;
            Address = address;
            Latency = latency;
        }
    }
}
