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
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;

using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
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

    private static readonly string[] SensitiveLogKeys =
    [
        "password",
        "secret",
        "token",
        "authorization",
        "set-cookie",
        "cookie",
        "storageState",
        "credentials",
        "auth",
        "key",
        "apiKey",
        "api_key",
        "accessKey",
        "secretKey",
        "privateKey",
    ];

    private readonly ConcurrentDictionary<int, ConnectionCallback> _callbacks = new();
    private readonly Root _rootObject;
    private readonly TaskQueue _queue = new();
    private int _tracingCount;
    private int _lastId;
    private Exception? _closedError;

    public Connection(LocalUtils? localUtils = null)
    {
        _rootObject = new(null, this, string.Empty);
        LocalUtils = localUtils;

        JsonSerializerOptions NewJsonSerializerOptions(bool keepNulls)
        {
            var options = JsonExtensions.GetNewDefaultSerializerOptions(keepNulls);
            options.Converters.Add(new ChannelOwnerToGuidConverter(this));
            options.Converters.Add(new ChannelOwnerListToGuidListConverter(this));
            return options;
        }
        DefaultJsonSerializerOptions = NewJsonSerializerOptions(false);
        DefaultJsonSerializerOptionsKeepNulls = NewJsonSerializerOptions(true);
    }

    /// <inheritdoc cref="IDisposable.Dispose"/>
    ~Connection() => Dispose(false);

    internal event EventHandler<Exception>? Close;

    public ConcurrentDictionary<string, ChannelOwner> Objects { get; } = new();

    internal AsyncLocal<List<ApiZone?>> ApiZone { get; } = new();

    internal bool IsRemote { get; set; }

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
        bool hasAny = false;
        foreach (var l in log)
        {
            if (l != null)
            {
                hasAny = true;
                break;
            }
        }
        if (!hasAny)
        {
            return string.Empty;
        }
        return "\nCall log:\n" + string.Join("\n", log);
    }

    internal static object? NormalizeValue(object? value)
        => NormalizeValue(value, new NormalizationState());

    private static object? NormalizeValue(object? value, NormalizationState state)
    {
        if (value == null)
        {
            return null;
        }
        if (value is ChannelOwner co)
        {
            return new JsonObject { ["guid"] = JsonValue.Create(co.Guid) };
        }
        if (value is IEnumerable<ChannelOwner> channelOwners)
        {
            var array = new JsonArray();
            foreach (var channelOwner in channelOwners)
            {
                array.Add((JsonNode?)new JsonObject { ["guid"] = JsonValue.Create(channelOwner.Guid) });
            }

            return array;
        }
        if (value is Enum e)
        {
            return AotEnumMemberConverter.ToWireString(e);
        }

        var type = value.GetType();
        if (type == typeof(string) || type.IsPrimitive || type == typeof(decimal))
        {
            return value;
        }

        if (value is byte[] bytes)
        {
            return JsonValue.Create(Convert.ToBase64String(bytes));
        }

        if (value is JsonNode jsonNode)
        {
            return jsonNode.DeepClone();
        }

        if (value is JsonElement jsonElement)
        {
            return JsonNode.Parse(jsonElement.GetRawText());
        }

        if (value is IDictionary dictionary)
        {
            state.Enter(value, type);
            try
            {
                var node = new JsonObject();
                foreach (DictionaryEntry entry in dictionary)
                {
                    if (entry.Key is not string key)
                    {
                        throw new PlaywrightException(
                            $"Dictionary type '{type.FullName}' contains a non-string key. " +
                            "Protocol message arguments require Dictionary<string, object?> or another dictionary with string keys.");
                    }

                    node[key] = ToJsonNode(NormalizeValue(entry.Value, state));
                }

                return node;
            }
            finally
            {
                state.Leave(value);
            }
        }

        if (value is IEnumerable<KeyValuePair<string, object>> objectPairs)
        {
            state.Enter(value, type);
            try
            {
                var node = new JsonObject();
                foreach (var pair in objectPairs)
                {
                    node[pair.Key] = ToJsonNode(NormalizeValue(pair.Value, state));
                }

                return node;
            }
            finally
            {
                state.Leave(value);
            }
        }

        if (value is IList list)
        {
            state.Enter(value, type);
            try
            {
                var node = new JsonArray();
                foreach (var item in list)
                {
                    node.Add(ToJsonNode(NormalizeValue(item, state)));
                }

                return node;
            }
            finally
            {
                state.Leave(value);
            }
        }

        if (value is IEnumerable enumerable)
        {
            state.Enter(value, type);
            try
            {
                var node = new JsonArray();
                foreach (var item in enumerable)
                {
                    node.Add(ToJsonNode(NormalizeValue(item, state)));
                }

                return node;
            }
            finally
            {
                state.Leave(value);
            }
        }

        // For non-collection types registered in PlaywrightJsonContext, serialize
        // to JsonNode instead of passing raw objects to Dictionary<string, object?>.
        var knownTypeInfo = PlaywrightJsonContext.Default.GetTypeInfo(type)
            ?? EvaluateArgumentValueConverter.GetExtraTypeInfo(type);
        if (knownTypeInfo != null)
        {
            return JsonSerializer.SerializeToNode(value, knownTypeInfo) ?? value;
        }

        throw new PlaywrightException(
            $"Type '{type.FullName}' is not registered for AOT-safe protocol argument serialization. " +
            "Use primitives, JsonElement, JsonNode, dictionaries with string keys, arrays, or a type registered in PlaywrightJsonContext.");
    }

    private static JsonNode? ToJsonNode(object? value)
    {
        if (value == null)
        {
            return null;
        }

        if (value is JsonNode jn)
        {
            return jn;
        }

        if (value is string s)
        {
            return JsonValue.Create(s);
        }

        if (value is int i)
        {
            return JsonValue.Create(i);
        }

        if (value is long l)
        {
            return JsonValue.Create(l);
        }

        if (value is double d)
        {
            return JsonValue.Create(d);
        }

        if (value is bool b)
        {
            return JsonValue.Create(b);
        }

        if (value is decimal m)
        {
            return JsonValue.Create(m);
        }

        if (value is float f)
        {
            return JsonValue.Create(f);
        }

        if (value is short sh)
        {
            return JsonValue.Create((int)sh);
        }

        if (value is ushort ush)
        {
            return JsonValue.Create((int)ush);
        }

        if (value is byte by)
        {
            return JsonValue.Create((int)by);
        }

        if (value is sbyte sby)
        {
            return JsonValue.Create((int)sby);
        }

        if (value is uint ui)
        {
            return JsonValue.Create(ui);
        }

        if (value is ulong ul)
        {
            return JsonValue.Create(ul);
        }

        if (value is char ch)
        {
            return JsonValue.Create(ch.ToString());
        }

        throw new PlaywrightException(
            $"Value of type '{value.GetType().FullName}' cannot be converted to an AOT-safe JSON protocol argument.");
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
        bool keepNulls = false)
        => SendMessageToServerAsync<JsonElement?>(@object, method, args, keepNulls);

    internal Task<T> SendMessageToServerAsync<T>(
        ChannelOwner? @object,
        string method,
        Dictionary<string, object?>? args = null,
        bool keepNulls = false) => WrapApiCallAsync(() => InnerSendMessageToServerAsync<T>(@object, method, args, keepNulls), false, null);

    private async Task<T> InnerSendMessageToServerAsync<T>(
        ChannelOwner? @object,
        string method,
        Dictionary<string, object?>? dictionary = null,
        bool keepNulls = false)
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

        int id = Interlocked.Increment(ref _lastId);
        var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        var callback = new ConnectionCallback(tcs);

        if (!isWaitInfo)
        {
            _callbacks.TryAdd(id, callback);
        }

        var sanitizedArgs = new Dictionary<string, object?>();
        if (dictionary != null)
        {
            foreach (var kv in dictionary)
            {
                if (kv.Key != null && (keepNulls || kv.Value != null))
                {
                    sanitizedArgs[kv.Key] = NormalizeValue(kv.Value);
                }
            }
        }
        var (title, isInternal, frames) = (ApiZone.Value![0]!.Title, ApiZone.Value![0]!.Internal, ApiZone.Value![0]!.Frames);
        var metadata = new Dictionary<string, object?>
        {
            ["internal"] = isInternal,
            ["wallTime"] = DateTimeOffset.Now.ToUnixTimeMilliseconds(),
        };
        if (!string.IsNullOrEmpty(title))
        {
            metadata["title"] = NormalizeValue(title);
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

        await _queue.EnqueueAsync(() =>
        {
            var message = new Dictionary<string, object?>
            {
                ["id"] = id,
                ["guid"] = @object?.Guid ?? string.Empty,
                ["method"] = method,
                ["params"] = sanitizedArgs,
                ["metadata"] = metadata,
            };
            return OnMessage(message, keepNulls);
        }).ConfigureAwait(false);

        // Fire-and-forget: server intentionally never replies to __waitInfo__.
        if (isWaitInfo)
        {
            return default!;
        }

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
            foreach (var property in result.Value.EnumerateObject())
            {
                return property.Value.ToObject<T>(DefaultJsonSerializerOptions)!;
            }

            return default!;
        }
        else
        {
            return result.Value.ToObject<T>(DefaultJsonSerializerOptions);
        }
    }

    internal ChannelOwner GetObject(string guid)
    {
        Objects.TryGetValue(guid, out var result);
        return result!;
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

    internal void Dispatch(PlaywrightServerMessage message)
    {
        if (_closedError != null)
        {
            return;
        }
        if (message.Id.HasValue)
        {
            _callbacks.TryRemove(message.Id.Value, out var callback);
            if (callback == null)
            {
                throw new PlaywrightException($"Cannot find command to respond: '{message.Id}'");
            }

            if (message.Error != null && message.Result == null)
            {
                var exception = ParseException(message.Error.Error, FormatCallLog(message.Log));
                exception.Data[ErrorDetailsDataKey] = message.ErrorDetails;
                exception.Data[LogDataKey] = message.Log;
                callback.TaskCompletionSource.TrySetException(exception);
            }
            else
            {
                callback.TaskCompletionSource.TrySetResult(message.Result);
            }
            return;
        }

        try
        {
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
                throw new PlaywrightException($"Missing Playwright type binding for '{type}'.");
        }
        return result;
    }

    internal void DoClose(Exception? cause = null)
        => DoCloseImpl(cause != null ? new TargetClosedException(cause.Message, cause) : new TargetClosedException());

    internal void DoClose(string? cause = null)
        => DoCloseImpl(!string.IsNullOrEmpty(cause) ? new TargetClosedException(cause) : new TargetClosedException());

    internal void DoCloseImpl(Exception closeError)
    {
        _closedError = closeError;
        foreach (var callback in _callbacks)
        {
            callback.Value.TaskCompletionSource.TrySetException(closeError.InnerException ?? closeError);
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
        Close?.Invoke(this, new TargetClosedException("Connection disposed"));
    }

    internal static void TraceMessage(string logLevel, byte[] rawMessage)
    {
        string? actualLogLevel = Environment.GetEnvironmentVariable("DEBUG");
        if (string.IsNullOrEmpty(actualLogLevel))
        {
            return;
        }
        if (!actualLogLevel.Contains(logLevel))
        {
            return;
        }
        var message = UTF8Encoding.UTF8.GetString(rawMessage);

        message = RedactTraceMessage(message);

        string line = $"{logLevel}: {message}";
        Trace.WriteLine(line);
        Console.Error.WriteLine(line);
    }

    internal static string RedactTraceMessage(string message)
    {
        try
        {
            var node = JsonNode.Parse(message);
            if (node == null)
            {
                return message;
            }

            RedactJsonNode(node);
            return node.ToJsonString();
        }
        catch (JsonException)
        {
        }

        foreach (var key in SensitiveLogKeys)
        {
            message = RedactJsonValue(message, key);
        }

        return message;
    }

    private static void RedactJsonNode(JsonNode? node)
    {
        if (node is JsonObject obj)
        {
            List<string>? keysToRedact = null;
            foreach (var property in obj)
            {
                if (IsSensitiveLogKey(property.Key))
                {
                    (keysToRedact ??= []).Add(property.Key);
                }
                else
                {
                    RedactJsonNode(property.Value);
                }
            }

            if (keysToRedact != null)
            {
                foreach (var key in keysToRedact)
                {
                    obj[key] = "***REDACTED***";
                }
            }
        }
        else if (node is JsonArray array)
        {
            foreach (var item in array)
            {
                RedactJsonNode(item);
            }
        }
    }

    private static bool IsSensitiveLogKey(string key)
    {
        foreach (var sensitiveKey in SensitiveLogKeys)
        {
            if (string.Equals(key, sensitiveKey, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static string RedactJsonValue(string json, string key)
    {
        var pattern = $"\"{System.Text.RegularExpressions.Regex.Escape(key)}\"\\s*:\\s*\"(?<value>[^\"]+)\"";
        try
        {
            return System.Text.RegularExpressions.Regex.Replace(json, pattern, $"\"{key}\": \"***REDACTED***\"", System.Text.RegularExpressions.RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(100));
        }
        catch
        {
            return json;
        }
    }

    internal async Task<T> WrapApiCallAsync<T>(Func<Task<T>> action, bool isInternal = false, string? title = null)
    {
        EnsureApiZoneExists();
        if (ApiZone.Value![0] != null)
        {
            return await action().ConfigureAwait(false);
        }
        try
        {
            ApiZone.Value[0] = new() { Internal = isInternal, Title = title, Frames = new() };
            return await action().ConfigureAwait(false);
        }
        finally
        {
            ApiZone.Value[0] = null;
        }
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

    private void EnsureApiZoneExists()
    {
        if (ApiZone.Value == null)
        {
            ApiZone.Value = new() { null };
        }
    }

    private sealed class NormalizationState
    {
        private readonly HashSet<object> _active = new(ReferenceEqualityComparer.Instance);

        internal void Enter(object value, Type type)
        {
            if (!_active.Add(value))
            {
                throw new PlaywrightException(
                    $"Type '{type.FullName}' contains a cycle and cannot be serialized as an AOT-safe protocol message argument.");
            }
        }

        internal void Leave(object value) => _active.Remove(value);
    }

    private sealed class ReferenceEqualityComparer : IEqualityComparer<object>
    {
        internal static ReferenceEqualityComparer Instance { get; } = new();

        public new bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object obj) => System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(obj);
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
