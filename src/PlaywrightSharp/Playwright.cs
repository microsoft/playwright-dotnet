using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    /// <inheritdoc cref="PlaywrightSharp.IPlaywright" />
    public sealed class Playwright : IPlaywright, IDisposable
    {
        private readonly ILoggerFactory _loggerFactory;
        private readonly ConcurrentDictionary<string, IChannelOwner> _objects = new ConcurrentDictionary<string, IChannelOwner>();
        private readonly ConcurrentDictionary<string, ConnectionScope> _scopes = new ConcurrentDictionary<string, ConnectionScope>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>> _waitingForObject = new ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>>();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement?>> _callbacks = new ConcurrentDictionary<int, TaskCompletionSource<JsonElement?>>();
        private readonly ConnectionScope _rootScript;
        private Process _playwrightServerProcess;
        private int _lastId;
        private IConnectionTransport _transport;

        /// <summary>
        /// Initializes a new instance of the <see cref="Playwright"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        internal Playwright(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _rootScript = CreateScope(string.Empty);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Playwright() => Dispose(false);

        /// <inheritdoc/>
        public IBrowserType Chromium { get; private set; }

        /// <inheritdoc/>
        public IBrowserType Firefox { get; private set; }

        /// <inheritdoc/>
        public IBrowserType Webkit { get; private set; }

        /// <summary>
        /// Launches a Playwright server.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        /// <returns>A <see cref="Task"/> that completes when the playwright server is ready to be used.</returns>
        public static async Task<Playwright> CreateAsync(ILoggerFactory loggerFactory = null)
        {
            var playwright = new Playwright(loggerFactory);
            await playwright.LaunchPlaywrightServerAsync().ConfigureAwait(false);

            return playwright;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal void OnObjectCreated(string guid, IChannelOwner result)
        {
            _objects.TryAdd(guid, result);
            if (_waitingForObject.TryRemove(guid, out var callback))
            {
                callback.TrySetResult(result);
            }
        }

        internal async Task<T> SendMessageToServerAsync<T>(string guid, string method, object args)
            where T : class
        {
            int id = Interlocked.Increment(ref _lastId);
            var message = new MessageRequest
            {
                Id = id,
                Guid = guid,
                Method = method,
                Params = args,
            };

            string messageString = JsonSerializer.Serialize(message, GetDefaultJsonSerializerOptions());
            Debug.WriteLine($"pw:channel:command {messageString}");
            await _transport.SendAsync(messageString).ConfigureAwait(false);

            var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
            _callbacks.TryAdd(id, tcs);
            return (await tcs.Task.ConfigureAwait(false))?.ToObject<T>(GetDefaultJsonSerializerOptions());
        }

        internal ConnectionScope CreateScope(string guid)
        {
            var scope = new ConnectionScope(this, guid);
            _scopes.TryAdd(guid, scope);
            return scope;
        }

        internal void RemoveScope(string guid) => _scopes.TryRemove(guid, out _);

        internal void RemoveObject(string guid) => _objects.TryRemove(guid, out _);

        internal IChannelOwner GetObject(string guid)
        {
            _objects.TryGetValue(guid, out var result);
            return result;
        }

        private async Task<T> WaitForObjectWithKnownName<T>(string guid)
            where T : class
        {
            if (_objects.TryGetValue(guid, out var channel))
            {
                return channel as T;
            }

            var tcs = new TaskCompletionSource<IChannelOwner>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitingForObject.TryAdd(guid, tcs);
            return await tcs.Task.ConfigureAwait(false) as T;
        }

        private async Task LaunchPlaywrightServerAsync()
        {
            _playwrightServerProcess = GetProcess();
            _playwrightServerProcess.Start();
            _transport = new StdIOTransport(_playwrightServerProcess);
            _transport.MessageReceived += TransportOnMessageReceived;

            var chromiumTask = WaitForObjectWithKnownName<IBrowserType>(BrowserType.Chromium);
            var firefoxTask = WaitForObjectWithKnownName<IBrowserType>(BrowserType.Firefox);
            var webkitTask = WaitForObjectWithKnownName<IBrowserType>(BrowserType.Webkit);

            await Task.WhenAll(chromiumTask, firefoxTask, webkitTask).ConfigureAwait(false);

            Chromium = chromiumTask.Result;
            Firefox = firefoxTask.Result;
            Webkit = webkitTask.Result;
        }

        private void TransportOnMessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var message = JsonSerializer.Deserialize<PlaywrightServerMessage>(e.Message, JsonExtensions.DefaultJsonSerializerOptions);

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
                _objects.TryGetValue(message.Guid, out var scopeObject);
                var scope = scopeObject != null ? scopeObject.Scope : _rootScript;
                scope.CreateRemoteObject(message.Params.Type, message.Params.Guid, message.Params.Initializer);

                return;
            }

            _objects.TryGetValue(message.Guid, out var obj);
            obj?.Channel?.OnMessage(message.Method, message.Params);
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

        private Process GetProcess()
        {
            var process = new Process
            {
                StartInfo =
                {
                    FileName = GetExecutablePath(),
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    CreateNoWindow = true,
                },
            };

            process.StartInfo.FileName = "node";
            process.StartInfo.Arguments = "/Users/neo/Documents/Coding/microsoft/playwright/lib/rpc/server.js";

            return process;
        }

        private string GetExecutablePath()
        {
            // This is not the final solution.
            string tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Servers");
            string playwrightServer = "playwright-win.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                playwrightServer = "playwright-macos";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                playwrightServer = "playwright-linux";
            }

            return Path.Combine(tempDirectory, playwrightServer);
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

        private JsonSerializerOptions GetDefaultJsonSerializerOptions()
        {
            var options = JsonExtensions.GetNewDefaultSerializerOptions();
            options.Converters.Add(new ChannelOwnerToGuidConverter(this));
            options.Converters.Add(new ChannelToGuidConverter(this));

            return options;
        }
    }
}
