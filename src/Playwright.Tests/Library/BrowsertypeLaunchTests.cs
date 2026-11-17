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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsertype-launch.spec.ts.</summary>
public class BrowsertypeLaunchTests
{
    [UpstreamTest("BT-004 should reject all promises when browser is closed")]
    public async Task ShouldRejectAllPromisesWhenBrowserIsClosed(IBrowserType browserType, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var browser = await browserType.LaunchAsync(launchOptions);
        var page = await (await browser.NewContextAsync(contextOptions)).NewPageAsync();
        Exception error = null;
        var neverResolves = Catch(() => page.EvaluateAsync("() => new Promise(r => {})"));
        await page.EvaluateAsync("() => new Promise(f => window.builtins.setTimeout(f, 0))");
        await browser.CloseAsync();
        error = await neverResolves;
        // WebKit under task-set -c 1 is giving browser, rest are giving target.
        Assert.That(error.Message, Does.Contain(" closed"));
    }

    [UpstreamTest("BT-009 should throw if page argument is passed")]
    public async Task ShouldThrowIfPageArgumentIsPassed(IBrowserType browserType, string browserName, bool isBidi, BrowserTypeLaunchOptions launchOptions)
    {
        Info.Skip(browserName == "firefox" && !isBidi);

        Exception waitError = null;
        waitError = await Catch(() => browserType.LaunchAsync(new(launchOptions) { Args = ["http://example.com"] }));
        Assert.That(waitError.Message, Does.Contain("can not specify page"));
    }

    [UpstreamTest("BT-010 should reject if launched browser fails immediately")]
    [LocalFixme("dotnet: the message is 'Target page, context or browser has been closed\\nBrowser logs: ...', without the 'browserType.launch: ' prefix of the call (only in chromium and webkit); dotnet: the message is 'Failed to launch the browser process.\\nBrowser logs: ...', without the 'browserType.launch: ' prefix of the call (only in firefox)")]
    public async Task ShouldRejectIfLaunchedBrowserFailsImmediately(string mode, IBrowserType browserType, Func<string, string> asset, string channel, BrowserTypeLaunchOptions launchOptions)
    {
        var error = await Catch(() => browserType.LaunchAsync(new(launchOptions) { ExecutablePath = asset("dummy_bad_browser_executable.js") }));
        if (channel == "webkit-wsl")
        {
            Assert.That(error.Message, Does.Contain("Failed to launch"));
        }
        else
        {
            Assert.That(error.Message, Does.Match(@"browserType\.launch(.|\n)*(spawn UNKNOWN|spawn EFTYPE|Browser logs:)").IgnoreCase);
        }
    }

    [UpstreamTest("BT-011 should reject if executable path is invalid")]
    public async Task ShouldRejectIfExecutablePathIsInvalid(IBrowserType browserType, string mode, BrowserTypeLaunchOptions launchOptions)
    {
        Exception waitError = null;
        waitError = await Catch(() => browserType.LaunchAsync(new(launchOptions) { ExecutablePath = "random-invalid-path" }));
        Assert.That(waitError.Message, Does.Contain("Failed to launch"));
    }

    [UpstreamTest("BT-015 should fire close event for all contexts")]
    public async Task ShouldFireCloseEventForAllContexts(IBrowserType browserType, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        var browser = await browserType.LaunchAsync(launchOptions);
        var context = await browser.NewContextAsync(contextOptions);
        var closed = false;
        context.Close += (_, _) => closed = true;
        await browser.CloseAsync();
        Assert.That(closed, Is.EqualTo(true));
    }

    [UpstreamTest("BT-016 should be callable twice")]
    public async Task ShouldBeCallableTwice(IBrowserType browserType, BrowserTypeLaunchOptions launchOptions)
    {
        var browser = await browserType.LaunchAsync(launchOptions);
        await PromiseAll(
            browser.CloseAsync(),
            browser.CloseAsync());
        await browser.CloseAsync();
    }

    [UpstreamTest("BT-017 should allow await using")]
    public async Task ShouldAllowAwaitUsing(IBrowserType browserType, (int Major, int Minor, int Patch) nodeVersion, BrowserTypeLaunchOptions launchOptions, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(nodeVersion.Major < 18);

        IBrowser b;
        IBrowserContext c;
        IPage p;
        {
            await using var browser = await browserType.LaunchAsync(launchOptions);
            b = browser;
            {
                await using var context = await browser.NewContextAsync(contextOptions);
                c = context;
                {
                    await using var page = await context.NewPageAsync();
                    p = page;
                }
                Assert.That(p.IsClosed, Is.True);
            }
            Assert.That((await Catch(() => c.ClearCookiesAsync())).Message, Does.Contain(TargetClosedErrorMessage));
        }
        Assert.That(b.IsConnected, Is.False);
    }
}
