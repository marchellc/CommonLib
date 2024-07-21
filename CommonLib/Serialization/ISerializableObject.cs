namespace CommonLib.Serialization
{
    public interface ISerializableObject
    {
        void Serialize(Serializer serializer);
    }
}