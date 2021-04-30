using System;
using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Testing.Xunit;
using Microsoft.Playwright.Tests.Attributes;
using Microsoft.Playwright.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests
{
    [Collection(TestConstants.TestFixtureBrowserCollectionName)]
    public class PageClickTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public PageClickTests(ITestOutputHelper output) : base(output)
        {
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click svg")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickSvg()
        {
            await Page.SetContentAsync($@"
                <svg height=""100"" width=""100"">
                  <circle onclick=""javascript:window.__CLICKED=42"" cx=""50"" cy=""50"" r=""40"" stroke=""black"" stroke-width=""3"" fill=""red""/>
                </svg>
            ");
            await Page.ClickAsync("circle");
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click svg")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonIfWindowNodeIsRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync("delete window.Node");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click on a span with an inline element inside")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => window.CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should not throw UnhandledPromiseRejection when page closes")]
        [Fact(Skip = "We don't need to test this race condition")]
        public void ShouldGracefullyFailWhenPageCloses()
        {
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button after navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonAfterNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button after a cross origin navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click with disabled javascript")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickWithDisabledJavascript()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions { JavaScriptEnabled = false });
            var page = await context.NewPageAsync();
            await page.GoToAsync(TestConstants.ServerUrl + "/wrappedlink.html");
            await TaskUtils.WhenAll(
                page.ClickAsync("a"),
                page.WaitForNavigationAsync()
            );
            Assert.Equal(TestConstants.ServerUrl + "/wrappedlink.html#clicked", page.Url);
        }

        [PlaywrightTest("page-click.spec.ts", "should click when one of inline box children is outside of viewport")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal(42, await Page.EvaluateAsync<int>("() => window.CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should select the text by triple clicking")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldSelectTheTextByTripleClicking()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            const string text = "This is the text that we are going to try to select. Let's see how it goes.";
            await Page.FillAsync("textarea", text);
            await Page.ClickAsync("textarea", clickCount: 3);
            Assert.Equal(text, await Page.EvaluateAsync<string>(@"() => {
                const textarea = document.querySelector('textarea');
                return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
            }"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click offscreen buttons")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickOffscreenButtons()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/offscreenbuttons.html");
            var messages = new List<string>();
            Page.Console += (_, e) => messages.Add(e.Text);

            for (int i = 0; i < 11; ++i)
            {
                // We might have scrolled to click a button - reset to (0, 0).
                await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
                await Page.ClickAsync($"#btn{i}");
            }
            Assert.Equal(new List<string>
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisibleWhenAlreadyVisible()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should not wait with force")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotWaitWithForce()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.ClickAsync("button", force: true));

            Assert.Contains("Element is not visible", exception.Message);
            Assert.Equal("Was not clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should waitFor display:none to be gone")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForDisplayNoneToBeGone()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'none'");
            var clickTask = Page.ClickAsync("button", timeout: 0);

            await GiveItAChanceToClick(Page);

            Assert.False(clickTask.IsCompleted);
            Assert.Equal("Was not clicked", await Page.EvaluateAsync<string>("result"));

            await Page.EvalOnSelectorAsync("button", "b => b.style.display = 'block'");
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);

            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should waitFor visibility:hidden to be gone")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisibilityhiddenToBeGone()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'hidden'");
            var clickTask = Page.ClickAsync("button", timeout: 0);

            await GiveItAChanceToClick(Page);

            Assert.False(clickTask.IsCompleted);
            Assert.Equal("Was not clicked", await Page.EvaluateAsync<string>("result"));

            await Page.EvalOnSelectorAsync("button", "b => b.style.visibility = 'visible'");
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);

            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should waitFor visible when parent is hidden")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForVisibleWhenParentIsHidden()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'none'");
            var clickTask = Page.ClickAsync("button", timeout: 0);

            await GiveItAChanceToClick(Page);

            Assert.False(clickTask.IsCompleted);
            Assert.Equal("Was not clicked", await Page.EvaluateAsync<string>("result"));

            await Page.EvalOnSelectorAsync("button", "b => b.parentElement.style.display = 'block'");
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);

            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click wrapped links")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickWrappedLinks()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/wrappedlink.html");
            await Page.ClickAsync("a");
            Assert.True(await Page.EvaluateAsync<bool>("window.__clicked"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click on checkbox input and toggle")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickOnCheckboxInputAndToggle()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/checkbox.html");
            Assert.Null(await Page.EvaluateAsync<bool?>("result.check"));
            await Page.ClickAsync("input#agree");
            Assert.True(await Page.EvaluateAsync<bool>("result.check"));
            Assert.Equal(new[] {
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickOnCheckboxLabelAndToggle()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/checkbox.html");
            Assert.Null(await Page.EvaluateAsync("result.check"));
            await Page.ClickAsync("label[for=\"agree\"]");
            Assert.True(await Page.EvaluateAsync<bool>("result.check"));
            Assert.Equal(new[] {
                "click",
                "input",
                "change"
            }, await Page.EvaluateAsync<string[]>("result.events"));
            await Page.ClickAsync("label[for=\"agree\"]");
            Assert.False(await Page.EvaluateAsync<bool>("result.check"));
        }

        [PlaywrightTest("page-click.spec.ts", "should not hang with touch-enabled viewports")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldNotHangWithTouchEnabledViewports()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = TestConstants.iPhone6.Viewport,
                HasTouch = TestConstants.iPhone6.HasTouch,
            });

            var page = await context.NewPageAsync();
            await page.Mouse.DownAsync();
            await page.Mouse.MoveAsync(100, 10);
            await page.Mouse.UpAsync();
        }

        [PlaywrightTest("page-click.spec.ts", "should scroll and click the button")]
        [Fact(Skip = "Flacky")]
        public async Task ShouldScrollAndClickTheButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            await Page.ClickAsync("#button-5");
            Assert.Equal("clicked", await Page.EvaluateAsync<string>("document.querySelector(\"#button-5\").textContent"));
            await Page.ClickAsync("#button-80");
            Assert.Equal("clicked", await Page.EvaluateAsync<string>("document.querySelector(\"#button-80\").textContent"));
        }

        [PlaywrightTest("page-click.spec.ts", "should double click the button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldDoubleClickTheButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
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
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click a partially obscured button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickAPartiallyObscuredButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync(@"{
                const button = document.querySelector('button');
                button.textContent = 'Some really long text that will go offscreen';

                button.style.position = 'absolute';
                button.style.left = '368px';
            }");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click a rotated button")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickARotatedButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/rotatedButton.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should fire contextmenu event on right click")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFireContextmenuEventOnRightClick()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            await Page.ClickAsync("#button-8", button: MouseButton.Right);
            Assert.Equal("context menu", await Page.EvaluateAsync<string>("document.querySelector('#button-8').textContent"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click links which cause navigation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickLinksWhichCauseNavigation()
        {
            await Page.SetContentAsync($"<a href=\"{TestConstants.EmptyPage}\">empty.html</a>");
            // This await should not hang.
            await Page.ClickAsync("a");
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button inside an iframe")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonInsideAnIframe()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
            await FrameUtils.AttachFrameAsync(Page, "button-test", TestConstants.ServerUrl + "/input/button.html");
            var frame = Page.FirstChildFrame();
            var button = await frame.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.Equal("Clicked", await frame.EvaluateAsync<string>("window.result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button with fixed position inside an iframe")]
        [SkipBrowserAndPlatformFact(skipChromium: true, skipWebkit: true)]
        public async Task ShouldClickTheButtonWithFixedPositionInsideAnIframe()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetViewportSizeAsync(500, 500);
            await Page.SetContentAsync("<div style=\"width:100px;height:2000px\">spacer</div>");
            await FrameUtils.AttachFrameAsync(Page, "button-test", TestConstants.ServerUrl + "/input/button.html");
            var frame = Page.FirstChildFrame();
            await frame.EvalOnSelectorAsync("button", "button => button.style.setProperty('position', 'fixed')");
            await frame.ClickAsync("button");
            Assert.Equal("Clicked", await frame.EvaluateAsync<string>("window.result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button with deviceScaleFactor set")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonWithDeviceScaleFactorSet()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 400,
                    Height = 400,
                },
                DeviceScaleFactor = 5,
            });

            var page = await context.NewPageAsync();
            Assert.Equal(5, await page.EvaluateAsync<int>("window.devicePixelRatio"));
            await page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
            await FrameUtils.AttachFrameAsync(page, "button-test", TestConstants.ServerUrl + "/input/button.html");
            var frame = page.FirstChildFrame();
            var button = await frame.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.Equal("Clicked", await frame.EvaluateAsync<string>("window.result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button with px border with relative point")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonWithPxBorderWithRelativePoint()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
            await Page.ClickAsync("button", position: new Position { X = 20, Y = 10 });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(TestConstants.IsWebKit ? 20 + 8 : 20, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(TestConstants.IsWebKit ? 10 + 8 : 10, await Page.EvaluateAsync<int>("offsetY"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button with em border with offset")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonWithEmBorderWithOffset()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '2em'");
            await Page.EvalOnSelectorAsync("button", "button => button.style.fontSize = '12px'");
            await Page.ClickAsync("button", position: new Position { X = 20, Y = 10 });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(TestConstants.IsWebKit ? 12 * 2 + 20 : 20, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(TestConstants.IsWebKit ? 12 * 2 + 10 : 10, await Page.EvaluateAsync<int>("offsetY"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click a very large button with offset")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickAVeryLargeButtonWithOffset()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvalOnSelectorAsync("button", "button => button.style.borderWidth = '8px'");
            await Page.EvalOnSelectorAsync("button", "button => button.style.height = button.style.width = '2000px'");
            await Page.ClickAsync("button", position: new Position { X = 1900, Y = 1910 });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(TestConstants.IsWebKit ? 1900 + 8 : 1900, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(TestConstants.IsWebKit ? 1910 + 8 : 1910, await Page.EvaluateAsync<int>("offsetY"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click a button in scrolling container with offset")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickAButtonInScrollingContainerWithOffset()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
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

            await Page.ClickAsync("button", position: new Position { X = 1900, Y = 1910 });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(TestConstants.IsWebKit ? 1900 + 8 : 1900, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(TestConstants.IsWebKit ? 1910 + 8 : 1910, await Page.EvaluateAsync<int>("offsetY"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button with offset with page scale")]
        [SkipBrowserAndPlatformFact(skipFirefox: true)]
        public async Task ShouldClickTheButtonWithOffsetWithPageScale()
        {
            await using var context = await Browser.NewContextAsync(new BrowserContextOptions
            {
                Viewport = new ViewportSize
                {
                    Width = 400,
                    Height = 400,
                },
                IsMobile = true,
            });

            var page = await context.NewPageAsync();

            await page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await page.EvalOnSelectorAsync("button", @"button => {
                button.style.borderWidth = '8px';
                document.body.style.margin = '0';
            }");

            await page.ClickAsync("button", position: new Position { X = 20, Y = 10 });
            Assert.Equal("Clicked", await page.EvaluateAsync<string>("window.result"));

            var point = TestConstants.Product switch
            {
                TestConstants.ChromiumProduct => new Point(27, 18),
                TestConstants.WebkitProduct => new Point(29, 19),
                _ => new Point(28, 18),
            };

            Assert.Equal(point.X, Convert.ToInt32(await page.EvaluateAsync<decimal>("pageX")));
            Assert.Equal(point.Y, Convert.ToInt32(await page.EvaluateAsync<decimal>("pageY")));
        }

        [PlaywrightTest("page-click.spec.ts", "should wait for stable position")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForStablePosition()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
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
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            Assert.Equal(300, await Page.EvaluateAsync<int>("pageX"));
            Assert.Equal(10, await Page.EvaluateAsync<int>("pageY"));
        }

        [PlaywrightTest("page-click.spec.ts", "should wait for becoming hit target")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForBecomingHitTarget()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
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
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should fail when obscured and not waiting for hit target")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenObscuredAndNotWaitingForHitTarget()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
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

            await button.ClickAsync(force: true);
            Assert.Equal("Was not clicked", await Page.EvaluateAsync<string>("window.result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should wait for button to be enabled")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForButtonToBeEnabled()
        {
            await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\" disabled><span>Click target</span></button>");
            var clickTask = Page.ClickAsync("text=Click target");
            await GiveItAChanceToClick(Page);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
            Assert.False(clickTask.IsCompleted);
            await Page.EvaluateAsync("() => document.querySelector('button').removeAttribute('disabled')");
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should wait for input to be enabled")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForInputToBeEnabled()
        {
            await Page.SetContentAsync("<input onclick=\"javascript: window.__CLICKED = true;\" disabled>");
            var clickTask = Page.ClickAsync("input");
            await GiveItAChanceToClick(Page);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
            Assert.False(clickTask.IsCompleted);
            await Page.EvaluateAsync("() => document.querySelector('input').removeAttribute('disabled')");
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should wait for select to be enabled")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForSelectToBeEnabled()
        {
            await Page.SetContentAsync("<select onclick=\"javascript: window.__CLICKED = true;\" disabled><option selected>Hello</option></select>");
            var clickTask = Page.ClickAsync("select");
            await GiveItAChanceToClick(Page);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
            Assert.False(clickTask.IsCompleted);
            await Page.EvaluateAsync("() => document.querySelector('select').removeAttribute('disabled')");
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click disabled div")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickDisabledDiv()
        {
            await Page.SetContentAsync("<div onclick=\"javascript: window.__CLICKED = true;\" disabled>Click target</div>");
            await Page.ClickAsync("text=Click target");
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should climb dom for inner label with pointer-events:none")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClimbDomForInnerLabelWithPointerEventsNone()
        {
            await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\"><label style=\"pointer-events:none\">Click target</label></button>");
            await Page.ClickAsync("text=Click target");
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should climb up to [role=button]")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClimbUpToRoleButton()
        {
            await Page.SetContentAsync("<div role=button onclick=\"javascript: window.__CLICKED = true;\"><div style=\"pointer-events:none\"><span><div>Click target</div></span></div>");
            await Page.ClickAsync("text=Click target");
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should wait for BUTTON to be clickable when it has pointer-events:none")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldWaitForButtonToBeClickableWhenItHasPointerEventsNone()
        {
            await Page.SetContentAsync("<button onclick=\"javascript: window.__CLICKED = true;\" style=\"pointer-events:none\"><span>Click target</span></button>");
            var clickTask = Page.ClickAsync("text=Click target");
            await GiveItAChanceToClick(Page);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
            Assert.False(clickTask.IsCompleted);
            await Page.EvaluateAsync("() => document.querySelector('button').style.removeProperty('pointer-events')");
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should wait for LABEL to be clickable when it has pointer-events:none")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            await clickTask.WithTimeout(TestConstants.DefaultTaskTimeout);
            Assert.True(await Page.EvaluateAsync<bool?>("window.__CLICKED"));
        }

        [PlaywrightTest("page-click.spec.ts", "should update modifiers correctly")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldUpdateModifiersCorrectly()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button", modifiers: new[] { KeyboardModifier.Shift });
            Assert.True(await Page.EvaluateAsync<bool>("shiftKey"));
            await Page.ClickAsync("button", modifiers: Array.Empty<KeyboardModifier>());
            Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));

            await Page.Keyboard.DownAsync("Shift");

            await Page.ClickAsync("button", modifiers: Array.Empty<KeyboardModifier>());
            Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));

            await Page.ClickAsync("button");
            Assert.True(await Page.EvaluateAsync<bool>("shiftKey"));

            await Page.Keyboard.UpAsync("Shift");

            await Page.ClickAsync("button");
            Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click an offscreen element when scroll-behavior is smooth")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldReportNiceErrorWhenElementIsDetachedAndForceClicked()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/animating-button.html");
            await Page.EvaluateAsync("() => addButton()");
            var handle = await Page.QuerySelectorAsync("button");
            await Page.EvaluateAsync("() => stopButton(true)");
            var clickTask = handle.ClickAsync(force: true);
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => clickTask);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("page-click.spec.ts", "should fail when element detaches after animation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldFailWhenElementDetachesAfterAnimation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/animating-button.html");
            await Page.EvaluateAsync("() => addButton()");
            var handle = await Page.QuerySelectorAsync("button");
            var clickTask = handle.ClickAsync();
            await Page.EvaluateAsync("() => stopButton(true)");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => clickTask);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
            Assert.Contains("Element is not attached to the DOM", exception.Message);
        }

        [PlaywrightTest("page-click.spec.ts", "should retry when element detaches after animation")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldRetryWhenElementDetachesAfterAnimation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/animating-button.html");
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            var clickTask = handle.ClickAsync(force: true);
            await handle.EvaluateAsync("button => button.className = 'animated'");
            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => clickTask);
            Assert.Null(await Page.EvaluateAsync<bool?>("window.clicked"));
            Assert.Contains("Element is outside of the viewport", exception.Message);
        }

        [PlaywrightTest("page-click.spec.ts", "should dispatch microtasks in order")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
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
            Assert.Equal(1, await Page.EvaluateAsync<int>("window.result"));
        }

        [PlaywrightTest("page-click.spec.ts", "should click the button when window.innerWidth is corrupted")]
        [Fact(Timeout = TestConstants.DefaultTestTimeout)]
        public async Task ShouldClickTheButtonWhenWindowInnerWidthIsCorrupted()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync(@"() => window.innerWith = 0");

            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        private async Task GiveItAChanceToClick(IPage page)
        {
            for (int i = 0; i < 5; i++)
            {
                await page.EvaluateAsync("() => new Promise(f => requestAnimationFrame(() => requestAnimationFrame(f)))");
            }
        }
    }
}
