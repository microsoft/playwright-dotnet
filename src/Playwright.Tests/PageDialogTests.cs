/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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

public class PageDialogTests : PageTestEx
{
    [PlaywrightTest("page-dialog.spec.ts", "should fire")]
    public Task ShouldFire()
    {
        Page.Dialog += async (_, e) =>
        {
            Assert.AreEqual(DialogType.Alert, e.Type);
            Assert.AreEqual(string.Empty, e.DefaultValue);
            Assert.AreEqual("yo", e.Message);

            await e.AcceptAsync();
        };

        return Page.EvaluateAsync("alert('yo');");
    }

    [PlaywrightTest("page-dialog.spec.ts", "should allow accepting prompts")]
    public async Task ShouldAllowAcceptingPrompts()
    {
        Page.Dialog += async (_, e) =>
        {
            Assert.AreEqual(DialogType.Prompt, e.Type);
            Assert.AreEqual("yes.", e.DefaultValue);
            Assert.AreEqual("question?", e.Message);

            await e.AcceptAsync("answer!");
        };

        string result = await Page.EvaluateAsync<string>("prompt('question?', 'yes.')");
        Assert.AreEqual("answer!", result);
    }

    [PlaywrightTest("page-dialog.spec.ts", "should dismiss the prompt")]
    public async Task ShouldDismissThePrompt()
    {
        Page.Dialog += async (_, e) =>
        {
            await e.DismissAsync();
        };

        string result = await Page.EvaluateAsync<string>("prompt('question?')");
        Assert.Null(result);
    }

    [PlaywrightTest("page-dialog.spec.ts", "should accept the confirm prompt")]
    public async Task ShouldAcceptTheConfirmPrompts()
    {
        Page.Dialog += async (_, e) =>
        {
            await e.AcceptAsync();
        };

        bool result = await Page.EvaluateAsync<bool>("confirm('boolean?')");
        Assert.True(result);
    }

    [PlaywrightTest("page-dialog.spec.ts", "should dismiss the confirm prompt")]
    public async Task ShouldDismissTheConfirmPrompt()
    {
        Page.Dialog += async (_, e) =>
        {
            await e.DismissAsync();
        };

        bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
        Assert.False(result);
    }

    [PlaywrightTest("page-dialog.spec.ts", "should log prompt actions")]
    public async Task ShouldLogPromptActions()
    {
        Page.Dialog += async (_, e) =>
        {
            await e.DismissAsync();
        };

        bool result = await Page.EvaluateAsync<bool>("prompt('boolean?')");
        Assert.False(result);
    }

    [PlaywrightTest("page-dialog.spec.ts", "should be able to close context with open alert")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldBeAbleToCloseContextWithOpenAlert()
    {
        var context = await Browser.NewContextAsync();
        var page = await context.NewPageAsync();

        var alertEvent = new TaskCompletionSource<IDialog>();
        page.Dialog += (_, dialog) => alertEvent.TrySetResult(dialog);

        await page.EvaluateAsync("() => setTimeout(() => alert('hello'), 0)");
        await alertEvent.Task;
        await context.CloseAsync();
    }

    [PlaywrightTest("page-dialog.spec.ts", "should auto-dismiss the prompt without listeners")]
    public async Task ShouldAutoDismissThePrompt()
    {
        string result = await Page.EvaluateAsync<string>("prompt('question?')");
        Assert.Null(result);
    }
}
