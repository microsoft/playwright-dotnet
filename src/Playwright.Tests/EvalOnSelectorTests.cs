using System.Threading.Tasks;
using Microsoft.Playwright.Tests.BaseTests;
using Microsoft.Playwright.Testing.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class EvalOnSelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public EvalOnSelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with css selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithCssSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("css=section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with id selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithIdSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("id=testAttribute", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with data-test selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDataTestSelector()
        {
            await Page.SetContentAsync("<section data-test=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("data-test=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with data-testid selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDataTestidSelector()
        {
            await Page.SetContentAsync("<section data-testid=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with data-test-id selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithDataTestIdSelector()
        {
            await Page.SetContentAsync("<section data-test-id=foo id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("data-test-id=foo", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with text selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithTextSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("text=\"43543\"", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with xpath selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithXpathSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("xpath=/html/body/section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with text selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithTextSelector2()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("text=43543", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should auto-detect css selector")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("section", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should auto-detect nested selectors")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectNestedSelectors()
        {
            await Page.SetContentAsync("<div foo=bar><section>43543<span>Hello<div id=target></div></span></section></div>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("div[foo=bar] > section >> \"Hello\" >> div", "e => e.id");
            Assert.Equal("target", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should auto-detect css selector with attributes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectCssSelectorWithAttributes()
        {
            await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
            string idAttribute = await Page.EvalOnSelectorAsync<string>("section[id=\"testAttribute\"]", "e => e.id");
            Assert.Equal("testAttribute", idAttribute);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should accept arguments")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptArguments()
        {
            await Page.SetContentAsync("<section>hello</section>");
            string text = await Page.EvalOnSelectorAsync<string>("section", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should accept ElementHandles as arguments")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAcceptElementHandlesAsArguments()
        {
            await Page.SetContentAsync("<section>hello</section><div> world</div>");
            var divHandle = await Page.QuerySelectorAsync("div");
            string text = await Page.EvalOnSelectorAsync<string>("section", "(e, div) => e.textContent + div.textContent", divHandle);
            Assert.Equal("hello world", text);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should throw error if no element is found")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowErrorIfNoElementIsFound()
        {
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.EvalOnSelectorAsync("section", "e => e.id"));
            Assert.Contains("failed to find element matching selector \"section\"", exception.Message);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should support >> syntax")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<section><div>hello</div></section>");
            string text = await Page.EvalOnSelectorAsync<string>("css=section >> css=div", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should support >> syntax with different engines")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDoubleGreaterThanSyntaxWithDifferentEngines()
        {
            await Page.SetContentAsync("<section><div><span>hello</span></div></section>");
            string text = await Page.EvalOnSelectorAsync<string>("xpath=/html/body/section >> css=div >> text=\"hello\"", "(e, suffix) => e.textContent + suffix", " world!");
            Assert.Equal("hello world!", text);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should support spaces with >> syntax")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportSpacesWithDoubleGreaterThanSyntax()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/deep-shadow.html");
            string text = await Page.EvalOnSelectorAsync<string>(" css = div >>css=div>>css   = span  ", "e => e.textContent");
            Assert.Equal("Hello from root2", text);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should not stop at first failure with >> syntax")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotStopAtFirstFailureWithDoubleGraterThanSyntax()
        {
            await Page.SetContentAsync("<div><span>Next</span><button>Previous</button><button>Next</button></div>");
            string text = await Page.EvalOnSelectorAsync<string>("button >> \"Next\"", "(e) => e.outerHTML");
            Assert.Equal("<button>Next</button>", text);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should support * capture")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportStarCapture()
        {
            await Page.SetContentAsync("<section><div><span>a</span></div></section><section><div><span>b</span></div></section>");
            Assert.Equal("<div><span>b</span></div>", await Page.EvalOnSelectorAsync<string>("*css=div >> \"b\"", "(e) => e.outerHTML"));
            Assert.Equal("<div><span>b</span></div>", await Page.EvalOnSelectorAsync<string>("section >> *css=div >> \"b\"", "(e) => e.outerHTML"));
            Assert.Equal("<span>b</span>", await Page.EvalOnSelectorAsync<string>("css=div >> *text=\"b\"", "(e) => e.outerHTML"));
            Assert.NotNull(await Page.QuerySelectorAsync("*"));
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should throw on multiple * captures")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowOnMultipleStarCaptures()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvalOnSelectorAsync<string>("*css=div >> *css=span", "(e) => e.outerHTML"));
            Assert.Equal("Only one of the selectors can capture using * modifier", exception.Message);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should throw on malformed * capture")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowOnMalformedStarCapture()
        {
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.EvalOnSelectorAsync<string>("*=div", "(e) => e.outerHTML"));
            Assert.Equal("Unknown engine \"\" while parsing selector *=div", exception.Message);
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with spaces in css attributes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithSpacesInCssAttributes()
        {
            await Page.SetContentAsync("<div><input placeholder=\"Select date\"></div>");
            Assert.NotNull(await Page.WaitForSelectorAsync("[placeholder = \"Select date\"]"));
            Assert.NotNull(await Page.WaitForSelectorAsync("[placeholder = 'Select date']"));
            Assert.NotNull(await Page.WaitForSelectorAsync("input[placeholder = \"Select date\"]"));
            Assert.NotNull(await Page.WaitForSelectorAsync("input[placeholder = 'Select date']"));
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select date\"]"));
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select date']"));
            Assert.NotNull(await Page.QuerySelectorAsync("input[placeholder = \"Select date\"]"));
            Assert.NotNull(await Page.QuerySelectorAsync("input[placeholder = 'Select date']"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("[placeholder = \"Select date\"]", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("[placeholder = 'Select date']", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("input[placeholder = \"Select date\"]", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("input[placeholder = 'Select date']", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css =[placeholder = \"Select date\"]", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css =[placeholder = 'Select date']", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css = input[placeholder = \"Select date\"]", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("css = input[placeholder = 'Select date']", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("div >> [placeholder = \"Select date\"]", "e => e.outerHTML"));
            Assert.Equal("<input placeholder=\"Select date\">", await Page.EvalOnSelectorAsync<string>("div >> [placeholder = 'Select date']", "e => e.outerHTML"));
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with quotes in css attributes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWihQuotesInCssAttributes()
        {
            await Page.SetContentAsync("<div><input placeholder=\"Select&quot;date\"></div>");
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select\\\"date\"]"));
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select\"date']"));

            await Page.SetContentAsync("<div><input placeholder=\"Select &quot; date\"></div>");
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select \\\" date\"]"));
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select \" date']"));

            await Page.SetContentAsync("<div><input placeholder=\"Select&apos;date\"></div>");
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select'date\"]"));
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select\\'date']"));

            await Page.SetContentAsync("<div><input placeholder=\"Select &apos; date\"></div>");
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = \"Select ' date\"]"));
            Assert.NotNull(await Page.QuerySelectorAsync("[placeholder = 'Select \\' date']"));
        }

        [PlaywrightTest("eval-on-selector.spec.ts", "should work with quotes in css attributes when missing")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWihQuotesInCssAttributesWhenMissing()
        {
            var inputTask = Page.WaitForSelectorAsync("[placeholder = \"Select\\\"date\"]");
            Assert.Null(await Page.QuerySelectorAsync("[placeholder = \"Select\\\"date\"]"));
            await Page.SetContentAsync("<div><input placeholder=\"Select&quot;date\"></div>");
            await inputTask;
        }
    }
}
