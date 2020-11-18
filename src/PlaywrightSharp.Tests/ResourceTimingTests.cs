using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>resource-timing.spec.js</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ResourceTimingTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ResourceTimingTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>resource-timing.spec.js</playwright-file>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            var (request, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.RequestFinished),
                Page.GoToAsync(TestConstants.EmptyPage));

            var timing = request.Request.Timing;
            Assert.True(timing.DomainLookupStart >= 0);
            Assert.True(timing.DomainLookupEnd >= timing.DomainLookupStart);
            Assert.True(timing.ConnectStart >= timing.DomainLookupEnd);
            Assert.Equal(-1, timing.SecureConnectionStart);
            Assert.True(timing.ConnectEnd > timing.SecureConnectionStart);
            Assert.True(timing.RequestStart >= timing.ConnectEnd);
            Assert.True(timing.ResponseStart > timing.RequestStart);
            Assert.True(timing.ResponseEnd >= timing.ResponseStart);
            Assert.True(timing.ResponseEnd < 10000);
        }

        ///<playwright-file>resource-timing.spec.js</playwright-file>
        ///<playwright-it>should work for subresource</playwright-it>
        [Fact]
        public async Task ShouldWorkForSubresource()
        {
            var requests = new List<IRequest>();

            Page.RequestFinished += (sender, e) => requests.Add(e.Request);
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");

            Assert.Equal(2, requests.Count);

            var timing = requests[1].Timing;
            if (TestConstants.IsWebKit && TestConstants.IsWindows)
            {
                Assert.True(timing.DomainLookupStart >= 0);
                Assert.True(timing.DomainLookupEnd >= timing.DomainLookupStart);
                Assert.True(timing.ConnectStart >= timing.DomainLookupEnd);
                Assert.Equal(-1, timing.SecureConnectionStart);
                Assert.True(timing.ConnectEnd > timing.SecureConnectionStart);
            }
            else
            {
                Assert.True(timing.DomainLookupStart == 0 || timing.DomainLookupStart == -1);
                Assert.Equal(-1, timing.DomainLookupEnd);
                Assert.Equal(-1, timing.ConnectStart);
                Assert.Equal(-1, timing.SecureConnectionStart);
                Assert.Equal(-1, timing.ConnectEnd);
            }

            Assert.True(timing.RequestStart >= 0);
            Assert.True(timing.ResponseStart >= timing.RequestStart);
            Assert.True(timing.ResponseEnd >= timing.ResponseStart);
            Assert.True(timing.ResponseEnd < 10000);
        }

        ///<playwright-file>resource-timing.spec.js</playwright-file>
        ///<playwright-it>should work for SSL</playwright-it>
        [Fact]
        public async Task ShouldWorkForSSL()
        {
            var page = await Browser.NewPageAsync(ignoreHTTPSErrors: true);
            var (request, _) = await TaskUtils.WhenAll(
                page.WaitForEventAsync(PageEvent.RequestFinished),
                page.GoToAsync(TestConstants.HttpsPrefix + "/empty.html"));

            var timing = request.Request.Timing;
            if (!(TestConstants.IsWebKit && TestConstants.IsMacOSX))
            {
                Assert.True(timing.DomainLookupStart >= 0);
                Assert.True(timing.DomainLookupEnd >= timing.DomainLookupStart);
                Assert.True(timing.ConnectStart >= timing.DomainLookupEnd);
                Assert.True(timing.SecureConnectionStart >= timing.ConnectStart);
                Assert.True(timing.ConnectEnd > timing.SecureConnectionStart);
                Assert.True(timing.RequestStart >= timing.ConnectEnd);
            }

            Assert.True(timing.ResponseStart > timing.RequestStart);
            Assert.True(timing.ResponseEnd >= timing.ResponseStart);
            Assert.True(timing.ResponseEnd < 10000);
            await page.CloseAsync();
        }

        ///<playwright-file>resource-timing.spec.js</playwright-file>
        ///<playwright-it>should work for redirect</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldWorkForRedirect()
        {
            Server.SetRedirect("/foo.html", "/empty.html");
            var responses = new List<IResponse>();

            Page.Response += (sender, e) => responses.Add(e.Response);
            await Page.GoToAsync(TestConstants.ServerUrl + "/foo.html");

            // This is different on purpose, promises work different in TS.
            await responses[1].FinishedAsync();

            Assert.Equal(2, responses.Count);
            Assert.Equal(TestConstants.ServerUrl + "/foo.html", responses[0].Url);
            Assert.Equal(TestConstants.ServerUrl + "/empty.html", responses[1].Url);

            var timing1 = responses[0].Request.Timing;
            Assert.True(timing1.DomainLookupStart >= 0);
            Assert.True(timing1.DomainLookupEnd >= timing1.DomainLookupStart);
            Assert.True(timing1.ConnectStart >= timing1.DomainLookupEnd);
            Assert.Equal(-1, timing1.SecureConnectionStart);
            Assert.True(timing1.ConnectEnd > timing1.SecureConnectionStart);
            Assert.True(timing1.RequestStart >= timing1.ConnectEnd);
            Assert.True(timing1.ResponseStart > timing1.RequestStart);
            Assert.True(timing1.ResponseEnd >= timing1.ResponseStart);
            Assert.True(timing1.ResponseEnd < 10000);

            var timing2 = responses[1].Request.Timing;
            Assert.Equal(-1, timing2.DomainLookupStart);
            Assert.Equal(-1, timing2.DomainLookupEnd);
            Assert.Equal(-1, timing2.ConnectStart);
            Assert.Equal(-1, timing2.SecureConnectionStart);
            Assert.Equal(-1, timing2.ConnectEnd);
            Assert.True(timing2.RequestStart >= 0);
            Assert.True(timing2.ResponseStart >= timing2.RequestStart);
            Assert.True(timing2.ResponseEnd >= timing2.ResponseStart);
            Assert.True(timing2.ResponseEnd < 10000);
        }
    }
}
