using CommonLib.Caching;

namespace CommonLib.Utilities.Generation
{
    public class UniqueByteGenerator : UniqueGenerator<byte>
    {
        public byte MinValue { get; set; }
        public byte MaxValue { get; set; }

        public UniqueByteGenerator(ICache<byte> cache, byte minValue = byte.MinValue, byte maxValue = byte.MaxValue) : base(cache)
        {
            SetGenerator(GenerateByte);

            MinValue = minValue;
            MaxValue = maxValue;
        }

        private byte GenerateByte()
            => Generator.Instance.GetByte(MinValue, MaxValue);
    }
}