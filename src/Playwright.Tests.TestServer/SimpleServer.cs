/*
 * MIT License
 *
 * Copyright (c) 2020 Darío Kondratiuk
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
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.WebSockets;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.Playwright.Tests.TestServer;

public class SimpleServer
{
    const int MaxMessageSize = 256 * 1024;

    private readonly IDictionary<string, Action<HttpContext>> _requestWaits;
    private readonly IList<Action<HttpContext>> _waitForWebSocketConnectionRequestsWaits;
    private readonly IDictionary<string, RequestDelegate> _routes;
    private readonly IDictionary<string, (string username, string password)> _auths;
    private readonly IDictionary<string, string> _csp;
    private readonly IList<string> _gzipRoutes;
    private readonly string _contentRoot;


    private ArraySegment<byte> _onWebSocketConnectionData;
    private readonly IWebHost _webHost;
    private static int _counter;
    private readonly Dictionary<int, WebSocket> _clients = new();

    public int Port { get; }
    public string Prefix { get; }
    public string CrossProcessPrefix { get; }
    public string EmptyPage { get; internal set; }

    public static SimpleServer Create(int port, string contentRoot) => new(port, contentRoot, isHttps: false);

    public static SimpleServer CreateHttps(int port, string contentRoot) => new(port, contentRoot, isHttps: true);

    public SimpleServer(int port, string contentRoot, bool isHttps)
    {
        Port = port;
        var protocol = isHttps ? "https" : "http";
        Prefix = $"{protocol}://localhost:{port}";
        CrossProcessPrefix = $"{protocol}://127.0.0.1:{port}";

        EmptyPage = $"{Prefix}/empty.html";

        _requestWaits = new ConcurrentDictionary<string, Action<HttpContext>>();
        _waitForWebSocketConnectionRequestsWaits = new List<Action<HttpContext>>();
        _routes = new ConcurrentDictionary<string, RequestDelegate>();
        _auths = new ConcurrentDictionary<string, (string username, string password)>();
        _csp = new ConcurrentDictionary<string, string>();
        _gzipRoutes = new List<string>();
        _contentRoot = contentRoot;

        _webHost = new WebHostBuilder()
            .Configure((app) => app
                .UseWebSockets()
                .UseDeveloperExceptionPage()
                .Use(middleware: async (HttpContext context, Func<Task> next) =>
                {
                    if (context.Request.Path == "/ws")
                    {
                        if (context.WebSockets.IsWebSocketRequest)
                        {
                            foreach (var wait in _waitForWebSocketConnectionRequestsWaits)
                            {
                                wait(context);
                            }
                            var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                            if (_onWebSocketConnectionData != null)
                            {
                                await webSocket.SendAsync(_onWebSocketConnectionData, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                            }
                            await ReceiveLoopAsync(webSocket, context.Request.Headers["User-Agent"].ToString().Contains("Firefox"), CancellationToken.None).ConfigureAwait(false);
                        }
                        else if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = 400;
                        }
                        return;
                    }

                    if (_auths.TryGetValue(context.Request.Path, out var auth) && !Authenticate(auth.username, auth.password, context))
                    {
                        context.Response.Headers.Add("WWW-Authenticate", "Basic realm=\"Secure Area\"");

                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        }
                        await context.Response.WriteAsync("HTTP Error 401 Unauthorized: Access is denied").ConfigureAwait(false);
                        return;
                    }
                    if (_requestWaits.TryGetValue(context.Request.Path, out var requestWait))
                    {
                        requestWait(context);
                    }
                    if (_routes.TryGetValue(context.Request.Path + context.Request.QueryString, out var handler))
                    {
                        await handler(context).ConfigureAwait(false);
                        return;
                    }

                    if (
                        context.Request.Path.ToString().Contains("/cached/") &&
                        !string.IsNullOrEmpty(context.Request.Headers["if-modified-since"]) &&
                        !context.Response.HasStarted)
                    {
                        context.Response.StatusCode = StatusCodes.Status304NotModified;
                    }

                    await ServeFile(context).ConfigureAwait(false);
                }))
            .UseKestrel(options =>
            {
                if (isHttps)
                {
                    var cert = new X509Certificate2("key.pfx", "aaaa");
                    options.Listen(IPAddress.Loopback, port, listenOptions => listenOptions.UseHttps(cert));
                }
                else
                {
                    options.Listen(IPAddress.Loopback, port);
                }
                options.Limits.MaxRequestBodySize = int.MaxValue;
            })
            .ConfigureServices((builder, services) =>
            {
                services.Configure<FormOptions>(o =>
                {
                    o.MultipartBodyLengthLimit = int.MaxValue;
                });
            })
            .Build();
    }

    public async Task ServeFile(HttpContext context)
    {
        var pathName = context.Request.Path.ToString();
        var fileName = string.IsNullOrEmpty(pathName) ? "index.html" : pathName.Substring(1);
        var filePath = Path.Combine(_contentRoot, fileName);

        context.Response.Headers["Cache-Control"] = "no-cache, no-store";

        if (_csp.TryGetValue(pathName, out string csp))
        {
            context.Response.Headers["Content-Security-Policy"] = csp;
        }
        if (!File.Exists(filePath))
        {
            context.Response.StatusCode = StatusCodes.Status404NotFound;
            return;
        }
        context.Response.StatusCode = StatusCodes.Status200OK;
        using (var file = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
        {
            context.Response.ContentType = GetContentType(filePath);

            if (_gzipRoutes.Contains(pathName))
            {
                using (var gzipFile = new MemoryStream())
                {
                    using (var compressionStream = new GZipStream(gzipFile, CompressionMode.Compress, true))
                    {
                        await file.CopyToAsync(compressionStream).ConfigureAwait(false);
                    }
                    context.Response.Headers["Content-Encoding"] = "gzip";
                    context.Response.ContentLength = gzipFile.Length;
                    gzipFile.Position = 0;
                    await gzipFile.CopyToAsync(context.Response.Body).ConfigureAwait(false);
                }
            }
            else
            {
                context.Response.ContentLength = new FileInfo(filePath).Length;
                await file.CopyToAsync(context.Response.Body).ConfigureAwait(false);
            }
        }
    }

    private string GetContentType(string fileName)
    {
        var provider = new FileExtensionContentTypeProvider();
        var extension = Path.GetExtension(fileName);
        if (provider.TryGetContentType(extension, out var contentType))
        {
            var isTextEncoding = Regex.IsMatch(contentType, @"^text\/|application\/(javascript|json)");
            if (isTextEncoding)
            {
                return $"{contentType}; charset=utf-8";
            }
            return contentType;
        }
        return "application/octet-stream";
    }

    public void SetAuth(string path, string username, string password) => _auths.Add(path, (username, password));

    public void SetCSP(string path, string csp) => _csp.Add(path, csp);

    public Task StartAsync() => _webHost.StartAsync();

    public Task StopAsync()
    {
        Reset();

        return _webHost.StopAsync();
    }

    public void Reset()
    {
        _routes.Clear();
        _auths.Clear();
        _csp.Clear();
        _requestWaits.Clear();
        _gzipRoutes.Clear();
        _onWebSocketConnectionData = null;
    }

    public void EnableGzip(string path) => _gzipRoutes.Add(path);

    public void SetRoute(string path, RequestDelegate handler) => _routes[path] = handler;

    public void SendOnWebSocketConnection(string data) => _onWebSocketConnectionData = Encoding.UTF8.GetBytes(data);

    public void SetRedirect(string from, string to) => SetRoute(from, context =>
    {
        context.Response.Redirect(to);
        return Task.CompletedTask;
    });

    public async Task<T> WaitForRequest<T>(string path, Func<HttpRequest, T> selector)
    {
        var taskCompletion = new TaskCompletionSource<T>();
        _requestWaits.Add(path, context =>
        {
            taskCompletion.SetResult(selector(context.Request));
        });

        var request = await taskCompletion.Task.ConfigureAwait(false);
        _requestWaits.Remove(path);

        return request;
    }

    public Task WaitForRequest(string path) => WaitForRequest(path, _ => true);

    public async Task<HttpRequest> WaitForWebSocketConnectionRequest()
    {
        var taskCompletion = new TaskCompletionSource<HttpRequest>();
        void entryCb(HttpContext context)
        {
            taskCompletion.SetResult(context.Request);
        };
        _waitForWebSocketConnectionRequestsWaits.Add(entryCb);

        var request = await taskCompletion.Task.ConfigureAwait(false);
        _waitForWebSocketConnectionRequestsWaits.Remove(entryCb);
        return request;
    }

    private static bool Authenticate(string username, string password, HttpContext context)
    {
        string authHeader = context.Request.Headers["Authorization"];
        if (authHeader != null && authHeader.StartsWith("Basic", StringComparison.Ordinal))
        {
            string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
            var encoding = Encoding.GetEncoding("iso-8859-1");
            string auth = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

            return auth == $"{username}:{password}";
        }
        return false;
    }

    private async Task ReceiveLoopAsync(WebSocket webSocket, bool sendCloseMessage, CancellationToken token)
    {
        int connectionId = NextConnectionId();
        _clients.Add(connectionId, webSocket);

        byte[] buffer = new byte[MaxMessageSize];

        try
        {
            while (true)
            {
                var result = await webSocket.ReceiveAsync(new(buffer), token).ConfigureAwait(false);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    if (sendCloseMessage)
                    {
                        await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Close", CancellationToken.None).ConfigureAwait(false);
                    }
                    break;
                }

                var data = await ReadFrames(result, webSocket, buffer, token).ConfigureAwait(false);

                if (data.Count == 0)
                {
                    break;
                }
            }
        }
        finally
        {
            _clients.Remove(connectionId);
        }
    }

    private async Task<ArraySegment<byte>> ReadFrames(WebSocketReceiveResult result, WebSocket webSocket, byte[] buffer, CancellationToken token)
    {
        int count = result.Count;

        while (!result.EndOfMessage)
        {
            if (count >= MaxMessageSize)
            {
                string closeMessage = $"Maximum message size: {MaxMessageSize} bytes.";
                await webSocket.CloseAsync(WebSocketCloseStatus.MessageTooBig, closeMessage, token).ConfigureAwait(false);
                return new();
            }

            result = await webSocket.ReceiveAsync(new(buffer, count, MaxMessageSize - count), token).ConfigureAwait(false);
            count += result.Count;

        }
        return new(buffer, 0, count);
    }


    private static int NextConnectionId()
    {
        int id = Interlocked.Increment(ref _counter);

        if (id == int.MaxValue)
        {
            throw new("connection id limit reached: " + id);
        }

        return id;
    }
}
