using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Tests.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Frame
{
    ///<playwright-file>waittask.spec.js</playwright-file>
    ///<playwright-describe>Frame.$wait</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "xUnit1000:Test classes must be public", Justification = "Disabled")]
    class WaitForSelectorEvaluateTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public WaitForSelectorEvaluateTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.$wait</playwright-describe>
        ///<playwright-it>should accept arguments</playwright-it>
        [Retry]
        public async Task ShouldAcceptArguments()
        {
            await Page.SetContentAsync("<div></div>");
            var result = await Page.WaitForSelectorEvaluateAsync("div", "(e, foo, bar) => e.nodeName + foo + bar", null, "foo1", "bar2");
            Assert.Equal("DIVfoo1bar2", await result.GetJsonValueAsync<string>());
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.$wait</playwright-describe>
        ///<playwright-it>should query selector constantly</playwright-it>
        [Retry]
        public async Task ShouldQuerySelectorConstantly()
        {
            await Page.SetContentAsync("<div></div>");
            var waitTask = Page.WaitForSelectorEvaluateAsync("span", "(e) => e");

            Assert.False(waitTask.IsCompleted);
            await Page.SetContentAsync("<section></section>");
            Assert.False(waitTask.IsCompleted);
            await Page.SetContentAsync("<span>text</span>");
            await waitTask;
            Assert.Equal("text", await waitTask.Result.EvaluateAsync<string>("e => e.textContent"));
        }

        ///<playwright-file>waittask.spec.js</playwright-file>
        ///<playwright-describe>Frame.$wait</playwright-describe>
        ///<playwright-it>should be able to wait for removal</playwright-it>
        [Retry]
        public async Task ShouldBeAbleToWaitForRemoval()
        {
            await Page.SetContentAsync("<div></div>");
            var waitTask = Page.WaitForSelectorEvaluateAsync("div", "(e) => !e");

            Assert.False(waitTask.IsCompleted);
            await Page.SetContentAsync("<section></section>");
            await waitTask;
            Assert.True(await waitTask.Result.GetJsonValueAsync<bool>());
        }
    }
}
