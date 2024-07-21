using CommonLib.Pooling;

namespace CommonLib.Serialization.Pooling
{
    public class DeserializerPool : Pool<Deserializer>
    {
        public static DeserializerPool Shared { get; } = new DeserializerPool();

        public override Deserializer Construct()
            => new Deserializer();

        public Deserializer Rent(byte[] data)
        {
            var deserializer = Rent();

            deserializer.Buffer.SetData(data);
            return deserializer;
        }
    }
}