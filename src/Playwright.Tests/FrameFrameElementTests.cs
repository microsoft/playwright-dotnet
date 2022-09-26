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

public class FrameFrameElementTests : PageTestEx
{
    [PlaywrightTest("frame-frame-element.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame2", Server.EmptyPage);
        var frame3 = await FrameUtils.AttachFrameAsync(Page, "frame3", Server.EmptyPage);

        var frame1handle1 = await Page.QuerySelectorAsync("#frame1");
        var frame1handle2 = await frame1.FrameElementAsync();
        var frame3handle1 = await Page.QuerySelectorAsync("#frame3");
        var frame3handle2 = await frame3.FrameElementAsync();

        Assert.True(await frame1handle1.EvaluateAsync<bool>("(a, b) => a === b", frame1handle2));
        Assert.True(await frame3handle1.EvaluateAsync<bool>("(a, b) => a === b", frame3handle2));
        Assert.False(await frame1handle1.EvaluateAsync<bool>("(a, b) => a === b", frame3handle2));

        var windowHandle = await Page.MainFrame.EvaluateHandleAsync("() => window");
        Assert.NotNull(windowHandle);
    }

    [PlaywrightTest("frame-frame-element.spec.ts", "should work with contentFrame")]
    public async Task ShouldWorkWithContentFrame()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var handle = await frame.FrameElementAsync();
        var contentFrame = await handle.ContentFrameAsync();

        Assert.AreEqual(contentFrame, frame);
    }

    [PlaywrightTest("frame-frame-element.spec.ts", "should throw when detached")]
    public async Task ShouldThrowWhenDetached()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame1 = await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        await Page.EvalOnSelectorAsync("#frame1", "e => e.remove()");

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => frame1.FrameElementAsync());

        Assert.AreEqual("Frame has been detached.", exception.Message);
    }
}
