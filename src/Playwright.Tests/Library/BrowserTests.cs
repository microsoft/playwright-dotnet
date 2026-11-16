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
public class BrowserTests
{
    [UpstreamTest("BR-001 should return browserType")]
    public async Task ShouldReturnBrowserType(IBrowser browser, IBrowserType browserType)
    {
        Assert.That(browser.BrowserType, Is.SameAs(browserType));
    }

    [UpstreamTest("BR-002 should create new page @smoke")]
    public async Task ShouldCreateNewPage(IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        var page1 = await browser.NewPageAsync(newPageOptions);
        Assert.That(browser.Contexts.Count, Is.EqualTo(1));

        var page2 = await browser.NewPageAsync(newPageOptions);
        Assert.That(browser.Contexts.Count, Is.EqualTo(2));

        await page1.CloseAsync();
        Assert.That(browser.Contexts.Count, Is.EqualTo(1));

        await page2.CloseAsync();
        Assert.That(browser.Contexts.Count, Is.EqualTo(0));
    }

    [UpstreamTest("BR-003 should throw upon second create new page")]
    [Fixme("dotnet: the message is \"Please use Browser.NewContextAsync()\", upstream expects \"Please use browser.newContext()\"")]
    public async Task ShouldThrowUponSecondCreateNewPage(IBrowser browser, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(newPageOptions);
        Exception error = null;
        error = await Catch(() => page.Context.NewPageAsync());
        await page.CloseAsync();
        Assert.That(error.Message, Does.Contain("Please use browser.newContext()"));
    }

    [UpstreamTest("BR-004 version should work")]
    public async Task VersionShouldWork(IBrowser browser, string browserName)
    {
        var version = browser.Version;
        if (browserName == "chromium")
        {
            Assert.That(Regex.Match(version, @"^\d+\.\d+\.\d+\.\d+$").Success, Is.True);
        }
        else
        {
            Assert.That(Regex.Match(version, @"^\d+\.\d+").Success, Is.True);
        }
    }

    [UpstreamTest("BR-005 should dispatch page.on(close) upon browser.close and reject evaluate")]
    public async Task ShouldDispatchPageOnCloseUponBrowserCloseAndRejectEvaluate(IBrowserType browserType, BrowserTypeLaunchOptions launchOptions, BrowserNewPageOptions newPageOptions)
    {
        var browser = await browserType.LaunchAsync(launchOptions);
        var page = await browser.NewPageAsync(newPageOptions);
        var closed = false;
        page.Close += (_, _) => closed = true;
        var promise = Catch(() => page.EvaluateAsync("() => new Promise(() => {})"));
        await browser.CloseAsync();
        Assert.That(closed, Is.EqualTo(true));
        var error = await promise;
        Assert.That(error.Message, Does.Contain(TargetClosedErrorMessage));
    }

    [UpstreamTest("BR-006 should fire context event on newContext")]
    public async Task ShouldFireContextEventOnNewContext(IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var events = new List<IBrowserContext>();
        browser.Context += (_, ctx) => events.Add(ctx);
        var context = await browser.NewContextAsync(contextOptions);
        Assert.That(events, Is.EqualTo(new[] { context }));
        await context.CloseAsync();
    }
}
