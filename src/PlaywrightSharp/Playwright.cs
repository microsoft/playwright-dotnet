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
    public sealed class Playwright : ChannelOwnerBase, IPlaywright, IChannelOwner<Playwright>
    {
        /// <summary>
        /// Default timeout.
        /// </summary>
        public const int DefaultTimeout = 30_000;

        private readonly ILoggerFactory _loggerFactory;
        private readonly PlaywrightInitializer _initializer;
        private readonly PlaywrightChannel _channel;
        private readonly Connection _connection;

        private readonly Dictionary<string, DeviceDescriptor> _devices = new Dictionary<string, DeviceDescriptor>();

        internal Playwright(IChannelOwner parent, string guid, PlaywrightInitializer initializer, ILoggerFactory loggerFactory)
             : base(parent, guid)
        {
            _connection = parent.Connection;
            _initializer = initializer;
            _channel = new PlaywrightChannel(guid, parent.Connection, this);
            _loggerFactory = loggerFactory;

            foreach (var entry in initializer.DeviceDescriptors)
            {
                _devices[entry.Name] = entry.Descriptor;
            }

            _ = Selectors.AddChannelAsync(initializer.Selectors.Object);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Playwright() => Dispose(false);

        /// <inheritdoc/>
        Connection IChannelOwner.Connection => _connection;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Playwright> IChannelOwner<Playwright>.Channel => _channel;

        /// <summary>
        /// Returns a list of devices to be used with <see cref="IBrowser.NewContextAsync(BrowserContextOptions)"/>.
        /// </summary>
        public IReadOnlyDictionary<string, DeviceDescriptor> Devices => _devices;

        /// <inheritdoc/>
        public IBrowserType Chromium => _initializer.Chromium;

        /// <inheritdoc/>
        public IBrowserType Firefox => _initializer.Firefox;

        /// <inheritdoc/>
        public IBrowserType Webkit => _initializer.Webkit;

        /// <inheritdoc/>
        public Selectors Selectors => Selectors.SharedSelectors;

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
        /// <param name="driversLocationPath">Drivers location. Defaults to the PlaywrightSharp assembly path.</param>
        /// <param name="driverExecutablePath">Playwright driver path. If not set <see cref="InstallAsync(string, string)"/> needs to be used.</param>
        /// <param name="browsersPath">Specify a shared folder that playwright will use to download browsers and to look for browsers when launching browser instances.
        /// It is a shortcut to the PLAYWRIGHT_BROWSERS_PATH environment variable.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync(
            ILoggerFactory loggerFactory = null,
            TransportTaskScheduler scheduler = null,
            string driversLocationPath = null,
            string driverExecutablePath = null,
            string browsersPath = null)
        {
            var connection = new Connection(loggerFactory, scheduler, driversLocationPath, driverExecutablePath, browsersPath);

            var playwright = await connection.WaitForObjectWithKnownName<Playwright>("Playwright").ConfigureAwait(false);
            playwright.Connection = connection;

            return playwright;
        }

        /// <summary>
        /// Runs the playwright driver install command.
        /// </summary>
        /// <param name="browsersPath">Specify a shared folder that playwright will use to download browsers and to look for browsers when launching browser instances.
        /// <param name="driverExecutablePath">Drivers location. Defaults to the PlaywrightSharp assembly path.</param>
        /// It is a shortcut to the PLAYWRIGHT_BROWSERS_PATH environment variable.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver ran the install command.</returns>
        public static Task InstallAsync(string browsersPath = null, string driverExecutablePath = null)
            => Connection.InstallAsync(driverExecutablePath, browsersPath);

        /// <summary>
        /// Creates a Playwright driver in the specified <paramref name="path"/>.
        /// </summary>
        /// <param name="path">Destination path.</param>
        public static void InstallDriver(string path) => Connection.InstallDriver(path);

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
