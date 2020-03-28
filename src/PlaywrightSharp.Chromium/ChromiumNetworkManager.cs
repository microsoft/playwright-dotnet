using System;
using System.Threading.Tasks;
using PlaywrightSharp.Chromium.Protocol.Network;

namespace PlaywrightSharp.Chromium
{
    internal class ChromiumNetworkManager
    {
        private readonly ChromiumSession _client;
        private readonly ChromiumPage _chromiumPage;

        public ChromiumNetworkManager(ChromiumSession client, ChromiumPage chromiumPage)
        {
            _client = client;
            _chromiumPage = chromiumPage;
        }

        internal Task InitializeAsync() => _client.SendAsync(new NetworkEnableRequest());

        internal Task SetUserAgentAsync(string userAgent)
            => _client.SendAsync(new NetworkSetUserAgentOverrideRequest { UserAgent = userAgent });

        internal void InstrumentNetworkEvents(ChromiumSession session)
        {
        }
    }
}
