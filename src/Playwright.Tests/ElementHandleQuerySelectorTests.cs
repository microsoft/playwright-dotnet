using System.Linq;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleQuerySelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleQuerySelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle-query-selector.spec.ts", "should query existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElement()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/playground.html");
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAsync(".second");
            var inner = await second.QuerySelectorAsync(".inner");
            string content = await Page.EvaluateAsync<string>("e => e.textContent", inner);
            Assert.Equal("A", content);
        }

        [PlaywrightTest("elementhandle-query-selector.spec.ts", "should return null for non-existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonExistingElement()
        {
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAsync(".third");
            Assert.Null(second);
        }

        [PlaywrightTest("elementhandle-query-selector.spec.ts", "should work for adopted elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForAdoptedElements()
        {
            await Page.GotoAsync(TestConstants.EmptyPage);

            var (popup, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.Popup),
                Page.EvaluateAsync("url => window.__popup = window.open(url)", TestConstants.EmptyPage));

            var divHandle = await Page.EvaluateHandleAsync(@"() => {
                const div = document.createElement('div');
                document.body.appendChild(div);
                const span = document.createElement('span');
                span.textContent = 'hello';
                div.appendChild(span);
                return div;
            }") as IElementHandle;

            Assert.NotNull(divHandle);
            Assert.Equal("hello", await divHandle.EvalOnSelectorAsync<string>("span", "e => e.textContent"));

            await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
            await Page.EvaluateAsync(@"() => {
              const div = document.querySelector('div');
              window.__popup.document.body.appendChild(div);
            }");

            Assert.NotNull(await divHandle.QuerySelectorAsync("span"));
            Assert.Equal("hello", await divHandle.EvalOnSelectorAsync<string>("span", "e => e.textContent"));
        }

        [PlaywrightTest("elementhandle-query-selector.spec.ts", "should query existing elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElements()
        {
            await Page.SetContentAsync("<html><body><div>A</div><br/><div>B</div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var elements = await html.QuerySelectorAllAsync("div");
            Assert.Equal(2, elements.Count());
            var tasks = elements.Select(element => Page.EvaluateAsync<string>("e => e.textContent", element));
            Assert.Equal(new[] { "A", "B" }, await TaskUtils.WhenAll(tasks));
        }

        [PlaywrightTest("elementhandle-query-selector.spec.ts", "should return empty array for non-existing elements")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnEmptyArrayForNonExistingElements()
        {
            await Page.SetContentAsync("<html><body><span>A</span><br/><span>B</span></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var elements = await html.QuerySelectorAllAsync("div");
            Assert.Empty(elements);
        }

        [PlaywrightTest("elementhandle-query-selector.spec.ts", "xpath should query existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task XPathShouldQueryExistingElement()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/playground.html");
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAllAsync("xpath=./body/div[contains(@class, 'second')]");
            var inner = await second.First().QuerySelectorAllAsync("xpath=./div[contains(@class, 'inner')]");
            string content = await Page.EvaluateAsync<string>("e => e.textContent", inner.First());
            Assert.Equal("A", content);
        }

        [PlaywrightTest("elementhandle-query-selector.spec.ts", "xpath should return null for non-existing element")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task XPathShouldReturnNullForNonExistingElement()
        {
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAllAsync("xpath=/div[contains(@class, 'third')]");
            Assert.Empty(second);
        }
    }
}
