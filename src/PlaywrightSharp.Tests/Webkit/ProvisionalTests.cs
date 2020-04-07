using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Webkit
{
    ///<playwright-file>webkit/provisional.spec.js</playwright-file>
    ///<playwright-describe>provisional page</playwright-describe>
    public class ProvisionalTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ProvisionalTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>webkit/provisional.spec.js</playwright-file>
        ///<playwright-describe>provisional page</playwright-describe>
        ///<playwright-it>extraHttpHeaders should be pushed to provisional page</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipChromium: true)]
        public async Task ExtraHttpHeadersShouldBePushedToProvisionalPage()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            string pagePath = "/one-style.html";
            Server.Subscribe(pagePath, context => Page.SetExtraHttpHeadersAsync(new Dictionary<string, string>
            {
                ["foo"] = "bar"
            }));

            var (htmlReq, cssReq, _) = await TaskUtils.WhenAll(
                Server.WaitForRequest(pagePath, r => r),
                Server.WaitForRequest("/one-style.css", r => r),
                Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + pagePath));

            Assert.True(string.IsNullOrEmpty(htmlReq.Headers["foo"]));
            Assert.Equal("bar", cssReq.Headers["foo"]);
        }

        ///<playwright-file>webkit/provisional.spec.js</playwright-file>
        ///<playwright-describe>provisional page</playwright-describe>
        ///<playwright-it>should continue load when interception gets disabled during provisional load</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipChromium: true)]
        public async Task ShouldContinueLoadWhenInterceptionGetsDisabledDuringProvisionalLoad()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetRequestInterceptionAsync(true);

            Assert.True(await Page.EvaluateAsync<bool>("() => navigator.onLine"));

            var interceptedTcs = new TaskCompletionSource<bool>();
            var order = new List<string>();

            Page.Request += async (sender, e) =>
            {
                await Page.SetRequestInterceptionAsync(false);
                order.Add("setRequestInterception");
                interceptedTcs.TrySetResult(true);
            };

            var response = await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/empty.html");
            order.Add("goto");

            await interceptedTcs.Task;

            // Should continue on disabling and load successfully.
            Assert.Equal(HttpStatusCode.OK, response.Status);
            // Should resolve setRequestInterception before goto.
            Assert.Equal("setRequestInterception", order[0]);
            Assert.Equal("goto", order[1]);
        }
    }
}
