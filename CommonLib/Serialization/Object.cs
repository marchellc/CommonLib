namespace CommonLib.Serialization
{
    public class Object : ISerializableObject, IDeserializableObject
    {
        public virtual void Serialize(Serializer serializer) { }
        public virtual void Deserialize(Deserializer deserializer) { }
    }
}