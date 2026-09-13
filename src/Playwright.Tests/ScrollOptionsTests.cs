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

namespace Microsoft.Playwright.Tests;

public class ScrollOptionsTests : PageTestEx
{
    public override BrowserNewContextOptions ContextOptions() => new() { HasTouch = true };

    [PlaywrightTest]
    public async Task ShouldRespectScrollOption(
        [Values("page", "frame", "locator", "elementHandle")] string api,
        [Values("click", "dblclick", "hover", "tap", "check", "uncheck", "setChecked", "setUnchecked")] string action,
        [Values(ScrollMode.None, ScrollMode.Auto, null)] ScrollMode? scroll,
        [Values(false, true)] bool nested)
    {
        await Page.SetContentAsync($$"""
            <style>body { margin: 0; } #container { width: 200px; height: 200px; overflow: auto; }</style>
            <div id="container" style="{{(nested ? "" : "width: auto; height: auto; overflow: visible;")}}">
              <div style="padding: 1800px 0 0 1800px; width: 3000px; height: 3000px;">
                <input id="target" type="checkbox" style="width: 30px; height: 30px;"
                       onpointerover="window.hovered = true" onclick="window.clicked = true">
              </div>
            </div>
            """);
        var initiallyChecked = action is "uncheck" or "setUnchecked";
        await Page.Locator("#target").EvaluateAsync("(element, value) => element.checked = value", initiallyChecked);
        var before = await ScrollOffsetsAsync();
        Assert.AreEqual(new[] { 0, 0, 0, 0 }, before);

        if (scroll == ScrollMode.None)
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => PerformActionAsync(api, action, scroll));
            StringAssert.Contains("outside of the viewport", exception.Message);
            Assert.AreEqual(before, await ScrollOffsetsAsync());
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.clicked || !!window.hovered"));
            Assert.AreEqual(initiallyChecked, await Page.Locator("#target").IsCheckedAsync());

