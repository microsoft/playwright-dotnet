using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Playwright.Helpers;
using Xunit;
using Xunit.Abstractions;

namespace Microsoft.Playwright.Tests.BaseTests
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
        public virtual async Task DisposeAsync()
        {
            await Context.CloseAsync();
        }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public virtual async Task InitializeAsync()
        {
            Context = await Browser.NewContextAsync();
            Context.DefaultTimeout = TestConstants.DefaultPuppeteerTimeout;
        }
    }
}
