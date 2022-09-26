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

public class PageClickReactTests : PageTestEx
{
    [PlaywrightTest("page-click-react.spec.ts", "should timeout when click opens alert")]
    public async Task ShouldTimeoutWhenClickOpensAlert()
    {
        var dialogEvent = new TaskCompletionSource<IDialog>();
        Page.Dialog += (_, dialog) => dialogEvent.TrySetResult(dialog);

        await Page.SetContentAsync("<div onclick='window.alert(123)'>Click me</div>");

        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.ClickAsync("div", new() { Timeout = 3000 }));
        StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
        var dialog = await dialogEvent.Task;
        await dialog.DismissAsync();
    }

    [PlaywrightTest("page-click-react.spec.ts", "should not retarget when element changes on hover")]
    public async Task ShouldNotRetargetWhenElementChangesOnHover()
    {
        await Page.GotoAsync(Server.Prefix + "/react.html");
        await Page.EvaluateAsync(@"() => {
                renderComponent(e('div', {}, [e(MyButton, { name: 'button1', renameOnHover: true }), e(MyButton, { name: 'button2' })] ));
            }");

        await Page.ClickAsync("text=button1");
        Assert.True(await Page.EvaluateAsync<bool?>("window.button1"));
        Assert.Null(await Page.EvaluateAsync<bool?>("window.button2"));
    }

    [PlaywrightTest("page-click-react.spec.ts", "should not retarget when element is recycled on hover")]
    public async Task ShouldNotRetargetWhenElementIsRecycledOnHover()
    {
        await Page.GotoAsync(Server.Prefix + "/react.html");
        await Page.EvaluateAsync(@"() => {
                function shuffle() {
                    renderComponent(e('div', {}, [e(MyButton, { name: 'button2' }), e(MyButton, { name: 'button1' })] ));
                }
                renderComponent(e('div', {}, [e(MyButton, { name: 'button1', onHover: shuffle }), e(MyButton, { name: 'button2' })] ));
            }");

        await Page.ClickAsync("text=button1");
        Assert.Null(await Page.EvaluateAsync<bool?>("window.button1"));
        Assert.True(await Page.EvaluateAsync<bool?>("window.button2"));
    }

}
