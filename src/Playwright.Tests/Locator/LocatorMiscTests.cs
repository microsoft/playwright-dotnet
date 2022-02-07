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
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests.Locator
{
    public class LocatorMiscTests : PageTestEx
    {
        [PlaywrightTest("locator-misc-1.spec.ts", "should hover")]
        public async Task ShouldHover()
        {
            await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
            var button = Page.Locator("#button-6");
            await button.HoverAsync();
            Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should hover when Node is removed")]
        public async Task ShouldHoverWhenNodeIsRemoved()
        {
            await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var button = Page.Locator("#button-6");
            await button.HoverAsync();
            Assert.AreEqual("button-6", await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should fill input")]
        public async Task ShouldFillInput()
        {
            await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
            var handle = Page.Locator("input");
            await Page.FillAsync("input", "some value");
            //await handle.FillAsync("some value");
            Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window['result']"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should fill input when Node is removed")]
        public async Task ShouldFillInputWhenNodeIsRemoved()
        {
            await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
            await Page.EvaluateAsync("() => delete window['Node']");
            var handle = Page.Locator("input");
            await handle.FillAsync("some value");
            Assert.AreEqual("some value", await Page.EvaluateAsync<string>("() => window['result']"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should check the box")]
        public async Task ShouldCheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            var input = Page.Locator("input");
            await input.CheckAsync();
            Assert.IsTrue(await Page.EvaluateAsync<bool>("checkbox.checked"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should uncheck the box")]
        public async Task ShouldUncheckTheBox()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
            var input = Page.Locator("input");
            await input.UncheckAsync();
            Assert.IsFalse(await Page.EvaluateAsync<bool>("checkbox.checked"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should check the box using setChecked")]
        public async Task ShouldCheckTheBoxUsingSetChecked()
        {
            await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
            var input = Page.Locator("input");
            await input.SetCheckedAsync(true);
            Assert.IsTrue(await Page.EvaluateAsync<bool>("checkbox.checked"));
            await input.SetCheckedAsync(false);
            Assert.IsFalse(await Page.EvaluateAsync<bool>("checkbox.checked"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should select single option")]
        public async Task ShouldSelectSingleOption()
        {
            await Page.GotoAsync(Server.Prefix + "/input/select.html");
            var select = Page.Locator("select");
            await select.SelectOptionAsync("blue");
            CollectionAssert.AreEqual(new string[] { "blue" }, await Page.EvaluateAsync<string[]>("() => window['result'].onInput"));
            CollectionAssert.AreEqual(new string[] { "blue" }, await Page.EvaluateAsync<string[]>("() => window['result'].onChange"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should focus a button")]
        public async Task ShouldFocusAButton()
        {
            await Page.GotoAsync(Server.Prefix + "/input/button.html");
            var button = Page.Locator("button");
            Assert.IsFalse(await button.EvaluateAsync<bool>("button => document.activeElement === button"));
            await button.FocusAsync();
            Assert.IsTrue(await button.EvaluateAsync<bool>("button => document.activeElement === button"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should dispatch click event via ElementHandles")]
        public async Task ShouldDispatchClickEventViaElementhandles()
        {
            await Page.GotoAsync(Server.Prefix + "/input/button.html");
            var button = Page.Locator("button");
            await button.DispatchEventAsync("click");
            Assert.AreEqual("Clicked", await Page.EvaluateAsync<string>("() => window['result']"));
        }

        [PlaywrightTest("locator-misc-1.spec.ts", "should upload the file")]
        public async Task ShouldUploadTheFile()
        {
            await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
            var input = Page.Locator("input[type=file]");
            await input.SetInputFilesAsync(TestUtils.GetAsset("file-to-upload.txt"));
            Assert.AreEqual("file-to-upload.txt", await Page.EvaluateAsync<string>("e => e.files[0].name", await input.ElementHandleAsync()));
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should press")]
        public async Task ShouldPress()
        {
            await Page.SetContentAsync("<input type='text' />");
            await Page.Locator("input").PressAsync("h");
            Assert.AreEqual("h", await Page.EvaluateAsync<string>("input => input.value", await Page.QuerySelectorAsync("input")));
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should scroll into view")]
        public async Task ShouldScrollIntoView()
        {
            await Page.GotoAsync(Server.Prefix + "/offscreenbuttons.html");

            for (int i = 0; i < 11; ++i)
            {
                var button = Page.Locator($"#btn{i}");
                var before = await button.EvaluateAsync<int>("button => { return button.getBoundingClientRect().right - window.innerWidth; }");
                Assert.AreEqual(10 * i, before);

                await button.ScrollIntoViewIfNeededAsync();

                var after = await button.EvaluateAsync<int>("button => { return button.getBoundingClientRect().right - window.innerWidth; }");
                Assert.IsTrue(after <= 0);
                await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
            }
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should select textarea")]
        public async Task ShouldSelectTextarea()
        {
            await Page.GotoAsync(Server.Prefix + "/input/textarea.html");

            var textarea = Page.Locator("textarea");
            await textarea.EvaluateAsync<string>("textarea => textarea.value = 'some value'");

            await textarea.SelectTextAsync();
            if (TestConstants.IsFirefox)
            {
                Assert.AreEqual(0, await textarea.EvaluateAsync<int>("el => el.selectionStart"));
                Assert.AreEqual(10, await textarea.EvaluateAsync<int>("el => el.selectionEnd"));
            }
            else
            {
                Assert.AreEqual("some value", await textarea.EvaluateAsync<string>("() => window.getSelection().toString()"));
            }
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should type")]
        public async Task ShouldType()
        {
            await Page.SetContentAsync("<input type='text' />");
            await Page.Locator("input").TypeAsync("hello");
            Assert.AreEqual("hello", await Page.EvaluateAsync<string>("input => input.value", await Page.QuerySelectorAsync("input")));
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should take screenshot")]
        [Ignore("We don't have the ability to match screenshots at the moment.")]
        public async Task ShouldTakeScreenshot()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");

            await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
            var element = Page.Locator(".box:nth-of-type(3)");
            var screenshot = await element.ScreenshotAsync();
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should return bounding box")]
        [Skip(SkipAttribute.Targets.Firefox)]
        public async Task ShouldReturnBoundingBox()
        {
            await Page.SetViewportSizeAsync(500, 500);
            await Page.GotoAsync(Server.Prefix + "/grid.html");

            var element = Page.Locator(".box:nth-of-type(13)");
            var box = await element.BoundingBoxAsync();

            Assert.AreEqual(100, box.X);
            Assert.AreEqual(50, box.Y);
            Assert.AreEqual(50, box.Width);
            Assert.AreEqual(50, box.Height);
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should waitFor")]
        public async Task ShouldWaitFor()
        {
            await Page.SetContentAsync("<div></div>");
            var locator = Page.Locator("span");
            var task = locator.WaitForAsync();
            await Page.EvalOnSelectorAsync("div", "div => div.innerHTML = '<span>target</span>'");
            await task;
            Assert.AreEqual("target", await locator.TextContentAsync());
        }

        [PlaywrightTest("locator-misc-2.spec.ts", "should waitFor hidden")]
        public async Task ShouldWaitForHidden()
        {
            await Page.SetContentAsync("<div><span></span></div>");
            var locator = Page.Locator("span");
            var task = locator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            await Page.EvalOnSelectorAsync("div", "div => div.innerHTML = ''");
            await task;
        }
    }
}
