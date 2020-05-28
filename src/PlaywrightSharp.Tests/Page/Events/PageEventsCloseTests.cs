using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Close</playwright-describe>
    [Trait("Category", "chromium")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsCloseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsCloseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Close</playwright-describe>
        ///<playwright-it>should work with window.close</playwright-it>
        [Retry]
        public async Task ShouldWorkWithWindowClose()
        {
            var newPageTsc = new TaskCompletionSource<IPage>();
            void EventHandler(object sender, PopupEventArgs e)
            {
                newPageTsc.SetResult(e.Page);
                Page.Popup -= EventHandler;
            }
            Page.Popup += EventHandler;
            await Page.EvaluateAsync<string>("() => window['newPage'] = window.open('about:blank')");
            var newPage = await newPageTsc.Task;
            var closedTsc = new TaskCompletionSource<bool>();
            newPage.Close += (sender, e) => closedTsc.SetResult(true);
            await Page.EvaluateAsync<string>("() => window['newPage'].close()");
            await closedTsc.Task;
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Close</playwright-describe>
        ///<playwright-it>should work with page.close</playwright-it>
        [Retry]
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
