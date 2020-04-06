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
        private static readonly object _lock = new object();
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

        private Task SetupAsync()
        {
            if (!started)
            {
                lock (_lock)
                {
                    if (started)
                    {
                        return Task.CompletedTask;
                    }

                    started = true;

                    var downloaderTask = TestConstants.GetNewBrowserType().CreateBrowserFetcher().DownloadAsync();

                    Server = SimpleServer.Create(TestConstants.Port, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));
                    HttpsServer = SimpleServer.CreateHttps(TestConstants.HttpsPort, TestUtils.FindParentDirectory("PlaywrightSharp.TestServer"));

                    var serverStart = Server.StartAsync();
                    var httpsServerStart = HttpsServer.StartAsync();

                    return Task.WhenAll(downloaderTask, serverStart, httpsServerStart);
                }
            }

            return Task.CompletedTask;
        }
    }
}
