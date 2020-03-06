using PlaywrightSharp.Chromium.Protocol.Runtime;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumExecutionContext : IExecutionContextDelegate
    {
        private readonly ChromiumSession _client;
        private readonly ExecutionContextDescription _contextPayload;

        public ChromiumExecutionContext(ChromiumSession client, ExecutionContextDescription contextPayload)
        {
            _client = client;
            _contextPayload = contextPayload;
        }
    }
}