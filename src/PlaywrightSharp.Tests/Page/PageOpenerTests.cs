using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.opener</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageOpenerTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageOpenerTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.opener</playwright-describe>
        ///<playwright-it>should provide access to the opener page</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldProvideAccessToTheOpenerPage()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var opener = await popupEvent.Page.GetOpenerAsync();
            Assert.Equal(Page, opener);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.opener</playwright-describe>
        ///<playwright-it>should return null if parent page has been closed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldReturnNullIfParentPageHasBeenClosed()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            await Page.CloseAsync();
            var opener = await popupEvent.Page.GetOpenerAsync();
            Assert.Null(opener);
        }
    }
}
