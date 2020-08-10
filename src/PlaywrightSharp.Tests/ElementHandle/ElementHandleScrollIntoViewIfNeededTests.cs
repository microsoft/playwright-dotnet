using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.ElementHandle
{
    ///<playwright-file>elementhandle.spec.js</playwright-file>
    ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class ElementHandleScrollIntoViewIfNeededTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ElementHandleScrollIntoViewIfNeededTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/offscreenbuttons.html");
            for (int i = 0; i < 11; ++i)
            {
                var button = await Page.QuerySelectorAsync("#btn" + i);
                double before = await button.EvaluateAsync<double>(@"button => {
                    return button.getBoundingClientRect().right - window.innerWidth;
                }");
                Assert.Equal(10 * i, before);
                await button.ScrollIntoViewIfNeededAsync();
                double after = await button.EvaluateAsync<double>(@"button => {
                    return button.getBoundingClientRect().right - window.innerWidth;
                }");
                Assert.True(after <= 0);
                await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
            }
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should throw for detached element</playwright-it>
        [Fact]
        public async Task ShouldThrowForDetachedElement()
        {
            await Page.SetContentAsync("<div>Hello</div>");
            var div = await Page.QuerySelectorAsync("div");
            await div.EvaluateAsync("div => div.remove()");
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => div.ScrollIntoViewIfNeededAsync());
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should wait for display:none to become visible</playwright-it>
        [Fact]
        public async Task ShouldWaitForDisplayNoneToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"display: none\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.display = 'block'");
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should wait for display:contents to become visible</playwright-it>
        [Fact]
        public async Task ShouldWaitForDisplayContentsToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"display: contents\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.display = 'block'");
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should wait for visibility:hidden to become visible</playwright-it>
        [Fact]
        public async Task ShouldWaitForVisibilityHiddenToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"visibility:hidden\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.visibility = 'visible'");
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should wait for zero-sized element to become visible</playwright-it>
        [Fact]
        public async Task ShouldWaitForZeroSiedElementToBecomeVisible()
        {
            await Page.SetContentAsync("<div style=\"height:0\">Hello</div>");
            await TestWaitingAsync(Page, "div => div.style.height = '100px'");
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should wait for nested display:none to become visible</playwright-it>
        [Fact]
        public async Task ShouldWaitForNestedDisplayNoneToBecomeVisible()
        {
            await Page.SetContentAsync("<span style=\"display: none\"><div>Hello</div></span>");
            await TestWaitingAsync(Page, "div => div.parentElement.style.display = 'block'");
        }

        ///<playwright-file>elementhandle.spec.js</playwright-file>
        ///<playwright-describe>ElementHandle.scrollIntoViewIfNeeded</playwright-describe>
        ///<playwright-it>should timeout waiting for visible</playwright-it>
        [Fact]
        public async Task ShouldTimeoutWaitingForVisible()
        {
            await Page.SetContentAsync("<div style=\"display: none\">Hello</div>");
            var div = await Page.QuerySelectorAsync("div");
            var exception = await Assert.ThrowsAnyAsync<TimeoutException>(() => div.ScrollIntoViewIfNeededAsync(3000));
            Assert.Contains("element is not visible", exception.Message);
        }

        private async Task TestWaitingAsync(IPage page, string after)
        {
            var div = await page.QuerySelectorAsync("div");
            var task = div.ScrollIntoViewIfNeededAsync();
            await page.EvaluateAsync("() => new Promise(f => setTimeout(f, 1000))");
            Assert.False(task.IsCompleted);
            await div.EvaluateAsync(after);
            await task;
        }
    }
}
