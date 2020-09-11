using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport.Converters
{
    internal class EvaluateArgumentValueConverter<T> : JsonConverter<T>
    {
        private readonly EvaluateArgument _parentObject;
        private readonly List<object> _visited = new List<object>();

        public EvaluateArgumentValueConverter()
        {
        }

        internal EvaluateArgumentValueConverter(EvaluateArgument parentObject)
        {
            _parentObject = parentObject;
        }

        public override bool CanConvert(Type type) => true;

        public override T Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using JsonDocument document = JsonDocument.ParseValue(ref reader);
            var result = document.RootElement;

            return (T)ParseEvaluateResult(
                result.ValueKind == JsonValueKind.Object && result.TryGetProperty("value", out var valueProperty)
                    ? valueProperty
                    : result,
                typeof(T),
                options);
        }

        public override void Write(Utf8JsonWriter writer, T value, JsonSerializerOptions options)
        {
            if (_visited.Contains(value))
            {
                throw new JsonException("Argument is a circular structure");
            }

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

            if (value.GetType() == typeof(string))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("s");
                JsonSerializer.Serialize(writer, value);
                writer.WriteEndObject();

                return;
            }

            if (
                value.GetType() == typeof(int) ||
                value.GetType() == typeof(decimal) ||
                value.GetType() == typeof(long) ||
                value.GetType() == typeof(short) ||
                value.GetType() == typeof(double) ||
                value.GetType() == typeof(int?) ||
                value.GetType() == typeof(decimal?) ||
                value.GetType() == typeof(long?) ||
                value.GetType() == typeof(short?) ||
                value.GetType() == typeof(double?))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("n");
                JsonSerializer.Serialize(writer, value);
                writer.WriteEndObject();

                return;
            }

            if (value.GetType() == typeof(bool) || value.GetType() == typeof(bool?))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("b");
                JsonSerializer.Serialize(writer, value);
                writer.WriteEndObject();

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
                _parentObject.Handles.Add(new EvaluateArgumentGuidElement { Guid = channelOwner.Channel.Guid });

                writer.WriteStartObject();
                writer.WriteNumber("h", _parentObject.Handles.Count - 1);
                writer.WriteEndObject();

                return;
            }

            writer.WriteStartObject();
            writer.WritePropertyName("o");
            writer.WriteStartArray();

            _visited.Add(value);
            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(value))
            {
                writer.WriteStartObject();
                object obj = propertyDescriptor.GetValue(value);
                writer.WriteString("k", propertyDescriptor.Name);
                writer.WritePropertyName("v");

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

                writer.WriteEndObject();
            }

            _visited.Remove(value);

            writer.WriteEndArray();
            writer.WriteEndObject();
        }

        private static object ParseEvaluateResult(JsonElement result, Type t, JsonSerializerOptions options)
        {
            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("v", out var value))
            {
                if (value.ValueKind == JsonValueKind.Null)
                {
                    return GetDefaultValue(t);
                }

                return value.ToString() switch
                {
                    "null" => GetDefaultValue(t),
                    "undefined" => GetDefaultValue(t),
                    "Infinity" => double.PositiveInfinity,
                    "-Infinity" => double.NegativeInfinity,
                    "-0" => -0d,
                    "NaN" => double.NaN,
                    _ => value.ToObject(t),
                };
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("d", out var date))
            {
                return date.ToObject<DateTime>();
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("b", out var boolean))
            {
                return boolean.ToObject(t);
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("s", out var stringValue))
            {
                return stringValue.ToObject(t);
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("n", out var numbericValue))
            {
                return numbericValue.ToObject(t);
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("o", out var obj))
            {
                if (t == typeof(JsonElement) || t == typeof(JsonElement?))
                {
                    return obj;
                }

                var keyValues = obj.ToObject<KeyValueObject[]>();
                object objResult = Activator.CreateInstance(t);

                foreach (var kv in keyValues)
                {
                    var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions(false);

                    var property = t.GetProperties().FirstOrDefault(prop => string.Equals(prop.Name, kv.K, StringComparison.OrdinalIgnoreCase));
                    serializerOptions.Converters.Add(GetNewConverter(property.PropertyType));
                    property.SetValue(objResult, kv.V.ToObject(property.PropertyType, serializerOptions));
                }

                return objResult;
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("v", out var vNull) && vNull.ValueKind == JsonValueKind.Null)
            {
                return GetDefaultValue(t);
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("a", out var array) && array.ValueKind == JsonValueKind.Array)
            {
                if (t == typeof(ExpandoObject) || t == typeof(object))
                {
                    return ReadList(array, options);
                }

                return array.ToObject(t, options);
            }

            if (t == typeof(JsonElement?))
            {
                return result;
            }

            if (result.ValueKind == JsonValueKind.Array)
            {
                var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions(false);
                serializerOptions.Converters.Add(GetNewConverter(t.GetElementType()));

                var resultArray = new ArrayList();
                foreach (var item in result.EnumerateArray())
                {
                    resultArray.Add(ParseEvaluateResult(item, t.GetElementType(), serializerOptions));
                }

                var destinationArray = Array.CreateInstance(t.GetElementType(), resultArray.Count);
                Array.Copy(resultArray.ToArray(), destinationArray, resultArray.Count);

                return destinationArray;
            }

            return result.ToObject(t);
        }

        private static object GetDefaultValue(Type t)
        {
            if (t.IsValueType)
            {
                return Activator.CreateInstance(t);
            }

            return null;
        }

        private static JsonConverter GetNewConverter(Type type)
        {
            var converter = typeof(EvaluateArgumentValueConverter<>);
            Type[] typeArgs = { type };
            var makeme = converter.MakeGenericType(typeArgs);
            return (JsonConverter)Activator.CreateInstance(makeme);
        }

        private static Type ValueKindToType(JsonElement element)
            => element.ValueKind switch
            {
                JsonValueKind.Array => typeof(Array),
                JsonValueKind.String => typeof(string),
                JsonValueKind.Number => decimal.Truncate(element.ToObject<decimal>()) != element.ToObject<decimal>() ? typeof(decimal) : typeof(int),
                JsonValueKind.True => typeof(bool),
                JsonValueKind.False => typeof(bool),
                _ => typeof(object),
            };

        private static object ReadList(JsonElement jsonElement, JsonSerializerOptions options)
        {
            IList<object> list = new List<object>();
            foreach (var item in jsonElement.EnumerateArray())
            {
                list.Add(ParseEvaluateResult(item, ValueKindToType(item), options));
            }

            return list.Count == 0 ? null : list;
        }

        private static object ReadObject(JsonElement jsonElement, JsonSerializerOptions options)
        {
            IDictionary<string, object> expandoObject = new ExpandoObject();
            foreach (var obj in jsonElement.EnumerateObject())
            {
                expandoObject[obj.Name] = ParseEvaluateResult(obj.Value, ValueKindToType(obj.Value), options);
            }

            return expandoObject;
        }
    }
}
