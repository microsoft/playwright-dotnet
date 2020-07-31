using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
    /// <playwright-describe>defaultContext()</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class DefaultBrowserContextTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public DefaultBrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>defaultContext()</playwright-describe>
        /// <playwright-it>page.cookies() should work</playwright-it>
        [Retry]
        public async Task ContextGetCookiesAsyncShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"() => {
              document.cookie = 'username=John Doe';
            }");
            var cookie = (await Page.BrowserContext.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>defaultContext()</playwright-describe>
        /// <playwright-it>context.setCookies() should work</playwright-it>
        [Retry]
        public async Task ContextSetCookiesAsyncShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.BrowserContext.AddCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "username",
                Value = "John Doe"
            });
            Assert.Equal("username=John Doe", await Page.EvaluateAsync<string>("() => document.cookie"));
            var cookie = (await Page.BrowserContext.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-describe>defaultContext()</playwright-describe>
        /// <playwright-it>context.clearCookies() should work</playwright-it>
        [Retry]
        public async Task ContextClearCookiesAsyncShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.BrowserContext.AddCookiesAsync(
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "cookie1",
                    Value = "1"
                },
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "cookie2",
                    Value = "2"
                });

            Assert.Equal("cookie1=1; cookie2=2", await Page.EvaluateAsync<string>("() => document.cookie"));
            await Page.BrowserContext.ClearCookiesAsync();
            Assert.Equal(string.Empty, await Page.EvaluateAsync<string>("() => document.cookie"));
        }
    }
}
