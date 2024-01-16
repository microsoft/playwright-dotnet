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

using System.Net;
using System.Text.RegularExpressions;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Tests;

public class UnrouteBehaviorTests : PageTestEx
{
    [PlaywrightTest("unroute-behavior.spec.ts", "context.unroute should not wait for pending handlers to complete")]
    public async Task ContextUnrouteShouldNotWaitForPendingHandlersToComplete()
    {
        var secondHandlerCalled = false;
        await Context.RouteAsync("**/*", async (route) =>
        {
            secondHandlerCalled = true;
            await route.ContinueAsync();
        });

        var routeEvent = new TaskCompletionSource<bool>();
        var continueRouteEvent = new TaskCompletionSource<bool>();
        async Task handler(IRoute route)
        {
            routeEvent.TrySetResult(true);
            await continueRouteEvent.Task;
            await route.FallbackAsync();
        }

        await Context.RouteAsync("**/*", handler);
        var navigationTask = Page.GotoAsync(Server.EmptyPage);
        await routeEvent.Task;
        await Context.UnrouteAsync("**/*", handler);
        continueRouteEvent.TrySetResult(true);
        await navigationTask;
        Assert.True(secondHandlerCalled);
    }

    [PlaywrightTest("context-unroute.spec.ts", "context.unrouteAll removes all handlers")]
    public async Task ContextUnrouteAllRemovesAllHandlers()
    {
        await Context.RouteAsync("**/*", route => route.AbortAsync());
        await Context.RouteAsync("**/empty.html", route => route.AbortAsync());
        await Context.UnrouteAllAsync();
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.AreEqual((int)HttpStatusCode.OK, response.Status);
    }

    [PlaywrightTest("context-unroute.spec.ts", "context.unrouteAll should wait for pending handlers to complete")]
    public async Task ContextUnrouteAllShouldWaitForPendingHandlersToComplete()
    {
        var secondHandlerCalled = false;
        await Context.RouteAsync(new Regex(".*"), async (route) =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync();
        });

        var routePromise = new TaskCompletionSource<bool>();
        var routeBarrier = new TaskCompletionSource<bool>();
        async Task handler(IRoute route)
        {
            routePromise.SetResult(true);
            await routeBarrier.Task;
            await route.FallbackAsync();
        }

        await Context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;

