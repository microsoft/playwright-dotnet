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
        internal static string ContentRoot { get; private set; }

        internal static async Task SetupAsync()
        {
            var downloaderTask = TestConstants.GetNewBrowserType().CreateBrowserFetcher(new BrowserFetcherOptions
            {
                Host = !TestConstants.IsChromium ? TestConstants.BROWSER_CDN : null
            }).DownloadAsync();
            ContentRoot = TestUtils.FindParentDirectory("PlaywrightSharp.TestServer");
            Server = SimpleServer.Create(TestConstants.Port, ContentRoot);
            HttpsServer = SimpleServer.CreateHttps(TestConstants.HttpsPort, ContentRoot);

            var serverStart = Server.StartAsync();
            var httpsServerStart = HttpsServer.StartAsync();

            await Task.WhenAll(downloaderTask, serverStart, httpsServerStart);
        }

        internal static Task TeardownAsync() => Task.WhenAll(Server.StopAsync(), HttpsServer.StopAsync());
    }
}
