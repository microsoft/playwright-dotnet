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

using System.Text.Json;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/elementhandle-bounding-box.spec.ts.</summary>
public class ElementhandleBoundingBoxTests
{
    [BeforeEach]
    public static void Skip(bool isAndroid) => Info.Skip(isAndroid);

    [UpstreamTest("EH-001 should work")]
    public async Task ShouldWork(IPage page, Server server, string browserName, bool headless, bool isLinux)
    {
        await page.SetViewportSizeAsync(500, 500);
        await page.GotoAsync(server.Prefix + "/grid.html");
        var elementHandle = await page.QuerySelectorAsync(".box:nth-of-type(13)");
        var box = await elementHandle.BoundingBoxAsync();
        ExpectToEqual(box, new { x = 100, y = 50, width = 50, height = 50 });
    }

    [UpstreamTest("EH-002 should handle nested frames")]
    public async Task ShouldHandleNestedFrames(IPage page, Server server)
    {
        await page.SetViewportSizeAsync(616, 500);
        await page.GotoAsync(server.Prefix + "/frames/nested-frames.html");
        var nestedFrame = page.FrameLocator("[name=\"2frames\"]").FrameLocator("[name=dos]");
        var elementHandle = await nestedFrame.Locator("div").ElementHandleAsync();
        var box = await elementHandle.BoundingBoxAsync();
        ExpectToEqual(box, new { x = 24, y = 224, width = 268, height = 18 });
    }

    [UpstreamTest("EH-003 should get frame box")]
    public async Task ShouldGetFrameBox(IPage page, string browserName)
    {
        await page.SetViewportSizeAsync(250, 250);
        await page.SetContentAsync(@"<style>
  body {
      display: flex;
      height: 500px;
      margin: 0px;
  }
  body iframe {
      flex-shrink: 1;
      border: 0;
      background-color: green;
  }
  </style>
  <iframe></iframe>
  ");
        var elementHandle = await page.QuerySelectorAsync("iframe");
        var box = await elementHandle.BoundingBoxAsync();
        ExpectToEqual(box, new { x = 0, y = 0, width = 300, height = 500 });
    }

    [UpstreamTest("EH-004 should handle scroll offset and click")]
    public async Task ShouldHandleScrollOffsetAndClick(IPage page, Server server)
    {
        await page.SetContentAsync(@"
    <style>* { margin: 0; padding: 0; }</style>
    <div style=""width:8000px; height:8000px;"">
      <div id=target style=""width:20px; height:20px; margin-left:230px; margin-top:340px;""
        onclick=""window.__clicked = true"">
      </div>
    </div>
  ");
        var elementHandle = await page.QuerySelectorAsync("#target");
        var box1 = await elementHandle.BoundingBoxAsync();
        await page.EvaluateAsync("() => window.scrollBy(200, 300)");
        var box2 = await elementHandle.BoundingBoxAsync();
        ExpectToEqual(box1, new { x = 230, y = 340, width = 20, height = 20 });
        ExpectToEqual(box2, new { x = 30, y = 40, width = 20, height = 20 });
        await page.Mouse.ClickAsync(box2.X + 10, box2.Y + 10);
        Assert.That(await page.EvaluateAsync<bool>("() => window['__clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("EH-005 should return null for invisible elements")]
    public async Task ShouldReturnNullForInvisibleElements(IPage page, Server server)
    {
        await page.SetContentAsync("<div style=\"display:none\">hi</div>");
        var element = await page.QuerySelectorAsync("div");
        Assert.That(await element.BoundingBoxAsync(), Is.Null);
    }

    [UpstreamTest("EH-006 should get bounding box of element inside a cross-origin iframe")]
    public async Task ShouldGetBoundingBoxOfElementInsideACrossOriginIframe(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync($@"
    <div style=""height:120px""></div>
    <iframe style=""border:0;margin-left:30px;width:300px;height:200px"" src=""{server.CrossProcessPrefix}/input/button.html""></iframe>
  ");
        var frame = page.Frames[1];
        var button = await frame.WaitForSelectorAsync("button");
        var iframeBox = await (await page.QuerySelectorAsync("iframe")).BoundingBoxAsync();
        var inner = await button.EvaluateAsync<JsonElement>("b => { const r = b.getBoundingClientRect(); return { x: r.left, y: r.top }; }");
        var box = await button.BoundingBoxAsync();
        Assert.That(Math.Round(box.X, MidpointRounding.AwayFromZero), Is.EqualTo(Math.Round(iframeBox.X + inner.GetProperty("x").GetDouble(), MidpointRounding.AwayFromZero)));
        Assert.That(Math.Round(box.Y, MidpointRounding.AwayFromZero), Is.EqualTo(Math.Round(iframeBox.Y + inner.GetProperty("y").GetDouble(), MidpointRounding.AwayFromZero)));
    }

    [UpstreamTest("EH-007 should force a layout")]
    public async Task ShouldForceALayout(IPage page, Server server)
    {
        await page.SetViewportSizeAsync(500, 500);
        await page.SetContentAsync("<div style=\"width: 100px; height: 100px\">hello</div>");
        var elementHandle = await page.QuerySelectorAsync("div");
        await page.EvaluateAsync("element => element.style.height = '200px'", elementHandle);
        var box = await elementHandle.BoundingBoxAsync();
        ExpectToEqual(box, new { x = 8, y = 8, width = 100, height = 200 });
    }

    [UpstreamTest("EH-008 should work with SVG nodes")]
    public async Task ShouldWorkWithSVGNodes(IPage page, Server server)
    {
        await page.SetContentAsync(@"
      <svg xmlns=""http://www.w3.org/2000/svg"" width=""500"" height=""500"">
        <rect id=""theRect"" x=""30"" y=""50"" width=""200"" height=""300""></rect>
      </svg>
    ");
        var element = await page.QuerySelectorAsync("#therect");
        var pwBoundingBox = await element.BoundingBoxAsync();
        var webBoundingBox = await page.EvaluateAsync<object>(@"e => {
    const rect = e.getBoundingClientRect();
    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
  }", element);
        ExpectToEqual(pwBoundingBox, webBoundingBox);
    }

    [UpstreamTest("EH-009 should work when inline box child is outside of viewport")]
    public async Task ShouldWorkWhenInlineBoxChildIsOutsideOfViewport(IPage page, Server server)
    {
        await page.SetContentAsync(@"
      <style>
      i {
        position: absolute;
        top: -1000px;
      }
      body {
        margin: 0;
        font-size: 12px;
      }
      </style>
      <span><i>woof</i><b>doggo</b></span>
    ");
        var handle = await page.QuerySelectorAsync("span");
        var box = await handle.BoundingBoxAsync();
        // .NET: the box of the page is read as an ElementHandleBoundingBoxResult, so that round() takes both boxes.
        var webBoundingBox = await handle.EvaluateAsync<ElementHandleBoundingBoxResult>(@"e => {
    const rect = e.getBoundingClientRect();
    return { x: rect.x, y: rect.y, width: rect.width, height: rect.height };
  }");
        Func<ElementHandleBoundingBoxResult, object> round = box => new
        {
            x = Math.Round(box.X * 100, MidpointRounding.AwayFromZero),
            y = Math.Round(box.Y * 100, MidpointRounding.AwayFromZero),
            width = Math.Round(box.Width * 100, MidpointRounding.AwayFromZero),
            height = Math.Round(box.Height * 100, MidpointRounding.AwayFromZero),
        };
        ExpectToEqual(round(box), round(webBoundingBox));
    }
}
