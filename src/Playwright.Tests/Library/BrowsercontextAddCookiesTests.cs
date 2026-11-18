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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-add-cookies.spec.ts.</summary>
public class BrowsercontextAddCookiesTests : UpstreamBrowserTest
{
    [UpstreamTest("CO-019 should work @smoke")]
    public async Task ShouldWork()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = Server.EmptyPage,
                Name = "password",
                Value = "123456",
            },
        });
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("password=123456"));
    }

    [UpstreamTest("CO-020 should work with expires=-1")]
    public async Task ShouldWorkWithExpires1()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "username",
                Value = "John Doe",
                Domain = "www.example.com",
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteAttribute.Lax,
            },
        });
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
        });
        await page.GotoAsync("https://www.example.com");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("username=John Doe"));
    }

    [UpstreamTest("CO-021 should add cookies with empty value")]
    public async Task ShouldAddCookiesWithEmptyValue()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "marker",
                Value = "",
                Domain = "www.example.com",
                Path = "/",
                Expires = -1,
                HttpOnly = false,
                Secure = false,
                SameSite = SameSiteAttribute.Lax,
            },
        });
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new() { Body = "<html></html>" }));
        });
        await page.GotoAsync("https://www.example.com");
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("marker="));
    }

    [UpstreamTest("CO-022 should set cookies with SameSite attribute and no secure attribute")]
    public async Task ShouldSetCookiesWithSameSiteAttributeAndNoSecureAttribute()
    {
        var context = await ContextFixture();
        // Use domain instead of URL to ensure that the `secure` attribute is not set.
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Domain = "foo.com",
                Path = "/",
                Name = "same-site-unset",
                Value = "1",
            },
            new Cookie
            {
                Domain = "foo.com",
                Path = "/",
                Name = "same-site-none",
                Value = "1",
                SameSite = SameSiteAttribute.None,
            },
            new Cookie
            {
                Domain = "foo.com",
                Path = "/",
                Name = "same-site-lax",
                Value = "1",
                SameSite = SameSiteAttribute.Lax,
            },
            new Cookie
            {
                Domain = "foo.com",
                Path = "/",
                Name = "same-site-strict",
                Value = "1",
                SameSite = SameSiteAttribute.Strict,
            },
        });
        var cookies = new HashSet<BrowserContextCookiesResult>(await context.CookiesAsync(new[] { "https://foo.com" }));
        ExpectToEqual(cookies, new HashSet<object>([new
        {
            name = "same-site-unset",
            value = "1",
            domain = "foo.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = false,
            sameSite = DefaultSameSiteCookieValue,
        }, .. (BrowserName == "chromium" || (BrowserName == "webkit" && (IsLinux || Channel == "webkit-wsl")) || IsBidi ? Array.Empty<object>() : [new
        {
            name = "same-site-none",
            value = "1",
            domain = "foo.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = false,
            sameSite = "None",
        }]), new
        {
            name = "same-site-lax",
            value = "1",
            domain = "foo.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = false,
            sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
        }, new
        {
            name = "same-site-strict",
            value = "1",
            domain = "foo.com",
            path = "/",
            expires = -1,
            httpOnly = false,
            secure = false,
            sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Strict",
        }]));
    }

    [UpstreamTest("CO-023 should roundtrip cookie")]
    public async Task ShouldRoundtripCookie()
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
        await context.ClearCookiesAsync();
        ExpectToEqual(await context.CookiesAsync(), Array.Empty<object>());
        // .NET: CookiesAsync gives BrowserContextCookiesResult objects, and AddCookiesAsync takes Cookie objects.
        await context.AddCookiesAsync(cookies.Select(c => new Cookie { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path, Expires = c.Expires, HttpOnly = c.HttpOnly, Secure = c.Secure, SameSite = c.SameSite, PartitionKey = c.PartitionKey }));
        // Slightly different rounding on chromium win.
        var normalizedExpires = (IReadOnlyList<BrowserContextCookiesResult> cookies) => cookies.Select(c => new BrowserContextCookiesResult { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path, Expires = MathF.Floor(c.Expires), HttpOnly = c.HttpOnly, Secure = c.Secure, SameSite = c.SameSite, PartitionKey = c.PartitionKey });
        ExpectToEqual(normalizedExpires(await context.CookiesAsync()), normalizedExpires(cookies));
    }

    [UpstreamTest("CO-024 should send cookie header")]
    public async Task ShouldSendCookieHeader()
    {
        var context = await ContextFixture();
        var cookie = "";
        Server.SetRoute("/empty.html", (req, res) =>
        {
            cookie = req.Headers["cookie"];
            res.End();
        });
        await context.AddCookiesAsync(new[] { new Cookie { Url = Server.EmptyPage, Name = "cookie", Value = "value" } });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(cookie, Is.EqualTo("cookie=value"));
    }

    [UpstreamTest("CO-025 should isolate cookies in browser contexts")]
    public async Task ShouldIsolateCookiesInBrowserContexts()
    {
        var context = await ContextFixture();
        var anotherContext = await Browser.NewContextAsync(ContextOptions());
        await context.AddCookiesAsync(new[] { new Cookie { Url = Server.EmptyPage, Name = "isolatecookie", Value = "page1value" } });
        await anotherContext.AddCookiesAsync(new[] { new Cookie { Url = Server.EmptyPage, Name = "isolatecookie", Value = "page2value" } });

        var cookies1 = await context.CookiesAsync();
        var cookies2 = await anotherContext.CookiesAsync();
        Assert.That(cookies1.Count, Is.EqualTo(1));
        Assert.That(cookies2.Count, Is.EqualTo(1));
        Assert.That(cookies1[0].Name, Is.EqualTo("isolatecookie"));
        Assert.That(cookies1[0].Value, Is.EqualTo("page1value"));
        Assert.That(cookies2[0].Name, Is.EqualTo("isolatecookie"));
        Assert.That(cookies2[0].Value, Is.EqualTo("page2value"));
        await anotherContext.CloseAsync();
    }

    [UpstreamTest("CO-026 should isolate session cookies")]
    public async Task ShouldIsolateSessionCookies()
    {
        var context = await ContextFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "session=value");
            res.End();
        });
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/setcookie.html");
        }
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var cookies = await context.CookiesAsync();
            Assert.That(cookies.Count, Is.EqualTo(1));
            Assert.That(string.Join(",", cookies.Select(c => c.Value)), Is.EqualTo("value"));
        }
        {
            var context2 = await Browser.NewContextAsync(ContextOptions());
            var page = await context2.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            var cookies = await context2.CookiesAsync();
            // .NET: cookies[0] of an empty list throws, in JavaScript it is undefined.
            Assert.That(cookies.ElementAtOrDefault(0)?.Name, Is.Null);
            await context2.CloseAsync();
        }
    }

    [UpstreamTest("CO-027 should isolate persistent cookies")]
    public async Task ShouldIsolatePersistentCookies()
    {
        var context = await ContextFixture();
        Server.SetRoute("/setcookie.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", "persistent=persistent-value; max-age=3600");
            res.End();
        });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/setcookie.html");

        var context1 = context;
        var context2 = await Browser.NewContextAsync(ContextOptions());
        var (page1, page2) = await PromiseAll(context1.NewPageAsync(), context2.NewPageAsync());
        await PromiseAll(page1.GotoAsync(Server.EmptyPage), page2.GotoAsync(Server.EmptyPage));
        var (cookies1, cookies2) = await PromiseAll(context1.CookiesAsync(), context2.CookiesAsync());
        Assert.That(cookies1.Count, Is.EqualTo(1));
        Assert.That(cookies1[0].Name, Is.EqualTo("persistent"));
        Assert.That(cookies1[0].Value, Is.EqualTo("persistent-value"));
        Assert.That(cookies2.Count, Is.EqualTo(0));
        await context2.CloseAsync();
    }

    [UpstreamTest("CO-028 should isolate send cookie header")]
    public async Task ShouldIsolateSendCookieHeader()
    {
        var context = await ContextFixture();
        var cookie = "";
        Server.SetRoute("/empty.html", (req, res) =>
        {
            cookie = req.Headers["cookie"] ?? "";
            res.End();
        });
        await context.AddCookiesAsync(new[] { new Cookie { Url = Server.EmptyPage, Name = "sendcookie", Value = "value" } });
        {
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.That(cookie, Is.EqualTo("sendcookie=value"));
        }
        {
            // .NET: a block can not declare a variable with the name of a parameter: context2 is the context of upstream.
            var context2 = await Browser.NewContextAsync(ContextOptions());
            var page = await context2.NewPageAsync();
            await page.GotoAsync(Server.EmptyPage);
            Assert.That(cookie, Is.EqualTo(""));
            await context2.CloseAsync();
        }
    }

    [UpstreamTest("CO-029 should isolate cookies between launches")]
    public async Task ShouldIsolateCookiesBetweenLaunches()
    {
        Info.Slow();

        var browser1 = await BrowserType.LaunchAsync(LaunchOptions());
        var context1 = await browser1.NewContextAsync(ContextOptions());
        await context1.AddCookiesAsync(new[] { new Cookie { Url = Server.EmptyPage, Name = "cookie-in-context-1", Value = "value", Expires = (float)((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000d) + 10000) } });
        await browser1.CloseAsync();

        var browser2 = await BrowserType.LaunchAsync(LaunchOptions());
        var context2 = await browser2.NewContextAsync(ContextOptions());
        var cookies = await context2.CookiesAsync();
        Assert.That(cookies.Count, Is.EqualTo(0));
        await browser2.CloseAsync();
    }

    [UpstreamTest("CO-030 should set multiple cookies")]
    public async Task ShouldSetMultipleCookies()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = Server.EmptyPage,
                Name = "multiple-1",
                Value = "123456",
            },
            new Cookie
            {
                Url = Server.EmptyPage,
                Name = "multiple-2",
                Value = "bar",
            },
        });
        Assert.That(await page.EvaluateAsync<string[]>(@"() => {
    const cookies = document.cookie.split(';');
    return cookies.map(cookie => cookie.trim()).sort();
  }"), Is.EqualTo(new[]
        {
            "multiple-1=123456",
            "multiple-2=bar",
        }));
    }

    [UpstreamTest("CO-031 should have |expires| set to |-1| for session cookies")]
    public async Task ShouldHaveExpiresSetTo1ForSessionCookies()
    {
        var context = await ContextFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = Server.EmptyPage,
                Name = "expires",
                Value = "123456",
            },
        });
        var cookies = await context.CookiesAsync();
        Assert.That(cookies[0].Expires, Is.EqualTo(-1));
    }

    [UpstreamTest("CO-032 should set cookie with reasonable defaults")]
    public async Task ShouldSetCookieWithReasonableDefaults()
    {
        var context = await ContextFixture();
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = Server.EmptyPage,
                Name = "defaults",
                Value = "123456",
            },
        });
        var cookies = await context.CookiesAsync();
        ExpectToEqual(cookies.OrderBy(a => a.Name, StringComparer.InvariantCulture), new[]
        {
            new
            {
                name = "defaults",
                value = "123456",
                domain = Server.Hostname,
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = false,
                sameSite = DefaultSameSiteCookieValue,
            },
        });
    }

    [UpstreamTest("CO-033 should set a cookie with a path")]
    public async Task ShouldSetACookieWithAPath()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.Prefix + "/grid.html");
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Domain = Server.Hostname,
                Path = "/grid.html",
                Name = "gridcookie",
                Value = "GRID",
                SameSite = SameSiteAttribute.Lax,
            },
        });
        ExpectToEqual(await context.CookiesAsync(), new[]
        {
            new
            {
                name = "gridcookie",
                value = "GRID",
                domain = Server.Hostname,
                path = "/grid.html",
                expires = -1,
                httpOnly = false,
                secure = false,
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
            },
        });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("gridcookie=GRID"));
        await page.GotoAsync(Server.EmptyPage);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(""));
        await page.GotoAsync(Server.Prefix + "/grid.html");
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("gridcookie=GRID"));
    }

    [UpstreamTest("CO-034 should not set a cookie with blank page URL")]
    public async Task ShouldNotSetACookieWithBlankPageURL()
    {
        var context = await ContextFixture();
        Exception error = null;
        try
        {
            await context.AddCookiesAsync(new[]
            {
                new Cookie { Url = Server.EmptyPage, Name = "example-cookie", Value = "best" },
                new Cookie { Url = "about:blank", Name = "example-cookie-blank", Value = "best" },
            });
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain(
            "Blank page can not have cookie \"example-cookie-blank\""));
    }

    [UpstreamTest("CO-035 should not set a cookie on a data URL page")]
    public async Task ShouldNotSetACookieOnADataURLPage()
    {
        var context = await ContextFixture();
        Exception error = null;
        try
        {
            await context.AddCookiesAsync(new[] { new Cookie { Url = "data:,Hello%2C%20World!", Name = "example-cookie", Value = "best" } });
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain("Data URL page can not have cookie \"example-cookie\""));
    }

    [UpstreamTest("CO-036 should default to setting secure cookie for HTTPS websites")]
    public async Task ShouldDefaultToSettingSecureCookieForHTTPSWebsites()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        const string SECURE_URL = "https://example.com";
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = SECURE_URL,
                Name = "foo",
                Value = "bar",
            },
        });
        var cookie = (await context.CookiesAsync(SECURE_URL))[0];
        Assert.That(cookie.Secure, Is.EqualTo(true));
    }

    [UpstreamTest("CO-037 should be able to set unsecure cookie for HTTP website")]
    public async Task ShouldBeAbleToSetUnsecureCookieForHTTPWebsite()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        const string HTTP_URL = "http://example.com";
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = HTTP_URL,
                Name = "foo",
                Value = "bar",
            },
        });
        var cookie = (await context.CookiesAsync(HTTP_URL))[0];
        Assert.That(cookie.Secure, Is.EqualTo(false));
    }

    [UpstreamTest("CO-038 should set a cookie on a different domain")]
    public async Task ShouldSetACookieOnADifferentDomain()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = "https://www.example.com",
                Name = "example-cookie",
                Value = "best",
                SameSite = SameSiteAttribute.Lax,
            },
        });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(""));
        ExpectToEqual(await context.CookiesAsync("https://www.example.com"), new[]
        {
            new
            {
                name = "example-cookie",
                value = "best",
                domain = "www.example.com",
                path = "/",
                expires = -1,
                httpOnly = false,
                secure = true,
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
            },
        });
    }

    [UpstreamTest("CO-039 should set cookies for a frame")]
    public async Task ShouldSetCookiesForAFrame()
    {
        var context = await ContextFixture();
        var page = await PageFixture();
        await page.GotoAsync(Server.EmptyPage);
        await context.AddCookiesAsync(new[]
        {
            new Cookie { Url = Server.Prefix, Name = "frame-cookie", Value = "value" },
        });
        await page.EvaluateAsync(@"src => {
    let fulfill;
    const promise = new Promise(x => fulfill = x);
    const iframe = document.createElement('iframe');
    document.body.appendChild(iframe);
    iframe.onload = fulfill;
    iframe.src = src;
    return promise;
  }", Server.Prefix + "/grid.html");

        Assert.That(await page.Frames[1].EvaluateAsync<string>("document.cookie"), Is.EqualTo("frame-cookie=value"));
    }

    [UpstreamTest("CO-040 should allow unnamed cookies")]
    public async Task ShouldAllowUnnamedCookies()
    {
        var page = await PageFixture();
        var context = await ContextFixture();
        Server.SetRoute("/cookies", (req, res) =>
        {
            res.Write(req.Headers["cookie"] ?? "undefined-on-server");
            res.End();
        });
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = Server.EmptyPage,
                Name = "",
                Value = "unnamed-via-add-cookies",
            },
        });
        // Round-trip behavior
        var resp = await page.GotoAsync(Server.Prefix + "/cookies");
        if ((BrowserName == "webkit" && Platform == "darwin") || (BrowserName == "firefox" && IsBidi))
        {
            Assert.That(await resp.TextAsync(), Is.EqualTo("undefined-on-server"));
            Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(""));
        }
        else
        {
            Assert.That(await resp.TextAsync(), Is.EqualTo("unnamed-via-add-cookies"));
            Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("unnamed-via-add-cookies"));
        }
        // Within PW behavior
        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync("() => document.cookie = '=unnamed-via-js;'");
        // .NET: CookiesAsync gives BrowserContextCookiesResult objects, and AddCookiesAsync takes Cookie objects.
        await context.AddCookiesAsync((await context.CookiesAsync()).Select(c => new Cookie { Name = c.Name, Value = c.Value, Domain = c.Domain, Path = c.Path, Expires = c.Expires, HttpOnly = c.HttpOnly, Secure = c.Secure, SameSite = c.SameSite, PartitionKey = c.PartitionKey }));
        if ((BrowserName == "webkit" && Platform == "darwin") || (BrowserName == "firefox" && IsBidi))
        {
            Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(""));
        }
        else
        {
            Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("unnamed-via-js"));
        }
    }

    [UpstreamTest("CO-041 should set secure cookies on secure WebSocket")]
    public async Task ShouldSetSecureCookiesOnSecureWebSocket()
    {
        var receivedWebSocketHeaders = new TaskCompletionSource<ServerHeaders>(TaskCreationOptions.RunContinuationsAsynchronously);
        HttpsServer.OnceWebSocketConnection((ws, req) => receivedWebSocketHeaders.TrySetResult(req.Headers));
        var context = await ContextFactory(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(HttpsServer.EmptyPage);
        await context.AddCookiesAsync(new[] { new Cookie { Domain = new Uri(HttpsServer.Prefix).Host, Path = "/", Name = "foo", Value = "bar", Secure = true } });
        await page.EvaluateAsync("hostname => new WebSocket(`wss://${hostname}/ws`)", HttpsServer.Host);
        var headers = await receivedWebSocketHeaders.Task;
        Assert.That(headers["cookie"], Is.EqualTo("foo=bar"));
    }
}
