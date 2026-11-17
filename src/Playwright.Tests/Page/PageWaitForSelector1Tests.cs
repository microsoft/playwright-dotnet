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

using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-wait-for-selector-1.spec.ts.</summary>
public class PageWaitForSelector1Tests
{
    private static async Task GiveItTimeToLog(IFrame frame)
    {
        await Rafraf(frame, 2);
    }

    private const string addElement = "tag => document.body.appendChild(document.createElement(tag))";

    [UpstreamTest("WA-029 should immediately resolve promise if node exists")]
    public async Task ShouldImmediatelyResolvePromiseIfNodeExists(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame = page.MainFrame;
        await frame.WaitForSelectorAsync("*");
        await frame.EvaluateAsync(addElement, "div");
        await frame.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
    }

    [UpstreamTest("WA-030 elementHandle.waitForSelector should immediately resolve if node exists")]
    public async Task ElementHandleWaitForSelectorShouldImmediatelyResolveIfNodeExists(IPage page)
    {
        await page.SetContentAsync("<span>extra</span><div><span>target</span></div>");
        var div = await page.QuerySelectorAsync("div");
        var span = await div.WaitForSelectorAsync("span", new() { State = WaitForSelectorState.Attached });
        Assert.That(await span.EvaluateAsync<string>("e => e.textContent"), Is.EqualTo("target"));
    }

    [UpstreamTest("WA-031 elementHandle.waitForSelector should wait")]
    public async Task ElementHandleWaitForSelectorShouldWait(IPage page)
    {
        await page.SetContentAsync("<div></div>");
        var div = await page.QuerySelectorAsync("div");
        var promise = div.WaitForSelectorAsync("span", new() { State = WaitForSelectorState.Attached });
        await div.EvaluateAsync("div => div.innerHTML = '<span>target</span>'");
        var span = await promise;
        Assert.That(await span.EvaluateAsync<string>("e => e.textContent"), Is.EqualTo("target"));
    }

    [UpstreamTest("WA-032 elementHandle.waitForSelector should timeout")]
    public async Task ElementHandleWaitForSelectorShouldTimeout(IPage page)
    {
        await page.SetContentAsync("<div></div>");
        var div = await page.QuerySelectorAsync("div");
        var error = await Catch(() => div.WaitForSelectorAsync("span", new() { Timeout = 100 }));
        Assert.That(error.Message, Does.Contain("Timeout 100ms exceeded."));
    }

    [UpstreamTest("WA-033 elementHandle.waitForSelector should throw on navigation")]

    [LocalFixme("dotnet: the call log has Locator(\"span\"), upstream expects locator('span')")]
    public async Task ElementHandleWaitForSelectorShouldThrowOnNavigation(IPage page, Server server)
    {
        await page.SetContentAsync("<div></div>");
        var div = await page.QuerySelectorAsync("div");
        var promise = Catch(() => div.WaitForSelectorAsync("span"));
        // Give it some time before navigating.
        for (var i = 0; i < 10; i++)
        {
            await page.EvaluateAsync("() => 1");
        }
        await page.GotoAsync(server.EmptyPage);
        var error = await promise;
        Assert.That(error.Message, Does.Contain("waiting for locator('span') to be visible"));
    }

    [UpstreamTest("WA-034 should work with removed MutationObserver")]
    public async Task ShouldWorkWithRemovedMutationObserver(IPage page)
    {
        await page.EvaluateAsync("() => delete window.MutationObserver");
        var handle = await PromiseAll(
            page.WaitForSelectorAsync(".zombo"),
            page.SetContentAsync("<div class='zombo'>anything</div>"));
        Assert.That(await page.EvaluateAsync<string>("x => x.textContent", handle), Is.EqualTo("anything"));
    }

