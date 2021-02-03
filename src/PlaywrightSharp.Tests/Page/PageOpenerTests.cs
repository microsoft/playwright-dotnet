using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
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

        [PlaywrightTest("page.spec.js", "Page.opener", "should provide access to the opener page")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldProvideAccessToTheOpenerPage()
        {
            var (popupEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync("() => window.open('about:blank')")
            );
            var opener = await popupEvent.Page.GetOpenerAsync();
            Assert.Equal(Page, opener);
        }

        [PlaywrightTest("page.spec.js", "Page.opener", "should return null if parent page has been closed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
