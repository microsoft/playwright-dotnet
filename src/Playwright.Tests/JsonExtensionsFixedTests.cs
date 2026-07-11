using System.Text.Json;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class JsonExtensionsFixedTests
{
    [Test]
    public void GetNewDefaultSerializerOptionsShouldAlwaysSetTypeInfoResolver()
    {
        var keepNulls = JsonExtensions.GetNewDefaultSerializerOptions(keepNulls: true);
        var skipNulls = JsonExtensions.GetNewDefaultSerializerOptions(keepNulls: false);

        Assert.NotNull(keepNulls.TypeInfoResolver);
        Assert.NotNull(skipNulls.TypeInfoResolver);
    }

    [Test]
    public void GetNewDefaultSerializerOptionsShouldRespectKeepNulls()
    {
        var keepNulls = JsonExtensions.GetNewDefaultSerializerOptions(keepNulls: true);
        var skipNulls = JsonExtensions.GetNewDefaultSerializerOptions(keepNulls: false);

        // When keepNulls is true, the DefaultIgnoreCondition should not be set (stays at default).
        Assert.AreEqual(System.Text.Json.Serialization.JsonIgnoreCondition.Never, keepNulls.DefaultIgnoreCondition);
        // When keepNulls is false, null values should be excluded from serialization.
        Assert.AreEqual(System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull, skipNulls.DefaultIgnoreCondition);
    }

    [Test]
    public void ToJsonShouldUseSourceGenContext()
    {
        // Verify that ToJson<T> uses the PlaywrightJsonContext source-gen metadata
        var result = new NameValue { Name = "Content-Type", Value = "text/html" }.ToJson();

        using var doc = JsonDocument.Parse(result);
        Assert.AreEqual("Content-Type", doc.RootElement.GetProperty("name").GetString());
        Assert.AreEqual("text/html", doc.RootElement.GetProperty("value").GetString());
    }

    [Test]
    public void ToObjectFromElementShouldThrowForUnregisteredType()
    {
        using var doc = JsonDocument.Parse("""{"value":1}""");
        var ex = Assert.Throws<InvalidOperationException>(() =>
            doc.RootElement.ToObject(typeof(UnregisteredType)));

        Assert.That(ex!.Message, Does.Contain("not registered in PlaywrightJsonContext"));
    }

    private class UnregisteredType
    {
        public int Value { get; set; }
    }
}
