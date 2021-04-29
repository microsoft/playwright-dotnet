using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextClearCookiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextClearCookiesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should clear cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClearCookes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.AddCookiesAsync(new Cookie
            {
                Url = TestConstants.EmptyPage,
                Name = "cookie1",
                Value = "1"
            });
            Assert.Equal("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
            await Context.ClearCookiesAsync();
            Assert.Empty(await Context.GetCookiesAsync());
            await Page.ReloadAsync();
            Assert.Empty(await Page.EvaluateAsync<string>("document.cookie"));
        }

        [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should isolate cookies when clearing")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateWhenClearing()
        {
            await using var anotherContext = await Browser.NewContextAsync();
            await Context.AddCookiesAsync(new Cookie
            {
                Name = "page1cookie",
                Value = "page1value",
                Url = TestConstants.EmptyPage
            });

            await anotherContext.AddCookiesAsync(new Cookie
            {
                Name = "page2cookie",
                Value = "page2value",
                Url = TestConstants.EmptyPage
            });

            Assert.Single(await Context.GetCookiesAsync());
            Assert.Single(await anotherContext.GetCookiesAsync());

            await Context.ClearCookiesAsync();
            Assert.Empty((await Context.GetCookiesAsync()));
            Assert.Single((await anotherContext.GetCookiesAsync()));

            await anotherContext.ClearCookiesAsync();
            Assert.Empty(await Context.GetCookiesAsync());
            Assert.Empty(await anotherContext.GetCookiesAsync());
        }
    }
}
