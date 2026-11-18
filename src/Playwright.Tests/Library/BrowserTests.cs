/**
 * Copyright 2020 Microsoft Corporation. All rights reserved.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browser.spec.ts.</summary>
public class BrowserTests : UpstreamBrowserTest
{
    [UpstreamTest("BR-001 should return browserType")]
    public async Task ShouldReturnBrowserType()
    {
        Assert.That(Browser.BrowserType, Is.SameAs(BrowserType));
    }

    [UpstreamTest("BR-002 should create new page @smoke")]
    public async Task ShouldCreateNewPage()
    {
        var page1 = await Browser.NewPageAsync(NewPageOptions());
        Assert.That(Browser.Contexts.Count, Is.EqualTo(1));

        var page2 = await Browser.NewPageAsync(NewPageOptions());
        Assert.That(Browser.Contexts.Count, Is.EqualTo(2));

        await page1.CloseAsync();
        Assert.That(Browser.Contexts.Count, Is.EqualTo(1));

        await page2.CloseAsync();
        Assert.That(Browser.Contexts.Count, Is.EqualTo(0));
    }

    [UpstreamTest("BR-003 should throw upon second create new page")]
    public async Task ShouldThrowUponSecondCreateNewPage()
    {
        var page = await Browser.NewPageAsync(NewPageOptions());
        Exception error = null;
        error = await Catch(() => page.Context.NewPageAsync());
        await page.CloseAsync();
        Assert.That(error.Message, Does.Contain("Please use Browser.NewContextAsync()"));
    }

    [UpstreamTest("BR-004 version should work")]
    public async Task VersionShouldWork()
    {
        var version = Browser.Version;
        if (BrowserName == "chromium")
        {
            Assert.That(Regex.Match(version, @"^\d+\.\d+\.\d+\.\d+$").Success, Is.True);
        }
        else
        {
            Assert.That(Regex.Match(version, @"^\d+\.\d+").Success, Is.True);
        }
    }

    [UpstreamTest("BR-005 should dispatch page.on(close) upon browser.close and reject evaluate")]
    public async Task ShouldDispatchPageOnCloseUponBrowserCloseAndRejectEvaluate()
    {
        var browser = await BrowserType.LaunchAsync(LaunchOptions());
        var page = await browser.NewPageAsync(NewPageOptions());
        var closed = false;
        page.Close += (_, _) => closed = true;
        var promise = Catch(() => page.EvaluateAsync("() => new Promise(() => {})"));
        await browser.CloseAsync();
        Assert.That(closed, Is.EqualTo(true));
        var error = await promise;
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("BR-006 should fire context event on newContext")]
    public async Task ShouldFireContextEventOnNewContext()
    {
        var events = new List<IBrowserContext>();
        Browser.Context += (_, ctx) => events.Add(ctx);
        var context = await Browser.NewContextAsync(ContextOptions());
        Assert.That(events, Is.EqualTo(new[] { context }));
        await context.CloseAsync();
    }
}
