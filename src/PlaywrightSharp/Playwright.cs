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
    public sealed class Playwright : IPlaywright, IChannelOwner<Playwright>
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

        /// <summary>
        /// Default timeout.
        /// </summary>
        public static int DefaultTimeout => 30_000;

        /// <inheritdoc/>
        ConnectionScope IChannelOwner.Scope => _scope;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Playwright> IChannelOwner<Playwright>.Channel => _channel;

        /// <summary>
        /// Returns a list of devices to be used with <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/>.
        /// </summary>
        public IReadOnlyDictionary<string, DeviceDescriptor> Devices => _initializer.DeviceDescriptors;

        /// <inheritdoc/>
        public IBrowserType Chromium => _initializer.Chromium;

        /// <inheritdoc/>
        public IBrowserType Firefox => _initializer.Firefox;

        /// <inheritdoc/>
        public IBrowserType Webkit => _initializer.Webkit;

        internal Connection Connection { get; set; }

        /// <inheritdoc/>
        public IBrowserType this[string browserType]
            => browserType?.ToLower() switch
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
        /// <returns>A <see cref="Task"/> that completes when the playwright driver is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync(ILoggerFactory loggerFactory = null, TransportTaskScheduler scheduler = null)
        {
            var connection = new Connection(loggerFactory, scheduler);

            var playwright = await connection.WaitForObjectWithKnownName<Playwright>("playwright").ConfigureAwait(false);
            playwright.Connection = connection;

            return playwright;
        }

        /// <summary>
        /// Runs the playwright driver install command.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver ran the install command.</returns>
        public static Task InstallAsync(ILoggerFactory loggerFactory = null) => Connection.InstallAsync(loggerFactory);

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
