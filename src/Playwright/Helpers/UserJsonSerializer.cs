// MIT License

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization.Metadata;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright.Helpers;

internal static class UserJsonSerializer
{
    internal static T? Deserialize<T>(byte[] json, JsonSerializerOptions? options, string methodName)
        => JsonSerializer.Deserialize(json, ResolveDeserializeTypeInfo<T>(options, methodName));

    internal static string Serialize(object? value)
    {
        if (value == null)
        {
            return "null";
        }

        if (value is JsonElement je)
        {
            return je.GetRawText();
        }

        if (value is JsonNode jn)
        {
            return jn.ToJsonString();
        }

        return ToJsonNode(value, new SerializationState())?.ToJsonString() ?? "null";
    }

    private static JsonNode? ToJsonNode(object? value, SerializationState state)
    {
        if (value == null)
        {
            return null;
        }

        if (value is JsonElement jsonElement)
        {
            return JsonNode.Parse(jsonElement.GetRawText());
        }

        if (value is JsonNode jsonNode)
        {
            return jsonNode.DeepClone();
        }

        if (value is JsonDocument jsonDocument)
        {
            return JsonNode.Parse(jsonDocument.RootElement.GetRawText());
        }

        var type = value.GetType();

        if (value is string stringValue)
        {
            return JsonValue.Create(stringValue);
        }

        if (value is bool boolValue)
        {
            return JsonValue.Create(boolValue);
        }

        if (value is int intValue)
        {
            return JsonValue.Create(intValue);
        }

        if (value is long longValue)
        {
            return JsonValue.Create(longValue);
        }

        if (value is double doubleValue)
        {
            return JsonValue.Create(doubleValue);
        }

        if (value is decimal decimalValue)
        {
            return JsonValue.Create(decimalValue);
        }

        if (value is float floatValue)
        {
            return JsonValue.Create(floatValue);
        }

        if (value is short shortValue)
        {
            return JsonValue.Create((int)shortValue);
        }

        if (value is ushort ushortValue)
        {
            return JsonValue.Create((int)ushortValue);
        }

        if (value is byte byteValue)
        {
            return JsonValue.Create((int)byteValue);
        }

        if (value is sbyte sbyteValue)
        {
            return JsonValue.Create((int)sbyteValue);
        }

        if (value is uint uintValue)
        {
            return JsonValue.Create(uintValue);
        }

        if (value is ulong ulongValue)
        {
            return JsonValue.Create(ulongValue);
        }

        if (value is char charValue)
        {
            return JsonValue.Create(charValue.ToString());
        }

        if (value is byte[] bytes)
        {
            return JsonValue.Create(Convert.ToBase64String(bytes));
        }

        if (value is IDictionary rawDict)
        {
            state.Enter(value, type);
            try
            {
                var obj = new JsonObject();
                foreach (DictionaryEntry entry in rawDict)
                {
                    if (entry.Key is not string key)
                    {
                        throw new PlaywrightException(
                            $"Dictionary type '{type.FullName}' contains a non-string key. " +
                            "JSON request and response bodies require Dictionary<string, object?> or another dictionary with string keys.");
                    }

                    obj[key] = ToJsonNode(entry.Value, state);
                }

                return obj;
            }
            finally
            {
                state.Leave(value);
            }
        }

        if (value is IEnumerable<KeyValuePair<string, object?>> objectPairs)
        {
            state.Enter(value, type);
            try
            {
                var obj = new JsonObject();
                foreach (var pair in objectPairs)
                {
                    obj[pair.Key] = ToJsonNode(pair.Value, state);
                }

                return obj;
            }
            finally
            {
                state.Leave(value);
            }
        }

        if (value is IList list)
        {
            state.Enter(value, type);
            try
            {
                var items = new JsonArray();
                foreach (var item in list)
                {
                    items.Add(ToJsonNode(item, state));
                }

                return items;
            }
            finally
            {
                state.Leave(value);
            }
        }

        if (value is IEnumerable enumerable)
        {
            state.Enter(value, type);
            try
            {
                var items = new JsonArray();
                foreach (var item in enumerable)
                {
                    items.Add(ToJsonNode(item, state));
                }

                return items;
            }
            finally
            {
                state.Leave(value);
            }
        }

        var typeInfo = PlaywrightJsonContext.Default.GetTypeInfo(type)
            ?? EvaluateArgumentValueConverter.GetExtraTypeInfo(type);
        if (typeInfo != null)
        {
            return JsonSerializer.SerializeToNode(value, typeInfo);
        }

        throw new PlaywrightException(
            $"Type '{type.FullName}' is not registered for AOT-safe serialization. " +
            "Use primitives, JsonElement, Dictionary<string, object?>, arrays, or register your type in PlaywrightJsonContext.");
    }

    private static JsonTypeInfo<T> ResolveDeserializeTypeInfo<T>(JsonSerializerOptions? options, string methodName)
    {
        if (options == null)
        {
            var builtInTypeInfo = PlaywrightJsonContext.Default.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>;
            if (builtInTypeInfo != null)
            {
                return builtInTypeInfo;
            }

            throw new PlaywrightException(
                $"{methodName}<T>() requires source-generated JSON metadata. " +
                "Pass the JsonTypeInfo<T> overload or set JsonSerializerOptions.TypeInfoResolver to your JsonSerializerContext.");
        }

        if (options.TypeInfoResolver == null)
        {
            throw new PlaywrightException(
                $"{methodName}<T>() requires source-generated JSON metadata. " +
                "Set JsonSerializerOptions.TypeInfoResolver to your JsonSerializerContext or pass the JsonTypeInfo<T> overload.");
        }

        try
        {
            return options.GetTypeInfo(typeof(T)) as JsonTypeInfo<T>
                ?? throw new PlaywrightException(
                    $"{methodName}<T>() could not resolve JSON metadata for '{typeof(T).FullName}'. " +
                    "Add the type to a JsonSerializerContext and pass its JsonTypeInfo<T> overload or set JsonSerializerOptions.TypeInfoResolver.");
        }
        catch (Exception ex) when (ex is NotSupportedException or InvalidOperationException)
        {
            throw new PlaywrightException(
                $"{methodName}<T>() requires source-generated JSON metadata. " +
                "Pass the JsonTypeInfo<T> overload or set JsonSerializerOptions.TypeInfoResolver to your JsonSerializerContext.",
                ex);
        }
    }

    private sealed class SerializationState
    {
        private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);

        internal void Enter(object value, Type type)
        {
            if (!_active.Add(value))
            {
                throw new PlaywrightException(
                    $"Type '{type.FullName}' contains a cycle and cannot be serialized as an AOT-safe JSON request or response body.");
            }
        }

        internal void Leave(object value) => _active.Remove(value);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        internal static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
    }
}
