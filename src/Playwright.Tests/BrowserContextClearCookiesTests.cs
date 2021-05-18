using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
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
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = TestConstants.EmptyPage,
                    Name = "cookie1",
                    Value = "1"
                }
            });
            Assert.Equal("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
            await Context.ClearCookiesAsync();
            Assert.Empty(await Context.CookiesAsync());
            await Page.ReloadAsync();
            Assert.Empty(await Page.EvaluateAsync<string>("document.cookie"));
        }

        [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should isolate cookies when clearing")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateWhenClearing()
        {
            await using var anotherContext = await Browser.NewContextAsync();
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "page1cookie",
                    Value = "page1value",
                    Url = TestConstants.EmptyPage
                }
            });

            await anotherContext.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "page2cookie",
                    Value = "page2value",
                    Url = TestConstants.EmptyPage
                }
            });

            Assert.Single(await Context.CookiesAsync());
            Assert.Single(await anotherContext.CookiesAsync());

            await Context.ClearCookiesAsync();
            Assert.Empty((await Context.CookiesAsync()));
            Assert.Single((await anotherContext.CookiesAsync()));

            await anotherContext.ClearCookiesAsync();
            Assert.Empty(await Context.CookiesAsync());
            Assert.Empty(await anotherContext.CookiesAsync());
        }
    }
}
