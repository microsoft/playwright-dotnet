/**
 * Copyright 2024 Adobe Inc. All rights reserved.
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

/// <summary>Port of tests/page/page-request-gc.spec.ts.</summary>
public class PageRequestGcTests
{
    [UpstreamTest("RO-149 should work")]
    public async Task ShouldWork(IPage page)
    {
        await page.EvaluateAsync(@"() => {
    globalThis.objectToDestroy = { hello: 'world' };
    globalThis.weakRef = new WeakRef(globalThis.objectToDestroy);
  }");

        await page.RequestGCAsync();
        ExpectToEqual(await page.EvaluateAsync<object>("() => globalThis.weakRef.deref()"), new { hello = "world" });

        await page.RequestGCAsync();
        ExpectToEqual(await page.EvaluateAsync<object>("() => globalThis.weakRef.deref()"), new { hello = "world" });

        await page.EvaluateAsync("() => globalThis.objectToDestroy = null");
        await page.RequestGCAsync();
        Assert.That(await page.EvaluateAsync<object>("() => globalThis.weakRef.deref()"), Is.Null);
    }

    [UpstreamTest("RO-150 should collect element retained by locator hit-target interceptor after detach")]
    public async Task ShouldCollectElementRetainedByLocatorHitTargetInterceptorAfterDetach(IPage page, string browserName)
    {
        Info.Fixme(browserName == "firefox", "Firefox hit-target interceptor does not release its element reference on detach");
        Info.Fixme(browserName == "chromium", "Chromium retains a native reference to the last hit-tested element; fix pending upstream discussion, see #41575");
        await page.SetContentAsync("<button id=\"btn\">click me</button>");
        await page.Locator("#btn").ClickAsync();
        await page.EvaluateAsync(@"() => {
    globalThis.weakRef = new WeakRef(document.getElementById('btn'));
    document.getElementById('btn').remove();
  }");
        await page.RequestGCAsync();
        Assert.That(await page.EvaluateAsync<object>("() => globalThis.weakRef.deref()"), Is.Null);
    }
}
