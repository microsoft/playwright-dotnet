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

/// <summary>Port of tests/library/browsercontext-user-agent.spec.ts.</summary>
public class BrowsercontextUserAgentTests : UpstreamBrowserTest
{
    [UpstreamTest("UA-001 should work")]
    public async Task ShouldWork()
    {
        {
            var context = await Browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>("() => navigator.userAgent"), Does.Contain("Mozilla"));
            await context.CloseAsync();
        }
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { UserAgent = "foobar" });
            var page = await context.NewPageAsync();
            var (request, _) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                page.GotoAsync(Server.EmptyPage));
            Assert.That(request.Headers["user-agent"], Is.EqualTo("foobar"));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("UA-007 should preserve custom user agent in route.fetch")]
    public async Task ShouldPreserveCustomUserAgentInRouteFetch()
    {
        var context = await ContextFactory(new() { UserAgent = "my-custom-user-agent" });
        var page = await context.NewPageAsync();
        await page.RouteAsync("**/empty.html", async route =>
        {
            await route.FulfillAsync(new() { Response = await route.FetchAsync() });
        });
        var (request, serverRequest, _) = await PromiseAll(
            page.WaitForRequestAsync(Server.EmptyPage),
            Server.WaitForRequest("/empty.html"),
            page.GotoAsync(Server.EmptyPage));
        Assert.That(request.Headers["user-agent"], Is.EqualTo("my-custom-user-agent"));
        Assert.That(serverRequest.Headers["user-agent"], Is.EqualTo("my-custom-user-agent"));
    }

    [UpstreamTest("UA-002 should work for subframes")]
    public async Task ShouldWorkForSubframes()
    {
        {
            var context = await Browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();
            Assert.That(await page.EvaluateAsync<string>("() => navigator.userAgent"), Does.Contain("Mozilla"));
            await context.CloseAsync();
        }
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { UserAgent = "foobar" });
            var page = await context.NewPageAsync();
            var (request, _) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                AttachFrame(page, "frame1", Server.EmptyPage));
            Assert.That(request.Headers["user-agent"], Is.EqualTo("foobar"));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("UA-003 should emulate device user-agent")]
    public async Task ShouldEmulateDeviceUserAgent()
    {
        {
            var context = await Browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<string>("() => navigator.userAgent"), Does.Not.Contain("iPhone"));
            await context.CloseAsync();
        }
        {
            var context = await Browser.NewContextAsync(new(ContextOptions()) { UserAgent = Playwright.Devices["iPhone 6"].UserAgent });
            var page = await context.NewPageAsync();
            await page.GotoAsync(Server.Prefix + "/mobile.html");
            Assert.That(await page.EvaluateAsync<string>("() => navigator.userAgent"), Does.Contain("iPhone"));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("UA-004 should make a copy of default options")]
    public async Task ShouldMakeACopyOfDefaultOptions()
    {
        var options = new BrowserNewContextOptions(ContextOptions()) { UserAgent = "foobar" };
        var context = await Browser.NewContextAsync(options);
        options.UserAgent = "wrong";
        var page = await context.NewPageAsync();
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            page.GotoAsync(Server.EmptyPage));
        Assert.That(request.Headers["user-agent"], Is.EqualTo("foobar"));
        await context.CloseAsync();
    }

    [UpstreamTest("UA-005 custom user agent for download")]
    public async Task CustomUserAgentForDownload()
    {
        Info.Skip(BrowserName == "chromium" && ChromiumVersionLessThan(BrowserVersion, "116.0.0.0"), "https://chromium-review.googlesource.com/c/chromium/src/+/4554578");

        Server.SetRoute("/download", (req, res) =>
        {
            res.SetHeader("Content-Type", "application/octet-stream");
            res.SetHeader("Content-Disposition", "attachment");
            res.End("Hello world");
        });

        var context = await ContextFactory(new() { UserAgent = "MyCustomUA" });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.EmptyPage);
        await page.SetContentAsync("<a id=\"download\" download=\"name\" href=\"/download\">Download</a>");
        var serverRequest = Server.WaitForRequest("/download");
        IgnoreErrors(page.ClickAsync("#download"));
        var req = await serverRequest;
        Assert.That(req.Headers["user-agent"], Is.EqualTo("MyCustomUA"));
    }

    [UpstreamTest("UA-006 should work for navigator.userAgentData and sec-ch-ua headers")]
    public async Task ShouldWorkForNavigatorUserAgentDataAndSecChUaHeaders()
    {
        Info.Skip(BrowserName != "chromium", "This API is Chromium-only");

        {
            var context = await Browser.NewContextAsync(ContextOptions());
            var page = await context.NewPageAsync();
            var (request, _) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                page.GotoAsync(Server.EmptyPage));
            Assert.That(request.Headers["sec-ch-ua"], Does.Contain("\"Chromium\""));
            Assert.That(request.Headers["sec-ch-ua-mobile"], Is.EqualTo("?0"));
            Assert.That(request.Headers["sec-ch-ua-platform"], Is.Not.Empty);
            ExpectToEqual(
                await page.EvaluateAsync<object>("() => window.navigator.userAgentData.toJSON()"),
                ObjectContaining(new { mobile = false }));
            await context.CloseAsync();
        }

        {
            var context = await Browser.NewContextAsync(MergeOptions(new BrowserNewContextOptions(Playwright.Devices["Pixel 7"]), ContextOptions()));
            var page = await context.NewPageAsync();
            var (request, _) = await PromiseAll(
                Server.WaitForRequest("/empty.html"),
                page.GotoAsync(Server.EmptyPage));
            Assert.That(request.Headers["sec-ch-ua"], Does.Contain("\"Chromium\""));
            Assert.That(request.Headers["sec-ch-ua-mobile"], Is.EqualTo("?1"));
            Assert.That(request.Headers["sec-ch-ua-platform"], Is.EqualTo("\"Android\""));
            ExpectToEqual(
                await page.EvaluateAsync<object>("() => window.navigator.userAgentData.toJSON()"),
                ObjectContaining(new { mobile = true, platform = "Android" }));
            await context.CloseAsync();
        }
    }
}
