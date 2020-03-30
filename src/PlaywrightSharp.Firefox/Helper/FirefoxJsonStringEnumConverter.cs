using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Firefox.Helper
{
    internal class FirefoxJsonStringEnumConverter<TEnum> : JsonConverter<TEnum>
        where TEnum : Enum
    {
        public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => reader.GetString().ToEnum<TEnum>();

        public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options) => writer.WriteStringValue(value.ToValueString());
    }
}
