/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
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


namespace Microsoft.Playwright.Tests.Locator;

public class LocatorAnyFrameTests : PageTestEx
{
    private Task RoutePage(string url, string body)
    {
        return Page.RouteAsync("**/" + url, route => route.FulfillAsync(new()
        {
            Body = body,
            ContentType = "text/html"
        }));
    }

    private async Task WaitForAllFrames(int frameCount, string selector)
    {
        // Wait for all child frames to load their content, so that the search
        // deterministically sees elements in all of them.
        while (Page.Frames.Count != frameCount)
        {
            await Task.Delay(10);
        }
        foreach (var frame in Page.Frames)
        {
            if (frame != Page.MainFrame)
            {
                await frame.WaitForSelectorAsync(selector, new() { State = WaitForSelectorState.Attached });
            }
        }
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should click a button inside an iframe")]
    public async Task ShouldClickAButtonInsideAnIframe()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<button onclick=\"window.__clicked = true\">Click me</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.FrameLocator().GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        Assert.IsTrue(await Page.Frames[1].EvaluateAsync<bool>("() => window.__clicked"));
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should click a button in the main frame")]
    public async Task ShouldClickAButtonInTheMainFrame()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe><button onclick=\"window.__clicked = true\">Click me</button>");
        await RoutePage("a.html", "<div>No buttons here</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.FrameLocator().Locator("button").ClickAsync();
        Assert.IsTrue(await Page.EvaluateAsync<bool>("() => window.__clicked"));
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should fail click when elements match in multiple frames")]
    public async Task ShouldFailClickWhenElementsMatchInMultipleFrames()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage("a.html", "<button>one</button>");
        await RoutePage("b.html", "<button>two</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(3, "button");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FrameLocator().Locator("button").ClickAsync(new() { Timeout = 3000 }));
        StringAssert.Contains("frameLocator() matched elements in multiple frames", error.Message);
        StringAssert.Contains("waiting for FrameLocator().Locator(\"button\")", error.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should fail click upon strict mode violation inside a single frame")]
    public async Task ShouldFailClickUponStrictModeViolationInsideASingleFrame()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<button>one</button><button>two</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(2, "button");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FrameLocator().Locator("button").ClickAsync(new() { Timeout = 3000 }));
        StringAssert.Contains("strict mode violation", error.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should time out on click when there are no matches")]
    public async Task ShouldTimeOutOnClickWhenThereAreNoMatches()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div>Nothing here</div>");
        await Page.GotoAsync(Server.EmptyPage);
        var error = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.FrameLocator().Locator("button").ClickAsync(new() { Timeout = 1000 }));
        StringAssert.Contains("Timeout 1000ms exceeded", error.Message);
        StringAssert.Contains("waiting for FrameLocator().Locator(\"button\")", error.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should count elements in a single frame")]
    public async Task ShouldCountElementsInASingleFrame()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div>1</div><div>2</div><div>3</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(2, "div");
        Assert.AreEqual(3, await Page.FrameLocator().Locator("div").CountAsync());
        Assert.AreEqual(0, await Page.FrameLocator().Locator("button").CountAsync());
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should fail count when elements match in multiple frames")]
    public async Task ShouldFailCountWhenElementsMatchInMultipleFrames()
    {
        await RoutePage("empty.html", "<div>main</div><iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div>child</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(2, "div");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FrameLocator().Locator("div").CountAsync());
        StringAssert.Contains("frameLocator() matched elements in multiple frames", error.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support toHaveCount")]
    public async Task ShouldSupportToHaveCount()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("span")).ToHaveCountAsync(2);
        await Expect(Page.FrameLocator().Locator("button")).ToHaveCountAsync(0);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should wait for a frame to appear with toHaveCount")]
    public async Task ShouldWaitForAFrameToAppearWithToHaveCount()
    {
        await RoutePage("empty.html", "<div>No frames yet</div>");
        await RoutePage("a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.EvaluateAsync(@"() => {
            setTimeout(() => {
                const iframe = document.createElement('iframe');
                iframe.src = 'a.html';
                document.body.appendChild(iframe);
            }, 500);
        }");
        await Expect(Page.FrameLocator().Locator("span")).ToHaveCountAsync(2);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should fail toHaveCount when elements match in multiple frames")]
    public async Task ShouldFailToHaveCountWhenElementsMatchInMultipleFrames()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage("a.html", "<span>one</span>");
        await RoutePage("b.html", "<span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(3, "span");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(Page.FrameLocator().Locator("span")).ToHaveCountAsync(2, new() { Timeout = 3000 }));
        StringAssert.Contains("frameLocator() matched elements in multiple frames", error.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support toHaveText")]
    public async Task ShouldSupportToHaveText()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div>Hello iframe</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("div")).ToHaveTextAsync("Hello iframe");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support toHaveText with an array")]
    public async Task ShouldSupportToHaveTextWithAnArray()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("span")).ToHaveTextAsync(new[] { "one", "two" });
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should fail toHaveText when elements match in multiple frames")]
    public async Task ShouldFailToHaveTextWhenElementsMatchInMultipleFrames()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe><iframe src=\"b.html\"></iframe>");
        await RoutePage("a.html", "<div>one</div>");
        await RoutePage("b.html", "<div>two</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(3, "div");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Expect(Page.FrameLocator().Locator("div")).ToHaveTextAsync("one", new() { Timeout = 3000 }));
        StringAssert.Contains("frameLocator() matched elements in multiple frames", error.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support evaluate")]
    public async Task ShouldSupportEvaluate()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div data-foo=\"bar\">Hello</div>");
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("bar", await Page.FrameLocator().Locator("div").EvaluateAsync<string>("e => e.getAttribute('data-foo')"));
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support evaluateAll")]
    public async Task ShouldSupportEvaluateAll()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<span>one</span><span>two</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(2, "span");
        CollectionAssert.AreEqual(new[] { "one", "two" }, await Page.FrameLocator().Locator("span").EvaluateAllAsync<string[]>("els => els.map(e => e.textContent)"));
        Assert.AreEqual(0, await Page.FrameLocator().Locator("button").EvaluateAllAsync<int>("els => els.length"));
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support hasText filter")]
    public async Task ShouldSupportHasTextFilter()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div>foo</div><div>bar</div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("div", new() { HasText = "bar" })).ToHaveTextAsync("bar");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support first/last/nth")]
    public async Task ShouldSupportFirstLastNth()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<span>one</span><span>two</span><span>three</span>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(2, "span");
        await Expect(Page.FrameLocator().Locator("span").First).ToHaveTextAsync("one");
        await Expect(Page.FrameLocator().Locator("span").Last).ToHaveTextAsync("three");
        await Expect(Page.FrameLocator().Locator("span").Nth(1)).ToHaveTextAsync("two");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support composite locators")]
    public async Task ShouldSupportCompositeLocators()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div><span>foo</span></div><div><i>bar</i></div>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("div", new() { Has = Page.Locator("span") })).ToHaveTextAsync("foo");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should only search frames inside the starting frame")]
    public async Task ShouldOnlySearchFramesInsideTheStartingFrame()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe><button>main</button>");
        await RoutePage("a.html", "<iframe src=\"b.html\"></iframe>");
        await RoutePage("b.html", "<button>deep</button>");
        await Page.GotoAsync(Server.EmptyPage);
        while (Page.Frames.Count != 3)
        {
            await Task.Delay(10);
        }
        var deepFrame = Page.Frames.First(f => f.Url.Contains("b.html"));
        await deepFrame.WaitForSelectorAsync("button", new() { State = WaitForSelectorState.Attached });
        var middleFrame = Page.Frames.First(f => f.Url.Contains("a.html"));
        await Expect(middleFrame.FrameLocator().Locator("button")).ToHaveTextAsync("deep");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should enter a frame found in a nested frame")]
    public async Task ShouldEnterAFrameFoundInANestedFrame()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe><button>main</button>");
        await RoutePage("a.html", "<iframe id=\"target\" src=\"b.html\"></iframe><button>decoy</button>");
        await RoutePage("b.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().FrameLocator("#target").Locator("button")).ToHaveTextAsync("inside");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should click inside an entered frame")]
    public async Task ShouldClickInsideAnEnteredFrame()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<iframe id=\"target\" src=\"b.html\"></iframe><button>Click me</button>");
        await RoutePage("b.html", "<button onclick=\"window.__clicked = true\">Click me</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Page.FrameLocator().FrameLocator("#target").GetByRole(AriaRole.Button, new() { Name = "Click me" }).ClickAsync();
        var frame = Page.Frames.First(f => f.Url.Contains("b.html"));
        Assert.IsTrue(await frame.EvaluateAsync<bool>("() => window.__clicked"));
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should not search nested frames after entering a frame")]
    public async Task ShouldNotSearchNestedFramesAfterEnteringAFrame()
    {
        await RoutePage("empty.html", "<iframe id=\"target\" src=\"a.html\"></iframe><button>main</button>");
        await RoutePage("a.html", "<iframe src=\"b.html\"></iframe>");
        await RoutePage("b.html", "<button>deep</button>");
        await Page.GotoAsync(Server.EmptyPage);
        while (Page.Frames.Count != 3)
        {
            await Task.Delay(10);
        }
        // The entered frame itself has no button, and we do not look inside its nested frames.
        await Expect(Page.FrameLocator().FrameLocator("#target").Locator("button")).ToHaveCountAsync(0);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support two frameLocators")]
    public async Task ShouldSupportTwoFrameLocators()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<iframe id=\"x\" src=\"b.html\"></iframe>");
        await RoutePage("b.html", "<iframe id=\"y\" src=\"c.html\"></iframe><button>decoy</button>");
        await RoutePage("c.html", "<button>bottom</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().FrameLocator("#x").FrameLocator("#y").Locator("button")).ToHaveTextAsync("bottom");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support locator before frameLocator")]
    public async Task ShouldSupportLocatorBeforeFrameLocator()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<section><iframe src=\"b.html\"></iframe></section><iframe src=\"c.html\"></iframe>");
        await RoutePage("b.html", "<button>in-section</button>");
        await RoutePage("c.html", "<button>outside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("section").FrameLocator("iframe").Locator("button")).ToHaveTextAsync("in-section");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support owner of a frameLocator")]
    public async Task ShouldSupportOwnerOfAFrameLocator()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<iframe id=\"target\" src=\"b.html\"></iframe>");
        await RoutePage("b.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual("target", await Page.FrameLocator().FrameLocator("#target").Owner.GetAttributeAsync("id"));
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support contentFrame")]
    public async Task ShouldSupportContentFrame()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<iframe id=\"target\" src=\"b.html\"></iframe><button>decoy</button>");
        await RoutePage("b.html", "<button>inside</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await Expect(Page.FrameLocator().Locator("#target").ContentFrame.Locator("button")).ToHaveTextAsync("inside");
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should not allow frameLocator() inside a composite locator")]
    public async Task ShouldNotAllowFrameLocatorInsideACompositeLocator()
    {
        await RoutePage("empty.html", "<button>main</button><iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<a href=\"#\">link</a>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(2, "a");

        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("button").Or(Page.FrameLocator().Locator("a")).CountAsync());
        StringAssert.Contains("frameLocator() is not allowed inside composite locators", error.Message);

        var error2 = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("button").Filter(new() { Has = Page.FrameLocator().Locator("a") }).CountAsync());
        StringAssert.Contains("frameLocator() is not allowed inside composite locators", error2.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should support a composite locator under frameLocator()")]
    public async Task ShouldSupportACompositeLocatorUnderFrameLocator()
    {
        await RoutePage("empty.html", "<iframe src=\"a.html\"></iframe>");
        await RoutePage("a.html", "<div class=\"classname\">first</div><button>second</button>");
        await Page.GotoAsync(Server.EmptyPage);
        await WaitForAllFrames(2, "button");
        await Expect(Page.FrameLocator().Locator(".classname").Or(Page.GetByRole(AriaRole.Button))).ToHaveTextAsync(new[] { "first", "second" });
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should not allow first/last/nth on frameLocator()")]
    public void ShouldNotAllowFirstLastNthOnFrameLocator()
    {
        var error1 = Assert.Throws<PlaywrightException>(() => _ = Page.FrameLocator().First);
        StringAssert.Contains("Selecting the nth frame is not allowed on frameLocator()", error1.Message);
        var error2 = Assert.Throws<PlaywrightException>(() => _ = Page.FrameLocator().Last);
        StringAssert.Contains("Selecting the nth frame is not allowed on frameLocator()", error2.Message);
        var error3 = Assert.Throws<PlaywrightException>(() => Page.FrameLocator().Nth(1));
        StringAssert.Contains("Selecting the nth frame is not allowed on frameLocator()", error3.Message);
    }

    [PlaywrightTest("locator-any-frame.spec.ts", "should not allow owner on frameLocator()")]
    public async Task ShouldNotAllowOwnerOnFrameLocator()
    {
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.FrameLocator().Owner.CountAsync());
        StringAssert.Contains("Selector cannot be empty after frameLocator()", error.Message);
    }
}
