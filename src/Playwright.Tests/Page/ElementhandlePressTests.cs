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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/elementhandle-press.spec.ts.</summary>
public class ElementhandlePressTests : UpstreamPageTest
{
    [UpstreamTest("EH-062 should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<input type='text' />");
        await Page.PressAsync("input", "h");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("h"));
    }

    [UpstreamTest("EH-063 should not select existing value")]
    public async Task ShouldNotSelectExistingValue()
    {
        await Page.SetContentAsync("<input type='text' value='hello' />");
        await Page.PressAsync("input", "w");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("whello"));
    }

    [UpstreamTest("EH-064 should reset selection when not focused")]
    public async Task ShouldResetSelectionWhenNotFocused()
    {
        await Page.SetContentAsync("<input type='text' value='hello' /><div tabIndex=2>text</div>");
        await Page.EvalOnSelectorAsync("input", @"input => {
    input.selectionStart = 2;
    input.selectionEnd = 4;
    document.querySelector('div').focus();
  }");
        await Page.PressAsync("input", "w");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("whello"));
    }

    [UpstreamTest("EH-065 should not modify selection when focused")]
    public async Task ShouldNotModifySelectionWhenFocused()
    {
        await Page.SetContentAsync("<input type='text' value='hello' />");
        await Page.EvalOnSelectorAsync("input", @"input => {
    input.focus();
    input.selectionStart = 2;
    input.selectionEnd = 4;
  }");
        await Page.PressAsync("input", "w");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("hewo"));
    }

    [UpstreamTest("EH-066 should work with number input")]
    public async Task ShouldWorkWithNumberInput()
    {
        Info.Fixme(BrowserName == "webkit", "Started failing after https://github.com/WebKit/WebKit/commit/c92a2aea185d63b5e9998608a9c0321a461c496c");
        await Page.SetContentAsync("<input type='number' value=2 />");
        await Page.PressAsync("input", "1");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("12"));
    }
}
