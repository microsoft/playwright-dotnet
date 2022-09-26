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

namespace Microsoft.Playwright.Tests;

public class JSHandleAsElementTests : PageTestEx
{
    [PlaywrightTest("jshandle-as-element.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        var aHandle = await Page.EvaluateHandleAsync("() => document.body");
        var element = aHandle as IElementHandle;
        Assert.NotNull(element);
    }

    [PlaywrightTest("jshandle-as-element.spec.ts", "should return null for non-elements")]
    public async Task ShouldReturnNullForNonElements()
    {
        var aHandle = await Page.EvaluateHandleAsync("() => 2");
        var element = aHandle as IElementHandle;
        Assert.Null(element);
    }

    [PlaywrightTest("jshandle-as-element.spec.ts", "should return ElementHandle for TextNodes")]
    public async Task ShouldReturnElementHandleForTextNodes()
    {
        await Page.SetContentAsync("<div>ee!</div>");
        var aHandle = await Page.EvaluateHandleAsync("() => document.querySelector('div').firstChild");
        var element = aHandle as IElementHandle;
        Assert.NotNull(element);
        Assert.True(await Page.EvaluateAsync<bool>("e => e.nodeType === HTMLElement.TEXT_NODE", element));
    }

    [PlaywrightTest("jshandle-as-element.spec.ts", "should work with nullified Node")]
    public async Task ShouldWorkWithNullifiedNode()
    {
        await Page.SetContentAsync("<section>test</section>");
        await Page.EvaluateAsync("() => delete Node");
        var handle = await Page.EvaluateHandleAsync("() => document.querySelector('section')");
        var element = handle as IElementHandle;
        Assert.NotNull(element);
    }
}
