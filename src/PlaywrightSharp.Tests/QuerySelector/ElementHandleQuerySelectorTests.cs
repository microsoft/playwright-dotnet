using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.QuerySelector
{
    ///<playwright-file>queryselector.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.$</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleQuerySelectorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleQuerySelectorTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$</playwright-describe>
        ///<playwright-it>should query existing element</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldQueryExistingElement()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/playground.html");
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">A</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAsync(".second");
            var inner = await second.QuerySelectorAsync(".inner");
            string content = await Page.EvaluateAsync<string>("e => e.textContent", inner);
            Assert.Equal("A", content);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$</playwright-describe>
        ///<playwright-it>should return null for non-existing element</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReturnNullForNonExistingElement()
        {
            await Page.SetContentAsync("<html><body><div class=\"second\"><div class=\"inner\">B</div></div></body></html>");
            var html = await Page.QuerySelectorAsync("html");
            var second = await html.QuerySelectorAsync(".third");
            Assert.Null(second);
        }

        ///<playwright-file>queryselector.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.$</playwright-describe>
        ///<playwright-it>should work for adopted elements</playwright-it>
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForAdoptedElements()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);

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

            await popup.Page.WaitForLoadStateAsync(LifecycleEvent.DOMContentLoaded);
            await Page.EvaluateAsync(@"() => {
              const div = document.querySelector('div');
              window.__popup.document.body.appendChild(div);
            }");

            Assert.NotNull(await divHandle.QuerySelectorAsync("span"));
            Assert.Equal("hello", await divHandle.EvalOnSelectorAsync<string>("span", "e => e.textContent"));
        }
    }
}
