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

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    [TestClass]
    public class GlobTests : PageTestEx
    {
        [PlaywrightTest("interception.spec.ts", "should work with glob")]
        public void ShouldWorkWithGlob()
        {

            StringAssert.Matches("https://localhost:8080/foo.js", StringExtensions.GlobToRegex("**/*.js"));
            StringAssert.Matches("https://localhost:8080/foo.js", StringExtensions.GlobToRegex("https://**/*.js"));
            StringAssert.Matches("http://localhost:8080/simple/path.js", StringExtensions.GlobToRegex("http://localhost:8080/simple/path.js"));
            StringAssert.Matches("http://localhost:8080/Simple/path.js", StringExtensions.GlobToRegex("http://localhost:8080/?imple/path.js"));
            StringAssert.Matches("https://localhost:8080/a.js", StringExtensions.GlobToRegex("**/{a,b}.js"));
            StringAssert.Matches("https://localhost:8080/b.js", StringExtensions.GlobToRegex("**/{a,b}.js"));
            StringAssert.Matches("https://localhost:8080/c.jpg", StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"));
            StringAssert.Matches("https://localhost:8080/c.jpeg", StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"));
            StringAssert.Matches("https://localhost:8080/c.png", StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"));
            StringAssert.DoesNotMatch("https://localhost:8080/c.css", StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"));
            StringAssert.DoesNotMatch("https://localhost:8080/foo.js", StringExtensions.GlobToRegex("**/*.css"));
            StringAssert.DoesNotMatch("https://localhost:8080/foo.js", StringExtensions.GlobToRegex("*.js"));
            StringAssert.DoesNotMatch("https://localhost:8080/c.js", StringExtensions.GlobToRegex("**/{a,b}.js"));
            StringAssert.Matches("foo.js", StringExtensions.GlobToRegex("foo*"));
            StringAssert.DoesNotMatch("foo/bar.js", StringExtensions.GlobToRegex("foo*"));
            StringAssert.DoesNotMatch("http://localhost:3000/signin-oidc/foo", StringExtensions.GlobToRegex("http://localhost:3000/signin-oidc*"));
            StringAssert.Matches("http://localhost:3000/signin-oidcnice", StringExtensions.GlobToRegex("http://localhost:3000/signin-oidc*"));
        }

        [PlaywrightTest("interception.spec.ts", "should work with ignoreHTTPSErrors")]
        [Ignore("Fix me #1058")]
        public async Task ShouldWorkWitIgnoreHTTPSErrors()
        {
            await using var browser = await BrowserType.LaunchAsync();
            var context = await browser.NewContextAsync(new()
            {
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();

            await page.RouteAsync("**/*", (route) => route.ContinueAsync());
            var response = await page.GotoAsync(HttpsServer.Prefix + "/empty.html");
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
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
            Assert.IsTrue(requests["rrredirect"].IsNavigationRequest);
            Assert.IsTrue(requests["frame.html"].IsNavigationRequest);
            Assert.IsFalse(requests["script.js"].IsNavigationRequest);
            Assert.IsFalse(requests["style.css"].IsNavigationRequest);
        }

        [PlaywrightTest("interception.spec.ts", "should work with regular expression passed from a different context")]
        [Ignore("We don't need to test Regex contexts")]
        public void ShouldWorkWithRegularExpressionPassedFromADifferentContext()
        {
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
}
