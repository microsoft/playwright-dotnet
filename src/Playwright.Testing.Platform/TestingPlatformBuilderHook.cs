using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Services;

namespace Microsoft.Playwright.Testing.Platform;

public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] args)
    {
        testApplicationBuilder.TestHost.AddTestSessionLifetimeHandler(
            serviceProvider => new PlaywrightConfigurationExtension(serviceProvider.GetConfiguration()));
    }
}
