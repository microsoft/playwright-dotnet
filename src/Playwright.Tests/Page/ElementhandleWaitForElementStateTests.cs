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

/// <summary>Port of tests/page/elementhandle-wait-for-element-state.spec.ts.</summary>
public class ElementhandleWaitForElementStateTests : UpstreamPageTest
{
    private static Task GiveItAChanceToResolve(IPage page) => Rafraf(page, 5);

    [UpstreamTest("EH-112 should wait for visible")]
    public async Task ShouldWaitForVisible()
    {
        await Page.SetContentAsync("<div style='display:none'>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var done = false;
        // .NET: const promise = div.waitForElementState('visible').then(() => done = true);
        async Task Promise()
        {
            await div.WaitForElementStateAsync(ElementState.Visible);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await div.EvaluateAsync("div => div.style.display = 'block'");
        await promise;
    }

    [UpstreamTest("EH-113 should wait for already visible")]
    public async Task ShouldWaitForAlreadyVisible()
    {
        await Page.SetContentAsync("<div>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        await div.WaitForElementStateAsync(ElementState.Visible);
    }

    [UpstreamTest("EH-114 should timeout waiting for visible")]
    public async Task ShouldTimeoutWaitingForVisible()
    {
        await Page.SetContentAsync("<div style='display:none'>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var error = await Catch(() => div.WaitForElementStateAsync(ElementState.Visible, new() { Timeout = 1000 }));
        Assert.That(error.Message, Does.Contain("Timeout 1000ms exceeded"));
    }

    [UpstreamTest("EH-115 should throw waiting for visible when detached")]
    public async Task ShouldThrowWaitingForVisibleWhenDetached()
    {
        await Page.SetContentAsync("<div style='display:none'>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var promise = Catch(() => div.WaitForElementStateAsync(ElementState.Visible));
        await div.EvaluateAsync("div => div.remove()");
        var error = await promise;
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("EH-116 should wait for hidden")]
    public async Task ShouldWaitForHidden()
    {
        await Page.SetContentAsync("<div>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var done = false;
        // .NET: const promise = div.waitForElementState('hidden').then(() => done = true);
        async Task Promise()
        {
            await div.WaitForElementStateAsync(ElementState.Hidden);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await div.EvaluateAsync("div => div.style.display = 'none'");
        await promise;
    }

    [UpstreamTest("EH-117 should wait for already hidden")]
    public async Task ShouldWaitForAlreadyHidden()
    {
        await Page.SetContentAsync("<div></div>");
        var div = await Page.QuerySelectorAsync("div");
        await div.WaitForElementStateAsync(ElementState.Hidden);
    }

    [UpstreamTest("EH-118 should wait for hidden when detached")]
    public async Task ShouldWaitForHiddenWhenDetached()
    {
        await Page.SetContentAsync("<div>content</div>");
        var div = await Page.QuerySelectorAsync("div");
        var done = false;
        // .NET: const promise = div.waitForElementState('hidden').then(() => done = true);
        async Task Promise()
        {
            await div.WaitForElementStateAsync(ElementState.Hidden);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await div.EvaluateAsync("div => div.remove()");
        await promise;
    }

    [UpstreamTest("EH-119 should throw waiting for enabled when detached")]
    public async Task ShouldThrowWaitingForEnabledWhenDetached()
    {
        await Page.SetContentAsync("<button disabled>Target</button>");
        var button = await Page.QuerySelectorAsync("button");
        var promise = Catch(() => button.WaitForElementStateAsync(ElementState.Enabled));
        await button.EvaluateAsync("button => button.remove()");
        var error = await promise;
        Assert.That(error.Message, Does.Contain("Element is not attached to the DOM"));
    }

    [UpstreamTest("EH-120 should wait for aria enabled button")]
    public async Task ShouldWaitForAriaEnabledButton()
    {
        await Page.SetContentAsync("<button aria-disabled=true><span>Target</span></button>");
        var span = await Page.QuerySelectorAsync("text=Target");
        var done = false;
        // .NET: const promise = span.waitForElementState('enabled').then(() => done = true);
        async Task Promise()
        {
            await span.WaitForElementStateAsync(ElementState.Enabled);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await span.EvaluateAsync("span => span.parentElement.setAttribute('aria-disabled', 'false')");
        await promise;
    }

    [UpstreamTest("EH-121 should wait for button with an aria-disabled parent")]
    public async Task ShouldWaitForButtonWithAnAriaDisabledParent()
    {
        await Page.SetContentAsync("<div role=\"group\" aria-disabled=true><button><span>Target</span></button></div>");
        var span = await Page.QuerySelectorAsync("text=Target");
        var done = false;
        // .NET: const promise = span.waitForElementState('enabled').then(() => done = true);
        async Task Promise()
        {
            await span.WaitForElementStateAsync(ElementState.Enabled);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await span.EvaluateAsync("span => span.parentElement.parentElement.setAttribute('aria-disabled', 'false')");
        await promise;
    }

    [UpstreamTest("EH-122 should wait for stable position")]
    public async Task ShouldWaitForStablePosition()
    {
        await Page.GotoAsync(Server.Prefix + "/input/button.html");
        var button = await Page.QuerySelectorAsync("button");
        await Page.EvalOnSelectorAsync("button", @"button => {
    button.style.transition = 'margin 10000ms linear 0s';
    button.style.marginLeft = '20000px';
  }");
        // rafraf for Firefox to kick in the animation.
        await Rafraf(Page);
        var done = false;
        // .NET: const promise = button.waitForElementState('stable').then(() => done = true);
        async Task Promise()
        {
            await button.WaitForElementStateAsync(ElementState.Stable);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await button.EvaluateAsync("button => button.style.transition = ''");
        await promise;
    }

    [UpstreamTest("EH-123 should wait for editable input")]
    public async Task ShouldWaitForEditableInput()
    {
        await Page.SetContentAsync("<input readonly>");
        var input = await Page.QuerySelectorAsync("input");
        var done = false;
        // .NET: const promise = input.waitForElementState('editable').then(() => done = true);
        async Task Promise()
        {
            await input.WaitForElementStateAsync(ElementState.Editable);
            done = true;
        }
        var promise = Promise();
        await GiveItAChanceToResolve(Page);
        Assert.That(done, Is.EqualTo(false));
        await input.EvaluateAsync("input => input.readOnly = false");
        await promise;
    }
}
