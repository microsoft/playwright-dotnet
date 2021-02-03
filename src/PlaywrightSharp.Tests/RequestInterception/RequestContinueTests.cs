using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.RequestInterception
{
    ///<playwright-file>interception.spec.js</playwright-file>
    ///<playwright-describe>Request.continue</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class RequestContinueTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public RequestContinueTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("interception.spec.js", "Request.continue", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        [PlaywrightTest("interception.spec.js", "Request.continue", "should amend HTTP headers")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendHTTPHeaders()
        {
            await Page.RouteAsync("**/*", (route, request) =>
            {
                var headers = new Dictionary<string, string>(request.Headers) { ["FOO"] = "bar" };
                route.ContinueAsync(headers: headers);
            });
            await Page.GoToAsync(TestConstants.EmptyPage);
            var requestTask = Server.WaitForRequest("/sleep.zzz", request => request.Headers["foo"]);
            await TaskUtils.WhenAll(
                requestTask,
                Page.EvaluateAsync("() => fetch('/sleep.zzz')")
            );
            Assert.Equal("bar", requestTask.Result);
        }

        [PlaywrightTest("interception.spec.js", "Request.continue", "should amend method on main request")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendMethodOnMainRequest()
        {
            var methodTask = Server.WaitForRequest("/empty.html", r => r.Method);
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync(HttpMethod.Post));
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("POST", await methodTask);
        }

        [PlaywrightTest("interception.spec.js", "Request.continue", "should amend post data")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendPostData()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.RouteAsync("**/*", (route, request) =>
            {
                route.ContinueAsync(postData: "doggo");
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
