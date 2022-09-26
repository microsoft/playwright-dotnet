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

namespace Microsoft.Playwright.Tests;

public class PageWheelTests : PageTestEx
{
    private bool _ignoreDelta { get; set; }

    [SetUp]
    public void IgnoreDeltaSetup()
    {
        if (BrowserName == "chromium" && BrowserMajorVersion >= 102 && TestConstants.IsMacOSX)
        {
            // Chromium reports deltaX/deltaY scaled by host device scale factor.
            // https://bugs.chromium.org/p/chromium/issues/detail?id=1324819
            // https://github.com/microsoft/playwright/issues/7362
            // Different bots have different scale factors (usually 1 or 2), so we just ignore the values
            // instead of guessing the host scale factor.
            _ignoreDelta = true;
        }
    }

    [PlaywrightTest("wheel.spec.ts", "should dispatch wheel events")]
    public async Task ShouldDispatchWheelEvent()
    {
        await Page.SetContentAsync("<div style=\"width: 5000px; height: 5000px;\"></div>");
        await Page.Mouse.MoveAsync(50, 60);
        await ListenForWheelEvents("div");
        await Page.Mouse.WheelAsync(0, 100);
        await Page.WaitForFunctionAsync("window.scrollY === 100");
        if (!_ignoreDelta)
        {
            Assert.AreEqual(100, (await Page.EvaluateAsync("window.lastEvent")).Value.GetProperty("deltaY").GetInt32());
        }
    }

    [PlaywrightTest("wheel.spec.ts", "should scroll when nobody is listening")]
    public async Task ShouldScrollWhenNobodyIsListening()
    {
        await Page.GotoAsync(Server.Prefix + "/input/scrollable.html");
        await Page.Mouse.MoveAsync(50, 60);
        await Page.Mouse.WheelAsync(0, 100);
        await Page.WaitForFunctionAsync("window.scrollY === 100");
    }

    private async Task ListenForWheelEvents(string selector)
    {
        await Page.EvaluateAsync(@$"() =>
{{
    document.querySelector('{selector}').addEventListener('wheel', (e) => {{
      window['lastEvent'] = {{
        deltaX: e.deltaX,
        deltaY: e.deltaY,
        clientX: e.clientX,
        clientY: e.clientY,
        deltaMode: e.deltaMode,
        ctrlKey: e.ctrlKey,
        shiftKey: e.shiftKey,
        altKey: e.altKey,
        metaKey: e.metaKey,
      }};
    }}, {{ passive: false }});
}}");
    }
}
