using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page.Events
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.Events.PageError</playwright-describe>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventsPageErrorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventsPageErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page.spec.js", "Page.Events.PageError", "should fire")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFire()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.GoToAsync(TestConstants.ServerUrl + "/error.html")
            );

            Assert.Equal("Error", error.Name);
            Assert.Equal("Fancy error!", error.Message);
            string stack = await Page.EvaluateAsync<string>("() => window.e.stack");

            if (TestConstants.IsWebKit)
            {
                stack = stack.Replace("14:25", "15:19");
            }

            Assert.Equal(stack, error.Stack);
        }

        [PlaywrightTest("page.spec.js", "Page.Events.PageError", "should contain sourceURL")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldContainSourceURL()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.GoToAsync(TestConstants.ServerUrl + "/error.html"));

            Assert.Contains("myscript.js", error.Stack);
        }

        [PlaywrightTest("page.spec.js", "Page.Events.PageError", "should handle odd values")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldHandleOddValues()
        {
            object[][] cases = new object[][]
            {
                new []{ null, "null"},
                //[undefined], "undefined" Not undefined here
                new object[]{ 0, "0"},
                new []{ "", "" },
            };

            foreach (object[] kv in cases)
            {
                var (error, _) = await TaskUtils.WhenAll(
                    Page.WaitForEventAsync(PageEvent.PageError),
                    Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw value; }, 0)", kv[0]));

                Assert.Contains(TestConstants.IsFirefox ? "uncaught exception: " + kv[1].ToString() : kv[1].ToString(), error.Message);
            }
        }

        [PlaywrightTest("page.spec.js", "Page.Events.PageError", "should handle object")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldHandleObject()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw {}; }, 0)", 0));

            Assert.Contains(TestConstants.IsChromium ? "Object" : "[object Object]", error.Message);
        }

        [PlaywrightTest("page.spec.js", "Page.Events.PageError", "should handle window")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldHandleWindow()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw window ; }, 0)", 0));

            Assert.Contains(TestConstants.IsChromium ? "Window" : "[object Window]", error.Message);
        }
    }
}
