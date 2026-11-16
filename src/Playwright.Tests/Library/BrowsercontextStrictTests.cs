/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/browsercontext-strict.spec.ts.</summary>
public class BrowsercontextStrictTests
{
    [UpstreamTest("SM-011 should not fail page.textContent in non-strict mode")]
    public async Task ShouldNotFailPageTextContentInNonStrictMode(IPage page)
    {
        await page.SetContentAsync("<span>span1</span><div><span>target</span></div>");
        Assert.That(await page.TextContentAsync("span", new() { Strict = false }), Is.EqualTo("span1"));
    }

    public class StrictContextMode
    {
        // it.use({
        //   contextOptions: async ({ contextOptions }, use) => {
        //     const options = { ...contextOptions, strictSelectors: true };
        //     await use(options);
        //   }
        // });
        [Fixture]
        public static async Task ContextOptions(BrowserNewContextOptions contextOptions, Func<BrowserNewContextOptions, Task> use)
        {
            var options = new BrowserNewContextOptions(contextOptions) { StrictSelectors = true };
            await use(options);
        }

        [UpstreamTest("SM-012 should fail page.textContent in strict mode")]
        public async Task ShouldFailPageTextContentInStrictMode(IPage page)
        {
            await page.SetContentAsync("<span>span1</span><div><span>target</span></div>");
            var error = await Catch(() => page.TextContentAsync("span"));
            Assert.That(error.Message, Does.Contain("strict mode violation"));
        }

        [UpstreamTest("SM-013 should fail page.click in strict mode")]
        public async Task ShouldFailPageClickInStrictMode(IPage page)
        {
            await page.SetContentAsync("<button>button1</button><button>target</button>");
            var error = await Catch(() => page.ClickAsync("button"));
            Assert.That(error.Message, Does.Contain("strict mode violation"));
        }

        [UpstreamTest("SM-014 should opt out of strict mode")]
        public async Task ShouldOptOutOfStrictMode(IPage page)
        {
            await page.SetContentAsync("<span>span1</span><div><span>target</span></div>");
            Assert.That(await page.TextContentAsync("span", new() { Strict = false }), Is.EqualTo("span1"));
        }
    }
}
