using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForRequest</playwright-describe>
    public class PageWaitForRequestTests : PlaywrightSharpPageBaseTest
    {
        internal PageWaitForRequestTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForRequest</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (request, _) = await TaskUtils.WhenAll(
                Page.WaitForRequestAsync(TestConstants.ServerUrl + "/digits/2.png"),
                Page.EvaluateAsync(@"() => {
                  fetch('/digits/1.png');
                  fetch('/digits/2.png');
                  fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", request.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForRequest</playwright-describe>
        ///<playwright-it>should work with predicate</playwright-it>
        [Fact]
        public async Task ShouldWorkWithPredicate()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (requestEvent, _) = await TaskUtils.WhenAll(
                Page.WaitForEvent(PageEvent.Request, new WaitForEventOptions<RequestEventArgs>
                {
                    Predicate = e => e.Request.Url == TestConstants.ServerUrl + "/digits/2.png"
                }),
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", requestEvent.Request.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForRequest</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEvent(PageEvent.Request, new WaitForEventOptions<RequestEventArgs>
                {
                    Predicate = _ => false,
                    Timeout = 1
                }));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForRequest</playwright-describe>
        ///<playwright-it>should respect default timeout</playwright-it>
        [Fact]
        public async Task ShouldRespectDefaultTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEvent(PageEvent.Request, new WaitForEventOptions<RequestEventArgs>
                {
                    Predicate = _ => false
                }));
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForRequest</playwright-describe>
        ///<playwright-it>should work with no timeout</playwright-it>
        [Fact]
        public async Task ShouldWorkWithNoTimeout()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (request, _) = await TaskUtils.WhenAll(
                Page.WaitForRequestAsync(TestConstants.ServerUrl + "/digits/2.png", new WaitForOptions { Timeout = 0 }),
                Page.EvaluateAsync<string>(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", request.Url);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForRequest</playwright-describe>
        ///<playwright-it>should work with url match</playwright-it>
        [Fact]
        public async Task ShouldWorkWithUrlMatch()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var (request, _) = await TaskUtils.WhenAll(
                Page.WaitForRequestAsync(new Regex("digits\\d\\.png")),
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/1.png", request.Url);
        }
    }
}
