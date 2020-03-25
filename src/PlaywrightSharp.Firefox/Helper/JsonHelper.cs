using System.Text.Json;
using PlaywrightSharp.Firefox.Protocol;
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
                    new RemoteObjectUnserializableValueConverter(),
                    DefaultJsonHelper.DefaultJsonSerializerOptions.Converters[0],
                },
            };
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
    }
}
