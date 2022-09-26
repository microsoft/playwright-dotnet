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

public class BrowserContextExposeFunctionTests : ContextTestEx
{
    [PlaywrightTest("browsercontext-expose-function.spec.ts", "expose binding should work")]
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

        Assert.AreEqual(Context, bindingSource.Context);
        Assert.AreEqual(page, bindingSource.Page);
        Assert.AreEqual(page.MainFrame, bindingSource.Frame);

        Assert.AreEqual(11, result);
    }

    [PlaywrightTest("browsercontext-expose-function.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Context.ExposeFunctionAsync("add", (int a, int b) => a + b);
        var page = await Context.NewPageAsync();

        await page.ExposeFunctionAsync("mul", (int a, int b) => a * b);
        await Context.ExposeFunctionAsync("sub", (int a, int b) => a - b);

        var result = await page.EvaluateAsync<JsonElement>(@"async function() {
                return { mul: await mul(9, 4), add: await add(9, 4), sub: await sub(9, 4) };
            }");
        Assert.AreEqual(36, result.GetProperty("mul").GetInt32());
        Assert.AreEqual(13, result.GetProperty("add").GetInt32());
        Assert.AreEqual(5, result.GetProperty("sub").GetInt32());
    }

    [PlaywrightTest("browsercontext-expose-function.spec.ts", "should throw for duplicate registrations")]
    public async Task ShouldThrowForDuplicateRegistrations()
    {
        await Context.ExposeFunctionAsync("foo", () => { });
        await Context.ExposeFunctionAsync("bar", () => { });

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Context.ExposeFunctionAsync("foo", () => { }));
        Assert.AreEqual("Function \"foo\" has been already registered", exception.Message);

        var page = await Context.NewPageAsync();
        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => page.ExposeFunctionAsync("foo", () => { }));
        Assert.AreEqual("Function \"foo\" has been already registered in the browser context", exception.Message);

        await page.ExposeFunctionAsync("baz", () => { });
        exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Context.ExposeFunctionAsync("baz", () => { }));
        Assert.AreEqual("Function \"baz\" has been already registered in one of the pages", exception.Message);
    }

    [PlaywrightTest("browsercontext-expose-function.spec.ts", "should be callable from-inside addInitScript")]
    public async Task ShouldBeCallableFromInsideAddInitScript()
    {
        var args = new List<object>();
        await using var context = await Browser.NewContextAsync();
        await context.ExposeFunctionAsync("woof", (string arg) => { args.Add(arg); });

        await context.AddInitScriptAsync("woof('context')");
        var page = await context.NewPageAsync();
        await page.AddInitScriptAsync("woof('page')");

        args.Clear();
        await page.ReloadAsync();
        CollectionAssert.Contains(args, "context");
        CollectionAssert.Contains(args, "page");
    }

    [PlaywrightTest("browsercontext-expose-function.spec.ts", "exposeBindingHandle should work")]
    public async Task ExposeBindingHandleShouldWork()
    {
        IJSHandle target = null;
        await Context.ExposeBindingAsync(
            "logme",
            (BindingSource _, IJSHandle t) =>
            {
                target = t;
                return 17;
            });

        var page = await Context.NewPageAsync();
        int result = await page.EvaluateAsync<int>(@"async function() {
                return window['logme']({ foo: 42 });
            }");

        Assert.AreEqual(42, await target.EvaluateAsync<int>("x => x.foo"));
        Assert.AreEqual(17, result);
    }

    public async Task ExposeBindingHandleLikeInDocumentation()
    {
        var result = new TaskCompletionSource<string>();
        var page = await Context.NewPageAsync();
        await Context.ExposeBindingAsync("clicked", async (BindingSource _, IJSHandle t) =>
        {
            return result.TrySetResult(await t.AsElement().TextContentAsync());
        });

        await page.SetContentAsync("<script>\n" +
         "  document.addEventListener('click', event => window.clicked(event.target));\n" +
         "</script>\n" +
         "<div>Click me</div>\n" +
         "<div>Or click me</div>\n");

        await page.ClickAsync("div");
        Assert.AreEqual("Click me", await result.Task);
    }
}
