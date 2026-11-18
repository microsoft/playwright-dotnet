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
public class PageWaitForSelector2Tests : UpstreamPageTest
{
    private const string addElement = "tag => document.body.appendChild(document.createElement(tag))";

    [UpstreamTest("WA-044 should survive cross-process navigation")]
    public async Task ShouldSurviveCrossProcessNavigation()
    {
        var boxFound = false;
        // .NET: waitForSelector(...).then(() => boxFound = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await Page.WaitForSelectorAsync(".box");
            return boxFound = true;
        }
        var waitForSelector = WaitForSelector();
        await Page.GotoAsync(Server.EmptyPage);
        Assert.That(boxFound, Is.EqualTo(false));
        await Page.ReloadAsync();
        Assert.That(boxFound, Is.EqualTo(false));
        await Page.GotoAsync(Server.CrossProcessPrefix + "/grid.html");
        await waitForSelector;
        Assert.That(boxFound, Is.EqualTo(true));
    }

    [UpstreamTest("WA-045 should wait for visible")]
    public async Task ShouldWaitForVisible()
    {
        var divFound = false;
        // .NET: waitForSelector(...).then(() => divFound = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await Page.WaitForSelectorAsync("div");
            return divFound = true;
        }
        var waitForSelector = WaitForSelector();
        await Page.SetContentAsync("<div style='display: none; visibility: hidden;'>1</div>");
        Assert.That(divFound, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('display')");
        Assert.That(divFound, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('visibility')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divFound, Is.EqualTo(true));
    }

    [UpstreamTest("WA-046 should not consider visible when zero-sized")]

    public async Task ShouldNotConsiderVisibleWhenZeroSized()
    {
        await Page.SetContentAsync("<div style='width: 0; height: 0;'>1</div>");
        var error = await Catch(() => Page.WaitForSelectorAsync("div", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        await Page.EvaluateAsync("() => document.querySelector('div').style.width = '10px'");
        error = await Catch(() => Page.WaitForSelectorAsync("div", new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        await Page.EvaluateAsync("() => document.querySelector('div').style.height = '10px'");
        Assert.That(await Page.WaitForSelectorAsync("div", new() { Timeout = 1000 }), Is.Not.Null);
    }

    [UpstreamTest("WA-047 should wait for visible recursively")]
    public async Task ShouldWaitForVisibleRecursively()
    {
        var divVisible = false;
        // .NET: waitForSelector(...).then(() => divVisible = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await Page.WaitForSelectorAsync("div#inner");
            return divVisible = true;
        }
        var waitForSelector = WaitForSelector();
        await Page.SetContentAsync("<div style='display: none; visibility: hidden;'><div id=\"inner\">hi</div></div>");
        Assert.That(divVisible, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('display')");
        Assert.That(divVisible, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').style.removeProperty('visibility')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divVisible, Is.EqualTo(true));
    }

    [UpstreamTest("WA-048 should consider outside of viewport visible")]
    public async Task ShouldConsiderOutsideOfViewportVisible()
    {
        await Page.SetContentAsync(@"
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

        var cover = Page.Locator(".cover");
        await cover.WaitForAsync(new() { State = WaitForSelectorState.Visible });
        await Expect(cover).ToBeVisibleAsync();
    }

    [UpstreamTest("WA-049 hidden should wait for hidden")]
    public async Task HiddenShouldWaitForHidden()
    {
        var divHidden = false;
        await Page.SetContentAsync("<div style='display: block;'>content</div>");
        // .NET: waitForSelector(...).then(() => divHidden = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
            return divHidden = true;
        }
        var waitForSelector = WaitForSelector();
        await Page.WaitForSelectorAsync("div"); // do a round trip
        Assert.That(divHidden, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').style.setProperty('visibility', 'hidden')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divHidden, Is.EqualTo(true));
    }

    [UpstreamTest("WA-050 hidden should wait for display: none")]
    public async Task HiddenShouldWaitForDisplayNone()
    {
        var divHidden = false;
        await Page.SetContentAsync("<div style='display: block;'>content</div>");
        // .NET: waitForSelector(...).then(() => divHidden = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
            return divHidden = true;
        }
        var waitForSelector = WaitForSelector();
        await Page.WaitForSelectorAsync("div"); // do a round trip
        Assert.That(divHidden, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').style.setProperty('display', 'none')");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divHidden, Is.EqualTo(true));
    }

    [UpstreamTest("WA-051 hidden should wait for removal")]
    public async Task HiddenShouldWaitForRemoval()
    {
        await Page.SetContentAsync("<div>content</div>");
        var divRemoved = false;
        // .NET: waitForSelector(...).then(() => divRemoved = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
            return divRemoved = true;
        }
        var waitForSelector = WaitForSelector();
        await Page.WaitForSelectorAsync("div"); // do a round trip
        Assert.That(divRemoved, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').remove()");
        Assert.That(await waitForSelector, Is.EqualTo(true));
        Assert.That(divRemoved, Is.EqualTo(true));
    }

    [UpstreamTest("WA-052 should return null if waiting to hide non-existing element")]
    public async Task ShouldReturnNullIfWaitingToHideNonExistingElement()
    {
        var handle = await Page.WaitForSelectorAsync("non-existing", new() { State = WaitForSelectorState.Hidden });
        Assert.That(handle, Is.Null);
    }

    [UpstreamTest("WA-053 should respect timeout")]

    public async Task ShouldRespectTimeout()
    {
        Exception error = null;
        error = await Catch(() => Page.WaitForSelectorAsync("div", new() { Timeout = 3000, State = WaitForSelectorState.Attached }));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for Locator(\"div\")"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("WA-054 should have an error message specifically for awaiting an element to be hidden")]

    public async Task ShouldHaveAnErrorMessageSpecificallyForAwaitingAnElementToBeHidden()
    {
        await Page.SetContentAsync("<div>content</div>");
        Exception error = null;
        error = await Catch(() => Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden, Timeout = 1000 }));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for Locator(\"div\") to be hidden"));
    }

    [UpstreamTest("WA-055 should respond to node attribute mutation")]
    public async Task ShouldRespondToNodeAttributeMutation()
    {
        var divFound = false;
        // .NET: waitForSelector(...).then(() => divFound = true) is a local function that is called in its place.
        async Task<bool> WaitForSelector()
        {
            await Page.WaitForSelectorAsync(".zombo", new() { State = WaitForSelectorState.Attached });
            return divFound = true;
        }
        var waitForSelector = WaitForSelector();
        await Page.SetContentAsync("<div class='notZombo'></div>");
        Assert.That(divFound, Is.EqualTo(false));
        await Page.EvaluateAsync("() => document.querySelector('div').className = 'zombo'");
        Assert.That(await waitForSelector, Is.EqualTo(true));
    }

    [UpstreamTest("WA-056 should return the element handle")]
    public async Task ShouldReturnTheElementHandle()
    {
        var waitForSelector = Page.WaitForSelectorAsync(".zombo");
        await Page.SetContentAsync("<div class='zombo'>anything</div>");
        Assert.That(await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector), Is.EqualTo("anything"));
    }

    [UpstreamTest("WA-062 should support >> selector syntax")]
    public async Task ShouldSupportSelectorSyntax()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame = Page.MainFrame;
        var watchdog = frame.WaitForSelectorAsync("css=div >> css=span", new() { State = WaitForSelectorState.Attached });
        await frame.EvaluateAsync(addElement, "br");
        await frame.EvaluateAsync(addElement, "div");
        await frame.EvaluateAsync("() => document.querySelector('div').appendChild(document.createElement('span'))");
        var eHandle = await watchdog;
        var tagName = await (await eHandle.GetPropertyAsync("tagName")).JsonValueAsync<string>();
        Assert.That(tagName, Is.EqualTo("SPAN"));
    }

    [UpstreamTest("WA-063 should wait for detached if already detached")]
    public async Task ShouldWaitForDetachedIfAlreadyDetached()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        Assert.That(await Page.WaitForSelectorAsync("css=div", new() { State = WaitForSelectorState.Detached }), Is.Null);
    }

    [UpstreamTest("WA-064 should wait for detached")]
    public async Task ShouldWaitForDetached()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\"><div>43543</div></section>");
        var done = false;
        // .NET: waitForSelector(...).then(() => done = true) is a local function that is called in its place.
        async Task<bool> WaitFor()
        {
            await Page.WaitForSelectorAsync("css=div", new() { State = WaitForSelectorState.Detached });
            return done = true;
        }
        var waitFor = WaitFor();
        Assert.That(done, Is.EqualTo(false));
        await Page.WaitForSelectorAsync("css=section");
        Assert.That(done, Is.EqualTo(false));
        await Page.EvalOnSelectorAsync("div", "div => div.remove()");
        Assert.That(await waitFor, Is.EqualTo(true));
        Assert.That(done, Is.EqualTo(true));
    }

    [UpstreamTest("WA-065 should support some fancy xpath")]
    public async Task ShouldSupportSomeFancyXpath()
    {
        await Page.SetContentAsync("<p>red herring</p><p>hello  world  </p>");
        var waitForXPath = Page.WaitForSelectorAsync("//p[normalize-space(.)=\"hello world\"]");
        Assert.That(await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath), Is.EqualTo("hello  world  "));
    }

    [UpstreamTest("WA-066 should respect timeout xpath")]

    public async Task ShouldRespectTimeoutXpath()
    {
        Exception error = null;
        error = await Catch(() => Page.WaitForSelectorAsync("//div", new() { State = WaitForSelectorState.Attached, Timeout = 3000 }));
        Assert.That(error, Is.Not.Null);
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded"));
        Assert.That(error.Message, Does.Contain("waiting for Locator(\"//div\")"));
        Assert.That(error, Is.InstanceOf<TimeoutException>());
    }

    [UpstreamTest("WA-067 should run in specified frame xpath")]
    public async Task ShouldRunInSpecifiedFrameXpath()
    {
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        await AttachFrame(Page, "frame2", Server.EmptyPage);
        var frame1 = Page.Frames[1];
        var frame2 = Page.Frames[2];
        var waitForXPathPromise = frame2.WaitForSelectorAsync("//div", new() { State = WaitForSelectorState.Attached });
        await frame1.EvaluateAsync(addElement, "div");
        await frame2.EvaluateAsync(addElement, "div");
        var eHandle = await waitForXPathPromise;
        Assert.That(await eHandle.OwnerFrameAsync(), Is.SameAs(frame2));
    }

    [UpstreamTest("WA-068 should throw when frame is detached xpath")]

    public async Task ShouldThrowWhenFrameIsDetachedXpath()
    {
        await AttachFrame(Page, "frame1", Server.EmptyPage);
        var frame = Page.Frames[1];
        Exception waitError = null;
        // .NET: waitForSelector(...).catch(e => waitError = e) is a local function that is called in its place.
        async Task WaitForSelector()
        {
            waitError = await Catch(() => frame.WaitForSelectorAsync("//*[@class=\"box\"]"));
        }
        var waitPromise = WaitForSelector();
        await DetachFrame(Page, "frame1");
        await waitPromise;
        Assert.That(waitError, Is.Not.Null);
        Assert.That(waitError.Message, Does.Contain("Frame was detached"));
    }

    [UpstreamTest("WA-069 should return the element handle xpath")]
    public async Task ShouldReturnTheElementHandleXpath()
    {
        var waitForXPath = Page.WaitForSelectorAsync("//*[@class=\"zombo\"]");
        await Page.SetContentAsync("<div class='zombo'>anything</div>");
        Assert.That(await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath), Is.EqualTo("anything"));
    }

    [UpstreamTest("WA-070 should allow you to select an element with single slash xpath")]
    public async Task ShouldAllowYouToSelectAnElementWithSingleSlashXpath()
    {
        await Page.SetContentAsync("<div>some text</div>");
        var waitForXPath = Page.WaitForSelectorAsync("//html/body/div");
        Assert.That(await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath), Is.EqualTo("some text"));
    }

    [UpstreamTest("WA-071 should correctly handle hidden shadow host")]
    public async Task ShouldCorrectlyHandleHiddenShadowHost()
    {
        await Page.SetContentAsync(@"
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
        Assert.That(await Page.TextContentAsync("div"), Is.EqualTo("Find me"));
        await Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden });
    }

    [UpstreamTest("WA-074 should fail if element handle was detached while waiting")]
    public async Task ShouldFailIfElementHandleWasDetachedWhileWaiting()
    {
        await Page.SetContentAsync("<button>hello</button>");
        var button = await Page.QuerySelectorAsync("button");
        var promise = Catch(() => button.WaitForSelectorAsync("something"));
        await Page.WaitForTimeoutAsync(100);
        await Page.EvaluateAsync("() => document.body.innerText = ''");
        var error = await promise;
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("WA-075 should succeed if element handle was detached while waiting for hidden")]
    public async Task ShouldSucceedIfElementHandleWasDetachedWhileWaitingForHidden()
    {
        await Page.SetContentAsync("<button>hello</button>");
        var button = await Page.QuerySelectorAsync("button");
        var promise = button.WaitForSelectorAsync("something", new() { State = WaitForSelectorState.Hidden });
        await Page.WaitForTimeoutAsync(1000);
        await Page.EvaluateAsync("() => document.body.innerText = ''");
        await promise;
    }

    [UpstreamTest("WA-076 should succeed if element handle was detached while waiting for detached")]
    public async Task ShouldSucceedIfElementHandleWasDetachedWhileWaitingForDetached()
    {
        await Page.SetContentAsync("<button>hello</button>");
        var button = await Page.QuerySelectorAsync("button");
        var promise = button.WaitForSelectorAsync("something", new() { State = WaitForSelectorState.Detached });
        await Page.WaitForTimeoutAsync(1000);
        await Page.EvaluateAsync("() => document.body.innerText = ''");
        await promise;
    }
}
