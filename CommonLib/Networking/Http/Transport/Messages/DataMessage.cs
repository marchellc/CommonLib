using CommonLib.Networking.Interfaces;
using CommonLib.Serialization;

using System;
using System.Collections.Generic;
using System.IO;

namespace CommonLib.Networking.Http.Transport.Messages.Data
{
    public struct DataMessage : INetworkMessage
    {
        public List<INetworkMessage> Messages;
        public DateTime Sent;

        public void Write(BinaryWriter writer)
        {
            writer.Write(Sent);
            writer.Write(Messages.Count);

            foreach (var msg in Messages)
                writer.WriteObject(msg);
        }

        public void Read(BinaryReader reader)
        {
            Sent = reader.ReadDate();
            Messages = new List<INetworkMessage>(reader.ReadInt32());

            for (int i = 0; i < Messages.Capacity; i++)
                Messages.Add(reader.ReadAnonymous<INetworkMessage>());
        }
    }
}