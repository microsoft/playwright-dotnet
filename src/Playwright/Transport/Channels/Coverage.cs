using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class Coverage : ICoverage
    {
        private readonly PageChannel _channel;

        public Coverage(PageChannel channel)
        {
            _channel = channel;
        }

        public async Task StartCSSCoverageAsync(PageStartCSSCoverageOptions options = default)
        {
            options ??= new PageStartCSSCoverageOptions();
            await _channel.StartCSSCoverageAsync(options.ResetOnNavigation);
        }

        public async Task StartJSCoverageAsync(PageStartJSCoverageOptions options = default)
        {
            options ??= new PageStartJSCoverageOptions();
            await _channel.StartJSCoverageAsync(options.ResetOnNavigation, options.ReportAnonymousScripts);
        }

        public async Task<List<PageStopCSSCoverageResult>> StopCSSCoverageAsync()
            => await _channel.StopCSSCoverageAsync();

        public async Task<List<PageStopJSCoverageResult>> StopJSCoverageAsync()
            => await _channel.StopJSCoverageAsync();
    }
}
