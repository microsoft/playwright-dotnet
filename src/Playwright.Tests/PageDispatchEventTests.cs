/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

namespace Microsoft.Playwright.Tests;

///<playwright-file>dispatchevent.spec.ts</playwright-file>
public class PageDispatchEventTests : PageTestEx
{
    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click event")]
    public async Task ShouldDispatchClickEvent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click event properties")]
    public async Task ShouldDispatchClickEventProperties()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.True(await Page.EvaluateAsync<bool>("() => bubbles"));
        Assert.True(await Page.EvaluateAsync<bool>("() => cancelable"));
        Assert.True(await Page.EvaluateAsync<bool>("() => composed"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click svg")]
    public async Task ShouldDispatchClickSvg()
    {
        await Page.SetContentAsync(@"
            <svg height=""100"" width=""100"">
                <circle onclick=""javascript:window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width = ""3"" fill=""red"" />
            </svg>");

        await Page.DispatchEventAsync("circle", "click");
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => window.__CLICKED"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click on a span with an inline element inside")]
    public async Task ShouldDispatchClickOnASpanWithAnInlineElementInside()
    {
        await Page.SetContentAsync(@"
              <style>
                  span::before {
                    content: 'q';
                  }
              </style>
              <span onclick='javascript:window.CLICKED=42'></span>");

        await Page.DispatchEventAsync("span", "click");
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => window.CLICKED"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click after navigation")]
    public async Task ShouldDispatchClickAfterNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click after a cross origin navigation")]
    public async Task ShouldDispatchClickAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        await Page.GotoAsync(Server.CrossProcessPrefix + "/input/button.html");
        await Page.DispatchEventAsync("button", "click");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should not fail when element is blocked on hover")]
    public async Task ShouldNotFailWhenElementIsBlockedOnHover()
    {
        await Page.SetContentAsync(@"
              <style>
                container { display: block; position: relative; width: 200px; height: 50px; }
                div, button { position: absolute; left: 0; top: 0; bottom: 0; right: 0; }
                div { pointer-events: none; }
                container:hover div { pointer-events: auto; background: red; }
            </style>
            <container>
                <button onclick=""window.clicked = true"">Click me</button>
                <div></div>
            </container>");

        await Page.DispatchEventAsync("button", "click");
        Assert.True(await Page.EvaluateAsync<bool>("() => window.clicked"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click when node is added in shadow dom")]
    public async Task ShouldDispatchClickWhenNodeIsAddedInShadowDom()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var watchdog = Page.DispatchEventAsync("span", "click");

        await Page.EvaluateAsync(@"() => {
              const div = document.createElement('div');
              div.attachShadow({mode: 'open'});
              document.body.appendChild(div);
            }");
        await Page.EvaluateAsync("() => new Promise(f => setTimeout(f, 100))");

        await Page.EvaluateAsync(@"() => {
              const span = document.createElement('span');
              span.textContent = 'Hello from shadow';
              span.addEventListener('click', () => window.clicked = true);
              document.querySelector('div').shadowRoot.appendChild(span);
            }");

        await watchdog;
        Assert.True(await Page.EvaluateAsync<bool>("() => window.clicked"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should be atomic")]
    public async Task ShouldBeAtomic()
    {
        const string createDummySelector = @"({
                create(root, target) {},
                query(root, selector) {
                    const result = root.querySelector(selector);
                    if (result)
                    Promise.resolve().then(() => result.onclick = '');
                    return result;
                },
                queryAll(root, selector) {
                    const result = Array.from(root.querySelectorAll(selector));
                    for (const e of result)
                    Promise.resolve().then(() => e.onclick = null);
                    return result;
                }
            })";

        await TestUtils.RegisterEngineAsync(Playwright, "page-dispatchevent", createDummySelector);
        await Page.SetContentAsync("<div onclick=\"window._clicked = true\">Hello</div>");
        await Page.DispatchEventAsync("page-dispatchevent=div", "click");
        Assert.True(await Page.EvaluateAsync<bool>("() => window['_clicked']"));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "Page.dispatchEvent(drag)", "should dispatch drag drop events")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ShouldDispatchDragDropEvents()
    {
        await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
        var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
        await Page.DispatchEventAsync("#source", "dragstart", new { dataTransfer });
        await Page.DispatchEventAsync("#target", "drop", new { dataTransfer });

        var source = await Page.QuerySelectorAsync("#source");
        var target = await Page.QuerySelectorAsync("#target");
        Assert.True(await Page.EvaluateAsync<bool>(@"() => {
                return source.parentElement === target;
            }", new { source, target }));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "Page.dispatchEvent(drag)", "should dispatch drag drop events")]
    [Skip(SkipAttribute.Targets.Webkit)]
    public async Task ElementHandleShouldDispatchDragDropEvents()
    {
        await Page.GotoAsync(Server.Prefix + "/drag-n-drop.html");
        var dataTransfer = await Page.EvaluateHandleAsync("() => new DataTransfer()");
        var source = await Page.QuerySelectorAsync("#source");
        await source.DispatchEventAsync("dragstart", new { dataTransfer });
        var target = await Page.QuerySelectorAsync("#target");
        await target.DispatchEventAsync("drop", new { dataTransfer });

        Assert.True(await Page.EvaluateAsync<bool>(@"() => {
                return source.parentElement === target;
            }", new { source, target }));
    }

    [PlaywrightTest("page-dispatchevent.spec.ts", "should dispatch click event")]
    public async Task ElementHandleShouldDispatchClickEvent()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await button.DispatchEventAsync("click");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => result"));
    }
}