    [UpstreamTest("WA-035 should resolve promise when node is added")]
    public async Task ShouldResolvePromiseWhenNodeIsAdded(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame = page.MainFrame;
        var watchdog = frame.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
        await frame.EvaluateAsync(addElement, "br");
        await frame.EvaluateAsync(addElement, "div");
        var eHandle = await watchdog;
        var tagName = await (await eHandle.GetPropertyAsync("tagName")).JsonValueAsync<string>();
        Assert.That(tagName, Is.EqualTo("DIV"));
    }

    [UpstreamTest("WA-036 should report logs while waiting for visible")]

    [LocalFixme("dotnet: the message is 'Timeout 5000ms exceeded.', without the 'frame.waitForSelector: ' prefix of the call, and the call log has Locator(\"div\"), upstream expects locator('div')")]
    public async Task ShouldReportLogsWhileWaitingForVisible(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame = page.MainFrame;
        var watchdog = frame.WaitForSelectorAsync("div", new() { Timeout = 5000 });

        await frame.EvaluateAsync(@"() => {
    const div = document.createElement('div');
    div.className = 'foo bar';
    div.id = 'mydiv';
    div.setAttribute('style', 'display: none');
    div.setAttribute('foo', '123456789012345678901234567890123456789012345678901234567890');
    div.textContent = 'abcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvwyxz';
    document.body.appendChild(div);
  }");
        await GiveItTimeToLog(frame);

        await frame.EvaluateAsync("() => document.querySelector('div').remove()");
        await GiveItTimeToLog(frame);

        await frame.EvaluateAsync(@"() => {
    const div = document.createElement('div');
    div.className = 'another';
    div.style.display = 'none';
    document.body.appendChild(div);
  }");
        await GiveItTimeToLog(frame);

        var error = await Catch(() => watchdog);
        Assert.That(error.Message, Does.Contain("frame.waitForSelector: Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain("waiting for locator('div') to be visible"));
        Assert.That(error.Message, Does.Contain("locator resolved to hidden <div id=\"mydiv\" class=\"foo bar\" foo=\"123456789012345678901234567890123456789012345678901234567890\">abcdefghijklmnopqrstuvwyxzabcdefghijklmnopqrstuvw…</div>"));
        Assert.That(error.Message, Does.Contain("locator resolved to hidden <div class=\"another\"></div>"));
    }

    [UpstreamTest("WA-037 should report logs while waiting for hidden")]

    [LocalFixme("dotnet: the message is 'Timeout 5000ms exceeded.', without the 'frame.waitForSelector: ' prefix of the call, and the call log has Locator(\"div\"), upstream expects locator('div')")]
    public async Task ShouldReportLogsWhileWaitingForHidden(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame = page.MainFrame;
        await frame.EvaluateAsync(@"() => {
    const div = document.createElement('div');
    div.className = 'foo bar';
    div.id = 'mydiv';
    div.textContent = 'hello';
    document.body.appendChild(div);
  }");

        var watchdog = frame.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden, Timeout = 5000 });
        await GiveItTimeToLog(frame);

        await frame.EvaluateAsync(@"() => {
    document.querySelector('div').remove();
    const div = document.createElement('div');
    div.className = 'another';
    div.textContent = 'hello';
    document.body.appendChild(div);
  }");
        await GiveItTimeToLog(frame);

        var error = await Catch(() => watchdog);
        Assert.That(error.Message, Does.Contain("frame.waitForSelector: Timeout 5000ms exceeded."));
        Assert.That(error.Message, Does.Contain("waiting for locator('div') to be hidden"));
        Assert.That(error.Message, Does.Contain("locator resolved to visible <div id=\"mydiv\" class=\"foo bar\">hello</div>"));
        Assert.That(error.Message, Does.Contain("locator resolved to visible <div class=\"another\">hello</div>"));
    }

