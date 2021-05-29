using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class ElementHandleClickTests : PageTestEx
    {
        [PlaywrightTest("elementhandle-click.spec.ts", "should work")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should work with Node removed")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithNodeRemoved()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var button = await Page.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should work for Shadow DOM v1")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForShadowDOMV1()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/shadow.html");
            var buttonHandle = (IElementHandle)await Page.EvaluateHandleAsync("() => button");
            await buttonHandle.ClickAsync();
            Assert.True(await Page.EvaluateAsync<bool>("() => clicked"));
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should work for TextNodes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkForTextNodes()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            var buttonTextNode = (IElementHandle)await Page.EvaluateHandleAsync("() => document.querySelector('button').firstChild");
            await buttonTextNode.ClickAsync();
            Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should throw for detached nodes")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDetachedNodes()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.remove()", button);
            var exception = await AssertThrowsAsync<PlaywrightException>(() => button.ClickAsync());
            StringAssert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should throw for hidden nodes with force")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForHiddenNodesWithForce()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.style.display = 'none'", button);
            var exception = await AssertThrowsAsync<PlaywrightException>(() => button.ClickAsync(new ElementHandleClickOptions { Force = true }));
            StringAssert.Contains("Element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should throw for recursively hidden nodes with force")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForRecursivelyHiddenNodesWithForce()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
            var button = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("button => button.parentElement.style.display = 'none'", button);
            var exception = await AssertThrowsAsync<PlaywrightException>(() => button.ClickAsync(new ElementHandleClickOptions { Force = true }));
            StringAssert.Contains("Element is not visible", exception.Message);
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should throw for &lt;br&gt; elements with force")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForBrElementsWithforce()
        {
            await Page.SetContentAsync("hello<br>goodbye");
            var br = await Page.QuerySelectorAsync("br");
            var exception = await AssertThrowsAsync<PlaywrightException>(() => br.ClickAsync(new ElementHandleClickOptions { Force = true }));
            StringAssert.Contains("Element is outside of the viewport", exception.Message);
        }

        [PlaywrightTest("elementhandle-click.spec.ts", "should double click the button")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldDoubleClickTheButton()
        {
            await Page.GotoAsync(TestConstants.ServerUrl + "/input/button.html");
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
            Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
        }
    }
}
