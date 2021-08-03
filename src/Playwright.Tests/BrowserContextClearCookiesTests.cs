/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Modifications copyright (c) Microsoft Corporation.
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
using Microsoft.Playwright.MSTest;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Playwright.Tests
{
    [TestClass]
    public class BrowserContextClearCookiesTests : PageTestEx
    {
        [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should clear cookies")]
        public async Task ShouldClearCookies()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Url = Server.EmptyPage,
                    Name = "cookie1",
                    Value = "1"
                }
            });
            Assert.AreEqual("cookie1=1", await Page.EvaluateAsync<string>("document.cookie"));
            await Context.ClearCookiesAsync();
            Assert.That.Collection(await Context.CookiesAsync()).IsEmpty();
            await Page.ReloadAsync();
            Assert.That.Collection(await Page.EvaluateAsync<string>("document.cookie")).IsEmpty();
        }

        [PlaywrightTest("browsercontext-clearcookies.spec.ts", "should isolate cookies when clearing")]
        public async Task ShouldIsolateWhenClearing()
        {
            await using var anotherContext = await Browser.NewContextAsync();
            await Context.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "page1cookie",
                    Value = "page1value",
                    Url = Server.EmptyPage
                }
            });

            await anotherContext.AddCookiesAsync(new[]
            {
                new Cookie
                {
                    Name = "page2cookie",
                    Value = "page2value",
                    Url = Server.EmptyPage
                }
            });

            Assert.That.Collection(await Context.CookiesAsync()).HasExactly(1);
            Assert.That.Collection(await anotherContext.CookiesAsync()).HasExactly(1);

            await Context.ClearCookiesAsync();
            Assert.That.Collection(await Context.CookiesAsync()).IsEmpty();
            Assert.That.Collection(await anotherContext.CookiesAsync()).HasExactly(1);

            await anotherContext.ClearCookiesAsync();
            Assert.That.Collection(await Context.CookiesAsync()).IsEmpty();
            Assert.That.Collection(await anotherContext.CookiesAsync()).IsEmpty();
        }
    }
}
