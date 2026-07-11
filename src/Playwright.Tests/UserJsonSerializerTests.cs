using System.Text.Json;
using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class UserJsonSerializerTests
{
    [Test]
    public void SerializeShouldHandleNestedCollectionsWithoutObjectReflection()
    {
        var json = UserJsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["items"] = new object?[]
            {
                1,
                "two",
                true,
                null,
                new Dictionary<string, object?> { ["nested"] = "ok" },
            },
        });

        using var document = JsonDocument.Parse(json);
        var items = document.RootElement.GetProperty("items");
        Assert.AreEqual(1, items[0].GetInt32());
        Assert.AreEqual("two", items[1].GetString());
        Assert.True(items[2].GetBoolean());
        Assert.AreEqual(JsonValueKind.Null, items[3].ValueKind);
        Assert.AreEqual("ok", items[4].GetProperty("nested").GetString());
    }

    [Test]
    public void SerializeShouldPreserveByteArrayAsBase64String()
    {
        Assert.AreEqual("\"AQID\"", UserJsonSerializer.Serialize(new byte[] { 1, 2, 3 }));
    }

    [Test]
    public void SerializeShouldHandleNumericPrimitiveVariantsWithoutMetadataFallback()
    {
        var json = UserJsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["sbyte"] = (sbyte)-2,
            ["ushort"] = (ushort)3,
            ["uint"] = 4u,
            ["ulong"] = 5ul,
            ["float"] = 1.5f,
            ["char"] = 'x',
        });

        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(-2, document.RootElement.GetProperty("sbyte").GetInt32());
        Assert.AreEqual(3, document.RootElement.GetProperty("ushort").GetInt32());
        Assert.AreEqual(4u, document.RootElement.GetProperty("uint").GetUInt32());
        Assert.AreEqual(5ul, document.RootElement.GetProperty("ulong").GetUInt64());
        Assert.AreEqual(1.5d, document.RootElement.GetProperty("float").GetDouble());
        Assert.AreEqual("x", document.RootElement.GetProperty("char").GetString());
    }

    [Test]
    public void SerializeShouldRejectNonStringDictionaryKeys()
    {
        var input = new Dictionary<object, object?>
        {
            ["ok"] = true,
            [1] = "bad",
        };

        var exception = Assert.Throws<PlaywrightException>(() => UserJsonSerializer.Serialize(input));

        StringAssert.Contains("non-string key", exception!.Message);
    }

    [Test]
    public void SerializeShouldRejectCyclicCollections()
    {
        var dictionary = new Dictionary<string, object?>();
        dictionary["self"] = dictionary;

        var list = new List<object?>();
        list.Add(list);

        var dictionaryException = Assert.Throws<PlaywrightException>(() => UserJsonSerializer.Serialize(dictionary));
        var listException = Assert.Throws<PlaywrightException>(() => UserJsonSerializer.Serialize(list));

        StringAssert.Contains("contains a cycle", dictionaryException!.Message);
        StringAssert.Contains("contains a cycle", listException!.Message);
    }

    [Test]
    public void SerializeShouldAllowSharedNonCyclicReferences()
    {
        var shared = new Dictionary<string, object?> { ["value"] = 1 };
        var json = UserJsonSerializer.Serialize(new object?[] { shared, shared });

        using var document = JsonDocument.Parse(json);
        Assert.AreEqual(1, document.RootElement[0].GetProperty("value").GetInt32());
        Assert.AreEqual(1, document.RootElement[1].GetProperty("value").GetInt32());
    }

    [Test]
    public void SerializeShouldTreatObjectKeyValuePairsAsJsonObject()
    {
        var json = UserJsonSerializer.Serialize(new List<KeyValuePair<string, object?>>
        {
            new("enabled", true),
            new("count", 2),
            new("nested", new List<KeyValuePair<string, object?>>
            {
                new("name", "value"),
            }),
        });

        using var document = JsonDocument.Parse(json);
        Assert.True(document.RootElement.GetProperty("enabled").GetBoolean());
        Assert.AreEqual(2, document.RootElement.GetProperty("count").GetInt32());
        Assert.AreEqual("value", document.RootElement.GetProperty("nested").GetProperty("name").GetString());
    }
}
