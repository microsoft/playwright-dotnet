using System;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>waittask.spec.js</playwright-file>
    ///<playwright-describe>Page.WaitFor</playwright-describe>
    [Trait("Category", "chromium")]
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageWaitForTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageWaitForTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should wait for selector</playwright-it>
        [Retry]
        public async Task ShouldWaitForSelector()
        {
            bool found = false;
            var waitFor = Page.WaitForSelectorAsync("div").ContinueWith(_ => found = true);
            await Page.GoToAsync(TestConstants.EmptyPage);

            Assert.False(found);

            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await waitFor;
            Assert.True(found);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should wait for an xpath</playwright-it>
        [Retry]
        public async Task ShouldWaitForAnXpath()
        {
            bool found = false;
            var waitFor = Page.WaitForSelectorAsync("//div").ContinueWith(_ => found = true);
            await Page.GoToAsync(TestConstants.EmptyPage);
            Assert.False(found);
            await Page.GoToAsync(TestConstants.ServerUrl + "/grid.html");
            await waitFor;
            Assert.True(found);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should not allow you to select an element with single slash xpath</playwright-it>
        [Retry]
        public async Task ShouldNotAllowYouToSelectAnElementWithSingleSlashXpath()
        {
            await Page.SetContentAsync("<div>some text</div>");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() =>
                Page.WaitForSelectorAsync("/html/body/div"));
            Assert.NotNull(exception);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should timeout</playwright-it>
        [Retry]
        public async Task ShouldTimeout()
        {
            var startTime = DateTime.Now;
            int timeout = 42;
            await Page.WaitForTimeoutAsync(timeout);
            Assert.True((DateTime.Now - startTime).TotalMilliseconds > timeout / 2);
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should work with multiline body</playwright-it>
        [Retry]
        public async Task ShouldWorkWithMultilineBody()
        {
            var result = await Page.WaitForFunctionAsync(@"
                (() => true)()
            ");
            Assert.True(await result.GetJsonValueAsync<bool>());
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should work with multiline body</playwright-it>
        [Retry]
        public Task ShouldWaitForPredicate()
            => Task.WhenAll(
                Page.WaitForFunctionAsync("() => window.innerWidth < 100"),
                Page.SetViewportAsync(new Viewport { Width = 10, Height = 10 })
        );

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should throw when unknown type</playwright-it>
        [Fact(Skip = "We don't this test")]
        public void ShouldThrowWhenUnknownType() { }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>PageWaitFor</playwright-describe>
        ///<playwright-it>should wait for predicate with arguments</playwright-it>
        [Retry]
        public async Task ShouldWaitForPredicateWithArguments()
            => await Page.WaitForFunctionAsync("(arg1, arg2) => arg1 !== arg2", new WaitForSelectorOptions(), 1, 2);
    }
}
