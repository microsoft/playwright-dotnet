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

using System.IO.Compression;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary><c>{ doUpgrade, socket }</c> of <see cref="Server.WaitForUpgrade"/>.</summary>
public sealed class ServerUpgradeActions
{
    internal ServerUpgradeActions(Action doUpgrade, ServerSocket socket)
    {
        DoUpgrade = doUpgrade;
        Socket = socket;
    }

    public Action DoUpgrade { get; }

    public ServerSocket Socket { get; }

    public void Deconstruct(out Action doUpgrade, out ServerSocket socket)
    {
        doUpgrade = DoUpgrade;
        socket = Socket;
    }
}

/// <summary>
/// The HTTP or HTTPS server that the tests load pages from, the port of
/// <c>tests/config/testserver/index.ts</c> (<c>TestServer</c> upstream). It serves the files of the assets
/// directory, the routes of <see cref="SetRoute(string, Action{ServerRequest, ServerResponse})"/>, basic
/// auth, gzip, CSP headers and WebSocket connections on <c>/ws</c>, all on one port.
///
/// A route handler takes a <see cref="ServerRequest"/> and a <see cref="ServerResponse"/> with the members
/// of the Node.js objects that the upstream tests use. As in Node.js, the headers are sent with the first
/// <c>Write</c>, <c>End</c> or <c>FlushHeaders</c>; a response that ends with one <c>End(data)</c> gets a
/// Content-Length, one that is written in parts is chunked. A handler may keep the response and end it later.
/// </summary>
public sealed class Server
{
    private static readonly Dictionary<string, string> _mimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["html"] = "text/html",
        ["htm"] = "text/html",
        ["js"] = "text/javascript",
        ["mjs"] = "text/javascript",
        ["css"] = "text/css",
        ["json"] = "application/json",
        ["map"] = "application/json",
        ["png"] = "image/png",
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["gif"] = "image/gif",
        ["webp"] = "image/webp",
        ["avif"] = "image/avif",
        ["svg"] = "image/svg+xml",
        ["ico"] = "image/vnd.microsoft.icon",
        ["txt"] = "text/plain",
        ["md"] = "text/markdown",
        ["csv"] = "text/csv",
        ["xml"] = "application/xml",
        ["pdf"] = "application/pdf",
        ["zip"] = "application/zip",
        ["wasm"] = "application/wasm",
        ["woff"] = "font/woff",
        ["woff2"] = "font/woff2",
        ["ttf"] = "font/ttf",
        ["otf"] = "font/otf",
        ["mp3"] = "audio/mpeg",
        ["wav"] = "audio/wav",
        ["mp4"] = "video/mp4",
        ["webm"] = "video/webm",
        ["ogv"] = "video/ogg",
        ["ts"] = "video/mp2t",
        ["sh"] = "application/x-sh",
        ["pfx"] = "application/x-pkcs12",
        ["webapp"] = "application/x-web-app-manifest+json",
        ["webmanifest"] = "application/manifest+json",
    };

    private readonly object _lock = new();
    private readonly TcpListener _listener;
    private readonly X509Certificate2 _certificate;
    private readonly string _dirPath;
    private readonly DateTime _startTime = DateTime.UtcNow;
    private readonly Dictionary<string, Func<ServerRequest, ServerResponse, Task>> _routes = [];
    private readonly Dictionary<string, (string Username, string Password)> _auths = [];
    private readonly Dictionary<string, string> _csp = [];
    private readonly Dictionary<string, Dictionary<string, string>> _extraHeaders = [];
    private readonly HashSet<string> _gzipRoutes = [];
    private readonly Dictionary<string, TaskCompletionSource<ServerRequest>> _requestSubscribers = [];
    private readonly List<Action<ServerWebSocket, ServerRequest>> _webSocketListeners = [];
    private readonly HashSet<ServerSocket> _sockets = [];
    private TaskCompletionSource<ServerUpgradeActions> _upgradeCallback;
    private string _cachedPathPrefix;
    private volatile bool _stopped;

    private Server(string dirPath, int port, string loopback, X509Certificate2 certificate)
    {
        _dirPath = dirPath;
        _certificate = certificate;
        _listener = new TcpListener(IPAddress.IPv6Any, port);
        _listener.Server.DualMode = true;
        _listener.Start();

        var crossOrigin = loopback ?? "127.0.0.1";
        var sameOrigin = loopback ?? "localhost";
        var protocol = certificate != null ? "https" : "http";
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Prefix = $"{protocol}://{sameOrigin}:{Port}";
        CrossProcessPrefix = $"{protocol}://{crossOrigin}:{Port}";
        EmptyPage = $"{protocol}://{sameOrigin}:{Port}/empty.html";
        Host = $"{sameOrigin}:{Port}";
        Hostname = sameOrigin;
        HelloWorld = $"{Prefix}/hello-world";
        _ = AcceptLoopAsync();
    }

    /// <summary><c>server.PORT</c>.</summary>
    public int Port { get; }

    /// <summary><c>server.PREFIX</c>.</summary>
    public string Prefix { get; }

    /// <summary><c>server.CROSS_PROCESS_PREFIX</c>.</summary>
    public string CrossProcessPrefix { get; }

    /// <summary><c>server.EMPTY_PAGE</c>.</summary>
    public string EmptyPage { get; }

    /// <summary><c>server.HOST</c>.</summary>
    public string Host { get; }

    /// <summary><c>server.HOSTNAME</c>.</summary>
    public string Hostname { get; }

    /// <summary><c>server.HELLO_WORLD</c>.</summary>
    public string HelloWorld { get; }

    /// <summary><c>TestServer.create(dirPath, port, loopback)</c>.</summary>
    public static Server Create(string dirPath, int port, string loopback = null) => new(dirPath, port, loopback, null);

    /// <summary><c>TestServer.createHTTPS(dirPath, port, loopback)</c>.</summary>
    public static Server CreateHttps(string dirPath, int port, string loopback = null) => new(dirPath, port, loopback, CertOptions());

    /// <summary><c>new TestServer(dirPath, port, loopback, { key, cert })</c>: an HTTPS server with the certificate of <see cref="Certificate"/>.</summary>
    public static Server CreateHttps(string dirPath, int port, string loopback, X509Certificate2 certificate) => new(dirPath, port, loopback, certificate);

    /// <summary>The certificate of the HTTPS server, <c>TestServer.certOptions()</c>.</summary>
    public static X509Certificate2 CertOptions()
    {
        var directory = Path.Combine(Env.UpstreamTestsDir, "config", "testserver");
        return Certificate(Path.Combine(directory, "cert.pem"), Path.Combine(directory, "key.pem"));
    }

    /// <summary><c>{ cert: fs.readFileSync(certFile), key: fs.readFileSync(keyFile) }</c> for a server.</summary>
    public static X509Certificate2 Certificate(string certFile, string keyFile)
    {
        using var certificate = X509Certificate2.CreateFromPemFile(certFile, keyFile);
        // SslStream on Windows and macOS needs a key that is not ephemeral.
        return new X509Certificate2(certificate.Export(X509ContentType.Pfx));
    }

    public void EnableHTTPCache(string pathPrefix) => _cachedPathPrefix = pathPrefix;

    public void SetAuth(string path, string username, string password)
    {
        lock (_lock)
        {
            _auths[path] = (username, password);
        }
    }

    public void EnableGzip(string path)
    {
        lock (_lock)
        {
            _gzipRoutes.Add(path);
        }
    }

    public void SetCSP(string path, string csp)
    {
        lock (_lock)
        {
            _csp[path] = csp;
        }
    }

    public void SetExtraHeaders(string path, Dictionary<string, string> headers)
    {
        lock (_lock)
        {
            _extraHeaders[path] = headers;
        }
    }

    public async Task StopAsync()
    {
        _stopped = true;
        Reset();
        _listener.Stop();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    public void SetContent(string path, string content, string mimeType)
    {
        SetRoute(path, (req, res) =>
        {
            res.WriteHead(200, new() { ["Content-Type"] = mimeType });
            res.End(mimeType == "text/html" ? $"<!DOCTYPE html>{content}" : content);
        });
    }

    /// <summary><c>server.setRoute(path, (req, res) => { ... })</c>.</summary>
    public void SetRoute(string path, Action<ServerRequest, ServerResponse> handler)
    {
        SetRoute(path, (request, response) =>
        {
            handler(request, response);
            return Task.CompletedTask;
        });
    }

    /// <summary><c>server.setRoute(path, async (req, res) => { ... })</c>.</summary>
    public void SetRoute(string path, Func<ServerRequest, ServerResponse, Task> handler)
    {
        lock (_lock)
        {
            _routes[path] = handler;
        }
    }

    /// <summary><c>server.setRoute(path, req => { ... })</c>, a handler that does not answer.</summary>
    public void SetRoute(string path, Action<ServerRequest> handler) => SetRoute(path, (request, _) => handler(request));

    public void SetRedirect(string from, string to)
    {
        SetRoute(from, (req, res) =>
        {
            var headers = new Dictionary<string, object>();
            lock (_lock)
            {
                if (_extraHeaders.TryGetValue(req.Url, out var extra))
                {
                    foreach (var (name, value) in extra)
                    {
                        headers[name] = value;
                    }
                }
            }
            headers["location"] = to;
            res.WriteHead(302, headers);
            res.End();
        });
    }

    /// <summary>
    /// The next request for the path (with the query). The task completes when the route handler of the
    /// request has returned, and fails when the test finishes before the request.
    /// </summary>
    public Task<ServerRequest> WaitForRequest(string path)
    {
        lock (_lock)
        {
            if (!_requestSubscribers.TryGetValue(path, out var subscriber))
            {
                subscriber = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
                // Nobody may await the task of a request that never comes.
                _ = subscriber.Task.ContinueWith(task => _ = task.Exception, TaskScheduler.Default);
                _requestSubscribers[path] = subscriber;
            }
            return subscriber.Task;
        }
    }

    public void Reset()
    {
        List<ServerSocket> sockets;
        List<KeyValuePair<string, TaskCompletionSource<ServerRequest>>> subscribers;
        lock (_lock)
        {
            _routes.Clear();
            _auths.Clear();
            _csp.Clear();
            _extraHeaders.Clear();
            _gzipRoutes.Clear();
            _upgradeCallback = null;
            _webSocketListeners.Clear();
            sockets = [.. _sockets];
            subscribers = [.. _requestSubscribers];
            _requestSubscribers.Clear();
        }
        foreach (var socket in sockets)
        {
            socket.Destroy();
        }
        foreach (var (path, subscriber) in subscribers)
        {
            subscriber.TrySetException(new InvalidOperationException($"Request {path} was not received before the test finished."));
        }
    }

    /// <summary><c>server.serveFile(request, response, filePath)</c>.</summary>
    public void ServeFile(ServerRequest request, ServerResponse response, string filePath = null)
    {
        _ = ServeFileAsync(request, response, filePath).ContinueWith(task => _ = task.Exception, TaskScheduler.Default);
    }

    /// <summary><c>server.onceWebSocketConnection((ws, request) => { ... })</c>.</summary>
    public void OnceWebSocketConnection(Action<ServerWebSocket, ServerRequest> handler)
    {
        lock (_lock)
        {
            _webSocketListeners.Add(handler);
        }
    }

    public void OnceWebSocketConnection(Action<ServerWebSocket> handler) => OnceWebSocketConnection((ws, _) => handler(ws));

    public Task<ServerRequest> WaitForWebSocketConnectionRequest()
    {
        var result = new TaskCompletionSource<ServerRequest>(TaskCreationOptions.RunContinuationsAsynchronously);
        OnceWebSocketConnection((_, request) => result.TrySetResult(request));
        return result.Task;
    }

    public Task<ServerUpgradeActions> WaitForUpgrade()
    {
        var result = new TaskCompletionSource<ServerUpgradeActions>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (_lock)
        {
            _upgradeCallback = result;
        }
        return result.Task;
    }

    public Task<ServerWebSocket> WaitForWebSocket()
    {
        var result = new TaskCompletionSource<ServerWebSocket>(TaskCreationOptions.RunContinuationsAsynchronously);
        OnceWebSocketConnection((ws, _) => result.TrySetResult(ws));
        return result.Task;
    }

    public void SendOnWebSocketConnection(string data) => OnceWebSocketConnection((ws, _) => ws.Send(data));

    public void SendOnWebSocketConnection(byte[] data) => OnceWebSocketConnection((ws, _) => ws.Send(data));

    public override string ToString() => $"Server({Prefix})";

    private async Task AcceptLoopAsync()
    {
        while (!_stopped)
        {
            TcpClient client;
            try
            {
                client = await SafeAccept.AcceptAsync(_listener).ConfigureAwait(false);
            }
            catch (Exception)
            {
                if (_stopped)
                {
                    return;
                }
                continue;
            }
            _ = ServeAsync(client);
        }
    }

    private async Task ServeAsync(TcpClient client)
    {
        client.NoDelay = true;
        var socket = new ServerSocket(client);
        lock (_lock)
        {
            _sockets.Add(socket);
        }
        try
        {
            if (_certificate != null)
            {
                var ssl = new SslStream(client.GetStream(), false);
                socket.Stream = ssl;
                await ssl.AuthenticateAsServerAsync(new SslServerAuthenticationOptions { ServerCertificate = _certificate }).ConfigureAwait(false);
            }
            var head = socket.ReadHeadAsync();
            while (true)
            {
                var request = await head.ConfigureAwait(false);
                if (request == null)
                {
                    break;
                }
                if (string.Equals(request.Headers["upgrade"], "websocket", StringComparison.OrdinalIgnoreCase))
                {
                    await OnUpgradeAsync(request, socket).ConfigureAwait(false);
                    return;
                }
                var body = socket.ReadBodyAsync(request);
                request.PostBody = body;
                var response = new ServerResponse(request, socket);
                OnRequest(request, response);
                await body.ConfigureAwait(false);
                // Reading the next request also notices that the client went away while the response is pending.
                head = socket.ReadHeadAsync();
                await Task.WhenAny(response.Ended, head, socket.Closed).ConfigureAwait(false);
                if (!response.Ended.IsCompleted)
                {
                    if (head.IsCompleted && !head.IsFaulted && head.Result != null)
                    {
                        // The next request came early.
                        await Task.WhenAny(response.Ended, socket.Closed).ConfigureAwait(false);
                    }
                    if (!response.Ended.IsCompleted)
                    {
                        break;
                    }
                }
                if (!response.KeepAlive)
                {
                    await socket.FlushAsync().ConfigureAwait(false);
                    break;
                }
            }
        }
        catch (Exception)
        {
            // The browser closed the connection, which is legit when a tab goes away during a request,
            // or it is an https connection to an http server.
        }
        socket.Destroy();
        lock (_lock)
        {
            _sockets.Remove(socket);
        }
    }

    private void OnRequest(ServerRequest request, ServerResponse response)
    {
        var pathWithSearch = request.PathWithSearch;
        (string Username, string Password)? auth = null;
        TaskCompletionSource<ServerRequest> subscriber = null;
        Func<ServerRequest, ServerResponse, Task> handler;
        lock (_lock)
        {
            if (_auths.TryGetValue(pathWithSearch, out var value))
            {
                auth = value;
            }
        }
        if (auth != null)
        {
            var parts = (request.Headers["authorization"] ?? string.Empty).Split(' ');
            var credentials = string.Empty;
            try
            {
                credentials = Encoding.UTF8.GetString(Convert.FromBase64String(parts.Length > 1 ? parts[1] : string.Empty));
            }
            catch (FormatException)
            {
            }
            if (credentials != $"{auth.Value.Username}:{auth.Value.Password}")
            {
                response.WriteHead(401, new() { ["WWW-Authenticate"] = "Basic realm=\"Secure Area\"" });
                response.End("HTTP Error 401 Unauthorized: Access is denied");
                return;
            }
        }
        lock (_lock)
        {
            // Notify request subscriber.
            if (_requestSubscribers.Remove(pathWithSearch, out subscriber))
            {
            }
            _routes.TryGetValue(pathWithSearch, out handler);
        }
        try
        {
            if (handler != null)
            {
                _ = handler(request, response).ContinueWith(
                    task => Console.Error.WriteLine($"Route handler of {pathWithSearch} failed: {task.Exception?.InnerException}"),
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted,
                    TaskScheduler.Default);
            }
            else if (pathWithSearch == "/favicon.ico")
            {
                // Headless Chrome requests the favicon after a load and logs a 404 to the console of the page.
                response.StatusCode = 204;
                response.End();
            }
            else
            {
                ServeFile(request, response);
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Route handler of {pathWithSearch} failed: {e}");
        }
        subscriber?.TrySetResult(request);
    }

    private async Task ServeFileAsync(ServerRequest request, ServerResponse response, string filePath)
    {
        var pathName = request.PathName;
        if (filePath == null)
        {
            if (pathName == "/")
            {
                pathName = "/index.html";
            }
            filePath = Path.Combine(_dirPath, pathName.Substring(1).Replace('/', Path.DirectorySeparatorChar));
        }

        if (_cachedPathPrefix != null && filePath.StartsWith(_cachedPathPrefix, StringComparison.Ordinal))
        {
            if (request.Headers["if-modified-since"] != null)
            {
                response.StatusCode = 304; // not modified
                response.End();
                return;
            }
            response.SetHeader("Cache-Control", "public, max-age=31536000, no-cache");
            response.SetHeader("Last-Modified", _startTime.ToString("yyyy-MM-dd'T'HH:mm:ss.fff'Z'", System.Globalization.CultureInfo.InvariantCulture));
        }
        else
        {
            response.SetHeader("Cache-Control", "no-cache, no-store");
        }
        string csp;
        Dictionary<string, string> extraHeaders;
        bool gzip;
        lock (_lock)
        {
            _csp.TryGetValue(pathName, out csp);
            _extraHeaders.TryGetValue(pathName, out extraHeaders);
            gzip = _gzipRoutes.Contains(pathName);
        }
        if (csp != null)
        {
            response.SetHeader("Content-Security-Policy", csp);
        }
        if (extraHeaders != null)
        {
            foreach (var (name, value) in extraHeaders)
            {
                response.SetHeader(name, value);
            }
        }

        byte[] data = null;
        try
        {
            data = await File.ReadAllBytesAsync(filePath).ConfigureAwait(false);
        }
        catch (Exception)
        {
        }
        // The HTTP transaction might be already terminated after async hop here - do nothing in this case.
        if (response.WritableEnded)
        {
            return;
        }
        var isHead = request.Method == "HEAD";
        if (data == null)
        {
            response.StatusCode = 404;
            response.SetHeader("Content-Type", "text/plain");
            response.End(isHead ? null : $"File not found: {filePath}");
            return;
        }
        var extension = filePath.Substring(filePath.LastIndexOf('.') + 1);
        var mimeType = _mimeTypes.GetValueOrDefault(extension, "application/octet-stream");
        var isTextEncoding = Regex.IsMatch(mimeType, @"^text/|^application/(javascript|json)");
        response.SetHeader("Content-Type", isTextEncoding ? $"{mimeType}; charset=utf-8" : mimeType);
        if (gzip)
        {
            response.SetHeader("Content-Encoding", "gzip");
            using var compressed = new MemoryStream();
            using (var gzipStream = new GZipStream(compressed, CompressionLevel.Optimal, true))
            {
                await gzipStream.WriteAsync(data).ConfigureAwait(false);
            }
            data = compressed.ToArray();
        }
        if (isHead)
        {
            response.End();
        }
        else
        {
            response.End(data);
        }
    }

    private async Task OnUpgradeAsync(ServerRequest request, ServerSocket socket)
    {
        var upgraded = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        void DoUpgrade()
        {
            var key = request.Headers["sec-websocket-key"] ?? string.Empty;
            var accept = Convert.ToBase64String(SHA1.HashData(Encoding.ASCII.GetBytes(key + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11")));
            // The ws package answers with the first protocol that the client asks for.
            var protocol = (request.Headers["sec-websocket-protocol"] ?? string.Empty).Split(',')[0].Trim();
            var protocolHeader = protocol.Length == 0 ? string.Empty : $"Sec-WebSocket-Protocol: {protocol}\r\n";
            socket.Write($"HTTP/1.1 101 Switching Protocols\r\nUpgrade: websocket\r\nConnection: Upgrade\r\nSec-WebSocket-Accept: {accept}\r\n{protocolHeader}\r\n");
            var webSocket = new ServerWebSocket(socket);
            List<Action<ServerWebSocket, ServerRequest>> listeners;
            lock (_lock)
            {
                listeners = [.. _webSocketListeners];
                _webSocketListeners.Clear();
            }
            foreach (var listener in listeners)
            {
                listener(webSocket, request);
            }
            webSocket.Start();
            upgraded.TrySetResult(true);
        }

        TaskCompletionSource<ServerUpgradeActions> upgradeCallback;
        lock (_lock)
        {
            upgradeCallback = _upgradeCallback;
        }
        if (upgradeCallback != null)
        {
            upgradeCallback.TrySetResult(new ServerUpgradeActions(DoUpgrade, socket));
        }
        else
        {
            var pathName = request.PathName;
            if (pathName == "/ws-401")
            {
                socket.Write("HTTP/1.1 401 Unauthorized\r\n\r\nUnauthorized body");
                await socket.FlushAsync().ConfigureAwait(false);
                socket.Destroy();
                return;
            }
            if (pathName == "/ws-slow")
            {
                await Task.Delay(2000).ConfigureAwait(false);
            }
            if (pathName != "/ws" && pathName != "/ws-slow")
            {
                socket.Write("HTTP/1.1 400 Bad Request\r\n\r\n");
                await socket.FlushAsync().ConfigureAwait(false);
                socket.Destroy();
                return;
            }
            DoUpgrade();
        }
        // The connection stays in the set of the server, so that reset() closes it.
        await Task.WhenAny(upgraded.Task, socket.Closed).ConfigureAwait(false);
        await socket.Closed.ConfigureAwait(false);
        lock (_lock)
        {
            _sockets.Remove(socket);
        }
    }
}
