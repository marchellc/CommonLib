using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Http.Transport.Messages.Interfaces;

using CommonLib.Serialization;

using System;

namespace CommonLib.Networking.Http.Transport.Messages.Connection
{
    public struct ConnectionMessage : IHttpMessage
    {
        public string Token;
        public bool IsRejected;

        public RejectReason Reason;
        public TimeSpan Delay;

        public ConnectionMessage(RejectReason reason)
        {
            IsRejected = true;
            Reason = reason;
        }

        public ConnectionMessage(string token, TimeSpan delay)
        {
            IsRejected = false;
            Delay = delay;
            Token = token;
        }

        public void Serialize(Serializer serializer)
        {
            serializer.Put(IsRejected);

            if (IsRejected)
                serializer.Put((byte)Reason);
            else
            {
                serializer.Put(Token);
                serializer.Put(Delay);
            }
        }

        public void Deserialize(Deserializer deserializer)
        {
            IsRejected = deserializer.GetBool();

            if (IsRejected)
                Reason = (RejectReason)deserializer.GetByte();
            else
            {
                Token = deserializer.GetString();
                Delay = deserializer.GetTimeSpan();
            }
        }
    }
}
