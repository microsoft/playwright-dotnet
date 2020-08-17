using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BrowserContext
{
    ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
    ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class IgnoreHttpsErrorsTests : PlaywrightSharpBrowserBaseTest
    {
        /// <inheritdoc/>
        public IgnoreHttpsErrorsTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
            Assert.Equal(HttpStatusCode.OK, response.Status);
        }

        ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should isolate contexts</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldIsolateContexts()
        {
            await using (var context = await Browser.NewContextAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true }))
            {
                var page = await context.NewPageAsync();
                var response = await page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html");

                Assert.Equal(HttpStatusCode.OK, response.Status);
            }

            await using (var context = await Browser.NewContextAsync())
            {
                var page = await context.NewPageAsync();
                await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html"));
            }
        }

        ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should work with mixed content</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithMixedContent()
        {
            HttpsServer.SetRoute("/mixedcontent.html", async (context) =>
            {
                await context.Response.WriteAsync($"<iframe src='{TestConstants.EmptyPage}'></iframe>");
            });
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.HttpsPrefix + "/mixedcontent.html", LifecycleEvent.DOMContentLoaded);
            Assert.Equal(2, page.Frames.Length);
            Assert.Equal(3, await page.MainFrame.EvaluateAsync<int>("1 + 2"));
            Assert.Equal(5, await page.FirstChildFrame().EvaluateAsync<int>("2 + 3"));
        }

        ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should work with WebSocket</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>ignorehttpserrors.spec.js</playwright-file>
        ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
        ///<playwright-it>should fail with WebSocket if not ignored</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
