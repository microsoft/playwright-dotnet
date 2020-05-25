using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.error</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsErrorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.error</playwright-describe>
        ///<playwright-it>should throw when page crashes</playwright-it>
        [Fact]
        public async Task ShouldThrowWhenPageCrashes()
        {
            var errorTask = Page.WaitForEvent<ErrorEventArgs>(PageEvent.Error);

            if (TestConstants.IsChromium)
            {
                _ = Page.GoToAsync("chrome://crash").ContinueWith(t => { });
            }
            else if (TestConstants.IsWebKit)
            {
                Assert.True(false, "TODO: expose PageDelegate and invoke Page._delegate._session.send('Page.crash', }");
            }
            await errorTask;
            Assert.Equal("Page crashed!", errorTask.Result.Error);
        }
    }
}
