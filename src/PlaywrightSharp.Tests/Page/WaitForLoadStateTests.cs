using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForLoadState</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class WaitForLoadStateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForLoadStateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should pick up ongoing navigation</playwright-it>
        [Retry]
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

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should respect timeout</playwright-it>
        [Retry]
        public async Task ShouldRespectTimeout()
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
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => Page.WaitForLoadStateAsync(new NavigationOptions { Timeout = 1 }));
            Assert.Contains("Timeout of 1 ms exceeded", exception.Message);
            responseTask.TrySetResult(true);
            await navigationTask;
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should resolve immediately if loaded</playwright-it>
        [Retry]
        public async Task ShouldResolveImmediatelyIfLoaded()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await Page.WaitForLoadStateAsync();
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should resolve immediately if load state matches</playwright-it>
        [Retry]
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
            await Page.WaitForLoadStateAsync(new NavigationOptions { WaitUntil = new[] { WaitUntilNavigation.DOMContentLoaded } });
            responseTask.TrySetResult(true);
            await navigationTask;
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
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
            var pages = await Context.GetPagesAsync();
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
