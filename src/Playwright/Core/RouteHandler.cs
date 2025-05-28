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
#nullable enable

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class RouteHandler
{
    private readonly IDictionary<HandlerInvocation, bool> _activeInvocations = new ConcurrentDictionary<HandlerInvocation, bool>();

    private bool _ignoreException;

    public URLMatch urlMatcher { get; set; } = null!;

    public Delegate Handler { get; set; } = null!;

    public int? Times { get; internal set; }

    public int HandledCount { get; set; }

    public static List<Dictionary<string, object>> PrepareInterceptionPatterns(List<RouteHandler> handlers)
    {
        bool all = false;
        var patterns = new List<Dictionary<string, object>>();
        foreach (var handler in handlers)
        {
            var pattern = new Dictionary<string, object>();
            patterns.Add(pattern);

            if (!string.IsNullOrEmpty(handler.urlMatcher.glob) && handler.urlMatcher.glob != null)
            {
                pattern["glob"] = handler.urlMatcher.glob;
            }
            else if (handler.urlMatcher.re != null)
            {
                pattern["regexSource"] = handler.urlMatcher.re.ToString();
                pattern["regexFlags"] = handler.urlMatcher.re.Options.GetInlineFlags();
            }

            if (handler.urlMatcher.func != null)
            {
                all = true;
            }
        }

        if (all)
        {
            return [
                new Dictionary<string, object>
                {
                    ["glob"] = "**/*",
                }
            ];
        }

        return patterns;
    }

    public async Task<bool> HandleAsync(Route route)
    {
        var handlerInvocation = new HandlerInvocation
        {
            Complete = new TaskCompletionSource<bool>(),
            Route = route,
        };
        _activeInvocations[handlerInvocation] = false;
        try
        {
            return await HandleInternalAsync(route).ConfigureAwait(false);
        }
        catch (Exception)
        {
            // If the handler was stopped (without waiting for completion), we ignore all exceptions.
            if (_ignoreException)
            {
                return false;
            }
            throw;
        }
        finally
        {
            handlerInvocation.Complete.SetResult(true);
            _activeInvocations.Remove(handlerInvocation);
        }
    }

    public async Task StopAsync(UnrouteBehavior behavior)
    {
        // When a handler is manually unrouted or its page/context is closed we either
        // - wait for the current handler invocations to finish
        // - or do not wait, if the user opted out of it, but swallow all exceptions
        //   that happen after the unroute/close.
        if (behavior == UnrouteBehavior.IgnoreErrors)
        {
            _ignoreException = true;
        }
        else
        {
            var tasks = new List<Task>();
            foreach (var activation in _activeInvocations.Keys)
            {
                if (!activation.Route._didThrow)
                {
                    tasks.Add(activation.Complete.Task);
                }
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }
    }


    private async Task<bool> HandleInternalAsync(Route route)
    {
        ++HandledCount;
        var handledTask = route.StartHandlingAsync();
        var maybeTask = Handler.DynamicInvoke(new object[] { route });
        if (maybeTask is Task task)
        {
            await task.ConfigureAwait(false);
        }
        return await handledTask.ConfigureAwait(false);
    }

    internal bool Matches(string normalisedUrl) => urlMatcher.Match(normalisedUrl);

    public bool WillExpire()
    {
        return HandledCount + 1 >= Times;
    }

    internal class HandlerInvocation
    {
        public TaskCompletionSource<bool> Complete { get; set; } = new TaskCompletionSource<bool>();

        public Route Route { get; set; } = null!;
    }
}
