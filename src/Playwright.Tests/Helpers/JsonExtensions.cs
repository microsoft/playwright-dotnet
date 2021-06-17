using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Helpers
{
    /// <summary>
    /// JSON extensions.
    /// </summary>
    internal static class JsonExtensions
    {
        static JsonExtensions() => DefaultJsonSerializerOptions = GetNewDefaultSerializerOptions();

        /// <summary>
        /// Base serialization options used by Microsoft.Playwright.
        /// </summary>
        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }

        /// <summary>
        /// Convert a <see cref="JsonElement"/> to an object.
        /// </summary>
        /// <typeparam name="T">Type to convert the <see cref="JsonElement"/> to.</typeparam>
        /// <param name="element">Element to convert.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Converted value.</returns>
        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
            => JsonSerializer.Deserialize<T>(element.GetRawText(), options ?? DefaultJsonSerializerOptions);

        /// <summary>
        /// Serialize an object.
        /// </summary>
        /// <typeparam name="T">Object type.</typeparam>
        /// <param name="value">Object to serialize.</param>
        /// <param name="options">Serialization options.</param>
        /// <returns>Serialized object.</returns>
        public static string ToJson<T>(this T value, JsonSerializerOptions options = null)
            => JsonSerializer.Serialize(value, options ?? DefaultJsonSerializerOptions);

        internal static JsonSerializerOptions GetNewDefaultSerializerOptions()
            => new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                Converters =
                {
                    new JsonStringEnumMemberConverter(JsonNamingPolicy.CamelCase),
                },
            };
    }
}
