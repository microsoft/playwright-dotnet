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

using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Tests;

///<playwright-file>chromium/chromium.spec.ts</playwright-file>
public class BrowserTypeConnectOverCDPTests : PlaywrightTestEx
{
    [PlaywrightTest("chromium/chromium.spec.ts", "should connect to an existing cdp session")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldConnectToAnExistingCDPSession()
    {
        int port = 9393 + WorkerIndex;
        IBrowser browserServer = await BrowserType.LaunchAsync(new() { Args = new[] { $"--remote-debugging-port={port}" } });
        try
        {
            IBrowser cdpBrowser = await BrowserType.ConnectOverCDPAsync($"http://localhost:{port}/");
            Assert.AreEqual(cdpBrowser.BrowserType, Playwright.Chromium);
            var contexts = cdpBrowser.Contexts;
            Assert.AreEqual(1, cdpBrowser.Contexts.Count);
            var page = await cdpBrowser.Contexts[0].NewPageAsync();
            Assert.AreEqual(2, await page.EvaluateAsync<int>("1 + 1"));
            await cdpBrowser.CloseAsync();
        }
        finally
        {

            await browserServer.CloseAsync();
        }
    }

    [PlaywrightTest("chromium/chromium.spec.ts", "should send extra headers with connect request")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldSendExtraHeadersWithConnectRequest()
    {
        var waitForRequest = Server.WaitForWebSocketConnectionRequest();
        BrowserType.ConnectOverCDPAsync($"ws://localhost:{Server.Port}/ws", new()
        {
            Headers = new Dictionary<string, string> {
                    { "x-foo-bar", "fookek" }
                },
        }).IgnoreException();
        var req = await waitForRequest;
        Assert.AreEqual("fookek", req.Headers["x-foo-bar"]);
        StringAssert.Contains("Playwright", req.Headers["user-agent"]);
    }

    [PlaywrightTest("chromium/chromium.spec.ts", "should report all pages in an existing browser")]
    [Skip(SkipAttribute.Targets.Firefox, SkipAttribute.Targets.Webkit)]
    public async Task ShouldReportAllPagesInAnExistingBrowser()
    {
        int port = 9393 + WorkerIndex;
        var browserServer = await BrowserType.LaunchAsync(new() { Args = new[] { $"--remote-debugging-port={port}" } });
        try
        {
            var cdpBrowser = await BrowserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");
            var contexts = cdpBrowser.Contexts;
            Assert.AreEqual(1, cdpBrowser.Contexts.Count);
            for (int i = 0; i < 3; i++)
            {
                await cdpBrowser.Contexts[0].NewPageAsync();
            }
            await cdpBrowser.CloseAsync();

            var cdpBrowser2 = await BrowserType.ConnectOverCDPAsync($"http://127.0.0.1:{port}/");

            Assert.AreEqual(3, cdpBrowser2.Contexts[0].Pages.Count);
            await cdpBrowser2.CloseAsync();
        }
        finally
        {

            await browserServer.CloseAsync();
        }
    }
}
