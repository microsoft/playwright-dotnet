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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/tap.spec.ts.</summary>
public class TapTests
{
    // it.use({ hasTouch: true });
    [Fixture]
    public static BrowserNewContextOptions ContextOptions(BrowserNewContextOptions contextOptions) => new(contextOptions) { HasTouch = true };

    [UpstreamTest("TA-001 should send all of the correct events @smoke")]
    public async Task ShouldSendAllOfTheCorrectEvents(IPage page)
    {
        await page.SetContentAsync(@"
  <div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>
  <div id=""b"" style=""background: pink; width: 50px; height: 50px"">b</div>
");
        await page.TapAsync("#a");
        var eventsHandle = await TrackEvents(await page.QuerySelectorAsync("#b"));
        await page.TapAsync("#b");
        // webkit doesn't send pointerenter or pointerleave or mouseout
        Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[]
        {
            "pointerover",  "pointerenter",
            "pointerdown",  "touchstart",
            "pointerup",    "pointerout",
            "pointerleave", "touchend",
            "mouseover",    "mouseenter",
            "mousemove",    "mousedown",
            "mouseup",      "click",
        }));

        await eventsHandle.EvaluateAsync("events => events.length = 0");
        await page.Locator("#b").EvaluateAsync(@"target => {
    target.addEventListener('pointerdown', event => event.preventDefault(), { once: true });
  }");
        await page.TapAsync("#b");
        var events = (await eventsHandle.JsonValueAsync<string[]>()).Where(@event => new[] { "pointerdown", "pointerup", "mousedown", "mouseup", "click" }.Contains(@event)).ToArray();
        Assert.That(events, Is.EqualTo(new[] { "pointerdown", "pointerup", "click" }));
    }

    [UpstreamTest("TA-002 trial run should not tap")]
    public async Task TrialRunShouldNotTap(IPage page)
    {
        await page.SetContentAsync(@"
    <div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>
    <div id=""b"" style=""background: pink; width: 50px; height: 50px"">b</div>
  ");
        await page.TapAsync("#a");
        var eventsHandle = await TrackEvents(await page.QuerySelectorAsync("#b"));
        await page.TapAsync("#b", new() { Trial = true });
        var expected = new[] { "pointerover", "pointerenter", "pointerout", "pointerleave" };
        Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(expected));
    }

    [UpstreamTest("TA-003 should not send mouse events touchstart is canceled")]
    public async Task ShouldNotSendMouseEventsTouchstartIsCanceled(IPage page)
    {
        await page.SetContentAsync("<div style=\"width: 50px; height: 50px; background: red\">");
        await page.EvaluateAsync(@"() => {
    // touchstart is not cancelable unless passive is false
    document.addEventListener('touchstart', t => t.preventDefault(), { passive: false });
  }");
        var eventsHandle = await TrackEvents(await page.QuerySelectorAsync("div"));
        await page.TapAsync("div");
        Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[]
        {
            "pointerover",  "pointerenter",
            "pointerdown",  "touchstart",
            "pointerup",    "pointerout",
            "pointerleave", "touchend",
        }));
    }

    [UpstreamTest("TA-004 should not send mouse events when touchend is canceled")]
    public async Task ShouldNotSendMouseEventsWhenTouchendIsCanceled(IPage page)
    {
        await page.SetContentAsync("<div style=\"width: 50px; height: 50px; background: red\">");
        await page.EvaluateAsync(@"() => {
    document.addEventListener('touchend', t => t.preventDefault());
  }");
        var eventsHandle = await TrackEvents(await page.QuerySelectorAsync("div"));
        await page.TapAsync("div");
        Assert.That(await eventsHandle.JsonValueAsync<string[]>(), Is.EqualTo(new[]
        {
            "pointerover",  "pointerenter",
            "pointerdown",  "touchstart",
            "pointerup",    "pointerout",
            "pointerleave", "touchend",
        }));
    }

    [UpstreamTest("TA-005 should not wait for a navigation caused by a tap")]
    public async Task ShouldNotWaitForANavigationCausedByATap(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync("<a href=\"/intercept-this.html\">link</a>;");
        // .NET: new Promise(resolve => server.setRoute('/intercept-this.html', resolve)) is a TaskCompletionSource that the route completes.
        var promise = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        server.SetRoute("/intercept-this.html", (req, res) => promise.TrySetResult(req));
        await PromiseAll(
            promise.Task,
            page.TapAsync("a"));
    }

    [UpstreamTest("TA-006 should work with modifiers")]
    public async Task ShouldWorkWithModifiers(IPage page)
    {
        await page.SetContentAsync("hello world");
        var altKeyPromise = page.EvaluateAsync<bool>(@"() => new Promise(resolve => {
    document.addEventListener('touchstart', event => {
      resolve(event.altKey);
    }, { passive: false });
  })");
        // make sure the evals hit the page
        await page.EvaluateAsync("() => void 0");
        await page.TapAsync("body", new()
        {
            Modifiers = new[] { KeyboardModifier.Alt },
        });
        Assert.That(await altKeyPromise, Is.EqualTo(true));
    }

    [UpstreamTest("TA-007 should send well formed touch points")]
    public async Task ShouldSendWellFormedTouchPoints(IPage page)
    {
        var promises = PromiseAll(
            page.EvaluateAsync<object>(@"() => new Promise(resolve => {
      document.addEventListener('touchstart', event => {
        resolve([...event.touches].map(t => ({
          identifier: t.identifier,
          clientX: t.clientX,
          clientY: t.clientY,
          pageX: t.pageX,
          pageY: t.pageY,
          radiusX: 'radiusX' in t ? t.radiusX : t['webkitRadiusX'],
          radiusY: 'radiusY' in t ? t.radiusY : t['webkitRadiusY'],
          rotationAngle: 'rotationAngle' in t ? t.rotationAngle : t['webkitRotationAngle'],
          force: 'force' in t ? t.force : t['webkitForce'],
        })));
      }, false);
    })"),
            page.EvaluateAsync<object>(@"() => new Promise(resolve => {
      document.addEventListener('touchend', event => {
        resolve([...event.touches].map(t => ({
          identifier: t.identifier,
          clientX: t.clientX,
          clientY: t.clientY,
          pageX: t.pageX,
          pageY: t.pageY,
          radiusX: 'radiusX' in t ? t.radiusX : t['webkitRadiusX'],
          radiusY: 'radiusY' in t ? t.radiusY : t['webkitRadiusY'],
          rotationAngle: 'rotationAngle' in t ? t.rotationAngle : t['webkitRotationAngle'],
          force: 'force' in t ? t.force : t['webkitForce'],
        })));
      }, false);
    })"));
        // make sure the evals hit the page
        await page.EvaluateAsync("() => void 0");
        await page.Touchscreen.TapAsync(40, 60);
        var (touchstart, touchend) = await promises;

        ExpectToEqual(touchstart, new[]
        {
            new
            {
                clientX = 40,
                clientY = 60,
                force = 1,
                identifier = 0,
                pageX = 40,
                pageY = 60,
                radiusX = 1,
                radiusY = 1,
                rotationAngle = 0,
            },
        });
        ExpectToEqual(touchend, Array.Empty<object>());
    }

    [UpstreamTest("TA-008 should wait until an element is visible to tap it")]
    public async Task ShouldWaitUntilAnElementIsVisibleToTapIt(IPage page)
    {
        var div = (IElementHandle)await page.EvaluateHandleAsync(@"() => {
    const button = document.createElement('button');
    button.textContent = 'not clicked';
    document.body.appendChild(button);
    button.style.display = 'none';
    return button;
  }");
        var tapPromise = div.TapAsync();
        await div.EvaluateAsync("div => div.onclick = () => div.textContent = 'clicked'");
        await div.EvaluateAsync("div => div.style.display = 'block'");
        await tapPromise;
        Assert.That(await div.TextContentAsync(), Is.EqualTo("clicked"));
    }

    private static async Task<IJSHandle> TrackEvents(IElementHandle target)
    {
        var eventsHandle = await target.EvaluateHandleAsync(@"target => {
    const events = [];
    for (const event of [
      'mousedown', 'mouseenter', 'mouseleave', 'mousemove', 'mouseout', 'mouseover', 'mouseup', 'click',
      'pointercancel', 'pointerdown', 'pointerenter', 'pointerleave', 'pointermove', 'pointerout', 'pointerover', 'pointerup',
      'touchstart', 'touchend', 'touchmove', 'touchcancel',
    ])
      target.addEventListener(event, () => events.push(event), false);
    return events;
  }");
        return eventsHandle;
    }

    public class Locators
    {
        [UpstreamTest("TA-009 should send all of the correct events")]
        public async Task ShouldSendAllOfTheCorrectEvents(IPage page)
        {
            await page.SetContentAsync(@"
      <div id=""a"" style=""background: lightblue; width: 50px; height: 50px"">a</div>
      <div id=""b"" style=""background: pink; width: 50px; height: 50px"">b</div>
    ");
            await page.Locator("#a").TapAsync();
            await page.Locator("#b").TapAsync();
        }
    }
}
