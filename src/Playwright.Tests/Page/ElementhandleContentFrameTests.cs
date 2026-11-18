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

/// <summary>Port of tests/page/elementhandle-content-frame.spec.ts.</summary>
public class ElementhandleContentFrameTests : UpstreamPageTest
{
    [UpstreamTest("EH-019 should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        var elementHandle = await Page.QuerySelectorAsync("#frame1");
        var frame = await elementHandle.ContentFrameAsync();
        Assert.That(frame, Is.SameAs(Page.Frames[1]));
    }

    [UpstreamTest("EH-020 should work for cross-process iframes")]
    public async Task ShouldWorkForCrossProcessIframes()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.CrossProcessPrefix + "/empty.html");
        var elementHandle = await Page.QuerySelectorAsync("#frame1");
        var frame = await elementHandle.ContentFrameAsync();
        Assert.That(frame, Is.SameAs(Page.Frames[1]));
    }

    [UpstreamTest("EH-021 should work for cross-frame evaluations")]
    public async Task ShouldWorkForCrossFrameEvaluations()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames[1];
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => window.top.document.querySelector('#frame1')");
        Assert.That(await elementHandle.ContentFrameAsync(), Is.SameAs(frame));
    }

    [UpstreamTest("EH-022 should return null for non-iframes")]
    public async Task ShouldReturnNullForNonIframes()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames[1];
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.body");
        Assert.That(await elementHandle.ContentFrameAsync(), Is.Null);
    }

    [UpstreamTest("EH-023 should return null for document.documentElement")]
    public async Task ShouldReturnNullForDocumentDocumentElement()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames[1];
        var elementHandle = (IElementHandle)await frame.EvaluateHandleAsync("() => document.documentElement");
        Assert.That(await elementHandle.ContentFrameAsync(), Is.Null);
    }
}
