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

        private readonly List<IBrowserContext> _contexts = new List<IBrowserContext>();

        internal LaunchOptions DefaultOptions { get; set; }

        internal PlaywrightSharpBrowserBaseTest(ITestOutputHelper output) : base(output)
        {
        }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public virtual Task InitializeAsync() => Task.CompletedTask;

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public virtual Task DisposeAsync() => TaskUtils.WhenAll(_contexts.ConvertAll(context => context.CloseAsync()));
    }
}
