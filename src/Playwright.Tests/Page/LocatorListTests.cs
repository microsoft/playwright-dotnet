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

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-list.spec.ts.</summary>
public class LocatorListTests : UpstreamPageTest
{
    [UpstreamTest("LO-126 locator.all should work")]
    public async Task LocatorAllShouldWork()
    {
        await Page.SetContentAsync("<div><p>A</p><p>B</p><p>C</p></div>");
        var texts = new List<string>();
        foreach (var p in await Page.Locator("div >> p").AllAsync())
        {
            texts.Add(await p.TextContentAsync());
        }
        Assert.That(texts, Is.EqualTo(new[] { "A", "B", "C" }));
    }
}
