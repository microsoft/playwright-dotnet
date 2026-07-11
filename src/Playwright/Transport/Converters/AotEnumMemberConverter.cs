using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Transport.Converters;

internal sealed class AotEnumMemberConverter : JsonConverter<object>
{
    private static readonly EnumMapping[] _specialMappings = new[]
    {
        new EnumMapping(typeof(AnnotatePosition), "top-left", AnnotatePosition.TopLeft),
        new EnumMapping(typeof(AnnotatePosition), "top-right", AnnotatePosition.TopRight),
        new EnumMapping(typeof(AnnotatePosition), "bottom-left", AnnotatePosition.BottomLeft),
        new EnumMapping(typeof(AnnotatePosition), "bottom-right", AnnotatePosition.BottomRight),
        new EnumMapping(typeof(ColorScheme), "no-preference", ColorScheme.NoPreference),
        new EnumMapping(typeof(ConsoleMessagesFilter), "since-navigation", ConsoleMessagesFilter.SinceNavigation),
        new EnumMapping(typeof(Contrast), "no-preference", Contrast.NoPreference),
        new EnumMapping(typeof(KeyboardModifier), "Alt", KeyboardModifier.Alt),
        new EnumMapping(typeof(KeyboardModifier), "Control", KeyboardModifier.Control),
        new EnumMapping(typeof(KeyboardModifier), "ControlOrMeta", KeyboardModifier.ControlOrMeta),
        new EnumMapping(typeof(KeyboardModifier), "Meta", KeyboardModifier.Meta),
        new EnumMapping(typeof(KeyboardModifier), "Shift", KeyboardModifier.Shift),
        new EnumMapping(typeof(ReducedMotion), "no-preference", ReducedMotion.NoPreference),
        new EnumMapping(typeof(SameSiteAttribute), "Strict", SameSiteAttribute.Strict),
        new EnumMapping(typeof(SameSiteAttribute), "Lax", SameSiteAttribute.Lax),
        new EnumMapping(typeof(SameSiteAttribute), "None", SameSiteAttribute.None),
        new EnumMapping(typeof(UnrouteBehavior), "wait", UnrouteBehavior.Wait),
        new EnumMapping(typeof(UnrouteBehavior), "ignoreErrors", UnrouteBehavior.IgnoreErrors),
        new EnumMapping(typeof(UnrouteBehavior), "default", UnrouteBehavior.Default),
        new EnumMapping(typeof(ChannelOwnerType), "bindingCall", ChannelOwnerType.BindingCall),
        new EnumMapping(typeof(ChannelOwnerType), "browserType", ChannelOwnerType.BrowserType),
        new EnumMapping(typeof(ChannelOwnerType), "browserContext", ChannelOwnerType.BrowserContext),
        new EnumMapping(typeof(ChannelOwnerType), "Debugger", ChannelOwnerType.Debugger),
        new EnumMapping(typeof(ChannelOwnerType), "Disposable", ChannelOwnerType.Disposable),
        new EnumMapping(typeof(ChannelOwnerType), "elementHandle", ChannelOwnerType.ElementHandle),
        new EnumMapping(typeof(ChannelOwnerType), "jsHandle", ChannelOwnerType.JSHandle),
        new EnumMapping(typeof(ChannelOwnerType), "JsonPipe", ChannelOwnerType.JsonPipe),
        new EnumMapping(typeof(ChannelOwnerType), "LocalUtils", ChannelOwnerType.LocalUtils),
        new EnumMapping(typeof(ChannelOwnerType), "browserServer", ChannelOwnerType.BrowserServer),
        new EnumMapping(typeof(ChannelOwnerType), "SocksSupport", ChannelOwnerType.SocksSupport),
        new EnumMapping(typeof(ChannelOwnerType), "WebSocket", ChannelOwnerType.WebSocket),
        new EnumMapping(typeof(ChannelOwnerType), "Android", ChannelOwnerType.Android),
        new EnumMapping(typeof(ChannelOwnerType), "WritableStream", ChannelOwnerType.WritableStream),
        new EnumMapping(typeof(ChannelOwnerType), "fetchRequest", ChannelOwnerType.FetchRequest),
        new EnumMapping(typeof(ChannelOwnerType), "APIRequestContext", ChannelOwnerType.APIRequestContext),
        new EnumMapping(typeof(ChannelOwnerType), "WebSocketroute", ChannelOwnerType.WebSocketRoute),
    };

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
            return FromWireString(actualType, str);
        }

        if (reader.TokenType == JsonTokenType.Number)
        {
            return ReadNumber(actualType, ref reader);
        }

        throw new JsonException($"Unexpected token '{reader.TokenType}' for enum '{actualType.Name}'.");
    }

    public override void Write(Utf8JsonWriter writer, object value, JsonSerializerOptions options)
    {
        if (value is Enum enumValue)
        {
            writer.WriteStringValue(ToWireString(enumValue));
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    internal static string ToWireString(Enum value)
    {
        foreach (var m in _specialMappings)
        {
            if (m.EnumType == value.GetType() && Equals(m.EnumValue, value))
            {
                return m.WireName;
            }
        }
        return value.ToString().ToLowerInvariant();
    }

    internal static object FromWireString(Type enumType, string value)
    {
        foreach (var m in _specialMappings)
        {
            if (m.EnumType == enumType && string.Equals(m.WireName, value, StringComparison.OrdinalIgnoreCase))
            {
                return m.EnumValue;
            }
        }

        if (Enum.TryParse(enumType, value, ignoreCase: true, out var result))
        {
            return result;
        }
        throw new JsonException($"Unknown enum value '{value}' for '{enumType.Name}'.");
    }

    private static object ReadNumber(Type enumType, ref Utf8JsonReader reader)
    {
        // Avoid AOT-unsafe Enum.GetUnderlyingType by trying Int64 first
        // (covers sbyte, short, int, long, byte, ushort, uint).
        if (reader.TryGetInt64(out var longValue))
        {
            return Enum.ToObject(enumType, longValue);
        }
        // Fall back to UInt64 for ulong-backed values exceeding long.MaxValue.
        if (reader.TryGetUInt64(out var ulongValue))
        {
            return Enum.ToObject(enumType, ulongValue);
        }

        throw new JsonException($"Unsupported numeric value for enum '{enumType.Name}'.");
    }

    private readonly struct EnumMapping
    {
        internal EnumMapping(Type enumType, string wireName, Enum enumValue)
        {
            EnumType = enumType;
            WireName = wireName;
            EnumValue = enumValue;
        }

        internal Type EnumType { get; }

        internal string WireName { get; }

        internal Enum EnumValue { get; }
    }
}
