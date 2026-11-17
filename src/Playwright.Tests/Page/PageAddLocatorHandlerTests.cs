/**
 * Copyright (c) Microsoft Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
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

/// <summary>Port of tests/page/page-add-locator-handler.spec.ts.</summary>
public class PageAddLocatorHandlerTests
{
    [UpstreamTest("LH-001 should work")]
    public async Task ShouldWork(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var beforeCount = 0;
        var afterCount = 0;
        var originalLocator = page.GetByText("This interstitial covers the button");
        await page.AddLocatorHandlerAsync(originalLocator, async locatorArgument =>
        {
            Assert.That(locatorArgument, Is.SameAs(originalLocator));
            ++beforeCount;
            await page.Locator("#close").ClickAsync();
            ++afterCount;
        });

        foreach (var args in new object[][]
        {
            ["mouseover", 1],
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
            // .NET: args[2] is undefined when the array has two items.
            await Info.Step($"{args[0]}{((string)args.ElementAtOrDefault(2) == "capture" ? " with capture" : "")} {args[1]} times", async () =>
            {
                await page.Locator("#aside").HoverAsync();
                beforeCount = 0;
                afterCount = 0;
                await page.EvaluateAsync(@"args => {
        window.clicked = 0;
        window.setupAnnoyingInterstitial(...args);
      }", args);
                Assert.That(beforeCount, Is.EqualTo(0));
                Assert.That(afterCount, Is.EqualTo(0));
                await page.Locator("#target").ClickAsync();
                Assert.That(beforeCount, Is.EqualTo(args[1]));
                Assert.That(afterCount, Is.EqualTo(args[1]));
                Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
                await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();
            });
        }
    }

    [UpstreamTest("LH-002 should work with a custom check")]
    public async Task ShouldWorkWithACustomCheck(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        await page.AddLocatorHandlerAsync(page.Locator("body"), async () =>
        {
            if (await page.GetByText("This interstitial covers the button").IsVisibleAsync())
            {
                await page.Locator("#close").ClickAsync();
            }
        }, new() { NoWaitAfter = true });

        foreach (var args in new object[][]
        {
            ["mouseover", 2],
            ["none", 1],
            ["remove", 1],
            ["hide", 1],
        })
        {
            // .NET: args[2] is undefined when the array has two items.
            await Info.Step($"{args[0]}{((string)args.ElementAtOrDefault(2) == "capture" ? " with capture" : "")} {args[1]} times", async () =>
            {
                await page.Locator("#aside").HoverAsync();
                await page.EvaluateAsync(@"args => {
        window.clicked = 0;
        window.setupAnnoyingInterstitial(...args);
      }", args);
                await page.Locator("#target").ClickAsync();
                Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
                await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();
            });
        }
    }

    [UpstreamTest("LH-003 should work with locator.hover()")]
    public async Task ShouldWorkWithLocatorHover(IPage page, Server server, bool headless)
    {
        Info.Skip(!headless, "Stray hovers in headed mode");

        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        await page.AddLocatorHandlerAsync(page.GetByText("This interstitial covers the button"), async () =>
        {
            await page.Locator("#close").ClickAsync();
        });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.setupAnnoyingInterstitial('pointerover', 1, 'capture');
  }");
        await page.Locator("#target").HoverAsync();
        await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        Assert.That(await page.EvalOnSelectorAsync<string>("#target", "e => window.getComputedStyle(e).backgroundColor"), Is.EqualTo("rgb(255, 255, 0)"));
    }

    [UpstreamTest("LH-004 should not work with force:true")]
    public async Task ShouldNotWorkWithForceTrue(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        await page.AddLocatorHandlerAsync(page.GetByText("This interstitial covers the button"), async () =>
        {
            await page.Locator("#close").ClickAsync();
        });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.setupAnnoyingInterstitial('none', 1);
  }");
        await page.Locator("#target").ClickAsync(new() { Force = true, Timeout = 2000 });
        Assert.That(await page.Locator("#interstitial").IsVisibleAsync(), Is.EqualTo(true));
        Assert.That(await page.EvaluateAsync<object>("window.clicked"), Is.Null);
    }

    [UpstreamTest("LH-005 should throw when handler times out")]
    public async Task ShouldThrowWhenHandlerTimesOut(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        var releaseHandler = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var handlerFinished = new TaskCompletionSource<Exception>(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.AddLocatorHandlerAsync(page.GetByText("This interstitial covers the button"), async () =>
        {
            ++called;
            // Deliberately timeout.
            await releaseHandler.Task;
            handlerFinished.SetResult(await Catch(() => page.Locator("#close").ClickAsync(new() { Timeout = 3000 })));
        });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('mouseover', 1);
  }");
        var error = await Catch(() => page.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded"));

        var error2 = await Catch(() => page.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(error2.Message, Does.Contain("Timeout 3000ms exceeded"));

        // Should not enter the same handler while it is still running.
        Assert.That(called, Is.EqualTo(1));

        releaseHandler.SetResult();
        Assert.That(await handlerFinished.Task, Is.Null);
    }

    [UpstreamTest("LH-006 should work with toBeVisible")]
    public async Task ShouldWorkWithToBeVisible(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.GetByText("This interstitial covers the button"), async () =>
        {
            ++called;
            await page.Locator("#close").ClickAsync();
        });

        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('remove', 1);
  }");
        await Expect(page.Locator("#target")).ToBeVisibleAsync();
        await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        Assert.That(called, Is.EqualTo(1));
    }

    [UpstreamTest("LH-007 should work with locator.waitFor")]
    public async Task ShouldWorkWithLocatorWaitFor(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.GetByText("This interstitial covers the button"), async () =>
        {
            ++called;
            await page.Locator("#close").ClickAsync();
        });

        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('remove', 1);
  }");
        await page.Locator("#target").WaitForAsync();
        await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        Assert.That(called, Is.EqualTo(1));
    }

    [UpstreamTest("LH-009 should work when owner frame detaches")]
    public async Task ShouldWorkWhenOwnerFrameDetaches(IPage page, Server server)
    {
        await page.GotoAsync(server.EmptyPage);

        await page.EvaluateAsync(@"() => {
    const iframe = document.createElement('iframe');
    iframe.src = 'data:text/html,<body>hello from iframe</body>';
    document.body.append(iframe);

    const target = document.createElement('button');
    target.textContent = 'Click me';
    target.id = 'target';
    target.addEventListener('click', () => window._clicked = true);
    document.body.appendChild(target);

    const closeButton = document.createElement('button');
    closeButton.textContent = 'close';
    closeButton.id = 'close';
    closeButton.addEventListener('click', () => iframe.remove());
    document.body.appendChild(closeButton);
  }");

        await page.AddLocatorHandlerAsync(page.FrameLocator("iframe").Locator("body"), async () =>
        {
            await page.Locator("#close").ClickAsync();
        });

        await page.Locator("#target").ClickAsync();
        Assert.That(await page.QuerySelectorAsync("iframe"), Is.Null);
        Assert.That(await page.EvaluateAsync<bool>("window._clicked"), Is.EqualTo(true));
    }

    [UpstreamTest("LH-010 should work with times: option")]
    public async Task ShouldWorkWithTimesOption(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.Locator("body"), async () =>
        {
            ++called;
        }, new() { NoWaitAfter = true, Times = 2 });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('mouseover', 4);
  }");
        var error = await Catch(() => page.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(called, Is.EqualTo(2));
        Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(0));
        await Expect(page.Locator("#interstitial")).ToBeVisibleAsync();
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded"));
        Assert.That(error.Message, Does.Contain("<div>This interstitial covers the button</div> from <div class=\"visible\" id=\"interstitial\">…</div> subtree intercepts pointer events"));
    }

    [UpstreamTest("LH-011 should wait for hidden by default")]
    public async Task ShouldWaitForHiddenByDefault(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.GetByRole(AriaRole.Button, new() { Name = "close" }), async button =>
        {
            called++;
            await button.ClickAsync();
        });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('timeout', 1);
  }");
        await page.Locator("#target").ClickAsync();
        Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
        await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        Assert.That(called, Is.EqualTo(1));
    }

    [UpstreamTest("LH-012 should wait for hidden by default 2")]
    [LocalFixme("dotnet: the call log has GetByRole(AriaRole.Button, new() { Name = \"close\" }), upstream expects getByRole('button', { name: 'close' })")]
    public async Task ShouldWaitForHiddenByDefault2(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.GetByRole(AriaRole.Button, new() { Name = "close" }), async button =>
        {
            called++;
        });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('hide', 1);
  }");
        var error = await Catch(() => page.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(0));
        Assert.That(await page.Locator("#interstitial").IsVisibleAsync(), Is.EqualTo(true));
        Assert.That(called, Is.EqualTo(1));
        Assert.That(error.Message, Does.Contain("locator handler has finished, waiting for getByRole('button', { name: 'close' }) to be hidden"));
    }

    [UpstreamTest("LH-013 should work with noWaitAfter")]
    public async Task ShouldWorkWithNoWaitAfter(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.GetByRole(AriaRole.Button, new() { Name = "close" }), async button =>
        {
            called++;
            if (called == 1)
            {
                await button.ClickAsync();
            }
            else
            {
                await page.Locator("#interstitial").WaitForAsync(new() { State = WaitForSelectorState.Hidden });
            }
        }, new() { NoWaitAfter = true });

        await page.Locator("#aside").HoverAsync();
        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('timeout', 1);
  }");
        await page.Locator("#target").ClickAsync();
        Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
        await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();
        Assert.That(called, Is.EqualTo(2));
    }

    [UpstreamTest("LH-014 should removeLocatorHandler")]
    public async Task ShouldRemoveLocatorHandler(IPage page, Server server)
    {
        await page.GotoAsync(server.Prefix + "/input/handle-locator.html");

        var called = 0;
        await page.AddLocatorHandlerAsync(page.GetByRole(AriaRole.Button, new() { Name = "close" }), async locator =>
        {
            ++called;
            await locator.ClickAsync();
        });

        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('hide', 1);
  }");
        await page.Locator("#target").ClickAsync();
        Assert.That(called, Is.EqualTo(1));
        Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(1));
        await Expect(page.Locator("#interstitial")).Not.ToBeVisibleAsync();

        await page.EvaluateAsync(@"() => {
    window.clicked = 0;
    window.setupAnnoyingInterstitial('hide', 1);
  }");
        await page.RemoveLocatorHandlerAsync(page.GetByRole(AriaRole.Button, new() { Name = "close" }));

        var error = await Catch(() => page.Locator("#target").ClickAsync(new() { Timeout = 3000 }));
        Assert.That(called, Is.EqualTo(1));
        Assert.That(await page.EvaluateAsync<int>("window.clicked"), Is.EqualTo(0));
        await Expect(page.Locator("#interstitial")).ToBeVisibleAsync();
        Assert.That(error.Message, Does.Contain("Timeout 3000ms exceeded"));
    }
}
