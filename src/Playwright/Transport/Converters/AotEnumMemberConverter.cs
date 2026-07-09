using System;
using System.Text.Json;
using System.Text.Json.Serialization;

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
        new EnumMapping(typeof(UnrouteBehavior), "ignoreErrors", UnrouteBehavior.IgnoreErrors),
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
            return Enum.ToObject(actualType, reader.GetInt32());
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
