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
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Transport.Converters;

internal static class EvaluateArgumentValueConverter
{
    internal static JsonObject Serialize(object? value, List<EvaluateArgumentGuidElement> handles, VisitorInfo visitorInfo)
    {
        if (value == null)
        {
            return new JsonObject { ["v"] = JsonValue.Create("null") };
        }

        if (visitorInfo.Visited.TryGetValue(visitorInfo.Identity(value), out var @ref))
        {
            return new JsonObject { ["ref"] = JsonValue.Create(@ref) };
        }

        if (value is double nan && double.IsNaN(nan))
        {
            return new JsonObject { ["v"] = JsonValue.Create("NaN") };
        }

        if (value is double infinity && double.IsPositiveInfinity(infinity))
        {
            return new JsonObject { ["v"] = JsonValue.Create("Infinity") };
        }

        if (value is double negativeInfinity && double.IsNegativeInfinity(negativeInfinity))
        {
            return new JsonObject { ["v"] = JsonValue.Create("-Infinity") };
        }

        if (value is double negativeZero && negativeZero.IsNegativeZero())
        {
            return new JsonObject { ["v"] = JsonValue.Create("-0") };
        }

        if (value.GetType() == typeof(string))
        {
            return new JsonObject { ["s"] = JsonValue.Create((string)value) };
        }

        if (value.GetType().IsEnum)
        {
            return new JsonObject { ["n"] = JsonValue.Create((int)value) };
        }

        if (value is int i)
        {
            return new JsonObject { ["n"] = JsonValue.Create(i) };
        }

        if (value is decimal)
        {
            return new JsonObject { ["n"] = JsonValue.Create((double)value) };
        }

        if (value is long l)
        {
            return new JsonObject { ["n"] = JsonValue.Create(l) };
        }

        if (value is short s)
        {
            return new JsonObject { ["n"] = JsonValue.Create((int)s) };
        }

        if (value is double dbl)
        {
            return new JsonObject { ["n"] = JsonValue.Create(dbl) };
        }

        if (value is bool b)
        {
            return new JsonObject { ["b"] = JsonValue.Create(b) };
        }

        if (value is DateTime date)
        {
            return new JsonObject { ["d"] = JsonValue.Create(date.ToString("o", CultureInfo.InvariantCulture)) };
        }

        if (value is Uri uri)
        {
            return new JsonObject { ["u"] = JsonValue.Create(uri.ToString()) };
        }

        if (value is BigInteger bigInteger)
        {
            return new JsonObject { ["bi"] = JsonValue.Create(bigInteger.ToString(CultureInfo.InvariantCulture)) };
        }

        if (value is Exception exception)
        {
            return new JsonObject
            {
                ["e"] = new JsonObject
                {
                    ["n"] = JsonValue.Create(exception.GetType().Name),
                    ["m"] = JsonValue.Create(exception.Message),
                    ["s"] = JsonValue.Create(exception.StackTrace ?? string.Empty),
                },
            };
        }

        if (value is Regex regex)
        {
            var (p, f) = regex.GetSourceAndFlags();
            return new JsonObject
            {
                ["r"] = new JsonObject
                {
                    ["p"] = JsonValue.Create(p),
                    ["f"] = JsonValue.Create(f),
                },
            };
        }

        if (value is Guid guid)
        {
            return new JsonObject { ["s"] = JsonValue.Create(guid.ToString()) };
        }

        if (value is ExpandoObject)
        {
            var entries = new List<JsonNode?>();
            int id = ++visitorInfo.LastId;
            visitorInfo.Visited.Add(visitorInfo.Identity(value), id);
            foreach (KeyValuePair<string, object?> property in (IDictionary<string, object?>)value)
            {
                entries.Add(new JsonObject
                {
                    ["k"] = JsonValue.Create(property.Key),
                    ["v"] = Serialize(property.Value, handles, visitorInfo),
                });
            }
            return new JsonObject { ["o"] = new JsonArray(entries.ToArray()), ["id"] = JsonValue.Create(id) };
        }

        if (value is IDictionary dictionary)
        {
            bool hasStringKey = false;
            foreach (object key in dictionary.Keys)
            {
                if (key is string)
                {
                    hasStringKey = true;
                    break;
                }
            }

            if (hasStringKey)
            {
                var entries = new List<JsonNode?>();
                int id = ++visitorInfo.LastId;
                visitorInfo.Visited.Add(visitorInfo.Identity(value), id);
                foreach (object key in dictionary.Keys)
                {
                    entries.Add(new JsonObject
                    {
                        ["k"] = JsonValue.Create(key.ToString()),
                        ["v"] = Serialize(dictionary[key], handles, visitorInfo),
                    });
                }

                return new JsonObject { ["o"] = new JsonArray(entries.ToArray()), ["id"] = JsonValue.Create(id) };
            }
        }

        if (value is IEnumerable enumerable)
        {
            var items = new List<JsonNode?>();
            int id = ++visitorInfo.LastId;
            visitorInfo.Visited.Add(visitorInfo.Identity(value), id);
            foreach (object item in enumerable)
            {
                items.Add(Serialize(item, handles, visitorInfo));
            }

            return new JsonObject { ["a"] = new JsonArray(items.ToArray()), ["id"] = JsonValue.Create(id) };
        }

        if (value is ChannelOwner channelOwner)
        {
            handles.Add(new() { Guid = channelOwner.Guid });
            return new JsonObject { ["h"] = JsonValue.Create(handles.Count - 1) };
        }

        throw new PlaywrightException($"Cannot serialize type '{value.GetType().FullName}'. Pass IDictionary<string, object?>, a supported primitive type, or an array of supported types.");
    }

