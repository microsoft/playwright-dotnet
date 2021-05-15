using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForResponseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForResponseTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var task = Page.WaitForResponseAsync(TestConstants.ServerUrl + "/digits/2.png");
            var (response, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEventAsync(PageEvent.Response, _ => false, 1));
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should respect default timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultTimeout()
        {
            Page.SetDefaultTimeout(1);
            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => Page.WaitForEventAsync(PageEvent.Response, _ => false));
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should work with predicate")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPredicate()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var task = Page.WaitForEventAsync(PageEvent.Response, e => e.Url == TestConstants.ServerUrl + "/digits/2.png");
            var (responseEvent, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", responseEvent.Url);
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should work with no timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var task = Page.WaitForResponseAsync(TestConstants.ServerUrl + "/digits/2.png", 0);
            var (response, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
            );
            Assert.Equal(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }
    }
}
