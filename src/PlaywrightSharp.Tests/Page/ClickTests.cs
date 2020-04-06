using System.Collections.Generic;
using System.Drawing;
using System.Threading.Tasks;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Input;
using PlaywrightSharp.Tests.Attributes;
using PlaywrightSharp.Tests.BaseTests;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.Page
{
    ///<playwright-file>click.spec.js</playwright-file>
    ///<playwright-describe>Page.click</playwright-describe>
    [Trait("Category", "firefox")]
    [Collection(TestConstants.TestFixtureCollectionName)]
    public class ClickTests : PlaywrightSharpPageBaseTest
    {
        /// <inheritdoc/>
        public ClickTests(ITestOutputHelper output) : base(output)
        {
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button</playwright-it>
        [Fact]
        public async Task ShouldClickTheButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click svg</playwright-it>
        [Fact]
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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click svg</playwright-it>
        [Fact]
        public async Task ShouldClickTheButtonIfWindowNodeIsRemoved()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.EvaluateAsync("delete window.Node");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click on a span with an inline element inside</playwright-it>
        [Fact]
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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should not throw UnhandledPromiseRejection when page closes</playwright-it>
        [Fact]
        public async Task ShouldGracefullyFailWhenPageCloses()
        {
            var context = await NewContextAsync();
            var newPage = await context.NewPageAsync();
            await Assert.ThrowsAsync<TargetClosedException>(() => Task.WhenAll(
                newPage.CloseAsync(),
                newPage.Mouse.ClickAsync(1, 2)
             ));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button after navigation </playwright-it>
        [Fact]
        public async Task ShouldClickTheButtonAfterNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button after a cross origin navigation </playwright-it>
        [Fact]
        public async Task ShouldClickTheButtonAfterACrossOriginNavigation()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            await Page.GoToAsync(TestConstants.CrossProcessHttpPrefix + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click with disabled javascript</playwright-it>
        [Fact]
        public async Task ShouldClickWithDisabledJavascript()
        {
            var page = await NewPageAsync(new BrowserContextOptions { JavaScriptEnabled = false });
            await page.GoToAsync(TestConstants.ServerUrl + "/wrappedlink.html");
            await Task.WhenAll(
                page.ClickAsync("a"),
                page.WaitForNavigationAsync()
            );
            Assert.Equal(TestConstants.ServerUrl + "/wrappedlink.html#clicked", page.Url);
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click when one of inline box children is outside of viewport</playwright-it>
        [Fact]
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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should select the text by triple clicking</playwright-it>
        [Fact]
        public async Task ShouldSelectTheTextByTripleClicking()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/textarea.html");
            const string text = "This is the text that we are going to try to select. Let's see how it goes.";
            await Page.FillAsync("textarea", text);
            await Page.TripleClickAsync("textarea");
            Assert.Equal(text, await Page.EvaluateAsync<string>(@"() => {
                const textarea = document.querySelector('textarea');
                return textarea.value.substring(textarea.selectionStart, textarea.selectionEnd);
            }"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click offscreen buttons</playwright-it>
        [Fact]
        public async Task ShouldClickOffscreenButtons()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/offscreenbuttons.html");
            var messages = new List<string>();
            Page.Console += (sender, e) => messages.Add(e.Message.GetText());

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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should waitFor visible when already visible</playwright-it>
        [Fact]
        public async Task ShouldWaitForVisibleWhenAlreadyVisible()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should waitFor hidden when already hidden</playwright-it>
        [Fact]
        public async Task ShouldWaitForHiddenWhenAlreadyHidden()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.QuerySelectorEvaluateAsync("button", "b => b.style.display = 'none'");

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(()
                => Page.ClickAsync("button", new ClickOptions { WaitFor = WaitForOption.Hidden }));

            Assert.Equal("Node is either not visible or not an HTMLElement", exception.Message);
            Assert.Equal("Was not clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should waitFor hidden</playwright-it>
        [Fact]
        public async Task ShouldWaitForHidden()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var clickTask = Page.ClickAsync("button", new ClickOptions { WaitFor = WaitForOption.Hidden });

            for (int i = 0; i < 5; i++)
            {
                await Page.EvaluateAsync("1");
            }
            Assert.False(clickTask.IsCompleted);

            await Page.QuerySelectorEvaluateAsync("button", "b => b.style.display = 'none'");

            var exception = await Assert.ThrowsAsync<PlaywrightSharpException>(() => clickTask);

            Assert.Equal("Node is either not visible or not an HTMLElement", exception.Message);
            Assert.Equal("Was not clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should waitFor visible</playwright-it>
        [Fact]
        public async Task ShouldWaitForVisible()
        {
            bool done = false;
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.QuerySelectorEvaluateAsync("button", "b => b.style.display = 'none'");

            var clicked = Page.ClickAsync("button").ContinueWith(_ => done = true);

            for (int i = 0; i < 5; i++)
            {
                await Page.EvaluateAsync("1");
            }
            Assert.False(done);

            await Page.QuerySelectorEvaluateAsync("button", "b => b.style.display = 'block'");
            await clicked;
            Assert.True(done);
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click wrapped links</playwright-it>
        [Fact]
        public async Task ShouldClickWrappedLinks()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/wrappedlink.html");
            await Page.ClickAsync("a");
            Assert.True(await Page.EvaluateAsync<bool>("window.__clicked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click on checkbox input and toggle</playwright-it>
        [Fact]
        public async Task ShouldClickOnCheckboxInputAndToggle()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/checkbox.html");
            Assert.Null(await Page.EvaluateAsync("result.check"));
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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click on checkbox label and toggle</playwright-it>
        [Fact]
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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should fail to click a missing button</playwright-it>
        [Fact]
        public async Task ShouldFailToClickAMissingButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            var exception = await Assert.ThrowsAsync<SelectorException>(()
                => Page.ClickAsync("button.does-not-exist", new ClickOptions { WaitFor = WaitForOption.NoWait }));
            Assert.Equal("No node found for selector", exception.Message);
            Assert.Equal("button.does-not-exist", exception.Selector);
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should not hang with touch-enabled viewports</playwright-it>
        [Fact]
        public async Task ShouldNotHangWithTouchEnabledViewports()
        {
            await Page.SetViewportAsync(TestConstants.IPhone.ViewPort);
            await Page.Mouse.DownAsync();
            await Page.Mouse.MoveAsync(100, 10);
            await Page.Mouse.UpAsync();
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should scroll and click the button</playwright-it>
        [Fact]
        public async Task ShouldScrollAndClickTheButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            await Page.ClickAsync("#button-5");
            Assert.Equal("clicked", await Page.EvaluateAsync<string>("document.querySelector(\"#button-5\").textContent"));
            await Page.ClickAsync("#button-80");
            Assert.Equal("clicked", await Page.EvaluateAsync<string>("document.querySelector(\"#button-80\").textContent"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should double click the button</playwright-it>
        [Fact]
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
            await button.DoubleClickAsync();
            Assert.True(await Page.EvaluateAsync<bool>("double"));
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click a partially obscured button</playwright-it>
        [Fact]
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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click a rotated button</playwright-it>
        [Fact]
        public async Task ShouldClickARotatedButton()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/rotatedButton.html");
            await Page.ClickAsync("button");
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should fire contextmenu event on right click</playwright-it>
        [Fact]
        public async Task ShouldFireContextmenuEventOnRightClick()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/scrollable.html");
            await Page.ClickAsync("#button-8", new ClickOptions { Button = MouseButton.Right });
            Assert.Equal("context menu", await Page.EvaluateAsync<string>("document.querySelector('#button-8').textContent"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click links which cause navigation</playwright-it>
        [Fact]
        public async Task ShouldClickLinksWhichCauseNavigation()
        {
            await Page.SetContentAsync($"<a href=\"{TestConstants.EmptyPage}\">empty.html</a>");
            // This await should not hang.
            await Page.ClickAsync("a");
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button inside an iframe</playwright-it>
        [Fact]
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

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button with fixed position inside an iframe</playwright-it>
        [Fact(Skip = "See test in playwright")]
        public async Task ShouldClickTheButtonWithFixedPositionInsideAnIframe()
        {
            await Page.GoToAsync(TestConstants.EmptyPage);
            await Page.SetViewportAsync(new Viewport
            {
                Width = 500,
                Height = 500
            });
            await Page.SetContentAsync("<div style=\"width:100px;height:2000px\">spacer</div>");
            await FrameUtils.AttachFrameAsync(Page, "button-test", TestConstants.ServerUrl + "/input/button.html");
            var frame = Page.FirstChildFrame();
            await frame.QuerySelectorEvaluateAsync("button", "button => button.style.setProperty('position', 'fixed')");
            await frame.ClickAsync("button");
            Assert.Equal("Clicked", await frame.EvaluateAsync<string>("window.result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button with deviceScaleFactor set</playwright-it>
        [Fact]
        public async Task ShouldClickTheButtonWithDeviceScaleFactorSet()
        {
            await Page.SetViewportAsync(new Viewport { Width = 400, Height = 400, DeviceScaleFactor = 5 });
            Assert.Equal(5, await Page.EvaluateAsync<int>("window.devicePixelRatio"));
            await Page.SetContentAsync("<div style=\"width:100px;height:100px\">spacer</div>");
            await FrameUtils.AttachFrameAsync(Page, "button-test", TestConstants.ServerUrl + "/input/button.html");
            var frame = Page.FirstChildFrame();
            var button = await frame.QuerySelectorAsync("button");
            await button.ClickAsync();
            Assert.Equal("Clicked", await frame.EvaluateAsync<string>("window.result"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button with px border with relative point</playwright-it>
        [Fact]
        public async Task ShouldClickTheButtonWithPxBorderWithRelativePoint()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.QuerySelectorEvaluateAsync("button", "button => button.style.borderWidth = '8px'");
            await Page.ClickAsync("button", new ClickOptions { RelativePoint = new Point { X = 20, Y = 10 } });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(TestConstants.IsWebKit ? 20 + 8 : 20, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(TestConstants.IsWebKit ? 10 + 8 : 10, await Page.EvaluateAsync<int>("offsetY"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click the button with em border with relative point</playwright-it>
        [Fact]
        public async Task ShouldClickTheButtonWithEmBorderWithRelativePoint()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.QuerySelectorEvaluateAsync("button", "button => button.style.borderWidth = '2em'");
            await Page.QuerySelectorEvaluateAsync("button", "button => button.style.fontSize = '12px'");
            await Page.ClickAsync("button", new ClickOptions { RelativePoint = new Point { X = 20, Y = 10 } });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(TestConstants.IsWebKit ? 12 * 2 + 20 : 20, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(TestConstants.IsWebKit ? 12 * 2 + 20 : 10, await Page.EvaluateAsync<int>("offsetY"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click a very large button with relative point</playwright-it>
        [Fact]
        public async Task ShouldClickAVeryLargeButtonWithRelativePoint()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.QuerySelectorEvaluateAsync("button", "button => button.style.borderWidth = '8px'");
            await Page.QuerySelectorEvaluateAsync("button", "button => button.style.height = button.style.width = '2000px'");
            await Page.ClickAsync("button", new ClickOptions { RelativePoint = new Point { X = 1900, Y = 1910 } });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(TestConstants.IsWebKit ? 1900 + 8 : 1900, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(TestConstants.IsWebKit ? 1910 + 8 : 1910, await Page.EvaluateAsync<int>("offsetY"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click a button in scrolling container with relative point</playwright-it>
        [Fact(Skip = "Skipped in Playwright")]
        public async Task ShouldClickAButtonInScrollingContainerWithRelativePoint()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.QuerySelectorEvaluateAsync("button", @"button => {
                const container = document.createElement('div');
                container.style.overflow = 'auto';
                container.style.width = '200px';
                container.style.height = '200px';
                button.parentElement.insertBefore(container, button);
                container.appendChild(button);
                button.style.height = '2000px';
                button.style.width = '2000px';
            }");

            await Page.ClickAsync("button", new ClickOptions { RelativePoint = new Point { X = 1900, Y = 1910 } });
            Assert.Equal("Clicked", await Page.EvaluateAsync<string>("window.result"));
            // Safari reports border-relative offsetX/offsetY.
            Assert.Equal(1900, await Page.EvaluateAsync<int>("offsetX"));
            Assert.Equal(1910, await Page.EvaluateAsync<int>("offsetY"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should update modifiers correctly</playwright-it>
        [Fact]
        public async Task ShouldUpdateModifiersCorrectly()
        {
            await Page.GoToAsync(TestConstants.ServerUrl + "/input/button.html");
            await Page.ClickAsync("button", new ClickOptions { Modifiers = new[] { Modifier.Shift } });
            Assert.True(await Page.EvaluateAsync<bool>("shiftKey"));
            await Page.ClickAsync("button", new ClickOptions { Modifiers = new Modifier[] { } });
            Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));

            await Page.Keyboard.DownAsync("Shift");

            await Page.ClickAsync("button", new ClickOptions { Modifiers = new Modifier[] { } });
            Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));

            await Page.ClickAsync("button");
            Assert.True(await Page.EvaluateAsync<bool>("shiftKey"));

            await Page.Keyboard.UpAsync("Shift");

            await Page.ClickAsync("button");
            Assert.False(await Page.EvaluateAsync<bool>("shiftKey"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click an offscreen element when scroll-behavior is smooth</playwright-it>
        [SkipBrowserAndPlatformFact(skipChromium: true)]
        public async Task ShouldClickAnOffscreenElementWhenScrollBehaviorIsSmooth()
        {
            await Page.SetContentAsync(@$"
            <div style=""border: 1px solid black; height: 500px; overflow: auto; width: 500px; scroll-behavior: smooth"">
                <button style=""margin-top: 2000px"" onClick=""window.clicked = true"" >hi</button>
            </div>");

            await Page.ClickAsync("button");
            Assert.True(await Page.EvaluateAsync<bool>("window.clicked"));
        }

        ///<playwright-file>click.spec.js</playwright-file>
        ///<playwright-describe>Page.click</playwright-describe>
        ///<playwright-it>should click on an animated button</playwright-it>
        [Fact(Skip = "Skipped in Playwright")]
        public async Task ShouldClickOnAnAnimatedButton()
        {
            int buttonSize = 50;
            int containerWidth = 500;
            int transition = 500;
            await Page.SetContentAsync($@"
                <html>
                <body>
                <div style=""border: 1px solid black; height: 50px; overflow: auto; width: {containerWidth}px;"">
                <button id=""button"" style=""height: {buttonSize}px; width: {buttonSize}px; transition: left {transition}ms linear 0s; left: 0; position: relative"" onClick=""window.clicked++"">hi</ button>
                </div>
                </body>
                <script>
                var animateLeft = () => {{
                  var button = document.querySelector('#button');
                  document.querySelector('#button').style.left = button.style.left === '0px' ? '{containerWidth - buttonSize}px' : '0px';
                }};
                window.clicked = 0;
                window.setTimeout(animateLeft, 0);
                window.setInterval(animateLeft, {transition});
                </script>
                </html>");
            await Page.ClickAsync("button");
            Assert.Equal(1, await Page.EvaluateAsync<int>("window.clicked"));
            Assert.Equal($"{containerWidth - buttonSize}px", await Page.EvaluateAsync<string>("document.querySelector('#button').style.left"));
            await Task.Delay(500);
            await Page.ClickAsync("button");
            Assert.Equal(2, await Page.EvaluateAsync<int>("window.clicked"));
            Assert.Equal("0px", await Page.EvaluateAsync<string>("document.querySelector('#button').style.left"));
        }
    }
}
