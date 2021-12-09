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

using System.Text.RegularExpressions;
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

        [PlaywrightTest("locator-query.spec.ts", "should filter by text")]
        public async Task ShouldFilterByText()
        {
            await Page.SetContentAsync("<div>Foobar</div><div>Bar</div>");
            StringAssert.Contains(await Page.Locator("div").WithText("Foo").TextContentAsync(), "Foobar");
        }

        [PlaywrightTest("locator-query.spec.ts", "should filter by text 2")]
        public async Task ShouldFilterByText2()
        {
            await Page.SetContentAsync("<div>foo <span>hello world</span> bar</div>");
            StringAssert.Contains(await Page.Locator("div").WithText("hello world").TextContentAsync(), "foo hello world bar");
        }

        [PlaywrightTest("locator-query.spec.ts", "should filter by regex")]
        public async Task ShouldFilterByRegex()
        {
            await Page.SetContentAsync("<div>Foobar</div><div>Bar</div>");
            StringAssert.Contains(await Page.Locator("div").WithText(new Regex("Foo.*")).InnerTextAsync(), "Foobar");
        }

        [PlaywrightTest("locator-query.spec.ts", "should filter by text with quotes")]
        public async Task ShouldFilterByTextWithQuotes()
        {
            await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
            StringAssert.Contains(await Page.Locator("div").WithText("Hello \"world\"").InnerTextAsync(), "Hello \"world\"");
        }

        [PlaywrightTest("locator-query.spec.ts", "should filter by regex with quotes")]
        public async Task ShouldFilterByRegexWithQuotes()
        {
            await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
            StringAssert.Contains(await Page.Locator("div").WithText(new Regex("Hello \"world\"")).InnerTextAsync(), "Hello \"world\"");
        }

        [PlaywrightTest("locator-query.spec.ts", "should filter by regex and regexp flags")]
        public async Task ShouldFilterByRegexandRegexpFlags()
        {
            await Page.SetContentAsync("<div>Hello \"world\"</div><div>Hello world</div>");
            StringAssert.Contains(await Page.Locator("div").WithText(new Regex("hElLo \"wOrld\"", RegexOptions.IgnoreCase)).InnerTextAsync(), "Hello \"world\"");
        }
    }
}
