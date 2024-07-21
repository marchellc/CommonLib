using CommonLib.Pooling;

namespace CommonLib.Serialization.Pooling
{
    public class SerializerPool : Pool<Serializer>
    {
        public static SerializerPool Shared { get; } = new SerializerPool();

        public override Serializer Construct()
            => new Serializer();

        public override void OnRenting(Serializer value)
        {
            base.OnRenting(value);

            if (value.Buffer.IsDisposed)
                value.Buffer.Retrieve();
        }

        public override void OnReturning(Serializer value)
        {
            base.OnReturning(value);

            if (!value.Buffer.IsDisposed)
                value.Buffer.Dispose();
        }
    }
}