using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net;
using System.Threading.Tasks;
using System.Web;
using Microsoft.AspNetCore.Http;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>navigation.spec.js</playwright-file>
    ///<playwright-describe>Page.waitForLoadState</playwright-describe>
    public class WaitForLoadStateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForLoadStateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should pick up ongoing navigation</playwright-it>
        [Fact]
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
        [Fact]
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
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.WaitForLoadStateAsync(new NavigationOptions { Timeout = 1 }));
            Assert.Contains("Timeout of 1 ms exceeded", exception.Message);
            responseTask.TrySetResult(true);
            await navigationTask;
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should resolve immediately if loaded</playwright-it>
        [Fact]
        public async Task ShouldResolveImmediatelyIfLoaded()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/one-style.html");
            await Page.WaitForLoadStateAsync();
        }

        ///<playwright-file>navigation.spec.js</playwright-file>
        ///<playwright-describe>Page.waitForLoadState</playwright-describe>
        ///<playwright-it>should resolve immediately if load state matches</playwright-it>
        [Fact]
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
        public async Task ShouldWorkWithPagesThatHaveLoadedBeforeBeingConnectedTso()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.EvaluateAsync(@"async () => {
                const child = window.open(document.location.href);
                while(child.document.readyState !== 'complete' || child.document.location.href === 'about:blank')
                  await new Promise(setTimeout);
            }");
            var childPage = (await Context.GetPagesAsync())[1];
            await childPage.WaitForLoadStateAsync();
        }
    }
}
