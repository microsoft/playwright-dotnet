using System;
using System.Text;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.close</playwright-describe>
    public class PageCloseTests : PlaywrightSharpPageBaseTest
    {
        internal PageCloseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should reject all promises when page is closed</playwright-it>
        [Fact]
        public async Task ShouldRejectAllPromisesWhenPageIsClosed()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await Assert.ThrowsAsync<AggregateException>(() => Task.WhenAll(
                newPage.EvaluateAsync<string>("() => new Promise(r => { })"),
                newPage.CloseAsync()
            ));
            Assert.Contains("Protocol error", Assert.IsType<PlaywrightSharpException>(exception).Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should not be visible in context.pages</playwright-it>
        [Fact]
        public async Task ShouldNotBeVisibleInContextPages()
        {
            var newPage = await Context.NewPageAsync();
            Assert.Contains(newPage, await Context.GetPagesAsync());
            await newPage.CloseAsync();
            Assert.DoesNotContain(newPage, await Context.GetPagesAsync());
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should run beforeunload if asked for</playwright-it>
        [Fact]
        public async Task ShouldRunBeforeunloadIfAskedFor()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            var pageClosingPromise = newPage.CloseAsync(new PageCloseOptions { RunBeforeUnload = true });
            var dialog = await newPage.WaitForEvent<DialogEventArgs>(PageEvent.Dialog).ContinueWith(task => task.Result.Dialog);
            Assert.Equal(DialogType.BeforeUnload, dialog.DialogType);
            Assert.Empty(dialog.DefaultValue);
            if (TestConstants.IsChromium)
            {
                Assert.Empty(dialog.Message);
            }
            else if (TestConstants.IsWebKit)
            {
                Assert.Equal("Leave?", dialog.Message);
            }
            else
            {
                Assert.Equal("This page is asking you to confirm that you want to leave - data you have entered may not be saved.", dialog.Message);
            }

            await dialog.AcceptAsync();
            await pageClosingPromise;
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should *not* run beforeunload by default</playwright-it>
        [Fact]
        public async Task ShouldNotRunBeforeunloadByDefault()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            await newPage.CloseAsync();
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should set the page close state</playwright-it>
        [Fact]
        public async Task ShouldSetThePageCloseState()
        {
            var newPage = await Context.NewPageAsync();
            Assert.False(newPage.IsClosed);
            await newPage.CloseAsync();
            Assert.True(newPage.IsClosed);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should terminate network waiters</playwright-it>
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var newPage = await Context.NewPageAsync();
            var aggregateException = await Assert.ThrowsAsync<AggregateException>(() => Task.WhenAll(
                newPage.WaitForRequestAsync(TestConstants.EmptyPage),
                newPage.WaitForResponseAsync(TestConstants.EmptyPage),
                newPage.CloseAsync()
            ));
            for (int i = 0; i < 2; i++)
            {
                string message = aggregateException.InnerExceptions[i].Message;
                Assert.Contains("Target closed", message);
                Assert.DoesNotContain("Timeout", message);
            }
        }
    }
}
