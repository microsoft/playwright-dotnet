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

/// <summary>Port of tests/library/resource-timing.spec.ts.</summary>
public class ResourceTimingTests
{
    [UpstreamTest("NE-131 should work @smoke")]
    public async Task ShouldWork(ContextFactory contextFactory, Server server)
    {
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        var (request, _) = await PromiseAll(
            page.WaitForRequestFinishedAsync(),
            page.GotoAsync(server.EmptyPage));
        var timing = request.Timing;
        VerifyConnectionTimingConsistency(timing);
        Assert.That(timing.RequestStart, Is.GreaterThanOrEqualTo(timing.ConnectEnd));
        Assert.That(timing.ResponseStart, Is.GreaterThanOrEqualTo(timing.RequestStart));
        Assert.That(timing.ResponseEnd, Is.GreaterThanOrEqualTo(timing.ResponseStart));
        Assert.That(timing.ResponseEnd, Is.LessThan(10000d));
        await context.CloseAsync();
    }

    [UpstreamTest("NE-132 should work for subresource")]
    public async Task ShouldWorkForSubresource(ContextFactory contextFactory, Server server, string browserName, string platform)
    {
        Info.Fixme(browserName == "webkit" && platform == "win32", "responseStart is wrong due upstream webkit/libcurl bug");
        var context = await contextFactory();
        var page = await context.NewPageAsync();
        var requests = new List<IRequest>();
        page.RequestFinished += (_, request) => requests.Add(request);
        await page.GotoAsync(server.Prefix + "/one-style.html");
        Assert.That(requests.Count, Is.EqualTo(2));
        var timing = requests[1].Timing;
        VerifyConnectionTimingConsistency(timing);
        Assert.That(timing.RequestStart, Is.GreaterThanOrEqualTo(0d));
        Assert.That(timing.ResponseStart, Is.GreaterThan(timing.RequestStart));
        Assert.That(timing.ResponseEnd, Is.GreaterThanOrEqualTo(timing.ResponseStart));
        Assert.That(timing.ResponseEnd, Is.LessThan(10000d));
        await context.CloseAsync();
    }

    [UpstreamTest("NE-133 should work for SSL")]
    public async Task ShouldWorkForSSL(IBrowser browser, Server httpsServer, string browserName, string platform, BrowserNewPageOptions newPageOptions)
    {
        var page = await browser.NewPageAsync(new(newPageOptions) { IgnoreHTTPSErrors = true });
        var (request, _) = await PromiseAll(
            page.WaitForRequestFinishedAsync(),
            page.GotoAsync(httpsServer.EmptyPage));
        var timing = request.Timing;
        VerifyConnectionTimingConsistency(timing);
        Assert.That(timing.RequestStart, Is.GreaterThanOrEqualTo(timing.ConnectEnd));
        Assert.That(timing.ResponseStart, Is.GreaterThan(timing.RequestStart));
        Assert.That(timing.ResponseEnd, Is.GreaterThanOrEqualTo(timing.ResponseStart));
        Assert.That(timing.ResponseEnd, Is.LessThan(10000d));
        await page.CloseAsync();
    }

    [UpstreamTest("NE-134 should work for redirect")]
    public async Task ShouldWorkForRedirect(ContextFactory contextFactory, string browserName, Server server)
    {
        Info.Fixme(browserName == "webkit", "In WebKit, redirects don't carry the timing info");

        var context = await contextFactory();
        var page = await context.NewPageAsync();
        server.SetRedirect("/foo.html", "/empty.html");
        var responses = new List<IResponse>();
        page.Response += (_, response) => responses.Add(response);
        await page.GotoAsync(server.Prefix + "/foo.html");
        await PromiseAll(responses.Select(r => r.FinishedAsync()));

        Assert.That(responses.Count, Is.EqualTo(2));
        Assert.That(responses[0].Url, Is.EqualTo(server.Prefix + "/foo.html"));
        Assert.That(responses[1].Url, Is.EqualTo(server.Prefix + "/empty.html"));

        var timing1 = responses[0].Request.Timing;
        VerifyConnectionTimingConsistency(timing1);
        Assert.That(timing1.RequestStart, Is.GreaterThanOrEqualTo(timing1.ConnectEnd));
        Assert.That(timing1.ResponseStart, Is.GreaterThan(timing1.RequestStart));
        Assert.That(timing1.ResponseEnd, Is.GreaterThanOrEqualTo(timing1.ResponseStart));
        Assert.That(timing1.ResponseEnd, Is.LessThan(10000d));

        var timing2 = responses[1].Request.Timing;
        VerifyConnectionTimingConsistency(timing2);
        Assert.That(timing2.RequestStart, Is.GreaterThanOrEqualTo(0d));
        Assert.That(timing2.ResponseStart, Is.GreaterThan(timing2.RequestStart));
        Assert.That(timing2.ResponseEnd, Is.GreaterThanOrEqualTo(timing2.ResponseStart));
        Assert.That(timing2.ResponseEnd, Is.LessThan(10000d));

        await context.CloseAsync();
    }

    [UpstreamTest("NE-135 should work when serving from memory cache")]
    public async Task ShouldWorkWhenServingFromMemoryCache(ContextFactory contextFactory, Server server, string browserName, bool isBidi)
    {
        Info.Fixme(browserName == "firefox" && !isBidi, "Response event is not fired in Firefox");
        server.SetRoute("/one-style.css", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/css",
                ["Cache-Control"] = "public, max-age=10031518",
            });
            res.End("body { background: red }");
        });

        var context = await contextFactory();
        var page = await context.NewPageAsync();
        await page.GotoAsync(server.Prefix + "/one-style.html");
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/one-style.css"),
            page.ReloadAsync());
        await response.FinishedAsync();

        var timing = response.Request.Timing;
        VerifyConnectionTimingConsistency(timing);

        Assert.That(timing.ResponseStart, Is.EqualTo(timing.ResponseEnd));
        Assert.That(timing.ResponseEnd, Is.LessThan(1000d));
        await context.CloseAsync();
    }

    private static void VerifyTimingValue(float value, float previous)
    {
        // .NET: upstream calls expect() without a matcher, which checks nothing.
        _ = value == -1 || (value > 0 && value >= previous);
    }

    private static void VerifyConnectionTimingConsistency(RequestTimingResult timing)
    {
        VerifyTimingValue(timing.DomainLookupStart, -1);
        VerifyTimingValue(timing.DomainLookupEnd, timing.DomainLookupStart);
        VerifyTimingValue(timing.ConnectStart, timing.DomainLookupEnd);
        VerifyTimingValue(timing.SecureConnectionStart, timing.ConnectStart);
        VerifyTimingValue(timing.ConnectEnd, timing.SecureConnectionStart);
    }
}
