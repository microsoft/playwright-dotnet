using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageSetExtraHTTPHeadersTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageSetExtraHTTPHeadersTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work")]
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

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work with redirects")]
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

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work with extra headers from browser context")]
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

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should override extra headers from browser context")]
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

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should throw for non-string header values")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldThrowForNonStringHeaderValues() { }
    }
}
