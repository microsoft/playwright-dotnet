using System.Threading.Tasks;
using PlaywrightSharp.Transport.Channels;

namespace PlaywrightSharp
{
    internal class Coverage : ICoverage
    {
        private readonly PageChannel _channel;

        public Coverage(PageChannel channel)
        {
            _channel = channel;
        }

        public Task StartCSSCoverageAsync(bool resetOnNavigation = true)
            => _channel.StartCSSCoverageAsync(resetOnNavigation);

        public Task StartJSCoverageAsync(bool resetOnNavigation = true, bool reportAnonymousScripts = false)
            => _channel.StartJSCoverageAsync(resetOnNavigation, reportAnonymousScripts);

        public Task<CSSCoverageEntry[]> StopCSSCoverageAsync() => _channel.StopCSSCoverageAsync();

        public Task<JSCoverageEntry[]> StopJSCoverageAsync() => _channel.StopJSCoverageAsync();
    }
}
