using Microsoft.Testing.Platform.Builder;
using Microsoft.Testing.Platform.Services;

namespace Microsoft.Testing.Extensions.Playwright;

public static class TestingPlatformBuilderHook
{
    public static void AddExtensions(ITestApplicationBuilder testApplicationBuilder, string[] args)
    {
        testApplicationBuilder.TestHost.AddTestSessionLifetimeHandler(
            serviceProvider => new PlaywrightConfigurationExtension(serviceProvider.GetConfiguration()));
    }
}
