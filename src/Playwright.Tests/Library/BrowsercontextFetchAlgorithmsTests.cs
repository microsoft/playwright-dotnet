/**
 * Copyright (c) 2014-present Matt Zabriskie
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

using System.IO.Compression;
using System.Text;
using Microsoft.Playwright.Tests.Fixtures;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-fetch-algorithms.spec.ts.</summary>
public class BrowsercontextFetchAlgorithmsTests
{
    // .NET: util.promisify(zlib.gzip) is a GZipStream.
    private static async Task<byte[]> Gzip(string buffer)
    {
        using var result = new MemoryStream();
        using (var stream = new GZipStream(result, CompressionLevel.Optimal, true))
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(buffer));
        }
        return result.ToArray();
    }

    // .NET: util.promisify(zlib.deflate) is a ZLibStream.
    private static async Task<byte[]> Deflate(string buffer)
    {
        using var result = new MemoryStream();
        using (var stream = new ZLibStream(result, CompressionLevel.Optimal, true))
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(buffer));
        }
        return result.ToArray();
    }

    // .NET: util.promisify(zlib.brotliCompress) is a BrotliStream.
    private static async Task<byte[]> BrotliCompress(string buffer)
    {
        using var result = new MemoryStream();
        using (var stream = new BrotliStream(result, CompressionLevel.Optimal, true))
        {
            await stream.WriteAsync(Encoding.UTF8.GetBytes(buffer));
        }
        return result.ToArray();
    }

    [BeforeEach]
    public static void Skip(string mode) => It.Skip(mode != "default");

    public class Algorithms
    {
        private const string responseBody = "str";

        [TestFixture("gzip")]
        [TestFixture("deflate")]
        [TestFixture("br")]
        public class Decompression
        {
            private readonly string type;
            private readonly Task<byte[]> zipped;

            public Decompression(string type)
            {
                this.type = type;
                // .NET: the value of the entry of { gzip: gzip(responseBody), deflate: deflate(responseBody), br: brotliCompress(responseBody) }.
                zipped = type switch
                {
                    "gzip" => Gzip(responseBody),
                    "deflate" => Deflate(responseBody),
                    "br" => BrotliCompress(responseBody),
                    _ => throw new ArgumentException(type, nameof(type)),
                };
            }

            [UpstreamTest("FE-180 should support decompression")]
            public async Task ShouldSupportDecompression(IBrowserContext context, Server server)
            {
                server.SetRoute("/compressed", async (req, res) =>
                {
                    res.SetHeader("Content-Encoding", type);
                    res.End(await zipped);
                });

                var response = await context.APIRequest.GetAsync(server.Prefix + "/compressed");
                Assert.That(await response.TextAsync(), Is.EqualTo(responseBody));
            }

            [UpstreamTest("FE-181 should not fail if response content-length header is missing (${type})")]
            public async Task ShouldNotFailIfResponseContentLengthHeaderIsMissing(IBrowserContext context, Server server)
            {
                server.SetRoute("/compressed", async (req, res) =>
                {
                    res.SetHeader("Content-Encoding", type);
                    res.RemoveHeader("Content-Length");
                    res.End(await zipped);
                });

                var response = await context.APIRequest.GetAsync(server.Prefix + "/compressed");
                Assert.That(await response.TextAsync(), Is.EqualTo(responseBody));
            }

            [UpstreamTest("FE-182 should not fail with chunked responses (without Content-Length header)")]
            public async Task ShouldNotFailWithChunkedResponsesWithoutContentLengthHeader(IBrowserContext context, Server server)
            {
                server.SetRoute("/compressed", async (req, res) =>
                {
                    res.SetHeader("Content-Encoding", type);
                    res.SetHeader("Transfer-Encoding", "chunked");
                    res.RemoveHeader("Content-Length");
                    res.Write(await zipped);
                    res.End();
                });

                var response = await context.APIRequest.GetAsync(server.Prefix + "/compressed");
                Assert.That(await response.TextAsync(), Is.EqualTo(responseBody));
            }

            [UpstreamTest("FE-183 should not fail with an empty response without content-length header (Z_BUF_ERROR)")]
            public async Task ShouldNotFailWithAnEmptyResponseWithoutContentLengthHeaderZBUFERROR(IBrowserContext context, Server server)
            {
                server.SetRoute("/compressed", async (req, res) =>
                {
                    res.SetHeader("Content-Encoding", type);
                    res.RemoveHeader("Content-Length");
                    res.End();
                });

                var response = await context.APIRequest.GetAsync(server.Prefix + "/compressed");
                Assert.That(await response.TextAsync(), Is.EqualTo(""));
            }

            [UpstreamTest("FE-184 should not fail with an empty response with content-length header (Z_BUF_ERROR)")]
            public async Task ShouldNotFailWithAnEmptyResponseWithContentLengthHeaderZBUFERROR(IBrowserContext context, Server server)
            {
                server.SetRoute("/compressed", async (req, res) =>
                {
                    res.SetHeader("Content-Encoding", type);
                    res.End();
                });

                await context.APIRequest.GetAsync(server.Prefix + "/compressed");
            }
        }
    }
}
