using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol;
using PlaywrightSharp.Chromium.Protocol.Debugger;
using PlaywrightSharp.Chromium.Protocol.Profiler;
using PlaywrightSharp.Chromium.Protocol.Runtime;

namespace PlaywrightSharp.Chromium
{
    internal class JSCoverage
    {
        private readonly ChromiumSession _client;
        private readonly Dictionary<string, string> _scriptURLs;
        private readonly Dictionary<string, string> _scriptSources;

        private bool _enabled;
        private bool _resetOnNavigation;
        private bool _reportAnonymousScripts;

        public JSCoverage(ChromiumSession client)
        {
            _client = client;
            _enabled = false;
            _scriptURLs = new Dictionary<string, string>();
            _scriptSources = new Dictionary<string, string>();

            _resetOnNavigation = false;
        }

        internal Task StartAsync(CoverageStartOptions options)
        {
            if (_enabled)
            {
                throw new InvalidOperationException("JSCoverage is already enabled");
            }

            _resetOnNavigation = options.ResetOnNavigation;
            _reportAnonymousScripts = options.ReportAnonymousScripts;
            _enabled = true;
            _scriptURLs.Clear();
            _scriptSources.Clear();

            _client.MessageReceived += Client_MessageReceived;

            return Task.WhenAll(
                _client.SendAsync(new ProfilerEnableRequest()),
                _client.SendAsync(new ProfilerStartPreciseCoverageRequest
                {
                    CallCount = false,
                    Detailed = true,
                }),
                _client.SendAsync(new DebuggerEnableRequest()),
                _client.SendAsync(new DebuggerSetSkipAllPausesRequest
                {
                    Skip = true,
                }));
        }

        internal async Task<CoverageEntry[]> StopAsync()
        {
            if (!_enabled)
            {
                throw new InvalidOperationException("JSCoverage is not enabled");
            }

            _enabled = false;

            var profileResponseTask = _client.SendAsync<ProfilerTakePreciseCoverageResponse>(new ProfilerTakePreciseCoverageRequest());
            await Task.WhenAll(
               profileResponseTask,
               _client.SendAsync(new ProfilerStopPreciseCoverageRequest()),
               _client.SendAsync(new ProfilerDisableRequest()),
               _client.SendAsync(new DebuggerDisableRequest())).ConfigureAwait(false);

            _client.MessageReceived -= Client_MessageReceived;

            var coverage = new List<CoverageEntry>();
            foreach (var entry in profileResponseTask.Result.Result)
            {
                _scriptURLs.TryGetValue(entry.ScriptId, out string url);
                if (string.IsNullOrEmpty(url) && _reportAnonymousScripts)
                {
                    url = "debugger://VM" + entry.ScriptId;
                }

                if (string.IsNullOrEmpty(url) ||
                    !_scriptSources.TryGetValue(entry.ScriptId, out string text))
                {
                    continue;
                }

                var flattenRanges = entry.Functions.SelectMany(f => f.Ranges).ToList();
                var ranges = Coverage.ConvertToDisjointRanges(flattenRanges);
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
                    case DebuggerScriptParsedChromiumEvent debuggerScriptParsed:
                        await OnScriptParsedAsync(debuggerScriptParsed).ConfigureAwait(false);
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

        private async Task OnScriptParsedAsync(DebuggerScriptParsedChromiumEvent scriptParseResponse)
        {
            if (scriptParseResponse.Url == ChromiumPage.EvaluationScriptUrl ||
                (string.IsNullOrEmpty(scriptParseResponse.Url) && !_reportAnonymousScripts))
            {
                return;
            }

            try
            {
                var response = await _client.SendAsync(new DebuggerGetScriptSourceRequest
                {
                    ScriptId = scriptParseResponse.ScriptId,
                }).ConfigureAwait(false);
                _scriptURLs.Add(scriptParseResponse.ScriptId, scriptParseResponse.Url);
                _scriptSources.Add(scriptParseResponse.ScriptId, response.ScriptSource);
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

            _scriptURLs.Clear();
            _scriptSources.Clear();
        }
    }
}
