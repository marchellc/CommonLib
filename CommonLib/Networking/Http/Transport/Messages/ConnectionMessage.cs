using CommonLib.Networking.Http.Transport.Enums;
using CommonLib.Networking.Interfaces;
using CommonLib.Serialization;
using System;
using System.IO;

namespace CommonLib.Networking.Http.Transport.Messages.Connection
{
    public struct ConnectionMessage : INetworkMessage
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

        public void Write(BinaryWriter writer)
        {
            writer.Write(IsRejected);

            if (IsRejected)
                writer.Write((byte)Reason);
            else
            {
                writer.Write(Token);
                writer.Write(Delay);
            }
        }

        public void Read(BinaryReader reader)
        {
            IsRejected = reader.ReadBoolean();

            if (IsRejected)
                Reason = (RejectReason)reader.ReadByte();
            else
            {
                Token = reader.ReadString();
                Delay = reader.ReadSpan();
            }
        }
    }
}
