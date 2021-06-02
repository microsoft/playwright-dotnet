using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageWaitForRequestTests : PageTestEx
    {
        [PlaywrightTest("page-wait-for-request.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var task = Page.WaitForRequestAsync(Server.Prefix + "/digits/2.png");
            var (request, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync(@"() => {
                  fetch('/digits/1.png');
                  fetch('/digits/2.png');
                  fetch('/digits/3.png');
                }")
            );
            Assert.AreEqual(Server.Prefix + "/digits/2.png", request.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with predicate")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithPredicate()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var task = Page.WaitForRequestAsync(e => e.Url == Server.Prefix + "/digits/2.png");
            var (requestEvent, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }")
            );
            Assert.AreEqual(Server.Prefix + "/digits/2.png", requestEvent.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should respect timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            await AssertThrowsAsync<TimeoutException>(
                () => Page.WaitForRequestAsync(_ => false, new PageWaitForRequestOptions { Timeout = 1 }));
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should respect default timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectDefaultTimeout()
        {
            Page.SetDefaultTimeout(1);
            await AssertThrowsAsync<TimeoutException>(
                () => Page.WaitForRequestAsync(_ => false));
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with no timeout")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNoTimeout()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var task = Page.WaitForRequestAsync(Server.Prefix + "/digits/2.png", new PageWaitForRequestOptions { Timeout = 0 });
            var (request, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync(@"() => setTimeout(() => {
                    fetch('/digits/1.png');
                    fetch('/digits/2.png');
                    fetch('/digits/3.png');
                }, 50)")
            );
            Assert.AreEqual(Server.Prefix + "/digits/2.png", request.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with url match")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithUrlMatch()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var task = Page.WaitForRequestAsync(new Regex(@"/digits/\d.png"));
            var (request, _) = await TaskUtils.WhenAll(
                task,
                Page.EvaluateAsync<string>(@"() => {
                    fetch('/digits/1.png');
                }")
            );
            Assert.AreEqual(Server.Prefix + "/digits/1.png", request.Url);
        }

        [PlaywrightTest("page-wait-for-request.spec.ts", "should work with url match regular expression from a different context")]
        [Test, Ignore("We dont't need to test this")]
        public void ShouldWorkWithUrlMatchRegularExpressionFromADifferentContext()
        {
        }
    }
}
