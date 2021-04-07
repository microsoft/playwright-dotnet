using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    /// <summary>
    /// Coverage gathers information about parts of JavaScript and CSS that were used by the page..
    /// </summary>
    public class ChromiumCoverage : ICoverage
    {
        private readonly PageChannel _channel;

        internal ChromiumCoverage(PageChannel channel)
        {
            _channel = channel;
        }

        /// <inheritdoc />
        public Task StartCSSCoverageAsync(bool? resetOnNavigation)
            => _channel.StartCSSCoverageAsync(resetOnNavigation ?? true);

        /// <inheritdoc />
        public Task StartJSCoverageAsync(bool? resetOnNavigation = true, bool? reportAnonymousScripts = false)
            => _channel.StartJSCoverageAsync(resetOnNavigation ?? true, reportAnonymousScripts ?? false);

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<CoverageStopCSSCoverageResult>> StopCSSCoverageAsync()
            => await _channel.StopCSSCoverageAsync().ConfigureAwait(false);

        /// <inheritdoc />
        public async Task<IReadOnlyCollection<CoverageStopJSCoverageResult>> StopJSCoverageAsync()
            => await _channel.StopJSCoverageAsync().ConfigureAwait(false);
    }
}
