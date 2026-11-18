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

using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Microsoft.Playwright.Tests.Fixtures;

/// <summary>The options of <see cref="TestProxy.ForwardTo"/>.</summary>
public sealed class TestProxyForwardToOptions
{
    public bool AllowConnectRequests { get; set; }

    public string RemovePrefix { get; set; }

    public bool PreserveHostname { get; set; }
}

/// <summary>What <see cref="TestProxy.SetupSocksForwardingServer"/> returns.</summary>
public sealed class SocksForwardingServer
{
    private readonly Func<IReadOnlyList<string>> _connectHosts;
    private readonly Func<Task> _closeProxyServer;

    internal SocksForwardingServer(string proxyServerAddr, Func<IReadOnlyList<string>> connectHosts, Func<Task> closeProxyServer)
    {
        ProxyServerAddr = proxyServerAddr;
        _connectHosts = connectHosts;
        _closeProxyServer = closeProxyServer;
    }

    /// <summary><c>socks5://127.0.0.1:port</c>.</summary>
    public string ProxyServerAddr { get; }

    /// <summary>The <c>host:port</c> targets that were connected so far, without duplicates.</summary>
    public IReadOnlyList<string> ConnectHosts => _connectHosts();

    public Task CloseProxyServer() => _closeProxyServer();
}

/// <summary>
/// The HTTP proxy of the tests, the port of <c>tests/config/proxy.ts</c> and of the proxy server that it
/// builds on, <c>tests/third_party/proxy</c>. It forwards plain requests, tunnels CONNECT requests and
/// forwards WebSocket upgrades, to the host of the request or to the port of <see cref="ForwardTo"/>.
/// </summary>
public sealed class TestProxy
{
    // Certain browsers perform telemetry requests which we want to ignore.
    private static readonly HashSet<string> _connectHostsToIgnore = ["www.bing.com:443", "www.google.com:443"];

