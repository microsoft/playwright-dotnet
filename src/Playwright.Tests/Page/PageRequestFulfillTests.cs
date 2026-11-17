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
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-request-fulfill.spec.ts.</summary>
public class PageRequestFulfillTests
{
    // We access test servers at 10.0.2.2 from inside the browser on Android,
    // which is actually forwarded to the desktop localhost.
    // To use request such an url with apiRequestContext on the desktop, we need to change it back to localhost.
    [Fixture]
    public static Func<string, string> RewriteAndroidLoopbackURL(bool isAndroid) => givenURL =>
    {
        if (!isAndroid)
        {
            return givenURL;
        }
        var requestURL = new UriBuilder(givenURL);
        requestURL.Host = "localhost";
        return requestURL.Uri.ToString();
    };

    [UpstreamTest("RO-096 should work")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 201,
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
                ContentType = "text/html",
                Body = "Yo, page!",
            }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(response.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(await page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-097 should work with buffer as body")]
    public async Task ShouldWorkWithBufferAsBody(IPage page, Server server, string browserName, bool isLinux)
    {
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/plain",
                BodyBytes = Encoding.UTF8.GetBytes("Yo, page!"),
            }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-098 should work with status code 422")]
    public async Task ShouldWorkWithStatusCode422(IPage page, Server server)
    {
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 422,
                Body = "Yo, page!",
            }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(422));
        Assert.That(response.StatusText, Is.EqualTo("Unprocessable Entity"));
        Assert.That(await page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-099 should fulfill with unuassigned status codes")]
    public async Task ShouldFulfillWithUnuassignedStatusCodes(IPage page, Server server, string browserName)
    {
        // .NET: a promise that is resolved with a promise is a task of a task.
        var fulfillPromise = new TaskCompletionSource<Task<Exception>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/data.json", route =>
        {
            fulfillPromise.SetResult(Catch(() => route.FulfillAsync(new()
            {
                Status = 430,
                Body = "Yo, page!",
            })));
        });
        await page.GotoAsync(server.EmptyPage);
        var response = await page.EvaluateAsync<JsonElement>(@"async url => {
    const { status, statusText } = await fetch(url);
    return { status, statusText };
  }", server.Prefix + "/data.json");
        var error = await await fulfillPromise.Task;
        Assert.That(error, Is.Null);
        Assert.That(response.GetProperty("status").GetInt32(), Is.EqualTo(430));
        Assert.That(response.GetProperty("statusText").GetString(), Is.EqualTo("Unknown"));
    }

    [UpstreamTest("RO-100 should not throw if request was cancelled by the page")]
    public async Task ShouldNotThrowIfRequestWasCancelledByThePage(IPage page, Server server)
    {
        var interceptPromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync("**/data.json", route => interceptPromise.SetResult(route));
        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"url => {
    globalThis.controller = new AbortController();
    fetch(url, { signal: globalThis.controller.signal }).catch(() => {});
  }", server.Prefix + "/data.json");
        var route = await interceptPromise.Task;
        var failurePromise = Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h);
        await page.EvaluateAsync("() => globalThis.controller.abort()");
        var cancelledRequest = await failurePromise;
        // .NET: request.Failure is the error text.
        Assert.That(cancelledRequest.Failure, Is.Not.Null);
        Assert.That(cancelledRequest.Failure, Does.Match("cancelled|aborted").IgnoreCase);
        await route.FulfillAsync(new() { Status = 200 }); // Should not throw.
    }

    [UpstreamTest("RO-101 should allow mocking binary responses")]
    public async Task ShouldAllowMockingBinaryResponses(IPage page, Server server, string browserName, bool headless, Func<string, string> asset, bool isAndroid, string mode)
    {
        Info.Skip(browserName == "firefox" && !headless, "Firefox headed produces a different image.");
        Info.Skip(isAndroid);

        await page.RouteAsync("**/*", route =>
        {
            var imageBuffer = File.ReadAllBytes(asset("pptr.png"));
            IgnoreErrors(route.FulfillAsync(new()
            {
                ContentType = "image/png",
                BodyBytes = imageBuffer,
            }));
        });
        await page.EvaluateAsync(@"PREFIX => {
    const img = document.createElement('img');
    img.src = PREFIX + '/does-not-exist.png';
    document.body.appendChild(img);
    return new Promise(fulfill => img.onload = fulfill);
  }", server.Prefix);
        var img = await page.QuerySelectorAsync("img");
        ExpectToMatchSnapshot(await img.ScreenshotAsync(), "mock-binary-response.png");
    }

    [UpstreamTest("RO-102 should allow mocking svg with charset")]
    public async Task ShouldAllowMockingSvgWithCharset(IPage page, Server server, string browserName, bool headless, bool isAndroid, bool isElectron, int electronMajorVersion)
    {
        Info.Skip(browserName == "firefox" && !headless, "Firefox headed produces a different image.");
        Info.Skip(isAndroid);
        Info.Skip(isElectron && electronMajorVersion < 30, "Protocol error (Storage.getCookies): Browser context management is not supported");

        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                ContentType = "image/svg+xml ; charset=utf-8",
                Body = "<svg width=\"50\" height=\"50\" version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\"><rect x=\"10\" y=\"10\" width=\"30\" height=\"30\" stroke=\"black\" fill=\"transparent\" stroke-width=\"5\"/></svg>",
            }));
        });
        await page.EvaluateAsync(@"PREFIX => {
    const img = document.createElement('img');
    img.src = PREFIX + '/does-not-exist.svg';
    document.body.appendChild(img);
    return new Promise((f, r) => { img.onload = f; img.onerror = r; });
  }", server.Prefix);
        var img = await page.QuerySelectorAsync("img");
        ExpectToMatchSnapshot(await img.ScreenshotAsync(), "mock-svg.png");
    }

    [UpstreamTest("RO-103 should work with file path")]
    public async Task ShouldWorkWithFilePath(IPage page, Server server, Func<string, string> asset, string mode, bool isAndroid)
    {
        Info.Skip(isAndroid);

        await page.RouteAsync("**/*", route => route.FulfillAsync(new() { ContentType = "shouldBeIgnored", Path = asset("pptr.png") }));
        await page.EvaluateAsync(@"PREFIX => {
    const img = document.createElement('img');
    img.src = PREFIX + '/does-not-exist.png';
    document.body.appendChild(img);
    return new Promise(fulfill => img.onload = fulfill);
  }", server.Prefix);
        var img = await page.QuerySelectorAsync("img");
        ExpectToMatchSnapshot(await img.ScreenshotAsync(), "mock-binary-response.png");
    }

    [UpstreamTest("RO-104 should stringify intercepted request response headers")]
    public async Task ShouldStringifyInterceptedRequestResponseHeaders(IPage page, Server server)
    {
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "true",
                },
                Body = "Yo, page!",
            }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        var headers = response.Headers;
        Assert.That(headers["foo"], Is.EqualTo("true"));
        Assert.That(await page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-105 should not modify the headers sent to the server")]
    public async Task ShouldNotModifyTheHeadersSentToTheServer(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/empty.html");
        var interceptedRequests = new List<ServerRequest>();

        // this is just to enable request interception, which disables caching in chromium
        await page.RouteAsync(server.Prefix + "/unused", route => { });

        server.SetRoute("/something", (request, response) =>
        {
            interceptedRequests.Add(request);
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });

        var text = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", server.CrossProcessPrefix + "/something");
        Assert.That(text, Is.EqualTo("done"));

        await page.RouteAsync(server.CrossProcessPrefix + "/something", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers),
            }));
        });

        var textAfterRoute = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", server.CrossProcessPrefix + "/something");
        Assert.That(textAfterRoute, Is.EqualTo("done"));

        Assert.That(interceptedRequests.Count, Is.EqualTo(2));
        ExpectToEqual(interceptedRequests[1].Headers, interceptedRequests[0].Headers);
    }

    [UpstreamTest("RO-106 should include the origin header")]
    public async Task ShouldIncludeTheOriginHeader(IPage page, Server server, bool isAndroid)
    {
        Info.Skip(isAndroid, "No cross-process on Android");

        await page.GotoAsync(server.Prefix + "/empty.html");
        IRequest interceptedRequest = null;
        await page.RouteAsync(server.CrossProcessPrefix + "/something", route =>
        {
            interceptedRequest = route.Request;
            IgnoreErrors(route.FulfillAsync(new()
            {
                Headers = new Dictionary<string, string>
                {
                    ["Access-Control-Allow-Origin"] = "*",
                },
                ContentType = "text/plain",
                Body = "done",
            }));
        });

        var text = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", server.CrossProcessPrefix + "/something");
        Assert.That(text, Is.EqualTo("done"));
        Assert.That(interceptedRequest.Headers["origin"], Is.EqualTo(server.Prefix));
    }

    [UpstreamTest("RO-107 should fulfill with global fetch result")]
    public async Task ShouldFulfillWithGlobalFetchResult(IPlaywright playwright, IPage page, Server server, bool isElectron, int electronMajorVersion, Func<string, string> rewriteAndroidLoopbackURL)
    {
        Info.Skip(isElectron && electronMajorVersion < 30, "error: Browser context management is not supported.");
        await page.RouteAsync("**/*", async route =>
        {
            var request = await playwright.APIRequest.NewContextAsync();
            var response = await request.GetAsync(rewriteAndroidLoopbackURL(server.Prefix + "/simple.json"));
            IgnoreErrors(route.FulfillAsync(new() { Response = response }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
    }

    [UpstreamTest("RO-108 should fulfill with fetch result")]
    public async Task ShouldFulfillWithFetchResult(IPage page, Server server, bool isElectron, int electronMajorVersion, Func<string, string> rewriteAndroidLoopbackURL)
    {
        Info.Skip(isElectron && electronMajorVersion < 30, "error: Browser context management is not supported.");
        await page.RouteAsync("**/*", async route =>
        {
            var response = await page.APIRequest.GetAsync(rewriteAndroidLoopbackURL(server.Prefix + "/simple.json"));
            IgnoreErrors(route.FulfillAsync(new() { Response = response }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
    }

    [UpstreamTest("RO-109 should fulfill with fetch result and overrides")]
    public async Task ShouldFulfillWithFetchResultAndOverrides(IPage page, Server server, bool isElectron, int electronMajorVersion, Func<string, string> rewriteAndroidLoopbackURL)
    {
        Info.Skip(isElectron && electronMajorVersion < 30, "error: Browser context management is not supported.");
        await page.RouteAsync("**/*", async route =>
        {
            var response = await page.APIRequest.GetAsync(rewriteAndroidLoopbackURL(server.Prefix + "/simple.json"));
            IgnoreErrors(route.FulfillAsync(new()
            {
                Response = response,
                Status = 201,
                Headers = new Dictionary<string, string>
                {
                    ["Content-Type"] = "application/json", // Case matters for the tested behavior
                    ["foo"] = "bar",
                },
            }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That((await response.AllHeadersAsync())["foo"], Is.EqualTo("bar"));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
    }

    [UpstreamTest("RO-110 should fetch original request and fulfill")]
    public async Task ShouldFetchOriginalRequestAndFulfill(IPage page, Server server, bool isElectron, int electronMajorVersion, bool isAndroid)
    {
        Info.Skip(isElectron && electronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(isAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        await page.RouteAsync("**/*", async route =>
        {
            var response = await page.APIRequest.FetchAsync(route.Request);
            IgnoreErrors(route.FulfillAsync(new()
            {
                Response = response,
            }));
        });
        var response = await page.GotoAsync(server.Prefix + "/title.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await page.TitleAsync(), Is.EqualTo("Woof-Woof"));
    }

    [UpstreamTest("RO-111 should fulfill with multiple set-cookie")]
    public async Task ShouldFulfillWithMultipleSetCookie(IPage page, Server server, bool isElectron, int electronMajorVersion, bool isAndroid)
    {
        Info.Skip(isElectron && electronMajorVersion < 14, "Electron 14+ is required");
        Info.Skip(isAndroid, "Android does not have an isolated context per test, so we get cookies from other tests");

        var cookies = new[] { "a=b", "c=d" };
        await page.RouteAsync("**/multiple-set-cookie.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    ["X-Header-1"] = "v1",
                    ["Set-Cookie"] = string.Join("\n", cookies),
                    ["X-Header-2"] = "v2",
                },
                Body = "",
            }));
        });
        var response = await page.GotoAsync(server.Prefix + "/multiple-set-cookie.html");
        Assert.That((await page.EvaluateAsync<string>("() => document.cookie")).Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(cookies));
        Assert.That(await response.HeaderValueAsync("X-Header-1"), Is.EqualTo("v1"));
        Assert.That(await response.HeaderValueAsync("X-Header-2"), Is.EqualTo("v2"));
    }

    [UpstreamTest("RO-112 should fulfill with fetch response that has multiple set-cookie")]
    public async Task ShouldFulfillWithFetchResponseThatHasMultipleSetCookie(IPlaywright playwright, IPage page, Server server, bool isAndroid)
    {
        Info.Fixme(isAndroid);
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=b", "c=d" });
            res.SetHeader("Content-Type", "text/html");
            res.End();
        });
        await page.RouteAsync("**/empty.html", async route =>
        {
            var request = await playwright.APIRequest.NewContextAsync();
            var response = await request.FetchAsync(route.Request);
            IgnoreErrors(route.FulfillAsync(new() { Response = response }));
        });
        await page.GotoAsync(server.EmptyPage);
        var cookie = await page.EvaluateAsync<string>("() => document.cookie");
        Assert.That(cookie.Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "a=b", "c=d" }));
    }

    [UpstreamTest("RO-113 headerValue should return set-cookie from intercepted response")]
    public async Task HeaderValueShouldReturnSetCookieFromInterceptedResponse(IPage page, Server server, string browserName)
    {
        Info.Fixme(browserName == "chromium", "Set-Cookie is missing in response after interception");
        Info.Skip(browserName == "webkit", "Set-Cookie with \n in intercepted response does not pass validation in WebCore, see also https://github.com/microsoft/playwright/pull/9273");
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    ["Set-Cookie"] = "a=b",
                },
                Body = "",
            }));
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(await response.HeaderValueAsync("Set-Cookie"), Is.EqualTo("a=b"));
    }

    [UpstreamTest("RO-114 should fulfill with har response")]
    public async Task ShouldFulfillWithHarResponse(IPage page, Func<string, string> asset)
    {
        var harPath = asset("har-fulfill.har");
        var har = JsonNode.Parse(await File.ReadAllTextAsync(harPath));
        await page.RouteAsync("**/*", async route =>
        {
            var response = FindResponse(har, route.Request.Url);
            var headers = new Dictionary<string, string>();
            foreach (var header in response["headers"].AsArray())
            {
                headers[(string)header["name"]] = (string)header["value"];
            }
            await route.FulfillAsync(new()
            {
                Status = (int)response["status"],
                Headers = headers,
                // .NET: Buffer.from(text, encoding), where the encoding is 'base64' or 'utf-8'.
                BodyBytes = ((string)response["content"]["encoding"] ?? "utf-8") == "base64"
                    ? Convert.FromBase64String((string)response["content"]["text"] ?? "")
                    : Encoding.UTF8.GetBytes((string)response["content"]["text"] ?? ""),
            });
        });
        await page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script.
        Assert.That(await page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        // HAR contains a POST for the css file but we match ignoring the method, so the file should be served.
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(0, 255, 255)");
    }

    private static JsonNode FindResponse(JsonNode har, string url)
    {
        JsonNode entry = null;
        var originalUrl = url;
        while (url.Trim().Length > 0)
        {
            entry = har["log"]["entries"].AsArray().FirstOrDefault(entry => (string)entry["request"]["url"] == url);
            url = (string)entry?["response"]["redirectURL"];
        }
        Assert.That(entry, Is.Not.Null, originalUrl);
        return entry?["response"];
    }

    [UpstreamTest("RO-115 should fulfill preload link requests")]
    public async Task ShouldFulfillPreloadLinkRequests(IPage page, Server server, string browserName)
    {
        var intercepted = false;
        await page.RouteAsync("**/one-style.css", route =>
        {
            intercepted = true;
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    ["content-type"] = "text/css; charset=utf-8",
                    ["cache-control"] = "no-cache, no-store",
                    ["custom"] = "value",
                },
                Body = "body { background-color: green; }",
            }));
        });
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/one-style.css"),
            page.GotoAsync(server.Prefix + "/preload.html"));
        Assert.That(await response.HeaderValueAsync("custom"), Is.EqualTo("value"));
        await page.WaitForFunctionAsync("() => window.preloadedStyles");
        Assert.That(intercepted, Is.EqualTo(true));
        var color = await page.EvaluateAsync<string>("() => window.getComputedStyle(document.body).backgroundColor");
        Assert.That(color, Is.EqualTo("rgb(0, 128, 0)"));
    }

    [UpstreamTest("RO-116 should fulfill json")]
    public async Task ShouldFulfillJson(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.RouteAsync("**/data.json", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 201,
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
                Json = new { bar = "baz" },
            }));
        });
        var (response, body) = await PromiseAll(
            page.WaitForResponseAsync("**/*"),
            page.EvaluateAsync<string>("() => fetch('./data.json').then(r => r.text())"));
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(response.Headers["content-type"], Is.EqualTo("application/json"));
        Assert.That(body, Is.EqualTo(JsonSerializer.Serialize(new { bar = "baz" })));
    }

    [UpstreamTest("RO-117 should fulfill with gzip and readback")]
    public async Task ShouldFulfillWithGzipAndReadback(IPage page, Server server, bool isAndroid, bool isElectron, int electronMajorVersion)
    {
        Info.Skip(isAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        Info.Skip(isElectron && electronMajorVersion < 30, "error: Browser context management is not supported.");
        server.EnableGzip("/one-style.html");
        await page.RouteAsync("**/one-style.html", async route =>
        {
            var response = await route.FetchAsync();
            Assert.That(response.Headers["content-encoding"], Is.EqualTo("gzip"));
            await route.FulfillAsync(new() { Response = response });
        });

        var response = await page.GotoAsync(server.Prefix + "/one-style.html");
        await Expect(page.Locator("div")).ToHaveTextAsync("hello, world!");
        await Expect(page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
        Assert.That(await response.TextAsync(), Does.Contain("<div>hello, world!</div>"));
    }

    [UpstreamTest("RO-118 should not go to the network for fulfilled requests body")]
    public async Task ShouldNotGoToTheNetworkForFulfilledRequestsBody(IPage page, Server server)
    {
        await page.RouteAsync("**/one-style.css", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 404,
                ContentType = "text/plain",
                Body = "Not Found! (mocked)",
            });
        });

        var serverHit = false;
        server.SetRoute("/one-style.css", (req, res) =>
        {
            serverHit = true;
            res.SetHeader("Content-Type", "text/css");
            res.End("body { background-color: green; }");
        });

        var responsePromise = page.WaitForResponseAsync("**/one-style.css");
        await page.GotoAsync(server.Prefix + "/one-style.html");
        var response = await responsePromise;
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("Not Found! (mocked)"));
        Assert.That(serverHit, Is.EqualTo(false));
    }

    [UpstreamTest("RO-119 should return body for fulfilled responses")]
    public async Task ShouldReturnBodyForFulfilledResponses(IPage page, Server server)
    {
        foreach (var status in new[] { 100, 200, 404, 500 })
        {
            await Info.Step($"status {status}", async () =>
            {
                var bodyOverride = $"Custom body {status}";
                await page.RouteAsync("**/one-style.css", async route =>
                {
                    await route.FulfillAsync(new()
                    {
                        Status = status,
                        ContentType = "text/plain",
                        Body = bodyOverride,
                    });
                });

                var responsePromise = page.WaitForResponseAsync("**/one-style.css");
                await page.GotoAsync(server.Prefix + "/one-style.html");
                var response = await responsePromise;
                var body = await response.BodyAsync();
                Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo(bodyOverride));
            });
            await page.UnrouteAllAsync();
        }
    }
}
