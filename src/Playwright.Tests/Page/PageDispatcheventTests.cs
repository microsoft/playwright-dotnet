/**
 * Copyright Microsoft Corporation. All rights reserved.
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

/// <summary>Port of tests/page/page-dispatchevent.spec.ts.</summary>
public class PageDispatcheventTests
{
    [UpstreamTest("DE-001 should dispatch click event @smoke")]
    public async Task ShouldDispatchClickEvent(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.DispatchEventAsync("button", "click");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-002 should dispatch click event properties")]
    public async Task ShouldDispatchClickEventProperties(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.DispatchEventAsync("button", "click");
        Assert.That(await page.EvaluateAsync<bool>("bubbles"), Is.True);
        Assert.That(await page.EvaluateAsync<bool>("cancelable"), Is.True);
        Assert.That(await page.EvaluateAsync<bool>("composed"), Is.True);
    }

    [UpstreamTest("DE-003 should dispatch click svg")]
    public async Task ShouldDispatchClickSvg(IPage page)
    {
        await page.SetContentAsync(@"
    <svg height=""100"" width=""100"">
      <circle onclick=""javascript:window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width=""3"" fill=""red"" />
    </svg>
  ");
        await page.DispatchEventAsync("circle", "click");
        Assert.That(await page.EvaluateAsync<int>("() => window['__CLICKED']"), Is.EqualTo(42));
    }

    [UpstreamTest("DE-004 should dispatch click on a span with an inline element inside")]
    public async Task ShouldDispatchClickOnASpanWithAnInlineElementInside(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
    span::before {
      content: 'q';
    }
    </style>
    <span onclick='javascript:window.CLICKED=42'></span>
  ");
        await page.DispatchEventAsync("span", "click");
        Assert.That(await page.EvaluateAsync<int>("() => window['CLICKED']"), Is.EqualTo(42));
    }

    [UpstreamTest("DE-005 should dispatch click after navigation ")]
    public async Task ShouldDispatchClickAfterNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.DispatchEventAsync("button", "click");
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.DispatchEventAsync("button", "click");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-006 should dispatch click after a cross origin navigation ")]
    public async Task ShouldDispatchClickAfterACrossOriginNavigation(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        await page.DispatchEventAsync("button", "click");
        await page.GotoAsync(server.CrossProcessPrefix + "/input/button.html");
        await page.DispatchEventAsync("button", "click");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-007 should not fail when element is blocked on hover")]
    public async Task ShouldNotFailWhenElementIsBlockedOnHover(IPage page)
    {
        await page.SetContentAsync(@"<style>
    container { display: block; position: relative; width: 200px; height: 50px; }
    div, button { position: absolute; left: 0; top: 0; bottom: 0; right: 0; }
    div { pointer-events: none; }
    container:hover div { pointer-events: auto; background: red; }
  </style>
  <container>
    <button onclick=""window.clicked=true"">Click me</button>
    <div></div>
  </container>");
        await page.DispatchEventAsync("button", "click");
        Assert.That(await page.EvaluateAsync<bool>("() => window['clicked']"), Is.True);
    }

    [UpstreamTest("DE-008 should dispatch click when node is added in shadow dom")]
    public async Task ShouldDispatchClickWhenNodeIsAddedInShadowDom(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var watchdog = page.DispatchEventAsync("span", "click");
        await page.EvaluateAsync(@"() => {
    const div = document.createElement('div');
    div.attachShadow({ mode: 'open' });
    document.body.appendChild(div);
  }");
        await page.WaitForTimeoutAsync(100);
        await page.EvaluateAsync(@"() => {
    const span = document.createElement('span');
    span.textContent = 'Hello from shadow';
    span.addEventListener('click', () => window['clicked'] = true);
    document.querySelector('div').shadowRoot.appendChild(span);
  }");
        await watchdog;
        Assert.That(await page.EvaluateAsync<bool>("() => window['clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("DE-009 should be atomic")]
    public async Task ShouldBeAtomic(IPlaywright playwright, IPage page)
    {
        const string createDummySelector = @"() => ({
    query(root, selector) {
      const result = root.querySelector(selector);
      if (result)
        void Promise.resolve().then(() => result.onclick = '');
      return result;
    },
    queryAll(root, selector) {
      const result = Array.from(root.querySelectorAll(selector));
      for (const e of result)
        void Promise.resolve().then(() => e.onclick = null);
      return result;
    }
  })";
        // .NET: a function is the script that the Node.js client makes of it, `(${fun})(undefined)`.
        await playwright.Selectors.RegisterAsync("dispatchEvent", new() { Script = $"({createDummySelector})(undefined)" });
        await page.GotoAsync("data:text/html,<div onclick=\"window._clicked=true\">Hello</div>");
        await page.DispatchEventAsync("dispatchEvent=div", "click");
        Assert.That(await page.EvaluateAsync<bool>("() => window['_clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("DE-010 should dispatch drag drop events")]
    public async Task ShouldDispatchDragDropEvents(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
        var dataTransfer = await page.EvaluateHandleAsync("() => new DataTransfer()");
        await page.DispatchEventAsync("#source", "dragstart", new { dataTransfer });
        await page.DispatchEventAsync("#target", "drop", new { dataTransfer });
        var source = await page.QuerySelectorAsync("#source");
        var target = await page.QuerySelectorAsync("#target");
        Assert.That(await page.EvaluateAsync<bool>(@"({ source, target }) => {
    return source.parentElement === target;
  }", new { source, target }), Is.True);
    }

    [UpstreamTest("DE-011 should dispatch drag drop events via ElementHandles")]
    public async Task ShouldDispatchDragDropEventsViaElementHandles(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/drag-n-drop.html");
        var dataTransfer = await page.EvaluateHandleAsync("() => new DataTransfer()");
        var source = await page.QuerySelectorAsync("#source");
        await source.DispatchEventAsync("dragstart", new { dataTransfer });
        var target = await page.QuerySelectorAsync("#target");
        await target.DispatchEventAsync("drop", new { dataTransfer });
        Assert.That(await page.EvaluateAsync<bool>(@"({ source, target }) => {
    return source.parentElement === target;
  }", new { source, target }), Is.True);
    }

    [UpstreamTest("DE-012 should dispatch click event via ElementHandles")]
    public async Task ShouldDispatchClickEventViaElementHandles(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/button.html");
        var button = await page.QuerySelectorAsync("button");
        await button.DispatchEventAsync("click");
        Assert.That(await page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-013 should dispatch wheel event")]
    public async Task ShouldDispatchWheelEvent(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        var eventsHandle = await page.Locator("body").EvaluateHandleAsync(@"e => {
    const events = [];
    e.addEventListener('wheel', event => {
      events.push(event);
      console.log(event);
    });
    return events;
  }");
        await page.Locator("body").DispatchEventAsync("wheel", new { deltaX = 100, deltaY = 200 });
        Assert.That(await eventsHandle.EvaluateAsync<int>("e => e.length"), Is.EqualTo(1));
        Assert.That(await eventsHandle.EvaluateAsync<bool>("e => e[0] instanceof WheelEvent"), Is.True);
        ExpectToEqual(await eventsHandle.EvaluateAsync<object>("e => ({ deltaX: e[0].deltaX, deltaY: e[0].deltaY })"), new { deltaX = 100, deltaY = 200 });
    }

    [UpstreamTest("DE-014 should dispatch device orientation event")]
    public async Task ShouldDispatchDeviceOrientationEvent(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid, "DeviceOrientationEvent is only available in a secure context. While Androids loopback is not treated as secure.");
        await page.GotoAsync(server.Prefix + "/device-orientation.html");
        await page.Locator("html").DispatchEventAsync("deviceorientation", new { alpha = 10, beta = 20, gamma = 30 });
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Oriented"));
        Assert.That(await page.EvaluateAsync<int>("alpha"), Is.EqualTo(10));
        Assert.That(await page.EvaluateAsync<int>("beta"), Is.EqualTo(20));
        Assert.That(await page.EvaluateAsync<int>("gamma"), Is.EqualTo(30));
        Assert.That(await page.EvaluateAsync<bool>("absolute"), Is.False);
    }

    [UpstreamTest("DE-015 should dispatch absolute device orientation event")]
    public async Task ShouldDispatchAbsoluteDeviceOrientationEvent(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid, "DeviceOrientationEvent is only available in a secure context. While Androids loopback is not treated as secure.");
        await page.GotoAsync(server.Prefix + "/device-orientation.html");
        await page.Locator("html").DispatchEventAsync("deviceorientationabsolute", new { alpha = 10, beta = 20, gamma = 30, absolute = true });
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Oriented"));
        Assert.That(await page.EvaluateAsync<int>("alpha"), Is.EqualTo(10));
        Assert.That(await page.EvaluateAsync<int>("beta"), Is.EqualTo(20));
        Assert.That(await page.EvaluateAsync<int>("gamma"), Is.EqualTo(30));
        Assert.That(await page.EvaluateAsync<bool>("absolute"), Is.True);
    }

    [UpstreamTest("DE-016 should dispatch device motion event")]
    public async Task ShouldDispatchDeviceMotionEvent(IPage page, Server server, bool isAndroid)
    {
        It.Skip(isAndroid, "DeviceOrientationEvent is only available in a secure context. While Androids loopback is not treated as secure.");
        await page.GotoAsync(server.Prefix + "/device-motion.html");
        await page.Locator("html").DispatchEventAsync("devicemotion", new
        {
            acceleration = new { x = 10, y = 20, z = 30 },
            accelerationIncludingGravity = new { x = 15, y = 25, z = 35 },
            rotationRate = new { alpha = 5, beta = 10, gamma = 15 },
            interval = 16,
        });
        Assert.That(await page.EvaluateAsync<string>("result"), Is.EqualTo("Moved"));
        Assert.That(await page.EvaluateAsync<int>("acceleration.x"), Is.EqualTo(10));
        Assert.That(await page.EvaluateAsync<int>("acceleration.y"), Is.EqualTo(20));
        Assert.That(await page.EvaluateAsync<int>("acceleration.z"), Is.EqualTo(30));
        Assert.That(await page.EvaluateAsync<int>("accelerationIncludingGravity.x"), Is.EqualTo(15));
        Assert.That(await page.EvaluateAsync<int>("accelerationIncludingGravity.y"), Is.EqualTo(25));
        Assert.That(await page.EvaluateAsync<int>("accelerationIncludingGravity.z"), Is.EqualTo(35));
        Assert.That(await page.EvaluateAsync<int>("rotationRate.alpha"), Is.EqualTo(5));
        Assert.That(await page.EvaluateAsync<int>("rotationRate.beta"), Is.EqualTo(10));
        Assert.That(await page.EvaluateAsync<int>("rotationRate.gamma"), Is.EqualTo(15));
        Assert.That(await page.EvaluateAsync<int>("interval"), Is.EqualTo(16));
    }

    [UpstreamTest("DE-017 should throw if argument is from different frame")]
    public async Task ShouldThrowIfArgumentIsFromDifferentFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/frames/one-frame.html");
        {
            var dataTransfer = await page.Frames[1].EvaluateHandleAsync("() => new DataTransfer()");
            await page.FrameLocator("iframe").Locator("div").DispatchEventAsync("drop", new { dataTransfer });
        }
        {
            var dataTransfer = await page.EvaluateHandleAsync("() => new DataTransfer()");
            var error = await ThrowsAsync<PlaywrightException>(() => page.FrameLocator("iframe").Locator("div").DispatchEventAsync("drop", new { dataTransfer }));
            Assert.That(error.Message, Does.Contain("JSHandles can be evaluated only in the context they were created!"));
        }
    }
}
