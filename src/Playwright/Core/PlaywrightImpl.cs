using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright.Transport;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Core
{
    [SuppressMessage("Microsoft.Design", "CA1724", Justification = "Playwright is the entrypoint for all languages.")]
    internal class PlaywrightImpl : ChannelOwnerBase, IPlaywright, IChannelOwner<PlaywrightImpl>
    {
        /// <summary>
        /// Default timeout.
        /// </summary>
        public const int DefaultTimeout = 30_000;

        private readonly ILoggerFactory _loggerFactory;
        private readonly PlaywrightInitializer _initializer;
        private readonly PlaywrightChannel _channel;
        private readonly Connection _connection;
        private readonly Dictionary<string, BrowserNewContextOptions> _devices = new(StringComparer.InvariantCultureIgnoreCase);

        internal PlaywrightImpl(IChannelOwner parent, string guid, PlaywrightInitializer initializer, ILoggerFactory loggerFactory)
             : base(parent, guid)
        {
            _connection = parent.Connection;
            _initializer = initializer;
            _channel = new(guid, parent.Connection, this);
            _loggerFactory = loggerFactory;

            foreach (var entry in initializer.DeviceDescriptors)
            {
                _devices[entry.Name] = entry.Descriptor;
            }
        }

        ~PlaywrightImpl() => Dispose(false);

        Connection IChannelOwner.Connection => _connection;

        ChannelBase IChannelOwner.Channel => _channel;

        IChannel<PlaywrightImpl> IChannelOwner<PlaywrightImpl>.Channel => _channel;

        public IBrowserType Chromium { get => _initializer.Chromium; set => throw new NotSupportedException(); }

        public IBrowserType Firefox { get => _initializer.Firefox; set => throw new NotSupportedException(); }

        public IBrowserType Webkit { get => _initializer.Webkit; set => throw new NotSupportedException(); }

        public ISelectors Selectors { get => _initializer.Selectors; }

        public IReadOnlyDictionary<string, BrowserNewContextOptions> Devices => _devices;

        internal Connection Connection { get; set; }

        /// <summary>
        /// Gets a <see cref="IBrowserType"/>.
        /// </summary>
        /// <param name="browserType"><see cref="IBrowserType"/> name. You can get the names from <see cref="global::Microsoft.Playwright.BrowserType"/>.
        /// e.g.: <see cref="global::Microsoft.Playwright.BrowserType.Chromium"/>,
        /// <see cref="global::Microsoft.Playwright.BrowserType.Firefox"/> or <see cref="global::Microsoft.Playwright.BrowserType.Webkit"/>.
        /// </param>
        public IBrowserType this[string browserType]
            => browserType?.ToLower() switch
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

            _loggerFactory?.Dispose();
            Connection?.Dispose();
        }
    }
}
