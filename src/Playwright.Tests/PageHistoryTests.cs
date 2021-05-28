using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
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
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.GotoAsync(TestConstants.ServerUrl + "/grid.html");

            var response = await Page.GoBackAsync();
            Assert.True(response.Ok);
            StringAssert.Contains(TestConstants.EmptyPage, response.Url);

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
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"
              history.pushState({ }, '', '/first.html');
              history.pushState({ }, '', '/second.html');
            ");
            Assert.AreEqual(TestConstants.ServerUrl + "/second.html", Page.Url);

            await Page.GoBackAsync();
            Assert.AreEqual(TestConstants.ServerUrl + "/first.html", Page.Url);
            await Page.GoBackAsync();
            Assert.AreEqual(TestConstants.EmptyPage, Page.Url);
            await Page.GoForwardAsync();
            Assert.AreEqual(TestConstants.ServerUrl + "/first.html", Page.Url);
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
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("() => window._foo = 10");
            await Page.ReloadAsync();
            Assert.Null(await Page.EvaluateAsync("() => window._foo"));
        }
    }
}
