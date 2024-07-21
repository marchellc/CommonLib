using CommonLib.Extensions;

using System.Collections.Generic;
using System.Text;

namespace CommonLib.Pooling.Pools
{
    public class StringBuilderPool : Pool<StringBuilder>
    {
        public static StringBuilderPool Shared { get; } = new StringBuilderPool(32);

        public StringBuilderPool(uint size) : base(size) { }

        public int MinSize { get; set; } = 512;

        public override StringBuilder Construct()
            => new StringBuilder(MinSize);

        public StringBuilder Rent(char lineSeparator, params string[] lines)
        {
            var builder = Rent();

            for (int i = 0; i < lines.Length; i++)
                builder.Append($"{lines[i]}{lineSeparator}");

            return builder;
        }

        public StringBuilder Rent(char lineSeparator, IEnumerable<string> lines)
        {
            var builder = Rent();

            foreach (var line in lines)
                builder.Append($"{line}{lineSeparator}");

            return builder;
        }

        public StringBuilder Rent(int capacity)
        {
            var builder = Rent();

            if (builder.Capacity < capacity)
                builder.Capacity = capacity;

            return builder;
        }

        public StringBuilder RentLines(params string[] lines)
            => Rent('\n', lines);

        public StringBuilder Rent(IEnumerable<string> lines)
            => Rent('\n', lines);

        public string[] ToArrayReturn(StringBuilder builder)
        {
            var str = builder.ToString();
            Return(builder);
            return str.SplitLines();
        }

        public string ToStringReturn(StringBuilder builder)
        {
            var str = builder.ToString();
            Return(builder);
            return str;
        }

        public override void OnReturning(StringBuilder value)
            => value.Clear();
    }
}
