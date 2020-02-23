using System;
using System.Threading.Tasks;

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

        internal Task InitializeAsync() => Task.CompletedTask;

        internal Task SetUserAgentAsync(string userAgent) => Task.CompletedTask;
    }
}