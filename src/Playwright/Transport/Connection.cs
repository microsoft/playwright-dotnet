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
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;
using Microsoft.Playwright.Helpers;
using Microsoft.Playwright.Transport.Channels;
using Microsoft.Playwright.Transport.Converters;
using Microsoft.Playwright.Transport.Protocol;

namespace Microsoft.Playwright.Transport;

internal class Connection : IDisposable
{
    // Keys used to attach server-provided error information to the exception,
    // marking it as a server error. The details shape is declared in the protocol.
    internal const string ErrorDetailsDataKey = "playwright.errorDetails";
    internal const string LogDataKey = "playwright.log";

    // The value of the DEBUG environment variable when the last connection was created.
    private static volatile string? _debugLogLevels;

    private readonly ConcurrentDictionary<string, ConnectionCallback> _callbacks = new();
    private readonly Root _rootObject;
    private readonly TaskQueue _queue = new();
    private readonly string _callIdPrefix = CreateCallIdPrefix();
    private readonly bool _ownsDispatcher;
    private int _tracingCount;
    private int _lastId;
    private volatile Exception? _closedError;
    private bool _disposed;

    public Connection(LocalUtils? localUtils = null, Dispatcher? dispatcher = null)
    {
        // Read here and not for every message. The driver also reads DEBUG once, when it starts.
        _debugLogLevels = Environment.GetEnvironmentVariable("DEBUG");
        // A connection over a pipe (BrowserType.ConnectAsync) shares the dispatcher of the
        // connection that owns the pipe, so that its messages need no extra thread hop.
        _ownsDispatcher = dispatcher == null;
        Dispatcher = dispatcher ?? new Dispatcher();
        if (_ownsDispatcher)
        {
            Dispatcher.UnhandledException += (_, ex) => DoClose(ex);
        }
        _rootObject = new(null, this, string.Empty);
        LocalUtils = localUtils;

        JsonSerializerOptions NewJsonSerializerOptions(bool keepNulls)
        {
            var options = JsonExtensions.GetNewDefaultSerializerOptions(keepNulls);

            // Workaround for https://github.com/dotnet/runtime/issues/46522
            options.Converters.Add(new ChannelOwnerConverterFactory(this));
            // Workaround for https://github.com/dotnet/runtime/issues/46522
            options.Converters.Add(new ChannelOwnerListToGuidListConverter<WritableStream>(this));
            return options;
        }
        DefaultJsonSerializerOptions = NewJsonSerializerOptions(false);
        DefaultJsonSerializerOptionsKeepNulls = NewJsonSerializerOptions(true);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    ~Connection() => Dispose(false);

    internal event EventHandler<Exception>? Close;

    public ConcurrentDictionary<string, ChannelOwner> Objects { get; } = new();

    /// <summary>
    /// The thread that handles messages and runs library code for this connection; see <see cref="Transport.Dispatcher"/>.
    /// </summary>
    internal Dispatcher Dispatcher { get; }

    /// <summary>
    /// The outermost API call in the current async flow, set by <see cref="WrapApiCallAsync{T}"/>.
    /// Nested calls inherit it, so that their messages carry the user's location and title.
    /// Connections that share a dispatcher share it: a call that crosses from one to the other
    /// (BrowserType.ConnectAsync, Selectors.RegisterAsync) is still one call.
    /// </summary>
    internal AsyncLocal<ApiZone?> ApiZone => Dispatcher.ApiZone;

    internal bool IsRemote { get; set; }

    internal bool IsClosed => _closedError != null;

    internal LocalUtils? LocalUtils { get; private set; }

    internal Func<object, bool, Task> OnMessage { get; set; } = null!;

    internal JsonSerializerOptions DefaultJsonSerializerOptions { get; }

    internal JsonSerializerOptions DefaultJsonSerializerOptionsKeepNulls { get; }

    internal static string FormatCallLog(string[]? log)
    {
        if (log == null)
        {
            return string.Empty;
        }
        if (!log.Any(l => l != null))
        {
            return string.Empty;
        }
        return "\nCall log:\n" + string.Join("\n", log);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    internal void SetIsTracing(bool isTracing)
    {
        if (isTracing)
        {
            _tracingCount++;
        }
        else
        {
            _tracingCount--;
        }
    }

    internal Task<JsonElement?> SendMessageToServerAsync(
        ChannelOwner? @object,
        string method,
        Dictionary<string, object?>? args = null,
        bool keepNulls = false,
        float? timeout = null)
        => SendMessageToServerAsync<JsonElement?>(@object, method, args, keepNulls, timeout);

    internal Task<T> SendMessageToServerAsync<T>(
        ChannelOwner? @object,
        string method,
        Dictionary<string, object?>? args = null,
        bool keepNulls = false,
        float? timeout = null) => WrapApiCallAsync(() => InnerSendMessageToServerAsync<T>(@object, method, args, keepNulls, timeout), false, null);

    private async Task<T> InnerSendMessageToServerAsync<T>(
        ChannelOwner? @object,
        string method,
        Dictionary<string, object?>? dictionary = null,
        bool keepNulls = false,
        float? timeout = null)
    {
        // Fire-and-forget: server intentionally never replies to __waitInfo__,
        // so silently drop it after the connection is closed or the object was collected.
        bool isWaitInfo = method == "__waitInfo__";
        if (isWaitInfo && (_closedError != null || @object?._wasCollected == true))
        {
            return default!;
        }
        if (_closedError != null)
        {
            throw _closedError;
        }
        if (@object?._wasCollected == true)
        {
            throw new PlaywrightException("The object has been collected to prevent unbounded heap growth.");
        }

        // Only the dispatcher completes this source, after the message that carries the reply has
        // been handled in full, and the continuation runs inline on the dispatcher thread.
        Dispatcher.AssertCurrent();
        string id = $"{_callIdPrefix}@{Interlocked.Increment(ref _lastId)}";
        var tcs = new TaskCompletionSource<JsonElement?>();
        var callback = new ConnectionCallback(tcs);

        if (!isWaitInfo)
        {
            _callbacks.TryAdd(id, callback);
        }

        var sanitizedArgs = new Dictionary<string, object>();
        if (dictionary?.Keys.Any(f => f != null) == true)
        {
            sanitizedArgs = dictionary
                .Where(f => f.Value != null)
                .ToDictionary(f => f.Key, f => f.Value) as Dictionary<string, object>;
        }
        var zone = ApiZone.Value ?? throw new InvalidOperationException("SendMessageToServerAsync must run inside WrapApiCallAsync.");
        var (title, isInternal, frames) = (zone.Title, zone.Internal, zone.Frames);
        var metadata = new Dictionary<string, object?>
        {
            ["internal"] = isInternal,
            ["wallTime"] = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
        };
        if (timeout.HasValue)
        {
            metadata["timeout"] = timeout.Value;
        }
        if (!string.IsNullOrEmpty(title))
        {
            metadata["title"] = title;
        }
        if (frames.Count > 0)
        {
            metadata["location"] = new Dictionary<string, object>
            {
                ["file"] = frames[0].File,
                ["line"] = frames[0].Line,
                ["column"] = frames[0].Column,
            };
        }

        if (_tracingCount > 0 && frames.Count > 0 && @object?.Guid != "localUtils")
        {
            LocalUtils?.AddStackToTracingNoReply(frames, id);
        }

        var message = new Dictionary<string, object?>
        {
            ["id"] = id,
            ["guid"] = @object?.Guid ?? string.Empty,
            ["method"] = method,
            ["params"] = sanitizedArgs,
            ["metadata"] = metadata,
        };
        // The write is not awaited: that would resume on a thread pool thread. Writes are
        // serialized by the queue, and a write that fails - fails this call instead.
        var sendTask = _queue.EnqueueAsync(() => OnMessage(message, keepNulls));

        // Fire-and-forget: server intentionally never replies to __waitInfo__.
        if (isWaitInfo)
        {
            sendTask.IgnoreException();
            return default!;
        }

        _ = sendTask.ContinueWith(
            t =>
            {
                if (_callbacks.TryRemove(id, out _))
                {
                    Dispatcher.Fail(tcs, t.Exception!.InnerException ?? t.Exception);
                }
            },
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);

        var result = await tcs.Task.ConfigureAwait(false);

        if (typeof(T) == typeof(JsonElement?))
        {
            return (T)(object)result!;
        }
        else if (result == null)
        {
            return default!;
        }
        else if (typeof(ChannelOwner).IsAssignableFrom(typeof(T)) || typeof(ChannelOwner[]).IsAssignableFrom(typeof(T)))
        {
            var enumerate = result.Value.EnumerateObject();

            return enumerate.Any()
                ? enumerate.FirstOrDefault().Value.ToObject<T>(DefaultJsonSerializerOptions)
                : default!;
        }
        else
        {
            return result.Value.ToObject<T>(DefaultJsonSerializerOptions);
        }
    }

    internal ChannelOwner GetObject(string guid)
    {
        Objects.TryGetValue(guid, out var result);
        return result;
    }

    internal void MarkAsRemote() => IsRemote = true;

    internal async Task<PlaywrightImpl> InitializePlaywrightAsync()
    {
        var args = new Dictionary<string, object?>
        {
            ["sdkLanguage"] = "csharp",
        };

        var jsonElement = await SendMessageToServerAsync(null, "initialize", args).ConfigureAwait(false);
        return jsonElement.GetObject<PlaywrightImpl>("playwright", this)!;
    }

    /// <summary>
    /// Queues a message from the driver. Messages are handled on the dispatcher thread, one at a
    /// time and in order; see <see cref="Transport.Dispatcher"/>.
    /// </summary>
    internal void Dispatch(PlaywrightServerMessage message)
        => Dispatcher.Post(() => DispatchOnDispatcherThread(message));

    private void DispatchOnDispatcherThread(PlaywrightServerMessage message)
    {
        Dispatcher.AssertCurrent();
        if (_closedError != null)
        {
            return;
        }

        try
        {
            if (message.Id != null)
            {
                _callbacks.TryRemove(message.Id, out var callback);
                if (callback == null)
                {
                    throw new PlaywrightException($"Cannot find command to respond: '{message.Id}'");
                }

                // Completed after this message, so that the caller resumes once every event that
                // preceded the reply has been handled, and never in the middle of one.
                if (message.Error != null && message.Result == null)
                {
                    var exception = ParseException(message.Error.Error, FormatCallLog(message.Log));
                    exception.Data[ErrorDetailsDataKey] = message.ErrorDetails?.GetRawText();
                    exception.Data[LogDataKey] = message.Log;
                    Dispatcher.Fail(callback.TaskCompletionSource, exception);
                }
                else
                {
                    Dispatcher.Complete(callback.TaskCompletionSource, message.Result);
                }
                return;
            }

            if (message.Method == "__create__")
            {
                CreateRemoteObject(message.Guid, message.Params.GetProperty("type").ToObject<ChannelOwnerType>(), message.Params.GetProperty("guid").ToString(), message.Params.GetProperty("initializer"));
                return;
            }

            Objects.TryGetValue(message.Guid, out var @object);
            if (@object == null)
            {
                throw new PlaywrightException($"Cannot find object to '{message.Method}': '{message.Guid}'");
            }

            if (message.Method == "__adopt__")
            {
                var childGuid = message.Params.GetProperty("guid").GetString()!;
                Objects.TryGetValue(childGuid, out var child);
                if (child == null)
                {
                    throw new PlaywrightException($"Unknown new child: '{childGuid}'");
                }
                @object.Adopt((ChannelOwner)child);
                return;
            }

            if (message.Method == "__dispose__")
            {
                @object.DisposeOwner(message.Params.TryGetProperty("reason", out var reason) == true ? reason.GetString() : null);
                return;
            }
            @object.OnMessage(message.Method, message.Params);
        }
        catch (Exception ex)
        {
            DoClose(ex);
        }
    }

    private ChannelOwner? CreateRemoteObject(string parentGuid, ChannelOwnerType? type, string guid, JsonElement? initializer)
    {
        ChannelOwner? result = null;
        var parent = string.IsNullOrEmpty(parentGuid) ? _rootObject : Objects[parentGuid];

        switch (type)
        {
            case ChannelOwnerType.APIRequestContext:
                result = new APIRequestContext(parent, guid, initializer?.ToObject<APIRequestContextInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Artifact:
                result = new Artifact(parent, guid, initializer?.ToObject<ArtifactInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.BindingCall:
                result = new BindingCall(parent, guid, initializer?.ToObject<BindingCallInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Playwright:
                result = new PlaywrightImpl(parent, guid, initializer?.ToObject<PlaywrightInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Browser:
                var browserInitializer = initializer?.ToObject<BrowserInitializer>(DefaultJsonSerializerOptions)!;
                result = new Browser(parent, guid, browserInitializer);
                break;
            case ChannelOwnerType.BrowserType:
                var browserTypeInitializer = initializer?.ToObject<BrowserTypeInitializer>(DefaultJsonSerializerOptions)!;
                result = new Core.BrowserType(parent, guid, browserTypeInitializer);
                break;
            case ChannelOwnerType.BrowserContext:
                var browserContextInitializer = initializer?.ToObject<BrowserContextInitializer>(DefaultJsonSerializerOptions)!;
                result = new BrowserContext(parent, guid, browserContextInitializer);
                break;
            case ChannelOwnerType.CDPSession:
                result = new CDPSession(parent, guid);
                break;
            case ChannelOwnerType.Debugger:
                result = new Core.Debugger(parent, guid);
                break;
            case ChannelOwnerType.Disposable:
                result = new Disposable(parent, guid);
                break;
            case ChannelOwnerType.Dialog:
                result = new Dialog(parent, guid, initializer?.ToObject<DialogInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.ElementHandle:
                result = new ElementHandle(parent, guid, initializer?.ToObject<ElementHandleInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Frame:
                result = new Frame(parent, guid, initializer?.ToObject<FrameInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.JSHandle:
                result = new JSHandle(parent, guid, initializer?.ToObject<JSHandleInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.JsonPipe:
                result = new JsonPipe(parent, guid, initializer?.ToObject<JsonPipeInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.LocalUtils:
                result = new LocalUtils(parent, guid, initializer?.ToObject<LocalUtilsInitializer>(DefaultJsonSerializerOptions)!);
                if (LocalUtils == null)
                {
                    LocalUtils = result as LocalUtils;
                }
                break;
            case ChannelOwnerType.Page:
                result = new Page(parent, guid, initializer?.ToObject<PageInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Request:
                result = new Request(parent, guid, initializer?.ToObject<RequestInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Response:
                result = new Response(parent, guid, initializer?.ToObject<ResponseInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Route:
                result = new Route(parent, guid, initializer?.ToObject<RouteInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.Worker:
                result = new Worker(parent, guid, initializer?.ToObject<WorkerInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.WebSocket:
                result = new WebSocket(parent, guid, initializer?.ToObject<WebSocketInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.WebSocketRoute:
                result = new WebSocketRoute(parent, guid, initializer?.ToObject<WebSocketRouteInitializer>(DefaultJsonSerializerOptions)!);
                break;
            case ChannelOwnerType.SocksSupport:
                result = new SocksSupport(parent, guid);
                break;
            case ChannelOwnerType.Stream:
                result = new Stream(parent, guid);
                break;
            case ChannelOwnerType.WritableStream:
                result = new WritableStream(parent, guid);
                break;
            case ChannelOwnerType.Tracing:
                result = new Tracing(parent, guid);
                break;
            case ChannelOwnerType.Electron:
            case ChannelOwnerType.Android:
                result = null;
                break;
            default:
                Debug.Fail($"Missing Playwright type binding for '{type}'");
                break;
        }
        return result;
    }

    /// <summary>
    /// Closes the connection after a failure. The transport reports its closing on the reader
    /// thread or on the thread that noticed the driver exit, and closing fails pending calls and
    /// raises Disconnected: dispatcher work. So this hops, and is immediate on the dispatcher thread.
    /// </summary>
    internal void DoClose(Exception? cause = null)
        => Dispatcher.RunOrPost(() => DoCloseImpl(cause != null ? new TargetClosedException(cause.Message, cause) : new TargetClosedException()));

    internal void DoClose(string? cause = null)
        => DoCloseImpl(!string.IsNullOrEmpty(cause) && cause != null ? new TargetClosedException(cause) : new TargetClosedException());

    internal void DoCloseImpl(Exception closeError)
    {
        _closedError = closeError;
        Dispose();
    }

    // Use a unique prefix for each client to avoid id clashes in a trace.
    private static string CreateCallIdPrefix()
    {
        const string Alphabet = "abcdefghijklmnopqrstuvwxyz";
        var bytes = Guid.NewGuid().ToByteArray();
        var prefix = new char[4];
        for (int i = 0; i < prefix.Length; i++)
        {
            prefix[i] = Alphabet[bytes[i] % Alphabet.Length];
        }
        return new string(prefix);
    }

    private Exception ParseException(PlaywrightServerError error, string messageSuffix)
    {
        if (string.IsNullOrEmpty(error.Message))
        {
            return new PlaywrightException(error.Value);
        }
        if (error.Name == "TimeoutError")
        {
            return new TimeoutException(error.Message + messageSuffix);
        }

        if (error.Name == "TargetClosedError")
        {
            return new TargetClosedException(error.Message + messageSuffix);
        }

        return new PlaywrightException(error.Message + messageSuffix);
    }

    private void Dispose(bool disposing)
    {
        if (!disposing || _disposed)
        {
            return;
        }

        _disposed = true;
        _closedError ??= new TargetClosedException("Connection disposed");

        foreach (var callback in _callbacks)
        {
            // We need to make sure that the task is handled otherwise it will be reported as unhandled on the caller side.
            // Its still possible to get the exception from the task.
            callback.Value.TaskCompletionSource.Task.IgnoreException();
            // Dispose can run on any thread; the callers still resume on the dispatcher.
            Dispatcher.Fail(callback.Value.TaskCompletionSource, _closedError);
        }
        _callbacks.Clear();

        _queue.Dispose();
        Close?.Invoke(this, new TargetClosedException("Connection disposed"));
        if (_ownsDispatcher)
        {
            Dispatcher.Dispose();
        }
    }

    internal static void TraceMessage(string logLevel, byte[] rawMessage)
    {
        string? actualLogLevel = _debugLogLevels;
        if (string.IsNullOrEmpty(actualLogLevel))
        {
            return;
        }
        if (!actualLogLevel!.Contains(logLevel))
        {
            return;
        }
        var message = UTF8Encoding.UTF8.GetString(rawMessage);
        string line = $"{logLevel}: {message}";
        Trace.WriteLine(line);
        Console.Error.WriteLine(line);
    }

    /// <summary>
    /// Runs an API call. Every public async method of the library, and every message it sends,
    /// goes through here, which gives the call three things:
    /// <list type="bullet">
    /// <item>An <see cref="ApiZone"/>: the user's stack frames and the title that the messages of
    /// this call report to the driver. A call nested in another call inherits the outer zone.</item>
    /// <item>The hop onto the dispatcher thread before <paramref name="action"/> runs, so that the
    /// call reads and writes client state in between messages, never concurrently with one.</item>
    /// <item>The hop off the dispatcher thread before the result is returned, so that the caller's
    /// continuation never runs on it. This holds for a call made from an event handler too: only
    /// the handler's synchronous part runs on the dispatcher thread.</item>
    /// </list>
    /// A call nested in another call, on the dispatcher thread, is library code calling itself:
    /// it runs inline and returns inline.
    /// <para>
    /// A flow that a message starts (a route, a binding call) is not inside a call, so each call
    /// that it makes returns on the thread pool as well: switch back before touching state.
    /// </para>
    /// </summary>
    [MethodImpl(MethodImplOptions.NoInlining)]
    internal async Task<T> WrapApiCallAsync<T>(Func<Task<T>> action, bool isInternal = false, string? title = null)
    {
        var outer = ApiZone.Value;
        if (outer == null)
        {
            // Captured before the hop: the frames of interest are the caller's.
            ApiZone.Value = new() { Internal = isInternal, Title = title, Frames = CaptureUserStack() };
        }
        else if ((isInternal && outer.Internal != true) || (title != null && outer.Title == null))
        {
            // An outer call may have set its own title; fall back to ours only when it has none.
            // Internal calls stay internal when nested in a user call.
            ApiZone.Value = new() { Internal = isInternal || outer.Internal == true, Title = outer.Title ?? title, Frames = outer.Frames };
        }

        if (outer != null && Dispatcher.IsCurrent)
        {
            return await action().ConfigureAwait(false);
        }

        // No-op on the dispatcher thread: an outermost call from an event handler starts inline,
        // so that what it sends stays ordered with the handler's synchronous code.
        await Dispatcher.SwitchTo();
        T result = default!;
        ExceptionDispatchInfo? error = null;
        try
        {
            result = await action().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            error = ExceptionDispatchInfo.Capture(ex);
        }
        await Dispatcher.ExitToThreadPool();
        error?.Throw();
        return result;
    }

    private static List<Protocol.StackFrame> CaptureUserStack()
    {
        var st = new StackTrace(true);
        var stack = new List<Protocol.StackFrame>();
        for (int i = 0; i < st.FrameCount; ++i)
        {
            var sf = st.GetFrame(i);
            string fileName = sf.GetFileName();
            if (!IsPlaywrightInternalNamespace(sf.GetMethod()?.ReflectedType?.Namespace) && !string.IsNullOrEmpty(fileName))
            {
                stack.Add(new() { File = fileName, Line = sf.GetFileLineNumber(), Column = sf.GetFileColumnNumber() });
            }
        }
        return stack;
    }

    internal Task WrapApiCallAsync(Func<Task> action, bool isInternal = false, string? title = null)
        => WrapApiCallAsync(
            async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            },
            isInternal,
            title);

    private static bool IsPlaywrightInternalNamespace(string? namespaceName)
    {
        return namespaceName != null &&
            (namespaceName == "Microsoft.Playwright" ||
            namespaceName.StartsWith("Microsoft.Playwright.Core", StringComparison.InvariantCultureIgnoreCase) ||
            namespaceName.StartsWith("Microsoft.Playwright.Transport", StringComparison.InvariantCultureIgnoreCase) ||
            namespaceName.StartsWith("Microsoft.Playwright.Helpers", StringComparison.InvariantCultureIgnoreCase));
    }
}

internal class ConnectionCallback
{
    public ConnectionCallback(TaskCompletionSource<JsonElement?> taskCompletionSource)
    {
        TaskCompletionSource = taskCompletionSource;
    }

    internal TaskCompletionSource<JsonElement?> TaskCompletionSource { get; }
}
