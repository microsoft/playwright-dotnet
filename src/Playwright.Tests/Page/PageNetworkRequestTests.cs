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
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-network-request.spec.ts.</summary>
public class PageNetworkRequestTests
{
    private static Dictionary<string, string> AdjustServerHeaders(ServerHeaders headers, string browserName)
    {
        // .NET: the headers of a server request are read-only, so delete headers['priority'] works on a copy.
        var result = new Dictionary<string, string>(headers);
        if (browserName == "firefox")
        {
            result.Remove("priority");
        }
        return result;
    }

    [UpstreamTest("NE-001 should work for main frame navigation request")]
    public async Task ShouldWorkForMainFrameNavigationRequest(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Frame, Is.SameAs(page.MainFrame));
    }

    [UpstreamTest("NE-002 should work for subframe navigation request")]
    public async Task ShouldWorkForSubframeNavigationRequest(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await AttachFrame(page, "frame1", server.EmptyPage);
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Frame, Is.SameAs(page.Frames[1]));
    }

    [UpstreamTest("NE-003 should work for fetch requests @smoke")]
    public async Task ShouldWorkForFetchRequests(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.EvaluateAsync("() => fetch('/digits/1.png')");
        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Frame, Is.SameAs(page.MainFrame));
    }

    [UpstreamTest("NE-004 should work for a redirect")]
    public async Task ShouldWorkForARedirect(IPage page, Server server)
    {
        server.SetRedirect("/foo.html", "/empty.html");
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.Prefix + "/foo.html");

        Assert.That(requests.Count, Is.EqualTo(2));
        Assert.That(requests[0].Url, Is.EqualTo(server.Prefix + "/foo.html"));
        Assert.That(requests[1].Url, Is.EqualTo(server.Prefix + "/empty.html"));
    }

    // https://github.com/microsoft/playwright/issues/3993
    [UpstreamTest("NE-005 should not work for a redirect and interception")]
    public async Task ShouldNotWorkForARedirectAndInterception(IPage page, Server server)
    {
        server.SetRedirect("/foo.html", "/empty.html");
        var requests = new List<IRequest>();
        await page.RouteAsync("**", route =>
        {
            requests.Add(route.Request);
            IgnoreErrors(route.ContinueAsync());
        });
        await page.GotoAsync(server.Prefix + "/foo.html");

        Assert.That(page.Url, Is.EqualTo(server.Prefix + "/empty.html"));

        Assert.That(requests.Count, Is.EqualTo(1));
        Assert.That(requests[0].Url, Is.EqualTo(server.Prefix + "/foo.html"));
    }

    [UpstreamTest("NE-006 should return headers")]
    public async Task ShouldReturnHeaders(IPage page, Server server, string browserName)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        if (browserName == "chromium")
        {
            Assert.That(response.Request.Headers["user-agent"], Does.Contain("Chrome"));
        }
        else if (browserName == "firefox")
        {
            Assert.That(response.Request.Headers["user-agent"], Does.Contain("Firefox"));
        }
        else if (browserName == "webkit")
        {
            Assert.That(response.Request.Headers["user-agent"], Does.Contain("WebKit"));
        }
    }

    [UpstreamTest("NE-007 should get the same headers as the server")]
    public async Task ShouldGetTheSameHeadersAsTheServer(IPage page, Server server, string browserName, string platform, bool isElectron, int browserMajorVersion, string channel)
    {
        It.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");
        It.Fixme(browserName == "webkit" && platform == "win32" && channel != "webkit-wsl", "Curl does not show accept-encoding and accept-language");
        ServerRequest serverRequest = null;
        server.SetRoute("/empty.html", (request, response) =>
        {
            serverRequest = request;
            response.End("done");
        });
        var response = await page.GotoAsync(server.Prefix + "/empty.html");
        var headers = await response.Request.AllHeadersAsync();
        ExpectToEqual(headers, AdjustServerHeaders(serverRequest.Headers, browserName));
    }

    [UpstreamTest("NE-008 should not return allHeaders() until they are available")]
    public async Task ShouldNotReturnAllHeadersUntilTheyAreAvailable(IPage page, Server server, string browserName, string platform, bool isElectron, int browserMajorVersion, string channel)
    {
        It.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");
        It.Fixme(browserName == "webkit" && platform == "win32" && channel != "webkit-wsl", "Curl does not show accept-encoding and accept-language");

        Task<Dictionary<string, string>> requestHeadersPromise = null;
        page.Request += (_, request) => requestHeadersPromise = request.AllHeadersAsync();
        Task<Dictionary<string, string>> responseHeadersPromise = null;
        page.Response += (_, response) => responseHeadersPromise = response.AllHeadersAsync();

        ServerRequest serverRequest = null;
        server.SetRoute("/empty.html", async (request, response) =>
        {
            serverRequest = request;
            response.WriteHead(200, new() { ["foo"] = "bar" });
            await Task.Delay(3000);
            response.End("done");
        });

        await page.GotoAsync(server.Prefix + "/empty.html");
        var requestHeaders = await requestHeadersPromise;
        ExpectToEqual(requestHeaders, AdjustServerHeaders(serverRequest.Headers, browserName));

        var responseHeaders = await responseHeadersPromise;
        Assert.That(responseHeaders["foo"], Is.EqualTo("bar"));
    }

    [UpstreamTest("NE-009 should get the same headers as the server CORS")]
    public async Task ShouldGetTheSameHeadersAsTheServerCORS(IPage page, Server server, string browserName, string platform, bool isElectron, int browserMajorVersion, string channel)
    {
        It.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");
        It.Fixme(browserName == "webkit" && platform == "win32" && channel != "webkit-wsl", "Curl does not show accept-encoding and accept-language");

        await page.GotoAsync(server.Prefix + "/empty.html");
        ServerRequest serverRequest = null;
        server.SetRoute("/something", (request, response) =>
        {
            serverRequest = request;
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        var responsePromise = page.WaitForResponseAsync(_ => true);
        var text = await page.EvaluateAsync<string>(@"async url => {
    const data = await fetch(url);
    return data.text();
  }", server.CrossProcessPrefix + "/something");
        Assert.That(text, Is.EqualTo("done"));
        var response = await responsePromise;
        var headers = await response.Request.AllHeadersAsync();
        ExpectToEqual(headers, AdjustServerHeaders(serverRequest.Headers, browserName));
    }

    [UpstreamTest("NE-010 should not get preflight CORS requests when intercepting")]
    public async Task ShouldNotGetPreflightCORSRequestsWhenIntercepting(IPage page, Server server, string browserName, bool isAndroid, bool isBidi)
    {
        It.Fixme(isAndroid, "Playwright does not get CORS pre-flight on Android");
        await page.GotoAsync(server.Prefix + "/empty.html");

        var requests = new List<string>();
        server.SetRoute("/something", (request, response) =>
        {
            requests.Add(request.Method);
            if (request.Method == "OPTIONS")
            {
                response.WriteHead(204, new()
                {
                    ["Access-Control-Allow-Origin"] = "*",
                    ["Access-Control-Allow-Methods"] = "POST, GET, OPTIONS, DELETE",
                    ["Access-Control-Allow-Headers"] = "*",
                    ["Cache-Control"] = "no-cache",
                });
                response.End();
                return;
            }
            response.WriteHead(200, new() { ["Access-Control-Allow-Origin"] = "*" });
            response.End("done");
        });
        // First check the browser will send preflight request when interception is OFF.
        {
            var text = await page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url, {
        method: 'DELETE',
        headers: { 'X-PINGOTHER': 'pingpong' }
      });
      return data.text();
    }", server.CrossProcessPrefix + "/something");
            Assert.That(text, Is.EqualTo("done"));
            Assert.That(requests, Is.EqualTo(new[] { "OPTIONS", "DELETE" }));
        }

        // Now check the browser will NOT send preflight request when interception is ON.
        {
            requests.Clear();
            var routed = new List<string>();
            await page.RouteAsync("**/something", route =>
            {
                routed.Add(route.Request.Method);
                IgnoreErrors(route.ContinueAsync());
            });

            var text = await page.EvaluateAsync<string>(@"async url => {
      const data = await fetch(url, {
        method: 'DELETE',
        headers: { 'X-PINGOTHER': 'pingpong' }
      });
      return data.text();
    }", server.CrossProcessPrefix + "/something");
            Assert.That(text, Is.EqualTo("done"));
            // Check that there was no preflight (OPTIONS) request.
            Assert.That(routed, Is.EqualTo(new[] { "DELETE" }));
            if (browserName == "firefox" && !isBidi)
            {
                Assert.That(requests, Is.EqualTo(new[] { "OPTIONS", "DELETE" }));
            }
            else
            {
                Assert.That(requests, Is.EqualTo(new[] { "DELETE" }));
            }
        }
    }

    [UpstreamTest("NE-011 should return postData")]
    public async Task ShouldReturnPostData(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/post", (req, res) => res.End());
        IRequest request = null;
        page.Request += (_, r) => request = r;
        await page.EvaluateAsync("() => fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar' }) })");
        Assert.That(request, Is.Not.Null);
        Assert.That(request.PostData, Is.EqualTo("{\"foo\":\"bar\"}"));
    }

    [UpstreamTest("NE-012 should work with binary post data")]
    public async Task ShouldWorkWithBinaryPostData(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/post", (req, res) => res.End());
        IRequest request = null;
        page.Request += (_, r) => request = r;
        await page.EvaluateAsync(@"async () => {
    await fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) });
  }");
        Assert.That(request, Is.Not.Null);
        var buffer = request.PostDataBuffer;
        Assert.That(buffer.Length, Is.EqualTo(256));
        for (var i = 0; i < 256; ++i)
        {
            Assert.That(buffer[i], Is.EqualTo(i));
        }
    }

    [UpstreamTest("NE-013 should work with binary post data and interception")]
    public async Task ShouldWorkWithBinaryPostDataAndInterception(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/post", (req, res) => res.End());
        IRequest request = null;
        await page.RouteAsync("/post", route => route.ContinueAsync());
        page.Request += (_, r) => request = r;
        await page.EvaluateAsync(@"async () => {
    await fetch('./post', { method: 'POST', body: new Uint8Array(Array.from(Array(256).keys())) });
  }");
        Assert.That(request, Is.Not.Null);
        var buffer = request.PostDataBuffer;
        Assert.That(buffer.Length, Is.EqualTo(256));
        for (var i = 0; i < 256; ++i)
        {
            Assert.That(buffer[i], Is.EqualTo(i));
        }
    }

    [UpstreamTest("NE-014 should override post data content type")]
    public async Task ShouldOverridePostDataContentType(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        ServerRequest request = null;
        server.SetRoute("/post", (req, res) =>
        {
            request = req;
            res.End();
        });
        await page.RouteAsync("**/post", route =>
        {
            var headers = route.Request.Headers;
            headers["content-type"] = "application/x-www-form-urlencoded; charset=UTF-8";
            IgnoreErrors(route.ContinueAsync(new()
            {
                Headers = headers,
                // .NET: the postData option takes the bytes of request.postData().
                PostData = Encoding.UTF8.GetBytes(route.Request.PostData),
            }));
        });
        await page.EvaluateAsync(@"async () => {
    await fetch('./post', { method: 'POST', body: 'foo=bar' });
  }");
        Assert.That(request, Is.Not.Null);
        Assert.That(request.Headers["content-type"], Is.EqualTo("application/x-www-form-urlencoded; charset=UTF-8"));
    }

    [UpstreamTest("NE-015 should get |undefined| with postData() when there is no post data")]
    public async Task ShouldGetUndefinedWithPostDataWhenThereIsNoPostData(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Request.PostData, Is.Null);
    }

    [UpstreamTest("NE-016 should parse the json post data")]
    public async Task ShouldParseTheJsonPostData(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/post", (req, res) => res.End());
        IRequest request = null;
        page.Request += (_, r) => request = r;
        await page.EvaluateAsync("() => fetch('./post', { method: 'POST', body: JSON.stringify({ foo: 'bar' }) })");
        Assert.That(request, Is.Not.Null);
        ExpectToEqual(request.PostDataJSON(), new { foo = "bar" });
    }

    [UpstreamTest("NE-017 should parse the data if content-type is application/x-www-form-urlencoded")]
    public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencoded(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/post", (req, res) => res.End());
        IRequest request = null;
        page.Request += (_, r) => request = r;
        await page.SetContentAsync("<form method='POST' action='/post'><input type='text' name='foo' value='bar'><input type='number' name='baz' value='123'><input type='submit'></form>");
        await page.ClickAsync("input[type=submit]");
        Assert.That(request, Is.Not.Null);
        ExpectToEqual(request.PostDataJSON(), new { foo = "bar", baz = "123" });
    }

    [UpstreamTest("NE-018 should parse the data if content-type is application/x-www-form-urlencoded; charset=UTF-8")]
    public async Task ShouldParseTheDataIfContentTypeIsApplicationXWwwFormUrlencodedCharsetUTF8(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var requestPromise = page.WaitForRequestAsync("**/post");
        await page.EvaluateAsync(@"() => fetch('./post', {
    method: 'POST',
    headers: {
      'content-type': 'application/x-www-form-urlencoded; charset=UTF-8',
    },
    body: 'foo=bar&baz=123'
  })");
        ExpectToEqual((await requestPromise).PostDataJSON(), new { foo = "bar", baz = "123" });
    }

    [UpstreamTest("NE-019 should get |undefined| with postDataJSON() when there is no post data")]
    public async Task ShouldGetUndefinedWithPostDataJSONWhenThereIsNoPostData(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Request.PostDataJSON(), Is.Null);
    }

    [UpstreamTest("NE-020 should return multipart/form-data")]
    public async Task ShouldReturnMultipartFormData(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Fixme(browserName == "webkit", "File content is missing in WebKit");
        It.Skip(browserName == "chromium" && browserMajorVersion < 126, "Requires a recent enough protocol");

        await page.GotoAsync(server.EmptyPage);
        server.SetRoute("/post", (req, res) => res.End());
        await page.RouteAsync("**/*", route => route.ContinueAsync());
        var requestPromise = page.WaitForRequestAsync("**/post");
        await page.EvaluateAsync(@"async () => {
    const body = new FormData();
    body.set('name1', 'value1');
    body.set('file', new File(['file-value'], 'foo.txt'));
    body.set('name2', 'value2');
    body.append('name2', 'another-value2');
    await fetch('/post', { method: 'POST', body });
  }");
        var request = await requestPromise;
        var contentType = await request.HeaderValueAsync("Content-Type");
        var re = new Regex("^multipart\\/form-data; boundary=(.*)$");
        Assert.That(contentType, Does.Match(re));
        var b = re.Match(contentType).Groups[1].Value;
        var expected = $"--{b}\r\nContent-Disposition: form-data; name=\"name1\"\r\n\r\nvalue1\r\n--{b}\r\nContent-Disposition: form-data; name=\"file\"; filename=\"foo.txt\"\r\nContent-Type: application/octet-stream\r\n\r\nfile-value\r\n--{b}\r\nContent-Disposition: form-data; name=\"name2\"\r\n\r\nvalue2\r\n--{b}\r\nContent-Disposition: form-data; name=\"name2\"\r\n\r\nanother-value2\r\n--{b}--\r\n";
        Assert.That(Encoding.UTF8.GetString(request.PostDataBuffer), Is.EqualTo(expected));
    }

    [UpstreamTest("NE-021 should return event source")]
    public async Task ShouldReturnEventSource(IPage page, Server server)
    {
        var SSE_MESSAGE = new { foo = "bar" };
        // 1. Setup server-sent events on server that immediately sends a message to the client.
        server.SetRoute("/sse", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/event-stream",
                ["Connection"] = "keep-alive",
                ["Cache-Control"] = "no-cache",
            });
            res.Write($"data: {JsonSerializer.Serialize(SSE_MESSAGE)}\n\n");
        });
        // 2. Subscribe to page request events.
        await page.GotoAsync(server.EmptyPage);
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        // 3. Connect to EventSource in browser and return first message.
        ExpectToEqual(await page.EvaluateAsync<object>(@"() => {
    const eventSource = new EventSource('/sse');
    return new Promise(resolve => {
      eventSource.onmessage = e => resolve(JSON.parse(e.data));
    });
  }"), SSE_MESSAGE);
        Assert.That(requests[0].ResourceType, Is.EqualTo("eventsource"));
    }

    [UpstreamTest("NE-022 should return navigation bit")]
    public async Task ShouldReturnNavigationBit(IPage page, Server server)
    {
        var requests = new Dictionary<string, IRequest>();
        page.Request += (_, request) => requests[request.Url.Split('/').Last()] = request;
        server.SetRedirect("/rrredirect", "/frames/one-frame.html");
        await page.GotoAsync(server.Prefix + "/rrredirect");
        Assert.That(requests["rrredirect"].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(requests["one-frame.html"].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(requests["frame.html"].IsNavigationRequest, Is.EqualTo(true));
        Assert.That(requests["script.js"].IsNavigationRequest, Is.EqualTo(false));
        Assert.That(requests["style.css"].IsNavigationRequest, Is.EqualTo(false));
    }

    [UpstreamTest("NE-023 should return navigation bit when navigating to image")]
    public async Task ShouldReturnNavigationBitWhenNavigatingToImage(IPage page, Server server)
    {
        var requests = new List<IRequest>();
        page.Request += (_, request) => requests.Add(request);
        await page.GotoAsync(server.Prefix + "/pptr.png");
        Assert.That(requests[0].IsNavigationRequest, Is.EqualTo(true));
    }

    [UpstreamTest("NE-024 should report raw headers")]
    public async Task ShouldReportRawHeaders(IPage page, Server server, string browserName, string platform, bool isElectron, int browserMajorVersion, string channel)
    {
        It.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");

        List<Header> expectedHeaders = null;
        server.SetRoute("/headers", (req, res) =>
        {
            expectedHeaders = [];
            for (var i = 0; i < req.RawHeaders.Count; i += 2)
            {
                expectedHeaders.Add(new() { Name = req.RawHeaders[i], Value = req.RawHeaders[i + 1] });
            }
            if (browserName == "webkit" && platform == "win32" && channel != "webkit-wsl")
            {
                expectedHeaders = expectedHeaders.Where(header => header.Name.ToLowerInvariant() != "accept-encoding").ToList();
                // Convert "value": "en-US, en-US" => "en-US"
                expectedHeaders = expectedHeaders.Select(e =>
                {
                    var (name, value) = (e.Name, e.Value);
                    if (name.ToLowerInvariant() != "accept-language")
                    {
                        return e;
                    }
                    var values = value.Split(',').Select(v => v.Trim()).ToArray();
                    if (values.Length == 1)
                    {
                        return e;
                    }
                    if (values[0] != values[1])
                    {
                        return e;
                    }
                    return new Header { Name = name, Value = values[0] };
                }).ToList();
            }
            if (browserName == "firefox")
            {
                expectedHeaders = expectedHeaders.Where(header => header.Name.ToLowerInvariant() != "priority").ToList();
            }

            res.End();
        });
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync("**/*"),
            page.EvaluateAsync(@"() => fetch('/headers', {
      headers: [
        ['header-a', 'value-a'],
        ['header-b', 'value-b'],
        ['header-a', 'value-a-1'],
        ['header-a', 'value-a-2'],
      ]
    })"));
        var headers = await request.HeadersArrayAsync();
        // .NET: sort((a, b) => a.name.localeCompare(b.name)) is a stable sort with the comparison of a culture.
        ExpectToEqual(headers.OrderBy(a => a.Name, StringComparer.InvariantCulture), expectedHeaders.OrderBy(a => a.Name, StringComparer.InvariantCulture));
        Assert.That(await request.HeaderValueAsync("header-a"), Is.EqualTo("value-a, value-a-1, value-a-2"));
        Assert.That(await request.HeaderValueAsync("not-there"), Is.Null);
    }

    [UpstreamTest("NE-025 should report raw response headers in redirects")]
    public async Task ShouldReportRawResponseHeadersInRedirects(IPage page, Server server, string browserName)
    {
        It.Skip(browserName == "webkit", "WebKit won't give us raw headers for redirects");
        server.SetExtraHeaders("/redirect/1.html", new() { ["sec-test-header"] = "1.html" });
        server.SetExtraHeaders("/redirect/2.html", new() { ["sec-test-header"] = "2.html" });
        server.SetExtraHeaders("/empty.html", new() { ["sec-test-header"] = "empty.html" });
        server.SetRedirect("/redirect/1.html", "/redirect/2.html");
        server.SetRedirect("/redirect/2.html", "/empty.html");

        var expectedUrls = new[] { "/redirect/1.html", "/redirect/2.html", "/empty.html" }.Select(s => server.Prefix + s);
        var expectedHeaders = new[] { "1.html", "2.html", "empty.html" };

        var response = await page.GotoAsync(server.Prefix + "/redirect/1.html");
        var redirectChain = new List<string>();
        var headersChain = new List<string>();
        for (var req = response.Request; req != null; req = req.RedirectedFrom)
        {
            redirectChain.Insert(0, req.Url);
            var res = await req.ResponseAsync();
            var headers = await res.AllHeadersAsync();
            headersChain.Insert(0, headers["sec-test-header"]);
        }

        Assert.That(redirectChain, Is.EqualTo(expectedUrls));
        Assert.That(headersChain, Is.EqualTo(expectedHeaders));
    }

    [UpstreamTest("NE-026 should report all cookies in one header")]
    public async Task ShouldReportAllCookiesInOneHeader(IPage page, Server server, bool isElectron, int browserMajorVersion)
    {
        It.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");

        var expectedHeaders = new Dictionary<string, string>();
        server.SetRoute("/headers", (req, res) =>
        {
            for (var i = 0; i < req.RawHeaders.Count; i += 2)
            {
                expectedHeaders[req.RawHeaders[i]] = req.RawHeaders[i + 1];
            }
            res.End();
        });

        await page.GotoAsync(server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    document.cookie = 'myCookie=myValue';
    document.cookie = 'myOtherCookie=myOtherValue';
  }");
        var response = await page.GotoAsync(server.EmptyPage);
        var cookie = (await response.Request.AllHeadersAsync())["cookie"];
        Assert.That(cookie, Is.EqualTo("myCookie=myValue; myOtherCookie=myOtherValue"));
    }

    [UpstreamTest("NE-027 should not allow to access frame on popup main request")]
    public async Task ShouldNotAllowToAccessFrameOnPopupMainRequest(IPage page, Server server)
    {
        await page.SetContentAsync($"<a target=_blank href=\"{server.EmptyPage}\">click me</a>");
        var requestPromise = Once<IRequest>(h => page.Context.Request += h, h => page.Context.Request -= h);
        var popupPromise = page.Context.WaitForPageAsync();
        var clicked = page.GetByText("click me").ClickAsync();
        var request = await requestPromise;

        Assert.That(request.IsNavigationRequest, Is.EqualTo(true));

        Exception error = null;
        try
        {
            _ = request.Frame;
        }
        catch (Exception e)
        {
            error = e;
        }
        Assert.That(error.Message, Does.Contain("Frame for this navigation request is not available"));

        var response = await request.ResponseAsync();
        await response.FinishedAsync();
        await popupPromise;
        await clicked;
    }

    [UpstreamTest("NE-028 page.reload return 304 status code")]
    public async Task PageReloadReturn304StatusCode(IPage page, Server server, string browserName, bool isBidi)
    {
        It.Fixme(browserName == "firefox" && !isBidi, "Does not send second request");
        var requestNumber = 0;
        server.SetRoute("/test.html", (req, res) =>
        {
            ++requestNumber;
            var headers = new Dictionary<string, object>
            {
                ["cf-cache-status"] = "DYNAMIC",
                ["Content-Type"] = "text/html;charset=UTF-8",
                ["Last-Modified"] = "Fri, 05 Jan 2024 01:56:20 GMT",
                ["Vary"] = "Access-Control-Request-Headers",
            };
            if (requestNumber == 1)
            {
                res.WriteHead(200, headers);
            }
            else
            {
                res.WriteHead(304, "Not Modified", headers);
            }
            res.Write("<div>Test</div>");
            res.End();
        });
        var response1 = await page.GotoAsync(server.Prefix + "/test.html");
        Assert.That(response1.Status, Is.EqualTo(200));
        var response2 = await page.ReloadAsync();
        Assert.That(requestNumber, Is.EqualTo(2));
        if (browserName == "chromium")
        {
            Assert.That(response2.Status, Is.EqualTo(200));
            Assert.That(response2.StatusText, Is.EqualTo("OK"));
            Assert.That(await response2.TextAsync(), Is.EqualTo("<div>Test</div>"));
        }
        else
        {
            Assert.That(response2.Status, Is.EqualTo(304));
            Assert.That(response2.StatusText, Is.EqualTo("Not Modified"));
        }
    }

    [UpstreamTest("NE-029 should handle mixed-content blocked requests")]
    public async Task ShouldHandleMixedContentBlockedRequests(IPage page, Func<string, string> asset, string browserName)
    {
        It.Skip(browserName != "chromium", "FF and WK actually succeed with the request, and block afterwards");

        await page.RouteAsync("**/mixedcontent.html", route =>
        {
            IgnoreErrors(route.FulfillAsync(new()
            {
                Status = 200,
                ContentType = "text/html",
                Body = @"
        <!doctype html>
        <meta charset=""utf-8"">
        <style>
        @font-face {
          font-family: 'pwtest-iconfont';
          src: url('http://another.com/iconfont.woff2') format('woff2');
        }
        body {
          font-family: 'pwtest-iconfont';
        }
        </style>
        <span>+-</span>
    ",
            }));
        });
        await page.RouteAsync("**/iconfont.woff2", async route =>
        {
            var body = await File.ReadAllBytesAsync(asset("webfont/iconfont2.woff"));
            await route.FulfillAsync(new() { BodyBytes = body });
        });

        var (request, _) = await PromiseAll(
            Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h, r => r.Url.Contains("iconfont.woff2")),
            page.GotoAsync("https://example.com/mixedcontent.html"));
        var headers = await request.AllHeadersAsync();
        Assert.That(headers["origin"], Is.Not.Empty);
        Assert.That(request.Failure, Is.EqualTo("mixed-content"));
    }
}
