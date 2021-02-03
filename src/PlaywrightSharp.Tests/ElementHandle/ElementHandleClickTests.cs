using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.click</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleClickTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleClickTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should work with Node removed")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNodeRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var button = await Page.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should work for Shadow DOM v1")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForShadowDOMV1()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/shadow.html");
            var buttonHandle = (IElementHandle)await Page.EvaluateHandleAsync("() => button");
            await buttonHandle.ClickAsync();
            Assert.True(await Page.EvaluateAsync<bool>("() => clicked"));
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should work for TextNodes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForTextNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var buttonTextNode = (IElementHandle)await Page.EvaluateHandleAsync("() => document.querySelector('button').firstChild");
            await buttonTextNode.ClickAsync();
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should throw for detached nodes")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDetachedNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.remove()", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync());
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should throw for hidden nodes with force")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForHiddenNodesWithForce()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.style.display = 'none'", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync(force: true));
            Assert.Contains("Element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should throw for recursively hidden nodes with force")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForRecursivelyHiddenNodesWithForce()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.parentElement.style.display = 'none'", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync(force: true));
            Assert.Contains("Element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should throw for &lt;br&gt; elements with force")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForBrElementsWithforce()
        {
            await Page.SetContentAsync("hello<br>goodbye");
            var br = await Page.QuerySelectorAsync("br");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => br.ClickAsync(force: true));
            Assert.Contains("Element is outside of the viewport", exception.Message);
        }

        [PlaywrightTest("elementhandle.spec.js", "ElementHandle.click", "should double click the button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDoubleClickTheButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync(@"() => {
              window.double = false;
              const button = document.querySelector('button');
              button.addEventListener('dblclick', event => {
                window.double = true;
              });
            }");

            var button = await Page.QuerySelectorAsync("button");
            await button.DblClickAsync();

            Assert.True(await Page.EvaluateAsync<bool>("double"));
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }
    }
}
