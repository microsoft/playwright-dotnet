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

public class ElementHandleContentFrameTests : PageTestEx
{
    [PlaywrightTest("elementhandle-content-frame.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var elementHandle = await Page.QuerySelectorAsync("#frame1");
        var frame = await elementHandle.ContentFrameAsync();
        Assert.AreEqual(Page.Frames.ElementAt(1), frame);
    }

    [PlaywrightTest("elementhandle-content-frame.spec.ts", "should work for cross-process iframes")]
    public async Task ShouldWorkForCrossProcessIframes()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.CrossProcessPrefix + "/empty.html");
        var elementHandle = await Page.QuerySelectorAsync("#frame1");
        var frame = await elementHandle.ContentFrameAsync();
        Assert.AreEqual(Page.Frames.ElementAt(1), frame);
    }

    [PlaywrightTest("elementhandle-content-frame.spec.ts", "should work for cross-frame evaluations")]
    public async Task ShouldWorkForCrossFrameEvaluations()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames.ElementAt(1);
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => window.top.document.querySelector('#frame1')");
        Assert.AreEqual(frame, await elementHandle.ContentFrameAsync());
    }

    [PlaywrightTest("elementhandle-content-frame.spec.ts", "should return null for non-iframes")]
    public async Task ShouldReturnNullForNonIframes()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames.ElementAt(1);
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
        Assert.Null(await elementHandle.ContentFrameAsync());
    }

    [PlaywrightTest("elementhandle-content-frame.spec.ts", "should return null for document.documentElement")]
    public async Task ShouldReturnNullForDocumentDocumentElement()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames.ElementAt(1);
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.documentElement");
        Assert.Null(await elementHandle.ContentFrameAsync());
    }
}
