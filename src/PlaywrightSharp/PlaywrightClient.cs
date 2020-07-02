using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channel;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="PlaywrightSharp.IPlaywrightClient" />
    public class PlaywrightClient : IPlaywrightClient, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<string, Channel> _channels = new ConcurrentDictionary<string, Channel>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>> _waitingForObject = new ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>>();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement?>> _callbacks = new ConcurrentDictionary<int, TaskCompletionSource<JsonElement?>>();
        private Process _playwrightServerProcess;

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

        internal void AddChannel(Channel channel) => _channels.TryAdd(channel.Guid, channel);

        private async Task<IBrowserType> GetBrowserTypeAsync(string browserType)
        {
            if (_playwrightServerProcess == null)
            {
                await LaunchPlaywrightServerAsync().ConfigureAwait(false);
            }

            return await WaitForObjectWithKnownName<IBrowserType>(browserType).ConfigureAwait(false);
        }

        private async Task<T> WaitForObjectWithKnownName<T>(string guid)
            where T : class
        {
            if (_channels.TryGetValue(guid, out var channel))
            {
                return channel.Object as T;
            }

            var tcs = new TaskCompletionSource<IChannelOwner>();
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

            if (message.Id.HasValue)
            {
                Debug.WriteLine($"pw:channel:response {e.Message}");

                if (_callbacks.TryRemove(message.Id.Value, out var callback))
                {
                    if (message.Error != null)
                    {
                        callback.TrySetException(CreateException(message.Error));
                    }
                    else
                    {
                        callback.TrySetResult(message.Result);
                    }
                }

                return;
            }

            Debug.WriteLine($"pw:channel:event {e.Message}");

            if (message.Method == "__create__")
            {
                CreateRemoteObject(message.Params.Type, message.Guid, message.Params.Initializer);
                return;
            }

            _channels.TryGetValue(message.Guid, out var channel);
            channel?.OnMessage(message.Method, message.Params);
        }

        private void CreateRemoteObject(ChannelOwnerType type, string guid, JsonElement? initializer)
        {
            Channel channel = null;
            IChannelOwner result = null;

            switch (type)
            {
                case ChannelOwnerType.BindingCall:
                    result = new BindingCall(this, new BindingCallChannel(guid, this), initializer?.ToObject<BindingCallInitializer>());
                    break;
                case ChannelOwnerType.Browser:
                    result = new Browser(this, new BrowserChannel(guid, this), initializer?.ToObject<BrowserInitializer>());
                    break;
                case ChannelOwnerType.BrowserType:
                    result = new BrowserType(this, channel, initializer?.ToObject<BrowserTypeInitializer>());
                    break;
                case ChannelOwnerType.Context:
                    result = new BrowserContext(this, channel, initializer?.ToObject<BrowserContextInitializer>());
                    break;
                case ChannelOwnerType.ConsoleMessage:
                    result = new ConsoleMessage(this, channel, initializer?.ToObject<ConsoleMessageInitializer>());
                    break;
                case ChannelOwnerType.Dialog:
                    result = new Dialog(this, channel, initializer?.ToObject<DialogInitializer>());
                    break;
                case ChannelOwnerType.Download:
                    result = new Download(this, channel, initializer?.ToObject<DownloadInitializer>());
                    break;
                case ChannelOwnerType.ElementHandle:
                    result = new ElementHandle(this, channel, initializer?.ToObject<ElementHandleInitializer>());
                    break;
                case ChannelOwnerType.Frame:
                    result = new Frame(this, channel, initializer?.ToObject<FrameInitializer>());
                    break;
                case ChannelOwnerType.JSHandle:
                    result = new JSHandle(this, channel, initializer?.ToObject<JSHandleInitializer>());
                    break;
                case ChannelOwnerType.Page:
                    result = new Page(this, channel, initializer?.ToObject<PageInitializer>());
                    break;
                case ChannelOwnerType.Request:
                    result = new Request(this, channel, initializer?.ToObject<RequestInitializer>());
                    break;
                case ChannelOwnerType.Response:
                    result = new Response(this, channel, initializer?.ToObject<ResponseInitializer>());
                    break;
                case ChannelOwnerType.Route:
                    result = new Route(this, channel, initializer?.ToObject<RouteInitializer>());
                    break;
                default:
                    Debug.Write("Missing type " + type);
                    break;
            }

            if (_waitingForObject.TryRemove(guid, out var callback))
            {
                callback.TrySetResult(result);
            }
        }

        private Exception CreateException(PlaywrightServerError error)
        {
            if (string.IsNullOrEmpty(error.Message))
            {
                return new PlaywrightSharpException(error.Value);
            }

            if (error.Name == "TimeoutError")
            {
                return new TimeoutException(error.Message);
            }

            return new PlaywrightSharpException(error.Message);
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
