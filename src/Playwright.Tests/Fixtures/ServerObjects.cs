/*
 * MIT License
 *
 * Copyright (c) Microsoft Corporation.
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and / or sell
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

using System.Collections;
using System.Globalization;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Net.WebSockets;
using System.Text;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>
/// <c>req.headers</c> of Node.js: the names are lower-case, a header that the request does not have is null.
/// </summary>
public sealed class ServerHeaders : IReadOnlyDictionary<string, string>
{
    // Node.js keeps the first value of these headers, and joins the values of the others.
    private static readonly HashSet<string> _singleValue = new(StringComparer.Ordinal)
    {
        "age", "authorization", "content-length", "content-type", "etag", "expires", "from", "host", "if-modified-since",
        "if-unmodified-since", "last-modified", "location", "max-forwards", "proxy-authorization", "referer", "retry-after",
        "server", "user-agent",
    };

    private readonly Dictionary<string, string> _headers = new(StringComparer.Ordinal);

    internal ServerHeaders(IReadOnlyList<string> rawHeaders)
    {
        for (var i = 0; i + 1 < rawHeaders.Count; i += 2)
        {
            var name = rawHeaders[i].ToLowerInvariant();
            var value = rawHeaders[i + 1];
            if (!_headers.TryGetValue(name, out var existing))
            {
                _headers[name] = value;
            }
            else if (!_singleValue.Contains(name))
            {
                _headers[name] = existing + (name == "cookie" ? "; " : ", ") + value;
            }
        }
    }

    public IEnumerable<string> Keys => _headers.Keys;

    public IEnumerable<string> Values => _headers.Values;

    public int Count => _headers.Count;

    public string this[string name] => _headers.GetValueOrDefault(name.ToLowerInvariant());

    public bool ContainsKey(string name) => _headers.ContainsKey(name.ToLowerInvariant());

    public bool TryGetValue(string name, out string value) => _headers.TryGetValue(name.ToLowerInvariant(), out value);

    public IEnumerator<KeyValuePair<string, string>> GetEnumerator() => _headers.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

/// <summary><c>req.socket</c>, <c>res.socket</c> and <c>res.connection</c> of Node.js.</summary>
public sealed class ServerSocket
{
    private readonly TcpClient _client;
    private readonly byte[] _buffer = new byte[64 * 1024];
    private readonly object _lock = new();
    private readonly TaskCompletionSource<bool> _closed = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<Action> _closeListeners = [];
    private int _start;
    private int _end;
    private Task _writes = Task.CompletedTask;
    private bool _destroyRequested;

    internal ServerSocket(TcpClient client)
    {
        _client = client;
        Stream = client.GetStream();
        if (client.Client.RemoteEndPoint is IPEndPoint remote)
        {
            RemoteAddress = remote.Address.IsIPv4MappedToIPv6 ? remote.Address.MapToIPv4().ToString() : remote.Address.ToString();
            RemotePort = remote.Port;
        }
    }

    public string RemoteAddress { get; }

    public int RemotePort { get; }

    public bool Destroyed => _closed.Task.IsCompleted;

    internal Stream Stream { get; set; }

    internal Task Closed => _closed.Task;

    /// <summary><c>socket.write(data)</c>.</summary>
    public void Write(string data) => Write(Encoding.UTF8.GetBytes(data));

    /// <summary><c>socket.write(data)</c>.</summary>
    public void Write(byte[] data) => Enqueue(() => Stream.WriteAsync(data, 0, data.Length));

    /// <summary><c>socket.end()</c>: sends what was written, then closes the sending side.</summary>
    public void End()
    {
        Enqueue(async () =>
        {
            await Stream.FlushAsync().ConfigureAwait(false);
            if (Stream is SslStream ssl)
            {
                await ssl.ShutdownAsync().ConfigureAwait(false);
            }
            _client.Client.Shutdown(SocketShutdown.Send);
        });
    }

    /// <summary><c>socket.destroy()</c>.</summary>
    public void Destroy()
    {
        Task writes;
        lock (_lock)
        {
            if (_destroyRequested)
            {
                return;
            }
            _destroyRequested = true;
            writes = _writes;
        }
        if (writes.IsCompleted)
        {
            Close(false);
            return;
        }
        // Node.js has given the earlier writes to the system when destroy() runs: they are sent.
        _ = Task.WhenAny(writes, Task.Delay(1000)).ContinueWith(_ => Close(false), TaskScheduler.Default);
    }

    /// <summary><c>socket.resetAndDestroy()</c>: closes with a TCP reset.</summary>
    public void ResetAndDestroy() => Close(true);

    /// <summary><c>socket.uncork()</c>: the writes are not corked.</summary>
    public void Uncork()
    {
    }

    /// <summary><c>socket.setTimeout(timeout)</c>: the server has no idle timeout.</summary>
    public void SetTimeout(int timeout)
    {
    }

    /// <summary><c>socket.on('close', listener)</c>.</summary>
    public void OnClose(Action listener)
    {
        lock (_lock)
        {
            if (!Destroyed)
            {
                _closeListeners.Add(listener);
                return;
            }
        }
        listener();
    }

    internal Task FlushAsync()
    {
        lock (_lock)
        {
            return _writes;
        }
    }

    internal void Enqueue(Func<Task> write)
    {
        lock (_lock)
        {
            if (_destroyRequested)
            {
                return;
            }
            _writes = _writes.ContinueWith(
                async _ =>
                {
                    if (Destroyed)
                    {
                        return;
                    }
                    try
                    {
                        await write().ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        Destroy();
                    }
                },
                TaskScheduler.Default).Unwrap();
        }
    }

    /// <summary>Reads the request line and the headers of the next request. Null when the client closed the connection.</summary>
    internal async Task<ServerRequest> ReadHeadAsync()
    {
        var requestLine = await ReadLineAsync().ConfigureAwait(false);
        while (requestLine != null && requestLine.Length == 0)
        {
            requestLine = await ReadLineAsync().ConfigureAwait(false);
        }
        if (requestLine == null)
        {
            return null;
        }
        var parts = requestLine.Split(' ', 3);
        if (parts.Length < 3)
        {
            throw new IOException("Malformed request line: " + requestLine);
        }
        var rawHeaders = new List<string>();
        for (var line = await ReadLineAsync().ConfigureAwait(false); !string.IsNullOrEmpty(line); line = await ReadLineAsync().ConfigureAwait(false))
        {
            var colon = line.IndexOf(':');
            if (colon == -1)
            {
                throw new IOException("Malformed header: " + line);
            }
            rawHeaders.Add(line.Substring(0, colon).Trim());
            rawHeaders.Add(line.Substring(colon + 1).Trim());
        }
        return new ServerRequest(parts[0], parts[1], parts[2].Replace("HTTP/", string.Empty), rawHeaders, this);
    }

    internal async Task<byte[]> ReadBodyAsync(ServerRequest request)
    {
        using var body = new MemoryStream();
        if (string.Equals(request.Headers["transfer-encoding"], "chunked", StringComparison.OrdinalIgnoreCase))
        {
            while (true)
            {
                var sizeLine = await ReadLineAsync().ConfigureAwait(false) ?? throw new IOException("Unexpected end of chunked body");
                var semicolon = sizeLine.IndexOf(';');
                var size = int.Parse((semicolon == -1 ? sizeLine : sizeLine.Substring(0, semicolon)).Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                if (size == 0)
                {
                    // Trailers, then the empty line.
                    while (!string.IsNullOrEmpty(await ReadLineAsync().ConfigureAwait(false)))
                    {
                    }
                    break;
                }
                await ReadExactlyAsync(body, size).ConfigureAwait(false);
                await ReadLineAsync().ConfigureAwait(false);
            }
        }
        else if (request.Headers["content-length"] is { } contentLength)
        {
            // An upload can have hundreds of megabytes: read it into its array at once.
            var result = new byte[int.Parse(contentLength.Trim(), CultureInfo.InvariantCulture)];
            using var target = new MemoryStream(result);
            await ReadExactlyAsync(target, result.Length).ConfigureAwait(false);
            return result;
        }
        return body.ToArray();
    }

    private void Close(bool reset)
    {
        List<Action> listeners;
        lock (_lock)
        {
            if (Destroyed)
            {
                return;
            }
            _destroyRequested = true;
            _closed.TrySetResult(true);
            listeners = [.. _closeListeners];
            _closeListeners.Clear();
        }
        try
        {
            if (reset)
            {
                _client.LingerState = new LingerOption(true, 0);
            }
            _client.Close();
        }
        catch (Exception)
        {
        }
        foreach (var listener in listeners)
        {
            try
            {
                listener();
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"A 'close' listener of a server socket failed: {e}");
            }
        }
    }

    private async Task<bool> FillAsync()
    {
        if (_start == _end)
        {
            _start = _end = 0;
        }
        else if (_end == _buffer.Length)
        {
            Buffer.BlockCopy(_buffer, _start, _buffer, 0, _end - _start);
            _end -= _start;
            _start = 0;
        }
        var read = await Stream.ReadAsync(_buffer.AsMemory(_end, _buffer.Length - _end)).ConfigureAwait(false);
        _end += read;
        return read > 0;
    }

    /// <summary>A line without its CRLF, or null at the end of the stream.</summary>
    private async Task<string> ReadLineAsync()
    {
        using var line = new MemoryStream();
        while (true)
        {
            if (_start == _end && !await FillAsync().ConfigureAwait(false))
            {
                return line.Length == 0 ? null : Encoding.Latin1.GetString(line.ToArray());
            }
            var newline = Array.IndexOf(_buffer, (byte)'\n', _start, _end - _start);
            if (newline == -1)
            {
                line.Write(_buffer, _start, _end - _start);
                _start = _end;
                continue;
            }
            line.Write(_buffer, _start, newline - _start);
            _start = newline + 1;
            var bytes = line.ToArray();
            var length = bytes.Length > 0 && bytes[^1] == '\r' ? bytes.Length - 1 : bytes.Length;
            return Encoding.Latin1.GetString(bytes, 0, length);
        }
    }

    private async Task ReadExactlyAsync(MemoryStream target, int size)
    {
        while (size > 0)
        {
            if (_start == _end && !await FillAsync().ConfigureAwait(false))
            {
                throw new IOException("Unexpected end of body");
            }
            var count = Math.Min(size, _end - _start);
            target.Write(_buffer, _start, count);
            _start += count;
            size -= count;
        }
    }
}

/// <summary><c>http.IncomingMessage</c> of Node.js, with <c>postBody</c>: the request of a route handler.</summary>
public sealed class ServerRequest
{
    internal ServerRequest(string method, string url, string httpVersion, List<string> rawHeaders, ServerSocket socket)
    {
        Method = method;
        Url = url;
        HttpVersion = httpVersion;
        RawHeaders = rawHeaders;
        Headers = new ServerHeaders(rawHeaders);
        Socket = socket;

        var target = url;
        if (!target.StartsWith('/') && Uri.TryCreate(target, UriKind.Absolute, out var absolute))
        {
            // A request through a proxy has the whole URL.
            target = absolute.PathAndQuery;
        }
        var hash = target.IndexOf('#');
        if (hash != -1)
        {
            target = target.Substring(0, hash);
        }
        var query = target.IndexOf('?');
        PathName = query == -1 ? target : target.Substring(0, query);
        PathWithSearch = query == target.Length - 1 ? PathName : target;
    }

    /// <summary><c>req.method</c>.</summary>
    public string Method { get; }

    /// <summary><c>req.url</c>: the path and the query.</summary>
    public string Url { get; }

    /// <summary><c>req.httpVersion</c>: '1.1'.</summary>
    public string HttpVersion { get; }

    /// <summary><c>req.headers</c>: lower-case names.</summary>
    public ServerHeaders Headers { get; }

    /// <summary><c>req.rawHeaders</c>: names and values in turn, as the client sent them.</summary>
    public IReadOnlyList<string> RawHeaders { get; }

    /// <summary><c>req.postBody</c>.</summary>
    public Task<byte[]> PostBody { get; internal set; }

    /// <summary><c>req.socket</c>.</summary>
    public ServerSocket Socket { get; }

    internal string PathName { get; }

    internal string PathWithSearch { get; }

    /// <summary><c>req.destroy()</c>.</summary>
    public void Destroy() => Socket.Destroy();
}

/// <summary><c>http.ServerResponse</c> of Node.js: the response of a route handler.</summary>
public sealed class ServerResponse
{
    private static readonly Dictionary<int, string> _statusMessages = new()
    {
        [100] = "Continue",
        [101] = "Switching Protocols",
        [200] = "OK",
        [201] = "Created",
        [202] = "Accepted",
        [204] = "No Content",
        [206] = "Partial Content",
        [300] = "Multiple Choices",
        [301] = "Moved Permanently",
        [302] = "Found",
        [303] = "See Other",
        [304] = "Not Modified",
        [307] = "Temporary Redirect",
        [308] = "Permanent Redirect",
        [400] = "Bad Request",
        [401] = "Unauthorized",
        [402] = "Payment Required",
        [403] = "Forbidden",
        [404] = "Not Found",
        [405] = "Method Not Allowed",
        [407] = "Proxy Authentication Required",
        [408] = "Request Timeout",
        [409] = "Conflict",
        [410] = "Gone",
        [418] = "I'm a Teapot",
        [422] = "Unprocessable Entity",
        [429] = "Too Many Requests",
        [500] = "Internal Server Error",
        [501] = "Not Implemented",
        [502] = "Bad Gateway",
        [503] = "Service Unavailable",
        [504] = "Gateway Timeout",
    };

    private readonly object _lock = new();
    private readonly ServerRequest _request;
    private readonly List<KeyValuePair<string, string[]>> _headers = [];
    private readonly TaskCompletionSource<bool> _ended = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly List<Action> _finishListeners = [];
    private bool _chunked;
    private bool _removedContentLength;
    private bool _headStored;

    internal ServerResponse(ServerRequest request, ServerSocket socket)
    {
        _request = request;
        Socket = socket;
        KeepAlive = request.HttpVersion != "1.0" && !string.Equals(request.Headers["connection"], "close", StringComparison.OrdinalIgnoreCase);
        _ = socket.Closed.ContinueWith(_ => _ended.TrySetResult(false), TaskScheduler.Default);
    }

    /// <summary><c>res.statusCode</c>.</summary>
    public int StatusCode { get; set; } = 200;

    /// <summary><c>res.statusMessage</c>.</summary>
    public string StatusMessage { get; set; }

    /// <summary><c>res.headersSent</c>.</summary>
    public bool HeadersSent { get; private set; }

    /// <summary><c>res.writableEnded</c>.</summary>
    public bool WritableEnded { get; private set; }

    /// <summary><c>res.socket</c>.</summary>
    public ServerSocket Socket { get; }

    /// <summary><c>res.connection</c>.</summary>
    public ServerSocket Connection => Socket;

    internal Task Ended => _ended.Task;

    internal bool KeepAlive { get; private set; }

    private bool HasBody => _request.Method != "HEAD" && StatusCode != 204 && StatusCode != 304 && StatusCode >= 200;

    /// <summary><c>res.setHeader(name, value)</c>. The value is a string, a number or an array of strings.</summary>
    public void SetHeader(string name, object value)
    {
        lock (_lock)
        {
            RemoveHeaderLocked(name);
            _headers.Add(new(name, ToValues(value)));
        }
    }

    /// <summary><c>res.getHeader(name)</c>.</summary>
    public string GetHeader(string name)
    {
        lock (_lock)
        {
            var index = _headers.FindIndex(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase));
            return index == -1 ? null : string.Join(", ", _headers[index].Value);
        }
    }

    /// <summary><c>res.removeHeader(name)</c>.</summary>
    public void RemoveHeader(string name)
    {
        lock (_lock)
        {
            RemoveHeaderLocked(name);
            if (string.Equals(name, "content-length", StringComparison.OrdinalIgnoreCase))
            {
                _removedContentLength = true;
            }
        }
    }

    /// <summary><c>res.writeHead(statusCode, headers)</c>.</summary>
    public void WriteHead(int statusCode, Dictionary<string, object> headers = null) => WriteHead(statusCode, null, headers);

    /// <summary><c>res.writeHead(statusCode, statusMessage, headers)</c>.</summary>
    public void WriteHead(int statusCode, string statusMessage, Dictionary<string, object> headers = null)
    {
        lock (_lock)
        {
            StatusCode = statusCode;
            StatusMessage = statusMessage ?? StatusMessage;
            // Node.js makes the header in writeHead(), before it knows the length of the body:
            // a response without a Content-Length header is chunked then.
            _headStored = true;
            if (headers != null)
            {
                // Node.js writes the headers of writeHead() as they are, also names that differ in case
                // only; it replaces a header by name only when setHeader() was called before.
                var replace = _headers.Count > 0;
                foreach (var (name, value) in headers)
                {
                    if (replace)
                    {
                        RemoveHeaderLocked(name);
                    }
                    _headers.Add(new(name, ToValues(value)));
                }
            }
        }
    }

    /// <summary>
    /// <c>res.writeHead(statusCode, rawHeaders)</c> with an array of names and values in turn: the headers
    /// are sent as written, also two headers of the same name.
    /// </summary>
    public void WriteHeadRaw(int statusCode, params string[] rawHeaders)
    {
        lock (_lock)
        {
            StatusCode = statusCode;
            _headStored = true;
            for (var i = 0; i + 1 < rawHeaders.Length; i += 2)
            {
                _headers.Add(new(rawHeaders[i], [rawHeaders[i + 1]]));
            }
        }
    }

    /// <summary><c>res.flushHeaders()</c>.</summary>
    public void FlushHeaders()
    {
        lock (_lock)
        {
            SendHeadersLocked(null);
        }
    }

    /// <summary><c>res.write(chunk)</c>.</summary>
    public void Write(string chunk) => Write(Encoding.UTF8.GetBytes(chunk));

    /// <summary><c>res.write(chunk)</c>.</summary>
    public void Write(byte[] chunk)
    {
        lock (_lock)
        {
            if (WritableEnded)
            {
                return;
            }
            SendHeadersLocked(null);
            WriteBodyLocked(chunk);
        }
    }

    /// <summary><c>await new Promise(f => res.write(chunk, f))</c>: writes the chunk and waits until it is sent.</summary>
    public Task WriteAsync(string chunk) => WriteAsync(Encoding.UTF8.GetBytes(chunk));

    /// <summary><c>await new Promise(f => res.write(chunk, f))</c>: writes the chunk and waits until it is sent.</summary>
    public Task WriteAsync(byte[] chunk)
    {
        Write(chunk);
        return Socket.FlushAsync();
    }

    /// <summary><c>await new Promise(f => res.end(data, f))</c>: ends the response and waits until it is sent.</summary>
    public Task EndAsync(string data = null) => EndAsync(data == null ? null : Encoding.UTF8.GetBytes(data));

    /// <summary><c>await new Promise(f => res.end(data, f))</c>: ends the response and waits until it is sent.</summary>
    public Task EndAsync(byte[] data)
    {
        End(data);
        return Socket.FlushAsync();
    }

    /// <summary><c>res.end()</c>.</summary>
    public void End() => End((byte[])null);

    /// <summary><c>res.end(data)</c>.</summary>
    public void End(string data) => End(data == null ? null : Encoding.UTF8.GetBytes(data));

    /// <summary><c>res.end(data)</c>.</summary>
    public void End(byte[] data)
    {
        List<Action> listeners;
        lock (_lock)
        {
            if (WritableEnded)
            {
                return;
            }
            WritableEnded = true;
            SendHeadersLocked(data?.Length ?? 0);
            if (data != null)
            {
                WriteBodyLocked(data);
            }
            if (_chunked)
            {
                Socket.Write("0\r\n\r\n");
            }
            listeners = [.. _finishListeners];
        }
        Socket.Enqueue(() =>
        {
            foreach (var listener in listeners)
            {
                listener();
            }
            _ended.TrySetResult(true);
            return Task.CompletedTask;
        });
    }

    /// <summary><c>res.uncork()</c>: the writes are not corked.</summary>
    public void Uncork()
    {
    }

    /// <summary><c>res.on('finish', listener)</c>.</summary>
    public void OnFinish(Action listener)
    {
        lock (_lock)
        {
            _finishListeners.Add(listener);
        }
    }

    private static string[] ToValues(object value) => value switch
    {
        null => [string.Empty],
        string text => [text],
        IEnumerable<string> list => [.. list],
        IFormattable number => [number.ToString(null, CultureInfo.InvariantCulture)],
        _ => [value.ToString()],
    };

    private void RemoveHeaderLocked(string name) => _headers.RemoveAll(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase));

    private bool HasHeaderLocked(string name) => _headers.Exists(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase));

    private void WriteBodyLocked(byte[] chunk)
    {
        if (!HasBody || chunk.Length == 0)
        {
            return;
        }
        if (_chunked)
        {
            Socket.Write(Encoding.ASCII.GetBytes(chunk.Length.ToString("x", CultureInfo.InvariantCulture) + "\r\n"));
            Socket.Write(chunk);
            Socket.Write("\r\n");
        }
        else
        {
            Socket.Write(chunk);
        }
    }

    /// <summary>Sends the status line and the headers once. <paramref name="knownLength"/> is the length of the whole body, when <c>end(data)</c> is the first write.</summary>
    private void SendHeadersLocked(int? knownLength)
    {
        if (HeadersSent)
        {
            return;
        }
        HeadersSent = true;
        var head = new StringBuilder();
        var message = StatusMessage ?? _statusMessages.GetValueOrDefault(StatusCode, "unknown");
        head.Append(CultureInfo.InvariantCulture, $"HTTP/1.1 {StatusCode} {message}\r\n");
        foreach (var (name, values) in _headers)
        {
            foreach (var value in values)
            {
                head.Append(name).Append(": ").Append(value).Append("\r\n");
            }
        }
        if (!HasHeaderLocked("date"))
        {
            head.Append("Date: ").Append(DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture)).Append("\r\n");
        }
        if (HasHeaderLocked("connection"))
        {
            KeepAlive = KeepAlive && !string.Equals(GetHeaderLockedValue("connection"), "close", StringComparison.OrdinalIgnoreCase);
        }
        else if (KeepAlive)
        {
            head.Append("Connection: keep-alive\r\nKeep-Alive: timeout=5\r\n");
        }
        else
        {
            head.Append("Connection: close\r\n");
        }
        var hasLength = HasHeaderLocked("content-length");
        if (HasHeaderLocked("transfer-encoding"))
        {
            _chunked = (GetHeaderLockedValue("transfer-encoding") ?? string.Empty).Contains("chunked", StringComparison.OrdinalIgnoreCase);
        }
        else if (!hasLength && HasBody)
        {
            if (knownLength != null && !_removedContentLength && !_headStored)
            {
                head.Append(CultureInfo.InvariantCulture, $"Content-Length: {knownLength}\r\n");
            }
            else if (_request.HttpVersion == "1.0")
            {
                KeepAlive = false;
            }
            else
            {
                head.Append("Transfer-Encoding: chunked\r\n");
                _chunked = true;
            }
        }
        head.Append("\r\n");
        Socket.Write(Encoding.Latin1.GetBytes(head.ToString()));
    }

    private string GetHeaderLockedValue(string name)
    {
        var index = _headers.FindIndex(header => string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase));
        return index == -1 ? null : string.Join(", ", _headers[index].Value);
    }
}

/// <summary>A message of a <see cref="ServerWebSocket"/>.</summary>
public sealed class ServerWebSocketMessage
{
    internal ServerWebSocketMessage(byte[] data, bool isBinary)
    {
        Data = data;
        IsBinary = isBinary;
    }

    public byte[] Data { get; }

    public bool IsBinary { get; }

    /// <summary><c>data.toString()</c>.</summary>
    public string Text => Encoding.UTF8.GetString(Data);

    public override string ToString() => Text;
}

/// <summary>
/// The server side of a WebSocket connection, <c>WebSocket</c> of the <c>ws</c> package:
/// <c>ws.on('message', data => ...)</c> is <c>ws.OnMessage(message => ...)</c>.
/// A message or a close that comes before the first listener waits for it.
/// </summary>
public sealed class ServerWebSocket
{
    private readonly object _lock = new();
    private readonly ServerSocket _socket;
    private readonly WebSocket _webSocket;
    private readonly List<(Action<ServerWebSocketMessage> Listener, bool Once)> _messageListeners = [];
    private readonly List<Action<int, string>> _closeListeners = [];
    private readonly List<Action<Exception>> _errorListeners = [];
    private readonly Queue<ServerWebSocketMessage> _pendingMessages = new();
    private (int Code, string Reason)? _closed;

    internal ServerWebSocket(ServerSocket socket)
    {
        _socket = socket;
        _webSocket = WebSocket.CreateFromStream(socket.Stream, new WebSocketCreationOptions { IsServer = true, KeepAliveInterval = TimeSpan.Zero });
    }

    /// <summary><c>ws.readyState</c>: 1 open, 2 closing, 3 closed.</summary>
    public int ReadyState => _webSocket.State switch
    {
        WebSocketState.Open => 1,
        WebSocketState.CloseSent or WebSocketState.CloseReceived => 2,
        _ => 3,
    };

    /// <summary><c>ws.send(text)</c>.</summary>
    public void Send(string data) => EnqueueSend(() => _webSocket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None));

    /// <summary><c>ws.send(buffer)</c>.</summary>
    public void Send(byte[] data) => EnqueueSend(() => _webSocket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None));

    /// <summary><c>await new Promise((f, r) => ws.send(text, error => error ? r(error) : f()))</c>: sends the text and waits until it is sent. It fails when the connection is not open or the send fails.</summary>
    public Task SendAsync(string data) => SendAsync(() => _webSocket.SendAsync(Encoding.UTF8.GetBytes(data), WebSocketMessageType.Text, true, CancellationToken.None));

    /// <summary><c>await new Promise((f, r) => ws.send(buffer, error => error ? r(error) : f()))</c>.</summary>
    public Task SendAsync(byte[] data) => SendAsync(() => _webSocket.SendAsync(data, WebSocketMessageType.Binary, true, CancellationToken.None));

    /// <summary><c>ws.close(code, reason)</c>.</summary>
    public void Close(int? code = null, string reason = null)
    {
        EnqueueSend(async () =>
        {
            if (_webSocket.State is WebSocketState.Open or WebSocketState.CloseReceived)
            {
                await _webSocket.CloseOutputAsync(code == null ? WebSocketCloseStatus.Empty : (WebSocketCloseStatus)code.Value, code == null ? null : reason ?? string.Empty, CancellationToken.None).ConfigureAwait(false);
            }
        });
    }

    /// <summary><c>ws.terminate()</c>: closes the connection without a close frame.</summary>
    public void Terminate() => _socket.Destroy();

    /// <summary><c>ws.on('message', listener)</c>.</summary>
    public void OnMessage(Action<ServerWebSocketMessage> listener) => AddMessageListener(listener, false);

    /// <summary><c>ws.once('message', listener)</c>.</summary>
    public void OnceMessage(Action<ServerWebSocketMessage> listener) => AddMessageListener(listener, true);

    /// <summary><c>ws.on('close', (code, reason) => ...)</c>.</summary>
    public void OnClose(Action<int, string> listener)
    {
        (int Code, string Reason)? closed;
        lock (_lock)
        {
            closed = _closed;
            if (closed == null)
            {
                _closeListeners.Add(listener);
                return;
            }
        }
        listener(closed.Value.Code, closed.Value.Reason);
    }

    /// <summary><c>ws.on('error', error => ...)</c>: the connection was reset, or a send failed. A 'close' follows.</summary>
    public void OnError(Action<Exception> listener)
    {
        lock (_lock)
        {
            _errorListeners.Add(listener);
        }
    }

    /// <summary><c>new Promise(f => ws.once('message', f))</c>.</summary>
    public Task<ServerWebSocketMessage> WaitForMessage()
    {
        var result = new TaskCompletionSource<ServerWebSocketMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        OnceMessage(message => result.TrySetResult(message));
        return result.Task;
    }

    /// <summary><c>new Promise(f => ws.once('close', (code, reason) => f({ code, reason })))</c>.</summary>
    public Task<(int Code, string Reason)> WaitForClose()
    {
        var result = new TaskCompletionSource<(int, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
        OnClose((code, reason) => result.TrySetResult((code, reason)));
        return result.Task;
    }

    internal void Start() => _ = ReceiveLoopAsync();

    private void AddMessageListener(Action<ServerWebSocketMessage> listener, bool once)
    {
        while (true)
        {
            ServerWebSocketMessage pending;
            lock (_lock)
            {
                if (_pendingMessages.Count == 0)
                {
                    _messageListeners.Add((listener, once));
                    return;
                }
                pending = _pendingMessages.Dequeue();
            }
            listener(pending);
            if (once)
            {
                return;
            }
        }
    }

    // The sends go after the response of the upgrade, in the queue of the socket.
    private void EnqueueSend(Func<Task> send) => _socket.Enqueue(send);

    private Task SendAsync(Func<Task> send)
    {
        var result = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (ReadyState != 1)
        {
            result.TrySetException(new InvalidOperationException($"WebSocket is not open: readyState {ReadyState} ({(ReadyState == 2 ? "CLOSING" : "CLOSED")})"));
            return result.Task;
        }
        EnqueueSend(async () =>
        {
            try
            {
                await send().ConfigureAwait(false);
                result.TrySetResult(true);
            }
            catch (Exception e)
            {
                result.TrySetException(e);
                if (!_socket.Destroyed)
                {
                    EmitError(e);
                }
                throw;
            }
        });
        // A send that the closed socket has dropped.
        _ = _socket.Closed.ContinueWith(_ => result.TrySetException(new IOException("The WebSocket connection is closed")), TaskScheduler.Default);
        return result.Task;
    }

    private void EmitError(Exception error)
    {
        List<Action<Exception>> listeners;
        lock (_lock)
        {
            listeners = [.. _errorListeners];
        }
        foreach (var listener in listeners)
        {
            listener(error);
        }
    }

    private static bool IsConnectionReset(Exception error)
    {
        for (var e = error; e != null; e = e.InnerException)
        {
            if (e is SocketException { SocketErrorCode: SocketError.ConnectionReset or SocketError.ConnectionAborted })
            {
                return true;
            }
        }
        return false;
    }

    private async Task ReceiveLoopAsync()
    {
        var code = 1006;
        var reason = string.Empty;
        var buffer = new byte[64 * 1024];
        try
        {
            while (true)
            {
                using var message = new MemoryStream();
                WebSocketReceiveResult result;
                do
                {
                    result = await _webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None).ConfigureAwait(false);
                    message.Write(buffer, 0, result.Count);
                }
                while (!result.EndOfMessage);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    code = result.CloseStatus == null ? 1005 : (int)result.CloseStatus.Value;
                    reason = result.CloseStatusDescription ?? string.Empty;
                    var echo = result.CloseStatus == null;
                    EnqueueSend(async () =>
                    {
                        if (_webSocket.State == WebSocketState.CloseReceived)
                        {
                            await _webSocket.CloseOutputAsync(echo ? WebSocketCloseStatus.Empty : result.CloseStatus.Value, echo ? null : reason, CancellationToken.None).ConfigureAwait(false);
                        }
                    });
                    break;
                }
                Dispatch(new ServerWebSocketMessage(message.ToArray(), result.MessageType == WebSocketMessageType.Binary));
            }
        }
        catch (Exception e)
        {
            // The client went away without a close frame. Node.js has an 'error' before the 'close' when it reset the connection.
            if (!_socket.Destroyed && IsConnectionReset(e))
            {
                EmitError(e);
            }
        }
        await _socket.FlushAsync().ConfigureAwait(false);
        _socket.Destroy();
        List<Action<int, string>> listeners;
        lock (_lock)
        {
            _closed = (code, reason);
            listeners = [.. _closeListeners];
            _closeListeners.Clear();
        }
        foreach (var listener in listeners)
        {
            listener(code, reason);
        }
    }

    private void Dispatch(ServerWebSocketMessage message)
    {
        List<Action<ServerWebSocketMessage>> listeners;
        lock (_lock)
        {
            if (_messageListeners.Count == 0)
            {
                _pendingMessages.Enqueue(message);
                return;
            }
            listeners = _messageListeners.Select(entry => entry.Listener).ToList();
            _messageListeners.RemoveAll(entry => entry.Once);
        }
        foreach (var listener in listeners)
        {
            listener(message);
        }
    }
}
