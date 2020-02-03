using PlaywrightSharp.TestServer;
using System;
using System.Threading.Tasks;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This class will build all http servers and download browsers
    /// </summary>
    public class PlaywrightSharpLoaderFixture : IDisposable
    {
        internal static SimpleServer Server { get; private set; }
        internal static SimpleServer HttpsServer { get; private set; }

        internal PlaywrightSharpLoaderFixture()
        {
            //SetupAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public void Dispose()
        {
            Task.WaitAll(Server.StopAsync(), HttpsServer.StopAsync());
        }

        /*
        private async Task SetupAsync()
        {
            var downloaderTask = new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultRevision);

            Server = SimpleServer.Create(TestConstants.Port, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));
            HttpsServer = SimpleServer.CreateHttps(TestConstants.HttpsPort, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));

            var serverStart = Server.StartAsync();
            var httpsServerStart = HttpsServer.StartAsync();

            await Task.WhenAll(downloaderTask, serverStart, httpsServerStart);
        }
        */
    }
}
