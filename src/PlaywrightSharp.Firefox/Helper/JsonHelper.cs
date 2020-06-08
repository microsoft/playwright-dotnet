using System.Text.Json;
using PlaywrightSharp.Helpers;
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
                    new RemoteObjectJsonConverter(),
                    new NetworkSetAuthCredentialsRequestJsonConverter(),
                    new JSHandleConverter(),
                },
            };
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
    }
}
