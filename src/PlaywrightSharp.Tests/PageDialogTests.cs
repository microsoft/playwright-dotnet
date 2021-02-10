using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageDialogTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageDialogTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-dialog.spec.ts", "should fire")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFire()
        {
            Page.Dialog += async (sender, e) =>
            {
                Assert.Equal(DialogType.Alert, e.Dialog.Type);
                Assert.Equal(string.Empty, e.Dialog.DefaultValue);
                Assert.Equal("yo", e.Dialog.Message);

                await e.Dialog.AcceptAsync();
            };

            await Page.EvaluateAsync("alert('yo');");
        }

        [PlaywrightTest("page-dialog.spec.ts", "should allow accepting prompts")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowAcceptingPrompts()
        {
            Page.Dialog += async (sender, e) =>
            {
                Assert.Equal(DialogType.Prompt, e.Dialog.Type);
                Assert.Equal("yes.", e.Dialog.DefaultValue);
                Assert.Equal("question?", e.Dialog.Message);

                await e.Dialog.AcceptAsync("answer!");
            };

            string result = await Page.EvaluateAsync<string>("prompt('question?', 'yes.')");
            Assert.Equal("answer!", result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should dismiss the prompt")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDismissThePrompt()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.DismissAsync();
            };

            string result = await Page.EvaluateAsync<string>("prompt('question?')");
            Assert.Null(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should accept the confirm prompt")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptTheConfirmPrompts()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.AcceptAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("confirm('boolean?')");
            Assert.True(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should dismiss the confirm prompt")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDismissTheConfirmPrompt()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.DismissAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
            Assert.False(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should log prompt actions")]
        [Fact(Skip = "FAIL CHANNEL")]
        public async Task ShouldLogPromptActions()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.DismissAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
            Assert.False(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should be able to close context with open alert")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldBeAbleToCloseContextWithOpenAlert()
        {
            var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var alertTask = page.WaitForEventAsync(PageEvent.Dialog);

            await page.EvaluateAsync("() => setTimeout(() => alert('hello'), 0)");
            await alertTask;
            await context.CloseAsync();
        }
    }
}
