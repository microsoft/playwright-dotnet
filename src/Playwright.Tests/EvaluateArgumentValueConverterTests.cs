using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Transport.Converters;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class EvaluateArgumentValueConverterTests
{
    [Test]
    public void RegisterTypeInfoShouldAcceptSourceGeneratedJsonTypeInfo()
    {
        EvaluateArgumentValueConverter.RegisterTypeInfo(EvaluateConverterJsonContext.Default.ConverterPayload);

        Assert.NotNull(EvaluateArgumentValueConverter.GetExtraTypeInfo(typeof(ConverterPayload)));
    }

    [Test]
    public void SerializeShouldTreatEmptyDictionaryAsObject()
    {
        var serialized = EvaluateArgumentValueConverter.Serialize(
            new Dictionary<string, object?>(),
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.True(serialized.TryGetPropertyValue("o", out var objectEntries));
        Assert.AreEqual(0, objectEntries!.AsArray().Count);
        Assert.False(serialized.ContainsKey("a"));
    }

    [Test]
    public void SerializeShouldRejectDictionaryWithNonStringKeys()
    {
        var input = new Dictionary<object, object?>
        {
            ["ok"] = true,
            [1] = "bad",
        };

        var exception = Assert.Throws<PlaywrightException>(() => EvaluateArgumentValueConverter.Serialize(
            input,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo()));

        StringAssert.Contains("non-string key", exception!.Message);
    }

    [Test]
    public void SerializeShouldUseRegisteredTypeInfoForDtoProperties()
    {
        EvaluateArgumentValueConverter.RegisterTypeInfo(EvaluateConverterJsonContext.Default.ConverterPayload);

        var serialized = EvaluateArgumentValueConverter.Serialize(
            new ConverterPayload("hello", 7, new[] { "a", "b" }, new ConverterChild("nested")),
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        var entries = serialized["o"]!.AsArray();
        Assert.AreEqual("hello", Property(entries, "message")!["s"]!.GetValue<string>());
        Assert.AreEqual(7L, Property(entries, "count")!["n"]!.GetValue<long>());
        Assert.AreEqual(2, Property(entries, "tags")!["a"]!.AsArray().Count);
        Assert.AreEqual("nested", Property(Property(entries, "child")!["o"]!.AsArray(), "name")!["s"]!.GetValue<string>());
    }

    [Test]
    public void SerializeShouldSupportNonIntEnums()
    {
        var signed = EvaluateArgumentValueConverter.Serialize(
            SignedEnum.Large,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());
        var unsigned = EvaluateArgumentValueConverter.Serialize(
            UnsignedEnum.Large,
            new List<EvaluateArgumentGuidElement>(),
            new EvaluateArgumentValueConverter.VisitorInfo());

        Assert.AreEqual(2147483648L, signed["n"]!.GetValue<long>());
        Assert.AreEqual(4294967295L, unsigned["n"]!.GetValue<long>());
    }

    [Test]
    public void DeserializeShouldSupportPrimitiveNumericValueTypes()
    {
        Assert.AreEqual((byte)255, Deserialize("""{"n":255}""", typeof(byte)));
        Assert.AreEqual((sbyte)-5, Deserialize("""{"n":-5}""", typeof(sbyte)));
        Assert.AreEqual((short)-32768, Deserialize("""{"n":-32768}""", typeof(short)));
        Assert.AreEqual((ushort)65535, Deserialize("""{"n":65535}""", typeof(ushort)));
        Assert.AreEqual(4294967295U, Deserialize("""{"n":4294967295}""", typeof(uint)));
        Assert.AreEqual(4294967295UL, Deserialize("""{"n":4294967295}""", typeof(ulong)));
        Assert.AreEqual('A', Deserialize("""{"s":"A"}""", typeof(char)));
        Assert.AreEqual('B', Deserialize("""{"n":66}""", typeof(char)));
    }

    private static System.Text.Json.Nodes.JsonObject? Property(System.Text.Json.Nodes.JsonArray entries, string name)
    {
        foreach (var entry in entries)
        {
            var obj = entry!.AsObject();
            if (obj["k"]!.GetValue<string>() == name)
            {
                return obj["v"]!.AsObject();
            }
        }

        return null;
    }

    private static object? Deserialize(string json, Type type)
    {
        using var document = JsonDocument.Parse(json);
        return EvaluateArgumentValueConverter.Deserialize(document.RootElement, type);
    }

    public record ConverterPayload(string Message, int Count, string[] Tags, ConverterChild Child);

    public record ConverterChild(string Name);

    public enum SignedEnum : long
    {
        Large = 2147483648L,
    }

    public enum UnsignedEnum : uint
    {
        Large = uint.MaxValue,
    }
}

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(EvaluateArgumentValueConverterTests.ConverterPayload))]
[JsonSerializable(typeof(EvaluateArgumentValueConverterTests.ConverterChild))]
internal partial class EvaluateConverterJsonContext : JsonSerializerContext
{
}