            await Page.Locator("#target").ScrollIntoViewIfNeededAsync();
            before = await ScrollOffsetsAsync();
            await PerformActionAsync(api, action, scroll);
            Assert.AreEqual(before, await ScrollOffsetsAsync());
        }
        else
        {
            await PerformActionAsync(api, action, scroll);
            Assert.AreNotEqual(before, await ScrollOffsetsAsync());
        }

        Assert.True(await Page.EvaluateAsync<bool>(action == "hover" ? "() => !!window.hovered" : "() => !!window.clicked"));
        if (action is "check" or "uncheck" or "setChecked" or "setUnchecked")
        {
            Assert.AreEqual(!initiallyChecked, await Page.Locator("#target").IsCheckedAsync());
        }
    }

    [PlaywrightTest]
    public async Task ShouldNotScrollWithoutForce()
    {
        await Page.SetContentAsync("<button style='margin-top: 2000px' onclick='window.clicked = true'>Click</button>");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.Locator("button").ClickAsync(new() { Scroll = ScrollMode.None, Timeout = 500 }));
        StringAssert.Contains("outside of the viewport", exception.Message);
        Assert.AreEqual(0, await Page.EvaluateAsync<int>("window.scrollY"));
        Assert.False(await Page.EvaluateAsync<bool>("() => !!window.clicked"));

        await Page.Locator("button").ClickAsync();
        Assert.True(await Page.EvaluateAsync<bool>("() => !!window.clicked"));
        Assert.Greater(await Page.EvaluateAsync<int>("window.scrollY"), 0);
    }

    [PlaywrightTest]
    public async Task ShouldRespectScrollOptionWhenDragging(
        [Values("page", "frame", "locator")] string api,
        [Values(ScrollMode.None, ScrollMode.Auto, null)] ScrollMode? scroll,
        [Values(false, true)] bool targetOutside)
    {
        await Page.SetContentAsync($"""
            <div id="source" onmousedown="window.started = true" style="width: 100px; height: 100px; position: absolute; top: {(targetOutside ? 0 : 2000)}px;">Drag</div>
            <div id="target" style="width: 100px; height: 100px; position: absolute; top: {(targetOutside ? 2000 : 0)}px;"
                 onmouseup="window.finished = true">Drop</div>
            """);
        Func<Task> drag = api switch
        {
            "page" => () => Page.DragAndDropAsync("#source", "#target", new() { Scroll = scroll, Force = true }),
            "frame" => () => Page.MainFrame.DragAndDropAsync("#source", "#target", new() { Scroll = scroll, Force = true }),
            _ => () => Page.Locator("#source").DragToAsync(Page.Locator("#target"), new() { Scroll = scroll, Force = true }),
        };
        if (scroll == ScrollMode.None)
        {
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(drag);
            StringAssert.Contains("outside of the viewport", exception.Message);
            Assert.AreEqual(0, await Page.EvaluateAsync<int>("window.scrollY"));
            Assert.False(await Page.EvaluateAsync<bool>("() => !!window.finished"));
        }
        else
        {
            await drag();
            Assert.True(await Page.EvaluateAsync<bool>("() => !!window.started && !!window.finished"));
        }
    }

    private Task<int[]> ScrollOffsetsAsync() => Page.EvaluateAsync<int[]>("() => [scrollX, scrollY, document.querySelector('#container').scrollLeft, document.querySelector('#container').scrollTop]");

    private async Task PerformActionAsync(string api, string action, ScrollMode? scroll)
    {
        var locator = Page.Locator("#target");
        var element = await locator.ElementHandleAsync();
        var frame = Page.MainFrame;
        await ((api, action) switch
        {
            ("page", "click") => Page.ClickAsync("#target", new() { Scroll = scroll, Force = true }),
            ("page", "dblclick") => Page.DblClickAsync("#target", new() { Scroll = scroll, Force = true }),
            ("page", "hover") => Page.HoverAsync("#target", new() { Scroll = scroll, Force = true }),
            ("page", "tap") => Page.TapAsync("#target", new() { Scroll = scroll, Force = true }),
            ("page", "check") => Page.CheckAsync("#target", new() { Scroll = scroll, Force = true }),
            ("page", "uncheck") => Page.UncheckAsync("#target", new() { Scroll = scroll, Force = true }),
            ("page", "setChecked") => Page.SetCheckedAsync("#target", true, new() { Scroll = scroll, Force = true }),
            ("page", "setUnchecked") => Page.SetCheckedAsync("#target", false, new() { Scroll = scroll, Force = true }),
            ("frame", "click") => frame.ClickAsync("#target", new() { Scroll = scroll, Force = true }),
            ("frame", "dblclick") => frame.DblClickAsync("#target", new() { Scroll = scroll, Force = true }),
            ("frame", "hover") => frame.HoverAsync("#target", new() { Scroll = scroll, Force = true }),
            ("frame", "tap") => frame.TapAsync("#target", new() { Scroll = scroll, Force = true }),
            ("frame", "check") => frame.CheckAsync("#target", new() { Scroll = scroll, Force = true }),
            ("frame", "uncheck") => frame.UncheckAsync("#target", new() { Scroll = scroll, Force = true }),
            ("frame", "setChecked") => frame.SetCheckedAsync("#target", true, new() { Scroll = scroll, Force = true }),
            ("frame", "setUnchecked") => frame.SetCheckedAsync("#target", false, new() { Scroll = scroll, Force = true }),
            ("locator", "click") => locator.ClickAsync(new() { Scroll = scroll, Force = true }),
            ("locator", "dblclick") => locator.DblClickAsync(new() { Scroll = scroll, Force = true }),
            ("locator", "hover") => locator.HoverAsync(new() { Scroll = scroll, Force = true }),
            ("locator", "tap") => locator.TapAsync(new() { Scroll = scroll, Force = true }),
            ("locator", "check") => locator.CheckAsync(new() { Scroll = scroll, Force = true }),
            ("locator", "uncheck") => locator.UncheckAsync(new() { Scroll = scroll, Force = true }),
            ("locator", "setChecked") => locator.SetCheckedAsync(true, new() { Scroll = scroll, Force = true }),
            ("locator", "setUnchecked") => locator.SetCheckedAsync(false, new() { Scroll = scroll, Force = true }),
            ("elementHandle", "click") => element.ClickAsync(new() { Scroll = scroll, Force = true }),
            ("elementHandle", "dblclick") => element.DblClickAsync(new() { Scroll = scroll, Force = true }),
            ("elementHandle", "hover") => element.HoverAsync(new() { Scroll = scroll, Force = true }),
            ("elementHandle", "tap") => element.TapAsync(new() { Scroll = scroll, Force = true }),
            ("elementHandle", "check") => element.CheckAsync(new() { Scroll = scroll, Force = true }),
            ("elementHandle", "uncheck") => element.UncheckAsync(new() { Scroll = scroll, Force = true }),
            ("elementHandle", "setChecked") => element.SetCheckedAsync(true, new() { Scroll = scroll, Force = true }),
            ("elementHandle", "setUnchecked") => element.SetCheckedAsync(false, new() { Scroll = scroll, Force = true }),
            _ => throw new ArgumentException($"Unknown action: {api}/{action}"),
        });
    }
}
