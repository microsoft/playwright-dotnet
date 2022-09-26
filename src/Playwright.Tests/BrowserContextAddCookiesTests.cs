/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

public class BrowserContextAddCookiesTests : PageTestEx
{
    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should work")]
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
        Assert.That(await Context.CookiesAsync(), Is.Empty);
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

        await Context.NewPageAsync();
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("cookie=value", cookie);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate cookies in browser contexts")]
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
        Assert.That(cookies, Is.Empty);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate persistent cookies")]
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
        Assert.That(cookies2, Is.Empty);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate send cookie header")]
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
        Assert.That(cookie, Is.Null.Or.Empty);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should isolate cookies between launches")]
    public async Task ShouldIsolateCookiesBetweenLaunches()
    {
        await using (await Playwright[TestConstants.BrowserName].LaunchAsync())
        {
            await Browser.NewContextAsync();

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

        await using (await Playwright[TestConstants.BrowserName].LaunchAsync())
        {
            var context1 = await Browser.NewContextAsync();
            var cookies = await context1.CookiesAsync();
            Assert.That(cookies, Is.Empty);
        }
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set multiple cookies")]
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

        CollectionAssert.AreEqual(
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
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set a cookie with a path")]
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
                    Value = "GRID",
                    SameSite = SameSiteAttribute.Lax,
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
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsWebKit && TestConstants.IsWindows ? SameSiteAttribute.None : SameSiteAttribute.Lax, cookie.SameSite);

        Assert.AreEqual("gridcookie=GRID", await Page.EvaluateAsync<string>("document.cookie"));
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(await Page.EvaluateAsync<string>("document.cookie"), Is.Empty);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        Assert.AreEqual("gridcookie=GRID", await Page.EvaluateAsync<string>("document.cookie"));
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should not set a cookie with blank page URL")]
    public async Task ShouldNotSetACookieWithBlankPageURL()
    {
        await Page.GotoAsync("about:blank");

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
        Assert.IsTrue(cookie.Secure);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should be able to set unsecure cookie for HTTP website")]
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

        Assert.IsFalse(cookie.Secure);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set a cookie on a different domain")]
    public async Task ShouldSetACookieOnADifferentDomain()
    {
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Context.AddCookiesAsync(new[]
        {
                new Cookie
                {
                    Name = "example-cookie",
                    Value = "best",
                    Url = "https://www.example.com",
                    SameSite = SameSiteAttribute.Lax,
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
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsTrue(cookie.Secure);
        Assert.AreEqual(TestConstants.IsWebKit && TestConstants.IsWindows ? SameSiteAttribute.None : SameSiteAttribute.Lax, cookie.SameSite);
    }

    [PlaywrightTest("browsercontext-add-cookies.spec.ts", "should set cookies for a frame")]
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
        bool allowsThirdParty = TestConstants.IsFirefox;
        var cookies = await Context.CookiesAsync(new[] { Server.CrossProcessPrefix + "/grid.html" });

        if (allowsThirdParty)
        {
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies[0];
            Assert.AreEqual("127.0.0.1", cookie.Domain);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.IsFalse(cookie.HttpOnly);
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(SameSiteAttribute.None, cookie.SameSite);
            Assert.IsFalse(cookie.Secure);
            Assert.AreEqual("John Doe", cookie.Value);
        }
        else
        {
            Assert.That(cookies, Is.Empty);
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
