using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class GlobTests : PageTestEx
    {
        [PlaywrightTest("interception.spec.ts", "should work with glob")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public void ShouldWorkWithGlob()
        {
            Assert.That("https://localhost:8080/foo.js", Does.Match(StringExtensions.GlobToRegex("**/*.js")));
            Assert.That("https://localhost:8080/foo.js", Does.Match(StringExtensions.GlobToRegex("https://**/*.js")));
            Assert.That("http://localhost:8080/simple/path.js", Does.Match(StringExtensions.GlobToRegex("http://localhost:8080/simple/path.js")));
            Assert.That("http://localhost:8080/Simple/path.js", Does.Match(StringExtensions.GlobToRegex("http://localhost:8080/?imple/path.js")));
            Assert.That("https://localhost:8080/a.js", Does.Match(StringExtensions.GlobToRegex("**/{a,b}.js")));
            Assert.That("https://localhost:8080/b.js", Does.Match(StringExtensions.GlobToRegex("**/{a,b}.js")));
            Assert.That("https://localhost:8080/c.jpg", Does.Match(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}")));
            Assert.That("https://localhost:8080/c.jpeg", Does.Match(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}")));
            Assert.That("https://localhost:8080/c.png", Does.Match(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}")));
            Assert.That("https://localhost:8080/c.css", Does.Not.Match(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}")));
            Assert.That("https://localhost:8080/foo.js", Does.Not.Match(StringExtensions.GlobToRegex("**/*.css")));
            Assert.That("https://localhost:8080/foo.js", Does.Not.Match(StringExtensions.GlobToRegex("*.js")));
            Assert.That("https://localhost:8080/c.js", Does.Not.Match(StringExtensions.GlobToRegex("**/{a,b}.js")));
            Assert.That("foo.js", Does.Match(StringExtensions.GlobToRegex("foo*")));
            Assert.That("foo/bar.js", Does.Not.Match(StringExtensions.GlobToRegex("foo*")));
            Assert.That("http://localhost:3000/signin-oidc/foo", Does.Not.Match(StringExtensions.GlobToRegex("http://localhost:3000/signin-oidc*")));
            Assert.That("http://localhost:3000/signin-oidcnice", Does.Match(StringExtensions.GlobToRegex("http://localhost:3000/signin-oidc*")));
        }

        [PlaywrightTest("interception.spec.ts", "should work with ignoreHTTPSErrors")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
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
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("interception.spec.ts", "should work with regular expression passed from a different context")]
        [Test, Ignore("We don't need to test Regex contexts")]
        public void ShouldWorkWithRegularExpressionPassedFromADifferentContext()
        {
        }

        [PlaywrightTest("interception.spec.ts", "should intercept after a service worker")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
