using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageHistoryTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageHistoryTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-history.spec.ts", "page.goBack should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageGobackShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");

            var response = await Page.GoBackAsync();
            Assert.True(response.Ok);
            Assert.Contains(TestConstants.EmptyPage, response.Url);

            response = await Page.GoForwardAsync();
            Assert.True(response.Ok);
            Assert.Contains("/grid.html", response.Url);

            response = await Page.GoForwardAsync();
            Assert.Null(response);
        }

        [PlaywrightTest("page-history.spec.ts", "page.goBack should work with HistoryAPI")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageGoBackShouldWorkWithHistoryAPI()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"
              history.pushState({ }, '', '/first.html');
              history.pushState({ }, '', '/second.html');
            ");
            Assert.Equal(TestConstants.ServerUrl + "/second.html", Page.Url);

            await Page.GoBackAsync();
            Assert.Equal(TestConstants.ServerUrl + "/first.html", Page.Url);
            await Page.GoBackAsync();
            Assert.Equal(TestConstants.EmptyPage, Page.Url);
            await Page.GoForwardAsync();
            Assert.Equal(TestConstants.ServerUrl + "/first.html", Page.Url);
        }

        [PlaywrightTest("page-history.spec.ts", "should work for file urls")]
        [Fact(Skip = "We need screenshots for this")]
        public void ShouldWorkForFileUrls()
        {
        }


        [PlaywrightTest("page-history.spec.ts", "page.reload should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task PageReloadShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync("() => window._foo = 10");
            await Page.ReloadAsync();
            Assert.Null(await Page.EvaluateAsync("() => window._foo"));
        }
    }
}
