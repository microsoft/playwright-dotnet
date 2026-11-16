/**
 * Copyright (c) Microsoft Corporation. All rights reserved.
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

using System.Text.RegularExpressions;
using Microsoft.Playwright.Tests.Fixtures;
using static Microsoft.Playwright.Tests.Fixtures.Utils;

namespace Microsoft.Playwright.Tests.Library;

/// <summary>Port of tests/library/unroute-behavior.spec.ts.</summary>
public class UnrouteBehaviorTests
{
    [UpstreamTest("RO-186 context.unroute should not wait for pending handlers to complete")]
    public async Task ContextUnrouteShouldNotWaitForPendingHandlersToComplete(IPage page, IBrowserContext context, Server server)
    {
        var secondHandlerCalled = false;
        await context.RouteAsync(new Regex(".*"), async route =>
        {
            secondHandlerCalled = true;
            await route.ContinueAsync();
        });
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var routeBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<IRoute, Task> handler = async route =>
        {
            routePromise.TrySetResult();
            await routeBarrier.Task;
            await route.FallbackAsync();
        };
        await context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = page.GotoAsync(server.EmptyPage);
        await routePromise.Task;
        await context.UnrouteAsync(new Regex(".*"), handler);
        routeBarrier.TrySetResult();
        await navigationPromise;
        Assert.That(secondHandlerCalled, Is.EqualTo(true));
    }

    [UpstreamTest("RO-187 context.unrouteAll removes all handlers")]
    public async Task ContextUnrouteAllRemovesAllHandlers(IPage page, IBrowserContext context, Server server)
    {
        await context.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await context.UnrouteAllAsync();
        await page.GotoAsync(server.EmptyPage);
    }

    [UpstreamTest("RO-188 context.unrouteAll should wait for pending handlers to complete")]
    public async Task ContextUnrouteAllShouldWaitForPendingHandlersToComplete(IPage page, IBrowserContext context, Server server)
    {
        var secondHandlerCalled = false;
        await context.RouteAsync(new Regex(".*"), async route =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync();
        });
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var routeBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<IRoute, Task> handler = async route =>
        {
            routePromise.TrySetResult();
            await routeBarrier.Task;
            await route.FallbackAsync();
        };
        await context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = page.GotoAsync(server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: context.unrouteAll({ behavior: 'wait' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait });
            didUnroute = true;
        }
        var unroutePromise = Then();
        await Task.Delay(500);
        Assert.That(didUnroute, Is.EqualTo(false));
        routeBarrier.TrySetResult();
        await unroutePromise;
        Assert.That(didUnroute, Is.EqualTo(true));
        await navigationPromise;
        Assert.That(secondHandlerCalled, Is.EqualTo(false));
    }

    [UpstreamTest("RO-189 context.unrouteAll should not wait for pending handlers to complete if behavior is ignoreErrors")]
    public async Task ContextUnrouteAllShouldNotWaitForPendingHandlersToCompleteIfBehaviorIsIgnoreErrors(IPage page, IBrowserContext context, Server server)
    {
        var secondHandlerCalled = false;
        await context.RouteAsync(new Regex(".*"), async route =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync();
        });
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var routeBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<IRoute, Task> handler = async route =>
        {
            routePromise.TrySetResult();
            await routeBarrier.Task;
            throw new Exception("Handler error");
        };
        await context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = page.GotoAsync(server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: context.unrouteAll({ behavior: 'ignoreErrors' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.IgnoreErrors });
            didUnroute = true;
        }
        var unroutePromise = Then();
        await Task.Delay(500);
        await unroutePromise;
        Assert.That(didUnroute, Is.EqualTo(true));
        routeBarrier.TrySetResult();
        await Catch(() => navigationPromise);
        // The error in the unrouted handler should be silently caught and remaining handler called.
        Assert.That(secondHandlerCalled, Is.EqualTo(false));
    }

    [UpstreamTest("RO-190 page.close should not wait for active route handlers on the owning context")]
    public async Task PageCloseShouldNotWaitForActiveRouteHandlersOnTheOwningContext(IPage page, IBrowserContext context, Server server)
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await context.RouteAsync(new Regex(".*"), route =>
        {
            routePromise.TrySetResult();
        });
        await page.RouteAsync(new Regex(".*"), async route =>
        {
            await route.FallbackAsync();
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        await routePromise.Task;
        await page.CloseAsync();
    }

    [UpstreamTest("RO-191 context.close should not wait for active route handlers on the owned pages")]
    public async Task ContextCloseShouldNotWaitForActiveRouteHandlersOnTheOwnedPages(IPage page, IBrowserContext context, Server server)
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync(new Regex(".*"), route =>
        {
            routePromise.TrySetResult();
        });
        await page.RouteAsync(new Regex(".*"), async route =>
        {
            await route.FallbackAsync();
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        await routePromise.Task;
        await context.CloseAsync();
    }

    [UpstreamTest("RO-192 page.unroute should not wait for pending handlers to complete")]
    public async Task PageUnrouteShouldNotWaitForPendingHandlersToComplete(IPage page, Server server)
    {
        var secondHandlerCalled = false;
        await page.RouteAsync(new Regex(".*"), async route =>
        {
            secondHandlerCalled = true;
            await route.ContinueAsync();
        });
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var routeBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<IRoute, Task> handler = async route =>
        {
            routePromise.TrySetResult();
            await routeBarrier.Task;
            await route.FallbackAsync();
        };
        await page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = page.GotoAsync(server.EmptyPage);
        await routePromise.Task;
        await page.UnrouteAsync(new Regex(".*"), handler);
        routeBarrier.TrySetResult();
        await navigationPromise;
        Assert.That(secondHandlerCalled, Is.EqualTo(true));
    }

    [UpstreamTest("RO-193 page.unrouteAll removes all routes")]
    public async Task PageUnrouteAllRemovesAllRoutes(IPage page, Server server)
    {
        await page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await page.UnrouteAllAsync();
        var response = await page.GotoAsync(server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("RO-194 page.unrouteAll should wait for pending handlers to complete")]
    public async Task PageUnrouteAllShouldWaitForPendingHandlersToComplete(IPage page, Server server)
    {
        var secondHandlerCalled = false;
        await page.RouteAsync(new Regex(".*"), async route =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync();
        });
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var routeBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<IRoute, Task> handler = async route =>
        {
            routePromise.TrySetResult();
            await routeBarrier.Task;
            await route.FallbackAsync();
        };
        await page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = page.GotoAsync(server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: page.unrouteAll({ behavior: 'wait' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait });
            didUnroute = true;
        }
        var unroutePromise = Then();
        await Task.Delay(500);
        Assert.That(didUnroute, Is.EqualTo(false));
        routeBarrier.TrySetResult();
        await unroutePromise;
        Assert.That(didUnroute, Is.EqualTo(true));
        await navigationPromise;
        Assert.That(secondHandlerCalled, Is.EqualTo(false));
    }

    [UpstreamTest("RO-195 page.unrouteAll should not wait for pending handlers to complete if behavior is ignoreErrors")]
    public async Task PageUnrouteAllShouldNotWaitForPendingHandlersToCompleteIfBehaviorIsIgnoreErrors(IPage page, Server server)
    {
        var secondHandlerCalled = false;
        await page.RouteAsync(new Regex(".*"), async route =>
        {
            secondHandlerCalled = true;
            await route.AbortAsync();
        });
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var routeBarrier = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Func<IRoute, Task> handler = async route =>
        {
            routePromise.TrySetResult();
            await routeBarrier.Task;
            throw new Exception("Handler error");
        };
        await page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = page.GotoAsync(server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: page.unrouteAll({ behavior: 'ignoreErrors' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.IgnoreErrors });
            didUnroute = true;
        }
        var unroutePromise = Then();
        await Task.Delay(500);
        await unroutePromise;
        Assert.That(didUnroute, Is.EqualTo(true));
        routeBarrier.TrySetResult();
        await Catch(() => navigationPromise);
        // The error in the unrouted handler should be silently caught.
        Assert.That(secondHandlerCalled, Is.EqualTo(false));
    }

    [UpstreamTest("RO-196 page.close does not wait for active route handlers")]
    public async Task PageCloseDoesNotWaitForActiveRouteHandlers(IPage page, Server server)
    {
        var secondHandlerCalled = false;
        await page.RouteAsync(new Regex(".*"), _ => secondHandlerCalled = true);
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await page.RouteAsync(new Regex(".*"), async route =>
        {
            routePromise.TrySetResult();
            await new TaskCompletionSource().Task;
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        await routePromise.Task;
        await page.CloseAsync();
        await Task.Delay(500);
        Assert.That(secondHandlerCalled, Is.EqualTo(false));
    }

    [UpstreamTest("RO-197 route.continue should not throw if page has been closed")]
    public async Task RouteContinueShouldNotThrowIfPageHasBeenClosed(IPage page, Server server)
    {
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the parameter of the handler is `route` upstream, which is a variable of the test here.
        await page.RouteAsync(new Regex(".*"), route1 =>
        {
            routePromise.TrySetResult(route1);
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        var route = await routePromise.Task;
        await page.CloseAsync();
        // Should not throw.
        await route.ContinueAsync();
    }

    [UpstreamTest("RO-198 route.fallback should not throw if page has been closed")]
    public async Task RouteFallbackShouldNotThrowIfPageHasBeenClosed(IPage page, Server server)
    {
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the parameter of the handler is `route` upstream, which is a variable of the test here.
        await page.RouteAsync(new Regex(".*"), route1 =>
        {
            routePromise.TrySetResult(route1);
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        var route = await routePromise.Task;
        await page.CloseAsync();
        // Should not throw.
        await route.FallbackAsync();
    }

    [UpstreamTest("RO-199 route.fulfill should not throw if page has been closed")]
    public async Task RouteFulfillShouldNotThrowIfPageHasBeenClosed(IPage page, Server server)
    {
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the parameter of the handler is `route` upstream, which is a variable of the test here.
        await page.RouteAsync(new Regex(".*"), route1 =>
        {
            routePromise.TrySetResult(route1);
        });
        IgnoreErrors(page.GotoAsync(server.EmptyPage));
        var route = await routePromise.Task;
        await page.CloseAsync();
        // Should not throw.
        await route.FulfillAsync();
    }

    [UpstreamTest("RO-200 should not continue requests in flight (page)")]
    [Fixme("dotnet: Page.OnRouteAsync turns the interception off when a handler ends and the routes are empty, also after unrouteAll() replaced them and still waits for the other handlers (upstream checks routeHandlers === this._routes), so the request '/b' in flight is continued: its body is 'File not found: <assets>/b'")]
    public async Task ShouldNotContinueRequestsInFlightPage(IPage page, Server server)
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        await page.GotoAsync(server.EmptyPage);
        await page.RouteAsync("**/*", async route =>
        {
            if (++started == 2)
            {
                routePromise.TrySetResult();
            }
            await routePromise.Task;
            await Task.Delay(route.Request.Url.EndsWith("/a", StringComparison.Ordinal) ? 100 : 1000);
            var response = await route.FetchAsync();
            await route.FulfillAsync(new() { Response = response, Body = "mocked" });
        });
        var fetchPromise = page.EvaluateAsync<string[]>("() => Promise.all(['/a', '/b'].map(async path => (await fetch(path)).text()))");
        await routePromise.Task;
        var bodies = await PromiseAll(page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait }), fetchPromise);
        Assert.That(bodies, Is.EqualTo(new[] { "mocked", "mocked" }));
    }

    [UpstreamTest("RO-201 should not continue requests in flight (context)")]
    [Fixme("dotnet: BrowserContext.OnRouteAsync turns the interception off when a handler ends and the routes are empty, also after unrouteAll() replaced them and still waits for the other handlers (upstream checks routeHandlers === this._routes), so the request '/b' in flight is continued: its body is 'File not found: <assets>/b'")]
    public async Task ShouldNotContinueRequestsInFlightContext(IPage page, IBrowserContext context, Server server)
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        await page.GotoAsync(server.EmptyPage);
        await context.RouteAsync("**/*", async route =>
        {
            if (++started == 2)
            {
                routePromise.TrySetResult();
            }
            await routePromise.Task;
            await Task.Delay(route.Request.Url.EndsWith("/a", StringComparison.Ordinal) ? 100 : 1000);
            var response = await route.FetchAsync();
            await route.FulfillAsync(new() { Response = response, Body = "mocked" });
        });
        var fetchPromise = page.EvaluateAsync<string[]>("() => Promise.all(['/a', '/b'].map(async path => (await fetch(path)).text()))");
        await routePromise.Task;
        var bodies = await PromiseAll(context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait }), fetchPromise);
        Assert.That(bodies, Is.EqualTo(new[] { "mocked", "mocked" }));
    }
}
