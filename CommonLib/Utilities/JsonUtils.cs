using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;

namespace CommonLib.Utilities
{
    public static class JsonUtils
    {
        private static readonly JsonSerializerSettings JsonSettings;

        static JsonUtils()
        {
            JsonSettings = new JsonSerializerSettings()
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented,
                CheckAdditionalContent = true
            };

            JsonSettings.Converters.Add(new StringEnumConverter(false));
        }

        public static T JsonDeserialize<T>(this string json)
            => JsonConvert.DeserializeObject<T>(json, JsonSettings);

        public static object JsonDeserialize(this string json, Type type)
            => JsonConvert.DeserializeObject(json, type, JsonSettings);

        public static string JsonSerialize(this object value)
            => JsonConvert.SerializeObject(value, JsonSettings);
    }
}