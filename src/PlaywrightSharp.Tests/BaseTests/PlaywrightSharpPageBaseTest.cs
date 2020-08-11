using System;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBrowserBaseTest"/>, this class will create a new Page.
    /// </summary>
    public class PlaywrightSharpPageBaseTest : PlaywrightSharpBrowserContextBaseTest
    {
        internal PlaywrightSharpPageBaseTest(ITestOutputHelper output) : base(output)
        {
        }

        internal IPage Page { get; private set; }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            Page = await Context.NewPageAsync();
        }

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        public override async Task DisposeAsync()
        {
            await Page.CloseAsync();
            await base.DisposeAsync();
        }

        /// <summary>
        /// Wait for an error event.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the error is received</returns>
        protected Task WaitForError()
        {
            var wrapper = new TaskCompletionSource<bool>();

            void errorEvent(object sender, EventArgs e)
            {
                wrapper.SetResult(true);
                Page.Crashed -= errorEvent;
            }

            Page.Crashed += errorEvent;

            return wrapper.Task;
        }
    }
}
