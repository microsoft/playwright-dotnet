/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
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

/// <playwright-file>tap.spec.ts</playwright-file>
public sealed class TapTests : PageTestEx
{
    public override BrowserNewContextOptions ContextOptions()
    {
        return new() { HasTouch = true };
    }

    [PlaywrightTest("tap.spec.ts", "should send all of the correct events")]
    public async Task ShouldSendAllOfTheCorrectEvents()
    {
        await Page.SetContentAsync(
            @"<div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>
                <div id=""b"" style=""background: pink; width: 50px; height: 50px"">b</div>");

        await Page.TapAsync("#a");
        var handle = await TrackEventsAsync("#b");
        await Page.TapAsync("#b");

        string[] result = await handle.JsonValueAsync<string[]>();

        Assert.AreEqual(result, new string[]
        {
                "pointerover",  "pointerenter",
                "pointerdown",  "touchstart",
                "pointerup",    "pointerout",
                "pointerleave", "touchend",
                "mouseover",    "mouseenter",
                "mousemove",    "mousedown",
                "mouseup",      "click",
        });
    }

    [PlaywrightTest("tap.spec.ts", "trial run should not tap")]
    public async Task TrialRunShouldNotTap()
    {
        await Page.SetContentAsync(
            @"<div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>
                <div id=""b"" style=""background: pink; width: 50px; height: 50px"">b</div>");

        await Page.TapAsync("#a");
        var handle = await TrackEventsAsync("#b");
        await Page.TapAsync("#b", new() { Trial = true });

        string[] result = await handle.JsonValueAsync<string[]>();
        Assert.AreEqual(result, new string[] { "pointerover", "pointerenter", "pointerout", "pointerleave" });
    }

    [PlaywrightTest("tap.spec.ts", "should not send mouse events touchstart is canceled")]
    public async Task ShouldNotSendMouseEventsTouchStartIsCanceled()
    {
        await Page.SetContentAsync(@"<div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>");
        await Page.EvaluateAsync(
            @"() => {
                    document.addEventListener('touchstart', t => t.preventDefault(), {passive: false});
                }");

        var handle = await TrackEventsAsync("div");
        await Page.TapAsync("div");

        string[] result = await handle.JsonValueAsync<string[]>();

        Assert.AreEqual(result, new string[]
        {
                "pointerover",  "pointerenter",
                "pointerdown",  "touchstart",
                "pointerup",    "pointerout",
                "pointerleave", "touchend",
        });
    }

    [PlaywrightTest("tap.spec.ts", "should not send mouse events when touchend is canceled")]
    public async Task ShouldNotSendMouseEventsWhenTouchEndIsCanceled()
    {
        await Page.SetContentAsync(@"<div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>");
        await Page.EvaluateAsync(
            @"() => {
                    document.addEventListener('touchend', t => t.preventDefault());
                }");

        var handle = await TrackEventsAsync("div");
        await Page.TapAsync("div");

        string[] result = await handle.JsonValueAsync<string[]>();

        Assert.AreEqual(result, new string[]
        {
                "pointerover",  "pointerenter",
                "pointerdown",  "touchstart",
                "pointerup",    "pointerout",
                "pointerleave", "touchend",
        });
    }

    [PlaywrightTest("tap.spec.ts", "should wait for a navigation caused by a tap")]
    public async Task ShouldWaitForANavigationCausedByATap()
    {
        var requestResponse = new TaskCompletionSource<bool>();
        string route = "/intercept-this.html";
        await Page.GotoAsync(Server.EmptyPage);
        Server.SetRoute(route, _ =>
        {
            requestResponse.SetResult(true);
            return requestResponse.Task;
        });

        await Page.SetContentAsync($@"<a href=""{route}"">link</a>");
        bool loaded = false;
        var awaitTask = Page.TapAsync("a").ContinueWith(_ =>
        {
            // this shouldn't happen before the request is called
            Assert.True(requestResponse.Task.IsCompleted);

            // and make sure this hasn't been set
            Assert.False(loaded);
            loaded = true;
        });

        await awaitTask;
        await requestResponse.Task;
        Assert.True(loaded);
    }

    [PlaywrightTest("tap.spec.ts", "should work with modifiers")]
    public async Task ShouldWorkWithModifiers()
    {
        await Page.SetContentAsync("hello world");

        var altKeyTask = Page.EvaluateAsync<bool>(@"() =>
                   new Promise(resolve => {
                        document.addEventListener('touchstart', event => {
                          resolve(event.altKey);
                        }, { passive: false })
                    })");

        await Page.EvaluateAsync("() => void 0");
        await Page.TapAsync("body", new() { Modifiers = new[] { KeyboardModifier.Alt } });
        Assert.True((await altKeyTask));
    }

    [PlaywrightTest("tap.spec.ts", "should send well formed touch points")]
    public async Task ShouldSendWellFormedTouchPoints()
    {
        var touchStartTask = Page.EvaluateAsync<dynamic>(@"() =>
                new Promise(resolve => {
                    document.addEventListener('touchstart', event => {
                        resolve([...event.touches].map(t => ({
                            identifier: t.identifier,
                            clientX: t.clientX,
                            clientY: t.clientY,
                            pageX: t.pageX,
                            pageY: t.pageY,
                            radiusX: 'radiusX' in t ? t.radiusX : t['webkitRadiusX'],
                            radiusY: 'radiusY' in t ? t.radiusY : t['webkitRadiusY'],
                            rotationAngle: 'rotationAngle' in t ? t.rotationAngle : t['webkitRotationAngle'],
                            force: 'force' in t ? t.force : t['webkitForce'],
                        })));
                    }, false);
                })");

        var touchEndTask = Page.EvaluateAsync<dynamic>(@"() =>
                new Promise(resolve => {
                    document.addEventListener('touchend', event => {
                        resolve([...event.touches].map(t => ({
                            identifier: t.identifier,
                            clientX: t.clientX,
                            clientY: t.clientY,
                            pageX: t.pageX,
                            pageY: t.pageY,
                            radiusX: 'radiusX' in t ? t.radiusX : t['webkitRadiusX'],
                            radiusY: 'radiusY' in t ? t.radiusY : t['webkitRadiusY'],
                            rotationAngle: 'rotationAngle' in t ? t.rotationAngle : t['webkitRotationAngle'],
                            force: 'force' in t ? t.force : t['webkitForce'],
                        })));
                    }, false);
                })");

        await Page.EvaluateAsync("() => void 0");

        await Page.Touchscreen.TapAsync(40, 60);
        var touchStartResult = (await touchStartTask)[0];
        var touchEndResult = await touchEndTask;

        Assert.AreEqual(new object[] { }, touchEndResult);
        Assert.AreEqual(40, touchStartResult.clientX);
        Assert.AreEqual(60, touchStartResult.clientY);
        Assert.AreEqual(1, touchStartResult.force);
        Assert.AreEqual(0, touchStartResult.identifier);
        Assert.AreEqual(40, touchStartResult.pageX);
        Assert.AreEqual(60, touchStartResult.pageY);
        Assert.AreEqual(1, touchStartResult.radiusX);
        Assert.AreEqual(1, touchStartResult.radiusY);
        Assert.AreEqual(0, touchStartResult.rotationAngle);
    }

    [PlaywrightTest("tap.spec.ts", "should wait until an element is visible to tap it")]
    public async Task ShouldWaitUntilAnElementIsVisibleToTapIt()
    {
        var div = (IElementHandle)await Page.EvaluateHandleAsync(@"() => {
                const button = document.createElement('button');
                button.textContent = 'not clicked';
                document.body.appendChild(button);
                button.style.display = 'none';
                return button;
            }");

        var tapTask = div.TapAsync();

        await div.EvaluateAsync(@"div => div.onclick = () => div.textContent = 'clicked'");
        await div.EvaluateAsync(@"div => div.style.display = 'block'");

        await tapTask;

        Assert.AreEqual("clicked", await div.TextContentAsync());
    }

    private async Task<IJSHandle> TrackEventsAsync(string selector)
    {
        var target = await Page.QuerySelectorAsync(selector);
        string jsFunc = @"(target) => {
                const events = [];
                for(const event of [
                    'mousedown', 'mouseenter', 'mouseleave', 'mousemove', 'mouseout', 'mouseover', 'mouseup', 'click',
                    'pointercancel', 'pointerdown', 'pointerenter', 'pointerleave', 'pointermove', 'pointerout', 'pointerover', 'pointerup',
                    'touchstart', 'touchend', 'touchmove', 'touchcancel',])
                        target.addEventListener(event, () => events.push(event), false);
                    return events;
                }";

        return await target.EvaluateHandleAsync(jsFunc);
    }
}
