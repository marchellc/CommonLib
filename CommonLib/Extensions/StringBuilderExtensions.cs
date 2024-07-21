using CommonLib.Pooling.Pools;

using System;
using System.Collections.Generic;
using System.Text;

namespace CommonLib.Extensions
{
    public static class StringBuilderExtensions
    {
        public static void ReturnToShared(this StringBuilder builder)
            => StringBuilderPool.Shared.Return(builder);

        public static string StringReturnToShared(this StringBuilder builder)
            => StringBuilderPool.Shared.ToStringReturn(builder);

        public static StringBuilder AppendLines<TValue>(this StringBuilder builder, IEnumerable<TValue> objects, Func<TValue, string> parser = null, string nullObj = "null")
        {
            objects.ForEach(obj =>
            {
                if (obj is null)
                {
                    builder.AppendLine(nullObj);
                    return;
                }

                if (parser != null)
                {
                    builder.AppendLine(parser(obj));
                    return;
                }

                builder.AppendLine(obj.ToString());
            });

            return builder;
        }
    }
}