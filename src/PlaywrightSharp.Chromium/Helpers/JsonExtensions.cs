using System;
using System.IO;
using System.Text.Json;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Chromium.Helpers
{
    internal static partial class JsonExtensions
    {
        public static T ToObject<T>(this JsonElement element)
            => JsonSerializer.Deserialize<T>(element.GetRawText(), JsonHelper.DefaultChromiumJsonSerializerOptions);
    }
}
