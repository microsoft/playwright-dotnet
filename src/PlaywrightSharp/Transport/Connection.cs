using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlaywrightSharp.Chromium;
using PlaywrightSharp.Helpers;
using PlaywrightSharp.Helpers.Linux;
using PlaywrightSharp.Transport;
using PlaywrightSharp.Transport.Channels;
using PlaywrightSharp.Transport.Converters;
using PlaywrightSharp.Transport.Protocol;

namespace PlaywrightSharp.Transport
{
    internal class Connection : IDisposable
    {
        private const string BrowsersPathEnvironmentVariable = "PLAYWRIGHT_BROWSERS_PATH";
        private const string DriverPathEnvironmentVariable = "PLAYWRIGHT_DRIVER_PATH";

        private readonly ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>> _waitingForObject = new ConcurrentDictionary<string, TaskCompletionSource<IChannelOwner>>();
        private readonly ConcurrentDictionary<int, ConnectionCallback> _callbacks = new ConcurrentDictionary<int, ConnectionCallback>();
        private readonly ChannelOwnerBase _rootObject;
        private readonly Process _playwrightServerProcess;
        private readonly IConnectionTransport _transport;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ILogger<Connection> _logger;
        private readonly TaskQueue _queue = new TaskQueue();
        private int _lastId;

        public Connection(
            ILoggerFactory loggerFactory,
            TransportTaskScheduler scheduler,
            string driversLocationPath = null,
            string driverExecutablePath = null,
            string browsersPath = null)
        {
            if (!string.IsNullOrEmpty(driverExecutablePath) && !string.IsNullOrEmpty(driversLocationPath))
            {
                throw new ArgumentException("Passing a driver executable path and a driver location path is not allowed");
            }

            if (!string.IsNullOrEmpty(browsersPath))
            {
                Environment.SetEnvironmentVariable(BrowsersPathEnvironmentVariable, Path.GetFullPath(browsersPath));
            }

            _loggerFactory = loggerFactory;
            _logger = _loggerFactory?.CreateLogger<Connection>();
            var debugLogger = _loggerFactory?.CreateLogger<Playwright>();

            _rootObject = new ChannelOwnerBase(null, this, string.Empty);

            _playwrightServerProcess = GetProcess(driversLocationPath, driverExecutablePath);
            _playwrightServerProcess.StartInfo.Arguments = "run-driver";
            _playwrightServerProcess.Start();
            _playwrightServerProcess.Exited += (sender, e) => Close("Process exited");
            _transport = new StdIOTransport(_playwrightServerProcess, scheduler);
            _transport.MessageReceived += Transport_MessageReceived;
            _transport.LogReceived += (s, e) => debugLogger?.LogInformation(e.Message);
            _transport.TransportClosed += (sender, e) => Close(e.CloseReason);
        }

        /// <inheritdoc cref="IDisposable.Dispose"/>
        ~Connection() => Dispose(false);

        public ConcurrentDictionary<string, IChannelOwner> Objects { get; } = new ConcurrentDictionary<string, IChannelOwner>();

        public bool IsClosed { get; private set; }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        internal static async Task InstallAsync(string driverPath = null, string browsersPath = null)
        {
            if (!string.IsNullOrEmpty(browsersPath))
            {
                Environment.SetEnvironmentVariable(BrowsersPathEnvironmentVariable, Path.GetFullPath(browsersPath));
            }

            var tcs = new TaskCompletionSource<bool>();
            using var process = GetProcess(driverPath);
            process.StartInfo.Arguments = "install";
            process.StartInfo.RedirectStandardOutput = false;
            process.StartInfo.RedirectStandardInput = false;
            process.StartInfo.RedirectStandardError = false;
            process.EnableRaisingEvents = true;
            process.Exited += (sender, e) => tcs.TrySetResult(true);
            process.Start();

            await tcs.Task.ConfigureAwait(false);
        }

        internal static string InstallDriver(string driversPath)
        {
            if (string.IsNullOrEmpty(driversPath) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DriverPathEnvironmentVariable)))
            {
                driversPath = Environment.GetEnvironmentVariable(DriverPathEnvironmentVariable);
            }

