using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channel;
using PlaywrightSharp.Transport.Protocol;

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
        private Process _playwrightServerProcess;
        private readonly ConnectionScope _rootScript;
        private int _lastId;
        private IConnectionTransport _transport;

        /// <summary>
        /// Initializes a new instance of the <see cref="Playwright"/> class.
        /// </summary>
        /// <param name="loggerFactory">Logger.</param>
        public Playwright(ILoggerFactory loggerFactory = null)
        {
            _loggerFactory = loggerFactory ?? NullLoggerFactory.Instance;
            _rootScript = CreateScope(string.Empty);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Playwright() => Dispose(false);

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

        internal void OnObjectCreated(string guid, IChannelOwner result)
        {
            _objects.TryAdd(guid, result);
            if (_waitingForObject.TryRemove(guid, out var callback))
            {
                callback.TrySetResult(result);
            }
        }

        internal async Task<JsonElement?> SendMessageToServerAsync(string guid, string method, params object[] args)
        {
            int id = Interlocked.Increment(ref _lastId);
            var message = new MessageRequest
            {
                Id = id,
                Guid = guid,
                Method = method,
                Params = args,
            };

            string messageString = JsonSerializer.Serialize(message, JsonExtensions.DefaultJsonSerializerOptions);
            Debug.WriteLine($"pw:channel:command {messageString}");
            await _transport.SendAsync(messageString).ConfigureAwait(false);

            var tcs = new TaskCompletionSource<JsonElement?>();
            _callbacks.TryAdd(id, tcs);
            return await tcs.Task.ConfigureAwait(false);
        }

        internal ConnectionScope CreateScope(string guid)
        {
            var scope = new ConnectionScope(this, guid);
            _scopes.TryAdd(guid, scope);
            return scope;
        }

        internal void RemoveScope(string guid) => _scopes.TryRemove(guid, out _);

        internal void RemoveObject(string guid) => _objects.TryRemove(guid, out _);

        private Task<IBrowserType> GetBrowserTypeAsync(string browserType)
        {
            if (_playwrightServerProcess == null)
            {
                LaunchPlaywrightServer();
            }

            return WaitForObjectWithKnownName<IBrowserType>(browserType);
        }

        private async Task<T> WaitForObjectWithKnownName<T>(string guid)
            where T : class
        {
            if (_objects.TryGetValue(guid, out var channel))
            {
                return channel as T;
            }

            var tcs = new TaskCompletionSource<IChannelOwner>();
            _waitingForObject.TryAdd(guid, tcs);
            return await tcs.Task.ConfigureAwait(false) as T;
        }

        private void LaunchPlaywrightServer()
        {
            _playwrightServerProcess = GetProcess();
            _playwrightServerProcess.Start();
            _transport = new StdIOTransport(_playwrightServerProcess);
            _transport.MessageReceived += TransportOnMessageReceived;
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
    }
}
