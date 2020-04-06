using System;
using System.Threading.Tasks;
using PlaywrightSharp.TestServer;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This class will build all http servers and download browsers
    /// </summary>
    public class PlaywrightSharpLoader
    {
        internal static SimpleServer Server { get; private set; }
        internal static SimpleServer HttpsServer { get; private set; }

        internal static async Task SetupAsync()
        {
            var downloaderTask = TestConstants.GetNewBrowserType().CreateBrowserFetcher().DownloadAsync();
            Server = SimpleServer.Create(TestConstants.Port, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));
            HttpsServer = SimpleServer.CreateHttps(TestConstants.HttpsPort, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));

            var serverStart = Server.StartAsync();
            var httpsServerStart = HttpsServer.StartAsync();

            await Task.WhenAll(downloaderTask, serverStart, httpsServerStart);
        }

        internal static Task TeardownAsync() => Task.WhenAll(Server.StopAsync(), HttpsServer.StopAsync());
    }
}
