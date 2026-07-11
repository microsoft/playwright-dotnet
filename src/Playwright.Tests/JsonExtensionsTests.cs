using System.Text.Json;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Protocol;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class JsonExtensionsTests
{
    [Test]
    public void DefaultOptionsShouldUseSourceGeneratedResolver()
    {
        Assert.NotNull(JsonExtensions.DefaultJsonSerializerOptions.TypeInfoResolver);
    }

    [Test]
    public void JsonDocumentToObjectShouldUseSourceGeneratedMetadata()
    {
        using var document = JsonDocument.Parse("""{"name":"accept","value":"text/html"}""");

        var result = document.ToObject<NameValue>();

        Assert.AreEqual("accept", result.Name);
        Assert.AreEqual("text/html", result.Value);
    }
}
