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

using System.Net;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

///<playwright-file>ignorehttpserrors.spec.ts</playwright-file>
///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
public class IgnoreHttpsErrorsTests : BrowserTestEx
{
    [PlaywrightTest("ignorehttpserrors.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await using var context = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        var responseTask = page.GotoAsync(HttpsServer.EmptyPage);

        var response = responseTask.Result;
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("ignorehttpserrors.spec.ts", "should isolate contexts")]
    public async Task ShouldIsolateContexts()
    {
        await using (var context = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true }))
        {
            var page = await context.NewPageAsync();
            var response = await page.GotoAsync(HttpsServer.Prefix + "/empty.html");

            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        await using (var context = await Browser.NewContextAsync())
        {
            var page = await context.NewPageAsync();
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync(HttpsServer.Prefix + "/empty.html"));
        }
    }

    [PlaywrightTest("ignorehttpserrors.spec.ts", "should work with mixed content")]
    public async Task ShouldWorkWithMixedContent()
    {
        HttpsServer.SetRoute("/mixedcontent.html", async (context) =>
        {
            await context.Response.WriteAsync($"<iframe src='{Server.EmptyPage}'></iframe>");
        });
        await using var context = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        await page.GotoAsync(HttpsServer.Prefix + "/mixedcontent.html", new() { WaitUntil = WaitUntilState.Load });
        Assert.AreEqual(2, page.Frames.Count);
        Assert.AreEqual(3, await page.MainFrame.EvaluateAsync<int>("1 + 2"));
        Assert.AreEqual(5, await page.FirstChildFrame().EvaluateAsync<int>("2 + 3"));
    }

    [PlaywrightTest("ignorehttpserrors.spec.ts", "should work with WebSocket")]
    public async Task ShouldWorkWithWebSocket()
    {
        HttpsServer.SendOnWebSocketConnection("incoming");
        await using var context = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
        var page = await context.NewPageAsync();
        string value = await page.EvaluateAsync<string>(@"endpoint => {
                let cb;
              const result = new Promise(f => cb = f);
              const ws = new WebSocket(endpoint);
              ws.addEventListener('message', data => { ws.close(); cb(data.data); });
              ws.addEventListener('error', error => cb('Error'));
              return result;
            }", HttpsServer.Prefix.Replace("https", "wss") + "/ws");

        Assert.AreEqual("incoming", value);
    }

    [PlaywrightTest("ignorehttpserrors.spec.ts", "should fail with WebSocket if not ignored")]
    public async Task ShouldFailWithWebSocketIfNotIgnored()
    {
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        string value = await page.EvaluateAsync<string>(@"endpoint => {
                let cb;
              const result = new Promise(f => cb = f);
              const ws = new WebSocket(endpoint);
              ws.addEventListener('message', data => { ws.close(); cb(data.data); });
              ws.addEventListener('error', error => cb('Error'));
              return result;
            }", HttpsServer.Prefix.Replace("https", "wss") + "/ws");

        Assert.AreEqual("Error", value);
    }
}
