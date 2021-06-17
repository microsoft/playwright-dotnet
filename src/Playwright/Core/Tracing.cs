using System.Threading.Tasks;
using Microsoft.Playwright.Transport.Channels;

namespace Microsoft.Playwright.Core
{
    internal partial class Tracing : ITracing
    {
        private readonly BrowserContextChannel _channel;

        public Tracing(BrowserContextChannel channel)
        {
            _channel = channel;
        }

        public Task StartAsync(TracingStartOptions options = default)
            => _channel.TracingStartAsync(
                    name: options?.Name,
                    screenshots: options?.Screenshots,
                    snapshots: options?.Snapshots);

        public async Task StopAsync(TracingStopOptions options = default)
        {
            await _channel.TracingStopAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(options?.Path))
            {
                var result = await _channel.TracingExportAsync().ConfigureAwait(false);
                var artifact = result.Object;
                await artifact.SaveAsAsync(options?.Path).ConfigureAwait(false);
                await artifact.DeleteAsync().ConfigureAwait(false);
            }
        }
    }
}
