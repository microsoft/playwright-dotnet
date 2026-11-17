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
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-request-intercept.spec.ts.</summary>
public class PageRequestInterceptTests : UpstreamPageTest
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

    [UpstreamTest("RO-134 should fulfill intercepted response")]
    public async Task ShouldFulfillInterceptedResponse()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        await Page.RouteAsync("**/*", async route =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new()
            {
                Response = response,
                Status = 201,
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                },
                ContentType = "text/plain",
                Body = "Yo, page!",
            });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(response.Headers["foo"], Is.EqualTo("bar"));
        Assert.That(response.Headers["content-type"], Is.EqualTo("text/plain"));
        Assert.That(await Page.EvaluateAsync<string>("() => document.body.textContent"), Is.EqualTo("Yo, page!"));
    }

    [UpstreamTest("RO-135 should fulfill response with empty body")]
    public async Task ShouldFulfillResponseWithEmptyBody()
    {
        Info.Skip(BrowserName == "chromium" && BrowserMajorVersion <= 91, "Fails in Electron that uses old Chromium");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        await Page.RouteAsync("**/*", async route =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new()
            {
                Response = response,
                Headers = new Dictionary<string, string> { ["content-length"] = "0" },
                Status = 201,
                Body = "",
            });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/title.html");
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(await response.TextAsync(), Is.EqualTo(""));
    }

    [UpstreamTest("RO-136 should override with defaults when intercepted response not provided")]
    public async Task ShouldOverrideWithDefaultsWhenInterceptedResponseNotProvided()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        Server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("foo", "bar");
            res.End("my content");
        });
        await Page.RouteAsync("**/*", async route =>
        {
            await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new()
            {
                Status = 201,
            });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(await response.TextAsync(), Is.EqualTo(""));
        if (BrowserName == "webkit")
        {
            ExpectToEqual(response.Headers, new Dictionary<string, object> { ["content-type"] = "text/plain" });
        }
        else
        {
            ExpectToEqual(response.Headers, new { });
        }
    }

    [UpstreamTest("RO-137 should fulfill with any response")]
    public async Task ShouldFulfillWithAnyResponse()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");

        Server.SetRoute("/sample", (req, res) =>
        {
            res.SetHeader("foo", "bar");
            res.End("Woo-hoo");
        });
        var sampleResponse = await Page.APIRequest.GetAsync(RewriteAndroidLoopbackURL($"{Server.Prefix}/sample"));

        await Page.RouteAsync("**/*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Response = sampleResponse,
                Status = 201,
                ContentType = "text/plain",
            });
        });
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Status, Is.EqualTo(201));
        Assert.That(await response.TextAsync(), Is.EqualTo("Woo-hoo"));
        Assert.That(response.Headers["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("RO-138 should support fulfill after intercept")]
    public async Task ShouldSupportFulfillAfterIntercept()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        var requestPromise = Server.WaitForRequest("/title.html");
        await Page.RouteAsync("**", async route =>
        {
            var response = await Page.APIRequest.FetchAsync(route.Request);
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/title.html");
        var request = await requestPromise;
        Assert.That(request.Url, Is.EqualTo("/title.html"));
        // .NET: path.join(__dirname, '..', 'assets', 'title.html') is the asset.
        var original = await File.ReadAllTextAsync(Asset("title.html"));
        Assert.That(await response.TextAsync(), Is.EqualTo(original));
    }

    [UpstreamTest("RO-139 should give access to the intercepted response")]
    public async Task ShouldGiveAccessToTheInterceptedResponse()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        await Page.GotoAsync(Server.EmptyPage);

        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/title.html", route => routePromise.TrySetResult(route));

        var evalPromise = Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/title.html");

        var route = await routePromise.Task;
        var response = await Page.APIRequest.FetchAsync(route.Request);

        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.StatusText, Is.EqualTo("OK"));
        Assert.That(response.Ok, Is.True);
        Assert.That(response.Url.EndsWith("/title.html", StringComparison.Ordinal), Is.EqualTo(true));
        Assert.That(response.Headers["content-type"], Is.EqualTo("text/html; charset=utf-8"));
        ExpectToEqual(response.HeadersArray.Where(header => header.Name.ToLowerInvariant() == "content-type"), new[] { new { name = "Content-Type", value = "text/html; charset=utf-8" } });

        await PromiseAll(route.FulfillAsync(new() { Response = response }), evalPromise);
    }

    [UpstreamTest("RO-140 should give access to the intercepted response body")]
    public async Task ShouldGiveAccessToTheInterceptedResponseBody()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        await Page.GotoAsync(Server.EmptyPage);

        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync("**/simple.json", route => routePromise.TrySetResult(route));

        // .NET: .catch(() => {}) of a promise that the test awaits later.
        var evalPromise = Catch(() => Page.EvaluateAsync("url => fetch(url)", Server.Prefix + "/simple.json"));

        var route = await routePromise.Task;
        var response = await Page.APIRequest.FetchAsync(route.Request);

        Assert.That(await response.TextAsync(), Is.EqualTo("{\"foo\": \"bar\"}\n"));

        await PromiseAll(route.FulfillAsync(new() { Response = response }), evalPromise);
    }

    [UpstreamTest("RO-141 should intercept multipart/form-data request body")]
    public async Task ShouldInterceptMultipartFormDataRequestBody()
    {
        Info.Fixme(BrowserName != "firefox");
        await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
        var filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Asset("file-to-upload.txt"));
        await Page.Locator("input[type=file]").SetInputFilesAsync(filePath);
        // .NET: the executor of the promise starts page.route() and does not await it.
        var requestPromise = new TaskCompletionSource<IRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        IgnoreErrors(Page.RouteAsync("**/upload", route =>
        {
            requestPromise.TrySetResult(route.Request);
        }));
        var request = await PromiseAll(
            requestPromise.Task,
            Page.ClickAsync("input[type=submit]", new() { NoWaitAfter = true }));
        Assert.That(request.Method, Is.EqualTo("POST"));
        Assert.That(request.PostData, Does.Contain(File.ReadAllText(filePath)));
    }

    [UpstreamTest("RO-142 should fulfill intercepted response using alias")]
    public async Task ShouldFulfillInterceptedResponseUsingAlias()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        await Page.RouteAsync("**/*", async route =>
        {
            var response = await route.FetchAsync();
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Headers["content-type"], Does.Contain("text/html"));
    }

    [UpstreamTest("RO-143 should support timeout option in route.fetch")]
    public async Task ShouldSupportTimeoutOptionInRouteFetch()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");

        Server.SetRoute("/slow", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["content-length"] = 4096,
                ["content-type"] = "text/html",
            });
        });
        await Page.RouteAsync("**/*", async route =>
        {
            var error = await Catch(() => route.FetchAsync(new() { Timeout = 1000 }));
            Assert.That(error.Message, Does.Contain("route.fetch: Timeout 1000ms exceeded"));
        });
        var error = await Catch(() => Page.GotoAsync(Server.Prefix + "/slow", new() { Timeout = 2000 }));
        Assert.That(error.Message, Does.Contain("Timeout 2000ms exceeded"));
    }

    [UpstreamTest("RO-144 should not follow redirects when maxRedirects is set to 0 in route.fetch")]
    public async Task ShouldNotFollowRedirectsWhenMaxRedirectsIsSetTo0InRouteFetch()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");

        Server.SetRedirect("/foo", "/empty.html");
        await Page.RouteAsync("**/*", async route =>
        {
            var response = await route.FetchAsync(new() { MaxRedirects = 0 });
            Assert.That(response.Headers["location"], Is.EqualTo("/empty.html"));
            Assert.That(response.Status, Is.EqualTo(302));
            await route.FulfillAsync(new() { Body = "hello" });
        });
        await Page.GotoAsync(Server.Prefix + "/foo");
        Assert.That(await Page.ContentAsync(), Does.Contain("hello"));
    }

    [UpstreamTest("RO-145 should intercept with url override")]
    public async Task ShouldInterceptWithUrlOverride()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        await Page.RouteAsync("**/*.html", async route =>
        {
            var response = await route.FetchAsync(new() { Url = Server.Prefix + "/one-style.html" });
            await route.FulfillAsync(new() { Response = response });
        });
        var response = await Page.GotoAsync(Server.Prefix + "/empty.html");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(Encoding.UTF8.GetString(await response.BodyAsync()), Does.Contain("one-style.css"));
    }

    [UpstreamTest("RO-146 should intercept with post data override")]
    public async Task ShouldInterceptWithPostDataOverride()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 30, "error: Browser context management is not supported.");
        Info.Skip(IsAndroid, "The internal Android localhost (10.0.0.2) != the localhost on the host");
        var requestPromise = Server.WaitForRequest("/empty.html");
        await Page.RouteAsync("**/*.html", async route =>
        {
            var response = await route.FetchAsync(new()
            {
                // .NET: postData is a byte[]: { 'foo': 'bar' } is the JSON that the Node.js client makes of it.
                PostData = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(new { foo = "bar" })),
            });
            await route.FulfillAsync(new() { Response = response });
        });
        await Page.GotoAsync(Server.Prefix + "/empty.html");
        var request = await requestPromise;
        Assert.That(Encoding.UTF8.GetString(await request.PostBody), Is.EqualTo(JsonSerializer.Serialize(new { foo = "bar" })));
    }

    [UpstreamTest("RO-147 request.postData is not null when fetching FormData with a Blob")]
    public async Task RequestPostDataIsNotNullWhenFetchingFormDataWithABlob()
    {
        Info.Skip(IsElectron && ElectronMajorVersion < 31);
        Info.Fixme(IsAndroid, "postData is null for some reason");
        Info.Fixme(BrowserName == "webkit", "The body is empty in WebKit when intercepting");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
<script>
  function doStuff() {
    const formData = new FormData();
    formData.append('file', new Blob([""hello""], { type: ""text/plain"" }));
    fetch('/upload', {
      method: 'POST',
      body: formData
    });
  }
</script>
<body>
<button onclick=""doStuff()"" data-testid=""click-me"">Click me!</button>
</body>");
        var postDataPromise = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync(Server.Prefix + "/upload", async route =>
        {
            Assert.That(route.Request.Method, Is.EqualTo("POST"));
            postDataPromise.TrySetResult(route.Request.PostData);
            await route.FulfillAsync(new()
            {
                Status = 200,
                Body = "ok",
            });
        });
        await Page.GetByTestId("click-me").ClickAsync();
        var postData = await postDataPromise.Task;
        Assert.That(postData, Does.Contain("Content-Disposition: form-data; name=\"file\"; filename=\"blob\""));
        Assert.That(postData, Does.Contain("\r\nhello\r\n"));
    }

    [UpstreamTest("RO-148 should abort favicon requests if interception is enabled")]
    public async Task ShouldAbortFaviconRequestsIfInterceptionIsEnabled()
    {
        var requestCount = 0;
        Server.SetRoute("/favicon.ico", (req, res) =>
        {
            ++requestCount;
            res.SetHeader("content-type", "text/plain");
            res.End("my content");
        });
        // Intercept all requests.
        await Page.RouteAsync("**/*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                Body = "Hello, world!",
            });
        });
        await Page.GotoAsync(Server.EmptyPage);
        var response = await Page.EvaluateAsync<string>("() => fetch('/favicon.ico').then(r => r.text()).catch(e => 'load failed')");
        Assert.That(response, Is.EqualTo("load failed"));
        // Browsers can send favicon requests in the background.
        await Task.Delay(1000);
        Assert.That(requestCount, Is.EqualTo(0));
    }
}
