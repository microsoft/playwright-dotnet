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

using System.Drawing;

namespace Microsoft.Playwright.Tests;

public class PageClickTests : PageTestEx
{
    [PlaywrightTest("page-click.spec.ts", "should click the button")]
    public async Task ShouldClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click svg")]
    public async Task ShouldClickSvg()
    {
        await Page.SetContentAsync($@"
                <svg height=""100"" width=""100"">
                  <circle onclick=""javascript:window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width=""3"" fill=""red""/>
                </svg>
            ");
        await Page.ClickAsync("circle");
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click svg")]
    public async Task ShouldClickTheButtonIfWindowNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync("delete window.Node");
        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click on a span with an inline element inside")]
    public async Task ShouldClickOnASpanWithAnInlineElementInside()
    {
        await Page.SetContentAsync($@"
                <style>
                span::before {{
                    content: 'q';
                }}
                </style>
                <span onclick='javascript:window.CLICKED=42'></span>
            ");
        await Page.ClickAsync("span");
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => window.CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button after navigation")]
    public async Task ShouldClickTheButtonAfterNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button after a cross origin navigation")]
    public async Task ShouldClickTheButtonAfterACrossOriginNavigation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        await Page.GotoAsync(Server.CrossProcessPrefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click with disabled javascript")]
    public async Task ShouldClickWithDisabledJavascript()
    {
        await using var context = await Browser.NewContextAsync(new() { JavaScriptEnabled = false });
        var page = await context.NewPageAsync();
        await page.GotoAsync(Server.Prefix + "/wrappedlink.html");
        await TaskUtils.WhenAll(
            page.ClickAsync("a"),
            page.WaitForNavigationAsync()
        );
        Assert.AreEqual(Server.Prefix + "/wrappedlink.html#clicked", page.Url);
    }

    [PlaywrightTest("page-click.spec.ts", "should click when one of inline box children is outside of viewport")]
    public async Task ShouldClickWhenOneOfInlineBoxChildrenIsOutsideOfViewport()
    {
        await Page.SetContentAsync($@"
            <style>
            i {{
                position: absolute;
                top: -1000px;
            }}
            </style>
            <span onclick='javascript:window.CLICKED = 42;'><i>woof</i><b>doggo</b></span>
            ");

        await Page.ClickAsync("span");
        Assert.AreEqual(42, await Page.EvaluateAsync<int>("() => window.CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should select the text by triple clicking")]
    public async Task ShouldSelectTheTextByTripleClicking()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        const string text = "This is the text that we are going to try to select. Let's see how it goes.";
        await Page.FillAsync("textarea", text);
        await Page.ClickAsync("textarea", new() { ClickCount = 3 });
        Assert.AreEqual(text, await Page.EvaluateAsync<string>(@"() => {
                const textarea = document.querySelector('textarea');
                return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
            }"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click offscreen buttons")]
    public async Task ShouldClickOffscreenButtons()
    {
        await Page.GotoAsync(Server.Prefix + "/offscreenbuttons.html");
        var messages = new List<string>();
        Page.Console += (_, e) => messages.Add(e.Text);

        for (int i = 0; i < 11; ++i)
        {
            // We might have scrolled to click a button - reset to (0, 0).
            await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
            await Page.ClickAsync($"#btn{i}");
        }
        Assert.AreEqual(new List<string>
            {
                "button #0 clicked",
                "button #1 clicked",
                "button #2 clicked",
                "button #3 clicked",
                "button #4 clicked",
                "button #5 clicked",
                "button #6 clicked",
                "button #7 clicked",
                "button #8 clicked",
                "button #9 clicked",
                "button #10 clicked"
            }, messages);
    }

    [PlaywrightTest("page-click.spec.ts", "should waitFor visible when already visible")]
    public async Task ShouldWaitForVisibleWhenAlreadyVisible()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should not wait with force")]
    public async Task ShouldNotWaitWithForce()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");

        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(()
            => Page.ClickAsync("button", new() { Force = true }));

        StringAssert.Contains("Element is not visible", exception.Message);
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should waitFor display:none to be gone")]
    public async Task ShouldWaitForDisplayNoneToBeGone()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
        var clickTask = Page.ClickAsync("button", new() { Timeout = 0 });

        await GiveItAChanceToClick(Page);

        Assert.False(clickTask.IsCompleted);
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("result"));

        await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'block'");
        await clickTask;

        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should waitFor visibility:hidden to be gone")]
    public async Task ShouldWaitForVisibilityhiddenToBeGone()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
        var clickTask = Page.ClickAsync("button", new() { Timeout = 0 });

        await GiveItAChanceToClick(Page);

        Assert.False(clickTask.IsCompleted);
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("result"));

        await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'visible'");
        await clickTask;

        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should waitFor visible when parent is hidden")]
    public async Task ShouldWaitForVisibleWhenParentIsHidden()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'none'");
        var clickTask = Page.ClickAsync("button", new() { Timeout = 0 });

        await GiveItAChanceToClick(Page);

        Assert.False(clickTask.IsCompleted);
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("result"));

        await Page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'block'");
        await clickTask;

        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click wrapped links")]
    public async Task ShouldClickWrappedLinks()
    {
        await Page.GotoAsync(Server.Prefix + "/wrappedlink.html");
        await Page.ClickAsync("a");
        Assert.True(await Page.EvaluateAsync<bool>("window.__clicked"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click on checkbox input and toggle")]
    public async Task ShouldClickOnCheckboxInputAndToggle()
    {
        await Page.GotoAsync(Server.Prefix + "/input/checkbox.html");
        Assert.Null(await Page.EvaluateAsync<bool?>("result.check"));
        await Page.ClickAsync("input#agree");
        Assert.True(await Page.EvaluateAsync<bool>("result.check"));
        Assert.AreEqual(new[] {
                "mouseover",
                "mouseenter",
                "mousemove",
                "mousedown",
                "mouseup",
                "click",
                "input",
                "change"
            }, await Page.EvaluateAsync<string[]>("result.events"));
        await Page.ClickAsync("input#agree");
        Assert.False(await Page.EvaluateAsync<bool>("result.check"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click on checkbox label and toggle")]
    public async Task ShouldClickOnCheckboxLabelAndToggle()
    {
        await Page.GotoAsync(Server.Prefix + "/input/checkbox.html");
        Assert.Null(await Page.EvaluateAsync("result.check"));
        await Page.ClickAsync("label[for=\"agree\"]");
        Assert.True(await Page.EvaluateAsync<bool>("result.check"));
        Assert.AreEqual(new[] {
                "click",
                "input",
                "change"
            }, await Page.EvaluateAsync<string[]>("result.events"));
        await Page.ClickAsync("label[for=\"agree\"]");
        Assert.False(await Page.EvaluateAsync<bool>("result.check"));
    }

    [PlaywrightTest("page-click.spec.ts", "should not hang with touch-enabled viewports")]
    public async Task ShouldNotHangWithTouchEnabledViewports()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = Playwright.Devices["iPhone 6"].ViewportSize,
            HasTouch = Playwright.Devices["iPhone 6"].HasTouch,
        });

        var page = await context.NewPageAsync();
        await page.Mouse.DownAsync();
        await page.Mouse.MoveAsync(100, 10);
        await page.Mouse.UpAsync();
    }

    [PlaywrightTest("page-click.spec.ts", "should scroll and click the button")]
    [Ignore("Flacky")]
    public async Task ShouldScrollAndClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.ClickAsync("#button-5");
        Assert.AreEqual("clicked", await Page.EvaluateAsync<string>("document.querySelector(\"#button-5\").textContent"));
        await Page.ClickAsync("#button-80");
        Assert.AreEqual("clicked", await Page.EvaluateAsync<string>("document.querySelector(\"#button-80\").textContent"));
    }

    [PlaywrightTest("page-click.spec.ts", "should double click the button")]
    public async Task ShouldDoubleClickTheButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"{
               window.double = false;
               const button = document.querySelector('button');
               button.addEventListener('dblclick', event => {
                 window.double = true;
               });
            }");
        var button = await Page.QuerySelectorAsync("button");
        await button.DblClickAsync();
        Assert.True(await Page.EvaluateAsync<bool>("double"));
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click a partially obscured button")]
    public async Task ShouldClickAPartiallyObscuredButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"{
                const button = document.querySelector('button');
                button.textContent = 'Some really long text that will go offscreen';

                button.style.position = 'absolute';
                button.style.left = '368px';
            }");
        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click a rotated button")]
    public async Task ShouldClickARotatedButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/rotatedButton.html");
        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should fire contextmenu event on right click")]
    public async Task ShouldFireContextmenuEventOnRightClick()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.ClickAsync("#button-8", new() { Button = MouseButton.Right });
        Assert.AreEqual("context menu", await Page.EvaluateAsync<string>("document.querySelector('#button-8').textContent"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click links which cause navigation")]
    public async Task ShouldClickLinksWhichCauseNavigation()
    {
        await Page.SetContentAsync($"<a href=\"{Server.EmptyPage}\">empty.html</a>");
        // This await should not hang.
        await Page.ClickAsync("a");
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button inside an iframe")]
    public async Task ShouldClickTheButtonInsideAnIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
        await FrameUtils.AttachFrameAsync(Page, "button-test", Server.Prefix + "/input/button.html");
        var frame = Page.FirstChildFrame();
        var button = await frame.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.AreEqual("Clicked", await frame.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button with fixed position inside an iframe")]
    [Skip(SkipAttribute.Targets.Chromium, SkipAttribute.Targets.Webkit)]
    public async Task ShouldClickTheButtonWithFixedPositionInsideAnIframe()
    {
        await Page.GotoAsync(Server.EmptyPage);
        await Page.SetViewportSizeAsync(500, 500);
        await Page.SetContentAsync("<div style=\"width:100px;height:2000px\">spacer</div>");
        await FrameUtils.AttachFrameAsync(Page, "button-test", Server.Prefix + "/input/button.html");
        var frame = Page.FirstChildFrame();
        await frame.EvalOnSelectorAsync("button", "button => button.style.setProperty('position', 'fixed')");
        await frame.ClickAsync("button");
        Assert.AreEqual("Clicked", await frame.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button with deviceScaleFactor set")]
    public async Task ShouldClickTheButtonWithDeviceScaleFactorSet()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 400,
                Height = 400,
            },
            DeviceScaleFactor = 5,
        });

        var page = await context.NewPageAsync();
        Assert.AreEqual(5, await page.EvaluateAsync<int>("window.devicePixelRatio"));
        await page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
        await FrameUtils.AttachFrameAsync(page, "button-test", Server.Prefix + "/input/button.html");
        var frame = page.FirstChildFrame();
        var button = await frame.QuerySelectorAsync("button");
        await button.ClickAsync();
        Assert.AreEqual("Clicked", await frame.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button with px border with relative point")]
    public async Task ShouldClickTheButtonWithPxBorderWithRelativePoint()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
        await Page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("window.result"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.AreEqual(TestConstants.IsWebKit ? 20 + 8 : 20, await Page.EvaluateAsync<int>("offsetX"));
        Assert.AreEqual(TestConstants.IsWebKit ? 10 + 8 : 10, await Page.EvaluateAsync<int>("offsetY"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button with em border with offset")]
    public async Task ShouldClickTheButtonWithEmBorderWithOffset()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '2em'");
        await Page.EvalOnSelectorAsync("button", "button => button.style.fontSize = '12px'");
        await Page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("window.result"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.AreEqual(TestConstants.IsWebKit ? 12 * 2 + 20 : 20, await Page.EvaluateAsync<int>("offsetX"));
        Assert.AreEqual(TestConstants.IsWebKit ? 12 * 2 + 10 : 10, await Page.EvaluateAsync<int>("offsetY"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click a very large button with offset")]
    public async Task ShouldClickAVeryLargeButtonWithOffset()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
        await Page.EvalOnSelectorAsync("button", "button => button.style.height = button.style.width = '2000px'");
        await Page.ClickAsync("button", new() { Position = new() { X = 1900, Y = 1910 } });
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("window.result"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.AreEqual(TestConstants.IsWebKit ? 1900 + 8 : 1900, await Page.EvaluateAsync<int>("offsetX"));
        Assert.AreEqual(TestConstants.IsWebKit ? 1910 + 8 : 1910, await Page.EvaluateAsync<int>("offsetY"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click a button in scrolling container with offset")]
    public async Task ShouldClickAButtonInScrollingContainerWithOffset()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
                const container = document.createElement('div');
                container.style.overflow = 'auto';
                container.style.width = '200px';
                container.style.height = '200px';
                button.parentElement.insertBefore(container, button);
                container.appendChild(button);
                button.style.height = '2000px';
                button.style.width = '2000px';
                button.style.borderWidth = '8px';
            }");

        await Page.ClickAsync("button", new() { Position = new() { X = 1900, Y = 1910 } });
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("window.result"));
        // Safari reports border-relative offsetX/offsetY.
        Assert.AreEqual(TestConstants.IsWebKit ? 1900 + 8 : 1900, await Page.EvaluateAsync<int>("offsetX"));
        Assert.AreEqual(TestConstants.IsWebKit ? 1910 + 8 : 1910, await Page.EvaluateAsync<int>("offsetY"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button with offset with page scale")]
    [Skip(SkipAttribute.Targets.Firefox)]
    public async Task ShouldClickTheButtonWithOffsetWithPageScale()
    {
        await using var context = await Browser.NewContextAsync(new()
        {
            ViewportSize = new()
            {
                Width = 400,
                Height = 400,
            },
            IsMobile = true,
        });

        var page = await context.NewPageAsync();

        await page.GotoAsync(Server.Prefix + "/input/button.html");
        await page.EvalOnSelectorAsync("button", @"button => {
                button.style.borderWidth = '8px';
                document.body.style.margin = '0';
            }");

        await page.ClickAsync("button", new() { Position = new() { X = 20, Y = 10 } });
        Assert.AreEqual("Clicked", await page.EvaluateAsync<string>("window.result"));

        var point = BrowserName switch
        {
            "chromium" => new(27, 18),
            "webkit" => new(26, 17),
            _ => new Point(28, 18),
        };

        Assert.AreEqual(point.X, Convert.ToInt32(await page.EvaluateAsync<decimal>("pageX")));
        Assert.AreEqual(point.Y, Convert.ToInt32(await page.EvaluateAsync<decimal>("pageY")));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for stable position")]
    public async Task ShouldWaitForStablePosition()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.transition = 'margin 500ms linear 0s';
                button.style.marginLeft = '200px';
                button.style.borderWidth = '0';
                button.style.width = '200px';
                button.style.height = '20px';
                // Set display to 'block'- otherwise Firefox layouts with non-even
                // values on Linux.
                button.style.display = 'block';
                document.body.style.margin = '0';
            }");

        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("window.result"));
        Assert.AreEqual(300, await Page.EvaluateAsync<int>("pageX"));
        Assert.AreEqual(10, await Page.EvaluateAsync<int>("pageY"));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for becoming hit target")]
    public async Task ShouldWaitForBecomingHitTarget()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.borderWidth = '0';
                button.style.width = '200px';
                button.style.height = '20px';
                document.body.style.margin = '0';
                document.body.style.position = 'relative';
                const flyOver = document.createElement('div');
                flyOver.className = 'flyover';
                flyOver.style.position = 'absolute';
                flyOver.style.width = '400px';
                flyOver.style.height = '20px';
                flyOver.style.left = '-200px';
                flyOver.style.top = '0';
                flyOver.style.background = 'red';
                document.body.appendChild(flyOver);
            }");

        var clickTask = Page.ClickAsync("button");
        Assert.False(clickTask.IsCompleted);

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '0'");
        await GiveItAChanceToClick(Page);
        Assert.False(clickTask.IsCompleted);

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '200px'");
        await clickTask;
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for becoming hit target with trial run")]
    public async Task ShouldWaitForBecomingHitTargetWithTrialRun()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvalOnSelectorAsync("button", @"button => {
                button.style.borderWidth = '0';
                button.style.width = '200px';
                button.style.height = '20px';
                document.body.style.margin = '0';
                document.body.style.position = 'relative';
                const flyOver = document.createElement('div');
                flyOver.className = 'flyover';
                flyOver.style.position = 'absolute';
                flyOver.style.width = '400px';
                flyOver.style.height = '20px';
                flyOver.style.left = '-200px';
                flyOver.style.top = '0';
                flyOver.style.background = 'red';
                document.body.appendChild(flyOver);
            }");

        var clickTask = Page.ClickAsync("button", new() { Trial = true });
        Assert.False(clickTask.IsCompleted);

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '0'");
        await GiveItAChanceToClick(Page);
        Assert.False(clickTask.IsCompleted);

        await Page.EvalOnSelectorAsync(".flyover", "flyOver => flyOver.style.left = '200px'");
        await clickTask;
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "trial run should work with short timeout")]
    public async Task TrialRunShouldWorkWithShortTimeout()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.QuerySelectorAsync("button");
        await Page.EvalOnSelectorAsync("button", @"button => button.disabled = true");
        var exception = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.ClickAsync("button", new() { Trial = true, Timeout = 500 }));
        StringAssert.Contains("click action (trial run)", exception.Message);
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "trial run should not click")]
    public async Task TrialRunShouldNotClick()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button", new() { Trial = true });
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "trial run should not double click")]
    public async Task TrialRunShouldNotDoubleClick()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => {
                window['double'] = false;
                const button = document.querySelector('button');
                button.addEventListener('dblclick', event => {
                    window['double'] = true;
                });
            }");
        await Page.DblClickAsync("button", new() { Trial = true });
        Assert.False(await Page.EvaluateAsync<bool>("double"));
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should fail when obscured and not waiting for hit target")]
    public async Task ShouldFailWhenObscuredAndNotWaitingForHitTarget()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvalOnSelectorAsync("button", @"button => {
                document.body.style.position = 'relative';
                const blocker = document.createElement('div');
                blocker.style.position = 'absolute';
                blocker.style.width = '400px';
                blocker.style.height = '20px';
                blocker.style.left = '0';
                blocker.style.top = '0';
                document.body.appendChild(blocker);
            }");

        await button.ClickAsync(new() { Force = true });
        Assert.AreEqual("Was not clicked", await Page.EvaluateAsync<string>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for button to be enabled")]
    public async Task ShouldWaitForButtonToBeEnabled()
    {
        await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\" disabled><span>Click target</span></button>");
        var clickTask = Page.ClickAsync("text=Click target");
        await GiveItAChanceToClick(Page);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        Assert.False(clickTask.IsCompleted);
        await Page.EvaluateAsync("() => document.querySelector('button').removeAttribute('disabled')");
        await clickTask;
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for input to be enabled")]
    public async Task ShouldWaitForInputToBeEnabled()
    {
        await Page.SetContentAsync("<input onclick=\"javascript: window.__CLICKED = true;\" disabled>");
        var clickTask = Page.ClickAsync("input");
        await GiveItAChanceToClick(Page);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        Assert.False(clickTask.IsCompleted);
        await Page.EvaluateAsync("() => document.querySelector('input').removeAttribute('disabled')");
        await clickTask;
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for select to be enabled")]
    public async Task ShouldWaitForSelectToBeEnabled()
    {
        await Page.SetContentAsync("<select onclick=\"javascript: window.__CLICKED = true;\" disabled><option selected>Hello</option></select>");
        var clickTask = Page.ClickAsync("select");
        await GiveItAChanceToClick(Page);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        Assert.False(clickTask.IsCompleted);
        await Page.EvaluateAsync("() => document.querySelector('select').removeAttribute('disabled')");
        await clickTask;
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click disabled div")]
    public async Task ShouldClickDisabledDiv()
    {
        await Page.SetContentAsync("<div onclick=\"javascript: window.__CLICKED = true;\" disabled>Click target</div>");
        await Page.ClickAsync("text=Click target");
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should climb dom for inner label with pointer-events:none")]
    public async Task ShouldClimbDomForInnerLabelWithPointerEventsNone()
    {
        await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\"><label style=\"pointer-events:none\">Click target</label></button>");
        await Page.ClickAsync("text=Click target");
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should climb up to [role=button]")]
    public async Task ShouldClimbUpToRoleButton()
    {
        await Page.SetContentAsync("<div role=button onclick=\"javascript: window.__CLICKED = true;\"><div style=\"pointer-events:none\"><span><div>Click target</div></span></div>");
        await Page.ClickAsync("text=Click target");
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for BUTTON to be clickable when it has pointer-events:none")]
    public async Task ShouldWaitForButtonToBeClickableWhenItHasPointerEventsNone()
    {
        await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\" style=\"pointer-events:none\"><span>Click target</span></button>");
        var clickTask = Page.ClickAsync("text=Click target");
        await GiveItAChanceToClick(Page);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        Assert.False(clickTask.IsCompleted);
        await Page.EvaluateAsync("() => document.querySelector('button').style.removeProperty('pointer-events')");
        await clickTask;
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should wait for LABEL to be clickable when it has pointer-events:none")]
    public async Task ShouldWaitForLabelToBeClickableWhenItHasPointerEventsNone()
    {
        await Page.SetContentAsync("<label onclick=\"javascript: window.__CLICKED = true;\" style=\"pointer-events:none\"><span>Click target</span></label>");
        var clickTask = Page.ClickAsync("text=Click target");

        for (int i = 0; i < 5; ++i)
        {
            Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        Assert.False(clickTask.IsCompleted);
        await Page.EvaluateAsync("() => document.querySelector('label').style.removeProperty('pointer-events')");
        await clickTask;
        Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
    }

    [PlaywrightTest("page-click.spec.ts", "should update modifiers correctly")]
    public async Task ShouldUpdateModifiersCorrectly()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.ClickAsync("button", new() { Modifiers = new[] { KeyboardModifier.Shift } });
        Assert.True(await Page.EvaluateAsync<bool>("shiftKey"));
        await Page.ClickAsync("button", new() { Modifiers = Array.Empty<KeyboardModifier>() });
        Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));

        await Page.Keyboard.DownAsync("Shift");

        await Page.ClickAsync("button", new() { Modifiers = Array.Empty<KeyboardModifier>() });
        Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));

        await Page.ClickAsync("button");
        Assert.True(await Page.EvaluateAsync<bool>("shiftKey"));

        await Page.Keyboard.UpAsync("Shift");

        await Page.ClickAsync("button");
        Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click an offscreen element when scroll-behavior is smooth")]
    public async Task ShouldClickAnOffscreenElementWhenScrollBehaviorIsSmooth()
    {
        await Page.SetContentAsync(@$"
            <div style=""border: 1px solid black; height: 500px; overflow: auto; width: 500px; scroll-behavior: smooth"">
                <button style=""margin-top: 2000px"" onClick=""window.clicked = true"" >hi</button>
            </div>");

        await Page.ClickAsync("button");
        Assert.True(await Page.EvaluateAsync<bool>("window.clicked"));
    }

    [PlaywrightTest("page-click.spec.ts", "should report nice error when element is detached and force-clicked")]
    public async Task ShouldReportNiceErrorWhenElementIsDetachedAndForceClicked()
    {
        await Page.GotoAsync(Server.Prefix + "/input/animating-button.html");
        await Page.EvaluateAsync("() => addButton()");
        var handle = await Page.QuerySelectorAsync("button");
        await Page.EvaluateAsync("() => stopButton(true)");
        var clickTask = handle.ClickAsync(new() { Force = true });
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => clickTask);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
        StringAssert.Contains("Element is not attached to the DOM", exception.Message);
    }

    [PlaywrightTest("page-click.spec.ts", "should fail when element detaches after animation")]
    public async Task ShouldFailWhenElementDetachesAfterAnimation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/animating-button.html");
        await Page.EvaluateAsync("() => addButton()");
        var handle = await Page.QuerySelectorAsync("button");
        var clickTask = handle.ClickAsync();
        await Page.EvaluateAsync("() => stopButton(true)");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => clickTask);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
        StringAssert.Contains("Element is not attached to the DOM", exception.Message);
    }

    [PlaywrightTest("page-click.spec.ts", "should retry when element detaches after animation")]
    public async Task ShouldRetryWhenElementDetachesAfterAnimation()
    {
        await Page.GotoAsync(Server.Prefix + "/input/animating-button.html");
        await Page.EvaluateAsync("() => addButton()");
        var clickTask = Page.ClickAsync("button");
        Assert.False(clickTask.IsCompleted);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
        await Page.EvaluateAsync("() => stopButton(true)");
        await Page.EvaluateAsync("() => addButton()");
        Assert.False(clickTask.IsCompleted);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
        await Page.EvaluateAsync("() => stopButton(true)");
        await Page.EvaluateAsync("() => addButton()");
        Assert.False(clickTask.IsCompleted);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
        await Page.EvaluateAsync("() => stopButton(false)");
        await clickTask;
        Assert.True(await Page.EvaluateAsync<bool?>("window.clicked"));
    }

    [PlaywrightTest("page-click.spec.ts", "should retry when element is animating from outside the viewport")]
    public async Task ShouldRetryWhenElementIsAnimatingFromOutsideTheViewport()
    {
        await Page.SetContentAsync($@"
                <style>
                  @keyframes move {{
                    from {{ left: -300px; }}
                    to {{ left: 0; }}
                  }}
                  button {{
                    position: absolute;
                    left: -300px;
                    top: 0;
                    bottom: 0;
                    width: 200px;
                  }}
                  button.animated {{
                    animation: 1s linear 1s move forwards;
                  }}
                  </style>
                  <div style=""position: relative; width: 300px; height: 300px;"">
                     <button onclick =""window.clicked=true""></button>
                  </div>");

        var handle = await Page.QuerySelectorAsync("button");
        var clickTask = handle.ClickAsync();
        await handle.EvaluateAsync("button => button.className = 'animated'");
        await clickTask;
        Assert.True(await Page.EvaluateAsync<bool?>("window.clicked"));
    }

    [PlaywrightTest("page-click.spec.ts", "should retry when element is animating from outside the viewport with force")]
    public async Task ShouldRetryWhenElementIsAnimatingFromOutsideTheViewportWithForce()
    {
        await Page.SetContentAsync($@"
                <style>
                  @keyframes move {{
                    from {{ left: -300px; }}
                    to {{ left: 0; }}
                  }}
                  button {{
                    position: absolute;
                    left: -300px;
                    top: 0;
                    bottom: 0;
                    width: 200px;
                  }}
                  button.animated {{
                    animation: 1s linear 1s move forwards;
                  }}
                  </style>
                  <div style=""position: relative; width: 300px; height: 300px;"">
                     <button onclick =""window.clicked=true""></button>
                  </div>");

        var handle = await Page.QuerySelectorAsync("button");
        var clickTask = handle.ClickAsync(new() { Force = true });
        await handle.EvaluateAsync("button => button.className = 'animated'");
        var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => clickTask);
        Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
        StringAssert.Contains("Element is outside of the viewport", exception.Message);
    }

    [PlaywrightTest("page-click.spec.ts", "should dispatch microtasks in order")]
    public async Task ShouldDispatchMicrotasksInOrder()
    {
        await Page.SetContentAsync($@"
                <button id=button>Click me</button>
                <script>
                let mutationCount = 0;
                const observer = new MutationObserver((mutationsList, observer) => {{
                    for(let mutation of mutationsList)
                    ++mutationCount;
                }});
                observer.observe(document.body, {{ attributes: true, childList: true, subtree: true }});
                button.addEventListener('mousedown', () => {{
                    mutationCount = 0;
                    document.body.appendChild(document.createElement('div'));
                }});
                button.addEventListener('mouseup', () => {{
                    window.result = mutationCount;
                }});
                </script>");

        await Page.ClickAsync("button");
        Assert.AreEqual(1, await Page.EvaluateAsync<int>("window.result"));
    }

    [PlaywrightTest("page-click.spec.ts", "should click the button when window.innerWidth is corrupted")]
    public async Task ShouldClickTheButtonWhenWindowInnerWidthIsCorrupted()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        await Page.EvaluateAsync(@"() => window.innerWith = 0");

        await Page.ClickAsync("button");
        Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("result"));
    }

    private async Task GiveItAChanceToClick(IPage page)
    {
        for (int i = 0; i < 5; i++)
        {
            await page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
        }
    }
}
