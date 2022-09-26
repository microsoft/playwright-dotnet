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

public class PageDragTests : PageTestEx
{
    [PlaywrightTest("page-drag.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
        await Page.HoverAsync("#source");
        await Page.Mouse.DownAsync();
        await Page.HoverAsync("#target");
        await Page.Mouse.UpAsync();

        Assert.True(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"));
    }

    [PlaywrightTest("page-drag.spec.ts", "should allow specifying the position")]
    public async Task ShouldAllowSpecifyingThePosition()
    {
        await Page.SetContentAsync(@"
                <div style=""width:100px;height:100px;background:red;"" id=""red"">
                </div>
                <div style=""width:100px;height:100px;background:blue;"" id=""blue"">
                </div>
            ");
        var eventsHandle = await Page.EvaluateHandleAsync(@"() => {
                const events = [];
                document.getElementById('red').addEventListener('mousedown', event => {
                    events.push({
                    type: 'mousedown',
                    x: event.offsetX,
                    y: event.offsetY,
                    });
                });
                document.getElementById('blue').addEventListener('mouseup', event => {
                    events.push({
                    type: 'mouseup',
                    x: event.offsetX,
                    y: event.offsetY,
                    });
                });
                return events;
            }");
        await Page.DragAndDropAsync("#red", "#blue", new()
        {
            SourcePosition = new() { X = 34, Y = 7 },
            TargetPosition = new() { X = 10, Y = 20 },
        });
        PlaywrightAssert.AreJsonEqual(new[]
        {
                new { type = "mousedown", x = 34, y = 7 },
                new { type = "mouseup", x = 10, y = 20 },
            }, await eventsHandle.JsonValueAsync<object>());
    }

    [PlaywrightTest("page-drag.spec.ts", "should work with locators")]
    public async Task DragAndDropWithLocatorsShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
        await Page.Locator("#source").DragToAsync(Page.Locator("#target"));
        Assert.IsTrue(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))")); // could not find source in target
    }
}
