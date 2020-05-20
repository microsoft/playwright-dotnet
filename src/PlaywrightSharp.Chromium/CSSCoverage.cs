using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.CSS;
using PlaywrightSharp.Chromium.Protocol.DOM;
using PlaywrightSharp.Chromium.Protocol.Profiler;
using PlaywrightSharp.Chromium.Protocol.Runtime;

namespace PlaywrightSharp.Chromium
{
    internal class CSSCoverage
    {
        private readonly ChromiumSession _client;
        private readonly Dictionary<string, string> _stylesheetURLs;
        private readonly Dictionary<string, string> _stylesheetSources;

        private bool _enabled;
        private bool _resetOnNavigation;

        public CSSCoverage(ChromiumSession client)
        {
            _client = client;
            _enabled = false;
            _stylesheetURLs = new Dictionary<string, string>();
            _stylesheetSources = new Dictionary<string, string>();

            _resetOnNavigation = false;
        }

        internal Task StartAsync(CoverageStartOptions options)
        {
            if (_enabled)
            {
                throw new InvalidOperationException("CSSCoverage is already enabled");
            }

            _resetOnNavigation = options.ResetOnNavigation;
            _enabled = true;
            _stylesheetURLs.Clear();
            _stylesheetSources.Clear();

            _client.MessageReceived += Client_MessageReceived;

            return Task.WhenAll(
                _client.SendAsync(new DOMEnableRequest()),
                _client.SendAsync(new CSSEnableRequest()),
                _client.SendAsync(new CSSStartRuleUsageTrackingRequest()));
        }

        internal async Task<CoverageEntry[]> StopAsync()
        {
            if (!_enabled)
            {
                throw new InvalidOperationException("CSSCoverage is not enabled");
            }

            _enabled = false;

            var trackingResponse = await _client.SendAsync(new CSSStopRuleUsageTrackingRequest()).ConfigureAwait(false);
            await Task.WhenAll(
                _client.SendAsync(new CSSDisableRequest()),
                _client.SendAsync(new DOMDisableRequest())).ConfigureAwait(false);
            _client.MessageReceived -= Client_MessageReceived;

            var styleSheetIdToCoverage = new Dictionary<string, List<CoverageRange>>();
            foreach (var entry in trackingResponse.RuleUsage)
            {
                styleSheetIdToCoverage.TryGetValue(entry.StyleSheetId, out var ranges);
                if (ranges == null)
                {
                    ranges = new List<CoverageRange>();
                    styleSheetIdToCoverage[entry.StyleSheetId] = ranges;
                }

                ranges.Add(new CoverageRange
                {
                    StartOffset = (int?)entry.StartOffset.Value,
                    EndOffset = (int?)entry.EndOffset.Value,
                    Count = entry.Used == true ? 1 : 0,
                });
            }

            var coverage = new List<CoverageEntry>();
            foreach (string styleSheetId in _stylesheetURLs.Keys)
            {
                string url = _stylesheetURLs[styleSheetId];
                string text = _stylesheetSources[styleSheetId];
                styleSheetIdToCoverage.TryGetValue(styleSheetId, out var responseRanges);
                var ranges = Coverage.ConvertToDisjointRanges(responseRanges ?? new List<CoverageRange>());
                coverage.Add(new CoverageEntry
                {
                    Url = url,
                    Ranges = ranges,
                    Text = text,
                });
            }

            return coverage.ToArray();
        }

        private async void Client_MessageReceived(object sender, IChromiumEvent e)
        {
            try
            {
                switch (e)
                {
                    case CSSStyleSheetAddedChromiumEvent cssStyleSheetAdded:
                        await OnStyleSheetAddedAsync(cssStyleSheetAdded).ConfigureAwait(false);
                        break;
                    case RuntimeExecutionContextsClearedChromiumEvent _:
                        OnExecutionContextsCleared();
                        break;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
                _client.OnClosed(ex.ToString());
            }
        }

        private async Task OnStyleSheetAddedAsync(CSSStyleSheetAddedChromiumEvent styleSheetAddedResponse)
        {
            if (string.IsNullOrEmpty(styleSheetAddedResponse.Header.SourceURL))
            {
                return;
            }

            try
            {
                var response = await _client.SendAsync(new CSSGetStyleSheetTextRequest
                {
                    StyleSheetId = styleSheetAddedResponse.Header.StyleSheetId,
                }).ConfigureAwait(false);

                _stylesheetURLs.Add(styleSheetAddedResponse.Header.StyleSheetId, styleSheetAddedResponse.Header.SourceURL);
                _stylesheetSources.Add(styleSheetAddedResponse.Header.StyleSheetId, response.Text);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void OnExecutionContextsCleared()
        {
            if (!_resetOnNavigation)
            {
                return;
            }

            _stylesheetURLs.Clear();
            _stylesheetSources.Clear();
        }
    }
}
