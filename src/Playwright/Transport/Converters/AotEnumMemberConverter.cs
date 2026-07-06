using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Converters;

internal sealed class AotEnumMemberConverter : JsonConverter<object>
{
    private static readonly ConcurrentDictionary<Type, Dictionary<string, object>> FromWireCache = new();
    private static readonly ConcurrentDictionary<Type, Dictionary<object, string>> ToWireCache = new();

    public override bool CanConvert(Type typeToConvert)
    {
        var actualType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;
        return actualType.IsEnum;
    }

    public override object? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var actualType = Nullable.GetUnderlyingType(typeToConvert) ?? typeToConvert;

        if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }

        if (reader.TokenType == JsonTokenType.String)
        {
            var str = reader.GetString()!;
            var map = GetFromWire(actualType);
            if (map.TryGetValue(str, out var value))
            {
                return value;
            }
            throw new JsonException($"Unknown enum value '{str}' for '{actualType.Name}'.");
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return Enum.ToObject(actualType, reader.GetInt32());
        }

        throw new JsonException($"Unexpected token '{reader.TokenType}' for enum '{actualType.Name}'.");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is Enum enumValue)
        {
            var wireValue = GetToWire(enumValue.GetType()).GetValueOrDefault(enumValue) ?? Enum.GetName(enumValue.GetType(), enumValue);
            writer.WriteStringValue(wireValue);
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    private static Dictionary<string, object> BuildFromWire(Type enumType)
    {
        var map = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in Enum.GetNames(enumType))
        {
            map[name] = Enum.Parse(enumType, name);
        }
        return map;
    }

    private static Dictionary<string, object> GetFromWire(Type t)
        => FromWireCache.GetOrAdd(t, _ => BuildFromWire(t));

    private static Dictionary<object, string> GetToWire(Type t)
        => ToWireCache.GetOrAdd(t, _ => BuildToWire(t));

    private static Dictionary<object, string> BuildToWire(Type enumType)
    {
        var map = new Dictionary<object, string>();
        foreach (var name in Enum.GetNames(enumType))
        {
            var value = Enum.Parse(enumType, name);
            map[value] = name.ToLowerInvariant();
        }
        return map;
    }

    internal static string ToWireString(Enum value)
    {
        var map = GetToWire(value.GetType());
        if (map.TryGetValue(value, out var wireValue))
        {
            return wireValue;
        }
        return Enum.GetName(value.GetType(), value) ?? value.ToString()!;
    }

    internal static object FromWireString(Type enumType, string value)
    {
        var map = GetFromWire(enumType);
        return map.TryGetValue(value, out var result) ? result : throw new JsonException($"Unknown enum value '{value}' for '{enumType.Name}'.");
    }
}
