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

/// <summary>Port of tests/page/elementhandle-misc.spec.ts.</summary>
public class ElementhandleMiscTests : UpstreamPageTest
{
    [UpstreamTest("EH-045 should hover")]
    public async Task ShouldHover()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        var button = await Page.QuerySelectorAsync("#button-6");
        await button.HoverAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("EH-046 should hover when Node is removed")]
    public async Task ShouldHoverWhenNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var button = await Page.QuerySelectorAsync("#button-6");
        await button.HoverAsync();
        Assert.That(await Page.EvaluateAsync<string>("() => document.querySelector('button:hover').id"), Is.EqualTo("button-6"));
    }

    [UpstreamTest("EH-047 should fill input")]
    public async Task ShouldFillInput()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        var handle = await Page.QuerySelectorAsync("input");
        await handle.FillAsync("some value");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("EH-048 should fill input when Node is removed")]
    public async Task ShouldFillInputWhenNodeIsRemoved()
    {
        await Page.GotoAsync(Server.Prefix + "/input/textarea.html");
        await Page.EvaluateAsync("() => delete window['Node']");
        var handle = await Page.QuerySelectorAsync("input");
        await handle.FillAsync("some value");
        Assert.That(await Page.EvaluateAsync<string>("() => window['result']"), Is.EqualTo("some value"));
    }

    [UpstreamTest("EH-049 should check the box")]
    public async Task ShouldCheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = await Page.QuerySelectorAsync("input");
        await input.CheckAsync();
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(true));
    }

    [UpstreamTest("EH-050 should check the box using setChecked")]
    public async Task ShouldCheckTheBoxUsingSetChecked()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox'></input>");
        var input = await Page.QuerySelectorAsync("input");
        await input.SetCheckedAsync(true);
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(true));
        await input.SetCheckedAsync(false);
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(false));
    }

    [UpstreamTest("EH-051 should uncheck the box")]
    public async Task ShouldUncheckTheBox()
    {
        await Page.SetContentAsync("<input id='checkbox' type='checkbox' checked></input>");
        var input = await Page.QuerySelectorAsync("input");
        await input.UncheckAsync();
        Assert.That(await Page.EvaluateAsync<bool>("checkbox.checked"), Is.EqualTo(false));
    }

    [UpstreamTest("EH-052 should select single option")]
    public async Task ShouldSelectSingleOption()
    {
        await Page.GotoAsync(Server.Prefix + "/input/select.html");
        var select = await Page.QuerySelectorAsync("select");
        await select.SelectOptionAsync("blue");
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onInput"), Is.EqualTo(new[] { "blue" }));
        Assert.That(await Page.EvaluateAsync<string[]>("() => window['result'].onChange"), Is.EqualTo(new[] { "blue" }));
    }

    [UpstreamTest("EH-053 should focus a button")]
    public async Task ShouldFocusAButton()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        Assert.That(await button.EvaluateAsync<bool>("button => document.activeElement === button"), Is.EqualTo(false));
        await button.FocusAsync();
        Assert.That(await button.EvaluateAsync<bool>("button => document.activeElement === button"), Is.EqualTo(true));
    }

    [UpstreamTest("EH-054 should allow disposing twice")]
    public async Task ShouldAllowDisposingTwice()
    {
        await Page.SetContentAsync("<section>39</section>");
        var element = await Page.QuerySelectorAsync("section");
        Assert.That(element, Is.Not.Null);
        await element.DisposeAsync();
        await element.DisposeAsync();
    }
}
