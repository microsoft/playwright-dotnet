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

using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    [Parallelizable(ParallelScope.Self)]
    public class PageEventPageErrorTests : PageTestEx
    {
        [PlaywrightTest("page-event-pageerror.spec.ts", "should fire")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldFire()
        {
            var errorEvent = new TaskCompletionSource<string>();
            Page.PageError += (_, error) => errorEvent.TrySetResult(error);

            var (error, _) = await TaskUtils.WhenAll(
                errorEvent.Task,
                Page.GotoAsync(Server.Prefix + "/error.html")
            );

            StringAssert.Contains("Error", error);
            StringAssert.Contains("Fancy error!", error);
            string stack = await Page.EvaluateAsync<string>("() => window.e.stack");

            if (TestConstants.IsWebKit)
            {
                stack = stack.Replace("14:25", "15:19");
            }

            StringAssert.Contains(stack, error);
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should contain sourceURL")]
        [Test, SkipBrowserAndPlatform(skipWebkit: true)]
        public async Task ShouldContainSourceURL()
        {
            var pageError = new TaskCompletionSource<string>();
            Page.PageError += (_, error) => pageError.TrySetResult(error);
            var (error, _) = await TaskUtils.WhenAll(
                pageError.Task,
                Page.GotoAsync(Server.Prefix + "/error.html"));

            StringAssert.Contains("myscript.js", error);
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should handle odd values")]
        [Test, Timeout(TestConstants.DefaultTestTimeout)]
        public async Task ShouldHandleOddValues()
        {
            object[][] cases = new object[][]
            {
                new []{ null, "null"},
                //[undefined], "undefined" Not undefined here
                new object[]{ 0, "0"},
                new []{ "", "" },
            };

            foreach (object[] kv in cases)
            {
                var pageError = new TaskCompletionSource<string>();
                Page.PageError += (_, error) => pageError.TrySetResult(error);
                var (error, _) = await TaskUtils.WhenAll(
                    pageError.Task,
                    Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw value; }, 0)", kv[0]));

                StringAssert.Contains(TestConstants.IsFirefox ? "uncaught exception: " + kv[1].ToString() : kv[1].ToString(), error);
            }
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should handle object")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldHandleObject()
        {
            var pageError = new TaskCompletionSource<string>();
            Page.PageError += (_, error) => pageError.TrySetResult(error);
            var (error, _) = await TaskUtils.WhenAll(
                pageError.Task,
                Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw {}; }, 0)", 0));

            StringAssert.Contains(TestConstants.IsChromium ? "Object" : "[object Object]", error);
        }

        [PlaywrightTest("page-event-pageerror.spec.ts", "should handle window")]
        [Test, SkipBrowserAndPlatform(skipFirefox: true)]
        public async Task ShouldHandleWindow()
        {
            var pageError = new TaskCompletionSource<string>();
            Page.PageError += (_, error) => pageError.TrySetResult(error);
            var (error, _) = await TaskUtils.WhenAll(
                pageError.Task,
                Page.EvaluateAsync<JsonElement>("value => setTimeout(() => { throw window ; }, 0)", 0));

            StringAssert.Contains(TestConstants.IsChromium ? "Window" : "[object Window]", error);
        }
    }
}
