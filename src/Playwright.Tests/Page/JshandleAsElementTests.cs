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

/// <summary>Port of tests/page/jshandle-as-element.spec.ts.</summary>
public class JshandleAsElementTests
{
    [UpstreamTest("JH-001 should work @smoke")]
    public async Task ShouldWork(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync("() => document.body");
        var element = aHandle.AsElement();
        Assert.That(element, Is.Not.Null);
    }

    [UpstreamTest("JH-002 should return null for non-elements")]
    [Fixme("dotnet: JSHandle.AsElement() throws InvalidCastException for a handle that is not an element, it casts itself to IElementHandle; upstream returns null")]
    public async Task ShouldReturnNullForNonElements(IPage page)
    {
        var aHandle = await page.EvaluateHandleAsync("() => 2");
        var element = aHandle.AsElement();
        Assert.That(element, Is.Null);
    }

    [UpstreamTest("JH-003 should return ElementHandle for TextNodes")]
    public async Task ShouldReturnElementHandleForTextNodes(IPage page)
    {
        await page.SetContentAsync("<div>ee!</div>");
        var aHandle = await page.EvaluateHandleAsync("() => document.querySelector('div').firstChild");
        var element = aHandle.AsElement();
        Assert.That(element, Is.Not.Null);
        Assert.That(await page.EvaluateAsync<bool>("e => e.nodeType === Node.TEXT_NODE", element), Is.True);
    }

    [UpstreamTest("JH-004 should work with nullified Node")]
    public async Task ShouldWorkWithNullifiedNode(IPage page)
    {
        await page.SetContentAsync("<section>test</section>");
        await page.EvaluateAsync("delete Node");
        var handle = await page.EvaluateHandleAsync("() => document.querySelector('section')");
        var element = handle.AsElement();
        Assert.That(element, Is.Not.Null);
    }
}
