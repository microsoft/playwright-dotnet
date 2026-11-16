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
public class BrowsercontextBaseUrlTests
{
    [UpstreamTest("NA-089 should construct a new URL when a baseURL in browser.newContext is passed to page.goto @smoke")]
    public async Task ShouldConstructANewURLWhenABaseURLInBrowserNewContextIsPassedToPageGoto(IBrowser browser, Server server, BrowserNewContextOptions contextOptions)
    {
        var context = await browser.NewContextAsync(new(contextOptions)
        {
            BaseURL = server.Prefix,
        });
        var page = await context.NewPageAsync();
        Assert.That((await page.GotoAsync("/empty.html")).Url, Is.EqualTo(server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("NA-090 should construct a new URL when a baseURL in browser.newPage is passed to page.goto")]
    public async Task ShouldConstructANewURLWhenABaseURLInBrowserNewPageIsPassedToPageGoto(IBrowser browser, Server server, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions)
        {
            BaseURL = server.Prefix,
        });
        Assert.That((await page.GotoAsync("/empty.html")).Url, Is.EqualTo(server.EmptyPage));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-091 should construct a new URL when a baseURL in browserType.launchPersistentContext is passed to page.goto")]
    public async Task ShouldConstructANewURLWhenABaseURLInBrowserTypeLaunchPersistentContextIsPassedToPageGoto(IBrowserType browserType, Server server, CreateUserDataDir createUserDataDir, BrowserTypeLaunchPersistentContextOptions launchPersistentContextOptions)
    {
        var userDataDir = await createUserDataDir();
        var context = await browserType.LaunchPersistentContextAsync(userDataDir, new(launchPersistentContextOptions)
        {
            BaseURL = server.Prefix,
        });
        var page = await context.NewPageAsync();
        Assert.That((await page.GotoAsync("/empty.html")).Url, Is.EqualTo(server.EmptyPage));
        await context.CloseAsync();
    }

    [UpstreamTest("NA-092 should construct the URLs correctly when a baseURL without a trailing slash in browser.newPage is passed to page.goto")]
    public async Task ShouldConstructTheURLsCorrectlyWhenABaseURLWithoutATrailingSlashInBrowserNewPageIsPassedToPageGoto(IBrowser browser, Server server, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions)
        {
            BaseURL = server.Prefix + "/url-construction",
        });
        Assert.That((await page.GotoAsync("mypage.html")).Url, Is.EqualTo(server.Prefix + "/mypage.html"));
        Assert.That((await page.GotoAsync("./mypage.html")).Url, Is.EqualTo(server.Prefix + "/mypage.html"));
        Assert.That((await page.GotoAsync("/mypage.html")).Url, Is.EqualTo(server.Prefix + "/mypage.html"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-093 should construct the URLs correctly when a baseURL with a trailing slash in browser.newPage is passed to page.goto")]
    public async Task ShouldConstructTheURLsCorrectlyWhenABaseURLWithATrailingSlashInBrowserNewPageIsPassedToPageGoto(IBrowser browser, Server server, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions)
        {
            BaseURL = server.Prefix + "/url-construction/",
        });
        Assert.That((await page.GotoAsync("mypage.html")).Url, Is.EqualTo(server.Prefix + "/url-construction/mypage.html"));
        Assert.That((await page.GotoAsync("./mypage.html")).Url, Is.EqualTo(server.Prefix + "/url-construction/mypage.html"));
        Assert.That((await page.GotoAsync("/mypage.html")).Url, Is.EqualTo(server.Prefix + "/mypage.html"));
        Assert.That((await page.GotoAsync(".")).Url, Is.EqualTo(server.Prefix + "/url-construction/"));
        Assert.That((await page.GotoAsync("/")).Url, Is.EqualTo(server.Prefix + "/"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-094 should not construct a new URL when valid URLs are passed")]
    public async Task ShouldNotConstructANewURLWhenValidURLsArePassed(IBrowser browser, Server server, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions)
        {
            BaseURL = "http://microsoft.com",
        });
        Assert.That((await page.GotoAsync(server.EmptyPage)).Url, Is.EqualTo(server.EmptyPage));

        await page.GotoAsync("data:text/html,Hello world");
        Assert.That(await page.EvaluateAsync<string>("() => window.location.href"), Is.EqualTo("data:text/html,Hello world"));

        await page.GotoAsync("about:blank");
        Assert.That(await page.EvaluateAsync<string>("() => window.location.href"), Is.EqualTo("about:blank"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-095 should be able to match a URL relative to its given URL with urlMatcher")]
    public async Task ShouldBeAbleToMatchAURLRelativeToItsGivenURLWithUrlMatcher(IBrowser browser, Server server, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions)
        {
            BaseURL = server.Prefix + "/foobar/",
        });
        await page.GotoAsync("/kek/index.html");
        await page.WaitForURLAsync("/kek/index.html");
        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/kek/index.html"));

        await page.RouteAsync("./kek/index.html", route => route.FulfillAsync(new()
        {
            Body = "base-url-matched-route",
        }));
        var (request, response, _) = await PromiseAll(
            page.WaitForRequestAsync("./kek/index.html"),
            page.WaitForResponseAsync("./kek/index.html"),
            page.GotoAsync("./kek/index.html"));
        Assert.That(request.Url, Is.EqualTo(server.Prefix + "/foobar/kek/index.html"));
        Assert.That(response.Url, Is.EqualTo(server.Prefix + "/foobar/kek/index.html"));
        Assert.That(Encoding.UTF8.GetString(await response.BodyAsync()), Is.EqualTo("base-url-matched-route"));
        await page.CloseAsync();
    }

    [UpstreamTest("NA-096 should not construct a new URL with baseURL when a glob was used")]
    public async Task ShouldNotConstructANewURLWithBaseURLWhenAGlobWasUsed(IBrowser browser, Server server, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions)
        {
            BaseURL = server.Prefix + "/foobar/",
        });
        await page.GotoAsync("./kek/index.html");
        await page.WaitForURLAsync("**/foobar/kek/index.html");
        await page.CloseAsync();
    }
}
