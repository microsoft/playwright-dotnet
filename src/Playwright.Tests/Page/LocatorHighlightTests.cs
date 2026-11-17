/**
 * Copyright Microsoft Corporation. All rights reserved.
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
using static Microsoft.Playwright.Assertions;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Page;

/// <summary>Port of tests/page/locator-highlight.spec.ts.</summary>
public class LocatorHighlightTests
{
    [BeforeEach]
    public static void Skip(string mode) => Info.Skip(mode != "default", "Highlight element has a closed shadow-root on != default");

    [UpstreamTest("LO-114 should highlight locator")]
    public async Task ShouldHighlightLocator(IPage page)
    {
        await page.SetContentAsync("<input type='text' />");
        await page.Locator("input").HighlightAsync();
        await Expect(page.Locator("x-pw-tooltip")).ToHaveTextAsync("locator('input')");
        await Expect(page.Locator("x-pw-highlight")).ToBeVisibleAsync();
        var box1 = RoundBox(await page.Locator("input").BoundingBoxAsync());
        var box2 = RoundBox(await page.Locator("x-pw-highlight").BoundingBoxAsync());
        ExpectToEqual(box1, box2);
    }
}
