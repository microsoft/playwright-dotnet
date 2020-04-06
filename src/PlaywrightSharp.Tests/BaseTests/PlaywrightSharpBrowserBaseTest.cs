using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;
using System.Collections.Generic;

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
        public async Task InitializeAsync()
        {
            await AdditionalInitializeAsync();
        }

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public async Task DisposeAsync()
        {
            await Task.WhenAll(_contexts.ConvertAll(context => context.CloseAsync()));
            await AdditionalDisposeAsync();
        }

        /// <inheritdoc/>
        protected virtual Task AdditionalInitializeAsync() => Task.CompletedTask;

        /// <inheritdoc/>
        protected virtual Task AdditionalDisposeAsync() => Task.CompletedTask;
    }
}
