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

using System.Text;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-base-url.spec.ts.</summary>
public class BrowsercontextBaseUrlTests : UpstreamBrowserTest
{
    [UpstreamTest("NA-089 should construct a new URL when a baseURL in browser.newContext is passed to page.goto @smoke")]
    public async Task ShouldConstructANewURLWhenABaseURLInBrowserNewContextIsPassedToPageGoto()
    {
        var context = await Browser.NewContextAsync(new(ContextOptions())
        {
            BaseURL = Server.Prefix,
        });
        var page = await context.NewPageAsync();
        Assert.That((await page.GotoAsync("/empty.html")).Url, Is.EqualTo(Server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("NA-090 should construct a new URL when a baseURL in browser.newPage is passed to page.goto")]
    public async Task ShouldConstructANewURLWhenABaseURLInBrowserNewPageIsPassedToPageGoto()
    {
        var page = await Browser.NewPageAsync(new(NewPageOptions())
        {
            BaseURL = Server.Prefix,
        });
        Assert.That((await page.GotoAsync("/empty.html")).Url, Is.EqualTo(Server.EmptyPage));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-091 should construct a new URL when a baseURL in browserType.launchPersistentContext is passed to page.goto")]
    public async Task ShouldConstructANewURLWhenABaseURLInBrowserTypeLaunchPersistentContextIsPassedToPageGoto()
    {
        var userDataDir = await CreateUserDataDir();
        var context = await BrowserType.LaunchPersistentContextAsync(userDataDir, new(LaunchPersistentContextOptions())
        {
            BaseURL = Server.Prefix,
        });
        var page = await context.NewPageAsync();
        Assert.That((await page.GotoAsync("/empty.html")).Url, Is.EqualTo(Server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("NA-092 should construct the URLs correctly when a baseURL without a trailing slash in browser.newPage is passed to page.goto")]
    public async Task ShouldConstructTheURLsCorrectlyWhenABaseURLWithoutATrailingSlashInBrowserNewPageIsPassedToPageGoto()
    {
        var page = await Browser.NewPageAsync(new(NewPageOptions())
        {
            BaseURL = Server.Prefix + "/url-construction",
        });
        Assert.That((await page.GotoAsync("mypage.html")).Url, Is.EqualTo(Server.Prefix + "/mypage.html"));
        Assert.That((await page.GotoAsync("./mypage.html")).Url, Is.EqualTo(Server.Prefix + "/mypage.html"));
        Assert.That((await page.GotoAsync("/mypage.html")).Url, Is.EqualTo(Server.Prefix + "/mypage.html"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-093 should construct the URLs correctly when a baseURL with a trailing slash in browser.newPage is passed to page.goto")]
    public async Task ShouldConstructTheURLsCorrectlyWhenABaseURLWithATrailingSlashInBrowserNewPageIsPassedToPageGoto()
    {
        var page = await Browser.NewPageAsync(new(NewPageOptions())
        {
            BaseURL = Server.Prefix + "/url-construction/",
        });
        Assert.That((await page.GotoAsync("mypage.html")).Url, Is.EqualTo(Server.Prefix + "/url-construction/mypage.html"));
        Assert.That((await page.GotoAsync("./mypage.html")).Url, Is.EqualTo(Server.Prefix + "/url-construction/mypage.html"));
        Assert.That((await page.GotoAsync("/mypage.html")).Url, Is.EqualTo(Server.Prefix + "/mypage.html"));
        Assert.That((await page.GotoAsync(".")).Url, Is.EqualTo(Server.Prefix + "/url-construction/"));
        Assert.That((await page.GotoAsync("/")).Url, Is.EqualTo(Server.Prefix + "/"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-094 should not construct a new URL when valid URLs are passed")]
    public async Task ShouldNotConstructANewURLWhenValidURLsArePassed()
    {
        var page = await Browser.NewPageAsync(new(NewPageOptions())
        {
            BaseURL = "http://microsoft.com",
        });
        Assert.That((await page.GotoAsync(Server.EmptyPage)).Url, Is.EqualTo(Server.EmptyPage));

        await page.GotoAsync("data:text/html,Hello world");
        Assert.That(await page.EvaluateAsync<string>("() => window.location.href"), Is.EqualTo("data:text/html,Hello world"));

        await page.GotoAsync("about:blank");
        Assert.That(await page.EvaluateAsync<string>("() => window.location.href"), Is.EqualTo("about:blank"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-095 should be able to match a URL relative to its given URL with urlMatcher")]
    public async Task ShouldBeAbleToMatchAURLRelativeToItsGivenURLWithUrlMatcher()
    {
        var page = await Browser.NewPageAsync(new(NewPageOptions())
        {
            BaseURL = Server.Prefix + "/foobar/",
        });
        await page.GotoAsync("/kek/index.html");
        await page.WaitForURLAsync("/kek/index.html");
        Assert.That(page.Url, Is.EqualTo(Server.Prefix + "/kek/index.html"));

        await page.RouteAsync("./kek/index.html", route => route.FulfillAsync(new()
        {
            Body = "base-url-matched-route",
        }));
        var (request, response, _) = await PromiseAll(
            page.WaitForRequestAsync("./kek/index.html"),
            page.WaitForResponseAsync("./kek/index.html"),
            page.GotoAsync("./kek/index.html"));
        Assert.That(request.Url, Is.EqualTo(Server.Prefix + "/foobar/kek/index.html"));
        Assert.That(response.Url, Is.EqualTo(Server.Prefix + "/foobar/kek/index.html"));
        Assert.That(Encoding.UTF8.GetString(await response.BodyAsync()), Is.EqualTo("base-url-matched-route"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-096 should not construct a new URL with baseURL when a glob was used")]
    public async Task ShouldNotConstructANewURLWithBaseURLWhenAGlobWasUsed()
    {
        var page = await Browser.NewPageAsync(new(NewPageOptions())
        {
            BaseURL = Server.Prefix + "/foobar/",
        });
        await page.GotoAsync("./kek/index.html");
        await page.WaitForURLAsync("**/foobar/kek/index.html");
        await page.CloseAsync();
    }
}
