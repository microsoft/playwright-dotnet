using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextCookiesTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextCookiesTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should return no cookies in pristine browser context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNoCookiesInPristineBrowserContext()
            => Assert.Empty(await Context.GetCookiesAsync());

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get a cookie")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldGetACookie()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("username=John Doe", await Page.EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                return document.cookie;
            }"));

            var cookie = (await Page.Context.GetCookiesAsync()).Single();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get a non-session cookie")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldGetANonSessionCookie()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var date = new DateTime(2038, 1, 1);
            Assert.Equal("username=John Doe", await Page.EvaluateAsync<string>(@"timestamp => {
                const date = new Date(timestamp);
                  document.cookie = `username=John Doe;expires=${date.toUTCString()}`;
                  return document.cookie;
            }", date));

            var cookie = (await Page.Context.GetCookiesAsync()).Single();
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(new DateTimeOffset(date).ToUnixTimeSeconds(), cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should properly report httpOnly cookie")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("browsercontext-cookies.spec.ts", @"should properly report ""Strict"" sameSite cookie")]
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
            Assert.Equal(SameSiteAttribute.Strict, cookies.ElementAt(0).SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", @"should properly report ""Lax"" sameSite cookie")]
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
            Assert.Equal(SameSiteAttribute.Lax, cookies.ElementAt(0).SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get multiple cookies")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal(SameSiteAttribute.None, cookie.SameSite);

            cookie = cookies[1];
            Assert.Equal("username", cookie.Name);
            Assert.Equal("John Doe", cookie.Value);
            Assert.Equal("localhost", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.Equal(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get cookies from multiple urls")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldGetCookiesFromMultipleUrls()
        {
            await Context.AddCookiesAsync(
                new Cookie
                {
                    Url = "https://foo.com",
                    Name = "doggo",
                    Value = "woofs"
                },
                new Cookie
                {
                    Url = "https://bar.com",
                    Name = "catto",
                    Value = "purrs"
                },
                new Cookie
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
            Assert.Equal(SameSiteAttribute.None, cookie.SameSite);

            cookie = cookies[1];
            Assert.Equal("doggo", cookie.Name);
            Assert.Equal("woofs", cookie.Value);
            Assert.Equal("foo.com", cookie.Domain);
            Assert.Equal("/", cookie.Path);
            Assert.Equal(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.Equal(SameSiteAttribute.None, cookie.SameSite);
        }
    }
}
