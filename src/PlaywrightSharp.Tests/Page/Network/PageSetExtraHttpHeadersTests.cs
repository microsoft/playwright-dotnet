using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Network
{
    ///<playwright-file>network.spec.js</playwright-file>
    ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetExtraHttpHeadersTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSetExtraHttpHeadersTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });

            var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
            await TaskUtils.WhenAll(Page.GoToAsync(TestConstants.EmptyPage), headerTask);

            Assert.Equal("Bar", headerTask.Result);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
        ///<playwright-it>should work with redirects</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirects()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            await Page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });

            var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
            await TaskUtils.WhenAll(Page.GoToAsync(TestConstants.ServerUrl + "/foo.html"), headerTask);

            Assert.Equal("Bar", headerTask.Result);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
        ///<playwright-it>should work with extra headers from browser context</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithExtraHeadersFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });
            var page = await context.NewPageAsync();

            var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
            await TaskUtils.WhenAll(page.GoToAsync(TestConstants.EmptyPage), headerTask);

            Assert.Equal("Bar", headerTask.Result);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
        ///<playwright-it>should override extra headers from browser context</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldOverrideExtraHeadersFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["fOo"] = "bAr",
                ["baR"] = "foO",
            });
            var page = await context.NewPageAsync();

            await page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });

            var headerTask = Server.WaitForRequest("/empty.html", request => (request.Headers["Foo"], request.Headers["baR"]));
            await TaskUtils.WhenAll(page.GoToAsync(TestConstants.EmptyPage), headerTask);

            Assert.Equal("Bar", headerTask.Result.Item1);
            Assert.Equal("foO", headerTask.Result.Item2);
        }

        ///<playwright-file>network.spec.js</playwright-file>
        ///<playwright-describe>Page.setExtraHTTPHeaders</playwright-describe>
        ///<playwright-it>should throw for non-string header values</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForNonStringHeaderValues() { }
    }
}
