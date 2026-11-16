/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
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

/// <summary>Port of tests/page/wheel.spec.ts.</summary>
public class WheelTests
{
    [BeforeEach]
    public static void Skip(bool isAndroid) => It.Skip(isAndroid);

    private static bool ignoreDelta = false;

    [BeforeAll]
    public static void BeforeAll(string browserName, bool isElectron, string platform)
    {
        if (((browserName == "chromium") || isElectron) && platform == "darwin")
        {
            // Chromium reports deltaX/deltaY scaled by host device scale factor.
            // https://bugs.chromium.org/p/chromium/issues/detail?id=1324819
            // https://github.com/microsoft/playwright/issues/7362
            // Different bots have different scale factors (usually 1 or 2), so we just ignore the values
            // instead of guessing the host scale factor.
            ignoreDelta = true;
        }
    }

    private static async Task ExpectEvent(IPage page, Dictionary<string, object> expected)
    {
        Dictionary<string, object> received = null;
        await Expect(async () =>
        {
            received = await page.EvaluateAsync<Dictionary<string, object>>("window.lastEvent");
            Assert.That(received, Is.Not.Null);
        }).ToPassAsync();
        if (ignoreDelta)
        {
            received.Remove("deltaX");
            received.Remove("deltaY");
            expected.Remove("deltaX");
            expected.Remove("deltaY");
        }
        ExpectToEqual(received, expected);
    }

    [UpstreamTest("MO-017 should dispatch wheel events @smoke")]
    public async Task ShouldDispatchWheelEvents(IPage page, Server server)
    {
        await page.SetContentAsync("<div style=\"width: 5000px; height: 5000px;\"></div>");
        await page.Mouse.MoveAsync(50, 60);
        await ListenForWheelEvents(page, "div");
        await page.Mouse.WheelAsync(0, 100);
        await page.WaitForFunctionAsync("window.scrollY === 100");
        await ExpectEvent(page, new()
        {
            ["deltaX"] = 0,
            ["deltaY"] = 100,
            ["clientX"] = 50,
            ["clientY"] = 60,
            ["deltaMode"] = 0,
            ["ctrlKey"] = false,
            ["shiftKey"] = false,
            ["altKey"] = false,
            ["metaKey"] = false,
        });
    }

