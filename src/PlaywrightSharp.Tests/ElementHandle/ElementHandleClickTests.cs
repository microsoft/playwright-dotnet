using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
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

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Retry]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should work with Node removed</playwright-it>
        [Retry]
        public async Task ShouldWorkWithNodeRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var button = await Page.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should work for Shadow DOM v1</playwright-it>
        [Retry]
        public async Task ShouldWorkForShadowDOMV1()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/shadow.html");
            var buttonHandle = (IElementHandle)await Page.EvaluateHandleAsync("() => button");
            await buttonHandle.ClickAsync();
            Assert.True(await Page.EvaluateAsync<bool>("() => clicked"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should work for TextNodes</playwright-it>
        [Retry]
        public async Task ShouldWorkForTextNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var buttonTextNode = (IElementHandle)await Page.EvaluateHandleAsync("() => document.querySelector('button').firstChild");
            await buttonTextNode.ClickAsync();
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for detached nodes</playwright-it>
        [Retry]
        public async Task ShouldThrowForDetachedNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.remove()", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync());
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for hidden nodes with force</playwright-it>
        [Retry]
        public async Task ShouldThrowForHiddenNodesWithForce()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.style.display = 'none'", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync(new ClickOptions { Force = true }));
            Assert.Contains("Element is not visible", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for recursively hidden nodes with force</playwright-it>
        [Retry]
        public async Task ShouldThrowForRecursivelyHiddenNodesWithForce()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.parentElement.style.display = 'none'", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync(new ClickOptions { Force = true }));
            Assert.Contains("Element is not visible", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for &lt;br&gt; elements with force</playwright-it>
        [Retry]
        public async Task ShouldThrowForBrElementsWithforce()
        {
            await Page.SetContentAsync("hello<br>goodbye");
            var br = await Page.QuerySelectorAsync("br");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => br.ClickAsync(new ClickOptions { Force = true }));
            Assert.Contains("Element is outside of the viewport", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should double click the button</playwright-it>
        [Retry]
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
            await button.DoubleClickAsync();

            Assert.True(await Page.EvaluateAsync<bool>("double"));
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }
    }
}
