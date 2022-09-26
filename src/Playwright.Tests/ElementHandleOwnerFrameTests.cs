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

public class ElementHandleOwnerFrameTests : PageTestEx
{
    [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames.ElementAt(1);
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
        Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for cross-process iframes")]
    public async Task ShouldWorkForCrossProcessIframes()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.CrossProcessPrefix + "/empty.html");
        var frame = Page.Frames.ElementAt(1);
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
        Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for document")]
    public async Task ShouldWorkForDocument()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames.ElementAt(1);
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document");
        Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for iframe elements")]
    public async Task ShouldWorkForIframeElements()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.MainFrame;
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('#frame1')");
        Assert.AreEqual(frame, await elementHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for cross-frame evaluations")]
    public async Task ShouldWorkForCrossFrameEvaluations()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.MainFrame;
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('#frame1').contentWindow.document.body");
        Assert.AreEqual(frame.ChildFrames.First(), await elementHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for detached elements")]
    public async Task ShouldWorkForDetachedElements()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var divHandle = (IElementHandle)await Page.EvaluateHandleAsync(@"() => {
                    var div = document.createElement('div');
                    document.body.appendChild(div);
                    return div;
                }");
        Assert.AreEqual(Page.MainFrame, await divHandle.OwnerFrameAsync());
        await Page.EvaluateAsync(@"() => {
                    var div = document.querySelector('div');
                    document.body.removeChild(div);
                }");
        Assert.AreEqual(Page.MainFrame, await divHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("elementhandle-owner-frame.spec.ts", "should work for adopted elements")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldWorkForAdoptedElements()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var popupTask = Page.WaitForPopupAsync();
        await TaskUtils.WhenAll(
          popupTask,
          Page.EvaluateAsync("url => window.__popup = window.open(url)", Server.EmptyPage));
        var popup = await popupTask;
        var divHandle = (IElementHandle)await Page.EvaluateHandleAsync(@"() => {
                    var div = document.createElement('div');
                    document.body.appendChild(div);
                    return div;
                }");
        Assert.AreEqual(Page.MainFrame, await divHandle.OwnerFrameAsync());
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await Page.EvaluateAsync(@"() => {
                    var div = document.querySelector('div');
                    window.__popup.document.body.appendChild(div);
                }");
        Assert.AreEqual(popup.MainFrame, await divHandle.OwnerFrameAsync());
    }
}
