/**
 * Copyright (c) Microsoft Corporation.
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

/// <summary>Port of tests/page/locator-wait-for-function.spec.ts.</summary>
public class LocatorWaitForFunctionTests
{
    [UpstreamTest("LO-183 should wait for an attribute to appear")]
    public async Task ShouldWaitForAnAttributeToAppear(IPage page)
    {
        await page.SetContentAsync("<button id=toggle>Menu</button>");
        await page.EvaluateAsync("() => window.builtins.setTimeout(() => document.querySelector('#toggle').setAttribute('aria-expanded', 'true'), 1000)");
        await page.Locator("#toggle").WaitForFunctionAsync("element => element.hasAttribute('aria-expanded')");
    }

    [UpstreamTest("LO-184 should return immediately when already truthy")]
    public async Task ShouldReturnImmediatelyWhenAlreadyTruthy(IPage page)
    {
        await page.SetContentAsync("<div id=target>yes</div>");
        // .NET: WaitForFunctionAsync returns a Task without a value: there is nothing to compare with undefined.
        await page.Locator("#target").WaitForFunctionAsync("element => element.textContent === 'yes'");
    }

    [UpstreamTest("LO-185 should accept ElementHandle arguments")]
    public async Task ShouldAcceptElementHandleArguments(IPage page)
    {
        await page.SetContentAsync("<div id=a></div><div id=b>value</div>");
        var handle = await page.QuerySelectorAsync("#b");
        await page.Locator("#a").WaitForFunctionAsync("(element, other) => other.textContent === 'value'", handle);
    }

    [UpstreamTest("LO-186 should accept string expression")]
    public async Task ShouldAcceptStringExpression(IPage page)
    {
        await page.SetContentAsync("<div id=target>yes</div>");
        await page.Locator("#target").WaitForFunctionAsync("element => element.textContent === 'yes'");
    }

    [UpstreamTest("LO-187 should resolve a promise returned by the predicate")]
    public async Task ShouldResolveAPromiseReturnedByThePredicate(IPage page)
    {
        await page.SetContentAsync("<div id=target>yes</div>");
        await page.Locator("#target").WaitForFunctionAsync("async element => element.textContent === 'yes'");
    }

    [UpstreamTest("LO-188 should wait for element to appear and survive rerender")]
    public async Task ShouldWaitForElementToAppearAndSurviveRerender(IPage page)
    {
        await page.SetContentAsync("<span>nothing here</span>");
        await page.EvaluateAsync(@"() => {
    let count = 0;
    let prev = null;
    const tick = () => {
      ++count;
      const next = document.createElement('div');
      next.id = 'target';
      next.textContent = String(count);
      if (prev)
        prev.remove();
      document.body.appendChild(next);
      prev = next;
      if (count < 3)
        window.builtins.setTimeout(tick, 500);
    };
    window.builtins.setTimeout(tick, 500);
  }");
        await page.Locator("#target").WaitForFunctionAsync("element => element.textContent === '3'");
    }

    [UpstreamTest("LO-189 should throw when predicate throws")]
    public async Task ShouldThrowWhenPredicateThrows(IPage page)
    {
        await page.SetContentAsync("<div id=target>no</div>");
        var error = await Catch(() => page.Locator("#target").WaitForFunctionAsync(@"() => {
    throw new Error('oh my');
  }"));
        Assert.That(error.Message, Does.Contain("oh my"));
    }

    [UpstreamTest("LO-190 should throw on strict mode violation")]
    public async Task ShouldThrowOnStrictModeViolation(IPage page)
    {
        await page.SetContentAsync("<div class=x>1</div><div class=x>2</div>");
        var error = await Catch(() => page.Locator("div.x").WaitForFunctionAsync("() => true"));
        Assert.That(error.Message, Does.Contain("strict mode violation"));
    }
}
