using CommonLib.Caching;

using System;

namespace CommonLib.Utilities.Generation
{
    public class UniqueStringGenerator : UniqueGenerator<string>
    {
        public bool AllowUnreadable { get; set; }
        public int StringSize { get; set; }

        public UniqueStringGenerator(ICache<string> cache, int stringSize, bool allowUnreadable) : base(cache)
        {
            if (stringSize < 0)
                throw new ArgumentOutOfRangeException(nameof(stringSize));

            SetGenerator(GenerateString);

            StringSize = stringSize;
            AllowUnreadable = allowUnreadable;
        }

        private string GenerateString()
            => Generator.Instance.GetString(StringSize, AllowUnreadable);
    }
}