using System.IO;
using PlaywrightSharp.TestServer;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This base tests setup logging and http servers
    /// </summary>
    public class PlaywrightSharpBaseTest
    {
        internal string BaseDirectory { get; set; }

        internal SimpleServer Server => PlaywrightSharpLoaderFixture.Server;
        internal SimpleServer HttpsServer => PlaywrightSharpLoaderFixture.HttpsServer;

        internal PlaywrightSharpBaseTest(ITestOutputHelper output)
        {
            TestConstants.SetupLogging(output);

            BaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
            var dirInfo = new DirectoryInfo(BaseDirectory);

            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }

            Initialize();
        }

        internal void Initialize()
        {
            Server.Reset();
            HttpsServer.Reset();
        }
    }
}
