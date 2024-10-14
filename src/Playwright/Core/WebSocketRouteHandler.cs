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

internal class WebSocketRouteHandler
{
    public Regex Regex { get; set; }

    public Func<string, bool> Function { get; set; }

    public Delegate Handler { get; set; }

    public static Dictionary<string, object> PrepareInterceptionPatterns(List<WebSocketRouteHandler> handlers)
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
            var allPattern = new Dictionary<string, object>
            {
                ["glob"] = "**/*",
            };

            patterns.Clear();
            patterns.Add(allPattern);
        }

        return new Dictionary<string, object>
        {
            ["patterns"] = patterns,
        };
    }

    public async Task HandleAsync(WebSocketRoute route)
    {
        var maybeTask = Handler.DynamicInvoke(new object[] { route });
        if (maybeTask is Task task)
        {
            await task.ConfigureAwait(false);
        }
        await route.AfterHandleAsync().ConfigureAwait(false);
    }
}
