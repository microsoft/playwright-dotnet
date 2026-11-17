/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-drag.spec.ts.</summary>
public class PageDragTests
{
    [BeforeEach]
    public static void Skip(string browserName, int browserMajorVersion) => Info.Skip(browserName == "chromium" && browserMajorVersion < 91);

    [BeforeEach]
    public static void Skip2(bool isAndroid) => Info.Skip(isAndroid, "No drag&drop on Android.");

    [BeforeEach]
    public static void Skip3(bool headless) => Info.Skip(!headless, "Stray mouse events mess up the tests.");

    public class DragAndDrop
    {
        [UpstreamTest("DD-001 should work @smoke")]
        public async Task ShouldWork(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            await page.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await page.HoverAsync("#target");
            await page.Mouse.UpAsync();
            Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        }

        [UpstreamTest("DD-002 should send the right events")]
        public async Task ShouldSendTheRightEvents(Server server, IPage page, string browserName)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            var events = await TrackEvents(await page.QuerySelectorAsync("body"));
            await page.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await page.HoverAsync("#target");
            await page.Mouse.UpAsync();
            Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                browserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                browserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
                "dragover at 240;350",
                "drop at 240;350",
                "dragend",
            }));
        }

        [UpstreamTest("DD-003 should not send dragover on the first mousemove")]
        public async Task ShouldNotSendDragoverOnTheFirstMousemove(Server server, IPage page, string browserName)
        {
            Info.Fixme(browserName != "chromium");

            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            var events = await TrackEvents(await page.QuerySelectorAsync("body"));
            await page.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await page.HoverAsync("#target");
            Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                browserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                browserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
            }));
        }

        [UpstreamTest("DD-004 should work inside iframe")]
        public async Task ShouldWorkInsideIframe(IPage page, Server server, string browserName, bool isElectron, bool isWindows)
        {
            await page.GotoAsync(server.EmptyPage);
            var frame = await AttachFrame(page, "myframe", server.Prefix + "/drag-n-drop.html");
            await page.EvalOnSelectorAsync("iframe", @"iframe => {
      iframe.style.width = '500px';
      iframe.style.height = '600px';
      iframe.style.marginLeft = '80px';
      iframe.style.marginTop = '60px';
    }");
            var pageEvents = await TrackEvents(await page.QuerySelectorAsync("body"));
            var frameEvents = await TrackEvents(await frame.QuerySelectorAsync("body"));
            await frame.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await frame.HoverAsync("#target");
            await page.Mouse.UpAsync();
            Assert.That(await frame.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
            Assert.That(await frameEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                browserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                browserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
                "dragover at 240;350",
                "drop at 240;350",
                "dragend",
            }));
            Assert.That(await pageEvents.JsonValueAsync<string[]>(), Is.EqualTo(Array.Empty<string>()));
        }

        [UpstreamTest("DD-005 should cancel on escape")]
        public async Task ShouldCancelOnEscape(Server server, IPage page, string browserName)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            var events = await TrackEvents(await page.QuerySelectorAsync("body"));
            await page.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await page.HoverAsync("#target");
            await page.Keyboard.PressAsync("Escape");
            await page.Mouse.UpAsync();
            Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(false)); // found source in target
            Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                browserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                browserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
                browserName == "chromium" ? null : "dragover at 240;350",
                "dragend",
                "mouseup at 240;350",
            }.Where(e => !string.IsNullOrEmpty(e))));
        }

        public class Iframe
        {
            [BeforeEach]
            public static void Fixme() => Info.Fixme(true, "implement dragging with iframes");

            [UpstreamTest("DD-006 should drag into an iframe")]
            public async Task ShouldDragIntoAnIframe(Server server, IPage page, string browserName)
            {
                await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
                var frame = await AttachFrame(page, "oopif", server.Prefix + "/drag-n-drop.html");
                await page.EvalOnSelectorAsync("iframe", @"iframe => {
        iframe.style.width = '500px';
        iframe.style.height = '600px';
        iframe.style.marginLeft = '500px';
        iframe.style.marginTop = '60px';
      }");
                var pageEvents = await TrackEvents(await page.QuerySelectorAsync("body"));
                var frameEvents = await TrackEvents(await frame.QuerySelectorAsync("body"));
                await page.HoverAsync("#source");
                await page.Mouse.DownAsync();
                await frame.HoverAsync("#target");
                await page.Mouse.UpAsync();
                Assert.That(await frame.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
                Assert.That(await pageEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
                {
                    "mousemove",
                    "mousedown",
                    browserName == "firefox" ? "dragstart" : "mousemove",
                    browserName == "firefox" ? "mousemove" : "dragstart",
                }));
                Assert.That(await frameEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
                {
                    "dragenter",
                    "dragover",
                    "drop",
                }));
            }

            [UpstreamTest("DD-007 should drag out of an iframe")]
            public async Task ShouldDragOutOfAnIframe(Server server, IPage page)
            {
                await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
                var frame = await AttachFrame(page, "oopif", server.Prefix + "/drag-n-drop.html");
                var pageEvents = await TrackEvents(await page.QuerySelectorAsync("body"));
                var frameEvents = await TrackEvents(await frame.QuerySelectorAsync("body"));
                await frame.HoverAsync("#source");
                await page.Mouse.DownAsync();
                await page.HoverAsync("#target");
                await page.Mouse.UpAsync();
                Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
                Assert.That(await frameEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
                {
                    "mousemove",
                    "mousedown",
                    "dragstart",
                    "dragend",
                }));
                Assert.That(await pageEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
                {
                    "dragenter",
                    "dragover",
                    "drop",
                }));
            }
        }

        [UpstreamTest("DD-008 should respect the drop effect")]
        public async Task ShouldRespectTheDropEffect(IPage page, string browserName, bool isLinux, bool isMac, bool headless, string trace)
        {
            Info.Fixme(browserName == "webkit" && isLinux && !headless, "https://github.com/microsoft/playwright/issues/21646");
            Info.Fixme(browserName == "chromium" && !isMac && !headless, "https://github.com/microsoft/playwright/issues/21646");
            Info.Slow(trace == "on");

            Assert.That(await testIfDropped("copy", "copy"), Is.EqualTo(true));
            Assert.That(await testIfDropped("copy", "move"), Is.EqualTo(false));
            Assert.That(await testIfDropped("all", "link"), Is.EqualTo(true));
            Assert.That(await testIfDropped("all", "none"), Is.EqualTo(false));

            Assert.That(await testIfDropped("copyMove", "copy"), Is.EqualTo(true));
            Assert.That(await testIfDropped("copyLink", "copy"), Is.EqualTo(true));
            Assert.That(await testIfDropped("linkMove", "copy"), Is.EqualTo(false));

            Assert.That(await testIfDropped("copyMove", "link"), Is.EqualTo(false));
            Assert.That(await testIfDropped("copyLink", "link"), Is.EqualTo(true));
            Assert.That(await testIfDropped("linkMove", "link"), Is.EqualTo(true));

            Assert.That(await testIfDropped("copyMove", "move"), Is.EqualTo(true));
            Assert.That(await testIfDropped("copyLink", "move"), Is.EqualTo(false));
            Assert.That(await testIfDropped("linkMove", "move"), Is.EqualTo(true));

            Assert.That(await testIfDropped("uninitialized", "copy"), Is.EqualTo(true));

            async Task<bool> testIfDropped(string effectAllowed, string dropEffect)
            {
                await page.SetContentAsync(@"
        <div draggable=""true"">drag target</div>
        <drop-target>this is the drop target</drop-target>
      ");
                await page.EvaluateAsync(@"({ effectAllowed, dropEffect }) => {
        window['dropped'] = false;

        document.querySelector('div').addEventListener('dragstart', event => {
          event.dataTransfer.effectAllowed = effectAllowed;
          event.dataTransfer.setData('text/plain', 'drag data');
        });

        const dropTarget = document.querySelector('drop-target');
        dropTarget.addEventListener('dragover', event => {
          event.dataTransfer.dropEffect = dropEffect;
          event.preventDefault();
        });
        dropTarget.addEventListener('drop', event => {
          window['dropped'] = true;
        });
      }", new { effectAllowed, dropEffect });
                await page.HoverAsync("div");
                await page.Mouse.DownAsync();
                await page.HoverAsync("drop-target");
                await page.Mouse.UpAsync();
                return await page.EvaluateAsync<bool>("dropped");
            }
        }

        [UpstreamTest("DD-028 should report the default drop effect from effectAllowed")]
        public async Task ShouldReportTheDefaultDropEffectFromEffectAllowed(IPage page, string browserName, bool isLinux, bool headless)
        {
            Info.Fixme(browserName == "webkit" && isLinux && !headless, "https://github.com/microsoft/playwright/issues/21646");

            await page.SetContentAsync(@"
      <div draggable=""true"">drag target</div>
      <drop-target>this is the drop target</drop-target>
    ");
            await page.EvaluateAsync(@"() => {
      window['dropEffect'] = 'none';

      document.querySelector('div').addEventListener('dragstart', event => {
        event.dataTransfer.effectAllowed = 'copyMove';
        event.dataTransfer.setData('text/plain', 'drag data');
      });
      document.querySelector('div').addEventListener('dragend', event => {
        window['dropEffect'] = event.dataTransfer.dropEffect;
      });

      // The target accepts the drag without setting dropEffect.
      document.querySelector('drop-target').addEventListener('dragover', event => {
        event.preventDefault();
      });
    }");
            await page.HoverAsync("div");
            await page.Mouse.DownAsync();
            await page.HoverAsync("drop-target");
            await page.Mouse.UpAsync();
            Assert.That(await page.EvaluateAsync<string>("dropEffect"), Is.EqualTo("move"));
        }

        [UpstreamTest("DD-009 should work if the drag is canceled")]
        public async Task ShouldWorkIfTheDragIsCanceled(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            await page.EvaluateAsync(@"() => {
      document.body.addEventListener('dragstart', event => {
        event.preventDefault();
      }, false);
    }");
            await page.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await page.HoverAsync("#target");
            await page.Mouse.UpAsync();
            Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(false));
        }

        [UpstreamTest("DD-010 should work if the drag event is captured but not canceled")]
        public async Task ShouldWorkIfTheDragEventIsCapturedButNotCanceled(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            await page.EvaluateAsync(@"() => {
      document.body.addEventListener('dragstart', event => {
        event.stopImmediatePropagation();
      }, false);
    }");
            await page.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await page.HoverAsync("#target");
            await page.Mouse.UpAsync();
            Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true));
        }

        [UpstreamTest("DD-011 should be able to drag the mouse in a frame")]
        public async Task ShouldBeAbleToDragTheMouseInAFrame(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
            var eventsHandle = await TrackEvents(await page.Frames[1].QuerySelectorAsync("html"));
            await page.Mouse.MoveAsync(30, 30);
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(60, 60);
            await page.Mouse.UpAsync();
            Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "mousemove at 20;20", "mousedown at 20;20", "mousemove at 50;50", "mouseup at 50;50" }));
        }

        // .NET: upstream also takes the toImpl fixture, which it does not use and which .NET does not have.
        [UpstreamTest("DD-012 should work if a frame is stalled")]
        public async Task ShouldWorkIfAFrameIsStalled(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
            await page.RouteAsync("**/empty.html", route =>
            {
                routePromise.SetResult(route);
            });
            IgnoreErrors(AttachFrame(page, "frame", server.EmptyPage));
            var route = await routePromise.Task;
            await page.HoverAsync("#source");
            await page.Mouse.DownAsync();
            await page.HoverAsync("#target");
            await page.Mouse.UpAsync();
            await route.AbortAsync();
            Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        }

        [UpstreamTest("DD-013 should work with the helper method")]
        public async Task ShouldWorkWithTheHelperMethod(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            await page.DragAndDropAsync("#source", "#target");
            Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        }

        private static readonly object[][] Drags =
        [
            [
                "dragAndDrop",
                (Func<IPage, int?, Task>)((page, steps) => page.DragAndDropAsync("#red", "#blue", new() { Steps = steps })),
            ],
            [
                "dragTo",
                (Func<IPage, int?, Task>)((page, steps) => page.Locator("#red").DragToAsync(page.Locator("#blue"), new() { Steps = steps })),
            ],
        ];

        [UpstreamTest("DD-014 should ${title} with tweened mouse movement")]
        [CaseSource(nameof(Drags))]
        public async Task ShouldWithTweenedMouseMovement(string title, Func<IPage, int?, Task> drag, IPage page, bool headless)
        {
            Info.Skip(!headless, "actual mouse interferes with the exact mousemove events");

            await page.SetContentAsync(@"
        <body style=""margin: 0; padding: 0;"">
          <div style=""width:100px;height:100px;background:red;"" id=""red""></div>
          <div style=""width:300px;height:100px;background:blue;"" id=""blue""></div>
        </body>
      ");
            var eventsHandle = await page.EvaluateHandleAsync(@"() => {
        const events = [];
        document.addEventListener('mousedown', event => {
          events.push({
            type: 'mousedown',
            x: event.pageX,
            y: event.pageY,
          });
        });
        document.addEventListener('mouseup', event => {
          events.push({
            type: 'mouseup',
            x: event.pageX,
            y: event.pageY,
          });
        });
        document.addEventListener('mousemove', event => {
          events.push({
            type: 'mousemove',
            x: event.pageX,
            y: event.pageY,
          });
        });
        return events;
      }");
            await drag(page, 4);
            await Expect(async () => ExpectToEqual(await eventsHandle.JsonValueAsync<object>(), new[]
            {
                new { type = "mousemove", x = 50, y = 50 },
                new { type = "mousedown", x = 50, y = 50 },
                new { type = "mousemove", x = 75, y = 75 },
                new { type = "mousemove", x = 100, y = 100 },
                new { type = "mousemove", x = 125, y = 125 },
                new { type = "mousemove", x = 150, y = 150 },
                new { type = "mouseup", x = 150, y = 150 },
            })).ToPassAsync();
        }

        [UpstreamTest("DD-015 should allow specifying the position")]
        public async Task ShouldAllowSpecifyingThePosition(IPage page, Server server)
        {
            await page.SetContentAsync(@"
      <div style=""width:100px;height:100px;background:red;"" id=""red"">
      </div>
      <div style=""width:100px;height:100px;background:blue;"" id=""blue"">
      </div>
    ");
            var eventsHandle = await page.EvaluateHandleAsync(@"() => {
      const events = [];
      document.getElementById('red').addEventListener('mousedown', event => {
        events.push({
          type: 'mousedown',
          x: event.offsetX,
          y: event.offsetY,
        });
      });
      document.getElementById('blue').addEventListener('mouseup', event => {
        events.push({
          type: 'mouseup',
          x: event.offsetX,
          y: event.offsetY,
        });
      });
      return events;
    }");
            await page.DragAndDropAsync("#red", "#blue", new()
            {
                SourcePosition = new() { X = 34, Y = 7 },
                TargetPosition = new() { X = 10, Y = 20 },
            });
            ExpectToEqual(await eventsHandle.JsonValueAsync<object>(), new[]
            {
                new { type = "mousedown", x = 34, y = 7 },
                new { type = "mouseup", x = 10, y = 20 },
            });
        }

        [UpstreamTest("DD-016 should work with locators")]
        public async Task ShouldWorkWithLocators(IPage page, Server server)
        {
            await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
            await page.Locator("#source").DragToAsync(page.Locator("#target"));
            Assert.That(await page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        }
    }

    [UpstreamTest("DD-017 should work if not doing a drag")]
    public async Task ShouldWorkIfNotDoingADrag(IPage page, bool isLinux, bool headless)
    {
        var eventsHandle = await TrackEvents(await page.QuerySelectorAsync("html"));
        await page.Mouse.MoveAsync(50, 50);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(100, 100);
        await page.Mouse.UpAsync();
        Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "mousemove at 50;50", "mousedown at 50;50", "mousemove at 100;100", "mouseup at 100;100" }));
    }

    [UpstreamTest("DD-018 should report event.buttons")]
    public async Task ShouldReportEventButtons(IPage page, string browserName)
    {
        var logsHandle = await page.EvaluateHandleAsync(@"async () => {
    const div = document.createElement('div');
    document.body.appendChild(div);
    div.style.width = '200px';
    div.style.height = '200px';
    div.style.backgroundColor = 'blue';
    div.addEventListener('mousedown', onEvent);
    div.addEventListener('mousemove', onEvent, { passive: false });
    div.addEventListener('mouseup', onEvent);
    const logs = [];
    function onEvent(event) {
      logs.push({ type: event.type, buttons: event.buttons });
    }
    await new Promise(window.builtins.requestAnimationFrame);
    return logs;
  }");
        await page.Mouse.MoveAsync(20, 20);
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(40, 40);
        await page.Mouse.UpAsync();
        var logs = await logsHandle.JsonValueAsync<object>();
        ExpectToEqual(logs, new[]
        {
            new { type = "mousemove", buttons = 0 },
            new { type = "mousedown", buttons = 1 },
            new { type = "mousemove", buttons = 1 },
            new { type = "mouseup", buttons = 0 },
        });
    }

    private static async Task<IJSHandle> TrackEvents(IElementHandle target)
    {
        var eventsHandle = await target.EvaluateHandleAsync(@"target => {
    const events = [];
    for (const event of [
      'mousedown', 'mousemove', 'mouseup',
      'dragstart', 'dragend', 'dragover', 'dragenter', 'dragleave', 'dragexit',
      'drop'
    ]) {
      target.addEventListener(event, e => {
        const pe = e;
        // Browsers are all over the place with dragend position.
        if (event === 'dragend')
          events.push('dragend');
        else
          events.push(`${event} at ${pe.clientX};${pe.clientY}`);
      }, false);
    }
    return events;
  }");
        return eventsHandle;
    }

    [UpstreamTest("DD-019 should handle custom dataTransfer")]
    public async Task ShouldHandleCustomDataTransfer(IPage page, string browserName, bool isWindows)
    {
        Info.Fixme(browserName == "webkit" && isWindows);
        await page.SetContentAsync("<button draggable=\"true\">Draggable</button>");

        await page.EvaluateAsync(@"() => {
    window.__result = new Promise(resolve => {
      document.addEventListener('dragstart', event => {
        event.dataTransfer.setData('custom-type', 'Hello World');
      }, false);

      document.addEventListener('dragenter', event => {
        event.preventDefault();
      }, false);
      document.addEventListener('dragover', event => {
        event.preventDefault();
      }, false);

      document.addEventListener('drop', event => {
        event.preventDefault();
        resolve({
          types: event.dataTransfer.types,
          data: event.dataTransfer.getData('custom-type'),
        });
      }, false);
    });
  }");

        await page.HoverAsync("[draggable=\"true\"]");
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(100, 100);
        await page.Mouse.UpAsync();

        ExpectToEqual(await page.EvaluateAsync<object>("() => window.__result"), new
        {
            types = new[] { "custom-type" },
            data = "Hello World",
        });
    }

    [UpstreamTest("DD-020 what happens when dragging element is destroyed")]
    public async Task WhatHappensWhenDraggingElementIsDestroyed(IPage page, string browserName)
    {
        await page.SetContentAsync(@"
    <button draggable=""true"">Draggable</button>
    <div id=target>drop here</div>
  ");

        await page.EvaluateAsync(@"() => {
    document.querySelector('#target').addEventListener('dragover', event => {
      document.querySelector('button')?.remove();
    }, false);

    document.querySelector('#target').addEventListener('drop', event => {
      document.querySelector('#target').textContent = 'dropped';
    }, false);
  }");

        await page.Locator("button").DragToAsync(page.Locator("div"));
        await Expect(page.Locator("div")).ToHaveTextAsync("drop here");
    }
}
