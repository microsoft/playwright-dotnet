using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Helpers;

namespace PlaywrightSharp.Transport.Converters
{
    internal class EvaluateArgumentConverter : JsonConverter<EvaluateArgument>
    {
        public override bool CanConvert(Type type) => typeof(EvaluateArgument).IsAssignableFrom(type);

        public override EvaluateArgument Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => null;

        public override void Write(Utf8JsonWriter writer, EvaluateArgument value, JsonSerializerOptions options)
        {
            var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions(false);
            serializerOptions.Converters.Add(new EvaluateArgumentValueConverter<object>(value));

            writer.WriteStartObject();
            writer.WritePropertyName("value");

            if (value.Value == null)
            {
                writer.WriteStartObject();
                writer.WriteString("v", "null");
                writer.WriteEndObject();
            }
            else
            {
                JsonSerializer.Serialize(writer, value.Value, serializerOptions);
            }

            writer.WritePropertyName("handles");
            JsonSerializer.Serialize(writer, value.Handles, options);
            writer.WriteEndObject();
        }
    }
}
