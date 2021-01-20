using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    /// <playwright-file>cookies.spec.js</playwright-file>
    /// <playwright-describe>BrowserContext.clearCookies</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextClearCookiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextClearCookiesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("cookies.spec.js", "BrowserContext.clearCookies", "should clear cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClearCookes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.AddCookiesAsync(new SetNetworkCookieParam
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

        [PlaywrightTest("cookies.spec.js", "BrowserContext.clearCookies", "should isolate cookies when clearing")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateWhenClearing()
        {
            await using var anotherContext = await Browser.NewContextAsync();
            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Name = "page1cookie",
                Value = "page1value",
                Url = TestConstants.EmptyPage
            });

            await anotherContext.AddCookiesAsync(new SetNetworkCookieParam
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
