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
    public class PageAddStyleTagTests : PageTestEx
    {
        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw an error if no options are provided")]
        [Test, Ignore("Not relevant for C#, js specific")]
        public void ShouldThrowAnErrorIfNoOptionsAreProvided()
        {
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with a url")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAUrl()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new() { Url = "/injectedstyle.css" });
            Assert.NotNull(styleHandle);
            Assert.AreEqual("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw an error if loading from url fail")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowAnErrorIfLoadingFromUrlFail()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new() { Url = "/nonexistfile.js" }));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with a path")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithAPath()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new() { Path = TestUtils.GetWebServerFile("injectedstyle.css") });
            Assert.NotNull(styleHandle);
            Assert.AreEqual("rgb(255, 0, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should include sourceURL when path is provided")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldIncludeSourceURLWhenPathIsProvided()
        {
            await Page.GotoAsync(Server.EmptyPage);
            await Page.AddStyleTagAsync(new() { Path = TestUtils.GetWebServerFile("injectedstyle.css") });
            var styleHandle = await Page.QuerySelectorAsync("style");
            string styleContent = await Page.EvaluateAsync<string>("style => style.innerHTML", styleHandle);
            StringAssert.Contains(TestUtils.GetWebServerFile("injectedstyle.css"), styleContent);
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should work with content")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldWorkWithContent()
        {
            await Page.GotoAsync(Server.EmptyPage);
            var styleHandle = await Page.AddStyleTagAsync(new() { Content = "body { background-color: green; }" });
            Assert.NotNull(styleHandle);
            Assert.AreEqual("rgb(0, 128, 0)", await Page.EvaluateAsync<string>("window.getComputedStyle(document.querySelector('body')).getPropertyValue('background-color')"));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw when added with content to the CSP page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithContentToTheCSPPage()
        {
            await Page.GotoAsync(Server.Prefix + "/csp.html");
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new() { Content = "body { background-color: green; }" }));
        }

        [PlaywrightTest("page-add-style-tag.spec.ts", "should throw when added with URL to the CSP page")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldThrowWhenAddedWithURLToTheCSPPage()
        {
            await Page.GotoAsync(Server.Prefix + "/csp.html");
            await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() =>
                Page.AddStyleTagAsync(new() { Url = Server.CrossProcessPrefix + "/injectedstyle.css" }));
        }
    }
}
