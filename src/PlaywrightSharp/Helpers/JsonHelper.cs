using System;
using System.Text.Json;

namespace PlaywrightSharp.Helpers
{
    internal static class JsonHelper
    {
        public static JsonSerializerOptions DefaultJsonSerializerOptions => new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            IgnoreNullValues = true,
        };
    }
}
