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

public class PageCheckTests : PageTestEx
{
    [PlaywrightTest("page-check.spec.ts", "should check the box")]
    public async Task ShouldCheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await Page.CheckAsync("input");
        Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should not check the checked box")]
    public async Task ShouldNotCheckTheCheckedBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await Page.CheckAsync("input");
        Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should uncheck the box")]
    public async Task ShouldUncheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await Page.UncheckAsync("input");
        Assert.False(await Page.EvaluateAsync<bool?>("checkbox.checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should check the box by label")]
    public async Task ShouldCheckTheBoxByLabel()
    {
        await Page.SetContentAsync("<label for='checkbox'><input id='checkbox' type='checkbox'></input></label>");
        await Page.CheckAsync("label");
        Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should check the box outside label")]
    public async Task ShouldCheckTheBoxOutsideLabel()
    {
        await Page.SetContentAsync("<label for='checkbox'>Text</label><div><input id='checkbox' type='checkbox'></input></div>");
        await Page.CheckAsync("label");
        Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should check the box inside label w/o id")]
    public async Task ShouldCheckTheBoxInsideLabelWoId()
    {
        await Page.SetContentAsync("<label>Text<span><input id='checkbox' type='checkbox'></input></span></label>");
        await Page.CheckAsync("label");
        Assert.True(await Page.EvaluateAsync<bool?>("checkbox.checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should check radio")]
    public async Task ShouldCheckRadio()
    {
        await Page.SetContentAsync(@"
                <input type='radio'>one</input>
                <input id='two' type='radio'>two</input>
                <input type='radio'>three</input>");
        await Page.CheckAsync("#two");
        Assert.True(await Page.EvaluateAsync<bool?>("two.checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should check the box by aria role")]
    public async Task ShouldCheckTheBoxByAriaRole()
    {
        await Page.SetContentAsync(@"
                <div role='checkbox' id='checkbox'>CHECKBOX</div>
                <script>
                checkbox.addEventListener('click', () => checkbox.setAttribute('aria-checked', 'true'));
                </script>");
        await Page.CheckAsync("div");
        Assert.AreEqual("true", await Page.EvaluateAsync<string>("checkbox.getAttribute('aria-checked')"));
    }

    [PlaywrightTest("page-check.spec.ts", "trial run should not check")]
    public async Task TrialRunShouldNotCheck()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await Page.CheckAsync("input", new() { Trial = true });
        Assert.False(await Page.EvaluateAsync<bool>("window['checkbox'].checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "trial run should not uncheck")]
    public async Task TrialRunShouldNotUncheck()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        await Page.CheckAsync("input", new() { Trial = true });
        Assert.True(await Page.EvaluateAsync<bool>("window['checkbox'].checked"));
    }

    [PlaywrightTest("page-check.spec.ts", "should check the box using setChecked")]
    public async Task ShouldCheckTheBoxUsingSetChecked()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        await Page.SetCheckedAsync("input", true);
        Assert.IsTrue(await Page.EvaluateAsync<bool>("checkbox.checked"));
        await Page.SetCheckedAsync("input", false);
        Assert.IsFalse(await Page.EvaluateAsync<bool>("checkbox.checked"));
    }

}
