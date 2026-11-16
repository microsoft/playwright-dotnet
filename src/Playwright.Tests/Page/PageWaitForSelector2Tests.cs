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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/page-wait-for-selector-2.spec.ts.</summary>
public class PageWaitForSelector2Tests
{
    private const string addElement = "tag => document.body.appendChild(document.createElement(tag))";

    [UpstreamTest("WA-044 should survive cross-process navigation")]
    public async Task ShouldSurviveCrossProcessNavigation(IPage page, Server server)
    {
        var boxFound = false;
        // .NET: waitForSelector(...).then(() => boxFound = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await page.WaitForSelectorAsync(".box");
            return boxFound = true;
        }
        var waitForSelector = WaitForSelector();
        await page.GotoAsync(server.EmptyPage);
        Assert.That(boxFound, Is.EqualTo(false));
        await page.ReloadAsync();
        Assert.That(boxFound, Is.EqualTo(false));
        await page.GotoAsync(server.CrossProcessPrefix + "/grid.html");
        await waitForSelector;
        Assert.That(boxFound, Is.EqualTo(true));
    }

    [UpstreamTest("WA-045 should wait for visible")]
    public async Task ShouldWaitForVisible(IPage page, Server server)
    {
        var divFound = false;
        // .NET: waitForSelector(...).then(() => divFound = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await page.WaitForSelectorAsync("div");
            return divFound = true;
        }
        var waitForSelector = WaitForSelector();
        await page.SetContentAsync("<div style='display: none; visibility: hidden;'>1</div>");
        Assert.That(divFound, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('display')");
        Assert.That(divFound, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('visibility')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divFound, Is.EqualTo(true));
    }

    [UpstreamTest("WA-046 should not consider visible when zero-sized")]

    [Fixme("dotnet: the message is 'Timeout 1000ms exceeded.', without the 'page.waitForSelector: ' prefix of the call")]
    public async Task ShouldNotConsiderVisibleWhenZeroSized(IPage page, Server server)
    {
        await page.SetContentAsync("<div style='width: 0; height: 0;'>1</div>");
        var error = await Catch(() => page.WaitForSelectorAsync("div", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("page.waitForSelector: Timeout 1000ms exceeded"));
        await page.EvaluateAsync("() => document.querySelector('div').style.width = '10px'");
        error = await Catch(() => page.WaitForSelectorAsync("div", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("page.waitForSelector: Timeout 1000ms exceeded"));
        await page.EvaluateAsync("() => document.querySelector('div').style.height = '10px'");
        Assert.That(await page.WaitForSelectorAsync("div", new() { Timeout = 1000 }), Is.Not.Null);
    }

    [UpstreamTest("WA-047 should wait for visible recursively")]
    public async Task ShouldWaitForVisibleRecursively(IPage page, Server server)
    {
        var divVisible = false;
        // .NET: waitForSelector(...).then(() => divVisible = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await page.WaitForSelectorAsync("div#inner");
            return divVisible = true;
        }
        var waitForSelector = WaitForSelector();
        await page.SetContentAsync("<div style='display: none; visibility: hidden;'><div id=\"inner\">hi</div></div>");
        Assert.That(divVisible, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('display')");
        Assert.That(divVisible, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('visibility')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divVisible, Is.EqualTo(true));
    }

    [UpstreamTest("WA-048 should consider outside of viewport visible")]
    public async Task ShouldConsiderOutsideOfViewportVisible(IPage page)
    {
        await page.SetContentAsync(@"
    <style>
      .cover {
        position: fixed;
        left: 0;
        top: 0;
        width: 100px;
        height: 100px;
        background-color: red;
        transform: translateX(-200px);
      }
    </style>
    <div class=""cover"">cover</div>
  ");

        var cover = page.Locator(".cover");
        await cover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Expect(cover).ToBeVisibleAsync();
    }

    [UpstreamTest("WA-049 hidden should wait for hidden")]
    public async Task HiddenShouldWaitForHidden(IPage page, Server server)
    {
        var divHidden = false;
        await page.SetContentAsync("<div style='display: block;'>content</div>");
        // .NET: waitForSelector(...).then(() => divHidden = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
            return divHidden = true;
        }
        var waitForSelector = WaitForSelector();
        await page.WaitForSelectorAsync("div"); // do a round trip
        Assert.That(divHidden, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').style.setProperty('visibility', 'hidden')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divHidden, Is.EqualTo(true));
    }

    [UpstreamTest("WA-050 hidden should wait for display: none")]
    public async Task HiddenShouldWaitForDisplayNone(IPage page, Server server)
    {
        var divHidden = false;
        await page.SetContentAsync("<div style='display: block;'>content</div>");
        // .NET: waitForSelector(...).then(() => divHidden = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
            return divHidden = true;
        }
        var waitForSelector = WaitForSelector();
        await page.WaitForSelectorAsync("div"); // do a round trip
        Assert.That(divHidden, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').style.setProperty('display', 'none')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divHidden, Is.EqualTo(true));
    }

    [UpstreamTest("WA-051 hidden should wait for removal")]
    public async Task HiddenShouldWaitForRemoval(IPage page, Server server)
    {
        await page.SetContentAsync("<div>content</div>");
        var divRemoved = false;
        // .NET: waitForSelector(...).then(() => divRemoved = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
            return divRemoved = true;
        }
        var waitForSelector = WaitForSelector();
        await page.WaitForSelectorAsync("div"); // do a round trip
        Assert.That(divRemoved, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').remove()");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divRemoved, Is.EqualTo(true));
    }

    [UpstreamTest("WA-052 should return null if waiting to hide non-existing element")]
    public async Task ShouldReturnNullIfWaitingToHideNonExistingElement(IPage page, Server server)
    {
        var handle = await page.WaitForSelectorAsync("non-existing", new() { State = WaitForSelectorState.Hidden });
        Assert.That(handle, Is.Null);
    }

    [UpstreamTest("WA-053 should respect timeout")]

    [Fixme("dotnet: the message is 'Timeout 3000ms exceeded.', without the 'page.waitForSelector: ' prefix of the call, and the call log has Locator(\"div\"), upstream expects locator('div')")]
    public async Task ShouldRespectTimeout(IPage page, IPlaywright playwright)
    {
        Exception error = null;
        error = await Catch(() => page.WaitForSelectorAsync("div", new() { Timeout = 3000, State = WaitForSelectorState.Attached }));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("page.waitForSelector: Timeout 3000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for locator('div')"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("WA-054 should have an error message specifically for awaiting an element to be hidden")]

    [Fixme("dotnet: the message is 'Timeout 1000ms exceeded.', without the 'page.waitForSelector: ' prefix of the call, and the call log has Locator(\"div\"), upstream expects locator('div')")]
    public async Task ShouldHaveAnErrorMessageSpecificallyForAwaitingAnElementToBeHidden(IPage page, Server server)
    {
        await page.SetContentAsync("<div>content</div>");
        Exception error = null;
        error = await Catch(() => page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden, Timeout = 1000 }));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("page.waitForSelector: Timeout 1000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for locator('div') to be hidden"));
    }

    [UpstreamTest("WA-055 should respond to node attribute mutation")]
    public async Task ShouldRespondToNodeAttributeMutation(IPage page, Server server)
    {
        var divFound = false;
        // .NET: waitForSelector(...).then(() => divFound = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await page.WaitForSelectorAsync(".zombo", new() { State = WaitForSelectorState.Attached });
            return divFound = true;
        }
        var waitForSelector = WaitForSelector();
        await page.SetContentAsync("<div class='notZombo'></div>");
        Assert.That(divFound, Is.EqualTo(false));
        await page.EvaluateAsync("() => document.querySelector('div').className = 'zombo'");
        Assert.That(await waitForSelector, Is.EqualTo(true));
    }

    [UpstreamTest("WA-056 should return the element handle")]
    public async Task ShouldReturnTheElementHandle(IPage page, Server server)
    {
        var waitForSelector = page.WaitForSelectorAsync(".zombo");
        await page.SetContentAsync("<div class='zombo'>anything</div>");
        Assert.That(await page.EvaluateAsync<string>("x => x.textContent", await waitForSelector), Is.EqualTo("anything"));
    }

    [UpstreamTest("WA-062 should support >> selector syntax")]
    public async Task ShouldSupportSelectorSyntax(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);
        var frame = page.MainFrame;
        var watchdog = frame.WaitForSelectorAsync("css=div >> css=span", new() { State = WaitForSelectorState.Attached });
        await frame.EvaluateAsync(addElement, "br");
        await frame.EvaluateAsync(addElement, "div");
        await frame.EvaluateAsync("() => document.querySelector('div').appendChild(document.createElement('span'))");
        var eHandle = await watchdog;
        var tagName = await (await eHandle.GetPropertyAsync("tagName")).JsonValueAsync<string>();
        Assert.That(tagName, Is.EqualTo("SPAN"));
    }

    [UpstreamTest("WA-063 should wait for detached if already detached")]
    public async Task ShouldWaitForDetachedIfAlreadyDetached(IPage page, Server server)
    {
        await page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        Assert.That(await page.WaitForSelectorAsync("css=div", new() { State = WaitForSelectorState.Detached }), Is.Null);
    }

    [UpstreamTest("WA-064 should wait for detached")]
    public async Task ShouldWaitForDetached(IPage page, Server server)
    {
        await page.SetContentAsync("<section id=\"testAttribute\"><div>43543</div></section>");
        var done = false;
        // .NET: waitForSelector(...).then(() => done = true) is a local function that is called in its place.
        async Task<bool> WaitFor()
        {
            await page.WaitForSelectorAsync("css=div", new() { State = WaitForSelectorState.Detached });
            return done = true;
        }
        var waitFor = WaitFor();
        Assert.That(done, Is.EqualTo(false));
        await page.WaitForSelectorAsync("css=section");
        Assert.That(done, Is.EqualTo(false));
        await page.EvalOnSelectorAsync("div", "div => div.remove()");
        Assert.That(await waitFor, Is.EqualTo(true));
        Assert.That(done, Is.EqualTo(true));
    }

    [UpstreamTest("WA-065 should support some fancy xpath")]
    public async Task ShouldSupportSomeFancyXpath(IPage page, Server server)
    {
        await page.SetContentAsync("<p>red herring</p><p>hello  world  </p>");
        var waitForXPath = page.WaitForSelectorAsync("//p[normalize-space(.)=\"hello world\"]");
        Assert.That(await page.EvaluateAsync<string>("x => x.textContent", await waitForXPath), Is.EqualTo("hello  world  "));
    }

    [UpstreamTest("WA-066 should respect timeout xpath")]

    [Fixme("dotnet: the message is 'Timeout 3000ms exceeded.', without the 'page.waitForSelector: ' prefix of the call, and the call log has Locator(\"//div\"), upstream expects locator('//div')")]
    public async Task ShouldRespectTimeoutXpath(IPage page, IPlaywright playwright)
    {
        Exception error = null;
        error = await Catch(() => page.WaitForSelectorAsync("//div", new() { State = WaitForSelectorState.Attached, Timeout = 3000 }));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("page.waitForSelector: Timeout 3000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for locator('//div')"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("WA-067 should run in specified frame xpath")]
    public async Task ShouldRunInSpecifiedFrameXpath(IPage page, Server server)
    {
        await AttachFrame(page, "frame1", server.EmptyPage);
        await AttachFrame(page, "frame2", server.EmptyPage);
        var frame1 = page.Frames[1];
        var frame2 = page.Frames[2];
        var waitForXPathPromise = frame2.WaitForSelectorAsync("//div", new() { State = WaitForSelectorState.Attached });
        await frame1.EvaluateAsync(addElement, "div");
        await frame2.EvaluateAsync(addElement, "div");
        var eHandle = await waitForXPathPromise;
        Assert.That(await eHandle.OwnerFrameAsync(), Is.SameAs(frame2));
    }

    [UpstreamTest("WA-068 should throw when frame is detached xpath")]

    [Fixme("dotnet: the message is 'Frame was detached', without the 'frame.waitForSelector: ' prefix of the call")]
    public async Task ShouldThrowWhenFrameIsDetachedXpath(IPage page, Server server)
    {
        await AttachFrame(page, "frame1", server.EmptyPage);
        var frame = page.Frames[1];
        Exception waitError = null;
        // .NET: waitForSelector(...).catch(e => waitError = e) is a local function that is called in its place.
        async Task WaitForSelector()
        {
            waitError = await Catch(() => frame.WaitForSelectorAsync("//*[@class=\"box\"]"));
        }
        var waitPromise = WaitForSelector();
        await DetachFrame(page, "frame1");
        await waitPromise;
        Assert.That(waitError, Is.Not.Null);
        Assert.That(waitError.Message, Does.Contain("frame.waitForSelector: Frame was detached"));
    }

    [UpstreamTest("WA-069 should return the element handle xpath")]
    public async Task ShouldReturnTheElementHandleXpath(IPage page, Server server)
    {
        var waitForXPath = page.WaitForSelectorAsync("//*[@class=\"zombo\"]");
        await page.SetContentAsync("<div class='zombo'>anything</div>");
        Assert.That(await page.EvaluateAsync<string>("x => x.textContent", await waitForXPath), Is.EqualTo("anything"));
    }

    [UpstreamTest("WA-070 should allow you to select an element with single slash xpath")]
    public async Task ShouldAllowYouToSelectAnElementWithSingleSlashXpath(IPage page, Server server)
    {
        await page.SetContentAsync("<div>some text</div>");
        var waitForXPath = page.WaitForSelectorAsync("//html/body/div");
        Assert.That(await page.EvaluateAsync<string>("x => x.textContent", await waitForXPath), Is.EqualTo("some text"));
    }

    [UpstreamTest("WA-071 should correctly handle hidden shadow host")]
    public async Task ShouldCorrectlyHandleHiddenShadowHost(IPage page, Server server)
    {
        await page.SetContentAsync(@"
    <x-host hidden></x-host>
    <script>
      const host = document.querySelector('x-host');
      const root = host.attachShadow({ mode: 'open' });
      const style = document.createElement('style');
      style.textContent = ':host([hidden]) { display: none; }';
      root.appendChild(style);
      const child = document.createElement('div');
      child.textContent = 'Find me';
      root.appendChild(child);
    </script>
  ");
        Assert.That(await page.TextContentAsync("div"), Is.EqualTo("Find me"));
        await page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
    }

    [UpstreamTest("WA-074 should fail if element handle was detached while waiting")]
    public async Task ShouldFailIfElementHandleWasDetachedWhileWaiting(IPage page, Server server)
    {
        await page.SetContentAsync("<button>hello</button>");
        var button = await page.QuerySelectorAsync("button");
        var promise = Catch(() => button.WaitForSelectorAsync("something"));
        await page.WaitForTimeoutAsync(100);
        await page.EvaluateAsync("() => document.body.innerText = ''");
        var error = await promise;
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("WA-075 should succeed if element handle was detached while waiting for hidden")]
    public async Task ShouldSucceedIfElementHandleWasDetachedWhileWaitingForHidden(IPage page, Server server)
    {
        await page.SetContentAsync("<button>hello</button>");
        var button = await page.QuerySelectorAsync("button");
        var promise = button.WaitForSelectorAsync("something", new() { State = WaitForSelectorState.Hidden });
        await page.WaitForTimeoutAsync(1000);
        await page.EvaluateAsync("() => document.body.innerText = ''");
        await promise;
    }

    [UpstreamTest("WA-076 should succeed if element handle was detached while waiting for detached")]
    public async Task ShouldSucceedIfElementHandleWasDetachedWhileWaitingForDetached(IPage page, Server server)
    {
        await page.SetContentAsync("<button>hello</button>");
        var button = await page.QuerySelectorAsync("button");
        var promise = button.WaitForSelectorAsync("something", new() { State = WaitForSelectorState.Detached });
        await page.WaitForTimeoutAsync(1000);
        await page.EvaluateAsync("() => document.body.innerText = ''");
        await promise;
    }
}
