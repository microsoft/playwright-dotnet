/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System;
using System.IO;
using System.IO.Compression;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests.TestServer
{
    internal class SimpleCompressionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly SimpleServer _server;

        public SimpleCompressionMiddleware(RequestDelegate next, SimpleServer server)
        {
            _next = next;
            _server = server;
        }

        public async Task Invoke(HttpContext context)
        {
            if (!_server.GzipRoutes.Contains(context.Request.Path))
            {
                await _next(context);
                return;
            }

            var response = context.Response.Body;
            var bodyWrapperStream = new MemoryStream();
            context.Response.Body = bodyWrapperStream;

            await _next(context);
            using (var stream = new MemoryStream())
            {
                using (var compressionStream = new GZipStream(stream, CompressionMode.Compress, true))
                {
                    bodyWrapperStream.Position = 0;
                    bodyWrapperStream.CopyTo(compressionStream);
                }

                context.Response.Headers["Content-Encoding"] = "gzip";
                context.Response.Headers["Content-Length"] = stream.Length.ToString();
                stream.Position = 0;
                await stream.CopyToAsync(response);
                context.Response.Body = response;
            }
        }
    }
}
