using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForEvent</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForEvent : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForEvent(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "Page.waitForEvent", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWithErrorUponDisconnect()
        {
            var task = Page.WaitForEventAsync(PageEvent.Download);
            await Page.CloseAsync();
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => task);
            Assert.Contains("Page closed", exception.Message);
        }
    }
}
