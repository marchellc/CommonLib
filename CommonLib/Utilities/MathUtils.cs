namespace CommonLib.Utilities
{
    public static class MathUtils
    {
        public static float PercentageOf(float number, float total)
            => (number / total) * 100f;
    }
}