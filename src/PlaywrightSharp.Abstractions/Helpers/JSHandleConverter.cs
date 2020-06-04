using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace PlaywrightSharp.Helpers
{
    /// <summary>
    /// JSHandleConverter will throw an exception if an <see cref="IJSHandle"/> object is trying to be serialized.
    /// </summary>
    public class JSHandleConverter : JsonConverter<IJSHandle>
    {
        /// <inheritdoc cref="JsonConverter"/>
        public override IJSHandle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;

        /// <inheritdoc cref="JsonConverter"/>
        public override void Write(Utf8JsonWriter writer, IJSHandle value, JsonSerializerOptions options)
            => throw new PlaywrightSharpException("Unable to make function call. Are you passing a nested JSHandle?");
    }
}
