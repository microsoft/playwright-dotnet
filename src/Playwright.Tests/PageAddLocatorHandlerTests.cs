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

using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Tests;

public class PageAddLocatorHandlerTests : PageTestEx
{
    [PlaywrightTest("page-add-locator-handler.spec.ts", "should work")]
    public async Task ShouldWork()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        var beforeCount = 0;
        var afterCount = 0;
        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            ++beforeCount;
            await Page.Locator("#close").ClickAsync();
            ++afterCount;
        });

        foreach (var args in new[]
        {
            new object[] { "mouseover", 1 },
            ["mouseover", 1, "capture"],
            ["mouseover", 2],
            ["mouseover", 2, "capture"],
            ["pointerover", 1],
            ["pointerover", 1, "capture"],
            ["none", 1],
            ["remove", 1],
            ["hide", 1],
        })
        {
            await Page.Locator("#aside").HoverAsync();
            beforeCount = 0;
            afterCount = 0;
            await Page.EvaluateAsync(@"args =>
            {
                window.clicked = 0;
                window.setupAnnoyingInterstitial(...args);
            }", args);
            Assert.AreEqual(0, beforeCount);
            Assert.AreEqual(0, afterCount);
            await Page.Locator("#target").ClickAsync();
            Assert.AreEqual(args[1], beforeCount);
            Assert.AreEqual(args[1], afterCount);
            Assert.AreEqual(1, await Page.EvaluateAsync<int>("window.clicked"));
            await Expect(Page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        }
    }

    [PlaywrightTest("page-add-locator-handler.spec.ts", "should work with a custom check")]
    public async Task ShouldWorkWithCustomCheck()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            if (await Page.GetByText("This interstitial covers the button").IsVisibleAsync())
            {
                await Page.Locator("#close").ClickAsync();
            }
        });

        foreach (var args in new[]
        {
        new object[] { "mouseover", 2 },
        ["none", 1],
        ["remove", 1],
        ["hide", 1],
    })
        {
            await Page.Locator("#aside").HoverAsync();
            await Page.EvaluateAsync(@"args =>
        {
            window.clicked = 0;
            window.setupAnnoyingInterstitial(...args);
        }", args);
            await Page.Locator("#target").ClickAsync();
            Assert.AreEqual(1, await Page.EvaluateAsync<int>("window.clicked"));
            await Expect(Page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        }

    }

    [PlaywrightTest("page-add-locator-handler.spec.ts", "should work with locator.hover()")]
    public async Task ShouldWorkWithLocatorHover()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            await Page.Locator("#close").ClickAsync();
        });

        await Page.Locator("#aside").HoverAsync();
        await Page.EvaluateAsync(@"() =>
        {
            window.setupAnnoyingInterstitial('pointerover', 1, 'capture');
        }");
        await Page.Locator("#target").HoverAsync();
        await Expect(Page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        Assert.AreEqual("rgb(255, 255, 0)", await Page.EvalOnSelectorAsync<string>("#target", "e => window.getComputedStyle(e).backgroundColor"));

    }

    [PlaywrightTest("page-add-locator-handler.spec.ts", "should not work with force:true")]
    public async Task ShouldNotWorkWithForceTrue()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            await Page.Locator("#close").ClickAsync();
        });

        await Page.Locator("#aside").HoverAsync();
        await Page.EvaluateAsync(@"() =>
        {
            window.setupAnnoyingInterstitial('none', 1);
        }");
        await Page.Locator("#target").ClickAsync(new() { Force = true, Timeout = 2000 });
        Assert.True(await Page.Locator("#interstitial").IsVisibleAsync());
        Assert.AreEqual(null, await Page.EvaluateAsync("window.clicked"));
    }

    [PlaywrightTest("page-add-locator-handler.spec.ts", "should throw when page closes")]
    public async Task ShouldThrowWhenPageCloses()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            await Page.CloseAsync();
        });

        await Page.Locator("#aside").HoverAsync();
        await Page.EvaluateAsync(@"() =>
        {
            window.clicked = 0;
            window.setupAnnoyingInterstitial('mouseover', 1);
        }");
        var error = await PlaywrightAssert.ThrowsAsync<PlaywrightException>(() => Page.Locator("#target").ClickAsync());
        StringAssert.Contains(DriverMessages.TargetClosedExceptionMessage, error.Message);
    }

    [PlaywrightTest("page-add-locator-handler.spec.ts", "should throw when handler times out")]
    public async Task ShouldWorkWhenHandlerTimesOut()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            ++called;
            // Deliberately timeout.
            await Task.Delay(int.MaxValue);
        });

        await Page.Locator("#aside").HoverAsync();
        await Page.EvaluateAsync(@"() =>
        {
            window.clicked = 0;
            window.setupAnnoyingInterstitial('mouseover', 1);
        }");
        var error = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        StringAssert.Contains("Timeout 3000ms exceeded", error.Message);

        var error2 = await PlaywrightAssert.ThrowsAsync<TimeoutException>(() => Page.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        StringAssert.Contains("Timeout 3000ms exceeded", error2.Message);

        // Should not enter the same handler while it is still running.
        Assert.AreEqual(1, called);
    }

    [PlaywrightTest("page-add-locator-handler.spec.ts", "should work with toBeVisible")]
    public async Task ShouldWorkWithToBeVisible()
    {
        await Page.GotoAsync(Server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await Page.AddLocatorHandlerAsync(Page.GetByText("This interstitial covers the button"), async () =>
        {
            ++called;
            await Page.Locator("#close").ClickAsync();
        });

        await Page.EvaluateAsync(@"() =>
        {
            window.clicked = 0;
            window.setupAnnoyingInterstitial('remove', 1);
        }");
        await Expect(Page.Locator("#target")).ToBeVisibleAsync();
        await Expect(Page.Locator("#interstitial")).Not.ToBeVisibleAsync();
    }
}
