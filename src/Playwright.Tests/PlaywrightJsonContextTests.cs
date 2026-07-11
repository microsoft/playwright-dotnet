using Microsoft.Playwright.Transport;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests;

public class PlaywrightJsonContextTests
{
    [Test]
    public void PublicOptionTypesShouldBeRegisteredForSourceGeneration()
    {
        var missingTypes = typeof(IBrowser).Assembly
            .GetTypes()
            .Where(type =>
                type.IsPublic &&
                !type.IsGenericType &&
                type.Namespace == "Microsoft.Playwright" &&
                type.Name.EndsWith("Options", StringComparison.Ordinal) &&
                PlaywrightJsonContext.Default.GetTypeInfo(type) == null)
            .Select(type => type.FullName)
            .OrderBy(static name => name, StringComparer.Ordinal)
            .ToArray();

        Assert.That(missingTypes, Is.Empty);
    }
}
