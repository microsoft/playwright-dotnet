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
    private readonly ConcurrentDictionary<int, ConnectionCallback> _callbacks = new();
    private readonly Root _rootObject;
    private readonly TaskQueue _queue = new();
    private int _tracingCount;
    private int _lastId;
    private Exception _closedError;

    public Connection(LocalUtils localUtils = null)
    {
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

    internal event EventHandler<Exception> Close;

    public ConcurrentDictionary<string, ChannelOwner> Objects { get; } = new();

    internal AsyncLocal<List<ApiZone>> ApiZone { get; } = new();

    internal bool IsRemote { get; set; }

    internal LocalUtils LocalUtils { get; private set; }

    internal Func<object, bool, Task> OnMessage { get; set; }

    internal JsonSerializerOptions DefaultJsonSerializerOptions { get; }

    internal JsonSerializerOptions DefaultJsonSerializerOptionsKeepNulls { get; }

    internal static string FormatCallLog(string[] log)
    {
        if (log == null)
        {
            return string.Empty;
        }
        if (!log.Any(l => l != null))
        {
            return string.Empty;
        }
        return "\nCall log:\n  - " + string.Join("\n  - ", log);
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
        ChannelOwner @object,
        string method,
        Dictionary<string, object> args = null,
        bool keepNulls = false)
        => SendMessageToServerAsync<JsonElement?>(@object, method, args, keepNulls);

    internal Task<T> SendMessageToServerAsync<T>(
        ChannelOwner @object,
        string method,
        Dictionary<string, object> args = null,
        bool keepNulls = false) => WrapApiCallAsync(() => InnerSendMessageToServerAsync<T>(@object, method, args, keepNulls));

    private async Task<T> InnerSendMessageToServerAsync<T>(
        ChannelOwner @object,
        string method,
        Dictionary<string, object> dictionary = null,
        bool keepNulls = false)
    {
        if (_closedError != null)
        {
            throw _closedError;
        }
        if (@object?._wasCollected == true)
        {
            throw new PlaywrightException("The object has been collected to prevent unbounded heap growth.");
        }

        int id = Interlocked.Increment(ref _lastId);
        var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new ConnectionCallback
        {
            TaskCompletionSource = tcs,
        };

        _callbacks.TryAdd(id, callback);

        var sanitizedArgs = new Dictionary<string, object>();
        if (dictionary?.Keys.Any(f => f != null) == true)
        {
            sanitizedArgs = dictionary
                .Where(f => f.Value != null)
                .ToDictionary(f => f.Key, f => f.Value);
        }
        var (apiName, frames) = (ApiZone.Value[0].ApiName, ApiZone.Value[0].Frames);
        var metadata = new Dictionary<string, object>
        {
            ["internal"] = string.IsNullOrEmpty(apiName),
            ["wallTime"] = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
        };
        if (!string.IsNullOrEmpty(apiName))
        {
            metadata["apiName"] = apiName;
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

        if (_tracingCount > 0 && frames.Count > 0 && @object.Guid != "localUtils")
        {
            LocalUtils.AddStackToTracingNoReply(frames, id);
        }

        await _queue.EnqueueAsync(() =>
        {
            var message = new MessageRequest
            {
                Id = id,
                Guid = @object == null ? string.Empty : @object.Guid,
                Method = method,
                Params = sanitizedArgs,
                Metadata = metadata,
            };

            TraceMessage("pw:channel:command", message);

            return OnMessage(message, keepNulls);
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
        else if (typeof(ChannelOwner).IsAssignableFrom(typeof(T)) || typeof(ChannelOwner[]).IsAssignableFrom(typeof(T)))
        {
            var enumerate = result.Value.EnumerateObject();

            return enumerate.Any()
                ? enumerate.FirstOrDefault().Value.ToObject<T>(DefaultJsonSerializerOptions)
                : default;
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
        var args = new Dictionary<string, object>
        {
            ["sdkLanguage"] = "csharp",
        };

        var jsonElement = await SendMessageToServerAsync(null, "initialize", args).ConfigureAwait(false);
        return jsonElement.GetObject<PlaywrightImpl>("playwright", this);
    }

    internal void Dispatch(PlaywrightServerMessage message)
    {
        if (_closedError != null)
        {
            return;
        }
        if (message.Id.HasValue)
        {
            TraceMessage("pw:channel:response", message);

            _callbacks.TryRemove(message.Id.Value, out var callback);
            if (callback == null)
            {
                throw new PlaywrightException($"Cannot find command to respond: '{message.Id}'");
            }

            if (message.Error != null && message.Result == null)
            {
                var exception = ParseException(message.Error.Error, FormatCallLog(message.Log));
                callback.TaskCompletionSource.TrySetException(exception);
            }
            else
            {
                callback.TaskCompletionSource.TrySetResult(message.Result);
            }
            return;
        }

        TraceMessage("pw:channel:event", message);

        try
        {
            if (message.Method == "__create__")
            {
                var createObjectInfo = message.Params.Value.ToObject<CreateObjectInfo>(DefaultJsonSerializerOptions);
                CreateRemoteObject(message.Guid, createObjectInfo.Type, createObjectInfo.Guid, createObjectInfo.Initializer);
                return;
            }

            Objects.TryGetValue(message.Guid, out var @object);
            if (@object == null)
            {
                throw new PlaywrightException($"Cannot find object to '{message.Method}': '{message.Guid}'");
            }

            if (message.Method == "__adopt__")
            {
                var childGuid = message.Params.Value.GetProperty("guid").GetString();
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
                @object.DisposeOwner(message.Params.Value.TryGetProperty("reason", out var reason) ? reason.GetString() : null);
                return;
            }
            @object.OnMessage(message.Method, message.Params);
        }
        catch (Exception ex)
        {
            TraceMessage("pw:dotnet", $"Connection Close: {ex.Message}\n{ex.StackTrace}");
            DoClose(ex);
        }
    }

    private ChannelOwner CreateRemoteObject(string parentGuid, ChannelOwnerType type, string guid, JsonElement? initializer)
    {
        ChannelOwner result = null;
        var parent = string.IsNullOrEmpty(parentGuid) ? _rootObject : Objects[parentGuid];

#pragma warning disable CA2000 // Dispose objects before losing scope
        switch (type)
        {
            case ChannelOwnerType.APIRequestContext:
                result = new APIRequestContext(parent, guid, initializer?.ToObject<APIRequestContextInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Artifact:
                result = new Artifact(parent, guid, initializer?.ToObject<ArtifactInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.BindingCall:
                result = new BindingCall(parent, guid, initializer?.ToObject<BindingCallInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Playwright:
                result = new PlaywrightImpl(parent, guid, initializer?.ToObject<PlaywrightInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Browser:
                var browserInitializer = initializer?.ToObject<BrowserInitializer>(DefaultJsonSerializerOptions);
                result = new Browser(parent, guid, browserInitializer);
                break;
            case ChannelOwnerType.BrowserType:
                var browserTypeInitializer = initializer?.ToObject<BrowserTypeInitializer>(DefaultJsonSerializerOptions);
                result = new Core.BrowserType(parent, guid, browserTypeInitializer);
                break;
            case ChannelOwnerType.BrowserContext:
                var browserContextInitializer = initializer?.ToObject<BrowserContextInitializer>(DefaultJsonSerializerOptions);
                result = new BrowserContext(parent, guid, browserContextInitializer);
                break;
            case ChannelOwnerType.CDPSession:
                result = new CDPSession(parent, guid);
                break;
            case ChannelOwnerType.Dialog:
                result = new Dialog(parent, guid, initializer?.ToObject<DialogInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.ElementHandle:
                result = new ElementHandle(parent, guid, initializer?.ToObject<ElementHandleInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Frame:
                result = new Frame(parent, guid, initializer?.ToObject<FrameInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.JSHandle:
                result = new JSHandle(parent, guid, initializer?.ToObject<JSHandleInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.JsonPipe:
                result = new JsonPipe(parent, guid, initializer?.ToObject<JsonPipeInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.LocalUtils:
                result = new LocalUtils(parent, guid, initializer?.ToObject<LocalUtilsInitializer>(DefaultJsonSerializerOptions));
                if (LocalUtils == null)
                {
                    LocalUtils = result as LocalUtils;
                }
                break;
            case ChannelOwnerType.Page:
                result = new Page(parent, guid, initializer?.ToObject<PageInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Request:
                result = new Request(parent, guid, initializer?.ToObject<RequestInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Response:
                result = new Response(parent, guid, initializer?.ToObject<ResponseInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Route:
                result = new Route(parent, guid, initializer?.ToObject<RouteInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Worker:
                result = new Worker(parent, guid, initializer?.ToObject<WorkerInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.WebSocket:
                result = new WebSocket(parent, guid, initializer?.ToObject<WebSocketInitializer>(DefaultJsonSerializerOptions));
                break;
            case ChannelOwnerType.Selectors:
                result = new Selectors(parent, guid);
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
            default:
                TraceMessage("pw:dotnet", "Missing type " + type);
                break;
        }
#pragma warning restore CA2000
        return result;
    }

    internal void DoClose(Exception cause = null)
    {
        _closedError = cause != null ? new TargetClosedException(cause.Message, cause) : new TargetClosedException();
        foreach (var callback in _callbacks)
        {
            callback.Value.TaskCompletionSource.TrySetException(_closedError);
            // We need to make sure that the task is handled otherwise it will be reported as unhandled on the caller side.
            // Its still possible to get the exception from the task.
            callback.Value.TaskCompletionSource.Task.IgnoreException();
        }
        _callbacks.Clear();

        Dispose();
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
        if (!disposing)
        {
            return;
        }

        _queue.Dispose();
        Close.Invoke(this, new TargetClosedException("Connection disposed"));
    }

    [Conditional("DEBUG")]
    internal void TraceMessage(string logLevel, object message)
    {
        string actualLogLevel = Environment.GetEnvironmentVariable("DEBUG");
        if (!string.IsNullOrEmpty(actualLogLevel))
        {
            if (!actualLogLevel.Contains(logLevel))
            {
                return;
            }
            if (message is not string)
            {
                message = JsonSerializer.Serialize(message, DefaultJsonSerializerOptions);
            }
            if (((string)message).Contains("deviceDescriptors"))
            {
                return;
            }
            string line = $"{logLevel}: {message}";
            Trace.WriteLine(line);
            Console.Error.WriteLine(line);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    internal async Task<T> WrapApiCallAsync<T>(Func<Task<T>> action, bool isInternal = false)
    {
        EnsureApiZoneExists();
        if (ApiZone.Value[0] != null)
        {
            return await action().ConfigureAwait(false);
        }
        var st = new StackTrace(true);
        var stack = new List<Protocol.StackFrame>();
        var lastInternalApiName = string.Empty;
        var apiName = string.Empty;
        var apiBoundaryReached = false;
        for (int i = 0; i < st.FrameCount; ++i)
        {
            var sf = st.GetFrame(i);
            string fileName = sf.GetFileName();
            if (IsPlaywrightInternalNamespace(sf.GetMethod().ReflectedType?.Namespace))
            {
                string methodName = $"{sf?.GetMethod()?.DeclaringType?.Name}.{sf?.GetMethod()?.Name}";
                if (methodName.Contains("WrapApiBoundaryAsync"))
                {
                    apiBoundaryReached = true;
                }
                var hasCleanMethodName = !methodName.StartsWith("<", StringComparison.InvariantCultureIgnoreCase);
                if (hasCleanMethodName)
                {
                    lastInternalApiName = methodName;
                }
            }
            else if (!string.IsNullOrEmpty(fileName))
            {
                stack.Add(new() { File = fileName, Line = sf.GetFileLineNumber(), Column = sf.GetFileColumnNumber() });
                if (!string.IsNullOrEmpty(lastInternalApiName) && !apiBoundaryReached)
                {
                    apiName = lastInternalApiName;
                }
            }
        }
        if (string.IsNullOrEmpty(apiName))
        {
            apiName = lastInternalApiName;
        }
        try
        {
            if (!string.IsNullOrEmpty(apiName))
            {
                ApiZone.Value[0] = new() { ApiName = isInternal ? null : apiName, Frames = stack };
            }
            return await action().ConfigureAwait(false);
        }
        finally
        {
            ApiZone.Value[0] = null;
        }
    }

    internal Task WrapApiCallAsync(Func<Task> action, bool isInternal = false)
        => WrapApiCallAsync(
            async () =>
            {
                await action().ConfigureAwait(false);
                return true;
            },
            isInternal);

    private static bool IsPlaywrightInternalNamespace(string namespaceName)
    {
        return namespaceName != null &&
            (namespaceName == "Microsoft.Playwright" ||
            namespaceName.StartsWith("Microsoft.Playwright.Core", StringComparison.InvariantCultureIgnoreCase) ||
            namespaceName.StartsWith("Microsoft.Playwright.Transport", StringComparison.InvariantCultureIgnoreCase) ||
            namespaceName.StartsWith("Microsoft.Playwright.Helpers", StringComparison.InvariantCultureIgnoreCase));
    }

    [MethodImpl(MethodImplOptions.NoInlining)] // This method is also a stacktrace marker.
    internal async Task WrapApiBoundaryAsync(Func<Task> action)
    {
        EnsureApiZoneExists();
        try
        {
            ApiZone.Value.Insert(0, null);
            await action().ConfigureAwait(false);
        }
        finally
        {
            ApiZone.Value.RemoveAt(0);
        }
    }

    private void EnsureApiZoneExists()
    {
        if (ApiZone.Value == null)
        {
            ApiZone.Value = new() { null };
        }
    }
}

internal class ConnectionCallback
{
    public TaskCompletionSource<JsonElement?> TaskCompletionSource { get; set; }
}
