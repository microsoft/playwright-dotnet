using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageCloseTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageCloseTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-close.spec.ts", "should reject all promises when page is closed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRejectAllPromisesWhenPageIsClosed()
        {
            var newPage = await Context.NewPageAsync();
            var exception = await Assert.ThrowsAsync<TargetClosedException>(() => TaskUtils.WhenAll(
                newPage.EvaluateAsync<string>("() => new Promise(r => { })"),
                newPage.CloseAsync()
            ));
            Assert.Contains("Protocol error", Assert.IsType<TargetClosedException>(exception).Message);
        }

        [PlaywrightTest("page-close.spec.ts", "should not be visible in context.pages")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotBeVisibleInContextPages()
        {
            var newPage = await Context.NewPageAsync();
            Assert.Contains(newPage, Context.Pages);
            await newPage.CloseAsync();
            Assert.DoesNotContain(newPage, Context.Pages);
        }

        [PlaywrightTest("page-close.spec.ts", "should run beforeunload if asked for")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRunBeforeunloadIfAskedFor()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            var pageClosingTask = newPage.CloseAsync(true);
            var dialog = await newPage.WaitForEventAsync(PageEvent.Dialog).ContinueWith(task => task.Result.Dialog);
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

        [PlaywrightTest("page-close.spec.ts", "should *not* run beforeunload by default")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRunBeforeunloadByDefault()
        {
            var newPage = await Context.NewPageAsync();
            await newPage.GoToAsync(TestConstants.ServerUrl + "/beforeunload.html");
            // We have to interact with a page so that 'beforeunload' handlers
            // fire.
            await newPage.ClickAsync("body");
            await newPage.CloseAsync();
        }

        [PlaywrightTest("page-close.spec.ts", "should set the page close state")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSetThePageCloseState()
        {
            var newPage = await Context.NewPageAsync();
            Assert.False(newPage.IsClosed);
            await newPage.CloseAsync();
            Assert.True(newPage.IsClosed);
        }

        [PlaywrightTest("page-close.spec.ts", "should terminate network waiters")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-close.spec.ts", "should be callable twice")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