    internal static object? Deserialize(JsonElement result, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type t)
    {
        var parsed = ParseEvaluateResultToExpando(result, new Dictionary<int, object>());

        // If use wants expando or any object -> return as is.
        if (t == typeof(ExpandoObject) || t == typeof(object))
        {
            return parsed;
        }

        // User wants Json, serialize to JsonElement.
        if (t == typeof(JsonElement) || t == typeof(JsonElement?))
        {
            if (t == typeof(JsonElement?) && parsed == null)
            {
                return null;
            }
            var jsonStr = JsonSerializer.Serialize(parsed, parsed!.GetType(), PlaywrightJsonContext.Default);
            return JsonDocument.Parse(jsonStr).RootElement;
        }

        // Convert recursively to a requested type.
        return ToExpectedType(parsed, t, new Dictionary<object, object>());
    }

    private static object? ToExpectedType(object? parsed, [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)] Type t, IDictionary<object, object> visited)
    {
        if (parsed == null)
        {
            return null;
        }

        if (visited.TryGetValue(parsed, out var value))
        {
            return value;
        }

        if (parsed is Array parsedArray)
        {
            var result = (IList)Activator.CreateInstance(t, parsedArray.Length)!;
            visited.Add(parsed, result);
            var elementType = t.GetElementType()!;
            for (int i = 0; i < parsedArray.Length; ++i)
            {
                result[i] = InterpretValue(parsedArray.GetValue(i), elementType);
            }
            return result;
        }

        if (parsed is ExpandoObject parsedExpando)
        {
            object objResult;
            try
            {
                objResult = Activator.CreateInstance(t)!;
            }
            catch (Exception ex)
            {
                throw new PlaywrightException("Return type mismatch. Expecting " + t.ToString() + ", got Object", ex);
            }
            visited.Add(parsed, objResult);

            foreach (var kv in parsedExpando)
            {
                var property = Array.Find(t.GetProperties(), prop => string.Equals(prop.Name, kv.Key, StringComparison.OrdinalIgnoreCase));
                if (property != null)
                {
                    property.SetValue(objResult, InterpretValue(kv.Value, property.PropertyType));
                }
            }

            return objResult;
        }

        return ChangeType(parsed, t);
    }

    private static object? InterpretValue(object? parsed, Type targetType)
    {
        if (parsed == null)
        {
            return null;
        }
        return ChangeType(parsed, targetType);
    }

    private static object? ChangeType(object value, Type conversion)
    {
        var t = conversion;

        if (t.IsGenericType && t.GetGenericTypeDefinition().Equals(typeof(Nullable<>)))
        {
            if (value == null)
            {
                return null;
            }

            t = Nullable.GetUnderlyingType(t)!;
        }

        if (t == typeof(Guid))
        {
            if (value == null)
            {
                return Guid.Empty;
            }
            return Guid.Parse(value.ToString()!);
        }

        return Convert.ChangeType(value, t, CultureInfo.InvariantCulture)!;
    }

    private static object? ParseEvaluateResultToExpando(JsonElement result, IDictionary<int, object> refs)
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
            return refs[refValue.GetInt32()];
        }

        if (result.TryGetProperty("d", out var date))
        {
            return date.ToObject<DateTime>();
        }

        if (result.TryGetProperty("u", out var url))
        {
            return url.ToObject<Uri>();
        }

        if (result.TryGetProperty("bi", out var bigInt))
        {
            return BigInteger.Parse(bigInt.ToObject<string>(), CultureInfo.InvariantCulture);
        }

        if (result.TryGetProperty("e", out var error))
        {
            return new Exception(error.GetProperty("s").ToString());
        }

        if (result.TryGetProperty("r", out var regex))
        {
            return new Regex(regex.GetProperty("p").ToString(), RegexOptionsExtensions.FromInlineFlags(regex.GetProperty("f").ToString()));
        }

        if (result.TryGetProperty("ta", out var ta))
        {
            byte[] bytes = Convert.FromBase64String(ta.GetProperty("b").ToString());
            return ta.GetProperty("k").ToString() switch
            {
                "i8" => bytes.Select(b => unchecked((sbyte)b)).ToArray(),
                "ui8" => bytes,
                "ui8c" => bytes,
                "i16" => Enumerable.Range(0, bytes.Length / 2).Select(i => BitConverter.ToInt16(bytes, i * 2)).ToArray(),
                "ui16" => Enumerable.Range(0, bytes.Length / 2).Select(i => BitConverter.ToUInt16(bytes, i * 2)).ToArray(),
                "i32" => Enumerable.Range(0, bytes.Length / 4).Select(i => BitConverter.ToInt32(bytes, i * 4)).ToArray(),
                "ui32" => Enumerable.Range(0, bytes.Length / 4).Select(i => BitConverter.ToUInt32(bytes, i * 4)).ToArray(),
                "f32" => Enumerable.Range(0, bytes.Length / 4).Select(i => BitConverter.ToSingle(bytes, i * 4)).ToArray(),
                "f64" => Enumerable.Range(0, bytes.Length / 8).Select(i => BitConverter.ToDouble(bytes, i * 8)).ToArray(),
                "bi64" => Enumerable.Range(0, bytes.Length / 8).Select(i => BitConverter.ToInt64(bytes, i * 8)).ToArray(),
                "bui64" => Enumerable.Range(0, bytes.Length / 8).Select(i => BitConverter.ToUInt64(bytes, i * 8)).ToArray(),
                _ => null,
            };
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
            refs.Add(result.GetProperty("id").GetInt32(), expando);
            IDictionary<string, object?> dict = expando;
            foreach (var kv in obj.ToObject<KeyJsonElementValueObject[]>())
            {
                dict[kv.K] = ParseEvaluateResultToExpando(kv.V, refs);
            }

            return expando;
        }

        if (result.TryGetProperty("a", out var array))
        {
            List<object?> list = [];
            refs.Add(result.GetProperty("id").GetInt32(), list);
            foreach (var item in array.EnumerateArray())
            {
                list.Add(ParseEvaluateResultToExpando(item, refs));
            }
            return list.ToArray();
        }
        return null;
    }

    internal class VisitorInfo
    {
        private readonly Dictionary<object, long> _objectIds = new(ReferenceEqualityComparer.Instance);
        private long _nextId;

        internal VisitorInfo()
        {
            Visited = new Dictionary<long, int>();
        }

        internal Dictionary<long, int> Visited { get; set; }

        internal int LastId { get; set; }

        internal long Identity(object obj)
        {
            if (!_objectIds.TryGetValue(obj, out var id))
            {
                id = Interlocked.Increment(ref _nextId);
                _objectIds[obj] = id;
            }
            return id;
        }
    }

    internal sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        public static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