        bool didUnroute = false;
        var unroutePromise = Context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait }).ContinueWith(_ => didUnroute = true);
        await Task.Delay(500);
        Assert.False(didUnroute);

        routeBarrier.SetResult(true);
        await unroutePromise.ConfigureAwait(false);
        Assert.True(didUnroute);

        await navigationPromise.ConfigureAwait(false);
        Assert.False(secondHandlerCalled);
    }

    [PlaywrightTest("context-unroute.spec.ts", "context.unrouteAll should not wait for pending handlers to complete if behavior is ignoreErrors")]
    public async Task ContextUnrouteAllShouldNotWaitForPendingHandlersToCompleteIfBehaviorIsIgnoreErrors()
    {
        bool secondHandlerCalled = false;
        await Context.RouteAsync(new Regex(".*"), async (route) =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync();
        });

        var routePromise = new TaskCompletionSource<bool>();
        var routeBarrier = new TaskCompletionSource<bool>();
        async Task handler(IRoute route)
        {
            routePromise.SetResult(true);
            await routeBarrier.Task;
            throw new Exception("Handler error");
        }

        await Context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;

        bool didUnroute = false;
        var unroutePromise = Context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.IgnoreErrors }).ContinueWith(_ => didUnroute = true);
        await Task.Delay(500);
        await unroutePromise;
        Assert.True(didUnroute);

        routeBarrier.SetResult(true);
        try
        {
            await navigationPromise;
        }
        catch
        {
        }
        // The error in the unrouted handler should be silently caught and remaining handler called.
        Assert.False(secondHandlerCalled);
    }

    [PlaywrightTest("context-unroute.spec.ts", "page.close should not wait for active route handlers on the owning context")]
    public async Task PageCloseShouldNotWaitForActiveRouteHandlersOnTheOwningContext()
    {
        var routePromise = new TaskCompletionSource<bool>();
        await Context.RouteAsync(new Regex(".*"), (route) =>
        {
            routePromise.SetResult(true);
        });

        await Page.RouteAsync(new Regex(".*"), (route) =>
        {
            return route.FallbackAsync();
        });

        Page.GotoAsync(Server.EmptyPage).IgnoreException();
        await routePromise.Task;
        await Page.CloseAsync();
    }

    [PlaywrightTest("context-unroute.spec.ts", "context.close should not wait for active route handlers on the owned pages")]
    public async Task ContextCloseShouldNotWaitForActiveRouteHandlersOnTheOwnedPages()
    {
        var routePromise = new TaskCompletionSource<bool>();
        await Page.RouteAsync(new Regex(".*"), (route) =>
        {
            routePromise.SetResult(true);
            return Task.CompletedTask;
        });

        await Page.RouteAsync(new Regex(".*"), (route) =>
        {
            return route.FallbackAsync();
        });

        Page.GotoAsync(Server.EmptyPage).IgnoreException();
        await routePromise.Task;
        await Context.CloseAsync();
    }

    [PlaywrightTest("context-unroute.spec.ts", "page.unroute should not wait for pending handlers to complete")]
    public async Task PageUnrouteShouldNotWaitForPendingHandlersToComplete()
    {
        bool secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), async (route) =>
        {
            secondHandlerCalled = true;
            await route.ContinueAsync();
        });

        var routePromise = new TaskCompletionSource<bool>();
        var routeBarrier = new TaskCompletionSource<bool>();
        async Task handler(IRoute route)
        {
            routePromise.SetResult(true);
            await routeBarrier.Task;
            await route.FallbackAsync();
        };

        await Page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;
        await Page.UnrouteAsync(new Regex(".*"), handler);
        routeBarrier.SetResult(true);
        await navigationPromise;
        Assert.True(secondHandlerCalled);
    }

    [PlaywrightTest("context-unroute.spec.ts", "page.unrouteAll removes all routes")]
    public async Task PageUnrouteAllRemovesAllRoutes()
    {
        // Set up routes
        await Page.RouteAsync("**/*", async (route) =>
        {
            await route.AbortAsync().ConfigureAwait(false);
        });

        await Page.RouteAsync("**/empty.html", async (route) =>
        {
            await route.AbortAsync().ConfigureAwait(false);
        });

        await Page.UnrouteAllAsync().ConfigureAwait(false);

        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.True(response.Ok);
    }

    [PlaywrightTest("context-unroute.spec.ts", "page.unrouteAll should wait for pending handlers to complete")]
    public async Task PageUnrouteAllShouldWaitForPendingHandlersToComplete()
    {
        bool secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), async (route) =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync().ConfigureAwait(false);
        });

        var routePromise = new TaskCompletionSource<bool>();
        var routeBarrier = new TaskCompletionSource<bool>();
        async Task handler(IRoute route)
        {
            routePromise.SetResult(true);
            await routeBarrier.Task;
            await route.FallbackAsync().ConfigureAwait(false);
        };

        await Page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;

        bool didUnroute = false;
        var unroutePromise = Page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait }).ContinueWith(_ => didUnroute = true);
        await Task.Delay(500);
        Assert.False(didUnroute);

        routeBarrier.SetResult(true);
        await unroutePromise;
        Assert.True(didUnroute);

        await navigationPromise;
        Assert.False(secondHandlerCalled);
    }

    [PlaywrightTest("context-unroute.spec.ts", "page.unrouteAll should not wait for pending handlers to complete if behavior is ignoreErrors")]
    public async Task PageUnrouteAllShouldNotWaitForPendingHandlersToCompleteIfBehaviorIsIgnoreErrors()
    {
        bool secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), async (route) =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync().ConfigureAwait(false);
        });

        var routePromise = new TaskCompletionSource<bool>();
        var routeBarrier = new TaskCompletionSource<bool>();
        async Task handler(IRoute route)
        {
            routePromise.SetResult(true);
            await routeBarrier.Task;
            throw new Exception("Handler error");
        };

        await Page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;

        bool didUnroute = false;
        var unroutePromise = Page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.IgnoreErrors }).ContinueWith(_ => didUnroute = true);
        await Task.Delay(500);
        await unroutePromise;
        Assert.True(didUnroute);

        routeBarrier.SetResult(true);
        try
        {
            await navigationPromise.ConfigureAwait(false);
        }
        catch
        {
        }
        // The error in the unrouted handler should be silently caught.
        Assert.False(secondHandlerCalled);
    }

    [PlaywrightTest("context-unroute.spec.ts", "page.close does not wait for active route handlers")]
    public async Task PageCloseDoesNotWaitForActiveRouteHandlers()
    {
        bool secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), (route) =>
        {
            secondHandlerCalled = true;
            return Task.CompletedTask;
        });

        var routePromise = new TaskCompletionSource<bool>();
        await Page.RouteAsync(new Regex(".*"), async (route) =>
        {
            routePromise.SetResult(true);
            await Task.Delay(-1);
        });

        Page.GotoAsync(Server.EmptyPage).IgnoreException();
        await routePromise.Task;
        await Page.CloseAsync();
        await Task.Delay(500);
        Assert.False(secondHandlerCalled);
    }

    [PlaywrightTest("context-unroute.spec.ts", "route.continue should not throw if page has been closed")]
    public async Task RouteContinueShouldNotThrowIfPageHasBeenClosed()
    {
        var routePromise = new TaskCompletionSource<IRoute>();
        await Page.RouteAsync(new Regex(".*"), (route) =>
        {
            routePromise.SetResult(route);
            return Task.CompletedTask;
        });

        Page.GotoAsync(Server.EmptyPage).IgnoreException();
        var route = await routePromise.Task;
        await Page.CloseAsync();
        await route.ContinueAsync();
    }

    [PlaywrightTest("context-unroute.spec.ts", "route.fallback should not throw if page has been closed")]
    public async Task RouteFallbackShouldNotThrowIfPageHasBeenClosed()
    {
        var routePromise = new TaskCompletionSource<IRoute>();
        await Page.RouteAsync(new Regex(".*"), (route) =>
        {
            routePromise.SetResult(route);
            return Task.CompletedTask;
        });

        Page.GotoAsync(Server.EmptyPage).IgnoreException();
        var route = await routePromise.Task;
        await Page.CloseAsync();
        await route.FallbackAsync();
    }

    [PlaywrightTest("context-unroute.spec.ts", "route.fulfill should not throw if page has been closed")]
    public async Task RouteFulfillShouldNotThrowIfPageHasBeenClosed()
    {
        var routePromise = new TaskCompletionSource<IRoute>();
        await Page.RouteAsync(new Regex(".*"), (route) =>
        {
            routePromise.SetResult(route);
            return Task.CompletedTask;
        });

        Page.GotoAsync(Server.EmptyPage).IgnoreException();
        var route = await routePromise.Task;
        await Page.CloseAsync();
        await route.FulfillAsync(new() { Status = (int)HttpStatusCode.OK });
    }
}
