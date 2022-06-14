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
    internal class EvaluateArgumentValueConverter<T>
    {
        private readonly List<object> _visited = new();

        public List<EvaluateArgumentGuidElement> Handles { get; } = new();

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

        internal static object ParseEvaluateResult(JsonElement result, Type t)
        {
            var parsed = ParseEvaluateResultToExpando(result);

            // If use wants expando or any object -> return as is.
            if (t == typeof(ExpandoObject) || t == typeof(object))
            {
                return parsed;
            }

            // User wants Json, serialize/parse. On .Net 6 there is a method that does this w/o full serialization.
            if (t == typeof(JsonElement) || t == typeof(JsonElement?))
            {
                string serialized = JsonSerializer.Serialize(parsed);
                return JsonSerializer.Deserialize<T>(serialized);
            }

            // Convert recursively to a requested type.
            return ToExpectedType(parsed, t);
        }

        private static object ToExpectedType(object parsed, Type t)
        {
            if (parsed == null)
            {
                return null;
            }

            if (parsed is Array)
            {
                var parsedArray = (Array)parsed;
                var result = (IList)Activator.CreateInstance(t, parsedArray.Length);
                for (int i = 0; i < parsedArray.Length; ++i)
                {
                    result[i] = ToExpectedType(parsedArray.GetValue(i), t.GetElementType());
                }
                return result;
            }

            if (parsed is ExpandoObject)
            {
                object objResult;
                try
                {
                    objResult = Activator.CreateInstance(t);
                }
                catch (Exception)
                {
                    throw new PlaywrightException("Return type mismatch. Expecting " + t.ToString() + ", got Object");
                }

                foreach (var kv in (ExpandoObject)parsed)
                {
                    var property = t.GetProperties().FirstOrDefault(prop => string.Equals(prop.Name, kv.Key, StringComparison.OrdinalIgnoreCase));
                    if (property != null)
                    {
                        property.SetValue(objResult, ToExpectedType(kv.Value, property.PropertyType));
                    }
                }

                return objResult;
            }

            return ChangeType(parsed, t);
        }

        private static object ChangeType(object value, Type conversion)
        {
            var t = conversion;

            if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
            {
                if (value == null)
                {
                    return null;
                }

                t = Nullable.GetUnderlyingType(t);
            }

            return Convert.ChangeType(value, t);
        }

        private static object ParseEvaluateResultToExpando(JsonElement result)
        {
            // Parse JSON into a structure where objects/arrays are represented with expando/arrays.
            if (result.TryGetProperty("v", out var value))
            {
                if (value.ValueKind == JsonValueKind.Null)
                {
                    return null;
                }

                return value.ToString() switch
                {
                    "null" => null,
                    "undefined" => null,
                    "Infinity" => double.PositiveInfinity,
                    "-Infinity" => double.NegativeInfinity,
                    "-0" => -0d,
                    "NaN" => double.NaN,
                    _ => null,
                };
            }

            if (result.TryGetProperty("ref", out var refValue))
            {
                return null;
            }

            if (result.TryGetProperty("d", out var date))
            {
                return date.ToObject<DateTime>();
            }

            if (result.TryGetProperty("r", out var regex))
            {
                return new Regex(regex.GetProperty("p").ToString(), RegexOptionsExtensions.FromInlineFlags(regex.GetProperty("f").ToString()));
            }

            if (result.TryGetProperty("b", out var boolean))
            {
                return boolean.ToObject<bool>();
            }

            if (result.TryGetProperty("s", out var stringValue))
            {
                return stringValue.ToObject<string>();
            }

            if (result.TryGetProperty("n", out var numericValue))
            {
                return numericValue.ToObject<double>();
            }

            if (result.TryGetProperty("o", out var obj))
            {
                var expando = new ExpandoObject();
                IDictionary<string, object> dict = expando;
                var keyValues = obj.ToObject<KeyJsonElementValueObject[]>();

                foreach (var kv in keyValues)
                {
                    dict[kv.K] = ParseEvaluateResultToExpando(kv.V);
                }

                return expando;
            }

            if (result.TryGetProperty("a", out var array))
            {
                IList<object> list = new List<object>();
                foreach (var item in array.EnumerateArray())
                {
                    list.Add(ParseEvaluateResultToExpando(item));
                }
                return list.ToArray();
            }
            return null;
        }
    }
}
