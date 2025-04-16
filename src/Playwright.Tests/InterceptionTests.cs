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

using System.Net;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Tests;

public class InterceptionTests : PageTestEx
{
    [PlaywrightTest("interception.spec.ts", "should work with ignoreHTTPSErrors")]
    public async Task ShouldWorkWithIgnoreHTTPSErrors()
    {
        var context = await Browser.NewContextAsync(new()
        {
            IgnoreHTTPSErrors = true
        });

        var page = await context.NewPageAsync();

        await page.RouteAsync("**/*", (route) => route.ContinueAsync());
        var response = await page.GotoAsync(HttpsServer.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        await context.CloseAsync();
    }


    [PlaywrightTest("interception.spec.ts", "should work with navigation")]
    public async Task ShouldWorkWithNavigation()
    {
        var requests = new Dictionary<string, IRequest>();
        await Page.RouteAsync("**/*", (route) =>
        {
            requests.Add(route.Request.Url.Split('/').Last(), route.Request);
            route.ContinueAsync();
        });

        Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
        await Page.GotoAsync(Server.Prefix + "/rrredirect");
        Assert.True(requests["rrredirect"].IsNavigationRequest);
        Assert.True(requests["frame.html"].IsNavigationRequest);
        Assert.False(requests["script.js"].IsNavigationRequest);
        Assert.False(requests["style.css"].IsNavigationRequest);
    }

    [PlaywrightTest("interception.spec.ts", "should intercept after a service worker")]
    public async Task ShouldInterceptAfterAServiceWorker()
    {
        await Page.GotoAsync(Server.Prefix + "/serviceworkers/fetchdummy/sw.html");
        await Page.EvaluateAsync("() => window.activationPromise");

        string swResponse = await Page.EvaluateAsync<string>("() => fetchDummy('foo')");
        Assert.AreEqual("responseFromServiceWorker:foo", swResponse);

        await Page.RouteAsync("**/foo", (route) =>
        {
            int slash = route.Request.Url.LastIndexOf("/");
            string name = route.Request.Url.Substring(slash + 1);

            route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "responseFromInterception:" + name, ContentType = "text/css" });
        });

        string swResponse2 = await Page.EvaluateAsync<string>("() => fetchDummy('foo')");
        Assert.AreEqual("responseFromServiceWorker:foo", swResponse2);

        string nonInterceptedResponse = await Page.EvaluateAsync<string>("() => fetchDummy('passthrough')");
        Assert.AreEqual("FAILURE: Not Found", nonInterceptedResponse);
    }
}
