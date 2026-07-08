using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Microsoft.Playwright.Transport.Converters;

internal sealed class AotEnumMemberConverter : JsonConverter<object>
{
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
            if (TryFromSpecialWireString(actualType, str, out var specialValue))
            {
                return specialValue;
            }

            if (Enum.TryParse(actualType, str, ignoreCase: true, out var parsed))
            {
                return parsed;
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
            writer.WriteStringValue(ToWireString(enumValue));
        }
        else
        {
            writer.WriteStringValue(value.ToString());
        }
    }

    internal static string ToWireString(Enum value)
    {
        if (TryToSpecialWireString(value, out var wireValue))
        {
            return wireValue;
        }
        return value.ToString().ToLowerInvariant();
    }

    internal static object FromWireString(Type enumType, string value)
    {
        if (TryFromSpecialWireString(enumType, value, out var specialValue))
        {
            return specialValue;
        }

        return Enum.TryParse(enumType, value, ignoreCase: true, out var result)
            ? result
            : throw new JsonException($"Unknown enum value '{value}' for '{enumType.Name}'.");
    }

    private static bool TryToSpecialWireString(Enum value, out string wireValue)
    {
        switch (value)
        {
            case AnnotatePosition.TopLeft: wireValue = "top-left"; return true;
            case AnnotatePosition.TopRight: wireValue = "top-right"; return true;
            case AnnotatePosition.BottomLeft: wireValue = "bottom-left"; return true;
            case AnnotatePosition.BottomRight: wireValue = "bottom-right"; return true;
            case ColorScheme.NoPreference: wireValue = "no-preference"; return true;
            case ConsoleMessagesFilter.SinceNavigation: wireValue = "since-navigation"; return true;
            case Contrast.NoPreference: wireValue = "no-preference"; return true;
            case KeyboardModifier.Alt: wireValue = "Alt"; return true;
            case KeyboardModifier.Control: wireValue = "Control"; return true;
            case KeyboardModifier.ControlOrMeta: wireValue = "ControlOrMeta"; return true;
            case KeyboardModifier.Meta: wireValue = "Meta"; return true;
            case KeyboardModifier.Shift: wireValue = "Shift"; return true;
            case ReducedMotion.NoPreference: wireValue = "no-preference"; return true;
            case SameSiteAttribute.Strict: wireValue = "Strict"; return true;
            case SameSiteAttribute.Lax: wireValue = "Lax"; return true;
            case SameSiteAttribute.None: wireValue = "None"; return true;
            case UnrouteBehavior.IgnoreErrors: wireValue = "ignoreErrors"; return true;
            default:
                wireValue = string.Empty;
                return false;
        }
    }

    private static bool TryFromSpecialWireString(Type enumType, string value, out object enumValue)
    {
        if (enumType == typeof(AnnotatePosition))
        {
            enumValue = value switch
            {
                "top-left" => AnnotatePosition.TopLeft,
                "top-right" => AnnotatePosition.TopRight,
                "bottom-left" => AnnotatePosition.BottomLeft,
                "bottom-right" => AnnotatePosition.BottomRight,
                _ => default(AnnotatePosition),
            };
            return value is "top-left" or "top-right" or "bottom-left" or "bottom-right";
        }
        if (enumType == typeof(ColorScheme) && string.Equals(value, "no-preference", StringComparison.OrdinalIgnoreCase))
        {
            enumValue = ColorScheme.NoPreference;
            return true;
        }
        if (enumType == typeof(ConsoleMessagesFilter) && string.Equals(value, "since-navigation", StringComparison.OrdinalIgnoreCase))
        {
            enumValue = ConsoleMessagesFilter.SinceNavigation;
            return true;
        }
        if (enumType == typeof(Contrast) && string.Equals(value, "no-preference", StringComparison.OrdinalIgnoreCase))
        {
            enumValue = Contrast.NoPreference;
            return true;
        }
        if (enumType == typeof(KeyboardModifier))
        {
            enumValue = value switch
            {
                "Alt" => KeyboardModifier.Alt,
                "Control" => KeyboardModifier.Control,
                "ControlOrMeta" => KeyboardModifier.ControlOrMeta,
                "Meta" => KeyboardModifier.Meta,
                "Shift" => KeyboardModifier.Shift,
                _ => default(KeyboardModifier),
            };
            return value is "Alt" or "Control" or "ControlOrMeta" or "Meta" or "Shift";
        }
        if (enumType == typeof(ReducedMotion) && string.Equals(value, "no-preference", StringComparison.OrdinalIgnoreCase))
        {
            enumValue = ReducedMotion.NoPreference;
            return true;
        }
        if (enumType == typeof(SameSiteAttribute))
        {
            enumValue = value switch
            {
                "Strict" => SameSiteAttribute.Strict,
                "Lax" => SameSiteAttribute.Lax,
                "None" => SameSiteAttribute.None,
                _ => default(SameSiteAttribute),
            };
            return value is "Strict" or "Lax" or "None";
        }
        if (enumType == typeof(UnrouteBehavior) && string.Equals(value, "ignoreErrors", StringComparison.Ordinal))
        {
            enumValue = UnrouteBehavior.IgnoreErrors;
            return true;
        }

        enumValue = default!;
        return false;
    }
}
