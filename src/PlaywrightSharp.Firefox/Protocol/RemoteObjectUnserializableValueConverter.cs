using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Firefox.Helper;
using PlaywrightSharp.Firefox.Protocol.Runtime;

namespace PlaywrightSharp.Firefox.Protocol
{
    internal class RemoteObjectUnserializableValueConverter : JsonConverter<RemoteObjectUnserializableValue>
    {
        public override RemoteObjectUnserializableValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            => RemoteObject.GetUnserializableValueFromRaw(reader.GetString()).GetValueOrDefault();

        public override void Write(Utf8JsonWriter writer, RemoteObjectUnserializableValue value, JsonSerializerOptions options)
            => writer.WriteStringValue(value.ToStringValue());
    }
}
