using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class GlobTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public GlobTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("interception.spec.ts", "should work with glob")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public void ShouldWorkWithGlob()
        {
            Assert.Matches(StringExtensions.GlobToRegex("**/*.js"), "https://localhost:8080/foo.js");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("**/*.css"), "https://localhost:8080/foo.js");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("*.js"), "https://localhost:8080/foo.js");
            Assert.Matches(StringExtensions.GlobToRegex("https://**/*.js"), "https://localhost:8080/foo.js");
            Assert.Matches(StringExtensions.GlobToRegex("http://localhost:8080/simple/path.js"), "http://localhost:8080/simple/path.js");
            Assert.Matches(StringExtensions.GlobToRegex("http://localhost:8080/?imple/path.js"), "http://localhost:8080/Simple/path.js");
            Assert.Matches(StringExtensions.GlobToRegex("**/{a,b}.js"), "https://localhost:8080/a.js");
            Assert.Matches(StringExtensions.GlobToRegex("**/{a,b}.js"), "https://localhost:8080/b.js");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("**/{a,b}.js"), "https://localhost:8080/c.js");

            Assert.Matches(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.jpg");
            Assert.Matches(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.jpeg");
            Assert.Matches(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.png");
            Assert.DoesNotMatch(StringExtensions.GlobToRegex("**/*.{png,jpg,jpeg}"), "https://localhost:8080/c.css");
        }

        [PlaywrightTest("interception.spec.ts", "should work with ignoreHTTPSErrors")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldWorkWitIgnoreHTTPSErrors()
        {
            await using var browser = await BrowserType.LaunchAsync(TestConstants.GetDefaultBrowserOptions());
            var context = await browser.NewContextAsync(new BrowserContextOptions
            {
                IgnoreHTTPSErrors = true
            });

            var page = await context.NewPageAsync();

            await page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
            var response = await page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }


        [PlaywrightTest("interception.spec.ts", "should work with navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNavigation()
        {
            var requests = new Dictionary<string, IRequest>();
            await Page.RouteAsync("**/*", (route, request) =>
            {
                requests.Add(request.Url.Split('/').Last(), request);
                route.ContinueAsync();
            });

            Server.SetRedirect("/rrredirect", "/frames/one-frame.html");
            await Page.GoToAsync(TestConstants.ServerUrl + "/rrredirect");
            Assert.True(requests["rrredirect"].IsNavigationRequest);
            Assert.True(requests["frame.html"].IsNavigationRequest);
            Assert.False(requests["script.js"].IsNavigationRequest);
            Assert.False(requests["style.css"].IsNavigationRequest);
        }

        [PlaywrightTest("interception.spec.ts", "should work with regular expression passed from a different context")]
        [Fact(Skip = "We don't need to test Regex contexts")]
        public void ShouldWorkWithRegularExpressionPassedFromADifferentContext()
        {
        }

        [PlaywrightTest("interception.spec.ts", "should intercept after a service worker")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldInterceptAfterAServiceWorker()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/serviceworkers/fetchdummy/sw.html");
            await Page.EvaluateAsync("() => window.activationPromise");

            string swResponse = await Page.EvaluateAsync<string>("() => fetchDummy('foo')");
            Assert.Equal("responseFromServiceWorker:foo", swResponse);

            await Page.RouteAsync("**/foo", (route, _) =>
            {
                int slash = route.Request.Url.LastIndexOf("/");
                string name = route.Request.Url.Substring(slash + 1);

                route.FulfillAsync(HttpStatusCode.OK, "responseFromInterception:" + name, contentType: "text/css");
            });

            string swResponse2 = await Page.EvaluateAsync<string>("() => fetchDummy('foo')");
            Assert.Equal("responseFromServiceWorker:foo", swResponse2);

            string nonInterceptedResponse = await Page.EvaluateAsync<string>("() => fetchDummy('passthrough')");
            Assert.Equal("FAILURE: Not Found", nonInterceptedResponse);
        }
    }
}
