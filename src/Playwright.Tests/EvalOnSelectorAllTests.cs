using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class EvalOnSelectorAllTests : PageTestEx
    {
        [PlaywrightTest("eval-on-selector-all.spec.ts", "should work with css selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCssSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("css=div", "divs => divs.length");
            Assert.AreEqual(3, divsCount);
        }

        [PlaywrightTest("eval-on-selector-all.spec.ts", "should work with text selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithTextSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("text=beautiful", "divs => divs.length");
            Assert.AreEqual(2, divsCount);
        }

        [PlaywrightTest("eval-on-selector-all.spec.ts", "should work with xpath selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithXpathSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("xpath=/html/body/div", "divs => divs.length");
            Assert.AreEqual(3, divsCount);
        }

        [PlaywrightTest("eval-on-selector-all.spec.ts", "should auto-detect css selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<div>hello</div><div>beautiful</div><div>world!</div>");
            int divsCount = await Page.EvalOnSelectorAllAsync<int>("div", "divs => divs.length");
            Assert.AreEqual(3, divsCount);
        }

        [PlaywrightTest("eval-on-selector-all.spec.ts", "should support >> syntax")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<div><span>hello</span></div><div>beautiful</div><div><span>wo</span><span>rld!</span></div><span>Not this one</span>");
            int spansCount = await Page.EvalOnSelectorAllAsync<int>("css=div >> css=span", "spans => spans.length");
            Assert.AreEqual(3, spansCount);
        }

        [PlaywrightTest("eval-on-selector-all.spec.ts", "should should support * capture")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportStarCapture()
        {
            await Page.SetContentAsync("<section><div><span>a</span></div></section><section><div><span>b</span></div></section>");
            Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("*css=div >> \"b\"", "divs => divs.length"));
            Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"b\"", "divs => divs.length"));
            Assert.AreEqual(4, await Page.EvalOnSelectorAllAsync<int>("section >> *", "divs => divs.length"));

            await Page.SetContentAsync("<section><div><span>a</span><span>a</span></div></section>");
            Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("*css=div >> \"a\"", "divs => divs.length"));
            Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"a\"", "divs => divs.length"));

            await Page.SetContentAsync("<div><span>a</span></div><div><span>a</span></div><section><div><span>a</span></div></section>");
            Assert.AreEqual(3, await Page.EvalOnSelectorAllAsync<int>("*css=div >> \"a\"", "divs => divs.length"));
            Assert.AreEqual(1, await Page.EvalOnSelectorAllAsync<int>("section >> *css=div >> \"a\"", "divs => divs.length"));
        }

        [PlaywrightTest("eval-on-selector-all.spec.ts", "should support * capture when multiple paths match")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportStarCaptureWhenMultiplePathsMatch()
        {
            await Page.SetContentAsync("<div><div><span></span></div></div><div></div>");
            Assert.AreEqual(2, await Page.EvalOnSelectorAllAsync<int>("*css=div >> span", "els => els.length"));

            await Page.SetContentAsync("<div><div><span></span></div><span></span><span></span></div><div></div>");
            Assert.AreEqual(2, await Page.EvalOnSelectorAllAsync<int>("*css=div >> span", "els => els.length"));
        }
    }
}
