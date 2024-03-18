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

using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class RouteHandler
{
    private readonly object _activeInvocationsLock = new object();
    private readonly HashSet<HandlerInvocation> _activeInvocations = new HashSet<HandlerInvocation>();

    private bool _ignoreException;

    public Regex Regex { get; set; }

    public Func<string, bool> Function { get; set; }

    public Delegate Handler { get; set; }

    public int? Times { get; internal set; }

    public int HandledCount { get; set; }

    public static Dictionary<string, object> PrepareInterceptionPatterns(List<RouteHandler> handlers)
    {
        bool all = false;
        var patterns = new List<Dictionary<string, object>>();
        foreach (var handler in handlers)
        {
            var pattern = new Dictionary<string, object>();
            patterns.Add(pattern);

            if (handler.Regex != null)
            {
                pattern["regexSource"] = handler.Regex.ToString();
                pattern["regexFlags"] = handler.Regex.Options.GetInlineFlags();
            }

            if (handler.Function != null)
            {
                all = true;
            }
        }

        if (all)
        {
            var allPattern = new Dictionary<string, object>();
            allPattern["glob"] = "**/*";

            patterns.Clear();
            patterns.Add(allPattern);
        }

        var result = new Dictionary<string, object>();
        result["patterns"] = patterns;
        return result;
    }

    public async Task<bool> HandleAsync(Route route)
    {
        var handlerInvocation = new HandlerInvocation
        {
            Complete = new TaskCompletionSource<bool>(),
            Route = route,
        };
        lock (_activeInvocationsLock)
        {
            _activeInvocations.Add(handlerInvocation);
        }
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
            lock (_activeInvocationsLock)
            {
                _activeInvocations.Remove(handlerInvocation);
            }
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
            lock (_activeInvocationsLock)
            {
                foreach (var activation in _activeInvocations)
                {
                    if (!activation.Route.DidThrow)
                    {
                        tasks.Add(activation.Complete.Task);
                    }
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

    public bool WillExpire()
    {
        return HandledCount + 1 >= Times;
    }

    internal class HandlerInvocation
    {
        public TaskCompletionSource<bool> Complete { get; set; } = new TaskCompletionSource<bool>();

        public Route Route { get; set; }
    }
}
