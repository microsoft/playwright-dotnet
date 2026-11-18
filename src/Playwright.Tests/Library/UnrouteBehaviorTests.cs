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
public class UnrouteBehaviorTests : UpstreamPageTest
{
    [UpstreamTest("RO-186 context.unroute should not wait for pending handlers to complete")]
    public async Task ContextUnrouteShouldNotWaitForPendingHandlersToComplete()
    {
        var secondHandlerCalled = false;
        await Context.RouteAsync(new Regex(".*"), async route =>
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
        await Context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;
        await Context.UnrouteAsync(new Regex(".*"), handler);
        routeBarrier.TrySetResult();
        await navigationPromise;
        Assert.That(secondHandlerCalled, Is.EqualTo(true));
    }

    [UpstreamTest("RO-187 context.unrouteAll removes all handlers")]
    public async Task ContextUnrouteAllRemovesAllHandlers()
    {
        await Context.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await Context.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await Context.UnrouteAllAsync();
        await Page.GotoAsync(Server.EmptyPage);
    }

    [UpstreamTest("RO-188 context.unrouteAll should wait for pending handlers to complete")]
    public async Task ContextUnrouteAllShouldWaitForPendingHandlersToComplete()
    {
        var secondHandlerCalled = false;
        await Context.RouteAsync(new Regex(".*"), async route =>
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
        await Context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: context.unrouteAll({ behavior: 'wait' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await Context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait });
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
    public async Task ContextUnrouteAllShouldNotWaitForPendingHandlersToCompleteIfBehaviorIsIgnoreErrors()
    {
        var secondHandlerCalled = false;
        await Context.RouteAsync(new Regex(".*"), async route =>
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
        await Context.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: context.unrouteAll({ behavior: 'ignoreErrors' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await Context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.IgnoreErrors });
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
    public async Task PageCloseShouldNotWaitForActiveRouteHandlersOnTheOwningContext()
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Context.RouteAsync(new Regex(".*"), route =>
        {
            routePromise.TrySetResult();
        });
        await Page.RouteAsync(new Regex(".*"), async route =>
        {
            await route.FallbackAsync();
        });
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        await routePromise.Task;
        await Page.CloseAsync();
    }

    [UpstreamTest("RO-191 context.close should not wait for active route handlers on the owned pages")]
    public async Task ContextCloseShouldNotWaitForActiveRouteHandlersOnTheOwnedPages()
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync(new Regex(".*"), route =>
        {
            routePromise.TrySetResult();
        });
        await Page.RouteAsync(new Regex(".*"), async route =>
        {
            await route.FallbackAsync();
        });
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        await routePromise.Task;
        await Context.CloseAsync();
    }

    [UpstreamTest("RO-192 page.unroute should not wait for pending handlers to complete")]
    public async Task PageUnrouteShouldNotWaitForPendingHandlersToComplete()
    {
        var secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), async route =>
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
        await Page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;
        await Page.UnrouteAsync(new Regex(".*"), handler);
        routeBarrier.TrySetResult();
        await navigationPromise;
        Assert.That(secondHandlerCalled, Is.EqualTo(true));
    }

    [UpstreamTest("RO-193 page.unrouteAll removes all routes")]
    public async Task PageUnrouteAllRemovesAllRoutes()
    {
        await Page.RouteAsync("**/*", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await Page.RouteAsync("**/empty.html", route =>
        {
            IgnoreErrors(route.AbortAsync());
        });
        await Page.UnrouteAllAsync();
        var response = await Page.GotoAsync(Server.EmptyPage);
        Assert.That(response.Ok, Is.EqualTo(true));
    }

    [UpstreamTest("RO-194 page.unrouteAll should wait for pending handlers to complete")]
    public async Task PageUnrouteAllShouldWaitForPendingHandlersToComplete()
    {
        var secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), async route =>
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
        await Page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: page.unrouteAll({ behavior: 'wait' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await Page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait });
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
    public async Task PageUnrouteAllShouldNotWaitForPendingHandlersToCompleteIfBehaviorIsIgnoreErrors()
    {
        var secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), async route =>
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
        await Page.RouteAsync(new Regex(".*"), handler);
        var navigationPromise = Page.GotoAsync(Server.EmptyPage);
        await routePromise.Task;
        var didUnroute = false;
        // .NET: page.unrouteAll({ behavior: 'ignoreErrors' }).then(() => didUnroute = true) is a local function that is called in its place.
        async Task Then()
        {
            await Page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.IgnoreErrors });
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
    public async Task PageCloseDoesNotWaitForActiveRouteHandlers()
    {
        var secondHandlerCalled = false;
        await Page.RouteAsync(new Regex(".*"), _ => secondHandlerCalled = true);
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        await Page.RouteAsync(new Regex(".*"), async route =>
        {
            routePromise.TrySetResult();
            await new TaskCompletionSource().Task;
        });
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        await routePromise.Task;
        await Page.CloseAsync();
        await Task.Delay(500);
        Assert.That(secondHandlerCalled, Is.EqualTo(false));
    }

    [UpstreamTest("RO-197 route.continue should not throw if page has been closed")]
    public async Task RouteContinueShouldNotThrowIfPageHasBeenClosed()
    {
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the parameter of the handler is `route` upstream, which is a variable of the test here.
        await Page.RouteAsync(new Regex(".*"), route1 =>
        {
            routePromise.TrySetResult(route1);
        });
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        var route = await routePromise.Task;
        await Page.CloseAsync();
        // Should not throw.
        await route.ContinueAsync();
    }

    [UpstreamTest("RO-198 route.fallback should not throw if page has been closed")]
    public async Task RouteFallbackShouldNotThrowIfPageHasBeenClosed()
    {
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the parameter of the handler is `route` upstream, which is a variable of the test here.
        await Page.RouteAsync(new Regex(".*"), route1 =>
        {
            routePromise.TrySetResult(route1);
        });
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        var route = await routePromise.Task;
        await Page.CloseAsync();
        // Should not throw.
        await route.FallbackAsync();
    }

    [UpstreamTest("RO-199 route.fulfill should not throw if page has been closed")]
    public async Task RouteFulfillShouldNotThrowIfPageHasBeenClosed()
    {
        var routePromise = new TaskCompletionSource<IRoute>(TaskCreationOptions.RunContinuationsAsynchronously);
        // .NET: the parameter of the handler is `route` upstream, which is a variable of the test here.
        await Page.RouteAsync(new Regex(".*"), route1 =>
        {
            routePromise.TrySetResult(route1);
        });
        IgnoreErrors(Page.GotoAsync(Server.EmptyPage));
        var route = await routePromise.Task;
        await Page.CloseAsync();
        // Should not throw.
        await route.FulfillAsync();
    }

    [UpstreamTest("RO-200 should not continue requests in flight (page)")]
    public async Task ShouldNotContinueRequestsInFlightPage()
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        await Page.GotoAsync(Server.EmptyPage);
        await Page.RouteAsync("**/*", async route =>
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
        var fetchPromise = Page.EvaluateAsync<string[]>("() => Promise.all(['/a', '/b'].map(async path => (await fetch(path)).text()))");
        await routePromise.Task;
        var bodies = await PromiseAll(Page.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait }), fetchPromise);
        Assert.That(bodies, Is.EqualTo(new[] { "mocked", "mocked" }));
    }

    [UpstreamTest("RO-201 should not continue requests in flight (context)")]
    public async Task ShouldNotContinueRequestsInFlightContext()
    {
        var routePromise = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var started = 0;
        await Page.GotoAsync(Server.EmptyPage);
        await Context.RouteAsync("**/*", async route =>
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
        var fetchPromise = Page.EvaluateAsync<string[]>("() => Promise.all(['/a', '/b'].map(async path => (await fetch(path)).text()))");
        await routePromise.Task;
        var bodies = await PromiseAll(Context.UnrouteAllAsync(new() { Behavior = UnrouteBehavior.Wait }), fetchPromise);
        Assert.That(bodies, Is.EqualTo(new[] { "mocked", "mocked" }));
    }
}
