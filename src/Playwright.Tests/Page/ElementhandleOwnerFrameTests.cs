/**
 * Copyright 2018 Google Inc. All rights reserved.
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/elementhandle-owner-frame.spec.ts.</summary>
public class ElementhandleOwnerFrameTests
{
    [UpstreamTest("EH-055 should work")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.EmptyPage);
        var frame = page.Frames[1];
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
        Assert.That(await elementHandle.OwnerFrameAsync(), Is.SameAs(frame));
    }

    [UpstreamTest("EH-056 should work for cross-process iframes")]
    public async Task ShouldWorkForCrossProcessIframes(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.CrossProcessPrefix + "/empty.html");
        var frame = page.Frames[1];
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
        Assert.That(await elementHandle.OwnerFrameAsync(), Is.SameAs(frame));
    }

    [UpstreamTest("EH-057 should work for document")]
    public async Task ShouldWorkForDocument(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.EmptyPage);
        var frame = page.Frames[1];
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document");
        Assert.That(await elementHandle.OwnerFrameAsync(), Is.SameAs(frame));
    }

    [UpstreamTest("EH-058 should work for iframe elements")]
    public async Task ShouldWorkForIframeElements(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.EmptyPage);
        var frame = page.MainFrame;
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('#frame1')");
        Assert.That(await elementHandle.OwnerFrameAsync(), Is.SameAs(frame));
    }

    [UpstreamTest("EH-059 should work for cross-frame evaluations")]
    public async Task ShouldWorkForCrossFrameEvaluations(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.EmptyPage);
        var frame = page.MainFrame;
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.querySelector('iframe').contentWindow.document.body");
        Assert.That(await elementHandle.OwnerFrameAsync(), Is.SameAs(frame.ChildFrames[0]));
    }

    [UpstreamTest("EH-060 should work for detached elements")]
    public async Task ShouldWorkForDetachedElements(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var divHandle = (IElementHandle)await page.EvaluateHandleAsync(@"() => {
    const div = document.createElement('div');
    document.body.appendChild(div);
    return div;
  }");
        Assert.That(await divHandle.OwnerFrameAsync(), Is.SameAs(page.MainFrame));
        await page.EvaluateAsync(@"() => {
    const div = document.querySelector('div');
    document.body.removeChild(div);
  }");
        Assert.That(await divHandle.OwnerFrameAsync(), Is.SameAs(page.MainFrame));
    }

    [UpstreamTest("EH-061 should work for adopted elements")]
    public async Task ShouldWorkForAdoptedElements(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var (popup, _) = await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("url => window['__popup'] = window.open(url)", server.EmptyPage));
        var divHandle = (IElementHandle)await page.EvaluateHandleAsync(@"() => {
    const div = document.createElement('div');
    document.body.appendChild(div);
    return div;
  }");
        Assert.That(await divHandle.OwnerFrameAsync(), Is.SameAs(page.MainFrame));
        await popup.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        await page.EvaluateAsync(@"() => {
    const div = document.querySelector('div');
    window['__popup'].document.body.appendChild(div);
  }");
        Assert.That(await divHandle.OwnerFrameAsync(), Is.SameAs(popup.MainFrame));
    }
}
