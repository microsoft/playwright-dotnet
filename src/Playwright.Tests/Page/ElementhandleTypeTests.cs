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

/// <summary>Port of tests/page/elementhandle-type.spec.ts.</summary>
public class ElementhandleTypeTests : UpstreamPageTest
{
    [UpstreamTest("EH-107 should work")]
    public async Task ShouldWork()
    {
        await Page.SetContentAsync("<input type='text' />");
        await Page.TypeAsync("input", "hello");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("hello"));
    }

    [UpstreamTest("EH-108 should not select existing value")]
    public async Task ShouldNotSelectExistingValue()
    {
        await Page.SetContentAsync("<input type='text' value='hello' />");
        await Page.TypeAsync("input", "world");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("worldhello"));
    }

    [UpstreamTest("EH-109 should reset selection when not focused")]
    public async Task ShouldResetSelectionWhenNotFocused()
    {
        await Page.SetContentAsync("<input type='text' value='hello' /><div tabIndex=2>text</div>");
        await Page.EvalOnSelectorAsync("input", @"input => {
    input.selectionStart = 2;
    input.selectionEnd = 4;
    document.querySelector('div').focus();
  }");
        await Page.TypeAsync("input", "world");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("worldhello"));
    }

    [UpstreamTest("EH-110 should not modify selection when focused")]
    public async Task ShouldNotModifySelectionWhenFocused()
    {
        await Page.SetContentAsync("<input type='text' value='hello' />");
        await Page.EvalOnSelectorAsync("input", @"input => {
    input.focus();
    input.selectionStart = 2;
    input.selectionEnd = 4;
  }");
        await Page.TypeAsync("input", "world");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("heworldo"));
    }

    [UpstreamTest("EH-111 should work with number input")]
    public async Task ShouldWorkWithNumberInput()
    {
        Info.Fixme(BrowserName == "webkit", "Started failing after https://github.com/WebKit/WebKit/commit/c92a2aea185d63b5e9998608a9c0321a461c496c");
        await Page.SetContentAsync("<input type='number' value=2 />");
        await Page.TypeAsync("input", "13");
        Assert.That(await Page.EvalOnSelectorAsync<string>("input", "input => input.value"), Is.EqualTo("132"));
    }
}
