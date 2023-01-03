using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Playwright.Core;

namespace Microsoft.Playwright.Transport.Channels
{
    internal class Coverage : ICoverage
    {
        private readonly PageChannel _channel;
        private readonly BrowserContext _context;

        public Coverage(BrowserContext context, PageChannel channel)
        {
            _context = context;
            _channel = channel;
        }

        public async Task StartCSSCoverageAsync(PageStartCSSCoverageOptions options = default)
        {
            if (!_context.IsChromium)
            {
                throw new NotSupportedException("This browser doesn't support this action.");
            }
            options ??= new PageStartCSSCoverageOptions();
            await _channel.StartCSSCoverageAsync(options.ResetOnNavigation);
        }

        public async Task StartJSCoverageAsync(PageStartJSCoverageOptions options = default)
        {
            if (!_context.IsChromium)
            {
                throw new NotSupportedException("This browser doesn't support this action.");
            }
            options ??= new PageStartJSCoverageOptions();
            await _channel.StartJSCoverageAsync(options.ResetOnNavigation, options.ReportAnonymousScripts);
        }

        public async Task<List<PageStopCSSCoverageResult>> StopCSSCoverageAsync()
        {
            if (!_context.IsChromium)
            {
                throw new NotSupportedException("This browser doesn't support this action.");
            }
            return await _channel.StopCSSCoverageAsync();
        }

        public async Task<List<PageStopJSCoverageResult>> StopJSCoverageAsync()
        {
            if (!_context.IsChromium)
            {
                throw new NotSupportedException("This browser doesn't support this action.");
            }
            return await _channel.StopJSCoverageAsync();
        }
    }
}
