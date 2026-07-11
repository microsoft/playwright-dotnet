using System.Text;
using System.Text.Json;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Converters;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class AotEnumMemberConverterTests
{
    [Test]
    public void ToWireStringShouldPreserveKnownEnumMemberNames()
    {
        Assert.AreEqual("bindingCall", AotEnumMemberConverter.ToWireString(ChannelOwnerType.BindingCall));
        Assert.AreEqual("APIRequestContext", AotEnumMemberConverter.ToWireString(ChannelOwnerType.APIRequestContext));
        Assert.AreEqual("WebSocketroute", AotEnumMemberConverter.ToWireString(ChannelOwnerType.WebSocketRoute));
        Assert.AreEqual("ignoreErrors", AotEnumMemberConverter.ToWireString(UnrouteBehavior.IgnoreErrors));
        Assert.AreEqual("no-preference", AotEnumMemberConverter.ToWireString(ColorScheme.NoPreference));
        Assert.AreEqual("ControlOrMeta", AotEnumMemberConverter.ToWireString(KeyboardModifier.ControlOrMeta));
    }

    [Test]
    public void FromWireStringShouldReadKnownEnumMemberNames()
    {
        Assert.AreEqual(ChannelOwnerType.BindingCall, AotEnumMemberConverter.FromWireString(typeof(ChannelOwnerType), "bindingCall"));
        Assert.AreEqual(ChannelOwnerType.APIRequestContext, AotEnumMemberConverter.FromWireString(typeof(ChannelOwnerType), "APIRequestContext"));
        Assert.AreEqual(UnrouteBehavior.IgnoreErrors, AotEnumMemberConverter.FromWireString(typeof(UnrouteBehavior), "ignoreErrors"));
        Assert.AreEqual(ColorScheme.NoPreference, AotEnumMemberConverter.FromWireString(typeof(ColorScheme), "no-preference"));
    }

    [Test]
    public void ReadShouldRespectEnumBackingType()
    {
        Assert.AreEqual(LongBackedEnum.Large, Read("2147483648", typeof(LongBackedEnum)));
        Assert.AreEqual(UnsignedBackedEnum.Large, Read("4294967295", typeof(UnsignedBackedEnum)));
    }

    [Test]
    public void ReadNumberShouldHandleAllUnderlyingTypes()
    {
        Assert.AreEqual(ByteBackedEnum.Max, ReadNumberJson("255", typeof(ByteBackedEnum)));
        Assert.AreEqual(SByteBackedEnum.Min, ReadNumberJson("-128", typeof(SByteBackedEnum)));
        Assert.AreEqual(ShortBackedEnum.Large, ReadNumberJson("32767", typeof(ShortBackedEnum)));
        Assert.AreEqual(UShortBackedEnum.Max, ReadNumberJson("65535", typeof(UShortBackedEnum)));
        Assert.AreEqual(IntBackedEnum.Value, ReadNumberJson("100000", typeof(IntBackedEnum)));
        Assert.AreEqual(UIntBackedEnum.Large, ReadNumberJson("4294967295", typeof(UIntBackedEnum)));
        Assert.AreEqual(LongBackedEnum.Large, ReadNumberJson("2147483648", typeof(LongBackedEnum)));
        Assert.AreEqual(ULongBackedEnum.Large, ReadNumberJson("18446744073709551615", typeof(ULongBackedEnum)));
    }

    [Test]
    public void ReadNumberShouldThrowForUnsupportedFloatValue()
    {
        Assert.Throws<JsonException>(() => ReadNumberJson("1.5", typeof(IntBackedEnum)));
    }

    private static object? Read(string json, Type type)
    {
        var reader = new Utf8JsonReader(Encoding.UTF8.GetBytes(json));
        Assert.True(reader.Read());
        return new AotEnumMemberConverter().Read(ref reader, type, new JsonSerializerOptions());
    }

    private static object? ReadNumberJson(string number, Type type)
    {
        var json = Encoding.UTF8.GetBytes(number);
        var reader = new Utf8JsonReader(json);
        Assert.True(reader.Read());
        Assert.AreEqual(JsonTokenType.Number, reader.TokenType);
        return new AotEnumMemberConverter().Read(ref reader, type, new JsonSerializerOptions());
    }

    public enum LongBackedEnum : long
    {
        Large = 2147483648L,
    }

    public enum UnsignedBackedEnum : uint
    {
        Large = uint.MaxValue,
    }

    public enum ByteBackedEnum : byte
    {
        Max = 255,
    }

    public enum SByteBackedEnum : sbyte
    {
        Min = -128,
    }

    public enum ShortBackedEnum : short
    {
        Large = 32767,
    }

    public enum UShortBackedEnum : ushort
    {
        Max = 65535,
    }

    public enum IntBackedEnum : int
    {
        Value = 100000,
    }

    public enum UIntBackedEnum : uint
    {
        Large = uint.MaxValue,
    }

    public enum ULongBackedEnum : ulong
    {
        Large = ulong.MaxValue,
    }
}
