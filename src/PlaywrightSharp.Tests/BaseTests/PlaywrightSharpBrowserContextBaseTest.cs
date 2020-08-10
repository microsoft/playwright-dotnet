using System.IO;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;
using PlaywrightSharp.Helpers;
using System;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBrowserBaseTest"/>, this calss creates a new <see cref="IBrowserContext"/>
    /// </summary>
    public class PlaywrightSharpBrowserContextBaseTest : PlaywrightSharpBrowserBaseTest, IAsyncLifetime
    {
        internal PlaywrightSharpBrowserContextBaseTest(ITestOutputHelper output) : base(output)
        {
        }

        internal IBrowserContext Context { get; set; }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public virtual Task DisposeAsync() => Context.CloseAsync();

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public virtual async Task InitializeAsync()
        {
            try
            {
                Context = await Browser.NewContextAsync().WithTimeout();
            }
            catch (TimeoutException)
            {
                await PlaywrightSharpBrowserLoaderFixture.RestartAsync();
                Context = await Browser.NewContextAsync().WithTimeout();
            }
        }
    }
}
