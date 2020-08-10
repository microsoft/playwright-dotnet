using System;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    /// <playwright-file>cookies.spec.js</playwright-file>
    /// <playwright-describe>BrowserContext.cookies</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextCookiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextCookiesTests(ITestOutputHelper output) : base(output)
        {
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should return no cookies in pristine browser context</playwright-it>
        [Fact]
        public async Task ShouldReturnNoCookiesInPristineBrowserContext()
            => Assert.Empty(await Context.GetCookiesAsync());

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should get a cookie</playwright-it>
        [Fact]
        public async Task ShouldGetACookie()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("username=John Doe", await Page.EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                return document.cookie;
            }"));

            var cookie = (await Page.Context.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should get a non-session cookie</playwright-it>
        [Fact]
        public async Task ShouldGetANonSessionCookie()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var date = new DateTime(2038, 1, 1);
            Assert.Equal("username=John Doe", await Page.EvaluateAsync<string>(@"timestamp => {
                const date = new Date(timestamp);
                  document.cookie = `username=John Doe;expires=${date.toUTCString()}`;
                  return document.cookie;
            }", date));

            var cookie = (await Page.Context.GetCookiesAsync()).FirstOrDefault();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(new DateTimeOffset(date).ToUnixTimeSeconds(), cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should properly report httpOnly cookie</playwright-it>
        [Fact]
        public async Task ShouldProperlyReportHttpOnlyCookie()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Response.Headers["Set-Cookie"] = "name=vaue;HttpOnly; Path=/";
                return Task.CompletedTask;
            });
            await Page.GoToAsync(TestConstants.EmptyPage);
            var cookies = await Context.GetCookiesAsync();
            Assert.Single(cookies);
            Assert.True(cookies.ElementAt(0).HttpOnly);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should properly report "Strict" sameSite cookie</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true)]
        public async Task ShouldProperlyReportStrictSameSiteCookie()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Response.Headers["Set-Cookie"] = "name=value;SameSite=Strict";
                return Task.CompletedTask;
            });
            await Page.GoToAsync(TestConstants.EmptyPage);
            var cookies = await Context.GetCookiesAsync();
            Assert.Single(cookies);
            Assert.Equal(SameSite.Strict, cookies.ElementAt(0).SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should properly report "Lax" sameSite cookie</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true)]
        public async Task ShouldProperlyReportLaxSameSiteCookie()
        {
            Server.SetRoute("/empty.html", context =>
            {
                context.Response.Headers["Set-Cookie"] = "name=value;SameSite=Lax";
                return Task.CompletedTask;
            });
            await Page.GoToAsync(TestConstants.EmptyPage);
            var cookies = await Context.GetCookiesAsync();
            Assert.Single(cookies);
            Assert.Equal(SameSite.Lax, cookies.ElementAt(0).SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should get multiple cookies</playwright-it>
        [Fact]
        public async Task ShouldGetMultipleCookies()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Empty(await Context.GetCookiesAsync());

            string documentCookie = await Page.EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                document.cookie = 'password=1234';
                return document.cookie.split('; ').sort().join('; ');
            }");

            var cookies = (await Context.GetCookiesAsync()).OrderBy(c => c.Name).ToList();
            Assert.Equal("password=1234; username=John Doe", documentCookie);
            var cookie = cookies[0];
            Assert.Equal("password", cookie.Name);
            Assert.Equal("1234", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);

            cookie = cookies[1];
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }

        /// <playwright-file>cookies.spec.js</playwright-file>
        /// <playwright-describe>BrowserContext.cookies</playwright-describe>
        /// <playwright-it>should get cookies from multiple urls</playwright-it>
        [Fact]
        public async Task ShouldGetCookiesFromMultipleUrls()
        {
            await Context.AddCookiesAsync(
                new SetNetworkCookieParam
                {
                    Url = "https://foo.com",
                    Name = "doggo",
                    Value = "woofs"
                },
                new SetNetworkCookieParam
                {
                    Url = "https://bar.com",
                    Name = "catto",
                    Value = "purrs"
                },
                new SetNetworkCookieParam
                {
                    Url = "https://baz.com",
                    Name = "birdo",
                    Value = "tweets"
                }
            );
            var cookies = (await Context.GetCookiesAsync("https://foo.com", "https://baz.com")).OrderBy(c => c.Name).ToList();

            Assert.Equal(2, cookies.Count);

            var cookie = cookies[0];
            Assert.Equal("birdo", cookie.Name);
            Assert.Equal("tweets", cookie.Value);
            Assert.Equal("baz.com", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);

            cookie = cookies[1];
            Assert.Equal("doggo", cookie.Name);
            Assert.Equal("woofs", cookie.Value);
            Assert.Equal("foo.com", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal(SameSite.None, cookie.SameSite);
        }
    }
}
