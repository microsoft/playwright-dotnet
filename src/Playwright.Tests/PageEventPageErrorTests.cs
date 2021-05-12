using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageEventPageErrorTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageEventPageErrorTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should fire")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFire()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.GotoAsync(TestConstants.ServerUrl + "/error.html")
            );

            Assert.Contains("Error", error);
            Assert.Contains("Fancy error!", error);
            string stack = await Page.EvaluateAsync<string>("() => window.e.stack");

            if (TestConstants.IsWebKit)
            {
                stack = stack.Replace("14:25", "15:19");
            }

            Assert.Contains(stack, error);
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should contain sourceURL")]
        [SkipBrowserAndPlatformFact(skipWebkit: true)]
        public async Task ShouldContainSourceURL()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.GotoAsync(TestConstants.ServerUrl + "/error.html"));

            Assert.Contains("myscript.js", error);
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should handle odd values")]
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

                Assert.Contains(TestConstants.IsFirefox ? "uncaught exception: " + kv[1].ToString() : kv[1].ToString(), error);
            }
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should handle object")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldHandleObject()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw {}; }, 0)", 0));

            Assert.Contains(TestConstants.IsChromium ? "Object" : "[object Object]", error);
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should handle window")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldHandleWindow()
        {
            var (error, _) = await TaskUtils.WhenAll(
                Page.WaitForEventAsync(PageEvent.PageError),
                Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw window ; }, 0)", 0));

            Assert.Contains(TestConstants.IsChromium ? "Window" : "[object Window]", error);
        }
    }
}
