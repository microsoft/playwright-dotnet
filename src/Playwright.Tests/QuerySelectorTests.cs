using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class QuerySelectorTests : PageTestEx
    {
        [PlaywrightTest("queryselector.spec.ts", "should query existing elements")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElements()
        {
            await Page.SetContentAsync("<div>A</div><br/><div>B</div>");
            var elements = await Page.QuerySelectorAllAsync("div");
            Assert.AreEqual(2, elements.Count());
            var tasks = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
            Assert.AreEqual(new[] { "A", "B" }, await TaskUtils.WhenAll(tasks));
        }

        [PlaywrightTest("queryselector.spec.ts", "should return empty array if nothing is found")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayIfNothingIsFound()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var elements = await Page.QuerySelectorAllAsync("div");
            Assert.IsEmpty(elements);
        }

        [PlaywrightTest("queryselector.spec.ts", "xpath should query existing element")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task XpathShouldQueryExistingElement()
        {
            await Page.SetContentAsync("<section>test</section>");
            var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/section");
            Assert.NotNull(elements.FirstOrDefault());
            Assert.That(elements, Has.Count.EqualTo(1));
        }

        [PlaywrightTest("queryselector.spec.ts", "should return empty array for non-existing element")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayForNonExistingElement()
        {
            var elements = await Page.QuerySelectorAllAsync("//html/body/non-existing-element");
            Assert.IsEmpty(elements);
        }

        [PlaywrightTest("queryselector.spec.ts", "should return multiple elements")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnMultipleElements()
        {
            await Page.SetContentAsync("<div></div><div></div>");
            var elements = await Page.QuerySelectorAllAsync("xpath=/html/body/div");
            Assert.AreEqual(2, elements.Count());
        }

        [PlaywrightTest("queryselector.spec.ts", "should query existing element with css selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("css=section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should query existing element with text selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithTextSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("text=\"test\"");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should query existing element with xpath selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElementWithXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("xpath=/html/body/section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should return null for non-existing element")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonExistingElement()
        {
            var element = await Page.QuerySelectorAsync("non-existing-element");
            Assert.Null(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should auto-detect xpath selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectXpathSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("//html/body/section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should auto-detect xpath selector with starting parenthesis")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectXpathSelectorWithStartingParenthesis()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("(//section)[1]");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should auto-detect text selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectTextSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("\"test\"");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should auto-detect css selector")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldAutoDetectCssSelector()
        {
            await Page.SetContentAsync("<section>test</section>");
            var element = await Page.QuerySelectorAsync("section");
            Assert.NotNull(element);
        }

        [PlaywrightTest("queryselector.spec.ts", "should support >> syntax")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldSupportDoubleGreaterThanSyntax()
        {
            await Page.SetContentAsync("<section><div>test</div></section>");
            var element = await Page.QuerySelectorAsync("css=section >> css=div");
            Assert.NotNull(element);
        }
    }
}
