using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForRequestTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForRequestTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var task = Page.WaitForRequestAsync(TestConstants.ServerUrl + "/digits/2.png");
            var (request, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync(@"() => {
                  fetch('/digits/1.png');
                  fetch('/digits/2.png');
                  fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", request.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with predicate")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPredicate()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var task = Page.WaitForEventAsync(PageEvent.Request, e => e.Request.Url == TestConstants.ServerUrl + "/digits/2.png");
            var (requestEvent, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", requestEvent.Request.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEventAsync<RequestEventArgs>(PageEvent.Request, _ => false, 1));
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should respect default timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultTimeout()
        {
            Page.DefaultTimeout = 1;
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEventAsync<RequestEventArgs>(PageEvent.Request, _ => false));
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with no timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var task = Page.WaitForRequestAsync(TestConstants.ServerUrl + "/digits/2.png", 0);
            var (request, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", request.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with url match")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUrlMatch()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            var task = Page.WaitForRequestAsync(new Regex(@"/digits/\d.png"));
            var (request, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/1.png", request.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with url match regular expression from a different context")]
        [Fact(Skip = "We dont't need to test this")]
        public void ShouldWorkWithUrlMatchRegularExpressionFromADifferentContext()
        {
        }
    }
}
