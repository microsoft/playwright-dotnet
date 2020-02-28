using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaywrightSharp.Chromium.Helpers
{
    internal static class JsonHelper
    {
        private static JsonSerializerOptions _defaultChromiumJsonSerializerOptions;

        public static JsonSerializerOptions DefaultChromiumJsonSerializerOptions
        {
            get
            {
                if (_defaultChromiumJsonSerializerOptions == null)
                {
                    _defaultChromiumJsonSerializerOptions = new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        IgnoreNullValues = true,
                    };

                    _defaultChromiumJsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
                }

                return _defaultChromiumJsonSerializerOptions;
            }
        }
    }
}
