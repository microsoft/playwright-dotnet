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
public class PageSetExtraHttpHeadersTests : UpstreamPageTest
{
    [UpstreamTest("NE-114 should work @smoke")]
    public async Task ShouldWork()
    {
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["foo"] = "bar",
        });
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.EmptyPage));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(request.Headers["baz"], Is.Null);
    }

    [UpstreamTest("NE-115 should work with redirects")]
    public async Task ShouldWorkWithRedirects()
    {
        Server.SetRedirect("/foo.html", "/empty.html");
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["foo"] = "bar"
        });
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.Prefix + "/foo.html"));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("NE-116 should work with extra headers from browser context")]
    public async Task ShouldWorkWithExtraHeadersFromBrowserContext()
    {
        await Page.Context.SetExtraHTTPHeadersAsync(new Dictionary<string, string>
        {
            ["foo"] = "bar",
        });
        var (request, _) = await PromiseAll(
            Server.WaitForRequest("/empty.html"),
            Page.GotoAsync(Server.EmptyPage));
        Assert.That(request.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("NE-118 should not duplicate referer header")]
    public async Task ShouldNotDuplicateRefererHeader()
    {
        Info.Fixme(BrowserName == "chromium" && BrowserMajorVersion < 154, "Request has referer and Referer");
        await Page.SetExtraHTTPHeadersAsync(new Dictionary<string, string> { ["referer"] = Server.EmptyPage });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
        Assert.That(response.Request.Headers["referer"], Is.EqualTo(Server.EmptyPage));
    }
}
