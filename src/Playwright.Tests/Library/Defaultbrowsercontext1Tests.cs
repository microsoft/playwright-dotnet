/**
 * Copyright 2017 Google Inc. All rights reserved.
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

using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/defaultbrowsercontext-1.spec.ts.</summary>
public class Defaultbrowsercontext1Tests : UpstreamBrowserTest
{
    private static IReadOnlyList<BrowserContextCookiesResult> MaybeFilterCookies(string channel, IReadOnlyList<BrowserContextCookiesResult> cookies)
    {
        if (channel?.StartsWith("msedge", StringComparison.Ordinal) == true)
        {
            return cookies.Where(c => !c.Domain.EndsWith("microsoft.com", StringComparison.Ordinal)).ToList();
        }
        return cookies;
    }

    [UpstreamTest("DC-001 context.cookies() should work @smoke")]
    public async Task ContextCookiesShouldWork()
    {
        var (_, page) = await LaunchPersistent();
        await page.GotoAsync(Server.EmptyPage);
        var documentCookie = await page.EvaluateAsync<string>(@"() => {
    document.cookie = 'username=John Doe';
    return document.cookie;
  }");
        Assert.That(documentCookie, Is.EqualTo("username=John Doe"));
        ExpectToEqual(MaybeFilterCookies(Channel, await page.Context.CookiesAsync()), new[]
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

    [UpstreamTest("DC-002 context.addCookies() should work")]
    public async Task ContextAddCookiesShouldWork()
    {
        var (_, page) = await LaunchPersistent();
        await page.GotoAsync(Server.EmptyPage);
        await page.Context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = Server.EmptyPage,
                Name = "username",
                Value = "John Doe",
                SameSite = SameSiteAttribute.Lax,
            },
        });
        Assert.That(await page.EvaluateAsync<string>("() => document.cookie"), Is.EqualTo("username=John Doe"));
        ExpectToEqual(MaybeFilterCookies(Channel, await page.Context.CookiesAsync()), new[]
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
                sameSite = (BrowserName == "webkit" && IsWindows && Channel != "webkit-wsl") ? "None" : "Lax",
            },
        });
    }

    [UpstreamTest("DC-003 context.clearCookies() should work")]
    public async Task ContextClearCookiesShouldWork()
    {
        var (_, page) = await LaunchPersistent();
        await page.GotoAsync(Server.EmptyPage);
        await page.Context.AddCookiesAsync(new[]
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
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1; cookie2=2"));
        await page.Context.ClearCookiesAsync();
        await page.ReloadAsync();
        ExpectToEqual(MaybeFilterCookies(Channel, await page.Context.CookiesAsync(Array.Empty<string>())), Array.Empty<object>());
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(string.Empty));
    }

    [UpstreamTest("DC-004 should support viewport option")]
    public async Task ShouldSupportViewportOption()
    {
        var (context, page) = await LaunchPersistent(new() { ViewportSize = new() { Width = 456, Height = 789 } });
        await VerifyViewport(page, 456, 789);
        var page2 = await context.NewPageAsync();
        await VerifyViewport(page2, 456, 789);
    }

    [UpstreamTest("DC-005 should support deviceScaleFactor option")]
    public async Task ShouldSupportDeviceScaleFactorOption()
    {
        var (_, page) = await LaunchPersistent(new() { DeviceScaleFactor = 3 });
        Assert.That(await page.EvaluateAsync<int>("window.devicePixelRatio"), Is.EqualTo(3));
    }

    [UpstreamTest("DC-006 should support userAgent option")]
    public async Task ShouldSupportUserAgentOption()
    {
        var (_, page) = await LaunchPersistent(new() { UserAgent = "foobar" });
        Assert.That(await page.EvaluateAsync<string>("() => navigator.userAgent"), Is.EqualTo("foobar"));
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.GotoAsync(Server.EmptyPage));
        Assert.That(request.Headers["user-agent"], Is.EqualTo("foobar"));
    }

    [UpstreamTest("DC-007 should support bypassCSP option")]
    public async Task ShouldSupportBypassCSPOption()
    {
        var (_, page) = await LaunchPersistent(new() { BypassCSP = true });
        await page.GotoAsync(Server.Prefix + "/csp.html");
        await page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" });
        Assert.That(await page.EvaluateAsync<int>("__injected"), Is.EqualTo(42));
        Assert.That(await page.EvaluateAsync<int>("window[\"__inlineScriptValue\"]"), Is.EqualTo(42));
    }

    [UpstreamTest("DC-008 should support javascriptEnabled option")]
    public async Task ShouldSupportJavascriptEnabledOption()
    {
        var (_, page) = await LaunchPersistent(new() { JavaScriptEnabled = false });
        await page.GotoAsync("data:text/html, <script>var something = \"forbidden\"</script>");
        Exception error = null;
        error = await Catch(() => page.EvaluateAsync("something"));
        if (BrowserName == "webkit")
        {
            Assert.That(error.Message, Does.Contain("Can't find variable: something"));
        }
        else
        {
            Assert.That(error.Message, Does.Contain("something is not defined"));
        }
    }

    [UpstreamTest("DC-009 should support httpCredentials option")]
    public async Task ShouldSupportHttpCredentialsOption()
    {
        var (_, page) = await LaunchPersistent(new() { HttpCredentials = new() { Username = "user", Password = "pass" } });
        Server.SetAuth("/playground.html", "user", "pass");
        var response = await page.GotoAsync(Server.Prefix + "/playground.html");
        Assert.That(response.Status, Is.EqualTo(200));
    }

    [UpstreamTest("DC-010 should support offline option")]
    public async Task ShouldSupportOfflineOption()
    {
        var (_, page) = await LaunchPersistent(new() { Offline = true });
        var error = await Catch(() => page.GotoAsync(Server.EmptyPage));
        Assert.That(error, Is.Not.Null);
    }

    [UpstreamTest("DC-011 should support acceptDownloads option")]
    public async Task ShouldSupportAcceptDownloadsOption()
    {
        Info.Skip(Mode != "default", "download.path() is not available in remote mode");

        var (_, page) = await LaunchPersistent();
        Server.SetRoute("/download", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            res.SetHeader("Content-Disposition", "attachment");
            res.End("Hello world");
        });
        await page.SetContentAsync($"<a href=\"{Server.Prefix}/download\">download</a>");
        var download = await PromiseAll(
            page.WaitForDownloadAsync(),
            page.ClickAsync("a"));
        var path = await download.PathAsync();
        Assert.That(File.Exists(path), Is.True);
        Assert.That(Encoding.UTF8.GetString(File.ReadAllBytes(path)), Is.EqualTo("Hello world"));
    }
}