            var assembly = typeof(Playwright).Assembly;
            string tempDirectory = new FileInfo(assembly.Location).Directory.FullName;
            driversPath ??= Path.Combine(tempDirectory, "playwright-sharp-drivers");
            string platform = "win32_x64";
            string driver = "playwright-cli-win32_x64.zip";
            string executableFile = "playwright-cli.exe";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                platform = "macos";
                driver = "playwright-cli-mac.zip";
                executableFile = "playwright-cli";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                platform = "linux";
                driver = "playwright-cli-linux.zip";
                executableFile = "playwright-cli";
            }

            string directory = Path.Combine(driversPath, assembly.GetName().Version.ToString(), platform);
            string file = Path.Combine(directory, driver);
            ExtractDriver(file, driver);
            return Path.Combine(directory, executableFile);
        }

        internal void RemoveObject(string guid) => Objects.TryRemove(guid, out _);

        internal Task<JsonElement?> SendMessageToServer(
            string guid,
            string method,
            object args = null,
            bool ignoreNullValues = true,
            bool treatErrorPropertyAsError = true)
            => SendMessageToServerAsync<JsonElement?>(guid, method, args, ignoreNullValues, treatErrorPropertyAsError: treatErrorPropertyAsError);

        internal Task<T> SendMessageToServer<T>(
            string guid,
            string method,
            object args,
            bool ignoreNullValues = true,
            bool treatErrorPropertyAsError = true,
            JsonSerializerOptions serializerOptions = null)
            => SendMessageToServerAsync<T>(guid, method, args, ignoreNullValues, serializerOptions, treatErrorPropertyAsError);

        internal IChannelOwner GetObject(string guid)
        {
            Objects.TryGetValue(guid, out var result);
            return result;
        }

        internal JsonSerializerOptions GetDefaultJsonSerializerOptions(bool ignoreNullValues = false)
        {
            var options = JsonExtensions.GetNewDefaultSerializerOptions(ignoreNullValues);
            options.Converters.Add(new ChannelOwnerToGuidConverter(this));
            options.Converters.Add(new ChannelToGuidConverter(this));
            options.Converters.Add(new HttpMethodConverter());

            return options;
        }

        internal async Task<T> WaitForObjectWithKnownName<T>(string guid)
            where T : class
        {
            if (Objects.TryGetValue(guid, out var channel))
            {
                return channel as T;
            }

            var tcs = new TaskCompletionSource<IChannelOwner>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waitingForObject.TryAdd(guid, tcs);
            return await tcs.Task.ConfigureAwait(false) as T;
        }

        internal void OnObjectCreated(string guid, IChannelOwner result)
        {
            Objects.TryAdd(guid, result);
            if (_waitingForObject.TryRemove(guid, out var callback))
            {
                callback.TrySetResult(result);
            }
        }

        internal async Task<T> SendMessageToServerAsync<T>(
            string guid,
            string method,
            object args,
            bool ignoreNullValues = true,
            JsonSerializerOptions options = null,
            bool treatErrorPropertyAsError = true)
        {
            if (IsClosed)
            {
                throw new PlaywrightSharpException("Connection closed");
            }

            int id = Interlocked.Increment(ref _lastId);
            var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
            var callback = new ConnectionCallback
            {
                TaskCompletionSource = tcs,
                TreatErrorPropertyAsError = treatErrorPropertyAsError,
            };

            _callbacks.TryAdd(id, callback);

            await _queue.Enqueue(() =>
            {
                var message = new MessageRequest
                {
                    Id = id,
                    Guid = guid,
                    Method = method,
                    Params = args,
                };

                string messageString = JsonSerializer.Serialize(message, options ?? GetDefaultJsonSerializerOptions(ignoreNullValues));
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

        private static Process GetProcess(string driversLocationPath = null, string driverExecutablePath = null)
            => new Process
            {
                StartInfo =
                {
                    FileName = string.IsNullOrEmpty(driverExecutablePath) ? GetExecutablePath(driversLocationPath) : driverExecutablePath,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardInput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
            };

        private static string GetExecutablePath(string driversPath = null)
        {
            if (string.IsNullOrEmpty(driversPath) && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(DriverPathEnvironmentVariable)))
            {
                driversPath = Environment.GetEnvironmentVariable(DriverPathEnvironmentVariable);
            }

            return InstallDriver(driversPath);
        }

        private static void ExtractDriver(string file, string driver)
        {
            var directory = new FileInfo(file).Directory;

            if (!directory.Exists)
            {
                Directory.CreateDirectory(directory.FullName);

                using (var resource = typeof(Playwright).Assembly.GetManifestResourceStream($"PlaywrightSharp.Drivers.{driver}"))
                using (var zipStream = new ZipArchive(resource))
                {
                    zipStream.ExtractToDirectory(directory.FullName);
                }

                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    foreach (var executable in directory.GetFiles().Where(f => f.Name == "playwright-cli" || f.Name.Contains("ffmpeg")))
                    {
                        if (LinuxSysCall.Chmod(executable.FullName, LinuxSysCall.ExecutableFilePermissions) != 0)
                        {
                            throw new PlaywrightSharpException($"Unable to chmod the driver ({Marshal.GetLastWin32Error()})");
                        }
                    }
                }
            }
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
                    else if (callback.TreatErrorPropertyAsError && message.Result?.TryGetProperty("error", out var _) == true)
                    {
                        callback.TaskCompletionSource.TrySetException(CreateException(message.Result?.GetProperty("error").ToString()));
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
                Close(ex.ToString());
            }
        }

        private void CreateRemoteObject(string parentGuid, ChannelOwnerType type, string guid, JsonElement? initializer)
        {
            IChannelOwner result = null;
            var parent = string.IsNullOrEmpty(parentGuid) ? _rootObject : Objects[parentGuid];

            switch (type)
            {
                case ChannelOwnerType.BindingCall:
                    result = new BindingCall(parent, guid, initializer?.ToObject<BindingCallInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Playwright:
#pragma warning disable CA2000 // Dispose objects before losing scope
                    result = new Playwright(parent, guid, initializer?.ToObject<PlaywrightInitializer>(GetDefaultJsonSerializerOptions()), _loggerFactory);
#pragma warning restore CA2000 // Dispose objects before losing scope
                    break;
                case ChannelOwnerType.Browser:
                    var browserInitializer = initializer?.ToObject<BrowserInitializer>(GetDefaultJsonSerializerOptions());

                    if (browserInitializer.Name == BrowserType.Chromium)
                    {
                        result = new ChromiumBrowser(parent, guid, browserInitializer);
                    }
                    else
                    {
                        result = new Browser(parent, guid, browserInitializer);
                    }

                    break;
                case ChannelOwnerType.BrowserType:
                    var browserTypeInitializer = initializer?.ToObject<BrowserTypeInitializer>(GetDefaultJsonSerializerOptions());

                    if (browserTypeInitializer.Name == BrowserType.Chromium)
                    {
                        result = new ChromiumBrowserType(parent, guid, browserTypeInitializer);
                    }
                    else
                    {
                        result = new BrowserType(parent, guid, browserTypeInitializer);
                    }

                    break;
                case ChannelOwnerType.BrowserContext:
                    var browserContextInitializer = initializer?.ToObject<BrowserContextInitializer>(GetDefaultJsonSerializerOptions());

                    if (browserContextInitializer.BrowserName == BrowserType.Chromium)
                    {
                        result = new ChromiumBrowserContext(parent, guid, browserContextInitializer);
                    }
                    else
                    {
                        result = new BrowserContext(parent, guid, browserContextInitializer);
                    }

                    break;
                case ChannelOwnerType.ConsoleMessage:
                    result = new ConsoleMessage(parent, guid, initializer?.ToObject<ConsoleMessageInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Dialog:
                    result = new Dialog(parent, guid, initializer?.ToObject<DialogInitializer>(GetDefaultJsonSerializerOptions()));
                    break;
                case ChannelOwnerType.Download:
                    result = new Download(parent, guid, initializer?.ToObject<DownloadInitializer>(GetDefaultJsonSerializerOptions()));
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
                case ChannelOwnerType.CDPSession:
                    result = new CDPSession(parent, guid);
                    break;
                case ChannelOwnerType.Selectors:
                    result = new SelectorsOwner(parent, guid);
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
            if (!IsClosed)
            {
                foreach (var callback in _callbacks)
                {
                    callback.Value.TaskCompletionSource.TrySetException(new TargetClosedException(reason));
                }

                Dispose();
                IsClosed = true;
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

        private Exception CreateException(string message)
        {
            if (message.Contains("Timeout") && message.Contains("ms exceeded"))
            {
                return new TimeoutException(message);
            }

            if (message.Contains("Target closed") || message.Contains("The page has been closed."))
            {
                return new TargetClosedException(message);
            }

            if (message.Contains("Navigation failed because"))
            {
                return new NavigationException(message);
            }

            return new PlaywrightSharpException(message);
        }

        private void Dispose(bool disposing)
        {
            if (!disposing)
            {
                return;
            }

            _queue.Dispose();
            _transport.Close("Connection closed");

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
