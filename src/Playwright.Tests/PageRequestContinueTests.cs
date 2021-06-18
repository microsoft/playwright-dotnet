using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageRequestContinueTests : PageTestEx
    {
        [PlaywrightTest("page-request-continue.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.RouteAsync("**/*", route => route.ContinueAsync());
            await Page.GotoAsync(Server.EmptyPage);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend HTTP headers")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendHTTPHeaders()
        {
            await Page.RouteAsync("**/*", route =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["FOO"] = "bar" };
                route.ContinueAsync(new() { Headers = headers });
            });
            await Page.GotoAsync(Server.EmptyPage);
            var requestTask = Server.WaitForRequest("/sleep.zzz", request => request.Headers["foo"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.EvaluateAsync("() => fetch('/sleep.zzz')")
            );
            Assert.AreEqual("bar", requestTask.Result);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend method on main request")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendMethodOnMainRequest()
        {
            var methodTask = Server.WaitForRequest("/empty.html", r => r.Method);
            await Page.RouteAsync("**/*", route => route.ContinueAsync(new() { Method = HttpMethod.Post.Method }));
            await Page.GotoAsync(Server.EmptyPage);
            Assert.AreEqual("POST", await methodTask);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend post data")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendPostData()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.RouteAsync("**/*", route =>
            {
                route.ContinueAsync(new() { PostData = Encoding.UTF8.GetBytes("doggo") });
            });
            var requestTask = Server.WaitForRequest("/sleep.zzz", request =>
            {
                using StreamReader reader = new StreamReader(request.Body);
                return reader.ReadToEndAsync().GetAwaiter().GetResult();
            });

            await TaskUtils.WhenAll(
                requestTask,
                Page.EvaluateAsync("() => fetch('/sleep.zzz', { method: 'POST', body: 'birdy' })")
            );
            Assert.AreEqual("doggo", requestTask.Result);
        }
    }
}
