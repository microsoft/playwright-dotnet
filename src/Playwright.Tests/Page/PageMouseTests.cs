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
public class PageMouseTests
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
    public async Task ShouldClickTheDocument(IPage page, Server server)
    {
        await page.EvaluateAsync(@"() => {
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
        await page.Mouse.ClickAsync(50, 60);
        var @event = await page.EvaluateAsync<JsonElement>("() => window['clickPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("click"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(1));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("MO-002 should dblclick the div")]
    public async Task ShouldDblclickTheDiv(IPage page, Server server)
    {
        await page.SetContentAsync("<div style='width: 100px; height: 100px;'>Click me</div>");
        await page.EvaluateAsync(@"() => {
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
        await page.Mouse.DblClickAsync(50, 60);
        var @event = await page.EvaluateAsync<JsonElement>("() => window['dblclickPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("dblclick"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(2));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("MO-003 down and up should generate click")]
    public async Task DownAndUpShouldGenerateClick(IPage page, Server server)
    {
        await page.EvaluateAsync(@"() => {
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
        await page.Mouse.MoveAsync(50, 60);
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();
        var @event = await page.EvaluateAsync<JsonElement>("() => window['clickPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("click"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(1));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(0));
    }

    [UpstreamTest("MO-004 should pointerdown the div with a custom button")]
    public async Task ShouldPointerdownTheDivWithACustomButton(IPage page, Server server, string browserName)
    {
        await page.SetContentAsync("<div style='width: 100px; height: 100px;'>Click me</div>");
        await page.EvaluateAsync(@"() => {
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
        await page.Mouse.ClickAsync(50, 60, new()
        {
            Button = MouseButton.Middle,
        });
        var @event = await page.EvaluateAsync<JsonElement>("() => window['pointerdownPromise']");
        Assert.That(@event.GetProperty("type").GetString(), Is.EqualTo("pointerdown"));
        Assert.That(@event.GetProperty("detail").GetInt32(), Is.EqualTo(browserName == "webkit" ? 1 : 0));
        Assert.That(@event.GetProperty("clientX").GetInt32(), Is.EqualTo(50));
        Assert.That(@event.GetProperty("clientY").GetInt32(), Is.EqualTo(60));
        Assert.That(@event.GetProperty("isTrusted").GetBoolean(), Is.EqualTo(true));
        Assert.That(@event.GetProperty("button").GetInt32(), Is.EqualTo(1));
        Assert.That(@event.GetProperty("buttons").GetInt32(), Is.EqualTo(4));
        Assert.That(@event.GetProperty("pointerId").GetInt32(), Is.GreaterThanOrEqualTo(0));
    }

    [UpstreamTest("MO-005 should report correct buttons property")]
    public async Task ShouldReportCorrectButtonsProperty(IPage page)
    {
        await page.EvaluateAsync(@"() => {
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
        await page.Mouse.MoveAsync(50, 60);
        await page.Mouse.DownAsync(new()
        {
            Button = MouseButton.Middle,
        });
        await page.Mouse.DownAsync(new()
        {
            Button = MouseButton.Left,
        });
        await page.Mouse.UpAsync(new()
        {
            Button = MouseButton.Middle,
        });
        await page.Mouse.UpAsync(new()
        {
            Button = MouseButton.Left,
        });
        ExpectToEqual(await page.EvaluateAsync<object>("() => window.__EVENTS"), new[]
        {
            new { type = "mousedown", button = 1, buttons = 4 },
            new { type = "mousedown", button = 0, buttons = 5 },
            new { type = "mouseup", button = 1, buttons = 1 },
            new { type = "mouseup", button = 0, buttons = 0 },
        });
    }

    [UpstreamTest("MO-006 should report correct pointerType property")]
    public async Task ShouldReportCorrectPointerTypeProperty(IPage page)
    {
        await page.Mouse.MoveAsync(50, 60);
        await page.EvaluateAsync(@"() => {
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
        await page.Mouse.MoveAsync(60, 50);
        await page.Mouse.DownAsync();
        await page.Mouse.UpAsync();
        ExpectToEqual(await page.EvaluateAsync<object>("() => window.__EVENTS"), new[]
        {
            new { type = "pointermove", pointerType = "mouse" },
            new { type = "pointerdown", pointerType = "mouse" },
            new { type = "pointerup", pointerType = "mouse" },
        });
    }

    [UpstreamTest("MO-007 should select the text with mouse")]
    public async Task ShouldSelectTheTextWithMouse(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/textarea.html");
        await page.FocusAsync("textarea");
        var text = "This is the text that we are going to try to select. Let's see how it goes.";
        await page.Keyboard.TypeAsync(text);
        // Firefox needs an extra frame here after typing or it will fail to set the scrollTop
        await Rafraf(page);
        await page.EvaluateAsync("() => document.querySelector('textarea').scrollTop = 0");
        // .NET: const { x, y } = await page.evaluate(dimensions);
        var rect = await page.EvaluateAsync<JsonElement>(dimensions);
        var (x, y) = (rect.GetProperty("x").GetSingle(), rect.GetProperty("y").GetSingle());
        await page.Mouse.MoveAsync(x + 2, y + 2);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(200, 200);
        await page.Mouse.UpAsync();
        Assert.That(await page.EvaluateAsync<string>(@"() => {
    const textarea = document.querySelector('textarea');
    return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
  }"), Is.EqualTo(text));
    }

    [UpstreamTest("MO-008 should trigger hover state")]
    public async Task ShouldTriggerHoverState(IPage page, Server server, bool headless)
    {
        It.Skip(!headless, "headed messes up with hover");

        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.HoverAsync("#button-6");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
        await page.HoverAsync("#button-2");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-2"));
        await page.HoverAsync("#button-91");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-91"));
    }

    [UpstreamTest("MO-009 should trigger hover state on disabled button")]
    public async Task ShouldTriggerHoverStateOnDisabledButton(IPage page, Server server, bool headless)
    {
        It.Skip(!headless, "headed messes up with hover");

        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.EvalOnSelectorAsync("#button-6", "button => button.disabled = true");
        await page.HoverAsync("#button-6", new() { Timeout = 5000 });
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("MO-010 should trigger hover state with removed window.Node")]
    public async Task ShouldTriggerHoverStateWithRemovedWindowNode(IPage page, Server server, bool headless)
    {
        It.Skip(!headless, "headed messes up with hover");

        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.EvaluateAsync("() => delete window.Node");
        await page.HoverAsync("#button-6");
        Assert.That(await page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("MO-011 should set modifier keys on click")]
    public async Task ShouldSetModifierKeysOnClick(IPage page, Server server, string browserName, bool isMac)
    {
        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.EvaluateAsync("() => document.querySelector('#button-3').addEventListener('mousedown', e => window['lastEvent'] = e, true)");
        var modifiers = new Dictionary<string, string> { ["Shift"] = "shiftKey", ["Control"] = "ctrlKey", ["Alt"] = "altKey", ["Meta"] = "metaKey" };
        // In Firefox, the Meta modifier only exists on Mac
        if (browserName == "firefox" && !isMac)
        {
            modifiers.Remove("Meta");
        }
        foreach (var modifier in modifiers.Keys)
        {
            await page.Keyboard.DownAsync(modifier);
            await page.ClickAsync("#button-3");
            if (!(await page.EvaluateAsync<bool>("mod => window['lastEvent'][mod]", modifiers[modifier])))
            {
                throw new Exception(modifiers[modifier] + " should be true");
            }
            await page.Keyboard.UpAsync(modifier);
        }
        await page.ClickAsync("#button-3");
        foreach (var modifier in modifiers.Keys)
        {
            if (await page.EvaluateAsync<bool>("mod => window['lastEvent'][mod]", modifiers[modifier]))
            {
                throw new Exception(modifiers[modifier] + " should be false");
            }
        }
    }

    [UpstreamTest("MO-012 should tween mouse movement")]
    public async Task ShouldTweenMouseMovement(IPage page, string browserName, bool isAndroid, bool headless)
    {
        It.Skip(isAndroid, "Bad rounding");
        It.Skip(!headless, "actual mouse interferes with the exact mousemove events");

        // The test becomes flaky on WebKit without next line.
        if (browserName == "webkit")
        {
            await page.EvaluateAsync("() => new Promise(requestAnimationFrame)");
        }
        await page.Mouse.MoveAsync(100, 100);
        await page.EvaluateAsync(@"() => {
    window['result'] = [];
    document.addEventListener('mousemove', event => {
      window['result'].push([event.clientX, event.clientY, event.movementX, event.movementY]);
    });
  }");
        await page.Mouse.MoveAsync(200, 300, new() { Steps = 5 });
        ExpectToEqual(await page.EvaluateAsync<object>("result"), new[]
        {
            new[] { 120, 140, 20, 40 },
            new[] { 140, 180, 20, 40 },
            new[] { 160, 220, 20, 40 },
            new[] { 180, 260, 20, 40 },
            new[] { 200, 300, 20, 40 },
        });
    }

    [UpstreamTest("MO-013 should always round down")]
    public async Task ShouldAlwaysRoundDown(IPage page)
    {
        await page.EvaluateAsync(@"() => {
    document.addEventListener('mousedown', event => {
      window['result'] = [event.clientX, event.clientY];
    });
  }");
        await page.Mouse.ClickAsync(50.1f, 50.9f);
        ExpectToEqual(await page.EvaluateAsync<object>("result"), new[] { 50, 50 });
    }

    [UpstreamTest("MO-014 should not crash on mouse drag with any button")]
    public async Task ShouldNotCrashOnMouseDragWithAnyButton(IPage page)
    {
        await page.EvaluateAsync(@"() => {
    // Do not show contextmenu on right click since it is poorly supported.
    window.addEventListener('contextmenu', e => e.preventDefault(), false);
  }");
        foreach (var button in new[] { MouseButton.Left, MouseButton.Middle, MouseButton.Right })
        {
            await page.Mouse.MoveAsync(50, 50);
            await page.Mouse.DownAsync(new() { Button = button });
            await page.Mouse.MoveAsync(100, 100);
        }
    }

    [UpstreamTest("MO-015 should dispatch mouse move after context menu was opened")]
    public async Task ShouldDispatchMouseMoveAfterContextMenuWasOpened(IPage page, string browserName, bool isWindows)
    {
        It.Fixme(browserName == "chromium" && isWindows, "context menu support is best-effort for Linux and MacOS");
        await page.EvaluateAsync(@"() => {
    window['contextMenuPromise'] = new Promise(x => {
      window.addEventListener('contextmenu', x, false);
    });
  }");
        const int CX = 100;
        const int CY = 100;
        await page.Mouse.MoveAsync(CX, CY);
        await page.Mouse.DownAsync(new() { Button = MouseButton.Right });
        await page.EvaluateAsync("() => window['contextMenuPromise']");
        const int N = 20;
        foreach (var radius in new[] { 10, 30, 60, 90 })
        {
            for (var i = 0; i < N; ++i)
            {
                var angle = 2 * Math.PI * i / N;
                var x = CX + (int)Math.Round(radius * Math.Cos(angle), MidpointRounding.AwayFromZero);
                var y = CY + (int)Math.Round(radius * Math.Sin(angle), MidpointRounding.AwayFromZero);
                await page.Mouse.MoveAsync(x, y);
            }
        }
    }

    [UpstreamTest("MO-016 should track hover across iframe boundaries")]
    public async Task ShouldTrackHoverAcrossIframeBoundaries(IPage page, bool headless)
    {
        It.Skip(!headless, "headed messes up with hover");

        await page.SetContentAsync(@"
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
        await page.WaitForSelectorAsync("iframe");
        await page.Frames[1].WaitForSelectorAsync("#childBox");
        var log = () => page.EvaluateAsync<string[]>("() => window['__log']");
        var parentBox = await page.Locator("#parentBox").BoundingBoxAsync();
        var iframeBox = await page.Locator("iframe").BoundingBoxAsync();
        var parentCenter = new { x = parentBox.X + (parentBox.Width / 2), y = parentBox.Y + (parentBox.Height / 2) };
        var childCenter = new { x = iframeBox.X + 90, y = iframeBox.Y + 90 };

        await page.Mouse.MoveAsync(parentCenter.x, parentCenter.y);
        await Expect(async () => Assert.That(await log(), Is.EqualTo(new[] { "parent:enter" }))).ToPassAsync();

        // Crossing into the iframe leaves the parent element and enters the child.
        await page.Mouse.MoveAsync(childCenter.x, childCenter.y);
        await Expect(async () => Assert.That(await log(), Is.EqualTo(new[] { "parent:enter", "parent:leave", "child:enter" }))).ToPassAsync();

        // Crossing back out leaves the child element and re-enters the parent.
        await page.Mouse.MoveAsync(parentCenter.x, parentCenter.y);
        await Expect(async () => Assert.That(await log(), Is.EqualTo(new[] { "parent:enter", "parent:leave", "child:enter", "child:leave", "parent:enter" }))).ToPassAsync();
    }
}
