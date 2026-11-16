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

/// <summary>Port of tests/library/browsercontext-network-event.spec.ts.</summary>
public class BrowsercontextNetworkEventTests
{
    [UpstreamTest("NE-121 BrowserContext.Events.Request")]
    public async Task BrowserContextEventsRequest(IBrowserContext context, Server server)
    {
        var page = await context.NewPageAsync();
        var requests = new List<IRequest>();
        context.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var page1 = await PromiseAll(
            context.WaitForPageAsync(),
            page.ClickAsync("a"));
        await page1.WaitForLoadStateAsync();
        var urls = requests.Select(r => r.Url);
        Assert.That(urls, Is.EqualTo(new[]
        {
            server.EmptyPage,
            $"{server.Prefix}/one-style.html",
            $"{server.Prefix}/one-style.css",
        }));
    }

    [UpstreamTest("NE-122 BrowserContext.Events.Response")]
    public async Task BrowserContextEventsResponse(IBrowserContext context, Server server)
    {
        var page = await context.NewPageAsync();
        var responses = new List<IResponse>();
        context.Response += (_, response) => responses.Add(response);
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a target=_blank rel=noopener href=\"/one-style.html\">yo</a>");
        var page1 = await PromiseAll(
            context.WaitForPageAsync(),
            page.ClickAsync("a"));
        await page1.WaitForLoadStateAsync();
        var urls = responses.Select(r => r.Url);
        Assert.That(urls, Is.EqualTo(new[]
        {
            server.EmptyPage,
            $"{server.Prefix}/one-style.html",
            $"{server.Prefix}/one-style.css",
        }));
    }

    [UpstreamTest("NE-123 BrowserContext.Events.RequestFailed")]
    public async Task BrowserContextEventsRequestFailed(IBrowserContext context, Server server)
    {
        server.SetRoute("/one-style.css", (_, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            res.Connection.Destroy();
        });
        var page = await context.NewPageAsync();
        var failedRequests = new List<IRequest>();
        context.RequestFailed += (_, request) => failedRequests.Add(request);
        await page.GotoAsync(server.Prefix + "/one-style.html");
        Assert.That(failedRequests.Count, Is.EqualTo(1));
        Assert.That(failedRequests[0].Url, Does.Contain("one-style.css"));
        Assert.That(await failedRequests[0].ResponseAsync(), Is.Null);
        Assert.That(failedRequests[0].ResourceType, Is.EqualTo("stylesheet"));
        Assert.That(failedRequests[0].Frame, Is.Not.Null);
    }

    [UpstreamTest("NE-124 BrowserContext.Events.RequestFinished")]
    public async Task BrowserContextEventsRequestFinished(IBrowserContext context, Server server)
    {
        var page = await context.NewPageAsync();
        var (response, _) = await PromiseAll(
            page.GotoAsync(server.EmptyPage),
            Once<IRequest>(h => context.RequestFinished += h, h => context.RequestFinished -= h));
        var request = response.Request;
        Assert.That(request.Url, Is.EqualTo(server.EmptyPage));
        Assert.That(await request.ResponseAsync(), Is.Not.Null);
        Assert.That(request.Frame == page.MainFrame, Is.EqualTo(true));
        Assert.That(request.Frame.Url, Is.EqualTo(server.EmptyPage));
        Assert.That(request.Failure, Is.Null);
    }

    [UpstreamTest("NE-125 should fire events in proper order")]
    public async Task ShouldFireEventsInProperOrder(IBrowserContext context, Server server)
    {
        var page = await context.NewPageAsync();
        var events = new List<string>();
        context.Request += (_, _) => events.Add("request");
        context.Response += (_, _) => events.Add("response");
        context.RequestFinished += (_, _) => events.Add("requestfinished");
        await PromiseAll(
            page.GotoAsync(server.EmptyPage),
            Once<IRequest>(h => context.RequestFinished += h, h => context.RequestFinished -= h));
        Assert.That(events, Is.EqualTo(new[]
        {
            "request",
            "response",
            "requestfinished",
        }));
    }

    [UpstreamTest("NE-126 should not fire events for favicon or favicon redirects")]
    public async Task ShouldNotFireEventsForFaviconOrFaviconRedirects(IBrowserContext context, IPage page, Server server, string browserName, bool headless, string channel)
    {
        It.Skip(headless && browserName != "firefox", "headless browsers, except firefox, do not request favicons");
        It.Skip(!headless && browserName == "webkit", "headed webkit does not have a favicon feature");
        var favicon = "/no-cache/favicon.ico";
        var hashedFaviconUrl = "/favicon-hashed.ico";
        var imagePath = "/fakeimage.png";
        var pagePath = "/page.html";
        server.SetRedirect(favicon, hashedFaviconUrl);
        server.SetRoute(pagePath, (_, res) =>
        {
            res.End($@"
      <!DOCTYPE html>
      <html>
        <head>
          <meta charset=""utf-8"">
          <link rel=""icon"" type=""image/svg+xml"" href=""{favicon}"">
          <title>SVG Favicon Test</title>
        </head>
        <body>
          <img src=""{imagePath}"" alt=""my fake image"">
        </body>
      </html>
");
        });

        var events = new List<string>();
        context.Request += (_, req) => events.Add(req.Url);
        context.Response += (_, res) => events.Add(res.Url);
        context.RequestFinished += (_, req) => events.Add(req.Url);

        await PromiseAll(
            server.WaitForRequest(favicon),
            server.WaitForRequest(hashedFaviconUrl),
            page.GotoAsync(server.Prefix + "/page.html"));

        ExpectToEqual(events, ArrayContaining(new[] { StringContaining(pagePath) }));
        ExpectToEqual(events, ArrayContaining(new[] { StringContaining(imagePath) }));
        ExpectNotToEqual(events, ArrayContaining(new[] { StringContaining(favicon) }));
        ExpectNotToEqual(events, ArrayContaining(new[] { StringContaining(hashedFaviconUrl) }));
    }

    [UpstreamTest("NE-127 should reject response.finished if context closes")]
    public async Task ShouldRejectResponseFinishedIfContextCloses(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/get", (req, res) =>
        {
            // In Firefox, |fetch| will be hanging until it receives |Content-Type| header
            // from server.
            res.SetHeader("Content-Type", "text/plain; charset=utf-8");
            res.Write("hello ");
        });
        // send request and wait for server response
        var (pageResponse, _) = await PromiseAll(
            page.WaitForResponseAsync(_ => true),
            page.EvaluateAsync("() => fetch('./get', { method: 'GET' })"));

        var finishPromise = Catch(() => pageResponse.FinishedAsync());
        await page.Context.CloseAsync();
        var error = await finishPromise;
        Assert.That(error.Message, Does.Contain("closed"));
    }

    [UpstreamTest("NE-144 should not wait for navigation after background cache revalidation")]
    public async Task ShouldNotWaitForNavigationAfterBackgroundCacheRevalidation(IBrowserContext context, Server server)
    {
        server.SetRoute("/swr.html", (req, res) =>
        {
            var headers = new Dictionary<string, object>
            {
                ["Content-Type"] = "text/html",
                ["Cache-Control"] = "s-maxage=900, stale-while-revalidate=31535100",
                ["ETag"] = "\"v1\"",
            };
            if (req.Headers["if-none-match"] == "\"v1\"")
            {
                res.WriteHead(304, headers);
                res.End();
                return;
            }
            res.WriteHead(200, headers);
            res.End("<h1>Hello</h1>");
        });
        var page1 = await context.NewPageAsync();
        await page1.GotoAsync(server.Prefix + "/swr.html");
        var page2 = await context.NewPageAsync();
        await page2.GotoAsync(server.Prefix + "/swr.html");
        await page2.WaitForTimeoutAsync(1000);
        await Expect(page2.Locator("h1")).ToHaveTextAsync("Hello");
    }
}
