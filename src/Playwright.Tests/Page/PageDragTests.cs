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

public abstract class PageDragTestsBase : UpstreamPageTest
{
    [SetUp]
    public void Skip() => Info.Skip(BrowserName == "chromium" && BrowserMajorVersion < 91);

    [SetUp]
    public void Skip2() => Info.Skip(IsAndroid, "No drag&drop on Android.");

    [SetUp]
    public void Skip3() => Info.Skip(!Headless, "Stray mouse events mess up the tests.");
}

/// <summary>Port of tests/page/page-drag.spec.ts.</summary>
public class PageDragTests : PageDragTestsBase
{
    public class DragAndDrop : PageDragTestsBase
    {
        [UpstreamTest("DD-001 should work @smoke")]
        public async Task ShouldWork()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();
            Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        }

        [UpstreamTest("DD-002 should send the right events")]
        public async Task ShouldSendTheRightEvents()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            var events = await TrackEvents(await Page.QuerySelectorAsync("body"));
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();
            Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                BrowserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                BrowserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
                "dragover at 240;350",
                "drop at 240;350",
                "dragend",
            }));
        }

        [UpstreamTest("DD-003 should not send dragover on the first mousemove")]
        public async Task ShouldNotSendDragoverOnTheFirstMousemove()
        {
            Info.Fixme(BrowserName != "chromium");

            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            var events = await TrackEvents(await Page.QuerySelectorAsync("body"));
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                BrowserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                BrowserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
            }));
        }

        [UpstreamTest("DD-004 should work inside iframe")]
        public async Task ShouldWorkInsideIframe()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var frame = await AttachFrame(Page, "myframe", Server.Prefix + "/drag-n-drop.html");
            await Page.EvalOnSelectorAsync("iframe", @"iframe => {
      iframe.style.width = '500px';
      iframe.style.height = '600px';
      iframe.style.marginLeft = '80px';
      iframe.style.marginTop = '60px';
    }");
            var pageEvents = await TrackEvents(await Page.QuerySelectorAsync("body"));
            var frameEvents = await TrackEvents(await frame.QuerySelectorAsync("body"));
            await frame.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await frame.HoverAsync("#target");
            await Page.Mouse.UpAsync();
            Assert.That(await frame.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
            Assert.That(await frameEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                BrowserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                BrowserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
                "dragover at 240;350",
                "drop at 240;350",
                "dragend",
            }));
            Assert.That(await pageEvents.JsonValueAsync<string[]>(), Is.EqualTo(Array.Empty<string>()));
        }

        [UpstreamTest("DD-005 should cancel on escape")]
        public async Task ShouldCancelOnEscape()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            var events = await TrackEvents(await Page.QuerySelectorAsync("body"));
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Keyboard.PressAsync("Escape");
            await Page.Mouse.UpAsync();
            Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(false)); // found source in target
            Assert.That(await events.JsonValueAsync<string[]>(), Is.EqualTo(new[]
            {
                "mousemove at 120;86",
                "mousedown at 120;86",
                BrowserName == "firefox" ? "dragstart at 120;86" : "mousemove at 240;350",
                BrowserName == "firefox" ? "mousemove at 240;350" : "dragstart at 120;86",
                "dragenter at 240;350",
                BrowserName == "chromium" ? null : "dragover at 240;350",
                "dragend",
                "mouseup at 240;350",
            }.Where(e => !string.IsNullOrEmpty(e))));
        }

        public class Iframe : PageDragTestsBase
        {
            [SetUp]
            public void Fixme() => Info.Fixme(true, "implement dragging with iframes");

            [UpstreamTest("DD-006 should drag into an iframe")]
            public async Task ShouldDragIntoAnIframe()
            {
                await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
                var frame = await AttachFrame(Page, "oopif", Server.Prefix + "/drag-n-drop.html");
                await Page.EvalOnSelectorAsync("iframe", @"iframe => {
        iframe.style.width = '500px';
        iframe.style.height = '600px';
        iframe.style.marginLeft = '500px';
        iframe.style.marginTop = '60px';
      }");
                var pageEvents = await TrackEvents(await Page.QuerySelectorAsync("body"));
                var frameEvents = await TrackEvents(await frame.QuerySelectorAsync("body"));
                await Page.HoverAsync("#source");
                await Page.Mouse.DownAsync();
                await frame.HoverAsync("#target");
                await Page.Mouse.UpAsync();
                Assert.That(await frame.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
                Assert.That(await pageEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
                {
                    "mousemove",
                    "mousedown",
                    BrowserName == "firefox" ? "dragstart" : "mousemove",
                    BrowserName == "firefox" ? "mousemove" : "dragstart",
                }));
                Assert.That(await frameEvents.JsonValueAsync<string[]>(), Is.EqualTo(new[]
                {
                    "dragenter",
                    "dragover",
                    "drop",
                }));
            }

            [UpstreamTest("DD-007 should drag out of an iframe")]
            public async Task ShouldDragOutOfAnIframe()
            {
                await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
                var frame = await AttachFrame(Page, "oopif", Server.Prefix + "/drag-n-drop.html");
                var pageEvents = await TrackEvents(await Page.QuerySelectorAsync("body"));
                var frameEvents = await TrackEvents(await frame.QuerySelectorAsync("body"));
                await frame.HoverAsync("#source");
                await Page.Mouse.DownAsync();
                await Page.HoverAsync("#target");
                await Page.Mouse.UpAsync();
                Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
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
        public async Task ShouldRespectTheDropEffect()
        {
            Info.Fixme(BrowserName == "webkit" && IsLinux && !Headless, "https://github.com/microsoft/playwright/issues/21646");
            Info.Fixme(BrowserName == "chromium" && !IsMac && !Headless, "https://github.com/microsoft/playwright/issues/21646");
            Info.Slow(Trace == "on");

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
                await Page.SetContentAsync(@"
        <div draggable=""true"">drag target</div>
        <drop-target>this is the drop target</drop-target>
      ");
                await Page.EvaluateAsync(@"({ effectAllowed, dropEffect }) => {
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
                await Page.HoverAsync("div");
                await Page.Mouse.DownAsync();
                await Page.HoverAsync("drop-target");
                await Page.Mouse.UpAsync();
                return await Page.EvaluateAsync<bool>("dropped");
            }
        }

        [UpstreamTest("DD-028 should report the default drop effect from effectAllowed")]
        public async Task ShouldReportTheDefaultDropEffectFromEffectAllowed()
        {
            Info.Fixme(BrowserName == "webkit" && IsLinux && !Headless, "https://github.com/microsoft/playwright/issues/21646");

            await Page.SetContentAsync(@"
      <div draggable=""true"">drag target</div>
      <drop-target>this is the drop target</drop-target>
    ");
            await Page.EvaluateAsync(@"() => {
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
            await Page.HoverAsync("div");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("drop-target");
            await Page.Mouse.UpAsync();
            Assert.That(await Page.EvaluateAsync<string>("dropEffect"), Is.EqualTo("move"));
        }

        [UpstreamTest("DD-009 should work if the drag is canceled")]
        public async Task ShouldWorkIfTheDragIsCanceled()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            await Page.EvaluateAsync(@"() => {
      document.body.addEventListener('dragstart', event => {
        event.preventDefault();
      }, false);
    }");
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();
            Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(false));
        }

        [UpstreamTest("DD-010 should work if the drag event is captured but not canceled")]
        public async Task ShouldWorkIfTheDragEventIsCapturedButNotCanceled()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            await Page.EvaluateAsync(@"() => {
      document.body.addEventListener('dragstart', event => {
        event.stopImmediatePropagation();
      }, false);
    }");
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();
            Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true));
        }

        [UpstreamTest("DD-011 should be able to drag the mouse in a frame")]
        public async Task ShouldBeAbleToDragTheMouseInAFrame()
        {
            await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
            var eventsHandle = await TrackEvents(await Page.Frames[1].QuerySelectorAsync("html"));
            await Page.Mouse.MoveAsync(30, 30);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync(60, 60);
            await Page.Mouse.UpAsync();
            Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "mousemove at 20;20", "mousedown at 20;20", "mousemove at 50;50", "mouseup at 50;50" }));
        }

        // .NET: upstream also takes the toImpl fixture, which it does not use and which .NET does not have.
        [UpstreamTest("DD-012 should work if a frame is stalled")]
        public async Task ShouldWorkIfAFrameIsStalled()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
            await Page.RouteAsync("**/empty.html", route =>
            {
                routePromise.SetResult(route);
            });
            IgnoreErrors(AttachFrame(Page, "frame", Server.EmptyPage));
            var route = await routePromise.Task;
            await Page.HoverAsync("#source");
            await Page.Mouse.DownAsync();
            await Page.HoverAsync("#target");
            await Page.Mouse.UpAsync();
            await route.AbortAsync();
            Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        }

        [UpstreamTest("DD-013 should work with the helper method")]
        public async Task ShouldWorkWithTheHelperMethod()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            await Page.DragAndDropAsync("#source", "#target");
            Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
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
        [TestCaseSource(nameof(Drags))]
        public async Task ShouldWithTweenedMouseMovement(string title, Func<IPage, int?, Task> drag)
        {
            Info.Skip(!Headless, "actual mouse interferes with the exact mousemove events");

            await Page.SetContentAsync(@"
        <body style=""margin: 0; padding: 0;"">
          <div style=""width:100px;height:100px;background:red;"" id=""red""></div>
          <div style=""width:300px;height:100px;background:blue;"" id=""blue""></div>
        </body>
      ");
            var eventsHandle = await Page.EvaluateHandleAsync(@"() => {
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
            await drag(Page, 4);
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
        public async Task ShouldAllowSpecifyingThePosition()
        {
            await Page.SetContentAsync(@"
      <div style=""width:100px;height:100px;background:red;"" id=""red"">
      </div>
      <div style=""width:100px;height:100px;background:blue;"" id=""blue"">
      </div>
    ");
            var eventsHandle = await Page.EvaluateHandleAsync(@"() => {
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
            await Page.DragAndDropAsync("#red", "#blue", new()
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
        public async Task ShouldWorkWithLocators()
        {
            await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
            await Page.Locator("#source").DragToAsync(Page.Locator("#target"));
            Assert.That(await Page.EvalOnSelectorAsync<bool>("#target", "target => target.contains(document.querySelector('#source'))"), Is.EqualTo(true)); // could not find source in target
        }
    }

    [UpstreamTest("DD-017 should work if not doing a drag")]
    public async Task ShouldWorkIfNotDoingADrag()
    {
        var eventsHandle = await TrackEvents(await Page.QuerySelectorAsync("html"));
        await Page.Mouse.MoveAsync(50, 50);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(100, 100);
        await Page.Mouse.UpAsync();
        Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[] { "mousemove at 50;50", "mousedown at 50;50", "mousemove at 100;100", "mouseup at 100;100" }));
    }

    [UpstreamTest("DD-018 should report event.buttons")]
    public async Task ShouldReportEventButtons()
    {
        var logsHandle = await Page.EvaluateHandleAsync(@"async () => {
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
        await Page.Mouse.MoveAsync(20, 20);
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(40, 40);
        await Page.Mouse.UpAsync();
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
    public async Task ShouldHandleCustomDataTransfer()
    {
        Info.Fixme(BrowserName == "webkit" && IsWindows);
        await Page.SetContentAsync("<button draggable=\"true\">Draggable</button>");

        await Page.EvaluateAsync(@"() => {
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

        await Page.HoverAsync("[draggable=\"true\"]");
        await Page.Mouse.DownAsync();
        await Page.Mouse.MoveAsync(100, 100);
        await Page.Mouse.UpAsync();

        ExpectToEqual(await Page.EvaluateAsync<object>("() => window.__result"), new
        {
            types = new[] { "custom-type" },
            data = "Hello World",
        });
    }

    [UpstreamTest("DD-020 what happens when dragging element is destroyed")]
    public async Task WhatHappensWhenDraggingElementIsDestroyed()
    {
        await Page.SetContentAsync(@"
    <button draggable=""true"">Draggable</button>
    <div id=target>drop here</div>
  ");

        await Page.EvaluateAsync(@"() => {
    document.querySelector('#target').addEventListener('dragover', event => {
      document.querySelector('button')?.remove();
    }, false);

    document.querySelector('#target').addEventListener('drop', event => {
      document.querySelector('#target').textContent = 'dropped';
    }, false);
  }");

        await Page.Locator("button").DragToAsync(Page.Locator("div"));
        await Expect(Page.Locator("div")).ToHaveTextAsync("drop here");
    }
}
