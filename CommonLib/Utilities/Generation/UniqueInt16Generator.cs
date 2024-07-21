using CommonLib.Caching;

namespace CommonLib.Utilities.Generation
{
    public class UniqueInt16Generator : UniqueGenerator<short>
    {
        public short MinValue { get; set; }
        public short MaxValue { get; set; }

        public UniqueInt16Generator(ICache<short> cache, short minValue = short.MinValue, short maxValue = short.MaxValue) : base(cache)
        {
            SetGenerator(GenerateShort);

            MinValue = minValue;
            MaxValue = maxValue;
        }

        private short GenerateShort()
            => Generator.Instance.GetInt16(MinValue, MaxValue);
    }
}