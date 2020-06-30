using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlaywrightSharp.Transport;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="PlaywrightSharp.IPlaywrightClient" />
    public class PlaywrightClient : IPlaywrightClient, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private Process _playwrightServerProcess;
        private readonly ConcurrentDictionary<string, Channel> _channels = new ConcurrentDictionary<string, Channel>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<object>> _waitingForObject = new ConcurrentDictionary<string, TaskCompletionSource<object>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="PlaywrightClient"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        public PlaywrightClient(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~PlaywrightClient() => Dispose(false);

        /// <inheritdoc />
        public Task<IBrowserType> GetChromiumBrowserAsync() => GetBrowserTypeAsync(BrowserType.Chromium);

        /// <inheritdoc />
        public Task<IBrowserType> GetFirefoxBrowserAsync() => GetBrowserTypeAsync(BrowserType.Chromium);

        /// <inheritdoc />
        public Task<IBrowserType> GetWebkitTypeAsync() => GetBrowserTypeAsync(BrowserType.Chromium);

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private async Task<IBrowserType> GetBrowserTypeAsync(string browserType)
        {
            if (_playwrightServerProcess == null)
            {
                await LaunchPlaywrightServerAsync().ConfigureAwait(false);
            }

            return await WaitForObjectWithKnownName<IBrowserType>(browserType).ConfigureAwait(false);
        }

        private async Task<T> WaitForObjectWithKnownName<T>(string guid) where T: class
        {
            if (_channels.TryGetValue(guid, out var channel))
            {
                return channel.Object as T;
            }

            var tcs = new TaskCompletionSource<object>();
            _waitingForObject.TryAdd(guid, tcs);
            return await tcs.Task.ConfigureAwait(false) as T;
        }

        private async Task LaunchPlaywrightServerAsync()
        {
            _playwrightServerProcess = await LaunchProcessAsync().ConfigureAwait(false);
            var transport = new StdIOTransport(_playwrightServerProcess);
            transport.MessageReceived += TransportOnMessageReceived;
        }

        private void TransportOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var message = JsonSerializer.Deserialize<PlaywrightServerMessage>(e.Message);
            if (id) {
                debug('pw:channel:response')(parsedMessage);
                const callback = this._callbacks.get(id)!;
                this._callbacks.delete(id);
                if (error)
                    callback.reject(parseError(error));
                else
                    callback.resolve(this._replaceGuidsWithChannels(result));
                return;
            }

            debug('pw:channel:event')(parsedMessage);
            if (method === '__create__') {
                this._createRemoteObject(params.type,  guid, params.initializer);
                return;
            }
            const channel = this._channels.get(guid)!;
            channel.emit(method, this._replaceGuidsWithChannels(params));
        }

        private async Task<Process> LaunchProcessAsync()
        {
            var process = new Process
            {
                StartInfo =
                {
                    UseShellExecute = false,
                    FileName = await GetExecutablePathAsync().ConfigureAwait(false),
                    RedirectStandardOutput = true,
                },
            };

            process.Start();
            return process;
        }

        private async Task<string> GetExecutablePathAsync()
        {
            // This is not the final solution.
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDirectory);
            string playwrightServer = "playwright-win.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                playwrightServer = "playwright-macos";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                playwrightServer = "playwright-linux";
            }

            await ExtractServerAsync(tempDirectory, playwrightServer).ConfigureAwait(false);

            return Path.Combine(tempDirectory, playwrightServer);
        }

        private async Task ExtractServerAsync(string tempDirectory, string playwrightServer)
        {
            using var stream = typeof(PlaywrightClient).Assembly.GetManifestResourceStream($"PlaywrightSharp.Servers.{playwrightServer}");
            using var file = File.Create(Path.Combine(tempDirectory, playwrightServer));
            await stream.CopyToAsync(file).ConfigureAwait(false);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _loggerFactory?.Dispose();

            if (_playwrightServerProcess?.HasExited == false)
            {
                _playwrightServerProcess.Kill();
            }

            _playwrightServerProcess?.Dispose();
        }
    }
}
