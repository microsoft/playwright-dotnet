using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    /// <playwright-file>cookies.spec.js</playwright-file>
    /// <playwright-describe>BrowserContext.clearCookies</playwright-describe>
    public class ClearCookiesTests : PlaywrightSharpPageBaseTest
    {
        internal ClearCookiesTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.clearCookies</playwright-describe>
        /// <playwright-it>should clear cookies</playwright-it>
        [Fact]
        public async Task ShouldClearCookes()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "cookie1",
                Value = "1"
            });
            Assert.Equal("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
            await Context.ClearCookiesAsync();
            Assert.Empty(await Page.EvaluateAsync<string>("document.cookie"));
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.clearCookies</playwright-describe>
        /// <playwright-it>should isolate cookies when clearing</playwright-it>
        [Fact]
        public async Task ShouldIsolateWhenClearing()
        {
            var anotherContext = await NewContextAsync();
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Name = "page1cookie",
                Value = "page1value",
                Url = TestConstants.EmptyPage
            });

            await anotherContext.SetCookiesAsync(new SetNetworkCookieParam
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
