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

namespace Microsoft.Playwright.Tests;

public class PageEventPageErrorTests : PageTestEx
{
    [PlaywrightTest("page-event-pageerror.spec.ts", "should fire")]
    public async Task ShouldFire()
    {
        var errorEvent = new TaskCompletionSource<string>();
        Page.PageError += (_, error) => errorEvent.TrySetResult(error);

        var (error, _) = await TaskUtils.WhenAll(
            errorEvent.Task,
            Page.GotoAsync(Server.Prefix + "/error.html")
        );

        string expectedError = "";
        if (TestConstants.IsFirefox)
        {
            expectedError = @"Error: Fancy error!
    at c (myscript.js:14:11)
    at b (myscript.js:10:5)
    at a (myscript.js:6:5)
    at  (myscript.js:3:1)";
        }
        else if (TestConstants.IsChromium)
        {
            expectedError = @"Error: Fancy error!
    at c (myscript.js:14:11)
    at b (myscript.js:10:5)
    at a (myscript.js:6:5)
    at myscript.js:3:1";
        }
        else if (TestConstants.IsWebKit)
        {
            expectedError = @$"Error: Fancy error!
    at c ({Page.Url}:14:36)
    at b ({Page.Url}:10:6)
    at a ({Page.Url}:6:6)
    at global code ({Page.Url}:3:2)";
        }

        expectedError = expectedError.Replace("\r", "");

        StringAssert.AreEqualIgnoringCase(expectedError, error);
    }

    [PlaywrightTest("page-event-pageerror.spec.ts", "should contain sourceURL")]
    [Skip(SkipAttribute.Targets.Webkit)]
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
    [Skip(SkipAttribute.Targets.Firefox)]
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
    [Skip(SkipAttribute.Targets.Firefox)]
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
