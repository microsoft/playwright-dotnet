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
            string host = !TestConstants.IsChromium ? TestConstants.BrowserCDN : null;
            var browserFetcher = TestConstants.GetNewBrowserType().CreateBrowserFetcher(new BrowserFetcherOptions
            {
                Host = host
            });
            int percentage = 0;
            browserFetcher.DownloadProgressChanged += (sender, e) =>
            {
                if (percentage != e.ProgressPercentage)
                {
                    percentage = e.ProgressPercentage;
                    Console.WriteLine($"[{TestConstants.Product}] downloading browser {percentage}%");
                }
            };
            Console.WriteLine($"Downloading browser from {host}...");
            var downloaderTask = browserFetcher.DownloadAsync();

            Server = SimpleServer.Create(TestConstants.Port, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));
            HttpsServer = SimpleServer.CreateHttps(TestConstants.HttpsPort, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));

            var serverStart = Server.StartAsync();
            var httpsServerStart = HttpsServer.StartAsync();

            await Task.WhenAll(downloaderTask, serverStart, httpsServerStart);
        }

        internal static Task TeardownAsync() => Task.WhenAll(Server.StopAsync(), HttpsServer.StopAsync());
    }
}
