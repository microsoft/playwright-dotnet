using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Playwright.NUnit;
using Microsoft.Playwright.Tests.TestServer;
using NUnit.Framework;

namespace Microsoft.Playwright.Tests
{
    public class HttpService : IWorkerService
    {
        private static int _availablePort = 8081;

        public SimpleServer Server { get; internal set; }
        public SimpleServer HttpsServer { get; internal set; }

        public static async Task<HttpService> Register(WorkerAwareTest test)
        {
            return await test.RegisterService("Http", async () =>
            {
                int attempt = 0;
                while (attempt <= 5)
                {
                    var httpPort = Interlocked.Increment(ref _availablePort);
                    var httpsPort = Interlocked.Increment(ref _availablePort);

                    var http = new HttpService
                    {
                        Server = SimpleServer.Create(httpPort, TestUtils.FindParentDirectory("Playwright.Tests.TestServer")),
                        HttpsServer = SimpleServer.CreateHttps(httpsPort, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"))
                    };

                    TestContext.Progress.WriteLine($"Worker {test.WorkerIndex} assigned ports {httpPort} (HTTP) and {httpsPort} (HTTPS).");

                    try
                    {
                        await Task.WhenAll(http.Server.StartAsync(), http.HttpsServer.StartAsync());
                        return http;
                    }
                    catch (IOException) // likely a failed to bind to port exception
                    {
                        TestContext.Progress.WriteLine($"Worker {test.WorkerIndex} port assignemnt failed. Re-trying...");
                        attempt++;
                    }
                }

                throw new System.Exception("Could not find an empty port for Http Server");
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
