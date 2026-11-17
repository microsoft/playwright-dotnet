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
public class ElementhandlePressTests
{
    [UpstreamTest("EH-062 should work")]
    public async Task ShouldWork(IPage page)
    {
        await page.SetContentAsync("<input type='text' />");
        await page.PressAsync("input", "h");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("h"));
    }

    [UpstreamTest("EH-063 should not select existing value")]
    public async Task ShouldNotSelectExistingValue(IPage page)
    {
        await page.SetContentAsync("<input type='text' value='hello' />");
        await page.PressAsync("input", "w");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("whello"));
    }

    [UpstreamTest("EH-064 should reset selection when not focused")]
    public async Task ShouldResetSelectionWhenNotFocused(IPage page)
    {
        await page.SetContentAsync("<input type='text' value='hello' /><div tabIndex=2>text</div>");
        await page.EvalOnSelectorAsync("input", @"input => {
    input.selectionStart = 2;
    input.selectionEnd = 4;
    document.querySelector('div').focus();
  }");
        await page.PressAsync("input", "w");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("whello"));
    }

    [UpstreamTest("EH-065 should not modify selection when focused")]
    public async Task ShouldNotModifySelectionWhenFocused(IPage page)
    {
        await page.SetContentAsync("<input type='text' value='hello' />");
        await page.EvalOnSelectorAsync("input", @"input => {
    input.focus();
    input.selectionStart = 2;
    input.selectionEnd = 4;
  }");
        await page.PressAsync("input", "w");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("hewo"));
    }

    [UpstreamTest("EH-066 should work with number input")]
    public async Task ShouldWorkWithNumberInput(IPage page, string browserName)
    {
        Info.Fixme(browserName == "webkit", "Started failing after https://github.com/WebKit/WebKit/commit/c92a2aea185d63b5e9998608a9c0321a461c496c");
        await page.SetContentAsync("<input type='number' value=2 />");
        await page.PressAsync("input", "1");
        Assert.That(await page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("12"));
    }
}
