using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>focus.spec.js</playwright-file>
    ///<playwright-describe>Page.focus</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageFocusTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageFocusTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>focus.spec.js</playwright-file>
        ///<playwright-describe>Page.focus</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
            Assert.Equal("BODY", await Page.EvaluateAsync<string>("() => document.activeElement.nodeName"));
            await Page.FocusAsync("#d1");
            Assert.Equal("d1", await Page.EvaluateAsync<string>("() => document.activeElement.id"));
        }

        ///<playwright-file>focus.spec.js</playwright-file>
        ///<playwright-describe>Page.focus</playwright-describe>
        ///<playwright-it>should emit focus event</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldEmitFocusEvent()
        {
            await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
            bool focused = false;
            await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
            await Page.EvaluateAsync("() => d1.addEventListener('focus', focusEvent)");
            await Page.FocusAsync("#d1");
            Assert.True(focused);
        }

        ///<playwright-file>focus.spec.js</playwright-file>
        ///<playwright-describe>Page.focus</playwright-describe>
        ///<playwright-it>should emit blur event</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>focus.spec.js</playwright-file>
        ///<playwright-describe>Page.focus</playwright-describe>
        ///<playwright-it>should traverse focus</playwright-it>
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
