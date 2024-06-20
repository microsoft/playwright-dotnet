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
using System.Diagnostics.CodeAnalysis;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

[SuppressMessage("Microsoft.Design", "CA1724", Justification = "Playwright is the entrypoint for all languages.")]
internal class PlaywrightImpl : ChannelOwner, IPlaywright
{
    private readonly PlaywrightInitializer _initializer;
    internal SelectorsAPI _selectors;

    private readonly Dictionary<string, BrowserNewContextOptions> _devices = new(StringComparer.InvariantCultureIgnoreCase);

    internal PlaywrightImpl(ChannelOwner parent, string guid, PlaywrightInitializer initializer)
         : base(parent, guid)
    {
        _initializer = initializer;

        _devices = _connection.LocalUtils?._devices ?? new();

        _initializer.Chromium.Playwright = this;
        _initializer.Firefox.Playwright = this;
        _initializer.Webkit.Playwright = this;
        APIRequest = new APIRequest(this);

        _selectors = new SelectorsAPI();
        var selectorsOwner = this._initializer.Selectors;
        _selectors.AddChannel(selectorsOwner);
        this._connection.Close += (_, _) => _selectors.RemoveChannel(selectorsOwner);
    }

    ~PlaywrightImpl() => Dispose(false);

    public IBrowserType Chromium { get => _initializer.Chromium; }

    public IBrowserType Firefox { get => _initializer.Firefox; }

    public IBrowserType Webkit { get => _initializer.Webkit; }

    public ISelectors Selectors => _selectors;

    public IReadOnlyDictionary<string, BrowserNewContextOptions> Devices => _devices;

    internal Browser PreLaunchedBrowser => _initializer.PreLaunchedBrowser;

    public IAPIRequest APIRequest { get; }

    /// <summary>
    /// Gets a <see cref="IBrowserType"/>.
    /// </summary>
    /// <param name="browserType"><see cref="IBrowserType"/> name. You can get the names from <see cref="global::Microsoft.Playwright.BrowserType"/>.
    /// e.g.: <see cref="global::Microsoft.Playwright.BrowserType.Chromium"/>,
    /// <see cref="global::Microsoft.Playwright.BrowserType.Firefox"/> or <see cref="global::Microsoft.Playwright.BrowserType.Webkit"/>.
    /// </param>
    public IBrowserType this[string browserType]
        => browserType?.ToLowerInvariant() switch
        {
            global::Microsoft.Playwright.BrowserType.Chromium => Chromium,
            global::Microsoft.Playwright.BrowserType.Firefox => Firefox,
            global::Microsoft.Playwright.BrowserType.Webkit => Webkit,
            _ => null,
        };

    internal void SetSelectors(SelectorsAPI selectors)
    {
        var selectorsOwner = this._initializer.Selectors;
        _selectors.RemoveChannel(selectorsOwner);
        _selectors = selectors;
        _selectors.AddChannel(selectorsOwner);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing)
        {
            return;
        }

        _connection?.Dispose();
    }
}
