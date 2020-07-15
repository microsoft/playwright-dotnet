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
        private readonly IPlaywright _playwright;
        internal string BaseDirectory { get; set; }
        internal IBrowserType BrowserType { get; set; }

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

            _playwright = PlaywrightSharp.Playwright.CreateAsync().GetAwaiter().GetResult();
            BrowserType = _playwright[TestConstants.Product];
            Initialize();
        }

        internal void Initialize()
        {
            Server.Reset();
            HttpsServer.Reset();
        }

        /*
        internal static Task<T> WaitEventAsync<T>(ChromiumSession emitter) where T : IChromiumEvent
        {
            var completion = new TaskCompletionSource<T>();
            void handler(object sender, IChromiumEvent e)
            {
                if (e is T)
                {
                    emitter.MessageReceived -= handler;
                    completion.SetResult((T)e);
                }

                return;
            }

            emitter.MessageReceived += handler;
            return completion.Task;
        }
        */

        internal async Task<IPage> NewPageAsync(IBrowser browser, BrowserContextOptions options = null)
        {
            var context = await browser.NewContextAsync(options);
            return await context.NewPageAsync();
        }
    }
}
