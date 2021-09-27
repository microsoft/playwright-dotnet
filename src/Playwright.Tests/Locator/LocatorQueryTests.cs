/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */

using System.Threading.Tasks;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests.Locator
{
    public class LocatorQueryTests : PageTestEx
    {
        [PlaywrightTest("locator-query.spec.ts", "should respect first() and last()")]
        public async Task ShouldRespectFirstAndLast()
        {
            await Page.SetContentAsync(@"
                <section>
                    <div><p>A</p></div>
                    <div><p>A</p><p>A</p></div>
                    <div><p>A</p><p>A</p><p>A</p></div>
                </section>");
            Assert.AreEqual(6, await Page.Locator("div >> p").CountAsync());
            Assert.AreEqual(6, await Page.Locator("div").Locator("p").CountAsync());
            Assert.AreEqual(1, await Page.Locator("div").First.Locator("p").CountAsync());
            Assert.AreEqual(3, await Page.Locator("div").Last.Locator("p").CountAsync());
        }

        [PlaywrightTest("locator-query.spec.ts", "should respect nth()")]
        public async Task ShouldRespectNth()
        {
            await Page.SetContentAsync(@"
                <section>
                    <div><p>A</p></div>
                    <div><p>A</p><p>A</p></div>
                    <div><p>A</p><p>A</p><p>A</p></div>
                </section>");
            Assert.AreEqual(1, await Page.Locator("div >> p").Nth(0).CountAsync());
            Assert.AreEqual(2, await Page.Locator("div").Nth(1).Locator("p").CountAsync());
            Assert.AreEqual(3, await Page.Locator("div").Nth(2).Locator("p").CountAsync());
        }

        [PlaywrightTest("locator-query.spec.ts", "should throw on capture w/ nth()")]
        public async Task ShouldThrowOnCaptureWithNth()
        {
            await Page.SetContentAsync("<section><div><p>A</p></div></section>");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("*css=div >> p").Nth(1).ClickAsync());
            StringAssert.Contains("Can't query n-th element", exception.Message);
        }

        [PlaywrightTest("locator-query.spec.ts", "should throw on due to strictness")]
        public async Task ShouldThrowDueToStrictness()
        {
            await Page.SetContentAsync("<div>A</div><div>B</div>");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("div").IsVisibleAsync());
            StringAssert.Contains("strict mode violation", exception.Message);
        }

        [PlaywrightTest("locator-query.spec.ts", "should throw on due to strictness 2")]
        public async Task ShouldThrowDueToStrictness2()
        {
            await Page.SetContentAsync("<select><option>One</option><option>Two</option></select>");
            var exception = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("option").EvaluateAsync("() => {}"));
            StringAssert.Contains("strict mode violation", exception.Message);
        }
    }
}
