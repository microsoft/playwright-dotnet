using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using PlaywrightSharp.Tests.BaseTests;
using PlaywrightSharp.Xunit;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class BrowserContextExposeFunctionTests : PlaywrightSharpBrowserContextBaseTest
    {
        /// <inheritdoc/>
        public BrowserContextExposeFunctionTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("browsercontext-expose-function.spec.ts", "expose binding should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ExposeBindingShouldWork()
        {
            BindingSource bindingSource = null;

            await Context.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
            {
                bindingSource = source;
                return a + b;
            });

            var page = await Context.NewPageAsync();
            int result = await page.EvaluateAsync<int>(@"async function() {
                return await add(5, 6);
            }");

            Assert.Same(Context, bindingSource.Context);
            Assert.Same(page, bindingSource.Page);
            Assert.Same(page.MainFrame, bindingSource.Frame);

            Assert.Equal(11, result);
        }

        [PlaywrightTest("browsercontext-expose-function.spec.ts", "should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWork()
        {
            await Context.ExposeFunctionAsync("add", (int a, int b) => a + b);
            var page = await Context.NewPageAsync();

            await page.ExposeFunctionAsync("mul", (int a, int b) => a * b);
            await Context.ExposeFunctionAsync("sub", (int a, int b) => a - b);

            var result = await page.EvaluateAsync<JsonElement>(@"async function() {
                return { mul: await mul(9, 4), add: await add(9, 4), sub: await sub(9, 4) };
            }");
            Assert.Equal(36, result.GetProperty("mul").GetInt32());
            Assert.Equal(13, result.GetProperty("add").GetInt32());
            Assert.Equal(5, result.GetProperty("sub").GetInt32());
        }

        [PlaywrightTest("browsercontext-expose-function.spec.ts", "should throw for duplicate registrations")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowForDuplicateRegistrations()
        {
            await Context.ExposeFunctionAsync("foo", () => { });
            await Context.ExposeFunctionAsync("bar", () => { });

            var exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Context.ExposeFunctionAsync("foo", () => { }));
            Assert.Equal("Function \"foo\" has been already registered", exception.Message);

            var page = await Context.NewPageAsync();
            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => page.ExposeFunctionAsync("foo", () => { }));
            Assert.Equal("Function \"foo\" has been already registered in the browser context", exception.Message);

            await page.ExposeFunctionAsync("baz", () => { });
            exception = await Assert.ThrowsAnyAsync<PlaywrightSharpException>(() => Context.ExposeFunctionAsync("baz", () => { }));
            Assert.Equal("Function \"baz\" has been already registered in one of the pages", exception.Message);
        }

        [PlaywrightTest("browsercontext-expose-function.spec.ts", "should be callable from-inside addInitScript")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldBeCallableFromInsideAddInitScript()
        {
            var args = new List<object>();
            await using var context = await Browser.NewContextAsync();
            await context.ExposeFunctionAsync("woof", (string arg) => { args.Add(arg); });

            await context.AddInitScriptAsync("() => woof('context')");
            var page = await context.NewPageAsync();
            await page.AddInitScriptAsync("() => woof('page')");

            args.Clear();
            await page.ReloadAsync();
            Assert.Contains("context", args);
            Assert.Contains("page", args);
        }

        [PlaywrightTest("browsercontext-expose-function.spec.ts", "exposeBindingHandle should work")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ExposeBindingHandleShouldWork()
        {
            IJSHandle target = null;
            await Context.ExposeBindingAsync(
                "logme",
                (BindingSource source, IJSHandle t) =>
                {
                    target = t;
                    return 17;
                });

            var page = await Context.NewPageAsync();
            int result = await page.EvaluateAsync<int>(@"async function() {
                return window['logme']({ foo: 42 });
            }");

            Assert.Equal(42, await target.EvaluateAsync<int>("x => x.foo"));
            Assert.Equal(17, result);
        }
    }
}
