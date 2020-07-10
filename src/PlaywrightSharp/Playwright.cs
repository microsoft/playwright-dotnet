using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="PlaywrightSharp.IPlaywright" />
    public sealed class Playwright : IPlaywright, IDisposable, IChannelOwner<Playwright>
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly PlaywrightInitializer _initializer;
        private readonly PlaywrightChannel _channel;
        private readonly ConnectionScope _scope;

        internal Playwright(ConnectionScope scope, string guid, PlaywrightInitializer initializer, ILoggerFactory loggerFactory)
        {
            _scope = scope;
            _initializer = initializer;
            _channel = new PlaywrightChannel(guid, scope, this);
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Playwright() => Dispose(false);

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        Channel<Playwright> IChannelOwner<Playwright>.Channel => _channel;

        /// <summary>
        /// Returns a list of devices to be used with <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/>.
        /// </summary>
        public IReadOnlyDictionary<DeviceDescriptorName, DeviceDescriptor> Devices { get; }

        /// <inheritdoc/>
        public IBrowserType Chromium { get; }

        /// <inheritdoc/>
        public IBrowserType Firefox { get; }

        /// <inheritdoc/>
        public IBrowserType Webkit { get; }

        internal PlaywrightConnection Connection { get; set; }

        /// <inheritdoc/>
        public IBrowserType this[string browserType]
            => browserType switch
            {
                BrowserType.Chromium => Chromium,
                BrowserType.Firefox => Firefox,
                BrowserType.Webkit => Webkit,
                _ => null,
            };

        /// <summary>
        /// Launches a Playwright server.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        /// <param name="scheduler">Task scheduler for long running tasks.</param>
        /// <returns>A <see cref="Task"/> that completes when the playwright server is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync(ILoggerFactory loggerFactory = null, TransportTaskScheduler scheduler = null)
        {
            var connection = new PlaywrightConnection(loggerFactory, scheduler);

            var playwright = await connection.WaitForObjectWithKnownName<Playwright>("playwright").ConfigureAwait(false);
            playwright.Connection = connection;

            return playwright;
        }

        /// <inheritdoc />
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
