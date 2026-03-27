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
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Core;

internal class Overlay : IOverlay
{
    private readonly Page _page;

    internal Overlay(Page page)
    {
        _page = page;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public async Task<IAsyncDisposable> ShowAsync(string html, OverlayShowOptions? options = default)
    {
        var result = await _page.SendMessageToServerAsync("overlayShow", new Dictionary<string, object?>
        {
            ["html"] = html,
            ["duration"] = options?.Duration,
        }).ConfigureAwait(false);
        var id = result.Value.GetProperty("id").GetString()!;
        return new DisposableStub(async () =>
        {
            await _page.SendMessageToServerAsync("overlayRemove", new Dictionary<string, object?>
            {
                ["id"] = id,
            }).ConfigureAwait(false);
        });
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task ChapterAsync(string title, OverlayChapterOptions? options = default) =>
        _page.SendMessageToServerAsync("overlayChapter", new Dictionary<string, object?>
        {
            ["title"] = title,
            ["description"] = options?.Description,
            ["duration"] = options?.Duration,
        });

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Task SetVisibleAsync(bool visible) =>
        _page.SendMessageToServerAsync("overlaySetVisible", new Dictionary<string, object?>
        {
            ["visible"] = visible,
        });
}
