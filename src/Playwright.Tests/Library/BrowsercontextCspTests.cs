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

/// <summary>Port of tests/library/browsercontext-csp.spec.ts.</summary>
public class BrowsercontextCspTests
{
    [UpstreamTest("CS-001 should bypass CSP meta tag @smoke")]
    public async Task ShouldBypassCSPMetaTag(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        // Make sure CSP prohibits addScriptTag.
        {
            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/csp.html");
            Assert.That(await page.EvaluateAsync<object>("window[\"__inlineScriptValue\"]"), Is.Null);
            await Catch(() => page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" }));
            Assert.That(await page.EvaluateAsync<object>("window[\"__injected\"]"), Is.Null);
            await context.CloseAsync();
        }

        // By-pass CSP and try one more time.
        {
            var context = await browser.NewContextAsync(new(contextOptions) { BypassCSP = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.Prefix + "/csp.html");
            Assert.That(await page.EvaluateAsync<int>("window[\"__inlineScriptValue\"]"), Is.EqualTo(42));
            await page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" });
            Assert.That(await page.EvaluateAsync<int>("window[\"__injected\"]"), Is.EqualTo(42));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("CS-002 should bypass CSP header")]
    public async Task ShouldBypassCSPHeader(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        // Make sure CSP prohibits addScriptTag.
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Content-Security-Policy", "default-src \"self\"");
            res.SetHeader("Content-Type", "text/html");
            res.End("<script type='text/javascript'>window.__inlineScriptValue = 42;</script>");
        });
        {
            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            Assert.That(await page.EvaluateAsync<object>("window[\"__inlineScriptValue\"]"), Is.Null);
            await Catch(() => page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" }));
            Assert.That(await page.EvaluateAsync<object>("window[\"__injected\"]"), Is.Null);
            await context.CloseAsync();
        }

        // By-pass CSP and try one more time.
        {
            var context = await browser.NewContextAsync(new(contextOptions) { BypassCSP = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            Assert.That(await page.EvaluateAsync<int>("window[\"__inlineScriptValue\"]"), Is.EqualTo(42));
            await page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" });
            Assert.That(await page.EvaluateAsync<int>("window[\"__injected\"]"), Is.EqualTo(42));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("CS-003 should bypass after cross-process navigation")]
    public async Task ShouldBypassAfterCrossProcessNavigation(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { BypassCSP = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/csp.html");
        Assert.That(await page.EvaluateAsync<int>("window[\"__inlineScriptValue\"]"), Is.EqualTo(42));
        await page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" });
        Assert.That(await page.EvaluateAsync<int>("window[\"__injected\"]"), Is.EqualTo(42));

        await page.GotoAsync(server.CrossProcessPrefix + "/csp.html");
        Assert.That(await page.EvaluateAsync<int>("window[\"__inlineScriptValue\"]"), Is.EqualTo(42));
        await page.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" });
        Assert.That(await page.EvaluateAsync<int>("window[\"__injected\"]"), Is.EqualTo(42));
        await context.CloseAsync();
    }

    [UpstreamTest("CS-004 should bypass CSP in iframes as well")]
    public async Task ShouldBypassCSPInIframesAsWell(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        // Make sure CSP prohibits addScriptTag in an iframe.
        {
            var context = await browser.NewContextAsync(contextOptions);
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            var frame = await AttachFrame(page, "frame1", server.Prefix + "/csp.html");
            Assert.That(await frame.EvaluateAsync<object>("window[\"__inlineScriptValue\"]"), Is.Null);
            await Catch(() => frame.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" }));
            Assert.That(await frame.EvaluateAsync<object>("window[\"__injected\"]"), Is.Null);
            await context.CloseAsync();
        }

        // By-pass CSP and try one more time.
        {
            var context = await browser.NewContextAsync(new(contextOptions) { BypassCSP = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(server.EmptyPage);
            var frame = await AttachFrame(page, "frame1", server.Prefix + "/csp.html");
            Assert.That(await frame.EvaluateAsync<int>("window[\"__inlineScriptValue\"]"), Is.EqualTo(42));
            await Catch(() => frame.AddScriptTagAsync(new() { Content = "window[\"__injected\"] = 42;" }));
            Assert.That(await frame.EvaluateAsync<int>("window[\"__injected\"]"), Is.EqualTo(42));
            await context.CloseAsync();
        }
    }

    [UpstreamTest("CS-005 should not crash on javascript: navigation")]
    public async Task ShouldNotCrashOnJavascriptNavigation(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions) { BypassCSP = true });
        var page = await context.NewPageAsync();
        var crashed = false;
        page.Crash += (_, _) => crashed = true;
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a id=\"a\" href=\"javascript:window.__clicked = true\">click</a>");
        await page.ClickAsync("#a");
        await Expect(async () => Assert.That(await page.EvaluateAsync<bool>("() => window['__clicked']"), Is.EqualTo(true))).ToPassAsync();
        Assert.That(crashed, Is.EqualTo(false));
        await context.CloseAsync();
    }
}
