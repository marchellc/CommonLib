using System.IO;

namespace CommonLib.Serialization
{
    public interface IDeserializableObject
    {
        void Read(BinaryReader reader);
    }
}