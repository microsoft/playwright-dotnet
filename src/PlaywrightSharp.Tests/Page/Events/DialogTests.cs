using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>dialog.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.Dialog</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class DialogTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public DialogTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should fire</playwright-it>
        [Retry]
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

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should allow accepting prompts</playwright-it>
        [Retry]
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

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should dismiss the prompt</playwright-it>
        [Retry]
        public async Task ShouldDismissThePrompt()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.DismissAsync();
            };

            string result = await Page.EvaluateAsync<string>("prompt('question?')");
            Assert.Null(result);
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should accept the confirm prompt</playwright-it>
        [Retry]
        public async Task ShouldAcceptTheConfirmPrompts()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.AcceptAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("confirm('boolean?')");
            Assert.True(result);
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should dismiss the confirm prompt</playwright-it>
        [Retry]
        public async Task ShouldDismissTheConfirmPrompt()
        {
            Page.Dialog += async (sender, e) =>
            {
                await e.Dialog.DismissAsync();
            };

            bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
            Assert.False(result);
        }

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should log prompt actions</playwright-it>
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

        ///<playwright-file>dialog.spec.js</playwright-file>
        ///<playwright-describe>Page.Events.Dialog</playwright-describe>
        ///<playwright-it>should be able to close context with open alert</playwright-it>
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldBeAbleToCloseContextWithOpenAlert()
        {
            var context = await Browser.NewContextAsync();
            var page = await context.NewPageAsync();
            var alertTask = page.WaitForEvent<DialogEventArgs>(PageEvent.Dialog);

            await page.EvaluateAsync("() => setTimeout(() => alert('hello'), 0)");
            await alertTask;
            await context.CloseAsync();
        }
    }
}
