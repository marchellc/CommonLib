using System.IO;

namespace CommonLib.Serialization
{
    public class Object : ISerializableObject, IDeserializableObject
    {
        public virtual void Write(BinaryWriter writer) { }
        public virtual void Read(BinaryReader reader) { }
    }
}