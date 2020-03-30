using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Firefox.Protocol.Runtime;
using DefaultJsonHelper = PlaywrightSharp.Helpers.JsonHelper;

namespace PlaywrightSharp.Firefox.Helper
{
    internal static class JsonHelper
    {
        static JsonHelper()
        {
            DefaultJsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = DefaultJsonHelper.DefaultJsonSerializerOptions.PropertyNamingPolicy,
                IgnoreNullValues = DefaultJsonHelper.DefaultJsonSerializerOptions.IgnoreNullValues,
                Converters =
                {
                    new FirefoxJsonStringEnumConverter<ScreenshotFormat>(),
                    new FirefoxJsonStringEnumConverter<RemoteObjectUnserializableValue>(),
                    new JsonStringEnumConverter(JsonNamingPolicy.CamelCase),
                },
            };
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
    }
}
