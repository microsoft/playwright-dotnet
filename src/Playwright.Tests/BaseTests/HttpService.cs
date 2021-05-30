using System;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnitTest;
using Microsoft.Playwright.Tests.TestServer;

namespace Microsoft.Playwright.Tests
{
    public class HttpService : IWorkerService
    {
        public SimpleServer Server { get; internal set; }
        public SimpleServer HttpsServer { get; internal set; }

        public static async Task<HttpService> Register(WorkerAwareTest test)
        {
            var workerIndex = test.WorkerIndex;
            return await test.RegisterService("Http", async () =>
            {
                var http = new HttpService();
                http.Server = SimpleServer.Create(8081 + workerIndex * 2, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"));
                http.HttpsServer = SimpleServer.CreateHttps(8081 + workerIndex * 2 + 1, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"));
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
