using System;
using Microsoft.Playwright.Transport.Converters;

namespace Microsoft.Playwright.Helpers;

internal static class EnumWireExtensions
{
    public static string ToValueString(this Enum value)
        => AotEnumMemberConverter.ToWireString(value);

    public static TEnum FromValueString<TEnum>(string value)
        where TEnum : struct, Enum
        => (TEnum)AotEnumMemberConverter.FromWireString(typeof(TEnum), value);
}
