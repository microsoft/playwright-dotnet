using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForLoadStateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForLoadStateTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should pick up ongoing navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldPickUpOngoingNavigation()
        {
            var responseTask = new TaskCompletionSource<bool>();
            var waitForRequestTask = Server.WaitForRequest("/one-style.css");

            Server.SetRoute("/one-style.css", async (ctx) =>
            {
                if (await responseTask.Task)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync("File not found");
                }
            });

            var navigationTask = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await waitForRequestTask;
            var waitLoadTask = Page.WaitForLoadStateAsync();
            responseTask.TrySetResult(true);
            await waitLoadTask;
            await navigationTask;
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should respect timeout")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            Server.SetRoute("/one-style.css", _ => Task.Delay(Timeout.Infinite));
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html", WaitUntilState.DOMContentLoaded);
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.WaitForLoadStateAsync(LoadState.Load, 1));
            Assert.Contains("Timeout 1ms exceeded", exception.Message);
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should resolve immediately if loaded")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldResolveImmediatelyIfLoaded()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await Page.WaitForLoadStateAsync();
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should throw for bad state")]
        [Fact(Skip = "We don't need this test")]
        public void ShouldTthrowForBadState()
        {
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should resolve immediately if load state matches")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldResolveImmediatelyIfLoadStateMatches()
        {
            var responseTask = new TaskCompletionSource<bool>();
            var waitForRequestTask = Server.WaitForRequest("/one-style.css");

            Server.SetRoute("/one-style.css", async (ctx) =>
            {
                if (await responseTask.Task)
                {
                    ctx.Response.StatusCode = 404;
                    await ctx.Response.WriteAsync("File not found");
                }
            });

            var navigationTask = Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await waitForRequestTask;
            await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            responseTask.TrySetResult(true);
            await navigationTask;
        }

        [PlaywrightTest("page-wait-for-load-state.ts", "should work with pages that have loaded before being connected to")]
        [SkipBrowserAndPlatformFact(skipFirefox: true, skipWebkit: true)]
        public async Task ShouldWorkWithPagesThatHaveLoadedBeforeBeingConnectedTo()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"async () => {
                const child = window.open(document.location.href);
                while (child.document.readyState !== 'complete' || child.document.location.href === 'about:blank')
                  await new Promise(f => setTimeout(f, 100));
            }");
            var pages = Context.Pages;
            Assert.Equal(2, pages.Count);

            // order is not guaranteed
            var mainPage = pages.FirstOrDefault(p => ReferenceEquals(Page, p));
            var connectedPage = pages.Single(p => !ReferenceEquals(Page, p));

            Assert.NotNull(mainPage);
            Assert.Equal(TestConstants.EmptyPage, mainPage.Url);

            Assert.Equal(TestConstants.EmptyPage, connectedPage.Url);
            await connectedPage.WaitForLoadStateAsync();
            Assert.Equal(TestConstants.EmptyPage, connectedPage.Url);
        }

        [PlaywrightTest("page-wait-for-load-state.spec.ts", "should work for frame")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForFrame()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/one-frame.html");
            var frame = Page.Frames.ElementAt(1);

            TaskCompletionSource<bool> requestTask = new TaskCompletionSource<bool>();
            TaskCompletionSource<bool> routeReachedTask = new TaskCompletionSource<bool>();
            await Page.RouteAsync(TestConstants.ServerUrl + "/one-style.css", async (route) =>
            {
                routeReachedTask.TrySetResult(true);
                await requestTask.Task;
                await route.ResumeAsync();
            });

            await frame.GoToAsync(TestConstants.ServerUrl + "/one-style.html", WaitUntilState.DOMContentLoaded);

            await routeReachedTask.Task;
            var loadTask = frame.WaitForLoadStateAsync();
            await Page.EvaluateAsync("1");
            Assert.False(loadTask.IsCompleted);
            requestTask.TrySetResult(true);
            await loadTask;
        }
    }
}
