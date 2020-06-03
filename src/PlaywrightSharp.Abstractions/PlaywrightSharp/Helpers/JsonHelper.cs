using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaywrightSharp.Helpers
{
    internal static class JsonHelper
    {
        static JsonHelper()
            => DefaultJsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                IgnoreNullValues = true,
                Converters =
                {
                    new JSHandleConverter(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                },
            };

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
    }
}
