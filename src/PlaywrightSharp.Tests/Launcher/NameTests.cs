using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.name</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class NameTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public NameTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>launcher.spec.js</playwright-file>
        ///<playwright-describe>Playwright.name</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public void ShouldRejectAllPromisesWhenBrowserIsClosed()
            => Assert.Equal(
                TestConstants.Product switch
                {
                    TestConstants.WebkitProduct => "webkit",
                    TestConstants.FirefoxProduct => "firefox",
                    TestConstants.ChromiumProduct => "chromium",
                    _ => null
                },
                Playwright.Name);
    }
}
