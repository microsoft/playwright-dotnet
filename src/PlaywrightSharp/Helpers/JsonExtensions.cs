using System;
using System.IO;
using System.Text.Json;

namespace PlaywrightSharp.Helpers
{
    internal static partial class JsonExtensions
    {
        public static T ToObject<T>(this JsonElement element, JsonSerializerOptions options = null)
            => JsonSerializer.Deserialize<T>(element.GetRawText(), options ?? JsonHelper.DefaultJsonSerializerOptions);

        public static T ToObject<T>(this JsonDocument document, JsonSerializerOptions options = null)
        {
            if (document == null)
            {
                throw new ArgumentNullException(nameof(document));
            }

            return document.RootElement.ToObject<T>(options);
        }
    }
}
