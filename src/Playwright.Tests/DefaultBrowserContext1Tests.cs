/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using System.Net;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class DefaultBrowserContext1Tests : PlaywrightTestEx
{
    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.cookies() should work")]
    public async Task ContextCookiesShouldWork()
    {
        var (tmp, context, page) = await LaunchPersistentAsync();

        await page.GotoAsync(Server.EmptyPage);

        string documentCookie = await page.EvaluateAsync<string>(@"() => {
              document.cookie = 'username=John Doe';
              return document.cookie;
            }");

        Assert.AreEqual("username=John Doe", documentCookie);
        var cookie = (await page.Context.CookiesAsync()).Single();
        Assert.AreEqual("username", cookie.Name);
        Assert.AreEqual("John Doe", cookie.Value);
        Assert.AreEqual("localhost", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(-1, cookie.Expires);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.addCookies() should work")]
    public async Task ContextAddCookiesShouldWork()
    {
        var (tmp, context, page) = await LaunchPersistentAsync();

        await page.GotoAsync(Server.EmptyPage);
        await context.AddCookiesAsync(new[]
        {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "username",
                    Value = "John Doe",
                }
            });

        Assert.AreEqual("username=John Doe", await page.EvaluateAsync<string>(@"() => document.cookie"));

        var cookie = (await page.Context.CookiesAsync()).Single();
        Assert.AreEqual("username", cookie.Name);
        Assert.AreEqual("John Doe", cookie.Value);
        Assert.AreEqual("localhost", cookie.Domain);
        Assert.AreEqual("/", cookie.Path);
        Assert.AreEqual(-1, cookie.Expires);
        Assert.IsFalse(cookie.HttpOnly);
        Assert.IsFalse(cookie.Secure);
        Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "context.clearCookies() should work")]
    public async Task ContextClearCookiesShouldWork()
    {
        var (tmp, context, page) = await LaunchPersistentAsync();

        await page.GotoAsync(Server.EmptyPage);
        await context.AddCookiesAsync(new[]
        {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "cookie1",
                    Value = "1",
                },
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "cookie2",
                    Value = "2",
                },
            });

        Assert.AreEqual("cookie1=1; cookie2=2", await page.EvaluateAsync<string>(@"() => document.cookie"));

        await context.ClearCookiesAsync();
        await page.ReloadAsync();
        Assert.That(await page.Context.CookiesAsync(), Is.Empty);
        Assert.That(await page.EvaluateAsync<string>(@"() => document.cookie"), Is.Empty);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should(not) block third party cookies")]
    public async Task ShouldNotBlockThirdPartyCookies()
    {
        var (tmp, context, page) = await LaunchPersistentAsync();

        await page.GotoAsync(Server.EmptyPage);
        await page.EvaluateAsync(@"src => {
                  let fulfill;
                  const promise = new Promise(x => fulfill = x);
                  const iframe = document.createElement('iframe');
                  document.body.appendChild(iframe);
                  iframe.onload = fulfill;
                  iframe.src = src;
                  return promise;
                }", Server.CrossProcessPrefix + "/grid.html");

        await page.FirstChildFrame().EvaluateAsync<string>("document.cookie = 'username=John Doe'");
        await page.WaitForTimeoutAsync(2000);
        bool allowsThirdParty = TestConstants.IsFirefox;
        var cookies = await context.CookiesAsync(new[] { Server.CrossProcessPrefix + "/grid.html" });

        if (allowsThirdParty)
        {
            Assert.That(cookies, Has.Count.EqualTo(1));
            var cookie = cookies.First();
            Assert.AreEqual("127.0.0.1", cookie.Domain);
            Assert.AreEqual(cookie.Expires, -1);
            Assert.IsFalse(cookie.HttpOnly);
            Assert.AreEqual("username", cookie.Name);
            Assert.AreEqual("/", cookie.Path);
            Assert.AreEqual(TestConstants.IsChromium ? SameSiteAttribute.Lax : SameSiteAttribute.None, cookie.SameSite);
            Assert.IsFalse(cookie.Secure);
            Assert.AreEqual("John Doe", cookie.Value);
        }
        else
        {
            Assert.That(cookies, Is.Empty);
        }

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support viewport option")]
    public async Task ShouldSupportViewportOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync(new()
        {
            ViewportSize = new()
            {
                Width = 456,
                Height = 789
            }
        });

        await TestUtils.VerifyViewportAsync(page, 456, 789);
        page = await context.NewPageAsync();
        await TestUtils.VerifyViewportAsync(page, 456, 789);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support deviceScaleFactor option")]
    public async Task ShouldSupportDeviceScaleFactorOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync(new()
        {
            DeviceScaleFactor = 3
        });

        Assert.AreEqual(3, await page.EvaluateAsync<int>("window.devicePixelRatio"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support userAgent option")]
    public async Task ShouldSupportUserAgentOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync(new()
        {
            UserAgent = "foobar"
        });

        string userAgent = string.Empty;

        await TaskUtils.WhenAll(
            Server.WaitForRequest("/empty.html", r => userAgent = r.Headers["user-agent"]),
            page.GotoAsync(Server.EmptyPage));

        Assert.AreEqual("foobar", userAgent);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support bypassCSP option")]
    public async Task ShouldSupportBypassCSPOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync(new()
        {
            BypassCSP = true
        });

        await page.GotoAsync(Server.Prefix + "/csp.html");
        await page.AddScriptTagAsync(new() { Content = "window.__injected = 42;" });
        Assert.AreEqual(42, await page.EvaluateAsync<int>("window.__injected"));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support javascriptEnabled option")]
    public async Task ShouldSupportJavascriptEnabledOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync(new()
        {
            JavaScriptEnabled = false
        });

        await page.GotoAsync("data:text/html, <script>var something = \"forbidden\"</script>");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.EvaluateAsync("something"));

        if (TestConstants.IsWebKit)
        {
            StringAssert.Contains("Can't find variable: something", exception.Message);
        }
        else
        {
            StringAssert.Contains("something is not defined", exception.Message);
        }

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support httpCredentials option")]
    public async Task ShouldSupportHttpCredentialsOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass",
            }
        });

        Server.SetAuth("/playground.html", "user", "pass");
        var response = await page.GotoAsync(Server.Prefix + "/playground.html");
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support offline option")]
    public async Task ShouldSupportOfflineOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync(new()
        {
            Offline = true
        });

        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync(Server.EmptyPage));

        await context.DisposeAsync();
        tmp.Dispose();
    }

    [PlaywrightTest("defaultbrowsercontext-1.spec.ts", "should support acceptDownloads option")]
    public async Task ShouldSupportAcceptDownloadsOption()
    {
        var (tmp, context, page) = await LaunchPersistentAsync();
        Server.SetRoute("/download", context =>
        {
            context.Response.Headers["Content-Type"] = "application/octet-stream";
            context.Response.Headers["Content-Disposition"] = "attachment";
            return context.Response.WriteAsync("Hello world");
        });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var downloadTask = page.WaitForDownloadAsync();

        await TaskUtils.WhenAll(
            downloadTask,
            page.ClickAsync("a"));
        var download = downloadTask.Result;
        var path = await download.PathAsync();
        Assert.AreEqual(File.Exists(path), true);
        Assert.AreEqual(File.ReadAllText(path), "Hello world");
    }

    private async Task<(TempDirectory tmp, IBrowserContext context, IPage page)> LaunchPersistentAsync(BrowserTypeLaunchPersistentContextOptions options = null)
    {
        var tmp = new TempDirectory();
        var context = await BrowserType.LaunchPersistentContextAsync(
            tmp.Path,
            options: options);
        var page = context.Pages.First();

        return (tmp, context, page);
    }
}