    [UpstreamTest("MO-018 should dispatch wheel events after popup was opened @smoke")]
    public async Task ShouldDispatchWheelEventsAfterPopupWasOpened(IPage page, Server server)
    {
        await page.SetContentAsync(@"
    <div style=""width: 5000px; height: 5000px;""></div>
  ");
        await page.Mouse.MoveAsync(50, 60);
        await ListenForWheelEvents(page, "div");
        await PromiseAll(
            page.WaitForPopupAsync(),
            page.EvaluateAsync("() => window.open('')"));
        await page.Mouse.WheelAsync(0, 100);
        await page.WaitForFunctionAsync("window.scrollY === 100");
        await ExpectEvent(page, new()
        {
            ["deltaX"] = 0,
            ["deltaY"] = 100,
            ["clientX"] = 50,
            ["clientY"] = 60,
            ["deltaMode"] = 0,
            ["ctrlKey"] = false,
            ["shiftKey"] = false,
            ["altKey"] = false,
            ["metaKey"] = false,
        });
    }

    [UpstreamTest("MO-019 should dispatch wheel event on svg element")]
    public async Task ShouldDispatchWheelEventOnSvgElement(IPage page, string browserName, bool headless, bool isLinux)
    {
        await page.SetContentAsync(@"
    <body>
      <svg class=""scroll-box""></svg>
    </body>
    <style>
      .scroll-box {
        position: absolute;
        top: 0px;
        left: 0px;
        background-color: brown;
        width: 200px;
        height: 200px;
      }
    </style>");
        await ListenForWheelEvents(page, "svg");
        await page.Mouse.MoveAsync(100, 100);
        await page.Mouse.WheelAsync(0, 100);
        await page.WaitForFunctionAsync("!!window.lastEvent");
        await ExpectEvent(page, new()
        {
            ["deltaX"] = 0,
            ["deltaY"] = 100,
            ["clientX"] = 100,
            ["clientY"] = 100,
            ["deltaMode"] = 0,
            ["ctrlKey"] = false,
            ["shiftKey"] = false,
            ["altKey"] = false,
            ["metaKey"] = false,
        });
    }

    [UpstreamTest("MO-020 should scroll when nobody is listening")]
    public async Task ShouldScrollWhenNobodyIsListening(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/scrollable.html");
        await page.Mouse.MoveAsync(50, 60);
        await page.Mouse.WheelAsync(0, 100);
        await page.WaitForFunctionAsync("window.scrollY === 100");
    }

    [UpstreamTest("MO-021 should set the modifiers")]
    public async Task ShouldSetTheModifiers(IPage page)
    {
        await page.SetContentAsync("<div style=\"width: 5000px; height: 5000px;\"></div>");
        await page.Mouse.MoveAsync(50, 60);
        await ListenForWheelEvents(page, "div");
        await page.Keyboard.DownAsync("Shift");
        await page.Mouse.WheelAsync(0, 100);
        await ExpectEvent(page, new()
        {
            ["deltaX"] = 0,
            ["deltaY"] = 100,
            ["clientX"] = 50,
            ["clientY"] = 60,
            ["deltaMode"] = 0,
            ["ctrlKey"] = false,
            ["shiftKey"] = true,
            ["altKey"] = false,
            ["metaKey"] = false,
        });
    }

    [UpstreamTest("MO-022 should scroll horizontally")]
    public async Task ShouldScrollHorizontally(IPage page)
    {
        await page.SetContentAsync("<div style=\"width: 5000px; height: 5000px;\"></div>");
        await page.Mouse.MoveAsync(50, 60);
        await ListenForWheelEvents(page, "div");
        await page.Mouse.WheelAsync(100, 0);
        await ExpectEvent(page, new()
        {
            ["deltaX"] = 100,
            ["deltaY"] = 0,
            ["clientX"] = 50,
            ["clientY"] = 60,
            ["deltaMode"] = 0,
            ["ctrlKey"] = false,
            ["shiftKey"] = false,
            ["altKey"] = false,
            ["metaKey"] = false,
        });
        await page.WaitForFunctionAsync("window.scrollX === 100");
    }

    [UpstreamTest("MO-023 should work when the event is canceled")]
    public async Task ShouldWorkWhenTheEventIsCanceled(IPage page)
    {
        await page.SetContentAsync("<div style=\"width: 5000px; height: 5000px;\"></div>");
        await page.Mouse.MoveAsync(50, 60);
        await ListenForWheelEvents(page, "div");
        await page.EvaluateAsync(@"() => {
    document.querySelector('div').addEventListener('wheel', e => e.preventDefault());
  }");
        // Give wheel listener a chance to propagate through all the layers in Firefox.
        await Rafraf(page, 10);
        await page.Mouse.WheelAsync(0, 100);
        await ExpectEvent(page, new()
        {
            ["deltaX"] = 0,
            ["deltaY"] = 100,
            ["clientX"] = 50,
            ["clientY"] = 60,
            ["deltaMode"] = 0,
            ["ctrlKey"] = false,
            ["shiftKey"] = false,
            ["altKey"] = false,
            ["metaKey"] = false,
        });
        // Give the page a chance to scroll.
        await page.WaitForFunctionAsync("!!window['lastEvent']");
        // Ensure that it did not.
        Assert.That(await page.EvaluateAsync<int>("window.scrollY"), Is.EqualTo(0));
    }

    private static async Task ListenForWheelEvents(IPage page, string selector)
    {
        await page.EvaluateAsync(@"selector => {
    document.querySelector(selector).addEventListener('wheel', event => {
      const e = event;
      window['lastEvent'] = {
        deltaX: e.deltaX,
        deltaY: e.deltaY,
        clientX: e.clientX,
        clientY: e.clientY,
        deltaMode: e.deltaMode,
        ctrlKey: e.ctrlKey,
        shiftKey: e.shiftKey,
        altKey: e.altKey,
        metaKey: e.metaKey,
      };
    }, { passive: false });
  }", selector);
    }
}
