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

public class PageWaitForSelector1Tests : PageTestEx
{
    private const string AddElement = "tag => document.body.appendChild(document.createElement(tag))";

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should immediately resolve promise if node exists")]
    public async Task ShouldImmediatelyResolveTaskIfNodeExists()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame = Page.MainFrame;
        await frame.WaitForSelectorAsync("*");
        await frame.EvaluateAsync(AddElement, "div");
        await frame.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should immediately resolve if node exists")]
    public async Task ElementHandleWaitForSelectorShouldImmediatelyResolveIfNodeExists()
    {
        await Page.SetContentAsync("<span>extra</span><div><span>target</span></div>");
        var div = await Page.QuerySelectorAsync("div");
        var span = await div.WaitForSelectorAsync("span", new() { State = WaitForSelectorState.Attached });
        Assert.AreEqual("target", await span.EvaluateAsync<string>("e => e.textContent"));
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should wait")]
    public async Task ElementHandleWaitForSelectorShouldWait()
    {
        await Page.SetContentAsync("<div></div>");
        var div = await Page.QuerySelectorAsync("div");
        var task = div.WaitForSelectorAsync("span", new() { State = WaitForSelectorState.Attached });
        await div.EvaluateAsync("div => div.innerHTML = '<span>target</span>'");
        var span = await task;
        Assert.AreEqual("target", await span.EvaluateAsync<string>("e => e.textContent"));
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should timeout")]
    public async Task ElementHandleWaitForSelectorShouldTimeout()
    {
        await Page.SetContentAsync("<div></div>");
        var div = await Page.QuerySelectorAsync("div");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => div.WaitForSelectorAsync("span", new() { State = WaitForSelectorState.Attached, Timeout = 100 }));
        StringAssert.Contains("Timeout 100ms exceeded.", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "elementHandle.waitForSelector should throw on navigation")]
    public async Task ElementHandleWaitForSelectorShouldThrowOnNavigation()
    {
        await Page.SetContentAsync("<div></div>");
        var div = await Page.QuerySelectorAsync("div");
        var task = div.WaitForSelectorAsync("span");

        for (int i = 0; i < 10; i++)
        {
            await Page.EvaluateAsync("() => 1");
        }

        await Page.GotoAsync(Server.EmptyPage);
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => task);
        StringAssert.Contains("Error: frame navigated while waiting for selector", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should work with removed MutationObserver")]
    public async Task ShouldWorkWithRemovedMutationObserver()
    {
        await Page.EvaluateAsync("delete window.MutationObserver");
        var waitForSelector = Page.WaitForSelectorAsync(".zombo");

        await TaskUtils.WhenAll(
            waitForSelector,
            Page.SetContentAsync("<div class='zombo'>anything</div>"));

        Assert.AreEqual("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should resolve promise when node is added")]
    public async Task ShouldResolveTaskWhenNodeIsAdded()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame = Page.MainFrame;
        var watchdog = frame.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
        await frame.EvaluateAsync(AddElement, "br");
        await frame.EvaluateAsync(AddElement, "div");
        var eHandle = await watchdog;
        var property = await eHandle.GetPropertyAsync("tagName");
        string tagName = await property.JsonValueAsync<string>();
        Assert.AreEqual("DIV", tagName);
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should report logs while waiting for visible")]
    public async Task ShouldReportLogsWhileWaitingForVisible()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame = Page.MainFrame;
        var watchdog = frame.WaitForSelectorAsync("div", new() { Timeout = 5000 });

        await frame.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.className = 'foo bar';
              div.id = 'mydiv';
              div.setAttribute('style', 'display: none');
              div.setAttribute('foo', '123456789012345678901234567890123456789012345678901234567890');
              div.textContent = 'abcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvwyxz';
              document.body.appendChild(div);
            }");

        await GiveItTimeToLogAsync(frame);

        await frame.EvaluateAsync("() => document.querySelector('div').remove()");
        await GiveItTimeToLogAsync(frame);

        await frame.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.className = 'another';
              div.style.display = 'none';
              document.body.appendChild(div);
            }");
        await GiveItTimeToLogAsync(frame);

        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => watchdog);

        StringAssert.Contains("Timeout 5000ms", exception.Message);
        StringAssert.Contains("waiting for selector \"div\" to be visible", exception.Message);
        StringAssert.Contains("selector resolved to hidden <div id=\"mydiv\" class=\"foo bar\" foo=\"1234567890123456…>abcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvw…</div>", exception.Message);
        StringAssert.Contains("selector did not resolve to any element", exception.Message);
        StringAssert.Contains("selector resolved to hidden <div class=\"another\"></div>", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should report logs while waiting for hidden")]
    public async Task ShouldReportLogsWhileWaitingForHidden()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame = Page.MainFrame;

        await frame.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.className = 'foo bar';
              div.id = 'mydiv';
              div.textContent = 'hello';
              document.body.appendChild(div);
            }");

        var watchdog = frame.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
        await GiveItTimeToLogAsync(frame);

        await frame.EvaluateAsync(@"() => {
              document.querySelector('div').remove();
              const div = document.createElement('div');
              div.className = 'another';
              div.textContent = 'hello';
              document.body.appendChild(div);
            }");
        await GiveItTimeToLogAsync(frame);

        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => watchdog);

        StringAssert.Contains("Timeout 5000ms", exception.Message);
        StringAssert.Contains("waiting for selector \"div\" to be hidden", exception.Message);
        StringAssert.Contains("selector resolved to visible <div id=\"mydiv\" class=\"foo bar\">hello</div>", exception.Message);
        StringAssert.Contains("selector resolved to visible <div class=\"another\">hello</div>", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should resolve promise when node is added in shadow dom")]
    public async Task ShouldResolvePromiseWhenNodeIsAddedInShadowDom()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var watchdog = Page.WaitForSelectorAsync("span");

        await Page.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.attachShadow({mode: 'open'});
              document.body.appendChild(div);
            }");

        await Page.EvaluateAsync(@"() => new Promise(f => setTimeout(f, 100))");

        await Page.EvaluateAsync(@"() => {
              const span = document.createElement('span');
              span.textContent = 'Hello from shadow';
              document.querySelector('div').shadowRoot.appendChild(span);
            }");

        var handle = await watchdog;

        Assert.AreEqual("Hello from shadow", await handle.EvaluateAsync<string>("e => e.textContent"));
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should work when node is added through innerHTML")]
    public async Task ShouldWorkWhenNodeIsAddedThroughInnerHTML()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var watchdog = Page.WaitForSelectorAsync("h3 div", new() { State = WaitForSelectorState.Attached });
        await Page.EvaluateAsync(AddElement, "span");
        await Page.EvaluateAsync("document.querySelector('span').innerHTML = '<h3><div></div></h3>'");
        await watchdog;
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "Page.$ waitFor is shortcut for main frame")]
    public async Task PageWaitForSelectorAsyncIsShortcutForMainFrame()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var otherFrame = Page.FirstChildFrame();
        var watchdog = Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
        await otherFrame.EvaluateAsync(AddElement, "div");
        await Page.EvaluateAsync(AddElement, "div");
        var eHandle = await watchdog;
        Assert.AreEqual(Page.MainFrame, await eHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should run in specified frame")]
    public async Task ShouldRunInSpecifiedFrame()
    {
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame2", Server.EmptyPage);
        var frame1 = Page.FirstChildFrame();
        var frame2 = Page.Frames.ElementAt(2);
        var waitForSelectorPromise = frame2.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
        await frame1.EvaluateAsync(AddElement, "div");
        await frame2.EvaluateAsync(AddElement, "div");
        var eHandle = await waitForSelectorPromise;
        Assert.AreEqual(frame2, await eHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("page-wait-for-selector-1.spec.ts", "should throw when frame is detached")]
    public async Task ShouldThrowWhenFrameIsDetached()
    {
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.FirstChildFrame();
        var waitTask = frame.WaitForSelectorAsync(".box").ContinueWith(task => task.Exception?.InnerException);
        await FrameUtils.DetachFrameAsync(Page, "frame1");
        var waitException = await waitTask;
        Assert.NotNull(waitException);
        StringAssert.Contains("Frame was detached", waitException.Message);
    }

    private async Task GiveItTimeToLogAsync(IFrame frame)
    {
        await frame.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
        await frame.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
    }
}
