using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.browserContext</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageBrowserContextTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageBrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.browserContext</playwright-describe>
        ///<playwright-it>should return the correct browser instance</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public void ShouldReturnTheCorrectBrowserInstance() => Assert.Equal(Context, Page.Context);
    }
}
