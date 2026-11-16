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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-set-extra-http-headers.spec.ts.</summary>
public class PageSetExtraHttpHeadersTests
{
    [UpstreamTest("NE-114 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["foo"] = "bar",
        });
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(request.Headers["baz"], Is.Null);
    }

    [UpstreamTest("NE-115 should work with redirects")]
    public async Task ShouldWorkWithRedirects(IPage page, Server server)
    {
        server.SetRedirect("/foo.html", "/empty.html");
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["foo"] = "bar"
        });
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.Prefix + "/foo.html"));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("NE-116 should work with extra headers from browser context")]
    public async Task ShouldWorkWithExtraHeadersFromBrowserContext(IPage page, Server server)
    {
        await page.Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["foo"] = "bar",
        });
        var (request, _) = await PromiseAll(
            server.WaitForRequest("/empty.html"),
            page.GotoAsync(server.EmptyPage));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("NE-118 should not duplicate referer header")]
    public async Task ShouldNotDuplicateRefererHeader(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Fail(browserName == "chromium" && browserMajorVersion < 154, "Request has referer and Referer");
        await page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = server.EmptyPage });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Request.Headers["referer"], Is.EqualTo(server.EmptyPage));
    }
}
