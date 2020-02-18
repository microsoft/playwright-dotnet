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
            var newPagePromise = new Promise(f => Page.once('popup', f));
            await Page.EvaluateAsync<string>(() => window['newPage'] = window.open('about:blank'));
            var newPage = await newPagePromise;
            var closedPromise = new Promise(x => newPage.close', x));



            await Page.EvaluateAsync<string>(() => window['newPage'].CloseAsync());
            await closedPromise;

        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Close</playwright-describe>
        ///<playwright-it>should work with page.close</playwright-it>
        [Fact]
        public async Task ShouldWorkWithPageClose()
        {
            var newPage = await context.NewPageAsync();
            var closedPromise = new Promise(x => newPage.close, x));


            await newPage.CloseAsync();
            await closedPromise;

        }
    }
}
