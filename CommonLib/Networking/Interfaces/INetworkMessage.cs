using CommonLib.Serialization;

namespace CommonLib.Networking.Interfaces
{
    public interface INetworkMessage : ISerializableObject, IDeserializableObject { }
}