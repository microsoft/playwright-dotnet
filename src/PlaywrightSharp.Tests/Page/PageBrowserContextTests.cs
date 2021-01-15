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

        [PlaywrightTest("page.spec.js", "Page.browserContext", "should return the correct browser instance")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void ShouldReturnTheCorrectBrowserInstance() => Assert.Equal(Context, Page.Context);
    }
}
