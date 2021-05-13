using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    ///<playwright-file>page-expose-function.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageExposeFunctionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageExposeFunctionTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-expose-function.spec.ts", "exposeBinding should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ExposeBindingShouldWork()
        {
            BindingSource bindingSource = null;

            await Page.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
            {
                bindingSource = source;
                return a + b;
            });

            int result = await Page.EvaluateAsync<int>("async function () { return add(5, 6); }");

            Assert.Same(Context, bindingSource.Context);
            Assert.Same(Page, bindingSource.Page);
            Assert.Same(Page.MainFrame, bindingSource.Frame);
            Assert.Equal(11, result);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
            Assert.Equal(36, result);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should work with handles and complex objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithHandlesAndComplexObjects()
        {
            var fooHandle = await Page.EvaluateHandleAsync(@"() => {
                window['fooValue'] = { bar: 2 };
                return window['fooValue'];
            }");

            await Page.ExposeFunctionAsync("handle", () => new[] { new { foo = fooHandle } });

            Assert.True(await Page.EvaluateAsync<bool>(@"async function() {
                const value = await window['handle']();
                const [{ foo }] = value;
                return foo === window['fooValue'];
            }"));
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should throw exception in page context")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowExceptionInPageContext()
        {
            await Page.ExposeFunctionAsync("woof", (System.Action)(() =>
            {
                throw new PlaywrightException("WOOF WOOF");
            }));
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

        [PlaywrightTest("page-expose-function.spec.ts", @"should support throwing ""null""")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldSupportThrowingNull()
        {
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should be callable from-inside addInitScript")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCallableFromInsideAddInitScript()
        {
            bool called = false;
            await Page.ExposeFunctionAsync("woof", () =>
            {
                called = true;
            });
            await Page.AddInitScriptAsync("() => woof()");
            await Page.ReloadAsync();
            Assert.True(called);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should survive navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSurviveNavigation()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
            await Page.GoToAsync(TestConstants.EmptyPage);
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
            Assert.Equal(36, result);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should await returned promise")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldAwaitReturnedPromise()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
            Assert.Equal(15, result);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should work on frames")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkOnFrames()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            var frame = Page.Frames.ElementAt(1);
            int result = await frame.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
            Assert.Equal(15, result);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should work on frames before navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkOnFramesBeforeNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/frames/nested-frames.html");
            await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
            var frame = Page.Frames.ElementAt(1);
            int result = await frame.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
            Assert.Equal(15, result);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "should work after cross origin navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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

        [PlaywrightTest("page-expose-function.spec.ts", "should work with complex objects")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithComplexObjects()
        {
            await Page.ExposeFunctionAsync("complexObject", (ComplexObject a, ComplexObject b) =>
            {
                return new ComplexObject { x = a.x + b.x };
            });
            var result = await Page.EvaluateAsync<ComplexObject>("async () => complexObject({ x: 5}, { x: 2})");
            Assert.Equal(7, result.x);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "exposeBindingHandle should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ExposeBindingHandleShouldWork()
        {
            IJSHandle target = null;
            await Page.ExposeBindingAsync(
                "logme",
                (_, t) =>
                {
                    target = t;
                    return 17;
                });

            int result = await Page.EvaluateAsync<int>(@"async function() {
                return window['logme']({ foo: 42 });
            }");

            Assert.Equal(42, await target.EvaluateAsync<int>("x => x.foo"));
            Assert.Equal(17, result);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "exposeBindingHandle should not throw during navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ExposeBindingHandleShouldNotThrowDuringNavigation()
        {
            IJSHandle target = null;
            await Page.ExposeBindingAsync(
                "logme",
                (_, t) =>
                {
                    target = t;
                    return 17;
                });

            await TaskUtils.WhenAll(
                Page.WaitForNavigationAsync(WaitUntilState.Load),
                Page.EvaluateAsync(@"async url => {
                    window['logme']({ foo: 42 });
                    window.location.href = url;
                }", TestConstants.ServerUrl + "/one-style.html"));
        }

        [PlaywrightTest("browsercontext-expose-function.spec.ts", "should throw for duplicate registrations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDuplicateRegistrations()
        {
            await Page.ExposeFunctionAsync("foo", () => { });
            var exception = await Assert.ThrowsAnyAsync<PlaywrightException>(() => Page.ExposeFunctionAsync("foo", () => { }));
            Assert.Equal("Function \"foo\" has been already registered", exception.Message);
        }

        [PlaywrightTest("page-expose-function.spec.ts", "exposeBindingHandle should throw for multiple arguments")]
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ExposeBindingHandleShouldThrowForMultipleArguments()
        {
        }

        internal class ComplexObject
        {
            public int x { get; set; }
        }
    }
}
