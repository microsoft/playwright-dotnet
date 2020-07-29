using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    /// <playwright-file>cookies.spec.js</playwright-file>
    /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextAddCookiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextAddCookiesTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "password",
                Value = "123456"
            });
            Assert.Equal("password=123456", await Page.EvaluateAsync<string>("document.cookie"));
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should roundtrip cookie</playwright-it>
        [Retry]
        public async Task ShouldRoundtripCookie()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("username=John Doe", await Page.EvaluateAsync<string>(@"timestamp => {
                const date = new Date(timestamp);
                  document.cookie = `username=John Doe;expires=${date.toUTCString()}`;
                  return document.cookie;
            }", new DateTime(2038, 1, 1)));
            var cookies = await Context.GetCookiesAsync();
            await Context.ClearCookiesAsync();
            Assert.Empty(await Context.GetCookiesAsync());
            await Context.AddCookiesAsync(cookies.Cast<SetNetworkCookieParam>());
            Assert.Equal(cookies, await Context.GetCookiesAsync());
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should send cookie header</playwright-it>
        [Retry]
        public async Task ShouldSendCookieHeader()
        {
            string cookie = string.Empty;
            Server.SetRoute("/empty.html", context =>
            {
                cookie = context.Request.Cookies.ToString();
                return Task.CompletedTask;
            });

            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Name = "cookie",
                Value = "value",
                Url = TestConstants.EmptyPage
            });

            var page = await Context.NewPageAsync();
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("cookie=value", cookie);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should isolate cookies in browser contexts</playwright-it>
        [Retry]
        public async Task ShouldIsolateCookiesInBrowserContexts()
        {
            await using var anotherContext = await Browser.NewContextAsync();
            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Name = "isolatecookie",
                Value = "page1value",
                Url = TestConstants.EmptyPage
            });

            await anotherContext.AddCookiesAsync(new SetNetworkCookieParam
            {
                Name = "isolatecookie",
                Value = "page2value",
                Url = TestConstants.EmptyPage
            });

            var cookies1 = await Context.GetCookiesAsync();
            var cookies2 = await anotherContext.GetCookiesAsync();

            Assert.Single(cookies1);
            Assert.Single(cookies2);
            Assert.Equal("isolatecookie", cookies1.ElementAt(0).Name);
            Assert.Equal("page1value", cookies1.ElementAt(0).Value);
            Assert.Equal("isolatecookie", cookies2.ElementAt(0).Name);
            Assert.Equal("page2value", cookies2.ElementAt(0).Value);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should isolate session cookies</playwright-it>
        [Retry]
        public async Task ShouldIsolateSessionCookies()
        {
            Server.SetRoute("/setcookie.html", context =>
            {
                context.Response.Cookies.Append("session", "value");
                return Task.CompletedTask;
            });

            var page = await Context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/setcookie.html");

            page = await Context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);

            await using var context2 = await Browser.NewContextAsync();
            page = await context2.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            var cookies = await context2.GetCookiesAsync();
            Assert.Empty(cookies);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should isolate persistent cookies</playwright-it>
        [Retry]
        public async Task ShouldIsolatePersistentCookies()
        {
            Server.SetRoute("/setcookie.html", context =>
            {
                context.Response.Cookies.Append("persistent", "persistent-value", new CookieOptions { MaxAge = TimeSpan.FromSeconds(3600) });
                return Task.CompletedTask;
            });

            var page = await Context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/setcookie.html");

            await using var context2 = await Browser.NewContextAsync();
            var (page1, page2) = await TaskUtils.WhenAll(Context.NewPageAsync(), context2.NewPageAsync());
            await Task.WhenAll(
                page1.GoToAsync(TestConstants.EmptyPage),
                page2.GoToAsync(TestConstants.EmptyPage));

            var (cookies1, cookies2) = await TaskUtils.WhenAll(Context.GetCookiesAsync(), context2.GetCookiesAsync());
            Assert.Single(cookies1);
            Assert.Equal("persistent", cookies1.First().Name);
            Assert.Equal("persistent-value", cookies1.First().Value);
            Assert.Empty(cookies2);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should isolate send cookie header</playwright-it>
        [Retry]
        public async Task ShouldIsolateSendCookieHeader()
        {
            string cookie = string.Empty;
            Server.SetRoute("/empty.html", context =>
            {
                cookie = context.Request.Cookies.ToString();
                return Task.CompletedTask;
            });

            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Name = "sendcookie",
                Value = "value",
                Url = TestConstants.EmptyPage
            });

            var page = await Context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/empty.html");
            Assert.Equal("cookie=value", cookie);

            var context = await Browser.NewContextAsync();
            page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(cookie);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should isolate cookies between launches</playwright-it>
        [Retry]
        public async Task ShouldIsolateCookiesBetweenLaunches()
        {
            await using (var browser1 = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions()))
            {
                var context1 = await Browser.NewContextAsync();

                await Context.AddCookiesAsync(new SetNetworkCookieParam
                {
                    Name = "cookie-in-context-1",
                    Value = "value",
                    Expires = DateTimeOffset.Now.ToUnixTimeSeconds() + 10000,
                    Url = TestConstants.EmptyPage
                });
            }

            await using (var browser2 = await Playwright[TestConstants.Product].LaunchAsync(TestConstants.GetDefaultBrowserOptions()))
            {
                var context1 = await Browser.NewContextAsync();
                var cookies = await context1.GetCookiesAsync();
                Assert.Empty(cookies);
            }
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set multiple cookies</playwright-it>
        [Retry]
        public async Task ShouldSetMultipleCookies()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Context.AddCookiesAsync(
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "multiple-1",
                    Value = "123456"
                },
                new SetNetworkCookieParam
                {
                    Url = TestConstants.EmptyPage,
                    Name = "multiple-2",
                    Value = "bar"
                }
            );

            Assert.Equal(
                new[]
                {
                    "multiple1=123456",
                    "multiple-2=bar"
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
            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "expires",
                Value = "123456"
            });

            var cookies = await Context.GetCookiesAsync();

            Assert.Equal(-1, cookies.ElementAt(0).Expires);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set cookie with reasonable defaults</playwright-it>
        [Retry]
        public async Task ShouldSetCookieWithReasonableDefaults()
        {
            await Context.AddCookiesAsync(new SetNetworkCookieParam
            {
                Url = TestConstants.EmptyPage,
                Name = "defaults",
                Value = "123456"
            });

            var cookie = Assert.Single(await Context.GetCookiesAsync());
            Assert.Equal("defaults", cookie.Name);
            Assert.Equal("123456", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set a cookie with a path</playwright-it>
        [Retry]
        public async Task ShouldSetACookieWithAPath()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await Context.AddCookiesAsync(new SetNetworkCookieParam
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
                => await Context.AddCookiesAsync(
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
                => await Context.AddCookiesAsync(
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

            await Context.AddCookiesAsync(new SetNetworkCookieParam
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

            await Context.AddCookiesAsync(new SetNetworkCookieParam
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
            await Context.AddCookiesAsync(new SetNetworkCookieParam { Name = "example-cookie", Value = "best", Url = "https://www.example.com" });
            Assert.Equal(string.Empty, await Page.EvaluateAsync<string>("document.cookie"));
            var cookie = Assert.Single(await Context.GetCookiesAsync("https://www.example.com"));
            Assert.Equal("example-cookie", cookie.Name);
            Assert.Equal("best", cookie.Value);
            Assert.Equal("www.example.com", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should set cookies for a frame</playwright-it>
        [Retry]
        public async Task ShouldSetCookiesForAFrame()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Context.AddCookiesAsync(
                new SetNetworkCookieParam
                {
                    Url = TestConstants.ServerUrl,
                    Name = "frame-cookie",
                    Value = "value"
                });

            await Page.EvaluateAsync(@"src => {
                    let fulfill;
                    const promise = new Promise(x => fulfill = x);
                    const iframe = document.createElement('iframe');
                    document.body.appendChild(iframe);
                    iframe.onload = fulfill;
                    iframe.src = src;
                    return promise;
                }", TestConstants.ServerUrl + "/grid.html");

            Assert.Equal("frame-cookie=value", await Page.FirstChildFrame().EvaluateAsync<string>("document.cookie"));
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.setCookies</playwright-describe>
        /// <playwright-it>should(not) block third party cookies</playwright-it>
        [Retry]
        public async Task ShouldNotBlockThirdPartyCookies()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

            await Page.EvaluateAsync(@"src => {
                  let fulfill;
                  const promise = new Promise(x => fulfill = x);
                  const iframe = document.createElement('iframe');
                  document.body.appendChild(iframe);
                  iframe.onload = fulfill;
                  iframe.src = src;
                  return promise;
                }", TestConstants.ServerUrl + "/grid.html");

            await Page.FirstChildFrame().EvaluateAsync<string>("document.cookie = 'username=John Doe'");
            await Page.WaitForTimeoutAsync(2000);
            bool allowsThirdPart = !TestConstants.IsWebKit;
            var cookies = await Context.GetCookiesAsync(TestConstants.CrossProcessUrl + "/grid.html");

            if (allowsThirdPart)
            {
                Assert.Single(cookies);
                var cookie = cookies.First();
                Assert.Equal("127.0.0.1", cookie.Domain);
                Assert.Equal(cookie.Expires, -1);
                Assert.False(cookie.HttpOnly);
                Assert.Equal("username", cookie.Name);
                Assert.Equal("/", cookie.Path);
                Assert.Equal(SameSite.None, cookie.SameSite);
                Assert.False(cookie.Secure);
                Assert.Equal("John Doe", cookie.Value);
            }
            else
            {
                Assert.Empty(cookies);
            }
        }
    }
}
