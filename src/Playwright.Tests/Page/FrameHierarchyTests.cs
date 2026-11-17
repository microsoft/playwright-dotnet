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
public class FrameHierarchyTests
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
    public async Task ShouldHandleNestedFrames(IPage page, Server server, bool isAndroid, bool isBidi)
    {
        Info.Skip(isAndroid, "No cross-process on Android");
        Info.Skip(isBidi, "frame.name() is racy with BiDi");

        await page.GotoAsync(server.Prefix + "/frames/nested-frames.html");
        Assert.That(DumpFrames(page.MainFrame), Is.EqualTo(new[]
        {
            $"{server.Prefix}/frames/nested-frames.html",
            $"    {server.Prefix}/frames/frame.html (aframe)",
            $"    {server.Prefix}/frames/two-frames.html (2frames)",
            $"        {server.Prefix}/frames/frame.html (dos)",
            $"        {server.Prefix}/frames/frame.html (uno)",
        }));
    }

    [UpstreamTest("FR-025 should send events when frames are manipulated dynamically")]
    public async Task ShouldSendEventsWhenFramesAreManipulatedDynamically(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        // validate frameattached events
        var attachedFrames = new List<IFrame>();
        page.FrameAttached += (_, frame) => attachedFrames.Add(frame);
        await AttachFrame(page, "frame1", "./assets/frame.html");
        Assert.That(attachedFrames.Count, Is.EqualTo(1));
        Assert.That(attachedFrames[0].Url, Does.Contain("/assets/frame.html"));

        // validate framenavigated events
        var navigatedFrames = new List<IFrame>();
        page.FrameNavigated += (_, frame) => navigatedFrames.Add(frame);
        await page.EvaluateAsync(@"() => {
    const frame = document.getElementById('frame1');
    frame.src = './empty.html';
    return new Promise(x => frame.onload = x);
  }");
        Assert.That(navigatedFrames.Count, Is.EqualTo(1));
        Assert.That(navigatedFrames[0].Url, Is.EqualTo(server.EmptyPage));

        // validate framedetached events
        var detachedFrames = new List<IFrame>();
        page.FrameDetached += (_, frame) => detachedFrames.Add(frame);
        await DetachFrame(page, "frame1");
        Assert.That(detachedFrames.Count, Is.EqualTo(1));
        Assert.That(detachedFrames[0].IsDetached, Is.EqualTo(true));
    }

    [UpstreamTest("FR-026 should send \"framenavigated\" when navigating on anchor URLs")]
    public async Task ShouldSendFramenavigatedWhenNavigatingOnAnchorURLs(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await PromiseAll(
            page.GotoAsync(server.EmptyPage + "#foo"),
            Once<IFrame>(h => page.FrameNavigated += h, h => page.FrameNavigated -= h));
        Assert.That(page.Url, Is.EqualTo(server.EmptyPage + "#foo"));
    }

    [UpstreamTest("FR-027 should persist mainFrame on cross-process navigation")]
    public async Task ShouldPersistMainFrameOnCrossProcessNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var mainFrame = page.MainFrame;
        await page.GotoAsync(server.CrossProcessPrefix + "/empty.html");
        Assert.That(page.MainFrame == mainFrame, Is.True);
    }

    [UpstreamTest("FR-028 should not send attach/detach events for main frame")]
    public async Task ShouldNotSendAttachDetachEventsForMainFrame(IPage page, Server server)
    {
        var hasEvents = false;
        page.FrameAttached += (_, frame) => hasEvents = true;
        page.FrameDetached += (_, frame) => hasEvents = true;
        await page.GotoAsync(server.EmptyPage);
        Assert.That(hasEvents, Is.EqualTo(false));
    }

    [UpstreamTest("FR-029 should detach child frames on navigation")]
    public async Task ShouldDetachChildFramesOnNavigation(IPage page, Server server)
    {
        var attachedFrames = new List<IFrame>();
        var detachedFrames = new List<IFrame>();
        var navigatedFrames = new List<IFrame>();
        page.FrameAttached += (_, frame) => attachedFrames.Add(frame);
        page.FrameDetached += (_, frame) => detachedFrames.Add(frame);
        page.FrameNavigated += (_, frame) => navigatedFrames.Add(frame);
        await page.GotoAsync(server.Prefix + "/frames/nested-frames.html");
        Assert.That(attachedFrames.Count, Is.EqualTo(4));
        Assert.That(detachedFrames.Count, Is.EqualTo(0));
        Assert.That(navigatedFrames.Count, Is.EqualTo(5));

        attachedFrames = new List<IFrame>();
        detachedFrames = new List<IFrame>();
        navigatedFrames = new List<IFrame>();
        await page.GotoAsync(server.EmptyPage);
        Assert.That(attachedFrames.Count, Is.EqualTo(0));
        Assert.That(detachedFrames.Count, Is.EqualTo(4));
        Assert.That(navigatedFrames.Count, Is.EqualTo(1));
    }

    [UpstreamTest("FR-030 should support framesets")]
    public async Task ShouldSupportFramesets(IPage page, Server server)
    {
        var attachedFrames = new List<IFrame>();
        var detachedFrames = new List<IFrame>();
        var navigatedFrames = new List<IFrame>();
        page.FrameAttached += (_, frame) => attachedFrames.Add(frame);
        page.FrameDetached += (_, frame) => detachedFrames.Add(frame);
        page.FrameNavigated += (_, frame) => navigatedFrames.Add(frame);
        await page.GotoAsync(server.Prefix + "/frames/frameset.html");
        Assert.That(attachedFrames.Count, Is.EqualTo(4));
        Assert.That(detachedFrames.Count, Is.EqualTo(0));
        Assert.That(navigatedFrames.Count, Is.EqualTo(5));

        attachedFrames = new List<IFrame>();
        detachedFrames = new List<IFrame>();
        navigatedFrames = new List<IFrame>();
        await page.GotoAsync(server.EmptyPage);
        Assert.That(attachedFrames.Count, Is.EqualTo(0));
        Assert.That(detachedFrames.Count, Is.EqualTo(4));
        Assert.That(navigatedFrames.Count, Is.EqualTo(1));
    }

    [UpstreamTest("FR-031 should report frame from-inside shadow DOM")]
    public async Task ShouldReportFrameFromInsideShadowDOM(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/shadow.html");
        await page.EvaluateAsync(@"async url => {
    const frame = document.createElement('iframe');
    frame.src = url;
    document.body.shadowRoot.appendChild(frame);
    await new Promise(x => frame.onload = x);
  }", server.EmptyPage);
        Assert.That(page.Frames.Count, Is.EqualTo(2));
        Assert.That(page.Frames[1].Url, Is.EqualTo(server.EmptyPage));
    }

    [UpstreamTest("FR-032 should report frame.name()")]
    public async Task ShouldReportFrameName(IPage page, Server server, bool isBidi)
    {
        Info.Skip(isBidi, "frame.name() is racy with BiDi");

        await AttachFrame(page, "theFrameId", server.EmptyPage);
        await page.EvaluateAsync(@"url => {
    const frame = document.createElement('iframe');
    frame.name = 'theFrameName';
    frame.src = url;
    document.body.appendChild(frame);
    return new Promise(x => frame.onload = x);
  }", server.EmptyPage);
        Assert.That(page.Frames[0].Name, Is.EqualTo(""));
        Assert.That(page.Frames[1].Name, Is.EqualTo("theFrameId"));
        Assert.That(page.Frames[2].Name, Is.EqualTo("theFrameName"));
    }

    [UpstreamTest("FR-033 should report frame.parent()")]
    public async Task ShouldReportFrameParent(IPage page, Server server)
    {
        await AttachFrame(page, "frame1", server.EmptyPage);
        await AttachFrame(page, "frame2", server.EmptyPage);
        Assert.That(page.Frames[0].ParentFrame, Is.Null);
        Assert.That(page.Frames[1].ParentFrame, Is.SameAs(page.MainFrame));
        Assert.That(page.Frames[2].ParentFrame, Is.SameAs(page.MainFrame));
    }

    [UpstreamTest("FR-034 should report different frame instance when frame re-attaches")]
    public async Task ShouldReportDifferentFrameInstanceWhenFrameReAttaches(IPage page, Server server)
    {

        var frame1 = await AttachFrame(page, "frame1", server.EmptyPage);
        await page.EvaluateAsync(@"() => {
    window['frame'] = document.querySelector('#frame1');
    window['frame'].remove();
  }");
        Assert.That(frame1.IsDetached, Is.EqualTo(true));
        var (frame2, _) = await PromiseAll(
            Once<IFrame>(h => page.FrameAttached += h, h => page.FrameAttached -= h),
            page.EvaluateAsync("() => document.body.appendChild(window['frame'])"));
        Assert.That(frame2.IsDetached, Is.EqualTo(false));
        Assert.That(frame1, Is.Not.SameAs(frame2));
    }

    [UpstreamTest("FR-035 should refuse to display x-frame-options:deny iframe")]
    public async Task ShouldRefuseToDisplayXFrameOptionsDenyIframe(IPage page, Server server, string browserName)
    {
        Info.Skip(browserName == "firefox");

        server.SetRoute("/x-frame-options-deny.html", async (req, res) =>
        {
            res.SetHeader("Content-Type", "text/html");
            res.SetHeader("X-Frame-Options", "DENY");
            res.End("<!DOCTYPE html><html><head><title>login</title></head><body style=\"background-color: red;\"><p>dangerous login page</p></body></html>");
        });
        await page.GotoAsync(server.EmptyPage);
        var refusalText = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        page.Console += (_, msg) =>
        {
            if (Regex.IsMatch(msg.Text, "Refused to display", RegexOptions.IgnoreCase))
            {
                refusalText.TrySetResult(msg.Text);
            }
        };
        await page.SetContentAsync($"<iframe src=\"{server.CrossProcessPrefix}/x-frame-options-deny.html\"></iframe>");
        Assert.That(await refusalText.Task, Does.Match("Refused to display .* in a frame because it set 'X-Frame-Options' to 'deny'.").IgnoreCase);
    }

    [UpstreamTest("FR-036 should return frame.page()")]
    public async Task ShouldReturnFramePage(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        Assert.That(page.MainFrame.Page, Is.SameAs(page));
        Assert.That(page.MainFrame.ChildFrames[0].Page, Is.SameAs(page));
    }
}
