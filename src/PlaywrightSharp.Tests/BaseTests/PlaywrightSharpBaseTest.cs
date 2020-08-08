using System.IO;
using System.Threading.Tasks;
using PlaywrightSharp.TestServer;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// This base tests setup logging and http servers
    /// </summary>
    public class PlaywrightSharpBaseTest
    {
        internal IPlaywright Playwright => PlaywrightSharpDriverLoaderFixture.Playwright;
        internal string BaseDirectory { get; set; }
        internal IBrowserType BrowserType => Playwright[TestConstants.Product];

        internal SimpleServer Server => PlaywrightSharpLoader.Server;
        internal SimpleServer HttpsServer => PlaywrightSharpLoader.HttpsServer;

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

        internal async Task<IPage> NewPageAsync(IBrowser browser, BrowserContextOptions options = null)
        {
            await using var context = await browser.NewContextAsync(options);
            return await context.NewPageAsync();
        }
    }
}
