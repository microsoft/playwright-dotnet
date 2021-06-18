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

        public Task StartAsync(string name, bool? screenshots, bool? snapshots)
            => _channel.TracingStartAsync(name, screenshots, snapshots);

        public async Task StopAsync(string path)
        {
            await _channel.TracingStopAsync().ConfigureAwait(false);
            if (!string.IsNullOrEmpty(path))
            {
                var result = await _channel.TracingExportAsync().ConfigureAwait(false);
                var artifact = result.Object;
                await artifact.SaveAsAsync(path).ConfigureAwait(false);
                await artifact.DeleteAsync().ConfigureAwait(false);
            }
        }
    }
}
