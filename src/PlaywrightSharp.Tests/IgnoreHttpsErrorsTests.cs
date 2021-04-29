using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>ignorehttpserrors.spec.ts</playwright-file>
    ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class IgnoreHttpsErrorsTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public IgnoreHttpsErrorsTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("ignorehttpserrors.spec.ts", "should work")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldWork()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            var requestTask = HttpsServer.WaitForRequest(
                "/empty.html",
                request => request.HttpContext.Features.Get<ITlsHandshakeFeature>().Protocol);
            var responseTask = page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");

            await TaskUtils.WhenAll(
                requestTask,
                responseTask);

            var response = responseTask.Result;
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [PlaywrightTest("ignorehttpserrors.spec.ts", "should isolate contexts")]
        /// [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldIsolateContexts()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true }))
            {
                var page = await context.NewPageAsync();
                var response = await page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");

                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html"));
            }
        }

        [PlaywrightTest("ignorehttpserrors.spec.ts", "should work with mixed content")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldWorkWithMixedContent()
        {
            HttpsServer.SetRoute("/mixedcontent.html", async (context) =>
            {
                await context.Response.WriteAsync($"<iframe src='{TestConstants.EmptyPage}'></iframe>");
            });
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.HttpsPrefix + "/mixedcontent.html", WaitUntilState.DOMContentLoaded);
            Assert.Equal(2, page.Frames.Count);
            Assert.Equal(3, await page.MainFrame.EvaluateAsync<int>("1 + 2"));
            Assert.Equal(5, await page.FirstChildFrame().EvaluateAsync<int>("2 + 3"));
        }

        [PlaywrightTest("ignorehttpserrors.spec.ts", "should work with WebSocket")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
        public async Task ShouldWorkWithWebSocket()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            string value = await page.EvaluateAsync<string>(@"endpoint => {
                let cb;
              const result = new Promise(f => cb = f);
              const ws = new WebSocket(endpoint);
              ws.addEventListener('message', data => { ws.close(); cb(data.data); });
              ws.addEventListener('error', error => cb('Error'));
              return result;
            }", TestConstants.HttpsPrefix.Replace("https", "wss") + "/ws");

            Assert.Equal("incoming", value);
        }

        [PlaywrightTest("ignorehttpserrors.spec.ts", "should fail with WebSocket if not ignored")]
        // [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        [Fact(Skip = "Fix me #1058")]
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
            }", TestConstants.HttpsPrefix.Replace("https", "wss") + "/ws");

            Assert.Equal("Error", value);
        }
    }
}
