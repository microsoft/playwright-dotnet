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

using System.Collections.Generic;
using System.Threading;

namespace Microsoft.Playwright.Helpers;

/// <summary>
/// Lists that getters such as <c>BrowserContext.Pages</c> hand out to user code. The dispatcher
/// thread mutates them while any thread may be enumerating, so a list is never changed once it
/// has been published: a mutation builds a new list and stores the reference. A reader sees the
/// old list or the new one, never a half-built one, and enumeration cannot throw.
/// </summary>
internal static class CopyOnWrite
{
    internal static void Add<T>(ref List<T> list, T item)
    {
        var next = new List<T>(list.Count + 1);
        next.AddRange(list);
        next.Add(item);
        Volatile.Write(ref list, next);
    }

    internal static void Remove<T>(ref List<T> list, T item)
    {
        var next = new List<T>(list);
        next.Remove(item);
        Volatile.Write(ref list, next);
    }
}
