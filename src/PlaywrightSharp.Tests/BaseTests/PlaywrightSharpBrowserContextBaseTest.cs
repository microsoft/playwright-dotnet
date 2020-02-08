using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBrowserBaseTest"/>, this calss creates a new <see cref="IBrowserContext"/>
    /// </summary>
    public class PlaywrightSharpBrowserContextBaseTest : PlaywrightSharpBrowserBaseTest
    {
        internal PlaywrightSharpBrowserContextBaseTest(ITestOutputHelper output) : base(output)
        {
        }

        internal IBrowserContext Context { get; set; }

        /*
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Context = await Browser.CreateIncognitoBrowserContextAsync();
        }
        */
    }
}
