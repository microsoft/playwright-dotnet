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
using System.Text.RegularExpressions;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Tests;

public class InterceptionTests : PageTestEx
{
    [PlaywrightTest("interception.spec.ts", "should work with glob")]
    public void ShouldWorkWithGlob()
    {
        Regex GlobToRegex(string glob)
        {
            return new Regex(URLMatch.GlobToRegexPattern(glob));
        }

        bool URLMatches(string baseURL, string url, string glob)
        {
            return new URLMatch()
            {
                baseURL = baseURL,
                glob = glob,
            }.Match(url);
        }

        Assert.That("https://localhost:8080/foo.js", Does.Match(GlobToRegex("**/*.js")));
        Assert.That("https://localhost:8080/foo.js", Does.Not.Match(GlobToRegex("**/*.css")));
        Assert.That("https://localhost:8080/foo.js", Does.Not.Match(GlobToRegex("*.js")));
        Assert.That("https://localhost:8080/foo.js", Does.Match(GlobToRegex("https://**/*.js")));
        Assert.That("http://localhost:8080/simple/path.js", Does.Match(GlobToRegex("http://localhost:8080/simple/path.js")));
        Assert.That("https://localhost:8080/a.js", Does.Match(GlobToRegex("**/{a,b}.js")));
        Assert.That("https://localhost:8080/b.js", Does.Match(GlobToRegex("**/{a,b}.js")));
        Assert.That("https://localhost:8080/c.js", Does.Not.Match(GlobToRegex("**/{a,b}.js")));
        Assert.That("https://localhost:8080/c.jpg", Does.Match(GlobToRegex("**/*.{png,jpg,jpeg}")));
        Assert.That("https://localhost:8080/c.jpeg", Does.Match(GlobToRegex("**/*.{png,jpg,jpeg}")));
        Assert.That("https://localhost:8080/c.png", Does.Match(GlobToRegex("**/*.{png,jpg,jpeg}")));
        Assert.That("https://localhost:8080/c.css", Does.Not.Match(GlobToRegex("**/*.{png,jpg,jpeg}")));
        Assert.That("foo.js", Does.Match(GlobToRegex("foo*")));
        Assert.That("foo/bar.js", Does.Not.Match(GlobToRegex("foo*")));
        Assert.That("http://localhost:3000/signin-oidc/foo", Does.Not.Match(GlobToRegex("http://localhost:3000/signin-oidc*")));
        Assert.That("http://localhost:3000/signin-oidcnice", Does.Match(GlobToRegex("http://localhost:3000/signin-oidc*")));

        // range [] is NOT supported
        Assert.That("http://example.com/api/v[0-9]", Does.Match(GlobToRegex("**/api/v[0-9]")));
        Assert.That("http://example.com/api/version", Does.Not.Match(GlobToRegex("**/api/v[0-9]")));

        // query params
        Assert.That("http://example.com/api?param", Does.Match(GlobToRegex("**/api\\?param")));
        Assert.That("http://example.com/api-param", Does.Not.Match(GlobToRegex("**/api\\?param")));
        Assert.That("http://mydomain:8080/blah/blah/three-columns/settings.html?id=settings-e3c58efe-02e9-44b0-97ac-dd138100cf7c&blah", Does.Match(GlobToRegex("**/three-columns/settings.html\\?**id=settings-**")));

        Assert.AreEqual("^\\?$", URLMatch.GlobToRegexPattern("\\?"));
        Assert.AreEqual("^\\\\$", URLMatch.GlobToRegexPattern("\\"));
        Assert.AreEqual("^\\\\$", URLMatch.GlobToRegexPattern("\\\\"));
        Assert.AreEqual("^\\[$", URLMatch.GlobToRegexPattern("\\["));
        Assert.AreEqual("^\\[a-z\\]$", URLMatch.GlobToRegexPattern("[a-z]"));
        Assert.AreEqual(@"^\$\^\+\.\*\(\)\|\?\{\}\[\]$", URLMatch.GlobToRegexPattern("$^+.\\*()|\\?\\{\\}\\[\\]"));

        Assert.True(URLMatches(null, "http://playwright.dev/", "http://playwright.dev"));
        Assert.True(URLMatches(null, "http://playwright.dev/?a=b", "http://playwright.dev?a=b"));
        Assert.True(URLMatches(null, "http://playwright.dev/", "h*://playwright.dev"));
        Assert.True(URLMatches(null, "http://api.playwright.dev/?x=y", "http://*.playwright.dev?x=y"));
        Assert.True(URLMatches(null, "http://playwright.dev/foo/bar", "**/foo/**"));
        Assert.True(URLMatches("http://playwright.dev", "http://playwright.dev/?x=y", "?x=y"));
        Assert.True(URLMatches("http://playwright.dev/foo/", "http://playwright.dev/foo/bar?x=y", "./bar?x=y"));

        // This is not supported, we treat ? as a query separator.
        Assert.That("http://localhost:8080/Simple/path.js", Does.Not.Match(GlobToRegex("http://localhost:8080/?imple/path.js")));
        Assert.False(URLMatches(null, "http://playwright.dev/", "http://playwright.?ev"));
        Assert.True(URLMatches(null, "http://playwright./?ev", "http://playwright.?ev"));
        Assert.False(URLMatches(null, "http://playwright.dev/foo", "http://playwright.dev/f??"));
        Assert.True(URLMatches(null, "http://playwright.dev/f??", "http://playwright.dev/f??"));
        Assert.True(URLMatches(null, "http://playwright.dev/?x=y", "http://playwright.dev\\?x=y"));
        Assert.True(URLMatches(null, "http://playwright.dev/?x=y", "http://playwright.dev/\\?x=y"));
        Assert.True(URLMatches("http://playwright.dev/foo", "http://playwright.dev/foo?bar", "?bar"));
        Assert.True(URLMatches("http://playwright.dev/foo", "http://playwright.dev/foo?bar", "\\\\?bar"));
        Assert.True(URLMatches("http://first.host/", "http://second.host/foo", "**/foo"));
        Assert.True(URLMatches("http://playwright.dev/", "http://localhost/", "*//localhost/"));

        // Should work with baseURL and various non-http schemes.
        Assert.True(URLMatches("http://playwright.dev/", "about:blank", "about:blank"));
        Assert.False(URLMatches("http://playwright.dev/", "about:blank", "http://playwright.dev/"));
        Assert.True(URLMatches("http://playwright.dev/", "about:blank", "about:*"));
        Assert.True(URLMatches("http://playwright.dev/", "data:text/html,", "data:*/*"));
        Assert.True(URLMatches("http://playwright.dev/", "file://path/to/file", "file://**"));
    }

    [PlaywrightTest("interception.spec.ts", "should intercept by glob")]
    public async Task ShouldInterceptByGlob()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("http://localhos**?*oo", (route) =>
        {
            return route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK, Body = "intercepted" });
        });
        var result = await Page.EvaluateAsync<string>("url => fetch(url).then(r => r.text())", Server.Prefix + "/?foo");
        Assert.AreEqual("intercepted", result);
    }

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
