using System.Globalization;
using Microsoft.Playwright.Helpers;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class EnumerableExtensionsTests
{
    [Test]
    public void ObjectKeyValuePairsToProtocolShouldUseInvariantFormatting()
    {
        var previousCulture = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var result = new[]
            {
                new KeyValuePair<string, object>("decimal", 1.5m),
                new KeyValuePair<string, object>("bool", true),
                new KeyValuePair<string, object>("null", null!),
            }.ToProtocol()!.ToArray();

            Assert.AreEqual("1.5", result[0].Value);
            Assert.AreEqual("true", result[1].Value);
            Assert.AreEqual(string.Empty, result[2].Value);
        }
        finally
        {
            CultureInfo.CurrentCulture = previousCulture;
        }
    }
}
