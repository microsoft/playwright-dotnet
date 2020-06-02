using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBaseTest"/>, this base class also creates a new Browser
    /// </summary>
    public class PlaywrightSharpBrowserBaseTest : PlaywrightSharpBaseTest, IAsyncLifetime
    {
        internal IBrowser Browser => PlaywrightSharpBrowserLoaderFixture.Browser;
        internal IBrowserApp BrowserApp => PlaywrightSharpBrowserLoaderFixture.BrowserApp;

        private readonly List<IBrowserContext> _contexts = new List<IBrowserContext>();

        internal LaunchOptions DefaultOptions { get; set; }

        internal PlaywrightSharpBrowserBaseTest(ITestOutputHelper output) : base(output)
        {
        }

        internal async Task<IBrowserContext> NewContextAsync(BrowserContextOptions options = null)
        {
            var context = await Browser.NewContextAsync(options);
            _contexts.Add(context);
            return context;
        }

        internal async Task<IPage> NewPageAsync(BrowserContextOptions options = null)
        {
            var context = await NewContextAsync(options);
            return await context.NewPageAsync();
        }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public virtual Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public virtual Task DisposeAsync() => Task.WhenAll(_contexts.ConvertAll(context => context.CloseAsync()));
    }
}
