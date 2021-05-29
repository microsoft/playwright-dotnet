using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageWaitForResponseTests : PageTestEx
    {
        [PlaywrightTest("page-wait-for-response.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
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
            Assert.AreEqual(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            var exception = await AssertThrowsAsync<TimeoutException>(
                () => Page.WaitForResponseAsync(_ => false, new PageWaitForResponseOptions
                {
                    Timeout = 1,
                }));
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should respect default timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultTimeout()
        {
            Page.SetDefaultTimeout(1);
            var exception = await AssertThrowsAsync<TimeoutException>(
                () => Page.WaitForResponseAsync(_ => false));
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should work with predicate")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPredicate()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var task = Page.WaitForResponseAsync(e => e.Url == TestConstants.ServerUrl + "/digits/2.png");
            var (responseEvent, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.AreEqual(TestConstants.ServerUrl + "/digits/2.png", responseEvent.Url);
        }

        [PlaywrightTest("page-wait-for-response.spec.ts", "should work with no timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);
            var task = Page.WaitForResponseAsync(TestConstants.ServerUrl + "/digits/2.png", new PageWaitForResponseOptions { Timeout = 0 });
            var (response, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
            );
            Assert.AreEqual(TestConstants.ServerUrl + "/digits/2.png", response.Url);
        }
    }
}
