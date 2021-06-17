using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageSetExtraHTTPHeadersTests : PageTestEx
    {
        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });

            var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
            await TaskUtils.WhenAll(Page.GotoAsync(Server.EmptyPage), headerTask);

            Assert.AreEqual("Bar", headerTask.Result);
        }

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work with redirects")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithRedirects()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });

            var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
            await TaskUtils.WhenAll(Page.GotoAsync(Server.Prefix + "/foo.html"), headerTask);

            Assert.AreEqual("Bar", headerTask.Result);
        }

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should work with extra headers from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithExtraHeadersFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });
            var page = await context.NewPageAsync();

            var headerTask = Server.WaitForRequest("/empty.html", request => request.Headers["Foo"]);
            await TaskUtils.WhenAll(page.GotoAsync(Server.EmptyPage), headerTask);

            Assert.AreEqual("Bar", headerTask.Result);
        }

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should override extra headers from browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldOverrideExtraHeadersFromBrowserContext()
        {
            await using var context = await Browser.NewContextAsync();
            await context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["fOo"] = "bAr",
                ["baR"] = "foO",
            });
            var page = await context.NewPageAsync();

            await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
            {
                ["Foo"] = "Bar"
            });

            var headerTask = Server.WaitForRequest("/empty.html", request => (request.Headers["Foo"], request.Headers["baR"]));
            await TaskUtils.WhenAll(page.GotoAsync(Server.EmptyPage), headerTask);

            Assert.AreEqual("Bar", headerTask.Result.Item1);
            Assert.AreEqual("foO", headerTask.Result.Item2);
        }

        [PlaywrightTest("page-set-extra-http-headers.spec.ts", "should throw for non-string header values")]
        [Test, Ignore("We don't need this test")]
        public void ShouldThrowForNonStringHeaderValues() { }
    }
}
