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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/hit-target.spec.ts.</summary>
public class HitTargetTests : UpstreamPageTest
{
    [UpstreamTest("HT-001 should block all events when hit target is wrong")]
    public async Task ShouldBlockAllEventsWhenHitTargetIsWrong()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => {
    const blocker = document.createElement('div');
    blocker.style.position = 'absolute';
    blocker.style.width = '400px';
    blocker.style.height = '400px';
    blocker.style.left = '0';
    blocker.style.top = '0';
    document.body.appendChild(blocker);

    const allEvents = [];
    window.allEvents = allEvents;
    for (const name of ['mousedown', 'mouseup', 'click', 'dblclick', 'auxclick', 'contextmenu', 'pointerdown', 'pointerup']) {
      window.addEventListener(name, e => allEvents.push(e.type));
      blocker.addEventListener(name, e => allEvents.push(e.type));
    }
  }");

        var error = await Catch(() => Page.ClickAsync("button", new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded."));

        // Give it some time, just in case.
        await Page.WaitForTimeoutAsync(1000);
        var allEvents = await Page.EvaluateAsync<string[]>("() => window.allEvents");
        Assert.That(allEvents, Is.EqualTo(Array.Empty<string>()));
    }

    [UpstreamTest("HT-002 should block click when mousedown fails")]
    public async Task ShouldBlockClickWhenMousedownFails()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
    button.addEventListener('mousemove', () => {
      button.style.marginLeft = '100px';
    });

    const allEvents = [];
    window.allEvents = allEvents;
    for (const name of ['mousemove', 'mousedown', 'mouseup', 'click', 'dblclick', 'auxclick', 'contextmenu', 'pointerdown', 'pointerup'])
      button.addEventListener(name, e => allEvents.push(e.type));
  }");

        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
        var allEvents = await Page.EvaluateAsync<string[]>("() => window.allEvents");
        Assert.That(allEvents, Is.EqualTo(new[]
        {
            // First attempt failed.
            "mousemove",
            // Second attempt succeeded.
            "mousemove", "pointerdown", "mousedown", "pointerup", "mouseup", "click",
        }));
    }

    [UpstreamTest("HT-003 should click when element detaches in mousedown")]
    public async Task ShouldClickWhenElementDetachesInMousedown()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
    button.addEventListener('mousedown', () => {
      window.result = 'Mousedown';
      button.remove();
    });
  }");

        await Page.ClickAsync("button", new() { Timeout = 15000 });
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Mousedown"));
    }

    [UpstreamTest("HT-004 should block all events when hit target is wrong and element detaches")]
    public async Task ShouldBlockAllEventsWhenHitTargetIsWrongAndElementDetaches()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
    const blocker = document.createElement('div');
    blocker.style.position = 'absolute';
    blocker.style.width = '400px';
    blocker.style.height = '400px';
    blocker.style.left = '0';
    blocker.style.top = '0';
    document.body.appendChild(blocker);

    window.addEventListener('mousemove', () => button.remove());

    const allEvents = [];
    window.allEvents = allEvents;
    for (const name of ['mousedown', 'mouseup', 'click', 'dblclick', 'auxclick', 'contextmenu', 'pointerdown', 'pointerup']) {
      window.addEventListener(name, e => allEvents.push(e.type));
      blocker.addEventListener(name, e => allEvents.push(e.type));
    }
  }");

        var error = await Catch(() => Page.ClickAsync("button", new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded."));

        // Give it some time, just in case.
        await Page.WaitForTimeoutAsync(1000);
        var allEvents = await Page.EvaluateAsync<string[]>("() => window.allEvents");
        Assert.That(allEvents, Is.EqualTo(Array.Empty<string>()));
    }

    [UpstreamTest("HT-005 should not block programmatic events")]
    public async Task ShouldNotBlockProgrammaticEvents()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
    button.addEventListener('mousemove', () => {
      button.style.marginLeft = '100px';
      button.dispatchEvent(new MouseEvent('click'));
    });

    const allEvents = [];
    window.allEvents = allEvents;
    button.addEventListener('click', e => {
      if (!e.isTrusted)
        allEvents.push(e.type);
    });
  }");

        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));
        var allEvents = await Page.EvaluateAsync<string[]>("() => window.allEvents");
        // We should get one programmatic click on each attempt.
        Assert.That(allEvents, Is.EqualTo(new[]
        {
            "click", "click",
        }));
    }

    [UpstreamTest("HT-006 should click the button again after document.write")]
    public async Task ShouldClickTheButtonAgainAfterDocumentWrite()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<string>("result"), Is.EqualTo("Clicked"));

        await Page.EvaluateAsync(@"() => {
    document.open();
    document.write('<button onclick=""window.result2 = true""></button>');
    document.close();
  }");
        await Page.ClickAsync("button");
        Assert.That(await Page.EvaluateAsync<bool>("result2"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-007 should work with mui select")]
    public async Task ShouldWorkWithMuiSelect()
    {
        await Page.GotoAsync(Server.Prefix + "/mui.html");
        await Page.EvaluateAsync(@"() => {
    renderComponent(e(MaterialUI.FormControl, { fullWidth: true }, [
      e(MaterialUI.InputLabel, { id: 'demo-simple-select-label' }, ['Age']),
      e(MaterialUI.Select, {
        labelId: 'demo-simple-select-label',
        id: 'demo-simple-select',
        value: 10,
        label: 'Age',
      }, [
        e(MaterialUI.MenuItem, { value: 10 }, ['Ten']),
        e(MaterialUI.MenuItem, { value: 20 }, ['Twenty']),
        e(MaterialUI.MenuItem, { value: 30 }, ['Thirty']),
      ]),
    ]));
  }");
        await Page.ClickAsync("div.MuiFormControl-root:has-text(\"Age\")");
        await Expect(Page.Locator("text=Thirty")).ToBeVisibleAsync();
    }

    [UpstreamTest("HT-008 should work with drag and drop that moves the element under cursor")]
    public async Task ShouldWorkWithDragAndDropThatMovesTheElementUnderCursor()
    {
        await Page.GotoAsync(Server.Prefix + "/input/drag-n-drop-manual.html");
        await Page.DragAndDropAsync("#from", "#to");
        await Expect(Page.Locator("#to")).ToHaveTextAsync("Dropped");
    }

    [UpstreamTest("HT-009 should work with block inside inline")]
    public async Task ShouldWorkWithBlockInsideInline()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <div>
      <span>
        <div id=""target"" onclick=""window._clicked=true"">
          Romimine
        </div>
      </span>
    </div>
  ");
        await Page.Locator("#target").ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-010 should work with block-block-block inside inline-inline")]
    public async Task ShouldWorkWithBlockBlockBlockInsideInlineInline()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <div>
      <a href=""#ney"">
        <div>
          <span>
            <a href=""#yay"">
              <div>
                <h3 id=""target"">
                  Romimine
                </h3>
              </div>
            </a>
          </span>
        </div>
      </a>
    </div>
  ");
        await Page.Locator("#target").ClickAsync();
        await Expect(Page).ToHaveURLAsync(Server.EmptyPage + "#yay");
    }

    [UpstreamTest("HT-011 should work with block inside inline in shadow dom")]
    public async Task ShouldWorkWithBlockInsideInlineInShadowDom()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <div>
    </div>
    <script>
      const root = document.querySelector('div');
      const shadowRoot = root.attachShadow({ mode: 'open' });
      const span = document.createElement('span');
      shadowRoot.appendChild(span);
      const div = document.createElement('div');
      span.appendChild(div);
      div.id = 'target';
      div.addEventListener('click', () => window._clicked = true);
      div.textContent = 'Hello';
    </script>
  ");
        await Page.Locator("#target").ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-012 should not click iframe overlaying the target")]
    public async Task ShouldNotClickIframeOverlayingTheTarget()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <button style=""position: absolute; left: 250px;bottom: 0;height: 40px;width: 200px;"" onclick=""window._clicked=1"">
      click-me
    </button>
    <div style=""background: transparent; bottom: 0px; left: 0px; margin: 0px; padding: 0px; position: fixed; z-index: 2147483647;"">
      <iframe srcdoc=""<body onclick='window.top._clicked=2' style='background-color:red;height:40px;'></body>"" style=""display: block; border: 0px; width: 100vw; height: 48px;""></iframe>
    </div>
  ");
        var error = await Catch(() => Page.ClickAsync("text=click-me", new() { Timeout = 3000 }));
        Assert.That(await Page.EvaluateAsync<object>("window._clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("<iframe srcdoc=\"<body onclick='window.top._clicked=2' style='background-color:red;height:40px;'></body>\"></iframe> from <div>…</div> subtree intercepts pointer events"));
    }

    [UpstreamTest("HT-013 should not click an element overlaying iframe with the target")]
    public async Task ShouldNotClickAnElementOverlayingIframeWithTheTarget()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <div onclick='window.top._clicked=1'>padding</div>
    <iframe width=600 height=600 srcdoc=""<iframe srcdoc='<div onclick=&quot;window.top._clicked=2&quot;>padding</div><div onclick=&quot;window.top._clicked=3&quot;>inner</div>'></iframe><div onclick='window.top._clicked=4'>outer</div>""></iframe>
    <div onclick='window.top._clicked=5' style=""position: absolute; left: 0; right: 0; top: 0; bottom: 0; background: rgba(255, 0, 0, 0.1); padding: 200px;"">PINK OVERLAY</div>
  ");

        var target = Page.FrameLocator("iframe").FrameLocator("iframe").Locator("text=inner");
        var error = await Catch(() => target.ClickAsync(new() { Timeout = 3000 }));
        Assert.That(await Page.EvaluateAsync<object>("window._clicked"), Is.Null);
        Assert.That(error.Message, Does.Contain("<div onclick=\"window.top._clicked=5\">PINK OVERLAY</div> intercepts pointer events"));

        await Page.Locator("text=overlay").EvaluateAsync("e => e.style.display = 'none'");

        await target.ClickAsync();
        Assert.That(await Page.EvaluateAsync<int>("window._clicked"), Is.EqualTo(3));
    }

    [UpstreamTest("HT-014 should click into frame inside closed shadow root")]
    public async Task ShouldClickIntoFrameInsideClosedShadowRoot()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <div id=framecontainer>
    </div>
    <script>
      const iframe = document.createElement('iframe');
      iframe.setAttribute('name', 'myframe');
      iframe.setAttribute('srcdoc', '<div onclick=""window.top.__clicked = true"">click me</div>');
      const div = document.getElementById('framecontainer');
      const host = div.attachShadow({ mode: 'closed' });
      host.appendChild(iframe);
    </script>
  ");

        var frame = Page.Frames[1];
        await frame.Locator("text=click me").ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-015 should click an element inside closed shadow root")]
    public async Task ShouldClickAnElementInsideClosedShadowRoot()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <div id=container>
    </div>
    <script>
      const span = document.createElement('span');
      span.textContent = 'click me';
      span.addEventListener('click', () => window.__clicked = true);
      const div = document.getElementById('container');
      const host = div.attachShadow({ mode: 'closed' });
      host.appendChild(span);
      window.__target = span;
    </script>
  ");

        var handle = await Page.EvaluateHandleAsync("window.__target");
        await ((IElementHandle)handle).ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-016 should detect overlay from another shadow root")]
    public async Task ShouldDetectOverlayFromAnotherShadowRoot()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync(@"
    <style>
      div > div {
        position: absolute;
        top: 0;
        left: 0;
        width: 10px;
        height: 10px;
      }
      span {
        display: block;
        position: absolute;
        left: 0;
        top: 0;
        width: 300px;
        height: 300px;
      }
    </style>
    <div style=""position:relative; width:300px; height:300px"">
      <div id=container1></div>
      <div id=container2></div>
    </div>
    <script>
      for (const id of ['container1', 'container2']) {
        const span = document.createElement('span');
        span.id = id + '-span';
        span.textContent = 'click me';
        span.style.display = 'block';
        span.style.position = 'absolute';
        span.style.left = '20px';
        span.style.top = '20px';
        span.style.width = '300px';
        span.style.height = '300px';
        span.addEventListener('click', () => window.__clicked = id);
        const div = document.getElementById(id);
        const host = div.attachShadow({ mode: 'open' });
        host.appendChild(span);
      }
    </script>
  ");

        var error = await Catch(() => Page.Locator("#container1 >> text=click me").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("<div id=\"container2\"></div> intercepts pointer events"));
    }

    [UpstreamTest("HT-017 should detect overlaid element in a transformed iframe")]
    public async Task ShouldDetectOverlaidElementInATransformedIframe()
    {
        await Page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; border: none; }
      iframe {
        border: 4px solid black;
        background: gray;
        margin-left: 33px;
        margin-top: 24px;
        width: 400px;
        height: 400px;
        transform: scale(1.2);
      }
    </style>
    <iframe srcdoc=""
      <style>
        body, html { margin: 0; padding: 0; }
        div { margin-left: 10px; margin-top: 20px; width: 2px; height: 2px; }
        section { position: absolute; top: 0; left: 0; bottom: 0; right: 0; }
      </style>
      <div>Target</div>
      <section>Overlay</section>
      <script>
        document.querySelector('div').addEventListener('click', () => window.top._clicked = true);
      </script>
    ""></iframe>
  ");
        var locator = Page.FrameLocator("iframe").Locator("div");
        var error = await Catch(() => locator.ClickAsync(new() { Timeout = 5000 }));
        Assert.That(error.Message, Does.Contain("<section>Overlay</section> intercepts pointer events"));
    }

    [UpstreamTest("HT-018 should click in iframe with padding")]
    public async Task ShouldClickInIframeWithPadding()
    {
        await Page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; border: none; box-sizing: border-box; }
      iframe { background: gray; width: 200px; height: 200px; padding-top: 100px; }
    </style>
    <iframe srcdoc=""
      <style>
        body, html { margin: 0; padding: 0; }
        div { height: 100px; }
      </style>
      <div>Non-target</div>
      <div id=target>Target</div>
      <div>Non-target</div>
      <script>
        document.querySelector('#target').addEventListener('click', () => window.top._clicked = true);
      </script>
    ""></iframe>
  ");
        var locator = Page.FrameLocator("iframe").Locator("#target");
        await locator.ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-019 should click in iframe with padding 2")]
    public async Task ShouldClickInIframeWithPadding2()
    {
        await Page.SetContentAsync(@"
    <style>
      body, html, iframe { margin: 0; padding: 0; border: none; box-sizing: content-box; }
      iframe { background: gray; width: 200px; height: 200px; padding-top: 100px; }
    </style>
    <iframe srcdoc=""
      <style>
        body, html { margin: 0; padding: 0; }
        div { height: 100px; }
      </style>
      <div>Non-target</div>
      <div id=target>Target</div>
      <div>Non-target</div>
      <script>
        document.querySelector('#target').addEventListener('click', () => window.top._clicked = true);
      </script>
    ""></iframe>
  ");
        var locator = Page.FrameLocator("iframe").Locator("#target");
        await locator.ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-020 should click in custom element")]
    public async Task ShouldClickInCustomElement()
    {
        await Page.SetContentAsync(@"
    <html>
      <body>
        <my-input></my-input>
        <script>
          class MyInput extends HTMLElement {
            connectedCallback() {
              this.attachShadow({mode:'open'});
              this.shadowRoot.innerHTML = '<div><span><input type=""text"" /></span></div>';
              this.shadowRoot.querySelector('input').addEventListener('click', () => window.__clicked = true);
            }
          }
          customElements.define('my-input', MyInput);
        </script>
      </body>
    </html>
  ");
        await Page.Locator("input").ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-021 should click in nested custom elements")]
    public async Task ShouldClickInNestedCustomElements()
    {
        await Page.SetContentAsync(@"
    <outer-host></outer-host>
    <script>
      customElements.define('inner-host', class extends HTMLElement {
        connectedCallback() {
          this.attachShadow({ mode: 'open' }).innerHTML = '<inner-most></inner-most>';
        }
      });
      customElements.define('inner-most', class extends HTMLElement {
        connectedCallback() {
          this.attachShadow({ mode: 'open' }).innerHTML = '<input>';
          this.shadowRoot.querySelector('input').addEventListener('click', () => window.__clicked = true);
        }
      });
      customElements.define('outer-host', class extends HTMLElement {
        connectedCallback() {
          this.attachShadow({ mode: 'open' }).innerHTML = '<inner-host></inner-host>';
        }
      });
    </script>
  ");
        await Page.Locator("input").ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("HT-022 should click an element inside nested closed shadow roots")]
    public async Task ShouldClickAnElementInsideNestedClosedShadowRoots()
    {
        await Page.SetContentAsync(@"
    <div id=container></div>
    <script>
      const input = document.createElement('input');
      input.addEventListener('click', () => window.__clicked = true);
      const outer = document.getElementById('container').attachShadow({ mode: 'closed' });
      const inner = document.createElement('div');
      outer.appendChild(inner);
      inner.attachShadow({ mode: 'closed' }).appendChild(input);
      window.__target = input;
    </script>
  ");
        var handle = await Page.EvaluateHandleAsync("window.__target");
        await ((IElementHandle)handle).ClickAsync();
        Assert.That(await Page.EvaluateAsync<bool>("window.__clicked"), Is.EqualTo(true));
    }
}
