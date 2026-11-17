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
public class PageRequestFulfillTests : UpstreamPageTest
{
    // We access test servers at 10.0.2.2 from inside the browser on Android,
    // which is actually forwarded to the desktop localhost.
    // To use request such an url with apiRequestContext on the desktop, we need to change it back to localhost.
    private string RewriteAndroidLoopbackURL(string givenURL)
    {
        if (!IsAndroid)
        {
            return givenURL;
        }
        var requestURL = new UriBuilder(givenURL);
        requestURL.Host = "localhost";
        return requestURL.Uri.ToString();
    }

    [UpstreamTest("RO-096 should work")]
    public async Task ShouldWork()
    {
        await Page.RouteAsync("**/*", route =>
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
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(response.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(await Page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-097 should work with buffer as body")]
    public async Task ShouldWorkWithBufferAsBody()
    {
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/plain",
                BodyBytes = Encoding.UTF8.GetBytes("Yo, page!"),
            }));
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await Page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-098 should work with status code 422")]
    public async Task ShouldWorkWithStatusCode422()
    {
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 422,
                Body = "Yo, page!",
            }));
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(422));
        Assert.That(response.StatusText, Is.EqualTo("Unprocessable Entity"));
        Assert.That(await Page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-099 should fulfill with unuassigned status codes")]
    public async Task ShouldFulfillWithUnuassignedStatusCodes()
    {
        // .NET: a promise that is resolved with a promise is a task of a task.
        var fulfillPromise = new TaskCompletionSource<Task<Exception>>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/data.json", route =>
        {
            fulfillPromise.SetResult(Catch(() => route.FulfillAsync(new()
            {
                Status = 430,
                Body = "Yo, page!",
            })));
        });
        await Page.GotoAsync(Server.EmptyPage);
        var response = await Page.EvaluateAsync<JsonElement>(@"async url => {
    const { status, statusText } = await fetch(url);
    return { status, statusText };
  }", Server.Prefix + "/data.json");
        var error = await await fulfillPromise.Task;
        Assert.That(error, Is.Null);
        Assert.That(response.GetProperty("status").GetInt32(), Is.EqualTo(430));
        Assert.That(response.GetProperty("statusText").GetString(), Is.EqualTo("Unknown"));
    }

    [UpstreamTest("RO-100 should not throw if request was cancelled by the page")]
    public async Task ShouldNotThrowIfRequestWasCancelledByThePage()
    {
        var interceptPromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/data.json", route => interceptPromise.SetResult(route));
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"url => {
    globalThis.controller = new AbortController();
    fetch(url, { signal: globalThis.controller.signal }).catch(() => {});
  }", Server.Prefix + "/data.json");
        var route = await interceptPromise.Task;
        var failurePromise = Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h);
        await Page.EvaluateAsync("() => globalThis.controller.abort()");
        var cancelledRequest = await failurePromise;
        // .NET: request.Failure is the error text.
        Assert.That(cancelledRequest.Failure, Is.Not.Null);
        Assert.That(cancelledRequest.Failure, Does.Match("cancelled|aborted").IgnoreCase);
        await route.FulfillAsync(new() { Status = 200 }); // Should not throw.
    }

    [UpstreamTest("RO-101 should allow mocking binary responses")]
    public async Task ShouldAllowMockingBinaryResponses()
    {
        Info.Skip(BrowserName == "firefox" && !Headless, "Firefox headed produces a different image.");
        Info.Skip(IsAndroid);

        await Page.RouteAsync("**/*", route =>
        {
            var imageBuffer = File.ReadAllBytes(Asset("pptr.png"));
            IgnoreErrors(route.FulfillAsync(new()
            {
                ContentType = "image/png",
                BodyBytes = imageBuffer,
            }));
        });
        await Page.EvaluateAsync(@"PREFIX => {
    const img = document.createElement('img');
    img.src = PREFIX + '/does-not-exist.png';
    document.body.appendChild(img);
    return new Promise(fulfill => img.onload = fulfill);
  }", Server.Prefix);
        var img = await Page.QuerySelectorAsync("img");
        ExpectToMatchSnapshot(await img.ScreenshotAsync(), "mock-binary-response.png");
    }

    [UpstreamTest("RO-102 should allow mocking svg with charset")]
    public async Task ShouldAllowMockingSvgWithCharset()
    {
        Info.Skip(BrowserName == "firefox" && !Headless, "Firefox headed produces a different image.");
        Info.Skip(IsAndroid);
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "Protocol error (Storage.getCookies): Browser context management is not supported");

        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                ContentType = "image/svg+xml ; charset=utf-8",
                Body = "<svg width=\"50\" height=\"50\" version=\"1.1\" xmlns=\"http://www.w3.org/2000/svg\"><rect x=\"10\" y=\"10\" width=\"30\" height=\"30\" stroke=\"black\" fill=\"transparent\" stroke-width=\"5\"/></svg>",
            }));
        });
        await Page.EvaluateAsync(@"PREFIX => {
    const img = document.createElement('img');
    img.src = PREFIX + '/does-not-exist.svg';
    document.body.appendChild(img);
    return new Promise((f, r) => { img.onload = f; img.onerror = r; });
  }", Server.Prefix);
        var img = await Page.QuerySelectorAsync("img");
        ExpectToMatchSnapshot(await img.ScreenshotAsync(), "mock-svg.png");
    }

    [UpstreamTest("RO-103 should work with file path")]
    public async Task ShouldWorkWithFilePath()
    {
        Info.Skip(IsAndroid);

        await Page.RouteAsync("**/*", route => route.FulfillAsync(new() { ContentType = "shouldBeIgnored", Path = Asset("pptr.png") }));
        await Page.EvaluateAsync(@"PREFIX => {
    const img = document.createElement('img');
    img.src = PREFIX + '/does-not-exist.png';
    document.body.appendChild(img);
    return new Promise(fulfill => img.onload = fulfill);
  }", Server.Prefix);
        var img = await Page.QuerySelectorAsync("img");
        ExpectToMatchSnapshot(await img.ScreenshotAsync(), "mock-binary-response.png");
    }

    [UpstreamTest("RO-104 should stringify intercepted request response headers")]
    public async Task ShouldStringifyInterceptedRequestResponseHeaders()
    {
        await Page.RouteAsync("**/*", route =>
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
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        var headers = response.Headers;
        Assert.That(headers["foo"], Is.EqualTo("true"));
        Assert.That(await Page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-105 should not modify the headers sent to the server")]
    public async Task ShouldNotModifyTheHeadersSentToTheServer()
    {
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var interceptedRequests = new List<ServerRequest>();

        // this is just to enable request interception, which disables caching in chromium
        await Page.RouteAsync(Server.Prefix + "/unused", route => { });

        Server.SetRoute("/something", (request, response) =>
        {
            interceptedRequests.Add(request);
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });

        var text = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", Server.CrossProcessPrefix + "/something");
        Assert.That(text, Is.EqualTo("done"));

        await Page.RouteAsync(Server.CrossProcessPrefix + "/something", route =>
        {
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = new Dictionary<string, string>(route.Request.Headers),
            }));
        });

        var textAfterRoute = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", Server.CrossProcessPrefix + "/something");
        Assert.That(textAfterRoute, Is.EqualTo("done"));

        Assert.That(interceptedRequests.Count, Is.EqualTo(2));
        ExpectToEqual(interceptedRequests[1].Headers, interceptedRequests[0].Headers);
    }

    [UpstreamTest("RO-106 should include the origin header")]
    public async Task ShouldIncludeTheOriginHeader()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");

        await Page.GotoAsync(Server.Prefix + "/empty.html");
        IRequest interceptedRequest = null;
        await Page.RouteAsync(Server.CrossProcessPrefix + "/something", route =>
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

        var text = await Page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", Server.CrossProcessPrefix + "/something");
        Assert.That(text, Is.EqualTo("done"));
        Assert.That(interceptedRequest.Headers["origin"], Is.EqualTo(Server.Prefix));
    }

    [UpstreamTest("RO-107 should fulfill with global fetch result")]
    public async Task ShouldFulfillWithGlobalFetchResult()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        await Page.RouteAsync("**/*", async route =>
        {
            var request = await Playwright.APIRequest.NewContextAsync();
            var response = await request.GetAsync(RewriteAndroidLoopbackURL(Server.Prefix + "/simple.json"));
            IgnoreErrors(route.FulfillAsync(new() { Response = response }));
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
    }

    [UpstreamTest("RO-108 should fulfill with fetch result")]
    public async Task ShouldFulfillWithFetchResult()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        await Page.RouteAsync("**/*", async route =>
        {
            var response = await Page.APIRequest.GetAsync(RewriteAndroidLoopbackURL(Server.Prefix + "/simple.json"));
            IgnoreErrors(route.FulfillAsync(new() { Response = response }));
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(200));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
    }

    [UpstreamTest("RO-109 should fulfill with fetch result and overrides")]
    public async Task ShouldFulfillWithFetchResultAndOverrides()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        await Page.RouteAsync("**/*", async route =>
        {
            var response = await Page.APIRequest.GetAsync(RewriteAndroidLoopbackURL(Server.Prefix + "/simple.json"));
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
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That((await response.AllHeadersAsync())["foo"], Is.EqualTo("bar"));
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
    }

    [UpstreamTest("RO-110 should fetch original request and fulfill")]
    public async Task ShouldFetchOriginalRequestAndFulfill()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        await Page.RouteAsync("**/*", async route =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            IgnoreErrors(route.FulfillAsync(new()
            {
                Response = response,
            }));
        });
        var response = await Page.GotoAsync(Server.Prefix + "/title.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(await Page.TitleAsync(), Is.EqualTo("Woof-Woof"));
    }

    [UpstreamTest("RO-111 should fulfill with multiple set-cookie")]
    public async Task ShouldFulfillWithMultipleSetCookie()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 14, "Electron 14+ is required");
        Info.Skip(IsAndroid, "Android does not have an isolated context per test, so we get cookies from other tests");

        var cookies = new[] { "a=b", "c=d" };
        await Page.RouteAsync("**/multiple-set-cookie.html", route =>
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
        var response = await Page.GotoAsync(Server.Prefix + "/multiple-set-cookie.html");
        Assert.That((await Page.EvaluateAsync<string>("() => document.cookie")).Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(cookies));
        Assert.That(await response.HeaderValueAsync("X-Header-1"), Is.EqualTo("v1"));
        Assert.That(await response.HeaderValueAsync("X-Header-2"), Is.EqualTo("v2"));
    }

    [UpstreamTest("RO-112 should fulfill with fetch response that has multiple set-cookie")]
    public async Task ShouldFulfillWithFetchResponseThatHasMultipleSetCookie()
    {
        Info.Fixme(IsAndroid);
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("Set-Cookie", new[] { "a=b", "c=d" });
            res.SetHeader("Content-Type", "text/html");
            res.End();
        });
        await Page.RouteAsync("**/empty.html", async route =>
        {
            var request = await Playwright.APIRequest.NewContextAsync();
            var response = await request.FetchAsync(route.Request);
            IgnoreErrors(route.FulfillAsync(new() { Response = response }));
        });
        await Page.GotoAsync(Server.EmptyPage);
        var cookie = await Page.EvaluateAsync<string>("() => document.cookie");
        Assert.That(cookie.Split(';').Select(s => s.Trim()).Order(StringComparer.Ordinal), Is.EqualTo(new[] { "a=b", "c=d" }));
    }

    [UpstreamTest("RO-113 headerValue should return set-cookie from intercepted response")]
    public async Task HeaderValueShouldReturnSetCookieFromInterceptedResponse()
    {
        Info.Fixme(BrowserName == "chromium", "Set-Cookie is missing in response after interception");
        Info.Skip(BrowserName == "webkit", "Set-Cookie with \n in intercepted response does not pass validation in WebCore, see also https://github.com/microsoft/playwright/pull/9273");
        await Page.RouteAsync("**/empty.html", route =>
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
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(await response.HeaderValueAsync("Set-Cookie"), Is.EqualTo("a=b"));
    }

    [UpstreamTest("RO-114 should fulfill with har response")]
    public async Task ShouldFulfillWithHarResponse()
    {
        var harPath = Asset("har-fulfill.har");
        var har = JsonNode.Parse(await File.ReadAllTextAsync(harPath));
        await Page.RouteAsync("**/*", async route =>
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
        await Page.GotoAsync("http://no.playwright/");
        // HAR contains a redirect for the script.
        Assert.That(await Page.EvaluateAsync<string>("window.value"), Is.EqualTo("foo"));
        // HAR contains a POST for the css file but we match ignoring the method, so the file should be served.
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(0, 255, 255)");
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
    public async Task ShouldFulfillPreloadLinkRequests()
    {
        var intercepted = false;
        await Page.RouteAsync("**/one-style.css", route =>
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
            Page.WaitForResponseAsync("**/one-style.css"),
            Page.GotoAsync(Server.Prefix + "/preload.html"));
        Assert.That(await response.HeaderValueAsync("custom"), Is.EqualTo("value"));
        await Page.WaitForFunctionAsync("() => window.preloadedStyles");
        Assert.That(intercepted, Is.EqualTo(true));
        var color = await Page.EvaluateAsync<string>("() => window.getComputedStyle(document.body).backgroundColor");
        Assert.That(color, Is.EqualTo("rgb(0, 128, 0)"));
    }

    [UpstreamTest("RO-116 should fulfill json")]
    public async Task ShouldFulfillJson()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/data.json", route =>
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
            Page.WaitForResponseAsync("**/*"),
            Page.EvaluateAsync<string>("() => fetch('./data.json').then(r => r.text())"));
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(response.Headers["content-type"], Is.EqualTo("application/json"));
        Assert.That(body, Is.EqualTo(JsonSerializer.Serialize(new { bar = "baz" })));
    }

    [UpstreamTest("RO-117 should fulfill with gzip and readback")]
    public async Task ShouldFulfillWithGzipAndReadback()
    {
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Server.EnableGzip("/one-style.html");
        await Page.RouteAsync("**/one-style.html", async route =>
        {
            var response = await route.FetchAsync();
            Assert.That(response.Headers["content-encoding"], Is.EqualTo("gzip"));
            await route.FulfillAsync(new() { Response = response });
        });

        var response = await Page.GotoAsync(Server.Prefix + "/one-style.html");
        await Expect(Page.Locator("div")).ToHaveTextAsync("hello, world!");
        await Expect(Page.Locator("body")).ToHaveCSSAsync("background-color", "rgb(255, 192, 203)");
        Assert.That(await response.TextAsync(), Does.Contain("<div>hello, world!</div>"));
    }

    [UpstreamTest("RO-118 should not go to the network for fulfilled requests body")]
    public async Task ShouldNotGoToTheNetworkForFulfilledRequestsBody()
    {
        await Page.RouteAsync("**/one-style.css", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 404,
                ContentType = "text/plain",
                Body = "Not Found! (mocked)",
            });
        });

        var serverHit = false;
        Server.SetRoute("/one-style.css", (req, res) =>
        {
            serverHit = true;
            res.SetHeader("Content-Type", "text/css");
            res.End("body { background-color: green; }");
        });

        var responsePromise = Page.WaitForResponseAsync("**/one-style.css");
        await Page.GotoAsync(Server.Prefix + "/one-style.html");
        var response = await responsePromise;
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("Not Found! (mocked)"));
        Assert.That(serverHit, Is.EqualTo(false));
    }

    [UpstreamTest("RO-119 should return body for fulfilled responses")]
    public async Task ShouldReturnBodyForFulfilledResponses()
    {
        foreach (var status in new[] { 100, 200, 404, 500 })
        {
            await Info.Step($"status {status}", async () =>
            {
                var bodyOverride = $"Custom body {status}";
                await Page.RouteAsync("**/one-style.css", async route =>
                {
                    await route.FulfillAsync(new()
                    {
                        Status = status,
                        ContentType = "text/plain",
                        Body = bodyOverride,
                    });
                });

                var responsePromise = Page.WaitForResponseAsync("**/one-style.css");
                await Page.GotoAsync(Server.Prefix + "/one-style.html");
                var response = await responsePromise;
                var body = await response.BodyAsync();
                Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo(bodyOverride));
            });
            await Page.UnrouteAllAsync();
        }
    }
}
