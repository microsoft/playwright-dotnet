/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class SelectorMiscTests : PageTestEx
    {
        [PlaywrightTest("selectors-misc.spec.ts", "should work for open shadow roots")]
        public async Task ShouldWorkForOpenShadowRoots()
        {
            await Page.GotoAsync(Server.Prefix + "/deep-shadow.html");
            Assert.AreEqual("Hello from root2", await Page.EvalOnSelectorAsync<string>("id=target", "e => e.textContent"));
            Assert.AreEqual("Hello from root1", await Page.EvalOnSelectorAsync<string>("data-testid=foo", "e => e.textContent"));
            Assert.AreEqual(3, await Page.EvalOnSelectorAllAsync<int>("data-testid=foo", "els => els.length"));
            Assert.Null(await Page.QuerySelectorAsync("id:light=target"));
            Assert.Null(await Page.QuerySelectorAsync("data-testid:light=foo"));
            Assert.IsEmpty(await Page.QuerySelectorAllAsync("data-testid:light=foo"));
        }
    }
}
