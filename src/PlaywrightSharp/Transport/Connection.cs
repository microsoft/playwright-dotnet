using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp.Transport
{
    internal class Connection : IDisposable
    {
        private readonly ConcurrentDictionary<string, IChannelOwner> _objects = new ConcurrentDictionary<string, IChannelOwner>();
        private readonly ConcurrentDictionary<string, ConnectionScope> _scopes = new ConcurrentDictionary<string, ConnectionScope>();
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>> _waitingForObject = new ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>>();
        private readonly ConcurrentDictionary<int, TaskCompletionSource<JsonElement?>> _callbacks = new ConcurrentDictionary<int, TaskCompletionSource<JsonElement?>>();
        private readonly ConnectionScope _rootScript;
        private readonly Process _playwrightServerProcess;
        private readonly IConnectionTransport _transport;
        private readonly ILoggerFactory _loggerFactory;
        private int _lastId;

        public Connection(ILoggerFactory loggerFactory, TransportTaskScheduler scheduler)
        {
            _rootScript = CreateScope(string.Empty);

            _playwrightServerProcess = GetProcess();

            _playwrightServerProcess.StartInfo.FileName = "node";
            _playwrightServerProcess.StartInfo.Arguments = "/Users/neo/Documents/Coding/microsoft/playwright/lib/rpc/server.js";

            _playwrightServerProcess.Start();
            _transport = new StdIOTransport(_playwrightServerProcess, scheduler);
            _transport.MessageReceived += TransportOnMessageReceived;
            _loggerFactory = loggerFactory;
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Connection() => Dispose(false);

        /// <inheritdoc/>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal static async Task InstallAsync(ILoggerFactory loggerFactory)
        {
            var tcs = new TaskCompletionSource<bool>();
            using var process = GetProcess();
            process.EnableRaisingEvents = true;
            process.StartInfo.Arguments = "install";
            process.Exited += (sender, e) => tcs.TrySetResult(true);
            process.Start();

            await tcs.Task.ConfigureAwait(false);
        }

        internal ConnectionScope CreateScope(string guid)
        {
            var scope = new ConnectionScope(this, guid, _loggerFactory);
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

        internal JsonSerializerOptions GetDefaultJsonSerializerOptions()
        {
            var options = JsonExtensions.GetNewDefaultSerializerOptions();
            options.Converters.Add(new ChannelOwnerToGuidConverter(this));
            options.Converters.Add(new ChannelToGuidConverter(this));
            options.Converters.Add(new HttpMethodConverter());

            return options;
        }

        internal async Task<T> WaitForObjectWithKnownName<T>(string guid)
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

        internal void OnObjectCreated(string guid, IChannelOwner result)
        {
            _objects.TryAdd(guid, result);
            if (_waitingForObject.TryRemove(guid, out var callback))
            {
                callback.TrySetResult(result);
            }
        }

        internal async Task<T> SendMessageToServerAsync<T>(string guid, string method, object args)
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
            var result = await tcs.Task.ConfigureAwait(false);

            if (typeof(T) == typeof(JsonElement?))
            {
                return (T)(object)result;
            }
            else if (result == null)
            {
                return default;
            }
            else
            {
                return result.Value.ToObject<T>(GetDefaultJsonSerializerOptions());
            }
        }

        private static Process GetProcess()
            => new Process
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

        private static string GetExecutablePath()
        {
            // This is not the final solution.
            string tempDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Drivers");
            string playwrightServer = "driver-win.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                playwrightServer = "driver-macos";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                playwrightServer = "driver-linux";
            }

            return Path.Combine(tempDirectory, playwrightServer);
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
                var createObjectInfo = message.Params.Value.ToObject<CreateObjectInfo>(GetDefaultJsonSerializerOptions());
                scope.CreateRemoteObject(createObjectInfo.Type, createObjectInfo.Guid, createObjectInfo.Initializer);

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

            if (error.Message.Contains("Target closed") || error.Message.Contains("The page has been closed."))
            {
                return new TargetClosedException(error.Message);
            }

            if (error.Message.Contains("Navigation failed because"))
            {
                return new NavigationException(error.Message);
            }

            return new PlaywrightSharpException(error.Message);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _playwrightServerProcess?.Kill();
            _playwrightServerProcess?.Dispose();
        }
    }
}
