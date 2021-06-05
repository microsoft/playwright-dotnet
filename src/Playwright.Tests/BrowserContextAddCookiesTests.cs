using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class BrowserContextAddCookiesTests : PageTestEx
    {
        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Context.AddCookiesAsync(new[] { new Cookie()
            {
                Url = Server.EmptyPage,
                Name = "password",
                Value = "123456"
            } });
            Assert.AreEqual("password=123456", await Page.EvaluateAsync<string>("document.cookie"));
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should roundtrip cookie")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRoundtripCookie()
        {
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("username=John Doe", await Page.EvaluateAsync<string>(@"timestamp => {
                const date = new Date(timestamp);
                  document.cookie = `username=John Doe;expires=${date.toUTCString()}`;
                  return document.cookie;
            }", new DateTime(2038, 1, 1)));
            var cookies = await Context.CookiesAsync();
            await Context.ClearCookiesAsync();
            Assert.IsEmpty(await Context.CookiesAsync());
            await Context.AddCookiesAsync(cookies.Select(c => new Cookie()
            {
                Domain = c.Domain,
                Expires = c.Expires,
                HttpOnly = c.HttpOnly,
                Name = c.Name,
                Path = c.Path,
                SameSite = c.SameSite,
                Secure = c.Secure,
                Value = c.Value
            }));
            var newCookies = await Context.CookiesAsync();
            AssertEqual(cookies, newCookies);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should send cookie header")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSendCookieHeader()
        {
            string cookie = string.Empty;
            Server.SetRoute("/empty.html", context =>
            {
                cookie = string.Join(";", context.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
                return Task.CompletedTask;
            });

            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "cookie",
                    Value = "value",
                    Url = Server.EmptyPage
                }
            });

            var page = await Context.NewPageAsync();
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("cookie=value", cookie);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate cookies in browser contexts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateCookiesInBrowserContexts()
        {
            await using var anotherContext = await Browser.NewContextAsync();
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "isolatecookie",
                    Value = "page1value",
                    Url = Server.EmptyPage
                }
            });

            await anotherContext.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "isolatecookie",
                    Value = "page2value",
                    Url = Server.EmptyPage
                }
            });

            var cookies1 = await Context.CookiesAsync();
            var cookies2 = await anotherContext.CookiesAsync();

            Assert.That(cookies1, Has.Count.EqualTo(1));
            Assert.That(cookies2, Has.Count.EqualTo(1));
            Assert.AreEqual("isolatecookie", cookies1.ElementAt(0).Name);
            Assert.AreEqual("page1value", cookies1.ElementAt(0).Value);
            Assert.AreEqual("isolatecookie", cookies2.ElementAt(0).Name);
            Assert.AreEqual("page2value", cookies2.ElementAt(0).Value);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate session cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateSessionCookies()
        {
            Server.SetRoute("/setcookie.html", context =>
            {
                context.Response.Cookies.Append("session", "value");
                return Task.CompletedTask;
            });

            var page = await Context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/setcookie.html");

            page = await Context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);

            await using var context2 = await Browser.NewContextAsync();
            page = await context2.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var cookies = await context2.CookiesAsync();
            Assert.IsEmpty(cookies);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate persistent cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolatePersistentCookies()
        {
            Server.SetRoute("/setcookie.html", context =>
            {
                context.Response.Cookies.Append("persistent", "persistent-value", new() { MaxAge = TimeSpan.FromSeconds(3600) });
                return Task.CompletedTask;
            });

            var page = await Context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/setcookie.html");

            await using var context2 = await Browser.NewContextAsync();
            var (page1, page2) = await TaskUtils.WhenAll(Context.NewPageAsync(), context2.NewPageAsync());
            await TaskUtils.WhenAll(
                page1.GotoAsync(Server.EmptyPage),
                page2.GotoAsync(Server.EmptyPage));

            var (cookies1, cookies2) = await TaskUtils.WhenAll(Context.CookiesAsync(), context2.CookiesAsync());
            Assert.That(cookies1, Has.Count.EqualTo(1));
            Assert.AreEqual("persistent", cookies1[0].Name);
            Assert.AreEqual("persistent-value", cookies1[0].Value);
            Assert.IsEmpty(cookies2);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate send cookie header")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateSendCookieHeader()
        {
            string cookie = string.Empty;
            Server.SetRoute("/empty.html", context =>
            {
                cookie = string.Join(";", context.Request.Cookies.Select(c => $"{c.Key}={c.Value}"));
                return Task.CompletedTask;
            });

            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "sendcookie",
                    Value = "value",
                    Url = Server.EmptyPage
                }
            });

            var page = await Context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/empty.html");
            Assert.AreEqual("sendcookie=value", cookie);

            var context = await Browser.NewContextAsync();
            page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.IsEmpty(cookie);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate cookies between launches")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIsolateCookiesBetweenLaunches()
        {
            await using (var browser1 = await Playwright[TestConstants.BrowserName].LaunchAsync())
            {
                var context1 = await Browser.NewContextAsync();

                await Context.AddCookiesAsync(new[]
                {
                    new Cookie
                    {
                        Name = "cookie-in-context-1",
                        Value = "value",
                        Expires = DateTimeOffset.Now.ToUnixTimeSeconds() + 10000,
                        Url = Server.EmptyPage
                    }
                });
            }

            await using (var browser2 = await Playwright[TestConstants.BrowserName].LaunchAsync())
            {
                var context1 = await Browser.NewContextAsync();
                var cookies = await context1.CookiesAsync();
                Assert.IsEmpty(cookies);
            }
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set multiple cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetMultipleCookies()
        {
            await Page.GotoAsync(Server.EmptyPage);

            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "multiple-1",
                    Value = "123456"
                },
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "multiple-2",
                    Value = "bar"
                },
            });

            Assert.AreEqual(
                new[]
                {
                    "multiple-1=123456",
                    "multiple-2=bar"
                },
                await Page.EvaluateAsync<string[]>(@"() => {
                    const cookies = document.cookie.split(';');
                    return cookies.map(cookie => cookie.trim()).sort();
                }")
            );
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should have |expires| set to |-1| for session cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHaveExpiresSetToMinus1ForSessionCookies()
        {
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "expires",
                    Value = "123456"
                }
            });

            var cookies = await Context.CookiesAsync();

            Assert.AreEqual(-1, cookies.ElementAt(0).Expires);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set cookie with reasonable defaults")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetCookieWithReasonableDefaults()
        {
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "defaults",
                    Value = "123456"
                }
            });

            var cookies = await Context.CookiesAsync();
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies[0];
            Assert.AreEqual("defaults", cookie.Name);
            Assert.AreEqual("123456", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(-1, cookie.Expires);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set a cookie with a path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetACookieWithAPath()
        {
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Domain = "localhost",
                    Path = "/grid.html",
                    Name = "gridcookie",
                    Value = "GRID"
                }
            });

            var cookies = await Context.CookiesAsync();
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies[0];
            Assert.AreEqual("gridcookie", cookie.Name);
            Assert.AreEqual("GRID", cookie.Value);
            Assert.AreEqual("localhost", cookie.Domain);
            Assert.AreEqual("/grid.html", cookie.Path);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.False(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);

            Assert.AreEqual("gridcookie=GRID", await Page.EvaluateAsync<string>("document.cookie"));
            await Page.GotoAsync(Server.EmptyPage);
            Assert.IsEmpty(await Page.EvaluateAsync<string>("document.cookie"));
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            Assert.AreEqual("gridcookie=GRID", await Page.EvaluateAsync<string>("document.cookie"));
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should not set a cookie with blank page URL")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotSetACookieWithBlankPageURL()
        {
            await Page.GotoAsync(TestConstants.AboutBlank);

            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(()
                => Context.AddCookiesAsync(new[]
                {
                        new Cookie
                        {
                            Url = Server.EmptyPage,
                            Name = "example-cookie",
                            Value = "best"
                        },
                        new Cookie
                        {
                            Url = "about:blank",
                            Name = "example-cookie-blank",
                            Value = "best"
                        },
                }));
            Assert.AreEqual("Blank page can not have cookie \"example-cookie-blank\"", exception.Message);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should not set a cookie on a data URL page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotSetACookieOnADataURLPage()
        {
            await Page.GotoAsync("data:,Hello%2C%20World!");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(()
                => Context.AddCookiesAsync(new[]
                {
                        new Cookie
                        {
                            Url = "data:,Hello%2C%20World!",
                            Name = "example-cookie",
                            Value = "best"
                        }
                }));

            Assert.AreEqual("Data URL page can not have cookie \"example-cookie\"", exception.Message);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should default to setting secure cookie for HTTPS websites")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDefaultToSettingSecureCookieForHttpsWebsites()
        {
            await Page.GotoAsync(Server.EmptyPage);
            const string secureUrl = "https://example.com";

            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = secureUrl,
                    Name = "foo",
                    Value = "bar"
                }
            });

            var cookies = await Context.CookiesAsync(new[] { secureUrl });
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies[0];
            Assert.True(cookie.Secure);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should be able to set unsecure cookie for HTTP website")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeAbleToSetUnsecureCookieForHttpWebSite()
        {
            await Page.GotoAsync(Server.EmptyPage);
            string SecureUrl = "http://example.com";

            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = SecureUrl,
                    Name = "foo",
                    Value = "bar"
                }
            });

            var cookies = await Context.CookiesAsync(new[] { SecureUrl });
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies[0];

            Assert.False(cookie.Secure);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set a cookie on a different domain")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetACookieOnADifferentDomain()
        {
            await Page.GotoAsync(Server.Prefix + "/grid.html");
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "example-cookie",
                    Value = "best",
                    Url = "https://www.example.com"
                }
            });
            Assert.AreEqual(string.Empty, await Page.EvaluateAsync<string>("document.cookie"));

            var cookies = await Context.CookiesAsync(new[] { "https://www.example.com" });
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies[0];

            Assert.AreEqual("example-cookie", cookie.Name);
            Assert.AreEqual("best", cookie.Value);
            Assert.AreEqual("www.example.com", cookie.Domain);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.False(cookie.HttpOnly);
            Assert.True(cookie.Secure);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set cookies for a frame")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetCookiesForAFrame()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.Prefix,
                    Name = "frame-cookie",
                    Value = "value"
                }
            });

            await Page.EvaluateAsync(@"src => {
                    let fulfill;
                    const promise = new Promise(x => fulfill = x);
                    const iframe = document.createElement('iframe');
                    document.body.appendChild(iframe);
                    iframe.onload = fulfill;
                    iframe.src = src;
                    return promise;
                }", Server.Prefix + "/grid.html");

            Assert.AreEqual("frame-cookie=value", await Page.FirstChildFrame().EvaluateAsync<string>("document.cookie"));
        }

        [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should(not) block third party cookies")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotBlockThirdPartyCookies()
        {
            await Page.GotoAsync(Server.EmptyPage);

            await Page.EvaluateAsync(@"src => {
                  let fulfill;
                  const promise = new Promise(x => fulfill = x);
                  const iframe = document.createElement('iframe');
                  document.body.appendChild(iframe);
                  iframe.onload = fulfill;
                  iframe.src = src;
                  return promise;
                }", Server.CrossProcessPrefix + "/grid.html");

            await Page.FirstChildFrame().EvaluateAsync<string>("document.cookie = 'username=John Doe'");
            await Page.WaitForTimeoutAsync(2000);
            bool allowsThirdPart = !TestConstants.IsWebKit;
            var cookies = await Context.CookiesAsync(new[] { Server.CrossProcessPrefix + "/grid.html" });

            if (allowsThirdPart)
            {
                Assert.That(cookies, Has.Count.EqualTo(1));
                var cookie = cookies[0];
                Assert.AreEqual("127.0.0.1", cookie.Domain);
                Assert.AreEqual(cookie.Expires, -1);
                Assert.False(cookie.HttpOnly);
                Assert.AreEqual("username", cookie.Name);
                Assert.AreEqual("/", cookie.Path);
                Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
                Assert.False(cookie.Secure);
                Assert.AreEqual("John Doe", cookie.Value);
            }
            else
            {
                Assert.IsEmpty(cookies);
            }
        }

        static void AssertEqual(IEnumerable<BrowserContextCookiesResult> ea, IEnumerable<BrowserContextCookiesResult> eb)
        {
            var aa = ea.ToList();
            var bb = eb.ToList();
            Assert.AreEqual(aa.Count, bb.Count);
            for (int i = 0; i < aa.Count; ++i)
            {
                var a = aa[i];
                var b = bb[i];
                Assert.AreEqual(a.Name, b.Name);
                Assert.AreEqual(a.Value, b.Value);
                Assert.AreEqual(a.Domain, b.Domain);
                Assert.AreEqual(a.Path, b.Path);
                Assert.AreEqual(a.Expires, b.Expires);
                Assert.AreEqual(a.HttpOnly, b.HttpOnly);
                Assert.AreEqual(a.Secure, b.Secure);
                Assert.AreEqual(a.SameSite, b.SameSite);
            }
        }
    }
}
