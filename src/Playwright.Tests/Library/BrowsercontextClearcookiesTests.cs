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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-clearcookies.spec.ts.</summary>
public class BrowsercontextClearcookiesTests
{
    [UpstreamTest("CO-042 should clear cookies")]
    public async Task ShouldClearCookies(IBrowserContext context, IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Url = server.EmptyPage,
                Name = "cookie1",
                Value = "1",
            },
        });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1"));
        await context.ClearCookiesAsync();
        ExpectToEqual(await context.CookiesAsync(), Array.Empty<object>());
        await page.ReloadAsync();
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(""));
    }

    [UpstreamTest("CO-043 should isolate cookies when clearing")]
    public async Task ShouldIsolateCookiesWhenClearing(IBrowserContext context, Server server, IBrowser browser, BrowserNewContextOptions contextOptions)
    {
        var anotherContext = await browser.NewContextAsync(contextOptions);
        await context.AddCookiesAsync(new[] { new Cookie { Url = server.EmptyPage, Name = "page1cookie", Value = "page1value" } });
        await anotherContext.AddCookiesAsync(new[] { new Cookie { Url = server.EmptyPage, Name = "page2cookie", Value = "page2value" } });

        Assert.That((await context.CookiesAsync()).Count, Is.EqualTo(1));
        Assert.That((await anotherContext.CookiesAsync()).Count, Is.EqualTo(1));

        await context.ClearCookiesAsync();
        Assert.That((await context.CookiesAsync()).Count, Is.EqualTo(0));
        Assert.That((await anotherContext.CookiesAsync()).Count, Is.EqualTo(1));

        await anotherContext.ClearCookiesAsync();
        Assert.That((await context.CookiesAsync()).Count, Is.EqualTo(0));
        Assert.That((await anotherContext.CookiesAsync()).Count, Is.EqualTo(0));
        await anotherContext.CloseAsync();
    }

    [UpstreamTest("CO-044 should remove cookies by name")]
    public async Task ShouldRemoveCookiesByName(IBrowserContext context, IPage page, Server server)
    {
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
        });
        await page.GotoAsync(server.Prefix);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1; cookie2=2"));
        await context.ClearCookiesAsync(new() { Name = "cookie1" });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie2=2"));
    }

    [UpstreamTest("CO-045 should remove cookies by name regex")]
    public async Task ShouldRemoveCookiesByNameRegex(IBrowserContext context, IPage page, Server server)
    {
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
        });
        await page.GotoAsync(server.Prefix);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1; cookie2=2"));
        await context.ClearCookiesAsync(new() { NameRegex = new Regex("coo.*1") });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie2=2"));
    }

    [UpstreamTest("CO-046 should remove cookies by domain")]
    public async Task ShouldRemoveCookiesByDomain(IBrowserContext context, IPage page, Server server)
    {
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(server.CrossProcessPrefix).Host,
                Path = "/",
            },
        });
        await page.GotoAsync(server.Prefix);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1"));
        await page.GotoAsync(server.CrossProcessPrefix);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie2=2"));
        await context.ClearCookiesAsync(new() { Domain = new Uri(server.CrossProcessPrefix).Host });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(""));
        await page.GotoAsync(server.Prefix);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1"));
    }

    [UpstreamTest("CO-047 should remove cookies by path")]
    public async Task ShouldRemoveCookiesByPath(IBrowserContext context, IPage page, Server server)
    {
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(server.Prefix).Host,
                Path = "/api/v1",
            },
            new Cookie
            {
                Name = "cookie2",
                Value = "2",
                Domain = new Uri(server.Prefix).Host,
                Path = "/api/v2",
            },
            new Cookie
            {
                Name = "cookie3",
                Value = "3",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
        });
        await page.GotoAsync(server.Prefix + "/api/v1");
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1; cookie3=3"));
        await context.ClearCookiesAsync(new() { Path = "/api/v1" });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie3=3"));
        await page.GotoAsync(server.Prefix + "/api/v2");
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie2=2; cookie3=3"));
        await page.GotoAsync(server.Prefix + "/");
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie3=3"));
    }

    [UpstreamTest("CO-048 should remove cookies by name and domain")]
    public async Task ShouldRemoveCookiesByNameAndDomain(IBrowserContext context, IPage page, Server server)
    {
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
            new Cookie
            {
                Name = "cookie1",
                Value = "1",
                Domain = new Uri(server.CrossProcessPrefix).Host,
                Path = "/",
            },
        });
        await page.GotoAsync(server.Prefix);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1"));
        await context.ClearCookiesAsync(new() { Name = "cookie1", Domain = new Uri(server.Prefix).Host });
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo(""));
        await page.GotoAsync(server.CrossProcessPrefix);
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("cookie1=1"));
    }

    [UpstreamTest("CO-049 should not transiently delete non-matching cookies when filtering")]
    public async Task ShouldNotTransientlyDeleteNonMatchingCookiesWhenFiltering(IBrowserContext context, IPage page, Server server, string browserName, bool isWindows, bool isFrozenWebkit)
    {
        Info.Skip(browserName == "webkit" && isWindows, "cookieStore change events not supported on WebKit/Windows (curl backend lacks cookie change notifications)");
        Info.Skip(isFrozenWebkit);

        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "keep_me",
                Value = "1",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
            new Cookie
            {
                Name = "delete_me",
                Value = "2",
                Domain = new Uri(server.Prefix).Host,
                Path = "/",
            },
        });
        await page.GotoAsync(server.Prefix);

        var eventsHandle = await page.EvaluateHandleAsync(@"() => {
    const events = [];
    cookieStore.addEventListener('change', event => {
      for (const changed of event.changed)
        events.push(`changed ${changed.name}`);
      for (const deleted of event.deleted)
        events.push(`deleted ${deleted.name}`);
    });
    return events;
  }");

        await context.ClearCookiesAsync(new() { Name = "delete_me" });
        await Expect(async () => Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "deleted delete_me" }))).ToPassAsync();
        Assert.That(await page.EvaluateAsync<string>("document.cookie"), Is.EqualTo("keep_me=1"));
    }

    [UpstreamTest("CO-050 should remove partitioned cookies by name")]
    public async Task ShouldRemovePartitionedCookiesByName(IBrowser browser, Server httpsServer, string browserName, BrowserNewContextOptions contextOptions)
    {
        Info.Skip(browserName != "chromium", "Partitioned cookies (CHIPS) are Chromium-specific");
        var context = await browser.NewContextAsync(new(contextOptions) { IgnoreHTTPSErrors = true });
        await context.AddCookiesAsync(new[]
        {
            new Cookie
            {
                Name = "delete_me",
                Value = "1",
                Domain = httpsServer.Hostname,
                Path = "/",
                Secure = true,
                SameSite = SameSiteAttribute.None,
                PartitionKey = $"https://{httpsServer.Hostname}",
            },
            new Cookie
            {
                Name = "keep_me",
                Value = "2",
                Domain = httpsServer.Hostname,
                Path = "/",
                Secure = true,
                SameSite = SameSiteAttribute.None,
                PartitionKey = $"https://{httpsServer.Hostname}",
            },
        });
        var before = await context.CookiesAsync();
        ExpectToEqual(before, new[]
        {
            ObjectContaining(new { name = "delete_me", partitionKey = $"https://{httpsServer.Hostname}" }),
            ObjectContaining(new { name = "keep_me", partitionKey = $"https://{httpsServer.Hostname}" }),
        });

        await context.ClearCookiesAsync(new() { Name = "delete_me" });
        ExpectToEqual(await context.CookiesAsync(), new[]
        {
            ObjectContaining(new { name = "keep_me", partitionKey = $"https://{httpsServer.Hostname}" }),
        });
        await context.CloseAsync();
    }

    [UpstreamTest("CO-051 should remove nameless cookies by empty name")]
    public async Task ShouldRemoveNamelessCookiesByEmptyName(IBrowserContext context)
    {
        await context.AddCookiesAsync(new[]
        {
            new Cookie { Name = "", Value = "nameless", Domain = "example.com", Path = "/" },
            new Cookie { Name = "session", Value = "secret", Domain = "example.com", Path = "/" },
        });
        await context.ClearCookiesAsync(new() { Name = "" });
        Assert.That((await context.CookiesAsync()).Select(cookie => cookie.Name), Is.EqualTo(new[] { "session" }));
    }
}
