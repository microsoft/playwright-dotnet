using System;
using System.Threading;
using System.Threading.Tasks;
using PlaywrightSharp.TestServer;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This class will build all http servers and download browsers
    /// </summary>
    public class PlaywrightSharpLoaderFixture : IDisposable
    {
        private static bool started = false;
        internal static SimpleServer Server { get; private set; }
        internal static SimpleServer HttpsServer { get; private set; }

        /// <inheritdoc/>
        public PlaywrightSharpLoaderFixture()
        {
            SetupAsync().GetAwaiter().GetResult();
        }

        /// <inheritdoc />
        public virtual void Dispose()
        {
            Task.WaitAll(Server.StopAsync(), HttpsServer.StopAsync());
        }

        private async Task SetupAsync()
        {
            if (started)
            {
                return;
            }
            started = true;

            var downloaderTask = TestConstants.GetNewBrowserType().CreateBrowserFetcher().DownloadAsync();

            Server = SimpleServer.Create(TestConstants.Port, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));
            HttpsServer = SimpleServer.CreateHttps(TestConstants.HttpsPort, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));

            var serverStart = Server.StartAsync();
            var httpsServerStart = HttpsServer.StartAsync();

            await Task.WhenAll(downloaderTask, serverStart, httpsServerStart);
        }
    }
}