    private static readonly HashSet<string> _hopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Connection", "Keep-Alive", "Proxy-Authenticate", "Proxy-Authorization", "TE", "Trailers", "Transfer-Encoding", "Upgrade",
    };

    private static readonly string _via = $"1.1 {Dns.GetHostName()} (proxy/1.0.0)";

    private readonly object _lock = new();
    private readonly TcpListener _listener;
    private readonly HashSet<TcpClient> _sockets = [];
    private volatile string[] _connectHosts = [];
    private volatile string[] _requestUrls = [];
    private volatile string[] _requestHosts = [];
    private volatile string[] _wsUrls = [];

    // The latest first: upstream prepends the listeners of forwardTo().
    private volatile (int Port, TestProxyForwardToOptions Options)[] _forwardings = [];
    private volatile Func<ServerRequest, bool> _authenticate;
    private volatile bool _stopped;

    private TestProxy(int port)
    {
        _listener = new TcpListener(IPAddress.IPv6Any, port);
        _listener.Server.DualMode = true;
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        Url = $"http://localhost:{Port}";
        Host = $"localhost:{Port}";
        _ = AcceptLoopAsync();
    }

    /// <summary><c>proxyServer.HOST</c>.</summary>
    public string Host { get; }

    /// <summary><c>proxyServer.PORT</c>.</summary>
    public int Port { get; }

    /// <summary><c>proxyServer.URL</c>.</summary>
    public string Url { get; }

    /// <summary>The targets of the CONNECT requests that <see cref="ForwardTo"/> with <c>AllowConnectRequests</c> forwarded.</summary>
    public IReadOnlyList<string> ConnectHosts
    {
        get => _connectHosts;
        set => _connectHosts = [.. value];
    }

    /// <summary>The URLs of the plain requests that <see cref="ForwardTo"/> forwarded.</summary>
    public IReadOnlyList<string> RequestUrls
    {
        get => _requestUrls;
        set => _requestUrls = [.. value];
    }

    /// <summary>The Host headers of the plain requests that <see cref="ForwardTo"/> forwarded.</summary>
    public IReadOnlyList<string> RequestHosts
    {
        get => _requestHosts;
        set => _requestHosts = [.. value];
    }

    /// <summary>The URLs of the WebSocket upgrade requests that <see cref="ForwardTo"/> forwarded.</summary>
    public IReadOnlyList<string> WsUrls
    {
        get => _wsUrls;
        set => _wsUrls = [.. value];
    }

    /// <summary><c>TestProxy.create(port)</c>.</summary>
    public static TestProxy Create(int port) => new(port);

    /// <summary>
    /// <c>setupSocksForwardingServer({ port, forwardPort, allowedTargetPort })</c>: a SOCKS5 server on
    /// 127.0.0.1 that sends the connections to localhost at <paramref name="allowedTargetPort"/> to
    /// <paramref name="forwardPort"/> and refuses the others.
    /// </summary>
    public static Task<SocksForwardingServer> SetupSocksForwardingServer(int port, int forwardPort, int allowedTargetPort)
    {
        var allowedHosts = new[] { "127.0.0.1", "fake-localhost-127-0-0-1.nip.io", "localhost" };
        var connectHosts = new List<string>();
        var socksProxy = new SocksProxy(async connection =>
        {
            if (!allowedHosts.Contains(connection.Host) || connection.Port != allowedTargetPort)
            {
                return;
            }
            using var target = new TcpClient();
            await target.ConnectAsync(IPAddress.Loopback, forwardPort).ConfigureAwait(false);
            lock (connectHosts)
            {
                if (!connectHosts.Contains($"{connection.Host}:{connection.Port}"))
                {
                    connectHosts.Add($"{connection.Host}:{connection.Port}");
                }
            }
            var local = (IPEndPoint)target.Client.LocalEndPoint;
            await connection.SocketConnectedAsync(local.Address, local.Port).ConfigureAwait(false);

            var socket = connection.Socket;
            async Task PipeToSocketAsync()
            {
                try
                {
                    await target.GetStream().CopyToAsync(socket.GetStream()).ConfigureAwait(false);
                    socket.Client.Shutdown(SocketShutdown.Send);
                }
                catch (Exception)
                {
                    socket.Close();
                }
            }

            var toSocket = PipeToSocketAsync();
            try
            {
                await socket.GetStream().CopyToAsync(target.GetStream()).ConfigureAwait(false);
            }
            finally
            {
                target.Close();
                await toSocket.ConfigureAwait(false);
            }
        });
        socksProxy.Listen(port, "127.0.0.1");
        return Task.FromResult(new SocksForwardingServer(
            $"socks5://127.0.0.1:{port}",
            () =>
            {
                lock (connectHosts)
                {
                    return [.. connectHosts];
                }
            },
            socksProxy.CloseAsync));
    }

    public async Task StopAsync()
    {
        _stopped = true;
        Reset();
        TcpClient[] sockets;
        lock (_lock)
        {
            sockets = [.. _sockets];
            _sockets.Clear();
        }
        foreach (var socket in sockets)
        {
            socket.Close();
        }
        _listener.Stop();
        await Task.CompletedTask.ConfigureAwait(false);
    }

    /// <summary><c>proxyServer.forwardTo(port, { allowConnectRequests, removePrefix, preserveHostname })</c>.</summary>
    public void ForwardTo(int port, TestProxyForwardToOptions options = null)
    {
        lock (_lock)
        {
            _forwardings = [(port, options ?? new()), .. _forwardings];
        }
    }

    /// <summary>
    /// <c>proxyServer.setAuthHandler(req => ...)</c>. The handler gets the plain requests and the CONNECT
    /// requests, after <see cref="ForwardTo"/> rewrote <c>req.Url</c>. A request that it returns false
    /// for, or throws for, gets a 407.
    /// </summary>
    public void SetAuthHandler(Func<ServerRequest, bool> handler) => _authenticate = handler;

    public void Reset()
    {
        lock (_lock)
        {
            // Upstream keeps wsUrls.
            _connectHosts = [];
            _requestUrls = [];
            _requestHosts = [];
            _forwardings = [];
            _authenticate = null;
        }
    }

    public override string ToString() => $"TestProxy({Url})";

    private static bool IsHostNotFound(Exception e) =>
        e is SocketException { SocketErrorCode: SocketError.HostNotFound or SocketError.NoData or SocketError.TryAgain };

    private static string Date() => DateTime.UtcNow.ToString("R", CultureInfo.InvariantCulture);

    /// <summary>The <c>request</c> and <c>upgrade</c> listeners of <c>forwardTo()</c>.</summary>
    private static string Rewrite(string requestUrl, string host, int port, TestProxyForwardToOptions options, bool upgrade)
    {
        var url = ProxyUrl.Parse(requestUrl, host);
        if (!options.PreserveHostname)
        {
            url.Hostname = "127.0.0.1";
        }
        url.Port = port;
        if (!string.IsNullOrEmpty(options.RemovePrefix))
        {
            var index = url.Pathname.IndexOf(options.RemovePrefix, StringComparison.Ordinal);
            if (index != -1)
            {
                url.Pathname = url.Pathname.Remove(index, options.RemovePrefix.Length);
            }
        }
        if (upgrade && url.Scheme == "ws")
        {
            url.Scheme = "http";
        }
        else if (upgrade && url.Scheme == "wss")
        {
            url.Scheme = "https";
        }
        return url.ToString();
    }

    /// <summary>Copies both ways until one side ends, or until both sides end when <paramref name="allowHalfOpen"/>.</summary>
    private static async Task TunnelAsync(HttpReader source, TcpClient socket, HttpReader targetSource, TcpClient target, bool allowHalfOpen)
    {
        static async Task PipeAsync(HttpReader from, TcpClient to)
        {
            var stream = to.GetStream();
            while (await from.ReadSomeAsync().ConfigureAwait(false) is { } chunk)
            {
                await stream.WriteAsync(chunk).ConfigureAwait(false);
            }
            to.Client.Shutdown(SocketShutdown.Send);
        }

        var pipes = new[] { PipeAsync(source, target), PipeAsync(targetSource, socket) };
        try
        {
            var first = await Task.WhenAny(pipes).ConfigureAwait(false);
            if (allowHalfOpen && first.IsCompletedSuccessfully)
            {
                await Task.WhenAll(pipes).ConfigureAwait(false);
            }
        }
        catch (Exception)
        {
        }
        socket.Close();
        target.Close();
        foreach (var pipe in pipes)
        {
            Utils.IgnoreErrors(pipe);
        }
    }

    private static byte[] SerializeProxyRequest(ServerRequest req, ProxyUrl parsed, byte[] body)
    {
        var remoteAddress = req.Socket.RemoteAddress;
        var head = new StringBuilder();
        head.Append(CultureInfo.InvariantCulture, $"{req.Method} {parsed.Pathname}{parsed.Search} HTTP/1.1\r\n");
        var hasXForwardedFor = false;
        var hasVia = false;
        for (var i = 0; i < req.RawHeaders.Count; i += 2)
        {
            var (key, value) = (req.RawHeaders[i], req.RawHeaders[i + 1]);
            if (!hasXForwardedFor && key.Equals("x-forwarded-for", StringComparison.OrdinalIgnoreCase))
            {
                // append to existing "X-Forwarded-For" header
                hasXForwardedFor = true;
                value += ", " + remoteAddress;
            }
            if (!hasVia && key.Equals("via", StringComparison.OrdinalIgnoreCase))
            {
                // append to existing "Via" header
                hasVia = true;
                value += ", " + _via;
            }
            if (!_hopByHopHeaders.Contains(key))
            {
                head.Append(key).Append(": ").Append(value).Append("\r\n");
            }
        }
        if (!hasXForwardedFor)
        {
            head.Append("X-Forwarded-For: ").Append(remoteAddress).Append("\r\n");
        }
        if (!hasVia)
        {
            head.Append("Via: ").Append(_via).Append("\r\n");
        }
        if (req.Headers["host"] == null)
        {
            head.Append("Host: ").Append(parsed.Host).Append("\r\n");
        }
        var chunked = req.Headers["transfer-encoding"] != null;
        if (chunked)
        {
            head.Append("Transfer-Encoding: chunked\r\n");
        }
        head.Append("Connection: keep-alive\r\n\r\n");

        using var request = new MemoryStream();
        request.Write(Encoding.Latin1.GetBytes(head.ToString()));
        if (chunked && body.Length > 0)
        {
            request.Write(Encoding.ASCII.GetBytes(body.Length.ToString("x", CultureInfo.InvariantCulture) + "\r\n"));
        }
        request.Write(body);
        if (chunked)
        {
            request.Write(Encoding.ASCII.GetBytes(body.Length > 0 ? "\r\n0\r\n\r\n" : "0\r\n\r\n"));
        }
        return request.ToArray();
    }

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
        AddSocket(client);
        var socket = new ServerSocket(client);
        var reader = new HttpReader(client.GetStream());
        var head = reader.ReadHeadAsync();
        try
        {
            while (await head.ConfigureAwait(false) is { } message)
            {
                var requestLine = message.StartLine.Split(' ', 3);
                if (requestLine.Length < 3)
                {
                    break;
                }
                var (method, url, httpVersion) = (requestLine[0], requestLine[1], requestLine[2].Replace("HTTP/", string.Empty));
                var headers = new ServerHeaders(message.RawHeaders);
                ServerRequest Request(string rewrittenUrl) => new(method, rewrittenUrl, httpVersion, message.RawHeaders, socket);

                if (method == "CONNECT")
                {
                    await OnConnectAsync(Request(RewriteConnect(url)), client, reader).ConfigureAwait(false);
                    return;
                }
                if (headers["upgrade"] != null && (headers["connection"] ?? string.Empty).Contains("upgrade", StringComparison.OrdinalIgnoreCase))
                {
                    await OnUpgradeAsync(Request(RewriteUpgrade(url, headers["host"])), client, reader).ConfigureAwait(false);
                    return;
                }

                using var body = new MemoryStream();
                await foreach (var chunk in reader.ReadBodyAsync(headers, false).ConfigureAwait(false))
                {
                    body.Write(chunk);
                }
                // Reading the next request also notices that the client went away while the response is pending.
                head = reader.ReadHeadAsync();
                if (!await OnRequestAsync(Request(RewriteRequest(url, headers["host"])), body.ToArray(), head).ConfigureAwait(false))
                {
                    break;
                }
            }
        }
        catch (Exception)
        {
            // ECONNRESET and alike are legit errors given that tab closing aborts outgoing connections to the server.
        }
        finally
        {
            socket.Destroy();
            Utils.IgnoreErrors(head);
            RemoveSocket(client);
        }
    }

    private string RewriteRequest(string url, string host)
    {
        foreach (var (port, options) in _forwardings)
        {
            lock (_lock)
            {
                _requestUrls = [.. _requestUrls, url];
                _requestHosts = [.. _requestHosts, host];
            }
            url = Rewrite(url, host, port, options, false);
        }
        return url;
    }

    private string RewriteConnect(string url)
    {
        foreach (var (port, options) in _forwardings)
        {
            if (!options.AllowConnectRequests || _connectHostsToIgnore.Contains(url))
            {
                continue;
            }
            lock (_lock)
            {
                _connectHosts = [.. _connectHosts, url];
            }
            url = $"127.0.0.1:{port}";
        }
        return url;
    }

    private string RewriteUpgrade(string url, string host)
    {
        foreach (var (port, options) in _forwardings)
        {
            lock (_lock)
            {
                _wsUrls = [.. _wsUrls, url];
            }
            url = Rewrite(url, host, port, options, true);
        }
        return url;
    }

    private bool Authenticate(ServerRequest request)
    {
        try
        {
            return _authenticate?.Invoke(request) ?? true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>HTTP GET/POST/DELETE/PUT, etc. proxy requests. Returns whether the connection takes another request.</summary>
    private async Task<bool> OnRequestAsync(ServerRequest req, byte[] body, Task<(string StartLine, List<string> RawHeaders)?> nextHead)
    {
        var res = new ServerResponse(req, req.Socket);
        if (!Authenticate(req))
        {
            res.WriteHead(407, new() { ["Proxy-Authenticate"] = "Basic realm=\"proxy\"" });
            res.End("Proxy authorization required");
        }
        else
        {
            var parsed = ProxyUrl.Parse(req.Url, "localhost");
            if (parsed.Scheme != "http")
            {
                // only "http://" is supported, "https://" should use CONNECT method
                res.WriteHead(400);
                res.End($"Only \"http:\" protocol prefix is supported (got: \"{parsed.Scheme}:\")\n");
            }
            else
            {
                await ProxyRequestAsync(req, res, parsed, body, nextHead).ConfigureAwait(false);
            }
        }
        await res.Ended.ConfigureAwait(false);
        return res.KeepAlive && !req.Socket.Destroyed;
    }

    private async Task ProxyRequestAsync(ServerRequest req, ServerResponse res, ProxyUrl parsed, byte[] body, Task<(string StartLine, List<string> RawHeaders)?> nextHead)
    {
        using var target = new TcpClient();
        AddSocket(target);
        // if the client closes the connection prematurely, then close the upstream socket
        _ = nextHead.ContinueWith(
            task =>
            {
                if (task.IsFaulted || task.Result == null)
                {
                    _ = task.Exception;
                    target.Close();
                }
            },
            TaskScheduler.Default);
        try
        {
            HttpReader reader;
            int statusCode;
            List<string> rawHeaders;
            try
            {
                await target.ConnectAsync(parsed.Hostname.Trim('[', ']'), parsed.Port ?? 80).ConfigureAwait(false);
                target.NoDelay = true;
                await target.GetStream().WriteAsync(SerializeProxyRequest(req, parsed, body)).ConfigureAwait(false);
                reader = new HttpReader(target.GetStream());
                do
                {
                    var message = await reader.ReadHeadAsync().ConfigureAwait(false) ?? throw new IOException("socket hang up");
                    statusCode = int.Parse(message.StartLine.Split(' ')[1], CultureInfo.InvariantCulture);
                    rawHeaders = message.RawHeaders;
                }
                while (statusCode < 200);
            }
            catch (Exception e)
            {
                res.WriteHead(IsHostNotFound(e) ? 404 : 500);
                res.End();
                return;
            }

            var headers = new Dictionary<string, object>();
            for (var i = 0; i < rawHeaders.Count; i += 2)
            {
                var (key, value) = (rawHeaders[i], rawHeaders[i + 1]);
                if (_hopByHopHeaders.Contains(key))
                {
                    continue;
                }
                if (headers.TryGetValue(key, out var existing))
                {
                    headers[key] = (existing as string[] ?? new[] { (string)existing }).Append(value).ToArray();
                }
                else
                {
                    headers[key] = value;
                }
            }
            res.WriteHead(statusCode, headers);
            if (req.Method != "HEAD" && statusCode != 204 && statusCode != 304)
            {
                await foreach (var chunk in reader.ReadBodyAsync(new ServerHeaders(rawHeaders), true).ConfigureAwait(false))
                {
                    res.Write(chunk);
                    await req.Socket.FlushAsync().ConfigureAwait(false);
                }
            }
            res.End();
        }
        catch (Exception)
        {
            // already sent a response, just destroying the socket...
            req.Socket.Destroy();
        }
        finally
        {
            RemoveSocket(target);
        }
    }

    /// <summary>HTTP CONNECT proxy requests.</summary>
    private async Task OnConnectAsync(ServerRequest req, TcpClient socket, HttpReader reader)
    {
        // Upstream answers with a ServerResponse of its own that does not keep the connection alive and
        // does not frame the body, and ends the socket after an error.
        async Task EndAsync(string status, string headers, string body)
        {
            req.Socket.Write($"HTTP/1.1 {status}\r\n{headers}Date: {Date()}\r\nConnection: close\r\n\r\n{body}");
            req.Socket.End();
            while (await reader.ReadSomeAsync().ConfigureAwait(false) != null)
            {
            }
        }

        if (!Authenticate(req))
        {
            await EndAsync("407 Proxy Authentication Required", "Proxy-Authenticate: Basic realm=\"proxy\"\r\n", "Proxy authorization required").ConfigureAwait(false);
            return;
        }

        // `req.url` should look like "example.com:443"
        var lastColon = req.Url.LastIndexOf(':');
        var host = req.Url.Substring(0, lastColon).Trim('[', ']');
        var port = int.Parse(req.Url.Substring(lastColon + 1), CultureInfo.InvariantCulture);

        using var target = new TcpClient();
        AddSocket(target);
        try
        {
            try
            {
                await target.ConnectAsync(host, port).ConfigureAwait(false);
            }
            catch (Exception e)
            {
                await EndAsync(IsHostNotFound(e) ? "404 Not Found" : "500 Internal Server Error", string.Empty, string.Empty).ConfigureAwait(false);
                return;
            }
            target.NoDelay = true;
            req.Socket.Write($"HTTP/1.1 200 Connection established\r\nDate: {Date()}\r\nConnection: close\r\n\r\n");
            await req.Socket.FlushAsync().ConfigureAwait(false);
            await TunnelAsync(reader, socket, new HttpReader(target.GetStream()), target, false).ConfigureAwait(false);
        }
        finally
        {
            RemoveSocket(target);
        }
    }

    private async Task OnUpgradeAsync(ServerRequest req, TcpClient socket, HttpReader reader)
    {
        var parsed = ProxyUrl.Parse(req.Url, null);
        if (parsed.Hostname.Length == 0 || parsed.Scheme != "http")
        {
            // http.request(req.url) throws for a URL that is relative or not http:.
            return;
        }

        using var target = new TcpClient();
        AddSocket(target);
        try
        {
            await target.ConnectAsync(parsed.Hostname.Trim('[', ']'), parsed.Port ?? 80).ConfigureAwait(false);
            target.NoDelay = true;
            var head = new StringBuilder();
            head.Append(CultureInfo.InvariantCulture, $"{req.Method} {parsed.Pathname}{parsed.Search} HTTP/1.1\r\n");
            foreach (var (key, value) in req.Headers)
            {
                head.Append(key).Append(": ").Append(value).Append("\r\n");
            }
            head.Append("\r\n");
            await target.GetStream().WriteAsync(Encoding.Latin1.GetBytes(head.ToString())).ConfigureAwait(false);

            var targetReader = new HttpReader(target.GetStream());
            var message = await targetReader.ReadHeadAsync().ConfigureAwait(false);
            if (message?.StartLine.Split(' ')[1] != "101")
            {
                return;
            }
            head.Clear().Append("HTTP/1.1 101 Switching Protocols\r\n");
            foreach (var (key, value) in new ServerHeaders(message.Value.RawHeaders))
            {
                head.Append(key).Append(": ").Append(value).Append("\r\n");
            }
            head.Append("\r\n");
            req.Socket.Write(Encoding.Latin1.GetBytes(head.ToString()));
            await req.Socket.FlushAsync().ConfigureAwait(false);
            await TunnelAsync(reader, socket, targetReader, target, true).ConfigureAwait(false);
        }
        finally
        {
            RemoveSocket(target);
        }
    }

    private void AddSocket(TcpClient socket)
    {
        lock (_lock)
        {
            _sockets.Add(socket);
        }
    }

    private void RemoveSocket(TcpClient socket)
    {
        lock (_lock)
        {
            _sockets.Remove(socket);
        }
    }

    /// <summary><c>new URL(url, 'http://' + baseHost)</c>, as far as the proxy needs it: nothing is normalized but the default port.</summary>
    private sealed class ProxyUrl
    {
        public string Scheme { get; set; } = "http";

        public string Hostname { get; set; }

        public int? Port { get; set; }

        public string Pathname { get; set; }

        public string Search { get; private set; }

        public string Host => Port == null ? Hostname : $"{Hostname}:{Port}";

        public static ProxyUrl Parse(string url, string baseHost)
        {
            var result = new ProxyUrl();
            var authority = baseHost ?? string.Empty;
            var rest = url;
            var schemeEnd = url.StartsWith('/') ? -1 : url.IndexOf("://", StringComparison.Ordinal);
            if (schemeEnd != -1)
            {
                result.Scheme = url.Substring(0, schemeEnd).ToLowerInvariant();
                var authorityEnd = url.IndexOfAny(['/', '?', '#'], schemeEnd + 3);
                authorityEnd = authorityEnd == -1 ? url.Length : authorityEnd;
                authority = url.Substring(schemeEnd + 3, authorityEnd - schemeEnd - 3);
                rest = url.Substring(authorityEnd);
            }

            authority = authority.Substring(authority.LastIndexOf('@') + 1);
            var colon = authority.LastIndexOf(':');
            var hasPort = colon > authority.LastIndexOf(']') && colon < authority.Length - 1;
            result.Hostname = (colon > authority.LastIndexOf(']') ? authority.Substring(0, colon) : authority).ToLowerInvariant();
            result.Port = hasPort ? int.Parse(authority.Substring(colon + 1), CultureInfo.InvariantCulture) : null;

            var pathEnd = rest.IndexOfAny(['?', '#']);
            result.Pathname = pathEnd == -1 ? rest : rest.Substring(0, pathEnd);
            result.Search = pathEnd == -1 ? string.Empty : rest.Substring(pathEnd);
            return result;
        }

        public override string ToString()
        {
            var defaultPort = Scheme is "https" or "wss" ? 443 : 80;
            var host = Port == null || Port == defaultPort ? Hostname : $"{Hostname}:{Port}";
            var pathname = Pathname.StartsWith('/') ? Pathname : "/" + Pathname;
            return $"{Scheme}://{host}{pathname}{Search}";
        }
    }

    /// <summary>Reads HTTP messages, and then the bytes that follow them, from one side of a connection.</summary>
    private sealed class HttpReader
    {
        private readonly Stream _stream;
        private readonly byte[] _buffer = new byte[64 * 1024];
        private int _start;
        private int _end;

        public HttpReader(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>The request line or the status line, and the names and the values of the headers in turn. Null at the end of the stream.</summary>
        public async Task<(string StartLine, List<string> RawHeaders)?> ReadHeadAsync()
        {
            var startLine = await ReadLineAsync().ConfigureAwait(false);
            while (startLine != null && startLine.Length == 0)
            {
                startLine = await ReadLineAsync().ConfigureAwait(false);
            }
            if (startLine == null)
            {
                return null;
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
            return (startLine, rawHeaders);
        }

        /// <summary>The body of the message with these headers, in the pieces that arrive.</summary>
        public async IAsyncEnumerable<byte[]> ReadBodyAsync(ServerHeaders headers, bool untilClose)
        {
            if ((headers["transfer-encoding"] ?? string.Empty).Contains("chunked", StringComparison.OrdinalIgnoreCase))
            {
                while (true)
                {
                    var sizeLine = await ReadLineAsync().ConfigureAwait(false) ?? throw new IOException("Unexpected end of chunked body");
                    var size = long.Parse(sizeLine.Split(';')[0].Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
                    if (size == 0)
                    {
                        // Trailers, then the empty line.
                        while (!string.IsNullOrEmpty(await ReadLineAsync().ConfigureAwait(false)))
                        {
                        }
                        yield break;
                    }
                    await foreach (var chunk in ReadExactlyAsync(size).ConfigureAwait(false))
                    {
                        yield return chunk;
                    }
                    await ReadLineAsync().ConfigureAwait(false);
                }
            }
            else if (headers["content-length"] is { } contentLength)
            {
                await foreach (var chunk in ReadExactlyAsync(long.Parse(contentLength, CultureInfo.InvariantCulture)).ConfigureAwait(false))
                {
                    yield return chunk;
                }
            }
            else if (untilClose)
            {
                while (await ReadSomeAsync().ConfigureAwait(false) is { } chunk)
                {
                    yield return chunk;
                }
            }
        }

        /// <summary>The bytes that are there, at most <paramref name="max"/> of them. Null at the end of the stream.</summary>
        public async Task<byte[]> ReadSomeAsync(long max = long.MaxValue)
        {
            if (_start == _end && !await FillAsync().ConfigureAwait(false))
            {
                return null;
            }
            var count = (int)Math.Min(max, _end - _start);
            var chunk = _buffer.AsSpan(_start, count).ToArray();
            _start += count;
            return chunk;
        }

        private async IAsyncEnumerable<byte[]> ReadExactlyAsync(long size)
        {
            while (size > 0)
            {
                var chunk = await ReadSomeAsync(size).ConfigureAwait(false) ?? throw new IOException("Unexpected end of body");
                size -= chunk.Length;
                yield return chunk;
            }
        }

        private async Task<bool> FillAsync()
        {
            _start = 0;
            _end = await _stream.ReadAsync(_buffer).ConfigureAwait(false);
            return _end > 0;
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
                var count = (newline == -1 ? _end : newline) - _start;
                line.Write(_buffer, _start, count);
                _start += count;
                if (newline != -1)
                {
                    _start++;
                    return Encoding.Latin1.GetString(line.ToArray()).TrimEnd('\r');
                }
            }
        }
    }
}
