using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    /// <playwright-file>cookies.spec.js</playwright-file>
    /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class SetCookiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public SetCookiesTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "password",
                Value = "123456"
            });
            Assert.Equal("password=123456", await Page.EvaluateAsync<string>("document.cookie"));
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should isolate cookies in browser contexts</playwright-it>
        [Retry]
        public async Task ShouldIsolateCookiesInBrowserContexts()
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

            var cookies1 = await Context.GetCookiesAsync();
            var cookies2 = await anotherContext.GetCookiesAsync();

            Assert.Single(cookies1);
            Assert.Single(cookies2);
            Assert.Equal("page1cookie", cookies1.ElementAt(0).Name);
            Assert.Equal("page1value", cookies1.ElementAt(0).Value);
            Assert.Equal("page2cookie", cookies2.ElementAt(0).Name);
            Assert.Equal("page2value", cookies2.ElementAt(0).Value);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set multiple cookies</playwright-it>
        [Retry]
        public async Task ShouldSetMultipleCookies()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Context.SetCookiesAsync(
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "password",
                    Value = "123456"
                },
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "foo",
                    Value = "bar"
                }
            );

            Assert.Equal(
                new[]
                {
                    "foo=bar",
                    "password=123456"
                },
                await Page.EvaluateAsync<string[]>(@"() => {
                    const cookies = document.cookie.split(';');
                    return cookies.map(cookie => cookie.trim()).sort();
                }")
            );
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should have |expires| set to |-1| for session cookies</playwright-it>
        [Retry]
        public async Task ShouldHaveExpiresSetToMinus1ForSessionCookies()
        {
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "password",
                Value = "123456"
            });

            var cookies = await Context.GetCookiesAsync();

            Assert.True(cookies.ElementAt(0).Session);
            Assert.Equal(-1, cookies.ElementAt(0).Expires);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set cookie with reasonable defaults</playwright-it>
        [Retry]
        public async Task ShouldSetCookieWithReasonableDefaults()
        {
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "password",
                Value = "123456"
            });

            var cookie = Assert.Single(await Context.GetCookiesAsync());
            Assert.Equal("password", cookie.Name);
            Assert.Equal("123456", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set a cookie with a path</playwright-it>
        [Retry]
        public async Task ShouldSetACookieWithAPath()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Domain = "localhost",
                Path = "/grid.html",
                Name = "gridcookie",
                Value = "GRID"
            });
            var cookie = Assert.Single(await Context.GetCookiesAsync());
            Assert.Equal("gridcookie", cookie.Name);
            Assert.Equal("GRID", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/grid.html", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
            Assert.Equal(SameSite.None, cookie.SameSite);

            Assert.Equal("gridcookie=GRID", await Page.EvaluateAsync<string>("document.cookie"));
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(await Page.EvaluateAsync<string>("document.cookie"));
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            Assert.Equal("gridcookie=GRID", await Page.EvaluateAsync<string>("document.cookie"));
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should not set a cookie with blank page URL</playwright-it>
        [Retry]
        public async Task ShouldNotSetACookieWithBlankPageURL()
        {
            await Page.GoToAsync(TestConstants.AboutBlank);

            var exception = await Assert.ThrowsAsync<ArgumentException>(async ()
                => await Context.SetCookiesAsync(
                    new SetNetworkCookieParam
                    {
                        Url = TestConstants.EmptyPage,
                        Name = "example-cookie",
                        Value = "best"
                    },
                    new SetNetworkCookieParam
                    {
                        Url = "about:blank",
                        Name = "example-cookie-blank",
                        Value = "best"
                    }));
            Assert.Equal("Blank page can not have cookie \"example-cookie-blank\"", exception.Message);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should not set a cookie on a data URL page</playwright-it>
        [Retry]
        public async Task ShouldNotSetACookieOnADataURLPage()
        {
            await Page.GoToAsync("data:,Hello%2C%20World!");
            var exception = await Assert.ThrowsAnyAsync<ArgumentException>(async ()
                => await Context.SetCookiesAsync(
                    new SetNetworkCookieParam
                    {
                        Url = "data:,Hello%2C%20World!",
                        Name = "example-cookie",
                        Value = "best"
                    }));

            Assert.Equal("Data URL page can not have cookie \"example-cookie\"", exception.Message);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should default to setting secure cookie for HTTPS websites</playwright-it>
        [Retry]
        public async Task ShouldDefaultToSettingSecureCookieForHttpsWebsites()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            const string secureUrl = "https://example.com";

            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = secureUrl,
                Name = "foo",
                Value = "bar"
            });
            var cookie = Assert.Single(await Context.GetCookiesAsync(secureUrl));
            Assert.True(cookie.Secure);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should be able to set unsecure cookie for HTTP website</playwright-it>
        [Retry]
        public async Task ShouldBeAbleToSetUnsecureCookieForHttpWebSite()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            string SecureUrl = "http://example.com";

            await Context.SetCookiesAsync(new SetNetworkCookieParam
            {
                Url = SecureUrl,
                Name = "foo",
                Value = "bar"
            });
            var cookie = Assert.Single(await Context.GetCookiesAsync(SecureUrl));
            Assert.False(cookie.Secure);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set a cookie on a different domain</playwright-it>
        [Retry]
        public async Task ShouldSetACookieOnADifferentDomain()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Context.SetCookiesAsync(new SetNetworkCookieParam { Name = "example-cookie", Value = "best", Url = "https://www.example.com" });
            Assert.Equal(string.Empty, await Page.EvaluateAsync<string>("document.cookie"));
            var cookie = Assert.Single(await Context.GetCookiesAsync("https://www.example.com"));
            Assert.Equal("example-cookie", cookie.Name);
            Assert.Equal("best", cookie.Value);
            Assert.Equal("www.example.com", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.True(cookie.Session);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>sshould set cookies from a frame</playwright-it>
        [Retry]
        public async Task ShouldSetCookiesFromAFrame()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Context.SetCookiesAsync(
                new SetNetworkCookieParam
                {
                    Url = TestConstants.ServerUrl,
                    Name = "localhost-cookie",
                    Value = "best"
                },
                new SetNetworkCookieParam
                {
                    Url = TestConstants.CrossProcessUrl,
                    Name = "127-cookie",
                    Value = "worst"
                });

            await Page.EvaluateAsync(@"src => {
                    let fulfill;
                    const promise = new Promise(x => fulfill = x);
                    const iframe = document.createElement('iframe');
                    document.body.appendChild(iframe);
                    iframe.onload = fulfill;
                    iframe.src = src;
                    return promise;
                }", TestConstants.CrossProcessHttpPrefix);

            Assert.Equal("localhost-cookie=best", await Page.EvaluateAsync<string>("document.cookie"));
            Assert.Equal("127-cookie=worst", await Page.FirstChildFrame().EvaluateAsync<string>("document.cookie"));

            var cookie = Assert.Single(await Context.GetCookiesAsync(TestConstants.ServerUrl));
            Assert.Equal("localhost-cookie", cookie.Name);
            Assert.Equal("best", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
            Assert.Equal(SameSite.None, cookie.SameSite);

            cookie = Assert.Single(await Context.GetCookiesAsync(TestConstants.CrossProcessHttpPrefix));
            Assert.Equal("127-cookie", cookie.Name);
            Assert.Equal("worst", cookie.Value);
            Assert.Equal("127.0.0.1", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.True(cookie.Session);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }
    }
}
