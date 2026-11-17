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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-mouse.spec.ts.</summary>
public class PageMouseTests : UpstreamPageTest
{
    private const string dimensions = @"function dimensions() {
  const rect = document.querySelector('textarea').getBoundingClientRect();
  return {
    x: rect.left,
    y: rect.top,
    width: rect.width,
    height: rect.height
  };
}";

    [UpstreamTest("MO-001 should click the document @smoke")]
    public async Task ShouldClickTheDocument()
    {
        await Page.EvaluateAsync(@"() => {
    window['clickPromise'] = new Promise(resolve => {
      document.addEventListener('click', event => {
        resolve({
          type: event.type,
          detail: event.detail,
          clientX: event.clientX,
          clientY: event.clientY,
          isTrusted: event.isTrusted,
          button: event.button
        });
      });
    });
  }");
        await Page.Mouse.ClickAsync(50, 60);
        var @event = await Page.EvaluateAsync<JsonElement>("() => window['clickPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("click"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(1));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("MO-002 should dblclick the div")]
    public async Task ShouldDblclickTheDiv()
    {
        await Page.SetContentAsync("<div style='width: 100px; height: 100px;'>Click me</div>");
        await Page.EvaluateAsync(@"() => {
    window['dblclickPromise'] = new Promise(resolve => {
      document.querySelector('div').addEventListener('dblclick', event => {
        resolve({
          type: event.type,
          detail: event.detail,
          clientX: event.clientX,
          clientY: event.clientY,
          isTrusted: event.isTrusted,
          button: event.button,
        });
      });
    });
  }");
        await Page.Mouse.DblClickAsync(50, 60);
        var @event = await Page.EvaluateAsync<JsonElement>("() => window['dblclickPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("dblclick"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(2));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("MO-003 down and up should generate click")]
    public async Task DownAndUpShouldGenerateClick()
    {
        await Page.EvaluateAsync(@"() => {
    window['clickPromise'] = new Promise(resolve => {
      document.addEventListener('click', event => {
        resolve({
          type: event.type,
          detail: event.detail,
          clientX: event.clientX,
          clientY: event.clientY,
          isTrusted: event.isTrusted,
          button: event.button
        });
      });
    });
  }");
        await Page.Mouse.MoveAsync(50, 60);
        await Page.Mouse.DownAsync();
        await Page.Mouse.UpAsync();
        var @event = await Page.EvaluateAsync<JsonElement>("() => window['clickPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("click"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(1));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("MO-004 should pointerdown the div with a custom button")]
    public async Task ShouldPointerdownTheDivWithACustomButton()
    {
        await Page.SetContentAsync("<div style='width: 100px; height: 100px;'>Click me</div>");
        await Page.EvaluateAsync(@"() => {
    window['pointerdownPromise'] = new Promise(resolve => {
      document.querySelector('div').addEventListener('pointerdown', event => {
        resolve({
          type: event.type,
          detail: event.detail,
          clientX: event.clientX,
          clientY: event.clientY,
          isTrusted: event.isTrusted,
          button: event.button,
          buttons: event.buttons,
          pointerId: event.pointerId,
        });
      });
    });
  }");
        await Page.Mouse.ClickAsync(50, 60, new()
        {
            Button = MouseButton.Middle,
        });
        var @event = await Page.EvaluateAsync<JsonElement>("() => window['pointerdownPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("pointerdown"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(BrowserName == "webkit" ? 1 : 0));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(1));
        Assert.That(@event.GetProperty("buttons").GetInt32(), Is.EqualTo(4));
        Assert.That(@event.GetProperty("pointerId").GetInt32(), Is.GreaterThanOrEqualTo(0));
    }

    [UpstreamTest("MO-005 should report correct buttons property")]
    public async Task ShouldReportCorrectButtonsProperty()
    {
        await Page.EvaluateAsync(@"() => {
    window.__EVENTS = [];
    const handler = event => {
      window.__EVENTS.push({
        type: event.type,
        button: event.button,
        buttons: event.buttons,
      });
    };
    window.addEventListener('mousedown', handler, false);
    window.addEventListener('mouseup', handler, false);
  }");
        await Page.Mouse.MoveAsync(50, 60);
        await Page.Mouse.DownAsync(new()
        {
            Button = MouseButton.Middle,
        });
        await Page.Mouse.DownAsync(new()
        {
            Button = MouseButton.Left,
        });
        await Page.Mouse.UpAsync(new()
        {
            Button = MouseButton.Middle,
        });
        await Page.Mouse.UpAsync(new()
        {
            Button = MouseButton.Left,
        });
        ExpectToEqual(await Page.EvaluateAsync<object>("() => window.__EVENTS"), new[]
        {
            new { type = "mousedown", button = 1, buttons = 4 },
            new { type = "mousedown", button = 0, buttons = 5 },
            new { type = "mouseup", button = 1, buttons = 1 },
            new { type = "mouseup", button = 0, buttons = 0 },
        });
    }

    [UpstreamTest("MO-006 should report correct pointerType property")]
    public async Task ShouldReportCorrectPointerTypeProperty()
    {
        await Page.Mouse.MoveAsync(50, 60);
        await Page.EvaluateAsync(@"() => {
    window.__EVENTS = [];
    const handler = event => {
      window.__EVENTS.push({
        type: event.type,
        pointerType: event.pointerType,
      });
    };
    window.addEventListener('pointerdown', handler, false);
    window.addEventListener('pointermove', handler, false);
    window.addEventListener('pointerup', handler, false);
  }");
        await Page.Mouse.MoveAsync(60, 50);
        await Page.Mouse.DownAsync();
        await Page.Mouse.UpAsync();
        ExpectToEqual(await Page.EvaluateAsync<object>("() => window.__EVENTS"), new[]
        {
            new { type = "pointermove", pointerType = "mouse" },
            new { type = "pointerdown", pointerType = "mouse" },
            new { type = "pointerup", pointerType = "mouse" },
        });
    }

    [UpstreamTest("MO-007 should select the text with mouse")]
    public async Task ShouldSelectTheTextWithMouse()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.FocusAsync("textarea");
        var text = "This is the text that we are going to try to select. Let's see how it goes.";
        await Page.Keyboard.TypeAsync(text);
        // Firefox needs an extra frame here after typing or it will fail to set the scrollTop
        await Rafraf(Page);
        await Page.EvaluateAsync("() => document.querySelector('textarea').scrollTop = 0");
        // .NET: const { x, y } = await page.evaluate(dimensions);
        var rect = await Page.EvaluateAsync<JsonElement>(dimensions);
        var (x, y) = (rect.GetProperty("x").GetSingle(), rect.GetProperty("y").GetSingle());
        await Page.Mouse.MoveAsync(x + 2, y + 2);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(200, 200);
        await Page.Mouse.UpAsync();
        Assert.That(await Page.EvaluateAsync<string>(@"() => {
    const textarea = document.querySelector('textarea');
    return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
  }"), Is.EqualTo(text));
    }

    [UpstreamTest("MO-008 should trigger hover state")]
    public async Task ShouldTriggerHoverState()
    {
        Info.Skip(!Headless, "headed messes up with hover");

        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.HoverAsync("#button-6");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
        await Page.HoverAsync("#button-2");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-2"));
        await Page.HoverAsync("#button-91");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-91"));
    }

    [UpstreamTest("MO-009 should trigger hover state on disabled button")]
    public async Task ShouldTriggerHoverStateOnDisabledButton()
    {
        Info.Skip(!Headless, "headed messes up with hover");

        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.EvalOnSelectorAsync("#button-6", "button => button.disabled = true");
        await Page.HoverAsync("#button-6", new() { Timeout = 5000 });
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("MO-010 should trigger hover state with removed window.Node")]
    public async Task ShouldTriggerHoverStateWithRemovedWindowNode()
    {
        Info.Skip(!Headless, "headed messes up with hover");

        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.EvaluateAsync("() => delete window.Node");
        await Page.HoverAsync("#button-6");
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("MO-011 should set modifier keys on click")]
    public async Task ShouldSetModifierKeysOnClick()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.EvaluateAsync("() => document.querySelector('#button-3').addEventListener('mousedown', e => window['lastEvent'] = e, true)");
        var modifiers = new Dictionary<string, string> { ["Shift"] = "shiftKey", ["Control"] = "ctrlKey", ["Alt"] = "altKey", ["Meta"] = "metaKey" };
        // In Firefox, the Meta modifier only exists on Mac
        if (BrowserName == "firefox" && !IsMac)
        {
            modifiers.Remove("Meta");
        }
        foreach (var modifier in modifiers.Keys)
        {
            await Page.Keyboard.DownAsync(modifier);
            await Page.ClickAsync("#button-3");
            if (!(await Page.EvaluateAsync<bool>("mod => window['lastEvent'][mod]", modifiers[modifier])))
            {
                throw new Exception(modifiers[modifier] + " should be true");
            }
            await Page.Keyboard.UpAsync(modifier);
        }
        await Page.ClickAsync("#button-3");
        foreach (var modifier in modifiers.Keys)
        {
            if (await Page.EvaluateAsync<bool>("mod => window['lastEvent'][mod]", modifiers[modifier]))
            {
                throw new Exception(modifiers[modifier] + " should be false");
            }
        }
    }

    [UpstreamTest("MO-012 should tween mouse movement")]
    public async Task ShouldTweenMouseMovement()
    {
        Info.Skip(IsAndroid, "Bad rounding");
        Info.Skip(!Headless, "actual mouse interferes with the exact mousemove events");

        // The test becomes flaky on WebKit without next line.
        if (BrowserName == "webkit")
        {
            await Page.EvaluateAsync("() => new Promise(requestAnimationFrame)");
        }
        await Page.Mouse.MoveAsync(100, 100);
        await Page.EvaluateAsync(@"() => {
    window['result'] = [];
    document.addEventListener('mousemove', event => {
      window['result'].push([event.clientX, event.clientY, event.movementX, event.movementY]);
    });
  }");
        await Page.Mouse.MoveAsync(200, 300, new() { Steps = 5 });
        ExpectToEqual(await Page.EvaluateAsync<object>("result"), new[]
        {
            new[] { 120, 140, 20, 40 },
            new[] { 140, 180, 20, 40 },
            new[] { 160, 220, 20, 40 },
            new[] { 180, 260, 20, 40 },
            new[] { 200, 300, 20, 40 },
        });
    }

    [UpstreamTest("MO-013 should always round down")]
    public async Task ShouldAlwaysRoundDown()
    {
        await Page.EvaluateAsync(@"() => {
    document.addEventListener('mousedown', event => {
      window['result'] = [event.clientX, event.clientY];
    });
  }");
        await Page.Mouse.ClickAsync(50.1f, 50.9f);
        ExpectToEqual(await Page.EvaluateAsync<object>("result"), new[] { 50, 50 });
    }

    [UpstreamTest("MO-014 should not crash on mouse drag with any button")]
    public async Task ShouldNotCrashOnMouseDragWithAnyButton()
    {
        await Page.EvaluateAsync(@"() => {
    // Do not show contextmenu on right click since it is poorly supported.
    window.addEventListener('contextmenu', e => e.preventDefault(), false);
  }");
        foreach (var button in new[] { MouseButton.Left, MouseButton.Middle, MouseButton.Right })
        {
            await Page.Mouse.MoveAsync(50, 50);
            await Page.Mouse.DownAsync(new() { Button = button });
            await Page.Mouse.MoveAsync(100, 100);
        }
    }

    [UpstreamTest("MO-015 should dispatch mouse move after context menu was opened")]
    public async Task ShouldDispatchMouseMoveAfterContextMenuWasOpened()
    {
        Info.Fixme(BrowserName == "chromium" && IsWindows, "context menu support is best-effort for Linux and MacOS");
        await Page.EvaluateAsync(@"() => {
    window['contextMenuPromise'] = new Promise(x => {
      window.addEventListener('contextmenu', x, false);
    });
  }");
        const int CX = 100;
        const int CY = 100;
        await Page.Mouse.MoveAsync(CX, CY);
        await Page.Mouse.DownAsync(new() { Button = MouseButton.Right });
        await Page.EvaluateAsync("() => window['contextMenuPromise']");
        const int N = 20;
        foreach (var radius in new[] { 10, 30, 60, 90 })
        {
            for (var i = 0; i < N; ++i)
            {
                var angle = 2 * Math.PI * i / N;
                var x = CX + (int)Math.Round(radius * Math.Cos(angle), MidpointRounding.AwayFromZero);
                var y = CY + (int)Math.Round(radius * Math.Sin(angle), MidpointRounding.AwayFromZero);
                await Page.Mouse.MoveAsync(x, y);
            }
        }
    }

    [UpstreamTest("MO-016 should track hover across iframe boundaries")]
    public async Task ShouldTrackHoverAcrossIframeBoundaries()
    {
        Info.Skip(!Headless, "headed messes up with hover");

        await Page.SetContentAsync(@"
    <style>
      body, html { margin: 0; padding: 0; }
      #parentBox { position: absolute; left: 10px; top: 10px; width: 100px; height: 100px; }
      iframe { position: absolute; left: 200px; top: 10px; width: 200px; height: 200px; border: none; }
    </style>
    <div id=""parentBox""></div>
    <iframe srcdoc=""
      <style>body, html { margin: 0; padding: 0; } #childBox { width: 180px; height: 180px; }</style>
      <div id='childBox'></div>
      <script>
        const box = document.querySelector('#childBox');
        box.addEventListener('mouseenter', () => window.top.__log.push('child:enter'));
        box.addEventListener('mouseleave', () => window.top.__log.push('child:leave'));
      </script>
    ""></iframe>
    <script>
      window.__log = [];
      const box = document.querySelector('#parentBox');
      box.addEventListener('mouseenter', () => window.__log.push('parent:enter'));
      box.addEventListener('mouseleave', () => window.__log.push('parent:leave'));
    </script>
  ");
        await Page.WaitForSelectorAsync("iframe");
        await Page.Frames[1].WaitForSelectorAsync("#childBox");
        var log = () => Page.EvaluateAsync<string[]>("() => window['__log']");
        var parentBox = await Page.Locator("#parentBox").BoundingBoxAsync();
        var iframeBox = await Page.Locator("iframe").BoundingBoxAsync();
        var parentCenter = new { x = parentBox.X + (parentBox.Width / 2), y = parentBox.Y + (parentBox.Height / 2) };
        var childCenter = new { x = iframeBox.X + 90, y = iframeBox.Y + 90 };

        await Page.Mouse.MoveAsync(parentCenter.x, parentCenter.y);
        await Expect(async () => Assert.That(await log(), Is.EqualTo(new[] { "parent:enter" }))).ToPassAsync();

        // Crossing into the iframe leaves the parent element and enters the child.
        await Page.Mouse.MoveAsync(childCenter.x, childCenter.y);
        await Expect(async () => Assert.That(await log(), Is.EqualTo(new[] { "parent:enter", "parent:leave", "child:enter" }))).ToPassAsync();

        // Crossing back out leaves the child element and re-enters the parent.
        await Page.Mouse.MoveAsync(parentCenter.x, parentCenter.y);
        await Expect(async () => Assert.That(await log(), Is.EqualTo(new[] { "parent:enter", "parent:leave", "child:enter", "child:leave", "parent:enter" }))).ToPassAsync();
    }
}
