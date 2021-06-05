/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
 * Copyright (c) 2020 Meir Blachman
 * Modifications copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
 * SOFTWARE.
 */
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Converters;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport
{
    internal class Connection : IDisposable
    {
        private readonly ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>> _waitingForObject = new();
        private readonly ConcurrentDictionary<int, ConnectionCallback> _callbacks = new();
        private readonly ChannelOwnerBase _rootObject;
        private readonly Process _playwrightServerProcess;
        private readonly IConnectionTransport _transport;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<Connection> _logger;
        private readonly TaskQueue _queue = new();
        private int _lastId;
        private string _reason = string.Empty;

        public Connection()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddFilter((f, _) => f == "PlaywrightSharp.Playwright");
            });

            _logger = _loggerFactory.CreateLogger<Connection>();
            var debugLogger = _loggerFactory?.CreateLogger<PlaywrightImpl>();

            _rootObject = new ChannelOwnerBase(null, this, string.Empty);

            _playwrightServerProcess = GetProcess();
            _playwrightServerProcess.StartInfo.Arguments = "run-driver";
            _playwrightServerProcess.Start();
            _playwrightServerProcess.Exited += (_, _) => Close("Process exited");
            _transport = new StdIOTransport(_playwrightServerProcess, _loggerFactory);
            _transport.MessageReceived += Transport_MessageReceived;
            _transport.LogReceived += (_, e) => debugLogger?.LogInformation(e.Message);
            _transport.TransportClosed += (_, e) => Close(e.CloseReason);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Connection() => Dispose(false);

        public ConcurrentDictionary<string, IChannelOwner> Objects { get; } = new ConcurrentDictionary<string, IChannelOwner>();

        public bool IsClosed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
            _loggerFactory.Dispose();
        }

        internal Task<JsonElement?> SendMessageToServerAsync(
            string guid,
            string method,
            object args = null)
            => SendMessageToServerAsync<JsonElement?>(guid, method, args);

        internal async Task<T> SendMessageToServerAsync<T>(
            string guid,
            string method,
            object args)
        {
            if (IsClosed)
            {
                throw new PlaywrightException($"Connection closed ({_reason})");
            }

            int id = Interlocked.Increment(ref _lastId);
            var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = new ConnectionCallback
            {
                TaskCompletionSource = tcs,
            };

            _callbacks.TryAdd(id, callback);

            var st = new StackTrace(true);
            var stack = new List<object>();

            for (int i = 0; i < st.FrameCount; ++i)
            {
                var sf = st.GetFrame(i);
                string fileName = sf.GetFileName();
                if (string.IsNullOrEmpty(fileName) || fileName.Contains("/Playwright/") || fileName.Contains("\\Playwright\\"))
                {
                    continue;
                }

                stack.Add(new { file = fileName, line = sf.GetFileLineNumber() });
            }

            var metadata = new { stack };

            var sanitizedArgs = new Dictionary<string, object>();
            if (args != null)
            {
                if (args is IDictionary<string, object> dictionary && dictionary.Keys.Any(f => f != null))
                {
                    foreach (var kv in dictionary)
                    {
                        if (kv.Value != null)
                        {
                            sanitizedArgs.Add(kv.Key, kv.Value);
                        }
                    }
                }
                else
                {
                    foreach (PropertyDescriptor propertyDescriptor in TypeDescriptor.GetProperties(args))
                    {
                        object obj = propertyDescriptor.GetValue(args);
                        if (obj != null)
                        {
                            string name = propertyDescriptor.Name.Substring(0, 1).ToLower() + propertyDescriptor.Name.Substring(1);
                            sanitizedArgs.Add(name, obj);
                        }
                    }
                }
            }

            await _queue.EnqueueAsync(() =>
            {
                var message = new MessageRequest
                {
                    Id = id,
                    Guid = guid,
                    Method = method,
                    Params = sanitizedArgs,
                    Metadata = metadata,
                };

                string messageString = JsonSerializer.Serialize(message, GetDefaultJsonSerializerOptions());
                _logger?.LogInformation($"pw:channel:command {messageString}");

                return _transport.SendAsync(messageString);
            }).ConfigureAwait(false);

            var result = await tcs.Task.ConfigureAwait(false);

            if (typeof(T) == typeof(JsonElement?))
            {
                return (T)(object)result;
            }
            else if (result == null)
            {
                return default;
            }
            else if (typeof(ChannelBase).IsAssignableFrom(typeof(T)) || typeof(ChannelBase[]).IsAssignableFrom(typeof(T)))
            {
                var enumerate = result.Value.EnumerateObject();

                return enumerate.Any()
                    ? enumerate.FirstOrDefault().Value.ToObject<T>(GetDefaultJsonSerializerOptions())
                    : default;
            }
            else
            {
                return result.Value.ToObject<T>(GetDefaultJsonSerializerOptions());
            }
        }

        internal IChannelOwner GetObject(string guid)
        {
            Objects.TryGetValue(guid, out var result);
            return result;
        }

        internal JsonSerializerOptions GetDefaultJsonSerializerOptions()
        {
            var options = JsonExtensions.GetNewDefaultSerializerOptions();
            options.Converters.Add(new ChannelToGuidConverter(this));
            options.Converters.Add(new ChannelOwnerToGuidConverter<JSHandle>(this));
            options.Converters.Add(new ChannelOwnerToGuidConverter<ElementHandle>(this));
            options.Converters.Add(new ChannelOwnerToGuidConverter<IChannelOwner>(this));
            options.Converters.Add(new HttpMethodConverter());

            return options;
        }

        internal async Task<T> WaitForObjectWithKnownNameAsync<T>(string guid)
            where T : class
        {
            if (Objects.TryGetValue(guid, out var channel))
            {
                return (T)channel;
            }

            if (IsClosed)
            {
                throw new PlaywrightException(_reason);
            }

            var tcs = new TaskCompletionSource<IChannelOwner>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitingForObject.TryAdd(guid, tcs);
            return (T)await tcs.Task.ConfigureAwait(false);
        }

        internal void OnObjectCreated(string guid, IChannelOwner result)
        {
            Objects.TryAdd(guid, result);
            if (_waitingForObject.TryRemove(guid, out var callback))
            {
                callback.TrySetResult(result);
            }
        }

        private static Process GetProcess(string driverExecutablePath = null)
            => new()
            {
                StartInfo =
                {
                    FileName = string.IsNullOrEmpty(driverExecutablePath) ? GetExecutablePath() : driverExecutablePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

        private static string GetExecutablePath()
        {
            var assembly = typeof(Playwright).Assembly;
            string driversPath = new FileInfo(assembly.Location).Directory.FullName;

            string executableFile = GetPath(driversPath);
            if (File.Exists(executableFile))
            {
                return executableFile;
            }

            string fallbackBinPath = Path.Combine(
                driversPath,
                RuntimeInformation.IsOSPlatform(OSPlatform.OSX) || RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ? "playwright.sh" : "playwright.cmd");

            if (File.Exists(fallbackBinPath))
            {
                return fallbackBinPath;
            }

            throw new PlaywrightException($@"Driver not found in any of the locations. Tried:
 * {executableFile}
 * {fallbackBinPath}");
        }

        private static string GetPath(string driversPath)
        {
            string platformId;
            string runnerName;
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                platformId = RuntimeInformation.OSArchitecture == Architecture.X64 ? "win-x64" : "win-x86";
                runnerName = "playwright.cmd";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                runnerName = "playwright.sh";
                platformId = "osx";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                runnerName = "playwright.sh";
                platformId = "unix";
            }
            else
            {
                throw new PlaywrightException("Unknown platform");
            }

            return Path.Combine(driversPath, ".playwright", platformId, "native", runnerName);
        }

        private void Transport_MessageReceived(object sender, MessageReceivedEventArgs e)
        {
            var message = JsonSerializer.Deserialize<PlaywrightServerMessage>(e.Message, JsonExtensions.DefaultJsonSerializerOptions);

            if (message.Id.HasValue)
            {
                _logger?.LogInformation($"pw:channel:response {e.Message}");

                if (_callbacks.TryRemove(message.Id.Value, out var callback))
                {
                    if (message.Error != null)
                    {
                        callback.TaskCompletionSource.TrySetException(CreateException(message.Error.Error));
                    }
                    else
                    {
                        callback.TaskCompletionSource.TrySetResult(message.Result);
                    }
                }

                return;
            }

            _logger?.LogInformation($"pw:channel:event {e.Message}");

            try
            {
                if (message.Method == "__create__")
                {
                    var createObjectInfo = message.Params.Value.ToObject<CreateObjectInfo>(GetDefaultJsonSerializerOptions());
                    CreateRemoteObject(message.Guid, createObjectInfo.Type, createObjectInfo.Guid, createObjectInfo.Initializer);

                    return;
                }

                if (message.Method == "__dispose__")
                {
                    Objects.TryGetValue(message.Guid, out var disableObject);
                    disableObject?.DisposeOwner();
                    return;
                }

                Objects.TryGetValue(message.Guid, out var obj);
                obj?.Channel?.OnMessage(message.Method, message.Params);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Connection Error");
                Close(ex.ToString());
            }
        }

        private void CreateRemoteObject(string parentGuid, ChannelOwnerType type, string guid, JsonElement? initializer)
        {
            IChannelOwner result = null;
            var parent = string.IsNullOrEmpty(parentGuid) ? _rootObject : Objects[parentGuid];

            switch (type)
            {
                case ChannelOwnerType.Artifact:
                    result = new Artifact(parent, guid, initializer?.ToObject<ArtifactInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.BindingCall:
                    result = new BindingCall(parent, guid, initializer?.ToObject<BindingCallInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Playwright:
                    result = new PlaywrightImpl(parent, guid, initializer?.ToObject<PlaywrightInitializer>(GetDefaultJsonSerializerOptions()), _loggerFactory);
                    break;
                case ChannelOwnerType.Browser:
                    var browserInitializer = initializer?.ToObject<BrowserInitializer>(GetDefaultJsonSerializerOptions());
                    result = new Browser(parent, guid, browserInitializer);
                    break;
                case ChannelOwnerType.BrowserType:
                    var browserTypeInitializer = initializer?.ToObject<BrowserTypeInitializer>(GetDefaultJsonSerializerOptions());
                    result = new BrowserType(parent, guid, browserTypeInitializer);
                    break;
                case ChannelOwnerType.BrowserContext:
                    var browserContextInitializer = initializer?.ToObject<BrowserContextInitializer>(GetDefaultJsonSerializerOptions());
                    result = new BrowserContext(parent, guid, browserContextInitializer);
                    break;
                case ChannelOwnerType.ConsoleMessage:
                    result = new ConsoleMessage(parent, guid, initializer?.ToObject<ConsoleMessageInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Dialog:
                    result = new Dialog(parent, guid, initializer?.ToObject<DialogInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.ElementHandle:
                    result = new ElementHandle(parent, guid, initializer?.ToObject<ElementHandleInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Frame:
                    result = new Frame(parent, guid, initializer?.ToObject<FrameInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.JSHandle:
                    result = new JSHandle(parent, guid, initializer?.ToObject<JSHandleInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Page:
                    result = new Page(parent, guid, initializer?.ToObject<PageInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Request:
                    result = new Request(parent, guid, initializer?.ToObject<RequestInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Response:
                    result = new Response(parent, guid, initializer?.ToObject<ResponseInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Route:
                    result = new Route(parent, guid, initializer?.ToObject<RouteInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Worker:
                    result = new Worker(parent, guid, initializer?.ToObject<WorkerInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.WebSocket:
                    result = new WebSocket(parent, guid, initializer?.ToObject<WebSocketInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Selectors:
                    result = new Selectors(parent, guid);
                    break;
                case ChannelOwnerType.Stream:
                    result = new PlaywrightStream(parent, guid);
                    break;
                default:
                    _logger?.LogInformation("Missing type " + type);
                    break;
            }

            Objects.TryAdd(guid, result);
            OnObjectCreated(guid, result);
        }

        private void Close(string reason)
        {
            _reason = string.IsNullOrEmpty(_reason) ? reason : _reason;
            if (!IsClosed)
            {
                foreach (var callback in _callbacks)
                {
                    callback.Value.TaskCompletionSource.TrySetException(new PlaywrightException(reason));
                }

                foreach (var callback in _waitingForObject)
                {
                    callback.Value.TrySetException(new PlaywrightException(reason));
                }

                Dispose();
                IsClosed = true;
            }
        }

        private Exception CreateException(PlaywrightServerError error)
        {
            if (string.IsNullOrEmpty(error.Message))
            {
                return new PlaywrightException(error.Value);
            }

            if (error.Name == "TimeoutError")
            {
                return new TimeoutException(error.Message);
            }

            if (error.Message.Contains("Browser closed") || error.Message.Contains("Target closed") || error.Message.Contains("The page has been closed."))
            {
                return new PlaywrightException(error.Message);
            }

            if (error.Message.Contains("Navigation failed because"))
            {
                return new PlaywrightException(error.Message);
            }

            string message = error.Message
                .Replace(
                    "Try re-installing playwright with \"npm install playwright\"",
                    "Try re-installing the browsers running `playwright.cmd install` in windows or `./playwright.sh install` in MacOS or Linux.")
                .Replace(
                    "use DEBUG=pw:api environment variable and rerun",
                    "pass `debug: \"pw:api\"` to LaunchAsync");

            return new PlaywrightException(message);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _queue.Dispose();
            _transport.Close("Connection disposed");

            try
            {
                _playwrightServerProcess?.Kill();
                _playwrightServerProcess?.Dispose();
            }
            catch
            {
            }
        }
    }
}
