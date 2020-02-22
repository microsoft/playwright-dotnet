using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaywrightSharp.Chromium.Helpers
{
    internal static class JsonHelper
    {
        public static JsonSerializerOptions DefaultChromiumJsonSerializerOptions
        {
            get
            {
                var options = new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    IgnoreNullValues = true,
                };

                options.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));

                return options;
            }
        }
    }
}
