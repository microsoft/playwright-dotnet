using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Launcher
{
    ///<playwright-file>launcher.spec.js</playwright-file>
    ///<playwright-describe>Playwright.name</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class NameTests : PlaywrightSharpBaseTest
    {
        /// <inheritdoc/>
        public NameTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("launcher.spec.js", "browserType.name", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void ShouldRejectAllPromisesWhenBrowserIsClosed()
            => Assert.Equal(
                TestConstants.Product switch
                {
                    TestConstants.WebkitProduct => "webkit",
                    TestConstants.FirefoxProduct => "firefox",
                    TestConstants.ChromiumProduct => "chromium",
                    _ => null
                },
                BrowserType.Name);
    }
}
