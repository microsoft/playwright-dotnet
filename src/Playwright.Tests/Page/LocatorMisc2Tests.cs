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

/// <summary>Port of tests/page/locator-misc-2.spec.ts.</summary>
public class LocatorMisc2Tests : UpstreamPageTest
{
    [UpstreamTest("LO-140 should press @smoke")]
    public async Task ShouldPress()
    {
        await Page.SetContentAsync("<input type='text' />");
        await Page.Locator("input").PressAsync("h");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("h"));
    }

    [UpstreamTest("LO-141 should scroll into view")]
    public async Task ShouldScrollIntoView()
    {
        Info.Fixme(IsAndroid);

        await Page.GotoAsync(Server.Prefix + "/offscreenbuttons.html");
        for (var i = 0; i < 11; ++i)
        {
            var button = Page.Locator("#btn" + i);
            var before = await button.EvaluateAsync<int>(@"button => {
      return button.getBoundingClientRect().right - window.innerWidth;
    }");
            Assert.That(before, Is.EqualTo(10 * i));
            await button.ScrollIntoViewIfNeededAsync();
            var after = await button.EvaluateAsync<double>(@"button => {
      return button.getBoundingClientRect().right - window.innerWidth;
    }");
            Assert.That(after <= 0, Is.EqualTo(true));
            await Page.EvaluateAsync("() => window.scrollTo(0, 0)");
        }
    }

    [UpstreamTest("LO-142 should scroll zero-sized element into view")]
    public async Task ShouldScrollZeroSizedElementIntoView()
    {
        Info.Fixme(IsAndroid || IsElectron);

        await Page.SetContentAsync(@"
    <style>
      html,body { margin: 0; padding: 0; }
      ::-webkit-scrollbar { display: none; }
      * { scrollbar-width: none; }
    </style>
    <div style=""height: 2000px; text-align: center; border: 10px solid blue;"">
      <h1>SCROLL DOWN</h1>
    </div>
    <div id=lazyload style=""font-size:75px; background-color: green;""></div>
    <script>
      const lazyLoadElement = document.querySelector('#lazyload');
      const observer = new IntersectionObserver((entries) => {
        if (entries.some(entry => entry.isIntersecting)) {
          lazyLoadElement.textContent = 'LAZY LOADED CONTENT';
          lazyLoadElement.style.height = '20px';
          observer.disconnect();
        }
      });
      observer.observe(lazyLoadElement);
    </script>
  ");
        ExpectToEqual(await Page.Locator("#lazyload").BoundingBoxAsync(), new { x = 0, y = 2020, width = 1280, height = 0 });
        await Page.Locator("#lazyload").ScrollIntoViewIfNeededAsync();
        await Expect(Page.Locator("#lazyload")).ToHaveTextAsync("LAZY LOADED CONTENT");
        ExpectToEqual(await Page.Locator("#lazyload").BoundingBoxAsync(), new { x = 0, y = 720, width = 1280, height = 20 });
    }

    [UpstreamTest("LO-143 should select textarea")]
    public async Task ShouldSelectTextarea()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var textarea = Page.Locator("textarea");
        await textarea.EvaluateAsync("textarea => textarea.value = 'some value'");
        await textarea.SelectTextAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => window.getSelection().toString()"), Is.EqualTo("some value"));
    }

    [UpstreamTest("LO-144 should type")]
    public async Task ShouldType()
    {
        await Page.SetContentAsync("<input type='text' />");
        await Page.Locator("input").TypeAsync("hello");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("hello"));
    }

    [UpstreamTest("LO-145 should pressSequentially")]
    public async Task ShouldPressSequentially()
    {
        await Page.SetContentAsync("<input type='text' />");
        await Page.Locator("input").PressSequentiallyAsync("hello");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("hello"));
    }

    [UpstreamTest("LO-146 should take screenshot")]
    public async Task ShouldTakeScreenshot()
    {
        Info.Skip(BrowserName == "firefox" && !Headless);
        Info.Skip(IsAndroid, "Different dpr. Remove after using 1x scale for screenshots.");
        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        await Page.EvaluateAsync("() => window.scrollBy(50, 100)");
        var element = Page.Locator(".box:nth-of-type(3)");
        var screenshot = await element.ScreenshotAsync();
        ExpectToMatchSnapshot(screenshot, "screenshot-element-bounding-box.png");
    }

    [UpstreamTest("LO-147 should return bounding box")]
    public async Task ShouldReturnBoundingBox()
    {
        Info.Skip(IsAndroid);

        await Page.SetViewportSizeAsync(500, 500);
        await Page.GotoAsync(Server.Prefix + "/grid.html");
        var element = Page.Locator(".box:nth-of-type(13)");
        var box = await element.BoundingBoxAsync();
        ExpectToEqual(box, new { x = 100, y = 50, width = 50, height = 50 });
    }

    [UpstreamTest("LO-148 should waitFor")]
    public async Task ShouldWaitFor()
    {
        await Page.SetContentAsync("<div></div>");
        var locator = Page.Locator("span");
        var promise = locator.WaitForAsync();
        await Page.EvalOnSelectorAsync("div", "div => div.innerHTML = '<span>target</span>'");
        await promise;
        await Expect(locator).ToHaveTextAsync("target");
    }

    [UpstreamTest("LO-149 should waitFor hidden")]
    public async Task ShouldWaitForHidden()
    {
        await Page.SetContentAsync("<div><span>target</span></div>");
        var locator = Page.Locator("span");
        var promise = locator.WaitForAsync(new() { State = WaitForSelectorState.Hidden });
        await Page.EvalOnSelectorAsync("div", "div => div.innerHTML = ''");
        await promise;
    }

    [UpstreamTest("LO-150 should combine visible with other selectors")]
    public async Task ShouldCombineVisibleWithOtherSelectors()
    {
        await Page.SetContentAsync(@"<div>
  <div class=""item"" style=""display: none"">Hidden data0</div>
  <div class=""item"">visible data1</div>
  <div class=""item"" style=""display: none"">Hidden data1</div>
  <div class=""item"">visible data2</div>
  <div class=""item"" style=""display: none"">Hidden data1</div>
  <div class=""item"">visible data3</div>
  </div>");
        var locator = Page.Locator(".item >> visible=true").Nth(1);
        await Expect(locator).ToHaveTextAsync("visible data2");
        await Expect(Page.Locator(".item >> visible=true >> text=data3")).ToHaveTextAsync("visible data3");
    }

    [UpstreamTest("LO-151 should support filter(visible)")]
    public async Task ShouldSupportFilterVisible()
    {
        await Page.SetContentAsync(@"<div>
    <div class=""item"" style=""display: none"">Hidden data0</div>
    <div class=""item"">visible data1</div>
    <div class=""item"" style=""display: none"">Hidden data1</div>
    <div class=""item"">visible data2</div>
    <div class=""item"" style=""display: none"">Hidden data2</div>
    <div class=""item"">visible data3</div>
    </div>
  ");
        var locator = Page.Locator(".item").Filter(new() { Visible = true }).Nth(1);
        await Expect(locator).ToHaveTextAsync("visible data2");
        await Expect(Page.Locator(".item").Filter(new() { Visible = true }).GetByText("data3")).ToHaveTextAsync("visible data3");
        await Expect(Page.Locator(".item").Filter(new() { Visible = false }).GetByText("data1")).ToHaveTextAsync("Hidden data1");
    }

    [UpstreamTest("LO-152 should support visible()")]
    public async Task ShouldSupportVisible()
    {
        await Page.SetContentAsync(@"<div>
    <div class=""item"" style=""display: none"">Hidden data0</div>
    <div class=""item"">visible data1</div>
    <div class=""item"" style=""display: none"">Hidden data1</div>
    <div class=""item"">visible data2</div>
    <div class=""item"" style=""display: none"">Hidden data2</div>
    <div class=""item"">visible data3</div>
    </div>
  ");
        var locator = Page.Locator(".item").Visible.Nth(1);
        await Expect(locator).ToHaveTextAsync("visible data2");
        await Expect(Page.Locator(".item").Visible.GetByText("data3")).ToHaveTextAsync("visible data3");
        await Expect(Page.Locator(".item").Visible).ToHaveCountAsync(3);
    }

    [UpstreamTest("LO-153 locator.count should work with deleted Map in main world")]
    public async Task LocatorCountShouldWorkWithDeletedMapInMainWorld()
    {
        await Page.EvaluateAsync("Map = 1");
        await Page.Locator("#searchResultTableDiv .x-grid3-row").CountAsync();
        await Expect(Page.Locator("#searchResultTableDiv .x-grid3-row")).ToHaveCountAsync(0);
    }

    [UpstreamTest("LO-154 Locator.locator() and FrameLocator.locator() should accept locator")]
    public async Task LocatorLocatorAndFrameLocatorLocatorShouldAcceptLocator()
    {
        await Page.SetContentAsync(@"
    <div><input value=outer></div>
    <iframe srcdoc=""<div><input value=inner></div>""></iframe>
  ");

        var inputLocator = Page.Locator("input");
        Assert.That(await inputLocator.InputValueAsync(), Is.EqualTo("outer"));
        Assert.That(await Page.Locator("div").Locator(inputLocator).InputValueAsync(), Is.EqualTo("outer"));
        Assert.That(await Page.FrameLocator("iframe").Locator(inputLocator).InputValueAsync(), Is.EqualTo("inner"));
        Assert.That(await Page.FrameLocator("iframe").Locator("div").Locator(inputLocator).InputValueAsync(), Is.EqualTo("inner"));

        var divLocator = Page.Locator("div");
        Assert.That(await divLocator.Locator("input").InputValueAsync(), Is.EqualTo("outer"));
        Assert.That(await Page.FrameLocator("iframe").Locator(divLocator).Locator("input").InputValueAsync(), Is.EqualTo("inner"));
    }

    [UpstreamTest("LO-155 should fill programmatically enabled textarea")]
    public async Task ShouldFillProgrammaticallyEnabledTextarea()
    {
        await Page.SetContentAsync(@"
    <button>Enable</button>
    <form>
      <textarea id=""text"" disabled></textarea>
    </form>
    <script>
      document.querySelector('button').addEventListener('click', () => {
        document.querySelector('#text').disabled = false;
      });
    </script>
  ");
        await Page.Locator("button").ClickAsync();
        await Page.Locator("#text").FillAsync("Hello");
        await Expect(Page.Locator("#text")).ToHaveValueAsync("Hello");
    }

    [UpstreamTest("LO-156 press should throw on unknown keys")]
    public async Task PressShouldThrowOnUnknownKeys()
    {
        await Page.SetContentAsync("<input type='text' value='hello' />");
        var locator = Page.GetByRole(AriaRole.Textbox);
        var error = await ThrowsAsync<PlaywrightException>(() => locator.PressAsync("NotARealKey"));
        Assert.That(error.Message, Does.Match("Unknown key: \"NotARealKey\""));
        var error2 = await ThrowsAsync<PlaywrightException>(() => locator.PressAsync("ё"));
        Assert.That(error2.Message, Does.Match("Unknown key: \"ё\""));
        var error3 = await ThrowsAsync<PlaywrightException>(() => locator.PressAsync("😊"));
        Assert.That(error3.Message, Does.Match("Unknown key: \"😊\""));
    }
}
