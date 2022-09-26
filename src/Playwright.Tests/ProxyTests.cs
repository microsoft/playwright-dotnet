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

using System.Text;
using Microsoft.AspNetCore.Http;

namespace Microsoft.Playwright.Tests;

public class ProxyTests : PlaywrightTestEx
{
    [PlaywrightTest("proxy.spec.ts", "should use proxy")]
    public async Task ShouldUseProxy()
    {
        Server.SetRoute("/target.html", ctx => ctx.Response.WriteAsync("<html><title>Served by the proxy</title></html>"));

        var proxy = new Proxy { Server = $"localhost:{Server.Port}" };

        await using var browser = await BrowserType.LaunchAsync(new() { Proxy = proxy });

        var page = await browser.NewPageAsync();
        await page.GotoAsync("http://non-existent.com/target.html");

        Assert.AreEqual("Served by the proxy", await page.TitleAsync());
    }

    [PlaywrightTest("proxy.spec.ts", "should authenticate")]
    public async Task ShouldAuthenticate()
    {
        Server.SetRoute("/target.html", ctx =>
        {
            string auth = ctx.Request.Headers["proxy-authorization"];

            if (string.IsNullOrEmpty(auth))
            {
                ctx.Response.StatusCode = 407;
                ctx.Response.Headers["Proxy-Authenticate"] = "Basic realm=\"Access to internal site\"";
            }

            return ctx.Response.WriteAsync($"<html><title>{auth}</title></html>");
        });

        var proxy = new Proxy
        {
            Server = $"localhost:{Server.Port}",
            Username = "user",
            Password = "secret"
        };

        await using var browser = await BrowserType.LaunchAsync(new() { Proxy = proxy });

        var page = await browser.NewPageAsync();
        await page.GotoAsync("http://non-existent.com/target.html");

        Assert.AreEqual("Basic " + Convert.ToBase64String(Encoding.UTF8.GetBytes("user:secret")), await page.TitleAsync());
    }

    [PlaywrightTest("proxy.spec.ts", "should exclude patterns")]
    public async Task ShouldExcludePatterns()
    {
        Server.SetRoute("/target.html", ctx => ctx.Response.WriteAsync("<html><title>Served by the proxy</title></html>"));

        var proxy = new Proxy
        {
            Server = $"localhost:{Server.Port}",
            Bypass = "non-existent1.com, .non-existent2.com, .another.test",
        };

        await using var browser = await BrowserType.LaunchAsync(new() { Proxy = proxy });

        var page = await browser.NewPageAsync();
        await page.GotoAsync("http://non-existent.com/target.html");

        Assert.AreEqual("Served by the proxy", await page.TitleAsync());

        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://non-existent1.com/target.html"));
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://sub.non-existent2.com/target.html"));
        await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.GotoAsync("http://foo.is.the.another.test/target.html"));
    }
}
