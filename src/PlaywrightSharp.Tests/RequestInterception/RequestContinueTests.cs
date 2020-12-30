using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
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

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Request.continue</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync());
            await Page.GoToAsync(TestConstants.EmptyPage);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Request.continue</playwright-describe>
        ///<playwright-it>should amend HTTP headers</playwright-it>
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

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Request.continue</playwright-describe>
        ///<playwright-it>should amend method on main request</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAmendMethodOnMainRequest()
        {
            var methodTask = Server.WaitForRequest("/empty.html", r => r.Method);
            await Page.RouteAsync("**/*", (route, request) => route.ContinueAsync(HttpMethod.Post));
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.Equal("POST", await methodTask);
        }

        ///<playwright-file>interception.spec.js</playwright-file>
        ///<playwright-describe>Request.continue</playwright-describe>
        ///<playwright-it>should amend post data</playwright-it>
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
