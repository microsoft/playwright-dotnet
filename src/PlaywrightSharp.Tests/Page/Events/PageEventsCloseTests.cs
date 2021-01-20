using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Close</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsCloseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsCloseTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "Page.Events.Close", "should work with window.close")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithWindowClose()
        {
            var newPageTask = Page.WaitForEventAsync(PageEvent.Popup);
            await Page.EvaluateAsync<string>("() => window['newPage'] = window.open('about:blank')");
            var newPage = (await newPageTask).Page;
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (sender, e) => closedTsc.SetResult(true);
            await Page.EvaluateAsync<string>("() => window['newPage'].close()");
            await closedTsc.Task;
        }

        [PlaywrightTest("page.spec.js", "Page.Events.Close", "should work with page.close")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPageClose()
        {
            var newPage = await Context.NewPageAsync();
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (sender, e) => closedTsc.SetResult(true);
            await newPage.CloseAsync();
            await closedTsc.Task;
        }
    }
}
