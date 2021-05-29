using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageFocusTests : PageTestEx
    {
        [PlaywrightTest("page-focus.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
            Assert.AreEqual("BODY", await Page.EvaluateAsync<string>("() => document.activeElement.nodeName"));
            await Page.FocusAsync("#d1");
            Assert.AreEqual("d1", await Page.EvaluateAsync<string>("() => document.activeElement.id"));
        }

        [PlaywrightTest("page-focus.spec.ts", "should emit focus event")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitFocusEvent()
        {
            await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
            bool focused = false;
            await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
            await Page.EvaluateAsync("() => d1.addEventListener('focus', focusEvent)");
            await Page.FocusAsync("#d1");
            Assert.True(focused);
        }

        [PlaywrightTest("page-focus.spec.ts", "should emit blur event")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldEmitBlurEvent()
        {
            await Page.SetContentAsync("<div id=d1 tabIndex=0>DIV1</div><div id=d2 tabIndex=0>DIV2</div>");
            await Page.FocusAsync("#d1");
            bool focused = false;
            bool blurred = false;
            await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
            await Page.ExposeFunctionAsync("blurEvent", () => blurred = true);
            await Page.EvaluateAsync("() => d1.addEventListener('blur', blurEvent)");
            await Page.EvaluateAsync("() => d2.addEventListener('focus', focusEvent)");
            await Page.FocusAsync("#d2");
            Assert.True(focused);
            Assert.True(blurred);
        }

        [PlaywrightTest("page-focus.spec.ts", "should traverse focus")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true, skipWindows: true, skipOSX: true)]
        public async Task ShouldTraverseFocus()
        {
            await Page.SetContentAsync("<input id=\"i1\"><input id=\"i2\">");
            bool focused = false;
            await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
            await Page.EvaluateAsync("() => i2.addEventListener('focus', focusEvent)");

            await Page.FocusAsync("#i1");
            await Page.Keyboard.TypeAsync("First");
            await Page.Keyboard.PressAsync("Tab");
            await Page.Keyboard.TypeAsync("Last");

            Assert.True(focused);

            Assert.AreEqual("First", await Page.EvalOnSelectorAsync<string>("#i1", "e => e.value"));
            Assert.AreEqual("Last", await Page.EvalOnSelectorAsync<string>("#i2", "e => e.value"));
        }
    }
}
