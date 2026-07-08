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
using System.Dynamic;
using System.Globalization;
using System.Numerics;
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

    internal static object? Deserialize(JsonElement result, Type t)
    {
        var parsed = ParseEvaluateResultToJsonNode(result, new Dictionary<int, JsonNode>());

        // For JsonElement, fully resolve protocol markers and return the deserialized JSON.
        if (t == typeof(JsonElement))
        {
            var json = parsed?.ToJsonString() ?? "null";
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }
        if (t == typeof(JsonElement?))
        {
            if (parsed == null)
            {
                return null;
            }
            var json = parsed.ToJsonString();
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement.Clone();
        }

        if (t == typeof(ExpandoObject) || t == typeof(object))
        {
            return ConvertJsonNodeToObject(parsed);
        }

        // Handle Exception specially (TargetSite has RequiresUnreferencedCode).
        if (t == typeof(Exception) && parsed is JsonObject exObj && exObj.TryGetPropertyValue("__exception__", out _))
        {
            var exMsg = exObj.TryGetPropertyValue("message", out var exMsgNode) ? exMsgNode?.ToString() : null;
            return new Exception(exMsg ?? "Unknown error");
        }

        if (parsed != null)
        {
            var typeInfo = PlaywrightJsonContext.Default.GetTypeInfo(t);
            if (typeInfo != null)
            {
                var json = parsed.ToJsonString();
                return JsonSerializer.Deserialize(json, typeInfo);
            }
        }

        throw new PlaywrightException(
            $"Return type '{t.FullName}' is not registered for AOT-safe deserialization. " +
            "Use object, JsonElement, or add [JsonSerializable(typeof(T))] to PlaywrightJsonContext.");
    }

    private static object? ConvertJsonNodeToObject(JsonNode? node)
    {
        if (node == null)
        {
            return null;
        }
        if (node is JsonObject obj)
        {
            if (obj.TryGetPropertyValue("__exception__", out _))
            {
                var msg = obj.TryGetPropertyValue("message", out var msgNode) ? msgNode?.ToString() : null;
                return new Exception(msg ?? "Unknown error");
            }
            var expando = new ExpandoObject();
            var dict = (IDictionary<string, object?>)expando;
            foreach (var kvp in obj)
            {
                dict[kvp.Key] = ConvertJsonNodeToObject(kvp.Value);
            }
            return expando;
        }
        if (node is JsonArray arr)
        {
            var list = new List<object?>();
            foreach (var item in arr)
            {
                list.Add(ConvertJsonNodeToObject(item));
            }
            return list.ToArray();
        }
        if (node is JsonValue val)
        {
            if (val.TryGetValue(out string? s))
            {
                return s;
            }
            if (val.TryGetValue(out int i))
            {
                return i;
            }
            if (val.TryGetValue(out long l))
            {
                return l;
            }
            if (val.TryGetValue(out double d))
            {
                return d;
            }
            if (val.TryGetValue(out bool b))
            {
                return b;
            }
            if (val.TryGetValue(out DateTime dt))
            {
                return dt;
            }
            if (val.TryGetValue(out Uri? uri))
            {
                return uri;
            }
        }
        return null;
    }

    private static JsonNode? ParseEvaluateResultToJsonNode(JsonElement result, Dictionary<int, JsonNode> refs)
    {
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
                "Infinity" => JsonValue.Create(double.PositiveInfinity),
                "-Infinity" => JsonValue.Create(double.NegativeInfinity),
                "-0" => JsonValue.Create(-0d),
                "NaN" => JsonValue.Create(double.NaN),
                _ => null,
            };
        }

        if (result.TryGetProperty("ref", out var refValue))
        {
            return refs[refValue.GetInt32()];
        }

        if (result.TryGetProperty("d", out var date))
        {
            return JsonValue.Create(date.ToObject<DateTime>());
        }

        if (result.TryGetProperty("u", out var url))
        {
            return JsonValue.Create(url.ToObject<Uri>().ToString());
        }

        if (result.TryGetProperty("bi", out var bigInt))
        {
            return JsonValue.Create(bigInt.ToObject<string>());
        }

        if (result.TryGetProperty("e", out var error))
        {
            var stack = error.TryGetProperty("s", out var s) ? s.ToString() : null;
            var msg = error.TryGetProperty("m", out var m) ? m.ToString() : null;
            return new JsonObject
            {
                ["__exception__"] = JsonValue.Create(true),
                ["message"] = JsonValue.Create(stack ?? msg ?? string.Empty),
                ["stack"] = JsonValue.Create(string.Empty),
            };
        }

        if (result.TryGetProperty("r", out var regex))
        {
            return JsonValue.Create(regex.GetProperty("p").ToString());
        }

        if (result.TryGetProperty("ta", out var ta))
        {
            return JsonValue.Create(ta.GetProperty("k").ToString());
        }

        if (result.TryGetProperty("b", out var boolean))
        {
            return JsonValue.Create(boolean.ToObject<bool>());
        }

        if (result.TryGetProperty("s", out var stringValue))
        {
            return JsonValue.Create(stringValue.ToObject<string>());
        }

        if (result.TryGetProperty("n", out var numericValue))
        {
            return JsonValue.Create(numericValue.ToObject<double>());
        }

        if (result.TryGetProperty("o", out var obj))
        {
            var jsonObj = new JsonObject();
            refs.Add(result.GetProperty("id").GetInt32(), jsonObj);
            foreach (var kv in obj.ToObject<KeyJsonElementValueObject[]>())
            {
                jsonObj[kv.K] = ParseEvaluateResultToJsonNode(kv.V, refs);
            }
            return jsonObj;
        }

        if (result.TryGetProperty("a", out var array))
        {
            var jsonArray = new JsonArray();
            refs.Add(result.GetProperty("id").GetInt32(), jsonArray);
            foreach (var item in array.EnumerateArray())
            {
                jsonArray.Add(ParseEvaluateResultToJsonNode(item, refs));
            }
            return jsonArray;
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
