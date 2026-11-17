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
public class PageNetworkSizesTests : UpstreamPageTest
{
    [SetUp]
    public void Skip() => Info.Skip(IsElectron && BrowserMajorVersion < 99, "This needs Chromium >= 99");

    [UpstreamTest("NE-058 should set bodySize and headersSize")]
    public async Task ShouldSetBodySizeAndHeadersSize()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(_ => true),
            Page.EvaluateAsync("() => fetch('./get', { method: 'POST', body: '12345' }).then(r => r.text())"));
        var sizes = await request.SizesAsync();
        Assert.That(sizes.RequestBodySize, Is.EqualTo(5));
        Assert.That(sizes.RequestHeadersSize, Is.GreaterThanOrEqualTo(250));
    }

    [UpstreamTest("NE-059 should set bodySize to 0 if there was no body")]
    public async Task ShouldSetBodySizeTo0IfThereWasNoBody()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Page.WaitForRequestAsync(_ => true),
            Page.EvaluateAsync("() => fetch('./get').then(r => r.text())"));
        var sizes = await request.SizesAsync();
        Assert.That(sizes.RequestBodySize, Is.EqualTo(0));
        Assert.That(sizes.RequestHeadersSize, Is.GreaterThanOrEqualTo(190));
    }

    [UpstreamTest("NE-060 should set bodySize, headersSize, and transferSize")]
    public async Task ShouldSetBodySizeHeadersSizeAndTransferSize()
    {
        Server.SetRoute("/get", (req, res) =>
        {
            // In Firefox, |fetch| will be hanging until it receives |Content-Type| header
            // from server.
            res.SetHeader("Content-Type", "text/plain; charset=utf-8");
            res.End("abc134");
        });
        await Page.GotoAsync(Server.EmptyPage);
        var (response, _, _) = await PromiseAll(
            Page.WaitForResponseAsync(_ => true),
            Page.EvaluateAsync("async () => fetch('./get').then(r => r.text())"),
            Server.WaitForRequest("/get"));
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(6));
        Assert.That(sizes.ResponseHeadersSize, Is.GreaterThanOrEqualTo(100));
    }

    [UpstreamTest("NE-061 should set bodySize to 0 when there was no response body")]
    public async Task ShouldSetBodySizeTo0WhenThereWasNoResponseBody()
    {
        var response = await Page.GotoAsync(Server.EmptyPage);
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(0));
        Assert.That(sizes.ResponseHeadersSize, Is.GreaterThanOrEqualTo(150));
    }

    [UpstreamTest("NE-062 should have the correct responseBodySize")]
    public async Task ShouldHaveTheCorrectResponseBodySize()
    {
        var response = await Page.GotoAsync(Server.Prefix + "/simplezip.json");
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(new FileInfo(Asset("simplezip.json")).Length));
    }

    [UpstreamTest("NE-063 should have the correct responseBodySize for chunked request")]
    public async Task ShouldHaveTheCorrectResponseBodySizeForChunkedRequest()
    {
        Info.Fixme(BrowserName == "firefox");
        Info.Fixme(BrowserName == "webkit" && Platform != "darwin");
        var content = File.ReadAllBytes(Asset("simplezip.json"));
        var AMOUNT_OF_CHUNKS = 10;
        var CHUNK_SIZE = (int)Math.Ceiling(content.Length / (double)AMOUNT_OF_CHUNKS);
        Server.SetRoute("/chunked-simplezip.json", async (req, resp) =>
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
        var response = await Page.GotoAsync(Server.Prefix + "/chunked-simplezip.json");
        var sizes = await response.Request.SizesAsync();
        // The actual file size is 5100 bytes. The extra 75 bytes are coming from the chunked encoding headers and end bytes.
        if (BrowserName == "webkit")
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
    public async Task ShouldHaveTheCorrectResponseBodySizeWithGzipCompression()
    {
        Server.EnableGzip("/simplezip.json");
        await Page.GotoAsync(Server.EmptyPage);
        var (response, _) = await PromiseAll(
            Page.WaitForResponseAsync(_ => true),
            Page.EvaluateAsync("() => fetch('./simplezip.json').then(r => r.text())"));
        var sizes = await response.Request.SizesAsync();

        // .NET: fs.createReadStream(asset('simplezip.json')).pipe(zlib.createGzip()) and its 'data' and 'end' events are a GZipStream.
        using var chunks = new MemoryStream();
        using (var gzip = new GZipStream(chunks, CompressionLevel.Optimal, true))
        {
            await gzip.WriteAsync(File.ReadAllBytes(Asset("simplezip.json")));
        }

        Assert.That(sizes.ResponseBodySize, Is.EqualTo(chunks.ToArray().Length));
    }

    [UpstreamTest("NE-065 should handle redirects")]
    public async Task ShouldHandleRedirects()
    {
        Server.SetRedirect("/foo", "/bar");
        Server.SetRoute("/bar", (req, resp) => resp.End("bar"));
        await Page.GotoAsync(Server.EmptyPage);
        var (response, _) = await PromiseAll(
            Page.WaitForResponseAsync(_ => true),
            Page.EvaluateAsync(@"async () => fetch('/foo', {
      method: 'POST',
      body: '12345',
    }).then(r => r.text())"));
        Assert.That((await response.Request.SizesAsync()).RequestBodySize, Is.EqualTo(5));
        var newRequest = response.Request.RedirectedTo;
        Assert.That((await newRequest.SizesAsync()).ResponseBodySize, Is.EqualTo(3));
    }

    [UpstreamTest("NE-066 should throw for failed requests")]
    [LocalFixme("dotnet: the message of Request.SizesAsync for a failed request is \"Unable to fetch resources sizes.\", upstream has \"Unable to fetch sizes for failed request\"")]
    public async Task ShouldThrowForFailedRequests()
    {
        Server.SetRoute("/one-style.css", (req, res) =>
        {
            res.SetHeader("Content-Type", "text/css");
            res.Connection.Destroy();
        });
        await Page.GotoAsync(Server.EmptyPage);
        var (request, _) = await PromiseAll(
            Once<IRequest>(h => Page.RequestFailed += h, h => Page.RequestFailed -= h),
            Page.GotoAsync(Server.Prefix + "/one-style.html"));
        var error = await ThrowsAsync<PlaywrightException>(() => request.SizesAsync());
        Assert.That(error.Message, Does.Contain("Unable to fetch sizes for failed request"));
    }

    [UpstreamTest("NE-067 should work with ${statusCode} status code")]
    [TestCase(200)]
    [TestCase(401)]
    [TestCase(404)]
    [TestCase(500)]
    public async Task ShouldWorkWithStatusCode(int statusCode)
    {
        Server.SetRoute("/foo", (req, resp) =>
        {
            resp.WriteHead(statusCode, new()
            {
                ["Content-Type"] = "text/plain; charset=utf-8",
                ["Content-Length"] = "3",
            });
            resp.End("bar");
        });
        await Page.GotoAsync(Server.EmptyPage);
        var (response, _) = await PromiseAll(
            Page.WaitForResponseAsync(_ => true),
            Page.EvaluateAsync(@"async () => fetch('/foo', {
        method: 'POST',
        body: '12345',
      }).then(r => r.text())"));
        Assert.That(response.Status, Is.EqualTo(statusCode));
        var sizes = await response.Request.SizesAsync();
        Assert.That(sizes.RequestBodySize, Is.EqualTo(5));
        Assert.That(sizes.ResponseBodySize, Is.EqualTo(3));
    }

    [UpstreamTest("NE-068 should have correct responseBodySize for 404 with content")]
    public async Task ShouldHaveCorrectResponseBodySizeFor404WithContent()
    {
        Info.Fixme(BrowserName == "chromium");

        Server.SetRoute("/broken-image.png", (req, resp) =>
        {
            resp.WriteHead(404);
            resp.End("<p>this should have a non-negative size</p>");
        });
        Server.SetRoute("/page-with-404-image.html", (req, resp) =>
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
            Page.WaitForRequestAsync(new Regex("broken-image\\.png$")),
            Page.GotoAsync(Server.Prefix + "/page-with-404-image.html"));

        // .NET: const { responseBodySize } = await req.sizes();
        var responseBodySize = (await req.SizesAsync()).ResponseBodySize;
        Assert.That(responseBodySize, Is.GreaterThanOrEqualTo(0));
    }

    [UpstreamTest("NE-069 should return sizes without hanging")]
    public async Task ShouldReturnSizesWithoutHanging()
    {
        Info.Fixme(BrowserName == "chromium");

        Server.SetRoute("/has-abandoned-fetch", (req, resp) =>
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
            Page.WaitForRequestAsync(new Regex("404$")),
            Page.GotoAsync(Server.Prefix + "/has-abandoned-fetch"));

        await req.SizesAsync();
    }
}
