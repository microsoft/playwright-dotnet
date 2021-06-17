using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>ignorehttpserrors.spec.ts</playwright-file>
    ///<playwright-describe>ignoreHTTPSErrors</playwright-describe>
    [Parallelizable(ParallelScope.Self)]
    public class IgnoreHttpsErrorsTests : BrowserTestEx
    {
        [PlaywrightTest("ignorehttpserrors.spec.ts", "should work")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldWork()
        {
            await using var context = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            var requestTask = Server.WaitForRequest(
                "/empty.html",
                request => request.HttpContext.Features.Get<ITlsHandshakeFeature>().Protocol);
            var responseTask = page.GotoAsync(HttpsServer.Prefix + "/empty.html");

            await TaskUtils.WhenAll(
                requestTask,
                responseTask);

            var response = responseTask.Result;
            Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
        }

        [PlaywrightTest("ignorehttpserrors.spec.ts", "should isolate contexts")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
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
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldWorkWithMixedContent()
        {
            Server.SetRoute("/mixedcontent.html", async (context) =>
            {
                await context.Response.WriteAsync($"<iframe src='{Server.EmptyPage}'></iframe>");
            });
            await using var context = await Browser.NewContextAsync(new() { IgnoreHTTPSErrors = true });
            var page = await context.NewPageAsync();
            await page.GotoAsync(HttpsServer.Prefix + "/mixedcontent.html", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
            Assert.AreEqual(2, page.Frames.Count);
            Assert.AreEqual(3, await page.MainFrame.EvaluateAsync<int>("1 + 2"));
            Assert.AreEqual(5, await page.FirstChildFrame().EvaluateAsync<int>("2 + 3"));
        }

        [PlaywrightTest("ignorehttpserrors.spec.ts", "should work with WebSocket")]
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
        public async Task ShouldWorkWithWebSocket()
        {
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
        // [Test, Timeout(TestConstants.DefaultTestTimeout)]
        [Test, Ignore("Fix me #1058")]
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
}
