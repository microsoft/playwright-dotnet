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
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core;

internal class RouteHandler
{
    public Regex Regex { get; set; }

    public Func<string, bool> Function { get; set; }

    public Delegate Handler { get; set; }

    public int? Times { get; internal set; }

    public int HandledCount { get; set; }

    public async Task<bool> HandleAsync(Route route)
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
}
