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
using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-network-sizes.spec.ts.</summary>
public class PageNetworkSizesTests
{
    [BeforeEach]
    public static void Skip(bool isElectron, int browserMajorVersion) => Info.Skip(isElectron && browserMajorVersion < 99, "This needs Chromium >= 99");

    [UpstreamTest("NE-058 should set bodySize and headersSize")]
    public async Task ShouldSetBodySizeAndHeadersSize(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync(_ => true),
            page.EvaluateAsync("() => fetch('./get', { method: 'POST', body: '12345' }).then(r => r.text())"));
        var sizes = await request.SizesAsync();
        Assert.That(sizes.RequestBodySize, Is.EqualTo(5));
        Assert.That(sizes.RequestHeadersSize, Is.GreaterThanOrEqualTo(250));
    }

    [UpstreamTest("NE-059 should set bodySize to 0 if there was no body")]
    public async Task ShouldSetBodySizeTo0IfThereWasNoBody(IPage page, Server server, string browserName, string platform)
    {
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            page.WaitForRequestAsync(_ => true),
            page.EvaluateAsync("() => fetch('./get').then(r => r.text())"));
        var sizes = await request.SizesAsync();
        Assert.That(sizes.RequestBodySize, Is.EqualTo(0));
        Assert.That(sizes.RequestHeadersSize, Is.GreaterThanOrEqualTo(190));
    }

    [UpstreamTest("NE-060 should set bodySize, headersSize, and transferSize")]
    public async Task ShouldSetBodySizeHeadersSizeAndTransferSize(IPage page, Server server)
    {
        server.SetRoute("/get", (req, res) =>
        {
            // In Firefox, |fetch| will be hanging until it receives |Content-Type| header
            // from server.
            res.SetHeader("Content-Type", "text/plain; charset=utf-8");
            res.End("abc134");
        });
        await page.GotoAsync(server.EmptyPage);
        var (response, _, _) = await PromiseAll(
            page.WaitForResponseAsync(_ => true),
            page.EvaluateAsync("async () => fetch('./get').then(r => r.text())"),
            server.WaitForRequest("/get"));
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(6));
        Assert.That(sizes.ResponseHeadersSize, Is.GreaterThanOrEqualTo(100));
    }

    [UpstreamTest("NE-061 should set bodySize to 0 when there was no response body")]
    public async Task ShouldSetBodySizeTo0WhenThereWasNoResponseBody(IPage page, Server server)
    {
        var response = await page.GotoAsync(server.EmptyPage);
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(0));
        Assert.That(sizes.ResponseHeadersSize, Is.GreaterThanOrEqualTo(150));
    }

    [UpstreamTest("NE-062 should have the correct responseBodySize")]
    public async Task ShouldHaveTheCorrectResponseBodySize(IPage page, Server server, Func<string, string> asset, string browserName)
    {
        var response = await page.GotoAsync(server.Prefix + "/simplezip.json");
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(new FileInfo(asset("simplezip.json")).Length));
    }

    [UpstreamTest("NE-063 should have the correct responseBodySize for chunked request")]
    public async Task ShouldHaveTheCorrectResponseBodySizeForChunkedRequest(IPage page, Server server, Func<string, string> asset, string browserName, string platform)
    {
        Info.Fixme(browserName == "firefox");
        Info.Fixme(browserName == "webkit" && platform != "darwin");
        var content = File.ReadAllBytes(asset("simplezip.json"));
        var AMOUNT_OF_CHUNKS = 10;
        var CHUNK_SIZE = (int)Math.Ceiling(content.Length / (double)AMOUNT_OF_CHUNKS);
        server.SetRoute("/chunked-simplezip.json", async (req, resp) =>
        {
            resp.WriteHead(200, new() { ["Content-Type"] = "text/html; charset=utf-8", ["Transfer-Encoding"] = "chunked" });
            for (var start = 0; start < content.Length; start += CHUNK_SIZE)
            {
                var end = Math.Min(start + CHUNK_SIZE, content.Length);
                // Wait for each chunk to be flushed, otherwise Node 26 merges synchronous writes into a single chunk.
                await resp.WriteAsync(content[start..end]);
            }
            resp.End();
        });
        var response = await page.GotoAsync(server.Prefix + "/chunked-simplezip.json");
        var sizes = await response.Request.SizesAsync();
        // The actual file size is 5100 bytes. The extra 75 bytes are coming from the chunked encoding headers and end bytes.
        if (browserName == "webkit")
        {
            // WebKit on macOS reports 5173 with the legacy CFNetwork loader (builds <= 2346) and the
            // correct 5175 with NWLoader. TODO: expect 5175 once the NWLoader-based build ships.
            Assert.That(new[] { 5173, 5175 }, Does.Contain(sizes.ResponseBodySize));
        }
        else
        {
            Assert.That(sizes.ResponseBodySize, Is.EqualTo(5175));
        }
    }

    [UpstreamTest("NE-064 should have the correct responseBodySize with gzip compression")]
    public async Task ShouldHaveTheCorrectResponseBodySizeWithGzipCompression(IPage page, Server server, Func<string, string> asset, TestInfo testInfo)
    {
        server.EnableGzip("/simplezip.json");
        await page.GotoAsync(server.EmptyPage);
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync(_ => true),
            page.EvaluateAsync("() => fetch('./simplezip.json').then(r => r.text())"));
        var sizes = await response.Request.SizesAsync();

        // .NET: fs.createReadStream(asset('simplezip.json')).pipe(zlib.createGzip()) and its 'data' and 'end' events are a GZipStream.
        using var chunks = new MemoryStream();
        using (var gzip = new GZipStream(chunks, CompressionLevel.Optimal, true))
        {
            await gzip.WriteAsync(File.ReadAllBytes(asset("simplezip.json")));
        }

        Assert.That(sizes.ResponseBodySize, Is.EqualTo(chunks.ToArray().Length));
    }

    [UpstreamTest("NE-065 should handle redirects")]
    public async Task ShouldHandleRedirects(IPage page, Server server)
    {
        server.SetRedirect("/foo", "/bar");
        server.SetRoute("/bar", (req, resp) => resp.End("bar"));
        await page.GotoAsync(server.EmptyPage);
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync(_ => true),
            page.EvaluateAsync(@"async () => fetch('/foo', {
      method: 'POST',
      body: '12345',
    }).then(r => r.text())"));
        Assert.That((await response.Request.SizesAsync()).RequestBodySize, Is.EqualTo(5));
        var newRequest = response.Request.RedirectedTo;
        Assert.That((await newRequest.SizesAsync()).ResponseBodySize, Is.EqualTo(3));
    }

    [UpstreamTest("NE-066 should throw for failed requests")]
    [LocalFixme("dotnet: the message of Request.SizesAsync for a failed request is \"Unable to fetch resources sizes.\", upstream has \"Unable to fetch sizes for failed request\"")]
    public async Task ShouldThrowForFailedRequests(IPage page, Server server)
    {
        server.SetRoute("/one-style.css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            res.Connection.Destroy();
        });
        await page.GotoAsync(server.EmptyPage);
        var (request, _) = await PromiseAll(
            Once<IRequest>(h => page.RequestFailed += h, h => page.RequestFailed -= h),
            page.GotoAsync(server.Prefix + "/one-style.html"));
        var error = await ThrowsAsync<PlaywrightException>(() => request.SizesAsync());
        Assert.That(error.Message, Does.Contain("Unable to fetch sizes for failed request"));
    }

    [UpstreamTest("NE-067 should work with ${statusCode} status code")]
    [Case(200)]
    [Case(401)]
    [Case(404)]
    [Case(500)]
    public async Task ShouldWorkWithStatusCode(int statusCode, IPage page, Server server)
    {
        server.SetRoute("/foo", (req, resp) =>
        {
            resp.WriteHead(statusCode, new()
            {
                ["Content-Type"] = "text/plain; charset=utf-8",
                ["Content-Length"] = "3",
            });
            resp.End("bar");
        });
        await page.GotoAsync(server.EmptyPage);
        var (response, _) = await PromiseAll(
            page.WaitForResponseAsync(_ => true),
            page.EvaluateAsync(@"async () => fetch('/foo', {
        method: 'POST',
        body: '12345',
      }).then(r => r.text())"));
        Assert.That(response.Status, Is.EqualTo(statusCode));
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.RequestBodySize, Is.EqualTo(5));
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(3));
    }

    [UpstreamTest("NE-068 should have correct responseBodySize for 404 with content")]
    public async Task ShouldHaveCorrectResponseBodySizeFor404WithContent(IPage page, Server server, string browserName)
    {
        Info.Fixme(browserName == "chromium");

        server.SetRoute("/broken-image.png", (req, resp) =>
        {
            resp.WriteHead(404);
            resp.End("<p>this should have a non-negative size</p>");
        });
        server.SetRoute("/page-with-404-image.html", (req, resp) =>
        {
            resp.End(@"
    <!DOCTYPE html>
    <html>
      <head><title>Page with Broken Image</title></head>
      <body>
        <img src=""/broken-image.png"" />
      </body>
    </html>
    ");
        });

        var (req, _) = await PromiseAll(
            page.WaitForRequestAsync(new Regex("broken-image\\.png$")),
            page.GotoAsync(server.Prefix + "/page-with-404-image.html"));

        // .NET: const { responseBodySize } = await req.sizes();
        var responseBodySize = (await req.SizesAsync()).ResponseBodySize;
        Assert.That(responseBodySize, Is.GreaterThanOrEqualTo(0));
    }

    [UpstreamTest("NE-069 should return sizes without hanging")]
    public async Task ShouldReturnSizesWithoutHanging(IPage page, Server server, string browserName)
    {
        Info.Fixme(browserName == "chromium");

        server.SetRoute("/has-abandoned-fetch", (req, resp) =>
        {
            resp.End(@"
    <!DOCTYPE html>
    <html>
      <head><title>t</title></head>
      <body>
        <script>
          fetch(""./404"");
        </script>
      </body>
    </html>
    ");
        });

        var (req, _) = await PromiseAll(
            page.WaitForRequestAsync(new Regex("404$")),
            page.GotoAsync(server.Prefix + "/has-abandoned-fetch"));

        await req.SizesAsync();
    }
}
