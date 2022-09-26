/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using Microsoft.Playwright.Tests.TestServer;

namespace Microsoft.Playwright.Tests;

public class HttpService : IWorkerService
{
    public SimpleServer Server { get; internal set; }
    public SimpleServer HttpsServer { get; internal set; }

    public static Task<HttpService> Register(WorkerAwareTest test)
    {
        var workerIndex = test.WorkerIndex;
        return test.RegisterService("Http", async () =>
        {
            var assetDir = Path.Combine(TestUtils.FindParentDirectory("Playwright.Tests.TestServer"), "assets");
            var http = new HttpService
            {
                Server = SimpleServer.Create(8907 + workerIndex * 2, assetDir),
                HttpsServer = SimpleServer.CreateHttps(8907 + workerIndex * 2 + 1, assetDir)
            };
            await Task.WhenAll(http.Server.StartAsync(), http.HttpsServer.StartAsync());
            return http;
        });
    }

    public Task ResetAsync()
    {
        Server.Reset();
        HttpsServer.Reset();
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        return Task.WhenAll(Server.StopAsync(), HttpsServer.StopAsync());
    }
}
