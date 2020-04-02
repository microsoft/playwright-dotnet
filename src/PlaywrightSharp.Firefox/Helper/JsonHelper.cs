using System.Text.Json;
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
                },
            };
        }

        public static JsonSerializerOptions DefaultJsonSerializerOptions { get; }
    }
}
