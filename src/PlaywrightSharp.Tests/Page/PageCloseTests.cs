using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.close</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageCloseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageCloseTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should reject all promises when page is closed</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRejectAllPromisesWhenPageIsClosed()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => TaskUtils.WhenAll(
                newPage.EvaluateAsync<string>("() => new Promise(r => { })"),
                newPage.CloseAsync()
            ));
            Assert.Contains("Protocol error", Assert.IsType<TargetClosedException>(exception).Message);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should not be visible in context.pages</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldNotBeVisibleInContextPages()
        {
            var newPage = await Context.NewPageAsync();
            Assert.Contains(newPage, Context.Pages);
            await newPage.CloseAsync();
            Assert.DoesNotContain(newPage, Context.Pages);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should run beforeunload if asked for</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldRunBeforeunloadIfAskedFor()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            var pageClosingTask = newPage.CloseAsync(true);
            var dialog = await newPage.WaitForEvent<DialogEventArgs>(PageEvent.Dialog).ContinueWith(task => task.Result.Dialog);
            Assert.Equal(DialogType.BeforeUnload, dialog.Type);
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
            await pageClosingTask;
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should *not* run beforeunload by default</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldTerminateNetworkWaiters()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => TaskUtils.WhenAll(
                newPage.WaitForRequestAsync(TestConstants.EmptyPage),
                newPage.WaitForResponseAsync(TestConstants.EmptyPage),
                newPage.CloseAsync()
            ));
            for (int i = 0; i < 2; i++)
            {
                string message = exception.Message;
                Assert.Contains("Page closed", message);
                Assert.DoesNotContain("Timeout", message);
            }
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.close</playwright-describe>
        ///<playwright-it>should be callable twice</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldBeCallableTwice()
        {
            var newPage = await Context.NewPageAsync();
            await TaskUtils.WhenAll(
                newPage.CloseAsync(),
                newPage.CloseAsync());

            await newPage.CloseAsync();
        }
    }
}
