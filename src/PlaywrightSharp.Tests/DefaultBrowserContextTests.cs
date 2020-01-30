using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
    public class DefaultBrowserContextTests : PlaywrightSharpPageBaseTest
    {
        internal DefaultBrowserContextTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-test>page.cookies() should work</playwright-test>
        [Fact]
        public async Task ContextGetCookiesAsyncShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateExpressionAsync("document.cookie = 'username=John Doe'");
            var cookie = (await Page.BrowserContext.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.Equal(16, cookie.Size);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-test>context.setCookies() should work</playwright-test>
        [Fact]
        public async Task ContextSetCookiesAsyncShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.BrowserContext.SetCookiesAsync(new SetNetworkCookieParam
            {
                Name = "username",
                Value = "John Doe"
            });

            var cookie = (await Page.BrowserContext.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.Equal(16, cookie.Size);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
        }

        /// <playwright-file>defaultbrowsercontext.spec.js</playwright-file>
        /// <playwright-file>context.clearCookies() should work</playwright-file>
        [Fact]
        public async Task ContextClearCookiesAsyncShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.BrowserContext.SetCookiesAsync(
                new SetNetworkCookieParam
                {
                    Name = "cookie1",
                    Value = "1"
                },
                new SetNetworkCookieParam
                {
                    Name = "cookie2",
                    Value = "2"
                });

            Assert.Equal("cookie1=1; cookie2=2", await Page.EvaluateExpressionAsync<string>("document.cookie"));
            await Page.BrowserContext.DeleteCookiesAsync(new SetNetworkCookieParam
            {
                Name = "cookie2"
            });
            Assert.Equal("cookie1=1", await Page.EvaluateExpressionAsync<string>("document.cookie"));

            var cookie = (await Page.BrowserContext.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("cookie1", cookie.Name);
            Assert.Equal("1", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.Equal(8, cookie.Size);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
        }
    }
}
