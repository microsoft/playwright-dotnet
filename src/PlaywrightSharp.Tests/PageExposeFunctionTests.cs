using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    ///<playwright-file>page-expose-function.spec.ts</playwright-file>
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageExposeFunctionTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageExposeFunctionTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>exposeBinding should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWork()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
            Assert.Equal(36, result);
        }

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should work with handles and complex objects</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should throw exception in page context</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should support throwing "null"</playwright-it>
        [Fact(Skip = "Not relevant for C#, js specific")]
        public void ShouldSupportThrowingNull()
        {
        }

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should be callable from-inside addInitScript</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should survive navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldSurviveNavigation()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
            await Page.GoToAsync(TestConstants.EmptyPage);
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
            Assert.Equal(36, result);
        }

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should await returned promise</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldAwaitReturnedPromise()
        {
            await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
            int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
            Assert.Equal(15, result);
        }

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should work on frames</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should work on frames before navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should work after cross origin navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should work with complex objects</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldWorkWithComplexObjects()
        {
            await Page.ExposeFunctionAsync("complexObject", (ComplexObject a, ComplexObject b) =>
            {
                return new ComplexObject { x = a.x + b.x };
            });
            var result = await Page.EvaluateAsync<ComplexObject>("async () => complexObject({ x: 5}, { x: 2})");
            Assert.Equal(7, result.x);
        }

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>exposeBindingHandle should work</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ExposeBindingHandleShouldWork()
        {
            IJSHandle target = null;
            await Page.ExposeBindingAsync(
                "logme",
                (BindingSource source, IJSHandle t) =>
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

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>exposeBindingHandle should not throw during navigation</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ExposeBindingHandleShouldNotThrowDuringNavigation()
        {
            IJSHandle target = null;
            await Page.ExposeBindingAsync(
                "logme",
                (BindingSource source, IJSHandle t) =>
                {
                    target = t;
                    return 17;
                });

            await TaskUtils.WhenAll(
                Page.EvaluateAsync(@"async url => {
                    window['logme']({ foo: 42 });
                    window.location.href = url;
                }", TestConstants.ServerUrl + "/one-style.html"),
                Page.WaitForNavigationAsync(LifecycleEvent.Load));
        }

        ///<playwright-file>browsercontext-expose-function.spec.ts</playwright-file>
        ///<playwright-it>should throw for duplicate registrations</playwright-it>
        [Fact(Timeout = PlaywrightSharp.Playwright.DefaultTimeout)]
        public async Task ShouldThrowForDuplicateRegistrations()
        {
            await Page.ExposeFunctionAsync("foo", () => { });
            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Page.ExposeFunctionAsync("foo", () => { }));
            Assert.Equal("Function \"foo\" has been already registered", exception.Message);
        }

        ///<playwright-file>page-expose-function.spec.ts</playwright-file>
        ///<playwright-it>exposeBindingHandle should throw for multiple arguments</playwright-it>
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
