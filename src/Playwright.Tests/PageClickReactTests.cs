using System;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageClickReactTests : PageTestEx
    {
        [PlaywrightTest("page-click-react.spec.ts", "should retarget when element is recycled during hit testing")]
        [Test, Ignore(" Skip USES_HOOKS")]
        public void ShouldRetargetWhenElementIsRecycledDuringHitTesting()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should report that selector does not match anymore")]
        [Test, Ignore(" Skip USES_HOOKS")]
        public void ShouldReportThatSelectorDoesNotMatchAnymore()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should retarget when element is recycled before enabled check")]
        [Test, Ignore(" Skip USES_HOOKS")]
        public void ShouldRetargetWhenElementIsRecycledBeforeEnabledCheck()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should not retarget the handle when element is recycled")]
        [Test, Ignore(" Skip USES_HOOKS")]
        public void ShouldNotRetargetTheHandleWhenElementIsRecycled()
        {
        }

        [PlaywrightTest("page-click-react.spec.ts", "should timeout when click opens alert")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldTimeoutWhenClickOpensAlert()
        {
            var dialogEvent = new TaskCompletionSource<IDialog>();
            Page.Dialog += (_, dialog) => dialogEvent.TrySetResult(dialog);

            await Page.SetContentAsync("<div onclick='window.alert(123)'>Click me</div>");

            var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.ClickAsync("div", new() { Timeout = 3000 }));
            StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
            var dialog = await dialogEvent.Task;
            await dialog.DismissAsync();
        }

        [PlaywrightTest("page-click-react.spec.ts", "should not retarget when element changes on hover")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRetargetWhenElementChangesOnHover()
        {
            await Page.GotoAsync(Server.Prefix + "/react.html");
            await Page.EvaluateAsync(@"() => {
                renderComponent(e('div', {}, [e(MyButton, { name: 'button1', renameOnHover: true }), e(MyButton, { name: 'button2' })] ));
            }");

            await Page.ClickAsync("text=button1");
            Assert.True(await Page.EvaluateAsync<bool?>("window.button1"));
            Assert.Null(await Page.EvaluateAsync<bool?>("window.button2"));
        }

        [PlaywrightTest("page-click-react.spec.ts", "should not retarget when element is recycled on hover")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotRetargetWhenElementIsRecycledOnHover()
        {
            await Page.GotoAsync(Server.Prefix + "/react.html");
            await Page.EvaluateAsync(@"() => {
                function shuffle() {
                    renderComponent(e('div', {}, [e(MyButton, { name: 'button2' }), e(MyButton, { name: 'button1' })] ));
                }
                renderComponent(e('div', {}, [e(MyButton, { name: 'button1', onHover: shuffle }), e(MyButton, { name: 'button2' })] ));
            }");

            await Page.ClickAsync("text=button1");
            Assert.Null(await Page.EvaluateAsync<bool?>("window.button1"));
            Assert.True(await Page.EvaluateAsync<bool?>("window.button2"));
        }

    }
}
