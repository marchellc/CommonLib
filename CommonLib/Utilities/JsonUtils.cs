using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

using System;

namespace CommonLib.Utilities
{
    public static class JsonUtils
    {
        private static readonly JsonSerializerSettings IndentedJsonSettings;
        private static readonly JsonSerializerSettings NotIndentedJsonSettings;

        static JsonUtils()
        {
            IndentedJsonSettings = new JsonSerializerSettings()
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.Indented,
                CheckAdditionalContent = true
            };

            NotIndentedJsonSettings = new JsonSerializerSettings()
            {
                ConstructorHandling = ConstructorHandling.AllowNonPublicDefaultConstructor,
                Formatting = Formatting.None,
                CheckAdditionalContent = true
            };

            IndentedJsonSettings.Converters.Add(new StringEnumConverter(false));
            NotIndentedJsonSettings.Converters.Add(new StringEnumConverter(false));
        }

        public static T JsonDeserialize<T>(this string json, bool indent = false)
            => JsonConvert.DeserializeObject<T>(json, (indent ? IndentedJsonSettings : NotIndentedJsonSettings));

        public static object JsonDeserialize(this string json, Type type, bool indent = false)
            => JsonConvert.DeserializeObject(json, type, (indent ? IndentedJsonSettings : NotIndentedJsonSettings));

        public static string JsonSerialize(this object value, bool indent = false)
            => JsonConvert.SerializeObject(value, (indent ? IndentedJsonSettings : NotIndentedJsonSettings));
    }
}