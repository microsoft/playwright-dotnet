using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using Microsoft.Playwright.Tests.TestServer;

namespace Microsoft.Playwright.Tests
{
    public class HttpService : IWorkerService
    {
        private static int _availablePort = 8081;

        public SimpleServer Server { get; internal set; }
        public SimpleServer HttpsServer { get; internal set; }

        public static async Task<HttpService> Register(WorkerAwareTest test)
        {
            var workerIndex = test.WorkerIndex;
            return await test.RegisterService("Http", async () =>
            {
                var httpPort = Interlocked.Increment(ref _availablePort);
                var httpsPort = Interlocked.Increment(ref _availablePort);

                var http = new HttpService
                {
                    Server = SimpleServer.Create(_availablePort, TestUtils.FindParentDirectory("Playwright.Tests.TestServer")),
                    HttpsServer = SimpleServer.CreateHttps(httpsPort, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"))
                };

                System.Diagnostics.Debug.WriteLine($"Worker {workerIndex} assigned ports {httpPort} (HTTP) and {httpsPort} (HTTPS).");

                await Task.WhenAll(http.Server.StartAsync(), http.HttpsServer.StartAsync());
                return http;
            });
        }

        public Task ResetAsync()
        {
            Server.Reset();
            HttpsServer.Reset();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await Task.WhenAll(Server.StopAsync(), HttpsServer.StopAsync());
        }
    }
}
