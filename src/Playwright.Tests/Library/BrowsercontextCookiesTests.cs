/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-cookies.spec.ts.</summary>
public class BrowsercontextCookiesTests : UpstreamBrowserTest
{
    [UpstreamTest("CO-001 should return no cookies in pristine browser context")]
    public async Task ShouldReturnNoCookiesInPristineBrowserContext()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        ExpectToEqual(await context.CookiesAsync(), Array.Empty<object>());
    }

    [UpstreamTest("CO-002 should get a cookie @smoke")]
    public async Task ShouldGetACookie()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        var documentCookie = await page.EvaluateAsync<string>(@"() => {
    document.cookie = 'username=John Doe';
    return document.cookie;
  }");
        Assert.That(documentCookie, Is.EqualTo("username=John Doe"));
        ExpectToEqual(await context.CookiesAsync(), new[]
        {
            new
            {
                name = "username",
                value = "John Doe",
                domain = Server.Hostname,
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
                sameSite = DefaultSameSiteCookieValue,
            },
        });
    }

    [UpstreamTest("CO-003 should get a non-session cookie")]
    public async Task ShouldGetANonSessionCookie()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        // @see https://en.wikipedia.org/wiki/Year_2038_problem
        var date = new DateTimeOffset(new DateTime(2038, 1, 1)).ToUnixTimeMilliseconds();
        var documentCookie = await page.EvaluateAsync<string>(@"timestamp => {
    const date = new Date(timestamp);
    document.cookie = `username=John Doe;expires=${date.toUTCString()}`;
    return document.cookie;
  }", date);
        Assert.That(documentCookie, Is.EqualTo("username=John Doe"));
        var cookies = await context.CookiesAsync();
        Assert.That(cookies.Count, Is.EqualTo(1));
        ExpectToEqual(cookies[0], new
        {
            name = "username",
            value = "John Doe",
            domain = Server.Hostname,
            path = "/",
            // We will check this separately.
            expires = Anything,
            httpOnly = false,
            secure = false,
            sameSite = DefaultSameSiteCookieValue,
        });
        // Browsers start to cap cookies with 400 days max expires value.
        // See https://github.com/httpwg/http-extensions/pull/1732
        // Chromium patch: https://chromium.googlesource.com/chromium/src/+/aaa5d2b55478eac2ee642653dcd77a50ac3faff6
        // We want to make sure that expires date is at least 400 days in future.
        const long FOUR_HUNDRED_DAYS = 1000L * 60 * 60 * 24 * 400;
        const long FIVE_MINUTES = 1000 * 60 * 5; // relax condition a bit to make sure test is not flaky.
        Assert.That(cookies[0].Expires, Is.GreaterThan((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + FOUR_HUNDRED_DAYS - FIVE_MINUTES) / 1000d));
    }

    [UpstreamTest("CO-004 should allow adding cookies with >400 days expiration")]
    public async Task ShouldAllowAddingCookiesWith400DaysExpiration()
    {
        var context = await ContextFixture();
        // Browsers start to cap cookies with 400 days max expires value.
        // See https://github.com/httpwg/http-extensions/pull/1732
        // Chromium patch: https://chromium.googlesource.com/chromium/src/+/aaa5d2b55478eac2ee642653dcd77a50ac3faff6
        var expire = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d) + (401 * 24 * 3600);
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "username",
                Value = "John Doe",
                Domain = Server.Hostname,
                Path = "/",
                Expires = (float)expire,
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteAttribute.Lax,
            },
        });

        var cookies = await context.CookiesAsync();
        Assert.That(cookies.Count, Is.EqualTo(1));
        ExpectToEqual(cookies[0], ObjectContaining(new
        {
            name = "username",
            value = "John Doe",
            domain = Server.Hostname,
            path = "/",
        }));
        Assert.That(cookies[0].Expires, Is.GreaterThan(0));
        Assert.That(cookies[0].Expires, Is.LessThanOrEqualTo(expire));
    }

    [UpstreamTest("CO-005 should properly report httpOnly cookie")]
    public async Task ShouldProperlyReportHttpOnlyCookie()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=value;HttpOnly; Path=/");
            res.End();
        });
        await page.GotoAsync(Server.EmptyPage);
        var cookies = await context.CookiesAsync();
        Assert.That(cookies.Count, Is.EqualTo(1));
        Assert.That(cookies[0].HttpOnly, Is.EqualTo(true));
    }

    [UpstreamTest("CO-006 should properly report \"Strict\" sameSite cookie")]
    public async Task ShouldProperlyReportStrictSameSiteCookie()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Info.Fixme(BrowserName == "webkit" && Platform == "win32" && Channel != "webkit-wsl");

        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=value;SameSite=Strict");
            res.End();
        });
        await page.GotoAsync(Server.EmptyPage);
        var cookies = await context.CookiesAsync();
        Assert.That(cookies.Count, Is.EqualTo(1));
        Assert.That(cookies[0].SameSite, Is.EqualTo(SameSiteAttribute.Strict));
    }

    [UpstreamTest("CO-007 should properly report \"Lax\" sameSite cookie")]
    public async Task ShouldProperlyReportLaxSameSiteCookie()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Info.Fixme(BrowserName == "webkit" && Platform == "win32" && Channel != "webkit-wsl");

        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=value;SameSite=Lax");
            res.End();
        });
        await page.GotoAsync(Server.EmptyPage);
        var cookies = await context.CookiesAsync();
        Assert.That(cookies.Count, Is.EqualTo(1));
        Assert.That(cookies[0].SameSite, Is.EqualTo(SameSiteAttribute.Lax));
    }

    [UpstreamTest("CO-008 should get multiple cookies")]
    public async Task ShouldGetMultipleCookies()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        var documentCookie = await page.EvaluateAsync<string>(@"() => {
    document.cookie = 'username=John Doe';
    document.cookie = 'password=1234';
    return document.cookie.split('; ').sort().join('; ');
  }");
        var cookies = new HashSet<BrowserContextCookiesResult>(await context.CookiesAsync());
        Assert.That(documentCookie, Is.EqualTo("password=1234; username=John Doe"));
        ExpectToEqual(cookies, new HashSet<object>([
            new
            {
                name = "password",
                value = "1234",
                domain = Server.Hostname,
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
                sameSite = DefaultSameSiteCookieValue,
            },
            new
            {
                name = "username",
                value = "John Doe",
                domain = Server.Hostname,
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
                sameSite = DefaultSameSiteCookieValue,
            },
        ]));
    }

    [UpstreamTest("CO-009 should get cookies from multiple urls")]
    public async Task ShouldGetCookiesFromMultipleUrls()
    {
        var context = await ContextFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = "https://foo.com",
                Name = "doggo",
                Value = "woofs",
                SameSite = SameSiteAttribute.None,
            },
            new Cookie
            {
                Url = "https://bar.com",
                Name = "catto",
                Value = "purrs",
                SameSite = SameSiteAttribute.Lax,
            },
            new Cookie
            {
                Url = "https://baz.com",
                Name = "birdo",
                Value = "tweets",
                SameSite = SameSiteAttribute.Lax,
            },
        });
        var cookies = new HashSet<BrowserContextCookiesResult>(await context.CookiesAsync(new[] { "https://foo.com", "https://baz.com" }));
        ExpectToEqual(cookies, new HashSet<object>([new
        {
            name = "birdo",
            value = "tweets",
            domain = "baz.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = true,
            sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
        }, new
        {
            name = "doggo",
            value = "woofs",
            domain = "foo.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = true,
            sameSite = "None",
        }]));
    }

    [UpstreamTest("CO-010 should work with subdomain cookie")]
    public async Task ShouldWorkWithSubdomainCookie()
    {
        var context = await ContextFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Domain = ".foo.com",
                Path = "/",
                Name = "doggo",
                Value = "woofs",
                SameSite = SameSiteAttribute.Lax,
                Secure = true,
            },
        });
        ExpectToEqual(await context.CookiesAsync("https://foo.com"), new[]
        {
            new
            {
                name = "doggo",
                value = "woofs",
                domain = ".foo.com",
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = true,
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
            },
        });
        ExpectToEqual(await context.CookiesAsync("https://sub.foo.com"), new[]
        {
            new
            {
                name = "doggo",
                value = "woofs",
                domain = ".foo.com",
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = true,
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
            },
        });
    }

    [UpstreamTest("CO-011 should only return cookies that apply to the url")]
    public async Task ShouldOnlyReturnCookiesThatApplyToTheUrl()
    {
        var context = await ContextFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie { Name = "hostonly", Value = "v", Domain = "foo.com", Path = "/api" },
            new Cookie { Name = "subdomains", Value = "v", Domain = ".foo.com", Path = "/api" },
            new Cookie { Name = "slash", Value = "v", Domain = "foo.com", Path = "/foo/" },
        });
        var names = async (string url) => (await context.CookiesAsync(url)).Select(c => c.Name).Order(StringComparer.Ordinal).ToArray();
        Assert.That(await names("http://foo.com/api"), Is.EqualTo(new[] { "hostonly", "subdomains" }));
        Assert.That(await names("http://foo.com/api/x"), Is.EqualTo(new[] { "hostonly", "subdomains" }));
        Assert.That(await names("http://foo.com/apiv2"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await names("http://sub.foo.com/api"), Is.EqualTo(new[] { "subdomains" }));
        Assert.That(await names("http://notfoo.com/api"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await names("http://foo.com/foo"), Is.EqualTo(Array.Empty<string>()));
        Assert.That(await names("http://foo.com/foo/"), Is.EqualTo(new[] { "slash" }));
        Assert.That(await names("http://foo.com/foo/bar"), Is.EqualTo(new[] { "slash" }));
    }

    [UpstreamTest("CO-012 should return cookies with empty value")]
    public async Task ShouldReturnCookiesWithEmptyValue()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=;Path=/");
            res.End();
        });
        await page.GotoAsync(Server.EmptyPage);
        var cookies = await context.CookiesAsync();
        ExpectToEqual(cookies, new[]
        {
            ObjectContaining(new
            {
                name = "name",
                value = "",
            }),
        });
    }

    [UpstreamTest("CO-013 should return secure cookies based on HTTP(S) protocol")]
    public async Task ShouldReturnSecureCookiesBasedOnHTTPSProtocol()
    {
        var context = await ContextFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = "https://foo.com",
                Name = "doggo",
                Value = "woofs",
                SameSite = SameSiteAttribute.Lax,
                Secure = true,
            },
            new Cookie
            {
                Url = "http://foo.com",
                Name = "catto",
                Value = "purrs",
                SameSite = SameSiteAttribute.Lax,
                Secure = false,
            },
        });
        var cookies = new HashSet<BrowserContextCookiesResult>(await context.CookiesAsync("https://foo.com"));
        ExpectToEqual(cookies, new HashSet<object>([new
        {
            name = "catto",
            value = "purrs",
            domain = "foo.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = false,
            sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
        }, new
        {
            name = "doggo",
            value = "woofs",
            domain = "foo.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = true,
            sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
        }]));
        ExpectToEqual(await context.CookiesAsync("http://foo.com/"), new[]
        {
            new
            {
                name = "catto",
                value = "purrs",
                domain = "foo.com",
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
            },
        });
    }

    [UpstreamTest("CO-014 should return secure cookies for http ${host}")]
    [TestCase("127.0.0.1")]
    [TestCase("[::1]")]
    public async Task ShouldReturnSecureCookiesForHttp(string host)
    {
        var context = await ContextFixture();
        await context.AddCookiesAsync(new[] { new Cookie { Name = "a", Value = "v", Domain = host, Path = "/", Secure = true } });
        Assert.That((await context.CookiesAsync($"http://{host}/")).Select(c => c.Name), Is.EqualTo(new[] { "a" }));
    }

    [UpstreamTest("CO-015 should add cookies with an expiration")]
    [LocalFixme("dotnet: Cookie.Expires is a float: 253402300800 is sent as 253402300416, which is a valid expiration, and AddCookiesAsync does not throw")]
    public async Task ShouldAddCookiesWithAnExpiration()
    {
        var context = await ContextFixture();
        var expires = Math.Floor(DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d) + 3600;
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = "https://foo.com",
                Name = "doggo",
                Value = "woofs",
                SameSite = SameSiteAttribute.None,
                Expires = (float)expires,
            },
        });
        var cookies = await context.CookiesAsync(new[] { "https://foo.com" });
        Assert.That(cookies.Count, Is.EqualTo(1));
        ExpectToEqual(cookies, new[]
        {
            new
            {
                name = "doggo",
                value = "woofs",
                domain = "foo.com",
                path = "/",
                expires,
                httpOnly = false,
                secure = true,
                sameSite = "None",
            },
        });
        {
            // Rollover to 5-digit year
            await context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = "https://foo.com",
                    Name = "doggo",
                    Value = "woofs",
                    SameSite = SameSiteAttribute.None,
                    Expires = 253402300799, // Fri, 31 Dec 9999 23:59:59 +0000 (UTC)
                },
            });
            Assert.That((await ThrowsAsync<PlaywrightException>(() => context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = "https://foo.com",
                    Name = "doggo",
                    Value = "woofs",
                    SameSite = SameSiteAttribute.None,
                    Expires = 253402300800, // Sat,  1 Jan 1000 00:00:00 +0000 (UTC)
                },
            }))).Message, Does.Match("Cookie should have a valid expires"));
        }

        Assert.That((await ThrowsAsync<PlaywrightException>(() => context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = "https://foo.com",
                Name = "doggo",
                Value = "woofs",
                SameSite = SameSiteAttribute.None,
                Expires = -42,
            },
        }))).Message, Does.Match("Cookie should have a valid expires"));
    }

    [UpstreamTest("CO-016 should support requestStorageAccess")]
    public async Task ShouldSupportRequestStorageAccess()
    {
        var page = await PageFixture();
        Info.Skip(BrowserName == "chromium", "requestStorageAccess API is not available in Chromium");

        Server.SetRoute("/set-cookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "name=value; Path=/");
            res.End();
        });
        // Navigate once to the domain as top level.
        await page.GotoAsync(Server.CrossProcessPrefix + "/set-cookie.html");
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync($"<iframe src=\"{Server.CrossProcessPrefix + "/empty.html"}\"></iframe>");

        var frame = page.Frames[1];
        if (BrowserName == "firefox")
        {
            Assert.That(await frame.EvaluateAsync<bool>("() => document.hasStorageAccess()"), Is.True);
            {
                var (serverRequest, _) = await PromiseAll(
                    Server.WaitForRequest("/title.html"),
                    frame.EvaluateAsync("() => fetch('/title.html')"));
                Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("name=value"));
            }
        }
        else
        {
            if (BrowserName == "webkit" && (IsLinux || Channel == "webkit-wsl"))
            {
                Assert.That(await frame.EvaluateAsync<bool>("() => document.hasStorageAccess()"), Is.True);
            }
            else
            {
                Assert.That(await frame.EvaluateAsync<bool>("() => document.hasStorageAccess()"), Is.False);
            }
            {
                var (serverRequest, _) = await PromiseAll(
                    Server.WaitForRequest("/title.html"),
                    frame.EvaluateAsync("() => fetch('/title.html')"));
                if (IsWindows && BrowserName == "webkit" && Channel != "webkit-wsl")
                {
                    Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("name=value"));
                }
                else
                {
                    Assert.That(serverRequest.Headers["cookie"], Is.Null.Or.Empty);
                }
            }
            Assert.That(await frame.EvaluateAsync<bool>("() => document.requestStorageAccess().then(() => true, e => false)"), Is.True);
            Assert.That(await frame.EvaluateAsync<bool>("() => document.hasStorageAccess()"), Is.True);
            {
                var (serverRequest, _) = await PromiseAll(
                    Server.WaitForRequest("/title.html"),
                    frame.EvaluateAsync("() => fetch('/title.html')"));
                if (BrowserName == "webkit" && (IsLinux || Channel == "webkit-wsl"))
                {
                    Assert.That(serverRequest.Headers["cookie"], Is.Null);
                }
                else
                {
                    Assert.That(serverRequest.Headers["cookie"], Is.EqualTo("name=value"));
                }
            }
        }
    }

    [UpstreamTest("CO-017 should parse cookie with large Max-Age correctly")]
    public async Task ShouldParseCookieWithLargeMaxAgeCorrectly()
    {
        var page = await PageFixture();
        Server.SetRoute("/foobar", (req, res) =>
        {
            res.SetHeader("set-cookie", new[]
            {
                "cookie1=value1; Path=/; Expires=Thu, 08 Sep 2270 15:06:12 GMT; Max-Age=7776000000",
            });
            res.StatusCode = 200;
            res.End();
        });
        await page.GotoAsync(Server.Prefix + "/foobar");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("cookie1=value1"));
        ExpectToEqual(await page.Context.CookiesAsync(), new[]
        {
            new
            {
                name = "cookie1",
                value = "value1",
                domain = Server.Hostname,
                path = "/",
                expires = AnyNumber,
                httpOnly = false,
                secure = false,
                sameSite = DefaultSameSiteCookieValue,
            },
        });
    }

    [UpstreamTest("CO-018 iframe should inherit cookies from parent")]
    public async Task IframeShouldInheritCookiesFromParent()
    {
        var page = await PageFixture();
        await page.RouteAsync("**/*", async route =>
        {
            if (route.Request.Url.Contains("sub.example.test"))
            {
                await route.FulfillAsync(new()
                {
                    Body = @"
            <p id=""result""></p>
            <script>document.getElementById('result').textContent = document.cookie || 'no cookies';</script>
        ",
                    ContentType = "text/html",
                });
                return;
            }
            await route.FulfillAsync(new()
            {
                Headers = new Dictionary<string, string> { ["set-cookie"] = "testCookie=value; SameSite=Lax; Domain=example.test" },
                ContentType = "text/html",
                Body = @"
          <p id=""result""></p>
          <script>document.getElementById('result').textContent = document.cookie || 'no cookies';</script>
          <iframe src=""http://sub.example.test""></iframe>
      ",
            });
        });

        await page.GotoAsync("http://example.test");

        await Expect(page.Locator("body")).ToContainTextAsync("testCookie=value");
        await Expect(page.FrameLocator("iframe").Locator("body")).ToContainTextAsync("testCookie=value");
    }
}
