using PlaywrightSharp.Tests.BaseTests;
using Xunit.Abstractions;
using Xunit;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.click</playwright-describe>
    public class ElementHandleClickTests : PlaywrightSharpPageBaseTest
    {
        internal ElementHandleClickTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
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
        [Fact]
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
        [Fact]
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
        [Fact]
        public async Task ShouldWorkForTextNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var buttonTextNode = (IElementHandle)await Page.EvaluateHandleAsync("() => document.querySelector('button').firstChild");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => buttonTextNode.ClickAsync());
            Assert.Equal("Node is not of type HTMLElement", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for detached nodes</playwright-it>
        [Fact]
        public async Task ShouldThrowForDetachedNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.remove()", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync());
            Assert.Equal("Node is detached from document", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for hidden nodes</playwright-it>
        [Fact]
        public async Task ShouldThrowForHiddenNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.style.display = 'none'", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync());
            Assert.Equal("Node is either not visible or not an HTMLElement", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for recursively hidden nodes</playwright-it>
        [Fact]
        public async Task ShouldThrowForRecursivelyHiddenNodes()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.parentElement.style.display = 'none'", button);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => button.ClickAsync());
            Assert.Equal("Node is either not visible or not an HTMLElement", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.click</playwright-describe>
        ///<playwright-it>should throw for &lt;br&gt; elements</playwright-it>
        [Fact]
        public async Task ShouldThrowForBrElements()
        {
            await Page.SetContentAsync("hello<br>goodbye");
            var br = await Page.QuerySelectorAsync("br");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => br.ClickAsync());
            Assert.Equal("Node is either not visible or not an HTMLElement", exception.Message);
        }
    }
}
