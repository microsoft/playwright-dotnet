using System;
using System.Collections;
using System.ComponentModel;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Converters
{
    internal class EvaluateArgumentValueConverter : JsonConverter<object>
    {
        private readonly EvaluateArgument _parentObject;

        internal EvaluateArgumentValueConverter(EvaluateArgument parentObject)
        {
            _parentObject = parentObject;
        }

        public override bool CanConvert(Type type) => true;

        public override object Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            throw new NotImplementedException();
        }

        public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteStartObject();
                writer.WriteNull("v");
                writer.WriteEndObject();

                return;
            }

            if (value is double nan && double.IsNaN(nan))
            {
                writer.WriteStartObject();
                writer.WriteString("v", "NaN");
                writer.WriteEndObject();

                return;
            }

            if (value is double infinity && double.IsPositiveInfinity(infinity))
            {
                writer.WriteStartObject();
                writer.WriteString("v", "Infinity");
                writer.WriteEndObject();

                return;
            }

            if (value is double negativeInfinity && double.IsNegativeInfinity(negativeInfinity))
            {
                writer.WriteStartObject();
                writer.WriteString("v", "-Infinity");
                writer.WriteEndObject();

                return;
            }

            if (value is double negativeZero && negativeZero.IsNegativeZero())
            {
                writer.WriteStartObject();
                writer.WriteString("v", "-0");
                writer.WriteEndObject();

                return;
            }

            if (IsPrimitiveValue(value.GetType()))
            {
                JsonSerializer.Serialize(writer, value);
                return;
            }

            if (value is DateTime date)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("d");
                JsonSerializer.Serialize(writer, date);
                writer.WriteEndObject();

                return;
            }

            if (value is IDictionary)
            {
                JsonSerializer.Serialize(writer, value);
                return;
            }

            if (value is IEnumerable array)
            {
                writer.WriteStartObject();
                writer.WritePropertyName("a");
                writer.WriteStartArray();

                foreach (object item in array)
                {
                    JsonSerializer.Serialize(writer, item, options);
                }

                writer.WriteEndArray();
                writer.WriteEndObject();

                return;
            }

            if (value is IChannelOwner channelOwner)
            {
                _parentObject.Guids.Add(new EvaluateArgumentGuidElement { Guid = channelOwner.Channel.Guid });

                writer.WriteStartObject();
                writer.WriteNumber("h", _parentObject.Guids.Count - 1);
                writer.WriteEndObject();

                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("o");
            writer.WriteStartObject();

            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(value))
            {
                object obj = propertyDescriptor.GetValue(value);
                writer.WritePropertyName(propertyDescriptor.Name);

                if (obj == null)
                {
                    writer.WriteStartObject();
                    writer.WriteNull("v");
                    writer.WriteEndObject();
                }
                else
                {
                    JsonSerializer.Serialize(writer, obj, options);
                }
            }

            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        private static bool IsPrimitiveValue(Type type)
            => type == typeof(string) ||
            type == typeof(int) ||
            type == typeof(decimal) ||
            type == typeof(double) ||
            type == typeof(bool) ||
            type == typeof(int?) ||
            type == typeof(decimal?) ||
            type == typeof(double?) ||
            type == typeof(bool?);
    }
}
