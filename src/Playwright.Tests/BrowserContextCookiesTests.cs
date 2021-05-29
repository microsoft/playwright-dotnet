using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextCookiesTests : PageTestEx
    {
        [PlaywrightTest("browsercontext-cookies.spec.ts", "should return no cookies in pristine browser context")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNoCookiesInPristineBrowserContext()
            => Assert.IsEmpty(await Context.CookiesAsync());

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get a cookie")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldGetACookie()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.AreEqual("username=John Doe", await Page.EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                return document.cookie;
            }"));

            var cookie = (await Page.Context.CookiesAsync()).Single();
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("John Doe", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get a non-session cookie")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldGetANonSessionCookie()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var date = new DateTime(2038, 1, 1);
            Assert.AreEqual("username=John Doe", await Page.EvaluateAsync<string>(@"timestamp => {
                const date = new Date(timestamp);
                  document.cookie = `username=John Doe;expires=${date.toUTCString()}`;
                  return document.cookie;
            }", date));

            var cookie = (await Page.Context.CookiesAsync()).Single();
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("John Doe", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(new DateTimeOffset(date).ToUnixTimeSeconds(), cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should properly report httpOnly cookie")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldProperlyReportHttpOnlyCookie()
        {
            HttpServer.Server.SetRoute("/empty.html", context =>
            {
                context.Response.Headers["Set-Cookie"] = "name=vaue;HttpOnly; Path=/";
                return Task.CompletedTask;
            });
            await Page.GotoAsync(TestConstants.EmptyPage);
            var cookies = await Context.CookiesAsync();
            Assert.That(cookies, Has.Count.EqualTo(1));
            Assert.True(cookies.ElementAt(0).HttpOnly);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", @"should properly report ""Strict"" sameSite cookie")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipWindows: true)]
        public async Task ShouldProperlyReportStrictSameSiteCookie()
        {
            HttpServer.Server.SetRoute("/empty.html", context =>
            {
                context.Response.Headers["Set-Cookie"] = "name=value;SameSite=Strict";
                return Task.CompletedTask;
            });
            await Page.GotoAsync(TestConstants.EmptyPage);
            var cookies = await Context.CookiesAsync();
            Assert.That(cookies, Has.Count.EqualTo(1));
            Assert.AreEqual(SameSiteAttribute.Strict, cookies.ElementAt(0).SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", @"should properly report ""Lax"" sameSite cookie")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipWindows: true)]
        public async Task ShouldProperlyReportLaxSameSiteCookie()
        {
            HttpServer.Server.SetRoute("/empty.html", context =>
            {
                context.Response.Headers["Set-Cookie"] = "name=value;SameSite=Lax";
                return Task.CompletedTask;
            });
            await Page.GotoAsync(TestConstants.EmptyPage);
            var cookies = await Context.CookiesAsync();
            Assert.That(cookies, Has.Count.EqualTo(1));
            Assert.AreEqual(SameSiteAttribute.Lax, cookies.ElementAt(0).SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get multiple cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldGetMultipleCookies()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.IsEmpty(await Context.CookiesAsync());

            string documentCookie = await Page.EvaluateAsync<string>(@"() => {
                document.cookie = 'username=John Doe';
                document.cookie = 'password=1234';
                return document.cookie.split('; ').sort().join('; ');
            }");

            var cookies = (await Context.CookiesAsync()).OrderBy(c => c.Name).ToList();
            Assert.AreEqual("password=1234; username=John Doe", documentCookie);
            var cookie = cookies[0];
            Assert.AreEqual("password", cookie.Name);
            Assert.AreEqual("1234", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);

            cookie = cookies[1];
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("John Doe", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-cookies.spec.ts", "should get cookies from multiple urls")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldGetCookiesFromMultipleUrls()
        {
            await Context.AddCookiesAsync(new[]
            {
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
                },
            });
            var cookies = (await Context.CookiesAsync(new[] { "https://foo.com", "https://baz.com" })).OrderBy(c => c.Name).ToList();

            Assert.AreEqual(2, cookies.Count);

            var cookie = cookies[0];
            Assert.AreEqual("birdo", cookie.Name);
            Assert.AreEqual("tweets", cookie.Value);
            Assert.AreEqual("baz.com", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);

            cookie = cookies[1];
            Assert.AreEqual("doggo", cookie.Name);
            Assert.AreEqual("woofs", cookie.Value);
            Assert.AreEqual("foo.com", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
        }
    }
}
