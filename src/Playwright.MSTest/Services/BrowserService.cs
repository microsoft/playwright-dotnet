/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.TestAdapter;

namespace Microsoft.Playwright.MSTest.Services;

internal class BrowserService : IWorkerService
{
    public IBrowser Browser { get; internal set; } = null!;

    public Task ResetAsync() => Task.CompletedTask;

    public Task DisposeAsync() => Browser?.CloseAsync() ?? Task.CompletedTask;

    public async Task BuildAsync(PlaywrightTest parentTest)
    {
        var accessKey = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_ACCESS_KEY");
        var serviceUrl = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_URL");

        if (string.IsNullOrEmpty(accessKey) || string.IsNullOrEmpty(serviceUrl))
        {
            Browser = await parentTest!.BrowserType!.LaunchAsync(PlaywrightSettingsProvider.LaunchOptions).ConfigureAwait(false);
        }
        else
        {
            var exposeNetwork = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_EXPOSE_NETWORK") ?? "<loopback>";
            var caps = new Dictionary<string, string>
            {
                ["os"] = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_OS") ?? "linux",
                ["runId"] = Environment.GetEnvironmentVariable("PLAYWRIGHT_SERVICE_RUN_ID") ?? DateTime.Now.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss", CultureInfo.InvariantCulture),
            };
            var wsEndpoint = $"{serviceUrl}?cap={JsonSerializer.Serialize(caps)}";
            var connectOptions = new BrowserTypeConnectOptions
            {
                Timeout = 3 * 60 * 1000,
                ExposeNetwork = exposeNetwork,
                Headers = new Dictionary<string, string>
                {
                    ["x-mpt-access-key"] = accessKey
                }
            };

            Browser = await parentTest!.BrowserType!.ConnectAsync(wsEndpoint, connectOptions).ConfigureAwait(false);
        }
    }
}
