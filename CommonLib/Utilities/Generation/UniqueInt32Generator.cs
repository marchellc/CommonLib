using CommonLib.Caching;

namespace CommonLib.Utilities.Generation
{
    public class UniqueInt32Generator : UniqueGenerator<int>
    {
        public int MinValue { get; set; }
        public int MaxValue { get; set; }

        public UniqueInt32Generator(ICache<int> cache, int minValue = int.MinValue, int maxValue = int.MaxValue) : base(cache)
        {
            SetGenerator(GenerateInt);

            MinValue = minValue;
            MaxValue = maxValue;
        }

        private int GenerateInt()
            => Generator.Instance.GetInt32(MinValue, MaxValue);
    }
}