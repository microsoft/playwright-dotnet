/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Text.Json;

namespace Microsoft.Playwright.Tests;

///<playwright-file>page-expose-function.spec.ts</playwright-file>
public class PageExposeFunctionTests : PageTestEx
{
    [PlaywrightTest("page-expose-function.spec.ts", "exposeBinding should work")]
    public async Task ExposeBindingShouldWork()
    {
        BindingSource bindingSource = null;

        await Page.ExposeBindingAsync("add", (BindingSource source, int a, int b) =>
        {
            bindingSource = source;
            return a + b;
        });

        int result = await Page.EvaluateAsync<int>("async function () { return add(5, 6); }");

        Assert.AreEqual(Context, bindingSource.Context);
        Assert.AreEqual(Page, bindingSource.Page);
        Assert.AreEqual(Page.MainFrame, bindingSource.Frame);
        Assert.AreEqual(11, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
        int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
        Assert.AreEqual(36, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should work with handles and complex objects")]
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
        Assert.AreEqual("WOOF WOOF", result.GetProperty("message").GetString());
        StringAssert.Contains(nameof(PageExposeFunctionTests), result.GetProperty("stack").GetString());
    }

    [PlaywrightTest("page-expose-function.spec.ts", @"should support throwing ""null""")]
    public async Task ShouldSupportThrowingNull()
    {
        await Page.ExposeFunctionAsync("woof", () =>
        {
            throw null;
        });
        var result = await Page.EvaluateAsync(@"async () => {
                try {
                    await window['woof']();
                } catch (e) {
                    return e;
                }
            }");
        Assert.AreEqual(null, null);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should be callable from-inside addInitScript")]
    public async Task ShouldBeCallableFromInsideAddInitScript()
    {
        bool called = false;
        await Page.ExposeFunctionAsync("woof", () =>
        {
            called = true;
        });
        await Page.AddInitScriptAsync("woof()");
        await Page.ReloadAsync();
        Assert.True(called);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should survive navigation")]
    public async Task ShouldSurviveNavigation()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
        await Page.GotoAsync(Server.EmptyPage);
        int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
        Assert.AreEqual(36, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should await returned promise")]
    public async Task ShouldAwaitReturnedPromise()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
        int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
        Assert.AreEqual(15, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should work on frames")]
    public async Task ShouldWorkOnFrames()
    {
        await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        var frame = Page.Frames.ElementAt(1);
        int result = await frame.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
        Assert.AreEqual(15, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should work on frames before navigation")]
    public async Task ShouldWorkOnFramesBeforeNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        await Page.ExposeFunctionAsync("compute", (int a, int b) => Task.FromResult(a * b));
        var frame = Page.Frames.ElementAt(1);
        int result = await frame.EvaluateAsync<int>(@"async function() {
                return await compute(3, 5);
            }");
        Assert.AreEqual(15, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should work after cross origin navigation")]
    public async Task ShouldWorkAfterCrossOriginNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.ExposeFunctionAsync("compute", (int a, int b) => a * b);
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        int result = await Page.EvaluateAsync<int>(@"async function() {
                return await compute(9, 4);
            }");
        Assert.AreEqual(36, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "should work with complex objects")]
    public async Task ShouldWorkWithComplexObjects()
    {
        await Page.ExposeFunctionAsync("complexObject", (ComplexObject a, ComplexObject b) =>
        {
            return new ComplexObject { x = a.x + b.x };
        });
        var result = await Page.EvaluateAsync<ComplexObject>("async () => complexObject({ x: 5}, { x: 2})");
        Assert.AreEqual(7, result.x);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "exposeBindingHandle should work")]
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

        Assert.AreEqual(42, await target.EvaluateAsync<int>("x => x.foo"));
        Assert.AreEqual(17, result);
    }

    [PlaywrightTest("page-expose-function.spec.ts", "exposeBindingHandle should not throw during navigation")]
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
            Page.WaitForNavigationAsync(new() { WaitUntil = WaitUntilState.Load }),
            Page.EvaluateAsync(@"async url => {
                    window['logme']({ foo: 42 });
                    window.location.href = url;
                }", Server.Prefix + "/one-style.html"));
    }

    [PlaywrightTest("browsercontext-expose-function.spec.ts", "should throw for duplicate registrations")]
    public async Task ShouldThrowForDuplicateRegistrations()
    {
        await Page.ExposeFunctionAsync("foo", () => { });
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.ExposeFunctionAsync("foo", () => { }));
        Assert.AreEqual("Function \"foo\" has been already registered", exception.Message);
    }

    [PlaywrightTest]
    public async Task ShouldReturnNullForTask()
    {
        await Page.ExposeFunctionAsync("compute", () => Task.CompletedTask);
        await Page.GotoAsync(Server.EmptyPage);
        var result = await Page.EvaluateAsync(@"async function() {
                return await compute();
            }");
        Assert.IsNull(result);
    }

    [PlaywrightTest]
    public async Task ShouldReturnNullForTaskDelay()
    {
        await Page.ExposeFunctionAsync("compute", () => Task.Delay(100));
        await Page.GotoAsync(Server.EmptyPage);
        var result = await Page.EvaluateAsync(@"async function() {
                return await compute();
            }");
        Assert.IsNull(result);
    }

    internal class ComplexObject
    {
        public int x { get; set; }
    }
}
