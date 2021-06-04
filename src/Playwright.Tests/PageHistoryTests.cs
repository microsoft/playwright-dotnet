using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageHistoryTests : PageTestEx
    {
        [PlaywrightTest("page-history.spec.ts", "page.goBack should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageGobackShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.GotoAsync(Server.Prefix + "/grid.html");

            var response = await Page.GoBackAsync();
            Assert.True(response.Ok);
            StringAssert.Contains(Server.EmptyPage, response.Url);

            response = await Page.GoForwardAsync();
            Assert.True(response.Ok);
            StringAssert.Contains("/grid.html", response.Url);

            response = await Page.GoForwardAsync();
            Assert.Null(response);
        }

        [PlaywrightTest("page-history.spec.ts", "page.goBack should work with HistoryAPI")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageGoBackShouldWorkWithHistoryAPI()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync(@"
              history.pushState({ }, '', '/first.html');
              history.pushState({ }, '', '/second.html');
            ");
            Assert.AreEqual(Server.Prefix + "/second.html", Page.Url);

            await Page.GoBackAsync();
            Assert.AreEqual(Server.Prefix + "/first.html", Page.Url);
            await Page.GoBackAsync();
            Assert.AreEqual(Server.EmptyPage, Page.Url);
            await Page.GoForwardAsync();
            Assert.AreEqual(Server.Prefix + "/first.html", Page.Url);
        }

        [PlaywrightTest("page-history.spec.ts", "should work for file urls")]
        [Test, Ignore("We need screenshots for this")]
        public void ShouldWorkForFileUrls()
        {
        }


        [PlaywrightTest("page-history.spec.ts", "page.reload should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task PageReloadShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.EvaluateAsync("() => window._foo = 10");
            await Page.ReloadAsync();
            Assert.Null(await Page.EvaluateAsync("() => window._foo"));
        }
    }
}
