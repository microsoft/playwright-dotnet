using System;
using System.Threading.Tasks;
using Microsoft.Playwright.Tests.TestServer;

namespace Microsoft.Playwright.Tests
{
    public class HttpServer
    {
        internal static SimpleServer Server { get; private set; }
        internal static SimpleServer HttpsServer { get; private set; }

        internal static async Task SetupAsync()
        {
            Server = SimpleServer.Create(TestConstants.Port, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"));
            HttpsServer = SimpleServer.CreateHttps(TestConstants.HttpsPort, TestUtils.FindParentDirectory("Playwright.Tests.TestServer"));

            var serverStart = Server.StartAsync();
            var httpsServerStart = HttpsServer.StartAsync();

            await Task.WhenAll(serverStart, httpsServerStart);
        }

        internal static Task TeardownAsync() => Task.WhenAll(Server.StopAsync(), HttpsServer.StopAsync());
    }
}
