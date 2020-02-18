using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Close</playwright-describe>
    public class PageEventsCloseTests : PlaywrightSharpPageBaseTest
    {
        internal PageEventsCloseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Close</playwright-describe>
        ///<playwright-it>should work with window.close</playwright-it>
        [Fact]
        public async Task ShouldWorkWithWindowClose()
        {
            var newPageTsc = new TaskCompletionSource<IPage>();
            Page.Once<PopupEventArgs>(PageEvent.Popup, popup => newPageTsc.SetResult(popup.Page));
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
        [Fact]
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
