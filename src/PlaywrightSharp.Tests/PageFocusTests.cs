using System.Threading.Tasks;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Test.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageFocusTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageFocusTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-focus.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
            Assert.Equal("BODY", await Page.EvaluateAsync<string>("() => document.activeElement.nodeName"));
            await Page.FocusAsync("#d1");
            Assert.Equal("d1", await Page.EvaluateAsync<string>("() => document.activeElement.id"));
        }

        [PlaywrightTest("page-focus.spec.ts", "should emit focus event")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
        [SkipBrowserAndPlatformFact(skipWebkit: true, skipWindows: true, skipOSX: true)]
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

            Assert.Equal("First", await Page.EvalOnSelectorAsync<string>("#i1", "e => e.value"));
            Assert.Equal("Last", await Page.EvalOnSelectorAsync<string>("#i2", "e => e.value"));
        }
    }
}
