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

using System.Globalization;
using System.Net;

namespace Microsoft.Playwright.Tests;

public class BrowserContextCredentialsTests : BrowserTestEx
{
    [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail without credentials")]
    public async Task ShouldFailWithoutCredentials()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        await using var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.Status);
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should work with correct credentials")]
    public async Task ShouldWorkWithCorrectCredentials()
    {
        // Use unique user/password since Chromium caches credentials per origin.
        Server.SetAuth("/empty.html", "user", "pass");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass"
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail if wrong credentials")]
    public async Task ShouldFailIfWrongCredentials()
    {
        // Use unique user/password since Chromium caches credentials per origin.
        Server.SetAuth("/empty.html", "user", "pass");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "foo",
                Password = "bar"
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.Status);
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should return resource body")]
    public async Task ShouldReturnResourceBody()
    {
        Server.SetAuth("/playground.html", "user", "pass");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass"
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.Prefix + "/playground.html");
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        Assert.AreEqual("Playground", await page.TitleAsync());
        StringAssert.Contains("Playground", await response.TextAsync());
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should work with correct credentials and matching origin")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOrigin()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass",
                Origin = Server.Prefix
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should work with correct credentials and matching origin case insensitive")]
    public async Task ShouldWorkWithCorrectCredentialsAndMatchingOriginCaseInsensitive()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass",
                Origin = Server.Prefix.ToUpperInvariant()
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail with correct credentials and mismatching scheme")]
    public async Task ShouldFailWithCorrectCredentialsAndMismatchingScheme()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass",
                Origin = Server.Prefix.Replace("http://", "https://")
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.Status);
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail with correct credentials and mismatching hostname")]
    public async Task ShouldFailWithCorrectCredentialsAndMismatchingHostname()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var hostname = new Uri(Server.Prefix).Host;
        var origin = Server.Prefix.Replace(hostname, "mismatching-hostname");
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass",
                Origin = origin
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.Status);
    }

    [PlaywrightTest("browsercontext-credentials.spec.ts", "should fail with correct credentials and mismatching port")]
    public async Task ShouldFailWithCorrectCredentialsAndMismatchingPort()
    {
        Server.SetAuth("/empty.html", "user", "pass");
        var origin = Server.Prefix.Replace(Server.Port.ToString(CultureInfo.InvariantCulture), (Server.Port + 1).ToString(CultureInfo.InvariantCulture));
        await using var context = await Browser.NewContextAsync(new()
        {
            HttpCredentials = new()
            {
                Username = "user",
                Password = "pass",
                Origin = origin
            },
        });

        var page = await context.NewPageAsync();
        var response = await page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.Unauthorized, response.Status);
    }
}
