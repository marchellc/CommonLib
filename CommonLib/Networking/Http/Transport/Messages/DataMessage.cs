using CommonLib.Networking.Http.Transport.Messages.Interfaces;
using CommonLib.Serialization;

using System;
using System.Collections.Generic;

namespace CommonLib.Networking.Http.Transport.Messages.Data
{
    public struct DataMessage : IHttpMessage
    {
        public List<IHttpMessage> Messages;
        public DateTime Sent;

        public void Serialize(Serializer serializer)
        {
            serializer.Put(Sent);
            serializer.Put(Messages.Count);

            foreach (var msg in Messages)
                serializer.PutObject(msg);
        }

        public void Deserialize(Deserializer deserializer)
        {
            Sent = deserializer.GetDateTime();
            Messages = new List<IHttpMessage>(deserializer.GetInt32());

            for (int i = 0; i < Messages.Capacity; i++)
                Messages.Add((IHttpMessage)deserializer.GetObject());
        }
    }
}