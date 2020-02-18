using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>page.spec.js</playwright-file>
    ///<playwright-describe>Page.exposeFunction</playwright-describe>
    public class PageExposeFunctionTests : PlaywrightSharpPageBaseTest
    {
        internal PageExposeFunctionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should work</playwright-it>
        [Fact]
        public async Task ShouldWork()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
            Assert.Equal(36, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should throw exception in page context</playwright-it>
        [Fact]
        public async Task ShouldThrowExceptionInPageContext()
        {
            await Page.ExposeFunctionAsync("woof", () =>
            {
                throw new PlaywrightSharpException("WOOF WOOF");
            });
            var result = await Page.EvaluateAsync<JsonElement>(@"async () => {
                try
                {
                    await woof();
                }
                catch (e)
                {
                    return { message: e.message, stack: e.stack};
                }
            }");
            Assert.Equal("WOOF WOOF", result.GetProperty("message").GetString());
            Assert.Contains(nameof(PageExposeFunctionTests), result.GetProperty("stack").GetString());
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should support throwing "null"</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldSupportThrowingNull()
        {
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should be callable from-inside evaluateOnNewDocument</playwright-it>
        [Fact]
        public async Task ShouldBeCallableFromInsideEvaluateOnNewDocument()
        {
            bool called = false;
            await Page.ExposeFunctionAsync("woof", () =>
            {
                called = true;
            });
            await Page.EvaluateOnNewDocumentAsync("() => woof()");
            await Page.ReloadAsync();
            Assert.True(called);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should survive navigation</playwright-it>
        [Fact]
        public async Task ShouldSurviveNavigation()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
            await Page.GoToAsync(TestConstants.EmptyPage);
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
            Assert.Equal(36, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should await returned promise</playwright-it>
        [Fact]
        public async Task ShouldAwaitReturnedPromise()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
            Assert.Equal(36, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should work on frames</playwright-it>
        [Fact]
        public async Task ShouldWorkOnFrames()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            var frame = Page.Frames[1];
            int result = await frame.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
            Assert.Equal(15, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should work on frames before navigation</playwright-it>
        [Fact]
        public async Task ShouldWorkOnFramesBeforeNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
            var frame = Page.Frames[1];
            int result = await frame.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
            Assert.Equal(15, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should work after cross origin navigation</playwright-it>
        [Fact]
        public async Task ShouldWorkAfterCrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
            await Page.GoToAsync(TestConstants.CrossProcessUrl + "/empty.html");
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
            Assert.Equal(36, result);
        }

        ///<playwright-file>page.spec.js</playwright-file>
        ///<playwright-describe>Page.exposeFunction</playwright-describe>
        ///<playwright-it>should work with complex objects</playwright-it>
        [Fact]
        public async Task ShouldWorkWithComplexObjects()
        {
            await Page.ExposeFunctionAsync("complexObject", (ComplexObject a, ComplexObject b) =>
            {
                return new ComplexObject { x = a.x + b.x };
            });
            var result = await Page.EvaluateAsync<ComplexObject>("async () => complexObject({ x: 5}, { x: 2})");
            Assert.Equal(7, result.x);
        }

        internal class ComplexObject
        {
            public int x { get; set; }
        }
    }

}
