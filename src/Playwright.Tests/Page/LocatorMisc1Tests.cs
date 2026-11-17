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
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-misc-1.spec.ts.</summary>
public class LocatorMisc1Tests : UpstreamPageTest
{
    [UpstreamTest("LO-127 should hover @smoke")]
    public async Task ShouldHover()
    {
        Info.Skip(!Headless, "headed messes up with hover");

        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        var button = Page.Locator("#button-6");
        await button.HoverAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("LO-128 should hover when Node is removed")]
    public async Task ShouldHoverWhenNodeIsRemoved()
    {
        Info.Skip(!Headless, "headed messes up with hover");

        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var button = Page.Locator("#button-6");
        await button.HoverAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("LO-129 should fill input")]
    public async Task ShouldFillInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var handle = Page.Locator("input");
        await handle.FillAsync("some value");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("LO-130 should fill input when Node is removed")]
    public async Task ShouldFillInputWhenNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var handle = Page.Locator("input");
        await handle.FillAsync("some value");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("LO-131 should clear input")]
    public async Task ShouldClearInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var handle = Page.Locator("input");
        await handle.FillAsync("some value");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
        await handle.ClearAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo(""));
    }

    [UpstreamTest("LO-132 should check the box")]
    public async Task ShouldCheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = Page.Locator("input");
        await input.CheckAsync();
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(true));
    }

    [UpstreamTest("LO-133 should check the box using setChecked")]
    public async Task ShouldCheckTheBoxUsingSetChecked()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = Page.Locator("input");
        await input.SetCheckedAsync(true);
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(true));
        await input.SetCheckedAsync(false);
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(false));
    }

    [UpstreamTest("LO-134 should uncheck the box")]
    public async Task ShouldUncheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        var input = Page.Locator("input");
        await input.UncheckAsync();
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(false));
    }

    [UpstreamTest("LO-135 should select single option")]
    public async Task ShouldSelectSingleOption()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var select = Page.Locator("select");
        await select.SelectOptionAsync("blue");
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("LO-136 should focus and blur a button")]
    public async Task ShouldFocusAndBlurAButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = Page.Locator("button");
        Assert.That(await button.EvaluateAsync<bool>("button => document.activeElement === button"), Is.EqualTo(false));

        var focused = false;
        var blurred = false;
        await Page.ExposeFunctionAsync("focusEvent", () => focused = true);
        await Page.ExposeFunctionAsync("blurEvent", () => blurred = true);
        await button.EvaluateAsync(@"button => {
    button.addEventListener('focus', window['focusEvent']);
    button.addEventListener('blur', window['blurEvent']);
  }");

        await button.FocusAsync();
        Assert.That(focused, Is.EqualTo(true));
        Assert.That(blurred, Is.EqualTo(false));
        Assert.That(await button.EvaluateAsync<bool>("button => document.activeElement === button"), Is.EqualTo(true));

        await button.BlurAsync();
        Assert.That(focused, Is.EqualTo(true));
        Assert.That(blurred, Is.EqualTo(true));
        Assert.That(await button.EvaluateAsync<bool>("button => document.activeElement === button"), Is.EqualTo(false));
    }

    [UpstreamTest("LO-137 focus should respect strictness")]
    public async Task FocusShouldRespectStrictness()
    {
        await Page.SetContentAsync("<div>A</div><div>B</div>");
        var error = await Catch(() => Page.Locator("div").FocusAsync());
        Assert.That(error.Message, Does.Contain("strict mode violation"));
    }

    [UpstreamTest("LO-138 should dispatch click event via ElementHandles")]
    public async Task ShouldDispatchClickEventViaElementHandles()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = Page.Locator("button");
        await button.DispatchEventAsync("click");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("Clicked"));
    }

    [UpstreamTest("LO-139 should upload the file")]
    public async Task ShouldUploadTheFile()
    {
        await Page.GotoAsync(Server.Prefix + "/input/fileupload.html");
        var filePath = Path.GetRelativePath(Directory.GetCurrentDirectory(), Asset("file-to-upload.txt"));
        var input = Page.Locator("input[type=file]");
        await input.SetInputFilesAsync(filePath);
        Assert.That(await Page.EvaluateAsync<string>("e => e.files[0].name", await input.ElementHandleAsync()), Is.EqualTo("file-to-upload.txt"));
    }
}
