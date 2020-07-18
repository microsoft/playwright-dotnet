using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaywrightSharp.Helpers
{
    internal static class JsonExtensions
    {
        static JsonExtensions() => DefaultJsonSerializerOptions = GetNewDefaultSerializerOptions();

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }

        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
            => JsonSerializer.Deserialize<T>(element.GetRawText(), options ?? DefaultJsonSerializerOptions);

        public static object ToObject(this JsonElement element, Type type, JsonSerializerOptions options = null)
            => JsonSerializer.Deserialize(element.GetRawText(), type, options ?? DefaultJsonSerializerOptions);

        public static string ToJson<T>(this T value, JsonSerializerOptions options = null)
            => JsonSerializer.Serialize(value, options ?? DefaultJsonSerializerOptions);

        public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.RootElement.ToObject<T>(options ?? DefaultJsonSerializerOptions);
        }

        public static JsonSerializerOptions GetNewDefaultSerializerOptions()
            => new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = false,
                Converters =
                {
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                },
            };
    }
}
