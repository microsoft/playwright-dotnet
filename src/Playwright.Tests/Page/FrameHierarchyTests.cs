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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/frame-hierarchy.spec.ts.</summary>
public class FrameHierarchyTests : UpstreamPageTest
{
    private static List<string> DumpFrames(IFrame frame, string indentation = "")
    {
        var description = frame.Url;
        if (!string.IsNullOrEmpty(frame.Name))
        {
            description += " (" + frame.Name + ")";
        }
        var result = new List<string> { indentation + description };
        var childFrames = frame.ChildFrames.ToList();
        childFrames.Sort((a, b) =>
        {
            if (a.Url != b.Url)
            {
                return string.CompareOrdinal(a.Url, b.Url) < 0 ? -1 : 1;
            }
            return string.CompareOrdinal(a.Name, b.Name) < 0 ? -1 : 1;
        });
        foreach (var child in childFrames)
        {
            result.AddRange(DumpFrames(child, "    " + indentation));
        }
        return result;
    }

    [UpstreamTest("FR-024 should handle nested frames @smoke")]
    public async Task ShouldHandleNestedFrames()
    {
        Info.Skip(IsAndroid, "No cross-process on Android");
        Info.Skip(IsBidi, "frame.name() is racy with BiDi");

        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        Assert.That(DumpFrames(Page.MainFrame), Is.EqualTo(new[]
        {
            $"{Server.Prefix}/frames/nested-frames.html",
            $"    {Server.Prefix}/frames/frame.html (aframe)",
            $"    {Server.Prefix}/frames/two-frames.html (2frames)",
            $"        {Server.Prefix}/frames/frame.html (dos)",
            $"        {Server.Prefix}/frames/frame.html (uno)",
        }));
    }

    [UpstreamTest("FR-025 should send events when frames are manipulated dynamically")]
    public async Task ShouldSendEventsWhenFramesAreManipulatedDynamically()
    {
        await Page.GotoAsync(Server.EmptyPage);
        // validate frameattached events
        var attachedFrames = new List<IFrame>();
        Page.FrameAttached += (_, frame) => attachedFrames.Add(frame);
        await AttachFrame(Page, "frame1", "./assets/frame.html");
        Assert.That(attachedFrames.Count, Is.EqualTo(1));
        Assert.That(attachedFrames[0].Url, Does.Contain("/assets/frame.html"));

        // validate framenavigated events
        var navigatedFrames = new List<IFrame>();
        Page.FrameNavigated += (_, frame) => navigatedFrames.Add(frame);
        await Page.EvaluateAsync(@"() => {
    const frame = document.getElementById('frame1');
    frame.src = './empty.html';
    return new Promise(x => frame.onload = x);
  }");
        Assert.That(navigatedFrames.Count, Is.EqualTo(1));
        Assert.That(navigatedFrames[0].Url, Is.EqualTo(Server.EmptyPage));

        // validate framedetached events
        var detachedFrames = new List<IFrame>();
        Page.FrameDetached += (_, frame) => detachedFrames.Add(frame);
        await DetachFrame(Page, "frame1");
        Assert.That(detachedFrames.Count, Is.EqualTo(1));
        Assert.That(detachedFrames[0].IsDetached, Is.EqualTo(true));
    }

