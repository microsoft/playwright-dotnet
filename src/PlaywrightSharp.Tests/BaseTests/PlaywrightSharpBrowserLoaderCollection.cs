using Xunit;

namespace Microsoft.Playwright.Tests.BaseTests
{
    /// This class has no code, and is never created. Its purpose is simply
    /// to be the place to apply [CollectionDefinition] and all the ICollectionFixture interfaces.
    /// Recipe from https://xunit.github.io/docs/shared-context.html#class-fixture
    [CollectionDefinition(TestConstants.TestFixtureBrowserCollectionName, DisableParallelization = true)]
    public class PlaywrightSharpBrowserLoaderCollection : ICollectionFixture<PlaywrightSharpBrowserLoaderFixture>
    {
    }
}
