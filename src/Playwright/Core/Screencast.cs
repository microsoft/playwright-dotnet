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
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;

namespace Microsoft.Playwright.Core;

internal class Screencast : IScreencast
{
    private readonly Page _page;
    private bool _started;
    private string? _savePath;
    private Func<ScreencastFrame, Task>? _onFrame;
    private Artifact? _artifact;

    public Screencast(Page page)
    {
        _page = page;
    }

    internal void OnScreencastFrame(byte[] data)
    {
        var handler = _onFrame;
        if (handler != null)
        {
            _ = handler(new ScreencastFrame { Data = data });
        }
    }

    public async Task<IAsyncDisposable> StartAsync(ScreencastStartOptions? options = default)
    {
        if (_started)
        {
            throw new PlaywrightException("Screencast is already started");
        }
        _started = true;
        options ??= new();
        if (options.OnFrame != null)
        {
            _onFrame = options.OnFrame;
        }
        var result = await _page.SendMessageToServerAsync("screencastStart", new Dictionary<string, object?>
        {
            ["quality"] = options.Quality,
            ["sendFrames"] = options.OnFrame != null,
            ["record"] = options.Path != null,
        }).ConfigureAwait(false);
        var artifact = result.GetObject<Artifact>("artifact", _page._connection);
        if (artifact != null)
        {
            _artifact = artifact;
            _savePath = options.Path;
        }
        return new DisposableStub(() => StopAsync());
    }

    public async Task StopAsync()
    {
        await _page.WrapApiCallAsync(async () =>
        {
            _started = false;
            _onFrame = null;
            await _page.SendMessageToServerAsync("screencastStop").ConfigureAwait(false);
            if (_savePath != null)
            {
                await _artifact!.SaveAsAsync(_savePath).ConfigureAwait(false);
            }
            _artifact = null;
            _savePath = null;
        }).ConfigureAwait(false);
    }

    public async Task<IAsyncDisposable> ShowOverlayAsync(string html, ScreencastShowOverlayOptions? options = default)
    {
        var result = await _page.SendMessageToServerAsync("overlayShow", new Dictionary<string, object?>
        {
            ["html"] = html,
            ["duration"] = options?.Duration,
        }).ConfigureAwait(false);
        var id = result!.Value.GetProperty("id").GetString();
        return new DisposableStub(() => _page.SendMessageToServerAsync("overlayRemove", new Dictionary<string, object?> { ["id"] = id }));
    }

    public Task ShowChapterAsync(string title, ScreencastShowChapterOptions? options = default)
        => _page.SendMessageToServerAsync("overlayChapter", new Dictionary<string, object?>
        {
            ["title"] = title,
            ["description"] = options?.Description,
            ["duration"] = options?.Duration,
        });

    public async Task<IAsyncDisposable> ShowActionsAsync(ScreencastShowActionsOptions? options = default)
    {
        await _page.SendMessageToServerAsync("screencastShowActions", new Dictionary<string, object?>
        {
            ["duration"] = options?.Duration,
            ["position"] = options?.Position,
            ["fontSize"] = options?.FontSize,
        }).ConfigureAwait(false);
        return new DisposableStub(() => _page.SendMessageToServerAsync("screencastHideActions"));
    }

    public Task HideActionsAsync()
        => _page.SendMessageToServerAsync("screencastHideActions");

    public Task ShowOverlaysAsync()
        => _page.SendMessageToServerAsync("overlaySetVisible", new Dictionary<string, object?> { ["visible"] = true });

    public Task HideOverlaysAsync()
        => _page.SendMessageToServerAsync("overlaySetVisible", new Dictionary<string, object?> { ["visible"] = false });
}
