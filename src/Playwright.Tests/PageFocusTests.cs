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

public class PageFocusTests : PageTestEx
{
    [PlaywrightTest("page-focus.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
        Assert.AreEqual("BODY", await Page.EvaluateAsync<string>("() => document.activeElement.nodeName"));
        await Page.FocusAsync("#d1");
        Assert.AreEqual("d1", await Page.EvaluateAsync<string>("() => document.activeElement.id"));
    }

    [PlaywrightTest("page-focus.spec.ts", "should emit focus event")]
    public async Task ShouldEmitFocusEvent()
    {
        await Page.SetContentAsync("<div id=d1 tabIndex=0></div>");
        bool focused = false;
        await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
        await Page.EvaluateAsync("() => d1.addEventListener('focus', focusEvent)");
        await Page.FocusAsync("#d1");
        Assert.True(focused);
    }

    [PlaywrightTest("page-focus.spec.ts", "should emit blur event")]
    public async Task ShouldEmitBlurEvent()
    {
        await Page.SetContentAsync("<div id=d1 tabIndex=0>DIV1</div><div id=d2 tabIndex=0>DIV2</div>");
        await Page.FocusAsync("#d1");
        bool focused = false;
        bool blurred = false;
        await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
        await Page.ExposeFunctionAsync("blurEvent", () => blurred = true);
        await Page.EvaluateAsync("() => d1.addEventListener('blur', blurEvent)");
        await Page.EvaluateAsync("() => d2.addEventListener('focus', focusEvent)");
        await Page.FocusAsync("#d2");
        Assert.True(focused);
        Assert.True(blurred);
    }

    [PlaywrightTest("page-focus.spec.ts", "should traverse focus")]
    public async Task ShouldTraverseFocus()
    {
        await Page.SetContentAsync("<input id=\"i1\"><input id=\"i2\">");
        bool focused = false;
        await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
        await Page.EvaluateAsync("() => i2.addEventListener('focus', focusEvent)");

        await Page.FocusAsync("#i1");
        await Page.Keyboard.TypeAsync("First");
        await Page.Keyboard.PressAsync("Tab");
        await Page.Keyboard.TypeAsync("Last");

        Assert.True(focused);

        Assert.AreEqual("First", await Page.EvalOnSelectorAsync<string>("#i1", "e => e.value"));
        Assert.AreEqual("Last", await Page.EvalOnSelectorAsync<string>("#i2", "e => e.value"));
    }
}
