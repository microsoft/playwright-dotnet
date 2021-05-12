using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageRequestContinueTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageRequestContinueTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync());
            await Page.GotoAsync(TestConstants.EmptyPage);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend HTTP headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendHTTPHeaders()
        {
            await Page.RouteAsync("**/*", (route) =>
            {
                var headers = new Dictionary<string, string>(route.Request.Headers.ToDictionary(x => x.Key, x => x.Value)) { ["FOO"] = "bar" };
                route.ResumeAsync(headers: headers);
            });
            await Page.GotoAsync(TestConstants.EmptyPage);
            var requestTask = Server.WaitForRequest("/sleep.zzz", request => request.Headers["foo"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.EvaluateAsync("() => fetch('/sleep.zzz')")
            );
            Assert.Equal("bar", requestTask.Result);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend method on main request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendMethodOnMainRequest()
        {
            var methodTask = Server.WaitForRequest("/empty.html", r => r.Method);
            await Page.RouteAsync("**/*", (route) => route.ResumeAsync(method: HttpMethod.Post.Method));
            await Page.GotoAsync(TestConstants.EmptyPage);
            Assert.Equal("POST", await methodTask);
        }

        [PlaywrightTest("page-request-continue.spec.ts", "should amend post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendPostData()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/*", (route) =>
            {
                route.ResumeAsync(postData: Encoding.UTF8.GetBytes("doggo"));
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
            Assert.Equal("doggo", requestTask.Result);
        }
    }
}
