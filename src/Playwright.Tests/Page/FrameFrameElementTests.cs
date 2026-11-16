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

/// <summary>Port of tests/page/frame-frame-element.spec.ts.</summary>
public class FrameFrameElementTests
{
    [UpstreamTest("FR-014 should work @smoke")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame1 = await AttachFrame(page, "frame1", server.EmptyPage);
        await AttachFrame(page, "frame2", server.EmptyPage);
        var frame3 = await AttachFrame(page, "frame3", server.EmptyPage);
        var frame1handle1 = await page.QuerySelectorAsync("#frame1");
        var frame1handle2 = await frame1.FrameElementAsync();
        var frame3handle1 = await page.QuerySelectorAsync("#frame3");
        var frame3handle2 = await frame3.FrameElementAsync();
        Assert.That(await frame1handle1.EvaluateAsync<bool>("(a, b) => a === b", frame1handle2), Is.EqualTo(true));
        Assert.That(await frame3handle1.EvaluateAsync<bool>("(a, b) => a === b", frame3handle2), Is.EqualTo(true));
        Assert.That(await frame1handle1.EvaluateAsync<bool>("(a, b) => a === b", frame3handle1), Is.EqualTo(false));
    }

    [UpstreamTest("FR-015 should work with contentFrame")]
    public async Task ShouldWorkWithContentFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame = await AttachFrame(page, "frame1", server.EmptyPage);
        var handle = await frame.FrameElementAsync();
        var contentFrame = await handle.ContentFrameAsync();
        Assert.That(contentFrame, Is.SameAs(frame));
    }

    [UpstreamTest("FR-016 should work with frameset")]
    public async Task ShouldWorkWithFrameset(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/frameset.html");
        var frameElement1 = await page.QuerySelectorAsync("frame");
        var frame = await frameElement1.ContentFrameAsync();
        var frameElement2 = await frame.FrameElementAsync();
        Assert.That(await frameElement1.EvaluateAsync<bool>("(a, b) => a === b", frameElement2), Is.EqualTo(true));
    }

    [UpstreamTest("FR-017 should throw when detached")]
    public async Task ShouldThrowWhenDetached(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame1 = await AttachFrame(page, "frame1", server.EmptyPage);
        await page.EvalOnSelectorAsync("#frame1", "e => e.remove()");
        var error = await Catch(() => frame1.FrameElementAsync());
        Assert.That(error.Message, Does.Contain("Frame has been detached."));
    }

    [UpstreamTest("FR-018 should work inside closed shadow root")]
    public async Task ShouldWorkInsideClosedShadowRoot(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <div id=framecontainer>
    </div>
    <script>
      const iframe = document.createElement('iframe');
      iframe.setAttribute('name', 'myframe');
      iframe.setAttribute('srcdoc', 'find me');
      const div = document.getElementById('framecontainer');
      const host = div.attachShadow({ mode: 'closed' });
      host.appendChild(iframe);
    </script>
  ");

        var frame = page.Frames[1];
        var element = await frame.FrameElementAsync();
        Assert.That(await element.GetAttributeAsync("name"), Is.EqualTo("myframe"));
    }

    [UpstreamTest("FR-019 should work inside declarative shadow root")]
    public async Task ShouldWorkInsideDeclarativeShadowRoot(IPage page, Server server, string browserName)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <div>
      <template shadowrootmode=""open"">
        <iframe name=""myframe"" srcdoc=""<h1>Hi!</h1>""></iframe>
        <slot></slot>
      </template>
      <span>footer</span>
    </div>
  ");
        var frame = page.Frames[1];
        var element = await frame.FrameElementAsync();
        Assert.That(await element.GetAttributeAsync("name"), Is.EqualTo("myframe"));
    }
}
