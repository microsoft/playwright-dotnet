using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>browsercontext.spec.js</playwright-file>
    ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class EventsBrowserContextPageTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public EventsBrowserContextPageTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have url</playwright-it>
        public async Task ShouldHaveUrl()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            Assert.Equal(TestConstants.EmptyPage, otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have url after domcontentloaded</playwright-it>
        public async Task ShouldHaveUrlAfterDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", TestConstants.EmptyPage));

            await otherPage.Page.WaitForLoadStateAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });
            Assert.Equal(TestConstants.EmptyPage, otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have about:blank url with domcontentloaded</playwright-it>
        public async Task ShouldHaveAboutBlankUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", "about:blank"));

            await otherPage.Page.WaitForLoadStateAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });
            Assert.Equal("about:blank", otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should have about:blank for empty url with domcontentloaded</playwright-it>
        public async Task ShouldHaveAboutBlankUrlForEmptyUrlWithDomcontentloaded()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("() => window.open()"));

            await otherPage.Page.WaitForLoadStateAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });
            Assert.Equal("about:blank", otherPage.Page.Url);
        }

        ///<playwright-file>browsercontext.spec.js</playwright-file>
        ///<playwright-describe>Events.BrowserContext.Page</playwright-describe>
        ///<playwright-it>should report when a new page is created and closed</playwright-it>
        public async Task ShouldReportWhenANewPageIsCreatedAndClosed()
        {
            await using var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var (otherPage, _) = await TaskUtils.WhenAll(
                context.WaitForEvent<PageEventArgs>(ContextEvent.PageCreated),
                page.EvaluateAsync("url => window.open(url)", TestConstants.CrossProcessUrl + "/empty.html"));

            Assert.Contains(TestConstants.CrossProcessUrl, otherPage.Page.Url);
        }
    }
}
