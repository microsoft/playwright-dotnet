using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBaseTest"/>, this base class also creates a new Browser
    /// </summary>
    public class PlaywrightSharpBrowserBaseTest : PlaywrightSharpBaseTest//, IAsyncLifetime
    {
        internal IBrowser Browser { get; set; }

        //protected LaunchOptions DefaultOptions { get; set; }

        internal PlaywrightSharpBrowserBaseTest(ITestOutputHelper output) : base(output)
        {
            BaseDirectory = Path.Combine(Directory.GetCurrentDirectory(), "workspace");
            var dirInfo = new DirectoryInfo(BaseDirectory);

            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
        }

        internal Task<IBrowserContext> NewContextAsync(BrowserContextOptions options) => Browser.NewContextAsync(options);

        internal async Task<IPage> NewPageAsync(BrowserContextOptions options = null)
        {
            var context = await NewContextAsync(options);
            return await context.NewPageAsync();
        }

        /*
        public virtual async Task InitializeAsync()
            => Browser = await Puppeteer.LaunchAsync(
                DefaultOptions ?? TestConstants.DefaultBrowserOptions(),
                TestConstants.LoggerFactory);

        public virtual async Task DisposeAsync() => await Browser.CloseAsync();
        */
    }
}