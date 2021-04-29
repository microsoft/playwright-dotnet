using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="IPlaywright" />
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

        internal Playwright(IChannelOwner parent, string guid, PlaywrightInitializer initializer, ILoggerFactory loggerFactory)
             : base(parent, guid)
        {
            _connection = parent.Connection;
            _initializer = initializer;
            _channel = new PlaywrightChannel(guid, parent.Connection, this);
            _loggerFactory = loggerFactory;
            _ = (Selectors as PlaywrightSharp.Selectors).AddChannelAsync(initializer.Selectors.Object);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Playwright() => Dispose(false);

        /// <inheritdoc/>
        Connection IChannelOwner.Connection => _connection;

        /// <inheritdoc/>
        ChannelBase IChannelOwner.Channel => _channel;

        /// <inheritdoc/>
        IChannel<Playwright> IChannelOwner<Playwright>.Channel => _channel;

        /// <inheritdoc/>
        public IBrowserType Chromium { get => _initializer.Chromium; set => throw new NotSupportedException(); }

        /// <inheritdoc/>
        public IBrowserType Firefox { get => _initializer.Firefox; set => throw new NotSupportedException(); }

        /// <inheritdoc/>
        public IBrowserType Webkit { get => _initializer.Webkit; set => throw new NotSupportedException(); }

        /// <inheritdoc/>
        public ISelectors Selectors { get => PlaywrightSharp.Selectors.SharedSelectors; set => throw new NotSupportedException(); }

        internal Connection Connection { get; set; }

        /// <summary>
        /// Gets a <see cref="IBrowserType"/>.
        /// </summary>
        /// <param name="browserType"><see cref="IBrowserType"/> name. You can get the names from <see cref="BrowserType"/>.
        /// e.g.: <see cref="BrowserType.Chromium"/>, <see cref="BrowserType.Firefox"/> or <see cref="BrowserType.Webkit"/>.
        /// </param>
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
        /// <param name="driverExecutablePath">Playwright driver path.</param>
        /// <param name="browsersPath">Specify a shared folder that playwright will use to download browsers and to look for browsers when launching browser instances.
        /// It is a shortcut to the PLAYWRIGHT_BROWSERS_PATH environment variable.
        /// </param>
        /// <param name="debug">Enables the playwright driver log. Pass `pw:api` to get the Playwright API log.
        /// It is a shortcut to the DEBUG=pw:api environment variable.
        /// </param>
        /// <returns>A <see cref="Task"/> that completes when the playwright driver is ready to be used.</returns>
        public static async Task<IPlaywright> CreateAsync(
            ILoggerFactory loggerFactory = null,
            TransportTaskScheduler scheduler = null,
            string driverExecutablePath = null,
            string browsersPath = null,
            string debug = null)
        {
            if (!string.IsNullOrEmpty(debug))
            {
                Environment.SetEnvironmentVariable("DEBUG", debug);
            }

            var connection = new Connection(loggerFactory, scheduler, driverExecutablePath, browsersPath);

            var playwright = await connection.WaitForObjectWithKnownNameAsync<Playwright>("Playwright").ConfigureAwait(false);
            playwright.Connection = connection;

            return playwright;
        }

        /// <summary>
        /// Runs the playwright driver install command.
        /// </summary>
        /// <param name="browsersPath">Specify a shared folder that playwright will use to download browsers and to look for browsers when launching browser instances.</param>
        /// <param name="driverExecutablePath">Drivers location. Defaults to the PlaywrightSharp assembly path.</param>
        /// It is a shortcut to the PLAYWRIGHT_BROWSERS_PATH environment variable.
        /// <returns>A <see cref="Task"/> that completes when the playwright driver ran the install command.</returns>
        public static Task InstallAsync(string browsersPath = null, string driverExecutablePath = null)
            => Connection.InstallAsync(driverExecutablePath, browsersPath);

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
