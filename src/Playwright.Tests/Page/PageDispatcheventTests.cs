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
public class PageDispatcheventTests : UpstreamPageTest
{
    [UpstreamTest("DE-001 should dispatch click event @smoke")]
    public async Task ShouldDispatchClickEvent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-002 should dispatch click event properties")]
    public async Task ShouldDispatchClickEventProperties()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.That(await Page.EvaluateAsync<bool>("bubbles"), Is.True);
        Assert.That(await Page.EvaluateAsync<bool>("cancelable"), Is.True);
        Assert.That(await Page.EvaluateAsync<bool>("composed"), Is.True);
    }

    [UpstreamTest("DE-003 should dispatch click svg")]
    public async Task ShouldDispatchClickSvg()
    {
        await Page.SetContentAsync(@"
    <svg height=""100"" width=""100"">
      <circle onclick=""javascript:window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width=""3"" fill=""red"" />
    </svg>
  ");
        await Page.DispatchEventAsync("circle", "click");
        Assert.That(await Page.EvaluateAsync<int>("() => window['__CLICKED']"), Is.EqualTo(42));
    }

    [UpstreamTest("DE-004 should dispatch click on a span with an inline element inside")]
    public async Task ShouldDispatchClickOnASpanWithAnInlineElementInside()
    {
        await Page.SetContentAsync(@"
    <style>
    span::before {
      content: 'q';
    }
    </style>
    <span onclick='javascript:window.CLICKED=42'></span>
  ");
        await Page.DispatchEventAsync("span", "click");
        Assert.That(await Page.EvaluateAsync<int>("() => window['CLICKED']"), Is.EqualTo(42));
    }

    [UpstreamTest("DE-005 should dispatch click after navigation ")]
    public async Task ShouldDispatchClickAfterNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-006 should dispatch click after a cross origin navigation ")]
    public async Task ShouldDispatchClickAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        await Page.GotoAsync(Server.CrossProcessPrefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-007 should not fail when element is blocked on hover")]
    public async Task ShouldNotFailWhenElementIsBlockedOnHover()
    {
        await Page.SetContentAsync(@"<style>
    container { display: block; position: relative; width: 200px; height: 50px; }
    div, button { position: absolute; left: 0; top: 0; bottom: 0; right: 0; }
    div { pointer-events: none; }
    container:hover div { pointer-events: auto; background: red; }
  </style>
  <container>
    <button onclick=""window.clicked=true"">Click me</button>
    <div></div>
  </container>");
        await Page.DispatchEventAsync("button", "click");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['clicked']"), Is.True);
    }

    [UpstreamTest("DE-008 should dispatch click when node is added in shadow dom")]
    public async Task ShouldDispatchClickWhenNodeIsAddedInShadowDom()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var watchdog = Page.DispatchEventAsync("span", "click");
        await Page.EvaluateAsync(@"() => {
    const div = document.createElement('div');
    div.attachShadow({ mode: 'open' });
    document.body.appendChild(div);
  }");
        await Page.WaitForTimeoutAsync(100);
        await Page.EvaluateAsync(@"() => {
    const span = document.createElement('span');
    span.textContent = 'Hello from shadow';
    span.addEventListener('click', () => window['clicked'] = true);
    document.querySelector('div').shadowRoot.appendChild(span);
  }");
        await watchdog;
        Assert.That(await Page.EvaluateAsync<bool>("() => window['clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("DE-009 should be atomic")]
    public async Task ShouldBeAtomic()
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
        await Playwright.Selectors.RegisterAsync("dispatchEvent", new() { Script = $"({createDummySelector})(undefined)" });
        await Page.GotoAsync("data:text/html,<div onclick=\"window._clicked=true\">Hello</div>");
        await Page.DispatchEventAsync("dispatchEvent=div", "click");
        Assert.That(await Page.EvaluateAsync<bool>("() => window['_clicked']"), Is.EqualTo(true));
    }

    [UpstreamTest("DE-010 should dispatch drag drop events")]
    public async Task ShouldDispatchDragDropEvents()
    {
        await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
        var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
        await Page.DispatchEventAsync("#source", "dragstart", new { dataTransfer });
        await Page.DispatchEventAsync("#target", "drop", new { dataTransfer });
        var source = await Page.QuerySelectorAsync("#source");
        var target = await Page.QuerySelectorAsync("#target");
        Assert.That(await Page.EvaluateAsync<bool>(@"({ source, target }) => {
    return source.parentElement === target;
  }", new { source, target }), Is.True);
    }

    [UpstreamTest("DE-011 should dispatch drag drop events via ElementHandles")]
    public async Task ShouldDispatchDragDropEventsViaElementHandles()
    {
        await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
        var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
        var source = await Page.QuerySelectorAsync("#source");
        await source.DispatchEventAsync("dragstart", new { dataTransfer });
        var target = await Page.QuerySelectorAsync("#target");
        await target.DispatchEventAsync("drop", new { dataTransfer });
        Assert.That(await Page.EvaluateAsync<bool>(@"({ source, target }) => {
    return source.parentElement === target;
  }", new { source, target }), Is.True);
    }

    [UpstreamTest("DE-012 should dispatch click event via ElementHandles")]
    public async Task ShouldDispatchClickEventViaElementHandles()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await button.DispatchEventAsync("click");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("DE-013 should dispatch wheel event")]
    public async Task ShouldDispatchWheelEvent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        var eventsHandle = await Page.Locator("body").EvaluateHandleAsync(@"e => {
    const events = [];
    e.addEventListener('wheel', event => {
      events.push(event);
      console.log(event);
    });
    return events;
  }");
        await Page.Locator("body").DispatchEventAsync("wheel", new { deltaX = 100, deltaY = 200 });
        Assert.That(await eventsHandle.EvaluateAsync<int>("e => e.length"), Is.EqualTo(1));
        Assert.That(await eventsHandle.EvaluateAsync<bool>("e => e[0] instanceof WheelEvent"), Is.True);
        ExpectToEqual(await eventsHandle.EvaluateAsync<object>("e => ({ deltaX: e[0].deltaX, deltaY: e[0].deltaY })"), new { deltaX = 100, deltaY = 200 });
    }

    [UpstreamTest("DE-014 should dispatch device orientation event")]
    public async Task ShouldDispatchDeviceOrientationEvent()
    {
        Info.Skip(IsAndroid, "DeviceOrientationEvent is only available in a secure context. While Androids loopback is not treated as secure.");
        await Page.GotoAsync(Server.Prefix + "/device-orientation.html");
        await Page.Locator("html").DispatchEventAsync("deviceorientation", new { alpha = 10, beta = 20, gamma = 30 });
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Oriented"));
        Assert.That(await Page.EvaluateAsync<int>("alpha"), Is.EqualTo(10));
        Assert.That(await Page.EvaluateAsync<int>("beta"), Is.EqualTo(20));
        Assert.That(await Page.EvaluateAsync<int>("gamma"), Is.EqualTo(30));
        Assert.That(await Page.EvaluateAsync<bool>("absolute"), Is.False);
    }

    [UpstreamTest("DE-015 should dispatch absolute device orientation event")]
    public async Task ShouldDispatchAbsoluteDeviceOrientationEvent()
    {
        Info.Skip(IsAndroid, "DeviceOrientationEvent is only available in a secure context. While Androids loopback is not treated as secure.");
        await Page.GotoAsync(Server.Prefix + "/device-orientation.html");
        await Page.Locator("html").DispatchEventAsync("deviceorientationabsolute", new { alpha = 10, beta = 20, gamma = 30, absolute = true });
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Oriented"));
        Assert.That(await Page.EvaluateAsync<int>("alpha"), Is.EqualTo(10));
        Assert.That(await Page.EvaluateAsync<int>("beta"), Is.EqualTo(20));
        Assert.That(await Page.EvaluateAsync<int>("gamma"), Is.EqualTo(30));
        Assert.That(await Page.EvaluateAsync<bool>("absolute"), Is.True);
    }

    [UpstreamTest("DE-016 should dispatch device motion event")]
    public async Task ShouldDispatchDeviceMotionEvent()
    {
        Info.Skip(IsAndroid, "DeviceOrientationEvent is only available in a secure context. While Androids loopback is not treated as secure.");
        await Page.GotoAsync(Server.Prefix + "/device-motion.html");
        await Page.Locator("html").DispatchEventAsync("devicemotion", new
        {
            acceleration = new { x = 10, y = 20, z = 30 },
            accelerationIncludingGravity = new { x = 15, y = 25, z = 35 },
            rotationRate = new { alpha = 5, beta = 10, gamma = 15 },
            interval = 16,
        });
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Moved"));
        Assert.That(await Page.EvaluateAsync<int>("acceleration.x"), Is.EqualTo(10));
        Assert.That(await Page.EvaluateAsync<int>("acceleration.y"), Is.EqualTo(20));
        Assert.That(await Page.EvaluateAsync<int>("acceleration.z"), Is.EqualTo(30));
        Assert.That(await Page.EvaluateAsync<int>("accelerationIncludingGravity.x"), Is.EqualTo(15));
        Assert.That(await Page.EvaluateAsync<int>("accelerationIncludingGravity.y"), Is.EqualTo(25));
        Assert.That(await Page.EvaluateAsync<int>("accelerationIncludingGravity.z"), Is.EqualTo(35));
        Assert.That(await Page.EvaluateAsync<int>("rotationRate.alpha"), Is.EqualTo(5));
        Assert.That(await Page.EvaluateAsync<int>("rotationRate.beta"), Is.EqualTo(10));
        Assert.That(await Page.EvaluateAsync<int>("rotationRate.gamma"), Is.EqualTo(15));
        Assert.That(await Page.EvaluateAsync<int>("interval"), Is.EqualTo(16));
    }

    [UpstreamTest("DE-017 should throw if argument is from different frame")]
    public async Task ShouldThrowIfArgumentIsFromDifferentFrame()
    {
        await Page.GotoAsync(Server.Prefix + "/frames/one-frame.html");
        {
            var dataTransfer = await Page.Frames[1].EvaluateHandleAsync("() => new DataTransfer()");
            await Page.FrameLocator("iframe").Locator("div").DispatchEventAsync("drop", new { dataTransfer });
        }
        {
            var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
            var error = await ThrowsAsync<PlaywrightException>(() => Page.FrameLocator("iframe").Locator("div").DispatchEventAsync("drop", new { dataTransfer }));
            Assert.That(error.Message, Does.Contain("JSHandles can be evaluated only in the context they were created!"));
        }
    }
}
