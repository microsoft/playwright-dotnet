using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageDialogTests : PageTestEx
    {
        [PlaywrightTest("page-dialog.spec.ts", "should fire")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public Task ShouldFire()
        {
            Page.Dialog += async (_, e) =>
            {
                Assert.AreEqual(DialogType.Alert, e.Type);
                Assert.AreEqual(string.Empty, e.DefaultValue);
                Assert.AreEqual("yo", e.Message);

                await e.AcceptAsync();
            };

            return Page.EvaluateAsync("alert('yo');");
        }

        [PlaywrightTest("page-dialog.spec.ts", "should allow accepting prompts")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAllowAcceptingPrompts()
        {
            Page.Dialog += async (_, e) =>
            {
                Assert.AreEqual(DialogType.Prompt, e.Type);
                Assert.AreEqual("yes.", e.DefaultValue);
                Assert.AreEqual("question?", e.Message);

                await e.AcceptAsync("answer!");
            };

            string result = await Page.EvaluateAsync<string>("prompt('question?', 'yes.')");
            Assert.AreEqual("answer!", result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should dismiss the prompt")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDismissThePrompt()
        {
            Page.Dialog += async (_, e) =>
            {
                await e.DismissAsync();
            };

            string result = await Page.EvaluateAsync<string>("prompt('question?')");
            Assert.Null(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should accept the confirm prompt")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptTheConfirmPrompts()
        {
            Page.Dialog += async (_, e) =>
            {
                await e.AcceptAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("confirm('boolean?')");
            Assert.True(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should dismiss the confirm prompt")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDismissTheConfirmPrompt()
        {
            Page.Dialog += async (_, e) =>
            {
                await e.DismissAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
            Assert.False(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should log prompt actions")]
        [Test, Ignore("FAIL CHANNEL")]
        public async Task ShouldLogPromptActions()
        {
            Page.Dialog += async (_, e) =>
            {
                await e.DismissAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
            Assert.False(result);
        }

        [PlaywrightTest("page-dialog.spec.ts", "should be able to close context with open alert")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldBeAbleToCloseContextWithOpenAlert()
        {
            var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();

            var alertEvent = new TaskCompletionSource<IDialog>();
            page.Dialog += (_, dialog) => alertEvent.TrySetResult(dialog);

            await page.EvaluateAsync("() => setTimeout(() => alert('hello'), 0)");
            await alertEvent.Task;
            await context.CloseAsync();
        }

        [PlaywrightTest("page-dialog.spec.ts", "should auto-dismiss the prompt without listeners")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDismissThePrompt()
        {
            string result = await Page.EvaluateAsync<string>("prompt('question?')");
            Assert.Null(result);
        }
    }
}
