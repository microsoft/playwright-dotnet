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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/locator-dispatchevent-touch.spec.ts.</summary>
public class LocatorDispatcheventTouchTests : UpstreamPageTest
{
    // it.use({ hasTouch: true });
    public override BrowserNewContextOptions ContextOptions() => new(base.ContextOptions()) { HasTouch = true };

    [UpstreamTest("TA-010 should support touch points in touch event arguments")]
    public async Task ShouldSupportTouchPointsInTouchEventArguments()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
      <div data-testid='outer' style=""position: absolute; width: 120px; height: 120px; background-color: red;"">
        <div data-testid='inner' style=""position: absolute; width: 100px; height: 100px; top: 10px; left: 10px; background-color: green; z-index: 3;"">inner</div>
      </div>");
        var outer = Page.GetByTestId("outer");
        await outer.EvaluateAsync(@"el => {
    const events = [];
    window.events = events;
    el.addEventListener('touchstart', e => events.push('touchstart: ' + [...e.touches].map(t => `${t.constructor.name}(id: ${t.identifier}, clientX: ${t.clientX}, clientY: ${t.clientY})`)));
    el.addEventListener('touchmove', e => events.push('touchmove: ' + [...e.touches].map(t => `${t.constructor.name}(id: ${t.identifier}, clientX: ${t.clientX}, clientY: ${t.clientY})`)));
    el.addEventListener('touchend', e => events.push('touchend: ' + [...e.touches].map(t => `${t.constructor.name}(id: ${t.identifier}, clientX: ${t.clientX}, clientY: ${t.clientY})`)));
  }");

        var touches = new[] { new { identifier = 0, clientX = 61, clientY = 60 }, new { identifier = 1, clientX = 59, clientY = 60 } };
        var inner = Page.GetByTestId("inner");
        await inner.DispatchEventAsync("touchstart", new
        {
            touches,
            changedTouches = touches,
            targetTouches = touches,
        });
        await inner.DispatchEventAsync("touchmove", new
        {
            touches,
            changedTouches = touches,
            targetTouches = touches,
        });
        await inner.DispatchEventAsync("touchend", new
        {
            touches = Array.Empty<object>(),
            changedTouches = touches,
            targetTouches = Array.Empty<object>(),
        });
        Assert.That(await Page.EvaluateAsync<string[]>("() => window.events"), Is.EqualTo(new[]
        {
            "touchstart: Touch(id: 0, clientX: 61, clientY: 60),Touch(id: 1, clientX: 59, clientY: 60)",
            "touchmove: Touch(id: 0, clientX: 61, clientY: 60),Touch(id: 1, clientX: 59, clientY: 60)",
            "touchend: ",
        }));
    }
}
