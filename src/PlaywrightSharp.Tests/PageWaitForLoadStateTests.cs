using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
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

        ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
        ///<playwright-it>should pick up ongoing navigation</playwright-it>
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

        ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
        ///<playwright-it>should respect timeout</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRespectTimeout()
        {
            Server.SetRoute("/one-style.css", context => Task.Delay(Timeout.Infinite));
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html", LifecycleEvent.DOMContentLoaded);
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.WaitForLoadStateAsync(LifecycleEvent.Load, 1));
            Assert.Contains("Timeout 1ms exceeded", exception.Message);
        }

        ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
        ///<playwright-it>should resolve immediately if loaded</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldResolveImmediatelyIfLoaded()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await Page.WaitForLoadStateAsync();
        }

        ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
        ///<playwright-it>should throw for bad state</playwright-it>
        [Fact(Skip = "We don't need this test")]
        public void ShouldTthrowForBadState()
        {
        }

        ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
        ///<playwright-it>should resolve immediately if load state matches</playwright-it>
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
            await Page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            responseTask.TrySetResult(true);
            await navigationTask;
        }

        ///<playwright-file>page-wait-for-load-state.ts</playwright-file>
        ///<playwright-it>should work with pages that have loaded before being connected to</playwright-it>
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
            Assert.Equal(2, pages.Length);

            // order is not guaranteed
            var mainPage = pages.FirstOrDefault(p => ReferenceEquals(Page, p));
            var connectedPage = pages.FirstOrDefault(p => !ReferenceEquals(Page, p));

            Assert.NotNull(mainPage);
            Assert.Equal(TestConstants.EmptyPage, mainPage.Url);

            Assert.Equal(TestConstants.EmptyPage, connectedPage.Url);
            await connectedPage.WaitForLoadStateAsync();
            Assert.Equal(TestConstants.EmptyPage, connectedPage.Url);
        }
    }
}
