/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Converters
{
    internal class EvaluateArgumentValueConverter<T> : JsonConverter<T>
    {
        private readonly List<object> _visited = new();

        public List<EvaluateArgumentGuidElement> Handles { get; } = new();

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
        }

        public object Serialize(object value)
        {
            if (_visited.Contains(value))
            {
                throw new JsonException("Argument is a circular structure");
            }

            if (value == null)
            {
                return new { v = "null" };
            }

            if (value is double nan && double.IsNaN(nan))
            {
                return new { v = "NaN" };
            }

            if (value is double infinity && double.IsPositiveInfinity(infinity))
            {
                return new { v = "Infinity" };
            }

            if (value is double negativeInfinity && double.IsNegativeInfinity(negativeInfinity))
            {
                return new { v = "-Infinity" };
            }

            if (value is double negativeZero && negativeZero.IsNegativeZero())
            {
                return new { v = "-0" };
            }

            if (value.GetType() == typeof(string))
            {
                return new { s = value };
            }

            if (value.GetType().IsEnum)
            {
                return new { n = (int)value };
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
                return new { n = value };
            }

            if (value.GetType() == typeof(bool) || value.GetType() == typeof(bool?))
            {
                return new { b = value };
            }

            if (value is DateTime date)
            {
                return new { d = date.ToString("o", CultureInfo.InvariantCulture) };
            }

            if (value is Regex regex)
            {
                return new { r = new { p = regex.ToString(), f = regex.Options.GetInlineFlags() } };
            }

            if (value is IDictionary dictionary && dictionary.Keys.OfType<string>().Any())
            {
                _visited.Add(value);

                var o = new List<object>();
                foreach (object key in dictionary.Keys)
                {
                    object obj = dictionary[key];
                    o.Add(new { k = key.ToString(), v = Serialize(obj) });
                }

                _visited.Remove(value);
                return new { o = o };
            }

            if (value is IEnumerable array)
            {
                var a = new List<object>();
                foreach (object item in array)
                {
                    a.Add(Serialize(item));
                }

                return new { a = a };
            }

            if (value is IChannelOwner channelOwner)
            {
                Handles.Add(new() { Guid = channelOwner.Channel.Guid });
                return new { h = Handles.Count - 1 };
            }

            _visited.Add(value);

            var entries = new List<object>();
            foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(value))
            {
                object obj = propertyDescriptor.GetValue(value);
                entries.Add(new { k = propertyDescriptor.Name, v = Serialize(obj) });
            }

            _visited.Remove(value);
            return new { o = entries };
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

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("ref", out var refValue))
            {
                return null;
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("d", out var date))
            {
                return date.ToObject<DateTime>();
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("r", out var regex))
            {
                return new Regex(regex.GetProperty("p").ToString(), RegexOptionsExtensions.FromInlineFlags(regex.GetProperty("f").ToString()));
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("b", out var boolean))
            {
                return boolean.ToObject(t);
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("s", out var stringValue))
            {
                return stringValue.ToObject(t);
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("n", out var numericValue))
            {
                return numericValue.ToObject(t);
            }

            if (result.ValueKind == JsonValueKind.Object && result.TryGetProperty("o", out var obj))
            {
                var keyValues = obj.ToObject<KeyJsonElementValueObject[]>();

                if (t == typeof(JsonElement) || t == typeof(JsonElement?) || t == typeof(ExpandoObject) || t == typeof(object))
                {
                    var dynamicResult = new ExpandoObject();
                    IDictionary<string, object> dicResult;

                    if (typeof(T) == typeof(ExpandoObject) || typeof(T) == typeof(object))
                    {
                        dicResult = dynamicResult;
                    }
                    else
                    {
                        dicResult = new Dictionary<string, object>();
                    }

                    foreach (var kv in keyValues)
                    {
                        var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions();
                        var type = ValueKindToType(kv.V);

                        serializerOptions.Converters.Add(GetNewConverter(type));
                        dicResult[kv.K] = kv.V.ToObject(type, serializerOptions);
                    }

                    if (typeof(T) == typeof(ExpandoObject) || typeof(T) == typeof(object))
                    {
                        return dynamicResult;
                    }

                    var defaultConverter = JsonExtensions.GetNewDefaultSerializerOptions();
                    string serialized = JsonSerializer.Serialize(dicResult, defaultConverter);

                    return JsonSerializer.Deserialize<T>(serialized, defaultConverter);
                }

                try
                {
                    object objResult = Activator.CreateInstance(t);

                    foreach (var kv in keyValues)
                    {
                        var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions();

                        var property = t.GetProperties().FirstOrDefault(prop => string.Equals(prop.Name, kv.K, StringComparison.OrdinalIgnoreCase));
                        serializerOptions.Converters.Add(GetNewConverter(property.PropertyType));
                        property.SetValue(objResult, kv.V.ToObject(property.PropertyType, serializerOptions));
                    }

                    return objResult;
                }
                catch (Exception)
                {
                    return null;
                }
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

            if (t == typeof(JsonElement?) || t == typeof(JsonElement))
            {
                var asString = JsonSerializer.Serialize(ParseEvaluateResult(result, typeof(object), options));
                return JsonSerializer.Deserialize<JsonElement>(asString);
            }
            if (result.ValueKind == JsonValueKind.Array)
            {
                var elementType = t.GetElementType() ?? typeof(object);
                var serializerOptions = JsonExtensions.GetNewDefaultSerializerOptions();
                serializerOptions.Converters.Add(GetNewConverter(elementType));

                var resultArray = Array.CreateInstance(elementType, result.GetArrayLength());
                var i = 0;
                foreach (var item in result.EnumerateArray())
                {
                    resultArray.SetValue(ParseEvaluateResult(item, elementType, serializerOptions), i++);
                }

                return resultArray;
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
        {
            if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("d", out _))
                {
                    return typeof(DateTime);
                }

                if (element.TryGetProperty("b", out _))
                {
                    return typeof(bool);
                }

                if (element.TryGetProperty("s", out _))
                {
                    return typeof(string);
                }

                if (element.TryGetProperty("n", out _))
                {
                    return typeof(decimal);
                }

                if (
                    element.TryGetProperty("v", out var number) &&
                    (number.ToString() == "Infinity" || number.ToString() == "-Infinity" || number.ToString() == "-0" || number.ToString() == "NaN"))
                {
                    return typeof(double);
                }
            }

            return element.ValueKind switch
            {
                JsonValueKind.Array => typeof(Array),
                JsonValueKind.String => typeof(string),
                JsonValueKind.Number => decimal.Truncate(element.ToObject<decimal>()) != element.ToObject<decimal>() ? typeof(decimal) : typeof(int),
                JsonValueKind.True => typeof(bool),
                JsonValueKind.False => typeof(bool),
                _ => typeof(object),
            };
        }

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
