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

/// <summary>Port of tests/page/frame-evaluate.spec.ts.</summary>
public class FrameEvaluateTests : UpstreamPageTest
{
    [UpstreamTest("FR-001 should have different execution contexts")]
    public async Task ShouldHaveDifferentExecutionContexts()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        Assert.That(Page.Frames.Count, Is.EqualTo(2));
        await Page.Frames[0].EvaluateAsync("() => window['FOO'] = 'foo'");
        await Page.Frames[1].EvaluateAsync("() => window['FOO'] = 'bar'");
        Assert.That(await Page.Frames[0].EvaluateAsync<string>("() => window['FOO']"), Is.EqualTo("foo"));
        Assert.That(await Page.Frames[1].EvaluateAsync<string>("() => window['FOO']"), Is.EqualTo("bar"));
    }

    [UpstreamTest("FR-002 should have correct execution contexts @smoke")]
    public async Task ShouldHaveCorrectExecutionContexts()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        Assert.That(Page.Frames.Count, Is.EqualTo(2));
        Assert.That(await Page.Frames[0].EvaluateAsync<string>("() => document.body.textContent.trim()"), Is.EqualTo(""));
        Assert.That(await Page.Frames[1].EvaluateAsync<string>("() => document.body.textContent.trim()"), Is.EqualTo("Hi, I'm frame"));
    }

    [UpstreamTest("FR-005 should execute after cross-site navigation")]
    public async Task ShouldExecuteAfterCrossSiteNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var mainFrame = Page.MainFrame;
        Assert.That(await mainFrame.EvaluateAsync<string>("() => window.location.href"), Does.Contain(Server.EmptyPage));
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.That(await mainFrame.EvaluateAsync<string>("() => window.location.href"), Does.Contain(Server.CrossProcessPrefix));
    }

    [UpstreamTest("FR-006 should not allow cross-frame js handles")]
    public async Task ShouldNotAllowCrossFrameJsHandles()
    {
        // TODO: this should actually be possible because frames script each other,
        // but protocol implementations do not support this. For now, assume current
        // behavior.
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        var handle = await Page.EvaluateHandleAsync(@"() => {
    const iframe = document.querySelector('iframe');
    const foo = { bar: 'baz' };
    iframe.contentWindow['__foo'] = foo;
    return foo;
  }");
        var childFrame = Page.MainFrame.ChildFrames[0];
        var childResult = await childFrame.EvaluateAsync<object>("() => window['__foo']");
        ExpectToEqual(childResult, new { bar = "baz" });
        var error = await Catch(() => childFrame.EvaluateAsync("foo => foo.bar", handle));
        Assert.That(error.Message, Does.Contain("JSHandles can be evaluated only in the context they were created!"));
    }

    [UpstreamTest("FR-007 should allow cross-frame element handles")]
    public async Task ShouldAllowCrossFrameElementHandles()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        var bodyHandle = await Page.MainFrame.ChildFrames[0].QuerySelectorAsync("body");
        var result = await Page.EvaluateAsync<string>("body => body.innerHTML", bodyHandle);
        Assert.That(result.Trim(), Is.EqualTo("<div>Hi, I'm frame</div>"));
    }

    [UpstreamTest("FR-008 should not allow cross-frame element handles when frames do not script each other")]
    public async Task ShouldNotAllowCrossFrameElementHandlesWhenFramesDoNotScriptEachOther()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");

        await Page.GotoAsync(Server.EmptyPage);
        var frame = await AttachFrame(Page, "frame1", Server.CrossProcessPrefix + "/empty.html");
        var bodyHandle = await frame.QuerySelectorAsync("body");
        var error = await Catch(() => Page.EvaluateAsync("body => body.innerHTML", bodyHandle));
        Assert.That(error.Message, Does.Contain("Unable to adopt element handle from a different document"));
    }

    [UpstreamTest("FR-009 should throw for detached frames")]
    public async Task ShouldThrowForDetachedFrames()
    {
        var frame1 = await AttachFrame(Page, "frame1", Server.EmptyPage);
        await DetachFrame(Page, "frame1");
        Exception error = null;
        error = await Catch(() => frame1.EvaluateAsync("() => 7 * 8"));
        Assert.That(error.Message, Does.Contain("Frame was detached"));
    }

    [UpstreamTest("FR-010 should be isolated between frames")]
    public async Task ShouldBeIsolatedBetweenFrames()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        Assert.That(Page.Frames.Count, Is.EqualTo(2));
        // .NET: const [frame1, frame2] = page.frames();
        var (frame1, frame2) = (Page.Frames[0], Page.Frames[1]);
        Assert.That(frame1 != frame2, Is.True);

        await PromiseAll(
            frame1.EvaluateAsync("() => window['a'] = 1"),
            frame2.EvaluateAsync("() => window['a'] = 2"));
        var (a1, a2) = await PromiseAll(
            frame1.EvaluateAsync<int>("() => window['a']"),
            frame2.EvaluateAsync<int>("() => window['a']"));
        Assert.That(a1, Is.EqualTo(1));
        Assert.That(a2, Is.EqualTo(2));
    }

    [UpstreamTest("FR-011 should work in iframes that failed initial navigation")]
    public async Task ShouldWorkInIframesThatFailedInitialNavigation()
    {
        await Page.SetContentAsync(@"
    <meta http-equiv=""Content-Security-Policy"" content=""script-src 'none';"">
    <iframe src='javascript:""""'></iframe>
  ", new() { WaitUntil = WaitUntilState.DOMContentLoaded });
        await Page.EvaluateAsync(@"() => {
    const iframe = document.querySelector('iframe');
    const div = iframe.contentDocument.createElement('div');
    iframe.contentDocument.body.appendChild(div);
  }");
        Assert.That(Page.Frames[1].Url, Is.EqualTo(BrowserName == "webkit" ? "about:blank" : ""));
        // Main world should work.
        Assert.That(await Page.Frames[1].EvaluateAsync<string>("() => window.location.href"), Is.EqualTo("about:blank"));
        // Utility world should work.
        Assert.That(await Page.Frames[1].QuerySelectorAsync("div"), Is.Not.Null);
    }

    [UpstreamTest("FR-012 should work in iframes that interrupted initial javascript url navigation")]
    public async Task ShouldWorkInIframesThatInterruptedInitialJavascriptUrlNavigation()
    {
        // Chromium does not report isolated world for the iframe.
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'javascript:""""';
    document.body.appendChild(iframe);
    iframe.contentDocument.open();
    iframe.contentDocument.write('<div>hello</div>');
    iframe.contentDocument.close();
  }");
        // Main world should work.
        Assert.That(await Page.Frames[1].EvaluateAsync<string>("() => window.top.location.href"), Is.EqualTo(Server.EmptyPage));
        // Utility world should work.
        Assert.That(await Page.Frames[1].QuerySelectorAsync("div"), Is.Not.Null);
    }

    [UpstreamTest("FR-013 evaluateHandle should work")]
    public async Task EvaluateHandleShouldWork()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var mainFrame = Page.MainFrame;
        var windowHandle = await mainFrame.EvaluateHandleAsync("() => window");
        Assert.That(windowHandle, Is.Not.Null);
    }
}
