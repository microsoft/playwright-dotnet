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

using System.IO.Compression;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-network-response.spec.ts.</summary>
public class PageNetworkResponseTests
{
    [UpstreamTest("NE-030 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server)
    {
        server.SetRoute("/empty.html", (req, res) =>
        {
            res.SetHeader("foo", "bar");
            res.SetHeader("BaZ", "bAz");
            res.End();
        });
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That((await response.AllHeadersAsync())["foo"], Is.EqualTo("bar"));
        Assert.That((await response.AllHeadersAsync())["baz"], Is.EqualTo("bAz"));
        // .NET: a dictionary throws for a key that it does not have, where an object gives undefined.
        Assert.That((await response.AllHeadersAsync()).GetValueOrDefault("BaZ"), Is.Null);
    }

    [UpstreamTest("NE-031 should return multiple header value")]
    public async Task ShouldReturnMultipleHeaderValue(IPage page, Server server, string browserName, string platform)
    {
        It.Skip(browserName == "webkit" && platform == "win32", "libcurl does not support non-set-cookie multivalue headers");

        server.SetRoute("/headers", (req, res) =>
        {
            // Headers array is only supported since Node v14.14.0 so we write directly to the socket.
            // res.writeHead(200, ['name-a', 'v1','name-b', 'v4','Name-a', 'v2', 'name-A', 'v3']);
            var conn = res.Connection;
            conn.Write("HTTP/1.1 200 OK\r\n");
            conn.Write("Name-A: v1\r\n");
            conn.Write("Name-a: v2\r\n");
            conn.Write("name-A: v3\r\n");
            conn.Write("\r\n");
            conn.Uncork();
            conn.End();
        });
        var response = await page.GotoAsync($"{server.Prefix}/headers");
        Assert.That(response.Status, Is.EqualTo(200));
        Assert.That(response.Headers["name-a"], Is.EqualTo("v1, v2, v3"));
    }

    [UpstreamTest("NE-032 should return text")]
    public async Task ShouldReturnText(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.Prefix + "/simple.json");
        Assert.That(await response.TextAsync(), Is.EqualTo("{\"foo\": \"bar\"}\n"));
    }

    [UpstreamTest("NE-033 should return uncompressed text")]
    public async Task ShouldReturnUncompressedText(IPage page, Server server)
    {
        server.EnableGzip("/simple.json");
        var response = await page.GotoAsync(server.Prefix + "/simple.json");
        Assert.That(response.Headers["content-encoding"], Is.EqualTo("gzip"));
        Assert.That(await response.TextAsync(), Is.EqualTo("{\"foo\": \"bar\"}\n"));
    }

    [UpstreamTest("NE-034 should return uncompressed text for brotli encoding")]
    public async Task ShouldReturnUncompressedTextForBrotliEncoding(IPage page, Server server, string browserName, bool isAndroid)
    {
        It.Fixme(isAndroid, "net::ERR_CONTENT_DECODING_FAILED");

        var text = "{\"foo\": \"bar\"}\n";
        // .NET: zlib.brotliCompressSync(Buffer.from(text)) is a BrotliStream.
        byte[] compressed;
        using (var stream = new MemoryStream())
        {
            using (var brotli = new BrotliStream(stream, CompressionLevel.Optimal))
            {
                brotli.Write(Encoding.UTF8.GetBytes(text));
            }
            compressed = stream.ToArray();
        }
        server.SetRoute("/brotli.json", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "application/json",
                ["Content-Encoding"] = "br",
            });
            res.End(compressed);
        });
        var response = await page.GotoAsync(server.Prefix + "/brotli.json");
        Assert.That(response.Headers["content-encoding"], Is.EqualTo("br"));
        Assert.That(await response.TextAsync(), Is.EqualTo(text));
    }

    [UpstreamTest("NE-035 should return text for identity encoding")]
    public async Task ShouldReturnTextForIdentityEncoding(IPage page, Server server)
    {
        var text = "<div>hello</div>";
        server.SetRoute("/identity.html", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/html; charset=utf-8",
                ["Content-Encoding"] = "Identity",
            });
            res.End(text);
        });
        var response = await page.GotoAsync(server.Prefix + "/identity.html");
        Assert.That(response.Headers["content-encoding"], Is.EqualTo("Identity"));
        Assert.That(await response.TextAsync(), Is.EqualTo(text));
    }

    [UpstreamTest("NE-036 should throw when requesting body of redirected response")]
    public async Task ShouldThrowWhenRequestingBodyOfRedirectedResponse(IPage page, Server server)
    {
        server.SetRedirect("/foo.html", "/empty.html");
        var response = await page.GotoAsync(server.Prefix + "/foo.html");
        var redirectedFrom = response.Request.RedirectedFrom;
        Assert.That(redirectedFrom, Is.Not.Null);
        var redirected = await redirectedFrom.ResponseAsync();
        Assert.That(redirected.Status, Is.EqualTo(302));
        Exception error = null;
        error = await Catch(() => redirected.TextAsync());
        Assert.That(error.Message, Does.Contain("Response body is unavailable for redirect responses"));
    }

    [UpstreamTest("NE-037 should wait until response completes")]
    public async Task ShouldWaitUntilResponseCompletes(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        // Setup server to trap request.
        ServerResponse serverResponse = null;
        server.SetRoute("/get", (req, res) =>
        {
            serverResponse = res;
            // In Firefox, |fetch| will be hanging until it receives |Content-Type| header
            // from server.
            res.SetHeader("Content-Type", "text/plain; charset=utf-8");
            res.Write("hello ");
        });
        // Setup page to trap response.
        var requestFinished = false;
        page.RequestFinished += (_, r) => requestFinished = requestFinished || r.Url.Contains("/get");
        // send request and wait for server response
        var (pageResponse, _, _) = await PromiseAll(
            page.WaitForResponseAsync(_ => true),
            page.EvaluateAsync("() => fetch('./get', { method: 'GET' })"),
            server.WaitForRequest("/get"));

        Assert.That(serverResponse, Is.Not.Null);
        Assert.That(pageResponse, Is.Not.Null);
        Assert.That(pageResponse.Status, Is.EqualTo(200));
        Assert.That(requestFinished, Is.EqualTo(false));

        var responseText = pageResponse.TextAsync();
        // Write part of the response and wait for it to be flushed.
        await serverResponse.WriteAsync("wor");
        // Finish response.
        await serverResponse.EndAsync("ld!");
        Assert.That(await responseText, Is.EqualTo("hello world!"));
    }

    [UpstreamTest("NE-038 should return json")]
    public async Task ShouldReturnJson(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.Prefix + "/simple.json");
        ExpectToEqual(await response.JsonAsync(), new { foo = "bar" });
    }

    [UpstreamTest("NE-039 should return body")]
    public async Task ShouldReturnBody(IPage page, Server server, Func<string, string> asset)
    {
        var response = await page.GotoAsync(server.Prefix + "/pptr.png");
        var imageBuffer = File.ReadAllBytes(asset("pptr.png"));
        var responseBuffer = await response.BodyAsync();
        Assert.That(responseBuffer.SequenceEqual(imageBuffer), Is.EqualTo(true));
    }

    [UpstreamTest("NE-040 should return body with compression")]
    public async Task ShouldReturnBodyWithCompression(IPage page, Server server, Func<string, string> asset)
    {
        server.EnableGzip("/pptr.png");
        var response = await page.GotoAsync(server.Prefix + "/pptr.png");
        var imageBuffer = File.ReadAllBytes(asset("pptr.png"));
        var responseBuffer = await response.BodyAsync();
        Assert.That(responseBuffer.SequenceEqual(imageBuffer), Is.EqualTo(true));
    }

    [UpstreamTest("NE-041 should return non-utf8 body even when content-type says utf8")]
    public async Task ShouldReturnNonUtf8BodyEvenWhenContentTypeSaysUtf8(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Fixme(browserName == "webkit", "webkit encodes the response body");
        It.Fixme(browserName == "chromium" && browserMajorVersion < 151, "older chromium re-encodes the non-utf8 response body and lacks Response.bytes()");

        // Binary data with bytes that are invalid UTF-8.
        var bytes = new byte[] { 0x80, 0x81, 0x82, 0xFF, 0xFE, 0x00, 0x01, 0x02 };
        var buffer = bytes.ToArray();
        server.SetRoute("/binary-as-text", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Content-Type"] = "text/plain;charset=UTF-8",
                ["Content-Length"] = buffer.Length,
            });
            res.End(buffer);
        });
        await page.GotoAsync(server.EmptyPage);
        var (response, bytesReceived) = await PromiseAll(
            page.WaitForResponseAsync(server.Prefix + "/binary-as-text"),
            page.EvaluateAsync<byte[]>("url => fetch(url).then(r => r.bytes())", server.Prefix + "/binary-as-text"));
        var body = await response.BodyAsync();
        Assert.That(body.SequenceEqual(buffer), Is.EqualTo(true));
        Assert.That(bytesReceived, Is.EqualTo(bytes));
    }

    [UpstreamTest("NE-042 should return status text")]
    public async Task ShouldReturnStatusText(IPage page, Server server)
    {
        server.SetRoute("/cool", (req, res) =>
        {
            res.WriteHead(200, "cool!");
            res.End();
        });
        var response = await page.GotoAsync(server.Prefix + "/cool");
        Assert.That(response.StatusText, Is.EqualTo("cool!"));
    }

    [UpstreamTest("NE-043 should report all headers")]
    public async Task ShouldReportAllHeaders(IPage page, Server server, string browserName, string platform, bool isElectron, int browserMajorVersion)
    {
        It.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");
        It.Skip(browserName == "webkit" && platform == "win32", "libcurl does not support non-set-cookie multivalue headers");

        var expectedHeaders = new Dictionary<string, object>
        {
            ["header-a"] = new[] { "value-a", "value-a-1", "value-a-2" },
            ["header-b"] = new[] { "value-b" },
        };
        server.SetRoute("/headers", (req, res) =>
        {
            res.WriteHead(200, expectedHeaders);
            res.End();
        });

        await page.GotoAsync(server.EmptyPage);
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/*"),
            page.EvaluateAsync("() => fetch('/headers')"));
        var headers = await response.HeadersArrayAsync();
        var actualHeaders = new Dictionary<string, List<string>>();
        foreach (var header in headers)
        {
            if (!actualHeaders.ContainsKey(header.Name))
            {
                actualHeaders[header.Name] = [];
            }
            actualHeaders[header.Name].Add(header.Value);
        }
        actualHeaders.Remove("Keep-Alive");
        actualHeaders.Remove("keep-alive");
        actualHeaders.Remove("Connection");
        actualHeaders.Remove("connection");
        actualHeaders.Remove("Date");
        actualHeaders.Remove("date");
        actualHeaders.Remove("Transfer-Encoding");
        actualHeaders.Remove("transfer-encoding");
        ExpectToEqual(actualHeaders, expectedHeaders);
    }

    [UpstreamTest("NE-044 should report multiple set-cookie headers")]
    public async Task ShouldReportMultipleSetCookieHeaders(IPage page, Server server, bool isElectron, int browserMajorVersion)
    {
        It.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");

        server.SetRoute("/headers", (req, res) =>
        {
            res.WriteHead(200, new()
            {
                ["Set-Cookie"] = new[] { "a=b", "c=d" },
            });
            res.Write("\r\n");
            res.End();
        });

        await page.GotoAsync(server.EmptyPage);
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/*"),
            page.EvaluateAsync("() => fetch('/headers')"));
        var headers = await response.HeadersArrayAsync();
        var cookies = headers.Where(header => header.Name.ToLowerInvariant() == "set-cookie").Select(header => header.Value);
        Assert.That(cookies, Is.EqualTo(new[] { "a=b", "c=d" }));
        Assert.That(await response.HeaderValueAsync("not-there"), Is.Null);
        Assert.That(await response.HeaderValueAsync("set-cookie"), Is.EqualTo("a=b\nc=d"));
        Assert.That(await response.HeaderValuesAsync("set-cookie"), Is.EqualTo(new[] { "a=b", "c=d" }));
    }

    [UpstreamTest("NE-045 should behave the same way for headers and allHeaders")]
    public async Task ShouldBehaveTheSameWayForHeadersAndAllHeaders(IPage page, Server server, string browserName, string platform)
    {
        It.Skip(browserName == "webkit" && platform == "win32", "libcurl does not support non-set-cookie multivalue headers");
        server.SetRoute("/headers", (req, res) =>
        {
            var headers = new Dictionary<string, object>
            {
                ["Set-Cookie"] = new[] { "a=b", "c=d" },
                ["header-a"] = new[] { "a=b", "c=d" },
                ["Name-A"] = "v1",
                ["name-b"] = "v4",
                ["Name-a"] = "v2",
                ["name-A"] = "v3",
            };
            // Chromium does not report set-cookie headers immediately, so they are missing from .headers()
            if (browserName == "chromium")
            {
                headers.Remove("Set-Cookie");
            }

            res.WriteHead(200, headers);
            res.Write("\r\n");
            res.End();
        });

        await page.GotoAsync(server.EmptyPage);
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/*"),
            page.EvaluateAsync("() => fetch('/headers')"));
        var allHeaders = await response.AllHeadersAsync();
        ExpectToEqual(response.Headers, allHeaders);
        Assert.That(allHeaders["header-a"], Is.EqualTo("a=b, c=d"));
        Assert.That(allHeaders["name-a"], Is.EqualTo("v1, v2, v3"));
        Assert.That(allHeaders["name-b"], Is.EqualTo("v4"));
    }

    [UpstreamTest("NE-046 should provide a Response with a file URL")]
    public async Task ShouldProvideAResponseWithAFileURL(IPage page, Func<string, string> asset, bool isAndroid, bool isElectron, bool isWindows, string browserName, string mode, string channel)
    {
        It.Skip(isAndroid, "No files on Android");
        It.Skip(browserName == "firefox", "Firefox does return null for file:// URLs");
        It.Skip(channel == "webkit-wsl");

        var fileurl = new Uri(asset("frames/two-frames.html")).AbsoluteUri;
        var response = await page.GotoAsync(fileurl);
        if (isElectron || (browserName == "chromium") || (browserName == "webkit" && isWindows))
        {
            Assert.That(response.Status, Is.EqualTo(200));
        }
        else
        {
            Assert.That(response.Status, Is.EqualTo(0));
        }
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("NE-047 should return set-cookie header after route.fulfill")]
    public async Task ShouldReturnSetCookieHeaderAfterRouteFulfill(IPage page, Server server, string browserName)
    {
        It.Fixme(browserName == "webkit" || browserName == "chromium", "https://github.com/microsoft/playwright/issues/11035");
        await page.RouteAsync("**/*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    ["set-cookie"] = "a=b",
                },
                ContentType = "text/plain",
                Body = "",
            });
        });
        var response = await page.GotoAsync(server.EmptyPage);
        var headers = await response.AllHeadersAsync();
        Assert.That(headers["set-cookie"], Is.EqualTo("a=b"));
    }

    [UpstreamTest("NE-048 should return headers after route.fulfill")]
    public async Task ShouldReturnHeadersAfterRouteFulfill(IPage page, Server server)
    {
        await page.RouteAsync("**/*", async route =>
        {
            await route.FulfillAsync(new()
            {
                Status = 200,
                Headers = new Dictionary<string, string>
                {
                    ["foo"] = "bar",
                    ["content-language"] = "en",
                },
                ContentType = "text/plain",
                Body = "done",
            });
        });
        var response = await page.GotoAsync(server.EmptyPage);
        ExpectToEqual(await response.AllHeadersAsync(), new Dictionary<string, object>
        {
            ["foo"] = "bar",
            ["content-type"] = "text/plain",
            ["content-length"] = "4",
            ["content-language"] = "en",
        });
    }

    [UpstreamTest("NE-049 should report if request was fromServiceWorker")]
    public async Task ShouldReportIfRequestWasFromServiceWorker(IPage page, Server server, bool isAndroid, bool isElectron)
    {
        It.Skip(isAndroid || isElectron);
        {
            var res = await page.GotoAsync(server.Prefix + "/serviceworkers/fetch/sw.html");
            Assert.That(res.FromServiceWorker, Is.EqualTo(false));
        }
        await page.EvaluateAsync("() => window['activationPromise']");
        {
            var (res, _) = await PromiseAll(
                page.WaitForResponseAsync(new Regex("example\\.txt")),
                page.EvaluateAsync("() => fetch('/example.txt')"));
            Assert.That(res.FromServiceWorker, Is.EqualTo(true));
        }
    }

    [UpstreamTest("NE-050 should return body for prefetch script")]
    public async Task ShouldReturnBodyForPrefetchScript(IPage page, Server server, string browserName, int browserMajorVersion)
    {
        It.Skip(browserName == "webkit", "No prefetch in WebKit: https://caniuse.com/link-rel-prefetch");
        It.Skip(browserName == "chromium" && browserMajorVersion < 138, "Requires Sec-Purpose header, shipped in Chrome 138");
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/prefetch.js"),
            page.GotoAsync(server.Prefix + "/prefetch.html"));
        var body = await response.BodyAsync();
        Assert.That(Encoding.UTF8.GetString(body), Is.EqualTo("// Scripts will be pre-fetched"));
    }

    [UpstreamTest("NE-051 should return body for image with evicted body")]
    public async Task ShouldReturnBodyForImageWithEvictedBody(IPage page, Server server, bool isMac, string browserName)
    {
        It.Fixme(isMac && browserName == "webkit", "WebKit on Mac evicts the body and returns empty buffer");
        var imageBase64 = "R0lGODlhAQABAAAAACw="; // truncated 1x1 gif, Chromium evicts its body
        server.SetRoute("/pixel.gif", (req, res) =>
        {
            res.SetHeader("content-type", "image/gif");
            res.End(Convert.FromBase64String(imageBase64));
        });
        server.SetRoute("/page.html", (req, res) =>
        {
            res.SetHeader("content-type", "text/html");
            res.End("<html><body><img src=\"/pixel.gif\"></body></html>");
        });
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync("**/pixel.gif"),
            page.GotoAsync(server.Prefix + "/page.html"));
        var body = await response.BodyAsync();
        Assert.That(Convert.ToBase64String(body), Is.EqualTo(imageBase64));
    }

    [UpstreamTest("NE-145 should return source for a module script loaded from cache")]
    public async Task ShouldReturnSourceForAModuleScriptLoadedFromCache(IPage page, Server server)
    {
        var source = string.Join("\n", Enumerable.Range(0, 400).Select(i => $"export function f{i}() {{ return {i}; }}"));
        server.SetRoute("/module.js", (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = "text/javascript", ["Cache-Control"] = "max-age=3600" });
            res.End(source);
        });
        server.SetRoute("/module.html", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.End("<script type=\"module\" src=\"/module.js\"></script>");
        });
        // Firefox stores module bytecode in the HTTP cache after several loads.
        for (var i = 0; i < 5; ++i)
        {
            var (response, _) = await PromiseAll(
                page.WaitForResponseAsync("**/module.js"),
                page.GotoAsync(server.Prefix + "/module.html"));
            Assert.That(Encoding.UTF8.GetString(await response.BodyAsync()), Is.EqualTo(source));
            await page.WaitForTimeoutAsync(500);
        }
    }

    [UpstreamTest("NE-052 should bypass disk cache when page interception is enabled")]
    public async Task ShouldBypassDiskCacheWhenPageInterceptionIsEnabled(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        await page.RouteAsync("**/api*", route => route.ContinueAsync());
        {
            var requests = new List<ServerRequest>();
            server.SetRoute("/api", (req, res) =>
            {
                requests.Add(req);
                res.StatusCode = 200;
                res.SetHeader("content-type", "text/plain");
                res.SetHeader("cache-control", "public, max-age=31536000");
                res.End("Hello");
            });
            for (var i = 0; i < 3; i++)
            {
                await It.Step($"main frame iteration {i}", async () =>
                {
                    var respPromise = page.WaitForResponseAsync("**/api");
                    await page.EvaluateAsync(@"async () => {
          const response = await fetch('/api');
          return response.status;
        }");
                    var response = await respPromise;
                    Assert.That(response.Status, Is.EqualTo(200));
                    Assert.That(requests.Count, Is.EqualTo(i + 1));
                });
            }
        }

        {
            var requests = new List<ServerRequest>();
            server.SetRoute("/frame/api", (req, res) =>
            {
                requests.Add(req);
                res.StatusCode = 200;
                res.SetHeader("content-type", "text/plain");
                res.SetHeader("cache-control", "public, max-age=31536000");
                res.End("Hello");
            });
            for (var i = 0; i < 3; i++)
            {
                await It.Step($"subframe iteration {i}", async () =>
                {
                    var respPromise = page.WaitForResponseAsync("**/frame/api");
                    await page.FrameByUrl("**/frame.html").EvaluateAsync(@"async () => {
          const response = await fetch('/frame/api');
          return response.status;
        }");
                    var response = await respPromise;
                    Assert.That(response.Status, Is.EqualTo(200));
                    Assert.That(requests.Count, Is.EqualTo(i + 1));
                });
            }
        }
    }

    [UpstreamTest("NE-053 request.existingResponse should return null before response is received")]
    public async Task RequestExistingResponseShouldReturnNullBeforeResponseIsReceived(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        ServerResponse serverResponse = null;
        server.SetRoute("/get", (req, res) =>
        {
            serverResponse = res;
            // Don't end the response yet
        });

        var (request, _, _) = await PromiseAll(
            page.WaitForRequestAsync(_ => true),
            server.WaitForRequest("/get"),
            page.EvaluateAsync("() => { void fetch('./get', { method: 'GET' }); }"));

        // Response hasn't been received yet
        Assert.That(request.ExistingResponse, Is.Null);

        // Now send the response
        serverResponse.SetHeader("Content-Type", "text/plain; charset=utf-8");
        serverResponse.End("done");
        await page.WaitForResponseAsync(_ => true);

        // After response is received, existingResponse should return the response
        var existingResponse = request.ExistingResponse;
        Assert.That(existingResponse, Is.Not.Null);
        Assert.That(existingResponse.Status, Is.EqualTo(200));
    }

    [UpstreamTest("NE-054 request.existingResponse should return the response after it is received")]
    public async Task RequestExistingResponseShouldReturnTheResponseAfterItIsReceived(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        var request = response.Request;
        Assert.That(request.ExistingResponse, Is.SameAs(response));
    }

    [UpstreamTest("NE-055 should return http version")]
    public async Task ShouldReturnHttpVersion(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(await response.HttpVersionAsync(), Is.EqualTo("HTTP/1.1"));
    }

    [UpstreamTest("NE-056 Response.formData() should parse multipart/form-data in page context")]
    public async Task ResponseFormDataShouldParseMultipartFormDataInPageContext(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        var result = await page.EvaluateAsync<JsonElement>(@"async () => {
    const boundary = '----WebKitFormBoundary1234';
    const body = [
      `--${boundary}`,
      'Content-Disposition: form-data; name=""field1""',
      '',
      'value1',
      `--${boundary}`,
      'Content-Disposition: form-data; name=""file1""; filename=""test.txt""',
      'Content-Type: text/plain',
      '',
      'hello',
      `--${boundary}--`,
    ].join('\r\n');
    const response = new Response(body, {
      headers: { 'Content-Type': `multipart/form-data; boundary=${boundary}` },
    });
    const fd = await response.formData();
    const file = fd.get('file1');
    return {
      field1: fd.get('field1'),
      filename: file instanceof File ? file.name : null,
      fileContent: file instanceof File ? await file.text() : null,
    };
  }");
        Assert.That(result.GetProperty("field1").GetString(), Is.EqualTo("value1"));
        Assert.That(result.GetProperty("filename").GetString(), Is.EqualTo("test.txt"));
        Assert.That(result.GetProperty("fileContent").GetString(), Is.EqualTo("hello"));
    }

    [UpstreamTest("NE-057 should give a readable error when response.body() races with navigation")]
    public async Task ShouldGiveAReadableErrorWhenResponseBodyRacesWithNavigation(IPage page, Server server, string browserName)
    {
        It.Skip(browserName == "firefox", "Firefox keeps the response body available after navigating away, so it never throws");
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync(server.Prefix + "/title.html"),
            page.GotoAsync(server.Prefix + "/title.html"));
        // Navigate away — the browser frees the network resource from the first page load.
        // The first page must have a non-empty body, otherwise WebKit returns an empty buffer instead of throwing.
        await page.GotoAsync(server.Prefix + "/grid.html");
        var error = await Catch(() => response.BodyAsync());
        Assert.That(error, Is.InstanceOf<Exception>());
        Assert.That(error.Message, Does.Contain("navigated away"));
    }
}
