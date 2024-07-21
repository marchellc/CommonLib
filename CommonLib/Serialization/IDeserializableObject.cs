namespace CommonLib.Serialization
{
    public interface IDeserializableObject
    {
        void Deserialize(Deserializer deserializer);
    }
}