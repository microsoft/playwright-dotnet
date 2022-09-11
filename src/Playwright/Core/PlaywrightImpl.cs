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
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core;

[SuppressMessage("Microsoft.Design", "CA1724", Justification = "Playwright is the entrypoint for all languages.")]
internal class PlaywrightImpl : ChannelOwnerBase, IPlaywright, IChannelOwner<PlaywrightImpl>
{
    private readonly PlaywrightInitializer _initializer;
    internal readonly PlaywrightChannel _channel;
    private readonly Connection _connection;

    private readonly Dictionary<string, BrowserNewContextOptions> _devices = new(StringComparer.InvariantCultureIgnoreCase);

    internal PlaywrightImpl(IChannelOwner parent, string guid, PlaywrightInitializer initializer)
         : base(parent, guid)
    {
        _connection = parent.Connection;
        _initializer = initializer;
        _channel = new(guid, parent.Connection, this);

        foreach (var entry in initializer.DeviceDescriptors)
        {
            _devices[entry.Name] = entry.Descriptor;
        }

        _initializer.Chromium.Playwright = this;
        _initializer.Firefox.Playwright = this;
        _initializer.Webkit.Playwright = this;
        APIRequest = new APIRequest(this);
    }

    ~PlaywrightImpl() => Dispose(false);

    Connection IChannelOwner.Connection => _connection;

    ChannelBase IChannelOwner.Channel => _channel;

    IChannel<PlaywrightImpl> IChannelOwner<PlaywrightImpl>.Channel => _channel;

    public IBrowserType Chromium { get => _initializer.Chromium; set => throw new NotSupportedException(); }

    public IBrowserType Firefox { get => _initializer.Firefox; set => throw new NotSupportedException(); }

    public IBrowserType Webkit { get => _initializer.Webkit; set => throw new NotSupportedException(); }

    public ISelectors Selectors => _initializer.Selectors;

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