    [UpstreamTest("FR-026 should send \"framenavigated\" when navigating on anchor URLs")]
    public async Task ShouldSendFramenavigatedWhenNavigatingOnAnchorURLs()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await PromiseAll(
            Page.GotoAsync(Server.EmptyPage + "#foo"),
            Once<IFrame>(h => Page.FrameNavigated += h, h => Page.FrameNavigated -= h));
        Assert.That(Page.Url, Is.EqualTo(Server.EmptyPage + "#foo"));
    }

    [UpstreamTest("FR-027 should persist mainFrame on cross-process navigation")]
    public async Task ShouldPersistMainFrameOnCrossProcessNavigation()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var mainFrame = Page.MainFrame;
        await Page.GotoAsync(Server.CrossProcessPrefix + "/empty.html");
        Assert.That(Page.MainFrame == mainFrame, Is.True);
    }

    [UpstreamTest("FR-028 should not send attach/detach events for main frame")]
    public async Task ShouldNotSendAttachDetachEventsForMainFrame()
    {
        var hasEvents = false;
        Page.FrameAttached += (_, frame) => hasEvents = true;
        Page.FrameDetached += (_, frame) => hasEvents = true;
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(hasEvents, Is.EqualTo(false));
    }

    [UpstreamTest("FR-029 should detach child frames on navigation")]
    public async Task ShouldDetachChildFramesOnNavigation()
    {
        var attachedFrames = new List<IFrame>();
        var detachedFrames = new List<IFrame>();
        var navigatedFrames = new List<IFrame>();
        Page.FrameAttached += (_, frame) => attachedFrames.Add(frame);
        Page.FrameDetached += (_, frame) => detachedFrames.Add(frame);
        Page.FrameNavigated += (_, frame) => navigatedFrames.Add(frame);
        await Page.GotoAsync(Server.Prefix + "/frames/nested-frames.html");
        Assert.That(attachedFrames.Count, Is.EqualTo(4));
        Assert.That(detachedFrames.Count, Is.EqualTo(0));
        Assert.That(navigatedFrames.Count, Is.EqualTo(5));

        attachedFrames = new List<IFrame>();
        detachedFrames = new List<IFrame>();
        navigatedFrames = new List<IFrame>();
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(attachedFrames.Count, Is.EqualTo(0));
        Assert.That(detachedFrames.Count, Is.EqualTo(4));
        Assert.That(navigatedFrames.Count, Is.EqualTo(1));
    }

    [UpstreamTest("FR-030 should support framesets")]
    public async Task ShouldSupportFramesets()
    {
        var attachedFrames = new List<IFrame>();
        var detachedFrames = new List<IFrame>();
        var navigatedFrames = new List<IFrame>();
        Page.FrameAttached += (_, frame) => attachedFrames.Add(frame);
        Page.FrameDetached += (_, frame) => detachedFrames.Add(frame);
        Page.FrameNavigated += (_, frame) => navigatedFrames.Add(frame);
        await Page.GotoAsync(Server.Prefix + "/frames/frameset.html");
        Assert.That(attachedFrames.Count, Is.EqualTo(4));
        Assert.That(detachedFrames.Count, Is.EqualTo(0));
        Assert.That(navigatedFrames.Count, Is.EqualTo(5));

        attachedFrames = new List<IFrame>();
        detachedFrames = new List<IFrame>();
        navigatedFrames = new List<IFrame>();
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(attachedFrames.Count, Is.EqualTo(0));
        Assert.That(detachedFrames.Count, Is.EqualTo(4));
        Assert.That(navigatedFrames.Count, Is.EqualTo(1));
    }

    [UpstreamTest("FR-031 should report frame from-inside shadow DOM")]
    public async Task ShouldReportFrameFromInsideShadowDOM()
    {
        await Page.GotoAsync(Server.Prefix + "/shadow.html");
        await Page.EvaluateAsync(@"async url => {
    const frame = document.createElement('iframe');
    frame.src = url;
    document.body.shadowRoot.appendChild(frame);
    await new Promise(x => frame.onload = x);
  }", Server.EmptyPage);
        Assert.That(Page.Frames.Count, Is.EqualTo(2));
        Assert.That(Page.Frames[1].Url, Is.EqualTo(Server.EmptyPage));
    }

    [UpstreamTest("FR-032 should report frame.name()")]
    public async Task ShouldReportFrameName()
    {
        Info.Skip(IsBidi, "frame.name() is racy with BiDi");

        await AttachFrame(Page, "theFrameId", Server.EmptyPage);
        await Page.EvaluateAsync(@"url => {
    const frame = document.createElement('iframe');
    frame.name = 'theFrameName';
    frame.src = url;
    document.body.appendChild(frame);
    return new Promise(x => frame.onload = x);
  }", Server.EmptyPage);
        Assert.That(Page.Frames[0].Name, Is.EqualTo(""));
        Assert.That(Page.Frames[1].Name, Is.EqualTo("theFrameId"));
        Assert.That(Page.Frames[2].Name, Is.EqualTo("theFrameName"));
    }

    [UpstreamTest("FR-033 should report frame.parent()")]
    public async Task ShouldReportFrameParent()
    {
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        await AttachFrame(Page, "frame2", Server.EmptyPage);
        Assert.That(Page.Frames[0].ParentFrame, Is.Null);
        Assert.That(Page.Frames[1].ParentFrame, Is.SameAs(Page.MainFrame));
        Assert.That(Page.Frames[2].ParentFrame, Is.SameAs(Page.MainFrame));
    }

    [UpstreamTest("FR-034 should report different frame instance when frame re-attaches")]
    public async Task ShouldReportDifferentFrameInstanceWhenFrameReAttaches()
    {

        var frame1 = await AttachFrame(Page, "frame1", Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
    window['frame'] = document.querySelector('#frame1');
    window['frame'].remove();
  }");
        Assert.That(frame1.IsDetached, Is.EqualTo(true));
        var (frame2, _) = await PromiseAll(
            Once<IFrame>(h => Page.FrameAttached += h, h => Page.FrameAttached -= h),
            Page.EvaluateAsync("() => document.body.appendChild(window['frame'])"));
        Assert.That(frame2.IsDetached, Is.EqualTo(false));
        Assert.That(frame1, Is.Not.SameAs(frame2));
    }

    [UpstreamTest("FR-035 should refuse to display x-frame-options:deny iframe")]
    public async Task ShouldRefuseToDisplayXFrameOptionsDenyIframe()
    {
        Info.Skip(BrowserName == "firefox");

        Server.SetRoute("/x-frame-options-deny.html", async (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.SetHeader("X-Frame-Options", "DENY");
            res.End("<!DOCTYPE html><html><head><title>login</title></head><body style=\"background-color: red;\"><p>dangerous login page</p></body></html>");
        });
        await Page.GotoAsync(Server.EmptyPage);
        var refusalText = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        Page.Console += (_, msg) =>
        {
            if (Regex.IsMatch(msg.Text, "Refused to display", RegexOptions.IgnoreCase))
            {
                refusalText.TrySetResult(msg.Text);
            }
        };
        await Page.SetContentAsync($"<iframe src=\"{Server.CrossProcessPrefix}/x-frame-options-deny.html\"></iframe>");
        Assert.That(await refusalText.Task, Does.Match("Refused to display .* in a frame because it set 'X-Frame-Options' to 'deny'.").IgnoreCase);
    }

    [UpstreamTest("FR-036 should return frame.page()")]
    public async Task ShouldReturnFramePage()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        Assert.That(Page.MainFrame.Page, Is.SameAs(Page));
        Assert.That(Page.MainFrame.ChildFrames[0].Page, Is.SameAs(Page));
    }
}