    [UpstreamTest("WA-038 should report logs when the selector resolves to multiple elements")]
    public async Task ShouldReportLogsWhenTheSelectorResolvesToMultipleElements(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await page.SetContentAsync(@"
    <button style=""display: none; position: absolute; top: 0px; left: 0px; width: 100%;"">Reset</button>
    <button>Reset</button>
  ");
        var error = await Catch(() => page.ClickAsync("text=Reset", new()
        {
            Timeout = 1000,
        }));
        Assert.That(error.ToString(), Does.Contain("locator resolved to 2 elements. Proceeding with the first one: <button>Reset</button>"));
    }

    [UpstreamTest("WA-039 should resolve promise when node is added in shadow dom")]
    public async Task ShouldResolvePromiseWhenNodeIsAddedInShadowDom(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var watchdog = page.WaitForSelectorAsync("span");
        await page.EvaluateAsync(@"() => {
    const div = document.createElement('div');
    div.attachShadow({ mode: 'open' });
    document.body.appendChild(div);
  }");
        await page.WaitForTimeoutAsync(100);
        await page.EvaluateAsync(@"() => {
    const span = document.createElement('span');
    span.textContent = 'Hello from shadow';
    document.querySelector('div').shadowRoot.appendChild(span);
  }");
        var handle = await watchdog;
        Assert.That(await handle.EvaluateAsync<string>("e => e.textContent"), Is.EqualTo("Hello from shadow"));
    }

    [UpstreamTest("WA-040 should work when node is added through innerHTML")]
    public async Task ShouldWorkWhenNodeIsAddedThroughInnerHTML(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var watchdog = page.WaitForSelectorAsync("h3 div", new() { State = WaitForSelectorState.Attached });
        await page.EvaluateAsync(addElement, "span");
        await page.EvaluateAsync("() => document.querySelector('span').innerHTML = '<h3><div></div></h3>'");
        await watchdog;
    }

    [UpstreamTest("WA-041 page.waitForSelector is shortcut for main frame")]
    public async Task PageWaitForSelectorIsShortcutForMainFrame(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        await AttachFrame(page, "frame1", server.EmptyPage);
        var otherFrame = page.Frames[1];
        var watchdog = page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
        await otherFrame.EvaluateAsync(addElement, "div");
        await page.EvaluateAsync(addElement, "div");
        var eHandle = await watchdog;
        Assert.That(await eHandle.OwnerFrameAsync(), Is.SameAs(page.MainFrame));
    }

    [UpstreamTest("WA-042 should run in specified frame")]
    public async Task ShouldRunInSpecifiedFrame(IPage page, Server server)
    {
        await AttachFrame(page, "frame1", server.EmptyPage);
        await AttachFrame(page, "frame2", server.EmptyPage);
        var frame1 = page.Frames[1];
        var frame2 = page.Frames[2];
        var waitForSelectorPromise = frame2.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached });
        await frame1.EvaluateAsync(addElement, "div");
        await frame2.EvaluateAsync(addElement, "div");
        var eHandle = await waitForSelectorPromise;
        Assert.That(await eHandle.OwnerFrameAsync(), Is.SameAs(frame2));
    }

    [UpstreamTest("WA-043 should throw when frame is detached")]

    [LocalFixme("dotnet: the message is 'Frame was detached', without the 'frame.waitForSelector: ' prefix of the call")]
    public async Task ShouldThrowWhenFrameIsDetached(IPage page, Server server)
    {
        await AttachFrame(page, "frame1", server.EmptyPage);
        var frame = page.Frames[1];
        Exception waitError = null;
        // .NET: waitForSelector(...).catch(e => waitError = e) is a local function that is called in its place.
        async Task WaitForSelector()
        {
            waitError = await Catch(() => frame.WaitForSelectorAsync(".box"));
        }
        var waitPromise = WaitForSelector();
        await DetachFrame(page, "frame1");
        await waitPromise;
        Assert.That(waitError, Is.Not.Null);
        Assert.That(waitError.Message, Does.Contain("frame.waitForSelector: Frame was detached"));
    }
}
