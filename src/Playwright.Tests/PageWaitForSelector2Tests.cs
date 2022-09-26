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

public class PageWaitForSelector2Tests : PageTestEx
{
    private const string AddElement = "tag => document.body.appendChild(document.createElement(tag))";

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should survive cross-process navigation")]
    public async Task ShouldSurviveCrossProcessNavigation()
    {
        bool boxFound = false;
        var waitForSelector = Page.WaitForSelectorAsync(".box").ContinueWith(_ => boxFound = true);
        await Page.GotoAsync(Server.EmptyPage);
        Assert.False(boxFound);
        await Page.ReloadAsync();
        Assert.False(boxFound);
        await Page.GotoAsync(Server.CrossProcessPrefix + "/grid.html");
        await waitForSelector;
        Assert.True(boxFound);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for visible")]
    public async Task ShouldWaitForVisible()
    {
        bool divFound = false;
        var waitForSelector = Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Visible })
            .ContinueWith(_ => divFound = true);
        await Page.SetContentAsync("<div style='display: none; visibility: hidden;'>1</div>");
        Assert.False(divFound);
        await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('display')");
        Assert.False(divFound);
        await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('visibility')");
        Assert.True(await waitForSelector);
        Assert.True(divFound);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should not consider visible when zero-sized")]
    public async Task ShouldNotConsiderVisibleWhenZeroSized()
    {
        await Page.SetContentAsync("<div style='width: 0; height: 0;'>1</div>");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForSelectorAsync("div", new() { Timeout = 1000 }));
        StringAssert.Contains("Timeout 1000ms", exception.Message);
        await Page.EvaluateAsync("() => document.querySelector('div').style.width = '10px'");
        exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.WaitForSelectorAsync("div", new() { Timeout = 1000 }));
        StringAssert.Contains("Timeout 1000ms", exception.Message);
        await Page.EvaluateAsync("() => document.querySelector('div').style.height = '10px'");
        Assert.NotNull(await Page.WaitForSelectorAsync("div", new() { Timeout = 1000 }));
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for visible recursively")]
    public async Task ShouldWaitForVisibleRecursively()
    {
        bool divVisible = false;
        var waitForSelector = Page.WaitForSelectorAsync("div#inner", new() { State = WaitForSelectorState.Visible }).ContinueWith(_ => divVisible = true);
        await Page.SetContentAsync("<div style='display: none; visibility: hidden;'><div id='inner'>hi</div></div>");
        Assert.False(divVisible);
        await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('display')");
        Assert.False(divVisible);
        await Page.EvaluateAsync("document.querySelector('div').style.removeProperty('visibility')");
        Assert.True(await waitForSelector);
        Assert.True(divVisible);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "hidden should wait for removal")]
    public async Task HiddenShouldWaitForRemoval()
    {
        await Page.SetContentAsync("<div>content</div>");
        bool divRemoved = false;
        var waitForSelector = Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden })
            .ContinueWith(_ => divRemoved = true);
        await Page.WaitForSelectorAsync("div"); // do a round trip
        Assert.False(divRemoved);
        await Page.EvaluateAsync("document.querySelector('div').remove()");
        Assert.True(await waitForSelector);
        Assert.True(divRemoved);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return null if waiting to hide non-existing element")]
    public async Task ShouldReturnNullIfWaitingToHideNonExistingElement()
    {
        var handle = await Page.WaitForSelectorAsync("non-existing", new() { State = WaitForSelectorState.Hidden });
        Assert.Null(handle);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respect timeout")]
    public async Task ShouldRespectTimeout()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Attached, Timeout = 3000 }));

        StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
        StringAssert.Contains("waiting for selector \"div\"", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should have an error message specifically for awaiting an element to be hidden")]
    public async Task ShouldHaveAnErrorMessageSpecificallyForAwaitingAnElementToBeHidden()
    {
        await Page.SetContentAsync("<div>content</div>");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
            => Page.WaitForSelectorAsync("div", new() { State = WaitForSelectorState.Hidden, Timeout = 1000 }));

        StringAssert.Contains("Timeout 1000ms exceeded", exception.Message);
        StringAssert.Contains("waiting for selector \"div\" to be hidden", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respond to node attribute mutation")]
    public async Task ShouldRespondToNodeAttributeMutation()
    {
        bool divFound = false;
        var waitForSelector = Page.WaitForSelectorAsync(".zombo", new() { State = WaitForSelectorState.Attached }).ContinueWith(_ => divFound = true);
        await Page.SetContentAsync("<div class='notZombo'></div>");
        Assert.False(divFound);
        await Page.EvaluateAsync("document.querySelector('div').className = 'zombo'");
        Assert.True(await waitForSelector);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return the element handle")]
    public async Task ShouldReturnTheElementHandle()
    {
        var waitForSelector = Page.WaitForSelectorAsync(".zombo");
        await Page.SetContentAsync("<div class='zombo'>anything</div>");
        Assert.AreEqual("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForSelector));
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should have correct stack trace for timeout")]
    public async Task ShouldHaveCorrectStackTraceForTimeout()
    {
        Exception exception = null;
        try
        {
            await Page.WaitForSelectorAsync(".zombo", new() { Timeout = 10 });
        }
        catch (Exception e)
        {
            exception = e;
        }
        StringAssert.Contains("WaitForSelector2Tests", exception.ToString());
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should support >> selector syntax")]
    public async Task ShouldSupportSelectorSyntax()
    {
        await Page.GotoAsync(Server.EmptyPage);
        var frame = Page.MainFrame;
        var watchdog = frame.WaitForSelectorAsync("css=div >> css=span", new() { State = WaitForSelectorState.Attached });
        await frame.EvaluateAsync(AddElement, "br");
        await frame.EvaluateAsync(AddElement, "div");
        await frame.EvaluateAsync("() => document.querySelector('div').appendChild(document.createElement('span'))");
        var eHandle = await watchdog;
        var tagProperty = await eHandle.GetPropertyAsync("tagName");
        string tagName = await tagProperty.JsonValueAsync<string>();
        Assert.AreEqual("SPAN", tagName);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for detached if already detached")]
    public async Task ShouldWaitForDetachedIfAlreadyDetached()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\">43543</section>");
        Assert.Null(await Page.WaitForSelectorAsync("css=div", new() { State = WaitForSelectorState.Detached }));
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should wait for detached")]
    public async Task ShouldWaitForDetached()
    {
        await Page.SetContentAsync("<section id=\"testAttribute\"><div>43543</div></section>");
        var waitForTask = Page.WaitForSelectorAsync("css=div", new() { State = WaitForSelectorState.Detached });
        Assert.False(waitForTask.IsCompleted);
        await Page.WaitForSelectorAsync("css=section");
        Assert.False(waitForTask.IsCompleted);
        await Page.EvalOnSelectorAsync("div", "div => div.remove()");
        await waitForTask;
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should support some fancy xpath")]
    public async Task ShouldSupportSomeFancyXpath()
    {
        await Page.SetContentAsync("<p>red herring</p><p>hello  world  </p>");
        var waitForXPath = Page.WaitForSelectorAsync("//p[normalize-space(.)=\"hello world\"]");
        Assert.AreEqual("hello  world  ", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should respect timeout xpath")]
    public async Task ShouldRespectTimeoutXpath()
    {
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(()
                => Page.WaitForSelectorAsync("//div", new() { State = WaitForSelectorState.Attached, Timeout = 3000 }));

        StringAssert.Contains("Timeout 3000ms exceeded", exception.Message);
        StringAssert.Contains("waiting for selector \"//div\"", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should run in specified frame xpath")]
    public async Task ShouldRunInSpecifiedFrameXPath()
    {
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        await FrameUtils.AttachFrameAsync(Page, "frame2", Server.EmptyPage);
        var frame1 = Page.Frames.First(f => f.Name == "frame1");
        var frame2 = Page.Frames.First(f => f.Name == "frame2");
        var waitForXPathPromise = frame2.WaitForSelectorAsync("//div", new() { State = WaitForSelectorState.Attached });
        await frame1.EvaluateAsync(AddElement, "div");
        await frame2.EvaluateAsync(AddElement, "div");
        var eHandle = await waitForXPathPromise;
        Assert.AreEqual(frame2, await eHandle.OwnerFrameAsync());
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should throw when frame is detached xpath")]
    public async Task ShouldThrowWhenFrameIsDetachedXPath()
    {
        await FrameUtils.AttachFrameAsync(Page, "frame1", Server.EmptyPage);
        var frame = Page.FirstChildFrame();
        var waitPromise = frame.WaitForSelectorAsync("//*[@class=\"box\"]");
        await FrameUtils.DetachFrameAsync(Page, "frame1");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => waitPromise);
        StringAssert.Contains("Frame was detached", exception.Message);
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should return the element handle xpath")]
    public async Task ShouldReturnTheElementHandleXPath()
    {
        var waitForXPath = Page.WaitForSelectorAsync("//*[@class=\"zombo\"]");
        await Page.SetContentAsync("<div class='zombo'>anything</div>");
        Assert.AreEqual("anything", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
    }

    [PlaywrightTest("page-wait-for-selector-2.spec.ts", "should allow you to select an element with single slash xpath")]
    public async Task ShouldAllowYouToSelectAnElementWithSingleSlashXPath()
    {
        await Page.SetContentAsync("<div>some text</div>");
        var waitForXPath = Page.WaitForSelectorAsync("//html/body/div");
        Assert.AreEqual("some text", await Page.EvaluateAsync<string>("x => x.textContent", await waitForXPath));
    }
}
