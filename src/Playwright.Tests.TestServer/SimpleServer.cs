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
using System.Reflection;
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
using Microsoft.Extensions.Logging;
using NUnit.Framework.Internal;

namespace Microsoft.Playwright.Tests.TestServer;

public class SimpleServer
{
    private readonly IDictionary<string, Action<HttpContext>> _requestSubscribers;
    private readonly ConcurrentBag<Func<WebSocketWithEvents, HttpContext, Task>> _webSocketSubscribers;
    private readonly IDictionary<string, Func<HttpContext, Task>> _routes;
    private readonly IDictionary<string, (string username, string password)> _auths;
    private readonly IDictionary<string, string> _csp;
    private readonly IList<string> _gzipRoutes;
    private readonly string _contentRoot;

    private ArraySegment<byte> _onWebSocketConnectionData;
    private readonly IWebHost _webHost;

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

        var currentExecutionContext = TestExecutionContext.CurrentContext;
        _requestSubscribers = new ConcurrentDictionary<string, Action<HttpContext>>();
        _webSocketSubscribers = [];
        _routes = new ConcurrentDictionary<string, Func<HttpContext, Task>>();
        _auths = new ConcurrentDictionary<string, (string username, string password)>();
        _csp = new ConcurrentDictionary<string, string>();
        _gzipRoutes = new List<string>();
        _contentRoot = contentRoot;

        _webHost = new WebHostBuilder()
            .ConfigureLogging(logging =>
            {
                // Allow seeing exceptions in the console output.
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Error);
            })
            .Configure((app) => app
                .UseWebSockets()
                .Use(middleware: async (HttpContext context, Func<Task> next) =>
                {
                    {
                        // This hack allows us to have Console.WriteLine etc. appear in the test output.
                        var currentContext = typeof(TestExecutionContext).GetField("AsyncLocalCurrentContext", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null) as AsyncLocal<TestExecutionContext>;
                        currentContext.Value = currentExecutionContext;
                    }
                    if (context.WebSockets.IsWebSocketRequest && context.Request.Path == "/ws")
                    {
                        var webSocket = await context.WebSockets.AcceptWebSocketAsync().ConfigureAwait(false);
                        var testWebSocket = new WebSocketWithEvents(webSocket, context.Request);
                        if (_onWebSocketConnectionData != null)
                        {
                            await webSocket.SendAsync(_onWebSocketConnectionData, WebSocketMessageType.Text, true, CancellationToken.None).ConfigureAwait(false);
                        }
                        foreach (var wait in _webSocketSubscribers)
                        {
                            await wait(testWebSocket, context).ConfigureAwait(false);
                        }
                        await testWebSocket.RunReceiveLoop().ConfigureAwait(false);
                        return;
                    }

                    if (_requestSubscribers.TryGetValue(context.Request.Path, out var requestWait))
                    {
                        requestWait(context);
                    }

                    if (_auths.TryGetValue(context.Request.Path, out var auth) && !Authenticate(auth.username, auth.password, context))
                    {
                        context.Response.Headers.Append("WWW-Authenticate", "Basic realm=\"Secure Area\"");

                        if (!context.Response.HasStarted)
                        {
                            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                        }
                        await context.Response.WriteAsync("HTTP Error 401 Unauthorized: Access is denied").ConfigureAwait(false);
                        return;
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

    public Task StartAsync(CancellationToken cancellationToken) => _webHost.StartAsync(cancellationToken);

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
        _requestSubscribers.Clear();
        _gzipRoutes.Clear();
        _onWebSocketConnectionData = null;
        _webSocketSubscribers.Clear();
    }

    public void EnableGzip(string path) => _gzipRoutes.Add(path);

    public void SetRoute(string path, Func<HttpContext, Task> handler) => _routes[path] = handler;

    public void SetRoute(string path, Action<HttpContext> handler) => _routes[path] = (context) =>
    {
        handler(context);
        return Task.CompletedTask;
    };

    public void SendOnWebSocketConnection(string data) => _onWebSocketConnectionData = Encoding.UTF8.GetBytes(data);

    public void SetRedirect(string from, string to) => SetRoute(from, context =>
    {
        context.Response.Redirect(to);
        return Task.CompletedTask;
    });

    public async Task<T> WaitForRequest<T>(string path, Func<HttpRequest, T> selector)
    {
        var taskCompletion = new TaskCompletionSource<T>();
        _requestSubscribers.Add(path, context =>
        {
            taskCompletion.SetResult(selector(context.Request));
        });

        var request = await taskCompletion.Task.ConfigureAwait(false);
        _requestSubscribers.Remove(path);

        return request;
    }

    public Task WaitForRequest(string path) => WaitForRequest(path, _ => true);

    public Task<WebSocketWithEvents> WaitForWebSocketAsync()
    {
        var tcs = new TaskCompletionSource<WebSocketWithEvents>();
        OnceWebSocketConnection((ws, _) =>
        {
            tcs.SetResult(ws);
            return Task.CompletedTask;
        });
        return tcs.Task;
    }

    public void OnceWebSocketConnection(Func<WebSocketWithEvents, HttpContext, Task> handler)
    {
        _webSocketSubscribers.Add(handler);
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
}
