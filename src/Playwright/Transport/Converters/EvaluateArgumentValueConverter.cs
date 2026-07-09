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

        // Try source-gen serialization for types registered in PlaywrightJsonContext.
        var knownTypeInfo = PlaywrightJsonContext.Default.GetTypeInfo(value.GetType());
        if (knownTypeInfo != null)
        {
            var node = JsonSerializer.SerializeToNode(value, knownTypeInfo);
            if (node is JsonObject obj)
            {
                var entries = new List<JsonNode?>();
                int id = ++visitorInfo.LastId;
                visitorInfo.Visited.Add(visitorInfo.Identity(value), id);
                foreach (var kvp in obj)
                {
                    entries.Add(new JsonObject
                    {
                        ["k"] = JsonValue.Create(kvp.Key),
                        ["v"] = Serialize(kvp.Value, handles, visitorInfo),
                    });
                }
                return new JsonObject { ["o"] = new JsonArray(entries.ToArray()), ["id"] = JsonValue.Create(id) };
            }
        }

        // No reflection fallback for AOT: user must use primitives, Dictionary, ExpandoObject, or a registered type.
        throw new PlaywrightException(
            $"Type '{value.GetType().FullName}' is not registered for AOT-safe serialization. " +
            "Use primitives, Dictionary<string, object?>, ExpandoObject, arrays, or register your type in PlaywrightJsonContext.");
    }

    internal static object? Deserialize(JsonElement result, Type t)
    {
        var refCounter = new RefCounter();
        var parsed = ParseEvaluateResultToJsonNode(result, new Dictionary<int, JsonNode>(), refCounter);

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

        var nullableType = Nullable.GetUnderlyingType(t);
        if (nullableType != null)
        {
            if (parsed == null)
            {
                return null;
            }

            t = nullableType;
        }

        if (parsed == null)
        {
            if (!t.IsValueType)
            {
                return null!;
            }
            if (t.IsEnum)
            {
                return Enum.ToObject(t, 0);
            }
            return Convert.ChangeType(0, t, CultureInfo.InvariantCulture);
        }

        if (TryConvertJsonValue(parsed, t, out var converted))
        {
            return converted;
        }

        // Handle Regex: reconstruct from pattern and flags.
        if (t == typeof(Regex) && parsed is JsonObject regexObj)
        {
            var pattern = regexObj.TryGetPropertyValue("p", out var p) ? p?.ToString() : null;
            var flagsVal = regexObj.TryGetPropertyValue("f", out var f) ? (int)(f ?? 0) : 0;
            if (pattern != null)
            {
                return new Regex(pattern, (RegexOptions)flagsVal);
            }
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

    private static bool TryConvertJsonValue(JsonNode? node, Type type, out object? value)
    {
        value = null;
        if (node is not JsonValue jsonValue)
        {
            return false;
        }

        if (type == typeof(string))
        {
            if (jsonValue.TryGetValue(out string? stringValue))
            {
                value = stringValue;
                return true;
            }

            value = jsonValue.ToString();
            return true;
        }

        if (type == typeof(bool))
        {
            if (jsonValue.TryGetValue(out bool boolValue))
            {
                value = boolValue;
                return true;
            }

            return false;
        }

        if (type == typeof(int))
        {
            if (jsonValue.TryGetValue(out int intValue))
            {
                value = intValue;
                return true;
            }

            if (jsonValue.TryGetValue(out long longValue))
            {
                value = checked((int)longValue);
                return true;
            }

            if (jsonValue.TryGetValue(out double doubleValue))
            {
                value = checked((int)doubleValue);
                return true;
            }

            return false;
        }

        if (type == typeof(long))
        {
            if (jsonValue.TryGetValue(out long longValue))
            {
                value = longValue;
                return true;
            }

            if (jsonValue.TryGetValue(out int intValue))
            {
                value = (long)intValue;
                return true;
            }

            if (jsonValue.TryGetValue(out double doubleValue))
            {
                value = checked((long)doubleValue);
                return true;
            }

            return false;
        }

        if (type == typeof(double))
        {
            if (jsonValue.TryGetValue(out double doubleValue))
            {
                value = doubleValue;
                return true;
            }

            if (jsonValue.TryGetValue(out int intValue))
            {
                value = (double)intValue;
                return true;
            }

            if (jsonValue.TryGetValue(out long longValue))
            {
                value = (double)longValue;
                return true;
            }

            return false;
        }

        if (type == typeof(float))
        {
            if (jsonValue.TryGetValue(out float floatValue))
            {
                value = floatValue;
                return true;
            }

            if (jsonValue.TryGetValue(out double doubleValue))
            {
                value = checked((float)doubleValue);
                return true;
            }

            return false;
        }

        if (type == typeof(decimal))
        {
            if (jsonValue.TryGetValue(out decimal decimalValue))
            {
                value = decimalValue;
                return true;
            }

            if (jsonValue.TryGetValue(out double doubleValue))
            {
                value = (decimal)doubleValue;
                return true;
            }

            return false;
        }

        if (type == typeof(DateTime) && jsonValue.TryGetValue(out DateTime dateTimeValue))
        {
            value = dateTimeValue;
            return true;
        }

        if (type == typeof(Uri) && jsonValue.TryGetValue(out Uri? uriValue))
        {
            value = uriValue;
            return true;
        }

        if (type == typeof(Guid) && jsonValue.TryGetValue(out Guid guidValue))
        {
            value = guidValue;
            return true;
        }

        if (type == typeof(BigInteger) && jsonValue.TryGetValue(out string? bigIntString))
        {
            value = BigInteger.Parse(bigIntString, CultureInfo.InvariantCulture);
            return true;
        }

        return false;
    }

    private static JsonNode? ParseEvaluateResultToJsonNode(JsonElement result, Dictionary<int, JsonNode> refs, RefCounter? refCounter = null)
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
            // JsonNode instances cannot be attached in multiple places or form cycles.
            // Evaluate results are returned as JSON-compatible values, so repeated JS
            // references are materialized as cloned JSON subtrees.
            return refs[refValue.GetInt32()].DeepClone();
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
            int flags = 0;
            var flagsEl = regex.GetProperty("f");
            if (flagsEl.ValueKind == JsonValueKind.String)
            {
            var flagsStr = flagsEl.ToString();
            if (flagsStr.Contains('i'))
            {
                flags |= (int)RegexOptions.IgnoreCase;
            }
            if (flagsStr.Contains('m'))
            {
                flags |= (int)RegexOptions.Multiline;
            }
            if (flagsStr.Contains('s'))
            {
                flags |= (int)RegexOptions.Singleline;
            }
            }
            else if (flagsEl.ValueKind == JsonValueKind.Number)
            {
                flags = flagsEl.GetInt32();
            }
            return new JsonObject
            {
                ["p"] = JsonValue.Create(regex.GetProperty("p").ToString()),
                ["f"] = JsonValue.Create(flags),
            };
        }

        if (result.TryGetProperty("ta", out var ta))
        {
            var kind = ta.GetProperty("k").ToString();
            var bEl = ta.GetProperty("b");
            var bytes = bEl.ToObject<byte[]>();
            var array = new JsonArray();
            switch (kind)
            {
                case "Int8Array":
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        array.Add((JsonNode?)JsonValue.Create((int)(sbyte)bytes[i]));
                    }
                    break;
                case "Uint8Array" or "Uint8ClampedArray":
                    for (int i = 0; i < bytes.Length; i++)
                    {
                        array.Add((JsonNode?)JsonValue.Create((int)bytes[i]));
                    }
                    break;
                case "Int16Array":
                    for (int i = 0; i < bytes.Length - 1; i += 2)
                    {
                        array.Add((JsonNode?)JsonValue.Create((int)BitConverter.ToInt16(bytes, i)));
                    }
                    break;
                case "Uint16Array":
                    for (int i = 0; i < bytes.Length - 1; i += 2)
                    {
                        array.Add((JsonNode?)JsonValue.Create((int)BitConverter.ToUInt16(bytes, i)));
                    }
                    break;
                case "Int32Array":
                    for (int i = 0; i < bytes.Length - 3; i += 4)
                    {
                        array.Add((JsonNode?)JsonValue.Create(BitConverter.ToInt32(bytes, i)));
                    }
                    break;
                case "Uint32Array":
                    for (int i = 0; i < bytes.Length - 3; i += 4)
                    {
                        array.Add((JsonNode?)JsonValue.Create((long)BitConverter.ToUInt32(bytes, i)));
                    }
                    break;
                case "Float32Array":
                    for (int i = 0; i < bytes.Length - 3; i += 4)
                    {
                        array.Add((JsonNode?)JsonValue.Create(BitConverter.ToSingle(bytes, i)));
                    }
                    break;
                case "Float64Array":
                    for (int i = 0; i < bytes.Length - 7; i += 8)
                    {
                        array.Add((JsonNode?)JsonValue.Create(BitConverter.ToDouble(bytes, i)));
                    }
                    break;
                case "BigInt64Array":
                    for (int i = 0; i < bytes.Length - 7; i += 8)
                    {
                        array.Add((JsonNode?)JsonValue.Create(BitConverter.ToInt64(bytes, i)));
                    }
                    break;
                case "BigUint64Array":
                    for (int i = 0; i < bytes.Length - 7; i += 8)
                    {
                        array.Add((JsonNode?)JsonValue.Create(BitConverter.ToUInt64(bytes, i)));
                    }
                    break;
            }
            throw new InvalidOperationException($"ta returns {array.ToJsonString()}");
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
            int id = result.GetProperty("id").GetInt32();
            int objCounter = refCounter!.Next++;
            var jsonObj = new JsonObject
            {
                ["$id"] = JsonValue.Create(objCounter.ToString(CultureInfo.InvariantCulture)),
            };
            refs.Add(id, jsonObj);
            foreach (var kv in obj.ToObject<KeyJsonElementValueObject[]>())
            {
                jsonObj[kv.K] = ParseEvaluateResultToJsonNode(kv.V, refs, refCounter);
            }
            return jsonObj;
        }

        if (result.TryGetProperty("a", out var arrayVal))
        {
            var jsonArray = new JsonArray();
            refs.Add(result.GetProperty("id").GetInt32(), jsonArray);
            foreach (var item in arrayVal.EnumerateArray())
            {
                jsonArray.Add(ParseEvaluateResultToJsonNode(item, refs, refCounter));
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

    private sealed class RefCounter
    {
        internal int Next = 1;
    }
}
