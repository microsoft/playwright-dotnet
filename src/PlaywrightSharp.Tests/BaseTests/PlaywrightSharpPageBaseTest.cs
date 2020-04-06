using System.Threading.Tasks;
using Xunit.Abstractions;
using Xunit;

namespace PlaywrightSharp.Tests.BaseTests
{
    /// <summary>
    /// Based on <see cref="PlaywrightSharpBrowserBaseTest"/>, this class will create a new Page.
    /// </summary>
    public class PlaywrightSharpPageBaseTest : PlaywrightSharpBrowserBaseTest
    {
        internal PlaywrightSharpPageBaseTest(ITestOutputHelper output) : base(output)
        {
        }

        internal IBrowserContext Context { get; private set; }
        internal IPage Page { get; private set; }

        /// <inheritdoc cref="IAsyncLifetime.InitializeAsync"/>
        protected override async Task AdditionalInitializeAsync()
        {
            Context = await NewContextAsync();
            Page = await Context.NewPageAsync();
        }

        /// <inheritdoc cref="IAsyncLifetime.DisposeAsync"/>
        protected override Task AdditionalDisposeAsync()
        {
            Context = null;
            Page = null;
            return Task.CompletedTask;
        }

        /// <summary>
        /// Wait for an error event.
        /// </summary>
        /// <returns>A <see cref="Task"/> that completes when the error is received</returns>
        protected Task WaitForError()
        {
            var wrapper = new TaskCompletionSource<bool>();

            void errorEvent(object sender, ErrorEventArgs e)
            {
                wrapper.SetResult(true);
                Page.Error -= errorEvent;
            }

            Page.Error += errorEvent;

            return wrapper.Task;
        }
    }
}
