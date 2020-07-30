using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;

namespace PlaywrightSharp.TestServer
{
    public class SimpleServer
    {
        private readonly IDictionary<string, Action<HttpContext>> _subscribers;
        private readonly IDictionary<string, Action<HttpContext>> _requestWaits;
        private readonly IDictionary<string, RequestDelegate> _routes;
        private readonly IDictionary<string, (string username, string password)> _auths;
        private readonly IDictionary<string, string> _csp;
        private readonly IWebHost _webHost;

        internal IList<string> GzipRoutes { get; }
        public static SimpleServer Create(int port, string contentRoot) => new SimpleServer(port, contentRoot, isHttps: false);
        public static SimpleServer CreateHttps(int port, string contentRoot) => new SimpleServer(port, contentRoot, isHttps: true);

        public SimpleServer(int port, string contentRoot, bool isHttps)
        {
            _subscribers = new ConcurrentDictionary<string, Action<HttpContext>>();
            _requestWaits = new ConcurrentDictionary<string, Action<HttpContext>>();
            _routes = new ConcurrentDictionary<string, RequestDelegate>();
            _auths = new ConcurrentDictionary<string, (string username, string password)>();
            _csp = new ConcurrentDictionary<string, string>();
            GzipRoutes = new List<string>();

            _webHost = new WebHostBuilder()
                .ConfigureAppConfiguration((context, builder) => builder
                    .SetBasePath(context.HostingEnvironment.ContentRootPath)
                    .AddEnvironmentVariables()
                )
                .Configure(app => app
#if NETCOREAPP
                    .UseWebSockets()
#endif
                    .Use(async (context, next) =>
                    {
                        if (context.Request.Path == "/ws")
                        {
                            if (context.WebSockets.IsWebSocketRequest)
                            {
                                var webSocket = await context.WebSockets.AcceptWebSocketAsync();
                                await webSocket.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes("incoming")), WebSocketMessageType.Text, true, CancellationToken.None);
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
                            await context.Response.WriteAsync("HTTP Error 401 Unauthorized: Access is denied");
                        }
                        if (_subscribers.TryGetValue(context.Request.Path, out var subscriber))
                        {
                            subscriber(context);
                        }
                        if (_requestWaits.TryGetValue(context.Request.Path, out var requestWait))
                        {
                            requestWait(context);
                        }
                        if (_routes.TryGetValue(context.Request.Path + context.Request.QueryString, out var handler))
                        {
                            await handler(context);
                            return;
                        }

                        if (
                            context.Request.Path.ToString().Contains("/cached/") &&
                            !string.IsNullOrEmpty(context.Request.Headers["if-modified-since"]) &&
                            !context.Response.HasStarted)
                        {
                            context.Response.StatusCode = StatusCodes.Status304NotModified;
                        }

                        await next();
                    })
                    .UseMiddleware<SimpleCompressionMiddleware>(this)
                    .UseStaticFiles(new StaticFileOptions
                    {
                        OnPrepareResponse = fileResponseContext =>
                        {
                            if (_csp.TryGetValue(fileResponseContext.Context.Request.Path, out string csp))
                            {
                                fileResponseContext.Context.Response.Headers["Content-Security-Policy"] = csp;
                            }

                            if (fileResponseContext.Context.Request.Path.ToString().EndsWith(".html"))
                            {
                                fileResponseContext.Context.Response.Headers["Content-Type"] = "text/html; charset=utf-8";

                                if (fileResponseContext.Context.Request.Path.ToString().Contains("/cached/"))
                                {
                                    fileResponseContext.Context.Response.Headers["Cache-Control"] = "public, max-age=31536000, no-cache";
                                    fileResponseContext.Context.Response.Headers["Last-Modified"] = DateTime.Now.ToString("s");
                                }
                                else
                                {
                                    fileResponseContext.Context.Response.Headers["Cache-Control"] = "no-cache, no-store";
                                }
                            }
                        }
                    }))
                .UseKestrel(options =>
                {
                    if (isHttps)
                    {
                        options.Listen(IPAddress.Loopback, port, listenOptions => listenOptions.UseHttps("testCert.cer"));
                    }
                    else
                    {
                        options.Listen(IPAddress.Loopback, port);
                    }
                })
                .UseContentRoot(contentRoot)
                .Build();
        }

        public void SetAuth(string path, string username, string password) => _auths.Add(path, (username, password));

        public void SetCSP(string path, string csp) => _csp.Add(path, csp);

        public Task StartAsync() => _webHost.StartAsync();

        public async Task StopAsync()
        {
            Reset();

            await _webHost.StopAsync();
        }

        public void Reset()
        {
            _routes.Clear();
            _auths.Clear();
            _csp.Clear();
            _subscribers.Clear();
            _requestWaits.Clear();
            GzipRoutes.Clear();
            foreach (var subscriber in _subscribers.Values)
            {
                subscriber(null);
            }
            _subscribers.Clear();
        }

        public void EnableGzip(string path) => GzipRoutes.Add(path);

        public void SetRoute(string path, RequestDelegate handler) => _routes.Add(path, handler);

        public void SetRedirect(string from, string to) => SetRoute(from, context =>
        {
            context.Response.Redirect(to);
            return Task.CompletedTask;
        });

        public void Subscribe(string path, Action<HttpContext> action)
            => _subscribers.Add(path, action);

        public async Task<T> WaitForRequest<T>(string path, Func<HttpRequest, T> selector)
        {
            var taskCompletion = new TaskCompletionSource<T>();
            _requestWaits.Add(path, context =>
            {
                taskCompletion.SetResult(selector(context.Request));
            });

            var request = await taskCompletion.Task;
            _requestWaits.Remove(path);

            return request;
        }

        public Task WaitForRequest(string path) => WaitForRequest(path, request => true);

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
}
