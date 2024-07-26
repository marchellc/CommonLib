using System.IO;

namespace CommonLib.Serialization
{
    public interface ISerializableObject
    {
        void Write(BinaryWriter writer);
    }
}